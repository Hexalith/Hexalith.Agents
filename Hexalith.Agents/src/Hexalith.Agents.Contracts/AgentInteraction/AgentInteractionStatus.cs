using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Lifecycle status of one Agent Call (<c>AgentInteraction</c>) (AC2; FR-8). Story 2.1 introduces only the
/// initial <see cref="Requested"/> state; later states (authorized/denied, context-loading, generating, posted)
/// are appended <em>additively</em> by Stories 2.2–2.5 without reshaping this enum or its ordinals (AD-2).
/// Story 2.5 appends the two terminal posting states <see cref="Posted"/> and <see cref="PostingFailed"/>.
/// Story 3.1 appends the two Confirmation-mode states <see cref="ProposalCreated"/> and <see cref="ProposalCreationFailed"/>
/// the same way (the Confirmation-mode counterparts to <see cref="Posted"/>/<see cref="PostingFailed"/>).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the "not-yet-known" sentinel: an absent/unrecognized status must never
/// deserialize to a concrete state. Serialized by name so a missing value never resolves to <see cref="Requested"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete state.</summary>
    Unknown = 0,

    /// <summary>The Agent Call request record was created with its configuration snapshot (Story 2.1).</summary>
    Requested,

    /// <summary>The invocation gate passed: every dependency check was satisfied and the call may proceed to context building (Story 2.2; Story 2.3 consumes this).</summary>
    Authorized,

    /// <summary>An authorization-class gate check failed (tenant access, caller Party state, or Source Conversation access) — the caller is not permitted; recorded as fail-closed Audit Evidence (Story 2.2; AC2, AC3).</summary>
    Denied,

    /// <summary>A dependency-readiness-class gate check failed (Agent lifecycle/Party identity, Provider/model, response/content-safety policy, or dependency freshness) — required state is missing/stale/ambiguous/disabled/unavailable; recorded as fail-closed Audit Evidence (Story 2.2; AC1).</summary>
    Blocked,

    /// <summary>Conversation context was built within safe bounds (full or an approved bounded behavior) and the call may proceed to generation (Story 2.3; Story 2.4 consumes this) (AC2, AC4).</summary>
    ContextReady,

    /// <summary>Conversation context could not be built within safe bounds (oversized with no approved bounded behavior, not loadable fresh enough, or an untrustworthy model budget) — the call fails closed with no provider call, proposal, or Conversation Message; recorded as fail-closed Audit Evidence (Story 2.3; AC3).</summary>
    ContextBlocked,

    /// <summary>Generation succeeded and the generated content passed Content Safety Policy; the call may proceed to the response-mode branch — Story 2.5 automatic post (→ <see cref="Posted"/>/<see cref="PostingFailed"/>) or Story 3.1 proposal creation (→ <see cref="ProposalCreated"/>/<see cref="ProposalCreationFailed"/>), which consume this state (Story 2.4; AC2).</summary>
    Generated,

    /// <summary>Generation failed closed — provider timeout, disabled provider/model, adapter failure, invalid/unloadable context, or safety-policy failure — recorded as fail-closed Audit Evidence; no Conversation Message and no approvable proposal is created (Story 2.4; AC3).</summary>
    GenerationFailed,

    /// <summary>Generated content failed Content Safety Policy — recorded as fail-closed Audit Evidence; the content is non-postable and non-approvable (no generated version is created) (Story 2.4; AC2, AC3).</summary>
    SafetyFailed,

    /// <summary>The generated version was appended to the Source Conversation as a Conversation Message authored by the Agent Party identity (Story 2.5 automatic mode); this is the terminal success state for an automatic interaction (AC1, AC2).</summary>
    Posted,

    /// <summary>Posting failed closed AFTER successful generation — membership/Party/Conversation/append failure — recorded as fail-closed Audit Evidence; no Conversation Message exists; distinct from generation/auth/context/safety failure (Story 2.5; AC4).</summary>
    PostingFailed,

    /// <summary>A Proposed Agent Reply was created (in Confirmation Response Mode) and awaits authorized Approver action — the terminal creation-success state for a confirmation interaction; the Confirmation-mode counterpart to <see cref="Posted"/> (Story 3.1; AC1).</summary>
    ProposalCreated,

    /// <summary>Proposal creation failed closed AFTER successful generation — the selected version could not be read / adapter failure — recorded as fail-closed Audit Evidence; no approvable proposal exists; distinct from generation/auth/context/safety failure (Story 3.1; AC3, AC4).</summary>
    ProposalCreationFailed,
}
