using System.Globalization;
using Hexalith.Agents.Contracts.ConversationAgentState;

namespace Hexalith.Agents.Contracts.Security;

/// <summary>AD-29 identities; no content enters the identity derivation.</summary>
public static class ConversationAgentStateIdentity
{
    /// <summary>The directory aggregate domain.</summary>
    public const string Domain = "conversation-agent-state";
    /// <summary>The private host-admitted Interaction Workflow authority assertion.</summary>
    public const string WorkflowAuthority = "actor:interactionWorkflow";
    /// <summary>The separately admitted target-limited deletion Workflow assertion; it grants no interaction effect.</summary>
    public const string DeletionAuthority = "actor:conversationDeletionWorkflow";
    /// <summary>Derives one tenant/Conversation directory owner.</summary>
    public static string Directory(string tenantId, string conversationId) => AgentsSecurityIdentity.Derive("conversation-agent-state", tenantId, conversationId);
    /// <summary>Derives an interaction using the shipped untagged AD-29 formula.</summary>
    public static string Interaction(string tenantId, string agentId, string conversationId, string callerPartyId, string clientKey) => AgentsSecurityIdentity.Derive(tenantId, agentId, conversationId, callerPartyId, clientKey);
    /// <summary>Derives the permit.</summary>
    public static string Permit(string tenantId, string interactionId) => AgentsSecurityIdentity.Derive("interaction-permit", tenantId, interactionId);
    /// <summary>Derives the creation outbox.</summary>
    public static string CreationOutbox(string tenantId, string interactionId) => AgentsSecurityIdentity.Derive("interaction-creation-outbox", tenantId, interactionId);
    /// <summary>Derives the workflow-start outbox.</summary>
    public static string WorkflowStartOutbox(string tenantId, string interactionId) => AgentsSecurityIdentity.Derive("interaction-workflow-start-outbox", tenantId, interactionId);
    /// <summary>Derives the directory acknowledgement outbox.</summary>
    public static string DirectoryAckOutbox(string tenantId, string interactionId) => AgentsSecurityIdentity.Derive("interaction-directory-ack-outbox", tenantId, interactionId);
    /// <summary>Derives the exact phase lease.</summary>
    public static string Lease(string tenantId, string conversationId, string interactionId, ConversationEffectKind kind, string operationFamily, string operationIdentity) => AgentsSecurityIdentity.Derive("conversation-effect-lease", tenantId, conversationId, interactionId, kind.ToString(), operationFamily, operationIdentity);
    /// <summary>Derives an immutable attempt.</summary>
    public static string Attempt(string tenantId, string interactionId, int ordinal) => AgentsSecurityIdentity.Derive("attempt", tenantId, interactionId, ordinal.ToString(CultureInfo.InvariantCulture));
    /// <summary>Derives the reservation.</summary>
    public static string Reservation(string attemptId) => AgentsSecurityIdentity.Derive("reservation", attemptId);
    /// <summary>Derives the capacity admission.</summary>
    public static string Admission(string attemptId) => AgentsSecurityIdentity.Derive("admission", attemptId);
    /// <summary>Derives an immutable generated version.</summary>
    public static string Version(string interactionId, int ordinal, string kind) => AgentsSecurityIdentity.Derive("version", interactionId, ordinal.ToString(CultureInfo.InvariantCulture), kind);
    /// <summary>Derives the exact post identity.</summary>
    public static string Message(string interactionId, string versionId) => AgentsSecurityIdentity.Derive("message-id", interactionId, versionId);
}
