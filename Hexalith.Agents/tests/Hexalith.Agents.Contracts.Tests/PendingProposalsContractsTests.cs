using System;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 3.2 proposal-queue READ surface (AC1–AC4; AD-12, AD-14). The public read contracts
/// must survive System.Text.Json; the inspection-status enum must serialize by name and fail safe to its
/// <c>Unknown</c> sentinel; a denied/faulted result must carry an EMPTY list AND a ZERO count so it never fingerprints
/// other tenants' records (AC4); and the <see cref="PendingProposalView"/> must be structurally content-free — it
/// carries only safe ids (the generated content's sole durable home stays the Story 2.4 success event — AD-14). The
/// assembly-wide <c>ContractsSecretNonDisclosureTests</c> additionally auto-covers the new view.
/// </summary>
public sealed class PendingProposalsContractsTests
{
    // A generated-content sample string that must NEVER appear on the proposal-queue view (AD-14).
    private const string GeneratedContentText = "top-secret-generated-answer-do-not-leak";

    // A secret-looking token that must NEVER appear on the proposal-queue view (AD-14).
    private const string SecretText = "sk-secret-credential-do-not-leak";

    private static PendingProposalView View { get; } = new(
        AgentInteractionId: "interaction-001",
        ProposalId: "proposal-001",
        State: ProposedAgentReplyState.Pending,
        InteractionStatus: AgentInteractionStatus.ProposalCreated,
        SourceConversationId: "conversation-001",
        CallerPartyId: "caller-001",
        AgentId: "agent-001",
        NeedsCurrentUserAction: true,
        ProposedVersionId: "version-attempt-1",
        ApproverPolicyVersion: 2,
        ContentSafetyPolicyVersion: 3,
        ExpiresAt: "2026-12-31T23:59:59Z",
        CreatedAt: "2026-06-24T08:00:00Z");

    // ===== Round-trips =====

    [Fact]
    public void View_round_trips()
        => RoundTrip(View).ShouldBe(View);

    [Fact]
    public void View_round_trips_with_null_optional_timestamps()
    {
        var view = View with { ExpiresAt = null, CreatedAt = null };
        RoundTrip(view).ShouldBe(view);
    }

    [Fact]
    public void Query_round_trips_in_both_modes()
    {
        RoundTrip(new ListPendingProposalsQuery(IncludeHistorical: true)).IncludeHistorical.ShouldBeTrue();
        RoundTrip(new ListPendingProposalsQuery(IncludeHistorical: false)).IncludeHistorical.ShouldBeFalse();
    }

    [Fact]
    public void Success_result_round_trips_with_its_rows_and_count()
    {
        PendingProposalsResult result = PendingProposalsResult.Success([View], pendingCount: 1);
        PendingProposalsResult roundTripped = RoundTrip(result);
        roundTripped.Status.ShouldBe(PendingProposalsInspectionStatus.Success);
        roundTripped.PendingCount.ShouldBe(1);
        roundTripped.Proposals.ShouldHaveSingleItem().ShouldBe(View);
    }

    [Fact]
    public void Stale_result_round_trips_with_its_rows_and_count()
    {
        PendingProposalsResult result = PendingProposalsResult.Stale([View], pendingCount: 1);
        PendingProposalsResult roundTripped = RoundTrip(result);
        roundTripped.Status.ShouldBe(PendingProposalsInspectionStatus.Stale);
        roundTripped.PendingCount.ShouldBe(1);
        roundTripped.Proposals.ShouldHaveSingleItem().ShouldBe(View);
    }

    [Fact]
    public void Stale_result_can_carry_no_trustworthy_rows()
    {
        // Degraded data without trustworthy rows still fails safe: an empty list + zero count, exactly like a denial (AC4).
        PendingProposalsResult result = PendingProposalsResult.Stale([], pendingCount: 0);
        result.Status.ShouldBe(PendingProposalsInspectionStatus.Stale);
        result.Proposals.ShouldBeEmpty();
        result.PendingCount.ShouldBe(0);
    }

    [Fact]
    public void Success_result_round_trips_multiple_rows_in_order()
    {
        PendingProposalView second = View with
        {
            AgentInteractionId = "interaction-002",
            ProposalId = "proposal-002",
            NeedsCurrentUserAction = false,
        };
        PendingProposalsResult result = PendingProposalsResult.Success([View, second], pendingCount: 1);

        PendingProposalsResult roundTripped = RoundTrip(result);

        roundTripped.Proposals.Count.ShouldBe(2);
        roundTripped.Proposals[0].ShouldBe(View);
        roundTripped.Proposals[1].ShouldBe(second);
        roundTripped.PendingCount.ShouldBe(1);
    }

    // ===== AC4: a denied/faulted result discloses nothing =====

    [Fact]
    public void Not_authorized_result_carries_an_empty_list_and_a_zero_count()
    {
        PendingProposalsResult result = PendingProposalsResult.NotAuthorized();
        result.Status.ShouldBe(PendingProposalsInspectionStatus.NotAuthorized);
        result.Proposals.ShouldBeEmpty();
        result.PendingCount.ShouldBe(0);
    }

    [Fact]
    public void Unavailable_result_carries_an_empty_list_and_a_zero_count()
    {
        PendingProposalsResult result = PendingProposalsResult.Unavailable();
        result.Status.ShouldBe(PendingProposalsInspectionStatus.Unavailable);
        result.Proposals.ShouldBeEmpty();
        result.PendingCount.ShouldBe(0);
    }

    // ===== Inspection status serializes by name and fails safe =====

    [Fact]
    public void Inspection_status_serializes_by_name()
    {
        JsonSerializer.Serialize(PendingProposalsInspectionStatus.Success).ShouldBe("\"Success\"");
        JsonSerializer.Serialize(PendingProposalsInspectionStatus.NotAuthorized).ShouldBe("\"NotAuthorized\"");
        JsonSerializer.Serialize(PendingProposalsInspectionStatus.Unavailable).ShouldBe("\"Unavailable\"");
        JsonSerializer.Serialize(PendingProposalsInspectionStatus.Stale).ShouldBe("\"Stale\"");
    }

    [Fact]
    public void Inspection_status_is_unknown_by_default_and_round_trips_the_sentinel()
    {
        default(PendingProposalsInspectionStatus).ShouldBe(PendingProposalsInspectionStatus.Unknown);
        JsonSerializer.Deserialize<PendingProposalsInspectionStatus>("\"Unknown\"").ShouldBe(PendingProposalsInspectionStatus.Unknown);
    }

    // ===== AD-14: the view is structurally content-free =====

    [Fact]
    public void Serialized_view_never_carries_generated_content_or_a_secret()
    {
        string json = JsonSerializer.Serialize(View);
        json.ShouldNotContain(GeneratedContentText);
        json.ShouldNotContain(SecretText);
    }

    [Fact]
    public void View_has_no_content_bearing_member()
    {
        // Exact-name matching so the safe id members (ProposalId, ProposedVersionId, …) are not tripped — only a
        // content-bearing member name is forbidden on the read view (AD-14).
        string[] forbiddenExactNames =
            ["GeneratedContent", "Content", "Text", "Prompt", "Body", "Claim", "Claims", "Payload", "Message"];

        foreach (PropertyInfo property in typeof(PendingProposalView).GetProperties())
        {
            forbiddenExactNames.ShouldNotContain(
                forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                $"{typeof(PendingProposalView).FullName}.{property.Name} exposes a content-bearing member on the read view (AD-14).");
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
