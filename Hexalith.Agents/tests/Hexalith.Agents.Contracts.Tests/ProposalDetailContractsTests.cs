using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Contract guard for the Story 3.7 proposal-DETAIL read surface (AC1, AC2; AD-12, AD-14). The public read contracts
/// must survive System.Text.Json; the inspection-status enum must serialize by name, fail safe to its <c>Unknown</c>
/// sentinel, and keep stable append-only ordinals; a denied/faulted/not-found result must carry NO detail so it never
/// fingerprints other tenants' records; and the <see cref="ProposalDetailView"/> and <see cref="ProposalVersionSummary"/>
/// must be structurally content-free — they carry only safe ids (the generated content's sole durable home stays the
/// Story 2.4/3.3 success events — AD-14). The assembly-wide <c>ContractsSecretNonDisclosureTests</c> additionally
/// auto-covers the new types.
/// </summary>
public sealed class ProposalDetailContractsTests
{
    // A generated-content sample string that must NEVER appear on the proposal-detail contracts (AD-14).
    private const string GeneratedContentText = "top-secret-generated-answer-do-not-leak";

    // A secret-looking token that must NEVER appear on the proposal-detail contracts (AD-14).
    private const string SecretText = "sk-secret-credential-do-not-leak";

    private static ProposalVersionSummary GeneratedSummary { get; } = new(
        VersionId: "version-attempt-1",
        Kind: AgentGenerationKind.Generated,
        ProviderId: "openai",
        ModelId: "gpt-x",
        SourceVersionId: null,
        EditorPartyId: null,
        CreatedAt: "2026-06-24T08:00:00Z",
        IsApproved: false,
        IsPosted: false);

    private static ProposalVersionSummary EditedSummary { get; } = new(
        VersionId: "edited-version-2",
        Kind: AgentGenerationKind.Edited,
        ProviderId: "openai",
        ModelId: "gpt-x",
        SourceVersionId: "version-attempt-1",
        EditorPartyId: "editor-001",
        CreatedAt: "2026-06-24T09:00:00Z",
        IsApproved: true,
        IsPosted: false);

    private static ProposalDetailView View { get; } = new(
        AgentInteractionId: "interaction-001",
        ProposalId: "proposal-001",
        State: ProposedAgentReplyState.Edited,
        InteractionStatus: AgentInteractionStatus.ProposalEdited,
        SourceConversationId: "conversation-001",
        CallerPartyId: "caller-001",
        AgentId: "agent-001",
        NeedsCurrentUserAction: true,
        SelectedVersionId: "edited-version-2",
        ResponseMode: AgentResponseMode.Confirmation,
        ProviderId: "openai",
        ModelId: "gpt-x",
        ApproverPolicyVersion: 2,
        ContentSafetyPolicyVersion: 3,
        ExpiresAt: "2026-12-31T23:59:59Z",
        CreatedAt: "2026-06-24T08:00:00Z",
        ApprovedVersionId: null,
        ApprovedAt: null,
        PostedAt: null,
        Versions: [GeneratedSummary, EditedSummary]);

    // ===== Round-trips =====

    [Fact]
    public void View_round_trips() => AssertViewRoundTrips(View);

    [Fact]
    public void View_round_trips_with_null_optional_fields()
        => AssertViewRoundTrips(View with
        {
            ExpiresAt = null,
            CreatedAt = null,
            ApprovedVersionId = null,
            ApprovedAt = null,
            PostedAt = null,
            Versions = [],
        });

    [Fact]
    public void Version_summary_round_trips_for_each_kind()
    {
        RoundTrip(GeneratedSummary).ShouldBe(GeneratedSummary);
        RoundTrip(EditedSummary).ShouldBe(EditedSummary);
    }

    [Fact]
    public void Query_round_trips()
        => RoundTrip(new GetProposalDetailQuery("interaction-001")).AgentInteractionId.ShouldBe("interaction-001");

    [Fact]
    public void Success_result_round_trips_with_its_detail()
    {
        ProposalDetailResult roundTripped = RoundTrip(ProposalDetailResult.Success(View));
        roundTripped.Status.ShouldBe(ProposalDetailInspectionStatus.Success);
        AssertViewsEqual(roundTripped.Detail.ShouldNotBeNull(), View);
    }

    [Fact]
    public void Stale_result_round_trips_with_its_degraded_detail()
    {
        ProposalDetailResult roundTripped = RoundTrip(ProposalDetailResult.Stale(View));
        roundTripped.Status.ShouldBe(ProposalDetailInspectionStatus.Stale);
        AssertViewsEqual(roundTripped.Detail.ShouldNotBeNull(), View);
    }

    [Fact]
    public void Stale_result_can_carry_no_trustworthy_detail()
    {
        ProposalDetailResult result = ProposalDetailResult.Stale(null);
        result.Status.ShouldBe(ProposalDetailInspectionStatus.Stale);
        result.Detail.ShouldBeNull();
    }

    // ===== AC1/AD-12: a denied/faulted/not-found result discloses nothing =====

