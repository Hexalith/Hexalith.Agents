namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The server-assembled input to the pure context-budget decision (AC2, AC3, AC4; AD-3, AD-11). The orchestration
/// assembles this from a trusted live Conversations read, a token measurement, and the Provider-catalog budget read,
/// then puts it on <see cref="Commands.BuildAgentInteractionContext"/>; the pure aggregate decides on it and never
/// reads any dependency itself (AD-3).
/// </summary>
/// <remarks>
/// Carries ONLY safe scalars/enums/references and the <em>measured</em> token count — NEVER the raw Conversation text,
/// prompt, claims, tokens(secret), <c>PartyId</c> personal data, or provider payloads (AD-14). When
/// <see cref="LoadOutcome"/> is not <see cref="AgentInteractionContextLoadOutcome.Loaded"/> the numeric fields are
/// <c>0</c> (the read failed before measurement); <see cref="ApprovedBoundedBehavior"/> is non-null only when the
/// resolved context policy declares an approved bounded behavior (V1 default resolves to <see langword="null"/>).
/// </remarks>
/// <param name="LoadOutcome">The server-assembled classification of the authorized Conversations read.</param>
/// <param name="FullContextTokenCount">The measured token count of the full visible Source Conversation timeline (<c>0</c> when not loaded).</param>
/// <param name="MessageCount">The visible message count of the loaded timeline (<c>0</c> when not loaded).</param>
/// <param name="ContextWindowTokenLimit">The selected model's context-window token limit from the Provider catalog (<c>0</c> when the budget could not be trusted).</param>
/// <param name="ReservedOutputTokenCount">The configured output tokens reserved before fitting context (the catalog entry's max-output limit; AC2).</param>
/// <param name="ProviderCapabilityVersion">The Provider capability version backing the budget (<c>0</c> when the budget could not be trusted).</param>
/// <param name="ContextPolicyReference">The Conversation Context Policy reference in force (the snapshot's policy reference; recorded in evidence — FR-9).</param>
/// <param name="ApprovedBoundedBehavior">The approved bounded-context behavior, or <see langword="null"/> when the policy approves none (fail closed on overflow).</param>
public record AgentInteractionContextMeasurement(
    AgentInteractionContextLoadOutcome LoadOutcome,
    int FullContextTokenCount,
    int MessageCount,
    int ContextWindowTokenLimit,
    int ReservedOutputTokenCount,
    int ProviderCapabilityVersion,
    string ContextPolicyReference,
    AgentInteractionBoundedContextBehavior? ApprovedBoundedBehavior);
