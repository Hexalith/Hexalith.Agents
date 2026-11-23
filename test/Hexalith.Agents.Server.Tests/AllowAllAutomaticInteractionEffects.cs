using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Ports;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Deterministic content-free external-owner model; never establishes live adapter availability.</summary>
internal sealed class AllowAllAutomaticInteractionEffects : IAutomaticInteractionStepEffects
{
    /// <summary>Gets or sets a typed failure at one phase.</summary>
    internal AutomaticInteractionStep? FailAt { get; set; }
    /// <summary>Gets or sets the exact generated-attempt failure classification.</summary>
    internal AutomaticInteractionFailureKind FailureKind { get; set; } = AutomaticInteractionFailureKind.ProviderError;
    /// <summary>Gets or sets a missing output authority.</summary>
    internal bool AuthorityAvailable { get; set; } = true;
    /// <summary>Gets or sets a malformed authority test fixture.</summary>
    internal ProviderOutputAuthority? AuthorityOverride { get; set; }
    /// <summary>Gets or sets a complete authority result for malformed-envelope controls.</summary>
    internal AutomaticInteractionEffectResult? AuthorityResultOverride { get; set; }
    /// <summary>Gets or sets the safe classification of an unavailable authority.</summary>
    internal ProviderOutputAuthorityRefusal AuthorityRefusal { get; set; } = ProviderOutputAuthorityRefusal.Missing;
    /// <summary>Gets or sets whether the allocator lookup supplies a no-invocation reference.</summary>
    internal bool NoInvocationReferenceAvailable { get; set; } = true;
    /// <summary>Gets or sets a foreign admission for allocator evidence tests.</summary>
    internal string? CapacityAdmissionOverride { get; set; }
    /// <summary>Gets or sets an exact protected generation result for envelope-preservation controls.</summary>
    internal AutomaticInteractionEffectResult? GenerationResultOverride { get; set; }
    /// <summary>Gets the executed effect order.</summary>
    internal List<AutomaticInteractionStep> Observed { get; } = [];
    /// <summary>Gets the immutable Provider attempt identities.</summary>
    internal HashSet<string> Attempts { get; } = [];
    /// <summary>Gets exactly-once messages.</summary>
    internal HashSet<string> Posts { get; } = [];
    /// <summary>Gets membership effects.</summary>
    internal HashSet<string> Memberships { get; } = [];
    /// <summary>Gets outstanding external preparations.</summary>
    internal HashSet<string> Preparations { get; } = [];
    /// <summary>Gets exactly settled identities.</summary>
    internal HashSet<string> Settlements { get; } = [];
    /// <summary>Gets the evidence supplied to settle historical capacity.</summary>
    internal AutomaticInteractionEffectResult? HistoricalSettlementEvidence { get; private set; }
    /// <inheritdoc />
    public Task<AutomaticInteractionEffectResult> ExecuteAsync(AutomaticInteractionStep step, AgentInteractionState interaction, ConversationEffectLease? committedLease, CancellationToken ct)
    {
        var creation = interaction.PermittedCreation.ShouldNotBeNull(); Observed.Add(step);
        if (step is AutomaticInteractionStep.ApproverResolutionRecorded or AutomaticInteractionStep.ContextReadRecorded or AutomaticInteractionStep.PromptSafetyRecorded
            or AutomaticInteractionStep.ConversationMembershipRecorded or AutomaticInteractionStep.ProviderInvocationAuthorized
            or AutomaticInteractionStep.GeneratedVersionRecorded or AutomaticInteractionStep.ConversationPostingRecorded)
        {
            committedLease.ShouldNotBeNull(); committedLease.Status.ShouldBe(ConversationEffectLeaseStatus.CommittedToEffect); committedLease.CommitRevision.ShouldBeGreaterThan(0);
        }
        if (FailAt == step) { return Task.FromResult(new AutomaticInteractionEffectResult(AutomaticInteractionOutcome.DependencyNotAvailable, "EXT-HOST-1", FailureKind: step == AutomaticInteractionStep.GeneratedVersionRecorded ? FailureKind : AutomaticInteractionFailureKind.Unknown)); }
        AutomaticInteractionOutcome outcome = AutomaticInteractionOutcome.Allowed;
        if (step == AutomaticInteractionStep.RateAdmissionAuthorized) { outcome = AutomaticInteractionOutcome.Authorized; }
        if (step is AutomaticInteractionStep.RateAdmissionRecorded or AutomaticInteractionStep.OpenInteractionLeaseRecorded or AutomaticInteractionStep.BudgetReservationRecorded or AutomaticInteractionStep.CapacityAdmissionRecorded)
        { Preparations.Add(AgentsSecurityIdentity.Derive("fixture-preparation", creation.AttemptId, step.ToString())); }
        if (step == AutomaticInteractionStep.ConversationMembershipRecorded) { Memberships.Add(creation.Permit.AgentInteractionId); }
        if (step == AutomaticInteractionStep.ProviderInvocationAuthorized)
        {
            return Task.FromResult(AuthorityResultOverride ?? (AuthorityAvailable
                ? new AutomaticInteractionEffectResult(AutomaticInteractionOutcome.Authorized, Authority: AuthorityOverride ?? new(1, new string('b', 64), AgentInteractionStatus.GenerationFailed, "product-reason", "product-metric", "product-open-disposition", 1, new string('c', 64)))
                : new AutomaticInteractionEffectResult(AutomaticInteractionOutcome.DependencyNotAvailable,
                    AuthorityRefusal == ProviderOutputAuthorityRefusal.Open ? "fixture-open-decision-observation" : "OD-INITIAL-OUTPUT-SAFETY-STATUS-1")
                {
                    OutputAuthorityRefusal = AuthorityRefusal,
                }));
        }
        if (step == AutomaticInteractionStep.GeneratedVersionRecorded)
        {
            interaction.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.ProviderInvocationAuthorized); Attempts.Add(creation.AttemptId);
            return Task.FromResult(GenerationResultOverride ?? new AutomaticInteractionEffectResult(AutomaticInteractionOutcome.Allowed,
                Output: new(creation.Permit.Request.Target, JsonSerializer.SerializeToElement(new { FixtureSealed = "allowed-output" })), ContentDigest: new string('d', 64), DigestKeyVersion: "fixture-digest-v1"));
        }
        if (step == AutomaticInteractionStep.ConversationPostingRecorded)
        { Posts.Add(creation.MessageId); outcome = AutomaticInteractionOutcome.Posted; }
        if (step == AutomaticInteractionStep.PreProviderCapacityDispositionDecided)
        {
            interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBeFalse();
            return Task.FromResult(new AutomaticInteractionEffectResult(AutomaticInteractionOutcome.CancelNoInvocation,
                NoInvocationReferenceAvailable ? "fixture-allocator-no-invocation-record" : null)
            {
                CapacityAdmissionId = CapacityAdmissionOverride ?? creation.AdmissionId,
            });
        }
        return Task.FromResult(new AutomaticInteractionEffectResult(outcome));
    }
    /// <inheritdoc />
    public Task SettleAsync(AgentInteractionState interaction, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (Preparations.Contains(AgentsSecurityIdentity.Derive("fixture-preparation", interaction.PermittedCreation!.AttemptId, AutomaticInteractionStep.BudgetReservationRecorded.ToString())))
        { interaction.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.BudgetDispositionDecided); }
        Settlements.UnionWith(Preparations); Preparations.Clear(); return Task.CompletedTask;
    }
    /// <inheritdoc />
    public Task SettleAsync(AgentInteractionState interaction, AutomaticInteractionEffectResult noInvocationEvidence, CancellationToken ct)
    {
        noInvocationEvidence.CapacityAdmissionId.ShouldBe(interaction.PermittedCreation!.AdmissionId);
        noInvocationEvidence.DependencyRecordId.ShouldBe("fixture-allocator-no-invocation-record");
        HistoricalSettlementEvidence = noInvocationEvidence;
        return SettleAsync(interaction, ct);
    }
}