    [Fact]
    public void Not_authorized_result_carries_no_detail()
    {
        ProposalDetailResult result = ProposalDetailResult.NotAuthorized();
        result.Status.ShouldBe(ProposalDetailInspectionStatus.NotAuthorized);
        result.Detail.ShouldBeNull();
    }

    [Fact]
    public void Unavailable_result_carries_no_detail()
    {
        ProposalDetailResult result = ProposalDetailResult.Unavailable();
        result.Status.ShouldBe(ProposalDetailInspectionStatus.Unavailable);
        result.Detail.ShouldBeNull();
    }

    [Fact]
    public void Not_found_result_carries_no_detail()
    {
        ProposalDetailResult result = ProposalDetailResult.NotFound();
        result.Status.ShouldBe(ProposalDetailInspectionStatus.NotFound);
        result.Detail.ShouldBeNull();
    }

    // ===== Inspection status serializes by name, fails safe, and keeps stable ordinals =====

    [Fact]
    public void Inspection_status_serializes_by_name()
    {
        JsonSerializer.Serialize(ProposalDetailInspectionStatus.Success).ShouldBe("\"Success\"");
        JsonSerializer.Serialize(ProposalDetailInspectionStatus.NotAuthorized).ShouldBe("\"NotAuthorized\"");
        JsonSerializer.Serialize(ProposalDetailInspectionStatus.Unavailable).ShouldBe("\"Unavailable\"");
        JsonSerializer.Serialize(ProposalDetailInspectionStatus.Stale).ShouldBe("\"Stale\"");
        JsonSerializer.Serialize(ProposalDetailInspectionStatus.NotFound).ShouldBe("\"NotFound\"");
    }

    [Fact]
    public void Inspection_status_is_unknown_by_default_and_round_trips_the_sentinel()
    {
        default(ProposalDetailInspectionStatus).ShouldBe(ProposalDetailInspectionStatus.Unknown);
        JsonSerializer.Deserialize<ProposalDetailInspectionStatus>("\"Unknown\"").ShouldBe(ProposalDetailInspectionStatus.Unknown);
    }

    [Fact]
    public void Inspection_status_ordinals_are_stable_and_append_only()
    {
        ((int)ProposalDetailInspectionStatus.Unknown).ShouldBe(0);
        ((int)ProposalDetailInspectionStatus.Success).ShouldBe(1);
        ((int)ProposalDetailInspectionStatus.NotAuthorized).ShouldBe(2);
        ((int)ProposalDetailInspectionStatus.Unavailable).ShouldBe(3);
        ((int)ProposalDetailInspectionStatus.Stale).ShouldBe(4);
        ((int)ProposalDetailInspectionStatus.NotFound).ShouldBe(5);
    }

    // ===== AD-14: the view + version summary are structurally content-free =====

    [Fact]
    public void Serialized_detail_never_carries_generated_content_or_a_secret()
    {
        string viewJson = JsonSerializer.Serialize(View);
        viewJson.ShouldNotContain(GeneratedContentText);
        viewJson.ShouldNotContain(SecretText);

        string summaryJson = JsonSerializer.Serialize(EditedSummary);
        summaryJson.ShouldNotContain(GeneratedContentText);
        summaryJson.ShouldNotContain(SecretText);

        string resultJson = JsonSerializer.Serialize(ProposalDetailResult.Success(View));
        resultJson.ShouldNotContain(GeneratedContentText);
        resultJson.ShouldNotContain(SecretText);
    }

    [Fact]
    public void View_and_version_summary_have_no_content_bearing_member()
    {
        // Exact-name matching so the safe id members (ProposalId, SelectedVersionId, …) are not tripped — only a
        // content-bearing member name is forbidden on the read contracts (AD-14).
        string[] forbiddenExactNames =
            ["GeneratedContent", "Content", "Text", "Prompt", "Body", "Claim", "Claims", "Payload", "Message"];

        foreach (Type type in new[] { typeof(ProposalDetailView), typeof(ProposalVersionSummary) })
        {
            foreach (PropertyInfo property in type.GetProperties())
            {
                forbiddenExactNames.ShouldNotContain(
                    forbidden => string.Equals(forbidden, property.Name, StringComparison.OrdinalIgnoreCase),
                    $"{type.FullName}.{property.Name} exposes a content-bearing member on the read contract (AD-14).");
            }
        }
    }

    private static void AssertViewRoundTrips(ProposalDetailView original)
        => AssertViewsEqual(RoundTrip(original), original);

    // Record value equality compares the IReadOnlyList Versions by reference, so a JSON round-trip (which rebuilds the
    // list) never equals the original by whole-record equality. Compare the scalar fields (by normalizing the list
    // reference) and the version history element-by-element instead.
    private static void AssertViewsEqual(ProposalDetailView actual, ProposalDetailView expected)
    {
        actual.ShouldBe(expected with { Versions = actual.Versions });
        actual.Versions.Count.ShouldBe(expected.Versions.Count);
        for (int i = 0; i < expected.Versions.Count; i++)
        {
            actual.Versions[i].ShouldBe(expected.Versions[i]);
        }
    }

    private static T RoundTrip<T>(T value)
        => JsonSerializer.Deserialize<T>(JsonSerializer.SerializeToUtf8Bytes(value))!;
}
