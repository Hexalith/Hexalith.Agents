using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Agents.Server.Projections;

/// <summary>Persisted tenant provider enablement and acceptance state.</summary>
public sealed class TenantProviderEnablementReadModel : IReadModelFreshness
{
    /// <summary>Gets or sets the replayed state.</summary>
    public TenantProviderEnablementState State { get; set; } = new();

    /// <summary>Gets or sets the projected sequence.</summary>
    public long LastSequenceNumber { get; set; }

    /// <summary>Gets recent successful projected command identities for exact write confirmation.</summary>
    public List<string> ProjectedCommandMessageIds { get; set; } = [];

    /// <inheritdoc />
    public DateTimeOffset? ProjectedAt { get; set; }

    /// <inheritdoc />
    public string? ProjectionVersion { get; set; }

    /// <summary>
    /// Checks that this persisted model belongs to the requested tenant. The requested key never establishes the
    /// returned value's scope; only a model that has not yet recorded any tenant fact may omit its tenant.
    /// </summary>
    /// <param name="tenantId">The tenant whose enablement was requested.</param>
    /// <returns><see langword="true"/> when the model can be rendered or used for that tenant.</returns>
    public bool IsScopedTo(string tenantId)
        => string.Equals(State.TenantId, tenantId, StringComparison.Ordinal)
            || (string.IsNullOrEmpty(State.TenantId) && State.Entries.Count == 0 && State.Revision == 0
                && ProjectedCommandMessageIds.Count == 0);
}
