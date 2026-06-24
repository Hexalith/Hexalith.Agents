using System;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, replay-safe aggregate for one tenant-scoped Agent Call (<c>AgentInteraction</c>) (AD-2, AD-3) — a distinct
/// boundary from <c>Agent</c> and <c>ProviderCatalog</c>. Story 2.1 implements the request-creation step only: it
/// records the AD-4 configuration snapshot frozen at request time and the caller's prompt, deduplicates re-issued
/// calls by their deterministic identity (AD-13), and rejects structurally-invalid requests. Authorization and
/// dependency readiness (Story 2.2), Conversation context (2.3), generation (2.4), and posting (2.5) attach to this
/// same aggregate later.
/// </summary>
/// <remarks>
/// <para>
/// <b>Pure aggregate, side effects outside (AD-3).</b> The single static
/// <c>Handle(command, state, envelope) -&gt; DomainResult</c> (discovered by the EventStore client by convention)
/// emits events only. The deterministic interaction id and the Agent configuration snapshot arrive pre-assembled in
/// the command/envelope from the Server request orchestration; the aggregate performs no provider call, no
/// Conversations/Parties/Tenants read, no Dapr, no HTTP, no logging/telemetry, no <c>DateTimeOffset.UtcNow</c>, and
/// no <c>Guid.NewGuid</c>. Request time is the EventStore event-metadata timestamp, server-stamped at persist.
/// </para>
/// <para>
/// <b>No ambient triggers, no side effects (AC3).</b> Only this explicit <see cref="RequestAgentInteraction"/>
/// command creates an interaction; Conversation state changes never do. <b>Sensitive content (AD-14):</b> the raw
/// prompt lives only on the durable <see cref="InteractionRequested"/> event and <c>AgentInteractionState</c> —
/// never on a rejection, the status view/reference, logs, telemetry, or audit summaries.
/// </para>
/// </remarks>
[EventStoreDomain("agent-interaction")]
public class AgentInteractionAggregate : EventStoreAggregate<AgentInteractionState>
{
    /// <summary>Handles creation (or idempotent re-creation) of the Agent Call request record (AC1, AC2, AC4).</summary>
    /// <param name="command">The request command (server-populated Agent id + AD-4 snapshot; caller prompt/references).</param>
    /// <param name="state">The current interaction state (null/never-requested before the first request).</param>
    /// <param name="envelope">The command envelope (carries the deterministic interaction id and the tenant scope).</param>
    /// <returns>The domain result (success event, typed rejection, or a deterministic no-op).</returns>
    public static DomainResult Handle(RequestAgentInteraction command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // Pure structural validation: required caller/source/prompt fields + a usable server-assembled snapshot
        // (AC1, AC4). A not-available snapshot (pre-activation Agent) surfaces here as MissingAgentSnapshot — never a
        // cross-aggregate read. The classification never echoes the prompt or any caller value (AD-14).
        AgentInteractionRequestValidationStatus? invalid = AgentInteractionRequestPolicy.Validate(
            command.AgentId,
            command.CallerPartyId,
            command.SourceConversationId,
            command.Prompt,
            command.Snapshot);
        if (invalid is { } status)
        {
            return DomainResult.Rejection([new InvalidAgentInteractionRequestRejection(interactionId, status)]);
        }

        // Idempotent duplicate handling (AD-13): re-issuing the same call on the same deterministic id is a no-op;
        // a conflicting payload on that id is rejected and never silently mutates the recorded request. Record
        // value-equality is not relied upon — the request and snapshot scalars are compared explicitly (ordinal).
        if (state is { IsRequested: true })
        {
            return RequestMatchesExisting(state, command)
                ? DomainResult.NoOp()
                : DomainResult.Rejection([new AgentInteractionAlreadyRequestedRejection(interactionId)]);
        }

        // Snapshot is non-null here (validation guaranteed a usable snapshot). The prompt is recorded on the durable
        // success event only (AD-14). No wall-clock field — request time is the EventStore event metadata (AD-3).
        return DomainResult.Success([
            new InteractionRequested(
                interactionId,
                command.AgentId,
                command.CallerPartyId,
                command.SourceConversationId,
                command.Snapshot!,
                command.Prompt,
                command.IdempotencyKey),
        ]);
    }

