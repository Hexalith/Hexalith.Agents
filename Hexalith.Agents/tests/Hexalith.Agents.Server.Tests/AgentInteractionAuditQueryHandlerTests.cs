using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Queries;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

public sealed class AgentInteractionAuditQueryHandlerTests
{
    [Fact]
    public async Task Non_global_user_gets_not_authorized_without_state_access()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Available(State()));
        var handler = new GetAgentInteractionStatusQueryHandler(reader, new DeferredTenantAccessReader());

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentInteractionStatusQuery.QueryType, isGlobalAdmin: false), CancellationToken.None);

        result.Success.ShouldBeTrue();
        reader.Calls.ShouldBe(0);
        result.Payload<AgentInteractionInspectionResult>().Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
    }

    [Fact]
    public async Task Global_admin_gets_safe_status_from_tenant_scoped_state()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Available(State()));
        var handler = new GetAgentInteractionStatusQueryHandler(reader, new DeferredTenantAccessReader());

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentInteractionStatusQuery.QueryType), CancellationToken.None);

        result.Success.ShouldBeTrue();
        reader.Calls.ShouldBe(1);
        result.Payload<AgentInteractionInspectionResult>().ShouldSatisfyAllConditions(
            r => r.Status.ShouldBe(AgentInteractionInspectionStatus.Success),
            r => r.View.ShouldNotBeNull().AgentInteractionId.ShouldBe("interaction-1"));
    }

    [Fact]
    public async Task Unavailable_reader_returns_audit_unavailable_not_success_available()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Unavailable);
        var handler = new GetAgentAuditAvailabilityQueryHandler(reader, new DeferredTenantAccessReader());

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentAuditAvailabilityQuery.QueryType), CancellationToken.None);

        result.Success.ShouldBeTrue();
        AgentOperationResult<AuditAvailabilityStatus> payload = result.Payload<AgentOperationResult<AuditAvailabilityStatus>>();
        payload.IsSuccess.ShouldBeTrue();
        payload.Value.ShouldBe(AuditAvailabilityStatus.AuditUnavailable);
    }

    [Fact]
    public async Task Stale_state_returns_audit_delayed()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Available(State(), isFresh: false));
        var handler = new GetAgentAuditAvailabilityQueryHandler(reader, new DeferredTenantAccessReader());

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentAuditAvailabilityQuery.QueryType), CancellationToken.None);

        result.Payload<AgentOperationResult<AuditAvailabilityStatus>>().Value.ShouldBe(AuditAvailabilityStatus.AuditDelayed);
    }

    [Fact]
    public async Task Generation_handler_never_serializes_generated_content()
    {
        AgentInteractionState state = State();
        state.GeneratedVersions =
        [
            new AgentGeneratedVersion(
                "version-1",
                "attempt-1",
                AgentGenerationKind.Generated,
                "poison generated content",
                "provider",
                "model",
                3,
                4,
                10,
                20),
        ];
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Available(state));
        var handler = new GetAgentGenerationEvidenceQueryHandler(reader, new DeferredTenantAccessReader());

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentGenerationEvidenceQuery.QueryType), CancellationToken.None);

        string json = JsonSerializer.Serialize(result.GetPayload());
        json.ShouldNotContain("poison generated content");
        result.Payload<AgentOperationResult<AgentGenerationAttemptEvidence>>().Value.ShouldNotBeNull().AttemptId.ShouldBe("attempt-1");
    }

    [Fact]
    public async Task Tenant_member_with_fresh_access_is_authorized_and_reads_scoped_state()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Available(State()));
        var tenant = new StubTenantAccessReader(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        var handler = new GetAgentInteractionStatusQueryHandler(reader, tenant);

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentInteractionStatusQuery.QueryType, isGlobalAdmin: false), CancellationToken.None);

        reader.Calls.ShouldBe(1);
        result.Payload<AgentInteractionInspectionResult>().Status.ShouldBe(AgentInteractionInspectionStatus.Success);
    }

    [Fact]
    public async Task Tenant_member_with_stale_access_is_not_authorized_without_state_access()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Available(State()));
        var tenant = new StubTenantAccessReader(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: false));
        var handler = new GetAgentInteractionStatusQueryHandler(reader, tenant);

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentInteractionStatusQuery.QueryType, isGlobalAdmin: false), CancellationToken.None);

        reader.Calls.ShouldBe(0);
        result.Payload<AgentInteractionInspectionResult>().Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
    }

    [Fact]
    public async Task Every_audit_handler_fails_closed_to_not_authorized_without_state_access()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Available(State()));

        foreach (AgentInteractionAuditQueryHandlerBase handler in AllHandlers(reader, new DeferredTenantAccessReader()))
        {
            QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType, isGlobalAdmin: false), CancellationToken.None);

            result.Success.ShouldBeTrue();
            result.GetPayload().GetProperty("status").GetString().ShouldBe("NotAuthorized");
        }

        reader.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Posting_handler_unavailable_reader_returns_unavailable_not_success()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Unavailable);
        var handler = new GetAgentPostingEvidenceQueryHandler(reader, new DeferredTenantAccessReader());

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentPostingEvidenceQuery.QueryType), CancellationToken.None);

        reader.Calls.ShouldBe(1);
        AgentOperationResult<AgentPostedMessageEvidence> payload = result.Payload<AgentOperationResult<AgentPostedMessageEvidence>>();
        payload.IsSuccess.ShouldBeFalse();
        payload.Status.ShouldBe(AgentOperationStatus.Unavailable);
    }

    [Fact]
    public async Task Gate_handler_unavailable_reader_returns_not_found_not_success()
    {
        var reader = new RecordingAuditStateReader(AgentInteractionAuditStateReadResult.Unavailable);
        var handler = new GetAgentInteractionGateEvidenceQueryHandler(reader, new DeferredTenantAccessReader());

        QueryResult result = await handler.ExecuteAsync(Envelope(GetAgentInteractionGateEvidenceQuery.QueryType), CancellationToken.None);

        result.Payload<AgentInteractionGateEvidenceResult>().ShouldSatisfyAllConditions(
            r => r.Status.ShouldBe(AgentInteractionGateInspectionStatus.NotFound),
            r => r.Evidence.ShouldBeNull());
    }

    [Fact]
    public async Task State_reader_is_scoped_strictly_to_the_envelope_tenant_and_aggregate()
    {
        var reader = new CapturingAuditStateReader(AgentInteractionAuditStateReadResult.Available(State()));
        var handler = new GetAgentInteractionStatusQueryHandler(reader, new DeferredTenantAccessReader());

        await handler.ExecuteAsync(
            new QueryEnvelope(
                "tenant-9",
                GetAgentInteractionStatusQuery.Domain,
                "interaction-9",
                GetAgentInteractionStatusQuery.QueryType,
                [],
                "corr-1",
                "user-1",
                isGlobalAdmin: true),
            CancellationToken.None);

        reader.TenantId.ShouldBe("tenant-9");
        reader.AggregateId.ShouldBe("interaction-9");
    }

    private static AgentInteractionAuditQueryHandlerBase[] AllHandlers(IAgentInteractionAuditStateReader reader, ITenantAccessReader tenant)
        =>
        [
            new GetAgentInteractionStatusQueryHandler(reader, tenant),
            new GetAgentInteractionGateEvidenceQueryHandler(reader, tenant),
            new GetAgentInteractionContextEvidenceQueryHandler(reader, tenant),
            new GetAgentGenerationEvidenceQueryHandler(reader, tenant),
            new GetAgentPostingEvidenceQueryHandler(reader, tenant),
            new GetAgentProposalEditEvidenceQueryHandler(reader, tenant),
            new GetAgentProposalRegenerationEvidenceQueryHandler(reader, tenant),
            new GetAgentProposalApprovalEvidenceQueryHandler(reader, tenant),
            new GetAgentAuditAvailabilityQueryHandler(reader, tenant),
        ];

    private static QueryEnvelope Envelope(string queryType, bool isGlobalAdmin = true)
        => new(
            "tenant-1",
            GetAgentInteractionStatusQuery.Domain,
            "interaction-1",
            queryType,
            [],
            "corr-1",
            "user-1",
            isGlobalAdmin: isGlobalAdmin);

    private static AgentInteractionState State()
        => new()
        {
            IsRequested = true,
            AgentInteractionId = "interaction-1",
            AgentId = "agent-1",
            CallerPartyId = "caller-1",
            SourceConversationId = "conversation-1",
            Status = AgentInteractionStatus.Requested,
            Snapshot = new(
                1,
                2,
                Hexalith.Agents.Contracts.Agent.AgentResponseMode.Automatic,
                3,
                "provider",
                "model",
                4,
                5,
                "context-policy"),
        };

    private sealed class RecordingAuditStateReader(AgentInteractionAuditStateReadResult result) : IAgentInteractionAuditStateReader
    {
        public int Calls { get; private set; }

        public Task<AgentInteractionAuditStateReadResult> ReadAsync(string tenantId, string agentInteractionId, CancellationToken ct)
        {
            tenantId.ShouldBe("tenant-1");
            agentInteractionId.ShouldBe("interaction-1");
            Calls++;
            return Task.FromResult(result);
        }
    }

    private sealed class CapturingAuditStateReader(AgentInteractionAuditStateReadResult result) : IAgentInteractionAuditStateReader
    {
        public string? TenantId { get; private set; }

        public string? AggregateId { get; private set; }

        public Task<AgentInteractionAuditStateReadResult> ReadAsync(string tenantId, string agentInteractionId, CancellationToken ct)
        {
            TenantId = tenantId;
            AggregateId = agentInteractionId;
            return Task.FromResult(result);
        }
    }

    private sealed class StubTenantAccessReader(TenantAccessReadResult result) : ITenantAccessReader
    {
        public Task<TenantAccessReadResult> ReadAsync(string tenantId, string actorUserId, string callerPartyId, CancellationToken ct)
            => Task.FromResult(result);
    }
}

internal static class QueryResultTestExtensions
{
    public static T Payload<T>(this QueryResult result)
    {
        result.Success.ShouldBeTrue();
        return result.GetPayload().Deserialize<T>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        })!;
    }
}
