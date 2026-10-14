namespace Hexalith.Agents.Server.Ports;

/// <summary>Current action identity and Source Conversation; stable actor separation spans all principal kinds.</summary>
/// <param name="ActorId">Authenticated stable acting human.</param><param name="PartyId">Exact acting Party.</param>
/// <param name="SourceConversationId">Current source requiring participation/read access.</param>
/// <param name="ExcludedActorId">Stable actor excluded by separation of duties.</param>
public sealed record ApproverResolutionContext(string ActorId, string PartyId, string SourceConversationId, string? ExcludedActorId = null);
