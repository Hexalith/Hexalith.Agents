using Hexalith.EventStore.Contracts.Security;
using NSubstitute;
using Shouldly;

namespace Hexalith.Platform.Custody.Tests;

/// <summary>Exact canonical signature through actual guard adapter/reducer and private execution coordinator; synthetic owner authority/consumption never qualifies a production installation.</summary>
public sealed class DeletionBatchExecutionCoordinatorTests
{
    /// <summary>Canonical signed artifact, actual issue/dispatch revisions and complete ordered owner vector correlate through conditional completion; restart reuses all original outcomes.</summary>
    [Fact]
    public async Task ActualCanonicalArtifactFlowsThroughIssueDispatchConsumptionAndCompletion()
    {
        using var fixture = new DeletionBatchExecutionFixture();
        var result = await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken);
        result.Status.ShouldBe("CompletionSealed"); result.Signing!.SigningRequestId.ShouldBe(DeletionBatchCapabilityIdentity.SigningRequestId(fixture.Payload));
        var batch = fixture.State.Deletions.Single().Batches.Single(); batch.Capability.ShouldBe(fixture.Payload); batch.IssuedGuardRevision.ShouldBe(11); batch.DispatchGuardRevision.ShouldBe(13);
        result.Protection!.TargetReceipts.Count.ShouldBe(2); fixture.Reservations.ShouldBe(1); fixture.Signatures.ShouldBe(1); fixture.State.Deletions.Single().Completed.ShouldBeTrue();
        int mutations = fixture.SourceMutations;
        (await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken)).Status.ShouldBe("CompletionSealed");
        fixture.Signatures.ShouldBe(1); fixture.Reservations.ShouldBe(1); fixture.SourceMutations.ShouldBe(mutations);
    }

    /// <summary>Irreversible response loss uses only exact original protection lookup and retains the original vector without a second reservation.</summary>
    [Fact]
    public async Task LostConsumptionAcknowledgementUsesExactOriginalLookup()
    {
        using var fixture = new DeletionBatchExecutionFixture { LoseConsumptionAcknowledgement = true };
        var result = await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken);
        result.Status.ShouldBe("CompletionSealed"); fixture.Reservations.ShouldBe(1);
        await fixture.Protection.Received(1).LookupAsync("tenant-a", "batch-a", Arg.Any<CancellationToken>());
    }

    /// <summary>Unknown or partial owner outcomes cannot mirror consumption or complete, despite a valid signature and dispatch.</summary>
    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task UnknownOrPartialConsumptionNeverBecomesCompletion(bool unknown)
    {
        using var fixture = new DeletionBatchExecutionFixture { LoseConsumptionAcknowledgement = unknown, UnknownProtectionLookup = unknown, PartialProtectionResult = !unknown };
        var result = await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken);
        result.Status.ShouldBe(unknown ? "ConsumptionUnknown" : "ConsumptionUnverified"); fixture.State.Deletions.Single().Completed.ShouldBeFalse();
        fixture.State.Deletions.Single().Batches.Single().ProtectionOutcome.ShouldBe("Unconsumed"); fixture.Reservations.ShouldBe(1);
    }

    /// <summary>Only durable exact no-issue terminalizes the stale signed attempt; its successor preserves stable identities and increments exactly the attempt/current compare/key.</summary>
    [Theory]
    [InlineData(true)][InlineData(false)]
    public async Task StaleIssueNeedsExactNoIssueBeforeStableSuccessor(bool missingProof)
    {
        using var fixture = new DeletionBatchExecutionFixture { StaleIssue = true, NoIssueProofUnavailable = missingProof };
        var result = await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken);
        result.Status.ShouldBe(missingProof ? "NoIssueProofUnavailable" : "ObsoleteUnissued"); fixture.Reservations.ShouldBe(0); fixture.Signatures.ShouldBe(1);
        if (missingProof) { result.NextAttempt.ShouldBeNull(); }
        else
        {
            var next = result.NextAttempt!; next.SigningAttemptOrdinal.ShouldBe(2); next.AttestationOrdinal.ShouldBe(fixture.Payload.AttestationOrdinal);
            next.ShouldBe(fixture.Payload with { SigningAttemptOrdinal = 2, IntendedIssuedGuardRevision = 11, CapabilityKeyVersion = "key-b" });
            (await fixture.Coordinator.ExecuteAsync(next, cancellationToken: TestContext.Current.CancellationToken)).Status.ShouldBe("CompletionSealed");
            fixture.Signatures.ShouldBe(2); fixture.Reservations.ShouldBe(1);
        }
    }

    /// <summary>A post-start Open hold prevents dispatch and every protection call; signature/issue alone never authorizes consumption.</summary>
    [Fact]
    public async Task PostStartHoldBlocksBeforeProtectionOwner()
    {
        using var fixture = new DeletionBatchExecutionFixture { BlockDispatchWithHold = true };
        (await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken)).Status.ShouldBe("Blocked");
        fixture.Protection.ReceivedCalls().ShouldBeEmpty(); fixture.State.Deletions.Single().Completed.ShouldBeFalse();
    }

    /// <summary>A persisted guard issue with lost response is recovered by original exact source result on restart; no second source issue or signature is created.</summary>
    [Fact]
    public async Task LostIssueResponseRetainsOriginalArtifactAndRecoversWithoutNewIssue()
    {
        using var fixture = new DeletionBatchExecutionFixture { LoseIssueAcknowledgement = true };
        (await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken)).Status.ShouldBe("Unavailable");
        fixture.State.Deletions.Single().Batches.Single().Capability.ShouldBe(fixture.Payload);
        (await fixture.Coordinator.ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken)).Status.ShouldBe("CompletionSealed");
        fixture.Signatures.ShouldBe(1); fixture.Reservations.ShouldBe(1);
    }

    /// <summary>Missing qualified private bindings leave the ordinary coordinator unavailable without signer, guard or consuming-owner invocation.</summary>
    [Fact]
    public async Task DefaultUnavailableCompositionMakesNoEffects()
    {
        using var fixture = new DeletionBatchExecutionFixture();
        (await new DeletionBatchExecutionCoordinator(TimeProvider.System).ExecuteAsync(fixture.Payload, cancellationToken: TestContext.Current.CancellationToken)).Status.ShouldBe("Unavailable");
        fixture.Signatures.ShouldBe(0); fixture.Reservations.ShouldBe(0); fixture.Source.ReceivedCalls().ShouldBeEmpty();
    }
}
