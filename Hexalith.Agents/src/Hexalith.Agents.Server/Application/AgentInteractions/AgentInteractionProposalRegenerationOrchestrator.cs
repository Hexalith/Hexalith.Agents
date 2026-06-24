using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for an authorized Approver's regeneration of a pending Proposed Agent Reply (Story 3.4;
/// AC1–AC4; AD-3, AD-5, AD-9, AD-12, AD-13, AD-14). It IS the durable-owner regeneration step and combines the Story 3.3
/// edit-time approver authorization with the Story 2.4 provider invocation + content-safety gate: it guards AC4 (a terminal
/// proposal never invokes the provider), re-resolves the proposal's snapshotted Approver Policy and fails closed, re-reads the
/// SAME Source Conversation, re-invokes the provider behind its adapter using the SAME snapshotted configuration, evaluates the
/// effective Content Safety Policy, derives a deterministic regenerated-version id, assembles a server-trusted
/// <see cref="AgentProposalRegenerationResult"/>, and dispatches exactly one <see cref="RegenerateProposedAgentReply"/> command.
/// The impure work happens here, outside the pure aggregate, and feeds back through the single command (the AD-3 round-trip).
/// Mirrors <see cref="AgentInteractionGenerationOrchestrator"/> (provider + safety) plus
/// <see cref="AgentInteractionProposalEditOrchestrator"/> (approver authorization).
/// </summary>
/// <remarks>
/// <b>No provider invocation before authorization/terminal checks (AC4; AD-12):</b> the terminal-proposal guard and the
/// fail-closed approver authorization both run BEFORE any conversation re-read or provider call — a terminal proposal or a
/// non-<c>Valid</c> verdict returns a no-dispatch denial (no command, no event, NO provider invocation). The approver resolver
/// folds projection staleness into its <c>Unavailable</c> outcomes, which the pure <see cref="ApproverPolicyVerdict"/> fails
/// closed on. <b>Provider boundary (AD-9):</b> the provider SDK, credentials, raw payloads, and provider-specific errors stay
/// behind <see cref="IAgentGenerationProvider"/>; this orchestration sees only safe ids/outcomes/usage. <b>Block before side
/// effect:</b> content safety is evaluated BEFORE the success command is dispatched — a blocked verdict yields a fail-closed
/// decision and NO version. <b>Configuration confinement (FR-16):</b> the same snapshotted provider/model/capability/
/// content-safety configuration is reused; no caller-supplied configuration is honored. <b>Conversations boundary (AD-6):</b>
/// a regenerated Proposed Agent Reply is NEVER a Conversation Message — this path makes NO Conversations write. <b>Content
/// confinement (AD-14):</b> the re-read text and regenerated content are used only transiently in memory and ride into the
/// aggregate ONLY on a successful command; failures carry no content. The returned status comes from the shared
/// <see cref="AgentProposalRegenerationPolicy"/> so it cannot drift from the aggregate's recorded decision.
/// </remarks>
public sealed class AgentInteractionProposalRegenerationOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This regeneration path repopulates none of them; they are stripped from
    // client-supplied extensions so a client cannot smuggle a forged admin/verdict onto the interaction stream.
    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IConversationContextReader _contextReader;
    private readonly IProviderCatalogReader _providerCatalogReader;
    private readonly IAgentGenerationProvider _generationProvider;
    private readonly IAgentContentSafetyPolicyReader _policyReader;
    private readonly IContentSafetyEvaluator _safetyEvaluator;
    private readonly IApproverPolicyResolver _approverPolicyResolver;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionProposalRegenerationOrchestrator"/> class.</summary>
    /// <param name="contextReader">The Source Conversation content re-read port (reused from Story 2.4; live binding conditional on a Conversations config section).</param>
    /// <param name="providerCatalogReader">The reused provider-catalog reader (budget/timeout/status source).</param>
    /// <param name="generationProvider">The provider-generation adapter port (reused from Story 2.4; live binding deferred — fails closed).</param>
    /// <param name="policyReader">The effective Content Safety Policy reader port (reused from Story 2.4; live binding deferred — fails closed).</param>
    /// <param name="safetyEvaluator">The content-safety evaluator port (reused from Story 2.4; live binding deferred — fails closed).</param>
    /// <param name="approverPolicyResolver">The reused approver-policy resolution port (Story 1.6/3.3; live binding deferred — fails closed).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionProposalRegenerationOrchestrator(
        IConversationContextReader contextReader,
        IProviderCatalogReader providerCatalogReader,
        IAgentGenerationProvider generationProvider,
        IAgentContentSafetyPolicyReader policyReader,
        IContentSafetyEvaluator safetyEvaluator,
        IApproverPolicyResolver approverPolicyResolver,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(contextReader);
        ArgumentNullException.ThrowIfNull(providerCatalogReader);
        ArgumentNullException.ThrowIfNull(generationProvider);
        ArgumentNullException.ThrowIfNull(policyReader);
        ArgumentNullException.ThrowIfNull(safetyEvaluator);
        ArgumentNullException.ThrowIfNull(approverPolicyResolver);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _contextReader = contextReader;
        _providerCatalogReader = providerCatalogReader;
        _generationProvider = generationProvider;
        _policyReader = policyReader;
        _safetyEvaluator = safetyEvaluator;
        _approverPolicyResolver = approverPolicyResolver;
        _dispatcher = dispatcher;
    }

    /// <summary>Guards the terminal/authorization checks, re-reads + regenerates + safety-checks, assembles + dispatches the regenerate command, and returns the safe decided outcome (AC1–AC4).</summary>
    /// <param name="request">The regeneration orchestration request (trusted proposal state + snapshot-recorded ids/policy + caller context).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe regeneration outcome (regenerated version id + decided status), or a no-dispatch fail-closed denial.</returns>
    public async Task<AgentInteractionProposalRegenerationOutcomeResult> ExecuteAsync(AgentInteractionProposalRegenerationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // (1) AC4 — a terminal proposal can never invoke the provider. The trusted request carries the current proposal
        // sub-state; if it is not in the retryable set { Pending, Edited, Regenerated } we deny WITHOUT reading the
        // conversation, invoking the provider, or dispatching. No provider invocation occurs for a terminal proposal.
        if (!IsRetryable(request.ProposalState))
        {
            return new AgentInteractionProposalRegenerationOutcomeResult(
                string.Empty,
                AgentInteractionStatus.Unknown,
                AgentProposedReplyNotRegeneratableReason.ProposalNotPending);
        }

        // (2) Resolve regeneration-time approver authorization, fail closed (AD-12). A non-Valid verdict denies the
        // regeneration WITHOUT invoking the provider or dispatching — WHO may act is enforced before the model runs (FR-16).
        ApproverPolicyValidationStatus verdict = await ResolveAuthorizationAsync(request, ct).ConfigureAwait(false);
        if (verdict != ApproverPolicyValidationStatus.Valid)
        {
            return new AgentInteractionProposalRegenerationOutcomeResult(
                string.Empty,
                AgentInteractionStatus.Unknown,
                AgentProposedReplyNotRegeneratableReason.NotAuthorized);
        }

        // (3) Derive the deterministic regeneration ids from the interaction + source conversation + regeneration attempt so a
        // retry reuses the same identity and the aggregate's terminal no-op dedupes it (AD-13).
        string attemptId = AgentProposalRegenerationIdentity.DeriveAttemptId(
            request.AgentInteractionId, request.SourceConversationId, request.RegenerationAttemptId);
        string regeneratedVersionId = AgentProposalRegenerationIdentity.DeriveVersionId(
            request.AgentInteractionId, request.SourceConversationId, request.RegenerationAttemptId);

        // (4) Assemble the server-trusted regeneration result from the impure steps (each fail-closed). Sensitive content is
        // used only transiently inside RegenerateAsync and rides onto the result ONLY on a Regenerated outcome (AD-5, AD-14).
        AgentProposalRegenerationResult result = await RegenerateAsync(request, verdict, attemptId, regeneratedVersionId, ct).ConfigureAwait(false);

        // (5) Build the server-trusted regenerate command + envelope (AggregateId = interaction id, Domain = agent-interaction);
        // strip reserved client trust extensions (none repopulated on this path).
        var command = new RegenerateProposedAgentReply(request.AgentInteractionId, result);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(RegenerateProposedAgentReply),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // (6) Dispatch (live binding deferred), then return the safe outcome — status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-3).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionProposalRegenerationOutcomeResult(regeneratedVersionId, AgentProposalRegenerationPolicy.Decide(result));
    }

    // A proposal is regeneratable only while its sub-state is in the retryable set { Pending, Edited, Regenerated } — the same
    // set the aggregate enforces. Terminal sub-states (Stories 3.5/3.6) and a missing proposal are non-retryable (AC4).
    private static bool IsRetryable(ProposedAgentReplyState state)
        => state is ProposedAgentReplyState.Pending or ProposedAgentReplyState.Edited or ProposedAgentReplyState.Regenerated;

    // Re-resolves the snapshotted Approver Policy against current dependencies and computes the fail-closed verdict (AD-8,
    // AD-12). A null/empty policy is Incomplete (nothing to authorize with → denied); a resolver that throws fails closed to
    // Unavailable. A genuine cancellation propagates (the catch deliberately excludes OperationCanceledException).
    private async Task<ApproverPolicyValidationStatus> ResolveAuthorizationAsync(AgentInteractionProposalRegenerationRequest request, CancellationToken ct)
    {
        if (request.ApproverPolicy is not { Sources.Count: > 0 } policy)
        {
            return ApproverPolicyValidationStatus.Incomplete;
        }

        try
        {
            ApproverPolicyResolutionResult resolution = await _approverPolicyResolver
                .ResolveAsync(request.TenantId, policy, ct)
                .ConfigureAwait(false);
            return ApproverPolicyVerdict.Evaluate(policy, resolution);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception (AD-14: no stack traces/payloads leak).
            return ApproverPolicyValidationStatus.Unavailable;
        }
    }

    // Assembles the server-trusted regeneration result: re-read content → read budget → invoke provider → content-safety gate.
    // Every step is fail-closed; the first failing step short-circuits to a content-free failure result so the pure policy
    // records a fail-closed audit and no approvable version (AD-5, AD-12). The same snapshotted configuration is reused (FR-16).
    private async Task<AgentProposalRegenerationResult> RegenerateAsync(
        AgentInteractionProposalRegenerationRequest request,
        ApproverPolicyValidationStatus verdict,
        string attemptId,
        string regeneratedVersionId,
        CancellationToken ct)
    {
        // (1) Re-read the SAME Source Conversation content fresh (the proposal is linked to it; AD-6). Fail closed → InvalidContext.
        ConversationContextReadResult read = await ReadContextAsync(request, ct).ConfigureAwait(false);
        if (read.Outcome != AgentInteractionContextLoadOutcome.Loaded || read.Messages is null)
        {
            return FailedResult(AgentProposalRegenerationOutcome.InvalidContext, request, verdict, attemptId, regeneratedVersionId);
        }

        // (2) Read the SAME snapshotted model budget/timeout/status from the reused catalog. Disabled/not-text-gen →
        // ProviderDisabled; read failure / not-found / not-authorized → ProviderUnavailable.
        ProviderRegenerationBudget budget = await ReadBudgetAsync(request, ct).ConfigureAwait(false);
        if (budget.Outcome != AgentProposalRegenerationOutcome.Regenerated)
        {
            return FailedResult(budget.Outcome, request, verdict, attemptId, regeneratedVersionId);
        }

        // (3) Re-invoke the provider behind its adapter, honoring the timeout/retry budget. A thrown adapter → AdapterFailure
        // (no raw provider error leaks; AD-14). A non-success provider outcome maps through; a degraded success (no content)
        // fails closed to AdapterFailure (there is no GenerationError on the regeneration surface).
        AgentGenerationProviderResult providerResult = await InvokeProviderAsync(request, read.Messages, budget, attemptId, ct).ConfigureAwait(false);
        if (providerResult.Outcome != AgentGenerationOutcome.Succeeded || providerResult.GeneratedContent is null)
        {
            AgentProposalRegenerationOutcome outcome = providerResult.Outcome == AgentGenerationOutcome.Succeeded
                ? AgentProposalRegenerationOutcome.AdapterFailure // a "succeeded" result with no content is a degraded provider result
                : MapProviderOutcome(providerResult.Outcome);
            return FailedResult(outcome, request, verdict, attemptId, regeneratedVersionId, providerResult.PromptTokenCount, providerResult.OutputTokenCount);
        }

        // (4) Content-safety gate — BEFORE any downstream artifact. Read the effective policy (not-available → PolicyFailure),
        // then evaluate the regenerated content (Blocked → ContentSafetyBlocked, which the policy maps to a fail-closed decision
        // and emits NO version — AD-5).
        AgentContentSafetyPolicyReadResult policy = await ReadPolicyAsync(request, ct).ConfigureAwait(false);
        if (!policy.IsAvailable || policy.Policy is null)
        {
            return FailedResult(AgentProposalRegenerationOutcome.PolicyFailure, request, verdict, attemptId, regeneratedVersionId, providerResult.PromptTokenCount, providerResult.OutputTokenCount);
        }

        ContentSafetyVerdict safety = await EvaluateSafetyAsync(providerResult.GeneratedContent, policy.Policy, ct).ConfigureAwait(false);
        if (safety != ContentSafetyVerdict.Passed)
        {
            // Fail closed and carry NO content — the unsafe content never reaches the aggregate (AD-5, AD-14).
            return FailedResult(AgentProposalRegenerationOutcome.ContentSafetyBlocked, request, verdict, attemptId, regeneratedVersionId, providerResult.PromptTokenCount, providerResult.OutputTokenCount);
        }

        // (5) Success — carry the regenerated content on the new immutable Regenerated version (its sole durable home is the
        // success event — AD-14). VersionId is the deterministic regenerated-version id; the version is a fresh provider
        // generation, so it has no source-version/editor provenance (mirroring a first-pass Generated version).
        var version = new AgentGeneratedVersion(
            regeneratedVersionId,
            attemptId,
            AgentGenerationKind.Regenerated,
            providerResult.GeneratedContent,
            request.ProviderId,
            request.ModelId,
            request.ProviderCapabilityVersion,
            policy.PolicyVersion,
            providerResult.PromptTokenCount,
            providerResult.OutputTokenCount);

        return new AgentProposalRegenerationResult(
            AgentProposalRegenerationOutcome.Regenerated,
            attemptId,
            regeneratedVersionId,
            version,
            verdict,
            request.ProposalId,
            request.SourceConversationId,
            request.RequesterPartyId,
            request.ProviderId,
            request.ModelId,
            request.ProviderCapabilityVersion,
            policy.PolicyVersion,
            request.ApproverPolicyVersion,
            request.ApproverPolicy?.DisclosureCategory ?? ApproverPolicyBasisDisclosure.Omitted,
            providerResult.PromptTokenCount,
            providerResult.OutputTokenCount);
    }

    private async Task<ConversationContextReadResult> ReadContextAsync(AgentInteractionProposalRegenerationRequest request, CancellationToken ct)
    {
        try
        {
            // The regeneration re-read keys on the actor security principal; authorization was already settled at the gate
            // (Story 2.2) and at the approver check above, so the auxiliary caller-party reference is not carried here.
            return await _contextReader
                .ReadAsync(request.TenantId, request.SourceConversationId, string.Empty, request.ActorUserId, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — never propagate the raw exception (AD-14: no stack traces/payloads leak).
            return ConversationContextReadResult.Unavailable;
        }
    }

    // Reads the model budget from the reused provider catalog. A missing/not-authorized/degraded read or a throw fails closed
    // to ProviderUnavailable; a disabled or non-text-capable entry fails closed to ProviderDisabled.
    private async Task<ProviderRegenerationBudget> ReadBudgetAsync(AgentInteractionProposalRegenerationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ModelId))
        {
            return ProviderRegenerationBudget.Failed(AgentProposalRegenerationOutcome.ProviderUnavailable);
        }

        try
        {
            ProviderCatalogEntryReadResult result = await _providerCatalogReader
                .GetEntryAsync(request.TenantId, request.ProviderId, request.ModelId, ct)
                .ConfigureAwait(false);

            if (result is not { Status: ProviderCatalogInspectionStatus.Success, Entry: { } entry })
            {
                return ProviderRegenerationBudget.Failed(AgentProposalRegenerationOutcome.ProviderUnavailable);
            }

            if (entry.Status != ProviderModelStatus.Enabled || !entry.SupportsTextGeneration)
            {
                return ProviderRegenerationBudget.Failed(AgentProposalRegenerationOutcome.ProviderDisabled);
            }

            return ProviderRegenerationBudget.Ready(
                entry.MaxOutputTokenLimit,
                entry.TimeoutPolicy.RequestTimeoutMilliseconds,
                entry.TimeoutPolicy.MaxRetries);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProviderRegenerationBudget.Failed(AgentProposalRegenerationOutcome.ProviderUnavailable);
        }
    }

    private async Task<AgentGenerationProviderResult> InvokeProviderAsync(
        AgentInteractionProposalRegenerationRequest request,
        IReadOnlyList<ConversationContextMessage> messages,
        ProviderRegenerationBudget budget,
        string attemptId,
        CancellationToken ct)
    {
        try
        {
            var providerRequest = new AgentGenerationProviderRequest(
                request.ProviderId,
                request.ModelId,
                request.ProviderCapabilityVersion,
                BuildContextPayload(messages),
                budget.MaxOutputTokenLimit,
                budget.RequestTimeoutMilliseconds,
                budget.MaxRetries,
                attemptId);
            return await _generationProvider.GenerateAsync(providerRequest, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — the provider error text never crosses this boundary (AD-9, AD-14).
            return new AgentGenerationProviderResult(AgentGenerationOutcome.AdapterFailure, null, 0, 0);
        }
    }

    private async Task<AgentContentSafetyPolicyReadResult> ReadPolicyAsync(AgentInteractionProposalRegenerationRequest request, CancellationToken ct)
    {
        try
        {
            return await _policyReader.ReadAsync(request.TenantId, request.AgentId, request.ResponseMode, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-12) — a policy that cannot be read drives PolicyFailure, never a skipped safety gate.
            return AgentContentSafetyPolicyReadResult.NotAvailable;
        }
    }

    private async Task<ContentSafetyVerdict> EvaluateSafetyAsync(string content, AgentContentSafetyPolicy policy, CancellationToken ct)
    {
        try
        {
            var safetyRequest = new ContentSafetyEvaluationRequest(
                content,
                policy.PromptConstraints,
                policy.BlockedOutputCategories,
                policy.RestrictedOutputCategories,
                policy.FailureHandling);
            ContentSafetyEvaluationResult result = await _safetyEvaluator.EvaluateAsync(safetyRequest, ct).ConfigureAwait(false);
            return result.Verdict;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail closed (AD-14) — an evaluator that cannot clear the content blocks it.
            return ContentSafetyVerdict.Blocked;
        }
    }

    // Assembles the model input from the re-read visible timeline (ordered for stability). Sensitive content held only in
    // memory for the provider invocation — never persisted, put on a command/event/view, or logged (AD-14).
    private static string BuildContextPayload(IReadOnlyList<ConversationContextMessage> messages)
    {
        var builder = new StringBuilder();
        foreach (ConversationContextMessage message in messages.OrderBy(m => m.CreatedAt))
        {
            builder.AppendLine(message.Text);
        }

        return builder.ToString();
    }

    // Maps a provider/generation outcome to the regeneration outcome. The provider surface (AgentGenerationOutcome) has a
    // GenerationError value the regeneration surface deliberately omits; it and any unknown sentinel fail closed to the
    // content-free adapter generic.
    private static AgentProposalRegenerationOutcome MapProviderOutcome(AgentGenerationOutcome outcome) => outcome switch
    {
        AgentGenerationOutcome.ProviderTimeout => AgentProposalRegenerationOutcome.ProviderTimeout,
        AgentGenerationOutcome.ProviderDisabled => AgentProposalRegenerationOutcome.ProviderDisabled,
        AgentGenerationOutcome.ProviderUnavailable => AgentProposalRegenerationOutcome.ProviderUnavailable,
        AgentGenerationOutcome.AdapterFailure => AgentProposalRegenerationOutcome.AdapterFailure,
        AgentGenerationOutcome.InvalidContext => AgentProposalRegenerationOutcome.InvalidContext,
        AgentGenerationOutcome.ContentSafetyBlocked => AgentProposalRegenerationOutcome.ContentSafetyBlocked,
        AgentGenerationOutcome.PolicyFailure => AgentProposalRegenerationOutcome.PolicyFailure,
        _ => AgentProposalRegenerationOutcome.AdapterFailure,
    };

    // A content-free failure result — never carries regenerated content (AD-5, AD-14). Token counts are carried only when a
    // provider produced them before the failure (e.g. a safety block after a successful provider call). The verdict is Valid
    // here (a non-Valid verdict denies before any provider call), so the policy classifies on the provider/safety outcome.
    private static AgentProposalRegenerationResult FailedResult(
        AgentProposalRegenerationOutcome outcome,
        AgentInteractionProposalRegenerationRequest request,
        ApproverPolicyValidationStatus verdict,
        string attemptId,
        string regeneratedVersionId,
        int promptTokenCount = 0,
        int outputTokenCount = 0)
        => new(
            outcome,
            attemptId,
            regeneratedVersionId,
            RegeneratedVersion: null,
            verdict,
            request.ProposalId,
            request.SourceConversationId,
            request.RequesterPartyId,
            request.ProviderId,
            request.ModelId,
            request.ProviderCapabilityVersion,
            request.ContentSafetyPolicyVersion,
            request.ApproverPolicyVersion,
            request.ApproverPolicy?.DisclosureCategory ?? ApproverPolicyBasisDisclosure.Omitted,
            promptTokenCount,
            outputTokenCount);

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the regeneration path
    // carries no admin/verdict extension — the verdict rides the trusted command result, not an envelope extension). Returns
    // null when nothing benign remains so the envelope carries no empty map.
    private static Dictionary<string, string>? BuildTrustedExtensions(IReadOnlyDictionary<string, string>? clientSupplied)
    {
        if (clientSupplied is null)
        {
            return null;
        }

        var extensions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string key, string value) in clientSupplied)
        {
            if (Array.IndexOf(_reservedExtensionKeys, key) < 0)
            {
                extensions[key] = value;
            }
        }

        return extensions.Count > 0 ? extensions : null;
    }

    // The safe model-budget/timeout read result. A non-Regenerated outcome short-circuits regeneration to a fail-closed result.
    private readonly record struct ProviderRegenerationBudget(
        AgentProposalRegenerationOutcome Outcome,
        int MaxOutputTokenLimit,
        int RequestTimeoutMilliseconds,
        int MaxRetries)
    {
        public static ProviderRegenerationBudget Ready(int maxOutputTokenLimit, int requestTimeoutMilliseconds, int maxRetries)
            => new(AgentProposalRegenerationOutcome.Regenerated, maxOutputTokenLimit, requestTimeoutMilliseconds, maxRetries);

        public static ProviderRegenerationBudget Failed(AgentProposalRegenerationOutcome outcome)
            => new(outcome, 0, 0, 0);
    }
}
