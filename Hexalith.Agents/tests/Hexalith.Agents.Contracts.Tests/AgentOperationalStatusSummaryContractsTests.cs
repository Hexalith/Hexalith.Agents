namespace Hexalith.Agents.Contracts.Tests;

using System.Text.Json;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;

using Shouldly;

/// <summary>
/// Story 4.3 AC4 — the additive operational-status summary view/query/result round-trips by name, reserves the
/// <c>Unknown=0</c> sentinel, fails closed with no summary on non-success, and never serializes prompt/generated/secret
/// content (the summary is dimensioned only by safe enums/ids/ints/timestamps; AD-14).
/// </summary>
public sealed class AgentOperationalStatusSummaryContractsTests
{
    private static readonly string[] _poisonValues =
    [
        "prompt text",
        "generated content",
        "edited content",
        "provider secret",
        "raw provider payload",
        "System.InvalidOperationException",
        "stack trace",
        "other-tenant-id",
        "EventStore stream",
    ];

    private static AgentOperationalStatusSummaryView Summary()
        => new(
            AgentReadiness: AgentReadinessStatus.Callable,
            ReadinessBlockers: [AgentActivationBlocker.MissingProviderSelection],
            AuditGovernanceBlockers: [AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved],
            AuditAvailability: AuditAvailabilityStatus.AuditPending,
            RecentCallOutcomes: [new AgentCallOutcomeCount(AgentCallOperationStatus.Generated, 3)],
            ProposalOutcomes: [new ProposalOutcomeCount(ProposalOperationStatus.Posted, 4)],
            PendingProposalCount: 2,
            GeneratedAt: "2026-06-25T10:00:00Z");

    [Fact]
    public void Summary_result_round_trips_status_by_name_and_preserves_safe_fields()
    {
        AgentOperationalStatusSummaryResult original = AgentOperationalStatusSummaryResult.Success(Summary());

        string json = JsonSerializer.Serialize(original);
        AgentOperationalStatusSummaryResult? roundTrip = JsonSerializer.Deserialize<AgentOperationalStatusSummaryResult>(json);

        json.ShouldContain("\"Status\":\"Success\"");
        roundTrip.ShouldNotBeNull();
        roundTrip.Status.ShouldBe(OperationalStatusInspectionStatus.Success);
        roundTrip.Summary.ShouldNotBeNull();
        roundTrip.Summary.PendingProposalCount.ShouldBe(2);
        roundTrip.Summary.AgentReadiness.ShouldBe(AgentReadinessStatus.Callable);
        roundTrip.Summary.RecentCallOutcomes.ShouldHaveSingleItem().Count.ShouldBe(3);
        roundTrip.Summary.ProposalOutcomes.ShouldHaveSingleItem().Status.ShouldBe(ProposalOperationStatus.Posted);
    }

    [Fact]
    public void Inspection_status_reserves_unknown_zero_and_serializes_by_name()
    {
        Enum.ToObject(typeof(OperationalStatusInspectionStatus), 0).ToString().ShouldBe("Unknown");
        JsonSerializer.Serialize(OperationalStatusInspectionStatus.Stale).ShouldBe("\"Stale\"");
    }

    [Theory]
    [InlineData(OperationalStatusInspectionStatus.NotAuthorized)]
    [InlineData(OperationalStatusInspectionStatus.Unavailable)]
    public void Fail_closed_results_carry_no_summary(OperationalStatusInspectionStatus expected)
    {
        AgentOperationalStatusSummaryResult result = expected == OperationalStatusInspectionStatus.NotAuthorized
            ? AgentOperationalStatusSummaryResult.NotAuthorized()
            : AgentOperationalStatusSummaryResult.Unavailable();

        result.Status.ShouldBe(expected);
        result.Summary.ShouldBeNull();
    }

    [Fact]
    public void Stale_result_may_carry_a_degraded_summary()
    {
        AgentOperationalStatusSummaryResult result = AgentOperationalStatusSummaryResult.Stale(Summary());

        result.Status.ShouldBe(OperationalStatusInspectionStatus.Stale);
        result.Summary.ShouldNotBeNull();
    }

    [Fact]
    public void Summary_serialization_never_includes_poison_values()
    {
        string json = JsonSerializer.Serialize(AgentOperationalStatusSummaryResult.Success(Summary()));

        foreach (string poison in _poisonValues)
        {
            json.ShouldNotContain(poison, Case.Insensitive);
        }
    }

    [Fact]
    public void Query_carries_routing_discriminators_and_no_tenant_id()
    {
        GetAgentOperationalStatusSummaryQuery.Domain.ShouldBe("agent-operational-status");
        GetAgentOperationalStatusSummaryQuery.QueryType.ShouldBe("get-agent-operational-status-summary");

        // A bare record (mirrors ListPendingProposalsQuery) — tenant scope is supplied by the request envelope.
        GetAgentOperationalStatusSummaryQuery query = new();
        JsonSerializer.Serialize(query).ShouldBe("{}");
    }
}
