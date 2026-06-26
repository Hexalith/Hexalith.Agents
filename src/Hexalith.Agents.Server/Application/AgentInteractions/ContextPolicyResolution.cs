using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Pure resolver from a Conversation Context Policy reference to its approved bounded-context behavior, if any
/// (Story 2.3; AC4; AD-11, OQ-10). V1 ships exactly one policy
/// (<see cref="AgentInteractionSnapshot.DefaultContextPolicyReference"/> = <c>"full-conversation-v1"</c>) which approves
/// NO bounded behavior, so an oversized Conversation always blocks rather than being silently truncated. Any unknown
/// reference also resolves to <see langword="null"/> (fail closed).
/// </summary>
/// <remarks>
/// The bounded path's <em>shape</em> (the <see cref="AgentInteractionContextMode.Bounded"/> mode, evidence, and
/// policy-gated branch) is implemented and tested so AC4 is satisfiable, but V1 wires no approved bounded behavior
/// (OQ-10 keeps a concrete bounded policy deferred). A future story binds a concrete bounded policy here once OQ-10 is
/// decided — without inventing a truncation/summarization behavior.
/// </remarks>
internal static class ContextPolicyResolution
{
    /// <summary>Resolves the approved bounded-context behavior for a context policy reference, or <see langword="null"/> when none is approved (V1 default and any unknown reference; fail closed).</summary>
    /// <param name="contextPolicyReference">The snapshot-recorded Conversation Context Policy reference.</param>
    /// <returns>The approved bounded-context behavior, or <see langword="null"/>.</returns>
    internal static AgentInteractionBoundedContextBehavior? Resolve(string contextPolicyReference)
    {
        _ = contextPolicyReference;

        // V1 "full-conversation-v1" approves no bounded behavior; every other reference is unknown and also fails closed.
        return null;
    }
}
