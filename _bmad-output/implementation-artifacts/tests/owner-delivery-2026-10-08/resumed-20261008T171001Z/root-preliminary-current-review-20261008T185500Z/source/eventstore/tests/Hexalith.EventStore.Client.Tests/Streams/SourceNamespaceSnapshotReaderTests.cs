using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using NSubstitute;
using Shouldly;

namespace Hexalith.EventStore.Client.Tests.Streams;

/// <summary>Finite namespace completeness, detachment and release regressions; authority is synthetic.</summary>
public sealed class SourceNamespaceSnapshotReaderTests
{
    private static readonly SourcePublicationScope Scope = new("tenant-a", "conversation", "catalogue", "installed-1");
    private static readonly AggregateIdentity Identity = new("tenant-a", "conversation", "source-a");

    /// <summary>Streaming small summaries handles complete namespaces without retaining the sum of their payloads; raw snapshots retain the existing whole-read byte bound.</summary>
    [Fact]
    public async Task FoldReleasesEachPrefixBeforeNextAndRawSnapshotBoundsAggregateBytes()
    {
        var now = DateTimeOffset.UtcNow; var second = new AggregateIdentity("tenant-a", "conversation", "source-b");
        var cut = new SourcePublicationCut(Scope, "authority", now, now.AddMinutes(1), [new(Identity, 1), new(second, 1)], true);
        var namespaces = Substitute.For<ISourcePublicationNamespaceSource>(); namespaces.ReadAsync(Scope, Arg.Any<CancellationToken>()).Returns(cut);
        var streams = Substitute.For<IAuthoritativeEventStreamReader>(); var foldedIds = new List<string>();
        streams.ReadAsync(Arg.Any<AggregateIdentity>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var identity = call.Arg<AggregateIdentity>();
            if (identity == second && foldedIds.Count > 0) { foldedIds.ShouldBe([Identity.AggregateId]); }
            var source = new AuthoritativeEventStream(identity, 1, now,
                [new(1, "Created", new byte[9 * 1024 * 1024], "json", 1, "event-" + identity.AggregateId, null, null, now, null)], "exact-read");
            return new AuthoritativeStreamReadResult(source, null);
        });
        var reader = new SourceNamespaceSnapshotReader(namespaces, streams, TimeProvider.System);
        var folded = await reader.ReadFoldedAsync<string>(Scope, (source, token) =>
        { token.ThrowIfCancellationRequested(); foldedIds.Add(source.Identity.AggregateId); return source.Identity.AggregateId; }, TestContext.Current.CancellationToken);
        folded!.Values.ShouldBe([Identity.AggregateId, second.AggregateId]); folded.Cut.Sources.ShouldBe(cut.Sources);
        foldedIds.Clear();
        (await reader.ReadAsync(Scope, TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>Registered empty streams and zero Agent Call streams retain complete exact namespace membership.</summary>
    [Fact]
    public async Task CompleteSnapshotDetachesBytesAndPreservesRegisteredEmptySources()
    {
        var now = DateTimeOffset.UtcNow;
        var empty = new AggregateIdentity("tenant-a", "conversation", "source-empty");
        var cut = new SourcePublicationCut(Scope, "authority-1", now, now.AddMinutes(1), [new(Identity, 1), new(empty, 0)], true);
        var namespaces = Substitute.For<ISourcePublicationNamespaceSource>();
        namespaces.ReadAsync(Scope, Arg.Any<CancellationToken>()).Returns(cut);
        var payload = new byte[] { 1, 2, 3 };
        var source = new AuthoritativeEventStream(Identity, 1, now,
            [new(1, "Created", payload, "json", 1, "event-1", null, null, now, null)], "read-1");
        var streams = Substitute.For<IAuthoritativeEventStreamReader>();
        streams.ReadAsync(Identity, Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(source, null));
        streams.ReadAsync(empty, Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(null, "source-unavailable"));
        var result = await new SourceNamespaceSnapshotReader(namespaces, streams, TimeProvider.System).ReadAsync(Scope, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull(); result.Cut.Sources.Count.ShouldBe(2); result.Sources.Count.ShouldBe(1);
        await streams.DidNotReceive().ReadAsync(empty, Arg.Any<CancellationToken>());
        payload[0] = 99;
        result.Sources.Single(s => s.Identity == Identity).Events.Single().Payload.ShouldBe(new byte[] { 1, 2, 3 });
    }

    /// <summary>Partial, expired and foreign cuts cannot read source bytes or certify zero.</summary>
    [Theory]
    [InlineData("partial")]
    [InlineData("foreign")]
    [InlineData("expired")]
    [InlineData("duplicate")]
    public async Task InvalidCutDeniesBeforeSourceReads(string vector)
    {
        var now = DateTimeOffset.UtcNow;
        var cut = new SourcePublicationCut(Scope, "authority", now, now.AddMinutes(1), [new(Identity, 1)], true);
        cut = vector switch
        {
            "partial" => cut with { IsComplete = false },
            "foreign" => cut with { Sources = [new(new("tenant-b", "conversation", "source-a"), 1)] },
            "duplicate" => cut with { Sources = [new(Identity, 1), new(Identity, 1)] },
            _ => cut with { ValidUntil = now },
        };
        var namespaces = Substitute.For<ISourcePublicationNamespaceSource>();
        namespaces.ReadAsync(Scope, Arg.Any<CancellationToken>()).Returns(cut);
        var streams = Substitute.For<IAuthoritativeEventStreamReader>();
        (await new SourceNamespaceSnapshotReader(namespaces, streams, TimeProvider.System).ReadAsync(Scope,
            TestContext.Current.CancellationToken)).ShouldBeNull();
        streams.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A changed head or coverage revision during capture releases no current catalogue.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NamespaceChangesDuringSourceReadDenyRelease(bool headChanges)
    {
        var now = DateTimeOffset.UtcNow;
        var cut = new SourcePublicationCut(Scope, "authority", now, now.AddMinutes(1), [new(Identity, 0)], true);
        var namespaces = Substitute.For<ISourcePublicationNamespaceSource>();
        namespaces.ReadAsync(Scope, Arg.Any<CancellationToken>()).Returns(cut, headChanges
            ? cut with { Sources = [new(Identity, 1)] } : cut with { AuthorityRevision = "authority-2" });
        var streams = Substitute.For<IAuthoritativeEventStreamReader>();
        streams.ReadAsync(Identity, Arg.Any<CancellationToken>()).Returns(new AuthoritativeStreamReadResult(new(Identity, 0, now, [], "read"), null));
        (await new SourceNamespaceSnapshotReader(namespaces, streams, TimeProvider.System).ReadAsync(Scope,
            TestContext.Current.CancellationToken)).ShouldBeNull();
    }

    /// <summary>Original caller cancellation bounds a stalled namespace invocation and retains no evidence.</summary>
    [Fact]
    public async Task CallerCancellationStopsNoncooperativeNamespaceWait()
    {
        var namespaces = Substitute.For<ISourcePublicationNamespaceSource>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<SourcePublicationCut?>(TaskCreationOptions.RunContinuationsAsynchronously);
        namespaces.ReadAsync(Scope, Arg.Any<CancellationToken>()).Returns(_ => { entered.TrySetResult(); return pending.Task; });
        using var caller = new CancellationTokenSource();
        var read = new SourceNamespaceSnapshotReader(namespaces, Substitute.For<IAuthoritativeEventStreamReader>(), TimeProvider.System)
            .ReadAsync(Scope, caller.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        caller.Cancel();
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => read.WaitAsync(TimeSpan.FromSeconds(5)));
        exception.CancellationToken.ShouldBe(caller.Token);
        pending.SetResult(null);
    }
}
