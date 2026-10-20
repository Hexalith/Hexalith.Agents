
namespace Hexalith.Agents.Contracts.ConversationAgentState.Commands;

/// <summary>Trusted directory command; expected revision serializes with Closing.</summary>
/// <param name="Lease">The Lease binding.</param>
/// <param name="ExpectedRevision">The ExpectedRevision binding.</param>
public sealed record AcquireConversationEffectLease(ConversationEffectLease Lease, long ExpectedRevision);
