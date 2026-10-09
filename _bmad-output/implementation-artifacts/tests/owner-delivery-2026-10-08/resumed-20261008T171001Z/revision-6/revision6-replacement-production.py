from pathlib import Path
base=Path('/home/administrator/projects/hexalith');ev=base/'eventstore';pl=base/'platform'
def w(p,s):p.write_bytes(s.replace('\r\n','\n').replace('\n','\r\n').encode())
def edit(root,path,f):p=root/path;w(p,f(p.read_text()))
p=ev/'src/Hexalith.EventStore.Contracts/Security/DeletionBlockedReplacementReconciliation.cs'
w(p,'''namespace Hexalith.EventStore.Contracts.Security;

/// <summary>Exact independently proved issued-but-blocked successor. This phase grants no dispatch, reservation, signing or physical consumption authority.</summary>
/// <param name="OperationId">Stable complete original reconciliation identity.</param><param name="CompromiseBlockReceiptId">Exact retained previous batch block.</param>
/// <param name="ExpectedKeyBlockSetRevision">Current independent block-set comparison.</param><param name="GuardReplacementReceiptId">Exact committed replacement issue receipt.</param>
/// <param name="Capability">Exact canonical issued successor.</param><param name="DetachedJws">Exact retained signed artifact.</param>
/// <param name="SigningRequestId">Canonical original signing request.</param><param name="CommittedIssuedGuardRevision">Actual durable issue revision.</param>
/// <param name="Targets">Unchanged exact immutable manifest.</param><param name="RevocationReceipt">Exact independently retained successor-key revocation.</param>
public sealed record DeletionBlockedReplacementReconciliation(string OperationId, string CompromiseBlockReceiptId, long ExpectedKeyBlockSetRevision,
    string GuardReplacementReceiptId, DeletionBatchCapabilityV1 Capability, string DetachedJws, string SigningRequestId,
    long CommittedIssuedGuardRevision, IReadOnlyList<ProtectionTarget> Targets, DeletionCapabilityRevocationReceipt RevocationReceipt);

/// <summary>Only an independently retained original blocked disposition; never a dispatch or physical effect receipt.</summary>
/// <param name="Original">Exact original no-effect reconciliation.</param><param name="Outcome">Its original immutable owner outcome.</param>
public sealed record DeletionBlockedReplacementResult(DeletionBlockedReplacementReconciliation Original, DeletionConsumptionOutcome Outcome);
''')
edit(ev,'src/Hexalith.EventStore.Contracts/Security/IDeletionProtectionOwner.cs',lambda s:s.replace('    /// <summary>Reads/reconciles only', '''    /// <summary>Settles only the exact independently proved issued-and-revoked successor, without dispatch or consumption; omitted implementations remain unavailable.</summary>
    Task<DeletionConsumptionOutcome> ReconcileBlockedReplacementAsync(DeletionBlockedReplacementReconciliation request, CancellationToken cancellationToken = default)
        => Task.FromResult(new DeletionConsumptionOutcome(request.Capability.TenantId, request.Capability.BatchId, DeletionConsumptionStatus.Unavailable, 0, 0, null, null, null, null, []));
    /// <summary>Reads only the exact retained original issued-but-blocked phase and outcome, never advancing an ordinal.</summary>
    Task<DeletionBlockedReplacementResult?> ReadBlockedReplacementAsync(DeletionBatchCapabilityV1 capability, CancellationToken cancellationToken = default)
        => Task.FromResult<DeletionBlockedReplacementResult?>(null);
    /// <summary>Reads/reconciles only'''))
