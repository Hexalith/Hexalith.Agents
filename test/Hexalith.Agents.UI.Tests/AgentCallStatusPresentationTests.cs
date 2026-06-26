using System;
using System.Linq;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC2/AC3 — the pure call-status presentation mapping. The durable→UX map is total; Posted is the ONLY Success colour
/// and only the durable Posted status reaches it (UX-DR11); failures are Danger; blocked-but-not-failed is Severe;
/// in-flight is Informative; Unknown is Subtle. Every state has a non-null icon and a whole-string label key, and every
/// safe reason resolves to a coarse, content-free reason key — never raw error text, a secret, an id, or PII (AD-14).
/// </summary>
public sealed class AgentCallStatusPresentationTests
{
    [Theory]
    [InlineData(AgentInteractionStatus.Requested, AgentCallStatus.Requested)]
    [InlineData(AgentInteractionStatus.Authorized, AgentCallStatus.Authorized)]
    [InlineData(AgentInteractionStatus.Denied, AgentCallStatus.Denied)]
    [InlineData(AgentInteractionStatus.Blocked, AgentCallStatus.Blocked)]
    [InlineData(AgentInteractionStatus.ContextReady, AgentCallStatus.Generating)]
    [InlineData(AgentInteractionStatus.ContextBlocked, AgentCallStatus.ContextBlocked)]
    [InlineData(AgentInteractionStatus.Generated, AgentCallStatus.Generated)]
    [InlineData(AgentInteractionStatus.GenerationFailed, AgentCallStatus.GenerationFailed)]
    [InlineData(AgentInteractionStatus.SafetyFailed, AgentCallStatus.SafetyFailed)]
    [InlineData(AgentInteractionStatus.Posted, AgentCallStatus.Posted)]
    [InlineData(AgentInteractionStatus.PostingFailed, AgentCallStatus.PostingFailed)]
    [InlineData(AgentInteractionStatus.Unknown, AgentCallStatus.Unknown)]
    public void MapStatus_maps_every_durable_status_to_its_ux_state(AgentInteractionStatus durable, AgentCallStatus expected)
        => AgentCallStatusPresentation.MapStatus(durable).ShouldBe(expected);

    [Fact]
    public void MapStatus_is_total_over_every_durable_status()
    {
        foreach (AgentInteractionStatus status in Enum.GetValues<AgentInteractionStatus>())
        {
            // No durable status throws or silently becomes a transient state (transients are derived, never mapped).
            AgentCallStatus mapped = AgentCallStatusPresentation.MapStatus(status);
            mapped.ShouldNotBe(AgentCallStatus.ContextLoading);
            mapped.ShouldNotBe(AgentCallStatus.PostingPending);
        }
    }

    [Theory]
    [InlineData(AgentInteractionStatus.Authorized, true, AgentResponseMode.Automatic, AgentCallStatus.ContextLoading)]
    [InlineData(AgentInteractionStatus.ContextReady, true, AgentResponseMode.Automatic, AgentCallStatus.Generating)]
    [InlineData(AgentInteractionStatus.Generated, true, AgentResponseMode.Automatic, AgentCallStatus.PostingPending)]
    [InlineData(AgentInteractionStatus.Generated, true, AgentResponseMode.Confirmation, AgentCallStatus.Generated)]
    [InlineData(AgentInteractionStatus.Authorized, false, AgentResponseMode.Automatic, AgentCallStatus.Authorized)]
    [InlineData(AgentInteractionStatus.Generated, false, AgentResponseMode.Automatic, AgentCallStatus.Generated)]
    [InlineData(AgentInteractionStatus.Posted, true, AgentResponseMode.Automatic, AgentCallStatus.Posted)]
    // A status with no transient successor returns the durable map even while in flight (the `_ => MapStatus` default).
    [InlineData(AgentInteractionStatus.Requested, true, AgentResponseMode.Automatic, AgentCallStatus.Requested)]
    [InlineData(AgentInteractionStatus.Denied, true, AgentResponseMode.Automatic, AgentCallStatus.Denied)]
    // Response mode is only consulted for the Generated → PostingPending derivation; ContextReady is mode-independent.
    [InlineData(AgentInteractionStatus.ContextReady, true, AgentResponseMode.Confirmation, AgentCallStatus.Generating)]
    public void Derive_renders_transient_states_only_while_a_milestone_is_pending(
        AgentInteractionStatus durable, bool inFlight, AgentResponseMode mode, AgentCallStatus expected)
        => AgentCallStatusPresentation.Derive(durable, inFlight, mode).ShouldBe(expected);

    [Fact]
    public void Posted_is_the_only_success_color_and_only_posted_maps_to_it()
    {
        AgentCallStatusPresentation.ColorFor(AgentCallStatus.Posted).ShouldBe(BadgeColor.Success);

        foreach (AgentCallStatus state in Enum.GetValues<AgentCallStatus>())
        {
            if (state != AgentCallStatus.Posted)
            {
                AgentCallStatusPresentation.ColorFor(state).ShouldNotBe(BadgeColor.Success);
            }
        }

        // Only the durable Posted status reaches the Posted UX state, so Success is unreachable from any other durable
        // status (UX-DR11).
        foreach (AgentInteractionStatus durable in Enum.GetValues<AgentInteractionStatus>())
        {
            if (durable != AgentInteractionStatus.Posted)
            {
                AgentCallStatusPresentation.ColorFor(AgentCallStatusPresentation.MapStatus(durable)).ShouldNotBe(BadgeColor.Success);
            }
        }
    }

