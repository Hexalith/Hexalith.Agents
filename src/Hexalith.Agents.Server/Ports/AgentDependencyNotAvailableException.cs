namespace Hexalith.Agents.Server.Ports;

/// <summary>A closed execution gate failure; no gateway request or external effect was dispatched.</summary>
/// <param name="recordId">Unavailable accepted record.</param>
public sealed class AgentDependencyNotAvailableException(string recordId) : InvalidOperationException("DependencyNotAvailable")
{
    /// <summary>Gets the unavailable external dependency record.</summary>
    public string RecordId { get; } = recordId;
}
