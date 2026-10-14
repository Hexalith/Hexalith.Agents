using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>No local signing/replay/spool substitute is installed; unavailable dependencies stop before target construction.</summary>
public sealed class UnavailableAgentTrustedCommandAdmission(IAgentDependencyAvailability availability) : IAgentTrustedCommandAdmission
{
    /// <inheritdoc />
    public Task<CommandEnvelope> AdmitAsync(CommandEnvelope intent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intent);
        cancellationToken.ThrowIfCancellationRequested();
        foreach (string record in new[] { "EXT-SECRETS-1", "EXT-HOST-1" })
        {
            if (availability.Check(record) is not null)
            {
                return Task.FromException<CommandEnvelope>(new AgentDependencyNotAvailableException(record));
            }
        }

        // Availability of a record never invents a host adapter or credentials.
        return Task.FromException<CommandEnvelope>(new AgentDependencyNotAvailableException("EXT-HOST-1"));
    }
}
