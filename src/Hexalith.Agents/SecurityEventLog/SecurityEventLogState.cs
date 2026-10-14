using Hexalith.Agents.Contracts.SecurityEventLog;

namespace Hexalith.Agents.SecurityEventLog;

/// <summary>Daily immutable id-index folded from recorded security events.</summary>
public sealed class SecurityEventLogState
{
    /// <summary>Gets acknowledged original observations by exact id.</summary>
    public Dictionary<string, RecordSecurityEvent> Observations { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Applies one original; contradictory accepted history is invalid.</summary>
    public void Apply(SecurityEventRecorded item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Observations.TryGetValue(item.Observation.SecurityObservationId, out RecordSecurityEvent? original)
            && original != item.Observation)
        {
            throw new InvalidOperationException("Security observation history conflicts with its original.");
        }

        Observations[item.Observation.SecurityObservationId] = item.Observation;
    }

    /// <summary>Preserves acknowledged observations when a rejection is replayed.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "EventStore requires instance Apply methods for replay discovery.")]
    public void Apply(SecurityEventRejected item) => ArgumentNullException.ThrowIfNull(item);
}
