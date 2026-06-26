using System;
using System.Linq;
using System.Reflection;
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
/// Contract guard for the Story 2.3 Conversation context surface (AC1–AC4; AD-2, AD-11, AD-14). Durable event sourcing
/// replays every new event/rejection, so each must survive System.Text.Json; the new classification enums must serialize
/// by name and fail safe to their <c>Unknown</c> sentinel; the additive <see cref="AgentInteractionStatus"/> extension
/// must not perturb the existing 0-4 ordinals; and the measurement/evidence/view types must carry ONLY safe
/// numerics/enums/references (no message text, prompt, claims, secret tokens, PartyId, or provider payload — AD-14).
/// The assembly-wide <c>ContractsSecretNonDisclosureTests</c> additionally auto-covers the new types.
/// </summary>
public sealed class AgentInteractionContextContractsTests
{
    private static AgentInteractionContextEvidence FullEvidence { get; } = new(
        AgentInteractionContextMode.Full,
        FullContextTokenCount: 1_000,
        UsedContextTokenCount: 1_000,
        MessageCount: 3,
        ReservedOutputTokenCount: 16_000,
        ContextWindowTokenLimit: 128_000,
        ProviderCapabilityVersion: 1,
        AgentInteractionSnapshot.DefaultContextPolicyReference,
        BoundedBehaviorReference: null);

    private static AgentInteractionContextMeasurement LoadedMeasurement { get; } = new(
        AgentInteractionContextLoadOutcome.Loaded,
        FullContextTokenCount: 1_000,
        MessageCount: 3,
        ContextWindowTokenLimit: 128_000,
        ReservedOutputTokenCount: 16_000,
        ProviderCapabilityVersion: 1,
        AgentInteractionSnapshot.DefaultContextPolicyReference,
        new AgentInteractionBoundedContextBehavior("bounded-v1", 50_000));

    // ===== Marker interfaces =====

