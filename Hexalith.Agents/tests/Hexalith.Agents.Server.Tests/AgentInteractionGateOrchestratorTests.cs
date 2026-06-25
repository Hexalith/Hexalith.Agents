namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentInteractionGateOrchestrator"/> (Story 2.2; AC1–AC4; AD-3, AD-12, AD-14). Verify that
/// all-ready reads assemble all-<c>Satisfied</c> verdicts and a dispatched <see cref="EvaluateAgentInteractionGate"/>
/// returning <c>Authorized</c>; that each reader's denial/disabled/missing/stale/unavailable maps to the correct
/// <c>(Check, Outcome)</c> verdict and <c>Denied</c>/<c>Blocked</c> decision; that a reader that throws or returns
/// not-available fails closed to <c>Unavailable</c> (no unhandled exception, no raw error text — AD-14); that reserved
/// client extensions are stripped and only server-read verdicts are dispatched; that no provider adapter / Conversations
/// post / proposal is on the path (AC2); and that all-deferred defaults fail closed to <c>Denied</c>.
/// </summary>
public sealed class AgentInteractionGateOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentInteractionId = "interaction-001";
    private const string AgentId = "hexa";
    private const string CallerPartyId = "party-001";
    private const string AgentPartyId = "agent-party-001";
    private const string SourceConversationId = "conversation-001";
    private const string ProviderId = "openai";
    private const string ModelId = "gpt-4o";

    private readonly ITenantAccessReader _tenantAccessReader = Substitute.For<ITenantAccessReader>();
    private readonly IConversationAccessReader _conversationAccessReader = Substitute.For<IConversationAccessReader>();
    private readonly IAgentInvocationReadinessReader _readinessReader = Substitute.For<IAgentInvocationReadinessReader>();
    private readonly IAgentPartyDirectory _partyDirectory = Substitute.For<IAgentPartyDirectory>();
    private readonly IProviderCatalogReader _providerCatalogReader = Substitute.For<IProviderCatalogReader>();
    private readonly IApproverPolicyResolver _approverPolicyResolver = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _lastDispatched;

    private AgentInteractionGateOrchestrator Orchestrator => new(
        _tenantAccessReader,
        _conversationAccessReader,
        _readinessReader,
        _partyDirectory,
        _providerCatalogReader,
        _approverPolicyResolver,
        _dispatcher);

    // ===== All ready → Authorized (AC1) =====

    [Fact]
    public async Task All_ready_reads_assemble_all_satisfied_verdicts_and_return_authorized()
    {
        StubAllReady();
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.AgentInteractionId.ShouldBe(AgentInteractionId);
        outcome.Status.ShouldBe(AgentInteractionStatus.Authorized);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(EvaluateAgentInteractionGate));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(AgentInteractionId);

        IReadOnlyList<AgentInvocationGateVerdict> verdicts = DispatchedVerdicts();
        verdicts.Count.ShouldBe(10); // one per AgentInteractionGateCheck, in evaluation order
        verdicts.Select(v => v.Check).ShouldBe(EvaluationOrder());
        verdicts.ShouldAllBe(v => v.Outcome == AgentInteractionGateOutcome.Satisfied);
    }

    [Fact]
    public async Task Confirmation_mode_with_a_resolvable_approver_policy_is_satisfied()
    {
        StubAllReady();
        var policy = new AgentApproverPolicy(
            [new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "party-approver", null)],
            ApproverPolicyBasisDisclosure.OperatorOnly);
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { ResponseMode = AgentResponseMode.Confirmation, ApproverPolicy = policy });
        _approverPolicyResolver.ResolveAsync(TenantId, policy, Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([new ApproverSourceResolution(ApproverPolicySourceKind.PredefinedParty, ApproverSourceOutcome.Resolved)]));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Authorized);
        VerdictFor(AgentInteractionGateCheck.ResponsePolicy).ShouldBe(AgentInteractionGateOutcome.Satisfied);
    }

    // ===== Per-reader verdict mapping + decision (AC1, AC2, AC3) =====

    [Fact]
    public async Task Tenant_access_unauthorized_maps_to_denied()
    {
        StubAllReady();
        _tenantAccessReader.ReadAsync(TenantId, Arg.Any<string>(), CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Unauthorized, IsFresh: true));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Denied);
        VerdictFor(AgentInteractionGateCheck.TenantAccess).ShouldBe(AgentInteractionGateOutcome.Unauthorized);
    }

    [Fact]
    public async Task Caller_party_missing_maps_to_denied()
    {
        StubAllReady();
        _partyDirectory.ValidateExistingPartyAsync(TenantId, CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Missing, null));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Denied);
        VerdictFor(AgentInteractionGateCheck.CallerPartyState).ShouldBe(AgentInteractionGateOutcome.Missing);
    }

    [Fact]
    public async Task Source_conversation_stale_maps_to_denied()
    {
        StubAllReady();
        _conversationAccessReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new ConversationAccessReadResult(AgentInteractionGateOutcome.Stale, IsFresh: true));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Denied);
        VerdictFor(AgentInteractionGateCheck.SourceConversationAccess).ShouldBe(AgentInteractionGateOutcome.Stale);
    }

    [Fact]
    public async Task Disabled_agent_lifecycle_maps_to_blocked()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { Lifecycle = AgentLifecycleStatus.Disabled });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.AgentLifecycle).ShouldBe(AgentInteractionGateOutcome.Disabled);
    }

    [Fact]
    public async Task Draft_agent_lifecycle_maps_to_missing_blocked()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { Lifecycle = AgentLifecycleStatus.Draft });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.AgentLifecycle).ShouldBe(AgentInteractionGateOutcome.Missing);
    }

    [Fact]
    public async Task Missing_agent_party_identity_maps_to_missing_blocked()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { HasPartyIdentity = false, PartyId = null });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.AgentPartyIdentity).ShouldBe(AgentInteractionGateOutcome.Missing);
    }

    [Fact]
    public async Task Invalid_agent_party_identity_maps_via_the_directory_to_blocked()
    {
        StubAllReady();
        _partyDirectory.ValidateExistingPartyAsync(TenantId, AgentPartyId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Disabled, null));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.AgentPartyIdentity).ShouldBe(AgentInteractionGateOutcome.Disabled);
    }

    [Fact]
    public async Task Non_selectable_provider_entry_maps_to_disabled_blocked()
    {
        StubAllReady();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(selectable: false)));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ProviderModelReadiness).ShouldBe(AgentInteractionGateOutcome.Disabled);
    }

    [Fact]
    public async Task Provider_entry_not_found_maps_to_missing_blocked()
    {
        StubAllReady();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ProviderModelReadiness).ShouldBe(AgentInteractionGateOutcome.Missing);
    }

    [Fact]
    public async Task Unknown_response_mode_maps_to_missing_blocked()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { ResponseMode = AgentResponseMode.Unknown });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ResponsePolicy).ShouldBe(AgentInteractionGateOutcome.Missing);
    }

    [Fact]
    public async Task Confirmation_mode_with_an_unresolvable_approver_policy_maps_to_blocked()
    {
        StubAllReady();
        var policy = new AgentApproverPolicy(
            [new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "party-approver", null)],
            ApproverPolicyBasisDisclosure.OperatorOnly);
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { ResponseMode = AgentResponseMode.Confirmation, ApproverPolicy = policy });
        _approverPolicyResolver.ResolveAsync(TenantId, policy, Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([new ApproverSourceResolution(ApproverPolicySourceKind.PredefinedParty, ApproverSourceOutcome.Ambiguous)]));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ResponsePolicy).ShouldBe(AgentInteractionGateOutcome.Ambiguous);
    }

    [Fact]
    public async Task Missing_content_safety_policy_maps_to_missing_blocked()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { HasActiveContentSafetyPolicy = false });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ContentSafetyPolicy).ShouldBe(AgentInteractionGateOutcome.Missing);
    }

    [Fact]
    public async Task Production_like_generation_not_enabled_maps_launch_readiness_to_missing_blocked()
    {
        // Story 4.4: an available Agent that has not had production-like generation enabled fails the readiness-class
        // LaunchReadiness check (Missing → Blocked). The pure decision policy needs no change — the new verdict folds in.
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { ProductionLikeGenerationEnabled = false });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.LaunchReadiness).ShouldBe(AgentInteractionGateOutcome.Missing);
        VerdictFor(AgentInteractionGateCheck.AgentLifecycle).ShouldBe(AgentInteractionGateOutcome.Satisfied); // launch readiness is a distinct check
    }

    [Fact]
    public async Task Stale_projection_maps_dependency_freshness_to_stale_blocked()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { IsFresh = false });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.DependencyFreshness).ShouldBe(AgentInteractionGateOutcome.Stale);
        VerdictFor(AgentInteractionGateCheck.AgentLifecycle).ShouldBe(AgentInteractionGateOutcome.Satisfied); // freshness is a distinct check
    }

    // ===== Fail closed on throw / not-available (AC1, AC3; AD-14) =====

    [Fact]
    public async Task A_reader_that_throws_fails_closed_to_unavailable_without_an_unhandled_exception()
    {
        StubAllReady();
        _tenantAccessReader.ReadAsync(TenantId, Arg.Any<string>(), CallerPartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("projection blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Denied); // tenant access is authorization-class
        VerdictFor(AgentInteractionGateCheck.TenantAccess).ShouldBe(AgentInteractionGateOutcome.Unavailable);

        // AD-14: no raw error text crosses the boundary — the dispatched command and outcome carry only safe enums.
        string outcomeJson = JsonSerializer.Serialize(outcome);
        outcomeJson.ShouldNotContain("secret-bearing detail");
        JsonSerializer.Serialize(DispatchedVerdicts()).ShouldNotContain("secret-bearing detail");
    }

    [Fact]
    public async Task A_not_available_readiness_read_fails_closed_every_readiness_check_to_unavailable()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(AgentInvocationReadiness.NotAvailable);
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked); // authorization checks still satisfied
        VerdictFor(AgentInteractionGateCheck.AgentLifecycle).ShouldBe(AgentInteractionGateOutcome.Unavailable);
        VerdictFor(AgentInteractionGateCheck.AgentPartyIdentity).ShouldBe(AgentInteractionGateOutcome.Unavailable);
        VerdictFor(AgentInteractionGateCheck.ResponsePolicy).ShouldBe(AgentInteractionGateOutcome.Unavailable);
        VerdictFor(AgentInteractionGateCheck.ContentSafetyPolicy).ShouldBe(AgentInteractionGateOutcome.Unavailable);
        VerdictFor(AgentInteractionGateCheck.LaunchReadiness).ShouldBe(AgentInteractionGateOutcome.Unavailable);
        VerdictFor(AgentInteractionGateCheck.DependencyFreshness).ShouldBe(AgentInteractionGateOutcome.Stale);
    }

    // ===== Trust model: reserved extensions stripped; server verdicts win (AC3) =====

    [Fact]
    public async Task Strips_client_forged_reserved_extensions_and_preserves_benign_ones()
    {
        StubAllReady();
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",
            ["provider:selectionValidation"] = "Valid",
            ["approver:policyValidation"] = "Valid",
            ["party:linkValidation"] = "Valid",
            ["audit:governanceResolved"] = "true",
            ["trace"] = "abc-123",
        };

        await Orchestrator.ExecuteAsync(Request(clientExtensions: clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions.ShouldNotContainKey("actor:agentsAdmin");
        dispatched.Extensions.ShouldNotContainKey("provider:selectionValidation");
        dispatched.Extensions.ShouldNotContainKey("approver:policyValidation");
        dispatched.Extensions.ShouldNotContainKey("party:linkValidation");
        dispatched.Extensions.ShouldNotContainKey("audit:governanceResolved");
        dispatched.Extensions["trace"].ShouldBe("abc-123");
    }

    [Fact]
    public async Task Only_server_read_verdicts_are_dispatched_the_request_carries_none()
    {
        // The request has no verdict field, so a client cannot supply verdicts — the dispatched verdicts come ONLY from
        // the server reads. A blocking server read therefore appears verbatim; nothing client-shaped can override it.
        StubAllReady();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(selectable: false)));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        DispatchedVerdicts().Count.ShouldBe(10);
        VerdictFor(AgentInteractionGateCheck.ProviderModelReadiness).ShouldBe(AgentInteractionGateOutcome.Disabled);
    }

    // ===== No side effects (AC2) =====

    [Fact]
    public async Task Only_reads_and_one_gate_dispatch_no_provider_invocation_or_conversation_post()
    {
        StubAllReady();
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        // The orchestrator's only collaborators are read ports + the command dispatcher — there is structurally no
        // provider adapter, no IConversationClient.AppendMessageAsync, and no proposal creation on this path (AC2). The
        // conversation seam is a READ (IConversationAccessReader.ReadAsync), and exactly one gate command is dispatched.
        await _conversationAccessReader.Received(1).ReadAsync(TenantId, SourceConversationId, CallerPartyId, Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        LastDispatched().ShouldNotBeNull().CommandType.ShouldBe(nameof(EvaluateAgentInteractionGate));
    }

    [Fact]
    public async Task Builds_the_envelope_with_the_trusted_scope_from_the_request()
    {
        StubAllReady();
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.TenantId.ShouldBe(TenantId);
        dispatched.MessageId.ShouldBe("msg-1");
        dispatched.CorrelationId.ShouldBe("corr-1");
        dispatched.UserId.ShouldBe("caller-user");
        dispatched.CausationId.ShouldBeNull();
    }

    // ===== All-deferred defaults fail closed to Denied (AC1; FR-21) =====

    [Fact]
    public async Task All_deferred_defaults_fail_closed_to_denied()
    {
        // The three new ports use their real Deferred* placeholders (Unavailable / not-available); the reused
        // provider-catalog reader throws (caught → Unavailable); the party directory fails closed to Unavailable. Tenant
        // access is Unavailable and authorization-class, so the gate decides Denied — the correct safe default.
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        var partyDirectory = Substitute.For<IAgentPartyDirectory>();
        partyDirectory.ValidateExistingPartyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));

        var orchestrator = new AgentInteractionGateOrchestrator(
            new DeferredTenantAccessReader(),
            new DeferredConversationAccessReader(),
            new DeferredAgentInvocationReadinessReader(),
            partyDirectory,
            new DeferredProviderCatalogReader(),
            new DeferredApproverPolicyResolver(),
            dispatcher);

        AgentInteractionGateOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Denied);
        await dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Cross-seam: the dispatched gate command drives the real pure aggregate =====

    [Fact]
    public async Task End_to_end_the_dispatched_gate_command_drives_the_aggregate_to_the_same_decision()
    {
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { Lifecycle = AgentLifecycleStatus.Disabled });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<EvaluateAgentInteractionGate>(dispatched.Payload)!;

        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, AgentId, CallerPartyId, SourceConversationId, SampleSnapshot(), "prompt", "idem"));
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched);

        AgentInteractionGateFailed failed = result.Events[0].ShouldBeOfType<AgentInteractionGateFailed>();
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        failed.Decision.ShouldBe(outcome.Status);
        failed.Decision.ShouldBe(AgentInteractionStatus.Blocked);
    }

    // ===== QA gap coverage: untested verdict-mapping branches (AC1) =====

    [Fact]
    public async Task Empty_provider_or_model_id_short_circuits_to_missing_without_reading_the_catalog()
    {
        // The orchestrator skips the catalog read when the snapshot recorded no provider/model — the readiness check is
        // Missing (readiness-class → Blocked) and the reused catalog reader is never consulted (AD-9).
        StubAllReady();
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request() with { ProviderId = string.Empty }, CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ProviderModelReadiness).ShouldBe(AgentInteractionGateOutcome.Missing);
        await _providerCatalogReader.DidNotReceive().GetEntryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirmation_mode_with_no_approver_policy_maps_to_missing_blocked()
    {
        // Confirmation mode with nothing to confirm with fails closed (AD-8) — without calling the resolver.
        StubAllReady();
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { ResponseMode = AgentResponseMode.Confirmation, ApproverPolicy = null });
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ResponsePolicy).ShouldBe(AgentInteractionGateOutcome.Missing);
        await _approverPolicyResolver.DidNotReceive().ResolveAsync(Arg.Any<string>(), Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Caller_party_unauthorized_maps_to_denied()
    {
        // The caller's Party verdict is authorization-class; an Unauthorized outcome decides Denied (AC3).
        StubAllReady();
        _partyDirectory.ValidateExistingPartyAsync(TenantId, CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Unauthorized, null));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Denied);
        VerdictFor(AgentInteractionGateCheck.CallerPartyState).ShouldBe(AgentInteractionGateOutcome.Unauthorized);
    }

    [Fact]
    public async Task Agent_party_unauthorized_maps_to_blocked_not_denied()
    {
        // An Unauthorized OUTCOME on a readiness-class CHECK (the Agent's own linked Party) still decides Blocked, not
        // Denied — the decision class follows the check, never the outcome value.
        StubAllReady();
        _partyDirectory.ValidateExistingPartyAsync(TenantId, AgentPartyId, Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Unauthorized, null));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.AgentPartyIdentity).ShouldBe(AgentInteractionGateOutcome.Unauthorized);
    }

    [Fact]
    public async Task Provider_read_not_authorized_fails_closed_to_unavailable_without_leaking_existence()
    {
        // A not-authorized provider read must not reveal whether the entry exists in another tenant — it fails closed to
        // Unavailable (AC3), identical to a degraded read.
        StubAllReady();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ProviderModelReadiness).ShouldBe(AgentInteractionGateOutcome.Unavailable);
    }

    [Fact]
    public async Task Provider_read_success_with_a_null_entry_is_a_degraded_read_failing_closed_to_unavailable()
    {
        // Success status but no entry payload is a degraded read — it must fail closed rather than be treated as ready.
        StubAllReady();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, null));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ProviderModelReadiness).ShouldBe(AgentInteractionGateOutcome.Unavailable);
    }

    // ===== QA gap coverage: DependencyFreshness aggregates EVERY consulted projection (AD-12) =====

    [Fact]
    public async Task Stale_tenant_projection_maps_dependency_freshness_to_stale_while_tenant_access_stays_satisfied()
    {
        // Freshness is a distinct check: a behind-threshold Tenants projection blocks on DependencyFreshness even though
        // the tenant-access OUTCOME itself was satisfied.
        StubAllReady();
        _tenantAccessReader.ReadAsync(TenantId, Arg.Any<string>(), CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: false));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.DependencyFreshness).ShouldBe(AgentInteractionGateOutcome.Stale);
        VerdictFor(AgentInteractionGateCheck.TenantAccess).ShouldBe(AgentInteractionGateOutcome.Satisfied);
    }

    [Fact]
    public async Task Stale_conversation_read_maps_dependency_freshness_to_stale_while_conversation_access_stays_satisfied()
    {
        StubAllReady();
        _conversationAccessReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new ConversationAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: false));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.DependencyFreshness).ShouldBe(AgentInteractionGateOutcome.Stale);
        VerdictFor(AgentInteractionGateCheck.SourceConversationAccess).ShouldBe(AgentInteractionGateOutcome.Satisfied);
    }

    // ===== QA gap coverage: per-reader fail-closed catches (FR-21; AD-14) =====

    [Fact]
    public async Task A_conversation_reader_that_throws_fails_closed_to_unavailable_denied()
    {
        StubAllReady();
        _conversationAccessReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("conversations transport blew up"));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Denied); // Source Conversation access is authorization-class
        VerdictFor(AgentInteractionGateCheck.SourceConversationAccess).ShouldBe(AgentInteractionGateOutcome.Unavailable);
        JsonSerializer.Serialize(DispatchedVerdicts()).ShouldNotContain("transport blew up");
    }

    [Fact]
    public async Task A_provider_catalog_reader_that_throws_fails_closed_to_unavailable_blocked()
    {
        StubAllReady();
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("catalog projection blew up"));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ProviderModelReadiness).ShouldBe(AgentInteractionGateOutcome.Unavailable);
    }

    [Fact]
    public async Task An_approver_policy_resolver_that_throws_fails_closed_to_unavailable_blocked()
    {
        StubAllReady();
        var policy = new AgentApproverPolicy(
            [new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "party-approver", null)],
            ApproverPolicyBasisDisclosure.OperatorOnly);
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness() with { ResponseMode = AgentResponseMode.Confirmation, ApproverPolicy = policy });
        _approverPolicyResolver.ResolveAsync(TenantId, policy, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("approver resolver blew up"));
        CaptureDispatch();

        AgentInteractionGateOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.Blocked);
        VerdictFor(AgentInteractionGateCheck.ResponsePolicy).ShouldBe(AgentInteractionGateOutcome.Unavailable);
    }

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_is_never_masked_as_a_fail_closed_verdict()
    {
        // The per-reader fail-closed catches deliberately exclude OperationCanceledException — a real cancellation must
        // bubble up (not be silently swallowed into an Unavailable verdict), and no gate command is dispatched.
        StubAllReady();
        _tenantAccessReader.ReadAsync(TenantId, Arg.Any<string>(), CallerPartyId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Helpers =====

    private static IReadOnlyList<AgentInteractionGateCheck> EvaluationOrder() =>
    [
        AgentInteractionGateCheck.TenantAccess,
        AgentInteractionGateCheck.CallerPartyState,
        AgentInteractionGateCheck.SourceConversationAccess,
        AgentInteractionGateCheck.AgentLifecycle,
        AgentInteractionGateCheck.AgentPartyIdentity,
        AgentInteractionGateCheck.ProviderModelReadiness,
        AgentInteractionGateCheck.ResponsePolicy,
        AgentInteractionGateCheck.ContentSafetyPolicy,
        AgentInteractionGateCheck.DependencyFreshness,
        AgentInteractionGateCheck.LaunchReadiness,
    ];

    private void StubAllReady()
    {
        _tenantAccessReader.ReadAsync(TenantId, Arg.Any<string>(), CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        _conversationAccessReader.ReadAsync(TenantId, SourceConversationId, CallerPartyId, Arg.Any<CancellationToken>())
            .Returns(new ConversationAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        _readinessReader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>())
            .Returns(Readiness());
        _partyDirectory.ValidateExistingPartyAsync(TenantId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, AgentPartyId));
        _providerCatalogReader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>())
            .Returns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, Entry(selectable: true)));
    }

    private static AgentInvocationReadiness Readiness()
        => new(
            IsAvailable: true,
            AgentLifecycleStatus.Active,
            HasPartyIdentity: true,
            PartyId: AgentPartyId,
            AgentResponseMode.Automatic,
            HasActiveContentSafetyPolicy: true,
            ProviderId,
            ModelId,
            ApproverPolicy: null,
            IsFresh: true,
            ProductionLikeGenerationEnabled: true);

    private static ProviderCatalogEntryView Entry(bool selectable)
        => new(
            ProviderId,
            ModelId,
            "OpenAI GPT-4o",
            ProviderModelStatus.Enabled,
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 128_000,
            MaxOutputTokenLimit: 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            IsSelectableForNewActiveUse: selectable,
            CapabilityVersion: 1);

    private static AgentInteractionSnapshot SampleSnapshot()
        => new(
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            AgentResponseMode.Automatic,
            ApproverPolicyVersion: 1,
            ProviderId,
            ModelId,
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion: 1,
            AgentInteractionSnapshot.DefaultContextPolicyReference);

    private static AgentInteractionGateRequest Request(IReadOnlyDictionary<string, string>? clientExtensions = null)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentInteractionId,
            AgentId,
            ActorUserId: "caller-user",
            CallerPartyId,
            SourceConversationId,
            ProviderId,
            ModelId,
            AgentResponseMode.Automatic,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;

    private IReadOnlyList<AgentInvocationGateVerdict> DispatchedVerdicts()
        => JsonSerializer.Deserialize<EvaluateAgentInteractionGate>(LastDispatched()!.Payload)!.Verdicts;

    private AgentInteractionGateOutcome VerdictFor(AgentInteractionGateCheck check)
        => DispatchedVerdicts().Single(v => v.Check == check).Outcome;
}
