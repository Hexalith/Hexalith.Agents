using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>Queryable governance readiness for Agents audit evidence.</summary>
/// <param name="ActiveTreatment">The audit treatment currently allowed by governance.</param>
/// <param name="BlockedTreatment">The content-bearing treatment blocked until governance exists.</param>
/// <param name="Blockers">Named launch-readiness blockers.</param>
public sealed record AgentAuditGovernanceReadiness(
    ContentSafetyAuditTreatment ActiveTreatment,
    ContentSafetyAuditTreatment BlockedTreatment,
    IReadOnlyList<string> Blockers)
{
    /// <summary>The named launch-readiness blocker for content-bearing Agents audit.</summary>
    public const string RetentionLegalHoldExportDeletionPolicyUnresolved =
        "Agents audit retention / legal-hold / export / deletion policy unresolved";

    /// <summary>The current metadata-only readiness state.</summary>
    public static AgentAuditGovernanceReadiness MetadataOnlyBlocked { get; } = new(
        ContentSafetyAuditTreatment.MetadataOnly,
        ContentSafetyAuditTreatment.RedactedExcerpt,
        [RetentionLegalHoldExportDeletionPolicyUnresolved]);
}
