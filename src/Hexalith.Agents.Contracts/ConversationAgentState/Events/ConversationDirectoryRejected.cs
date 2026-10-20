using Hexalith.EventStore.Contracts.Events;
namespace Hexalith.Agents.Contracts.ConversationAgentState.Events;

/// <summary>Content-free directory rejection.</summary>
/// <param name="Reason">The Reason binding.</param>
public sealed record ConversationDirectoryRejected(ConversationDirectoryRejectionReason Reason) : IRejectionEvent;