    /// <summary>
    /// Evaluates the invocation authorization + dependency-readiness gate from server-assembled verdicts and records the
    /// terminal decision (AC1–AC4; FR-20, FR-21; AD-3, AD-12, AD-13).
    /// </summary>
    /// <param name="command">The gate command carrying the server-assembled per-check verdicts (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be requested before the gate can run).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the authorized/failed outcome event, a not-evaluable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(EvaluateAgentInteractionGate command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: the gate can only evaluate an interaction whose request was recorded. A gate command on a
        // never-requested stream is a structural rejection (no state change), not a recorded decision (AD-12). The
        // positive pattern binds the non-null requested state so the gate logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // No verdicts means the gate has nothing to decide — fail closed as a structural rejection rather than
            // silently authorizing (a zero-blocker set must come from a real, populated evaluation, never an empty one).
            if (command.Verdicts is null or { Count: 0 })
            {
                return DomainResult.Rejection([
                    new AgentInteractionGateNotEvaluableRejection(interactionId, AgentInteractionGateNotEvaluableReason.NoVerdictsProvided),
                ]);
            }

            // Idempotent terminal gate (AD-13): the decision is recorded once and is terminal. A re-issued gate command
            // on an already-gated interaction is a clean no-op — the aggregate never silently flips a recorded decision.
            if (requested.Status is AgentInteractionStatus.Authorized or AgentInteractionStatus.Denied or AgentInteractionStatus.Blocked)
            {
                return DomainResult.NoOp();
            }

