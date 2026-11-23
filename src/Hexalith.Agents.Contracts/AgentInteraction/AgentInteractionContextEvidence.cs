using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one Conversation context decision (AC2 "safe context evidence"; AC4 "audit-safe
/// metadata"; FR-24, FR-25). Recorded on both <see cref="Events.AgentInteractionContextReady"/> and
/// <see cref="Events.AgentInteractionContextBlocked"/> so a scoped inspector or eligible proposal approver can distinguish a full vs
/// bounded vs blocked context decision and see the budget math behind it.
/// </summary>
/// <remarks>
/// Carries ONLY safe numerics/enums/references — deliberately NEVER message text, the prompt, claims, tokens(secret),
/// <c>PartyId</c> personal data, or provider payloads (AD-9, AD-14), so the context events stay content-free.
/// <see cref="BoundedBehaviorReference"/> is <see langword="null"/> for <see cref="AgentInteractionContextMode.Full"/>;
/// on a blocked-before-load record the numeric fields are <c>0</c> and the block reason (on the event) classifies the
/// failure, while on an oversized block the <see cref="FullContextTokenCount"/> + budget are recorded so audit shows
/// the overflow.
/// </remarks>
/// <param name="Mode">How the context check resolved, including <see cref="AgentInteractionContextMode.Blocked"/> on new refusals.</param>
/// <param name="FullContextTokenCount">The measured token count of the full Source Conversation.</param>
/// <param name="UsedContextTokenCount">The token count actually used (equals <see cref="FullContextTokenCount"/> for full; the bounded limit for bounded; <c>0</c> when blocked).</param>
/// <param name="MessageCount">The visible message count of the loaded timeline.</param>
/// <param name="ReservedOutputTokenCount">The configured output tokens reserved before fitting context (AC2).</param>
/// <param name="ContextWindowTokenLimit">The selected model's context-window token limit.</param>
/// <param name="ProviderCapabilityVersion">The Provider capability version backing the budget.</param>
/// <param name="ContextPolicyReference">The Conversation Context Policy reference in force (FR-9).</param>
/// <param name="BoundedBehaviorReference">The approved bounded-context behavior reference when <see cref="Mode"/> is <see cref="AgentInteractionContextMode.Bounded"/>, otherwise <see langword="null"/>.</param>
public record AgentInteractionContextEvidence(
    AgentInteractionContextMode Mode,
    int FullContextTokenCount,
    int UsedContextTokenCount,
    int MessageCount,
    int ReservedOutputTokenCount,
    int ContextWindowTokenLimit,
    int ProviderCapabilityVersion,
    string ContextPolicyReference,
    string? BoundedBehaviorReference)
{
    /// <summary>Gets the percentage of the context window reserved as a safety margin, when evaluated.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SafetyMarginPercent { get; init; }

    /// <summary>Gets the window safety margin in whole tokens, when evaluated.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SafetyMarginTokenCount { get; init; }

    /// <summary>Gets the remaining token budget after reserves, which can be negative when the reserves exceed the window.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? AvailableContextTokenBudget { get; init; }
}
