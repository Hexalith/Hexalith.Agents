using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>One current human candidate with exact source, actor binding and Conversation authority correspondence.</summary>
/// <param name="Source">Exact configured source.</param><param name="Actor">Current active human binding.</param>
/// <param name="SourceConversationId">Authorized exact source.</param><param name="HasReadAccess">Current participation/read authority.</param>
public sealed record CurrentApproverEvidence(ApproverPolicySource Source, CurrentHumanActorEvidence Actor,
    string? SourceConversationId, bool HasReadAccess);
