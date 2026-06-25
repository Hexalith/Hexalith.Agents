using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Story 4.3 AC1 — the operational-status presentation distinguishes every readiness/failure/posting state, maps each
/// to a recovery-action group (grouped by action, never the raw subsystem), binds to a Fluent semantic role, and never
/// renders a non-proven state as success. Pure mapper — no bUnit.
/// </summary>
public sealed class OperationalStatusPresentationTests
{
    [Theory]
    // AC1 — configuration error, authorization failure, context-policy failure, content-safety/generation failure,
    // provider failure, pending approval, approval completion, posting pending, posting failure, successful post.
    [InlineData(AgentReadinessStatus.InvalidConfiguration, RecoveryActionGroup.FixPolicy, BadgeColor.Severe)]
    [InlineData(AgentReadinessStatus.MissingPartyIdentity, RecoveryActionGroup.LinkPartyIdentity, BadgeColor.Severe)]
    [InlineData(AgentReadinessStatus.ProviderUnavailable, RecoveryActionGroup.ConfigureProvider, BadgeColor.Severe)]
    [InlineData(AgentReadinessStatus.Disabled, RecoveryActionGroup.FixPolicy, BadgeColor.Severe)]
    [InlineData(AgentReadinessStatus.Checking, RecoveryActionGroup.None, BadgeColor.Informative)]
    [InlineData(AgentReadinessStatus.Callable, RecoveryActionGroup.None, BadgeColor.Success)]
    public void Agent_readiness_status_maps_to_action_group_and_role(AgentReadinessStatus status, RecoveryActionGroup group, BadgeColor color)
    {
        OperationalStatusPresentation.GroupFor(status).ShouldBe(group);
        OperationalStatusPresentation.ColorFor(status).ShouldBe(color);
        OperationalStatusPresentation.IconFor(status).ShouldNotBeNull();
        OperationalStatusPresentation.LabelKeyFor(status).ShouldBe($"Agents.OperationalStatus.AgentReadiness.{status}");
    }

    [Theory]
    [InlineData(AgentCallOperationStatus.Denied, RecoveryActionGroup.FixPolicy, BadgeColor.Danger)]
    [InlineData(AgentCallOperationStatus.ContextBlocked, RecoveryActionGroup.StartNewCall, BadgeColor.Severe)]
    [InlineData(AgentCallOperationStatus.GenerationFailed, RecoveryActionGroup.RetryGeneration, BadgeColor.Danger)]
    [InlineData(AgentCallOperationStatus.Generating, RecoveryActionGroup.None, BadgeColor.Informative)]
    [InlineData(AgentCallOperationStatus.Requested, RecoveryActionGroup.None, BadgeColor.Informative)]
    [InlineData(AgentCallOperationStatus.Generated, RecoveryActionGroup.None, BadgeColor.Informative)]
    public void Agent_call_status_maps_to_action_group_and_role(AgentCallOperationStatus status, RecoveryActionGroup group, BadgeColor color)
    {
        OperationalStatusPresentation.GroupFor(status).ShouldBe(group);
        OperationalStatusPresentation.ColorFor(status).ShouldBe(color);
        OperationalStatusPresentation.IconFor(status).ShouldNotBeNull();
    }

    [Theory]
    [InlineData(ProviderModelReadinessStatus.Failed, RecoveryActionGroup.ConfigureProvider, BadgeColor.Danger)]
    [InlineData(ProviderModelReadinessStatus.NotConfigured, RecoveryActionGroup.ConfigureProvider, BadgeColor.Severe)]
    [InlineData(ProviderModelReadinessStatus.Disabled, RecoveryActionGroup.ConfigureProvider, BadgeColor.Severe)]
    [InlineData(ProviderModelReadinessStatus.Degraded, RecoveryActionGroup.ConfigureProvider, BadgeColor.Warning)]
    [InlineData(ProviderModelReadinessStatus.Enabled, RecoveryActionGroup.None, BadgeColor.Success)]
    public void Provider_readiness_status_maps_to_action_group_and_role(ProviderModelReadinessStatus status, RecoveryActionGroup group, BadgeColor color)
    {
        OperationalStatusPresentation.GroupFor(status).ShouldBe(group);
        OperationalStatusPresentation.ColorFor(status).ShouldBe(color);
    }

