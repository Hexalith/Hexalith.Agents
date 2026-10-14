namespace Hexalith.Agents.Server.Ports;

/// <summary>A safe current-authority event from the exact tenant or global-administrators stream.</summary>
/// <param name="Sequence">Contiguous authoritative event position.</param>
/// <param name="ActorId">Opaque authenticated stable human actor.</param>
/// <param name="PartyId">Opaque Party bound to this actor.</param>
/// <param name="Role">Closed operation role or TenantActive.</param>
/// <param name="Granted">Whether the current assignment remains granted.</param>
public sealed record TenantAuthorityEvent(long Sequence, string ActorId, string? PartyId, string Role, bool Granted);
