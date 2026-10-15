using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Unit-only admission fixture for existing gateway behavior tests; supplies no live cryptographic/ACL evidence.</summary>
internal sealed class FixtureTrustedCommandAdmission : IAgentTrustedCommandAdmission
{
    /// <inheritdoc />
    public Task<AgentCommandIntent> AdmitAsync(AgentCommandIntent intent, CancellationToken cancellationToken)
        => Task.FromResult(intent);
}