    [Theory]
    [InlineData(ProposalOperationStatus.PendingApproval, RecoveryActionGroup.WaitForApproval, BadgeColor.Informative)]
    [InlineData(ProposalOperationStatus.Approved, RecoveryActionGroup.None, BadgeColor.Important)]
    [InlineData(ProposalOperationStatus.PostingPending, RecoveryActionGroup.None, BadgeColor.Informative)]
    [InlineData(ProposalOperationStatus.PostingFailed, RecoveryActionGroup.RetryGeneration, BadgeColor.Danger)]
    [InlineData(ProposalOperationStatus.Posted, RecoveryActionGroup.None, BadgeColor.Success)]
    [InlineData(ProposalOperationStatus.Rejected, RecoveryActionGroup.StartNewCall, BadgeColor.Danger)]
    [InlineData(ProposalOperationStatus.Expired, RecoveryActionGroup.StartNewCall, BadgeColor.Severe)]
    [InlineData(ProposalOperationStatus.Abandoned, RecoveryActionGroup.StartNewCall, BadgeColor.Subtle)]
    [InlineData(ProposalOperationStatus.Generated, RecoveryActionGroup.None, BadgeColor.Informative)]
    public void Proposal_status_maps_to_action_group_and_role(ProposalOperationStatus status, RecoveryActionGroup group, BadgeColor color)
    {
        OperationalStatusPresentation.GroupFor(status).ShouldBe(group);
        OperationalStatusPresentation.ColorFor(status).ShouldBe(color);
    }

    [Theory]
    [InlineData(AuditAvailabilityStatus.AuditAvailable, BadgeColor.Success)]
    [InlineData(AuditAvailabilityStatus.AuditPending, BadgeColor.Informative)]
    [InlineData(AuditAvailabilityStatus.AuditDelayed, BadgeColor.Warning)]
    [InlineData(AuditAvailabilityStatus.AuditUnavailable, BadgeColor.Severe)]
    public void Audit_availability_maps_to_inspect_audit_and_role(AuditAvailabilityStatus status, BadgeColor color)
    {
        OperationalStatusPresentation.GroupFor(status).ShouldBe(RecoveryActionGroup.InspectAudit);
        OperationalStatusPresentation.ColorFor(status).ShouldBe(color);
    }

    [Fact]
    public void Success_is_never_used_for_pending_checking_posting_pending_or_audit_pending()
    {
        // AC1 / AD-5 — never collapse a not-yet-proven state into success.
        OperationalStatusPresentation.ColorFor(AgentReadinessStatus.Checking).ShouldNotBe(BadgeColor.Success);
        OperationalStatusPresentation.ColorFor(ProposalOperationStatus.PostingPending).ShouldNotBe(BadgeColor.Success);
        OperationalStatusPresentation.ColorFor(ProposalOperationStatus.PendingApproval).ShouldNotBe(BadgeColor.Success);
        OperationalStatusPresentation.ColorFor(AuditAvailabilityStatus.AuditPending).ShouldNotBe(BadgeColor.Success);
        OperationalStatusPresentation.ColorFor(AuditAvailabilityStatus.AuditDelayed).ShouldNotBe(BadgeColor.Success);
        OperationalStatusPresentation.ColorFor(AuditAvailabilityStatus.AuditUnavailable).ShouldNotBe(BadgeColor.Success);
    }

    [Fact]
    public void Success_is_used_only_for_proven_states_and_brand_is_never_a_status()
    {
        foreach (AgentReadinessStatus status in Enum.GetValues<AgentReadinessStatus>())
        {
            if (status != AgentReadinessStatus.Callable)
            {
                OperationalStatusPresentation.ColorFor(status).ShouldNotBe(BadgeColor.Success);
            }

            OperationalStatusPresentation.ColorFor(status).ShouldNotBe(BadgeColor.Brand);
        }

        foreach (ProviderModelReadinessStatus status in Enum.GetValues<ProviderModelReadinessStatus>())
        {
            if (status != ProviderModelReadinessStatus.Enabled)
            {
                OperationalStatusPresentation.ColorFor(status).ShouldNotBe(BadgeColor.Success);
            }
        }

        foreach (AgentCallOperationStatus status in Enum.GetValues<AgentCallOperationStatus>())
        {
            // No state in the call-status enum is a proven terminal success (Posted lives on the proposal enum).
            OperationalStatusPresentation.ColorFor(status).ShouldNotBe(BadgeColor.Success);
            OperationalStatusPresentation.ColorFor(status).ShouldNotBe(BadgeColor.Brand);
        }

        foreach (ProposalOperationStatus status in Enum.GetValues<ProposalOperationStatus>())
        {
            if (status != ProposalOperationStatus.Posted)
            {
                OperationalStatusPresentation.ColorFor(status).ShouldNotBe(BadgeColor.Success);
            }

            OperationalStatusPresentation.ColorFor(status).ShouldNotBe(BadgeColor.Brand);
        }
    }

    [Fact]
    public void Recovery_guidance_and_group_headings_are_keyed_by_action_not_subsystem()
    {
        foreach (RecoveryActionGroup group in Enum.GetValues<RecoveryActionGroup>())
        {
            OperationalStatusPresentation.GroupLabelKeyFor(group).ShouldBe($"Agents.OperationalStatus.Group.{group}");
            OperationalStatusPresentation.GuidanceKeyFor(group).ShouldBe($"Agents.OperationalStatus.Recovery.{group}");
        }
    }