edit(ev,'src/Hexalith.EventStore.Contracts/Security/DeletionActivationComparison.cs',lambda s:s.replace('    string ReplacementKeyVersion, bool ReplacementKeyBlocked, long OwnerRevision);','''    string ReplacementKeyVersion, bool ReplacementKeyBlocked, long OwnerRevision)
{
    /// <summary>Exact independently retained blocked-key event at this current comparison; absence grants no reconciliation.</summary>
    public DeletionCapabilityRevocationReceipt? ReplacementKeyRevocation { get; init; }
}'''))
edit(ev,'src/Hexalith.EventStore.Contracts/Security/GovernanceGuardEvidence.cs',lambda s:s.replace('\n}', '''
    /// <summary>Independently authenticated exact retained no-dispatch blocked replacement and original owner outcome.</summary>
    public DeletionBlockedReplacementResult? ProtectionBlockedReplacement { get; init; }
}'''))
edit(ev,'src/Hexalith.EventStore.Server/Security/IDeletionConsumptionActor.cs',lambda s:s.replace('    /// <summary>Reads current independently', '''    /// <summary>Reconciles an independently authenticated exact issued-and-blocked successor without any dispatch or reservation.</summary>
    Task<DeletionConsumptionOutcome> ReconcileBlockedReplacementAsync(DeletionBlockedReplacementReconciliation request);
    /// <summary>Reads the exact retained original no-effect reconciliation and outcome.</summary>
    Task<DeletionBlockedReplacementResult?> ReadBlockedReplacementAsync(DeletionBatchCapabilityV1 capability);
    /// <summary>Reads current independently'''))
edit(ev,'src/Hexalith.EventStore.Server/Security/IDeletionConsumptionAuthority.cs',lambda s:s.replace('\n}', '''
    /// <summary>Authenticates the exact retained original batch block, canonical signed issued successor/manifest/actual issue receipt and current independently retained successor-key revocation. No dispatch or physical grant is inferred.</summary>
    Task<bool> VerifyBlockedReplacementAsync(DeletionBlockedReplacementReconciliation request, CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}'''))
edit(ev,'src/Hexalith.EventStore.Server/Security/DeletionConsumptionBatch.cs',lambda s:s.replace('DeletionConsumptionOutcome Outcome);','''DeletionConsumptionOutcome Outcome)
{
    public DeletionBlockedReplacementReconciliation? BlockedReplacement { get; init; }
}'''))
edit(ev,'src/Hexalith.EventStore.Server/Security/DeletionConsumptionIdentity.cs',lambda s:s.replace('    internal static void Revocation', '''    internal static DeletionBlockedReplacementReconciliation Capture(DeletionBlockedReplacementReconciliation request)
    {
        ArgumentNullException.ThrowIfNull(request); var c = request.Capability;
        if (request.SigningRequestId != DeletionBatchCapabilityIdentity.SigningRequestId(c) || request.DetachedJws is not { Length: > 0 and <= 16384 }
            || request.CommittedIssuedGuardRevision <= 0 || request.ExpectedKeyBlockSetRevision <= 0 || request.Targets is null || request.Targets.Count is < 1 or > 1000)
        { throw new ArgumentException("Malformed blocked replacement."); }
        foreach (string text in new[] { request.OperationId, request.CompromiseBlockReceiptId, request.GuardReplacementReceiptId }) { Text(text); }
        var targets = new List<ProtectionTarget>();
        foreach (var target in request.Targets)
        {
            if (targets.Count >= 1000 || target is null || target.TenantId != c.TenantId) { throw new ArgumentException("Malformed blocked replacement target."); }
            Text(target.TenantId); Text(target.AgentInteractionId); Text(target.TargetProtectionKeyAlias); targets.Add(target);
        }
        if (TargetDigest(targets) != c.ManifestDigest) { throw new ArgumentException("Changed blocked replacement manifest."); }
        Revocation(request.RevocationReceipt.Envelope);
        var revocation = request.RevocationReceipt;
        if (revocation.Envelope.TenantId != c.TenantId || revocation.Envelope.KeyVersion != c.CapabilityKeyVersion
            || revocation.ReceiptId != Digest(revocation.Envelope) || revocation.KeyBlockSetRevision <= 0 || revocation.OwnerRevision <= 0
            || revocation.KeyBlockSetRevision > request.ExpectedKeyBlockSetRevision || revocation.AffectedBatchIds.Count > 1000)
        { throw new ArgumentException("Changed blocked replacement revocation."); }
        return request with { Targets = targets.AsReadOnly(), RevocationReceipt = revocation with { AffectedBatchIds = Array.AsReadOnly(revocation.AffectedBatchIds.ToArray()) } };
    }
    internal static bool SameBatch(DeletionBatchConsumptionRequest original, DeletionBlockedReplacementReconciliation replacement)
    {
        var x = original.Capability; var y = replacement.Capability;
        return x.TenantId == y.TenantId && x.Issuer == y.Issuer && x.Audience == y.Audience && x.DeletionRequestId == y.DeletionRequestId
            && x.DestructionSealId == y.DestructionSealId && x.BatchKind == y.BatchKind && x.BatchOrdinal == y.BatchOrdinal
            && x.BatchId == y.BatchId && x.ManifestDigest == y.ManifestDigest && x.GuardStreamId == y.GuardStreamId && original.Targets.SequenceEqual(replacement.Targets);
    }
    internal static void Revocation'''))
