namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// The distinct non-data surface states an Agents setup/operational page renders for a gateway result (AC6, 4.3-AC2;
/// AD-12). Each maps to a localized whole-string title/message and the correct live-region politeness:
/// <see cref="Error"/>, <see cref="PermissionDenied"/>, and <see cref="Unavailable"/> announce assertively
/// (<c>role="alert"</c>), the rest politely (<c>role="status"</c>). Empty/permission-denied/unavailable never leak or
/// fingerprint unauthorized records. <see cref="Stale"/>, <see cref="Degraded"/>, and <see cref="Unavailable"/> are
/// three distinct kinds (Story 4.3 AC2): <see cref="Stale"/> is a fresh-but-aged projection, <see cref="Degraded"/> is
/// completed-but-stale data (never rendered as a fresh success), and <see cref="Unavailable"/> is a down
/// dependency/projection. New members are appended (additive — never reorder/rename existing members; AD-17).
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

    /// <summary>The projection reports stale (fresh-but-aged) data — offers a refresh; never rendered as fresh (distinct from <see cref="Degraded"/>/<see cref="Unavailable"/>).</summary>
    Stale,

    /// <summary>The projection reports completed-but-stale (degraded) data — offers a refresh; announced politely; never rendered as a fresh success (Story 4.3 AC2).</summary>
    Degraded,

    /// <summary>A required dependency/projection is down — fail-closed, announced assertively, no record fingerprinting (Story 4.3 AC2; AD-12).</summary>
    Unavailable,
}
