using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentCommandStatusReader"/> registered so the Server DI graph stays complete while the
/// live EventStore binding is deferred, mirroring <see cref="DeferredAgentCommandDispatcher"/>. Unlike the deferred
/// dispatcher it does not throw: a status read is supplementary evidence on an already-failing path, so the safe
/// deferred answer is "unknown", which leaves the outcome unverifiable instead of inventing a rejection.
/// </summary>
/// <remarks>
/// The live binding reads <c>GET api/v1/commands/status/{messageId}</c> through the EventStore gateway client. It is
/// not wired yet because <c>IEventStoreGatewayClient.GetCommandStatusAsync</c> is added but unreleased: Release
/// builds resolve Hexalith libraries by package reference, so Agents cannot bind to it until that package ships.
/// Everything downstream of this seam -- the rejection mapping and its tests -- is already in place, so going live
/// is a single registration change.
/// </remarks>
public sealed class DeferredAgentCommandStatusReader : IAgentCommandStatusReader
{
    /// <inheritdoc />
    public Task<bool?> WasRejectedAsync(string messageId, CancellationToken ct) => Task.FromResult<bool?>(null);
}
