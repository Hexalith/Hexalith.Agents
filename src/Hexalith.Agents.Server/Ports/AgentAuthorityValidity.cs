namespace Hexalith.Agents.Server.Ports;

/// <summary>Retains a current authorization's exclusive deadline and checked clock across asynchronous work.</summary>
/// <remarks>This transient bound grants no authority and never enters a command payload or persisted metadata.</remarks>
public sealed class AgentAuthorityValidity
{
    private readonly object _gate = new();
    private readonly TimeProvider _clock;
    private readonly DateTimeOffset _validUntil;
    private readonly AgentAuthorityValidity? _retained;
    private DateTimeOffset _checkedAt;
    private bool _invalid;

    /// <summary>Initializes a bound after current actor and role evidence have both been verified.</summary>
    /// <param name="clock">The same clock used to evaluate current evidence.</param>
    /// <param name="checkedAt">The verified authorization completion instant.</param>
    /// <param name="validUntil">The earliest exclusive actor or role evidence deadline.</param>
    public AgentAuthorityValidity(TimeProvider clock, DateTimeOffset checkedAt, DateTimeOffset validUntil)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _clock = clock;
        _checkedAt = checkedAt;
        _validUntil = validUntil;
        _invalid = checkedAt == default || checkedAt.Offset != TimeSpan.Zero
            || validUntil.Offset != TimeSpan.Zero || validUntil <= checkedAt;
    }

    private AgentAuthorityValidity(TimeProvider clock, DateTimeOffset checkedAt, DateTimeOffset validUntil, AgentAuthorityValidity retained)
        : this(clock, checkedAt, validUntil)
    {
        _retained = retained;
    }

    /// <summary>Adds a narrower interval without discarding this bound's deadline or checked history.</summary>
    /// <param name="checkedAt">The latest checked completion instant.</param>
    /// <param name="validUntil">The additional exclusive deadline.</param>
    /// <returns>A transient bound that requires both intervals to remain current.</returns>
    internal AgentAuthorityValidity Restrict(DateTimeOffset checkedAt, DateTimeOffset validUntil)
        => new(_clock, checkedAt, validUntil, this);

    /// <summary>Checks exclusive expiry and per-await clock ordering; a failed bound never becomes valid again.</summary>
    public bool IsCurrent
    {
        get
        {
            lock (_gate)
            {
                return Check(_clock.GetUtcNow());
            }
        }
    }

    /// <summary>Stops protected work after exclusive expiry or a checked clock rollback.</summary>
    public void ThrowIfNotCurrent()
    {
        if (!IsCurrent)
        {
            throw new AgentCommandAuthorityExpiredException();
        }
    }

    /// <summary>Includes a captured UTC observation in the original authorization interval before protected work.</summary>
    /// <param name="observedAt">The clock instant captured while evaluating the protected result.</param>
    public void ThrowIfNotCurrent(DateTimeOffset observedAt)
    {
        lock (_gate)
        {
            if (!Check(observedAt))
            {
                throw new AgentCommandAuthorityExpiredException();
            }
        }
    }

    private bool Check(DateTimeOffset now)
    {
        if (_retained is not null)
        {
            lock (_retained._gate)
            {
                _invalid |= !_retained.Check(now);
            }
        }
        _invalid |= now < _checkedAt || now >= _validUntil || now.Offset != TimeSpan.Zero;
        _checkedAt = now;
        return !_invalid;
    }

    /// <summary>Returns a safe label without identity or authority timestamps.</summary>
    /// <returns>The transient bound type label.</returns>
    public override string ToString() => nameof(AgentAuthorityValidity);
}
