namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// An explicitly approved bounded-context behavior used only when the full Source Conversation does not fit the model
/// context budget (AC4; AD-11, OQ-10). Its <see cref="BehaviorReference"/> and <see cref="BoundedContextTokenLimit"/>
/// are recorded as evidence whenever bounded context is used, so bounded context is NEVER a silent truncation — it is
/// always an explicit, audited policy decision.
/// </summary>
/// <remarks>
/// Carries only a safe opaque reference and a token bound — no raw content (AD-14). This is non-null on a measurement
/// ONLY when the resolved Conversation Context Policy declares an approved bounded behavior. V1's only policy
/// (<see cref="AgentInteractionSnapshot.DefaultContextPolicyReference"/>) approves none, so it resolves to
/// <see langword="null"/> and the oversized case always blocks (OQ-10 keeps a concrete bounded policy deferred).
/// </remarks>
/// <param name="BehaviorReference">An opaque safe reference identifying the approved bounded-context behavior.</param>
/// <param name="BoundedContextTokenLimit">The token bound the approved behavior allows (must fit within the available context budget to be used).</param>
public record AgentInteractionBoundedContextBehavior(
    string BehaviorReference,
    int BoundedContextTokenLimit);
