using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 2.2 invocation gate surface (AC1–AC4; AD-12, AD-14). Durable event sourcing replays
/// every new event/rejection, so each must survive System.Text.Json; the new classification enums must serialize by
/// name and fail safe to their <c>Unknown</c> sentinel; the additive <see cref="AgentInteractionStatus"/> extension
/// must not perturb the existing <c>Unknown</c>/<c>Requested</c> ordinals; and the verdict/evidence types must expose
/// ONLY the two safe gate enums (no prompt, claims, tokens, PartyId, provider payload, or message — AD-14). The
/// assembly-wide <see cref="ContractsSecretNonDisclosureTests"/> additionally auto-covers the new types.
/// </summary>
public sealed class AgentInteractionGateContractsTests
{
    private static IReadOnlyList<AgentInvocationGateVerdict> Blockers { get; } =
    [
        new(AgentInteractionGateCheck.TenantAccess, AgentInteractionGateOutcome.Unauthorized),
        new(AgentInteractionGateCheck.AgentLifecycle, AgentInteractionGateOutcome.Disabled),
    ];

    // ===== Marker interfaces =====

    [Fact]
    public void Gate_outcome_events_implement_IEventPayload_and_are_not_rejections()
    {
        // Denied/Blocked is a recorded SUCCESS outcome (Audit Evidence), NOT a rejection (AD-2; FR-24).
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentInteractionAuthorized)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentInteractionGateFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentInteractionAuthorized)).ShouldBeFalse();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentInteractionGateFailed)).ShouldBeFalse();
    }

    [Fact]
    public void Gate_not_evaluable_rejection_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentInteractionGateNotEvaluableRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentInteractionGateNotEvaluableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Gate_command_has_no_marker_interface_or_domain_attribute()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(EvaluateAgentInteractionGate)).ShouldBeFalse();
        typeof(EvaluateAgentInteractionGate)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Gate_command_round_trips_with_its_verdicts()
    {
        var command = new EvaluateAgentInteractionGate("interaction-001", Blockers);
        EvaluateAgentInteractionGate roundTripped = RoundTrip(command);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Verdicts.ShouldBe(Blockers);
    }

    [Fact]
    public void Authorized_event_round_trips()
    {
        var authorized = new AgentInteractionAuthorized("interaction-001");
        RoundTrip(authorized).ShouldBe(authorized);
    }

    [Fact]
    public void Gate_failed_event_round_trips_with_its_decision_and_blockers()
    {
        var failed = new AgentInteractionGateFailed("interaction-001", AgentInteractionStatus.Denied, Blockers);
        AgentInteractionGateFailed roundTripped = RoundTrip(failed);
        roundTripped.Decision.ShouldBe(AgentInteractionStatus.Denied);
        roundTripped.Blockers.ShouldBe(Blockers);
    }

    [Fact]
    public void Gate_not_evaluable_rejection_round_trips_with_the_enum_reason()
    {
        var rejection = new AgentInteractionGateNotEvaluableRejection("interaction-001", AgentInteractionGateNotEvaluableReason.NoVerdictsProvided);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    [Fact]
    public void Verdict_round_trips()
    {
        var verdict = new AgentInvocationGateVerdict(AgentInteractionGateCheck.ProviderModelReadiness, AgentInteractionGateOutcome.Stale);
        RoundTrip(verdict).ShouldBe(verdict);
    }

    [Fact]
    public void Evidence_query_round_trips()
    {
        var query = new GetAgentInteractionGateEvidenceQuery("interaction-001");
        RoundTrip(query).ShouldBe(query);
    }

    [Fact]
    public void Evidence_view_round_trips_with_its_verdicts()
    {
        var view = new AgentInteractionGateEvidenceView("interaction-001", AgentInteractionStatus.Blocked, Blockers);
        AgentInteractionGateEvidenceView roundTripped = RoundTrip(view);
        roundTripped.Status.ShouldBe(AgentInteractionStatus.Blocked);
        roundTripped.Verdicts.ShouldBe(Blockers);
    }

    [Fact]
    public void Evidence_result_round_trips_for_a_successful_inspection()
    {
        var result = AgentInteractionGateEvidenceResult.Success(
            new AgentInteractionGateEvidenceView("interaction-001", AgentInteractionStatus.Authorized, []));
        AgentInteractionGateEvidenceResult roundTripped = RoundTrip(result);
        roundTripped.Status.ShouldBe(AgentInteractionGateInspectionStatus.Success);
        roundTripped.Evidence.ShouldNotBeNull();
    }

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Gate_check_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionGateCheck.SourceConversationAccess).ShouldBe("\"SourceConversationAccess\"");
        default(AgentInteractionGateCheck).ShouldBe(AgentInteractionGateCheck.Unknown);
        JsonSerializer.Deserialize<AgentInteractionGateCheck>("\"Unknown\"").ShouldBe(AgentInteractionGateCheck.Unknown);
    }

    [Fact]
    public void Gate_outcome_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionGateOutcome.Unauthorized).ShouldBe("\"Unauthorized\"");
        default(AgentInteractionGateOutcome).ShouldBe(AgentInteractionGateOutcome.Unknown);
        JsonSerializer.Deserialize<AgentInteractionGateOutcome>("\"Unknown\"").ShouldBe(AgentInteractionGateOutcome.Unknown);
    }

    [Fact]
    public void Gate_not_evaluable_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionGateNotEvaluableReason.InteractionNotRequested).ShouldBe("\"InteractionNotRequested\"");
        default(AgentInteractionGateNotEvaluableReason).ShouldBe(AgentInteractionGateNotEvaluableReason.Unknown);
        JsonSerializer.Deserialize<AgentInteractionGateNotEvaluableReason>("\"Unknown\"").ShouldBe(AgentInteractionGateNotEvaluableReason.Unknown);
    }

    // ===== AgentInteractionStatus additive extension (AD-2) =====

    [Fact]
    public void Interaction_status_gate_states_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.Authorized).ShouldBe("\"Authorized\"");
        JsonSerializer.Serialize(AgentInteractionStatus.Denied).ShouldBe("\"Denied\"");
        JsonSerializer.Serialize(AgentInteractionStatus.Blocked).ShouldBe("\"Blocked\"");
    }

    [Fact]
    public void Interaction_status_additive_extension_preserves_the_existing_ordinals()
    {
        // The Story 2.1 ordinals must be untouched so existing round-trip/replay stays green (AD-2 additive rule).
        ((int)AgentInteractionStatus.Unknown).ShouldBe(0);
        ((int)AgentInteractionStatus.Requested).ShouldBe(1);
        JsonSerializer.Deserialize<AgentInteractionStatus>("\"Requested\"").ShouldBe(AgentInteractionStatus.Requested);
        JsonSerializer.Deserialize<AgentInteractionStatus>("\"Unknown\"").ShouldBe(AgentInteractionStatus.Unknown);
    }

    // ===== Safe by construction: only the two gate enums, null view on failed inspection (AC3, AC4; AD-14) =====

    [Fact]
    public void Verdict_exposes_only_the_two_safe_gate_enums()
    {
        System.Reflection.PropertyInfo[] properties = typeof(AgentInvocationGateVerdict).GetProperties();
        properties.Length.ShouldBe(2);
        properties.ShouldContain(p => p.Name == "Check" && p.PropertyType == typeof(AgentInteractionGateCheck));
        properties.ShouldContain(p => p.Name == "Outcome" && p.PropertyType == typeof(AgentInteractionGateOutcome));
    }

    [Fact]
    public void Gate_evidence_and_outcome_types_carry_no_sensitive_members()
    {
        // No prompt, claims, tokens, PartyId personal data, provider payload, or message string on any gate surface.
        HasNoSensitiveMember(typeof(AgentInvocationGateVerdict));
        HasNoSensitiveMember(typeof(AgentInteractionGateEvidenceView));
        HasNoSensitiveMember(typeof(AgentInteractionGateFailed));
        HasNoSensitiveMember(typeof(AgentInteractionAuthorized));
        HasNoSensitiveMember(typeof(AgentInteractionGateNotEvaluableRejection));
    }

    [Fact]
    public void Failed_inspection_result_carries_a_null_evidence_view()
    {
        // A failed inspection NEVER reveals whether the interaction exists in another tenant (AC3).
        AgentInteractionGateEvidenceResult.NotAuthorized().Evidence.ShouldBeNull();
        AgentInteractionGateEvidenceResult.NotAuthorized().Status.ShouldBe(AgentInteractionGateInspectionStatus.NotAuthorized);
        AgentInteractionGateEvidenceResult.NotFound().Evidence.ShouldBeNull();
        AgentInteractionGateEvidenceResult.NotFound().Status.ShouldBe(AgentInteractionGateInspectionStatus.NotFound);
    }

    [Fact]
    public void Failed_inspection_results_round_trip_with_a_null_evidence_view()
    {
        // The wire form of a failed inspection must also carry no evidence — the null view survives serialization so a
        // probe of another tenant's interaction learns nothing (AC3; AD-12).
        AgentInteractionGateEvidenceResult notAuthorized = RoundTrip(AgentInteractionGateEvidenceResult.NotAuthorized());
        notAuthorized.Status.ShouldBe(AgentInteractionGateInspectionStatus.NotAuthorized);
        notAuthorized.Evidence.ShouldBeNull();

        AgentInteractionGateEvidenceResult notFound = RoundTrip(AgentInteractionGateEvidenceResult.NotFound());
        notFound.Status.ShouldBe(AgentInteractionGateInspectionStatus.NotFound);
        notFound.Evidence.ShouldBeNull();
    }

    private static void HasNoSensitiveMember(Type type)
    {
        string[] forbidden = ["Prompt", "Claim", "Token", "Secret", "Payload", "Message", "Content", "Personal", "DisplayName", "Contact"];
        foreach (System.Reflection.PropertyInfo property in type.GetProperties())
        {
            forbidden.ShouldNotContain(
                token => property.Name.Contains(token, StringComparison.OrdinalIgnoreCase),
                $"{type.Name}.{property.Name} exposes a sensitive member on a gate surface (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
