using System;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1 — the pure proposal-state presentation mapping. Every switch is total over <see cref="ProposedAgentReplyState"/>
/// so the reserved 3.3–3.6 states render through a safe default (never throw, never a Success/Brand colour); each state
/// has a non-null curated icon and a whole-string label key; and the coarse "age" bucket helper is deterministic given a
/// fixed <c>now</c> (no <see cref="DateTimeOffset.UtcNow"/> in the component path — avoids flaky tests).
/// </summary>
public sealed class ProposedAgentReplyStatePresentationTests
{
    private static readonly DateTimeOffset _now = new(2026, 6, 24, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(ProposedAgentReplyState.Pending, BadgeColor.Informative)]
    [InlineData(ProposedAgentReplyState.Edited, BadgeColor.Informative)]
    [InlineData(ProposedAgentReplyState.Regenerated, BadgeColor.Informative)]
    [InlineData(ProposedAgentReplyState.Approved, BadgeColor.Important)]
    [InlineData(ProposedAgentReplyState.PostingPending, BadgeColor.Informative)]
    [InlineData(ProposedAgentReplyState.Posted, BadgeColor.Success)]
    [InlineData(ProposedAgentReplyState.PostingFailed, BadgeColor.Danger)]
    // Story 3.6 terminal states (DESIGN #Colors): rejected = Danger, abandoned = Subtle, expired = Severe.
    [InlineData(ProposedAgentReplyState.Rejected, BadgeColor.Danger)]
    [InlineData(ProposedAgentReplyState.Abandoned, BadgeColor.Subtle)]
    [InlineData(ProposedAgentReplyState.Expired, BadgeColor.Severe)]
    [InlineData(ProposedAgentReplyState.Unknown, BadgeColor.Subtle)]
    public void ColorFor_binds_each_shipped_state_to_its_role(ProposedAgentReplyState state, BadgeColor expected)
        => ProposedAgentReplyStatePresentation.ColorFor(state).ShouldBe(expected);

    [Theory]
    // Story 3.6 terminal states bind to a dedicated curated glyph (reject/abandon = "removed", expired = "warning"),
    // never the QuestionCircle total default that the Unknown sentinel and unmapped reserved states render through.
    [InlineData(ProposedAgentReplyState.Rejected, "SubtractCircle")]
    [InlineData(ProposedAgentReplyState.Abandoned, "SubtractCircle")]
    [InlineData(ProposedAgentReplyState.Expired, "Warning")]
    public void IconFor_binds_each_terminal_state_to_a_dedicated_curated_glyph(ProposedAgentReplyState state, string expectedIconName)
    {
        ProposedAgentReplyStatePresentation.IconFor(state).Name.ShouldBe(expectedIconName);
        ProposedAgentReplyStatePresentation.IconFor(state).Name
            .ShouldNotBe(ProposedAgentReplyStatePresentation.IconFor((ProposedAgentReplyState)999).Name);
    }

    [Fact]
    public void ColorFor_is_total_and_uses_posted_success_only_for_posted()
    {
        foreach (ProposedAgentReplyState state in Enum.GetValues<ProposedAgentReplyState>())
        {
            BadgeColor color = ProposedAgentReplyStatePresentation.ColorFor(state);
            if (state != ProposedAgentReplyState.Posted)
            {
                color.ShouldNotBe(BadgeColor.Success);
            }

            color.ShouldNotBe(BadgeColor.Brand);
        }

        // A reserved value not yet defined in the enum still maps through the total default (mirrors MapStatus totality).
        ProposedAgentReplyStatePresentation.ColorFor((ProposedAgentReplyState)999).ShouldBe(BadgeColor.Subtle);
    }

    [Fact]
    public void IconFor_is_total_and_non_null_over_every_state()
    {
        foreach (ProposedAgentReplyState state in Enum.GetValues<ProposedAgentReplyState>())
        {
            ProposedAgentReplyStatePresentation.IconFor(state).ShouldNotBeNull();
        }

        ProposedAgentReplyStatePresentation.IconFor((ProposedAgentReplyState)999).ShouldNotBeNull();
    }

    [Fact]
    public void LabelKeyFor_is_a_whole_string_key_for_every_state()
    {
        foreach (ProposedAgentReplyState state in Enum.GetValues<ProposedAgentReplyState>())
        {
            ProposedAgentReplyStatePresentation.LabelKeyFor(state).ShouldBe($"Agents.ProposalState.Label.{state}");
        }
    }

    [Theory]
    [InlineData(null, "Agents.ProposalQueue.Age.Unknown")]
    [InlineData("not-a-timestamp", "Agents.ProposalQueue.Age.Unknown")]
    public void Age_helper_returns_the_unknown_bucket_for_a_missing_or_unparseable_timestamp(string? createdAt, string expected)
        => ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(createdAt, _now).ShouldBe(expected);

    [Fact]
    public void Age_helper_is_deterministic_and_bucketed_given_a_fixed_now()
    {
        ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddMinutes(-30)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.LessThanHour");
        ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddHours(-5)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.Today");
        ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddDays(-3)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.ThisWeek");
        ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddDays(-30)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.Older");
    }

    [Fact]
    public void Age_helper_clamps_a_future_timestamp_to_the_freshest_bucket()
        => ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddHours(1)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.LessThanHour");

    [Fact]
    public void Age_helper_uses_inclusive_lower_bounds_at_each_bucket_boundary()
    {
        // Each bucket boundary belongs to the NEXT (older) bucket: an age of exactly 1h/1d/7d is no longer "<1h"/"<1d"/"<7d".
        ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddHours(-1)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.Today");
        ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddDays(-1)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.ThisWeek");
        ProposedAgentReplyStatePresentation.AgeLabelKeyOrText(Iso(_now.AddDays(-7)), _now)
            .ShouldBe("Agents.ProposalQueue.Age.Older");
    }

    private static string Iso(DateTimeOffset value) => value.ToString("O");
}
