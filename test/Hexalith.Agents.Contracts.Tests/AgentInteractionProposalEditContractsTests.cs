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
/// Contract guard for the Story 3.3 Confirmation-mode proposal-edit surface (AC1–AC4; AD-2, AD-5, AD-13, AD-14). Durable
/// event sourcing replays every new event/rejection, so each must survive System.Text.Json; the new classification enums
/// must serialize by name and fail safe to their <c>Unknown</c> sentinel; the appended <see cref="AgentGenerationKind.Edited"/>,
/// <see cref="ProposedAgentReplyState.Edited"/>, and the two new <see cref="AgentInteractionStatus"/> states must not perturb
/// existing ordinals; the new nullable <see cref="AgentGeneratedVersion"/> fields are backward-compatible (absent ⇒ null).
/// Per the Content-confinement rule the no-leak assertions apply to the evidence, failed event, rejection, and view — but NOT
/// to <see cref="ProposedAgentReplyEdited"/> / the edited version, which legitimately carry content (same as
/// <see cref="AgentOutputGenerated"/>). The assembly-wide <c>ContractsSecretNonDisclosureTests</c> additionally auto-covers the new types.
/// </summary>
public sealed class AgentInteractionProposalEditContractsTests
{
    // An edited-content sample string that legitimately rides ProposedAgentReplyEdited but must NEVER appear on the
    // evidence / failed event / rejection / view (AD-14).
    private const string EditedContentText = "top-secret-edited-answer-do-not-leak";

    // A secret-looking token that must NEVER appear on any edit surface (AD-14).
    private const string SecretText = "sk-secret-credential-do-not-leak";

    private static AgentGeneratedVersion EditedVersion { get; } = new(
        VersionId: "edited-version-1",
        AttemptId: "edit-attempt-1",
        AgentGenerationKind.Edited,
        EditedContentText,
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 1,
        PromptTokenCount: 0,
        OutputTokenCount: 0,
        SourceVersionId: "version-attempt-1",
        EditorPartyId: "editor-party-1");

    private static AgentProposedReplyEditEvidence EditEvidence { get; } = new(
        ProposalId: "proposal-001",
        SourceConversationId: "conversation-001",
        EditedVersionId: "edited-version-1",
        SourceVersionId: "version-attempt-1",
        EditorPartyId: "editor-party-1",
        ApproverPolicyVersion: 1,
        PolicyBasisVerdict: ApproverPolicyValidationStatus.Valid,
        DisclosureCategory: ApproverPolicyBasisDisclosure.OperatorOnly);

    private static AgentProposalEditResult EditedResult { get; } = new(
        AgentProposalEditOutcome.Edited,
        EditedVersion,
        ApproverPolicyValidationStatus.Valid,
        ProposalId: "proposal-001",
        SourceConversationId: "conversation-001",
        ApproverPolicyVersion: 1,
        DisclosureCategory: ApproverPolicyBasisDisclosure.OperatorOnly);

    private static AgentProposalEditEvidenceView EditView { get; } = new(
        "interaction-001",
        "proposal-001",
        ProposedAgentReplyState.Edited,
        "edited-version-1",
        "version-attempt-1",
        "editor-party-1",
        1,
        ApproverPolicyValidationStatus.Valid,
        ApproverPolicyBasisDisclosure.OperatorOnly,
        "2026-06-24T12:00:00Z");

    // ===== Marker interfaces =====

