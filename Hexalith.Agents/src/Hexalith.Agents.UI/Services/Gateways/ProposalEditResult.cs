namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Outcome of submitting an authorized edit of a pending Proposed Agent Reply (Story 3.3; AC1). The edit seam returns a
/// structured status rather than throwing; only <see cref="Edited"/> carries the safe edited-version id, while
/// <see cref="NotAuthorized"/>/<see cref="Unavailable"/> carry none, so a failed/denied edit never reveals version
/// identity (AD-12, AD-14).
/// </summary>
public enum ProposalEditStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The edit was accepted; a new immutable edited version was appended (prior versions preserved).</summary>
    Edited,

    /// <summary>The caller is not an authorized Approver for this proposal; no edit was made (AD-12 fail-closed).</summary>
    NotAuthorized,

    /// <summary>The edit seam faulted / a dependency is unreachable; no edit was made (the UI error surface).</summary>
    Unavailable,
}

/// <summary>
/// Structured result of submitting an authorized proposal edit (Story 3.3; AC1). On <see cref="ProposalEditStatus.Edited"/>
/// the <see cref="EditedVersionId"/> is the safe id of the new immutable edited version; on every fail-closed outcome it is
/// <see langword="null"/>, so a failed/denied edit never reveals version identity (AD-12, AD-14). It is never the edited
/// content. Mirrors <see cref="Hexalith.Agents.Contracts.AgentInteraction.AgentCallRequestResult"/>.
/// </summary>
/// <param name="Status">The edit outcome.</param>
/// <param name="EditedVersionId">The safe edited version id (non-null only when <see cref="Status"/> is <see cref="ProposalEditStatus.Edited"/>).</param>
public record ProposalEditResult(
    ProposalEditStatus Status,
    string? EditedVersionId)
{
    /// <summary>Creates an edited result carrying the safe edited-version id.</summary>
    /// <param name="editedVersionId">The safe edited version id.</param>
    /// <returns>An edited result.</returns>
    public static ProposalEditResult Edited(string editedVersionId)
        => new(ProposalEditStatus.Edited, editedVersionId);

    /// <summary>Creates a not-authorized result with no version id (AD-12 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static ProposalEditResult NotAuthorized()
        => new(ProposalEditStatus.NotAuthorized, null);

    /// <summary>Creates an unavailable result with no version id (the UI error surface).</summary>
    /// <returns>An unavailable result.</returns>
    public static ProposalEditResult Unavailable()
        => new(ProposalEditStatus.Unavailable, null);
}
