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
/// Contract guard for the Story 2.5 automatic-posting surface (AC1–AC4; AD-2, AD-7, AD-13, AD-14). Durable event sourcing
/// replays every new event/rejection, so each must survive System.Text.Json; the new classification enums must serialize by
/// name and fail safe to their <c>Unknown</c> sentinel; the additive <see cref="AgentInteractionStatus"/> extension must not
/// perturb the existing 0-9 ordinals and must add 10-11; the posting surfaces are structurally content-free — they carry
/// only safe ids (the generated content's sole durable home stays the Story 2.4 success event — AD-14). The assembly-wide
/// <c>ContractsSecretNonDisclosureTests</c> additionally auto-covers the new types.
/// </summary>
public sealed class AgentInteractionPostingContractsTests
{
    // A generated-content sample string that must NEVER appear on any posting surface (AD-14).
    private const string GeneratedContentText = "top-secret-generated-answer-do-not-leak";

    private static AgentPostedMessageEvidence Evidence { get; } = new(
        MessageId: "message-001",
        SourceConversationId: "conversation-001",
        AgentPartyId: "agent-party-001",
        PostedVersionId: "version-attempt-1");

    private static AgentResponsePostingResult PostedResult { get; } = new(
        AgentResponsePostingOutcome.Posted,
        MessageId: "message-001",
        SourceConversationId: "conversation-001",
        AgentPartyId: "agent-party-001",
        PostedVersionId: "version-attempt-1");

    // ===== Marker interfaces =====

