from pathlib import Path
root=Path('/home/administrator/projects/hexalith')
p=root/'eventstore/src/Hexalith.EventStore.Server/Security/DeletionConsumptionOperation.cs';s=p.read_text().replace('DeletionConsumptionOutcome Outcome);','''DeletionConsumptionOutcome Outcome)
{
    /// <summary>Exact retained no-effect reconciliation original, including historical outcomes after a healthy successor.</summary>
    public DeletionBlockedReplacementReconciliation? BlockedReplacementOriginal { get; init; }
}''');p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'eventstore/src/Hexalith.EventStore.Server/Security/DeletionConsumptionActor.cs';s=p.read_text();s=s.replace('new DeletionConsumptionOperation(owned.OperationId, digest, outcome)).ToArray()','new DeletionConsumptionOperation(owned.OperationId, digest, outcome) { BlockedReplacementOriginal = owned }).ToArray()',1)
s=s.replace('        ArgumentNullException.ThrowIfNull(capability);\n        ArgumentNullException.ThrowIfNull(capability);','        ArgumentNullException.ThrowIfNull(capability);',1)
old='''        var state = await ReadAsync(capability.TenantId).ConfigureAwait(false); var phase = Find(state, capability.BatchId)?.BlockedReplacement;
        if (phase is null || phase.Capability != capability) { return null; }
        var operation = state.Operations.SingleOrDefault(o => o.OperationId == phase.OperationId && o.RequestDigest == DeletionConsumptionIdentity.Digest(phase));'''
new='''        var state = await ReadAsync(capability.TenantId).ConfigureAwait(false);
        var operation = state.Operations.SingleOrDefault(o => o.BlockedReplacementOriginal?.Capability == capability);
        var phase = operation?.BlockedReplacementOriginal;
        if (phase is null || operation!.RequestDigest != DeletionConsumptionIdentity.Digest(phase)) { return null; }'''
assert old in s;s=s.replace(old,new)
old='''        var owned = state with { Batches = Array.AsReadOnly(batches), Revocations = Array.AsReadOnly(state.Revocations.Select(r => r with {'''
new='''        foreach (var operation in state.Operations.Where(o => o.BlockedReplacementOriginal is not null))
        {
            var phase = DeletionConsumptionIdentity.Capture(operation.BlockedReplacementOriginal!); var batch = batches.SingleOrDefault(b => b.Current.Capability.BatchId == phase.Capability.BatchId);
            if (batch is null || !DeletionConsumptionIdentity.SameBatch(batch.Original, phase) || phase.Capability.TenantId != tenant
                || operation.OperationId != phase.OperationId || operation.RequestDigest != DeletionConsumptionIdentity.Digest(phase)
                || operation.Outcome.Status != DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise || operation.Outcome.BatchId != phase.Capability.BatchId
                || operation.Outcome.KeyBlockSetRevision != phase.ExpectedKeyBlockSetRevision || operation.Outcome.BlockReason != DeletionConsumptionBlockReason.CapabilityKeyCompromise
                || operation.Outcome.BlockedKeyVersion != phase.Capability.CapabilityKeyVersion || operation.Outcome.RevocationRevision != phase.RevocationReceipt.Envelope.RevocationRevision
                || operation.Outcome.ReceiptId != DeletionConsumptionIdentity.Digest(new[] { phase.OperationId, operation.RequestDigest, "blocked-issued-replacement" })
                || operation.Outcome.TargetReceipts.Count != 0 || !state.Revocations.Any(r => DeletionConsumptionIdentity.Digest(r) == DeletionConsumptionIdentity.Digest(phase.RevocationReceipt)))
            { throw new InvalidOperationException("Malformed retained reconciliation original."); }
        }
        var owned = state with { Batches = Array.AsReadOnly(batches), Revocations = Array.AsReadOnly(state.Revocations.Select(r => r with {'''
