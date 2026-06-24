namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// The distinct non-data surface states an Agents setup page renders for a gateway result (AC6; AD-12). Each maps
/// to a localized whole-string title/message and the correct live-region politeness: <see cref="Error"/> and
/// <see cref="PermissionDenied"/> announce assertively (<c>role="alert"</c>), the rest politely
/// (<c>role="status"</c>). Empty/permission-denied never leak or fingerprint unauthorized records.
/// </summary>
public enum AgentSurfaceKind
{
    /// <summary>A gateway read is in flight.</summary>
    Loading,

    /// <summary>An authorized empty result — no record exists yet (not a failure).</summary>
    Empty,

    /// <summary>An authorized result with active filters that match nothing — offers a filter reset.</summary>
    FilteredEmpty,

    /// <summary>The read faulted or the service is unreachable.</summary>
    Error,

    /// <summary>The caller is not authorized — fail-closed, no record fingerprinting (AD-12).</summary>
    PermissionDenied,

    /// <summary>The projection reports stale/degraded data — offers a refresh; never rendered as fresh.</summary>
    Stale,
}
