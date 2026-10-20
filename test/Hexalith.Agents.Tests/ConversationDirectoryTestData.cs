using System.Text.Json;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Security;

namespace Hexalith.Agents.Tests;

/// <summary>Explicit no-content fixture; it supplies no live migration or protection attestation.</summary>
internal static class ConversationDirectoryTestData
{
    /// <summary>Gets the original permit.</summary>
    internal static InteractionPermit Permit()
    {
        const string tenant = "tenant-a"; const string conversation = "conversation-a";
        string id = ConversationAgentStateIdentity.Interaction(tenant, "hexa", conversation, "person-a", "client-a");
        return new(tenant, conversation, id, "hexa", "person-a", "client-a", ConversationAgentStateIdentity.Permit(tenant, id),
            ConversationAgentStateIdentity.CreationOutbox(tenant, id), new string('a', 64), new(1, 1, AgentResponseMode.Automatic, 1, "provider-a", "model-a", 1, 1, "full-conversation-v1"),
            new(tenant, "fixture-installation", "fixture-epoch", 0, "fixture-revocation", "fixture-enforcement"),
            new(new(tenant, id, "fixture-interaction-dek"), JsonSerializer.SerializeToElement(new { FixtureSealed = "request" })), "fixture-digest-v1");
    }
    /// <summary>Gets a trusted test envelope.</summary>
    internal static CommandEnvelope Envelope() => new("message", "tenant-a", ConversationAgentStateIdentity.Domain,
        ConversationAgentStateIdentity.Directory("tenant-a", "conversation-a"), nameof(RegisterInteractionPermit), [], "correlation", null, Permit().AgentInteractionId,
        new Dictionary<string, string>(StringComparer.Ordinal) { [ConversationAgentStateIdentity.WorkflowAuthority] = "true" });
    /// <summary>Gets the separately admitted Closing workflow envelope.</summary>
    internal static CommandEnvelope DeletionEnvelope() => Envelope() with { Extensions = new Dictionary<string, string>(StringComparer.Ordinal) { [ConversationAgentStateIdentity.DeletionAuthority] = "true" } };
    /// <summary>Replays a real permit decision.</summary>
    internal static ConversationAgentStateState State()
    {
        var state = new ConversationAgentStateState();
        foreach (var item in ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(Permit(), 0), null, Envelope()).Events) { state.Apply((dynamic)item); }
        return state;
    }
    /// <summary>Gets the exact reserved lease.</summary>
    internal static ConversationEffectLease Lease(ConversationEffectKind kind = ConversationEffectKind.WorkflowStart)
    {
        var p = Permit(); string operationId = AgentsSecurityIdentity.Derive("fixture-operation", kind.ToString());
        return new(ConversationAgentStateIdentity.Lease(p.TenantId, p.ConversationId, p.AgentInteractionId, kind, "fixture", operationId), p.AgentInteractionId, kind, "fixture", operationId,
            p.AgentInteractionId, p.DirectoryEpoch.EpochId, ConversationEffectLeaseStatus.Reserved, 0, 0, false);
    }
    /// <summary>Folds accepted events through the public replay seam.</summary>
    internal static void Apply(ConversationAgentStateState state, Hexalith.EventStore.Contracts.Results.DomainResult result)
    { foreach (var item in result.Events) { state.Apply((dynamic)item); } }
}
