using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// Structured result of an Agent setup write (Story 5.2 AC1). Submitted, already-applied, and awaiting-projection
/// outcomes carry the acceptance evidence a retry and a catch-up read need; denied and unverifiable outcomes carry
/// none.
/// </summary>
/// <remarks>
/// These factories record an outcome; they do not establish one. Verifying that the acceptance actually carries a
/// known <see cref="AgentSetupWriteEffect"/> and a target version is the gateway's job, and a gateway that cannot
/// must report <see cref="AgentSetupWriteStatus.UnableToVerify"/> rather than construct a success-like result.
/// </remarks>
/// <param name="Status">The write outcome.</param>
/// <param name="Acceptance">The structured accepted identity and exact target version, when verified.</param>
public record AgentSetupWriteResult(
    AgentSetupWriteStatus Status,
    AgentCommandAcceptance? Acceptance)
{
    /// <summary>Gets the truth stage this write has reached (never a projection claim).</summary>
    public AgentSetupTruthState TruthState
        => Acceptance?.TruthState ?? AgentSetupTruthState.Unknown;

    /// <summary>Gets the safe command effect, or <see cref="AgentSetupWriteEffect.Unknown"/> when none was verified.</summary>
    public AgentSetupWriteEffect Effect
        => Acceptance?.Effect ?? AgentSetupWriteEffect.Unknown;

    /// <summary>Gets the exact command-derived configuration version the projection must reach.</summary>
    public int? TargetConfigurationVersion
        => Acceptance?.TargetConfigurationVersion;

    /// <summary>Creates an accepted result carrying the structured accepted identity.</summary>
    /// <param name="acceptance">The accepted identity.</param>
    /// <returns>
    /// An <see cref="AgentSetupWriteStatus.AlreadyApplied"/> result when the acceptance reports a no-op, otherwise
    /// an <see cref="AgentSetupWriteStatus.Submitted"/> result.
    /// </returns>
    public static AgentSetupWriteResult Submitted(AgentCommandAcceptance acceptance)
    {
        ArgumentNullException.ThrowIfNull(acceptance);
        return new(
            acceptance.Effect == AgentSetupWriteEffect.AlreadyApplied
                ? AgentSetupWriteStatus.AlreadyApplied
                : AgentSetupWriteStatus.Submitted,
            acceptance);
    }

    /// <summary>Creates an awaiting-projection result while preserving the verified command acceptance.</summary>
    /// <param name="acceptance">The accepted identity and exact target version.</param>
    /// <returns>An awaiting-projection result.</returns>
    public static AgentSetupWriteResult AwaitingProjection(AgentCommandAcceptance acceptance)
    {
        ArgumentNullException.ThrowIfNull(acceptance);
        return new(AgentSetupWriteStatus.AwaitingProjection, acceptance);
    }

    /// <summary>Creates a fail-closed result with no acceptance.</summary>
    /// <param name="status">The denied or unverifiable outcome, which carries no acceptance evidence.</param>
    /// <returns>A failed result.</returns>
    public static AgentSetupWriteResult Failed(AgentSetupWriteStatus status)
        => new(status, null);
}
