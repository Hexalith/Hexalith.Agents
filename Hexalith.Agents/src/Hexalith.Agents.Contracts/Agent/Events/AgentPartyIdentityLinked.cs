namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent (<c>hexa</c>) was linked to its single active Party identity (AC1, AC3; FR-2). Linking is
/// a configuration change, so <see cref="ConfigurationVersion"/> is bumped. Lifecycle is deliberately unchanged —
/// a linked Party clears the <see cref="AgentActivationBlocker.MissingPartyIdentity"/> readiness gate but does not
/// by itself make the Agent <see cref="AgentLifecycleStatus.Active"/> (the Story 1.3 lifecycle/readiness invariant).
/// </summary>
/// <remarks>
/// <see cref="PartyId"/> is a stable Parties-owned identifier — a <em>reference, not PII</em> — so persisting it is
/// exactly the AC1-sanctioned "store only the stable reference". No Party display name, contact value, personal
/// identifier, or Parties personal-data object is carried here (AD-7, AD-14). No wall-clock timestamp is carried
/// (AD-3); occurrence time comes from EventStore event metadata.
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="PartyId">The stable Parties-owned identifier now linked (a reference, not PII).</param>
/// <param name="ConfigurationVersion">The bumped configuration version after the link.</param>
public record AgentPartyIdentityLinked(
    string AgentId,
    string PartyId,
    int ConfigurationVersion) : IEventPayload;
