namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentProviderSelectionOrchestrator"/> (Story 1.5 AC1, AC2, AC4; AD-3, AD-9, AD-12). Verifies
/// the orchestration authorizes fail-closed, maps each catalog read outcome to the correct fail-closed verdict per
/// the deterministic precedence, captures the entry's capability version, builds the
/// <see cref="SelectAgentProviderModel"/> command, and dispatches it with the <b>server-populated</b> trusted
/// extensions — and, critically, that a client cannot forge <c>actor:agentsAdmin</c> /
/// <c>provider:selectionValidation</c> to bypass catalog validation (the reserved keys are stripped and repopulated
/// from trusted sources only). Also covers the deferred reader placeholder and the activation re-validation step.
/// </summary>
public sealed class AgentProviderSelectionOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";
    private const string ProviderId = "openai";
    private const string ModelId = "gpt-4o";

    private readonly IProviderCatalogReader _reader = Substitute.For<IProviderCatalogReader>();
    private readonly IApproverPolicyResolver _approverResolver = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private AgentProviderSelectionOrchestrator Orchestrator => new(_reader, _dispatcher);

    // ===== Verdict mapping per deterministic precedence (AC2; AD-10, AD-12) =====

    [Fact]
    public async Task Enabled_configured_text_gen_entry_with_valid_metadata_maps_to_valid_and_captures_capability_version()
    {
        ReaderReturns(SuccessRead(ValidEntry(capabilityVersion: 7)));
        CaptureDispatch();

        AgentProviderSelectionOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Authorized.ShouldBeTrue();
        outcome.Dispatched.ShouldBeTrue();
        outcome.Verdict.ShouldBe(ProviderSelectionValidationStatus.Valid);
        outcome.CapabilityVersion.ShouldBe(7);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(SelectAgentProviderModel));
        dispatched.AggregateId.ShouldBe(AgentId);
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");
        dispatched.Extensions["provider:selectionValidation"].ShouldBe("Valid");

        SelectAgentProviderModel command = JsonSerializer.Deserialize<SelectAgentProviderModel>(dispatched.Payload)!;
        command.ProviderId.ShouldBe(ProviderId);
        command.ModelId.ShouldBe(ModelId);
        command.ProviderCapabilityVersion.ShouldBe(7); // the captured catalog version, not a client-supplied one
    }

    [Fact]
    public async Task Entry_not_found_maps_to_missing()
        => (await RunVerdict(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null)))
            .ShouldBe(ProviderSelectionValidationStatus.Missing);

    [Fact]
    public async Task Not_authorized_read_maps_to_unauthorized()
        => (await RunVerdict(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.NotAuthorized, null)))
            .ShouldBe(ProviderSelectionValidationStatus.Unauthorized);

    [Fact]
    public async Task Degraded_read_success_without_entry_maps_to_unavailable()
        => (await RunVerdict(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.Success, null)))
            .ShouldBe(ProviderSelectionValidationStatus.Unavailable);

    [Fact]
    public async Task Disabled_entry_maps_to_disabled()
        => (await RunVerdict(SuccessRead(ValidEntry() with { Status = ProviderModelStatus.Disabled })))
            .ShouldBe(ProviderSelectionValidationStatus.Disabled);

    [Fact]
    public async Task Non_text_generation_entry_maps_to_not_text_generation_capable()
        => (await RunVerdict(SuccessRead(ValidEntry() with { SupportsTextGeneration = false })))
            .ShouldBe(ProviderSelectionValidationStatus.NotTextGenerationCapable);

    [Fact]
    public async Task Not_configured_entry_maps_to_not_configured()
        => (await RunVerdict(SuccessRead(ValidEntry() with { ConfigurationState = ProviderConfigurationState.NotConfigured })))
            .ShouldBe(ProviderSelectionValidationStatus.NotConfigured);

    [Fact]
    public async Task Output_exceeding_context_window_maps_to_missing_capability_metadata()
        => (await RunVerdict(SuccessRead(ValidEntry() with { MaxOutputTokenLimit = 999_999 })))
            .ShouldBe(ProviderSelectionValidationStatus.MissingCapabilityMetadata);

    [Fact]
    public async Task Non_positive_context_window_maps_to_missing_capability_metadata()
        => (await RunVerdict(SuccessRead(ValidEntry() with { ContextWindowTokenLimit = 0 })))
            .ShouldBe(ProviderSelectionValidationStatus.MissingCapabilityMetadata);

    // ===== QA gap-fill (Story 1.5 AC2): the remaining fail-closed verdict branches =====

    [Theory]
    [InlineData(ProviderModelStatus.Degraded)]
    [InlineData(ProviderModelStatus.Failed)]
    [InlineData(ProviderModelStatus.Unknown)]
    public async Task Any_non_enabled_entry_status_maps_to_disabled(ProviderModelStatus status)
        // AC2 + AD-12: the verdict gate is "Status != Enabled → Disabled", so a degraded/failed/unknown entry
        // (not just an explicitly Disabled one) fails closed and is never selectable. The spine enumerates
        // "missing, disabled, or failed" — every non-Enabled status must surface the same fail-closed verdict.
        => (await RunVerdict(SuccessRead(ValidEntry() with { Status = status })))
            .ShouldBe(ProviderSelectionValidationStatus.Disabled);

    [Fact]
    public async Task Non_positive_output_limit_maps_to_missing_capability_metadata()
        // AC2: the capability floor requires MaxOutputTokenLimit > 0 (the "output metadata" clause), distinct from
        // the already-covered output-exceeds-context case.
        => (await RunVerdict(SuccessRead(ValidEntry() with { MaxOutputTokenLimit = 0 })))
            .ShouldBe(ProviderSelectionValidationStatus.MissingCapabilityMetadata);

    [Fact]
    public async Task Non_positive_request_timeout_maps_to_missing_capability_metadata()
        // AC2: the AC enumerates "lacks required ... timeout metadata" — a non-positive request timeout fails closed.
        => (await RunVerdict(SuccessRead(ValidEntry() with { TimeoutPolicy = new ProviderModelTimeoutPolicy(0, 3) })))
            .ShouldBe(ProviderSelectionValidationStatus.MissingCapabilityMetadata);

    [Fact]
    public async Task Negative_max_retries_maps_to_missing_capability_metadata()
        // AC2: a negative retry count is invalid timeout metadata and fails closed (the verdict requires MaxRetries >= 0).
        => (await RunVerdict(SuccessRead(ValidEntry() with { TimeoutPolicy = new ProviderModelTimeoutPolicy(30_000, -1) })))
            .ShouldBe(ProviderSelectionValidationStatus.MissingCapabilityMetadata);

    // ===== Trust model + fail-closed authorization =====

    [Fact]
    public async Task Client_supplied_reserved_extensions_are_stripped_and_repopulated_from_trusted_sources()
    {
        // The REAL catalog read is Disabled; the client forges a Valid verdict to try to bypass validation.
        ReaderReturns(SuccessRead(ValidEntry() with { Status = ProviderModelStatus.Disabled }));
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",                  // forged
            ["provider:selectionValidation"] = "Valid",      // forged bypass attempt
            ["trace"] = "abc-123",                           // benign, must be preserved
        };

        await Orchestrator.ExecuteAsync(Request(clientExtensions: clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["provider:selectionValidation"].ShouldBe("Disabled"); // the trusted verdict wins, not the forged "Valid"
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");                 // repopulated from the trusted decision
        dispatched.Extensions["trace"].ShouldBe("abc-123");                          // non-reserved client extension preserved
    }

    [Fact]
    public async Task Non_valid_verdict_is_still_dispatched_for_an_auditable_rejection()
    {
        ReaderReturns(new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null));
        CaptureDispatch();

        AgentProviderSelectionOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Verdict.ShouldBe(ProviderSelectionValidationStatus.Missing);
        outcome.Dispatched.ShouldBeTrue();
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        LastDispatched().ShouldNotBeNull().Extensions!["provider:selectionValidation"].ShouldBe("Missing");
    }

    [Fact]
    public async Task Unauthorized_actor_is_denied_without_reading_the_catalog_or_dispatching()
    {
        AgentProviderSelectionOutcome outcome = await Orchestrator.ExecuteAsync(
            Request(isAgentsAdmin: false), CancellationToken.None);

        outcome.Authorized.ShouldBeFalse();
        outcome.Dispatched.ShouldBeFalse();
        await _reader.DidNotReceiveWithAnyArgs().GetEntryAsync(default!, default!, default!, default);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    // ===== Deferred placeholder behaves like the Story 1.4 deferred seam =====

    [Fact]
    public async Task Deferred_provider_catalog_reader_throws_until_the_live_binding_is_wired()
    {
        IProviderCatalogReader deferred = new DeferredProviderCatalogReader();

        await Should.ThrowAsync<NotSupportedException>(
            async () => await deferred.GetEntryAsync(TenantId, ProviderId, ModelId, CancellationToken.None));
    }

    // ===== Activation re-validation step (AC2 "or activate") =====

    [Fact]
    public async Task Activation_revalidation_reads_the_recorded_selection_and_dispatches_activate_with_the_verdict()
    {
        ReaderReturns(SuccessRead(ValidEntry()));
        CaptureDispatch();
        var revalidation = new AgentActivationProviderRevalidation(_reader, _approverResolver, _dispatcher);

        AgentActivationRevalidationOutcome outcome = await revalidation.ExecuteAsync(
            ActivationRequest(ProviderId, ModelId), CancellationToken.None);

        outcome.ProviderVerdict.ShouldBe(ProviderSelectionValidationStatus.Valid);
        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(ActivateAgent));
        dispatched.Extensions!["provider:selectionValidation"].ShouldBe("Valid");
        await _reader.Received(1).GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Activation_revalidation_with_no_recorded_selection_skips_the_catalog_read()
    {
        CaptureDispatch();
        var revalidation = new AgentActivationProviderRevalidation(_reader, _approverResolver, _dispatcher);

        AgentActivationRevalidationOutcome outcome = await revalidation.ExecuteAsync(
            ActivationRequest(selectedProviderId: null, selectedModelId: null), CancellationToken.None);

        outcome.ProviderVerdict.ShouldBe(ProviderSelectionValidationStatus.Unknown);
        await _reader.DidNotReceiveWithAnyArgs().GetEntryAsync(default!, default!, default!, default);
        LastDispatched().ShouldNotBeNull().Extensions!["provider:selectionValidation"].ShouldBe("Unknown");
    }

    [Fact]
    public async Task Activation_revalidation_denies_an_unauthorized_actor_without_reading_or_dispatching()
    {
        var revalidation = new AgentActivationProviderRevalidation(_reader, _approverResolver, _dispatcher);

        AgentActivationRevalidationOutcome outcome = await revalidation.ExecuteAsync(
            ActivationRequest(ProviderId, ModelId, isAgentsAdmin: false), CancellationToken.None);

        outcome.Authorized.ShouldBeFalse();
        outcome.Dispatched.ShouldBeFalse();
        await _reader.DidNotReceiveWithAnyArgs().GetEntryAsync(default!, default!, default!, default);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    // QA gap-fill (Story 1.5 AC2/AC4): the activation "or activate" path shares the selection orchestration's trust
    // model, but only the SELECT path's reserved-key stripping was proven. Here the recorded selection is genuinely
    // Disabled while the client forges a Valid verdict on the activate command — the trusted verdict must win so a
    // not-ready selection cannot be activated by spoofing the extension (and benign extensions are preserved).
    [Fact]
    public async Task Activation_revalidation_strips_client_forged_reserved_extensions_and_repopulates_the_trusted_verdict()
    {
        ReaderReturns(SuccessRead(ValidEntry() with { Status = ProviderModelStatus.Disabled }));
        CaptureDispatch();
        var revalidation = new AgentActivationProviderRevalidation(_reader, _approverResolver, _dispatcher);

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",             // forged
            ["provider:selectionValidation"] = "Valid", // forged bypass attempt
            ["trace"] = "xyz-789",                       // benign, must be preserved
        };

        var request = new AgentActivationRevalidationRequest(
            MessageId: "msg-activate",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            IsAgentsAdmin: true,
            SelectedProviderId: ProviderId,
            SelectedModelId: ModelId,
            ClientSuppliedExtensions: clientExtensions);

        await revalidation.ExecuteAsync(request, CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["provider:selectionValidation"].ShouldBe("Disabled"); // the trusted verdict wins, not the forged "Valid"
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");                 // repopulated from the trusted decision
        dispatched.Extensions["trace"].ShouldBe("xyz-789");                          // non-reserved client extension preserved
    }

    // ===== Helpers =====

    private async Task<ProviderSelectionValidationStatus> RunVerdict(ProviderCatalogEntryReadResult read)
    {
        ReaderReturns(read);
        CaptureDispatch();
        AgentProviderSelectionOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);
        return outcome.Verdict;
    }

    private void ReaderReturns(ProviderCatalogEntryReadResult read)
        => _reader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>()).Returns(read);

    private static ProviderCatalogEntryReadResult SuccessRead(ProviderCatalogEntryView entry)
        => new(ProviderCatalogInspectionStatus.Success, entry);

    private static ProviderCatalogEntryView ValidEntry(int capabilityVersion = 1)
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
            IsSelectableForNewActiveUse: true,
            capabilityVersion);

    private static AgentProviderSelectionRequest Request(
        bool isAgentsAdmin = true,
        IReadOnlyDictionary<string, string>? clientExtensions = null)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            isAgentsAdmin,
            ProviderId,
            ModelId,
            clientExtensions);

    private static AgentActivationRevalidationRequest ActivationRequest(
        string? selectedProviderId,
        string? selectedModelId,
        bool isAgentsAdmin = true)
        => new(
            MessageId: "msg-activate",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            isAgentsAdmin,
            selectedProviderId,
            selectedModelId);

    private CommandEnvelope? _lastDispatched;

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;
}
