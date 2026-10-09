from pathlib import Path
p=Path('/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/DeletionBatchActualOwnerFixture.cs');s=p.read_text()
s=s.replace('internal DeletionConsumptionActor ProtectionActor { get; }','internal DeletionConsumptionActor ProtectionActor { get; private set; }')
s=s.replace('    internal int Signatures', '    private readonly IAtomicDeletionManifestProvider _protectionProvider;\n    internal bool LoseBlockedReconciliationAcknowledgement { get; set; }\n    internal int Signatures')
s=s.replace('''        protectionAuthority.AuthorizeOperationAsync("tenant-a", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
            !(UnknownConsumptionLookup && call.ArgAt<string>(2) == "LookupDeletionBatch"));''','''        protectionAuthority.AuthorizeOperationAsync("tenant-a", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (LoseBlockedReconciliationAcknowledgement && call.ArgAt<string>(2) == "ReconcileBlockedDeletionReplacement" && ReadProtectionBlockedReplacement(call.ArgAt<string>(1)) is not null)
            { LoseBlockedReconciliationAcknowledgement = false; throw new IOException("Controlled lost durable blocked-replacement acknowledgement."); }
            return !(UnknownConsumptionLookup && call.ArgAt<string>(2) == "LookupDeletionBatch");
        });''')
s=s.replace('''        protectionAuthority.VerifyRevocationAsync''','''        protectionAuthority.VerifyBlockedReplacementAsync(Arg.Any<DeletionBlockedReplacementReconciliation>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var phase = call.Arg<DeletionBlockedReplacementReconciliation>(); var batch = State.Deletions.Single().Batches.Single();
            return batch.ProtectionOutcome == "ReplacementAwaitingActivation" && batch.Capability == phase.Capability && batch.DetachedJws == phase.DetachedJws
                && batch.SigningRequestId == phase.SigningRequestId && batch.IssuedGuardRevision == phase.CommittedIssuedGuardRevision
                && batch.IssueReceiptId == phase.GuardReplacementReceiptId && batch.ProtectionReceiptId == phase.CompromiseBlockReceiptId
                && batch.Targets.Select(t => new ProtectionTarget(t.TenantId, t.AgentInteractionId, t.TargetProtectionKeyAlias)).SequenceEqual(phase.Targets)
                && State.Revocations.Any(r => Hash(r) == Hash(phase.RevocationReceipt))
                && DeletionBatchCapabilityCodec.Verify(phase.Capability, new("issuer-a", "protection-a", "tenant-a", phase.Capability.CapabilityKeyVersion, "anchor-a", "v1"), phase.DetachedJws, Key);
        });
        protectionAuthority.VerifyRevocationAsync''')
s=s.replace('var provider = Substitute.For<IAtomicDeletionManifestProvider>();','var provider = _protectionProvider = Substitute.For<IAtomicDeletionManifestProvider>();')
s=s.replace('''ProtectionActivationOutcome = LatestActivation is null ? null : ReadProtectionOutcome(LatestActivation.Replacement.Capability.BatchId) };''', '''ProtectionActivationOutcome = LatestActivation is null ? null : ReadProtectionOutcome(LatestActivation.Replacement.Capability.BatchId),
                ProtectionBlockedReplacement = command.Batch is { } blocked ? ReadProtectionBlockedReplacement(blocked.BatchId) : null };''')
i=s.index('    private bool VerifyDispatch(')
s=s[:i]+'''    internal DeletionBlockedReplacementResult? ReadProtectionBlockedReplacement(string batchId)
    {
        var ledger = ProtectionState.CommittedState.Values.SingleOrDefault(value => value.GetType().Name == "DeletionConsumptionLedger");
        if (ledger is null) { return null; }
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(ledger));
        var batch = document.RootElement.GetProperty("Batches").EnumerateArray().SingleOrDefault(value => value.GetProperty("Current").GetProperty("Capability").GetProperty("BatchId").GetString() == batchId);
        if (batch.ValueKind == JsonValueKind.Undefined || !batch.TryGetProperty("BlockedReplacement", out var retained) || retained.ValueKind == JsonValueKind.Null) { return null; }
        var phase = retained.Deserialize<DeletionBlockedReplacementReconciliation>()!;
        var operation = document.RootElement.GetProperty("Operations").EnumerateArray().Single(value => value.GetProperty("OperationId").GetString() == phase.OperationId);
        return new(phase, operation.GetProperty("Outcome").Deserialize<DeletionConsumptionOutcome>()!);
    }
    internal async Task RestartSerializedOwnersAsync()
    {
        foreach (var state in _signerStates.Values.Append(ProtectionState))
        {
            await state.ClearCacheAsync();
            foreach (var value in state.CommittedState.ToArray())
            { await state.SetStateAsync(value.Key, JsonSerializer.Deserialize(JsonSerializer.SerializeToUtf8Bytes(value.Value), value.Value.GetType())!); }
            await state.SaveStateAsync(); await state.ClearCacheAsync();
        }
        ProtectionActor = new(ActorHost.CreateForTest<DeletionConsumptionActor>(new ActorTestOptions { ActorId = new(DeletionConsumptionActor.GetActorId("tenant-a")) }), ProtectionAuthority, _protectionProvider);
        InstallStateManager(ProtectionActor, ProtectionState);
    }
'''+s[i:]
p.write_bytes(s.replace('\n','\r\n').encode())
p=Path('/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/DeletionBatchExecutionCoordinatorTests.cs');s=p.read_text();s=s.replace('[InlineData(false)][InlineData(true)]\n    public async Task ActualIssuedReplacementRevocationCanReconcileAndAdvanceHealthyNext(bool dispatched)', '[InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]\n    public async Task ActualIssuedReplacementRevocationCanReconcileAndAdvanceHealthyNext(bool dispatched, bool loseAcknowledgement)')
s=s.replace('''        var result = await f.Coordinator.ExecuteAsync(replacement, true, TestContext.Current.CancellationToken);
        result.Status.ShouldBe("ActivationBlockedByReplacementKeyCompromise"); f.Reservations.ShouldBe(0);''','''        await f.RestartSerializedOwnersAsync(); f.LoseBlockedReconciliationAcknowledgement = loseAcknowledgement;
        var result = await f.Coordinator.ExecuteAsync(replacement, true, TestContext.Current.CancellationToken);
        result.Status.ShouldBe("ActivationBlockedByReplacementKeyCompromise"); f.Reservations.ShouldBe(0);
        var retained = f.ReadProtectionBlockedReplacement(replacement.BatchId)!;
        retained.Original.Capability.ShouldBe(replacement); retained.Original.CompromiseBlockReceiptId.ShouldBe(block);
        retained.Original.CommittedIssuedGuardRevision.ShouldBe(pending.IssuedGuardRevision); retained.Outcome.ReceiptId.ShouldBe(result.Protection!.ReceiptId);
        await f.RestartSerializedOwnersAsync();''')
p.write_bytes(s.replace('\n','\r\n').encode())
