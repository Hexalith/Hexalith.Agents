namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// The important Proposed-Agent-Reply lifecycle transitions the Story 3.7 detail workspace announces through an ARIA
/// live region (AC4; UX-DR33, UX-DR36; review-accessibility live-region matrix). Each value maps to a whole localized
/// announcement string and a politeness level: the six ordinary status transitions announce <b>politely</b>; the
/// single destructive-action-prevention transition (<see cref="StaleApprovalBlocked"/>) announces <b>assertively</b>.
/// Ordinary pending progress has <see cref="None"/> so it never produces a disruptive assertive announcement (AC4).
/// </summary>
public enum ProposalTransitionKind
{
    /// <summary>No transition to announce (ordinary pending progress) — the live region stays silent (AC4).</summary>
    None = 0,

    /// <summary>Generation failed closed — announced politely (AC4).</summary>
    GenerationFailed,

    /// <summary>A Proposed Agent Reply was created and awaits action — announced politely (AC4).</summary>
    ProposalCreated,

    /// <summary>The proposal expiry elapsed — announced politely (AC4).</summary>
    ProposalExpired,

    /// <summary>The approved version was posted to the Conversation — announced politely (AC4).</summary>
    ApprovalPosted,

    /// <summary>Posting the approved version failed closed — announced politely, distinct from approval failure (AC4).</summary>
    PostingFailed,

    /// <summary>Permission was denied — announced politely (no record fingerprinting; AC4, AD-12).</summary>
    PermissionDenied,

    /// <summary>
    /// A newer version appeared after the Approver selected an older one, so approval of the stale selection is blocked
    /// and re-prompted — the ONLY assertive announcement, reserved for immediate destructive-action prevention
    /// (review-accessibility matrix; review-governance selected-version lock).
    /// </summary>
    StaleApprovalBlocked,
}
