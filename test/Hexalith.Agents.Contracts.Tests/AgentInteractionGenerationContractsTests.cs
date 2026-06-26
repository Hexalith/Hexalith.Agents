using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 2.4 Agent-output generation surface (AC1–AC4; AD-2, AD-5, AD-9, AD-14). Durable event
/// sourcing replays every new event/rejection, so each must survive System.Text.Json; the new classification enums must
/// serialize by name and fail safe to their <c>Unknown</c> sentinel; the additive <see cref="AgentInteractionStatus"/>
/// extension must not perturb the existing 0-6 ordinals and must add 7-9; the generated content lives ONLY on the success
/// version/event (never on the failure event, the attempt evidence, or the rejection — AD-14). The assembly-wide
/// <c>ContractsSecretNonDisclosureTests</c> additionally auto-covers the new types.
/// </summary>
public sealed class AgentInteractionGenerationContractsTests
{
    private const string GeneratedContentText = "top-secret-generated-answer-do-not-leak";

    private static AgentGeneratedVersion Version { get; } = new(
        VersionId: "version-attempt-1",
        AttemptId: "attempt-1",
        AgentGenerationKind.Generated,
        GeneratedContentText,
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 2,
        PromptTokenCount: 1_200,
        OutputTokenCount: 350);

    private static AgentGenerationAttemptEvidence AttemptEvidence { get; } = new(
        AttemptId: "attempt-1",
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        PromptTokenCount: 1_200,
        OutputTokenCount: 350);

    private static AgentOutputGenerationResult SuccessResult { get; } = new(
        AgentGenerationOutcome.Succeeded,
        AttemptId: "attempt-1",
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 2,
        GeneratedContent: GeneratedContentText,
        PromptTokenCount: 1_200,
        OutputTokenCount: 350);

    // ===== Marker interfaces =====

