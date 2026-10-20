using System.Text.Json;
using System.Globalization;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Constructs a deterministic private host-admitted intent. No client extension reaches this path.</summary>
internal static class InteractionCommandDispatch
{
    /// <summary>Dispatches one command; the gateway receipt alone never proves the target append.</summary>
    internal static async Task SendAsync(IAgentCommandDispatcher dispatcher, string tenantId, string domain, string aggregateId, string operationId, object command, CancellationToken ct)
    {
        long expectedRevision = command switch
        {
            RegisterInteractionPermit c => c.ExpectedRevision,
            AcknowledgeInteractionCreation c => c.ExpectedRevision,
            AcquireConversationEffectLease c => c.ExpectedRevision,
            CommitConversationEffect c => c.ExpectedRevision,
            CancelConversationEffectLease c => c.ExpectedRevision,
            SettleConversationEffectLease c => c.ExpectedRevision,
            EnterConversationDeletionClosing c => c.ExpectedRevision,
            CreatePermittedAgentInteraction c => c.ExpectedRevision,
            RecordAutomaticInteractionStep c => c.ExpectedRevision,
            _ => throw new ArgumentException("UnsupportedInteractionCommand", nameof(command)),
        };
        // A terminal stale-revision receipt belongs to this submission, never to the immutable business operation.
        string submissionOperationId = AgentsSecurityIdentity.Derive("interaction-submission", operationId, expectedRevision.ToString(CultureInfo.InvariantCulture));
        var intent = new AgentCommandIntent(AgentsSecurityIdentity.Derive("logical-command", tenantId, "Workflow", aggregateId, domain, command.GetType().Name, submissionOperationId),
            tenantId, domain, aggregateId, command.GetType().Name, JsonSerializer.SerializeToUtf8Bytes(command, command.GetType()),
            aggregateId, null, aggregateId, new Dictionary<string, string>(StringComparer.Ordinal) { [command is Hexalith.Agents.Contracts.ConversationAgentState.Commands.EnterConversationDeletionClosing ? ConversationAgentStateIdentity.DeletionAuthority : ConversationAgentStateIdentity.WorkflowAuthority] = "true" });
        await dispatcher.DispatchAsync(intent, ct).ConfigureAwait(false);
    }
}
