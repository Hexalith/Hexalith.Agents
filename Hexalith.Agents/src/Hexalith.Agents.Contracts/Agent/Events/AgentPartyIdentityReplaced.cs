namespace Hexalith.Agents.Contracts.Agent.Events;

/// <summary>
/// Records that an Agent's (<c>hexa</c>) linked Party identity was explicitly replaced with a different one (AC3;
/// FR-2). Replacement is a configuration change, so <see cref="ConfigurationVersion"/> is bumped; lifecycle is
/// unchanged. The Agent still holds exactly one active Party identity afterwards — the new <see cref="PartyId"/>
/// (AC3).
/// </summary>
/// <remarks>
/// Both <see cref="PreviousPartyId"/> and <see cref="PartyId"/> are stable Parties-owned identifiers —
/// <em>references, not PII</em> — so persisting them is the AC1-sanctioned "store only the stable reference". No
/// Party display name, contact value, personal identifier, or Parties personal-data object is carried here (AD-7,
/// AD-14). <see cref="PreviousPartyId"/> is nullable to tolerate a replace issued against a not-yet-linked Agent.
/// No wall-clock timestamp is carried (AD-3); occurrence time comes from EventStore event metadata.
/// </remarks>
/// <param name="AgentId">Stable Agent identifier (the aggregate id).</param>
/// <param name="PreviousPartyId">The previously linked identifier, or <see langword="null"/> if none was linked (a reference, not PII).</param>
/// <param name="PartyId">The stable Parties-owned identifier now linked (a reference, not PII).</param>
/// <param name="ConfigurationVersion">The bumped configuration version after the replacement.</param>
public record AgentPartyIdentityReplaced(
    string AgentId,
    string? PreviousPartyId,
    string PartyId,
    int ConfigurationVersion) : IEventPayload;
