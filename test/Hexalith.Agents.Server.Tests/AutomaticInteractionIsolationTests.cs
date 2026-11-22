using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.DomainService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Untrusted tenant/person shape exits precede all dependency and owner reads.</summary>
public sealed class AutomaticInteractionIsolationTests
{
    [Theory]
    [InlineData("tenant-b", true)]
    [InlineData("tenant-a", false)]
    public async Task CrossTenantCallStartsNoWorkflow(string authenticatedTenant, bool person)
    {
        var fixture = new AutomaticInteractionReplayFixture(); var gate = Substitute.For<IAutomaticInteractionIntakeGate>(); var availability = Substitute.For<IAgentDependencyAvailability>();
        var intake = new AutomaticInteractionIntakeOrchestrator(gate, availability, fixture, fixture, fixture, fixture.Recovery());
        var result = await intake.ExecuteAsync(fixture.Request with { AuthenticatedTenantId = authenticatedTenant, IsPerson = person }, CancellationToken.None);
        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.NotFound); result.AgentInteractionId.ShouldBeNull(); result.Dependency.ShouldBeNull();
        gate.ReceivedCalls().ShouldBeEmpty(); availability.ReceivedCalls().ShouldBeEmpty(); fixture.Dispatches.ShouldBeEmpty(); fixture.Scheduled.ShouldBeEmpty(); fixture.Directory.ShouldBeNull();
    }
    [Fact]
    public async Task DefaultHostFailsWithTheExactUnavailableHostRecordBeforeAnyOwnerExists()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly);
        AgentDomainHostComposition.Configure(builder);
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        var scheduler = Substitute.For<IInteractionWorkflowScheduler>();
        builder.Services.RemoveAll<IAgentCommandDispatcher>();
        builder.Services.RemoveAll<IInteractionWorkflowScheduler>();
        builder.Services.AddSingleton(dispatcher);
        builder.Services.AddSingleton(scheduler);

        using var provider = builder.Services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var intake = scope.ServiceProvider.GetRequiredService<AutomaticInteractionIntakeOrchestrator>();
        var fixture = new AutomaticInteractionReplayFixture();

        var result = await intake.ExecuteAsync(fixture.Request, CancellationToken.None);

        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.DependencyNotAvailable);
        result.Dependency.ShouldBe(new DependencyNotAvailable("EXT-HOST-1"));
        result.AgentInteractionId.ShouldBeNull();
        dispatcher.ReceivedCalls().ShouldBeEmpty();
        scheduler.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("EXT-PROTECTION-1", AutomaticInteractionIntakeStatus.PayloadProtectionUnavailable)]
    [InlineData("EXT-HOST-1", AutomaticInteractionIntakeStatus.DependencyNotAvailable)]
    [InlineData("EXT-SECRETS-1", AutomaticInteractionIntakeStatus.DependencyNotAvailable)]
    [InlineData("EXT-PARTIES-1", AutomaticInteractionIntakeStatus.DependencyNotAvailable)]
    [InlineData("EXT-CONV-AI-1", AutomaticInteractionIntakeStatus.DependencyNotAvailable)]
    [InlineData("EXT-TOPOLOGY-1", AutomaticInteractionIntakeStatus.DependencyNotAvailable)]
    [InlineData("OD-DAPR-SECURITY-1", AutomaticInteractionIntakeStatus.DependencyNotAvailable)]
    public async Task UnavailableDependencyStopsBeforeAnyOwnerRead(string recordId, AutomaticInteractionIntakeStatus expected)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var blocker = new DependencyNotAvailable(recordId);
        var availability = Substitute.For<IAgentDependencyAvailability>();
        availability.Check(recordId).Returns(blocker);
        var readiness = Substitute.For<IInteractionDirectoryReadiness>();
        var directory = Substitute.For<IConversationAgentStateReader>();
        var interaction = Substitute.For<IAgentInteractionStateReader>();
        var recovery = new InteractionOutboxRecoveryOrchestrator(directory, interaction, fixture, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, availability, readiness, directory, fixture, recovery);

        var result = await intake.ExecuteAsync(fixture.Request, CancellationToken.None);

        result.Status.ShouldBe(expected);
        result.Dependency.ShouldBe(blocker);
        result.AgentInteractionId.ShouldBeNull();
        readiness.ReceivedCalls().ShouldBeEmpty();
        directory.ReceivedCalls().ShouldBeEmpty();
        interaction.ReceivedCalls().ShouldBeEmpty();
        fixture.Dispatches.ShouldBeEmpty();
        fixture.Scheduled.ShouldBeEmpty();
    }

    [Fact]
    public async Task MissingReadinessStopsBeforeReadingTheDirectory()
    {
        var fixture = new AutomaticInteractionReplayFixture { DirectoryReady = false };
        var availability = Substitute.For<IAgentDependencyAvailability>();
        var directory = Substitute.For<IConversationAgentStateReader>();
        var interaction = Substitute.For<IAgentInteractionStateReader>();
        var recovery = new InteractionOutboxRecoveryOrchestrator(directory, interaction, fixture, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, availability, fixture, directory, fixture, recovery);

        var result = await intake.ExecuteAsync(fixture.Request, CancellationToken.None);

        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.DirectoryNotReady);
        result.Dependency.ShouldBeNull();
        result.AgentInteractionId.ShouldBeNull();
        directory.ReceivedCalls().ShouldBeEmpty();
        interaction.ReceivedCalls().ShouldBeEmpty();
        fixture.Dispatches.ShouldBeEmpty();
        fixture.Scheduled.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClosingDirectoryRefusesIntakeWithoutWritingAPermit()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var availability = Substitute.For<IAgentDependencyAvailability>();
        var closing = new ConversationAgentStateState();
        closing.Apply(new ConversationDeletionClosingEntered(
            fixture.Request.TenantId, fixture.Request.ConversationId, new string('d', 64), [], 1));
        var directory = Substitute.For<IConversationAgentStateReader>();
        directory.ReadAsync(fixture.Request.TenantId, fixture.Request.ConversationId, CancellationToken.None).Returns(closing);
        var interaction = Substitute.For<IAgentInteractionStateReader>();
        var recovery = new InteractionOutboxRecoveryOrchestrator(directory, interaction, fixture, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, availability, fixture, directory, fixture, recovery);

        var result = await intake.ExecuteAsync(fixture.Request, CancellationToken.None);

        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.ConversationClosing);
        result.Dependency.ShouldBeNull();
        result.AgentInteractionId.ShouldBeNull();
        await directory.Received(1).ReadAsync(fixture.Request.TenantId, fixture.Request.ConversationId, CancellationToken.None);
        directory.ReceivedCalls().ShouldHaveSingleItem();
        interaction.ReceivedCalls().ShouldBeEmpty();
        fixture.Dispatches.ShouldBeEmpty();
        fixture.Scheduled.ShouldBeEmpty();
    }

    [Fact]
    public async Task FailedPreCreationGatesReturnTheSameNotFoundBeforeDependenciesOrOwners()
    {
        var fixture = new AutomaticInteractionReplayFixture(); var gate = Substitute.For<IAutomaticInteractionIntakeGate>(); var availability = Substitute.For<IAgentDependencyAvailability>();
        gate.EvaluateAsync(Arg.Any<AutomaticInteractionIntakeRequest>(), Arg.Any<CancellationToken>()).Returns(new AutomaticInteractionGateResult(false, null));
        var intake = new AutomaticInteractionIntakeOrchestrator(gate, availability, fixture, fixture, fixture, fixture.Recovery());
        var result = await intake.ExecuteAsync(fixture.Request, CancellationToken.None);
        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.NotFound); result.AgentInteractionId.ShouldBeNull(); result.Dependency.ShouldBeNull();
        availability.ReceivedCalls().ShouldBeEmpty(); fixture.Dispatches.ShouldBeEmpty(); fixture.Scheduled.ShouldBeEmpty(); fixture.Directory.ShouldBeNull();
    }
}
