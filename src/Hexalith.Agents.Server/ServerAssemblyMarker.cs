using Hexalith.Agents.Contracts;

namespace Hexalith.Agents.Server;

/// <summary>
/// Marks the <c>Hexalith.Agents.Server</c> boundary assembly for the EventStore domain-service assembly scan
/// (<c>AddEventStoreDomainService</c>) and the boundary-guard tests. Registers no runtime behavior. Mirrors the
/// sibling <c>ServerAssemblyMarker</c> pattern so future <c>IDomainQueryHandler</c> / <c>IDomainProjectionHandler</c>
/// implementations in this assembly are discovered without changing the host wiring.
/// </summary>
public static class ServerAssemblyMarker
{
    /// <summary>Gets the contracts marker type for boundary smoke tests.</summary>
    public static Type ContractsMarkerType => typeof(AgentsContractsAssemblyMarker);

    /// <summary>Gets the domain marker type for boundary smoke tests.</summary>
    public static Type DomainMarkerType => typeof(AgentsAssemblyMarker);
}