    [Fact]
    public void Edit_outcome_events_implement_IEventPayload_and_are_not_rejections()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyEdited)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyEditFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyEdited)).ShouldBeFalse();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyEditFailed)).ShouldBeFalse();
    }

    [Fact]
    public void Not_editable_rejection_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyNotEditableRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyNotEditableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Edit_command_has_no_marker_interface_or_hexalith_attribute()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(EditProposedAgentReply)).ShouldBeFalse();
        typeof(EditProposedAgentReply)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Edit_evidence_round_trips()
        => RoundTrip(EditEvidence).ShouldBe(EditEvidence);

    [Fact]
    public void Edited_version_round_trips_with_its_provenance_fields()
    {
        AgentGeneratedVersion roundTripped = RoundTrip(EditedVersion);
        roundTripped.ShouldBe(EditedVersion);
        roundTripped.Kind.ShouldBe(AgentGenerationKind.Edited);
        roundTripped.SourceVersionId.ShouldBe("version-attempt-1");
        roundTripped.EditorPartyId.ShouldBe("editor-party-1");
    }

    [Fact]
    public void Edit_result_round_trips_with_its_edited_version()
    {
        AgentProposalEditResult roundTripped = RoundTrip(EditedResult);
        roundTripped.ShouldBe(EditedResult);
        roundTripped.EditedVersion.GeneratedContent.ShouldBe(EditedContentText); // content rides the write-path result
    }

    [Fact]
    public void Edit_command_round_trips_with_its_result()
    {
        var command = new EditProposedAgentReply("interaction-001", EditedResult);
        EditProposedAgentReply roundTripped = RoundTrip(command);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Result.ShouldBe(EditedResult);
    }

    [Fact]
    public void Edited_event_round_trips_with_its_version_and_evidence()
    {
        var edited = new ProposedAgentReplyEdited("interaction-001", EditedVersion, EditEvidence);
        ProposedAgentReplyEdited roundTripped = RoundTrip(edited);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.EditedVersion.ShouldBe(EditedVersion);
        roundTripped.Evidence.ShouldBe(EditEvidence);
    }

    [Fact]
    public void Edit_failed_event_round_trips_with_its_reason_and_evidence()
    {
        var failed = new ProposedAgentReplyEditFailed("interaction-001", AgentProposalEditFailureReason.NotAuthorized, EditEvidence);
        ProposedAgentReplyEditFailed roundTripped = RoundTrip(failed);
        roundTripped.Reason.ShouldBe(AgentProposalEditFailureReason.NotAuthorized);
        roundTripped.Evidence.ShouldBe(EditEvidence);
    }

    [Fact]
    public void Not_editable_rejection_round_trips_with_the_enum_reason()
    {
        var rejection = new ProposedAgentReplyNotEditableRejection("interaction-001", AgentProposedReplyNotEditableReason.ProposalNotPending);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    [Fact]
    public void Edit_evidence_view_round_trips()
    {
        var view = new AgentProposalEditEvidenceView(
            "interaction-001",
            "proposal-001",
            ProposedAgentReplyState.Edited,
            "edited-version-1",
            "version-attempt-1",
            "editor-party-1",
            1,
            ApproverPolicyValidationStatus.Valid,
            ApproverPolicyBasisDisclosure.OperatorOnly,
            "2026-06-24T12:00:00Z");
        RoundTrip(view).ShouldBe(view);
    }

    // ===== AC4 audit read-result: success carries the view, fail-closed factories reveal nothing (AD-12) =====

    [Fact]
    public void Edit_evidence_result_success_carries_the_safe_view()
    {
        AgentProposalEditEvidenceResult result = AgentProposalEditEvidenceResult.Success(EditView);
        result.Status.ShouldBe(AgentInteractionInspectionStatus.Success);
        result.Evidence.ShouldBe(EditView);
    }

    [Fact]
    public void Edit_evidence_result_not_authorized_is_fail_closed_with_no_evidence()
    {
        // A denied edit-evidence inspection carries the NotAuthorized status and NO view, so it never leaks the edit record
        // or its provenance to an unauthorized caller (AD-12, AD-14).
        AgentProposalEditEvidenceResult result = AgentProposalEditEvidenceResult.NotAuthorized();
        result.Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
        result.Evidence.ShouldBeNull();
    }

    [Fact]
    public void Edit_evidence_result_not_found_reveals_no_cross_tenant_existence()
    {
        // NotFound also carries no view, so a failed inspection never reveals whether the proposal exists in another tenant.
        AgentProposalEditEvidenceResult result = AgentProposalEditEvidenceResult.NotFound();
        result.Status.ShouldBe(AgentInteractionInspectionStatus.NotFound);
        result.Evidence.ShouldBeNull();
    }

    [Fact]
    public void Edit_evidence_result_round_trips_with_its_view()
        => RoundTrip(AgentProposalEditEvidenceResult.Success(EditView)).ShouldBe(AgentProposalEditEvidenceResult.Success(EditView));

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Generation_kind_edited_serializes_by_name()
        => JsonSerializer.Serialize(AgentGenerationKind.Edited).ShouldBe("\"Edited\"");

    [Fact]
    public void Proposal_state_edited_serializes_by_name()
        => JsonSerializer.Serialize(ProposedAgentReplyState.Edited).ShouldBe("\"Edited\"");

    [Fact]
    public void Edit_outcome_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposalEditOutcome.Edited).ShouldBe("\"Edited\"");
        default(AgentProposalEditOutcome).ShouldBe(AgentProposalEditOutcome.Unknown);
        JsonSerializer.Deserialize<AgentProposalEditOutcome>("\"Unknown\"").ShouldBe(AgentProposalEditOutcome.Unknown);
    }

    [Fact]
    public void Edit_failure_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposalEditFailureReason.AdapterFailure).ShouldBe("\"AdapterFailure\"");
        default(AgentProposalEditFailureReason).ShouldBe(AgentProposalEditFailureReason.Unknown);
        JsonSerializer.Deserialize<AgentProposalEditFailureReason>("\"Unknown\"").ShouldBe(AgentProposalEditFailureReason.Unknown);
    }

    [Fact]
    public void Not_editable_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposedReplyNotEditableReason.ProposalNotPending).ShouldBe("\"ProposalNotPending\"");
        default(AgentProposedReplyNotEditableReason).ShouldBe(AgentProposedReplyNotEditableReason.Unknown);
        JsonSerializer.Deserialize<AgentProposedReplyNotEditableReason>("\"Unknown\"").ShouldBe(AgentProposedReplyNotEditableReason.Unknown);
    }

    [Fact]
    public void Interaction_status_edit_states_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.ProposalEdited).ShouldBe("\"ProposalEdited\"");
        JsonSerializer.Serialize(AgentInteractionStatus.ProposalEditFailed).ShouldBe("\"ProposalEditFailed\"");
    }

    // ===== Additive ordinal stability (AD-2 — append-only) =====

    [Fact]
    public void Generation_kind_additive_extension_preserves_generated_and_appends_edited_at_two()
    {
        ((int)AgentGenerationKind.Unknown).ShouldBe(0);
        ((int)AgentGenerationKind.Generated).ShouldBe(1);
        ((int)AgentGenerationKind.Edited).ShouldBe(2);
    }

    [Fact]
    public void Proposal_state_additive_extension_preserves_pending_and_appends_edited_at_two()
    {
        ((int)ProposedAgentReplyState.Unknown).ShouldBe(0);
        ((int)ProposedAgentReplyState.Pending).ShouldBe(1);
        ((int)ProposedAgentReplyState.Edited).ShouldBe(2);
    }

    [Fact]
    public void Interaction_status_additive_extension_preserves_existing_ordinals_and_adds_fourteen_to_fifteen()
    {
        // The Story 2.1-3.1 ordinals (0-13) must be untouched (AD-2 additive rule); Story 3.3 appends 14-15.
        ((int)AgentInteractionStatus.ProposalCreated).ShouldBe(12);
        ((int)AgentInteractionStatus.ProposalCreationFailed).ShouldBe(13);
        ((int)AgentInteractionStatus.ProposalEdited).ShouldBe(14);
        ((int)AgentInteractionStatus.ProposalEditFailed).ShouldBe(15);
    }

    // ===== Backward-compat of the new nullable AgentGeneratedVersion fields (absent ⇒ null) =====

    [Fact]
    public void A_generated_version_omits_the_edit_provenance_fields_as_null()
    {
        var generated = new AgentGeneratedVersion(
            "version-1", "attempt-1", AgentGenerationKind.Generated, "content", "openai", "gpt-4o", 1, 1, 100, 50);
        generated.SourceVersionId.ShouldBeNull();
        generated.EditorPartyId.ShouldBeNull();
        AgentGeneratedVersion roundTripped = RoundTrip(generated);
        roundTripped.SourceVersionId.ShouldBeNull();
        roundTripped.EditorPartyId.ShouldBeNull();
    }

    [Fact]
    public void A_pre_3_3_version_json_without_the_new_fields_deserializes_them_to_null()
    {
        // A persisted Story 2.4 version JSON has no SourceVersionId/EditorPartyId — they must deserialize to null (additive).
        const string legacyJson =
            """{"VersionId":"v1","AttemptId":"a1","Kind":"Generated","GeneratedContent":"c","ProviderId":"openai","ModelId":"gpt-4o","ProviderCapabilityVersion":1,"ContentSafetyPolicyVersion":1,"PromptTokenCount":10,"OutputTokenCount":5}""";
        AgentGeneratedVersion version = JsonSerializer.Deserialize<AgentGeneratedVersion>(legacyJson)!;
        version.SourceVersionId.ShouldBeNull();
        version.EditorPartyId.ShouldBeNull();
        version.Kind.ShouldBe(AgentGenerationKind.Generated);
    }

    // ===== AD-14: the SAFE edit surfaces are structurally content-free (but the edit EVENT/version carry content) =====

    [Fact]
    public void The_safe_edit_surfaces_never_carry_edited_content_or_a_secret()
    {
        var failed = new ProposedAgentReplyEditFailed("interaction-001", AgentProposalEditFailureReason.AdapterFailure, EditEvidence);
        var rejection = new ProposedAgentReplyNotEditableRejection("interaction-001", AgentProposedReplyNotEditableReason.NotAuthorized);
        var view = new AgentProposalEditEvidenceView(
            "interaction-001", "proposal-001", ProposedAgentReplyState.Edited, "edited-version-1", "version-attempt-1",
            "editor-party-1", 1, ApproverPolicyValidationStatus.Valid, ApproverPolicyBasisDisclosure.OperatorOnly, null);

        foreach (string json in new[]
        {
            JsonSerializer.Serialize(failed),
            JsonSerializer.Serialize(rejection),
            JsonSerializer.Serialize(EditEvidence),
            JsonSerializer.Serialize(view),
        })
        {
            json.ShouldNotContain(EditedContentText);
            json.ShouldNotContain(SecretText);
        }
    }

    [Fact]
    public void The_edited_event_and_version_legitimately_carry_the_edited_content()
    {
        // The write-path edit event IS the content's durable home (like AgentOutputGenerated) — asserting it is content-free
        // would be a mistake (see Dev Notes "Content confinement"). Assert the opposite: the content survives the round-trip.
        var edited = new ProposedAgentReplyEdited("interaction-001", EditedVersion, EditEvidence);
        JsonSerializer.Serialize(edited).ShouldContain(EditedContentText);
        RoundTrip(edited).EditedVersion.GeneratedContent.ShouldBe(EditedContentText);
    }

    [Fact]
    public void The_safe_edit_surfaces_have_no_content_bearing_member()
    {
        // No "GeneratedContent"/"Content"/"Text"/… member on the failed event, evidence, rejection, or view — only safe ids
        // + classifications (AD-14). The edited event/version/result legitimately carry content and are intentionally excluded.
        CarriesNoContentMember(typeof(ProposedAgentReplyEditFailed));
        CarriesNoContentMember(typeof(ProposedAgentReplyNotEditableRejection));
        CarriesNoContentMember(typeof(AgentProposedReplyEditEvidence));
        CarriesNoContentMember(typeof(AgentProposalEditEvidenceView));
    }

    private static void CarriesNoContentMember(Type type)
    {
        string[] forbiddenExactNames =
            ["GeneratedContent", "Content", "Text", "Prompt", "Body", "Claim", "Claims", "Payload", "Message"];

        foreach (PropertyInfo property in type.GetProperties())
        {
            forbiddenExactNames.ShouldNotContain(
                forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                $"{type.FullName}.{property.Name} exposes a content-bearing member on a safe edit surface (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
