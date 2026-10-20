namespace Hexalith.Agents.Server.Ports;

/// <summary>Fresh authorization, human Party binding, Conversation access, lifecycle, Blocked and provider eligibility in FR-8 order.</summary>
public interface IAutomaticInteractionIntakeGate
{
    /// <summary>Runs gates 1–4 before permit creation or any membership/Approver effect.</summary>
    Task<AutomaticInteractionGateResult> EvaluateAsync(AutomaticInteractionIntakeRequest request, CancellationToken ct);
}
