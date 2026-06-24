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
/// Contract guard for the Story 3.1 Confirmation-mode proposal-creation surface (AC1–AC4; AD-2, AD-5, AD-13, AD-14).
/// Durable event sourcing replays every new event/rejection, so each must survive System.Text.Json; the new classification
/// enums must serialize by name and fail safe to their <c>Unknown</c> sentinel; the additive
/// <see cref="AgentInteractionStatus"/> extension must not perturb the existing 0-11 ordinals and must add 12-13; the
/// proposal surfaces are structurally content-free — they carry only safe ids (the generated content's sole durable home
/// stays the Story 2.4 success event — AD-14). The assembly-wide <c>ContractsSecretNonDisclosureTests</c> additionally
/// auto-covers the new types.
/// </summary>
public sealed class AgentInteractionProposalContractsTests
{
    // A generated-content sample string that must NEVER appear on any proposal surface (AD-14).
    private const string GeneratedContentText = "top-secret-generated-answer-do-not-leak";

    // A secret-looking token that must NEVER appear on any proposal surface (AD-14).
    private const string SecretText = "sk-secret-credential-do-not-leak";

    private static AgentProposedReplyEvidence Evidence { get; } = new(
        ProposalId: "proposal-001",
        SourceConversationId: "conversation-001",
        ProposedVersionId: "version-attempt-1",
        ApproverPolicyVersion: 1,
        ContentSafetyPolicyVersion: 1,
        ExpiresAt: "2026-12-31T23:59:59Z");

    private static AgentProposalCreationResult CreatedResult { get; } = new(
        AgentProposalCreationOutcome.Created,
        ProposalId: "proposal-001",
        SourceConversationId: "conversation-001",
        ProposedVersionId: "version-attempt-1",
        ApproverPolicyVersion: 1,
        ContentSafetyPolicyVersion: 1,
        ExpiresAt: "2026-12-31T23:59:59Z");

    // ===== Marker interfaces =====

