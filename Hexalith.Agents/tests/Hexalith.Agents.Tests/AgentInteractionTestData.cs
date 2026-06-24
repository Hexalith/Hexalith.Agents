using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Shared fixtures for the <c>AgentInteraction</c> aggregate and state-replay tests: a command-envelope builder
/// scoped to the <c>agent-interaction</c> domain, a valid request command with a populated AD-4 snapshot, a
/// success-event builder, and helpers to pre-build and advance interaction state through the production
/// <c>Apply</c> handlers (mirroring <see cref="AgentTestData"/>).
/// </summary>
internal static class AgentInteractionTestData
{
    internal const string InteractionId = "interaction-001";
    internal const string TenantId = "acme";
    internal const string AgentId = "hexa";
    internal const string CallerPartyId = "party-001";
    internal const string SourceConversationId = "conversation-001";
    internal const string Prompt = "Summarize the latest decisions in this thread, please.";
    internal const string IdempotencyKey = "idem-001";
    internal const string ClientCorrelationId = "client-corr-001";

    /// <summary>A valid sample AD-4 configuration snapshot (Automatic mode, openai/gpt-4o, V1 default context policy).</summary>
    internal static AgentInteractionSnapshot SampleSnapshot { get; } = new(
        ConfigurationVersion: 3,
        InstructionsVersion: 2,
        ResponseMode: AgentResponseMode.Automatic,
        ApproverPolicyVersion: 1,
        ProviderId: "openai",
        ModelId: "gpt-4o",
        ProviderCapabilityVersion: 1,
        ContentSafetyPolicyVersion: 1,
        AgentInteractionSnapshot.DefaultContextPolicyReference);

    internal static CommandEnvelope Envelope<T>(
        T command,
        string interactionId = InteractionId,
        string tenantId = TenantId,
        string actorUserId = "caller-user")
        where T : notnull
        => new(
            "msg-" + typeof(T).Name,
            tenantId,
            "agent-interaction",
            interactionId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            "corr-1",
            null,
            actorUserId,
            null);

    /// <summary>A valid server-assembled request command carrying the sample snapshot.</summary>
    /// <param name="agentId">The target Agent id.</param>
    /// <param name="sourceConversationId">The source Conversation reference.</param>
    /// <param name="callerPartyId">The caller Party reference.</param>
    /// <param name="prompt">The caller prompt.</param>
    /// <param name="idempotencyKey">The caller idempotency metadata.</param>
    /// <returns>The request command.</returns>
    internal static RequestAgentInteraction ValidRequest(
        string agentId = AgentId,
        string sourceConversationId = SourceConversationId,
        string callerPartyId = CallerPartyId,
        string prompt = Prompt,
        string idempotencyKey = IdempotencyKey)
        => new(agentId, sourceConversationId, callerPartyId, prompt, idempotencyKey, SampleSnapshot, ClientCorrelationId);

    /// <summary>Builds the success event for a request command (the same shape the aggregate emits).</summary>
    /// <param name="request">The request whose recorded event is built.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The success event.</returns>
    internal static InteractionRequested RequestedEvent(RequestAgentInteraction request, string interactionId = InteractionId)
        => new(
            interactionId,
            request.AgentId,
            request.CallerPartyId,
            request.SourceConversationId,
            request.Snapshot!,
            request.Prompt,
            request.IdempotencyKey);

    /// <summary>Builds interaction state by applying the success event for the given request.</summary>
    /// <param name="request">The request whose recorded event seeds the state.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The rehydrated interaction state.</returns>
    internal static AgentInteractionState StateWith(RequestAgentInteraction request, string interactionId = InteractionId)
    {
        var state = new AgentInteractionState();
        state.Apply(RequestedEvent(request, interactionId));
        return state;
    }

    // ===== Story 2.2 gate fixtures =====

    /// <summary>The gate precondition: requested interaction state (status <c>Requested</c>) for the sample request.</summary>
    /// <returns>The rehydrated requested interaction state, driven through the real <c>Apply(InteractionRequested)</c>.</returns>
    internal static AgentInteractionState StateRequested() => StateWith(ValidRequest());

    /// <summary>Builds one gate verdict.</summary>
    /// <param name="check">The gate check.</param>
    /// <param name="outcome">The fail-closed outcome.</param>
    /// <returns>The verdict.</returns>
    internal static AgentInvocationGateVerdict Verdict(AgentInteractionGateCheck check, AgentInteractionGateOutcome outcome)
        => new(check, outcome);

