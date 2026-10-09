using Hexalith.EventStore.Contracts.Security;

namespace Hexalith.EventStore.Server.Security;

/// <summary>Independent exact source, current principal/decision, finite cohort, custody and nonrollback authority. No fixture or default grants production authority.</summary>
public interface IGovernanceGuardAuthority
{
    /// <summary>Authenticates the installed current healthy signing key and immutable terminal no-issue source; omission disables signing continuation.</summary>
    Task<GovernanceSigningRecovery?> ReadSigningRecoveryAsync(GovernanceProtocolReceipt noIssue, TenantGovernanceGuardState state,
        CancellationToken cancellationToken = default) => Task.FromResult<GovernanceSigningRecovery?>(null);
    /// <summary>Authenticates current private exact original-result lookup independently of any fresh effect/decision permission; omission disables lookup.</summary>
    Task<GovernanceGuardEvidence?> ReadLookupAsync(GovernanceGuardTransition transition, string intentDigest, string targetMutationDigest,
        CancellationToken cancellationToken = default) => Task.FromResult<GovernanceGuardEvidence?>(null);
    /// <summary>Authenticates the closed transition, immutable first-event/permit facts and exact protected source/outbox/phase mutations; supplies applicable current policy and original protection receipts.</summary>
    Task<GovernanceGuardEvidence?> ReadAsync(GovernanceGuardTransition transition, string intentDigest, string targetMutationDigest,
        TenantGovernanceGuardState state, CancellationToken cancellationToken = default);
}
