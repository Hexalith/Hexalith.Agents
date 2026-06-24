using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of reading a governed Agent's current configuration as an AD-4 snapshot for the Agent
/// Call request orchestration (Story 2.1; AC1; AD-3, AD-4). It carries the fail-closed availability flag plus the
/// safe <see cref="AgentInteractionSnapshot"/> (present only when the Agent has passed activation). No secret value,
/// provider SDK type, prompt, or Conversation content ever crosses this boundary — only the safe snapshot scalars
/// (AD-9, AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>ProviderCatalogEntryReadResult</c> /
/// <c>ApproverPolicyResolutionResult</c>). A <see cref="NotAvailable"/> result models the pre-activation case (the
/// AC1 "active in setup readiness" precondition not met): the orchestration dispatches a request with a null
/// snapshot, which the pure aggregate rejects as a structural
/// <see cref="AgentInteractionRequestValidationStatus.MissingAgentSnapshot"/> — never a re-implemented activation
/// gate.
/// </remarks>
/// <param name="IsAvailable">Whether a populated snapshot is available (the Agent has passed activation).</param>
/// <param name="Snapshot">The safe AD-4 snapshot (non-null only when <paramref name="IsAvailable"/> is set), or <see langword="null"/>.</param>
public sealed record AgentConfigurationSnapshot(bool IsAvailable, AgentInteractionSnapshot? Snapshot)
{
    /// <summary>Gets the fail-closed not-available result (no snapshot) for a pre-activation/unreadable Agent.</summary>
    public static AgentConfigurationSnapshot NotAvailable { get; } = new(false, null);

    /// <summary>Creates an available result carrying the populated snapshot.</summary>
    /// <param name="snapshot">The populated AD-4 snapshot.</param>
    /// <returns>The available read result.</returns>
    public static AgentConfigurationSnapshot Available(AgentInteractionSnapshot snapshot) => new(true, snapshot);
}
