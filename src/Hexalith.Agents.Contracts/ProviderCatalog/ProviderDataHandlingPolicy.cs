using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Pure validation and comparison for governed provider terms.</summary>
public static class ProviderDataHandlingPolicy
{
    private static readonly Regex _reference = new("^[A-Za-z0-9._:-]+$", RegexOptions.Compiled);
    private static readonly Regex _region = new("^[A-Za-z0-9-]{2,32}$", RegexOptions.Compiled);

    /// <summary>Returns a safe validation reason, or null for valid terms.</summary>
    public static string? Validate(ProviderDataHandlingRecord? proposed, ProviderDataHandlingRecord? current, bool allowHistoricalVersion = false)
    {
        if (proposed is null)
        {
            return null;
        }

        if (proposed.RetentionDays < 0 || proposed.DataHandlingVersion < 0)
        {
            return "Invalid data handling retention or version.";
        }

        if (string.IsNullOrWhiteSpace(proposed.TermsReferenceId)
            || proposed.TermsReferenceId.Length > 128
            || !_reference.IsMatch(proposed.TermsReferenceId))
        {
            return "Invalid data handling terms reference.";
        }

        if (proposed.ProcessingRegions is null || proposed.ProcessingRegions.Count == 0
            || proposed.ProcessingRegions.Any(region => string.IsNullOrWhiteSpace(region) || !_region.IsMatch(region))
            || proposed.ProcessingRegions.Distinct(StringComparer.OrdinalIgnoreCase).Count() != proposed.ProcessingRegions.Count)
        {
            return "Invalid data handling processing regions.";
        }

        bool changed = current is null || !SameFields(proposed, current);
        if (changed && current?.DataHandlingVersion == int.MaxValue)
        {
            return "Data handling version is exhausted.";
        }

        int required = current is null ? 1 : changed ? current.DataHandlingVersion + 1 : current.DataHandlingVersion;
        return proposed.DataHandlingVersion is 0 || proposed.DataHandlingVersion == required
            || allowHistoricalVersion && current is null && proposed.DataHandlingVersion > 0
            ? null
            : "Data handling version must advance only when governed terms change.";
    }

    /// <summary>Assigns the next non-reusable version and canonicalizes region names.</summary>
    public static ProviderDataHandlingRecord? Assign(ProviderDataHandlingRecord? proposed, ProviderDataHandlingRecord? current)
    {
        if (proposed is null)
        {
            return current;
        }

        if (current is not null && SameFields(proposed, current))
        {
            return current;
        }

        if (current?.DataHandlingVersion == int.MaxValue)
        {
            throw new InvalidOperationException("Data handling version is exhausted.");
        }

        int version = current is null
            ? proposed.DataHandlingVersion > 0 ? proposed.DataHandlingVersion : 1
            : current.DataHandlingVersion + 1;
        return proposed with
        {
            DataHandlingVersion = version,
            ProcessingRegions = proposed.ProcessingRegions.Select(region => region.ToUpperInvariant())
                .Order(StringComparer.Ordinal).ToArray(),
        };
    }

