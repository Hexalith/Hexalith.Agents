from pathlib import Path
r=Path('/home/administrator/projects/hexalith/platform')
def w(p,s):p.write_bytes(s.replace('\n','\r\n').encode())
p=r/'tests/Hexalith.Platform.Custody.Tests/ActorPendingFixture.cs';s=p.read_text();i=s.rfind('\n}');s=s[:i]+'''
    internal static (IActorStateManager Manager, Task Entered, TaskCompletionSource Release, Task Finished) Suspended<T>(InMemoryStateManager backend, int saveNumber)
    {
        var manager = Faulting<T>(backend, -1, false); int saves = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        manager.SaveStateAsync(Arg.Any<CancellationToken>()).Returns(async call =>
        {
            if (++saves == saveNumber) { entered.TrySetResult(); await release.Task; }
            await backend.SaveStateAsync(call.Arg<CancellationToken>());
            if (saves == saveNumber) { finished.TrySetResult(); }
        });
        return (manager, entered.Task, release, finished.Task);
    }
'''+s[i:];w(p,s)
p=r/'tests/Hexalith.Platform.Custody.Tests/DeletionCapabilitySigningActorTests.cs';s=p.read_text();i=s.rfind('\n}');s=s[:i]+'''
    /// <summary>One actual actor budget includes admission/trust/journal/state/final release; abandoned state I/O excludes later turns until actual completion.</summary>
    [Theory]
    [InlineData("admission")][InlineData("trust")][InlineData("post-trust")][InlineData("journal")][InlineData("final")][InlineData("stage-save")][InlineData("terminal-save")]
    public async Task WholeSigningActorBudgetPreservesOriginalAndStateIoBoundary(string stage)
    {
        var (clock, advance) = PrivateOwnerDeadlineTestClock.Create(); var payload = Payload(); string id = DeletionBatchCapabilityIdentity.SigningRequestId(payload);
        var backend = new InMemoryStateManager(); var authority = Authority(); using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var trust = Trust(payload, key); var current = await trust.ResolveAsync(payload.TenantId, "DeletionBatchCapabilitySigningKey", payload.CapabilityKeyVersion, TestContext.Current.CancellationToken);
        var provider = Substitute.For<IDeletionCapabilitySigningProvider>(); var signed = Signed(payload, key); int signatures = 0;
        provider.SignAsync(payload, id, Arg.Any<CancellationToken>()).Returns(_ => { signatures++; return signed; });
        provider.LookupAsync(payload, id, Arg.Any<CancellationToken>()).Returns(_ => signatures == 0 ? new DeletionCapabilitySigningResult(new(id, payload, DeletionCapabilitySigningState.Unknown), null) : signed);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingTrust = new TaskCompletionSource<DeletionCapabilityPublishedTrust?>(TaskCreationOptions.RunContinuationsAsynchronously); int admissions = 0; int trusts = 0;
        authority.AuthorizeOperationAsync(payload, id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        { if (++admissions == (stage == "final" ? 3 : 1) && stage is "final" or "admission") { entered.TrySetResult(); return pending.Task; } return Task.FromResult(true); });
        if (stage is "trust" or "post-trust") { trust.ResolveAsync(payload.TenantId, "DeletionBatchCapabilitySigningKey", payload.CapabilityKeyVersion, Arg.Any<CancellationToken>()).Returns(_ =>
            { if (++trusts == (stage == "post-trust" ? 2 : 1)) { entered.TrySetResult(); return pendingTrust.Task; } return Task.FromResult(current); }); }
        if (stage == "journal") { authority.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(_ => { entered.TrySetResult(); return pending.Task; }); }
        var io = ActorPendingFixture.Suspended<DeletionCapabilitySigningOutcome>(backend, stage == "stage-save" ? 1 : 4);
        bool stateIo = stage is "stage-save" or "terminal-save";
        var actor = Actor(payload, stateIo ? io.Manager : backend, authority, provider, trust, clock: clock);
        var reading = actor.SignAsync(payload); await (stateIo ? io.Entered : entered.Task).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            advance(TimeSpan.FromSeconds(30)); (await reading.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).State.ShouldBe(DeletionCapabilitySigningState.Unavailable);
            if (stateIo)
            {
                int calls = io.Manager.ReceivedCalls().Count(); (await actor.LookupAsync(payload).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).State.ShouldBe(DeletionCapabilitySigningState.Unavailable);
                io.Manager.ReceivedCalls().Count().ShouldBe(calls);
            }
            int effects = signatures; io.Release.TrySetResult(); pending.TrySetResult(true); pendingTrust.TrySetResult(null);
            if (stateIo) { await io.Finished.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken); }
            signatures.ShouldBe(effects);
            if (stage == "post-trust") { backend.CommittedState.Single().Value.ShouldBeOfType<DeletionCapabilitySigningOutcome>().State.ShouldBe(DeletionCapabilitySigningState.Unknown); }
            if (stage is "terminal-save" or "final")
            {
                (await actor.LookupAsync(payload)).ShouldBe(signed.Outcome); signatures.ShouldBe(1);
                var stored = backend.CommittedState.Single(); var restored = new InMemoryStateManager();
                await restored.SetStateAsync(stored.Key, JsonSerializer.Deserialize<DeletionCapabilitySigningOutcome>(JsonSerializer.SerializeToUtf8Bytes(stored.Value))!); await restored.SaveStateAsync();
                (await Actor(payload, restored, authority, provider, trust, clock: clock).SignAsync(payload)).ShouldBe(signed.Outcome); signatures.ShouldBe(1);
            }
        }
        finally { io.Release.TrySetResult(); pending.TrySetResult(true); pendingTrust.TrySetResult(null); }
    }
'''+s[i:];w(p,s)
p=r/'tests/Hexalith.Platform.Custody.Tests/ExportKeyDeliveryActorTests.cs';s=p.read_text();i=s.rfind('\n}');s=s[:i]+'''
    /// <summary>Actual delivery admission/journal/state/final checks share one budget; late state completion preserves only the original release.</summary>
    [Theory]
    [InlineData("admission")][InlineData("journal")][InlineData("final")][InlineData("stage-save")][InlineData("terminal-save")]
    public async Task WholeDeliveryActorBudgetPreservesOriginalAndStateIoBoundary(string stage)
    {
        var (clock, advance) = PrivateOwnerDeadlineTestClock.Create(); var fixture = new CustodyFixtureClock { Now = clock.GetUtcNow() }; var identity = Identity(fixture);
        var backend = new InMemoryStateManager(); var authority = Authority(); var provider = Substitute.For<IExportKeyDirectDeliveryProvider>(); int releases = 0;
        var original = new ExportKeyDeliveryOutcome(identity, ExportKeyDeliveryState.Delivered, clock.GetUtcNow());
        provider.ReleaseAsync(identity, Arg.Any<CancellationToken>()).Returns(_ => { releases++; return original; });
        provider.LookupAsync(identity, Arg.Any<CancellationToken>()).Returns(_ => releases == 0 ? new(identity, ExportKeyDeliveryState.Unknown) : original);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); int admissions = 0;
        authority.AuthorizeOperationAsync(identity, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        { if (++admissions == (stage == "final" ? 3 : 1) && stage is "admission" or "final") { entered.TrySetResult(); return pending.Task; } return Task.FromResult(true); });
        if (stage == "journal") { authority.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(_ => { entered.TrySetResult(); return pending.Task; }); }
        var io = ActorPendingFixture.Suspended<ExportKeyDeliveryOutcome>(backend, stage == "stage-save" ? 1 : 4); bool stateIo = stage is "stage-save" or "terminal-save";
        var actor = Actor(identity, stateIo ? io.Manager : backend, clock, authority, provider); var reading = actor.DeliverAsync(identity);
        await (stateIo ? io.Entered : entered.Task).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            advance(TimeSpan.FromSeconds(30)); (await reading.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).State.ShouldBe(ExportKeyDeliveryState.Unavailable);
            if (stateIo) { int calls = io.Manager.ReceivedCalls().Count(); (await actor.LookupAsync(identity).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).State.ShouldBe(ExportKeyDeliveryState.Unavailable); io.Manager.ReceivedCalls().Count().ShouldBe(calls); }
            int effects = releases; io.Release.TrySetResult(); pending.TrySetResult(true); if (stateIo) { await io.Finished.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken); }
            releases.ShouldBe(effects);
            if (stage is "terminal-save" or "final")
            {
                (await actor.LookupAsync(identity)).ShouldBe(original); releases.ShouldBe(1);
                var stored = backend.CommittedState.Single(); var restored = new InMemoryStateManager(); await restored.SetStateAsync(stored.Key, JsonSerializer.Deserialize<ExportKeyDeliveryOutcome>(JsonSerializer.SerializeToUtf8Bytes(stored.Value))!); await restored.SaveStateAsync();
                (await Actor(identity, restored, clock, authority, provider).DeliverAsync(identity)).ShouldBe(original); releases.ShouldBe(1);
            }
        }
        finally { io.Release.TrySetResult(); pending.TrySetResult(true); }
    }
'''+s[i:];w(p,s)
p=r/'tests/Hexalith.Platform.Custody.Tests/CustodyKeyLifecycleTests.cs';s=p.read_text();i=s.rfind('\n}');s=s[:i]+'''
    /// <summary>Actual lifecycle permission/journal/state/final boundaries share one budget and retain original pins and effects through state-I/O abandonment.</summary>
    [Theory]
    [InlineData("admission")][InlineData("journal")][InlineData("final")][InlineData("stage-save")][InlineData("terminal-save")]
    public async Task WholeLifecycleActorBudgetPreservesOriginalAndStateIoBoundary(string stage)
    {
        var f = new CustodyKeyLifecycleFixture(); await f.Actor.RegisterWrappedAsync(CustodyKeyLifecycleFixture.Registration());
        var request = CustodyKeyLifecycleFixture.Request(CustodyKeyLifecycleAction.Pin, f.Anchor); var (clock, advance) = PrivateOwnerDeadlineTestClock.Create();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously); int admissions = 0;
        f.Authority.AuthorizeOperationAsync(request.Identity, request.OperationId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        { if (++admissions == (stage == "final" ? 2 : 1) && stage is "admission" or "final") { entered.TrySetResult(); return pending.Task; } return Task.FromResult(true); });
        if (stage == "journal") { f.Authority.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(_ => { entered.TrySetResult(); return pending.Task; }); }
        var io = ActorPendingFixture.Suspended<CustodyKeyLifecycleLedger>(f.Backend, stage == "stage-save" ? 1 : 4); bool stateIo = stage is "stage-save" or "terminal-save";
        var actor = CustodyKeyLifecycleFixture.Create(stateIo ? io.Manager : f.Backend, f.Authority, f, clock); var reading = actor.ApplyAsync(request);
        await (stateIo ? io.Entered : entered.Task).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            advance(TimeSpan.FromSeconds(30)); (await reading.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).Status.ShouldBe(CustodyKeyLifecycleStatus.Unavailable);
            if (stateIo) { int calls = io.Manager.ReceivedCalls().Count(); (await actor.LookupAsync(request).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).Status.ShouldBe(CustodyKeyLifecycleStatus.Unavailable); io.Manager.ReceivedCalls().Count().ShouldBe(calls); }
            int effects = f.Effects; io.Release.TrySetResult(); pending.TrySetResult(true); if (stateIo) { await io.Finished.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken); }
            f.Effects.ShouldBe(effects);
            if (stage is "terminal-save" or "final")
            {
                var original = await actor.LookupAsync(request); original.Status.ShouldBe(CustodyKeyLifecycleStatus.Pinned); f.Persisted.Keys.Single().Pins.Single().ShouldBe(request);
                var restored = new InMemoryStateManager(); await restored.SetStateAsync(f.Backend.CommittedState.Single().Key, JsonSerializer.Deserialize<CustodyKeyLifecycleLedger>(JsonSerializer.SerializeToUtf8Bytes(f.Persisted))!); await restored.SaveStateAsync();
                (await CustodyKeyLifecycleFixture.Create(restored, f.Authority, f, clock).ApplyAsync(request)).ShouldBe(original); f.Effects.ShouldBe(1);
            }
        }
        finally { io.Release.TrySetResult(); pending.TrySetResult(true); }
    }
'''+s[i:];w(p,s)
