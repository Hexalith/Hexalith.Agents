namespace Hexalith.Agents.Server.Ports;

/// <summary>Closed default; JWT claims cannot supply a missing current human binding.</summary>
public sealed class UnavailableCurrentHumanActorSource : ICurrentHumanActorSource
{
    /// <inheritdoc />
    public Task<CurrentHumanActorEvidence?> ReadAsync(string tenantId, string subject, CancellationToken cancellationToken)
        => Task.FromResult<CurrentHumanActorEvidence?>(null);
}
