using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.UI.Components.Shared;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC4 — the proposal-transition ARIA live region. The six ordinary lifecycle transitions (generation failed, proposal
/// created, proposal expired, approval posted, posting failed, permission denied) announce <b>politely</b>
/// (<c>role="status"</c>/<c>aria-live="polite"</c>); only the destructive-action-prevention transition
/// (<see cref="ProposalTransitionKind.StaleApprovalBlocked"/>) announces <b>assertively</b> (<c>role="alert"</c>);
/// ordinary pending progress (<see cref="ProposalTransitionKind.None"/>) keeps the region silent; and every announced
/// string is a whole localized key only — never generated content (AD-14).
/// </summary>
public sealed class ProposalTransitionAnnouncerTests : AgentsTestContext
{
    // A content sample that must NEVER reach the live region (AD-14).
    private const string ContentSentinel = "top-secret-proposed-content-7f3a";

    [Theory]
    [InlineData(ProposalTransitionKind.GenerationFailed)]
    [InlineData(ProposalTransitionKind.ProposalCreated)]
    [InlineData(ProposalTransitionKind.ProposalExpired)]
    [InlineData(ProposalTransitionKind.ApprovalPosted)]
    [InlineData(ProposalTransitionKind.PostingFailed)]
    [InlineData(ProposalTransitionKind.PermissionDenied)]
    public void Ordinary_transitions_announce_politely_with_their_message(ProposalTransitionKind kind)
    {
        IRenderedComponent<ProposalTransitionAnnouncer> cut = RenderAnnouncer(kind);

        IElement region = cut.Find("[data-testid='transition']");
        region.GetAttribute("role").ShouldBe("status");
        region.GetAttribute("aria-live").ShouldBe("polite");
        region.GetAttribute("aria-atomic").ShouldBe("true");
        cut.Find("[data-testid='transition-message']").TextContent.Trim()
            .ShouldBe(ProposalTransitionPresentation.LabelKeyFor(kind));
    }

    [Fact]
    public void Stale_approval_block_announces_assertively()
    {
        // The only assertive announcement — immediate destructive-action prevention (review-accessibility matrix).
        IRenderedComponent<ProposalTransitionAnnouncer> cut = RenderAnnouncer(ProposalTransitionKind.StaleApprovalBlocked);

        IElement region = cut.Find("[data-testid='transition']");
        region.GetAttribute("role").ShouldBe("alert");
        region.GetAttribute("aria-live").ShouldBe("assertive");
        cut.Find("[data-testid='transition-message']");
    }

    [Fact]
    public void None_keeps_the_live_region_present_but_silent()
    {
        // Ordinary pending progress must not produce a disruptive announcement: the region exists (so a later
        // transition is announced) but carries no message and stays polite (AC4).
        IRenderedComponent<ProposalTransitionAnnouncer> cut = RenderAnnouncer(ProposalTransitionKind.None);

        IElement region = cut.Find("[data-testid='transition']");
        region.GetAttribute("role").ShouldBe("status");
        region.GetAttribute("aria-live").ShouldBe("polite");
        cut.FindAll("[data-testid='transition-message']").ShouldBeEmpty();
    }

    [Fact]
    public void The_announced_text_is_a_whole_localized_string_and_never_carries_content()
    {
        // The announcer takes only a content-free Kind, so it is structurally safe; guard the rendering anyway — no
        // live-region text or accessible name may carry a content-looking value (AD-14).
        IRenderedComponent<ProposalTransitionAnnouncer> cut = RenderAnnouncer(ProposalTransitionKind.ApprovalPosted);

        foreach (IElement liveRegion in cut.FindAll("[aria-live]"))
        {
            liveRegion.TextContent.ShouldNotContain(ContentSentinel);
        }

        cut.Find("[data-testid='transition-message']").TextContent.ShouldStartWith("Agents.ProposalDetail.Transition.");
    }

    private IRenderedComponent<ProposalTransitionAnnouncer> RenderAnnouncer(ProposalTransitionKind kind)
        => Render<ProposalTransitionAnnouncer>(parameters => parameters
            .Add(announcer => announcer.Kind, kind)
            .Add(announcer => announcer.TestId, "transition"));
}