edit(ev,'src/Hexalith.EventStore.Server/Security/DaprDeletionProtectionOwner.cs',lambda s:s.replace('    /// <inheritdoc/>\n    public Task<DeletionConsumptionOutcome> LookupAsync', '''    /// <inheritdoc/>
    public Task<DeletionConsumptionOutcome> ReconcileBlockedReplacementAsync(DeletionBlockedReplacementReconciliation request, CancellationToken cancellationToken = default)
        => InvokeAsync(request.Capability.TenantId, actor => actor.ReconcileBlockedReplacementAsync(request), cancellationToken);
    /// <inheritdoc/>
    public async Task<DeletionBlockedReplacementResult?> ReadBlockedReplacementAsync(DeletionBatchCapabilityV1 capability, CancellationToken cancellationToken = default)
    {
        if (capability.TenantId != tenantId) { throw new ArgumentException("Private protection tenant mismatch."); }
        using var deadline = new AuthoritativeStreamReadDeadline(TimeSpan.FromSeconds(30), clock, cancellationToken, clock.GetTimestamp());
        var result = await deadline.ReadAsync(_ => proxies.CreateActorProxy<IDeletionConsumptionActor>(new(DeletionConsumptionActor.GetActorId(tenantId)), DeletionConsumptionActor.ActorTypeName)
            .ReadBlockedReplacementAsync(capability)).ConfigureAwait(false);
        deadline.ThrowIfCancellationRequested(); return result;
    }
    /// <inheritdoc/>
    public Task<DeletionConsumptionOutcome> LookupAsync'''))
p=ev/'src/Hexalith.EventStore.Server/Security/DeletionConsumptionActor.cs';s=p.read_text()
s=s.replace('''            replacementKeyVersion, KeyBlock(state, replacementKeyVersion) is not null, state.Revision);''','''            replacementKeyVersion, KeyBlock(state, replacementKeyVersion) is not null, state.Revision)
            { ReplacementKeyRevocation = KeyBlock(state, replacementKeyVersion) };''')
