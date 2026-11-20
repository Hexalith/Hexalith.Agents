using System.Text.Json;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
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
            AgentInteractionAuditQueryHandlerBase handler = Handler(index, reader, Substitute.For<IAgentInteractionAuditAuthority>());

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
            IAgentInteractionAuditAuthority authority = Substitute.For<IAgentInteractionAuditAuthority>();
            authority.AuthorizeAsync(Arg.Any<AgentAuditReadTarget>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                cancellation.Cancel();
                return (AgentInteractionAuditGrant?)null;
            });
            AgentInteractionAuditQueryHandlerBase handler = Handler(index, reader, authority);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TenantAuthorityMustRemainCurrentUntilTheReadCompletes(bool expireDuringRead)
    {
        var clock = new Story54TimeProvider();
        AgentAuthorityValidity? lifetime = expireDuringRead ? new(clock, clock.Now, clock.Now.AddSeconds(1)) : null;
        ITenantAccessReader tenant = Substitute.For<ITenantAccessReader>();
        tenant.ReadAsync("tenant-1", "user-1", string.Empty, Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true)
            {
                Authority = lifetime,
                CurrentActor = AgentInteractionAuditQueryHandlerTests.Actor("tenant-1", "user-1"),
            });
        IAgentInteractionAuditStateReader reader = Substitute.For<IAgentInteractionAuditStateReader>();
        reader.ReadAsync("tenant-1", "interaction-1", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            clock.Now = clock.Now.AddSeconds(1);
            return AgentInteractionAuditStateReadResult.Available("tenant-1", "interaction-1", State());
        });
        var handler = new GetAgentInteractionStatusQueryHandler(reader, tenant);

        QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType), CancellationToken.None);

        result.GetPayload().GetProperty("status").GetString().ShouldBe("NotAuthorized");
        await reader.Received(expireDuringRead ? 1 : 0).ReadAsync("tenant-1", "interaction-1", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(AgentAuditAudience.TenantOperations, "approver", "poison-conversation", "proposal", null, false)]
    [InlineData(AgentAuditAudience.EligibleApprover, "approver", "poison-conversation", "proposal", null, true)]
    [InlineData(AgentAuditAudience.EligibleApprover, "approver", "other", "proposal", null, false)]
    [InlineData(AgentAuditAudience.EligibleApprover, "approver", "poison-conversation", "other", null, false)]
    [InlineData(AgentAuditAudience.EligibleApprover, "poison-party", "poison-conversation", "proposal", null, false)]
    [InlineData(AgentAuditAudience.ComplianceInspector, null, "poison-conversation", null, "case", true)]
    [InlineData(AgentAuditAudience.ComplianceInspector, null, "poison-conversation", null, null, false)]
    public async Task ContextRequiresTheMatchingProposalOrGovernedCase(AgentAuditAudience audience, string? party,
        string conversation, string? proposal, string? inspectionCase, bool allowed)
    {
        AgentInteractionState state = State();
        state.ProposalEvidence = new("proposal", "poison-conversation", "version", 1, 1, null);
        IAgentInteractionAuditStateReader reader = Reader(AgentInteractionAuditStateReadResult.Available("tenant-1", "interaction-1", state));
        var clock = new Story54TimeProvider();
        IAgentInteractionAuditAuthority authority = Substitute.For<IAgentInteractionAuditAuthority>();
        authority.AuthorizeAsync(Arg.Any<AgentAuditReadTarget>(), Arg.Any<CancellationToken>()).Returns(call =>
            new AgentInteractionAuditGrant(call.Arg<AgentAuditReadTarget>(), audience, new(clock, clock.Now, clock.Now.AddMinutes(1)),
                party, conversation, proposal, inspectionCase));
        var handler = new GetAgentInteractionContextEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority);

        QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType), CancellationToken.None);

        result.GetPayload().GetProperty("status").GetString().ShouldBe(allowed ? "Success" : "NotAuthorized");
    }

    [Theory]
    [InlineData("poison-party", true)]
    [InlineData("other-party", false)]
    public async Task CallerStatusDisclosesOnlyTheOutcomeOfTheirOwnCall(string party, bool allowed)
    {
        var clock = new Story54TimeProvider();
        IAgentInteractionAuditAuthority authority = Substitute.For<IAgentInteractionAuditAuthority>();
        authority.AuthorizeAsync(Arg.Any<AgentAuditReadTarget>(), Arg.Any<CancellationToken>()).Returns(call =>
            new AgentInteractionAuditGrant(call.Arg<AgentAuditReadTarget>(), AgentAuditAudience.Caller,
                new(clock, clock.Now, clock.Now.AddMinutes(1)), PartyId: party));
        var handler = new GetAgentInteractionStatusQueryHandler(
            Reader(AgentInteractionAuditStateReadResult.Available("tenant-1", "interaction-1", State())), new DeferredTenantAccessReader(), authority);

        QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType), CancellationToken.None);

        var payload = result.Payload<AgentInteractionInspectionResult>();
        payload.Status.ShouldBe(allowed ? AgentInteractionInspectionStatus.Success : AgentInteractionInspectionStatus.NotAuthorized);
        if (allowed)
        {
            payload.View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Requested);
        }
        JsonSerializer.Serialize(result.GetPayload()).ShouldNotContain("poison");
    }

    [Theory]
    [InlineData("tenant")]
    [InlineData("resource")]
    [InlineData("subject")]
    [InlineData("operation")]
    public async Task MisroutedAuthorityRefusesBeforeStateAccess(string fault)
    {
        var clock = new Story54TimeProvider();
        var expected = new AgentAuditReadTarget("tenant-1", "user-1", "interaction-1", GetAgentInteractionStatusQuery.QueryType);
        AgentAuditReadTarget returned = fault switch
        {
            "tenant" => expected with { TenantId = "other" },
            "resource" => expected with { InteractionId = "other" },
            "subject" => expected with { Subject = "other" },
            _ => expected with { QueryType = GetAgentInteractionContextEvidenceQuery.QueryType },
        };
        IAgentInteractionAuditAuthority authority = Substitute.For<IAgentInteractionAuditAuthority>();
        authority.AuthorizeAsync(expected, Arg.Any<CancellationToken>()).Returns(new AgentInteractionAuditGrant(
            returned, AgentAuditAudience.TenantOperations, new(clock, clock.Now, clock.Now.AddMinutes(1))));
        IAgentInteractionAuditStateReader reader = Reader(AgentInteractionAuditStateReadResult.Unavailable);
        var handler = new GetAgentInteractionStatusQueryHandler(reader, new DeferredTenantAccessReader(), authority);

        QueryResult result = await handler.ExecuteAsync(Envelope(handler.QueryType), CancellationToken.None);

        result.GetPayload().GetProperty("status").GetString().ShouldBe("NotAuthorized");
        await reader.DidNotReceiveWithAnyArgs().ReadAsync(default!, default!, default);
    }

    private static IAgentInteractionAuditStateReader Reader(AgentInteractionAuditStateReadResult read)
    {
        IAgentInteractionAuditStateReader reader = Substitute.For<IAgentInteractionAuditStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(read);
        return reader;
    }

    private static IAgentInteractionAuditAuthority Authorized()
    {
        var clock = new Story54TimeProvider();
        IAgentInteractionAuditAuthority authority = Substitute.For<IAgentInteractionAuditAuthority>();
        authority.AuthorizeAsync(Arg.Any<AgentAuditReadTarget>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            AgentAuditReadTarget target = call.Arg<AgentAuditReadTarget>();
            AgentAuditAudience audience = target.QueryType switch
            {
                GetAgentPostingEvidenceQuery.QueryType => AgentAuditAudience.Participant,
                GetAgentInteractionContextEvidenceQuery.QueryType or GetAgentProposalApprovalEvidenceQuery.QueryType
                    or GetAgentProposalEditEvidenceQuery.QueryType or GetAgentProposalRegenerationEvidenceQuery.QueryType
                    => AgentAuditAudience.ComplianceInspector,
                _ => AgentAuditAudience.TenantOperations,
            };
            return new AgentInteractionAuditGrant(target, audience, new(clock, clock.Now, clock.Now.AddMinutes(1)),
                ConversationId: "poison-conversation", InspectionCaseId: "case-1");
        });
        return authority;
    }

    private static AgentInteractionAuditQueryHandlerBase Handler(int index, IAgentInteractionAuditStateReader reader, IAgentInteractionAuditAuthority authority)
        => index switch
        {
            0 => new GetAgentInteractionStatusQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            1 => new GetAgentInteractionGateEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            2 => new GetAgentInteractionContextEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            3 => new GetAgentGenerationEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            4 => new GetAgentPostingEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            5 => new GetAgentProposalEditEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            6 => new GetAgentProposalRegenerationEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            7 => new GetAgentProposalApprovalEvidenceQueryHandler(reader, new DeferredTenantAccessReader(), authority),
            _ => new GetAgentAuditAvailabilityQueryHandler(reader, new DeferredTenantAccessReader(), authority),
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
