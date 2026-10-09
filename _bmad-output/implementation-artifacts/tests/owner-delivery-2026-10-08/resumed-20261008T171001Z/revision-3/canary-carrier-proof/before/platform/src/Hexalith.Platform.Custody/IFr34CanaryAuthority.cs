namespace Hexalith.Platform.Custody;

/// <summary>Independent exact production target qualification and irreversible canary destruction evidence.</summary>
public interface IFr34CanaryAuthority
{
    /// <summary>Authenticates currently installed exact engine/version/custody bindings, independently of engine self-report.</summary>
    Task<Fr34CanaryAuthorization?> ObserveAsync(Fr34ProtectionTarget expected, CancellationToken cancellationToken = default);
    /// <summary>Independently authenticates the original committed canary record and dedicated key against the expected target, fresh original ID and admitted current lifetime. Engine echoes or opaque references alone are insufficient. Omission denies every destruction, including late cleanup.</summary>
    Task<bool> VerifyOwnershipAsync(Fr34ProtectionTarget expected, string originalCanaryId, Fr34CanaryReference reference, Fr34CanaryAuthorization admitted,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
    /// <summary>Authenticates complete irreversible original canary DEK destruction at the enrolled custody owner.</summary>
    Task<bool> ConfirmDestroyedAsync(Fr34CanaryReference reference, CancellationToken cancellationToken = default);
}
