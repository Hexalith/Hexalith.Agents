using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Outcome of an Agent setup write attempted from an administration surface (Story 5.2 AC1, AC3, AC4).
/// </summary>
public enum AgentSetupWriteStatus
{
    /// <summary>The write was accepted for processing; the durable truth is not yet confirmed.</summary>
    Submitted = 0,

    /// <summary>The caller is not an authorized Agent administrator for the tenant; nothing was mutated.</summary>
    NotAuthorized,

    /// <summary>No Agent exists for the requested aggregate; nothing was mutated.</summary>
    NotFound,

    /// <summary>The submitted fields are invalid; prior state is preserved.</summary>
    ValidationFailed,

    /// <summary>The write conflicted with a concurrent change; prior state is preserved.</summary>
    Conflict,

    /// <summary>The write path is not bound or is unreachable; nothing was mutated.</summary>
    Unavailable,
}

/// <summary>
/// Structured result of an Agent setup write (Story 5.2 AC1). A non-<see cref="AgentSetupWriteStatus.Submitted"/>
/// outcome carries no acceptance, so a denied or failed write can never be rendered as progress.
/// </summary>
/// <param name="Status">The write outcome.</param>
/// <param name="Acceptance">The structured accepted identity (non-null only on <see cref="AgentSetupWriteStatus.Submitted"/>).</param>
public record AgentSetupWriteResult(
    AgentSetupWriteStatus Status,
    AgentCommandAcceptance? Acceptance)
{
    /// <summary>Gets the truth stage this write has reached (never a projection claim).</summary>
    public AgentSetupTruthState TruthState
        => Acceptance?.TruthState ?? AgentSetupTruthState.Unknown;

    /// <summary>Creates a submitted result carrying the structured accepted identity.</summary>
    /// <param name="acceptance">The accepted identity.</param>
    /// <returns>A submitted result.</returns>
    public static AgentSetupWriteResult Submitted(AgentCommandAcceptance acceptance)
        => new(AgentSetupWriteStatus.Submitted, acceptance);

    /// <summary>Creates a fail-closed result with no acceptance.</summary>
    /// <param name="status">The non-submitted outcome.</param>
    /// <returns>A failed result.</returns>
    public static AgentSetupWriteResult Failed(AgentSetupWriteStatus status)
        => new(status, null);
}
