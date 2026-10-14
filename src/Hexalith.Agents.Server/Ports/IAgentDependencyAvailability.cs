using Hexalith.Agents.Contracts.Security;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Host-owned exact installed-target availability and passing compatibility evidence.</summary>
public interface IAgentDependencyAvailability
{
    /// <summary>Returns a blocker unless the record is Available with current passing exact-target evidence.</summary>
    DependencyNotAvailable? Check(string recordId);
}
