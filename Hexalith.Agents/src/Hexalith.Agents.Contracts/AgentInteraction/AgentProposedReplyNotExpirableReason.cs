using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> an expire-proposed-reply command could not be evaluated (AC3; FR-18, AD-3,
/// AD-12). Expiry is system-triggered policy, not an approver action: a not-expirable classification means the deterministic
/// expiry decision did not transition the proposal — either the proposal is not pending, no expiry policy is configured, or
/// the configured expiry has not yet elapsed against the trusted evaluation timestamp. Carried on
/// <see cref="Events.Rejections.ProposedAgentReplyNotExpirableRejection"/>.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel. Serialized by name so an absent value never resolves to a
/// concrete classification. <see cref="InteractionNotProposed"/> and <see cref="ProposalNotPending"/> are the aggregate's
/// structural rejections; <see cref="NoExpiryPolicy"/> and <see cref="ExpiryNotReached"/> are the orchestrator's deterministic
/// no-transition outcomes (no expiry configured, or the elapsed time has not reached <c>ExpiresAt</c>) — neither dispatches a
/// terminal transition (AD-3: expiry "now" is supplied to the orchestrator, never read by the aggregate).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposedReplyNotExpirableReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The interaction has no proposal to expire (no proposal was created, or the stream has no recorded request).</summary>
    InteractionNotProposed,

    /// <summary>The proposal has reached a terminal/non-pending state (approved, rejected, abandoned, expired, posted) and can no longer expire.</summary>
    ProposalNotPending,

    /// <summary>No proposal expiry policy is configured (no recorded <c>ExpiresAt</c>) — the proposal is unbounded and never expires (fail closed to no-expiry).</summary>
    NoExpiryPolicy,

    /// <summary>The configured expiry has not yet elapsed against the trusted evaluation timestamp — the proposal stays pending.</summary>
    ExpiryNotReached,
}