            // Pure evaluation (AD-3): emit the authorized/failed outcome only. The verdicts arrive pre-assembled from
            // trusted server reads (the gate orchestration); the aggregate's sole job is the blockers → decision math.
            return AgentInvocationGatePolicy.Evaluate(interactionId, command.Verdicts);
        }

        return DomainResult.Rejection([
            new AgentInteractionGateNotEvaluableRejection(interactionId, AgentInteractionGateNotEvaluableReason.InteractionNotRequested),
        ]);
    }

    /// <summary>
    /// Builds the Conversation context within safe bounds from the server-assembled measurement and records the terminal
    /// context decision (AC1–AC4; FR-9; AD-3, AD-11, AD-12, AD-13).
    /// </summary>
    /// <param name="command">The context command carrying the server-assembled measurement (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be Authorized before context can be built).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the context-ready/blocked outcome event, a not-buildable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(BuildAgentInteractionContext command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: context can only be built on a recorded interaction. A context command on a never-requested
        // stream is a structural rejection (no state change), not a recorded decision (AD-12). The positive pattern binds
        // the non-null requested state so the context logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // Idempotent terminal context (AD-13): the decision is recorded once and is terminal. A re-issued context
            // command on an already-decided interaction is a clean no-op — the aggregate never silently flips a recorded
            // ContextReady/ContextBlocked decision.
            if (requested.Status is AgentInteractionStatus.ContextReady or AgentInteractionStatus.ContextBlocked)
            {
                return DomainResult.NoOp();
            }

            // Authorization precondition (AD-11): context must never be built on a call that has not cleared the gate. A
            // Requested/Denied/Blocked interaction is a structural rejection (no state change) — distinct from a recorded
            // context-blocked decision, which only ever follows an Authorized interaction.
            if (requested.Status != AgentInteractionStatus.Authorized)
            {
                return DomainResult.Rejection([
                    new AgentInteractionContextNotBuildableRejection(interactionId, AgentInteractionContextNotBuildableReason.InteractionNotAuthorized),
                ]);
            }

            // Pure evaluation (AD-3): emit the context-ready/blocked outcome only. The measurement arrives pre-assembled
            // from trusted server reads (the context orchestration); the aggregate's sole job is the budget → decision math.
            return AgentInteractionContextPolicy.Evaluate(interactionId, command.Measurement);
        }

        return DomainResult.Rejection([
            new AgentInteractionContextNotBuildableRejection(interactionId, AgentInteractionContextNotBuildableReason.InteractionNotRequested),
        ]);
    }

    /// <summary>
    /// Records the terminal generation decision from the server-assembled outcome (AC1–AC4; FR-9, FR-10, FR-12; AD-3,
    /// AD-5, AD-9, AD-13). The orchestrator performs the impure provider invocation + content-safety gate and returns the
    /// outcome through <see cref="GenerateAgentOutput.Result"/>; the aggregate's sole job is the outcome → event math.
    /// </summary>
    /// <param name="command">The generate command carrying the server-assembled generation outcome (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be ContextReady before output can be generated).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the generated/failed outcome event, a not-generatable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(GenerateAgentOutput command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: output can only be generated on a recorded interaction. A generate command on a
        // never-requested stream is a structural rejection (no state change), not a recorded decision (AD-12). The
        // positive pattern binds the non-null requested state so the generation logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // Idempotent terminal generation (AD-13): the decision is recorded once and is terminal. Re-dispatching a
            // generate command after a terminal Generated/GenerationFailed/SafetyFailed outcome is a clean no-op that
            // preserves version history — the aggregate never silently flips a recorded decision or appends a duplicate
            // version (AC4).
            if (requested.Status is AgentInteractionStatus.Generated or AgentInteractionStatus.GenerationFailed or AgentInteractionStatus.SafetyFailed)
            {
                return DomainResult.NoOp();
            }

            // Context-ready precondition (AD-11): generation must never run before Conversation context is built within
            // safe bounds. Any other status (Requested/Authorized/Denied/Blocked/ContextBlocked) is a structural rejection
            // (no state change) — distinct from a recorded generation-failed decision, which only ever follows ContextReady.
            if (requested.Status != AgentInteractionStatus.ContextReady)
            {
                return DomainResult.Rejection([
                    new AgentOutputNotGeneratableRejection(interactionId, AgentOutputNotGeneratableReason.ContextNotReady),
                ]);
            }

            // Pure evaluation (AD-3): emit the generated/failed outcome only. The result arrives pre-assembled from the
            // trusted generation orchestration; the aggregate's sole job is the outcome → event/status math.
            return AgentOutputGenerationPolicy.Evaluate(interactionId, command.Result);
        }

        return DomainResult.Rejection([
            new AgentOutputNotGeneratableRejection(interactionId, AgentOutputNotGeneratableReason.InteractionNotRequested),
        ]);
    }

    /// <summary>
    /// Records the terminal automatic-posting decision from the server-assembled outcome (AC1–AC4; FR-11, FR-12; AD-3,
    /// AD-6, AD-7, AD-13, AD-14). The orchestrator performs the impure Agent-Party read + selected-version read +
    /// Conversations membership ensure + message append and returns the outcome through <see cref="PostAgentResponse.Result"/>;
    /// the aggregate's sole job is the outcome → event math.
    /// </summary>
    /// <param name="command">The post command carrying the server-assembled posting outcome (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be Generated + Automatic mode before output can be posted).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the posted/failed outcome event, a not-postable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(PostAgentResponse command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: output can only be posted on a recorded interaction. A post command on a never-requested
        // stream is a structural rejection (no state change), not a recorded decision (AD-12). The positive pattern binds
        // the non-null requested state so the posting logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // Idempotent terminal posting (AD-13): the decision is recorded once and is terminal. Re-dispatching a post
            // command after a terminal Posted/PostingFailed outcome is a clean no-op that preserves the recorded outcome —
            // the aggregate never silently flips a recorded decision or appends a duplicate Conversation Message (AC3).
            if (requested.Status is AgentInteractionStatus.Posted or AgentInteractionStatus.PostingFailed)
            {
                return DomainResult.NoOp();
            }

            // Response-mode precondition (AD-7): automatic posting must never run for a Confirmation-mode interaction —
            // that path posts via Epic 3 approval (Story 3.5). A Confirmation-mode post command is a structural rejection
            // (no state change), distinct from a recorded posting-failed decision.
            if (requested.Snapshot?.ResponseMode != AgentResponseMode.Automatic)
            {
                return DomainResult.Rejection([
                    new AgentResponseNotPostableRejection(interactionId, AgentResponseNotPostableReason.NotAutomaticResponseMode),
                ]);
            }

            // Generation precondition (AD-12): posting must never run before output is generated within safe bounds. Any
            // other status (Requested/Authorized/Denied/Blocked/ContextReady/ContextBlocked/GenerationFailed/SafetyFailed)
            // is a structural rejection (no state change) — distinct from a recorded posting-failed decision, which only
            // ever follows Generated.
            if (requested.Status != AgentInteractionStatus.Generated)
            {
                return DomainResult.Rejection([
                    new AgentResponseNotPostableRejection(interactionId, AgentResponseNotPostableReason.OutputNotGenerated),
                ]);
            }

            // Pure evaluation (AD-3): emit the posted/failed outcome only. The result arrives pre-assembled from the
            // trusted posting orchestration; the aggregate's sole job is the outcome → event/status math.
            return AgentResponsePostingPolicy.Evaluate(interactionId, command.Result);
        }

        return DomainResult.Rejection([
            new AgentResponseNotPostableRejection(interactionId, AgentResponseNotPostableReason.InteractionNotRequested),
        ]);
    }

    /// <summary>
    /// Records the terminal proposal-creation decision from the server-assembled outcome (AC1–AC4; FR-13, FR-14, FR-27;
    /// AD-3, AD-5, AD-6, AD-13, AD-14). The orchestrator performs the impure selected-version read + optional expiry read and
    /// returns the outcome through <see cref="CreateProposedAgentReply.Result"/>; the aggregate's sole job is the outcome →
    /// event math. This is the Confirmation-mode counterpart to <see cref="Handle(PostAgentResponse, AgentInteractionState?, CommandEnvelope)"/>:
    /// the response-mode precondition is the exact inverse (it requires <see cref="AgentResponseMode.Confirmation"/>).
    /// </summary>
    /// <param name="command">The create command carrying the server-assembled proposal-creation outcome (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must be Generated + Confirmation mode before a proposal can be created).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the created/failed outcome event, a not-creatable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(CreateProposedAgentReply command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: a proposal can only be created on a recorded interaction. A create command on a never-requested
        // stream is a structural rejection (no state change), not a recorded decision (AD-12). The positive pattern binds the
        // non-null requested state so the creation logic runs only over a recorded interaction.
        if (state is { IsRequested: true } requested)
        {
            // Idempotent terminal creation (AD-13): the decision is recorded once and is terminal. Re-dispatching a create
            // command after a terminal ProposalCreated/ProposalCreationFailed outcome is a clean no-op that preserves the
            // recorded decision — the aggregate never silently flips a decision or creates a duplicate proposal/version (AC4).
            if (requested.Status is AgentInteractionStatus.ProposalCreated or AgentInteractionStatus.ProposalCreationFailed)
            {
                return DomainResult.NoOp();
            }

            // Response-mode precondition (the Confirmation counterpart to PostAgentResponse's Automatic check): proposal
            // creation must never run for an Automatic-mode interaction — that path posts via Story 2.5, never creates a
            // proposal. An Automatic-mode create command is a structural rejection (no state change).
            if (requested.Snapshot?.ResponseMode != AgentResponseMode.Confirmation)
            {
                return DomainResult.Rejection([
                    new ProposedAgentReplyNotCreatableRejection(interactionId, AgentProposedReplyNotCreatableReason.NotConfirmationResponseMode),
                ]);
            }

            // Generation precondition (AD-12): proposal creation only ever follows a successful, safety-passing generation.
            // Any other status (Requested/Authorized/Denied/Blocked/ContextReady/ContextBlocked/GenerationFailed/SafetyFailed)
            // is a structural rejection (no state change). This is the structural enforcement of AC3: a SafetyFailed/
            // GenerationFailed interaction never reaches proposal creation — there is no generated version to propose (AD-5).
            if (requested.Status != AgentInteractionStatus.Generated)
            {
                return DomainResult.Rejection([
                    new ProposedAgentReplyNotCreatableRejection(interactionId, AgentProposedReplyNotCreatableReason.OutputNotGenerated),
                ]);
            }

            // Pure evaluation (AD-3): emit the created/failed outcome only. The result arrives pre-assembled from the trusted
            // proposal orchestration; the aggregate's sole job is the outcome → event/status math.
            return AgentProposalCreationPolicy.Evaluate(interactionId, command.Result);
        }

        return DomainResult.Rejection([
            new ProposedAgentReplyNotCreatableRejection(interactionId, AgentProposedReplyNotCreatableReason.InteractionNotRequested),
        ]);
    }

    /// <summary>
    /// Records the terminal proposal-edit decision from the server-assembled outcome (AC1, AC2, AC4; FR-14, FR-15; AD-3,
    /// AD-5, AD-13, AD-14). The orchestrator resolves edit-time approver authorization + derives the deterministic edited
    /// version id and returns the outcome through <see cref="EditProposedAgentReply.Result"/>; the aggregate's sole job is
    /// to validate that a pending proposal exists (AC2) and do the outcome → event math. This is the seventh handler on the
    /// aggregate; it builds directly on the Story 3.1 proposal created by <see cref="Handle(CreateProposedAgentReply, AgentInteractionState?, CommandEnvelope)"/>.
    /// </summary>
    /// <param name="command">The edit command carrying the server-assembled proposal-edit outcome (client values discarded upstream).</param>
    /// <param name="state">The current interaction state (must hold a pending/edited proposal before it can be edited).</param>
    /// <param name="envelope">The command envelope (carries the interaction id and the tenant scope).</param>
    /// <returns>The domain result (the edited/failed outcome event, a not-editable rejection, or an idempotent no-op).</returns>
    public static DomainResult Handle(EditProposedAgentReply command, AgentInteractionState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        string interactionId = envelope.AggregateId;

        // State precondition: a proposal can only be edited on a recorded interaction. An edit command on a never-requested
        // stream is a structural rejection (no state change), not a recorded decision (AD-12). The positive pattern binds the
        // non-null requested state so the edit logic runs only over a recorded interaction (CA1062 idiom).
        if (state is { IsRequested: true } requested)
        {
            // Idempotent edited version (AD-13): a retried edit command carries the same deterministic edited version id, so
            // if that version is already on the append-only history the edit already landed — a clean no-op that never
            // appends a duplicate version. Keyed on the version id (NOT the status) because a proposal may be edited more
            // than once, each distinct edit appending a new version (AC4).
            if (EditAlreadyLanded(requested, command.Result.EditedVersion.VersionId))
            {
                return DomainResult.NoOp();
            }

            // Precondition (AC2): there must be a pending/edited proposal to edit. The proposal exists once creation
            // succeeded (ProposalCreated) or after a prior edit (ProposalEdited), AND its sub-state must be in the editable
            // set { Pending, Edited }. A terminal proposal (approved/rejected/abandoned/expired/posted — Stories 3.5/3.6) or
            // an interaction with no pending proposal is ProposalNotPending → reject, no new version. This is the structural
            // enforcement of AC2: a terminal proposal can never be edited because it can no longer post.
            bool proposalExists = requested.Status is AgentInteractionStatus.ProposalCreated or AgentInteractionStatus.ProposalEdited;
            bool editable = requested.ProposalState is ProposedAgentReplyState.Pending or ProposedAgentReplyState.Edited;
            if (!proposalExists || !editable)
            {
                return DomainResult.Rejection([
                    new ProposedAgentReplyNotEditableRejection(interactionId, AgentProposedReplyNotEditableReason.ProposalNotPending),
                ]);
            }

            // Pure evaluation (AD-3): emit the edited/failed outcome only. The result arrives pre-assembled from the trusted
            // edit orchestration (authorization resolved + version id derived); the aggregate's sole job is the outcome →
            // event/status math.
            return AgentProposalEditPolicy.Evaluate(interactionId, command.Result);
        }

        return DomainResult.Rejection([
            new ProposedAgentReplyNotEditableRejection(interactionId, AgentProposedReplyNotEditableReason.InteractionNotProposed),
        ]);
    }

    // True when the deterministic edited version id is already on the append-only version history — a retried edit that
    // already landed (AD-13). The edited version id uses a distinct SHA-256 purpose tag, so it never collides with a
    // generated version id; checking the whole history is safe.
    private static bool EditAlreadyLanded(AgentInteractionState state, string editedVersionId)
    {
        if (string.IsNullOrEmpty(editedVersionId) || state.GeneratedVersions is null)
        {
            return false;
        }

        foreach (AgentGeneratedVersion version in state.GeneratedVersions)
        {
            if (string.Equals(version.VersionId, editedVersionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // By-value comparison of a re-issued request against the recorded one (AD-13). Strings are compared ordinally;
    // the snapshot scalars are compared explicitly so a re-derived-id collision with a different configuration is
    // surfaced as a conflict rather than a silent no-op.
    private static bool RequestMatchesExisting(AgentInteractionState existing, RequestAgentInteraction command)
        => string.Equals(existing.AgentId, command.AgentId, StringComparison.Ordinal)
            && string.Equals(existing.CallerPartyId, command.CallerPartyId, StringComparison.Ordinal)
            && string.Equals(existing.SourceConversationId, command.SourceConversationId, StringComparison.Ordinal)
            && string.Equals(existing.Prompt, command.Prompt, StringComparison.Ordinal)
            && string.Equals(existing.IdempotencyKey, command.IdempotencyKey, StringComparison.Ordinal)
            && SnapshotsEqual(existing.Snapshot, command.Snapshot);

    private static bool SnapshotsEqual(AgentInteractionSnapshot? a, AgentInteractionSnapshot? b)
    {
        if (a is null || b is null)
        {
            return a is null && b is null;
        }

        return a.ConfigurationVersion == b.ConfigurationVersion
            && a.InstructionsVersion == b.InstructionsVersion
            && a.ResponseMode == b.ResponseMode
            && a.ApproverPolicyVersion == b.ApproverPolicyVersion
            && string.Equals(a.ProviderId, b.ProviderId, StringComparison.Ordinal)
            && string.Equals(a.ModelId, b.ModelId, StringComparison.Ordinal)
            && a.ProviderCapabilityVersion == b.ProviderCapabilityVersion
            && a.ContentSafetyPolicyVersion == b.ContentSafetyPolicyVersion
            && string.Equals(a.ContextPolicyReference, b.ContextPolicyReference, StringComparison.Ordinal);
    }
}
