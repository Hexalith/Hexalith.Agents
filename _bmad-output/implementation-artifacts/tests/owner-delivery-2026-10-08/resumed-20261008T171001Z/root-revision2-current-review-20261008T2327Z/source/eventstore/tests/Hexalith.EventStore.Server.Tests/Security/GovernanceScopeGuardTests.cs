using System.Text.Json;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Server.Security;
using NSubstitute;
using Shouldly;

namespace Hexalith.EventStore.Server.Tests.Security;

/// <summary>Real typed source owner/reducer with serialized modeled multi-cell CAS. Tests prove technical state transitions, never actual production backend/issuer/protection qualification.</summary>
public sealed class GovernanceScopeGuardTests
{
    /// <summary>Every concrete writer is fenced at repair, only its exact finite cohort work drains, and successor activation retains prior guard evidence while revoking the old epoch.</summary>
    [Theory]
    [InlineData(DirectoryWriteKind.Create)][InlineData(DirectoryWriteKind.ContentAppend)][InlineData(DirectoryWriteKind.Permit)]
    [InlineData(DirectoryWriteKind.CreationOutbox)][InlineData(DirectoryWriteKind.UserActionIntent)][InlineData(DirectoryWriteKind.LeaseAcquire)]
    [InlineData(DirectoryWriteKind.LeaseCommit)][InlineData(DirectoryWriteKind.RateAuthorization)][InlineData(DirectoryWriteKind.OpenAuthorization)]
    [InlineData(DirectoryWriteKind.BudgetAuthorization)][InlineData(DirectoryWriteKind.CapacityAuthorization)][InlineData(DirectoryWriteKind.OriginalRecoveryResult)]
    public async Task FiniteRepairFencesEveryWriterAndSuccessorPreservesExactHistory(DirectoryWriteKind kind)
    {
        var fixture = new GovernanceGuardFixture(); var owner = new AggregateIdentity("tenant-a", "directory", "directory-a");
        var cohort = new DirectoryRepairCohortItem(owner, "original-work", kind, "conversation-a", "permit-a", "effect-a", 1, "Authorized");
        var install = fixture.Command(GovernanceGuardOperation.InstallRepairFence);
        install = install with { Repair = new("tenant-a", install.OperationId, "epoch-a", install.ExpectedGuardRevision, "post-fence-checkpoint", "repair-revocation", [cohort]) };
        (await fixture.Apply(install)).Status.ShouldBe("Committed");
        var mutation = new GuardedStateMutation("source-a", 0, GuardedTransactionFixture.Hash([]), "sealed-content"u8.ToArray());
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AppendWrite, write: fixture.Facts(kind)), [mutation])).Status.ShouldBe("Blocked");
        fixture.Backend.Stored.ContainsKey(fixture.Backend.Key("source-a")).ShouldBeFalse();
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AppendWrite, write: fixture.Facts(kind), reference: "original-work"), [mutation])).Status.ShouldBe("Accepted");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.ActivateSuccessor) with { EpochId = "epoch-b" })).Status.ShouldBe("Blocked");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordBridgeDrain, reference: "original-work"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AppendWrite, write: fixture.Facts(kind), reference: "original-work"), [mutation with { CellId = "after-drain" }])).Status.ShouldBe("Blocked");
        fixture.Backend.Stored.ContainsKey(fixture.Backend.Key("after-drain")).ShouldBeFalse();
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.ActivateSuccessor) with { EpochId = "epoch-b" })).Status.ShouldBe("Committed");
        fixture.State.Repair.ShouldBeNull(); fixture.State.RepairHistory.Single().Cohort.Single().ShouldBe(cohort);
        var late = fixture.Command(GovernanceGuardOperation.AppendWrite, write: fixture.Facts(kind) with { EpochId = "epoch-a" }) with { EpochId = "epoch-a" };
        (await fixture.Apply(late, [mutation with { CellId = "late-source" }])).Status.ShouldBe("Blocked");
        fixture.Backend.Stored.ContainsKey(fixture.Backend.Key("late-source")).ShouldBeFalse();
    }

    /// <summary>The append guard stamps accepted source writes and later matching admission/content categories receive no source effect.</summary>
    [Theory]
    [InlineData(DirectoryWriteKind.Create)][InlineData(DirectoryWriteKind.ContentAppend)][InlineData(DirectoryWriteKind.Permit)]
    [InlineData(DirectoryWriteKind.CreationOutbox)][InlineData(DirectoryWriteKind.UserActionIntent)][InlineData(DirectoryWriteKind.LeaseAcquire)]
    [InlineData(DirectoryWriteKind.LeaseCommit)][InlineData(DirectoryWriteKind.RateAuthorization)][InlineData(DirectoryWriteKind.OpenAuthorization)]
    [InlineData(DirectoryWriteKind.BudgetAuthorization)][InlineData(DirectoryWriteKind.CapacityAuthorization)]
    public async Task AcceptedAttributionAndInstalledFencesAreJointWithEverySourceWrite(DirectoryWriteKind kind)
    {
        var fixture = new GovernanceGuardFixture(); await fixture.PrepareBoundAsync();
        fixture.OriginalAcceptance!.GuardHighWater.ShouldBe(2); fixture.OriginalAcceptance.AcceptedAtAdmissionFenceOrdinal.ShouldBe(0);
        var command = fixture.Command(GovernanceGuardOperation.AppendWrite, write: fixture.Facts(kind));
        long prior = fixture.State.Revision;
        (await fixture.Apply(command, [new("fenced-source", 0, GuardedTransactionFixture.Hash([]), "sealed-content"u8.ToArray())])).Status.ShouldBe("Blocked");
        fixture.State.Revision.ShouldBe(prior); fixture.Backend.Stored.ContainsKey(fixture.Backend.Key("fenced-source")).ShouldBeFalse();
    }

    /// <summary>Scope, owner-obligation and candidate cut/token substitutions cannot reuse an authorization at the identical guard revision.</summary>
    [Theory]
    [InlineData("scope")][InlineData("owners")][InlineData("obligations")][InlineData("cut")][InlineData("token")]
    public async Task AuthorizationCannotChangeItsExactEffectBasis(string vector)
    {
        var fixture = new GovernanceGuardFixture(); GovernanceGuardTransition effect;
        if (vector is "cut" or "token")
        {
            await fixture.PrepareBoundAsync(); var deletion = fixture.State.Deletions.Single(); var latest = deletion.ContentBindings.Single();
            (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: fixture.Ordinal(violation: "recut", invalidated: [latest.GlobalCutId, latest.TokenId])))).Status.ShouldBe("Committed");
            await fixture.EffectiveAsync(); var ordinal = fixture.Ordinal(2, "cut-b", "token-b");
            (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeContentBinding, ordinal: ordinal, authorization: "binding-b"))).Status.ShouldBe("Committed");
            effect = fixture.Command(GovernanceGuardOperation.CommitContentBinding, ordinal: ordinal with { GlobalCutId = vector == "cut" ? "substituted-cut" : ordinal.GlobalCutId,
                TokenId = vector == "token" ? "substituted-token" : ordinal.TokenId }, authorization: "binding-b");
        }
        else
        {
            (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeAdmissionFence, scope: fixture.Scope, authorization: "admission-a"))).Status.ShouldBe("Committed");
            effect = fixture.Command(GovernanceGuardOperation.CommitAdmissionFence, scope: vector == "scope" ? fixture.Scope with { SourceConversationId = "other-conversation" } : fixture.Scope, authorization: "admission-a");
            if (vector == "owners") { fixture.ChangeEvidence = (proof, _) => proof with { RequiredOwnerIds = ["substituted-owner"] }; }
            if (vector == "obligations") { fixture.ChangeEvidence = (proof, _) => proof with { ObligationIds = ["substituted-obligation"] }; }
        }
        long prior = fixture.State.Revision;
        (await fixture.Apply(effect)).Status.ShouldBe("Blocked"); fixture.State.Revision.ShouldBe(prior);
    }

    /// <summary>Pre-cut explicit no-cut recut and later complete gap chain preserve prior immutable Effective cycles; overtaken authorization terminalizes without a guard append.</summary>
    [Fact]
    public async Task RepeatedRecutsRetainViolationsAndContinuousBindingWithObsoleteOriginalOutcome()
    {
        var fixture = new GovernanceGuardFixture(); await fixture.PrepareBoundAsync();
        var firstBinding = fixture.State.Deletions.Single().ContentBindings.Single(); var firstCycles = fixture.State.Deletions.Single().OwnerCycles.ToArray();
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: fixture.Ordinal(violation: "violation-a", invalidated: [firstBinding.GlobalCutId, firstBinding.TokenId])))).Status.ShouldBe("Committed");
        await fixture.EffectiveAsync(); var second = fixture.Ordinal(2, "cut-b", "token-b");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeContentBinding, ordinal: second, authorization: "binding-b"))).Status.ShouldBe("Committed");
        var overtaken = fixture.Command(GovernanceGuardOperation.CommitContentBinding, ordinal: second, authorization: "binding-b");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: fixture.Ordinal(2, violation: "violation-b", invalidated: [firstBinding.GlobalCutId, firstBinding.TokenId])))).Status.ShouldBe("Committed");
        long beforeObsolete = fixture.State.Revision;
        var original = await fixture.Apply(overtaken); original.Status.ShouldBe("Obsolete"); fixture.State.Revision.ShouldBe(beforeObsolete);
        await fixture.EffectiveAsync(); await fixture.BindAsync("binding-c", "cut-c", "token-c");
        var current = fixture.State.Deletions.Single(); current.Ordinal.ShouldBe(3); current.Violations.Count.ShouldBe(2);
        current.ContentBindings.Count.ShouldBe(2); current.ContentBindings.Last().PredecessorBindingId.ShouldBe(firstBinding.BindingId);
        current.OwnerCycles.Take(2).ShouldBe(firstCycles);
        fixture.DenyEffects = true;
        (await new GovernanceScopeGuardOwner(fixture.Backend.Owner, TimeProvider.System, fixture.Authority).ExecuteAsync(overtaken, [], TestContext.Current.CancellationToken)).ShouldBe(original);
    }

    /// <summary>Hold registration wins after barrier authorization; stale compare is immutable, Open precedence blocks reauthorization, and exact acknowledged release permits a new irreversible seal.</summary>
    [Fact]
    public async Task HoldAndSealSerializeAtConditionalEffectWithExactStaleLookup()
    {
        var fixture = new GovernanceGuardFixture(); await fixture.PrepareBoundAsync(); var batch = fixture.Batch();
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeDestructionStart, batch: batch, authorization: "seal-a"))).Status.ShouldBe("Committed");
        var stale = fixture.Command(GovernanceGuardOperation.CommitDestructionStart, batch: batch, authorization: "seal-a");
        var hold = new GovernanceHoldCommand("hold-a", fixture.Scope, "policy-a", "");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RegisterHold, hold: hold))).Status.ShouldBe("Committed");
        long prior = fixture.State.Revision; fixture.Backend.LoseAcknowledgement = true;
        var result = await fixture.Apply(stale); result.Status.ShouldBe("Stale"); fixture.State.Revision.ShouldBe(prior); fixture.State.Deletions.Single().SealId.ShouldBeEmpty();
        (await fixture.Apply(stale)).ShouldBe(result);
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeDestructionStart, batch: batch, authorization: "seal-blocked"))).Status.ShouldBe("Blocked");
        hold = hold with { ReleaseReceiptId = "fully-acknowledged-release" };
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.ReleaseHold, hold: hold))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeDestructionStart, batch: batch, authorization: "seal-b"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.CommitDestructionStart, batch: batch, authorization: "seal-b"))).Status.ShouldBe("Committed");
        fixture.State.Deletions.Single().SealId.ShouldNotBeEmpty();
    }

    /// <summary>A complete post-seal content target gains one singleton containment batch; repeated resources link to coverage, all-target outcomes permit conditional completion.</summary>
    [Fact]
    public async Task PostSealContentContainmentCanCompleteWithoutReopeningAcceptedOwnerCycles()
    {
        var fixture = new GovernanceGuardFixture(); await fixture.PrepareBoundAsync(seal: true);
        var acceptedCycles = fixture.State.Deletions.Single().OwnerCycles.ToArray(); var obligations = fixture.State.Deletions.Single().ObligationIds.ToArray();
        await fixture.IssueConsumeAsync(fixture.Batch(), "accepted");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: fixture.Ordinal(violation: "content-a", resource: "resource-a")))).Status.ShouldBe("Committed");
        var containment = fixture.Batch("Containment", 1, [fixture.ViolationTarget]);
        var substituted = containment with { Targets = [fixture.ViolationTarget with { TargetProtectionKeyAlias = "foreign-alias" }] };
        substituted = substituted with { ManifestDigest = GovernanceScopeGuardReducer.ManifestDigest(substituted.Targets) };
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeContainment, batch: substituted, authorization: "wrong-target", reference: "resource-a"))).Status.ShouldBe("Blocked");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeContainment, batch: containment, authorization: "containment-a", reference: "resource-a"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.CommitContainment, batch: containment, authorization: "containment-a"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeCompletion, authorization: "incomplete"))).Status.ShouldBe("Blocked");
        await fixture.IssueConsumeAsync(containment, "containment");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: fixture.Ordinal(violation: "content-b", resource: "resource-b")))).Status.ShouldBe("Committed");
        var linked = await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeContainment, batch: containment, authorization: "duplicate-target", reference: "resource-b"));
        linked.Status.ShouldBe("CoverageLinked"); linked.ReferenceId.ShouldBe(containment.BatchId);
        fixture.State.Deletions.Single().Batches.Count.ShouldBe(2); fixture.State.Deletions.Single().OwnerCycles.ShouldBe(acceptedCycles);
        fixture.State.Deletions.Single().ObligationIds.ShouldBe(obligations);
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeCompletion, authorization: "complete-a"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.CommitCompletion, authorization: "complete-a"))).Status.ShouldBe("CompletionSealed");
        fixture.State.Deletions.Single().Completed.ShouldBeTrue();
    }

    /// <summary>Emergency block forbids old-key dispatch; exact blocked same-batch replacement receives successor dispatch and expected-block activation before all-target completion.</summary>
    [Fact]
    public async Task CompromiseAndReplacementRemainExactSameBatchWithoutClearingGlobalBlock()
    {
        var fixture = new GovernanceGuardFixture(); await fixture.PrepareBoundAsync(seal: true); var batch = fixture.Attest(fixture.Batch());
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordBatchIssued, batch: batch))).Status.ShouldBe("Committed");
        var revocation = new DeletionCapabilityRevocationReceipt(new("qualified-issuer", "protection-audience", "tenant-a", "DeletionBatchCapabilitySigningKey", "healthy-key-a", 1, 1, "revocation-a", new string('A', 64)), 1, 1, "exact-protection-block", [batch.BatchId]);
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordKeyCompromise, batch: batch with { BlockSetRevision = 1 }) with { RevocationReceipt = revocation })).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeDispatch, batch: batch, authorization: "old-dispatch"))).Status.ShouldBe("Blocked");
        var replacement = fixture.Attest(batch with { AttestationOrdinal = 2, CapabilityKeyVersion = "healthy-key-b", BlockSetRevision = 1 });
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.ReplaceAttestation, batch: replacement))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeDispatch, batch: replacement, authorization: "new-dispatch"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.CommitDispatch, batch: replacement, authorization: "new-dispatch"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordProtectionOutcome, batch: replacement with { ProtectionOutcome = "Activated", ProtectionReceiptId = "activation-a" }))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordProtectionOutcome, batch: replacement with { ProtectionOutcome = "Consumed", ProtectionReceiptId = "destroyed-a", TargetReceiptIds = ["target-a"] }))).Status.ShouldBe("Committed");
        fixture.State.CompromisedKeyVersions.ShouldContain("healthy-key-a"); fixture.State.Deletions.Single().Batches.Single().BatchId.ShouldBe(batch.BatchId);
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeCompletion, authorization: "complete-a"))).Status.ShouldBe("Committed");
        var completion = fixture.Command(GovernanceGuardOperation.CommitCompletion, authorization: "complete-a");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RegisterHold, hold: new("later-hold", fixture.Scope, "policy-a", "")))).Status.ShouldBe("Committed");
        fixture.State.Holds.Single().PostStartAttempt.ShouldBeTrue();
        (await fixture.Apply(completion)).Status.ShouldBe("Stale"); fixture.State.Deletions.Single().Completed.ShouldBeFalse();
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeCompletion, authorization: "complete-open"))).Status.ShouldBe("Blocked");
    }

    /// <summary>An attributed accepted admission after sealing requires owner block evidence, records integrity compromise and permanently excludes completion or new containment batches.</summary>
    [Fact]
    public async Task PostSealAdmissionCannotBecomeContentContainmentOrSuccessfulCompletion()
    {
        var fixture = new GovernanceGuardFixture(); await fixture.PrepareBoundAsync(seal: true, kind: DirectoryWriteKind.Permit);
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: fixture.Ordinal(violation: "admission-breach", kind: "Admission")))).Status.ShouldBe("Committed");
        fixture.State.Deletions.Single().IntegrityCompromised.ShouldBeTrue();
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeCompletion, authorization: "complete-a"))).Status.ShouldBe("Blocked");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeContainment, batch: fixture.Batch("Containment", 1, [fixture.ViolationTarget]), authorization: "containment-a", reference: "resource-b"))).Status.ShouldBe("Blocked");
    }

    /// <summary>A hostile false Count cannot reach authority or transaction; unsupported and cross-tenant source facts also fail before private lookup.</summary>
    [Fact]
    public async Task MalformedScopeAndFalseCountNeverReachPrivateAuthorityOrPersistence()
    {
        var fixture = new GovernanceGuardFixture(); var write = fixture.Command(GovernanceGuardOperation.AppendWrite, write: fixture.Facts());
        (await fixture.Owner.ExecuteAsync(write, new FalseCountGuardMutations(), TestContext.Current.CancellationToken)).ShouldBeNull();
        (await fixture.Owner.ExecuteAsync(write with { Write = fixture.Facts() with { FirstEventSourceConversationId = "foreign-conversation" } }, [], TestContext.Current.CancellationToken)).ShouldBeNull();
        (await fixture.Owner.ExecuteAsync(fixture.Command(GovernanceGuardOperation.AuthorizeAdmissionFence, scope: fixture.Scope with { Kind = "ClassUtcRange" }, authorization: "unsupported"), [], TestContext.Current.CancellationToken)).ShouldBeNull();
        (await fixture.Owner.ExecuteAsync(write with { OperationId = "invalid-\uD800" }, [], TestContext.Current.CancellationToken)).ShouldBeNull();
        (await fixture.Owner.ExecuteAsync(fixture.Command(GovernanceGuardOperation.AuthorizeAdmissionFence, scope: fixture.Scope with { SourceConversationId = "invalid-\uD800" }, authorization: "malformed"), [], TestContext.Current.CancellationToken)).ShouldBeNull();
        Should.Throw<ArgumentException>(() => GovernanceScopeGuardReducer.PredicateDigest(fixture.Scope with { SourceConversationId = "invalid-\uD800" }));
        GovernanceScopeGuardReducer.PredicateDigest(fixture.Scope with { SourceConversationId = "caf\u00e9" }).ShouldNotBe(GovernanceScopeGuardReducer.PredicateDigest(fixture.Scope with { SourceConversationId = "cafe\u0301" }));
        fixture.Authority.ReceivedCalls().ShouldBeEmpty(); fixture.Backend.TransactionCalls.ShouldBe(0);
        (await new GovernanceScopeGuardOwner(fixture.Backend.Owner, TimeProvider.System).ExecuteAsync(write, [], TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>The first tokenless recut requires the exact no-cut receipt, carries prior obligations and certifies its complete gap before the first stable content binding.</summary>
    [Fact]
    public async Task FirstTokenlessViolationRequiresExactNoCutProofAndNewEffectiveCycles()
    {
        var fixture = new GovernanceGuardFixture();
        fixture.OriginalAcceptance = await fixture.Apply(fixture.Command(GovernanceGuardOperation.AppendWrite, write: fixture.Facts()), [new("original-source", 0, GuardedTransactionFixture.Hash([]), "sealed"u8.ToArray())]);
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.AuthorizeAdmissionFence, scope: fixture.Scope, authorization: "admission-a"))).Status.ShouldBe("Committed");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.CommitAdmissionFence, scope: fixture.Scope, authorization: "admission-a"))).Status.ShouldBe("Committed");
        var tokenless = fixture.Ordinal(violation: "tokenless-a", noCut: "");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: tokenless))).Status.ShouldBe("Blocked");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: tokenless with { NoCutReceiptId = "wrong-no-cut" }))).Status.ShouldBe("Blocked");
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordViolation, ordinal: tokenless with { NoCutReceiptId = "authenticated-no-cut" }))).Status.ShouldBe("Committed");
        fixture.State.Deletions.Single().Ordinal.ShouldBe(2); fixture.State.Deletions.Single().ContentBindings.ShouldBeEmpty();
        await fixture.EffectiveAsync(); await fixture.BindAsync("first-binding", "first-cut", "first-token");
        fixture.State.Deletions.Single().ContentBindings.Single().Ordinal.ShouldBe(2);
        fixture.State.Deletions.Single().Violations.Single().NoCutReceiptId.ShouldBe("authenticated-no-cut");
    }

    /// <summary>The actual shared canonical identity binds every issued artifact field and separates intended from successful issue revision.</summary>
    [Theory]
    [InlineData("tenant")][InlineData("seal")][InlineData("manifest")][InlineData("request")][InlineData("attempt")][InlineData("revision")]
    [InlineData("issuer")][InlineData("audience")][InlineData("guard")][InlineData("key")][InlineData("attestation")][InlineData("batch-kind")][InlineData("batch-ordinal")][InlineData("batch-id")]
    public async Task IssuanceRejectsChangedCanonicalCapabilityCorrelation(string vector)
    {
        var fixture = new GovernanceGuardFixture(); await fixture.PrepareBoundAsync(seal: true); var valid = fixture.Attest(fixture.Batch()); var payload = valid.Capability!;
        var changed = vector switch { "tenant" => payload with { TenantId = "tenant-b" }, "seal" => payload with { DestructionSealId = "wrong-seal" },
            "manifest" => payload with { ManifestDigest = GovernanceGuardFixture.Hash("wrong-manifest") }, "request" => payload with { DeletionRequestId = "wrong-request" },
            "attempt" => payload with { SigningAttemptOrdinal = 0 }, "issuer" => payload with { Issuer = "foreign-issuer" }, "audience" => payload with { Audience = "foreign-audience" },
            "guard" => payload with { GuardStreamId = "foreign-guard" }, "key" => payload with { CapabilityKeyVersion = "foreign-key" }, "attestation" => payload with { AttestationOrdinal = 2 },
            "batch-kind" => payload with { BatchKind = "Containment" }, "batch-ordinal" => payload with { BatchOrdinal = 1 }, "batch-id" => payload with { BatchId = "foreign-batch" },
            _ => payload with { IntendedIssuedGuardRevision = payload.IntendedIssuedGuardRevision + 1 } };
        string signingId = vector == "attempt" ? valid.SigningRequestId : DeletionBatchCapabilityIdentity.SigningRequestId(changed);
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordBatchIssued, batch: valid with { Capability = changed, SigningRequestId = signingId }))).Status.ShouldBe("Blocked");
        long intended = valid.Capability!.IntendedIssuedGuardRevision;
        (await fixture.Apply(fixture.Command(GovernanceGuardOperation.RecordBatchIssued, batch: valid))).Status.ShouldBe("Committed");
        fixture.State.Deletions.Single().Batches.Single().IssuedGuardRevision.ShouldBe(intended + 1);
    }
    /// <summary>Both caller substitutions are denied against independently authenticated concrete writer categories, without altering sealed owner state.</summary>
    [Theory]
    [InlineData(DirectoryWriteKind.Permit, "Content")][InlineData(DirectoryWriteKind.ContentAppend, "Admission")]
    public async Task ViolationClassificationCannotSubstituteTheIndependentWriteKind(DirectoryWriteKind kind, string substituted)
    {
        var f = new GovernanceGuardFixture(); await f.PrepareBoundAsync(seal: true, kind: kind);
        string before = JsonSerializer.Serialize(f.State);
        var denied = f.Command(GovernanceGuardOperation.RecordViolation, ordinal: f.Ordinal(violation: "substitution", kind: substituted));
        (await f.Apply(denied)).Status.ShouldBe("Blocked"); JsonSerializer.Serialize(f.State).ShouldBe(before);
        string actual = kind == DirectoryWriteKind.Permit ? "Admission" : "Content";
        var original = await f.Apply(f.Command(GovernanceGuardOperation.RecordViolation, ordinal: f.Ordinal(violation: "original", kind: actual)));
        original.Status.ShouldBe("Committed");
        f.State.Deletions.Single().Violations.Single().Kind.ShouldBe(actual);
        f.State.Deletions.Single().IntegrityCompromised.ShouldBe(actual == "Admission");
    }

    /// <summary>A genuine committed acceptance cannot prove another operation/resource/facts/vector, and missing legacy correlation is unavailable; exact original correlation survives restart.</summary>
    [Theory]
    [InlineData("operation")][InlineData("resource")][InlineData("facts")][InlineData("mutation")][InlineData("missing")]
    public async Task ViolationRequiresItsOwnExactCommittedAcceptanceProvenance(string changed)
    {
        var f = new GovernanceGuardFixture(); await f.PrepareBoundAsync(seal: true);
        var accepted = f.OriginalAcceptance!; var foreign = f.State.Receipts.Single(r => r.AcceptedWriteResourceId == "resource-a"); accepted.AcceptedWriteResourceId.ShouldBe("resource-b"); accepted.AcceptedWriteFacts.ShouldBe(f.Facts());
        accepted.AcceptedTargetMutationDigest.Length.ShouldBe(64);
        f.State.Receipts.Single(r => r.OperationId == accepted.OperationId).ShouldBe(accepted);
        string before = JsonSerializer.Serialize(f.State);
        f.ChangeEvidence = (proof, command) => command.Operation != GovernanceGuardOperation.RecordViolation ? proof : changed switch
        {
            "operation" => proof with { ViolationAcceptanceOperationId = f.State.Receipts.Single(r => r.AcceptedWriteResourceId == "resource-a").OperationId },
            "resource" => proof with { OriginalAcceptance = foreign, ViolationAcceptanceOperationId = foreign.OperationId, ViolationWriteFacts = foreign.AcceptedWriteFacts, ViolationTargetMutationDigest = foreign.AcceptedTargetMutationDigest },
            "facts" => proof with { ViolationWriteFacts = proof.ViolationWriteFacts! with { PermitId = "other-authentic-permit" } },
            "mutation" => proof with { ViolationTargetMutationDigest = new string('F', 64) },
            _ => proof with { OriginalAcceptance = accepted with { AcceptedWriteFacts = null, AcceptedWriteResourceId = "", AcceptedTargetMutationDigest = "" } },
        };
        var proposed = f.Ordinal(violation: "foreign-original");
        if (changed == "resource") { proposed = proposed with { AcceptedAtOrdinal = foreign.AcceptedAtAdmissionFenceOrdinal, AcceptedAtGuardHighWater = foreign.GuardHighWater }; }
        (await f.Apply(f.Command(GovernanceGuardOperation.RecordViolation, ordinal: proposed))).Status.ShouldBe("Blocked");
        JsonSerializer.Serialize(f.State).ShouldBe(before);
        f.ChangeEvidence = null;
        var request = f.Command(GovernanceGuardOperation.RecordViolation, ordinal: f.Ordinal(violation: "exact-original"));
        var original = await f.Apply(request); original.Status.ShouldBe("Committed");
        var restarted = new GovernanceScopeGuardOwner(f.Backend.Owner, TimeProvider.System, f.Authority);
        (await restarted.ExecuteAsync(request, [], TestContext.Current.CancellationToken)).ShouldBe(original);
        f.State.Receipts.Single(r => r.OperationId == accepted.OperationId).ShouldBe(accepted);
    }

}
