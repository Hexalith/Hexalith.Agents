using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 3.4 Confirmation-mode proposal-regeneration surface (AC1–AC4; AD-2, AD-5, AD-13, AD-14).
/// Durable event sourcing replays every new event/rejection, so each must survive System.Text.Json; the new classification
/// enums must serialize by name and fail safe to their <c>Unknown</c> sentinel; the appended
/// <see cref="AgentGenerationKind.Regenerated"/>, <see cref="ProposedAgentReplyState.Regenerated"/>, and the two new
/// <see cref="AgentInteractionStatus"/> states must not perturb existing ordinals. Per the Content-confinement rule the
/// no-leak assertions apply to the evidence, failed event, rejection, and view — but NOT to
/// <see cref="ProposedAgentReplyRegenerated"/> / the regenerated version, which legitimately carry content (same as
/// <see cref="AgentOutputGenerated"/>). The assembly-wide <c>ContractsSecretNonDisclosureTests</c> additionally auto-covers
/// the new types.
/// </summary>
public sealed class AgentInteractionProposalRegenerationContractsTests
{
    // Regenerated content that legitimately rides ProposedAgentReplyRegenerated but must NEVER appear on the evidence /
    // failed event / rejection / view (AD-14).
    private const string RegeneratedContentText = "top-secret-regenerated-answer-do-not-leak";

    // A secret-looking token that must NEVER appear on any regeneration surface (AD-14).
    private const string SecretText = "sk-secret-credential-do-not-leak";

    private static AgentGeneratedVersion RegeneratedVersion { get; } = new(
        VersionId: "regenerated-version-1",
        AttemptId: "regeneration-attempt-1",
        AgentGenerationKind.Regenerated,
        RegeneratedContentText,
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 1,
        PromptTokenCount: 1200,
        OutputTokenCount: 350);

    private static AgentProposedReplyRegenerationEvidence RegenerationEvidence { get; } = new(
        ProposalId: "proposal-001",
        SourceConversationId: "conversation-001",
        RegeneratedVersionId: "regenerated-version-1",
        RegenerationAttemptId: "regeneration-attempt-1",
        RequesterPartyId: "requester-party-1",
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 1,
        ApproverPolicyVersion: 1,
        PolicyBasisVerdict: ApproverPolicyValidationStatus.Valid,
        DisclosureCategory: ApproverPolicyBasisDisclosure.OperatorOnly);

    private static AgentProposalRegenerationResult RegeneratedResult { get; } = new(
        AgentProposalRegenerationOutcome.Regenerated,
        RegenerationAttemptId: "regeneration-attempt-1",
        RegeneratedVersionId: "regenerated-version-1",
        RegeneratedVersion,
        ApproverPolicyValidationStatus.Valid,
        ProposalId: "proposal-001",
        SourceConversationId: "conversation-001",
        RequesterPartyId: "requester-party-1",
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 1,
        ApproverPolicyVersion: 1,
        ApproverPolicyBasisDisclosure.OperatorOnly,
        PromptTokenCount: 1200,
        OutputTokenCount: 350);

    private static AgentProposalRegenerationEvidenceView RegenerationView { get; } = new(
        "interaction-001",
        "proposal-001",
        ProposedAgentReplyState.Regenerated,
        "regenerated-version-1",
        "regeneration-attempt-1",
        "conversation-001",
        "requester-party-1",
        "openai",
        "gpt-4o",
        1,
        1,
        1,
        ApproverPolicyValidationStatus.Valid,
        ApproverPolicyBasisDisclosure.OperatorOnly,
        AgentProposalRegenerationFailureReason.Unknown,
        "2026-06-24T12:00:00Z");

    // ===== Marker interfaces =====

