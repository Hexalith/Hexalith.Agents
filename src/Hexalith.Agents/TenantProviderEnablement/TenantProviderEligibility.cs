using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.ProviderCatalog;

namespace Hexalith.Agents.TenantProviderEnablement;

/// <summary>Evaluates tenant terms against the complete platform terms history.</summary>
public static class TenantProviderEligibility
{
    /// <summary>Returns the safe eligibility status and exclusive grace deadline. Each pending version must
    /// cumulatively tighten the accepted terms and declare its change from the immediately preceding version.</summary>
    /// <param name="tenant">The tenant enablement and last decision.</param>
    /// <param name="history">The complete platform terms history.</param>
    /// <param name="evaluatedAt">The trusted instant at which eligibility is evaluated.</param>
    /// <returns>The eligibility status and any exclusive grace deadline.</returns>
    public static TenantProviderEligibilityResult Evaluate(
        TenantProviderEntryState? tenant,
        IReadOnlyList<ProviderDataHandlingRecord>? history,
        DateTimeOffset evaluatedAt)
    {
        if (tenant is not { Enabled: true } || history is null || history.Count == 0)
        {
            return new("MissingEnablementOrTerms", null);
        }

        ProviderDataHandlingRecord current = history[^1];
        if (tenant?.DeclinedVersion == current.DataHandlingVersion)
        {
            return new("Declined", null);
        }

        if (tenant?.DeclinedVersion is not null)
        {
            return new("AcceptanceRequired", null);
        }

        ProviderDataHandlingRecord? accepted = tenant?.AcceptedTerms;
        if (accepted is null)
        {
            return new("AcceptanceRequired", null);
        }

        if (current.DataHandlingVersion == accepted.DataHandlingVersion)
        {
            return ProviderDataHandlingPolicy.SameFields(current, accepted)
                ? new("Current", null)
                : new("UnknownEvidence", null);
        }

        if (current.DataHandlingVersion < accepted.DataHandlingVersion)
        {
            return new("UnknownEvidence", null);
        }

        ProviderDataHandlingRecord[] pending = [.. history
            .Where(item => item.DataHandlingVersion > accepted.DataHandlingVersion)
            .OrderBy(item => item.DataHandlingVersion)];
        if (pending.Length == 0 || pending[0].DataHandlingVersion != accepted.DataHandlingVersion + 1
            || pending[^1].DataHandlingVersion != current.DataHandlingVersion)
        {
            return new("UnknownEvidence", null);
        }

        ProviderDataHandlingRecord previous = accepted;
        for (int index = 0; index < pending.Length; index++)
        {
            if (pending[index].DataHandlingVersion != accepted.DataHandlingVersion + index + 1
                || pending[index].EffectiveAt is null
                || !ProviderDataHandlingPolicy.IsCumulativeTightening(accepted, pending[index])
                || !ProviderDataHandlingPolicy.HasRecordedTightening(previous, pending[index]))
            {
                return new("TermsChanged", null);
            }

            previous = pending[index];
        }

        DateTimeOffset deadline = pending[0].EffectiveAt!.Value.AddDays(30);
        return evaluatedAt < deadline
            ? new("Grace", deadline)
            : new("GraceExpired", deadline);
    }
}
