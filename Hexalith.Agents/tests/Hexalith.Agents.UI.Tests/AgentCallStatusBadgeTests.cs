using System;
using System.Text.RegularExpressions;

using Bunit;

using Hexalith.Agents.UI.Components.Shared;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC2/AC4 — the call-status badge renders color + icon + visible text together (never color-only), binds to a Fluent
/// semantic role (not a hex value), and resolves a single whole string through the localizer (visible text ==
/// accessible name == the key under the stub localizer). Mirrors <see cref="BadgeConformanceTests"/>.
/// </summary>
public sealed class AgentCallStatusBadgeTests : AgentsTestContext
{
    private static readonly Regex _sixDigitHex = new("#[0-9a-fA-F]{6}", RegexOptions.Compiled);

    [Theory]
    [InlineData(AgentCallStatus.Requested)]
    [InlineData(AgentCallStatus.Authorized)]
    [InlineData(AgentCallStatus.Denied)]
    [InlineData(AgentCallStatus.Blocked)]
    [InlineData(AgentCallStatus.ContextLoading)]
    [InlineData(AgentCallStatus.ContextBlocked)]
    [InlineData(AgentCallStatus.Generating)]
    [InlineData(AgentCallStatus.GenerationFailed)]
    [InlineData(AgentCallStatus.SafetyFailed)]
    [InlineData(AgentCallStatus.Generated)]
    [InlineData(AgentCallStatus.PostingPending)]
    [InlineData(AgentCallStatus.Posted)]
    [InlineData(AgentCallStatus.PostingFailed)]
    [InlineData(AgentCallStatus.Unknown)]
    public void Call_status_badge_renders_color_icon_and_localized_whole_string(AgentCallStatus state)
    {
        IRenderedComponent<AgentCallStatusBadge> cut = Render<AgentCallStatusBadge>(
            parameters => parameters.Add(badge => badge.State, state));

        FluentBadge badge = cut.FindComponent<FluentBadge>().Instance;
        badge.Color.ShouldBe(AgentCallStatusPresentation.ColorFor(state));
        badge.IconStart.ShouldNotBeNull();

        string expectedKey = AgentCallStatusPresentation.LabelKeyFor(state);
        cut.VisibleText().Trim().ShouldBe(expectedKey);
        cut.Find("[data-testid='agent-call-status-badge']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='agent-call-status-badge']").GetAttribute("aria-label").ShouldBe(expectedKey);
        _sixDigitHex.IsMatch(cut.Markup).ShouldBeFalse("status badges bind to Fluent roles, never inline hex (AC2/UX-DR12)");
    }

    [Fact]
    public void Call_status_badge_uses_success_only_for_posted()
    {
        foreach (AgentCallStatus state in Enum.GetValues<AgentCallStatus>())
        {
            BadgeColor color = AgentCallStatusPresentation.ColorFor(state);
            if (state != AgentCallStatus.Posted)
            {
                color.ShouldNotBe(BadgeColor.Success);
            }
        }
    }

    [Fact]
    public void Call_status_badge_honors_a_custom_test_id()
    {
        IRenderedComponent<AgentCallStatusBadge> cut = Render<AgentCallStatusBadge>(parameters => parameters
            .Add(badge => badge.State, AgentCallStatus.Generating)
            .Add(badge => badge.TestId, "custom-call-badge"));

        cut.Find("[data-testid='custom-call-badge']").GetAttribute("role").ShouldBe("status");
    }
}