    [Fact]
    public void Regeneration_outcome_events_implement_IEventPayload_and_are_not_rejections()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyRegenerated)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyRegenerationFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyRegenerated)).ShouldBeFalse();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyRegenerationFailed)).ShouldBeFalse();
    }

    [Fact]
    public void Not_regeneratable_rejection_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyNotRegeneratableRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyNotRegeneratableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Regenerate_command_has_no_marker_interface_or_hexalith_attribute()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(RegenerateProposedAgentReply)).ShouldBeFalse();
        typeof(RegenerateProposedAgentReply)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Regeneration_evidence_round_trips()
        => RoundTrip(RegenerationEvidence).ShouldBe(RegenerationEvidence);

    [Fact]
    public void Regenerated_version_round_trips_with_its_kind()
    {
        AgentGeneratedVersion roundTripped = RoundTrip(RegeneratedVersion);
        roundTripped.ShouldBe(RegeneratedVersion);
        roundTripped.Kind.ShouldBe(AgentGenerationKind.Regenerated);
        roundTripped.SourceVersionId.ShouldBeNull(); // a fresh provider generation, not an edit
        roundTripped.EditorPartyId.ShouldBeNull();
    }

    [Fact]
    public void Regeneration_result_round_trips_with_its_regenerated_version()
    {
        AgentProposalRegenerationResult roundTripped = RoundTrip(RegeneratedResult);
        roundTripped.ShouldBe(RegeneratedResult);
        roundTripped.RegeneratedVersion!.GeneratedContent.ShouldBe(RegeneratedContentText); // content rides the write-path result
    }

    [Fact]
    public void A_failure_shaped_regeneration_result_round_trips_with_no_version_and_stays_content_free()
    {
        // The OTHER on-the-wire shape: a fail-closed result carrier (AC3) with NO content-bearing version. It rides inside the
        // dispatched RegenerateProposedAgentReply command exactly like the success shape, so it must survive the JSON round-trip,
        // and — unlike the success shape — it must carry neither a version nor any regenerated content (AD-14).
        AgentProposalRegenerationResult failure = RegeneratedResult with
        {
            Outcome = AgentProposalRegenerationOutcome.ProviderTimeout,
            RegeneratedVersion = null,
        };

        AgentProposalRegenerationResult roundTripped = RoundTrip(failure);
        roundTripped.ShouldBe(failure);
        roundTripped.RegeneratedVersion.ShouldBeNull();
        JsonSerializer.Serialize(failure).ShouldNotContain(RegeneratedContentText);
    }

    [Fact]
    public void Regenerate_command_round_trips_with_its_result()
    {
        var command = new RegenerateProposedAgentReply("interaction-001", RegeneratedResult);
        RegenerateProposedAgentReply roundTripped = RoundTrip(command);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Result.ShouldBe(RegeneratedResult);
    }

    [Fact]
    public void Regenerated_event_round_trips_with_its_version_and_evidence()
    {
        var regenerated = new ProposedAgentReplyRegenerated("interaction-001", RegeneratedVersion, RegenerationEvidence);
        ProposedAgentReplyRegenerated roundTripped = RoundTrip(regenerated);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.RegeneratedVersion.ShouldBe(RegeneratedVersion);
        roundTripped.Evidence.ShouldBe(RegenerationEvidence);
    }

    [Fact]
    public void Regeneration_failed_event_round_trips_with_its_reason_and_evidence()
    {
        var failed = new ProposedAgentReplyRegenerationFailed("interaction-001", AgentProposalRegenerationFailureReason.ProviderTimeout, RegenerationEvidence);
        ProposedAgentReplyRegenerationFailed roundTripped = RoundTrip(failed);
        roundTripped.Reason.ShouldBe(AgentProposalRegenerationFailureReason.ProviderTimeout);
        roundTripped.Evidence.ShouldBe(RegenerationEvidence);
    }

    [Fact]
    public void Not_regeneratable_rejection_round_trips_with_the_enum_reason()
    {
        var rejection = new ProposedAgentReplyNotRegeneratableRejection("interaction-001", AgentProposedReplyNotRegeneratableReason.ProposalNotPending);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    [Fact]
    public void Regeneration_evidence_view_round_trips()
        => RoundTrip(RegenerationView).ShouldBe(RegenerationView);

    // ===== AC4 audit read-result: success carries the view, fail-closed factories reveal nothing (AD-12) =====

    [Fact]
    public void Regeneration_evidence_result_success_carries_the_safe_view()
    {
        AgentProposalRegenerationEvidenceResult result = AgentProposalRegenerationEvidenceResult.Success(RegenerationView);
        result.Status.ShouldBe(AgentInteractionInspectionStatus.Success);
        result.Evidence.ShouldBe(RegenerationView);
    }

    [Fact]
    public void Regeneration_evidence_result_not_authorized_is_fail_closed_with_no_evidence()
    {
        AgentProposalRegenerationEvidenceResult result = AgentProposalRegenerationEvidenceResult.NotAuthorized();
        result.Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
        result.Evidence.ShouldBeNull();
    }

    [Fact]
    public void Regeneration_evidence_result_not_found_reveals_no_cross_tenant_existence()
    {
        AgentProposalRegenerationEvidenceResult result = AgentProposalRegenerationEvidenceResult.NotFound();
        result.Status.ShouldBe(AgentInteractionInspectionStatus.NotFound);
        result.Evidence.ShouldBeNull();
    }

    [Fact]
    public void Regeneration_evidence_result_round_trips_with_its_view()
        => RoundTrip(AgentProposalRegenerationEvidenceResult.Success(RegenerationView)).ShouldBe(AgentProposalRegenerationEvidenceResult.Success(RegenerationView));

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Generation_kind_regenerated_serializes_by_name()
        => JsonSerializer.Serialize(AgentGenerationKind.Regenerated).ShouldBe("\"Regenerated\"");

    [Fact]
    public void Proposal_state_regenerated_serializes_by_name()
        => JsonSerializer.Serialize(ProposedAgentReplyState.Regenerated).ShouldBe("\"Regenerated\"");

    [Fact]
    public void Regeneration_outcome_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposalRegenerationOutcome.Regenerated).ShouldBe("\"Regenerated\"");
        default(AgentProposalRegenerationOutcome).ShouldBe(AgentProposalRegenerationOutcome.Unknown);
        JsonSerializer.Deserialize<AgentProposalRegenerationOutcome>("\"Unknown\"").ShouldBe(AgentProposalRegenerationOutcome.Unknown);
    }

    [Fact]
    public void Regeneration_failure_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposalRegenerationFailureReason.ContentSafetyBlocked).ShouldBe("\"ContentSafetyBlocked\"");
        default(AgentProposalRegenerationFailureReason).ShouldBe(AgentProposalRegenerationFailureReason.Unknown);
        JsonSerializer.Deserialize<AgentProposalRegenerationFailureReason>("\"Unknown\"").ShouldBe(AgentProposalRegenerationFailureReason.Unknown);
    }

    [Fact]
    public void Not_regeneratable_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposedReplyNotRegeneratableReason.ProposalNotPending).ShouldBe("\"ProposalNotPending\"");
        default(AgentProposedReplyNotRegeneratableReason).ShouldBe(AgentProposedReplyNotRegeneratableReason.Unknown);
        JsonSerializer.Deserialize<AgentProposedReplyNotRegeneratableReason>("\"Unknown\"").ShouldBe(AgentProposedReplyNotRegeneratableReason.Unknown);
    }

    [Fact]
    public void Interaction_status_regeneration_states_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.ProposalRegenerated).ShouldBe("\"ProposalRegenerated\"");
        JsonSerializer.Serialize(AgentInteractionStatus.ProposalRegenerationFailed).ShouldBe("\"ProposalRegenerationFailed\"");
    }

    // ===== Additive ordinal stability (AD-2 — append-only) =====

    [Fact]
    public void Generation_kind_additive_extension_appends_regenerated_at_three()
    {
        ((int)AgentGenerationKind.Generated).ShouldBe(1);
        ((int)AgentGenerationKind.Edited).ShouldBe(2);
        ((int)AgentGenerationKind.Regenerated).ShouldBe(3);
    }

    [Fact]
    public void Proposal_state_additive_extension_appends_regenerated_at_three()
    {
        ((int)ProposedAgentReplyState.Pending).ShouldBe(1);
        ((int)ProposedAgentReplyState.Edited).ShouldBe(2);
        ((int)ProposedAgentReplyState.Regenerated).ShouldBe(3);
    }

    [Fact]
    public void Interaction_status_additive_extension_preserves_existing_ordinals_and_adds_sixteen_to_seventeen()
    {
        // The Story 2.1-3.3 ordinals (0-15) must be untouched (AD-2 additive rule); Story 3.4 appends 16-17.
        ((int)AgentInteractionStatus.ProposalEdited).ShouldBe(14);
        ((int)AgentInteractionStatus.ProposalEditFailed).ShouldBe(15);
        ((int)AgentInteractionStatus.ProposalRegenerated).ShouldBe(16);
        ((int)AgentInteractionStatus.ProposalRegenerationFailed).ShouldBe(17);
    }

    // ===== AD-14: the SAFE regeneration surfaces are structurally content-free (but the event/version carry content) =====

    [Fact]
    public void The_safe_regeneration_surfaces_never_carry_regenerated_content_or_a_secret()
    {
        var failed = new ProposedAgentReplyRegenerationFailed("interaction-001", AgentProposalRegenerationFailureReason.AdapterFailure, RegenerationEvidence);
        var rejection = new ProposedAgentReplyNotRegeneratableRejection("interaction-001", AgentProposedReplyNotRegeneratableReason.NotAuthorized);

        foreach (string json in new[]
        {
            JsonSerializer.Serialize(failed),
            JsonSerializer.Serialize(rejection),
            JsonSerializer.Serialize(RegenerationEvidence),
            JsonSerializer.Serialize(RegenerationView),
        })
        {
            json.ShouldNotContain(RegeneratedContentText);
            json.ShouldNotContain(SecretText);
        }
    }

    [Fact]
    public void The_regenerated_event_and_version_legitimately_carry_the_regenerated_content()
    {
        // The write-path regeneration event IS the content's durable home (like AgentOutputGenerated) — asserting it is
        // content-free would be a mistake (see Dev Notes "Content confinement"). Assert the content survives the round-trip.
        var regenerated = new ProposedAgentReplyRegenerated("interaction-001", RegeneratedVersion, RegenerationEvidence);
        JsonSerializer.Serialize(regenerated).ShouldContain(RegeneratedContentText);
        RoundTrip(regenerated).RegeneratedVersion.GeneratedContent.ShouldBe(RegeneratedContentText);
    }

    [Fact]
    public void The_safe_regeneration_surfaces_have_no_content_bearing_member()
    {
        // No "GeneratedContent"/"Content"/"Text"/… member on the failed event, evidence, rejection, or view — only safe ids
        // + classifications (AD-14). The regenerated event/version/result legitimately carry content and are excluded.
        CarriesNoContentMember(typeof(ProposedAgentReplyRegenerationFailed));
        CarriesNoContentMember(typeof(ProposedAgentReplyNotRegeneratableRejection));
        CarriesNoContentMember(typeof(AgentProposedReplyRegenerationEvidence));
        CarriesNoContentMember(typeof(AgentProposalRegenerationEvidenceView));
    }

    private static void CarriesNoContentMember(Type type)
    {
        string[] forbiddenExactNames =
            ["GeneratedContent", "Content", "Text", "Prompt", "Body", "Claim", "Claims", "Payload", "Message"];

        foreach (PropertyInfo property in type.GetProperties())
        {
            forbiddenExactNames.ShouldNotContain(
                forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                $"{type.FullName}.{property.Name} exposes a content-bearing member on a safe regeneration surface (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
