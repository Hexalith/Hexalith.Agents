from pathlib import Path
p=Path('/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/DeletionBatchExecutionCoordinatorTests.cs');s=p.read_text();a=s.index('    public async Task ActualIssuedReplacementRevocationCanReconcileAndAdvanceHealthyNext');beg=s.index('        using var f = ',a);end=s.index('        await f.RestartSerializedOwnersAsync();',beg);setup=s[beg:end].replace('if (dispatched)', 'if (false)')
i=s.rfind('\n}')
s=s[:i]+'''
    /// <summary>Omitted/foreign/stale/forged independently supplied reconciliation proof cannot change either actual owner's original ledger.</summary>
    [Theory]
    [InlineData("omitted")][InlineData("foreign")][InlineData("stale")][InlineData("forged")][InlineData("foreign-block")][InlineData("foreign-issue")]
    public async Task InvalidBlockedReplacementProofPreservesOriginalLedgers(string vector)
    {
'''+setup+'''
        var comparison = (await f.ProtectionActor.ReadActivationComparisonAsync("tenant-a", replacement.BatchId, replacement.CapabilityKeyVersion))!;
        var phase = new DeletionBlockedReplacementReconciliation("blocked-original", block, comparison.KeyBlockSetRevision, pending.IssueReceiptId,
            replacement, second.DetachedJws!, second.SigningRequestId, pending.IssuedGuardRevision,
            pending.Targets.Select(t => new ProtectionTarget(t.TenantId, t.AgentInteractionId, t.TargetProtectionKeyAlias)).ToArray(), comparison.ReplacementKeyRevocation!);
        phase = vector switch { "foreign" => phase with { Capability = phase.Capability with { DestructionSealId = "foreign-seal" }, SigningRequestId = DeletionBatchCapabilityIdentity.SigningRequestId(phase.Capability with { DestructionSealId = "foreign-seal" }) },
            "stale" => phase with { ExpectedKeyBlockSetRevision = phase.ExpectedKeyBlockSetRevision + 1 },
            "forged" => phase with { RevocationReceipt = phase.RevocationReceipt with { ReceiptId = "forged-proof" } },
            "foreign-block" => phase with { CompromiseBlockReceiptId = "foreign-block" }, "foreign-issue" => phase with { GuardReplacementReceiptId = "foreign-issue" }, _ => phase };
        if (vector == "omitted") { f.ProtectionAuthority.VerifyBlockedReplacementAsync(Arg.Any<DeletionBlockedReplacementReconciliation>(), Arg.Any<CancellationToken>()).Returns(false); }
        byte[] guardBefore = JsonSerializer.SerializeToUtf8Bytes(f.State); byte[] ownerBefore = JsonSerializer.SerializeToUtf8Bytes(f.ProtectionState.CommittedState.Single().Value);
        if (vector == "forged") { await Should.ThrowAsync<ArgumentException>(() => f.ProtectionActor.ReconcileBlockedReplacementAsync(phase)); }
        else { (await f.ProtectionActor.ReconcileBlockedReplacementAsync(phase)).Status.ShouldNotBe(DeletionConsumptionStatus.ActivationBlockedByReplacementKeyCompromise); }
        JsonSerializer.SerializeToUtf8Bytes(f.State).ShouldBe(guardBefore); JsonSerializer.SerializeToUtf8Bytes(f.ProtectionState.CommittedState.Single().Value).ShouldBe(ownerBefore);
        f.Reservations.ShouldBe(0); f.State.Deletions.Single().Batches.Single().ProtectionReceiptId.ShouldBe(block);
    }
'''+s[i:];s=s.replace('''        if (false) { (await port.DispatchAsync(second, TestContext.Current.CancellationToken))!.Status.ShouldBe("Committed"); }
''','')
p.write_bytes(s.replace('\n','\r\n').encode())
