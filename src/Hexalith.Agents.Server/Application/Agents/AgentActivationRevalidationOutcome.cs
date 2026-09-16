using Hexalith.Agents.Contracts.Agent;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal outcome of the activation re-validation step (Story 1.5 AC2; Story 1.6 AC3). <see cref="Authorized"/>
/// is <see langword="false"/> when the actor was not an Agents admin (fail closed before any read or dispatch). When
/// authorized, the <c>ActivateAgent</c> command is dispatched carrying either re-validated provider + approver
/// verdicts or canonical fail-closed replay evidence, so the aggregate's gates can clear only from exact-version
/// validation. Carries only the safe verdicts.
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether the activate command was dispatched.</param>
/// <param name="ProviderVerdict">The re-validated provider-readiness verdict (<c>Unknown</c> when no selection is recorded).</param>
/// <param name="ApproverVerdict">The re-validated approver-policy verdict (<c>Unknown</c> in Automatic mode or when no policy source is configured).</param>
/// <param name="Receipt">The canonical EventStore receipt when dispatch completed.</param>
public sealed record AgentActivationRevalidationOutcome(
    bool Authorized,
    bool Dispatched,
    ProviderSelectionValidationStatus ProviderVerdict,
    ApproverPolicyValidationStatus ApproverVerdict,
    SubmitCommandResponse? Receipt = null)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was read or dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentActivationRevalidationOutcome Denied()
        => new(Authorized: false, Dispatched: false, ProviderSelectionValidationStatus.Unknown, ApproverPolicyValidationStatus.Unknown);

    /// <summary>Creates the outcome for an authorized request whose activate command was dispatched with the verdicts.</summary>
    /// <param name="providerVerdict">The re-validated provider verdict fed to the aggregate.</param>
    /// <param name="approverVerdict">The re-validated approver verdict fed to the aggregate.</param>
    /// <param name="receipt">The canonical EventStore receipt.</param>
    /// <returns>The dispatched outcome.</returns>
    public static AgentActivationRevalidationOutcome FromDispatch(
        ProviderSelectionValidationStatus providerVerdict,
        ApproverPolicyValidationStatus approverVerdict,
        SubmitCommandResponse receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new(Authorized: true, Dispatched: true, providerVerdict, approverVerdict, receipt);
    }
}
