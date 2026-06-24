namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Outcome of submitting an authorized regeneration of a pending Proposed Agent Reply (Story 3.4; AC1, AC3, AC4). The
/// regeneration seam returns a structured status rather than throwing; only <see cref="Regenerated"/> carries the safe
/// regenerated-version id, while <see cref="NotAuthorized"/>/<see cref="Unavailable"/>/<see cref="NotPending"/> carry none,
/// so a failed/denied regeneration never reveals version identity (AD-12, AD-14).
/// </summary>
public enum ProposalRegenerationStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The regeneration succeeded; a new immutable regenerated version was appended (prior versions preserved).</summary>
    Regenerated,

    /// <summary>The caller is not an authorized Approver for this proposal; no regeneration was made (AD-12 fail-closed).</summary>
    NotAuthorized,

    /// <summary>The regeneration seam faulted / a dependency is unreachable, or the provider/safety gate failed closed; no version was added (the UI error surface).</summary>
    Unavailable,

    /// <summary>The proposal is no longer pending (terminal), so it cannot be regenerated and no provider was invoked (AC4 fail-closed).</summary>
    NotPending,
}

/// <summary>
/// Structured result of submitting an authorized proposal regeneration (Story 3.4; AC1, AC3, AC4). On
/// <see cref="ProposalRegenerationStatus.Regenerated"/> the <see cref="RegeneratedVersionId"/> is the safe id of the new
/// immutable regenerated version; on every fail-closed outcome it is <see langword="null"/>, so a failed/denied regeneration
/// never reveals version identity (AD-12, AD-14). It is never the regenerated content. Mirrors <see cref="ProposalEditResult"/>.
/// </summary>
/// <param name="Status">The regeneration outcome.</param>
/// <param name="RegeneratedVersionId">The safe regenerated version id (non-null only when <see cref="Status"/> is <see cref="ProposalRegenerationStatus.Regenerated"/>).</param>
public record ProposalRegenerationResult(
    ProposalRegenerationStatus Status,
    string? RegeneratedVersionId)
{
    /// <summary>Creates a regenerated result carrying the safe regenerated-version id.</summary>
    /// <param name="regeneratedVersionId">The safe regenerated version id.</param>
    /// <returns>A regenerated result.</returns>
    public static ProposalRegenerationResult Regenerated(string regeneratedVersionId)
        => new(ProposalRegenerationStatus.Regenerated, regeneratedVersionId);

    /// <summary>Creates a not-authorized result with no version id (AD-12 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static ProposalRegenerationResult NotAuthorized()
        => new(ProposalRegenerationStatus.NotAuthorized, null);

    /// <summary>Creates an unavailable result with no version id (the UI error surface).</summary>
    /// <returns>An unavailable result.</returns>
    public static ProposalRegenerationResult Unavailable()
        => new(ProposalRegenerationStatus.Unavailable, null);

    /// <summary>Creates a not-pending result with no version id (the terminal-proposal fail-closed surface; AC4).</summary>
    /// <returns>A not-pending result.</returns>
    public static ProposalRegenerationResult NotPending()
        => new(ProposalRegenerationStatus.NotPending, null);
}
