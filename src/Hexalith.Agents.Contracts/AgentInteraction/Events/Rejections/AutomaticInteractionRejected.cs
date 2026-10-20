using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>Content-free automatic-path rejection.</summary>
/// <param name="Reason">The Reason binding.</param>
public sealed record AutomaticInteractionRejected(AutomaticInteractionRejectionReason Reason) : IRejectionEvent;
