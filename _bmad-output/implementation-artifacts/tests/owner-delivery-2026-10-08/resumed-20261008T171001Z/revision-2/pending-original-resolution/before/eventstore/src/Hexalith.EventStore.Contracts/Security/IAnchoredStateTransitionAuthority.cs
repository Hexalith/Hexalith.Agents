namespace Hexalith.EventStore.Contracts.Security;

/// <summary>Independent exact conditional-transition journal, separate from the primary owner store and private caller authority.</summary>
/// <remarks>The installed authority binds the complete owner/purpose scope, predecessor revision/digest and target revision/digest.
/// Record atomically compares the current predecessor anchor, advances it and retains this immutable transition proof.
/// Exact retry reuses the original proof. Verification is a fresh independent read; neither pending bytes nor cached state are authority.</remarks>
public interface IAnchoredStateTransitionAuthority
{
    /// <summary>Atomically records the exact transition and advances its independently governed current anchor. Missing implementation denies.</summary>
    Task<bool> RecordTransitionAsync(AnchoredStateTransition transition, CancellationToken cancellationToken = default) => Task.FromResult(false);
    /// <summary>Authenticates the exact original transition, including its predecessor, target and scope; it does not advance an anchor.</summary>
    Task<bool> VerifyTransitionAsync(AnchoredStateTransition transition, CancellationToken cancellationToken = default) => Task.FromResult(false);
}
