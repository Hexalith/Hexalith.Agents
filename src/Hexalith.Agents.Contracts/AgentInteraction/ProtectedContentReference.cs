using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>The only content handle permitted in workflow history; bytes stay in protected EventStore fields.</summary>
/// <param name="TenantId">The TenantId binding.</param>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="EventRevision">The EventRevision binding.</param>
/// <param name="ContentDigest">The ContentDigest binding.</param>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ProtectedContentReference(string TenantId, string AgentInteractionId, long EventRevision, string ContentDigest);
