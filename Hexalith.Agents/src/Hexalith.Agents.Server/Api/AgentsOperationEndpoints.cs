using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.Server.Api;

/// <summary>
/// Registers the public Agents operations API/BFF contract surface.
/// </summary>
public static class AgentsOperationEndpoints
{
    /// <summary>
    /// Maps stable public Agents operation endpoints.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The mapped route group.</returns>
    public static RouteGroupBuilder MapAgentsOperationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder group = endpoints.MapGroup("/api/agents/operations");

        MapProviderCatalog(group);
        MapAgentAdministration(group);
        MapInteractions(group);
        MapProposalWorkflow(group);
        MapStatus(group);
        MapAudit(group);

        return group;
    }

    private static void MapProviderCatalog(RouteGroupBuilder group)
    {
        RouteGroupBuilder providers = group.MapGroup("/providers");

        providers.MapGet("/", (bool includeDisabled, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProviderCatalog.ListEntriesAsync(includeDisabled, cancellationToken: cancellationToken));
        providers.MapGet("/{providerId}/{modelId}", (string providerId, string modelId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProviderCatalog.GetEntryAsync(providerId, modelId, cancellationToken: cancellationToken));
        providers.MapPost("/", (CreateProviderModelEntry command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProviderCatalog.CreateEntryAsync(command, cancellationToken: cancellationToken));
        providers.MapPut("/", (UpdateProviderModelEntry command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProviderCatalog.UpdateEntryAsync(command, cancellationToken: cancellationToken));
        providers.MapPost("/enable", (EnableProviderModelEntry command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProviderCatalog.EnableEntryAsync(command, cancellationToken: cancellationToken));
        providers.MapPost("/disable", (DisableProviderModelEntry command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProviderCatalog.DisableEntryAsync(command, cancellationToken: cancellationToken));
    }

    private static void MapAgentAdministration(RouteGroupBuilder group)
    {
        RouteGroupBuilder agents = group.MapGroup("/agents");

        agents.MapGet("/{agentId}/status", (string agentId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.GetStatusAsync(agentId, cancellationToken: cancellationToken));
        agents.MapGet("/{agentId}/configuration", (string agentId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.GetConfigurationAsync(agentId, cancellationToken: cancellationToken));
        agents.MapPost("/", (CreateAgent command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.CreateAsync(command, cancellationToken: cancellationToken));
        agents.MapPut("/", (UpdateAgentConfiguration command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.UpdateConfigurationAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/party-link", (LinkAgentPartyIdentity command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.LinkPartyIdentityAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/party-link/replace", (ReplaceAgentPartyIdentity command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.ReplacePartyIdentityAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/provider-selection", (SelectAgentProviderModel command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.SelectProviderModelAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/response-mode", (ConfigureAgentResponseMode command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.ConfigureResponseModeAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/approver-policy", (ConfigureAgentApproverPolicy command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.ConfigureApproverPolicyAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/content-safety-policy", (ConfigureAgentContentSafetyPolicy command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.ConfigureContentSafetyPolicyAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/launch-readiness", (RecordAgentLaunchReadiness command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.RecordLaunchReadinessAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/enable-production-like-generation", (EnableProductionLikeGeneration command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.EnableProductionLikeGenerationAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/activate", (ActivateAgent command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.ActivateAsync(command, cancellationToken: cancellationToken));
        agents.MapPost("/disable", (DisableAgent command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentAdministration.DisableAsync(command, cancellationToken: cancellationToken));
    }

    private static void MapInteractions(RouteGroupBuilder group)
    {
        RouteGroupBuilder interactions = group.MapGroup("/interactions");

        interactions.MapPost("/", (RequestAgentInteraction command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentInteractions.RequestAsync(command, cancellationToken: cancellationToken));
        interactions.MapGet("/{agentInteractionId}/status", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentInteractions.GetStatusAsync(agentInteractionId, cancellationToken: cancellationToken));
        interactions.MapGet("/{agentInteractionId}/gate-evidence", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentInteractions.GetGateEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        interactions.MapGet("/{agentInteractionId}/context-evidence", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentInteractions.GetContextEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        interactions.MapGet("/{agentInteractionId}/generation-evidence", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentInteractions.GetGenerationEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        interactions.MapGet("/{agentInteractionId}/posting-evidence", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentInteractions.GetPostingEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        interactions.MapGet("/{agentInteractionId}/approval-evidence", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.AgentInteractions.GetApprovalEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
    }

    private static void MapProposalWorkflow(RouteGroupBuilder group)
    {
        RouteGroupBuilder proposals = group.MapGroup("/proposals");

        proposals.MapGet("/", (bool includeHistorical, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.ListAsync(includeHistorical, cancellationToken: cancellationToken));
        proposals.MapGet("/{agentInteractionId}", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.GetDetailAsync(agentInteractionId, cancellationToken: cancellationToken));
        proposals.MapPost("/edit", (EditProposedAgentReply command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.EditAsync(command, cancellationToken: cancellationToken));
        proposals.MapPost("/regenerate", (RegenerateProposedAgentReply command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.RegenerateAsync(command, cancellationToken: cancellationToken));
        proposals.MapPost("/approve", (ApproveProposedAgentReply command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.ApproveAsync(command, cancellationToken: cancellationToken));
        proposals.MapPost("/reject", (RejectProposedAgentReply command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.RejectAsync(command, cancellationToken: cancellationToken));
        proposals.MapPost("/abandon", (AbandonProposedAgentReply command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.AbandonAsync(command, cancellationToken: cancellationToken));
        proposals.MapPost("/expire", (ExpireProposedAgentReply command, IAgentsClient client, CancellationToken cancellationToken) =>
            client.ProposalWorkflow.ExpireAsync(command, cancellationToken: cancellationToken));
    }

    private static void MapStatus(RouteGroupBuilder group)
    {
        RouteGroupBuilder status = group.MapGroup("/status");

        status.MapGet("/agents/{agentId}/readiness", (string agentId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Status.GetAgentReadinessAsync(agentId, cancellationToken: cancellationToken));
        status.MapGet("/agents/{agentId}/launch-readiness", (string agentId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Status.GetAgentLaunchReadinessAsync(agentId, cancellationToken: cancellationToken));
        status.MapGet("/providers/{providerId}/{modelId}/readiness", (string providerId, string modelId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Status.GetProviderModelReadinessAsync(providerId, modelId, cancellationToken: cancellationToken));
        status.MapGet("/interactions/{agentInteractionId}/call", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Status.GetCallStatusAsync(agentInteractionId, cancellationToken: cancellationToken));
        status.MapGet("/interactions/{agentInteractionId}/proposal", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Status.GetProposalStatusAsync(agentInteractionId, cancellationToken: cancellationToken));
        status.MapGet("/interactions/{agentInteractionId}/audit", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Status.GetAuditAvailabilityAsync(agentInteractionId, cancellationToken: cancellationToken));
    }

    private static void MapAudit(RouteGroupBuilder group)
    {
        RouteGroupBuilder audit = group.MapGroup("/audit");

        audit.MapGet("/interactions/{agentInteractionId}/gate", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Audit.GetGateEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        audit.MapGet("/interactions/{agentInteractionId}/context", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Audit.GetContextEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        audit.MapGet("/interactions/{agentInteractionId}/generation", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Audit.GetGenerationEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        audit.MapGet("/interactions/{agentInteractionId}/posting", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Audit.GetPostingEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        audit.MapGet("/interactions/{agentInteractionId}/proposal-edit", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Audit.GetProposalEditEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        audit.MapGet("/interactions/{agentInteractionId}/proposal-regeneration", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Audit.GetProposalRegenerationEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
        audit.MapGet("/interactions/{agentInteractionId}/proposal-approval", (string agentInteractionId, IAgentsClient client, CancellationToken cancellationToken) =>
            client.Audit.GetProposalApprovalEvidenceAsync(agentInteractionId, cancellationToken: cancellationToken));
    }
}
