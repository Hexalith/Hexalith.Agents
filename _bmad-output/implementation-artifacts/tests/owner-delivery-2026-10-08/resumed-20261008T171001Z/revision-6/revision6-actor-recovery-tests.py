from pathlib import Path
import json
r=Path('/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/resumed-20261008T171001Z/revision-6');p=r/'ordered-baselines.json';v=json.loads(p.read_text());v.append(str(r/'actor-late-state-recovery/before-hashes.json'));p.write_text(json.dumps(v,indent=2)+'\n')
r=Path('/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests')
p=r/'AnchoredFixtureJournal.cs';s=p.read_text().replace('    private static readonly ConditionalWeakTable<IAnchoredStateTransitionAuthority, StrongBox<bool>> Availability = new();','''    private static readonly ConditionalWeakTable<IAnchoredStateTransitionAuthority, StrongBox<bool>> Availability = new();
    private static readonly ConditionalWeakTable<IAnchoredStateTransitionAuthority, StrongBox<(Func<Task> Before, Action Finished)?>> RecordHooks = new();
    internal static void SuspendRecord(IAnchoredStateTransitionAuthority authority, Func<Task> before, Action finished)
        => RecordHooks.GetValue(authority, _ => new(null)).Value = (before, finished);''')
s=s.replace('authority.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call =>\n        {','authority.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(async call =>\n        {\n            var hook = RecordHooks.GetValue(authority, _ => new(null)).Value;\n            try\n            {\n                if (hook is { } suspended) { await suspended.Before(); }',1)
s=s.replace('proofs[transition.TargetDigest] = JsonSerializer.SerializeToUtf8Bytes(transition); advance(transition.TargetRevision, transition.TargetDigest); return true;\n        });','proofs[transition.TargetDigest] = JsonSerializer.SerializeToUtf8Bytes(transition); advance(transition.TargetRevision, transition.TargetDigest); return true;\n            }\n            finally { hook?.Finished(); }\n        });',1);p.write_bytes(s.replace('\n','\r\n').encode())
for filename,auth,lookup,mutate,enum,backend in [
 ('DeletionCapabilitySigningActorTests.cs','authority','actor.LookupAsync(payload)','actor.SignAsync(payload)','DeletionCapabilitySigningState','backend'),
 ('ExportKeyDeliveryActorTests.cs','authority','actor.LookupAsync(identity)','actor.DeliverAsync(identity)','ExportKeyDeliveryState','backend'),
 ('CustodyKeyLifecycleTests.cs','f.Authority','actor.LookupAsync(request)','actor.ApplyAsync(request)','CustodyKeyLifecycleStatus','f.Backend')]:
 p=r/filename;s=p.read_text();start=s.index('    public async Task Whole')
 a=s[:start];b=s[start:]
 old=f'        if (stage == "journal") {{ {auth}.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(_ => {{ entered.TrySetResult(); return pending.Task; }}); }}'
 new=f'''        var journalFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (stage == "journal") {{ AnchoredFixtureJournal.SuspendRecord({auth}, async () => {{ entered.TrySetResult(); await pending.Task; }}, () => journalFinished.TrySetResult()); }}'''
 assert old in b,filename;b=b.replace(old,new,1)
 anchor={'DeletionCapabilitySigningActorTests.cs':'            signatures.ShouldBe(effects);','ExportKeyDeliveryActorTests.cs':'            releases.ShouldBe(effects);','CustodyKeyLifecycleTests.cs':'            f.Effects.ShouldBe(effects);'}[filename]
 extra=f'''
            if (stage is "stage-save" or "journal")
            {{
                if (stage == "journal") {{ await journalFinished.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken); }}
                byte[] pendingBytes = JsonSerializer.SerializeToUtf8Bytes({backend}.CommittedState.Single(v => v.Value is AnchoredStateTransition).Value);
                int records = {auth}.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IAnchoredStateTransitionAuthority.RecordTransitionAsync));
                var read = await {lookup};
                {'read.State' if enum != 'CustodyKeyLifecycleStatus' else 'read.Status'}.ShouldBe(stage == "stage-save" ? {enum}.Unavailable : {enum}.Unknown);
                {auth}.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IAnchoredStateTransitionAuthority.RecordTransitionAsync)).ShouldBe(records);
                if (stage == "stage-save") {{ JsonSerializer.SerializeToUtf8Bytes({backend}.CommittedState.Single(v => v.Value is AnchoredStateTransition).Value).ShouldBe(pendingBytes); }}
                var recovered = await {mutate}; {'recovered.State' if enum != 'CustodyKeyLifecycleStatus' else 'recovered.Status'}.ShouldBe({enum}.Unknown);
'''
 if enum=='DeletionCapabilitySigningState':
  extra+='''                recovered.Payload.ShouldBe(payload); recovered.SigningRequestId.ShouldBe(id); signatures.ShouldBe(0);
                var stored = backend.CommittedState.Single(); var restored = new InMemoryStateManager();
                await restored.SetStateAsync(stored.Key, JsonSerializer.Deserialize<DeletionCapabilitySigningOutcome>(JsonSerializer.SerializeToUtf8Bytes(stored.Value))!, TestContext.Current.CancellationToken); await restored.SaveStateAsync(TestContext.Current.CancellationToken);
                (await Actor(payload, restored, authority, provider, trust, clock: clock).LookupAsync(payload)).State.ShouldBe(DeletionCapabilitySigningState.Unknown); signatures.ShouldBe(0);
'''
 elif enum=='ExportKeyDeliveryState':
  extra+='''                recovered.Identity.ShouldBe(identity); releases.ShouldBe(0);
                var stored = backend.CommittedState.Single(); var restored = new InMemoryStateManager();
                await restored.SetStateAsync(stored.Key, JsonSerializer.Deserialize<ExportKeyDeliveryOutcome>(JsonSerializer.SerializeToUtf8Bytes(stored.Value))!, TestContext.Current.CancellationToken); await restored.SaveStateAsync(TestContext.Current.CancellationToken);
                (await Actor(identity, restored, clock, authority, provider).LookupAsync(identity)).State.ShouldBe(ExportKeyDeliveryState.Unknown); releases.ShouldBe(0);
'''
 else:
  extra+='''                recovered.OriginalRequest.ShouldBe(request); f.Persisted.Keys.Single().Pins.Single().ShouldBe(request); f.Effects.ShouldBe(0);
                var restored = new InMemoryStateManager(); await restored.SetStateAsync(f.Backend.CommittedState.Single().Key, JsonSerializer.Deserialize<CustodyKeyLifecycleLedger>(JsonSerializer.SerializeToUtf8Bytes(f.Persisted))!, TestContext.Current.CancellationToken); await restored.SaveStateAsync(TestContext.Current.CancellationToken);
                (await CustodyKeyLifecycleFixture.Create(restored, f.Authority, f, clock).LookupAsync(request)).Status.ShouldBe(CustodyKeyLifecycleStatus.Unknown); f.Effects.ShouldBe(0);
'''
 extra+='            }'
 b=b.replace(anchor,anchor+extra,1);s=a+b;p.write_bytes(s.replace('\n','\r\n').encode())
