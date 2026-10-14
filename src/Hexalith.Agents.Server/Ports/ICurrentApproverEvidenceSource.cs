using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Host adapter for exact current Parties, Tenants role and Conversations Facilitator/participant evidence.</summary>
public interface ICurrentApproverEvidenceSource
{
    /// <summary>Returns all current candidates; missing/ambiguous/stale observations never supply an eligible actor.</summary>
    Task<IReadOnlyList<CurrentApproverEvidence>> ReadAsync(string tenantId, ApproverPolicySource source,
        string? sourceConversationId, CancellationToken cancellationToken);

    /// <summary>Reconfirms exact current source heads, bindings and authority after all reads; omission grants no authority.</summary>
    Task<bool> ConfirmAsync(string tenantId, IReadOnlyList<CurrentApproverEvidence> candidates, CancellationToken cancellationToken)
        => Task.FromResult(false);
}
