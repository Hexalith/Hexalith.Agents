using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Tests;

/// <summary>A legacy test provider implementing only the former synchronous role-bearing context contract.</summary>
internal sealed class LegacyRoleClaimAdministrationContextProvider : IAgentAdministrationContextProvider
{
    /// <inheritdoc />
    public AgentAdministrationContext GetContext()
        => new("tenant-a", "authenticated-subject", IsAgentsAdmin: true, IsPlatformOperator: true);
}
