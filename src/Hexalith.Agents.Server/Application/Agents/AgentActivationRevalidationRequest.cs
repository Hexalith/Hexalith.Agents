using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal request driving the activation re-validation step (Story 1.5 AC2 "or activate"; Story 1.6 AC3).
/// Activation re-validates all of the Agent's <em>currently recorded</em> dependency gates against their live state
/// before dispatching <c>ActivateAgent</c> — the recorded Provider/model against the catalog, and (in Confirmation
/// mode) the recorded Approver Policy sources against Parties/Tenants/Conversations — so a genuinely-ready agent can
/// activate while a stale/degraded one fails closed. The recorded selection
/// (<see cref="SelectedProviderId"/>/<see cref="SelectedModelId"/>), <see cref="ResponseMode"/>, and
/// <see cref="RecordedApproverPolicy"/> are all supplied by the caller from the Agent status read.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope (also the catalog / dependency scope).</param>
/// <param name="AgentId">The Agent aggregate id.</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsAgentsAdmin">The trusted Agents-admin decision from claims (the orchestration fails closed when false).</param>
/// <param name="ExpectedConfigurationVersion">The positive projected configuration version captured by the activation intent.</param>
/// <param name="SelectedProviderId">The recorded selected provider id, or <see langword="null"/> when none is selected.</param>
/// <param name="SelectedModelId">The recorded selected model id, or <see langword="null"/> when none is selected.</param>
/// <param name="ResponseMode">The recorded Response Mode (decides whether the approver policy is re-resolved; 1.6 AC3).</param>
/// <param name="RecordedApproverPolicy">The recorded Approver Policy, or <see langword="null"/> when none is configured.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record AgentActivationRevalidationRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    bool IsAgentsAdmin,
    int ExpectedConfigurationVersion,
    string? SelectedProviderId,
    string? SelectedModelId,
    AgentResponseMode ResponseMode = AgentResponseMode.Unknown,
    AgentApproverPolicy? RecordedApproverPolicy = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);
