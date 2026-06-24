using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Pure, deterministic mapping from a safe approver-policy resolution result to the trusted
/// <see cref="ApproverPolicyValidationStatus"/> verdict (Story 1.6 AC3; AD-8, AD-12). Mirrors
/// <see cref="ProviderSelectionVerdict"/> in spirit — used by the activation re-validation step so the aggregate's
/// approver gate clears only for a genuinely-resolvable policy and fails closed otherwise.
/// </summary>
/// <remarks>
/// Precedence is worst-first (fail closed): an empty policy is <see cref="ApproverPolicyValidationStatus.Incomplete"/>;
/// then any <c>Unauthorized</c> source dominates (a cross-tenant source is never permissive — AC3), then any
/// <c>Unavailable</c>/<c>Unknown</c> (stale/degraded fails closed — AD-12), then <c>Ambiguous</c>, then
/// <c>Disabled</c>, then <c>Missing</c>; only when every source is <c>Resolved</c> is the policy
/// <see cref="ApproverPolicyValidationStatus.Valid"/>. It reads only safe outcomes and never resolves to
/// <c>Valid</c> under any uncertainty.
/// </remarks>
internal static class ApproverPolicyVerdict
{
    /// <summary>Computes the fail-closed approver-policy verdict from a resolution result.</summary>
    /// <param name="policy">The configured Approver Policy (its source count decides the empty/Incomplete case).</param>
    /// <param name="result">The safe per-source resolution result.</param>
    /// <returns>The deterministic verdict.</returns>
    internal static ApproverPolicyValidationStatus Evaluate(AgentApproverPolicy policy, ApproverPolicyResolutionResult result)
    {
        if (policy.Sources.Count == 0)
        {
            return ApproverPolicyValidationStatus.Incomplete;
        }

        // Fail closed on a partial/degraded read: if the resolution does not cover every configured source, the policy
        // can never be Valid under that uncertainty — treat the gap as a degraded read (Unavailable) so an omitted
        // source can never be silently ignored into a Valid verdict (AC3; AD-12).
        if (result.Sources.Count != policy.Sources.Count)
        {
            return ApproverPolicyValidationStatus.Unavailable;
        }

        bool anyUnavailable = false;
        bool anyAmbiguous = false;
        bool anyDisabled = false;
        bool anyMissing = false;

        foreach (ApproverSourceResolution source in result.Sources)
        {
            switch (source.Outcome)
            {
                case ApproverSourceOutcome.Unauthorized:
                    // A cross-tenant / unauthorized source dominates and never leaks or permits another tenant (AC3).
                    return ApproverPolicyValidationStatus.Unauthorized;
                case ApproverSourceOutcome.Unavailable:
                case ApproverSourceOutcome.Unknown:
                    anyUnavailable = true;
                    break;
                case ApproverSourceOutcome.Ambiguous:
                    anyAmbiguous = true;
                    break;
                case ApproverSourceOutcome.Disabled:
                    anyDisabled = true;
                    break;
                case ApproverSourceOutcome.Missing:
                    anyMissing = true;
                    break;
                case ApproverSourceOutcome.Resolved:
                default:
                    break;
            }
        }

        if (anyUnavailable)
        {
            return ApproverPolicyValidationStatus.Unavailable;
        }

        if (anyAmbiguous)
        {
            return ApproverPolicyValidationStatus.Ambiguous;
        }

        if (anyDisabled)
        {
            return ApproverPolicyValidationStatus.Disabled;
        }

        return anyMissing
            ? ApproverPolicyValidationStatus.Missing
            : ApproverPolicyValidationStatus.Valid;
    }
}
