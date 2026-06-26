namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentInteractionRequestOrchestrator"/> and <see cref="AgentInteractionIdentity"/> (Story 2.1;
/// AC1, AC2, AC3, AC4; AD-3, AD-4, AD-13). Verify the deterministic id derivation (stable + regex-valid + colon-free),
/// that the AD-4 snapshot is read from <see cref="IAgentConfigurationSnapshotReader"/> and written into the dispatched
/// command (with the V1 context-policy reference stamped), that a safe <see cref="AgentInteractionReference"/> is
/// returned, that reserved client-forged extensions are stripped, that the not-available case still dispatches an
/// auditable request, and that no provider/Conversation client is touched (AC3).
/// </summary>
public sealed class AgentInteractionRequestOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";
    private const string SourceConversationId = "conversation-001";
    private const string CallerPartyId = "party-001";
    private const string Prompt = "Summarize the latest decisions in this thread.";
    private const string IdempotencyKey = "idem-001";

    private static readonly Regex _aggregateIdRegex = new(@"^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$");

    private readonly IAgentConfigurationSnapshotReader _reader = Substitute.For<IAgentConfigurationSnapshotReader>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private AgentInteractionRequestOrchestrator Orchestrator => new(_reader, _dispatcher);

    private CommandEnvelope? _lastDispatched;

    // ===== Snapshot read + command assembly (AC1) =====

    [Fact]
    public async Task Reads_snapshot_and_writes_it_into_the_dispatched_command_with_default_context_policy()
    {
        // A deliberately stale context-policy reference on the read must be overwritten with the V1 default.
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot(contextPolicyReference: "stale-ref")));
        CaptureDispatch();

        AgentInteractionRequestOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.SnapshotAvailable.ShouldBeTrue();
        outcome.Dispatched.ShouldBeTrue();
        outcome.Reference.Status.ShouldBe(AgentInteractionStatus.Requested);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(RequestAgentInteraction));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(ExpectedId());

        RequestAgentInteraction command = JsonSerializer.Deserialize<RequestAgentInteraction>(dispatched.Payload)!;
        command.AgentId.ShouldBe(AgentId);
        command.CallerPartyId.ShouldBe(CallerPartyId);
        command.SourceConversationId.ShouldBe(SourceConversationId);
        command.Prompt.ShouldBe(Prompt);
        command.Snapshot.ShouldNotBeNull();
        command.Snapshot!.ProviderId.ShouldBe("openai");
        command.Snapshot.ModelId.ShouldBe("gpt-4o");
        command.Snapshot.ContextPolicyReference.ShouldBe(AgentInteractionSnapshot.DefaultContextPolicyReference);

        await _reader.Received(1).ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_a_safe_reference_carrying_only_the_id_and_status()
    {
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot()));
        CaptureDispatch();

        AgentInteractionRequestOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        // The reference is the only thing returned to callers — id + coarse status, no stream name or provider detail.
        outcome.Reference.AgentInteractionId.ShouldBe(ExpectedId());
        outcome.Reference.Status.ShouldBe(AgentInteractionStatus.Requested);
    }

    // ===== Deterministic identity (AC2; AD-13) =====

    [Fact]
    public void Derives_a_stable_regex_valid_colon_free_id_for_identical_inputs()
    {
        string id1 = AgentInteractionIdentity.Derive(TenantId, AgentId, SourceConversationId, CallerPartyId, IdempotencyKey);
        string id2 = AgentInteractionIdentity.Derive(TenantId, AgentId, SourceConversationId, CallerPartyId, IdempotencyKey);

        id1.ShouldBe(id2);
        id1.Length.ShouldBeLessThanOrEqualTo(256);
        id1.ShouldNotContain(":");
        _aggregateIdRegex.IsMatch(id1).ShouldBeTrue();
    }

    [Fact]
    public void Different_identity_components_yield_different_ids()
    {
        string baseId = AgentInteractionIdentity.Derive(TenantId, AgentId, SourceConversationId, CallerPartyId, IdempotencyKey);

        AgentInteractionIdentity.Derive(TenantId, AgentId, SourceConversationId, CallerPartyId, "idem-002").ShouldNotBe(baseId);
        AgentInteractionIdentity.Derive(TenantId, "other-agent", SourceConversationId, CallerPartyId, IdempotencyKey).ShouldNotBe(baseId);
        AgentInteractionIdentity.Derive("other-tenant", AgentId, SourceConversationId, CallerPartyId, IdempotencyKey).ShouldNotBe(baseId);
        AgentInteractionIdentity.Derive(TenantId, AgentId, "other-conversation", CallerPartyId, IdempotencyKey).ShouldNotBe(baseId);
        AgentInteractionIdentity.Derive(TenantId, AgentId, SourceConversationId, "other-party", IdempotencyKey).ShouldNotBe(baseId);
    }

    [Fact]
    public void Derives_a_regex_valid_colon_free_id_even_when_components_contain_illegal_characters()
    {
        // Opaque caller-supplied references may contain colons, slashes, and whitespace — characters illegal in an
        // EventStore AggregateId. Hashing must neutralize them so the derived id is always regex-valid (AD-13).
        string id = AgentInteractionIdentity.Derive(
            "tenant:with:colons",
            "agent/with/slashes",
            "conversation 01\twith ws",
            "party#99",
            "idem key:002");

        id.ShouldNotContain(":");
        id.Length.ShouldBeLessThanOrEqualTo(256);
        _aggregateIdRegex.IsMatch(id).ShouldBeTrue();
    }

    [Fact]
    public void Length_prefix_framing_makes_component_boundaries_unambiguous()
    {
        // Without length-prefix framing, ("ab","c") and ("a","bc") would concatenate identically and collide. The
        // framing must keep them distinct (AD-13 collision-resistance).
        string left = AgentInteractionIdentity.Derive(TenantId, "ab", "c", CallerPartyId, IdempotencyKey);
        string right = AgentInteractionIdentity.Derive(TenantId, "a", "bc", CallerPartyId, IdempotencyKey);

        left.ShouldNotBe(right);
    }

    // ===== Trust model: reserved extensions stripped, server snapshot wins (AC4) =====

    [Fact]
    public async Task Strips_client_forged_reserved_extensions_and_preserves_benign_ones()
    {
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot()));
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",                 // forged admin
            ["provider:selectionValidation"] = "Valid",     // forged verdict
            ["approver:policyValidation"] = "Valid",        // forged verdict
            ["party:linkValidation"] = "Valid",             // forged verdict
            ["trace"] = "abc-123",                          // benign, must be preserved
        };

        await Orchestrator.ExecuteAsync(Request(clientExtensions: clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions.ShouldNotContainKey("actor:agentsAdmin");
        dispatched.Extensions.ShouldNotContainKey("provider:selectionValidation");
        dispatched.Extensions.ShouldNotContainKey("approver:policyValidation");
        dispatched.Extensions.ShouldNotContainKey("party:linkValidation");
        dispatched.Extensions["trace"].ShouldBe("abc-123");
    }

    [Fact]
    public async Task Supplying_only_reserved_extensions_yields_no_extension_map()
    {
        // When nothing benign survives the strip, the envelope carries a null extension map (not an empty one).
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot()));
        CaptureDispatch();

        var onlyReserved = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",
            ["provider:selectionValidation"] = "Valid",
        };

        await Orchestrator.ExecuteAsync(Request(clientExtensions: onlyReserved), CancellationToken.None);

        LastDispatched().ShouldNotBeNull().Extensions.ShouldBeNull();
    }

    [Fact]
    public async Task No_client_extensions_yields_no_extension_map()
    {
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot()));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        LastDispatched().ShouldNotBeNull().Extensions.ShouldBeNull();
    }

    // ===== Trusted envelope scope (AC2; AC4 tenant isolation) =====

    [Fact]
    public async Task Builds_the_envelope_with_the_trusted_scope_from_the_request()
    {
        // Tenant scope, idempotency MessageId, correlation, and the authenticated caller flow from the request onto
        // the envelope — never from the command payload (AC4: tenant scope comes from the envelope; AD-13).
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot()));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.TenantId.ShouldBe(TenantId);
        dispatched.MessageId.ShouldBe("msg-1");
        dispatched.CorrelationId.ShouldBe("corr-1");
        dispatched.UserId.ShouldBe("caller-user");
        dispatched.CausationId.ShouldBeNull();
    }

    // ===== Not-available snapshot still dispatches for an auditable rejection (AC1) =====

    [Fact]
    public async Task Not_available_snapshot_dispatches_a_null_snapshot_request_with_an_unknown_reference()
    {
        ReaderReturns(AgentConfigurationSnapshot.NotAvailable);
        CaptureDispatch();

        AgentInteractionRequestOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.SnapshotAvailable.ShouldBeFalse();
        outcome.Dispatched.ShouldBeTrue();
        outcome.Reference.Status.ShouldBe(AgentInteractionStatus.Unknown);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        RequestAgentInteraction command = JsonSerializer.Deserialize<RequestAgentInteraction>(dispatched.Payload)!;
        command.Snapshot.ShouldBeNull(); // the aggregate will fail closed with MissingAgentSnapshot
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== No side effects (AC3) =====

    [Fact]
    public async Task Touches_only_the_snapshot_reader_and_dispatcher_no_provider_or_conversation_client()
    {
        // The orchestrator's only collaborators are the snapshot reader and the command dispatcher — there is
        // structurally no provider adapter or IConversationClient/Parties client on this path (AC3). One read, one
        // dispatch, nothing else.
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot()));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        await _reader.Received(1).ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Deferred placeholder fails closed to not-available =====

    [Fact]
    public async Task Deferred_snapshot_reader_returns_not_available_until_the_live_binding_is_wired()
    {
        IAgentConfigurationSnapshotReader deferred = new DeferredAgentConfigurationSnapshotReader();

        AgentConfigurationSnapshot result = await deferred.ReadAsync(TenantId, AgentId, CancellationToken.None);

        result.IsAvailable.ShouldBeFalse();
        result.Snapshot.ShouldBeNull();
    }

    // ===== Cross-seam end-to-end: orchestrator-built command → real pure aggregate =====

    [Fact]
    public async Task End_to_end_the_dispatched_command_drives_the_aggregate_to_interaction_requested()
    {
        // The orchestrator and the aggregate are unit-tested in isolation above; here the orchestrator's own
        // dispatched command is deserialized and run through the real pure aggregate, exercising both halves of the
        // request-creation flow together (AC1, AC2).
        ReaderReturns(AgentConfigurationSnapshot.Available(Snapshot()));
        CaptureDispatch();

        AgentInteractionRequestOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        RequestAgentInteraction command = JsonSerializer.Deserialize<RequestAgentInteraction>(dispatched.Payload)!;

        DomainResult result = AgentInteractionAggregate.Handle(command, state: null, dispatched);

        result.IsSuccess.ShouldBeTrue();
        InteractionRequested requested = result.Events[0].ShouldBeOfType<InteractionRequested>();
        requested.AgentInteractionId.ShouldBe(outcome.Reference.AgentInteractionId);
        requested.AgentId.ShouldBe(AgentId);
        requested.CallerPartyId.ShouldBe(CallerPartyId);
        requested.SourceConversationId.ShouldBe(SourceConversationId);
        requested.Prompt.ShouldBe(Prompt);
        requested.Snapshot.ProviderId.ShouldBe("openai");
        requested.Snapshot.ContextPolicyReference.ShouldBe(AgentInteractionSnapshot.DefaultContextPolicyReference);
    }

    [Fact]
    public async Task End_to_end_a_not_available_snapshot_drives_the_aggregate_to_a_missing_snapshot_rejection()
    {
        // The AC1 precondition path, end to end: a pre-activation Agent yields a not-available snapshot, the
        // orchestrator still dispatches a null-snapshot command, and the pure aggregate fails closed with a safe
        // MissingAgentSnapshot rejection — never a created interaction (AC1, AC3).
        ReaderReturns(AgentConfigurationSnapshot.NotAvailable);
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        RequestAgentInteraction command = JsonSerializer.Deserialize<RequestAgentInteraction>(dispatched.Payload)!;

        DomainResult result = AgentInteractionAggregate.Handle(command, state: null, dispatched);

        result.IsRejection.ShouldBeTrue();
        InvalidAgentInteractionRequestRejection rejection = result.Events[0].ShouldBeOfType<InvalidAgentInteractionRequestRejection>();
        rejection.Status.ShouldBe(AgentInteractionRequestValidationStatus.MissingAgentSnapshot);
        result.Events.ShouldNotContain(e => e is InteractionRequested);
    }

    // ===== Helpers =====

    private static string ExpectedId()
        => AgentInteractionIdentity.Derive(TenantId, AgentId, SourceConversationId, CallerPartyId, IdempotencyKey);

    private static AgentInteractionSnapshot Snapshot(string? contextPolicyReference = null)
        => new(
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            AgentResponseMode.Automatic,
            ApproverPolicyVersion: 1,
            ProviderId: "openai",
            ModelId: "gpt-4o",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion: 1,
            contextPolicyReference ?? AgentInteractionSnapshot.DefaultContextPolicyReference);

    private void ReaderReturns(AgentConfigurationSnapshot result)
        => _reader.ReadAsync(TenantId, AgentId, Arg.Any<CancellationToken>()).Returns(result);

    private static AgentInteractionRequest Request(IReadOnlyDictionary<string, string>? clientExtensions = null)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "caller-user",
            SourceConversationId,
            CallerPartyId,
            Prompt,
            IdempotencyKey,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;
}