    [Theory]
    [InlineData(AgentCallStatus.Denied, BadgeColor.Danger)]
    [InlineData(AgentCallStatus.GenerationFailed, BadgeColor.Danger)]
    [InlineData(AgentCallStatus.SafetyFailed, BadgeColor.Danger)]
    [InlineData(AgentCallStatus.PostingFailed, BadgeColor.Danger)]
    [InlineData(AgentCallStatus.Blocked, BadgeColor.Severe)]
    [InlineData(AgentCallStatus.ContextBlocked, BadgeColor.Severe)]
    [InlineData(AgentCallStatus.Requested, BadgeColor.Informative)]
    [InlineData(AgentCallStatus.Authorized, BadgeColor.Informative)]
    [InlineData(AgentCallStatus.ContextLoading, BadgeColor.Informative)]
    [InlineData(AgentCallStatus.Generating, BadgeColor.Informative)]
    [InlineData(AgentCallStatus.Generated, BadgeColor.Informative)]
    [InlineData(AgentCallStatus.PostingPending, BadgeColor.Informative)]
    [InlineData(AgentCallStatus.Unknown, BadgeColor.Subtle)]
    public void ColorFor_binds_each_state_to_its_semantic_role(AgentCallStatus state, BadgeColor expected)
        => AgentCallStatusPresentation.ColorFor(state).ShouldBe(expected);

    [Fact]
    public void Brand_is_never_used_as_a_status_color()
    {
        foreach (AgentCallStatus state in Enum.GetValues<AgentCallStatus>())
        {
            AgentCallStatusPresentation.ColorFor(state).ShouldNotBe(BadgeColor.Brand);
        }
    }

    [Fact]
    public void Every_state_has_a_non_null_icon_and_a_whole_string_label_key()
    {
        foreach (AgentCallStatus state in Enum.GetValues<AgentCallStatus>())
        {
            AgentCallStatusPresentation.IconFor(state).ShouldNotBeNull();
            AgentCallStatusPresentation.LabelKeyFor(state).ShouldBe($"Agents.CallStatus.Status.{state}");
        }
    }

    [Fact]
    public void Coarse_reason_key_is_present_only_for_failure_and_blocked_states()
    {
        AgentCallStatus[] withReason =
        [
            AgentCallStatus.Denied,
            AgentCallStatus.Blocked,
            AgentCallStatus.ContextBlocked,
            AgentCallStatus.SafetyFailed,
            AgentCallStatus.GenerationFailed,
            AgentCallStatus.PostingFailed,
        ];

        foreach (AgentCallStatus state in Enum.GetValues<AgentCallStatus>())
        {
            string? key = AgentCallStatusPresentation.ReasonKeyFor(state);
            if (withReason.Contains(state))
            {
                key.ShouldBe($"Agents.CallStatus.Reason.{state}");
            }
            else
            {
                key.ShouldBeNull($"{state} carries no reason");
            }
        }
    }

    [Fact]
    public void Every_reason_enum_value_maps_to_a_safe_whole_string_key_with_no_leak()
    {
        // A safe reason key is a single whole-string key of the shape "Agents.CallStatus.Reason.<EnumValue>"; it never
        // embeds raw error text, a secret, an id, or PII (AD-14). Enums cannot be "poisoned", so we assert the produced
        // key is structurally safe for EVERY value of EVERY reason enum.
        foreach (AgentInteractionContextBlockReason r in Enum.GetValues<AgentInteractionContextBlockReason>())
        {
            AssertSafeReasonKey(AgentCallStatusPresentation.ReasonKeyFor(r), r.ToString());
        }

        foreach (AgentOutputGenerationFailureReason r in Enum.GetValues<AgentOutputGenerationFailureReason>())
        {
            AssertSafeReasonKey(AgentCallStatusPresentation.ReasonKeyFor(r), r.ToString());
        }

        foreach (AgentResponsePostingFailureReason r in Enum.GetValues<AgentResponsePostingFailureReason>())
        {
            AssertSafeReasonKey(AgentCallStatusPresentation.ReasonKeyFor(r), r.ToString());
        }

        foreach (AgentInteractionGateOutcome o in Enum.GetValues<AgentInteractionGateOutcome>())
        {
            AssertSafeReasonKey(AgentCallStatusPresentation.ReasonKeyFor(o), o.ToString());
        }

        foreach (AgentInteractionGateCheck c in Enum.GetValues<AgentInteractionGateCheck>())
        {
            AssertSafeReasonKey(AgentCallStatusPresentation.ReasonKeyFor(c), c.ToString());
        }
    }

    private static void AssertSafeReasonKey(string key, string enumValue)
    {
        key.ShouldBe($"Agents.CallStatus.Reason.{enumValue}");
        key.ShouldStartWith("Agents.CallStatus.Reason.");
        key.ShouldNotContain(" ");
        key.ShouldNotContain(":");
    }
}
