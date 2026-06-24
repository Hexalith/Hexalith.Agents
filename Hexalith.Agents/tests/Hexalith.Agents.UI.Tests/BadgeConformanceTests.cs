using System;
using System.Text.RegularExpressions;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC2/AC5 — every status badge renders color + icon + visible text together (never color-only), binds to a Fluent
/// semantic role (not a hex value), and resolves a single whole string through the localizer (no runtime fragment
/// concatenation). The stub localizer returns the key, so a visible text equal to the key proves both.
/// </summary>
public sealed class BadgeConformanceTests : AgentsTestContext
{
    private static readonly Regex _sixDigitHex = new("#[0-9a-fA-F]{6}", RegexOptions.Compiled);

    [Theory]
    [InlineData(AgentReadinessState.Callable)]
    [InlineData(AgentReadinessState.Disabled)]
    [InlineData(AgentReadinessState.MissingPartyIdentity)]
    [InlineData(AgentReadinessState.ProviderUnavailable)]
    [InlineData(AgentReadinessState.InvalidConfiguration)]
    [InlineData(AgentReadinessState.Unresolved)]
    [InlineData(AgentReadinessState.Checking)]
    [InlineData(AgentReadinessState.Unknown)]
    public void Readiness_badge_renders_color_icon_and_localized_whole_string(AgentReadinessState state)
    {
        IRenderedComponent<AgentReadinessBadge> cut = Render<AgentReadinessBadge>(
            parameters => parameters.Add(badge => badge.State, state));

        FluentBadge badge = cut.FindComponent<FluentBadge>().Instance;
        badge.Color.ShouldBe(AgentReadiness.ColorFor(state));
        badge.IconStart.ShouldNotBeNull();

        string expectedKey = AgentReadiness.LabelKeyFor(state);
        cut.VisibleText().Trim().ShouldBe(expectedKey);
        cut.Find("[data-testid='agent-readiness-badge']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='agent-readiness-badge']").GetAttribute("aria-label").ShouldBe(expectedKey);
        _sixDigitHex.IsMatch(cut.Markup).ShouldBeFalse("status badges bind to Fluent roles, never inline hex (AC5)");
    }

    [Fact]
    public void Readiness_badge_uses_success_only_for_callable()
    {
        foreach (AgentReadinessState state in Enum.GetValues<AgentReadinessState>())
        {
            BadgeColor color = AgentReadiness.ColorFor(state);
            if (state != AgentReadinessState.Callable)
            {
                color.ShouldNotBe(BadgeColor.Success);
            }
        }
    }

    [Theory]
    [InlineData(ProviderReadinessState.Enabled)]
    [InlineData(ProviderReadinessState.NotConfigured)]
    [InlineData(ProviderReadinessState.Disabled)]
    [InlineData(ProviderReadinessState.HistoricalSelection)]
    [InlineData(ProviderReadinessState.Degraded)]
    [InlineData(ProviderReadinessState.Failed)]
    [InlineData(ProviderReadinessState.Unknown)]
    public void Provider_status_badge_renders_color_icon_and_localized_whole_string(ProviderReadinessState state)
    {
        IRenderedComponent<ProviderStatusBadge> cut = Render<ProviderStatusBadge>(
            parameters => parameters.Add(badge => badge.State, state));

        FluentBadge badge = cut.FindComponent<FluentBadge>().Instance;
        badge.Color.ShouldBe(AgentReadiness.ColorFor(state));
        badge.IconStart.ShouldNotBeNull();

        string expectedKey = AgentReadiness.LabelKeyFor(state);
        cut.VisibleText().Trim().ShouldBe(expectedKey);
        cut.Find("[data-testid='provider-status-badge']").GetAttribute("role").ShouldBe("status");
        _sixDigitHex.IsMatch(cut.Markup).ShouldBeFalse("status badges bind to Fluent roles, never inline hex (AC5)");
    }

    [Fact]
    public void Provider_status_badge_uses_success_only_for_enabled()
    {
        foreach (ProviderReadinessState state in Enum.GetValues<ProviderReadinessState>())
        {
            BadgeColor color = AgentReadiness.ColorFor(state);
            if (state != ProviderReadinessState.Enabled)
            {
                color.ShouldNotBe(BadgeColor.Success);
            }
        }
    }

    [Theory]
    [InlineData(ProposedAgentReplyState.Pending)]
    [InlineData(ProposedAgentReplyState.Edited)]
    // Story 3.6 terminal states render through the badge with color + icon + whole localized string (DESIGN: color is
    // never the only signal; never inline hex).
    [InlineData(ProposedAgentReplyState.Rejected)]
    [InlineData(ProposedAgentReplyState.Abandoned)]
    [InlineData(ProposedAgentReplyState.Expired)]
    [InlineData(ProposedAgentReplyState.Unknown)]
    public void Proposal_state_badge_renders_color_icon_and_localized_whole_string(ProposedAgentReplyState state)
    {
        IRenderedComponent<ProposedAgentReplyStateBadge> cut = Render<ProposedAgentReplyStateBadge>(
            parameters => parameters.Add(badge => badge.State, state));

        FluentBadge badge = cut.FindComponent<FluentBadge>().Instance;
        badge.Color.ShouldBe(ProposedAgentReplyStatePresentation.ColorFor(state));
        badge.IconStart.ShouldNotBeNull();

        string expectedKey = ProposedAgentReplyStatePresentation.LabelKeyFor(state);
        cut.VisibleText().Trim().ShouldBe(expectedKey);
        cut.Find("[data-testid='proposed-agent-reply-state-badge']").GetAttribute("role").ShouldBe("status");
        cut.Find("[data-testid='proposed-agent-reply-state-badge']").GetAttribute("aria-label").ShouldBe(expectedKey);
        _sixDigitHex.IsMatch(cut.Markup).ShouldBeFalse("status badges bind to Fluent roles, never inline hex (AC5)");
    }

    [Fact]
    public void Proposal_state_badge_uses_success_only_after_posting_and_never_uses_brand()
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
    }
}