    /// <summary>All nine gate checks satisfied (the authorized path), one verdict per check in evaluation order.</summary>
    /// <returns>The all-satisfied verdict list.</returns>
    internal static IReadOnlyList<AgentInvocationGateVerdict> AllSatisfied() =>
    [
        Verdict(AgentInteractionGateCheck.TenantAccess, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.CallerPartyState, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.SourceConversationAccess, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.AgentLifecycle, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.AgentPartyIdentity, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.ProviderModelReadiness, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.ResponsePolicy, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.ContentSafetyPolicy, AgentInteractionGateOutcome.Satisfied),
        Verdict(AgentInteractionGateCheck.DependencyFreshness, AgentInteractionGateOutcome.Satisfied),
    ];

    /// <summary>The all-satisfied set with one check overridden to a (blocking) outcome.</summary>
    /// <param name="check">The check to override.</param>
    /// <param name="outcome">The overriding outcome.</param>
    /// <returns>The verdict list with exactly one overridden check.</returns>
    internal static IReadOnlyList<AgentInvocationGateVerdict> SatisfiedExcept(AgentInteractionGateCheck check, AgentInteractionGateOutcome outcome)
        => AllSatisfied().Select(v => v.Check == check ? v with { Outcome = outcome } : v).ToList();

    /// <summary>The gate command carrying the given verdicts for the sample interaction.</summary>
    /// <param name="verdicts">The server-assembled verdicts.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The gate command.</returns>
    internal static EvaluateAgentInteractionGate GateCommand(IReadOnlyList<AgentInvocationGateVerdict> verdicts, string interactionId = InteractionId)
        => new(interactionId, verdicts);

    // ===== Story 2.3 context fixtures =====

    /// <summary>The sample model context-window token limit (matches the OpenAI gpt-4o catalog entry used in the gate tests).</summary>
    internal const int ContextWindowTokenLimit = 128_000;

    /// <summary>The sample reserved-output token count (the catalog entry's max-output limit; AC2).</summary>
    internal const int ReservedOutputTokenCount = 16_000;

    /// <summary>The sample provider capability version backing the budget.</summary>
    internal const int ProviderCapabilityVersion = 1;

    /// <summary>The context precondition: an interaction whose gate passed (status <c>Authorized</c>) so context building may run.</summary>
    /// <returns>The rehydrated authorized interaction state, driven through the real <c>Apply</c> handlers.</returns>
    internal static AgentInteractionState StateAuthorized()
    {
        AgentInteractionState state = StateRequested();
        state.Apply(new AgentInteractionAuthorized(InteractionId));
        return state;
    }

    /// <summary>Builds a server-assembled context measurement with sane budget defaults.</summary>
    /// <param name="loadOutcome">The Conversations load classification.</param>
    /// <param name="fullContextTokenCount">The measured full-context token count.</param>
    /// <param name="messageCount">The visible message count.</param>
    /// <param name="contextWindowTokenLimit">The model context-window token limit.</param>
    /// <param name="reservedOutputTokenCount">The reserved output tokens.</param>
    /// <param name="providerCapabilityVersion">The provider capability version.</param>
    /// <param name="approvedBoundedBehavior">The approved bounded behavior, if any.</param>
    /// <returns>The context measurement.</returns>
    internal static AgentInteractionContextMeasurement Measurement(
        AgentInteractionContextLoadOutcome loadOutcome = AgentInteractionContextLoadOutcome.Loaded,
        int fullContextTokenCount = 1_000,
        int messageCount = 3,
        int contextWindowTokenLimit = ContextWindowTokenLimit,
        int reservedOutputTokenCount = ReservedOutputTokenCount,
        int providerCapabilityVersion = ProviderCapabilityVersion,
        AgentInteractionBoundedContextBehavior? approvedBoundedBehavior = null)
        => new(
            loadOutcome,
            fullContextTokenCount,
            messageCount,
            contextWindowTokenLimit,
            reservedOutputTokenCount,
            providerCapabilityVersion,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            approvedBoundedBehavior);

    /// <summary>A loaded measurement whose full context fits the budget (available = 112000) → ContextReady(Full).</summary>
    /// <returns>The fitting measurement.</returns>
    internal static AgentInteractionContextMeasurement FullFitsMeasurement() => Measurement(fullContextTokenCount: 1_000);

