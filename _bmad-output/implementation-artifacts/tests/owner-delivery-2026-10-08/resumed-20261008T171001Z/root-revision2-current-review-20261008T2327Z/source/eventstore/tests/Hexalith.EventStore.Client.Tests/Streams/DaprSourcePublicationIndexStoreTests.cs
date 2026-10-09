using System.Reflection;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Server.Streams;
using Hexalith.EventStore.Testing.Fakes;
using NSubstitute;
using Shouldly;

namespace Hexalith.EventStore.Client.Tests.Streams;

/// <summary>Actual DAPR index actor/storage transport with the standard committed-state test provider; no live backend qualification.</summary>
public sealed class DaprSourcePublicationIndexStoreTests
{
    private static readonly SourcePublicationScope Scope = new("tenant-a", "conversation", "approved-deletion-v1", "installation-1");
    private static readonly ConditionalWeakTable<IActorStateManager, ISourcePublicationOperationAuthority> Authorities = new();
    private static SourcePublicationIndexState State(long revision = 1, string? poison = null)
    {
        var source = new AggregateIdentity("tenant-a", "conversation", "source-a");
        return new(Scope, revision, "authority-1", [new(source, 2)], [new(1, new("signal-1", source, 2, "event-2", new string('A', 64)))], poison);
    }
    private static SourcePublicationIndexActor Actor(IActorStateManager state, ISourcePublicationOperationAuthority? operations = null)
    {
        var actor = new SourcePublicationIndexActor(ActorHost.CreateForTest<SourcePublicationIndexActor>(new ActorTestOptions { ActorId = new(Scope.ActorId) }), operations ?? Authorities.GetValue(state, _ => Operations()));
        typeof(Dapr.Actors.Runtime.Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance)!.SetValue(actor, state);
        return actor;
    }
    private static ISourcePublicationOperationAuthority Operations(Func<bool>? journalAvailable = null)
    {
        var operations = Substitute.For<ISourcePublicationOperationAuthority>(); operations.ReadIndexAsync(Arg.Any<SourcePublicationScope>()).Returns(true);
        operations.WriteIndexAsync(Arg.Any<SourcePublicationIndexWrite>()).Returns(true);
        long revision = 0; string digest = Digest(null);
        operations.ValidateIndexStateAsync(Scope, Arg.Any<long>(), Arg.Any<string>()).Returns(call => call.ArgAt<long>(1) == revision && call.ArgAt<string>(2) == digest);
        var admissions = new Dictionary<string, string>(StringComparer.Ordinal);
        operations.AdmitTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call => {
            var t = call.Arg<AnchoredStateTransition>(); string exact = JsonSerializer.Serialize(t);
            if (admissions.TryGetValue(t.TargetDigest, out var admitted)) { return admitted == exact; }
            if (t.ScopeId != Scope.ActorId + "|source-publications-v1" || t.ExpectedRevision != revision || t.TargetRevision != revision + 1 || t.PredecessorDigest != digest) { return false; }
            admissions.Add(t.TargetDigest, exact); return true;
        });
        operations.RecoverTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call => {
            var t = call.Arg<AnchoredStateTransition>(); return admissions.TryGetValue(t.TargetDigest, out var admitted) && admitted == JsonSerializer.Serialize(t)
                ? operations.RecordTransitionAsync(t, call.Arg<CancellationToken>()) : Task.FromResult(false);
        });
        var transitions = new Dictionary<string, AnchoredStateTransition>(StringComparer.Ordinal);
        operations.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call => {
            var t = call.Arg<AnchoredStateTransition>();
            if (journalAvailable is not null && !journalAvailable()) { return false; }
            if (t.ScopeId != Scope.ActorId + "|source-publications-v1") { return false; }
            if (transitions.TryGetValue(t.TargetDigest, out var original)) { return JsonSerializer.Serialize(original) == JsonSerializer.Serialize(t); }
            if (t.ExpectedRevision != revision || t.TargetRevision != revision + 1 || t.PredecessorDigest != digest) { return false; }
            revision++; digest = t.TargetDigest; transitions.Add(t.TargetDigest, t with { TargetBytes = t.TargetBytes.ToArray() }); return true;
        });
        operations.VerifyTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call => {
            var t = call.Arg<AnchoredStateTransition>(); return transitions.TryGetValue(t.TargetDigest, out var original) && JsonSerializer.Serialize(original) == JsonSerializer.Serialize(t);
        });
        operations.RecordIndexRevisionAsync(Scope, Arg.Any<long>(), Arg.Any<long>(), Arg.Any<string>()).Returns(call =>
        { if (call.ArgAt<long>(1) != revision || call.ArgAt<long>(2) != revision + 1) { return false; } revision++; digest = call.ArgAt<string>(3); return true; });
        return operations;
    }
    private static DaprSourcePublicationIndexStore Store(SourcePublicationIndexActor actor)
    {
        var proxies = Substitute.For<IActorProxyFactory>();
        proxies.CreateActorProxy<ISourcePublicationIndexActor>(new ActorId(Scope.ActorId), SourcePublicationIndexActor.ActorTypeName).Returns(actor);
        return new(proxies);
    }

    /// <summary>Actual SaveState persists the exact revision/references and a fresh actor instance reads the committed bytes.</summary>
    [Fact]
    public async Task ConditionalCommitAndRestartPreservePersistedExactOutcome()
    {
        var backend = new InMemoryStateManager(); var actor = Actor(backend); var store = Store(actor);
        (await store.ReadAsync(Scope, TestContext.Current.CancellationToken)).ShouldBeNull();
        (await store.TryWriteAsync(Scope, 0, State(), TestContext.Current.CancellationToken)).ShouldBeTrue();
        var saved = backend.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationIndexState>();
        saved.Revision.ShouldBe(1); saved.Entries.Single().Publication.PublicationId.ShouldBe("signal-1");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(saved);
        var restored = new InMemoryStateManager();
        await restored.SetStateAsync(backend.CommittedState.Single().Key, JsonSerializer.Deserialize<SourcePublicationIndexState>(bytes)!);
        await restored.SaveStateAsync();
        var fresh = Store(Actor(restored, Authorities.GetValue(backend, _ => Operations()))); var read = await fresh.ReadAsync(Scope, TestContext.Current.CancellationToken);
        read!.Sources.Single().Head.ShouldBe(2); read.Entries.Single().ShouldBe(saved.Entries.Single());
        (await fresh.TryWriteAsync(Scope, 0, State(), TestContext.Current.CancellationToken)).ShouldBeFalse();
        restored.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationIndexState>().Revision.ShouldBe(1);
    }

    /// <summary>A failed save cannot make its staged cache authoritative; an unknown committed outcome is read from persisted storage.</summary>
    [Theory]
    [InlineData(1, false)][InlineData(1, true)][InlineData(2, false)][InlineData(2, true)]
    public async Task FailedSaveCannotReleaseStagedState(int failSave, bool committed)
    {
        var backend = new InMemoryStateManager(); var manager = Faulting<SourcePublicationIndexState>(backend, failSave, committed); var actor = Actor(manager);
        await Should.ThrowAsync<HttpRequestException>(() => actor.TryWriteAsync(new(0, State())));
        foreach (var item in backend.CommittedState.ToArray())
        {
            if (item.Value is AnchoredStateTransition pending) { await backend.SetStateAsync(item.Key, JsonSerializer.Deserialize<AnchoredStateTransition>(JsonSerializer.Serialize(pending))!); }
            else { await backend.SetStateAsync(item.Key, JsonSerializer.Deserialize<SourcePublicationIndexState>(JsonSerializer.Serialize(item.Value))!); }
        }
        await backend.SaveStateAsync();
        var restarted = Actor(backend, Authorities.GetValue(manager, _ => Operations()));
        if (failSave == 1) { (await restarted.ReadAsync(Scope)).ShouldBeNull(); }
        (await restarted.TryWriteAsync(new(0, State()))).ShouldBe(failSave == 1 || !committed);
        (await restarted.ReadAsync(Scope))!.Entries.Single().Publication.PublicationId.ShouldBe("signal-1");
        backend.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationIndexState>().Revision.ShouldBe(1);
    }

    /// <summary>A stale writer cannot overwrite later exact state, and poison has no implicit clear branch.</summary>
    [Fact]
    public async Task StaleRevisionAndPoisonCannotBeOverwritten()
    {
        var backend = new InMemoryStateManager(); var store = Store(Actor(backend));
        (await store.TryWriteAsync(Scope, 0, State(), TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await store.TryWriteAsync(Scope, 1, State(2, "publication-reference-conflict"), TestContext.Current.CancellationToken)).ShouldBeTrue();
        (await store.TryWriteAsync(Scope, 1, State(2), TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await store.TryWriteAsync(Scope, 2, State(3), TestContext.Current.CancellationToken)).ShouldBeFalse();
        backend.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationIndexState>().PoisonCode.ShouldBe("publication-reference-conflict");
    }

    /// <summary>Actor identity validates exact tenant/feed/installation before state lookup.</summary>
    [Theory]
    [InlineData("tenant")]
    [InlineData("feed")]
    [InlineData("installation")]
    public async Task CrossScopeActorCallsCannotReadOrWrite(string vector)
    {
        var backend = new InMemoryStateManager(); var actor = Actor(backend);
        var wrong = new SourcePublicationScope(vector == "tenant" ? "tenant-b" : Scope.Tenant, Scope.Domain,
            vector == "feed" ? "other-feed" : Scope.FeedName, vector == "installation" ? "other-installation" : Scope.InstallationId);
        await Should.ThrowAsync<ArgumentException>(() => actor.ReadAsync(wrong));
        await Should.ThrowAsync<ArgumentException>(() => actor.TryWriteAsync(new(0, State() with { Scope = wrong })));
        backend.CommittedState.ShouldBeEmpty();
    }

    /// <summary>Caller cancellation stops the exact transport wait with its original token.</summary>
    [Fact]
    public async Task CancelledTransportDoesNotReleaseLateState()
    {
        var proxy = Substitute.For<ISourcePublicationIndexActor>();
        var pending = new TaskCompletionSource<SourcePublicationIndexState?>(TaskCreationOptions.RunContinuationsAsynchronously);
        proxy.ReadAsync(Scope).Returns(pending.Task);
        var factories = Substitute.For<IActorProxyFactory>();
        factories.CreateActorProxy<ISourcePublicationIndexActor>(new ActorId(Scope.ActorId), SourcePublicationIndexActor.ActorTypeName).Returns(proxy);
        using var caller = new CancellationTokenSource();
        var reading = new DaprSourcePublicationIndexStore(factories).ReadAsync(Scope, caller.Token); caller.Cancel();
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => reading);
        exception.CancellationToken.ShouldBe(caller.Token); pending.TrySetResult(State());
    }
    /// <summary>Existing immutable index cannot be released or changed through missing/withdrawn private read/write credentials.</summary>
    [Fact]
    public async Task MissingOrWithdrawnPrivateOperationCredentialDeniesIndex()
    {
        var backend = new InMemoryStateManager(); var actor = Actor(backend); (await actor.TryWriteAsync(new(0, State()))).ShouldBeTrue();
        var host = ActorHost.CreateForTest<SourcePublicationIndexActor>(new ActorTestOptions { ActorId = new(Scope.ActorId) });
        var operations = Authorities.GetValue(backend, _ => Operations()); var restricted = new SourcePublicationIndexActor(host, operations);
        typeof(Dapr.Actors.Runtime.Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance)!.SetValue(restricted, backend);
        operations.ReadIndexAsync(Scope).Returns(false); (await restricted.ReadAsync(Scope)).ShouldBeNull();
        operations.WriteIndexAsync(Arg.Any<SourcePublicationIndexWrite>()).Returns(false); (await restricted.TryWriteAsync(new(1, State(2)))).ShouldBeFalse();
        (await new SourcePublicationIndexActor(host).ReadAsync(Scope)).ShouldBeNull();
        backend.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationIndexState>().Revision.ShouldBe(1);
        operations.ReadIndexAsync(Scope).Returns(true);
        (await actor.ReadAsync(Scope))!.Entries.Single().Publication.PublicationId.ShouldBe("signal-1");
    }

    /// <summary>Admitted writers cannot change, drop or reorder an immutable prefix or regress/drop source coverage; rejected candidates never enter committed state or restart reads.</summary>
    [Theory]
    [InlineData("changed")][InlineData("dropped")][InlineData("reordered")][InlineData("regressed-head")][InlineData("missing-head")]
    public async Task OwningMutationRejectsPrefixAndCoverageSubstitution(string vector)
    {
        var backend = new InMemoryStateManager(); var actor = Actor(backend); var original = State();
        var source = original.Sources.Single().Identity; var emptySource = new AggregateIdentity("tenant-a", "conversation", "source-empty");
        original = original with { Sources = [new(source, 4), new(emptySource, 0)], Entries = [original.Entries.Single(), new(2, new("signal-2", source, 3, "event-3", new string('B', 64)))] };
        (await actor.TryWriteAsync(new(0, original))).ShouldBeTrue(); var committed = backend.CommittedState.Single(); string bytes = JsonSerializer.Serialize(committed.Value);
        var next = original with { Revision = 2 };
        next = vector switch
        {
            "changed" => next with { Entries = [next.Entries[0] with { Publication = next.Entries[0].Publication with { StableFieldsDigest = new string('C', 64) } }, next.Entries[1]] },
            "dropped" => next with { Entries = [] },
            "reordered" => next with { Entries = [next.Entries[1] with { Offset = 1 }, next.Entries[0] with { Offset = 2 }] },
            "regressed-head" => next with { Sources = [new(source, 3), new(emptySource, 0)] },
            _ => next with { Sources = [new(source, 4)] },
        };
        (await actor.TryWriteAsync(new(1, next))).ShouldBeFalse(); JsonSerializer.Serialize(backend.CommittedState.Single().Value).ShouldBe(bytes);
        JsonSerializer.Serialize(await Actor(backend).ReadAsync(Scope)).ShouldBe(bytes);
    }

    /// <summary>Valid append growth retains exact prefix/head vectors, and deliberate poison can retain the exact committed vectors.</summary>
    [Fact]
    public async Task AppendGrowthAndExactPoisonRetentionPersist()
    {
        var backend = new InMemoryStateManager(); var actor = Actor(backend); var first = State(); (await actor.TryWriteAsync(new(0, first))).ShouldBeTrue();
        var next = first with { Revision = 2, Sources = [new(first.Sources.Single().Identity, 3)], Entries = [first.Entries.Single(), new(2, new("signal-2", first.Sources.Single().Identity, 3, "event-3", new string('B', 64)))] };
        (await actor.TryWriteAsync(new(1, next))).ShouldBeTrue(); (await actor.TryWriteAsync(new(2, next with { Revision = 3, PoisonCode = "publication-source-regressed" }))).ShouldBeTrue();
        var persisted = (await Actor(backend).ReadAsync(Scope))!; persisted.Revision.ShouldBe(3); persisted.Entries.ShouldBe(next.Entries); persisted.Sources.ShouldBe(next.Sources);
    }

    /// <summary>An older valid state or equal-revision divergent restore cannot reassign immutable publication offsets; only the independently anchored exact latest state is released.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task IndependentExactAnchorRejectsOldAndDivergentRestore(bool divergent)
    {
        var backend = new InMemoryStateManager(); var actor = Actor(backend); var first = State(); await actor.TryWriteAsync(new(0, first));
        var latest = first with { Revision = 2, AuthorityRevision = "authority-2" }; await actor.TryWriteAsync(new(1, latest));
        string key = backend.CommittedState.Single().Key;
        await backend.SetStateAsync(key, divergent ? latest with { Entries = [latest.Entries.Single() with { Publication = latest.Entries.Single().Publication with { StableFieldsDigest = new string('B', 64) } }] } : first); await backend.SaveStateAsync();
        await Should.ThrowAsync<InvalidOperationException>(() => Actor(backend).ReadAsync(Scope));
        await Should.ThrowAsync<InvalidOperationException>(() => Actor(backend).TryWriteAsync(new(divergent ? 2 : 1, latest with { Revision = divergent ? 3 : 2 })));
        await backend.SetStateAsync(key, latest); await backend.SaveStateAsync(); (await Actor(backend).ReadAsync(Scope))!.Revision.ShouldBe(2);
    }

    /// <summary>Backend pending bytes do not authenticate a transition; exact independent predecessor/target proof is mandatory.</summary>
    [Theory]
    [InlineData("unanchored")][InlineData("predecessor")][InlineData("target")][InlineData("scope")]
    public async Task UnauthenticatedOrTamperedPendingCannotAdvanceOrRelease(string vector)
    {
        var backend = new InMemoryStateManager(); var operations = Operations();
        var pending = RecoverableAnchoredState.Prepare(Scope.ActorId + "|source-publications-v1", 0, 1, (SourcePublicationIndexState?)null, State());
        if (vector != "unanchored") { (await operations.RecordTransitionAsync(pending)).ShouldBeTrue(); }
        pending = vector switch
        {
            "predecessor" => pending with { PredecessorDigest = new string('A', 64) },
            "target" => RecoverableAnchoredState.Prepare(pending.ScopeId, 0, 1, (SourcePublicationIndexState?)null, State() with { AuthorityRevision = "changed" }),
            "scope" => pending with { ScopeId = "foreign-owner|source-publications-v1" },
            _ => pending,
        };
        await backend.SetStateAsync("source-publications-v1-pending-transition-v1", JsonSerializer.Deserialize<AnchoredStateTransition>(JsonSerializer.Serialize(pending))!);
        await backend.SaveStateAsync(); operations.ClearReceivedCalls();
        var actor = Actor(backend, operations);
        if (vector == "unanchored")
        {
            (await actor.ReadAsync(Scope)).ShouldBeNull();
            await Should.ThrowAsync<InvalidOperationException>(() => actor.TryWriteAsync(new(0, State() with { AuthorityRevision = "different-original" })));
            (await actor.TryWriteAsync(new(0, State()))).ShouldBeTrue();
        }
        else { await Should.ThrowAsync<InvalidOperationException>(() => actor.ReadAsync(Scope)); }
        await operations.Received(vector == "unanchored" ? 1 : 0).RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>());
        if (vector != "unanchored") { backend.CommittedState.Single().Value.ShouldBeOfType<AnchoredStateTransition>(); }
    }

    /// <summary>A later admitted mutation recovers the retained exact original without its caller; ordinary reads never advance an unanchored stage.</summary>
    [Fact]
    public async Task LaterMutationRecoversPreJournalOriginalAfterRestartWithoutOriginalCaller()
    {
        bool journalAvailable = false; var backend = new InMemoryStateManager(); var operations = Operations(() => journalAvailable);
        (await Actor(backend, operations).TryWriteAsync(new(0, State()))).ShouldBeFalse();
        var original = backend.CommittedState.Single().Value.ShouldBeOfType<AnchoredStateTransition>();
        original.TargetRevision.ShouldBe(1); operations.ClearReceivedCalls();
        (await Actor(backend, operations).ReadAsync(Scope)).ShouldBeNull();
        await operations.DidNotReceive().RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>());
        journalAvailable = true;
        (await Actor(backend, operations).TryWriteAsync(new(1, State(2)))).ShouldBeTrue();
        var saved = backend.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationIndexState>();
        saved.Revision.ShouldBe(2); saved.Entries.ShouldBe(State().Entries);
        (await Actor(backend, operations).ReadAsync(Scope))!.Revision.ShouldBe(2);
    }

    private static string Digest(SourcePublicationIndexState? state) => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(state)));

    private static IActorStateManager Faulting<T>(InMemoryStateManager backend, int failSave, bool committed)
    {
        var manager = Substitute.For<IActorStateManager>(); int saves = 0;
        manager.ClearCacheAsync(Arg.Any<CancellationToken>()).Returns(call => backend.ClearCacheAsync(call.Arg<CancellationToken>()));
        manager.TryGetStateAsync<T>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => backend.TryGetStateAsync<T>(call.Arg<string>(), call.Arg<CancellationToken>()));
        manager.SetStateAsync(Arg.Any<string>(), Arg.Any<T>(), Arg.Any<CancellationToken>()).Returns(call => backend.SetStateAsync(call.Arg<string>(), call.Arg<T>(), call.Arg<CancellationToken>()));
        manager.TryGetStateAsync<AnchoredStateTransition>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => backend.TryGetStateAsync<AnchoredStateTransition>(call.Arg<string>(), call.Arg<CancellationToken>()));
        manager.SetStateAsync(Arg.Any<string>(), Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call => backend.SetStateAsync(call.Arg<string>(), call.Arg<AnchoredStateTransition>(), call.Arg<CancellationToken>()));
        manager.TryRemoveStateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => backend.TryRemoveStateAsync(call.Arg<string>(), call.Arg<CancellationToken>()));
        manager.SaveStateAsync(Arg.Any<CancellationToken>()).Returns(async call => {
            if (++saves != failSave) { await backend.SaveStateAsync(call.Arg<CancellationToken>()); return; }
            if (committed) { await backend.SaveStateAsync(call.Arg<CancellationToken>()); }
            throw new HttpRequestException("Controlled exact pending/main save failure or lost acknowledgement.");
        }); return manager;
    }
    /// <summary>A read-only prefix proposal cannot recover an independently admitted unanchored original before separate exact mutation admission.</summary>
    [Fact]
    public async Task ReadOnlyProgressProposalNeverElevatesPendingOriginalAuthority()
    {
        bool journal = true; var backend = new InMemoryStateManager(); var authority = Operations(() => journal); var actor = Actor(backend, authority);
        (await actor.TryWriteAsync(new(0, State()))).ShouldBeTrue(); journal = false;
        (await actor.TryWriteAsync(new(1, State(2)))).ShouldBeFalse();
        string before = JsonSerializer.Serialize(backend.CommittedState); authority.ClearReceivedCalls(); journal = true;
        authority.WriteIndexAsync(Arg.Any<SourcePublicationIndexWrite>()).Returns(false);
        var current = State(); var cut = new SourcePublicationCheckpoint(Scope, 1, current.AuthorityRevision, current.Sources, 1);
        var progress = new SourcePublicationDispatchProgress(Scope, 1, 1, cut.AuthorityRevision,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(cut.Sources))), 1,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(current.Entries))), "receiver", "worker-authority", "independent-original-prefix");
        authority.AuthorizeDispatchAdvanceAsync(Arg.Any<SourcePublicationDispatchAdvance>(), Arg.Any<SourcePublicationIndexState>()).Returns(progress);
        authority.VerifyDispatchProgressAsync(Arg.Any<SourcePublicationCheckpoint>(), Arg.Any<SourcePublicationDispatchProgress>()).Returns(true);
        (await Actor(backend, authority).AdvanceDispatchProgressAsync(new(cut, 0, current.Entries))).ShouldBeNull();
        await authority.DidNotReceive().RecoverTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>());
        await authority.DidNotReceive().RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>());
        JsonSerializer.Serialize(backend.CommittedState).ShouldBe(before);
    }

    /// <summary>The actual DAPR transport retains an independently authenticated exact prefix across serialized actor restart and denies a withdrawn proof without changing the original.</summary>
    [Fact]
    public async Task QualifiedProgressTransportPreservesOriginalAcrossRestartAndWithdrawal()
    {
        var backend = new InMemoryStateManager(); var authority = Operations(); var store = Store(Actor(backend, authority));
        var state = State(); (await store.TryWriteAsync(Scope, 0, state, TestContext.Current.CancellationToken)).ShouldBeTrue();
        var cut = new SourcePublicationCheckpoint(Scope, state.Revision, state.AuthorityRevision, state.Sources, state.Entries.Count);
        bool proofCurrent = true;
        var original = new SourcePublicationDispatchProgress(Scope, 1, 1, cut.AuthorityRevision,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(cut.Sources))), 1,
            Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(state.Entries))), "receiver", "worker-authority", "qualified-original-prefix");
        authority.AuthorizeDispatchAdvanceAsync(Arg.Any<SourcePublicationDispatchAdvance>(), Arg.Any<SourcePublicationIndexState>()).Returns(call =>
        {
            var proposal = call.Arg<SourcePublicationDispatchAdvance>();
            return proposal.ExpectedPrefix == 0 && proposal.AcknowledgedEntries.SequenceEqual(state.Entries) ? original : null;
        });
        authority.VerifyDispatchProgressAsync(Arg.Any<SourcePublicationCheckpoint>(), Arg.Any<SourcePublicationDispatchProgress>()).Returns(call =>
            proofCurrent && JsonSerializer.Serialize(call.Arg<SourcePublicationDispatchProgress>()) == JsonSerializer.Serialize(original));
        (await store.ReadDispatchProgressAsync(cut, TestContext.Current.CancellationToken)).ShouldBeNull();
        JsonSerializer.Serialize(await store.AdvanceDispatchProgressAsync(new(cut, 0, state.Entries), TestContext.Current.CancellationToken)).ShouldBe(JsonSerializer.Serialize(original));
        var persisted = backend.CommittedState.Single(); var restored = new InMemoryStateManager();
        await restored.SetStateAsync(persisted.Key, JsonSerializer.Deserialize<SourcePublicationIndexState>(JsonSerializer.Serialize(persisted.Value))!);
        await restored.SaveStateAsync(); var fresh = Store(Actor(restored, authority));
        JsonSerializer.Serialize(await fresh.ReadDispatchProgressAsync(cut, TestContext.Current.CancellationToken)).ShouldBe(JsonSerializer.Serialize(original));
        var exact = JsonSerializer.Serialize(restored.CommittedState); proofCurrent = false;
        (await fresh.ReadDispatchProgressAsync(cut, TestContext.Current.CancellationToken)).ShouldBeNull();
        JsonSerializer.Serialize(restored.CommittedState).ShouldBe(exact);
        restored.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationIndexState>().Revision.ShouldBe(2);
    }

    /// <summary>A suspended progress owner cannot hold the transport caller after cancellation or release its late proof as caller success.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task CancelledProgressTransportDoesNotReleaseLateProof(bool advancing)
    {
        var state = State(); var cut = new SourcePublicationCheckpoint(Scope, state.Revision, state.AuthorityRevision, state.Sources, 1);
        var advance = new SourcePublicationDispatchAdvance(cut, 0, state.Entries);
        var pending = new TaskCompletionSource<SourcePublicationDispatchProgress?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var proxy = Substitute.For<ISourcePublicationIndexActor>();
        proxy.ReadDispatchProgressAsync(cut).Returns(pending.Task); proxy.AdvanceDispatchProgressAsync(advance).Returns(pending.Task);
        var factories = Substitute.For<IActorProxyFactory>();
        factories.CreateActorProxy<ISourcePublicationIndexActor>(new ActorId(Scope.ActorId), SourcePublicationIndexActor.ActorTypeName).Returns(proxy);
        var store = new DaprSourcePublicationIndexStore(factories); using var caller = new CancellationTokenSource();
        var waiting = advancing ? store.AdvanceDispatchProgressAsync(advance, caller.Token) : store.ReadDispatchProgressAsync(cut, caller.Token);
        waiting.IsCompleted.ShouldBeFalse(); caller.Cancel();
        var exception = await Should.ThrowAsync<OperationCanceledException>(() => waiting); exception.CancellationToken.ShouldBe(caller.Token);
        pending.TrySetResult(new(Scope, 1, 1, cut.AuthorityRevision, new string('A', 64), 1, new string('B', 64), "receiver", "authority", "late-original"));
        waiting.IsCanceled.ShouldBeTrue();
    }
}
