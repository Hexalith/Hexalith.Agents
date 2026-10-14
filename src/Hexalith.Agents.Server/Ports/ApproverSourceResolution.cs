using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The safe per-source resolution outcome for one configured approver source (Story 1.6 AC3). Carries only the safe
/// source <see cref="Kind"/> and the resolution <see cref="Outcome"/> — no PII and no secret.
/// </summary>
/// <param name="Kind">The approver source kind that was resolved.</param>
/// <param name="Outcome">The fail-closed resolution outcome for that source.</param>
public sealed record ApproverSourceResolution(ApproverPolicySourceKind Kind, ApproverSourceOutcome Outcome)
{
    /// <summary>Gets the exact resolved configured source.</summary>
    public ApproverPolicySource? Source { get; init; }
    /// <summary>Gets current safe actor, binding, source and Conversation authority.</summary>
    public CurrentApproverEvidence? Evidence { get; init; }
    /// <summary>Gets all accepted source candidates retained through final current-authority confirmation.</summary>
    public IReadOnlyList<CurrentApproverEvidence> Candidates { get; init; } = [];
}