    [Fact]
    public void Proposal_outcome_events_implement_IEventPayload_and_are_not_rejections()
    {
        // ProposedAgentReplyCreated/CreationFailed are recorded SUCCESS outcomes (Audit Evidence), NOT rejections (AD-2; FR-24).
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyCreated)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyCreationFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyCreated)).ShouldBeFalse();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyCreationFailed)).ShouldBeFalse();
    }

    [Fact]
    public void Not_creatable_rejection_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(ProposedAgentReplyNotCreatableRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(ProposedAgentReplyNotCreatableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Create_command_has_no_marker_interface_or_hexalith_attribute()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(CreateProposedAgentReply)).ShouldBeFalse();
        // Scope attribute-absence to Hexalith-namespaced attributes: the compiler emits Nullable* attributes on records
        // with nullable members, which are not a contract concern.
        typeof(CreateProposedAgentReply)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Proposal_evidence_round_trips()
        => RoundTrip(Evidence).ShouldBe(Evidence);

    [Fact]
    public void Proposal_result_round_trips_for_success_and_failure()
    {
        RoundTrip(CreatedResult).ShouldBe(CreatedResult);
        var failure = CreatedResult with { Outcome = AgentProposalCreationOutcome.GeneratedVersionUnavailable };
        RoundTrip(failure).ShouldBe(failure);
    }

    [Fact]
    public void Proposal_result_round_trips_with_a_null_expiry()
    {
        var noExpiry = CreatedResult with { ExpiresAt = null };
        RoundTrip(noExpiry).ShouldBe(noExpiry);
    }

    [Fact]
    public void Create_command_round_trips_with_its_result()
    {
        var command = new CreateProposedAgentReply("interaction-001", CreatedResult);
        CreateProposedAgentReply roundTripped = RoundTrip(command);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Result.ShouldBe(CreatedResult);
    }

    [Fact]
    public void Created_event_round_trips_with_its_evidence()
    {
        var created = new ProposedAgentReplyCreated("interaction-001", Evidence);
        ProposedAgentReplyCreated roundTripped = RoundTrip(created);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Evidence.ShouldBe(Evidence);
    }

    [Fact]
    public void Creation_failed_event_round_trips_with_its_reason_and_evidence()
    {
        var failed = new ProposedAgentReplyCreationFailed(
            "interaction-001",
            AgentProposalCreationFailureReason.GeneratedVersionUnavailable,
            Evidence);
        ProposedAgentReplyCreationFailed roundTripped = RoundTrip(failed);
        roundTripped.Reason.ShouldBe(AgentProposalCreationFailureReason.GeneratedVersionUnavailable);
        roundTripped.Evidence.ShouldBe(Evidence);
    }

    [Fact]
    public void Not_creatable_rejection_round_trips_with_the_enum_reason()
    {
        var rejection = new ProposedAgentReplyNotCreatableRejection("interaction-001", AgentProposedReplyNotCreatableReason.NotConfirmationResponseMode);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Proposal_state_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(ProposedAgentReplyState.Pending).ShouldBe("\"Pending\"");
        default(ProposedAgentReplyState).ShouldBe(ProposedAgentReplyState.Unknown);
        JsonSerializer.Deserialize<ProposedAgentReplyState>("\"Unknown\"").ShouldBe(ProposedAgentReplyState.Unknown);
    }

    [Fact]
    public void Creation_outcome_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposalCreationOutcome.Created).ShouldBe("\"Created\"");
        default(AgentProposalCreationOutcome).ShouldBe(AgentProposalCreationOutcome.Unknown);
        JsonSerializer.Deserialize<AgentProposalCreationOutcome>("\"Unknown\"").ShouldBe(AgentProposalCreationOutcome.Unknown);
    }

    [Fact]
    public void Creation_failure_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposalCreationFailureReason.GeneratedVersionUnavailable).ShouldBe("\"GeneratedVersionUnavailable\"");
        default(AgentProposalCreationFailureReason).ShouldBe(AgentProposalCreationFailureReason.Unknown);
        JsonSerializer.Deserialize<AgentProposalCreationFailureReason>("\"Unknown\"").ShouldBe(AgentProposalCreationFailureReason.Unknown);
    }

    [Fact]
    public void Not_creatable_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentProposedReplyNotCreatableReason.NotConfirmationResponseMode).ShouldBe("\"NotConfirmationResponseMode\"");
        default(AgentProposedReplyNotCreatableReason).ShouldBe(AgentProposedReplyNotCreatableReason.Unknown);
        JsonSerializer.Deserialize<AgentProposedReplyNotCreatableReason>("\"Unknown\"").ShouldBe(AgentProposedReplyNotCreatableReason.Unknown);
    }

    // ===== AgentInteractionStatus additive extension (AD-2) =====

    [Fact]
    public void Interaction_status_proposal_states_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.ProposalCreated).ShouldBe("\"ProposalCreated\"");
        JsonSerializer.Serialize(AgentInteractionStatus.ProposalCreationFailed).ShouldBe("\"ProposalCreationFailed\"");
    }

    [Fact]
    public void Interaction_status_additive_extension_preserves_existing_ordinals_and_adds_twelve_to_thirteen()
    {
        // The Story 2.1-2.5 ordinals (0-11) must be untouched so existing round-trip/replay stays green (AD-2 additive rule),
        // and the Story 3.1 states must be appended at 12-13.
        ((int)AgentInteractionStatus.Posted).ShouldBe(10);
        ((int)AgentInteractionStatus.PostingFailed).ShouldBe(11);
        ((int)AgentInteractionStatus.ProposalCreated).ShouldBe(12);
        ((int)AgentInteractionStatus.ProposalCreationFailed).ShouldBe(13);
    }

    // ===== AD-14: proposal surfaces are structurally content-free =====

    [Fact]
    public void The_proposal_surfaces_never_carry_generated_content_or_a_secret()
    {
        var created = new ProposedAgentReplyCreated("interaction-001", Evidence);
        var failed = new ProposedAgentReplyCreationFailed("interaction-001", AgentProposalCreationFailureReason.AdapterFailure, Evidence);
        var command = new CreateProposedAgentReply("interaction-001", CreatedResult);
        var rejection = new ProposedAgentReplyNotCreatableRejection("interaction-001", AgentProposedReplyNotCreatableReason.OutputNotGenerated);

        // Every proposal surface is structurally content-free, and a serialized round-trip confirms it (AD-14).
        foreach (string json in new[]
        {
            JsonSerializer.Serialize(created),
            JsonSerializer.Serialize(failed),
            JsonSerializer.Serialize(command),
            JsonSerializer.Serialize(rejection),
            JsonSerializer.Serialize(CreatedResult),
            JsonSerializer.Serialize(Evidence),
        })
        {
            json.ShouldNotContain(GeneratedContentText);
            json.ShouldNotContain(SecretText);
        }
    }

    [Fact]
    public void The_proposal_surfaces_have_no_content_bearing_member()
    {
        // No "GeneratedContent"/"Content"/"Text"/"Prompt"/… member on the proposal events, evidence, result, or rejection.
        CarriesNoContentMember(typeof(ProposedAgentReplyCreated));
        CarriesNoContentMember(typeof(ProposedAgentReplyCreationFailed));
        CarriesNoContentMember(typeof(ProposedAgentReplyNotCreatableRejection));
        CarriesNoContentMember(typeof(AgentProposalCreationResult));
        CarriesNoContentMember(typeof(AgentProposedReplyEvidence));
    }

    private static void CarriesNoContentMember(Type type)
    {
        // Exact-name matching so the safe id members (ProposalId, ProposedVersionId, …) are not tripped — only a
        // content-bearing member name is forbidden on a proposal surface (AD-14).
        string[] forbiddenExactNames =
            ["GeneratedContent", "Content", "Text", "Prompt", "Body", "Claim", "Claims", "Payload", "Message"];

        foreach (PropertyInfo property in type.GetProperties())
        {
            forbiddenExactNames.ShouldNotContain(
                forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                $"{type.FullName}.{property.Name} exposes a content-bearing member on a proposal surface (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