assert old in s;s=s.replace(old,new)
s=s.replace('Operations = Array.AsReadOnly(state.Operations.ToArray())','Operations = Array.AsReadOnly(state.Operations.Select(o => o with { BlockedReplacementOriginal = o.BlockedReplacementOriginal is null ? null : DeletionConsumptionIdentity.Capture(o.BlockedReplacementOriginal) }).ToArray())')
p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'platform/src/Hexalith.Platform.Custody/DeletionBatchExecutionCoordinator.cs';s=p.read_text().replace('if (pendingReplacement?.Batch.ProtectionOutcome == "ReplacementAwaitingActivation")','if (pendingReplacement?.Batch.ProtectionOutcome is "ReplacementAwaitingActivation" or "ConsumptionBlocked:CapabilityKeyCompromise")',1)
s=s.replace('                    if (retained is null)\n                    {','                    if (retained is null && pendingReplacement.Batch.ProtectionOutcome == "ReplacementAwaitingActivation")\n                    {',1)
s=s.replace('pendingReplacement.Batch.IssuedGuardRevision, pendingReplacement.Batch.Targets.Select(t => new ProtectionTarget(t.TenantId, t.AgentInteractionId, t.TargetProtectionKeyAlias)).ToArray(), comparison.ReplacementKeyRevocation);','pendingReplacement.Batch.IssuedGuardRevision, await CaptureTargetsAsync(pendingReplacement.Batch.Targets).ConfigureAwait(false), comparison.ReplacementKeyRevocation);')
s=s.replace('retained.Original.CompromiseBlockReceiptId != pendingReplacement.Batch.ProtectionReceiptId || retained.Original.GuardReplacementReceiptId', '(pendingReplacement.Batch.ProtectionOutcome == "ReplacementAwaitingActivation" ? retained.Original.CompromiseBlockReceiptId : retained.Outcome.ReceiptId) != pendingReplacement.Batch.ProtectionReceiptId || retained.Original.GuardReplacementReceiptId',1)
old='''            var targets = await WaitAsync(() =>
            {
                var captured = new List<ProtectionTarget>();
                foreach (var value in snapshot.Batch.Targets)
                { deadline.Check(); if (captured.Count >= 1000 || value is null) { throw new ArgumentException("Oversized exact protection manifest."); } captured.Add(new(value.TenantId, value.AgentInteractionId, value.TargetProtectionKeyAlias)); }
                if (DeletionBatchCapabilityIdentity.TargetManifestDigest(captured) != payload.ManifestDigest) { throw new ArgumentException("Mismatched exact protection manifest."); }
                return Task.FromResult<IReadOnlyList<ProtectionTarget>>(captured.AsReadOnly());
            }).ConfigureAwait(false);'''
new='''            var targets = await CaptureTargetsAsync(snapshot.Batch.Targets).ConfigureAwait(false);'''
assert old in s;s=s.replace(old,new)
s=s.replace('            async Task<T> WaitAsync<T>(Func<Task<T>> operation)', '''            async Task<IReadOnlyList<ProtectionTarget>> CaptureTargetsAsync(IReadOnlyList<GovernanceProtectionTarget> supplied)
                => await WaitAsync(() =>
                {
                    var captured = new List<ProtectionTarget>();
                    foreach (var value in supplied)
                    { deadline.Check(); if (captured.Count >= 1000 || value is null) { throw new ArgumentException("Oversized exact protection manifest."); } captured.Add(new(value.TenantId, value.AgentInteractionId, value.TargetProtectionKeyAlias)); }
                    if (DeletionBatchCapabilityIdentity.TargetManifestDigest(captured) != payload.ManifestDigest) { throw new ArgumentException("Mismatched exact protection manifest."); }
                    return Task.FromResult<IReadOnlyList<ProtectionTarget>>(captured.AsReadOnly());
                }).ConfigureAwait(false);
            async Task<T> WaitAsync<T>(Func<Task<T>> operation)''')
# A previously committed mirror is recovered by its original exact receipt, without dispatch or a new phase.
s=s.replace('''                        var mirroredBlock = await WaitAsync(() => guard.RecordBlockedReplacementAsync(signed, retained, providerCancellation.Token)).ConfigureAwait(false);''','''                        if (pendingReplacement.Batch.ProtectionOutcome == "ConsumptionBlocked:CapabilityKeyCompromise")
                        { deadline.Check(); return new("ActivationBlockedByReplacementKeyCompromise", signed, retained.Outcome, issued); }
                        var mirroredBlock = await WaitAsync(() => guard.RecordBlockedReplacementAsync(signed, retained, providerCancellation.Token)).ConfigureAwait(false);''',1)
p.write_bytes(s.replace('\n','\r\n').encode())
p=root/'platform/tests/Hexalith.Platform.Custody.Tests/DeletionBatchExecutionCoordinatorTests.cs';s=p.read_text().replace('replay.Protection.ShouldBe(retained.Outcome);','JsonSerializer.SerializeToUtf8Bytes(replay.Protection).ShouldBe(JsonSerializer.SerializeToUtf8Bytes(retained.Outcome));').replace('historical.Original.ShouldBe(retained.Original); historical.Outcome.ShouldBe(retained.Outcome);','JsonSerializer.SerializeToUtf8Bytes(historical.Original).ShouldBe(JsonSerializer.SerializeToUtf8Bytes(retained.Original)); JsonSerializer.SerializeToUtf8Bytes(historical.Outcome).ShouldBe(JsonSerializer.SerializeToUtf8Bytes(retained.Outcome));');p.write_bytes(s.replace('\n','\r\n').encode())
