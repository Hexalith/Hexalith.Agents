namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for the Story 1.6 approver-policy leg of <see cref="AgentActivationProviderRevalidation"/> (AC3; AD-3, AD-8,
/// AD-12). Verifies activation re-resolves the recorded Approver Policy only in Confirmation mode with at least one
/// configured source — populating the trusted <c>approver:policyValidation</c> verdict from the resolver — while
/// Automatic mode (and an empty Confirmation policy) leaves it <c>Unknown</c>; and that the reserved key is
/// server-populated / client-stripped. Also covers the deferred resolver placeholder.
/// </summary>
public sealed class AgentActivationApproverRevalidationTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";
    private const string ProviderId = "openai";
    private const string ModelId = "gpt-4o";

    private static readonly AgentApproverPolicy _policy = new(
        [
            new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null),
            new ApproverPolicySource(ApproverPolicySourceKind.PredefinedParty, "party-approver", null),
        ],
        ApproverPolicyBasisDisclosure.OperatorOnly);

    private readonly IProviderCatalogReader _reader = Substitute.For<IProviderCatalogReader>();
    private readonly IApproverPolicyResolver _resolver = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private AgentActivationProviderRevalidation Revalidation => new(_reader, _resolver, _dispatcher);

    [Fact]
    public async Task Confirmation_mode_with_a_policy_resolves_sources_and_populates_the_trusted_verdict()
    {
        ResolverReturns(AllResolved());
        CaptureDispatch();

        AgentActivationRevalidationOutcome outcome = await Revalidation.ExecuteAsync(
            Request(AgentResponseMode.Confirmation, _policy), CancellationToken.None);

        outcome.ApproverVerdict.ShouldBe(ApproverPolicyValidationStatus.Valid);
        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions!["approver:policyValidation"].ShouldBe("Valid");
        await _resolver.Received(1).ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirmation_mode_with_a_recorded_selection_populates_both_provider_and_approver_verdicts()
    {
        // Activation is one command / one envelope carrying ALL trusted dependency verdicts: a Confirmation-mode agent
        // that also has a recorded Provider/model has BOTH legs re-validated independently — the catalog reader for the
        // provider verdict and the resolver for the approver verdict — and both land on the dispatched envelope.
        _reader.GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>()).Returns(ValidProviderRead());
        ResolverReturns(AllResolved());
        CaptureDispatch();

        var request = new AgentActivationRevalidationRequest(
            MessageId: "msg-activate",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            IsAgentsAdmin: true,
            SelectedProviderId: ProviderId,
            SelectedModelId: ModelId,
            ResponseMode: AgentResponseMode.Confirmation,
            RecordedApproverPolicy: _policy);

        AgentActivationRevalidationOutcome outcome = await Revalidation.ExecuteAsync(request, CancellationToken.None);

        outcome.ProviderVerdict.ShouldBe(ProviderSelectionValidationStatus.Valid);
        outcome.ApproverVerdict.ShouldBe(ApproverPolicyValidationStatus.Valid);
        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions!["provider:selectionValidation"].ShouldBe("Valid");
        dispatched.Extensions!["approver:policyValidation"].ShouldBe("Valid");
        await _reader.Received(1).GetEntryAsync(TenantId, ProviderId, ModelId, Arg.Any<CancellationToken>());
        await _resolver.Received(1).ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirmation_mode_with_a_non_resolvable_source_populates_the_fail_closed_verdict()
    {
        ResolverReturns(new ApproverPolicyResolutionResult(
        [
            new ApproverSourceResolution(ApproverPolicySourceKind.Caller, ApproverSourceOutcome.Resolved),
            new ApproverSourceResolution(ApproverPolicySourceKind.PredefinedParty, ApproverSourceOutcome.Disabled),
        ]));
        CaptureDispatch();

        AgentActivationRevalidationOutcome outcome = await Revalidation.ExecuteAsync(
            Request(AgentResponseMode.Confirmation, _policy), CancellationToken.None);

        outcome.ApproverVerdict.ShouldBe(ApproverPolicyValidationStatus.Disabled);
        LastDispatched().ShouldNotBeNull().Extensions!["approver:policyValidation"].ShouldBe("Disabled");
    }

    [Fact]
    public async Task Automatic_mode_does_not_resolve_the_policy_and_leaves_the_verdict_unknown()
    {
        CaptureDispatch();

        AgentActivationRevalidationOutcome outcome = await Revalidation.ExecuteAsync(
            Request(AgentResponseMode.Automatic, _policy), CancellationToken.None);

        outcome.ApproverVerdict.ShouldBe(ApproverPolicyValidationStatus.Unknown);
        await _resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, default!, default);
        LastDispatched().ShouldNotBeNull().Extensions!["approver:policyValidation"].ShouldBe("Unknown");
    }

    [Fact]
    public async Task Confirmation_mode_with_an_empty_policy_does_not_resolve_and_leaves_the_verdict_unknown()
    {
        CaptureDispatch();

        AgentActivationRevalidationOutcome outcome = await Revalidation.ExecuteAsync(
            Request(AgentResponseMode.Confirmation, new AgentApproverPolicy([], ApproverPolicyBasisDisclosure.OperatorOnly)), CancellationToken.None);

        outcome.ApproverVerdict.ShouldBe(ApproverPolicyValidationStatus.Unknown);
        await _resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, default!, default);
    }

    [Fact]
    public async Task Client_forged_approver_verdict_is_stripped_and_repopulated_from_the_resolver()
    {
        // The REAL resolution is Missing (one source resolves, the other is missing → the policy verdict is Missing);
        // the client forges a Valid verdict to try to bypass it. The resolver returns one outcome per configured source.
        ResolverReturns(new ApproverPolicyResolutionResult(
        [
            new ApproverSourceResolution(ApproverPolicySourceKind.Caller, ApproverSourceOutcome.Missing),
            new ApproverSourceResolution(ApproverPolicySourceKind.PredefinedParty, ApproverSourceOutcome.Resolved),
        ]));
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",          // forged
            ["approver:policyValidation"] = "Valid", // forged bypass attempt
            ["trace"] = "abc-123",                    // benign, must be preserved
        };

        await Revalidation.ExecuteAsync(
            Request(AgentResponseMode.Confirmation, _policy, clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["approver:policyValidation"].ShouldBe("Missing"); // the trusted verdict wins, not the forged "Valid"
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");
        dispatched.Extensions["trace"].ShouldBe("abc-123");
    }

    [Fact]
    public async Task Unauthorized_actor_is_denied_without_resolving_or_dispatching()
    {
        AgentActivationRevalidationOutcome outcome = await Revalidation.ExecuteAsync(
            Request(AgentResponseMode.Confirmation, _policy, isAgentsAdmin: false), CancellationToken.None);

        outcome.Authorized.ShouldBeFalse();
        outcome.Dispatched.ShouldBeFalse();
        await _resolver.DidNotReceiveWithAnyArgs().ResolveAsync(default!, default!, default);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task Deferred_approver_policy_resolver_throws_until_the_live_binding_is_wired()
    {
        IApproverPolicyResolver deferred = new DeferredApproverPolicyResolver();

        await Should.ThrowAsync<NotSupportedException>(
            async () => await deferred.ResolveAsync(TenantId, _policy, CancellationToken.None));
    }

    private void ResolverReturns(ApproverPolicyResolutionResult result)
        => _resolver.ResolveAsync(Arg.Any<string>(), Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>()).Returns(result);

    private static ProviderCatalogEntryReadResult ValidProviderRead()
        => new(
            ProviderCatalogInspectionStatus.Success,
            new ProviderCatalogEntryView(
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
                1));

    private static ApproverPolicyResolutionResult AllResolved()
        => new(
        [
            new ApproverSourceResolution(ApproverPolicySourceKind.Caller, ApproverSourceOutcome.Resolved),
            new ApproverSourceResolution(ApproverPolicySourceKind.PredefinedParty, ApproverSourceOutcome.Resolved),
        ]);

    private static AgentActivationRevalidationRequest Request(
        AgentResponseMode mode,
        AgentApproverPolicy? policy,
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        bool isAgentsAdmin = true)
        => new(
            MessageId: "msg-activate",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            isAgentsAdmin,
            SelectedProviderId: null, // focus on the approver leg — no provider selection to re-read here
            SelectedModelId: null,
            ResponseMode: mode,
            RecordedApproverPolicy: policy,
            ClientSuppliedExtensions: clientExtensions);

    private CommandEnvelope? _lastDispatched;

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;
}
