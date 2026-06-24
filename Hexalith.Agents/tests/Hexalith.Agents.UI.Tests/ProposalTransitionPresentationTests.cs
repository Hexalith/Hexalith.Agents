using System;
using System.Linq;

using Hexalith.Agents.UI.Components.Shared;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC4 — the pure proposal-transition politeness/label mapping (review-accessibility live-region matrix). Every
/// transition maps to a whole-string announcement key (never a content fragment — AD-14); the six ordinary status
/// transitions are <b>polite</b> and only the destructive-action-prevention transition (<see
/// cref="ProposalTransitionKind.StaleApprovalBlocked"/>) is <b>assertive</b>; the switch is total so a future kind
/// renders through a safe polite default and never throws.
/// </summary>
public sealed class ProposalTransitionPresentationTests
{
    [Theory]
    [InlineData(ProposalTransitionKind.None, "Agents.ProposalDetail.Transition.None")]
    [InlineData(ProposalTransitionKind.GenerationFailed, "Agents.ProposalDetail.Transition.GenerationFailed")]
    [InlineData(ProposalTransitionKind.ProposalCreated, "Agents.ProposalDetail.Transition.ProposalCreated")]
    [InlineData(ProposalTransitionKind.ProposalExpired, "Agents.ProposalDetail.Transition.ProposalExpired")]
    [InlineData(ProposalTransitionKind.ApprovalPosted, "Agents.ProposalDetail.Transition.ApprovalPosted")]
    [InlineData(ProposalTransitionKind.PostingFailed, "Agents.ProposalDetail.Transition.PostingFailed")]
    [InlineData(ProposalTransitionKind.PermissionDenied, "Agents.ProposalDetail.Transition.PermissionDenied")]
    [InlineData(ProposalTransitionKind.StaleApprovalBlocked, "Agents.ProposalDetail.Transition.StaleApprovalBlocked")]
    public void LabelKeyFor_maps_each_transition_to_its_whole_string_key(ProposalTransitionKind kind, string expected)
        => ProposalTransitionPresentation.LabelKeyFor(kind).ShouldBe(expected);

    [Fact]
    public void LabelKeyFor_is_total_and_never_throws_over_every_transition()
    {
        foreach (ProposalTransitionKind kind in Enum.GetValues<ProposalTransitionKind>())
        {
            ProposalTransitionPresentation.LabelKeyFor(kind).ShouldNotBeNullOrWhiteSpace();
        }

        // A reserved value not yet defined still maps through the total polite default.
        ProposalTransitionPresentation.LabelKeyFor((ProposalTransitionKind)999).ShouldBe("Agents.ProposalDetail.Transition.None");
    }

    [Fact]
    public void Every_announced_transition_has_a_distinct_key()
    {
        string[] keys = Enum.GetValues<ProposalTransitionKind>()
            .Select(ProposalTransitionPresentation.LabelKeyFor)
            .ToArray();

        keys.Distinct().Count().ShouldBe(keys.Length);
    }

    [Theory]
    [InlineData(ProposalTransitionKind.None)]
    [InlineData(ProposalTransitionKind.GenerationFailed)]
    [InlineData(ProposalTransitionKind.ProposalCreated)]
    [InlineData(ProposalTransitionKind.ProposalExpired)]
    [InlineData(ProposalTransitionKind.ApprovalPosted)]
    [InlineData(ProposalTransitionKind.PostingFailed)]
    [InlineData(ProposalTransitionKind.PermissionDenied)]
    public void Ordinary_status_transitions_are_polite(ProposalTransitionKind kind)
        => ProposalTransitionPresentation.IsAssertive(kind).ShouldBeFalse();

    [Fact]
    public void Only_the_stale_approval_block_announces_assertively()
    {
        // The whole matrix: exactly one transition — immediate destructive-action prevention — is assertive.
        ProposalTransitionPresentation.IsAssertive(ProposalTransitionKind.StaleApprovalBlocked).ShouldBeTrue();

        ProposalTransitionKind[] assertive = Enum.GetValues<ProposalTransitionKind>()
            .Where(ProposalTransitionPresentation.IsAssertive)
            .ToArray();

        assertive.ShouldBe([ProposalTransitionKind.StaleApprovalBlocked]);
    }
}
