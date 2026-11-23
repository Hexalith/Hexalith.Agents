using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Exactly one phase-pinned branch precedes Provider work; Open never chooses a local status.</summary>
public sealed class ProviderOutputSafetyDecisionPhasePinTests
{
    [Theory]
    [InlineData(ProviderOutputAuthorityRefusal.Open, ProviderOutputAuthorityRefusal.Open)]
    [InlineData(ProviderOutputAuthorityRefusal.Missing, ProviderOutputAuthorityRefusal.Missing)]
    [InlineData(ProviderOutputAuthorityRefusal.Malformed, ProviderOutputAuthorityRefusal.Malformed)]
    [InlineData(ProviderOutputAuthorityRefusal.Unavailable, ProviderOutputAuthorityRefusal.Unavailable)]
    [InlineData(ProviderOutputAuthorityRefusal.Unknown, ProviderOutputAuthorityRefusal.Unavailable)]
    public async Task RefusalPersistsAcrossReplayAndRequiresAllocatorEvidenceBeforeSettlement(
        ProviderOutputAuthorityRefusal ownerReason, ProviderOutputAuthorityRefusal recordedReason)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        fixture.Effects.AuthorityAvailable = false;
        fixture.Effects.AuthorityRefusal = ownerReason;
        await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationNotAuthorized);
        fixture.Restart();

        var pending = fixture.Interaction.ShouldNotBeNull();
        var refusal = pending.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationNotAuthorized];
        refusal.OutputAuthorityRefusal.ShouldBe(recordedReason);
        refusal.DependencyRecordId.ShouldBe(ownerReason == ProviderOutputAuthorityRefusal.Open
            ? "fixture-open-decision-observation"
            : "OD-INITIAL-OUTPUT-SAFETY-STATUS-1");
        refusal.OutputAuthority.ShouldBeNull();
        var budget = pending.AutomaticFacts[AutomaticInteractionStep.BudgetDispositionDecided];
        budget.Outcome.ShouldBe(AutomaticInteractionOutcome.NotInvoked);
        budget.OutputAuthorityRefusal.ShouldBeNull();
        pending.AutomaticFacts.ShouldNotContainKey(AutomaticInteractionStep.PreProviderCapacityDispositionDecided);

        fixture.Effects.AuthorityAvailable = true;
        fixture.Effects.FailAt = AutomaticInteractionStep.PreProviderCapacityDispositionDecided;
        (await Should.ThrowAsync<InvalidOperationException>(() => fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None)))
            .Message.ShouldBe("AuthoritativeNoInvocationEvidenceRequired");
        fixture.Effects.FailAt = null;
        fixture.Effects.NoInvocationReferenceAvailable = false;
        (await Should.ThrowAsync<InvalidOperationException>(() => fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None)))
            .Message.ShouldBe("AuthoritativeNoInvocationEvidenceRequired");
        fixture.Effects.NoInvocationReferenceAvailable = true;
        fixture.Effects.CapacityAdmissionOverride = new string('e', 64);
        (await Should.ThrowAsync<InvalidOperationException>(() => fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None)))
            .Message.ShouldBe("AuthoritativeNoInvocationEvidenceRequired");
        pending.AutomaticFacts.ShouldNotContainKey(AutomaticInteractionStep.PreProviderCapacityDispositionDecided);
        fixture.Effects.Settlements.ShouldBeEmpty();
        fixture.Directory.ShouldNotBeNull().Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation)
            .Status.ShouldBe(ConversationEffectLeaseStatus.CommittedToEffect);

        fixture.Effects.CapacityAdmissionOverride = null;
        await fixture.CompleteAsync();
        fixture.Restart();
        var interaction = fixture.Interaction.ShouldNotBeNull();
        interaction.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationNotAuthorized].ShouldBe(refusal);
        interaction.AutomaticFacts[AutomaticInteractionStep.BudgetDispositionDecided].ShouldBe(budget);
        var disposition = interaction.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided];
        disposition.Outcome.ShouldBe(AutomaticInteractionOutcome.CancelNoInvocation);
        disposition.DependencyRecordId.ShouldBe("fixture-allocator-no-invocation-record");
        disposition.CapacityAdmissionId.ShouldBe(interaction.PermittedCreation!.AdmissionId);
        disposition.Revision.ShouldBe(budget.Revision + 1);
        interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBeFalse();
        fixture.Effects.Observed.Count(step => step == AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBe(1);
        fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Directory.ShouldNotBeNull().Leases.Values.Single(l => l.Kind == ConversationEffectKind.ProviderInvocation).Status.ShouldBe(ConversationEffectLeaseStatus.Settled);
    }

    [Fact]
    public async Task CommittedCapacityDispositionSurvivesLostAcknowledgement()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        fixture.Effects.AuthorityAvailable = false;
        await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.ProviderInvocationNotAuthorized);

        fixture.FaultAt = fixture.Dispatches.Count;
        fixture.CrashAfterAppend = true;
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None));
        fixture.Restart();
        var committed = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided];
        await fixture.CompleteAsync();

        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided].ShouldBe(committed);
        fixture.Effects.Observed.Count(step => step == AutomaticInteractionStep.PreProviderCapacityDispositionDecided).ShouldBe(1);
        fixture.Effects.Observed.Count(step => step == AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBe(1);
        fixture.Effects.Attempts.ShouldBeEmpty();
        fixture.Effects.Preparations.ShouldBeEmpty();
    }

    [Fact]
    public async Task HistoricalCancellationRequiresFreshEvidenceAndPassesItToSettlement()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        fixture.Effects.AuthorityAvailable = false;
        await fixture.StartAsync();
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.PreProviderCapacityDispositionDecided);
        fixture.Restart();

        // The historical refusal policy copied the authority reference into its synthesized cancellation.
        var facts = fixture.Interaction!.AutomaticFacts;
        var refusal = facts[AutomaticInteractionStep.ProviderInvocationNotAuthorized] with { OutputAuthorityRefusal = null };
        var cancellation = facts[AutomaticInteractionStep.PreProviderCapacityDispositionDecided] with
        {
            CapacityAdmissionId = null,
            OperationId = refusal.OperationId,
            DependencyRecordId = refusal.DependencyRecordId,
        };
        facts[refusal.Step] = refusal;
        facts[cancellation.Step] = cancellation;
        fixture.Effects.NoInvocationReferenceAvailable = false;
        await Should.ThrowAsync<InvalidOperationException>(() => fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None));
        fixture.Effects.Settlements.ShouldBeEmpty();

        fixture.Effects.NoInvocationReferenceAvailable = true;
        await fixture.CompleteAsync();
        fixture.Effects.HistoricalSettlementEvidence.ShouldNotBeNull().CapacityAdmissionId.ShouldBe(fixture.Interaction.PermittedCreation!.AdmissionId);
        fixture.Interaction.AutomaticFacts[cancellation.Step].ShouldBe(cancellation);
        fixture.Effects.Preparations.ShouldBeEmpty();
        fixture.Effects.Attempts.ShouldBeEmpty();
    }
    [Fact]
    public async Task RecordedAuthorityIsNeverReReadWhenItsSuccessorChanges()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        for (int i = 0; i < 100 && !fixture.Interaction!.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized); i++) { await fixture.Steps().ExecuteAsync(fixture.Input, CancellationToken.None); }
        var pinned = fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationAuthorized].OutputAuthority.ShouldNotBeNull();
        fixture.Effects.AuthorityAvailable = false; fixture.Restart(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationAuthorized].OutputAuthority.ShouldBe(pinned);
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBe(1); fixture.Effects.Attempts.Count.ShouldBe(1);
    }
}
