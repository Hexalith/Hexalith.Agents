using Hexalith.EventStore.Client.Events;

using Microsoft.Extensions.Hosting;

namespace Hexalith.EventStore.Client.Registration;

/// <summary>Forces event-evolution validation before a reader host begins serving requests.</summary>
internal sealed class EventPayloadEvolutionStartupValidator(EventPayloadEvolutionRegistry registry) : IHostedService {
    public Task StartAsync(CancellationToken cancellationToken) {
        _ = registry;
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
