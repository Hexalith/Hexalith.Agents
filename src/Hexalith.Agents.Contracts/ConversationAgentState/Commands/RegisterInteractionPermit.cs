
namespace Hexalith.Agents.Contracts.ConversationAgentState.Commands;

/// <summary>Trusted directory command; expected revision serializes with Closing.</summary>
/// <param name="Permit">The Permit binding.</param>
/// <param name="ExpectedRevision">The ExpectedRevision binding.</param>
public sealed record RegisterInteractionPermit(InteractionPermit Permit, long ExpectedRevision);
