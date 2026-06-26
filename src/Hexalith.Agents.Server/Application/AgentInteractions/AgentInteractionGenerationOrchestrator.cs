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
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for Agent-output generation + content safety (Story 2.4; AC1–AC4; AD-3, AD-5, AD-9, AD-13,
/// AD-18). It IS the durable-owner generation step: it re-reads the Source Conversation content (Story 2.3 discarded it,
/// keeping only token counts), reads the Provider budget/timeout, invokes the provider behind its adapter, resolves +
/// evaluates the effective Content Safety Policy, assembles a server-trusted <see cref="AgentOutputGenerationResult"/>,
/// and dispatches the <see cref="GenerateAgentOutput"/> command. The impure work happens here, outside the pure
/// aggregate, and feeds back through the single command (the AD-3 round-trip).
/// </summary>
/// <remarks>
/// <b>Provider boundary (AD-9):</b> the provider SDK, credentials, raw payloads, and provider-specific errors stay behind
/// <see cref="IAgentGenerationProvider"/>; this orchestration sees only safe ids/outcomes/usage. <b>Block before side
/// effect (AC2):</b> content safety is evaluated BEFORE the success command is dispatched — a blocked verdict yields a
/// <c>SafetyFailed</c> decision and NO version. <b>Fail closed (FR-10, FR-12):</b> conversation re-read failure →
/// <c>InvalidContext</c>; budget disabled/unreadable → <c>ProviderDisabled</c>/<c>ProviderUnavailable</c>; a provider that
/// throws → <c>AdapterFailure</c> (no raw error propagated, AD-14); safety-policy reader not-available → <c>PolicyFailure</c>;
/// the all-deferred default graph → <c>ProviderUnavailable</c>. <b>No posting / no proposal:</b> this story records the
/// generation outcome only — it never posts a Conversation Message (Story 2.5) or creates a Proposed Agent Reply (Story
/// 3.1). <b>Sensitive content (AD-14):</b> the re-read text and generated content are used only transiently in memory and
/// ride into the aggregate ONLY on a successful command; failures carry no content. The returned status comes from the
/// shared <see cref="AgentOutputGenerationPolicy"/> so it cannot drift from the aggregate's recorded decision.
/// </remarks>
public sealed class AgentInteractionGenerationOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    // Reserved server-populated trust keys. This generation path repopulates none of them; they are stripped from
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
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionGenerationOrchestrator"/> class.</summary>
    /// <param name="contextReader">The Source Conversation content re-read port (live binding conditional on a Conversations config section).</param>
    /// <param name="providerCatalogReader">The reused provider-catalog reader (budget/timeout/status source).</param>
    /// <param name="generationProvider">The provider-generation adapter port (live binding deferred — fails closed).</param>
    /// <param name="policyReader">The effective Content Safety Policy reader port (live binding deferred — fails closed).</param>
    /// <param name="safetyEvaluator">The content-safety evaluator port (live binding deferred — fails closed).</param>
    /// <param name="dispatcher">The command-dispatch seam (live binding deferred).</param>
    public AgentInteractionGenerationOrchestrator(
        IConversationContextReader contextReader,
        IProviderCatalogReader providerCatalogReader,
        IAgentGenerationProvider generationProvider,
        IAgentContentSafetyPolicyReader policyReader,
        IContentSafetyEvaluator safetyEvaluator,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(contextReader);
        ArgumentNullException.ThrowIfNull(providerCatalogReader);
        ArgumentNullException.ThrowIfNull(generationProvider);
        ArgumentNullException.ThrowIfNull(policyReader);
        ArgumentNullException.ThrowIfNull(safetyEvaluator);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _contextReader = contextReader;
        _providerCatalogReader = providerCatalogReader;
        _generationProvider = generationProvider;
        _policyReader = policyReader;
        _safetyEvaluator = safetyEvaluator;
        _dispatcher = dispatcher;
    }

    /// <summary>Re-reads + generates + safety-checks, assembles + dispatches the generate command, and returns the safe decided outcome (AC1–AC4).</summary>
    /// <param name="request">The generation orchestration request (snapshot-recorded ids/policy + caller context).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe generation outcome (interaction id + decided status).</returns>
    public async Task<AgentInteractionGenerationOutcomeResult> ExecuteAsync(AgentInteractionGenerationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The deterministic attempt id derived from the interaction id so a retried generation reuses it (AD-13).
        string attemptId = DeriveAttemptId(request.AgentInteractionId);

        // Assemble the server-trusted generation result from the impure steps (each fail-closed). Sensitive content is
        // used only transiently inside GenerateAsync and rides onto the result ONLY on a Succeeded outcome (AD-5, AD-14).
        AgentOutputGenerationResult result = await GenerateAsync(request, attemptId, ct).ConfigureAwait(false);

        // Build the server-trusted generate command — any client-supplied value is discarded. The interaction id mirrors
        // the envelope aggregate id (Story 2.1).
        var command = new GenerateAgentOutput(request.AgentInteractionId, result);

        // Build the envelope (AggregateId = interaction id, Domain = agent-interaction); strip reserved client trust
        // extensions (none repopulated on this path).
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(GenerateAgentOutput),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        // Dispatch (live binding deferred), then return the safe outcome — status via the SHARED policy so the
        // orchestrator's reported status cannot drift from the aggregate's recorded decision (AD-3).
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return new AgentInteractionGenerationOutcomeResult(request.AgentInteractionId, AgentOutputGenerationPolicy.Decide(result));
    }

    // Assembles the server-trusted generation result: re-read content → read budget → invoke provider → content-safety
    // gate. Every step is fail-closed; the first failing step short-circuits to a content-free failure result so the pure
    // policy records a fail-closed audit and no approvable version (AD-5, AD-12).
    private async Task<AgentOutputGenerationResult> GenerateAsync(AgentInteractionGenerationRequest request, string attemptId, CancellationToken ct)
    {
        // (1) Re-read the Source Conversation content fresh — Story 2.3 measured tokens then DISCARDED the raw text; only
        // counts are on the context evidence, so 2.4 must reload the content to build the model input. Fail closed →
        // InvalidContext.
        ConversationContextReadResult read = await ReadContextAsync(request, ct).ConfigureAwait(false);
        if (read.Outcome != AgentInteractionContextLoadOutcome.Loaded || read.Messages is null)
        {
            return FailedResult(AgentGenerationOutcome.InvalidContext, request, attemptId);
        }

        // (2) Read the model budget/timeout/status from the reused catalog. Disabled/not-text-gen → ProviderDisabled;
        // read failure / not-found / not-authorized → ProviderUnavailable.
        ProviderGenerationBudget budget = await ReadBudgetAsync(request, ct).ConfigureAwait(false);
        if (budget.Outcome != AgentGenerationOutcome.Succeeded)
        {
            return FailedResult(budget.Outcome, request, attemptId);
        }

        // (3) Invoke the provider behind its adapter, honoring the timeout/retry budget. A thrown adapter → AdapterFailure
        // (no raw provider error leaks; AD-14). A non-success provider outcome (timeout/error/…) maps straight through.
        AgentGenerationProviderResult providerResult = await InvokeProviderAsync(request, read.Messages, budget, attemptId, ct).ConfigureAwait(false);
        if (providerResult.Outcome != AgentGenerationOutcome.Succeeded || providerResult.GeneratedContent is null)
        {
            AgentGenerationOutcome outcome = providerResult.Outcome == AgentGenerationOutcome.Succeeded
                ? AgentGenerationOutcome.GenerationError // a "succeeded" result with no content is a degraded provider result
                : providerResult.Outcome;
            return FailedResult(outcome, request, attemptId, providerResult.PromptTokenCount, providerResult.OutputTokenCount);
        }

        // (4) Content-safety gate — BEFORE any downstream artifact (AC2). Read the effective policy (not-available →
        // PolicyFailure), then evaluate the generated content (Blocked → ContentSafetyBlocked, which the policy maps to
        // SafetyFailed and emits NO version — AD-5).
        AgentContentSafetyPolicyReadResult policy = await ReadPolicyAsync(request, ct).ConfigureAwait(false);
        if (!policy.IsAvailable || policy.Policy is null)
        {
            return FailedResult(AgentGenerationOutcome.PolicyFailure, request, attemptId, providerResult.PromptTokenCount, providerResult.OutputTokenCount);
        }

        ContentSafetyVerdict verdict = await EvaluateSafetyAsync(providerResult.GeneratedContent, policy.Policy, ct).ConfigureAwait(false);
        if (verdict != ContentSafetyVerdict.Passed)
        {
            // Fail closed and carry NO content — the unsafe content never reaches the aggregate (AD-5, AD-14).
            return FailedResult(AgentGenerationOutcome.ContentSafetyBlocked, request, attemptId, providerResult.PromptTokenCount, providerResult.OutputTokenCount);
        }

        // (5) Success — carry the generated content as the sole transport into the aggregate, whose success event becomes
        // its durable home (AD-14). Record the effective policy version the content passed.
        return new AgentOutputGenerationResult(
            AgentGenerationOutcome.Succeeded,
            attemptId,
            request.ProviderId,
            request.ModelId,
            request.ProviderCapabilityVersion,
            policy.PolicyVersion,
            providerResult.GeneratedContent,
            providerResult.PromptTokenCount,
            providerResult.OutputTokenCount);
    }

    private async Task<ConversationContextReadResult> ReadContextAsync(AgentInteractionGenerationRequest request, CancellationToken ct)
    {
        try
        {
            // The generation re-read keys on the actor security principal; authorization was already settled at the gate
            // (Story 2.2), so the auxiliary caller-party reference is not carried on the generation request.
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

    // Reads the model budget from the reused provider catalog. A missing/not-authorized/degraded read or a throw fails
    // closed to ProviderUnavailable; a disabled or non-text-capable entry fails closed to ProviderDisabled.
    private async Task<ProviderGenerationBudget> ReadBudgetAsync(AgentInteractionGenerationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderId) || string.IsNullOrWhiteSpace(request.ModelId))
        {
            return ProviderGenerationBudget.Failed(AgentGenerationOutcome.ProviderUnavailable);
        }

        try
        {
            ProviderCatalogEntryReadResult result = await _providerCatalogReader
                .GetEntryAsync(request.TenantId, request.ProviderId, request.ModelId, ct)
                .ConfigureAwait(false);

            if (result is not { Status: ProviderCatalogInspectionStatus.Success, Entry: { } entry })
            {
                return ProviderGenerationBudget.Failed(AgentGenerationOutcome.ProviderUnavailable);
            }

            if (entry.Status != ProviderModelStatus.Enabled || !entry.SupportsTextGeneration)
            {
                return ProviderGenerationBudget.Failed(AgentGenerationOutcome.ProviderDisabled);
            }

            return ProviderGenerationBudget.Ready(
                entry.MaxOutputTokenLimit,
                entry.TimeoutPolicy.RequestTimeoutMilliseconds,
                entry.TimeoutPolicy.MaxRetries);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ProviderGenerationBudget.Failed(AgentGenerationOutcome.ProviderUnavailable);
        }
    }

    private async Task<AgentGenerationProviderResult> InvokeProviderAsync(
        AgentInteractionGenerationRequest request,
        IReadOnlyList<ConversationContextMessage> messages,
        ProviderGenerationBudget budget,
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

    private async Task<AgentContentSafetyPolicyReadResult> ReadPolicyAsync(AgentInteractionGenerationRequest request, CancellationToken ct)
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

    // A content-free failure result — never carries generated content (AD-5, AD-14). Token counts are carried only when a
    // provider produced them before the failure (e.g. a safety block after a successful provider call).
    private static AgentOutputGenerationResult FailedResult(
        AgentGenerationOutcome outcome,
        AgentInteractionGenerationRequest request,
        string attemptId,
        int promptTokenCount = 0,
        int outputTokenCount = 0)
        => new(
            outcome,
            attemptId,
            request.ProviderId,
            request.ModelId,
            request.ProviderCapabilityVersion,
            request.ContentSafetyPolicyVersion,
            GeneratedContent: null,
            promptTokenCount,
            outputTokenCount);

    // The deterministic attempt id derived purely from the interaction id (AD-13) — no Guid/random/time, so retries reuse it.
    private static string DeriveAttemptId(string agentInteractionId) => $"attempt-{agentInteractionId}";

    // Copies the client-supplied extensions with the reserved trust keys removed; repopulates none (the generation path
    // carries no admin/verdict extension). Returns null when nothing benign remains so the envelope carries no empty map.
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

    // The safe model-budget/timeout read result. A non-Succeeded outcome short-circuits generation to a fail-closed result.
    private readonly record struct ProviderGenerationBudget(
        AgentGenerationOutcome Outcome,
        int MaxOutputTokenLimit,
        int RequestTimeoutMilliseconds,
        int MaxRetries)
    {
        public static ProviderGenerationBudget Ready(int maxOutputTokenLimit, int requestTimeoutMilliseconds, int maxRetries)
            => new(AgentGenerationOutcome.Succeeded, maxOutputTokenLimit, requestTimeoutMilliseconds, maxRetries);

        public static ProviderGenerationBudget Failed(AgentGenerationOutcome outcome)
            => new(outcome, 0, 0, 0);
    }
}