    /// <summary>A loaded measurement whose full context exceeds the budget with no approved bounded behavior → ContextBlocked(ExceedsModelBudget).</summary>
    /// <returns>The oversized measurement.</returns>
    internal static AgentInteractionContextMeasurement OversizedMeasurement() => Measurement(fullContextTokenCount: 200_000);

    /// <summary>A loaded, oversized measurement with an approved bounded behavior that fits → ContextReady(Bounded).</summary>
    /// <param name="boundedLimit">The approved bounded-context token limit.</param>
    /// <returns>The bounded-approved measurement.</returns>
    internal static AgentInteractionContextMeasurement BoundedApprovedMeasurement(int boundedLimit = 50_000)
        => Measurement(
            fullContextTokenCount: 200_000,
            approvedBoundedBehavior: new AgentInteractionBoundedContextBehavior("bounded-conversation-test-v1", boundedLimit));

    /// <summary>The context command carrying the given measurement for the sample interaction.</summary>
    /// <param name="measurement">The server-assembled measurement.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The context command.</returns>
    internal static BuildAgentInteractionContext ContextCommand(AgentInteractionContextMeasurement measurement, string interactionId = InteractionId)
        => new(interactionId, measurement);

    // ===== Story 2.4 generation fixtures =====

    /// <summary>The sample generated content (sensitive — used to assert it never leaks onto a failure event/result; AD-14).</summary>
    internal const string GeneratedContentText = "Here is a concise summary of the latest decisions in this thread.";

    /// <summary>The sample deterministic generation attempt id (reused across retries; AD-13).</summary>
    internal const string GenerationAttemptId = "attempt-interaction-001";

    /// <summary>The sample prompt/input token usage.</summary>
    internal const int PromptTokenCount = 1_200;

    /// <summary>The sample generated-output token usage.</summary>
    internal const int OutputTokenCount = 350;

    /// <summary>The sample content-safety policy version the generated content passed.</summary>
    internal const int ContentSafetyPolicyVersion = 1;

    /// <summary>The generation precondition: an interaction whose context built within bounds (status <c>ContextReady</c>) so generation may run.</summary>
    /// <returns>The rehydrated context-ready interaction state, driven through the real <c>Apply</c> handlers.</returns>
    internal static AgentInteractionState StateContextReady()
    {
        AgentInteractionState state = StateAuthorized();
        state.Apply(new AgentInteractionContextReady(
            InteractionId,
            new AgentInteractionContextEvidence(
                AgentInteractionContextMode.Full,
                FullContextTokenCount: 1_000,
                UsedContextTokenCount: 1_000,
                MessageCount: 3,
                ReservedOutputTokenCount,
                ContextWindowTokenLimit,
                ProviderCapabilityVersion,
                AgentInteractionSnapshot.DefaultContextPolicyReference,
                BoundedBehaviorReference: null)));
        return state;
    }

    /// <summary>A requested interaction whose context build was BLOCKED (status <c>ContextBlocked</c>) — generation must still refuse with <c>ContextNotReady</c> (AD-11).</summary>
    /// <returns>The rehydrated context-blocked interaction state, driven through the real <c>Apply</c> handlers.</returns>
    internal static AgentInteractionState StateContextBlocked()
    {
        AgentInteractionState state = StateAuthorized();
        state.Apply(new AgentInteractionContextBlocked(
            InteractionId,
            AgentInteractionContextBlockReason.ExceedsModelBudget,
            new AgentInteractionContextEvidence(
                AgentInteractionContextMode.Full,
                FullContextTokenCount: 200_000,
                UsedContextTokenCount: 0,
                MessageCount: 3,
                ReservedOutputTokenCount,
                ContextWindowTokenLimit,
                ProviderCapabilityVersion,
                AgentInteractionSnapshot.DefaultContextPolicyReference,
                BoundedBehaviorReference: null)));
        return state;
    }