s=s.replace('checked(batch.Current.Capability.AttestationOrdinal + 1)', 'checked((batch.BlockedReplacement?.Capability.AttestationOrdinal ?? batch.Current.Capability.AttestationOrdinal) + 1)')
s=s.replace('owned.Replacement.Capability.CapabilityKeyVersion == batch.Current.Capability.CapabilityKeyVersion', 'owned.Replacement.Capability.CapabilityKeyVersion == (batch.BlockedReplacement?.Capability.CapabilityKeyVersion ?? batch.Current.Capability.CapabilityKeyVersion)')
s=s.replace('batch with { Current = owned.Replacement, Outcome = durable }', 'batch with { Current = owned.Replacement, Outcome = durable, BlockedReplacement = null }')
i=s.index('    private async Task<DeletionConsumptionOutcome> ActivateAsyncCoreAsync')
s=s[:i]+'''    /// <inheritdoc/>
    public async Task<DeletionConsumptionOutcome> ReconcileBlockedReplacementAsync(DeletionBlockedReplacementReconciliation request)
    {
        var owned = DeletionConsumptionIdentity.Capture(request); string tenant = owned.Capability.TenantId; string id = owned.Capability.BatchId; Check(tenant);
        if (!await AdmitAsync(tenant, id, "ReconcileBlockedDeletionReplacement").ConfigureAwait(false)) { return Unavailable(tenant, id); }
        var state = await ReadAsync(tenant, true).ConfigureAwait(false); string digest = DeletionConsumptionIdentity.Digest(owned);
        var prior = state.Operations.SingleOrDefault(o => o.OperationId == owned.OperationId);
        if (prior is not null)
        { return prior.RequestDigest == digest && await AdmitAsync(tenant, id, "ReconcileBlockedDeletionReplacement").ConfigureAwait(false) ? prior.Outcome : Result(state, id, DeletionConsumptionStatus.Conflict); }
        var batch = Find(state, id); var keyBlock = KeyBlock(state, owned.Capability.CapabilityKeyVersion);
        if (batch is null || batch.Outcome.Status != DeletionConsumptionStatus.ConsumptionBlocked || batch.Outcome.BlockReason != DeletionConsumptionBlockReason.CapabilityKeyCompromise
            || batch.Outcome.ReceiptId != owned.CompromiseBlockReceiptId || !DeletionConsumptionIdentity.SameBatch(batch.Original, owned)
            || owned.Capability.AttestationOrdinal != checked((batch.BlockedReplacement?.Capability.AttestationOrdinal ?? batch.Current.Capability.AttestationOrdinal) + 1)
            || owned.Capability.CapabilityKeyVersion == (batch.BlockedReplacement?.Capability.CapabilityKeyVersion ?? batch.Current.Capability.CapabilityKeyVersion)
            || owned.ExpectedKeyBlockSetRevision != state.KeyBlockSetRevision || keyBlock is null || DeletionConsumptionIdentity.Digest(keyBlock) != DeletionConsumptionIdentity.Digest(owned.RevocationReceipt))
        { return Result(state, id, DeletionConsumptionStatus.Conflict); }
        if (state.Operations.Count >= 10000 || authority is null || !await authority.VerifyBlockedReplacementAsync(owned).ConfigureAwait(false)) { return Unavailable(tenant, id); }
        var next = state with { Revision = checked(state.Revision + 1) };
        var outcome = Result(next, id, DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise) with {
            ReceiptId = DeletionConsumptionIdentity.Digest(new[] { owned.OperationId, digest, "blocked-issued-replacement" }), BlockReason = DeletionConsumptionBlockReason.CapabilityKeyCompromise,
            BlockedKeyVersion = owned.Capability.CapabilityKeyVersion, RevocationRevision = keyBlock.Envelope.RevocationRevision };
        next = Replace(next, batch with { BlockedReplacement = owned, Outcome = outcome with { Status = DeletionConsumptionStatus.ConsumptionBlocked } });
        next = next with { Operations = state.Operations.Append(new DeletionConsumptionOperation(owned.OperationId, digest, outcome)).ToArray() };
        await SaveAsync(next).ConfigureAwait(false);
        return await AdmitAsync(tenant, id, "ReconcileBlockedDeletionReplacement").ConfigureAwait(false) ? outcome : Unavailable(tenant, id);
    }
    /// <inheritdoc/>
    public async Task<DeletionBlockedReplacementResult?> ReadBlockedReplacementAsync(DeletionBatchCapabilityV1 capability)
    {
        Check(capability.TenantId); _ = DeletionBatchCapabilityIdentity.SigningRequestId(capability);
        if (!await AdmitAsync(capability.TenantId, capability.BatchId, "ReadBlockedDeletionReplacement").ConfigureAwait(false)) { return null; }
        var state = await ReadAsync(capability.TenantId).ConfigureAwait(false); var phase = Find(state, capability.BatchId)?.BlockedReplacement;
        if (phase is null || phase.Capability != capability) { return null; }
        var operation = state.Operations.SingleOrDefault(o => o.OperationId == phase.OperationId && o.RequestDigest == DeletionConsumptionIdentity.Digest(phase));
        var final = await ReadAsync(capability.TenantId).ConfigureAwait(false);
        return operation is not null && DeletionConsumptionIdentity.Digest(final) == DeletionConsumptionIdentity.Digest(state)
            && await AdmitAsync(capability.TenantId, capability.BatchId, "ReadBlockedDeletionReplacement").ConfigureAwait(false) ? new(phase, operation.Outcome) : null;
    }
'''+s[i:]
s=s.replace('Outcome = b.Outcome with { TargetReceipts = Array.AsReadOnly(b.Outcome.TargetReceipts.ToArray()) }', 'Outcome = b.Outcome with { TargetReceipts = Array.AsReadOnly(b.Outcome.TargetReceipts.ToArray()) }, BlockedReplacement = b.BlockedReplacement is null ? null : DeletionConsumptionIdentity.Capture(b.BlockedReplacement)')
s=s.replace('outcome.BlockedKeyVersion != b.Current.Capability.CapabilityKeyVersion', 'outcome.BlockedKeyVersion != (b.BlockedReplacement?.Capability.CapabilityKeyVersion ?? b.Current.Capability.CapabilityKeyVersion)')
s=s.replace('''            var outcome = b.Outcome; DeletionConsumptionIdentity.Text(outcome.ReceiptId!);''','''            var outcome = b.Outcome; DeletionConsumptionIdentity.Text(outcome.ReceiptId!);
            if (b.BlockedReplacement is { } phase && (outcome.Status != DeletionConsumptionStatus.ConsumptionBlocked
                || outcome.BlockReason != DeletionConsumptionBlockReason.CapabilityKeyCompromise || !DeletionConsumptionIdentity.SameBatch(b.Original, phase)
                || phase.Capability.AttestationOrdinal <= b.Current.Capability.AttestationOrdinal || phase.ExpectedKeyBlockSetRevision > state.KeyBlockSetRevision
                || !state.Revocations.Any(r => DeletionConsumptionIdentity.Digest(r) == DeletionConsumptionIdentity.Digest(phase.RevocationReceipt))
                || !state.Operations.Any(o => o.OperationId == phase.OperationId && o.RequestDigest == DeletionConsumptionIdentity.Digest(phase)
                    && o.Outcome.ReceiptId == outcome.ReceiptId && o.Outcome.Status == DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise)))
            { throw new InvalidOperationException("Malformed retained blocked replacement."); }''')