    /// <summary>Returns whether the governed fields match, independent of region ordering and version.</summary>
    public static bool SameFields(ProviderDataHandlingRecord left, ProviderDataHandlingRecord right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.RetentionDays == right.RetentionDays
            && left.AllowsTrainingUse == right.AllowsTrainingUse
            && string.Equals(left.TermsReferenceId, right.TermsReferenceId, StringComparison.Ordinal)
            && left.ProcessingRegions is not null && right.ProcessingRegions is not null
            && left.ProcessingRegions.Order(StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(right.ProcessingRegions.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Compares every submitted term field, including effective time and tightening declaration.</summary>
    public static bool SameSnapshot(ProviderDataHandlingRecord left, ProviderDataHandlingRecord right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return JsonSerializer.SerializeToUtf8Bytes(left).AsSpan()
            .SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(right));
    }

    /// <summary>Returns whether current terms cumulatively tighten the last accepted terms.</summary>
    public static bool IsCumulativeTightening(ProviderDataHandlingRecord accepted, ProviderDataHandlingRecord current)
    {
        ArgumentNullException.ThrowIfNull(accepted);
        ArgumentNullException.ThrowIfNull(current);
        if (accepted.DataHandlingVersion >= current.DataHandlingVersion
            || accepted.ProcessingRegions is null || current.ProcessingRegions is null
            || !string.Equals(accepted.TermsReferenceId, current.TermsReferenceId, StringComparison.Ordinal)
            || current.RetentionDays > accepted.RetentionDays
            || (current.AllowsTrainingUse && !accepted.AllowsTrainingUse))
        {
            return false;
        }

        var acceptedRegions = new HashSet<string>(accepted.ProcessingRegions, StringComparer.OrdinalIgnoreCase);
        return current.ProcessingRegions.All(acceptedRegions.Contains)
            && (current.RetentionDays < accepted.RetentionDays
                || (accepted.AllowsTrainingUse && !current.AllowsTrainingUse)
                || current.ProcessingRegions.Count < accepted.ProcessingRegions.Count);
    }

    /// <summary>Records the Platform Operator's field-level declaration for a qualifying change.</summary>
    public static ProviderDataHandlingTighteningDeclaration? DeclareTightening(
        ProviderDataHandlingRecord previous,
        ProviderDataHandlingRecord current,
        string actorUserId)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        if (!IsCumulativeTightening(previous, current)
            || string.IsNullOrWhiteSpace(actorUserId)
            || current.EffectiveAt is not { } effectiveAt)
        {
            return null;
        }

        return new(previous.DataHandlingVersion, current.DataHandlingVersion,
            Diff(previous, current), actorUserId, "Agents.PlatformOperator", effectiveAt);
    }

    /// <summary>Checks that the declaration describes this exact adjacent change.</summary>
    public static bool HasRecordedTightening(ProviderDataHandlingRecord previous, ProviderDataHandlingRecord current)
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(current);
        ProviderDataHandlingTighteningDeclaration? declared = current.TighteningDeclaration;
        if (declared is null || declared.FromVersion != previous.DataHandlingVersion
            || declared.ToVersion != current.DataHandlingVersion
            || string.IsNullOrWhiteSpace(declared.ActorUserId)
            || !string.Equals(declared.RoleBasis, "Agents.PlatformOperator", StringComparison.Ordinal)
            || current.EffectiveAt is not { } effectiveAt || declared.DeclaredAt != effectiveAt
            || !IsCumulativeTightening(previous, current))
        {
            return false;
        }

        ProviderDataHandlingFieldDiff actual = Diff(previous, current);
        ProviderDataHandlingFieldDiff supplied = declared.FieldDiff;
        return supplied is not null
            && supplied.RemovedProcessingRegions is not null
            && supplied.AddedProcessingRegions is not null
            && supplied.PreviousRetentionDays == actual.PreviousRetentionDays
            && supplied.NewRetentionDays == actual.NewRetentionDays
            && supplied.PreviousAllowsTrainingUse == actual.PreviousAllowsTrainingUse
            && supplied.NewAllowsTrainingUse == actual.NewAllowsTrainingUse
            && string.Equals(supplied.PreviousTermsReferenceId, actual.PreviousTermsReferenceId, StringComparison.Ordinal)
            && string.Equals(supplied.NewTermsReferenceId, actual.NewTermsReferenceId, StringComparison.Ordinal)
            && supplied.RemovedProcessingRegions.SequenceEqual(actual.RemovedProcessingRegions, StringComparer.Ordinal)
            && supplied.AddedProcessingRegions.SequenceEqual(actual.AddedProcessingRegions, StringComparer.Ordinal);
    }

    private static ProviderDataHandlingFieldDiff Diff(ProviderDataHandlingRecord previous, ProviderDataHandlingRecord current)
    {
        string[] removed = [.. previous.ProcessingRegions.Except(current.ProcessingRegions, StringComparer.OrdinalIgnoreCase)
            .Select(region => region.ToUpperInvariant()).Order(StringComparer.Ordinal)];
        string[] added = [.. current.ProcessingRegions.Except(previous.ProcessingRegions, StringComparer.OrdinalIgnoreCase)
            .Select(region => region.ToUpperInvariant()).Order(StringComparer.Ordinal)];
        return new(previous.RetentionDays, current.RetentionDays,
            previous.AllowsTrainingUse, current.AllowsTrainingUse,
            removed, added, previous.TermsReferenceId, current.TermsReferenceId);
    }
}
