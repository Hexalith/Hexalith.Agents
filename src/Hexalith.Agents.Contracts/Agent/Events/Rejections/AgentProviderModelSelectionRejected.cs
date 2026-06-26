namespace Hexalith.Agents.Contracts.Agent.Events.Rejections;

/// <summary>
/// Selecting a Provider/model was rejected because the trusted provider-readiness verdict was not
/// <see cref="ProviderSelectionValidationStatus.Valid"/> — the Provider/model was disabled, missing, not
/// configured, not text-generation capable, lacked required capability metadata, was unauthorized, or unavailable
/// (failing closed); or the verdict was absent (a direct-gateway call) (AC2; FR-21). The Agent's recorded
/// selection is unchanged and <c>hexa</c> remains not callable. No provider SDK call or credential access occurs
/// on this rejection path — the verdict is computed from the safe catalog projection (AD-9, AD-12).
/// </summary>
/// <remarks>
/// <see cref="Status"/> classifies <em>which</em> provider-readiness state blocked the selection; it carries no
/// secret value, configuration reference, or capability-metadata blob (AC1; AD-9, AD-14). Recording the verdict
/// gives an auditable trail of the fail-closed decision without exposing any secret or provider detail.
/// </remarks>
/// <param name="AgentId">The Agent aggregate identifier the selection targeted.</param>
/// <param name="Status">The non-<see cref="ProviderSelectionValidationStatus.Valid"/> verdict that caused the rejection.</param>
public record AgentProviderModelSelectionRejected(
    string AgentId,
    ProviderSelectionValidationStatus Status) : IRejectionEvent;
