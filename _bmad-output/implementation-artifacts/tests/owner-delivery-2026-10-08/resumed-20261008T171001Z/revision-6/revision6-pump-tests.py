from pathlib import Path
r=Path('/home/administrator/projects/hexalith/conversations')
def w(p,s):p.write_bytes(s.replace('\n','\r\n').encode())
p=r/'src/Hexalith.Conversations.Server/Agents/ConversationDeletionDeliveryPump.cs';s=p.read_text().replace('    IConversationDeletionReceiver receiver)','    IConversationDeletionReceiver receiver, TimeProvider? timeProvider = null)').replace('    /// <summary>Delivers one', '    private TimeProvider OperationClock => timeProvider ?? TimeProvider.System;\n    /// <summary>Delivers one');w(p,s)
p=r/'tests/Hexalith.Conversations.Server.Tests/Agents/ConversationDeletionDeliveryPumpFixture.cs';s=p.read_text()
s=s.replace('        internal F Source', '        internal Func<string, Task>? OperationHook;\n        internal F Source')
s=s.replace('internal ConversationDeletionDeliveryPump Pump() =>','internal ConversationDeletionDeliveryPump Pump(TimeProvider? clock = null) =>').replace('this), this);','this), this, clock);')
s=s.replace('public Task<ConversationAgentAuthorization> AuthorizeAsync', 'public async Task<ConversationAgentAuthorization> AuthorizeAsync')
s=s.replace('''            token.ThrowIfCancellationRequested(); operation.ShouldBeOneOf''','''            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("authority") ?? Task.CompletedTask); operation.ShouldBeOneOf''')
s=s.replace('return Task.FromResult(new ConversationAgentAuthorization(', 'return new ConversationAgentAuthorization(').replace('BadBinding != "organization"));','BadBinding != "organization");')
s=s.replace('''            token.ThrowIfCancellationRequested(); SourceReads++;''','''            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("source") ?? Task.CompletedTask); SourceReads++;''')
s=s.replace('''            token.ThrowIfCancellationRequested(); command.Metadata.ActorPartyId''','''            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("record:" + command.Action) ?? Task.CompletedTask); command.Metadata.ActorPartyId''')
s=s.replace('''public Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken token) { token.ThrowIfCancellationRequested();''','''public async Task<string?> CurrentTargetAsync(TenantId tenant, CancellationToken token) { token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("target") ?? Task.CompletedTask);''')
s=s.replace('return TargetPending.Task; } return Task.FromResult<string?>(Target);','return await TargetPending.Task; } return Target;')
s=s.replace('public Task<ConversationDeletionReceiverResult> LookupAsync', 'public async Task<ConversationDeletionReceiverResult> LookupAsync')
s=s.replace('''            token.ThrowIfCancellationRequested(); ReceiverReads++;
            if (BlockLookup)''','''            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("lookup") ?? Task.CompletedTask); ReceiverReads++;
            if (BlockLookup)''').replace('return LookupPending.Task;', 'return await LookupPending.Task;')
s=s.replace('return Task.FromResult(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Unavailable));','return new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Unavailable);')
s=s.replace('return Task.FromResult(Receipts.TryGetValue(target, out var receipt) ? new(ConversationAgentsOutcome.Available, receipt)\n                : new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Absent));','return Receipts.TryGetValue(target, out var receipt) ? new(ConversationAgentsOutcome.Available, receipt)\n                : new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Absent);')
s=s.replace('public Task<ConversationDeletionReceiverResult> SubmitAsync', 'public async Task<ConversationDeletionReceiverResult> SubmitAsync')
s=s.replace('''            token.ThrowIfCancellationRequested(); Submissions++;''','''            token.ThrowIfCancellationRequested(); await (OperationHook?.Invoke("submit") ?? Task.CompletedTask); Submissions++;''')
s=s.replace('return Task.FromResult(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Denied));','return new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Denied);')
s=s.replace('return Task.FromException<ConversationDeletionReceiverResult>(new HttpRequestException("Controlled lost receiver response."));','throw new HttpRequestException("Controlled lost receiver response.");')
s=s.replace('return Task.FromResult(new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Available, receipt));','return new ConversationDeletionReceiverResult(ConversationAgentsOutcome.Available, receipt);')
w(p,s)
p=r/'tests/Hexalith.Conversations.Server.Tests/Agents/ConversationDeletionDeliveryPumpTests.cs';s=p.read_text();i=s.rfind('\n}')
inline=[]
for mode in [False,True]:
 for stage in (['authority','source'] if mode else ['authority','source','target','lookup','submit','record:Attempt']):
  for invocation in [False,True]:
   for cancellation in [False,True]:
    inline.append('    [InlineData("'+stage+'", '+str(mode).lower()+', '+str(invocation).lower()+', '+str(cancellation).lower()+')]')
