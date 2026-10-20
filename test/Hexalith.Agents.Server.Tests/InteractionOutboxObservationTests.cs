using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Recovery observes accepted asynchronous appends with bounded cancellation-aware waits.</summary>
public sealed class InteractionOutboxObservationTests
{
    [Fact]
    public async Task DelayedOwnerVisibilityConvergesWithoutRepeatedSubmission()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var directory = Substitute.For<IConversationAgentStateReader>(); var interaction = Substitute.For<IAgentInteractionStateReader>();
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        AgentCommandIntent? pending = null; DateTimeOffset visibleAt = default; var submitted = new List<string>(); int observations = 0;
        async Task ObserveAsync(CancellationToken ct)
        {
            observations++;
            if (pending is not null && DateTimeOffset.UtcNow >= visibleAt)
            { var append = pending; pending = null; await fixture.DispatchAsync(append, ct); }
        }
        directory.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
        { await ObserveAsync(call.ArgAt<CancellationToken>(2)); return fixture.Directory; });
        interaction.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
        { await ObserveAsync(call.ArgAt<CancellationToken>(2)); return fixture.Interaction; });
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            pending.ShouldBeNull(); pending = call.ArgAt<AgentCommandIntent>(0); visibleAt = DateTimeOffset.UtcNow.AddMilliseconds(50); submitted.Add(pending.MessageId);
            return Task.FromResult(new SubmitCommandResponse(pending.CorrelationId, MessageId: pending.MessageId));
        });
        var recovery = new InteractionOutboxRecoveryOrchestrator(directory, interaction, dispatcher, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, directory, dispatcher, recovery);
        (await intake.ExecuteAsync(fixture.Request, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        submitted.Distinct().Count().ShouldBe(submitted.Count); fixture.Directory!.Acknowledgements.ShouldContainKey(fixture.InteractionId);
        fixture.Interaction.ShouldNotBeNull(); fixture.Scheduled.ShouldBe([fixture.InteractionId]); pending.ShouldBeNull();
        observations.ShouldBeGreaterThan(submitted.Count * 2);
    }

    [Fact]
    public async Task CancellationInterruptsObservationOfAnAcceptedButInvisibleAppend()
    {
        var fixture = new AutomaticInteractionReplayFixture(); var dispatcher = Substitute.For<IAgentCommandDispatcher>(); int submitted = 0;
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        { submitted++; return Task.FromResult(new SubmitCommandResponse("accepted", MessageId: call.ArgAt<AgentCommandIntent>(0).MessageId)); });
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, dispatcher, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, fixture, dispatcher, recovery);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(75));
        await Should.ThrowAsync<OperationCanceledException>(() => intake.ExecuteAsync(fixture.Request, cancellation.Token));
        submitted.ShouldBe(1); fixture.Directory.ShouldBeNull(); fixture.Interaction.ShouldBeNull(); fixture.Scheduled.ShouldBeEmpty();
    }
}
