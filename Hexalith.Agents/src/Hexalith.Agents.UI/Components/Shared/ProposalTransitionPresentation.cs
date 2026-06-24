namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// Pure, dependency-free mapping from a <see cref="ProposalTransitionKind"/> to its whole-string localization key and
/// its live-region politeness (AC4; review-accessibility live-region matrix). It derives only the coarse, content-free
/// classification and never exposes any generated/edited content, provider payload, stack trace, secret, id, or PII
/// (AD-14). Mirrors <see cref="ProposedAgentReplyStatePresentation"/> / <see cref="AgentGenerationKindPresentation"/> and
/// is unit-testable in isolation (no bUnit). Both switches are <b>total</b> so any future transition renders through a
/// safe polite default until its owning story adds an explicit mapping.
/// </summary>
public static class ProposalTransitionPresentation
{
    /// <summary>The whole-string localization key for a transition announcement (UX-DR14).</summary>
    /// <param name="kind">The safe transition kind.</param>
    /// <returns>The resource key.</returns>
    public static string LabelKeyFor(ProposalTransitionKind kind)
        => kind switch
        {
            ProposalTransitionKind.GenerationFailed => "Agents.ProposalDetail.Transition.GenerationFailed",
            ProposalTransitionKind.ProposalCreated => "Agents.ProposalDetail.Transition.ProposalCreated",
            ProposalTransitionKind.ProposalExpired => "Agents.ProposalDetail.Transition.ProposalExpired",
            ProposalTransitionKind.ApprovalPosted => "Agents.ProposalDetail.Transition.ApprovalPosted",
            ProposalTransitionKind.PostingFailed => "Agents.ProposalDetail.Transition.PostingFailed",
            ProposalTransitionKind.PermissionDenied => "Agents.ProposalDetail.Transition.PermissionDenied",
            ProposalTransitionKind.StaleApprovalBlocked => "Agents.ProposalDetail.Transition.StaleApprovalBlocked",
            _ => "Agents.ProposalDetail.Transition.None",
        };

    /// <summary>
    /// Whether the transition announces assertively (<c>role="alert"</c>/<c>aria-live="assertive"</c>). Only
    /// <see cref="ProposalTransitionKind.StaleApprovalBlocked"/> — immediate destructive-action prevention — is
    /// assertive; every ordinary status transition (created/posted/failed/expired/denied) is polite, and ordinary
    /// pending progress never announces (AC4; review-accessibility matrix).
    /// </summary>
    /// <param name="kind">The safe transition kind.</param>
    /// <returns><see langword="true"/> when the transition must announce assertively.</returns>
    public static bool IsAssertive(ProposalTransitionKind kind)
        => kind is ProposalTransitionKind.StaleApprovalBlocked;
}
