using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Safe UI status for an audit-evidence read (Story 4.3 AC2; AD-12). Mirrors <see cref="ProposalDetailInspectionStatus"/>.</summary>
public enum AuditEvidenceInspectionStatus
{
    /// <summary>Unknown sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The inspection succeeded; the safe evidence is present.</summary>
    Success,

    /// <summary>The caller is not authorized; no evidence is returned (AD-12).</summary>
    NotAuthorized,

    /// <summary>A dependency/projection is unreachable; no evidence is returned (the UI <c>Unavailable</c> surface).</summary>
    Unavailable,

    /// <summary>No interaction exists for the requested aggregate; no evidence is returned (never reveals cross-tenant existence; AD-12).</summary>
    NotFound,
}

/// <summary>
/// Structured result of an authorized audit-evidence read (Story 4.3 AC1, AC2, AC3; AD-12, AD-14). It bundles the
/// existing Story 4.2 support-safe evidence views — the request-time <see cref="Detail"/> snapshot (caller, Agent,
/// Source Conversation, provider/model, response mode, version metadata, timestamps) and the <see cref="Approval"/>
/// approval/posting evidence (approver, approval/posting outcome, final Conversation Message reference, policy verdict)
/// — with the canonical <see cref="Availability"/> and the named <see cref="Governance"/> launch-readiness blockers. On
/// every non-success outcome the <see cref="Detail"/>/<see cref="Approval"/> views are <see langword="null"/>, so a
/// denied/faulted read never reveals whether the interaction exists in another tenant (AD-12). The bundled views carry
/// ONLY safe ids/enums/version-numbers/timestamps — never any prompt/generated/edited/context content, provider secret,
/// raw payload, or stack trace (AD-9, AD-14). <see cref="Governance"/> is a public launch-readiness fact (not a record),
/// surfaced even on a fail-closed read so Story 4.4 can consume the named blocker.
/// </summary>
/// <param name="Status">The read outcome.</param>
/// <param name="Detail">The safe request-time proposal/interaction snapshot (non-null only on <see cref="AuditEvidenceInspectionStatus.Success"/>).</param>
/// <param name="Approval">The safe approval/posting evidence (non-null only on <see cref="AuditEvidenceInspectionStatus.Success"/> and when an approval was recorded).</param>
/// <param name="Availability">The canonical audit-evidence availability state (never rendered as success unless <see cref="AuditAvailabilityStatus.AuditAvailable"/>; AD-5).</param>
/// <param name="Governance">The metadata-only audit governance readiness and its named launch-readiness blockers.</param>
public sealed record AuditEvidenceResult(
    AuditEvidenceInspectionStatus Status,
    ProposalDetailView? Detail,
    AgentProposalApprovalEvidenceView? Approval,
    AuditAvailabilityStatus Availability,
    AgentAuditGovernanceReadiness Governance)
{
    /// <summary>Creates a successful read result carrying the given safe evidence views, availability, and governance readiness.</summary>
    /// <param name="detail">The safe request-time snapshot view.</param>
    /// <param name="approval">The safe approval/posting evidence view, or <see langword="null"/> when no approval is recorded.</param>
    /// <param name="availability">The canonical audit availability state.</param>
    /// <param name="governance">The audit governance readiness (defaults to the metadata-only blocked state).</param>
    /// <returns>A success result.</returns>
    public static AuditEvidenceResult Success(
        ProposalDetailView detail,
        AgentProposalApprovalEvidenceView? approval,
        AuditAvailabilityStatus availability,
        AgentAuditGovernanceReadiness? governance = null)
        => new(AuditEvidenceInspectionStatus.Success, detail, approval, availability, governance ?? AgentAuditGovernanceReadiness.MetadataOnlyBlocked);

    /// <summary>Creates a fail-closed not-authorized result with no evidence (discloses nothing; AD-12). Governance stays the public metadata-only blocker.</summary>
    /// <returns>A not-authorized result.</returns>
    public static AuditEvidenceResult NotAuthorized()
        => new(AuditEvidenceInspectionStatus.NotAuthorized, null, null, AuditAvailabilityStatus.Unknown, AgentAuditGovernanceReadiness.MetadataOnlyBlocked);

    /// <summary>Creates a dependency-unreachable result with no evidence (the UI <c>Unavailable</c> surface).</summary>
    /// <returns>An unavailable result.</returns>
    public static AuditEvidenceResult Unavailable()
        => new(AuditEvidenceInspectionStatus.Unavailable, null, null, AuditAvailabilityStatus.AuditUnavailable, AgentAuditGovernanceReadiness.MetadataOnlyBlocked);

    /// <summary>Creates a not-found result with no evidence (never reveals cross-tenant existence; AD-12).</summary>
    /// <returns>A not-found result.</returns>
    public static AuditEvidenceResult NotFound()
        => new(AuditEvidenceInspectionStatus.NotFound, null, null, AuditAvailabilityStatus.Unknown, AgentAuditGovernanceReadiness.MetadataOnlyBlocked);
}
