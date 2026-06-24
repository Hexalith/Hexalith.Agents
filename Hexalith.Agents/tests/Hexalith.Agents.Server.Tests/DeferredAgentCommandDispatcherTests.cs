namespace Hexalith.Agents.Server.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

/// <summary>
/// Guard for the placeholder <see cref="DeferredAgentCommandDispatcher"/> (Story 1.4 deferred runtime binding).
/// The live DAPR/EventStore command-path binding is deferred (mirroring Story 1.2/1.3), so the registered seam must
/// fail loudly — never silently swallow a command — if it is invoked before the real dispatcher is wired. The
/// orchestration decision logic is exercised against a substituted seam; this locks the deferred placeholder's
/// fail-fast contract so a missing operational binding can never be mistaken for a successful dispatch.
/// </summary>
public sealed class DeferredAgentCommandDispatcherTests
{
    [Fact]
    public async Task Dispatch_throws_not_supported_until_the_live_binding_is_wired()
    {
        var dispatcher = new DeferredAgentCommandDispatcher();
        CommandEnvelope envelope = new(
            "msg-1",
            "acme",
            "agent",
            "hexa",
            "LinkAgentPartyIdentity",
            Array.Empty<byte>(),
            "corr-1",
            null,
            "admin-user",
            null);

        await Should.ThrowAsync<NotSupportedException>(
            async () => await dispatcher.DispatchAsync(envelope, CancellationToken.None));
    }
}
