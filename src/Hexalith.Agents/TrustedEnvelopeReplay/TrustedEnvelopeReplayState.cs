using Hexalith.Agents.Contracts.TrustedEnvelopeReplay;

namespace Hexalith.Agents.TrustedEnvelopeReplay;

/// <summary>Immutable first-seen replay facts folded only from accepted events.</summary>
public sealed class TrustedEnvelopeReplayState
{
    /// <summary>Gets the first accepted registration.</summary>
    [System.Text.Json.Serialization.JsonInclude]
    public RegisterTrustedEnvelopeFirstSeen? Registration { get; private set; }

    /// <summary>Applies the original event; contradictory accepted history cannot silently overwrite it.</summary>
    public void Apply(TrustedEnvelopeFirstSeen item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Registration is not null && Registration != item.Registration)
        {
            throw new InvalidOperationException("Replay history conflicts with the original registration.");
        }

        Registration = item.Registration;
    }

    /// <summary>Preserves original facts when a rejected command is replayed.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "EventStore requires instance Apply methods for replay discovery.")]
    public void Apply(TrustedEnvelopeReplayRejected item) => ArgumentNullException.ThrowIfNull(item);
}
