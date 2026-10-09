using Hexalith.EventStore.Contracts.Security;

namespace Hexalith.EventStore.Contracts.Tests.Security;

/// <summary>Executable exact final-durability/current-authority and incremental pending carrier bound checks over the production shared protocol.</summary>
public sealed class RecoverableAnchoredStateTests : IAnchoredStateTransitionAuthority
{
    private bool _verified = true;
    private int _records;
    /// <inheritdoc/>
    public Task<bool> RecordTransitionAsync(AnchoredStateTransition transition, CancellationToken cancellationToken = default)
    { _records++; return Task.FromResult(false); }
    /// <inheritdoc/>
    public Task<bool> VerifyTransitionAsync(AnchoredStateTransition transition, CancellationToken cancellationToken = default) => Task.FromResult(_verified);

    /// <summary>A target write suspended after prevalidation cannot certify stale durable bytes or changed final anchor/journal authority.</summary>
    [Theory]
    [InlineData("bytes")][InlineData("anchor")][InlineData("journal")][InlineData("valid")]
    public async Task ReconciliationReconfirmsFreshDurableTargetAndCurrentExactAuthorityAfterPersistence(string vector)
    {
        var transition = RecoverableAnchoredState.Prepare("scope", 0, 1, "before", "after");
        string anchor = transition.TargetDigest, durable = "after";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = RecoverableAnchoredState.ReconcileAsync("scope", "before", transition, value => value,
            value => Task.FromResult(RecoverableAnchoredState.Digest(value) == anchor), this, async value =>
            { value.ShouldBe("after"); entered.TrySetResult(); await release.Task; return durable; });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (vector == "bytes") { durable = "before"; }
        if (vector == "anchor") { anchor = RecoverableAnchoredState.Digest("different-current-authority"); }
        if (vector == "journal") { _verified = false; }
        release.SetResult();
        if (vector == "valid") { (await pending).ShouldBe("after"); }
        else { await Should.ThrowAsync<InvalidOperationException>(() => pending); }
        _records.ShouldBe(0);
    }
    /// <summary>Pending byte production stops at the bounded stream before completing the oversized carrier or making any independent journal call.</summary>
    [Fact]
    public void OversizedPendingSerializationStopsProductionBeforeJournalInvocation()
    {
        int produced = 0; string item = new('x', 4096);
        IEnumerable<string> original = Array.Empty<string>();
        IEnumerable<string> oversized = Enumerable.Range(0, 40000).Select(_ => { produced++; return item; });
        Should.Throw<InvalidOperationException>(() => RecoverableAnchoredState.Prepare("scope", 0, 1, original, oversized));
        produced.ShouldBeLessThan(10000); produced.ShouldBeGreaterThan(0); _records.ShouldBe(0);
    }
}
