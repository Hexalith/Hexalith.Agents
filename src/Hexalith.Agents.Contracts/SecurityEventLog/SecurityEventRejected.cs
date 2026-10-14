namespace Hexalith.Agents.Contracts.SecurityEventLog;

/// <summary>A private content-free invalid/conflicting recording outcome.</summary>
/// <param name="Reason">Closed safe rejection class.</param>
public sealed record SecurityEventRejected(string Reason) : IRejectionEvent;
