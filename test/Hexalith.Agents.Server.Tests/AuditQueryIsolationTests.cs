using System.Text.Json;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Queries;

using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

public sealed class AuditQueryIsolationTests
{
    public static IEnumerable<object[]> ScopeMismatches()
    {
        for (int handler = 0; handler < 9; handler++)
        {
            foreach (string fault in new[] { "tenant", "aggregate", "both", "missing-tenant", "missing-aggregate", "legacy", "state", "empty-state" })
            {
                yield return [handler, fault];
            }
        }
    }

    [Theory]
    [MemberData(nameof(ScopeMismatches))]
    public async Task MisroutedStateReturnsOnlyTheUnavailablePayload(int handlerIndex, string fault)
    {
        AgentInteractionState state = State();
        AgentInteractionAuditStateReadResult read = AgentInteractionAuditStateReadResult.Available("tenant-1", "interaction-1", state);
        read = fault switch
        {
            "tenant" => read with { SourceTenantId = "foreign-tenant" },
            "aggregate" => read with { SourceAggregateId = "foreign-interaction" },
            "both" => read with { SourceTenantId = "foreign-tenant", SourceAggregateId = "foreign-interaction" },
            "missing-tenant" => read with { SourceTenantId = null },
            "missing-aggregate" => read with { SourceAggregateId = null },
            "legacy" => AgentInteractionAuditStateReadResult.Available(state),
            _ => read,
        };
        if (fault is "state" or "empty-state")
        {
            state.AgentInteractionId = fault == "state" ? "foreign-interaction" : string.Empty;
        }

        IAgentInteractionAuditStateReader reader = Reader(read);
        AgentInteractionAuditQueryHandlerBase handler = Handler(handlerIndex, reader, Authorized());
        AgentInteractionAuditQueryHandlerBase unavailable = Handler(handlerIndex, Reader(AgentInteractionAuditStateReadResult.Unavailable), Authorized());

        QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType), CancellationToken.None);
        QueryResult expected = await unavailable.ExecuteAsync(Envelope(unavailable.QueryType), CancellationToken.None);

        result.Success.ShouldBeTrue();
        string json = JsonSerializer.Serialize(result.GetPayload());
        json.ShouldBe(JsonSerializer.Serialize(expected.GetPayload()));
        json.ShouldNotContain("foreign");
        json.ShouldNotContain("poison");
        json.ShouldNotContain("interaction-1");
        await reader.Received(1).ReadAsync("tenant-1", "interaction-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeniedTenantNeverReadsAnyAuditState()
    {
        for (int index = 0; index < 9; index++)
        {
            IAgentInteractionAuditStateReader reader = Reader(AgentInteractionAuditStateReadResult.Available("foreign-tenant", "foreign-interaction", State()));
            ITenantAccessReader tenant = Substitute.For<ITenantAccessReader>();
            tenant.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Unauthorized, false));
            AgentInteractionAuditQueryHandlerBase handler = Handler(index, reader, tenant);

            QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType), CancellationToken.None);

            JsonSerializer.Serialize(result.GetPayload()).ShouldNotContain("poison");
            await reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default!, default);
        }
    }

    [Fact]
    public async Task ScopedAbsenceStillUsesTheHandlerMissingStatePayload()
    {
        for (int index = 0; index < 9; index++)
        {
            AgentInteractionAuditQueryHandlerBase handler = Handler(index,
                Reader(AgentInteractionAuditStateReadResult.Available("tenant-1", "interaction-1", null)), Authorized());

            QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType), CancellationToken.None);

            result.Success.ShouldBeTrue();
            string json = JsonSerializer.Serialize(result.GetPayload());
            json.ShouldNotContain("poison");
            if (index == 8)
            {
                json.ShouldContain("Unknown");
            }
            else
            {
                json.ShouldContain("NotFound");
            }
        }
    }

    [Fact]
    public async Task CancellationDuringAuthorizationStopsBeforeStateAccess()
    {
        for (int index = 0; index < 9; index++)
        {
            using var cancellation = new CancellationTokenSource();
            IAgentInteractionAuditStateReader reader = Reader(AgentInteractionAuditStateReadResult.Available("tenant-1", "interaction-1", State()));
            ITenantAccessReader tenant = Substitute.For<ITenantAccessReader>();
            tenant.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    cancellation.Cancel();
                    return new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true);
                });
            AgentInteractionAuditQueryHandlerBase handler = Handler(index, reader, tenant);

            await Should.ThrowAsync<OperationCanceledException>(() => handler.ExecuteAsync(Envelope(handler.QueryType), cancellation.Token));

            await reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default!, default);
        }
    }

    [Fact]
    public async Task CancellationDuringStateReadStopsBeforeRendering()
    {
        for (int index = 0; index < 9; index++)
        {
            using var cancellation = new CancellationTokenSource();
            IAgentInteractionAuditStateReader reader = Substitute.For<IAgentInteractionAuditStateReader>();
            reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    cancellation.Cancel();
                    return AgentInteractionAuditStateReadResult.Available("tenant-1", "interaction-1", State());
                });
            AgentInteractionAuditQueryHandlerBase handler = Handler(index, reader, Authorized());

            await Should.ThrowAsync<OperationCanceledException>(() => handler.ExecuteAsync(Envelope(handler.QueryType), cancellation.Token));
        }
    }

    private static IAgentInteractionAuditStateReader Reader(AgentInteractionAuditStateReadResult read)
    {
        IAgentInteractionAuditStateReader reader = Substitute.For<IAgentInteractionAuditStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(read);
        return reader;
    }

    private static ITenantAccessReader Authorized()
    {
        ITenantAccessReader tenant = Substitute.For<ITenantAccessReader>();
        tenant.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true));
        return tenant;
    }

    private static AgentInteractionAuditQueryHandlerBase Handler(int index, IAgentInteractionAuditStateReader reader, ITenantAccessReader tenant)
        => index switch
        {
            0 => new GetAgentInteractionStatusQueryHandler(reader, tenant),
            1 => new GetAgentInteractionGateEvidenceQueryHandler(reader, tenant),
            2 => new GetAgentInteractionContextEvidenceQueryHandler(reader, tenant),
            3 => new GetAgentGenerationEvidenceQueryHandler(reader, tenant),
            4 => new GetAgentPostingEvidenceQueryHandler(reader, tenant),
            5 => new GetAgentProposalEditEvidenceQueryHandler(reader, tenant),
            6 => new GetAgentProposalRegenerationEvidenceQueryHandler(reader, tenant),
            7 => new GetAgentProposalApprovalEvidenceQueryHandler(reader, tenant),
            _ => new GetAgentAuditAvailabilityQueryHandler(reader, tenant),
        };

    private static QueryEnvelope Envelope(string queryType)
        => new("tenant-1", "agent-interaction", "interaction-1", queryType, [], "corr-1", "user-1", isGlobalAdmin: true);

    private static AgentInteractionState State()
        => new()
        {
            IsRequested = true,
            AgentInteractionId = "interaction-1",
            AgentId = "poison-agent",
            CallerPartyId = "poison-party",
            SourceConversationId = "poison-conversation",
            Prompt = "poison-prompt",
            Status = AgentInteractionStatus.Requested,
            GeneratedVersions = [new("poison-version", "poison-attempt", AgentGenerationKind.Generated, "poison-content", "poison-provider", "poison-model", 1, 1, 1, 1)],
            PostingEvidence = new("poison-message", "poison-conversation", "poison-party", "poison-version"),
            ProposalEditEvidence = new("poison-proposal", "poison-conversation", "poison-edited-version", "poison-source-version",
                "poison-editor", 1, ApproverPolicyValidationStatus.Valid, ApproverPolicyBasisDisclosure.UserVisible),
            ProposalRegenerationEvidence = new("poison-proposal", "poison-conversation", "poison-regenerated-version", "poison-attempt",
                "poison-requester", "poison-provider", "poison-model", 1, 1, 1, ApproverPolicyValidationStatus.Valid, ApproverPolicyBasisDisclosure.UserVisible),
            ProposalApprovalEvidence = new("poison-proposal", "poison-conversation", "poison-approved-version", "poison-approver",
                1, ApproverPolicyValidationStatus.Valid, ApproverPolicyBasisDisclosure.UserVisible, "poison-party", "poison-message", "poison-key", "poison-posted-message"),
        };
}
