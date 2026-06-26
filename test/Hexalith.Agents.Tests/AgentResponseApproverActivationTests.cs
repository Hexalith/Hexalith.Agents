using System.Collections.Generic;
using System.Text.Json;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Activation-gate tests for the Story 1.6 response-mode + approver-policy readiness gates. Covers: a not-yet-chosen
/// mode blocking with <c>MissingResponseMode</c>; Automatic mode activatable with no approver policy; Confirmation
/// mode blocked by <c>MissingApproverPolicy</c> when no source is configured; Confirmation mode blocked by
/// <c>ApproverPolicyUnresolvable</c> on a non-<c>Valid</c>/absent verdict; Confirmation mode activatable with a
/// configured policy and a trusted <c>Valid</c> verdict; and the verdict-parse fail-closed hardening (numeric/
/// aliased/cased input → <c>Unknown</c>).
/// </summary>
public sealed class AgentResponseApproverActivationTests
{
    // A party-linked, provider-selected state with NO response mode (so the response-mode gate is the one in play).
    private static AgentState StateProviderReadyNoMode(string instructions = ValidInstructions)
    {
        AgentState state = StateWithLinkedParty(ValidCreate(instructions: instructions));
        state.Apply(new AgentProviderModelSelected(AgentId, SelectedProviderId, SelectedModelId, SelectedCapabilityVersion, state.ConfigurationVersion + 1));
        return state;
    }

