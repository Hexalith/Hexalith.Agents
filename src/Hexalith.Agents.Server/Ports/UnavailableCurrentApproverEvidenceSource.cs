using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Closed default until complete current dependency adapters are composed.</summary>
public sealed class UnavailableCurrentApproverEvidenceSource : ICurrentApproverEvidenceSource
{
    /// <inheritdoc />
    public Task<IReadOnlyList<CurrentApproverEvidence>> ReadAsync(string tenantId, ApproverPolicySource source,
        string? sourceConversationId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<CurrentApproverEvidence>>([]);
}
