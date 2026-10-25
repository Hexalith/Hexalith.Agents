using Hexalith.EventStore.Client.Gateway;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Delegates only the status operation to the host issuer with one exact domain binding.</summary>
/// <param name="source">The independently configured host assertion source.</param>
/// <param name="domain">The exact authorized command domain for this read.</param>
internal sealed class CommandStatusWorkloadAssertionSource(
    IEventStoreGatewayWorkloadAssertionSource? source,
    string domain) : IEventStoreGatewayWorkloadAssertionSource
{
    /// <inheritdoc />
    public ValueTask<string?> IssueAsync(string operation, string tenant, CancellationToken cancellationToken)
        => IssueAsync(operation, tenant, domain, cancellationToken);

    /// <inheritdoc />
    public ValueTask<string?> IssueAsync(string operation, string tenant, string requestedDomain, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
#if HEXALITH_EVENTSTORE_DOMAIN_ASSERTIONS
        return source is not null
            && operation == EventStoreGatewayWorkloadOperations.CommandStatus
            && string.Equals(domain, requestedDomain, StringComparison.Ordinal)
            ? source.IssueAsync(operation, tenant, domain, cancellationToken)
            : ValueTask.FromResult<string?>(null);
#else
        // Client 3.127.0 cannot request an exact domain. Never fall back to its unscoped issuer call.
        _ = source;
        _ = domain;
        return ValueTask.FromResult<string?>(null);
#endif
    }
}
