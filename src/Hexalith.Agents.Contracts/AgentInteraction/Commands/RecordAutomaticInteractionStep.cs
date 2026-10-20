
namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>Records one durable phase at the trusted command seam; lease-bound results name their exact commit revision.</summary>
/// <param name="TenantId">The TenantId binding.</param>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="Fact">The Fact binding.</param>
/// <param name="ExpectedRevision">The ExpectedRevision binding.</param>
public sealed record RecordAutomaticInteractionStep(string TenantId, string AgentInteractionId, AutomaticInteractionFact Fact, long ExpectedRevision);