    [Fact]
    public void Activation_is_blocked_by_missing_response_mode_when_no_mode_is_chosen()
    {
        // Party + provider ready, but no Response Mode chosen — the only remaining gate is MissingResponseMode.
        AgentState state = StateProviderReadyNoMode();

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        // 1.7: this state has no content-safety policy, so the content-safety gate is appended last.
        blocked.Blockers.ShouldBe([
            AgentActivationBlocker.MissingResponseMode,
            AgentActivationBlocker.MissingContentSafetyPolicy,
        ]);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Fact]
    public void Automatic_mode_is_activatable_with_no_approver_policy()
    {
        // StateWithSelectedProvider records Automatic mode; Automatic needs no approver policy (AC1).
        AgentState state = StateWithSelectedProvider(ValidCreate());
        state.ResponseMode.ShouldBe(AgentResponseMode.Automatic);
        state.ApproverPolicySources.ShouldBeNull();

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    [Fact]
    public void Confirmation_mode_without_a_configured_policy_is_blocked_by_missing_approver_policy()
    {
        AgentState state = StateProviderReadyNoMode();
        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Confirmation, state.ConfigurationVersion + 1));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        // 1.7: this state has no content-safety policy, so the content-safety gate is appended after the approver gate.
        blocked.Blockers.ShouldBe([
            AgentActivationBlocker.MissingApproverPolicy,
            AgentActivationBlocker.MissingContentSafetyPolicy,
        ]);
    }

    [Fact]
    public void Confirmation_mode_with_an_empty_policy_is_blocked_by_missing_approver_policy()
    {
        // An empty (sourceless) policy is storable but does not satisfy confirmation readiness — it is reported as
        // MissingApproverPolicy, not ApproverPolicyUnresolvable (hasApproverPolicy is false).
        AgentState state = StateConfirmationReady(ValidCreate(), new AgentApproverPolicy([], ApproverPolicyBasisDisclosure.OperatorOnly));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.MissingApproverPolicy]);
    }

    [Theory]
    [InlineData(ApproverPolicyValidationStatus.Incomplete)]
    [InlineData(ApproverPolicyValidationStatus.Missing)]
    [InlineData(ApproverPolicyValidationStatus.Disabled)]
    [InlineData(ApproverPolicyValidationStatus.Ambiguous)]
    [InlineData(ApproverPolicyValidationStatus.Unavailable)]
    [InlineData(ApproverPolicyValidationStatus.Unauthorized)]
    [InlineData(ApproverPolicyValidationStatus.Unknown)]
    public void Confirmation_mode_with_a_policy_but_non_valid_verdict_is_blocked_by_unresolvable(ApproverPolicyValidationStatus verdict)
    {
        AgentState state = StateConfirmationReady(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope(approverValidation: verdict));

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.ApproverPolicyUnresolvable]);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Fact]
    public void Confirmation_mode_with_an_absent_approver_verdict_fails_closed()
    {
        // A direct-gateway activation that never re-resolved carries no trusted approver verdict — fails closed.
        AgentState state = StateConfirmationReady(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope(includeApproverValidation: false));

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.ApproverPolicyUnresolvable]);
    }

    [Fact]
    public void Confirmation_mode_with_a_policy_and_valid_verdict_is_activatable()
    {
        AgentState state = StateConfirmationReady(ValidCreate());

        DomainResult result = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            ActivateEnvelope(providerValidation: ProviderSelectionValidationStatus.Valid, approverValidation: ApproverPolicyValidationStatus.Valid));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    [Theory]
    [InlineData("1")]      // numeric underlying value of Valid — Enum.TryParse would resolve it, so it must be rejected
    [InlineData("01")]     // numeric with leading zero
    [InlineData("valid")]  // wrong case — matched case-sensitively by canonical name
    [InlineData(" Valid")] // padded — not an exact name
    public void Confirmation_activation_with_numeric_or_aliased_approver_verdict_fails_closed(string rawVerdict)
    {
        // Mirrors the 1.4/1.5 verdict-parse hardening: a numeric/aliased/cased approver verdict must never be trusted.
        AgentState state = StateConfirmationReady(ValidCreate());

        CommandEnvelope envelope = ActivateEnvelopeWithRawApproverVerdict(rawVerdict);
        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, envelope);

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.ApproverPolicyUnresolvable]);
    }

    // ===== QA gap-fill (AC3): the new response/approver gates keep the documented deterministic blocker order =====

    [Fact]
    public void Activation_blockers_are_reported_in_the_documented_order_including_the_response_mode_gate()
    {
        // A freshly created Draft agent has no Party, no Provider, an unchosen mode, and no content-safety policy. The
        // gate order is fixed: party identity → provider selection → response mode → content safety (each 1.4–1.7 gate
        // is appended last, never reordered).
        AgentState state = StateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([
                AgentActivationBlocker.MissingPartyIdentity,
                AgentActivationBlocker.MissingProviderSelection,
                AgentActivationBlocker.MissingResponseMode,
                AgentActivationBlocker.MissingContentSafetyPolicy,
            ]);
    }

    [Fact]
    public void Confirmation_activation_reports_the_provider_gate_before_the_approver_gate()
    {
        // Confirmation-ready state, but a direct-gateway activation carries neither trusted verdict: the provider
        // gate (ProviderUnavailable) is appended before the approver gate (ApproverPolicyUnresolvable) — the approver
        // gate never masks an earlier unresolved provider.
        AgentState state = StateConfirmationReady(ValidCreate());

        DomainResult result = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            ActivateEnvelope(includeProviderValidation: false, includeApproverValidation: false));

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([
                AgentActivationBlocker.ProviderUnavailable,
                AgentActivationBlocker.ApproverPolicyUnresolvable,
            ]);
    }

    // Builds an ActivateAgent envelope with provider:selectionValidation = Valid and an arbitrary raw
    // approver:policyValidation string (to test fail-closed parsing of an unrecognized verdict value).
    private static CommandEnvelope ActivateEnvelopeWithRawApproverVerdict(string rawApproverVerdict)
        => new(
            "msg-raw-activate",
            TenantId,
            "agent",
            AgentId,
            nameof(ActivateAgent),
            JsonSerializer.SerializeToUtf8Bytes(new ActivateAgent()),
            "corr-raw",
            null,
            "admin-user",
            new Dictionary<string, string>
            {
                [AgentAdminExtensionKey] = "true",
                [ProviderSelectionValidationExtensionKey] = ProviderSelectionValidationStatus.Valid.ToString(),
                [ApproverPolicyValidationExtensionKey] = rawApproverVerdict,
            });
}
