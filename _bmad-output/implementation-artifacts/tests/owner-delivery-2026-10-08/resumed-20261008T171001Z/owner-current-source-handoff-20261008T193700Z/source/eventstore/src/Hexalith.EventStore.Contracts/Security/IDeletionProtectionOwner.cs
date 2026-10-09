namespace Hexalith.EventStore.Contracts.Security;

/// <summary>Private transport-neutral existing atomic protection owner seam; signature-only callers have no destruction authority.</summary>
public interface IDeletionProtectionOwner
{
    /// <summary>Registers exact online guard-issued/dispatch-authorized batch without reservation.</summary>
    Task<DeletionConsumptionOutcome> RegisterAsync(DeletionBatchConsumptionRequest request, CancellationToken cancellationToken = default);
    /// <summary>Serializes irreversible original all-target reservation with hold/integrity/key blocks.</summary>
    Task<DeletionConsumptionOutcome> ReserveAndConsumeAsync(DeletionBatchConsumptionRequest request, CancellationToken cancellationToken = default);
    /// <summary>Activates exact same-batch sole replacement and successor dispatch under expected block-set compare.</summary>
    Task<DeletionConsumptionOutcome> ActivateAsync(DeletionReattestationActivation activation, CancellationToken cancellationToken = default);
    /// <summary>Reads/reconciles only exact original durable result; absence or unavailable state grants no effect.</summary>
    Task<DeletionConsumptionOutcome> LookupAsync(string tenantId, string batchId, CancellationToken cancellationToken = default);
}