    [Fact]
    public void Generation_outcome_events_implement_IEventPayload_and_are_not_rejections()
    {
        // AgentOutputGenerated/Failed are recorded SUCCESS outcomes (Audit Evidence), NOT rejections (AD-2; FR-24).
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentOutputGenerated)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentOutputGenerationFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentOutputGenerated)).ShouldBeFalse();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentOutputGenerationFailed)).ShouldBeFalse();
    }

    [Fact]
    public void Not_generatable_rejection_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentOutputNotGeneratableRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentOutputNotGeneratableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Generate_command_has_no_marker_interface_or_hexalith_attribute()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(GenerateAgentOutput)).ShouldBeFalse();
        // Scope attribute-absence to Hexalith-namespaced attributes: the compiler emits Nullable* attributes on records
        // with nullable members (Story 2.1 learning), which are not a contract concern.
        typeof(GenerateAgentOutput)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Generated_version_round_trips_with_its_content_and_usage()
    {
        AgentGeneratedVersion roundTripped = RoundTrip(Version);
        roundTripped.ShouldBe(Version);
        roundTripped.GeneratedContent.ShouldBe(GeneratedContentText);
        roundTripped.Kind.ShouldBe(AgentGenerationKind.Generated);
    }

    [Fact]
    public void Attempt_evidence_round_trips()
        => RoundTrip(AttemptEvidence).ShouldBe(AttemptEvidence);

    [Fact]
    public void Generation_result_round_trips_for_success_and_failure()
    {
        RoundTrip(SuccessResult).ShouldBe(SuccessResult);
        var failure = SuccessResult with { Outcome = AgentGenerationOutcome.ContentSafetyBlocked, GeneratedContent = null };
        RoundTrip(failure).ShouldBe(failure);
    }

    [Fact]
    public void Generate_command_round_trips_with_its_result()
    {
        var command = new GenerateAgentOutput("interaction-001", SuccessResult);
        GenerateAgentOutput roundTripped = RoundTrip(command);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Result.ShouldBe(SuccessResult);
    }

    [Fact]
    public void Generated_event_round_trips_with_its_version()
    {
        var generated = new AgentOutputGenerated("interaction-001", Version);
        AgentOutputGenerated roundTripped = RoundTrip(generated);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Version.ShouldBe(Version);
    }

    [Fact]
    public void Generation_failed_event_round_trips_with_its_decision_reason_and_evidence()
    {
        var failed = new AgentOutputGenerationFailed(
            "interaction-001",
            AgentInteractionStatus.SafetyFailed,
            AgentOutputGenerationFailureReason.ContentSafetyBlocked,
            AttemptEvidence);
        AgentOutputGenerationFailed roundTripped = RoundTrip(failed);
        roundTripped.Decision.ShouldBe(AgentInteractionStatus.SafetyFailed);
        roundTripped.Reason.ShouldBe(AgentOutputGenerationFailureReason.ContentSafetyBlocked);
        roundTripped.Evidence.ShouldBe(AttemptEvidence);
    }

    [Fact]
    public void Generation_failed_event_round_trips_with_the_generation_failed_decision()
    {
        // The other recorded-negative decision (provider/adapter/context/policy failures all map here) must also survive
        // replay by name, completing the Decision matrix alongside the SafetyFailed case (AD-2).
        var failed = new AgentOutputGenerationFailed(
            "interaction-001",
            AgentInteractionStatus.GenerationFailed,
            AgentOutputGenerationFailureReason.ProviderTimeout,
            AttemptEvidence);
        AgentOutputGenerationFailed roundTripped = RoundTrip(failed);
        roundTripped.Decision.ShouldBe(AgentInteractionStatus.GenerationFailed);
        roundTripped.Reason.ShouldBe(AgentOutputGenerationFailureReason.ProviderTimeout);
    }

    [Fact]
    public void Not_generatable_rejection_round_trips_with_the_enum_reason()
    {
        var rejection = new AgentOutputNotGeneratableRejection("interaction-001", AgentOutputNotGeneratableReason.ContextNotReady);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Generation_outcome_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentGenerationOutcome.Succeeded).ShouldBe("\"Succeeded\"");
        default(AgentGenerationOutcome).ShouldBe(AgentGenerationOutcome.Unknown);
        JsonSerializer.Deserialize<AgentGenerationOutcome>("\"Unknown\"").ShouldBe(AgentGenerationOutcome.Unknown);
    }

    [Fact]
    public void Generation_failure_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentOutputGenerationFailureReason.ContentSafetyBlocked).ShouldBe("\"ContentSafetyBlocked\"");
        default(AgentOutputGenerationFailureReason).ShouldBe(AgentOutputGenerationFailureReason.Unknown);
        JsonSerializer.Deserialize<AgentOutputGenerationFailureReason>("\"Unknown\"").ShouldBe(AgentOutputGenerationFailureReason.Unknown);
    }

    [Fact]
    public void Generation_kind_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentGenerationKind.Generated).ShouldBe("\"Generated\"");
        default(AgentGenerationKind).ShouldBe(AgentGenerationKind.Unknown);
        JsonSerializer.Deserialize<AgentGenerationKind>("\"Unknown\"").ShouldBe(AgentGenerationKind.Unknown);
    }

    [Fact]
    public void Not_generatable_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentOutputNotGeneratableReason.ContextNotReady).ShouldBe("\"ContextNotReady\"");
        default(AgentOutputNotGeneratableReason).ShouldBe(AgentOutputNotGeneratableReason.Unknown);
        JsonSerializer.Deserialize<AgentOutputNotGeneratableReason>("\"Unknown\"").ShouldBe(AgentOutputNotGeneratableReason.Unknown);
    }

    // ===== AgentInteractionStatus additive extension (AD-2) =====

    [Fact]
    public void Interaction_status_generation_states_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.Generated).ShouldBe("\"Generated\"");
        JsonSerializer.Serialize(AgentInteractionStatus.GenerationFailed).ShouldBe("\"GenerationFailed\"");
        JsonSerializer.Serialize(AgentInteractionStatus.SafetyFailed).ShouldBe("\"SafetyFailed\"");
    }

    [Fact]
    public void Interaction_status_additive_extension_preserves_existing_ordinals_and_adds_seven_to_nine()
    {
        // The Story 2.1-2.3 ordinals (0-6) must be untouched so existing round-trip/replay stays green (AD-2 additive rule),
        // and the Story 2.4 states must be appended at 7-9.
        ((int)AgentInteractionStatus.Unknown).ShouldBe(0);
        ((int)AgentInteractionStatus.Requested).ShouldBe(1);
        ((int)AgentInteractionStatus.Authorized).ShouldBe(2);
        ((int)AgentInteractionStatus.Denied).ShouldBe(3);
        ((int)AgentInteractionStatus.Blocked).ShouldBe(4);
        ((int)AgentInteractionStatus.ContextReady).ShouldBe(5);
        ((int)AgentInteractionStatus.ContextBlocked).ShouldBe(6);
        ((int)AgentInteractionStatus.Generated).ShouldBe(7);
        ((int)AgentInteractionStatus.GenerationFailed).ShouldBe(8);
        ((int)AgentInteractionStatus.SafetyFailed).ShouldBe(9);
    }

    // ===== AD-14: generated content lives ONLY on the success version/event =====

    [Fact]
    public void The_failure_event_evidence_and_rejection_never_carry_generated_content()
    {
        var failed = new AgentOutputGenerationFailed(
            "interaction-001",
            AgentInteractionStatus.SafetyFailed,
            AgentOutputGenerationFailureReason.ContentSafetyBlocked,
            AttemptEvidence);
        var rejection = new AgentOutputNotGeneratableRejection("interaction-001", AgentOutputNotGeneratableReason.ContextNotReady);

        // The recorded-negative surfaces are structurally content-free, and a serialized round-trip confirms it (AD-5, AD-14).
        JsonSerializer.Serialize(failed).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(AttemptEvidence).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(rejection).ShouldNotContain(GeneratedContentText);

        // ... whereas the success version/event IS the content's home (the only place it may appear; AD-14).
        JsonSerializer.Serialize(new AgentOutputGenerated("interaction-001", Version)).ShouldContain(GeneratedContentText);
    }

    [Fact]
    public void The_recorded_negative_surfaces_have_no_content_bearing_member()
    {
        // No "GeneratedContent"/"Content"/"Text"/"Prompt" member on the failure event, attempt evidence, or rejection.
        CarriesNoContentMember(typeof(AgentOutputGenerationFailed));
        CarriesNoContentMember(typeof(AgentGenerationAttemptEvidence));
        CarriesNoContentMember(typeof(AgentOutputNotGeneratableRejection));
    }

    private static void CarriesNoContentMember(Type type)
    {
        // Exact-name matching so the safe count members (PromptTokenCount, OutputTokenCount) are not tripped — only a
        // content-bearing member name is forbidden on a recorded-negative surface (AD-14).
        string[] forbiddenExactNames =
            ["GeneratedContent", "Content", "Text", "Prompt", "Body", "Claim", "Claims", "Payload", "Message"];

        foreach (PropertyInfo property in type.GetProperties())
        {
            forbiddenExactNames.ShouldNotContain(
                forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                $"{type.FullName}.{property.Name} exposes a content-bearing member on a recorded-negative surface (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
