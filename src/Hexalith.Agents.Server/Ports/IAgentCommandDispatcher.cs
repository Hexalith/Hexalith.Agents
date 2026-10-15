using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Dispatches an immutable command intent. The live implementation must complete private host admission before
/// constructing an EventStore target command; intent metadata alone grants no authority.
/// </summary>
public interface IAgentCommandDispatcher
{
    /// <summary>Admits the intent before constructing and dispatching its EventStore target command.</summary>
    /// <param name="intent">Exact immutable input awaiting host signature, replay and authority admission.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The canonical EventStore receipt for the completed submission.</returns>
    Task<SubmitCommandResponse> DispatchAsync(AgentCommandIntent intent, CancellationToken ct);
}