    /// <summary>Builds a server-assembled generation result with sane defaults. Content rides on a success outcome only.</summary>
    /// <param name="outcome">The server-assembled generation outcome.</param>
    /// <param name="generatedContent">The generated content (carried only on a success outcome).</param>
    /// <param name="promptTokenCount">The prompt/input token usage.</param>
    /// <param name="outputTokenCount">The generated-output token usage.</param>
    /// <returns>The generation result.</returns>
    internal static AgentOutputGenerationResult GenerationResult(
        AgentGenerationOutcome outcome = AgentGenerationOutcome.Succeeded,
        string? generatedContent = GeneratedContentText,
        int promptTokenCount = PromptTokenCount,
        int outputTokenCount = OutputTokenCount)
        => new(
            outcome,
            GenerationAttemptId,
            SampleSnapshot.ProviderId,
            SampleSnapshot.ModelId,
            SampleSnapshot.ProviderCapabilityVersion,
            ContentSafetyPolicyVersion,
            outcome == AgentGenerationOutcome.Succeeded ? generatedContent : null,
            promptTokenCount,
            outputTokenCount);

    /// <summary>A successful generation result carrying the generated content.</summary>
    /// <returns>The succeeded result.</returns>
    internal static AgentOutputGenerationResult SucceededGenerationResult() => GenerationResult(AgentGenerationOutcome.Succeeded);

    /// <summary>A safety-blocked result that DELIBERATELY still carries the (unsafe) content, to prove the policy never emits it (AD-5, AD-14).</summary>
    /// <returns>The safety-blocked result with content attached.</returns>
    internal static AgentOutputGenerationResult SafetyBlockedGenerationResult()
        => GenerationResult(AgentGenerationOutcome.ContentSafetyBlocked) with { GeneratedContent = GeneratedContentText };

    /// <summary>A provider-timeout generation result (no content).</summary>
    /// <returns>The timeout result.</returns>
    internal static AgentOutputGenerationResult ProviderTimeoutGenerationResult() => GenerationResult(AgentGenerationOutcome.ProviderTimeout);

    /// <summary>The generate command carrying the given result for the sample interaction.</summary>
    /// <param name="result">The server-assembled generation result.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The generate command.</returns>
    internal static GenerateAgentOutput GenerateCommand(AgentOutputGenerationResult result, string interactionId = InteractionId)
        => new(interactionId, result);

    // ===== Story 2.5 posting fixtures =====

    /// <summary>The Agent's stable Party reference used for posting (a reference, not PII — AD-7).</summary>
    internal const string AgentPartyId = "agent-party-001";

    /// <summary>The selected generated version id that posting targets (matches the generation attempt-derived version id).</summary>
    internal const string PostedVersionId = "version-" + GenerationAttemptId;

    /// <summary>A sample deterministic Conversation Message id (a safe id; never content).</summary>
    internal const string PostedMessageId = "post-message-001";

    /// <summary>A confirmation-mode snapshot (posts via Epic 3 approval, not automatic posting) for the not-automatic precondition.</summary>
    internal static AgentInteractionSnapshot ConfirmationSnapshot { get; } = SampleSnapshot with { ResponseMode = AgentResponseMode.Confirmation };

    /// <summary>The single generated version on the interaction history (the postable unit; AD-5).</summary>
    /// <returns>The sample generated version.</returns>
    internal static AgentGeneratedVersion SampleGeneratedVersion()
        => new(
            VersionId: PostedVersionId,
            GenerationAttemptId,
            AgentGenerationKind.Generated,
            GeneratedContentText,
            SampleSnapshot.ProviderId,
            SampleSnapshot.ModelId,
            SampleSnapshot.ProviderCapabilityVersion,
            ContentSafetyPolicyVersion,
            PromptTokenCount,
            OutputTokenCount);