    [Fact]
    public void Context_outcome_events_implement_IEventPayload_and_are_not_rejections()
    {
        // ContextReady/ContextBlocked are recorded SUCCESS outcomes (Audit Evidence), NOT rejections (AD-2; FR-24).
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentInteractionContextReady)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentInteractionContextBlocked)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentInteractionContextReady)).ShouldBeFalse();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentInteractionContextBlocked)).ShouldBeFalse();
    }

    [Fact]
    public void Context_not_buildable_rejection_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentInteractionContextNotBuildableRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentInteractionContextNotBuildableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Context_command_has_no_marker_interface_or_hexalith_attribute()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(BuildAgentInteractionContext)).ShouldBeFalse();
        // Scope attribute-absence to Hexalith-namespaced attributes: the compiler emits Nullable* attributes on records
        // with nullable members (Story 2.1 learning), which are not a contract concern.
        typeof(BuildAgentInteractionContext)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Context_command_round_trips_with_its_measurement()
    {
        var command = new BuildAgentInteractionContext("interaction-001", LoadedMeasurement);
        BuildAgentInteractionContext roundTripped = RoundTrip(command);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Measurement.ShouldBe(LoadedMeasurement);
    }

    [Fact]
    public void Measurement_and_bounded_behavior_round_trip()
    {
        RoundTrip(LoadedMeasurement).ShouldBe(LoadedMeasurement);
        var bounded = new AgentInteractionBoundedContextBehavior("bounded-v1", 50_000);
        RoundTrip(bounded).ShouldBe(bounded);
    }

    [Fact]
    public void Evidence_round_trips()
    {
        RoundTrip(FullEvidence).ShouldBe(FullEvidence);
        var boundedEvidence = FullEvidence with { Mode = AgentInteractionContextMode.Bounded, BoundedBehaviorReference = "bounded-v1" };
        RoundTrip(boundedEvidence).ShouldBe(boundedEvidence);
    }

    [Fact]
    public void Context_ready_event_round_trips_with_its_evidence()
    {
        var ready = new AgentInteractionContextReady("interaction-001", FullEvidence);
        AgentInteractionContextReady roundTripped = RoundTrip(ready);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Evidence.Mode.ShouldBe(AgentInteractionContextMode.Full);
    }

    [Fact]
    public void Context_blocked_event_round_trips_with_its_reason_and_evidence()
    {
        var blocked = new AgentInteractionContextBlocked("interaction-001", AgentInteractionContextBlockReason.ExceedsModelBudget, FullEvidence);
        AgentInteractionContextBlocked roundTripped = RoundTrip(blocked);
        roundTripped.Reason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        roundTripped.Evidence.ShouldBe(FullEvidence);
    }

    [Fact]
    public void Context_not_buildable_rejection_round_trips_with_the_enum_reason()
    {
        var rejection = new AgentInteractionContextNotBuildableRejection("interaction-001", AgentInteractionContextNotBuildableReason.InteractionNotAuthorized);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    [Fact]
    public void Evidence_query_round_trips()
    {
        var query = new GetAgentInteractionContextEvidenceQuery("interaction-001");
        RoundTrip(query).ShouldBe(query);
    }

    [Fact]
    public void Evidence_view_round_trips_with_its_evidence_and_block_reason()
    {
        var view = new AgentInteractionContextEvidenceView(
            "interaction-001", AgentInteractionStatus.ContextBlocked, FullEvidence, AgentInteractionContextBlockReason.ExceedsModelBudget);
        AgentInteractionContextEvidenceView roundTripped = RoundTrip(view);
        roundTripped.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        roundTripped.Evidence.ShouldBe(FullEvidence);
        roundTripped.BlockReason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
    }

    [Fact]
    public void Evidence_result_round_trips_for_a_successful_inspection()
    {
        AgentInteractionContextEvidenceResult result = AgentInteractionContextEvidenceResult.Success(
            new AgentInteractionContextEvidenceView("interaction-001", AgentInteractionStatus.ContextReady, FullEvidence, null));
        AgentInteractionContextEvidenceResult roundTripped = RoundTrip(result);
        roundTripped.Status.ShouldBe(AgentInteractionContextInspectionStatus.Success);
        roundTripped.Evidence.ShouldNotBeNull();
    }

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Context_mode_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionContextMode.Bounded).ShouldBe("\"Bounded\"");
        default(AgentInteractionContextMode).ShouldBe(AgentInteractionContextMode.Unknown);
        JsonSerializer.Deserialize<AgentInteractionContextMode>("\"Unknown\"").ShouldBe(AgentInteractionContextMode.Unknown);
    }

    [Fact]
    public void Context_load_outcome_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionContextLoadOutcome.Loaded).ShouldBe("\"Loaded\"");
        default(AgentInteractionContextLoadOutcome).ShouldBe(AgentInteractionContextLoadOutcome.Unknown);
        JsonSerializer.Deserialize<AgentInteractionContextLoadOutcome>("\"Unknown\"").ShouldBe(AgentInteractionContextLoadOutcome.Unknown);
    }

    [Fact]
    public void Context_block_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionContextBlockReason.ExceedsModelBudget).ShouldBe("\"ExceedsModelBudget\"");
        default(AgentInteractionContextBlockReason).ShouldBe(AgentInteractionContextBlockReason.Unknown);
        JsonSerializer.Deserialize<AgentInteractionContextBlockReason>("\"Unknown\"").ShouldBe(AgentInteractionContextBlockReason.Unknown);
    }

    [Fact]
    public void Context_not_buildable_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentInteractionContextNotBuildableReason.InteractionNotAuthorized).ShouldBe("\"InteractionNotAuthorized\"");
        default(AgentInteractionContextNotBuildableReason).ShouldBe(AgentInteractionContextNotBuildableReason.Unknown);
        JsonSerializer.Deserialize<AgentInteractionContextNotBuildableReason>("\"Unknown\"").ShouldBe(AgentInteractionContextNotBuildableReason.Unknown);
    }

    // ===== AgentInteractionStatus additive extension (AD-2) =====

    [Fact]
    public void Interaction_status_context_states_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.ContextReady).ShouldBe("\"ContextReady\"");
        JsonSerializer.Serialize(AgentInteractionStatus.ContextBlocked).ShouldBe("\"ContextBlocked\"");
    }

    [Fact]
    public void Interaction_status_additive_extension_preserves_the_existing_ordinals()
    {
        // The Story 2.1/2.2 ordinals must be untouched so existing round-trip/replay stays green (AD-2 additive rule).
        ((int)AgentInteractionStatus.Unknown).ShouldBe(0);
        ((int)AgentInteractionStatus.Requested).ShouldBe(1);
        ((int)AgentInteractionStatus.Authorized).ShouldBe(2);
        ((int)AgentInteractionStatus.Denied).ShouldBe(3);
        ((int)AgentInteractionStatus.Blocked).ShouldBe(4);
        ((int)AgentInteractionStatus.ContextReady).ShouldBe(5);
        ((int)AgentInteractionStatus.ContextBlocked).ShouldBe(6);
        JsonSerializer.Deserialize<AgentInteractionStatus>("\"Blocked\"").ShouldBe(AgentInteractionStatus.Blocked);
        JsonSerializer.Deserialize<AgentInteractionStatus>("\"Unknown\"").ShouldBe(AgentInteractionStatus.Unknown);
    }

    // ===== Safe by construction: only safe numerics/enums/references; null view on failed inspection (AC1; AD-14) =====

    [Fact]
    public void Measurement_evidence_and_view_carry_only_safe_members()
    {
        // No raw content member (message text, prompt, claims, secret tokens, PartyId, provider payload) on any context
        // surface — only safe COUNTS (token/message), budget numerics, enums, and opaque references (AD-14).
        CarriesOnlySafeContextMembers(typeof(AgentInteractionContextMeasurement));
        CarriesOnlySafeContextMembers(typeof(AgentInteractionContextEvidence));
        CarriesOnlySafeContextMembers(typeof(AgentInteractionContextEvidenceView));
        CarriesOnlySafeContextMembers(typeof(AgentInteractionContextReady));
        CarriesOnlySafeContextMembers(typeof(AgentInteractionContextBlocked));
        CarriesOnlySafeContextMembers(typeof(AgentInteractionBoundedContextBehavior));
    }

    [Fact]
    public void Failed_inspection_result_carries_a_null_evidence_view()
    {
        // A failed inspection NEVER reveals whether the interaction exists in another tenant (AC1).
        AgentInteractionContextEvidenceResult.NotAuthorized().Evidence.ShouldBeNull();
        AgentInteractionContextEvidenceResult.NotAuthorized().Status.ShouldBe(AgentInteractionContextInspectionStatus.NotAuthorized);
        AgentInteractionContextEvidenceResult.NotFound().Evidence.ShouldBeNull();
        AgentInteractionContextEvidenceResult.NotFound().Status.ShouldBe(AgentInteractionContextInspectionStatus.NotFound);
    }

    [Fact]
    public void Failed_inspection_results_round_trip_with_a_null_evidence_view()
    {
        AgentInteractionContextEvidenceResult notAuthorized = RoundTrip(AgentInteractionContextEvidenceResult.NotAuthorized());
        notAuthorized.Status.ShouldBe(AgentInteractionContextInspectionStatus.NotAuthorized);
        notAuthorized.Evidence.ShouldBeNull();

        AgentInteractionContextEvidenceResult notFound = RoundTrip(AgentInteractionContextEvidenceResult.NotFound());
        notFound.Status.ShouldBe(AgentInteractionContextInspectionStatus.NotFound);
        notFound.Evidence.ShouldBeNull();
    }

    // Each property must be safe by name (no raw-content name) AND by type (a primitive/enum/reference inside the Agents
    // contracts namespace — never a Conversations or provider-SDK type). Exact-name matching avoids tripping the safe
    // count members (e.g. MessageCount, FullContextTokenCount, ContextWindowTokenLimit).
    private static void CarriesOnlySafeContextMembers(Type type)
    {
        string[] forbiddenExactNames =
            ["Text", "Prompt", "Content", "Message", "Body", "Claim", "Claims", "Secret", "Payload", "PartyId", "Personal", "DisplayName", "Contact"];

        foreach (PropertyInfo property in type.GetProperties())
        {
            forbiddenExactNames.ShouldNotContain(
                forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                $"{type.FullName}.{property.Name} exposes a raw-content member name on a context surface (AD-14).");

            Type underlying = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            string ns = underlying.Namespace ?? string.Empty;
            bool safe = underlying == typeof(string)
                || underlying == typeof(int)
                || underlying == typeof(bool)
                || ns.StartsWith("Hexalith.Agents.Contracts", StringComparison.Ordinal);
            safe.ShouldBeTrue(
                $"{type.FullName}.{property.Name} exposes a non-safe type '{underlying.FullName}' on a context surface (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
