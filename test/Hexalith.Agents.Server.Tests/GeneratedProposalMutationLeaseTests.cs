using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Allowed output needs an independent mutation commit and loses cleanly to Closing.</summary>
public sealed class GeneratedProposalMutationLeaseTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task OneUninterruptedActivityGeneratesOnceAndRecordsTheExactMutationResult(int mutationCheckpoint)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        AutomaticInteractionEffectResult expectedResult = DistinctiveResult(fixture);
        fixture.Effects.GenerationResultOverride = expectedResult;
        if (mutationCheckpoint > 0) { await fixture.PauseGenerationAsync(afterCommit: mutationCheckpoint == 2); }
        else
        {
            await fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationAuthorized);
        }
        string attemptId = fixture.Interaction!.PermittedCreation!.AttemptId;
        var priorMutation = fixture.Directory!.Leases.Values.SingleOrDefault(l => l.Kind == ConversationEffectKind.ProposalMutation);
        int generations = fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded);
        int commands = fixture.Dispatches.Count;
        await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(generations + 1);
        (fixture.Dispatches.Count - commands).ShouldBe(3 - mutationCheckpoint);
        var mutation = fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation);
        var generated = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded];
        generated.LeaseId.ShouldBe(mutation.LeaseId); generated.LeaseCommitRevision.ShouldBe(mutation.CommitRevision);
        AssertExactResult(fixture, expectedResult);
        if (priorMutation is not null) { mutation.LeaseId.ShouldBe(priorMutation.LeaseId); }
        fixture.Interaction.PermittedCreation!.AttemptId.ShouldBe(attemptId);
        await fixture.CompleteAsync(); string expected = fixture.Snapshot(); fixture.Restart();
        fixture.Snapshot().ShouldBe(expected); fixture.Interaction!.AutomaticFacts.Keys.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(1);
    }

    [Theory]
    [InlineData("acquire")]
    [InlineData("commit")]
    [InlineData("record")]
    public async Task DelayedGenerationTransitionsKeepOneEffectAndTheEntireProtectedResult(string transition)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationAuthorized);
        var expected = DistinctiveResult(fixture); fixture.Effects.GenerationResultOverride = expected;
        int observations = 0;
        fixture.BeforeDispatchAsync = intent =>
        {
            if (intent.CommandType != TransitionCommand(transition)) { return Task.CompletedTask; }
            fixture.BeforeDispatchAsync = null;
            var directory = Clone(fixture.Directory!); var interaction = Clone(fixture.Interaction!);
            if (transition == "record")
            { fixture.InteractionObservationAsync = _ =>
                {
                    if (++observations <= 3) { return Task.FromResult<AgentInteractionState?>(interaction); }
                    fixture.InteractionObservationAsync = null; return Task.FromResult(fixture.Interaction);
                }; }
            else
            { fixture.DirectoryObservationAsync = _ =>
                {
                    if (++observations <= 3) { return Task.FromResult<ConversationAgentStateState?>(directory); }
                    fixture.DirectoryObservationAsync = null; return Task.FromResult(fixture.Directory);
                }; }
            return Task.CompletedTask;
        };
        int generations = fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded);
        await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None);
        observations.ShouldBe(4);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(generations + 1);
        AssertExactResult(fixture, expected);
        fixture.DirectoryObservationAsync = null; fixture.InteractionObservationAsync = null;
        await fixture.CompleteAsync(); fixture.Restart(); AssertExactResult(fixture, expected);
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled);
    }

    [Theory]
    [InlineData("acquire")]
    [InlineData("commit")]
    [InlineData("record")]
    public async Task CancelledGenerationObservationReplaysItsExactPartialStateAndRecovers(string transition)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationAuthorized);
        var expected = DistinctiveResult(fixture); fixture.Effects.GenerationResultOverride = expected;
        string identities = JsonSerializer.Serialize(fixture.Interaction!.PermittedCreation);
        using var cancellation = new CancellationTokenSource();
        fixture.BeforeDispatchAsync = intent =>
        {
            if (intent.CommandType != TransitionCommand(transition)) { return Task.CompletedTask; }
            fixture.BeforeDispatchAsync = null;
            var directory = Clone(fixture.Directory!); var interaction = Clone(fixture.Interaction!);
            if (transition == "record")
            { fixture.InteractionObservationAsync = _ => { cancellation.Cancel(); return Task.FromResult<AgentInteractionState?>(interaction); }; }
            else
            { fixture.DirectoryObservationAsync = _ => { cancellation.Cancel(); return Task.FromResult<ConversationAgentStateState?>(directory); }; }
            return Task.CompletedTask;
        };
        int generations = fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded);
        var error = await Should.ThrowAsync<OperationCanceledException>(() => fixture.Steps().ExecuteAsync(fixture.Input, cancellation.Token));
        error.CancellationToken.ShouldBe(cancellation.Token);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(generations + 1);
        var partialLease = fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation);
        partialLease.Status.ShouldBe(transition == "acquire" ? ConversationEffectLeaseStatus.Reserved : ConversationEffectLeaseStatus.CommittedToEffect);
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(transition == "record");
        string partial = fixture.Snapshot(); fixture.DirectoryObservationAsync = null; fixture.InteractionObservationAsync = null; fixture.Restart();
        fixture.Snapshot().ShouldBe(partial);
        JsonSerializer.Serialize(fixture.Interaction!.PermittedCreation).ShouldBe(identities);
        await fixture.CompleteAsync(); fixture.Restart(); AssertExactResult(fixture, expected);
        var settled = fixture.Directory!.Leases[partialLease.LeaseId];
        settled.Status.ShouldBe(ConversationEffectLeaseStatus.Settled); settled.OperationIdentity.ShouldBe(partialLease.OperationIdentity);
        if (partialLease.CommitRevision > 0) { settled.CommitRevision.ShouldBe(partialLease.CommitRevision); }
        fixture.Directory.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled);
        JsonSerializer.Serialize(fixture.Interaction!.PermittedCreation).ShouldBe(identities);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(generations + (transition == "record" ? 1 : 2));
    }

    [Fact]
    public async Task MissingSetupCheckpointFailsWithItsNameInsteadOfSpinning()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        fixture.Effects.FailAt = AutomaticInteractionStep.RateAdmissionAuthorized;
        var error = await Should.ThrowAsync<InvalidOperationException>(() => fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationAuthorized));
        error.Message.ShouldBe("FixtureCheckpointMissing:ProviderInvocationAuthorized");
    }

    private static string TransitionCommand(string transition) => transition switch
    {
        "acquire" => nameof(AcquireConversationEffectLease),
        "commit" => nameof(CommitConversationEffect),
        _ => "RecordAutomaticInteractionStep",
    };

    private static AutomaticInteractionEffectResult DistinctiveResult(AutomaticInteractionReplayFixture fixture) => new(AutomaticInteractionOutcome.Allowed,
        "EXT-PROVIDER-1", Output: new(fixture.Request.Request.Target,
            JsonSerializer.SerializeToElement(new { EnvelopeVersion = 3, FixtureSealed = "distinct-protected-output", Chunks = new[] { "cipher-a", "cipher-b" } })),
        ContentDigest: new string('e', 64), DigestKeyVersion: "fixture-output-digest-v7");

    private static void AssertExactResult(AutomaticInteractionReplayFixture fixture, AutomaticInteractionEffectResult result)
    {
        var actual = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded];
        var mutation = fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation);
        var expected = new AutomaticInteractionFact(AutomaticInteractionStep.GeneratedVersionRecorded,
            AgentsSecurityIdentity.Derive("interaction-step", AutomaticInteractionReplayFixture.Tenant, fixture.InteractionId, AutomaticInteractionStep.GeneratedVersionRecorded.ToString()),
            result.Outcome, actual.Revision, mutation.LeaseId, mutation.CommitRevision, result.DependencyRecordId, result.Authority,
            result.Output, result.ContentDigest, result.DigestKeyVersion, result.FailureKind);
        JsonSerializer.Serialize(actual).ShouldBe(JsonSerializer.Serialize(expected));
    }

    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ClosingDuringGenerationDiscardsOnlyTheLosingMutation(int closingCheckpoint)
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true }; await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationAuthorized);
        fixture.BeforeDispatchAsync = async intent =>
        {
            bool target = closingCheckpoint switch
            {
                0 => intent.CommandType == nameof(AcquireConversationEffectLease),
                1 => intent.CommandType == nameof(CommitConversationEffect),
                _ => intent.CommandType == "RecordAutomaticInteractionStep",
            };
            if (target) { fixture.BeforeDispatchAsync = null; await fixture.CloseAsync(); }
        };
        int generations = fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded);
        await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(generations + 1);
        bool won = closingCheckpoint == 2;
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(won);
        if (!won)
        {
            var failure = fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.GenerationFailed];
            failure.Outcome.ShouldBe(AutomaticInteractionOutcome.Discarded); failure.Output.ShouldBeNull();
            failure.LeaseId.ShouldBe(fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation).LeaseId);
        }
        await fixture.CompleteAsync(); fixture.Restart();
        fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(won);
        fixture.Directory!.Leases.Values.ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled || l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Effects.Posts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
    }

    [Fact]
    public async Task ClosingBeforeProposalCommitDiscardsGeneratedOutputAndSettlesTheProvider()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.ReserveUntilAsync(ConversationEffectKind.ProposalMutation);
        await fixture.CloseAsync(); fixture.Restart(); await fixture.CompleteAsync();
        var interaction = fixture.Interaction.ShouldNotBeNull();
        interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBeFalse();
        interaction.AutomaticFacts[AutomaticInteractionStep.GenerationFailed].Outcome.ShouldBe(AutomaticInteractionOutcome.Discarded);
        interaction.AutomaticFacts.Values.ShouldAllBe(f => f.Output == null); fixture.Effects.Posts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory!.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProposalMutation).Status.ShouldBe(ConversationEffectLeaseStatus.CancelledBeforeCommit);
        fixture.Directory.Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation).Status.ShouldBe(ConversationEffectLeaseStatus.Settled);
    }
}