w(p,s)
# The guard retains original block while a replacement remains unactivated; no absent affected membership becomes a new batch proof.
edit(ev,'src/Hexalith.EventStore.Server/Security/GovernanceScopeGuardReducer.cs',lambda s:s.replace('item.ProtectionOutcome != "ConsumptionBlocked:CapabilityKeyCompromise"','item.ProtectionOutcome is not ("ConsumptionBlocked:CapabilityKeyCompromise" or "ReplacementAwaitingActivation")').replace('if (batch.ProtectionOutcome != "ReplacementAwaitingActivation" || command.Batch.BlockSetRevision <= batch.BlockSetRevision) { return Denied(); }','''if (batch.ProtectionOutcome != "ReplacementAwaitingActivation" || command.Batch.BlockSetRevision <= batch.BlockSetRevision
                        || !BlockedReplacementProof(state, batch, command.Batch, evidence)) { return Denied(); }''').replace('    private static bool OriginalTerminalProof', '''    private static bool BlockedReplacementProof(TenantGovernanceGuardState state, GovernanceBatchState batch, GovernanceBatchCommand command, GovernanceGuardEvidence evidence)
    {
        var retained = evidence.ProtectionBlockedReplacement; var phase = retained?.Original; var outcome = retained?.Outcome;
        return phase is not null && outcome is not null && phase.CompromiseBlockReceiptId == batch.ProtectionReceiptId
            && phase.GuardReplacementReceiptId == batch.IssueReceiptId && phase.Capability == batch.Capability && phase.DetachedJws == batch.DetachedJws
            && phase.SigningRequestId == batch.SigningRequestId && phase.CommittedIssuedGuardRevision == batch.IssuedGuardRevision
            && phase.ExpectedKeyBlockSetRevision == command.BlockSetRevision && phase.Targets.SequenceEqual(batch.Targets.Select(t => new ProtectionTarget(t.TenantId, t.AgentInteractionId, t.TargetProtectionKeyAlias)))
            && phase.RevocationReceipt.Envelope.TenantId == state.TenantId && phase.RevocationReceipt.Envelope.KeyVersion == batch.CapabilityKeyVersion
            && phase.RevocationReceipt.KeyBlockSetRevision <= phase.ExpectedKeyBlockSetRevision && state.Revocations.Any(r => Hash(r) == Hash(phase.RevocationReceipt))
            && outcome.TenantId == state.TenantId && outcome.BatchId == batch.BatchId && outcome.Status == DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise
            && outcome.OwnerRevision > 0 && outcome.KeyBlockSetRevision == phase.ExpectedKeyBlockSetRevision && outcome.ReceiptId == command.ProtectionReceiptId
            && outcome.BlockReason == DeletionConsumptionBlockReason.CapabilityKeyCompromise && outcome.BlockedKeyVersion == batch.CapabilityKeyVersion
            && outcome.RevocationRevision == phase.RevocationReceipt.Envelope.RevocationRevision && outcome.TargetReceipts.Count == 0;
    }
    private static bool OriginalTerminalProof'''))