    [Fact]
    public void Posting_outcome_events_implement_IEventPayload_and_are_not_rejections()
    {
        // AgentResponsePosted/Failed are recorded SUCCESS outcomes (Audit Evidence), NOT rejections (AD-2; FR-24).
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentResponsePosted)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentResponsePostingFailed)).ShouldBeTrue();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentResponsePosted)).ShouldBeFalse();
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentResponsePostingFailed)).ShouldBeFalse();
    }

    [Fact]
    public void Not_postable_rejection_implements_IRejectionEvent()
    {
        typeof(IRejectionEvent).IsAssignableFrom(typeof(AgentResponseNotPostableRejection)).ShouldBeTrue();
        typeof(IEventPayload).IsAssignableFrom(typeof(AgentResponseNotPostableRejection)).ShouldBeTrue();
    }

    [Fact]
    public void Post_command_has_no_marker_interface_or_hexalith_attribute()
    {
        typeof(IEventPayload).IsAssignableFrom(typeof(PostAgentResponse)).ShouldBeFalse();
        // Scope attribute-absence to Hexalith-namespaced attributes: the compiler emits Nullable* attributes on records
        // with nullable members (Story 2.1 learning), which are not a contract concern.
        typeof(PostAgentResponse)
            .GetCustomAttributes(inherit: false)
            .Select(attribute => attribute.GetType().Namespace ?? string.Empty)
            .ShouldNotContain(ns => ns.StartsWith("Hexalith", StringComparison.Ordinal));
    }

    // ===== Round-trips =====

    [Fact]
    public void Posted_message_evidence_round_trips()
        => RoundTrip(Evidence).ShouldBe(Evidence);

    [Fact]
    public void Posting_result_round_trips_for_success_and_failure()
    {
        RoundTrip(PostedResult).ShouldBe(PostedResult);
        var failure = PostedResult with { Outcome = AgentResponsePostingOutcome.MembershipUnavailable };
        RoundTrip(failure).ShouldBe(failure);
    }

    [Fact]
    public void Post_command_round_trips_with_its_result()
    {
        var command = new PostAgentResponse("interaction-001", PostedResult);
        PostAgentResponse roundTripped = RoundTrip(command);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Result.ShouldBe(PostedResult);
    }

    [Fact]
    public void Posted_event_round_trips_with_its_evidence()
    {
        var posted = new AgentResponsePosted("interaction-001", Evidence);
        AgentResponsePosted roundTripped = RoundTrip(posted);
        roundTripped.AgentInteractionId.ShouldBe("interaction-001");
        roundTripped.Evidence.ShouldBe(Evidence);
    }

    [Fact]
    public void Posting_failed_event_round_trips_with_its_reason_and_evidence()
    {
        var failed = new AgentResponsePostingFailed(
            "interaction-001",
            AgentResponsePostingFailureReason.MembershipUnavailable,
            Evidence);
        AgentResponsePostingFailed roundTripped = RoundTrip(failed);
        roundTripped.Reason.ShouldBe(AgentResponsePostingFailureReason.MembershipUnavailable);
        roundTripped.Evidence.ShouldBe(Evidence);
    }

    [Fact]
    public void Not_postable_rejection_round_trips_with_the_enum_reason()
    {
        var rejection = new AgentResponseNotPostableRejection("interaction-001", AgentResponseNotPostableReason.NotAutomaticResponseMode);
        RoundTrip(rejection).ShouldBe(rejection);
    }

    // ===== Enums serialize by name and fail safe =====

    [Fact]
    public void Posting_outcome_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentResponsePostingOutcome.Posted).ShouldBe("\"Posted\"");
        default(AgentResponsePostingOutcome).ShouldBe(AgentResponsePostingOutcome.Unknown);
        JsonSerializer.Deserialize<AgentResponsePostingOutcome>("\"Unknown\"").ShouldBe(AgentResponsePostingOutcome.Unknown);
    }

    [Fact]
    public void Posting_failure_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentResponsePostingFailureReason.MembershipUnavailable).ShouldBe("\"MembershipUnavailable\"");
        default(AgentResponsePostingFailureReason).ShouldBe(AgentResponsePostingFailureReason.Unknown);
        JsonSerializer.Deserialize<AgentResponsePostingFailureReason>("\"Unknown\"").ShouldBe(AgentResponsePostingFailureReason.Unknown);
    }

    [Fact]
    public void Not_postable_reason_serializes_by_name_and_is_unknown_by_default()
    {
        JsonSerializer.Serialize(AgentResponseNotPostableReason.NotAutomaticResponseMode).ShouldBe("\"NotAutomaticResponseMode\"");
        default(AgentResponseNotPostableReason).ShouldBe(AgentResponseNotPostableReason.Unknown);
        JsonSerializer.Deserialize<AgentResponseNotPostableReason>("\"Unknown\"").ShouldBe(AgentResponseNotPostableReason.Unknown);
    }

    // ===== AgentInteractionStatus additive extension (AD-2) =====

    [Fact]
    public void Interaction_status_posting_states_serialize_by_name()
    {
        JsonSerializer.Serialize(AgentInteractionStatus.Posted).ShouldBe("\"Posted\"");
        JsonSerializer.Serialize(AgentInteractionStatus.PostingFailed).ShouldBe("\"PostingFailed\"");
    }

    [Fact]
    public void Interaction_status_additive_extension_preserves_existing_ordinals_and_adds_ten_to_eleven()
    {
        // The Story 2.1-2.4 ordinals (0-9) must be untouched so existing round-trip/replay stays green (AD-2 additive rule),
        // and the Story 2.5 states must be appended at 10-11.
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
        ((int)AgentInteractionStatus.Posted).ShouldBe(10);
        ((int)AgentInteractionStatus.PostingFailed).ShouldBe(11);
    }

    // ===== AD-14: posting surfaces are structurally content-free =====

    [Fact]
    public void The_posting_surfaces_never_carry_generated_content()
    {
        var posted = new AgentResponsePosted("interaction-001", Evidence);
        var failed = new AgentResponsePostingFailed("interaction-001", AgentResponsePostingFailureReason.PostRejected, Evidence);
        var command = new PostAgentResponse("interaction-001", PostedResult);
        var rejection = new AgentResponseNotPostableRejection("interaction-001", AgentResponseNotPostableReason.OutputNotGenerated);

        // Every posting surface is structurally content-free, and a serialized round-trip confirms it (AD-14).
        JsonSerializer.Serialize(posted).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(failed).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(command).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(rejection).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(PostedResult).ShouldNotContain(GeneratedContentText);
        JsonSerializer.Serialize(Evidence).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public void The_posting_surfaces_have_no_content_bearing_member()
    {
        // No "GeneratedContent"/"Content"/"Text"/"Prompt"/… member on the posting events, evidence, result, or rejection.
        CarriesNoContentMember(typeof(AgentResponsePosted));
        CarriesNoContentMember(typeof(AgentResponsePostingFailed));
        CarriesNoContentMember(typeof(AgentResponseNotPostableRejection));
        CarriesNoContentMember(typeof(AgentResponsePostingResult));
        CarriesNoContentMember(typeof(AgentPostedMessageEvidence));
    }

    private static void CarriesNoContentMember(Type type)
    {
        // Exact-name matching so the safe id members (MessageId, PostedVersionId, …) are not tripped — only a
        // content-bearing member name is forbidden on a posting surface (AD-14).
        string[] forbiddenExactNames =
            ["GeneratedContent", "Content", "Text", "Prompt", "Body", "Claim", "Claims", "Payload", "Message"];

        foreach (PropertyInfo property in type.GetProperties())
        {
            forbiddenExactNames.ShouldNotContain(
                forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                $"{type.FullName}.{property.Name} exposes a content-bearing member on a posting surface (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
