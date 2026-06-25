using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Client;

/// <summary>Public Agent administration operations.</summary>
public interface IAgentAdministrationOperations
{
    /// <summary>Gets the safe Agent status view.</summary>
    ValueTask<AgentOperationResult<AgentInspectionResult>> GetStatusAsync(
        string agentId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the safe Agent configuration view.</summary>
    ValueTask<AgentOperationResult<AgentInspectionResult>> GetConfigurationAsync(
        string agentId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates an Agent.</summary>
    ValueTask<AgentOperationResult> CreateAsync(
        CreateAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates safe Agent configuration metadata.</summary>
    ValueTask<AgentOperationResult> UpdateConfigurationAsync(
        UpdateAgentConfiguration command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Links an Agent to a Party identity.</summary>
    ValueTask<AgentOperationResult> LinkPartyIdentityAsync(
        LinkAgentPartyIdentity command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces an Agent Party identity link.</summary>
    ValueTask<AgentOperationResult> ReplacePartyIdentityAsync(
        ReplaceAgentPartyIdentity command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Selects a provider/model for an Agent.</summary>
    ValueTask<AgentOperationResult> SelectProviderModelAsync(
        SelectAgentProviderModel command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Configures an Agent response mode.</summary>
    ValueTask<AgentOperationResult> ConfigureResponseModeAsync(
        ConfigureAgentResponseMode command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Configures an Agent approver policy.</summary>
    ValueTask<AgentOperationResult> ConfigureApproverPolicyAsync(
        ConfigureAgentApproverPolicy command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Configures an Agent content-safety policy.</summary>
    ValueTask<AgentOperationResult> ConfigureContentSafetyPolicyAsync(
        ConfigureAgentContentSafetyPolicy command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Records an Agent launch-readiness decision (metrics, per-mode latency targets, cost posture, context reference).</summary>
    ValueTask<AgentOperationResult> RecordLaunchReadinessAsync(
        RecordAgentLaunchReadiness command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Enables production-like generation behind the launch-readiness gate (blocked when readiness gates fail).</summary>
    ValueTask<AgentOperationResult> EnableProductionLikeGenerationAsync(
        EnableProductionLikeGeneration command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Activates an Agent.</summary>
    ValueTask<AgentOperationResult> ActivateAsync(
        ActivateAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Disables an Agent.</summary>
    ValueTask<AgentOperationResult> DisableAsync(
        DisableAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);
}
