namespace Hexalith.Agents.Contracts.Security;

/// <summary>An unavailable accepted external record; local fixtures confer no availability.</summary>
/// <param name="RecordId">The stable external dependency record.</param>
public sealed record DependencyNotAvailable(string RecordId);
