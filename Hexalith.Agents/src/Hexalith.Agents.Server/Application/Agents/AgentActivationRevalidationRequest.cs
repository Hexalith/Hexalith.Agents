using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal request driving the activation re-validation step (Story 1.5 AC2 "or activate"). Activation must
/// re-validate the Agent's <em>currently recorded</em> Provider/model against the live catalog before dispatching
/// <c>ActivateAgent</c>, so a genuinely-ready agent can activate while a selected-but-no-longer-ready one fails
/// closed. The recorded selection (<see cref="SelectedProviderId"/>/<see cref="SelectedModelId"/>) is supplied by
/// the caller from the Agent status read; both are <see langword="null"/> when no selection has been recorded yet.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope (also the catalog scope).</param>
/// <param name="AgentId">The Agent aggregate id.</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsAgentsAdmin">The trusted Agents-admin decision from claims (the orchestration fails closed when false).</param>
/// <param name="SelectedProviderId">The recorded selected provider id, or <see langword="null"/> when none is selected.</param>
/// <param name="SelectedModelId">The recorded selected model id, or <see langword="null"/> when none is selected.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record AgentActivationRevalidationRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    bool IsAgentsAdmin,
    string? SelectedProviderId,
    string? SelectedModelId,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the activation re-validation step (Story 1.5 AC2). <see cref="Authorized"/> is
/// <see langword="false"/> when the actor was not an Agents admin (fail closed before any read or dispatch). When
/// authorized, the <c>ActivateAgent</c> command is dispatched carrying the re-validated provider verdict so the
/// aggregate's gate can clear (or fail closed). Carries only the safe verdict.
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether the activate command was dispatched.</param>
/// <param name="ProviderVerdict">The re-validated provider-readiness verdict fed to the aggregate (<c>Unknown</c> when no selection is recorded).</param>
public sealed record AgentActivationRevalidationOutcome(
    bool Authorized,
    bool Dispatched,
    ProviderSelectionValidationStatus ProviderVerdict)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was read or dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentActivationRevalidationOutcome Denied()
        => new(Authorized: false, Dispatched: false, ProviderSelectionValidationStatus.Unknown);

    /// <summary>Creates the outcome for an authorized request whose activate command was dispatched with the verdict.</summary>
    /// <param name="verdict">The re-validated provider verdict fed to the aggregate.</param>
    /// <returns>The dispatched outcome.</returns>
    public static AgentActivationRevalidationOutcome FromDispatch(ProviderSelectionValidationStatus verdict)
        => new(Authorized: true, Dispatched: true, verdict);
}
