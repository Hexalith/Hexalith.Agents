using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;
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
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        Hexalith.Agents.Server.Composition.AgentDomainHostComposition.Configure(builder);
        using var provider = builder.Services.BuildServiceProvider(); using var scope = provider.CreateScope();
        var fixture = new AutomaticInteractionReplayFixture();
        var result = await scope.ServiceProvider.GetRequiredService<AutomaticInteractionIntakeOrchestrator>().ExecuteAsync(fixture.Request, CancellationToken.None);
        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.DependencyNotAvailable); result.Dependency.ShouldNotBeNull().RecordId.ShouldBe("EXT-HOST-1");
        result.AgentInteractionId.ShouldBeNull(); fixture.Directory.ShouldBeNull(); fixture.Scheduled.ShouldBeEmpty();
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
