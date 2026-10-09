from pathlib import Path
r=Path('/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Server/Security')
p=r/'DeletionConsumptionOperation.cs';s=p.read_text().replace('    public DeletionBlockedReplacementReconciliation? BlockedReplacementOriginal { get; init; }','    public DeletionBlockedReplacementReconciliation? BlockedReplacementOriginal { get; init; }\n    /// <summary>Exact original fully dispatched activation, retained when replacement compromise wins its compare.</summary>\n    public DeletionReattestationActivation? ActivationOriginal { get; init; }');p.write_bytes(s.replace('\n','\r\n').encode())
p=r/'DeletionConsumptionActor.cs';s=p.read_text().replace('''        var batch = Find(state, id); var keyBlock = KeyBlock(state, owned.Capability.CapabilityKeyVersion);
        if (batch is null''','''        var batch = Find(state, id); var keyBlock = KeyBlock(state, owned.Capability.CapabilityKeyVersion);
        bool originalBlockedActivation = batch is not null && batch.Current.Capability == owned.Capability && state.Operations.Any(o =>
            o.ActivationOriginal is { } activation && activation.Replacement.Capability == owned.Capability
            && activation.CompromiseBlockReceiptId == owned.CompromiseBlockReceiptId && activation.GuardReplacementReceiptId == owned.GuardReplacementReceiptId
            && activation.Replacement.DetachedJws == owned.DetachedJws && activation.Replacement.CommittedIssuedGuardRevision == owned.CommittedIssuedGuardRevision
            && o.Outcome.Status == DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise && o.Outcome.ReceiptId == batch.Outcome.ReceiptId);
        if (batch is null''',1)
s=s.replace('|| batch.Outcome.ReceiptId != owned.CompromiseBlockReceiptId || !DeletionConsumptionIdentity.SameBatch(batch.Original, owned)\n            || owned.Capability.AttestationOrdinal != checked((batch.BlockedReplacement?.Capability.AttestationOrdinal ?? batch.Current.Capability.AttestationOrdinal) + 1)\n            || owned.Capability.CapabilityKeyVersion == (batch.BlockedReplacement?.Capability.CapabilityKeyVersion ?? batch.Current.Capability.CapabilityKeyVersion)', '|| !originalBlockedActivation && batch.Outcome.ReceiptId != owned.CompromiseBlockReceiptId || !DeletionConsumptionIdentity.SameBatch(batch.Original, owned)\n            || !originalBlockedActivation && (owned.Capability.AttestationOrdinal != checked((batch.BlockedReplacement?.Capability.AttestationOrdinal ?? batch.Current.Capability.AttestationOrdinal) + 1)\n                || owned.Capability.CapabilityKeyVersion == (batch.BlockedReplacement?.Capability.CapabilityKeyVersion ?? batch.Current.Capability.CapabilityKeyVersion))',1)
# Existing dispatch activation retains its own exact original; it is not fabricated by reconciliation.
s=s.replace('new DeletionConsumptionOperation(owned.OperationId, digest, outcome)).ToArray()','new DeletionConsumptionOperation(owned.OperationId, digest, outcome) { ActivationOriginal = owned }).ToArray()',1)
s=s.replace('|| phase.Capability.AttestationOrdinal <= b.Current.Capability.AttestationOrdinal || phase.ExpectedKeyBlockSetRevision > state.KeyBlockSetRevision', '|| phase.Capability.AttestationOrdinal < b.Current.Capability.AttestationOrdinal\n                || phase.Capability.AttestationOrdinal == b.Current.Capability.AttestationOrdinal && phase.Capability != b.Current.Capability\n                || phase.ExpectedKeyBlockSetRevision > state.KeyBlockSetRevision',1)
needle='        foreach (var operation in state.Operations.Where(o => o.BlockedReplacementOriginal is not null))'
new='''        foreach (var operation in state.Operations.Where(o => o.ActivationOriginal is not null))
        {
            var activation = operation.ActivationOriginal!; var replacement = DeletionConsumptionIdentity.Capture(activation.Replacement);
            var batch = batches.SingleOrDefault(b => b.Current.Capability.BatchId == replacement.Capability.BatchId);
            if (batch is null || !DeletionConsumptionIdentity.SameBatch(batch.Original, replacement) || replacement.Capability.TenantId != tenant
                || activation.OperationId != operation.OperationId || operation.RequestDigest != DeletionConsumptionIdentity.Digest(activation)
                || operation.Outcome.BatchId != replacement.Capability.BatchId || operation.Outcome.Status is not (DeletionConsumptionStatus.Unconsumed or DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise)
                || operation.Outcome.ReceiptId != DeletionConsumptionIdentity.Digest(new[] { activation.OperationId, operation.RequestDigest, "activation" })
                || activation.ExpectedKeyBlockSetRevision < 0 || activation.ExpectedKeyBlockSetRevision > state.KeyBlockSetRevision
                || string.IsNullOrWhiteSpace(activation.CompromiseBlockReceiptId) || string.IsNullOrWhiteSpace(activation.GuardReplacementReceiptId))
            { throw new InvalidOperationException("Malformed retained activation original."); }
        }
'''+needle
assert needle in s;s=s.replace(needle,new,1)
s=s.replace('Block…', 'Block…') if False else s
s=s.replace('Block…','Block…') if False else s
s=s.replace('Block…','Block…') if False else s
old='Block…'
s=s.replace('BlockedReplacementOriginal = o.BlockedReplacementOriginal is null ? null : DeletionConsumptionIdentity.Capture(o.BlockedReplacementOriginal) }','BlockedReplacementOriginal = o.BlockedReplacementOriginal is null ? null : DeletionConsumptionIdentity.Capture(o.BlockedReplacementOriginal),\n            ActivationOriginal = o.ActivationOriginal is null ? null : o.ActivationOriginal with { Replacement = DeletionConsumptionIdentity.Capture(o.ActivationOriginal.Replacement) } }')
p.write_bytes(s.replace('\n','\r\n').encode())