    /// <summary>The posting precondition: an interaction that generated output (status <c>Generated</c>) with the given snapshot.</summary>
    /// <param name="snapshot">The AD-4 snapshot to freeze (default: the Automatic-mode sample snapshot).</param>
    /// <returns>The rehydrated generated interaction state, driven through the real <c>Apply</c> handlers.</returns>
    internal static AgentInteractionState StateGenerated(AgentInteractionSnapshot? snapshot = null)
    {
        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            InteractionId, AgentId, CallerPartyId, SourceConversationId, snapshot ?? SampleSnapshot, Prompt, IdempotencyKey));
        state.Apply(new AgentInteractionAuthorized(InteractionId));
        state.Apply(new AgentInteractionContextReady(
            InteractionId,
            new AgentInteractionContextEvidence(
                AgentInteractionContextMode.Full,
                FullContextTokenCount: 1_000,
                UsedContextTokenCount: 1_000,
                MessageCount: 3,
                ReservedOutputTokenCount,
                ContextWindowTokenLimit,
                ProviderCapabilityVersion,
                AgentInteractionSnapshot.DefaultContextPolicyReference,
                BoundedBehaviorReference: null)));
        state.Apply(new AgentOutputGenerated(InteractionId, SampleGeneratedVersion()));
        return state;
    }

    /// <summary>The not-automatic precondition: a generated interaction whose snapshot Response Mode is Confirmation.</summary>
    /// <returns>The rehydrated generated, confirmation-mode interaction state.</returns>
    internal static AgentInteractionState StateGeneratedConfirmationMode() => StateGenerated(ConfirmationSnapshot);

    /// <summary>Builds a server-assembled posting result with sane safe-id defaults (never content).</summary>
    /// <param name="outcome">The server-assembled posting outcome.</param>
    /// <param name="messageId">The deterministic Conversation Message id.</param>
    /// <param name="agentPartyId">The Agent's stable Party reference.</param>
    /// <param name="postedVersionId">The selected generated version id.</param>
    /// <returns>The posting result.</returns>
    internal static AgentResponsePostingResult PostingResult(
        AgentResponsePostingOutcome outcome = AgentResponsePostingOutcome.Posted,
        string messageId = PostedMessageId,
        string agentPartyId = AgentPartyId,
        string postedVersionId = PostedVersionId)
        => new(outcome, messageId, SourceConversationId, agentPartyId, postedVersionId);

    /// <summary>A successful posting result.</summary>
    /// <returns>The posted result.</returns>
    internal static AgentResponsePostingResult PostedResult() => PostingResult(AgentResponsePostingOutcome.Posted);

    /// <summary>A membership-unavailable (seam-absent) posting result.</summary>
    /// <returns>The membership-unavailable result.</returns>
    internal static AgentResponsePostingResult MembershipUnavailableResult() => PostingResult(AgentResponsePostingOutcome.MembershipUnavailable);

    /// <summary>A party-identity-unavailable posting result.</summary>
    /// <returns>The party-identity-unavailable result.</returns>
    internal static AgentResponsePostingResult PartyIdentityUnavailableResult() => PostingResult(AgentResponsePostingOutcome.PartyIdentityUnavailable);

    /// <summary>A post-rejected posting result.</summary>
    /// <returns>The post-rejected result.</returns>
    internal static AgentResponsePostingResult PostRejectedResult() => PostingResult(AgentResponsePostingOutcome.PostRejected);

    /// <summary>The post command carrying the given result for the sample interaction.</summary>
    /// <param name="result">The server-assembled posting result.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The post command.</returns>
    internal static PostAgentResponse PostCommand(AgentResponsePostingResult result, string interactionId = InteractionId)
        => new(interactionId, result);

    // ===== Story 3.1 proposal fixtures =====

    /// <summary>A sample deterministic proposal id (a safe id; never content).</summary>
    internal const string SampleProposalId = "proposal-001";

    /// <summary>A sample optional ISO-8601 expiry timestamp (where configured; AC1).</summary>
    internal const string SampleExpiresAt = "2026-12-31T23:59:59Z";

    /// <summary>Builds a server-assembled proposal-creation result with sane safe-id defaults (never content).</summary>
    /// <param name="outcome">The server-assembled proposal-creation outcome.</param>
    /// <param name="proposalId">The deterministic proposal id.</param>
    /// <param name="proposedVersionId">The selected generated version id held in the proposal.</param>
    /// <param name="expiresAt">The optional ISO-8601 expiry timestamp (null when no expiry policy is configured).</param>
    /// <returns>The proposal-creation result.</returns>
    internal static AgentProposalCreationResult ProposalResult(
        AgentProposalCreationOutcome outcome = AgentProposalCreationOutcome.Created,
        string proposalId = SampleProposalId,
        string proposedVersionId = PostedVersionId,
        string? expiresAt = null)
        => new(
            outcome,
            proposalId,
            SourceConversationId,
            proposedVersionId,
            ConfirmationSnapshot.ApproverPolicyVersion,
            ConfirmationSnapshot.ContentSafetyPolicyVersion,
            expiresAt);

    /// <summary>A successful proposal-creation result.</summary>
    /// <returns>The created result.</returns>
    internal static AgentProposalCreationResult CreatedProposalResult() => ProposalResult(AgentProposalCreationOutcome.Created);

    /// <summary>A version-unavailable proposal-creation result (no version id, no proposal id).</summary>
    /// <returns>The version-unavailable result.</returns>
    internal static AgentProposalCreationResult GeneratedVersionUnavailableProposalResult()
        => ProposalResult(AgentProposalCreationOutcome.GeneratedVersionUnavailable, proposalId: string.Empty, proposedVersionId: string.Empty);

    /// <summary>An adapter-failure proposal-creation result.</summary>
    /// <returns>The adapter-failure result.</returns>
    internal static AgentProposalCreationResult AdapterFailureProposalResult() => ProposalResult(AgentProposalCreationOutcome.AdapterFailure);

    /// <summary>The create-proposal command carrying the given result for the sample interaction.</summary>
    /// <param name="result">The server-assembled proposal-creation result.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The create-proposal command.</returns>
    internal static CreateProposedAgentReply ProposalCommand(AgentProposalCreationResult result, string interactionId = InteractionId)
        => new(interactionId, result);

    // ===== Story 3.3 edit fixtures =====

    /// <summary>The authoring Approver's stable Party reference used for an edit (a reference, not PII — AD-7).</summary>
    internal const string EditorPartyId = "editor-party-001";

    /// <summary>The sample Approver-edited content (sensitive — durable only on the edit version; AD-14).</summary>
    internal const string EditedContentText = "An Approver-corrected version of the reply.";

    /// <summary>The sample deterministic edit attempt id (reused across retries; AD-13).</summary>
    internal const string EditAttemptId = "edit-attempt-001";

    /// <summary>The sample deterministic edited-version id (a safe id derived server-side; AD-13).</summary>
    internal const string EditedVersionId = "edited-version-001";

    /// <summary>Builds an edited generated version (Kind=Edited, carrying the edited content + source/editor provenance).</summary>
    /// <param name="versionId">The deterministic edited version id.</param>
    /// <param name="content">The edited content (sensitive — carried only on the edit version; AD-14).</param>
    /// <param name="sourceVersionId">The id of the version edited from (its provenance).</param>
    /// <returns>The edited version.</returns>
    internal static AgentGeneratedVersion SampleEditedVersion(
        string versionId = EditedVersionId,
        string content = EditedContentText,
        string sourceVersionId = PostedVersionId)
        => new(
            versionId,
            EditAttemptId,
            AgentGenerationKind.Edited,
            content,
            SampleSnapshot.ProviderId,
            SampleSnapshot.ModelId,
            SampleSnapshot.ProviderCapabilityVersion,
            ContentSafetyPolicyVersion,
            PromptTokenCount: 0,
            OutputTokenCount: 0,
            sourceVersionId,
            EditorPartyId);

    /// <summary>Builds a server-assembled proposal-edit result with sane defaults (carries the edited version + content).</summary>
    /// <param name="outcome">The server-assembled edit outcome.</param>
    /// <param name="verdict">The resolved edit-time approver-policy verdict (Valid authorizes the edit).</param>
    /// <param name="editedVersionId">The deterministic edited version id.</param>
    /// <returns>The proposal-edit result.</returns>
    internal static AgentProposalEditResult EditResult(
        AgentProposalEditOutcome outcome = AgentProposalEditOutcome.Edited,
        ApproverPolicyValidationStatus verdict = ApproverPolicyValidationStatus.Valid,
        string editedVersionId = EditedVersionId)
        => new(
            outcome,
            SampleEditedVersion(editedVersionId),
            verdict,
            SampleProposalId,
            SourceConversationId,
            ConfirmationSnapshot.ApproverPolicyVersion,
            ApproverPolicyBasisDisclosure.OperatorOnly);

    /// <summary>A successful, authorized proposal-edit result.</summary>
    /// <returns>The authorized edited result.</returns>
    internal static AgentProposalEditResult EditedProposalResult() => EditResult();

    /// <summary>An adapter-failure proposal-edit result (still authorized — fails closed at the adapter).</summary>
    /// <returns>The adapter-failure result.</returns>
    internal static AgentProposalEditResult AdapterFailureEditResult()
        => EditResult(AgentProposalEditOutcome.AdapterFailure);

    /// <summary>The edit-proposal command carrying the given result for the sample interaction.</summary>
    /// <param name="result">The server-assembled proposal-edit result.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The edit-proposal command.</returns>
    internal static EditProposedAgentReply EditCommand(AgentProposalEditResult result, string interactionId = InteractionId)
        => new(interactionId, result);

    // ===== Story 3.4 regeneration fixtures =====

    /// <summary>The requesting Approver's stable Party reference used for a regeneration (a reference, not PII — AD-7).</summary>
    internal const string RequesterPartyId = "requester-party-001";

    /// <summary>The sample freshly regenerated content (sensitive — durable only on the regeneration version; AD-14).</summary>
    internal const string RegeneratedContentText = "A freshly regenerated version of the reply.";

    /// <summary>The sample deterministic regeneration attempt id (reused across retries; AD-13).</summary>
    internal const string RegenerationAttemptId = "regeneration-attempt-001";

    /// <summary>The sample deterministic regenerated-version id (a safe id derived server-side; AD-13).</summary>
    internal const string RegeneratedVersionId = "regenerated-version-001";

    /// <summary>Builds a regenerated generated version (Kind=Regenerated, carrying the regenerated content; a fresh generation has no source-version/editor provenance).</summary>
    /// <param name="versionId">The deterministic regenerated version id.</param>
    /// <param name="content">The regenerated content (sensitive — carried only on the regeneration version; AD-14).</param>
    /// <returns>The regenerated version.</returns>
    internal static AgentGeneratedVersion SampleRegeneratedVersion(
        string versionId = RegeneratedVersionId,
        string content = RegeneratedContentText)
        => new(
            versionId,
            RegenerationAttemptId,
            AgentGenerationKind.Regenerated,
            content,
            SampleSnapshot.ProviderId,
            SampleSnapshot.ModelId,
            SampleSnapshot.ProviderCapabilityVersion,
            ContentSafetyPolicyVersion,
            PromptTokenCount,
            OutputTokenCount);

    /// <summary>Builds a server-assembled proposal-regeneration result with sane defaults (carries the regenerated version + content on a success outcome).</summary>
    /// <param name="outcome">The server-assembled regeneration outcome.</param>
    /// <param name="verdict">The resolved regeneration-time approver-policy verdict (Valid authorizes the regeneration).</param>
    /// <param name="regeneratedVersionId">The deterministic regenerated version id.</param>
    /// <param name="withVersion">Whether to carry the content-bearing version (a successful safe regeneration carries it; every failure carries none).</param>
    /// <returns>The proposal-regeneration result.</returns>
    internal static AgentProposalRegenerationResult RegenerationResult(
        AgentProposalRegenerationOutcome outcome = AgentProposalRegenerationOutcome.Regenerated,
        ApproverPolicyValidationStatus verdict = ApproverPolicyValidationStatus.Valid,
        string regeneratedVersionId = RegeneratedVersionId,
        bool withVersion = true)
        => new(
            outcome,
            RegenerationAttemptId,
            regeneratedVersionId,
            withVersion && outcome == AgentProposalRegenerationOutcome.Regenerated ? SampleRegeneratedVersion(regeneratedVersionId) : null,
            verdict,
            SampleProposalId,
            SourceConversationId,
            RequesterPartyId,
            SampleSnapshot.ProviderId,
            SampleSnapshot.ModelId,
            SampleSnapshot.ProviderCapabilityVersion,
            ContentSafetyPolicyVersion,
            ConfirmationSnapshot.ApproverPolicyVersion,
            ApproverPolicyBasisDisclosure.OperatorOnly,
            PromptTokenCount,
            OutputTokenCount);

    /// <summary>A successful, authorized proposal-regeneration result.</summary>
    /// <returns>The authorized regenerated result.</returns>
    internal static AgentProposalRegenerationResult RegeneratedProposalResult() => RegenerationResult();

    /// <summary>A provider-timeout regeneration result (still authorized — fails closed at the provider; no version).</summary>
    /// <returns>The provider-timeout result.</returns>
    internal static AgentProposalRegenerationResult ProviderTimeoutRegenerationResult()
        => RegenerationResult(AgentProposalRegenerationOutcome.ProviderTimeout, withVersion: false);

    /// <summary>A content-safety-blocked regeneration result (still authorized — fails closed at the safety gate; no version).</summary>
    /// <returns>The safety-blocked result.</returns>
    internal static AgentProposalRegenerationResult ContentSafetyBlockedRegenerationResult()
        => RegenerationResult(AgentProposalRegenerationOutcome.ContentSafetyBlocked, withVersion: false);

    /// <summary>The regenerate-proposal command carrying the given result for the sample interaction.</summary>
    /// <param name="result">The server-assembled proposal-regeneration result.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The regenerate-proposal command.</returns>
    internal static RegenerateProposedAgentReply RegenerateCommand(AgentProposalRegenerationResult result, string interactionId = InteractionId)
        => new(interactionId, result);

    /// <summary>The edit precondition: a Confirmation-mode interaction that reached a Pending proposal (status <c>ProposalCreated</c>).</summary>
    /// <returns>The rehydrated proposal-created interaction state, driven through the real <c>Apply</c> handlers.</returns>
    internal static AgentInteractionState StateProposalCreated()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();
        state.Apply(new ProposedAgentReplyCreated(
            InteractionId,
            new AgentProposedReplyEvidence(
                SampleProposalId,
                SourceConversationId,
                PostedVersionId,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ConfirmationSnapshot.ContentSafetyPolicyVersion,
                ExpiresAt: null)));
        return state;
    }

    /// <summary>
    /// Applies every event of a <see cref="DomainResult"/> to the supplied state through the aggregate's typed
    /// <c>Apply</c> methods — the same production replay handlers the EventStore state-store invokes. The success
    /// event advances state; rejection events are replay-safe no-ops (they must not mutate state).
    /// </summary>
    /// <param name="state">The interaction state to advance in place.</param>
    /// <param name="result">The domain result whose events are applied in order.</param>
    internal static void ApplyAll(AgentInteractionState state, DomainResult result)
    {
        foreach (IEventPayload payload in result.Events)
        {
            switch (payload)
            {
                case InteractionRequested e: state.Apply(e); break;
                case AgentInteractionAuthorized e: state.Apply(e); break;
                case AgentInteractionGateFailed e: state.Apply(e); break;
                case InvalidAgentInteractionRequestRejection e: state.Apply(e); break;
                case AgentInteractionAlreadyRequestedRejection e: state.Apply(e); break;
                case AgentInteractionGateNotEvaluableRejection e: state.Apply(e); break;
                case AgentInteractionContextReady e: state.Apply(e); break;
                case AgentInteractionContextBlocked e: state.Apply(e); break;
                case AgentInteractionContextNotBuildableRejection e: state.Apply(e); break;
                case AgentOutputGenerated e: state.Apply(e); break;
                case AgentOutputGenerationFailed e: state.Apply(e); break;
                case AgentOutputNotGeneratableRejection e: state.Apply(e); break;
                case AgentResponsePosted e: state.Apply(e); break;
                case AgentResponsePostingFailed e: state.Apply(e); break;
                case AgentResponseNotPostableRejection e: state.Apply(e); break;
                case ProposedAgentReplyCreated e: state.Apply(e); break;
                case ProposedAgentReplyCreationFailed e: state.Apply(e); break;
                case ProposedAgentReplyNotCreatableRejection e: state.Apply(e); break;
                case ProposedAgentReplyEdited e: state.Apply(e); break;
                case ProposedAgentReplyEditFailed e: state.Apply(e); break;
                case ProposedAgentReplyNotEditableRejection e: state.Apply(e); break;
                case ProposedAgentReplyRegenerated e: state.Apply(e); break;
                case ProposedAgentReplyRegenerationFailed e: state.Apply(e); break;
                case ProposedAgentReplyNotRegeneratableRejection e: state.Apply(e); break;
                default: throw new InvalidOperationException($"Unhandled event type '{payload.GetType().Name}' in test apply dispatch.");
            }
        }
    }

    /// <summary>
    /// Drives one command end-to-end through the real aggregate pipeline — JSON-serialized command envelope →
    /// reflection dispatch in <c>AgentInteractionAggregate.ProcessAsync</c> → typed handler → events — then applies
    /// the resulting events to <paramref name="state"/> so the next command sees the evolved state.
    /// </summary>
    /// <typeparam name="TCommand">The command type (drives the dispatch lookup and payload round-trip).</typeparam>
    /// <param name="aggregate">The aggregate under test.</param>
    /// <param name="state">The threaded interaction state, advanced in place.</param>
    /// <param name="command">The command to process.</param>
    /// <param name="interactionId">The deterministic interaction id (the aggregate id).</param>
    /// <returns>The domain result of processing the command.</returns>
    internal static async Task<DomainResult> ProcessAndApplyAsync<TCommand>(
        AgentInteractionAggregate aggregate,
        AgentInteractionState state,
        TCommand command,
        string interactionId = InteractionId)
        where TCommand : notnull
    {
        DomainResult result = await aggregate.ProcessAsync(Envelope(command, interactionId), state);
        ApplyAll(state, result);
        return result;
    }
}
