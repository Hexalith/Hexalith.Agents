using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal outcome of an Agent administration mutation (Story 5.2 AC1). <see cref="Authorized"/> is
/// <see langword="false"/> when the actor failed the Agents-admin gate, in which case nothing was dispatched.
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether the command was dispatched.</param>
/// <param name="Receipt">The canonical EventStore receipt when dispatch completed.</param>
public sealed record AgentAdministrationOutcome(
    bool Authorized,
    bool Dispatched,
    SubmitCommandResponse? Receipt = null)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentAdministrationOutcome Denied() => new(Authorized: false, Dispatched: false);

    /// <summary>Creates the outcome for an authorized request whose command was dispatched.</summary>
    /// <param name="receipt">The canonical EventStore receipt.</param>
    /// <returns>The dispatched outcome.</returns>
    public static AgentAdministrationOutcome FromDispatch(SubmitCommandResponse receipt)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        return new(Authorized: true, Dispatched: true, receipt);
    }
}
