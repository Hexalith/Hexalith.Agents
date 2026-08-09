using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Client;

/// <summary>Public Agent administration operations.</summary>
/// <remarks>
/// The Story 5.2 in-scope operations (setup reads, create, configuration update, response mode, activate, disable)
/// name their target Agent explicitly and answer with the authoritative projected setup truth or a structured
/// accepted identity. Acceptance is never terminal success: it says the command was accepted, and the caller must
/// re-read the setup to learn whether the projection has confirmed it. The remaining operations stay on the
/// command-only shape until their own stories bind them.
/// </remarks>
public interface IAgentAdministrationOperations
{
    /// <summary>Gets the authoritative Agent setup truth backing the status surface.</summary>
    /// <param name="agentId">The target Agent.</param>
    /// <param name="expectedConfigurationVersion">The configuration version the caller is waiting to see, or <see langword="null"/> for current projected truth.</param>
    /// <param name="options">Optional sanitized operation metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The safe setup result.</returns>
    ValueTask<AgentOperationResult<AgentSetupResult>> GetStatusAsync(
        string agentId,
        int? expectedConfigurationVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the authoritative Agent setup truth backing the configuration surface.</summary>
    /// <param name="agentId">The target Agent.</param>
    /// <param name="expectedConfigurationVersion">The configuration version the caller is waiting to see, or <see langword="null"/> for current projected truth.</param>
    /// <param name="options">Optional sanitized operation metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The safe setup result.</returns>
    ValueTask<AgentOperationResult<AgentSetupResult>> GetConfigurationAsync(
        string agentId,
        int? expectedConfigurationVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates an Agent.</summary>
    /// <param name="agentId">The Agent identity to create.</param>
    /// <param name="command">The safe create payload.</param>
    /// <param name="options">Optional sanitized operation metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity, or a typed failure.</returns>
    ValueTask<AgentOperationResult<AgentCommandAcceptance>> CreateAsync(
        string agentId,
        CreateAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates safe Agent configuration metadata.</summary>
    /// <param name="agentId">The target Agent.</param>
    /// <param name="command">The safe update payload.</param>
    /// <param name="options">Optional sanitized operation metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity, or a typed failure.</returns>
    ValueTask<AgentOperationResult<AgentCommandAcceptance>> UpdateConfigurationAsync(
        string agentId,
        UpdateAgentConfiguration command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Configures an Agent response mode.</summary>
    /// <param name="agentId">The target Agent.</param>
    /// <param name="command">The safe response-mode payload.</param>
    /// <param name="options">Optional sanitized operation metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity, or a typed failure.</returns>
    ValueTask<AgentOperationResult<AgentCommandAcceptance>> ConfigureResponseModeAsync(
        string agentId,
        ConfigureAgentResponseMode command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Activates an Agent. Acceptance is a lifecycle transition, never a callability claim.</summary>
    /// <param name="agentId">The target Agent.</param>
    /// <param name="command">The activate payload.</param>
    /// <param name="options">Optional sanitized operation metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity, or a typed failure.</returns>
    ValueTask<AgentOperationResult<AgentCommandAcceptance>> ActivateAsync(
        string agentId,
        ActivateAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Disables an Agent.</summary>
    /// <param name="agentId">The target Agent.</param>
    /// <param name="command">The disable payload.</param>
    /// <param name="options">Optional sanitized operation metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity, or a typed failure.</returns>
    ValueTask<AgentOperationResult<AgentCommandAcceptance>> DisableAsync(
        string agentId,
        DisableAgent command,
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
}
