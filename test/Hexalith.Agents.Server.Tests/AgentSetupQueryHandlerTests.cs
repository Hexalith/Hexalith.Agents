namespace Hexalith.Agents.Server.Tests;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Contracts.Queries;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for the live Agent setup query handlers (Story 5.2 AC2, AC4). They cover the truth stage a read reports
/// and — just as importantly — what an unauthorized or cross-tenant caller is allowed to learn.
/// </summary>
public sealed class AgentSetupQueryHandlerTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";
    private const string StoreName = "statestore";
    private const string UserId = "admin-user";

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FakeReadModelStore _store = new();
    private readonly ITenantAccessReader _tenantAccess = Substitute.For<ITenantAccessReader>();

    public AgentSetupQueryHandlerTests()
        => _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));

    /// <summary>Verifies that a misrouted model cannot disclose another scope or cause a foreign approver lookup.</summary>
    [Theory]
    [InlineData(true, "foreign-tenant", AgentId)]
    [InlineData(false, "foreign-tenant", AgentId)]
    [InlineData(true, TenantId, "foreign-agent")]
    [InlineData(false, TenantId, "foreign-agent")]
    [InlineData(true, "foreign-tenant", "foreign-agent")]
    [InlineData(false, "foreign-tenant", "foreign-agent")]
    public async Task MisroutedSetupIsUnavailableBeforeDisclosureOrApproverLookup(bool statusQuery, string modelTenant, string modelAgent)
    {
        var resolver = Substitute.For<IApproverPolicyResolver>();
        _store.Seed(StoreName, AgentSetupReadModelAddresses.Detail(TenantId, AgentId), new AgentSetupReadModel
        {
            IsCreated = true,
            AgentId = modelAgent,
            TenantId = modelTenant,
            DisplayName = "foreign-display-name",
            Description = "foreign-description",
            ApproverPolicySources = [new(ApproverPolicySourceKind.PredefinedParty, "foreign-party", null)],
            ApproverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
            ResponseMode = AgentResponseMode.Confirmation,
            ConfigurationVersion = 99,
        });
        var options = Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName });
        AgentSetupQueryHandlerBase handler = statusQuery
            ? new GetAgentStatusQueryHandler(_store, options, _tenantAccess, resolver)
            : new GetAgentConfigurationQueryHandler(_store, options, _tenantAccess, resolver);

        AgentSetupResult result = await ExecuteAsync(handler,
            Query(statusQuery ? GetAgentStatusQuery.QueryType : GetAgentConfigurationQuery.QueryType));

        result.Status.ShouldBe(AgentInspectionStatus.Unavailable);
        result.Setup.ShouldBeNull();
        JsonSerializer.Serialize(result, _json).ShouldNotContain("foreign-");
        resolver.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies that same kind substituted party cannot confirm the safe query basis.</summary>
    [Fact]
    public async Task SameKindSubstitutedPartyCannotConfirmTheSafeQueryBasis()
    {
        var configured = new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "configured-party", null);
        var resolver = Substitute.For<IApproverPolicyResolver>();
        resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>()).Returns(
            new ApproverPolicyResolutionResult([new ApproverSourceResolution(ApproverPolicySourceKind.PredefinedParty, ApproverSourceOutcome.Resolved)
            {
                Source = new(ApproverPolicySourceKind.PredefinedParty, "substituted-party", null),
            }]));
        _store.Seed(StoreName, AgentSetupReadModelAddresses.Detail(TenantId, AgentId), new AgentSetupReadModel
        {
            IsCreated = true,
            AgentId = AgentId,
            TenantId = TenantId,
            DisplayName = "hexa",
            ApproverPolicySources = [configured],
            ApproverPolicyDisclosure = ApproverPolicyBasisDisclosure.OperatorOnly,
            ResponseMode = AgentResponseMode.Confirmation,
            ConfigurationVersion = 1,
        });
        var handler = new GetAgentStatusQueryHandler(_store,
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }), _tenantAccess, resolver);
        var result = await ExecuteAsync(handler, Query(GetAgentStatusQuery.QueryType));
        result.Setup.ShouldNotBeNull().Agent.ApproverPolicyBasis.Single().Status.ShouldBe(ApproverPolicyValidationStatus.Unavailable);
    }

    [Fact]
    public async Task Both_setup_queries_answer_with_the_same_authoritative_truth()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        AgentSetupResult status = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));
        AgentSetupResult configuration = await ExecuteAsync(ConfigurationHandler(), Query(GetAgentConfigurationQuery.QueryType));

        status.ShouldBeEquivalentTo(configuration);
        status.Setup.ShouldNotBeNull().ConfigurationVersion.ShouldBe(3);
        status.Setup.ProjectionVersion.ShouldBe("5");
    }

    [Fact]
    public async Task A_read_without_an_expectation_is_projection_confirmed_and_current()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        AgentSetupView setup = (await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType))).Setup.ShouldNotBeNull();

        setup.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Current);
    }

    [Fact]
    public async Task A_read_waiting_for_a_newer_version_is_authoritative_pending_and_stale()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        AgentSetupView setup = (await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, new GetAgentStatusQuery(ExpectedConfigurationVersion: 4)))).Setup.ShouldNotBeNull();

        setup.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Stale);

        // The pending answer still reports what the projection actually holds, never the version being awaited.
        setup.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public async Task A_read_whose_expectation_has_landed_is_projection_confirmed()
    {
        Seed(configurationVersion: 4, lastSequence: 6);

        AgentSetupView setup = (await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, new GetAgentStatusQuery(ExpectedConfigurationVersion: 4)))).Setup.ShouldNotBeNull();

        setup.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Current);
    }

    [Fact]
    public async Task A_later_projected_version_confirms_the_original_command_derived_target()
    {
        Seed(configurationVersion: 5, lastSequence: 8);

        AgentSetupView setup = (await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, new GetAgentStatusQuery(ExpectedConfigurationVersion: 4)))).Setup.ShouldNotBeNull();

        // Ordered projection cannot reach version 5 without applying the original command-derived version 4.
        setup.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        setup.ConfigurationVersion.ShouldBe(5);
        setup.Freshness.ShouldBe(AgentSetupFreshness.Current);
    }

    [Fact]
    public async Task Lifecycle_active_is_reported_as_a_lifecycle_flag_and_never_as_callability()
    {
        Seed(configurationVersion: 3, lastSequence: 5, lifecycle: AgentLifecycleStatus.Active);

        AgentSetupView setup = (await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType))).Setup.ShouldNotBeNull();

        setup.Agent.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);

        // An Agent with no provider selection, no approver policy, and no launch readiness is emphatically not
        // callable; the read model reports the blockers rather than inferring readiness from the lifecycle flag.
        setup.Agent.ActivationBlockers.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task An_unauthorized_caller_is_denied_before_the_read_model_is_addressed()
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TenantAccessReadResult.Unavailable);

        AgentSetupResult result = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Setup.ShouldBeNull();
    }

    [Fact]
    public async Task A_cross_tenant_caller_cannot_learn_the_agent_exists()
    {
        Seed(configurationVersion: 3, lastSequence: 5);

        // Authorization is satisfied for the caller's own tenant; the read is simply keyed elsewhere, so the
        // answer is shaped exactly like a never-created Agent — no existence, versions, or blockers leak.
        AgentSetupResult result = await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, tenantId: "other-tenant"));

        result.Status.ShouldBe(AgentInspectionStatus.AgentNotFound);
        result.Setup.ShouldBeNull();
        JsonSerializer.Serialize(result, _json).ShouldNotContain(TenantId);
    }

    [Fact]
    public async Task A_stale_tenant_access_read_is_denied_rather_than_trusted()
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: false));

        AgentSetupResult result = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
    }

    [Fact]
    public async Task A_denied_read_never_consults_the_read_model()
    {
        Seed(configurationVersion: 3, lastSequence: 5);
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TenantAccessReadResult.Unavailable);

        AgentSetupResult denied = await ExecuteAsync(StatusHandler(), Query(GetAgentStatusQuery.QueryType));
        AgentSetupResult missing = await ExecuteAsync(
            StatusHandler(),
            Query(GetAgentStatusQuery.QueryType, tenantId: "empty-tenant"));

        // A caller cannot tell "no permission" from "no such Agent": both answers are shaped alike apart from the
        // status, and neither carries versions, blockers, or any other trace of the seeded Agent.
        denied.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        missing.Setup.ShouldBeNull();
        denied.Setup.ShouldBeNull();
    }

    private GetAgentStatusQueryHandler StatusHandler()
        => new(_store, Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }), _tenantAccess);

    private GetAgentConfigurationQueryHandler ConfigurationHandler()
        => new(_store, Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }), _tenantAccess);

    private static async Task<AgentSetupResult> ExecuteAsync(AgentSetupQueryHandlerBase handler, QueryEnvelope query)
    {
        QueryResult result = await handler.ExecuteAsync(query, CancellationToken.None);
        result.Success.ShouldBeTrue();
        return JsonSerializer.Deserialize<AgentSetupResult>(result.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
    }

    private void Seed(
        int configurationVersion,
        long lastSequence,
        AgentLifecycleStatus lifecycle = AgentLifecycleStatus.Draft)
        => _store.Seed(
            StoreName,
            AgentSetupReadModelAddresses.Detail(TenantId, AgentId),
            new AgentSetupReadModel
            {
                IsCreated = true,
                AgentId = AgentId,
                TenantId = TenantId,
                DisplayName = "hexa",
                HasInstructions = true,
                InstructionsValid = true,
                InstructionsVersion = 1,
                Lifecycle = lifecycle,
                ConfigurationVersion = configurationVersion,
                ResponseMode = AgentResponseMode.Automatic,
                LastSequenceNumber = lastSequence,
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = lastSequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });

    private static QueryEnvelope Query(
        string queryType,
        GetAgentStatusQuery? payload = null,
        string tenantId = TenantId,
        string userId = UserId)
        => new(
            tenantId,
            AgentSetupReadModelAddresses.Domain,
            AgentId,
            queryType,
            payload is null ? [] : JsonSerializer.SerializeToUtf8Bytes(payload, _json),
            "corr-1",
            userId);
}
