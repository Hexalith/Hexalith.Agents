using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal outcome of the Response Mode configuration orchestration (Story 1.6 AC1). <see cref="Authorized"/>
/// is <see langword="false"/> when the actor was not an Agents admin (fail closed before any dispatch).
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether the configure command was dispatched.</param>
/// <param name="Receipt">The canonical EventStore receipt when dispatch completed.</param>
public sealed record AgentResponseModeOutcome(
    bool Authorized,
    bool Dispatched,
    SubmitCommandResponse? Receipt = null)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentResponseModeOutcome Denied() => new(Authorized: false, Dispatched: false);

    /// <summary>Creates the outcome for an authorized request whose configure command was dispatched.</summary>
    /// <param name="receipt">The canonical EventStore receipt.</param>
    /// <returns>The dispatched outcome.</returns>
    public static AgentResponseModeOutcome FromDispatch(SubmitCommandResponse receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new(Authorized: true, Dispatched: true, receipt);
    }
}
