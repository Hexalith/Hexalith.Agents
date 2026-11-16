using System.Diagnostics;
using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;
using NSubstitute;
using Hexalith.Agents.Server.Application.Activities;
using Hexalith.Agents.Server.Application.Workflows;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Public dispatch/Handle/Apply agreement: exact owner truth and the generation-only three-command exception.</summary>
public sealed class AutomaticInteractionCheckpointAgreementTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TerminalRefusalsRemainBlockedAfterRealDispositionsSettlementAndReplay(bool capacityRefusal)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        if (capacityRefusal) { fixture.Effects.FailAt = AutomaticInteractionStep.CapacityAdmissionRecorded; }
        else { fixture.Effects.AuthorityAvailable = false; }
        await fixture.StartAsync(); (await fixture.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Failed);
        fixture.Restart();
        var state = fixture.Interaction.ShouldNotBeNull();
        state.AutomaticFacts.ShouldContainKey(capacityRefusal ? AutomaticInteractionStep.PreProviderCapacityDispositionDecided : AutomaticInteractionStep.ProviderInvocationNotAuthorized);
        state.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.BudgetDispositionDecided);
        state.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.PreProviderCapacityDispositionDecided);
        AgentInteractionAuditInspection.GetStatus(state, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Blocked);
        state.AutomaticFacts.Values.ShouldAllBe(f => f.Output == null);
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Effects.Preparations.ShouldBeEmpty(); fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Posts.ShouldBeEmpty();
    }

    [Fact]
    public async Task DelayedInteractionAppendIsObservedBeforeReturningItsExactOutcome()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        var stale = Clone(fixture.Interaction!); int reads = 0;
        var reader = Substitute.For<IAgentInteractionStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<AgentInteractionState?>(++reads < 5 ? stale : fixture.Interaction));
        int commands = fixture.Dispatches.Count;
        var result = await new AutomaticInteractionStepOrchestrator(fixture, reader, fixture, fixture.Effects).ExecuteAsync(fixture.Input, CancellationToken.None);
        (fixture.Dispatches.Count - commands).ShouldBe(1); reads.ShouldBeGreaterThanOrEqualTo(5);
        result.EventRevision.ShouldBe(fixture.Interaction!.AutomaticRevision); result.Outcome.ShouldBe(AutomaticInteractionOutcome.Allowed);
        fixture.Restart(); fixture.Interaction!.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.WorkflowStartCheckpoint);
    }

    [Theory]
    [InlineData("acquire")]
    [InlineData("commit")]
    [InlineData("settle")]
    public async Task DirectoryOnlyTransitionsWaitForTheirExactLeaseFact(string transition)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        if (transition == "settle") { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        else
        {
            await fixture.AdvanceUntilAsync(AutomaticInteractionStep.OpenInteractionLeaseRecorded);
            if (transition == "commit") { await fixture.ReserveUntilAsync(ConversationEffectKind.ContextRead); }
        }
        var stale = Clone(fixture.Directory!); int reads = 0;
        var reader = Substitute.For<IConversationAgentStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ConversationAgentStateState?>(++reads < 5 ? stale : fixture.Directory));
        int commands = fixture.Dispatches.Count; long revision = fixture.Interaction!.AutomaticRevision;
        var result = await new AutomaticInteractionStepOrchestrator(reader, fixture, fixture, fixture.Effects).ExecuteAsync(fixture.Input, CancellationToken.None);
        (fixture.Dispatches.Count - commands).ShouldBe(1); reads.ShouldBeGreaterThanOrEqualTo(5);
        result.EventRevision.ShouldBe(revision); fixture.Restart();
        var lease = fixture.Directory!.Leases.Values.Single(l => l.Kind == (transition == "settle" ? ConversationEffectKind.WorkflowStart : ConversationEffectKind.ContextRead));
        lease.Status.ShouldBe(transition == "settle" ? ConversationEffectLeaseStatus.Settled
            : transition == "commit" ? ConversationEffectLeaseStatus.CommittedToEffect : ConversationEffectLeaseStatus.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AnUnobservedOrRejectedAppendExhaustsPacedObservationWithoutAcceptingUnrelatedRevisions(bool reject)
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true }; await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.OpenInteractionLeaseRecorded);
        var stale = Clone(fixture.Directory!);
        // A different operation advances the same directory after the activity's initial owner read.
        var other = fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.WorkflowStart) with
        { LeaseId = new string('f', 64), OperationIdentity = new string('e', 64), Status = ConversationEffectLeaseStatus.Reserved, CommitRevision = 0, ResultRevision = 0 };
        other = other with { LeaseId = Hexalith.Agents.Contracts.Security.ConversationAgentStateIdentity.Lease(AutomaticInteractionReplayFixture.Tenant,
            AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, other.Kind, other.OperationFamily, other.OperationIdentity) };
        await Hexalith.Agents.Server.Ports.InteractionCommandDispatch.SendAsync(fixture, AutomaticInteractionReplayFixture.Tenant,
            Hexalith.Agents.Contracts.Security.ConversationAgentStateIdentity.Domain,
            Hexalith.Agents.Contracts.Security.ConversationAgentStateIdentity.Directory(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation),
            other.LeaseId, new AcquireConversationEffectLease(other, fixture.Directory.Revision), CancellationToken.None);
        int reads = 0; var reader = Substitute.For<IConversationAgentStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<ConversationAgentStateState?>(++reads == 1 ? stale : fixture.Directory));
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call => reject
            ? fixture.DispatchAsync((AgentCommandIntent)call[0], (CancellationToken)call[1])
            : Task.FromResult(new SubmitCommandResponse("receipt")));
        var elapsed = Stopwatch.StartNew();
        await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() =>
            new AutomaticInteractionStepOrchestrator(reader, fixture, dispatcher, fixture.Effects).ExecuteAsync(fixture.Input, CancellationToken.None));
        reads.ShouldBe(65); elapsed.Elapsed.ShouldBeGreaterThan(TimeSpan.FromSeconds(1));
        await dispatcher.Received(1).DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>());
        fixture.Directory!.Leases.Values.Any(l => l.Kind == ConversationEffectKind.ContextRead).ShouldBeFalse();
        fixture.Restart(); await fixture.CompleteAsync(); fixture.Interaction!.Status.ShouldBe(AgentInteractionStatus.Posted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ObservationPreservesCallerCancellation(bool directoryTransition)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        if (directoryTransition)
        {
            await fixture.AdvanceUntilAsync(AutomaticInteractionStep.OpenInteractionLeaseRecorded);
        }
        using var cancellation = new CancellationTokenSource();
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.CancelAfter(TimeSpan.FromMilliseconds(80));
            return Task.FromResult(new SubmitCommandResponse("receipt"));
        });
        var error = await Should.ThrowAsync<OperationCanceledException>(() =>
            new AutomaticInteractionStepOrchestrator(fixture, fixture, dispatcher, fixture.Effects).ExecuteAsync(fixture.Input, cancellation.Token));
        error.CancellationToken.ShouldBe(cancellation.Token);
        await dispatcher.Received(1).DispatchAsync(Arg.Any<AgentCommandIntent>(), cancellation.Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAtTheExactMatchingObservationStillPropagates(bool directoryTransition)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        if (directoryTransition)
        {
            await fixture.AdvanceUntilAsync(AutomaticInteractionStep.OpenInteractionLeaseRecorded);
        }
        using var cancellation = new CancellationTokenSource();
        int reads = 0; int commands = fixture.Dispatches.Count;
        var directoryReader = Substitute.For<IConversationAgentStateReader>();
        directoryReader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (directoryTransition && ++reads == 2) { cancellation.Cancel(); }
            return Task.FromResult(fixture.Directory);
        });
        var interactionReader = Substitute.For<IAgentInteractionStateReader>();
        interactionReader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (!directoryTransition && ++reads == 2) { cancellation.Cancel(); }
            return Task.FromResult(fixture.Interaction);
        });
        var error = await Should.ThrowAsync<OperationCanceledException>(() =>
            new AutomaticInteractionStepOrchestrator(directoryReader, interactionReader, fixture, fixture.Effects).ExecuteAsync(fixture.Input, cancellation.Token));
        error.CancellationToken.ShouldBe(cancellation.Token); (fixture.Dispatches.Count - commands).ShouldBe(1);
        fixture.Restart(); (await fixture.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Completed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedOrPermanentlyInvisibleInteractionResultRequiresSafeRetry(bool reject)
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true }; await fixture.StartAsync();
        string before = fixture.Snapshot(); int submitted = 0;
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var intent = call.ArgAt<AgentCommandIntent>(0); submitted++;
            var record = JsonSerializer.Deserialize<RecordAutomaticInteractionStep>(intent.Payload).ShouldNotBeNull();
            if (reject)
            {
                await InteractionCommandDispatch.SendAsync(fixture, intent.TenantId, intent.Domain, intent.AggregateId, record.Fact.OperationId,
                    record with { ExpectedRevision = -1 }, call.ArgAt<CancellationToken>(1));
            }
            return new SubmitCommandResponse(intent.CorrelationId, MessageId: intent.MessageId);
        });
        var error = await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() =>
            new AutomaticInteractionStepOrchestrator(fixture, fixture, dispatcher, fixture.Effects).ExecuteAsync(fixture.Input, CancellationToken.None));
        error.Message.ShouldBe("InteractionCheckpointUnavailable"); error.InnerException.ShouldBeNull(); submitted.ShouldBe(1);
        fixture.Restart(); fixture.Snapshot().ShouldBe(before);
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.WorkflowStartCheckpoint).ShouldBeFalse();
        (await fixture.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Completed);
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled);
    }

    [Theory]
    [InlineData("output")]
    [InlineData("digest")]
    [InlineData("dependency")]
    public async Task DivergentGeneratedOwnerFactCannotAcknowledgeTheLosingProtectedResult(string changedField)
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true }; await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationAuthorized);
        AutomaticInteractionFact? winning = null;
        fixture.BeforeDispatchAsync = async intent =>
        {
            if (intent.CommandType != nameof(RecordAutomaticInteractionStep)) { return; }
            fixture.BeforeDispatchAsync = null;
            var submitted = JsonSerializer.Deserialize<RecordAutomaticInteractionStep>(intent.Payload).ShouldNotBeNull();
            submitted.Fact.Step.ShouldBe(AutomaticInteractionStep.GeneratedVersionRecorded);
            winning = changedField switch
            {
                "output" => submitted.Fact with { Output = submitted.Fact.Output! with { Value = JsonSerializer.SerializeToElement(new { FixtureSealed = "competing-protected-envelope" }) } },
                "digest" => submitted.Fact with { ContentDigest = new string('f', 64) },
                _ => submitted.Fact with { DependencyRecordId = "EXT-PROVIDER-1" },
            };
            await InteractionCommandDispatch.SendAsync(fixture, intent.TenantId, intent.Domain, intent.AggregateId, winning.OperationId,
                submitted with { Fact = winning }, CancellationToken.None);
        };
        int generations = fixture.Effects.Observed.Count(step => step == AutomaticInteractionStep.GeneratedVersionRecorded);
        var error = await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() => fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None));
        error.Message.ShouldBe("InteractionCheckpointUnavailable"); error.InnerException.ShouldBeNull(); winning.ShouldNotBeNull();
        fixture.Effects.Observed.Count(step => step == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(generations + 1);
        string expected = JsonSerializer.Serialize(winning); fixture.Restart();
        JsonSerializer.Serialize(fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]).ShouldBe(expected);
        (await fixture.CompleteAsync()).Status.ShouldBe(WorkflowExecutionStatus.Completed); fixture.Restart();
        JsonSerializer.Serialize(fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]).ShouldBe(expected);
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled);
    }

    [Theory]
    [InlineData("cancelled")]
    [InlineData("committed")]
    [InlineData("rejected")]
    [InlineData("invisible")]
    public async Task CancellationCleanupObservesOnlyItsExactCancelledOrCommitWonLease(string outcome)
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true }; await fixture.StartAsync(); await fixture.CompleteAsync();
        // An interrupted owner left one reservation after the interaction's terminal result.
        var original = fixture.Directory!.Leases.Values.First();
        string operation = new string('f', 64);
        var reservation = original with { Kind = ConversationEffectKind.ContextRead, OperationFamily = "fixture-cancellation", OperationIdentity = operation,
            Status = ConversationEffectLeaseStatus.Reserved, CommitRevision = 0, ResultRevision = 0, MustSettle = false,
            LeaseId = ConversationAgentStateIdentity.Lease(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation,
                fixture.InteractionId, ConversationEffectKind.ContextRead, "fixture-cancellation", operation) };
        await SendDirectoryAsync(fixture, new AcquireConversationEffectLease(reservation, fixture.Directory.Revision), reservation.LeaseId);
        string interaction = JsonSerializer.Serialize(fixture.Interaction); int observations = 0;
        if (outcome is "cancelled" or "committed")
        {
            fixture.BeforeDispatchAsync = async intent =>
            {
                intent.CommandType.ShouldBe(nameof(CancelConversationEffectLease)); fixture.BeforeDispatchAsync = null;
                var stale = Clone(fixture.Directory!);
                if (outcome == "committed")
                { await SendDirectoryAsync(fixture, new CommitConversationEffect(reservation.LeaseId, fixture.InteractionId, fixture.InteractionId, fixture.Directory!.Revision), reservation.LeaseId); }
                fixture.DirectoryObservationAsync = _ => Task.FromResult<ConversationAgentStateState?>(++observations <= 3 ? stale : fixture.Directory);
            };
            await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); observations.ShouldBe(4);
        }
        else
        {
            int submitted = 0; var dispatcher = Substitute.For<IAgentCommandDispatcher>();
            dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(async call =>
            {
                var intent = call.ArgAt<AgentCommandIntent>(0); intent.CommandType.ShouldBe(nameof(CancelConversationEffectLease)); submitted++;
                if (outcome == "rejected")
                {
                    var cancel = JsonSerializer.Deserialize<CancelConversationEffectLease>(intent.Payload).ShouldNotBeNull();
                    await SendDirectoryAsync(fixture, cancel with { ExpectedRevision = -1 }, reservation.LeaseId);
                }
                return new SubmitCommandResponse(intent.CorrelationId, MessageId: intent.MessageId);
            });
            await Should.ThrowAsync<AutomaticInteractionCheckpointUnavailableException>(() =>
                new AutomaticInteractionStepOrchestrator(fixture, fixture, dispatcher, fixture.Effects).ExecuteAsync(fixture.Input, CancellationToken.None));
            submitted.ShouldBe(1);
        }
        fixture.DirectoryObservationAsync = null; fixture.Restart();
        var persisted = fixture.Directory!.Leases[reservation.LeaseId];
        persisted.Status.ShouldBe(outcome == "committed" ? ConversationEffectLeaseStatus.CommittedToEffect
            : outcome == "cancelled" ? ConversationEffectLeaseStatus.CancelledBeforeCommit : ConversationEffectLeaseStatus.Reserved);
        (persisted with { Status = reservation.Status, CommitRevision = 0 }).ShouldBe(reservation);
        if (outcome == "committed") { persisted.CommitRevision.ShouldBeGreaterThan(0); }
        else { persisted.CommitRevision.ShouldBe(0); await fixture.CompleteAsync(); fixture.Restart(); fixture.Directory!.Leases[reservation.LeaseId].Status.ShouldBe(ConversationEffectLeaseStatus.CancelledBeforeCommit); }
        JsonSerializer.Serialize(fixture.Interaction).ShouldBe(interaction);
    }

    private static Task SendDirectoryAsync(AutomaticInteractionReplayFixture fixture, object command, string operationId) => InteractionCommandDispatch.SendAsync(fixture,
        AutomaticInteractionReplayFixture.Tenant, ConversationAgentStateIdentity.Domain,
        ConversationAgentStateIdentity.Directory(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation), operationId, command, CancellationToken.None);

    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    [Fact]
    public async Task EveryCheckpointReturnsTheRecordedOutcomeAndDispatchesAtMostOneCommand()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        var input = fixture.Input;
        for (int n = 0; n < 128; n++)
        {
            int before = fixture.Dispatches.Count;
            int generations = fixture.Effects.Observed.Count(step => step == AutomaticInteractionStep.GeneratedVersionRecorded);
            input = await fixture.Steps().ExecuteAsync(input, CancellationToken.None);
            var commands = fixture.Dispatches.Skip(before).ToArray();
            if (fixture.Effects.Observed.Count(step => step == AutomaticInteractionStep.GeneratedVersionRecorded) > generations)
            {
                commands.Select(command => command.CommandType).ShouldBe([nameof(AcquireConversationEffectLease), nameof(CommitConversationEffect), nameof(RecordAutomaticInteractionStep)]);
                var acquire = JsonSerializer.Deserialize<AcquireConversationEffectLease>(commands[0].Payload).ShouldNotBeNull();
                var commit = JsonSerializer.Deserialize<CommitConversationEffect>(commands[1].Payload).ShouldNotBeNull();
                var record = JsonSerializer.Deserialize<RecordAutomaticInteractionStep>(commands[2].Payload).ShouldNotBeNull();
                acquire.Lease.Kind.ShouldBe(ConversationEffectKind.ProposalMutation); commit.LeaseId.ShouldBe(acquire.Lease.LeaseId);
                record.Fact.Step.ShouldBe(AutomaticInteractionStep.GeneratedVersionRecorded); record.Fact.LeaseId.ShouldBe(commit.LeaseId);
                record.Fact.LeaseCommitRevision.ShouldBe(fixture.Directory!.Leases[commit.LeaseId].CommitRevision);
                commands.Select(command => command.Domain).ShouldBe([ConversationAgentStateIdentity.Domain, ConversationAgentStateIdentity.Domain, "agent-interaction"]);
            }
            else { commands.Length.ShouldBeLessThanOrEqualTo(1); }
            var latest = fixture.Interaction!.AutomaticFacts.Values.MaxBy(f => f.Revision);
            input.Outcome.ShouldBe(latest?.Outcome ?? AutomaticInteractionOutcome.Unknown);
            input.EventRevision.ShouldBe(fixture.Interaction.AutomaticRevision);
            if (input.Status != WorkflowExecutionStatus.Running) { input.Status.ShouldBe(WorkflowExecutionStatus.Completed); return; }
        }
        throw new InvalidOperationException("FixtureDidNotConverge");
    }
}
