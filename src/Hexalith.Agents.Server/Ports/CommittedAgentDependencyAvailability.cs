using Hexalith.Agents.Contracts.Security;

namespace Hexalith.Agents.Server.Ports;

/// <summary>The accepted register grants contract work only; production composition must supply live evidence.</summary>
public sealed class CommittedAgentDependencyAvailability : IAgentDependencyAvailability
{
    /// <inheritdoc />
    public DependencyNotAvailable Check(string recordId) => new(recordId);
}
