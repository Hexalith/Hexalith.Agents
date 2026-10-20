using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>Closing prevented a new posting effect. This fact grants no lease authority and preserves the successful generated version.</summary>
/// <param name="Fact">The content-free suppression and exact Closing owner reference.</param>
public sealed record ConversationPostingSuppressedByDeletion(AutomaticInteractionFact Fact) : IEventPayload;