# Capture new provider-owned vectors under the existing deadline before serialization.
edit(ev,'src/Hexalith.EventStore.Server/Security/GovernanceScopeGuardOwner.cs',lambda s:s.replace('            ProtectionActivationRequest =', '''            ProtectionBlockedReplacement = value.ProtectionBlockedReplacement is null ? null : value.ProtectionBlockedReplacement with {
                Original = value.ProtectionBlockedReplacement.Original with { Targets = List(value.ProtectionBlockedReplacement.Original.Targets),
                    RevocationReceipt = value.ProtectionBlockedReplacement.Original.RevocationReceipt with { AffectedBatchIds = List(value.ProtectionBlockedReplacement.Original.RevocationReceipt.AffectedBatchIds) } },
                Outcome = value.ProtectionBlockedReplacement.Outcome with { TargetReceipts = List(value.ProtectionBlockedReplacement.Outcome.TargetReceipts) } },
            ProtectionActivationRequest ='''))
edit(pl,'src/Hexalith.Platform.Custody/IDeletionBatchGuardPort.cs',lambda s:s.replace('    /// <summary>Authorizes then conditionally commits completion', '''    /// <summary>Mirrors only the independently proved original issued-but-blocked disposition; omission grants no dispatch or effect.</summary>
    Task<GovernanceProtocolReceipt?> RecordBlockedReplacementAsync(DeletionCapabilitySigningOutcome signed, DeletionBlockedReplacementResult retained, CancellationToken cancellationToken = default)
        => Task.FromResult<GovernanceProtocolReceipt?>(null);
    /// <summary>Authorizes then conditionally commits completion'''))
edit(pl,'src/Hexalith.Platform.Custody/EventStoreDeletionBatchGuardPort.cs',lambda s:s.replace('    /// <inheritdoc/>\n    public async Task<GovernanceProtocolReceipt?> CompleteAsync', '''    /// <inheritdoc/>
    public Task<GovernanceProtocolReceipt?> RecordBlockedReplacementAsync(DeletionCapabilitySigningOutcome signed, DeletionBlockedReplacementResult retained, CancellationToken cancellationToken = default)
        => retained.Original.Capability == signed.Payload && retained.Original.DetachedJws == signed.DetachedJws && retained.Original.SigningRequestId == signed.SigningRequestId
            ? RecordProtectionAsync(signed, retained.Outcome, true, cancellationToken) : Task.FromResult<GovernanceProtocolReceipt?>(null);
    /// <inheritdoc/>
    public async Task<GovernanceProtocolReceipt?> CompleteAsync'''))
