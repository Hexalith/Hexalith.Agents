namespace Hexalith.Agents.Contracts.SecurityEventLog;

/// <summary>One acknowledged original safe observation in its authenticated daily stream.</summary>
/// <param name="Observation">Immutable safe first-seen facts.</param>
public sealed record SecurityEventRecorded(RecordSecurityEvent Observation) : IEventPayload;
