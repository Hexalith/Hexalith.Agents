namespace Hexalith.Agents.Server.Ports;

/// <summary>Host adapter for current actor authority and exact fresh human Party binding.</summary>
public interface ICurrentHumanActorSource
{
    /// <summary>Reads verified current subject/actor/Party correspondence, never historical attribution.</summary>
    Task<CurrentHumanActorEvidence?> ReadAsync(string tenantId, string subject, CancellationToken cancellationToken);
}