# Reconcile before dispatch, resolving only exact original phase after lost acknowledgement.
p=pl/'src/Hexalith.Platform.Custody/DeletionBatchExecutionCoordinator.cs';s=p.read_text();needle='''            var dispatch = await WaitAsync(() => guard.DispatchAsync(signed, providerCancellation.Token)).ConfigureAwait(false);'''
s=s.replace(needle,'''            IDeletionProtectionOwner? protection = null;
            if (replacement)
            {
                var pendingReplacement = await WaitAsync(() => guard.ReadAsync(payload, providerCancellation.Token)).ConfigureAwait(false);
                if (pendingReplacement?.Batch.ProtectionOutcome == "ReplacementAwaitingActivation")
                {
                    protection = await deadline.ReadAsync(() => Task.FromResult(protectionOwners(payload.TenantId))).ConfigureAwait(false);
                    var retained = await WaitAsync(() => protection.ReadBlockedReplacementAsync(payload, providerCancellation.Token)).ConfigureAwait(false);
                    if (retained is null)
                    {
                        var comparison = await WaitAsync(() => protection.ReadActivationComparisonAsync(payload.TenantId, payload.BatchId, payload.CapabilityKeyVersion, providerCancellation.Token)).ConfigureAwait(false);
                        if (comparison?.ReplacementKeyBlocked == true)
                        {
                            if (comparison.CompromiseBlockReceiptId != pendingReplacement.Batch.ProtectionReceiptId || comparison.TenantId != payload.TenantId
                                || comparison.BatchId != payload.BatchId || comparison.ReplacementKeyVersion != payload.CapabilityKeyVersion || comparison.OwnerRevision <= 0
                                || comparison.KeyBlockSetRevision < pendingReplacement.Batch.BlockSetRevision || comparison.ReplacementKeyRevocation is null)
                            { return new("ReplacementComparisonUnavailable", signed); }
                            var phase = new DeletionBlockedReplacementReconciliation("blocked-replacement-" + id + "-compare-" + comparison.KeyBlockSetRevision.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                pendingReplacement.Batch.ProtectionReceiptId, comparison.KeyBlockSetRevision, pendingReplacement.Batch.IssueReceiptId, payload, signed.DetachedJws!, id,
                                pendingReplacement.Batch.IssuedGuardRevision, pendingReplacement.Batch.Targets.Select(t => new ProtectionTarget(t.TenantId, t.AgentInteractionId, t.TargetProtectionKeyAlias)).ToArray(), comparison.ReplacementKeyRevocation);
                            try { _ = await WaitAsync(() => protection.ReconcileBlockedReplacementAsync(phase, providerCancellation.Token)).ConfigureAwait(false); }
                            catch (Exception) { deadline.Check(); }
                            retained = await WaitAsync(() => protection.ReadBlockedReplacementAsync(payload, providerCancellation.Token)).ConfigureAwait(false);
                            if (retained is null) { return new("ReplacementReconciliationUnknown", signed); }
                        }
                    }
                    if (retained is not null)
                    {
                        if (retained.Original.Capability != payload || retained.Original.SigningRequestId != id || retained.Original.DetachedJws != signed.DetachedJws
                            || retained.Original.CompromiseBlockReceiptId != pendingReplacement.Batch.ProtectionReceiptId || retained.Original.GuardReplacementReceiptId != pendingReplacement.Batch.IssueReceiptId
                            || retained.Original.CommittedIssuedGuardRevision != pendingReplacement.Batch.IssuedGuardRevision || retained.Outcome.Status != DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise
                            || !Exact(retained.Outcome)) { return new("ReplacementReconciliationUnverified", signed); }
                        var mirroredBlock = await WaitAsync(() => guard.RecordBlockedReplacementAsync(signed, retained, providerCancellation.Token)).ConfigureAwait(false);
                        deadline.Check();
                        return new(mirroredBlock?.Status == "Committed" ? "ActivationBlockedByReplacementKeyCompromise" : "ReplacementBlockMirrorUnknown", signed, retained.Outcome, mirroredBlock);
                    }
                }
            }
'''+needle)
s=s.replace('            var protection = await deadline.ReadAsync(() => Task.FromResult(protectionOwners(payload.TenantId))).ConfigureAwait(false);', '            protection ??= await deadline.ReadAsync(() => Task.FromResult(protectionOwners(payload.TenantId))).ConfigureAwait(false);')
w(p,s)
