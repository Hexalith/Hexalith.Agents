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

    /// <inheritdoc />
    public DateTimeOffset? ProjectedAt { get; set; }

    /// <inheritdoc />
    public string? ProjectionVersion { get; set; }
}
