using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure, dependency-free structural-validation rules for an Agent Call request, used by
/// <see cref="AgentInteractionAggregate"/> (AC1, AC4; FR-8). Centralizing the required-field and snapshot-presence
/// checks keeps the request-creation guard in one place, mirroring <c>AgentConfigurationPolicy</c>.
/// </summary>
/// <remarks>
/// No method here reads I/O, time, secrets, or any sibling aggregate (AD-3). The snapshot check is a pure
/// null/empty-scalar guard on the command payload — never a cross-aggregate read of the <c>Agent</c> aggregate: a
/// not-available snapshot is the request orchestration's responsibility (the reader returns a populated snapshot
/// only for an activated Agent), and surfaces here as a structural
/// <see cref="AgentInteractionRequestValidationStatus.MissingAgentSnapshot"/>. No method echoes the raw prompt or
/// any Conversation-derived content (AD-14).
/// </remarks>
internal static class AgentInteractionRequestPolicy
{
    /// <summary>
    /// The stable V1 default Conversation Context Policy reference. Re-exported from
    /// <see cref="AgentInteractionSnapshot.DefaultContextPolicyReference"/> so the domain, the request orchestration,
    /// and their tests reference a single source and cannot drift (FR-9). Story 2.3 binds the concrete policy.
    /// </summary>
    internal const string DefaultContextPolicyReference = AgentInteractionSnapshot.DefaultContextPolicyReference;

    /// <summary>
    /// Validates an Agent Call request's required fields and snapshot presence in deterministic order (caller →
    /// source → prompt → snapshot). Returns the first failing classification, or <see langword="null"/> when the
    /// request is structurally valid. Never echoes a field value (AD-14).
    /// </summary>
    /// <param name="agentId">The target Agent identifier (server-populated; absent ⇒ no resolvable snapshot).</param>
    /// <param name="callerPartyId">The caller Party reference.</param>
    /// <param name="sourceConversationId">The source Conversation reference.</param>
    /// <param name="prompt">The caller prompt.</param>
    /// <param name="snapshot">The server-assembled AD-4 configuration snapshot.</param>
    /// <returns>The first failing validation classification, or <see langword="null"/> when the request is valid.</returns>
    internal static AgentInteractionRequestValidationStatus? Validate(
        string agentId,
        string callerPartyId,
        string sourceConversationId,
        string prompt,
        AgentInteractionSnapshot? snapshot)
    {
        if (string.IsNullOrWhiteSpace(callerPartyId))
        {
            return AgentInteractionRequestValidationStatus.MissingCaller;
        }

        if (string.IsNullOrWhiteSpace(sourceConversationId))
        {
            return AgentInteractionRequestValidationStatus.MissingSourceConversation;
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return AgentInteractionRequestValidationStatus.MissingPrompt;
        }

        return HasUsableSnapshot(agentId, snapshot)
            ? null
            : AgentInteractionRequestValidationStatus.MissingAgentSnapshot;
    }

    /// <summary>
    /// Whether the request carries a usable AD-4 snapshot: a target Agent identity plus the required safe scalar
    /// references (provider id, model id, context-policy reference). A null/empty snapshot — the reader's
    /// not-available result for a pre-activation Agent — fails this guard (AC1 precondition). A pure payload check;
    /// it performs no cross-aggregate read.
    /// </summary>
    /// <param name="agentId">The target Agent identifier.</param>
    /// <param name="snapshot">The server-assembled snapshot.</param>
    /// <returns><see langword="true"/> when the snapshot is present and its required scalars are populated.</returns>
    internal static bool HasUsableSnapshot(string agentId, AgentInteractionSnapshot? snapshot)
        => !string.IsNullOrWhiteSpace(agentId)
            && snapshot is not null
            && !string.IsNullOrWhiteSpace(snapshot.ProviderId)
            && !string.IsNullOrWhiteSpace(snapshot.ModelId)
            && !string.IsNullOrWhiteSpace(snapshot.ContextPolicyReference);
}