    [Theory]
    // AC1 — every activation blocker is grouped by the recovery ACTION an operator takes (UX-DR9), never the raw
    // subsystem: party-identity → link identity; provider/model → configure provider; everything else (display
    // name/instructions/response mode/approver/content-safety policy) → fix policy.
    [InlineData(AgentActivationBlocker.MissingPartyIdentity, RecoveryActionGroup.LinkPartyIdentity)]
    [InlineData(AgentActivationBlocker.MissingProviderSelection, RecoveryActionGroup.ConfigureProvider)]
    [InlineData(AgentActivationBlocker.ProviderUnavailable, RecoveryActionGroup.ConfigureProvider)]
    [InlineData(AgentActivationBlocker.MissingApproverPolicy, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentActivationBlocker.ApproverPolicyUnresolvable, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentActivationBlocker.MissingDisplayName, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentActivationBlocker.MissingInstructions, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentActivationBlocker.InvalidInstructions, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentActivationBlocker.MissingResponseMode, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentActivationBlocker.MissingContentSafetyPolicy, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentActivationBlocker.Unknown, RecoveryActionGroup.FixPolicy)]
    public void Activation_blocker_maps_to_its_recovery_action_group(AgentActivationBlocker blocker, RecoveryActionGroup group)
        => OperationalStatusPresentation.GroupForBlocker(blocker).ShouldBe(group);

    [Theory]
    // AC1 — each canonical readiness status projects onto the matching shipped UI readiness state so the existing
    // AgentReadinessBadge can render it; the Unknown sentinel projects onto the UI Unknown state (never a concrete one).
    [InlineData(AgentReadinessStatus.Callable, AgentReadinessState.Callable)]
    [InlineData(AgentReadinessStatus.Checking, AgentReadinessState.Checking)]
    [InlineData(AgentReadinessStatus.InvalidConfiguration, AgentReadinessState.InvalidConfiguration)]
    [InlineData(AgentReadinessStatus.MissingPartyIdentity, AgentReadinessState.MissingPartyIdentity)]
    [InlineData(AgentReadinessStatus.ProviderUnavailable, AgentReadinessState.ProviderUnavailable)]
    [InlineData(AgentReadinessStatus.Disabled, AgentReadinessState.Disabled)]
    [InlineData(AgentReadinessStatus.Unknown, AgentReadinessState.Unknown)]
    public void Agent_readiness_status_maps_to_the_canonical_ui_readiness_state(AgentReadinessStatus status, AgentReadinessState state)
        => OperationalStatusPresentation.ToReadinessState(status).ShouldBe(state);

    [Fact]
    public void Unknown_sentinels_resolve_to_a_safe_subtle_default_with_a_non_null_icon()
    {
        // AD-14 / totality — every switch is total: the Unknown=0 sentinel of each canonical enum renders through a
        // safe Subtle default and a non-null icon (asserted never-Success in the success-only facts), so an absent or
        // unrecognized status never reads as a proven/healthy state nor throws.
        OperationalStatusPresentation.ColorFor(AgentReadinessStatus.Unknown).ShouldBe(BadgeColor.Subtle);
        OperationalStatusPresentation.IconFor(AgentReadinessStatus.Unknown).ShouldNotBeNull();
        OperationalStatusPresentation.ColorFor(ProviderModelReadinessStatus.Unknown).ShouldBe(BadgeColor.Subtle);
        OperationalStatusPresentation.IconFor(ProviderModelReadinessStatus.Unknown).ShouldNotBeNull();
        OperationalStatusPresentation.ColorFor(AgentCallOperationStatus.Unknown).ShouldBe(BadgeColor.Subtle);
        OperationalStatusPresentation.IconFor(AgentCallOperationStatus.Unknown).ShouldNotBeNull();
        OperationalStatusPresentation.ColorFor(ProposalOperationStatus.Unknown).ShouldBe(BadgeColor.Subtle);
        OperationalStatusPresentation.IconFor(ProposalOperationStatus.Unknown).ShouldNotBeNull();
        OperationalStatusPresentation.ColorFor(AuditAvailabilityStatus.Unknown).ShouldBe(BadgeColor.Subtle);
        OperationalStatusPresentation.IconFor(AuditAvailabilityStatus.Unknown).ShouldNotBeNull();

        // The audit-availability Unknown groups to no recovery action (an absent/trivial state needs no operator action).
        OperationalStatusPresentation.GroupFor(AuditAvailabilityStatus.Unknown).ShouldBe(RecoveryActionGroup.None);
    }
}
