using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>Immutable content-safe automatic phase fact.</summary>
/// <param name="Fact">The Fact binding.</param>
public sealed record ConversationPostingRecorded(AutomaticInteractionFact Fact) : IEventPayload;
