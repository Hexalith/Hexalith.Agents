namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentInteractionProposalEditOrchestrator"/> (Story 3.3; AC1, AC2, AC4; AD-3, AD-5, AD-12, AD-13,
/// AD-14). Verify the authorized path dispatches <see cref="EditProposedAgentReply"/> with the right domain/aggregate id and
/// a deterministic edited-version id, returning <c>ProposalEdited</c> with the edited content on the COMMAND but NONE on the
/// returned outcome (AD-14); that any non-Resolved approver source fails closed to a no-dispatch NotAuthorized denial; that a
/// genuine cancellation propagates; that the all-deferred default graph denies; that a retry reuses the deterministic id;
/// that reserved trust keys are stripped; and that the dispatched command drives the real aggregate to the same decision.
/// The shared edit policy is not visible to this assembly, so decisions are asserted via the returned status.
/// </summary>
public sealed class AgentInteractionProposalEditOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentInteractionId = "interaction-001";
    private const string ProposalId = "proposal-001";
    private const string SourceConversationId = "conversation-001";
    private const string SourceVersionId = "version-attempt-1";
    private const string EditorPartyId = "editor-party-1";
    private const string EditAttemptId = "edit-attempt-1";
    private const string ActorUserId = "approver-user";
    private const int ApproverPolicyVersion = 1;
    private const int ContentSafetyPolicyVersion = 1;
    private const string EditedContentText = "the-edited-answer-do-not-leak";

    private readonly IApproverPolicyResolver _resolver = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private CommandEnvelope? _lastDispatched;

    private AgentInteractionProposalEditOrchestrator Orchestrator => new(_resolver, _dispatcher);

    // ===== Happy path → ProposalEdited (AC1) =====

    [Fact]
    public async Task Authorized_edit_dispatches_with_the_deterministic_edited_version_id_and_returns_proposal_edited()
    {
        StubResolved();
        CaptureDispatch();

        AgentInteractionProposalEditOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalEdited);
        outcome.NotEditableReason.ShouldBe(AgentProposedReplyNotEditableReason.Unknown);
        outcome.EditedVersionId.ShouldBe(AgentProposalEditIdentity.DeriveEditedVersionId(AgentInteractionId, SourceVersionId, EditAttemptId));

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(EditProposedAgentReply));
        dispatched.Domain.ShouldBe("agent-interaction");
        dispatched.AggregateId.ShouldBe(AgentInteractionId);

        AgentProposalEditResult result = DispatchedResult();
        result.Outcome.ShouldBe(AgentProposalEditOutcome.Edited);
        result.AuthorizationVerdict.ShouldBe(ApproverPolicyValidationStatus.Valid);
        result.ProposalId.ShouldBe(ProposalId);
        result.EditedVersion.Kind.ShouldBe(AgentGenerationKind.Edited);
        result.EditedVersion.SourceVersionId.ShouldBe(SourceVersionId);
        result.EditedVersion.EditorPartyId.ShouldBe(EditorPartyId);
    }

    [Fact]
    public async Task The_command_carries_the_edited_content_but_the_returned_outcome_carries_none()
    {
        // The edited content originates from the user and rides the write-path command/version (its legitimate home), but
        // the safe outcome returned to the caller carries NO content (AD-14).
        StubResolved();
        CaptureDispatch();

        AgentInteractionProposalEditOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        Encoding.UTF8.GetString(LastDispatched()!.Payload).ShouldContain(EditedContentText); // the write-path command home
        JsonSerializer.Serialize(outcome).ShouldNotContain(EditedContentText); // the safe outcome is content-free
    }

    // ===== Fail-closed authorization → no-dispatch NotAuthorized denial (FR-15; AD-12) =====

    [Theory]
    [InlineData(ApproverSourceOutcome.Missing)]
    [InlineData(ApproverSourceOutcome.Disabled)]
    [InlineData(ApproverSourceOutcome.Ambiguous)]
    [InlineData(ApproverSourceOutcome.Unavailable)]
    [InlineData(ApproverSourceOutcome.Unauthorized)]
    [InlineData(ApproverSourceOutcome.Unknown)]
    public async Task A_non_resolved_approver_source_denies_the_edit_without_dispatching(ApproverSourceOutcome sourceOutcome)
    {
        StubResolver(sourceOutcome);
        CaptureDispatch();

        AgentInteractionProposalEditOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.NotEditableReason.ShouldBe(AgentProposedReplyNotEditableReason.NotAuthorized);
        outcome.Status.ShouldBe(AgentInteractionStatus.Unknown);
        outcome.EditedVersionId.ShouldBeEmpty();
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_null_or_empty_approver_policy_denies_the_edit_without_dispatching()
    {
        CaptureDispatch();

        AgentInteractionProposalEditOutcomeResult outcome = await Orchestrator.ExecuteAsync(
            Request(policy: new AgentApproverPolicy([], ApproverPolicyBasisDisclosure.OperatorOnly)), CancellationToken.None);

        outcome.NotEditableReason.ShouldBe(AgentProposedReplyNotEditableReason.NotAuthorized);
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_resolver_that_throws_fails_closed_to_a_no_dispatch_denial_without_leaking()
    {
        _resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("resolve blew up: secret-bearing detail"));
        CaptureDispatch();

        AgentInteractionProposalEditOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.NotEditableReason.ShouldBe(AgentProposedReplyNotEditableReason.NotAuthorized);
        JsonSerializer.Serialize(outcome).ShouldNotContain("secret-bearing detail");
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== All-deferred default graph fails closed (FR-21) =====

    [Fact]
    public async Task All_deferred_defaults_deny_the_edit_without_dispatching()
    {
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        var orchestrator = new AgentInteractionProposalEditOrchestrator(new DeferredApproverPolicyResolver(), dispatcher);

        AgentInteractionProposalEditOutcomeResult outcome = await orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.NotEditableReason.ShouldBe(AgentProposedReplyNotEditableReason.NotAuthorized);
        await dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Deterministic id reuse on retry (AC4; AD-13) =====

    [Fact]
    public async Task A_retried_edit_reuses_the_same_deterministic_edited_version_id()
    {
        StubResolved();
        var dispatched = new List<CommandEnvelope>();
        _ = _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(dispatched.Add), Arg.Any<CancellationToken>());

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);
        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        dispatched.Count.ShouldBe(2);
        string first = JsonSerializer.Deserialize<EditProposedAgentReply>(dispatched[0].Payload)!.Result.EditedVersion.VersionId;
        string second = JsonSerializer.Deserialize<EditProposedAgentReply>(dispatched[1].Payload)!.Result.EditedVersion.VersionId;
        second.ShouldBe(first);
    }

    // ===== Trust model + envelope scope (AC4) =====

    [Fact]
    public async Task Strips_client_forged_reserved_extensions_and_preserves_benign_ones()
    {
        StubResolved();
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",
            ["provider:selectionValidation"] = "Valid",
            ["approver:policyValidation"] = "Valid",
            ["party:linkValidation"] = "Valid",
            ["trace"] = "abc-123",
        };

        await Orchestrator.ExecuteAsync(Request(clientExtensions: clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions.ShouldNotContainKey("actor:agentsAdmin");
        dispatched.Extensions.ShouldNotContainKey("approver:policyValidation");
        dispatched.Extensions.ShouldNotContainKey("party:linkValidation");
        dispatched.Extensions["trace"].ShouldBe("abc-123");
    }

    [Fact]
    public async Task Builds_the_envelope_with_the_trusted_scope_from_the_request()
    {
        StubResolved();
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.TenantId.ShouldBe(TenantId);
        dispatched.MessageId.ShouldBe("msg-1");
        dispatched.CorrelationId.ShouldBe("corr-1");
        dispatched.UserId.ShouldBe(ActorUserId);
        dispatched.CausationId.ShouldBeNull();
    }

    // ===== Cancellation propagates (AC2) =====

    [Fact]
    public async Task A_genuine_cancellation_propagates_and_no_command_is_dispatched()
    {
        _resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        CaptureDispatch();

        await Should.ThrowAsync<OperationCanceledException>(
            async () => await Orchestrator.ExecuteAsync(Request(), CancellationToken.None));

        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
    }

    // ===== Cross-seam: the dispatched command drives the real aggregate to the same decision =====

    [Fact]
    public async Task End_to_end_the_dispatched_edit_command_drives_the_aggregate_to_proposal_edited()
    {
        StubResolved();
        CaptureDispatch();

        AgentInteractionProposalEditOutcomeResult outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        var command = JsonSerializer.Deserialize<EditProposedAgentReply>(dispatched.Payload)!;

        AgentInteractionState state = ProposalCreatedState();
        DomainResult result = AgentInteractionAggregate.Handle(command, state, dispatched);

        ProposedAgentReplyEdited edited = result.Events[0].ShouldBeOfType<ProposedAgentReplyEdited>();
        edited.EditedVersion.SourceVersionId.ShouldBe(SourceVersionId);
        // The orchestrator's returned status and the aggregate's recorded decision agree (shared policy — no drift).
        outcome.Status.ShouldBe(AgentInteractionStatus.ProposalEdited);
    }

    // ===== Helpers =====

    private void StubResolved()
        => StubResolver(ApproverSourceOutcome.Resolved);

    private void StubResolver(ApproverSourceOutcome sourceOutcome)
        => _resolver.ResolveAsync(TenantId, Arg.Any<AgentApproverPolicy>(), Arg.Any<CancellationToken>())
            .Returns(new ApproverPolicyResolutionResult([new ApproverSourceResolution(ApproverPolicySourceKind.Caller, sourceOutcome)]));

    private static AgentApproverPolicy SingleSourcePolicy()
        => new([new ApproverPolicySource(ApproverPolicySourceKind.Caller, null, null)], ApproverPolicyBasisDisclosure.OperatorOnly);

    private static AgentInteractionProposalEditRequest Request(
        AgentApproverPolicy? policy = null,
        IReadOnlyDictionary<string, string>? clientExtensions = null)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentInteractionId,
            ProposalId,
            SourceConversationId,
            SourceVersionId,
            EditedContentText,
            EditorPartyId,
            EditAttemptId,
            policy ?? SingleSourcePolicy(),
            ApproverPolicyVersion,
            ContentSafetyPolicyVersion,
            SourceProviderId: "openai",
            SourceModelId: "gpt-4o",
            SourceProviderCapabilityVersion: 1,
            ActorUserId,
            ClientCorrelationId: "client-corr-1",
            clientExtensions);

    private void CaptureDispatch()
        => _ = _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;

    private AgentProposalEditResult DispatchedResult()
        => JsonSerializer.Deserialize<EditProposedAgentReply>(LastDispatched()!.Payload)!.Result;

    private static AgentInteractionState ProposalCreatedState()
    {
        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            AgentInteractionId, "hexa", "party-001", SourceConversationId, ConfirmationSnapshot(), "prompt", "idem"));
        state.Apply(new AgentInteractionAuthorized(AgentInteractionId));
        state.Apply(new AgentInteractionContextReady(AgentInteractionId, SampleContextEvidence()));
        state.Apply(new AgentOutputGenerated(AgentInteractionId, SampleGeneratedVersion()));
        state.Apply(new ProposedAgentReplyCreated(
            AgentInteractionId,
            new AgentProposedReplyEvidence(ProposalId, SourceConversationId, SourceVersionId, ApproverPolicyVersion, ContentSafetyPolicyVersion, ExpiresAt: null)));
        return state;
    }

    private static AgentInteractionSnapshot ConfirmationSnapshot()
        => new(
            ConfigurationVersion: 3,
            InstructionsVersion: 2,
            AgentResponseMode.Confirmation,
            ApproverPolicyVersion,
            "openai",
            "gpt-4o",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion,
            AgentInteractionSnapshot.DefaultContextPolicyReference);

    private static AgentInteractionContextEvidence SampleContextEvidence()
        => new(
            AgentInteractionContextMode.Full,
            FullContextTokenCount: 1_000,
            UsedContextTokenCount: 1_000,
            MessageCount: 3,
            ReservedOutputTokenCount: 16_000,
            ContextWindowTokenLimit: 128_000,
            ProviderCapabilityVersion: 1,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            BoundedBehaviorReference: null);

    private static AgentGeneratedVersion SampleGeneratedVersion()
        => new(
            SourceVersionId,
            AttemptId: "attempt-1",
            AgentGenerationKind.Generated,
            "the-original-generated-answer",
            "openai",
            "gpt-4o",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion,
            PromptTokenCount: 1_200,
            OutputTokenCount: 350);
}