s=s[:i]+'''
    /// <summary>Standalone pump entries bound actual dependency Tasks and synchronous invocations; late results resume no later protocol phase.</summary>
    [Theory]
'''+ '\n'.join(inline)+'''
    public async Task StandalonePumpBudgetBoundsEveryDependency(string stage, bool lookupOnly, bool invocation, bool cancellation)
    {
        var f = await Approved(); var clock = new PumpClock(); using var caller = new CancellationTokenSource(); using var unblock = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); int pauses = 0;
        f.OperationHook = async operation =>
        {
            if (operation != stage || Interlocked.Increment(ref pauses) != 1) { return; }
            entered.TrySetResult(); if (invocation) { unblock.Wait(); } else { await released.Task; }
            finished.TrySetResult();
        };
        var pump = f.Pump(clock);
        var pending = Task.Run(async () => lookupOnly ? (int)await pump.LookupAcknowledgementAsync(f.Entry, caller.Token) : (int)await pump.DeliverAsync(f.Entry, caller.Token), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            if (cancellation) { caller.Cancel(); var error = await Should.ThrowAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)); error.CancellationToken.ShouldBe(caller.Token); }
            else { clock.Advance(TimeSpan.FromSeconds(30)); (await pending.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).ShouldBe(lookupOnly ? (int)SourcePublicationDeliveryStatus.Unavailable : (int)ConversationAgentsOutcome.Unavailable); }
            (await f.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
            int attempts = f.AttemptCommands.Count; int submissions = f.Submissions;
            unblock.Set(); released.TrySetResult(); await finished.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            f.OperationHook = null;
            if (stage == "submit")
            {
                SpinWait.SpinUntil(() => f.Receipts.ContainsKey(f.Target), TimeSpan.FromSeconds(2)).ShouldBeTrue();
                (await f.Pump().DeliverAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(ConversationAgentsOutcome.Available);
                f.Submissions.ShouldBe(1); f.AttemptCommands.Count.ShouldBe(1);
                (await new ConversationDeletionPublicationDelivery(f.Pump()).LookupAcknowledgementAsync(f.Entry, TestContext.Current.CancellationToken)).ShouldBe(SourcePublicationDeliveryStatus.Acknowledged);
            }
            else
            {
                if (stage == "record:Attempt") { SpinWait.SpinUntil(() => f.AttemptCommands.Count == 1, TimeSpan.FromSeconds(2)).ShouldBeTrue(); }
                else { f.AttemptCommands.Count.ShouldBe(attempts); }
                f.Submissions.ShouldBe(submissions); (await f.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(0);
            }
        }
        finally { unblock.Set(); released.TrySetResult(); }
    }

    private sealed class PumpClock : TimeProvider
    {
        private long _ticks;
        private readonly List<PumpTimer> _timers = [];
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public override DateTimeOffset GetUtcNow() => F.At.AddTicks(GetTimestamp());
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        { var timer = new PumpTimer(callback, state, GetTimestamp() + dueTime.Ticks); lock (_timers) { _timers.Add(timer); } return timer; }
        internal void Advance(TimeSpan elapsed)
        { long now = Interlocked.Add(ref _ticks, elapsed.Ticks); PumpTimer[] timers; lock (_timers) { timers = _timers.ToArray(); } foreach (var timer in timers) { timer.Fire(now); } }
        private sealed class PumpTimer(TimerCallback callback, object? state, long due) : ITimer
        {
            private int _disposed;
            internal void Fire(long now) { if (now >= due && Interlocked.Exchange(ref _disposed, 1) == 0) { callback(state); } }
            public bool Change(TimeSpan dueTime, TimeSpan period) => false;
            public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
            public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
        }
    }
'''+s[i:];w(p,s)
