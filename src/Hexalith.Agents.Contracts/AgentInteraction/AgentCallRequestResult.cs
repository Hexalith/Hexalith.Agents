namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Structured result of submitting a normalized Conversation-originated Agent Call request (AC1, AC3). On
/// <see cref="AgentCallRequestStatus.Accepted"/> the <see cref="Reference"/> (the safe <c>AgentInteractionId</c> + coarse
/// <c>Status</c>) is the handle the feedback UI polls for live call status; on
/// <see cref="AgentCallRequestStatus.NotAuthorized"/>/<see cref="AgentCallRequestStatus.Rejected"/> it is
/// <see langword="null"/>, so a failed request never reveals interaction identity (AD-12, AD-14). The reference is never
/// the prompt or generated content. Mirrors <see cref="Agent.AgentInspectionResult"/>.
/// </summary>
/// <param name="Status">The request outcome.</param>
/// <param name="Reference">The safe Agent Call reference (non-null only when <see cref="Status"/> is <see cref="AgentCallRequestStatus.Accepted"/>).</param>
public record AgentCallRequestResult(
    AgentCallRequestStatus Status,
    AgentInteractionReference? Reference)
{
    /// <summary>Creates an accepted result carrying the safe Agent Call reference.</summary>
    /// <param name="reference">The safe Agent Call reference handle.</param>
    /// <returns>An accepted result.</returns>
    public static AgentCallRequestResult Accepted(AgentInteractionReference reference)
        => new(AgentCallRequestStatus.Accepted, reference);

    /// <summary>Creates a not-authorized result with no reference (AD-12 fail-closed).</summary>
    /// <returns>A not-authorized result.</returns>
    public static AgentCallRequestResult NotAuthorized()
        => new(AgentCallRequestStatus.NotAuthorized, null);

    /// <summary>Creates a rejected result with no reference.</summary>
    /// <returns>A rejected result.</returns>
    public static AgentCallRequestResult Rejected()
        => new(AgentCallRequestStatus.Rejected, null);
}
