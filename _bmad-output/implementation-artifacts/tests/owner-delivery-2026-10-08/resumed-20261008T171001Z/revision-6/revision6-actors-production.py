from pathlib import Path
import re
r=Path('/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody')
def w(p,s):p.write_bytes(s.replace('\r\n','\n').replace('\n','\r\n').encode())
p=r/'PrivateActorStateIoLifetime.cs';w(p,'''namespace Hexalith.Platform.Custody;

/// <summary>Turn-owned exclusion while an abandoned StateManager Task is unfinished. It grants no persistence or backend isolation authority.</summary>
internal sealed class PrivateActorStateIoLifetime
{
    private Task? _pending;
    internal void CheckReady()
    {
        if (_pending is { IsCompleted: false }) { throw new TimeoutException("Previous private actor state I/O is unfinished."); }
        if (_pending is not null) { _ = _pending.Exception; _pending = null; }
    }
    internal async Task<T> ReadAsync<T>(PrivateOwnerOperationDeadline budget, Func<Task<T>> operation)
    {
        budget.Check(); CheckReady();
        // Invocation remains on the actor turn. Only the returned task's wait is bounded.
        Task<T> pending = operation(); _pending = pending;
        return await budget.WaitAsync(pending).ConfigureAwait(false);
    }
    internal async Task ReadAsync(PrivateOwnerOperationDeadline budget, Func<Task> operation)
    {
        budget.Check(); CheckReady();
        Task pending = operation(); _pending = pending;
        await budget.WaitAsync(pending).ConfigureAwait(false);
    }
}
''')
p=r/'PrivateOwnerOperationDeadline.cs';s=p.read_text();i=s.rfind('\n}');s=s[:i]+'''
    /// <summary>Bounds an already invoked turn-owned state task without moving invocation or resuming abandoned actor work on a worker.</summary>
    internal async Task WaitAsync(Task pending)
    {
        _ = pending.ContinueWith(static task => { _ = task.Exception; }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        Check();
        try
        {
            await pending.WaitAsync(TimeSpan.FromSeconds(30) - clock.GetElapsedTime(_start), clock, token).ConfigureAwait(false);
            Check();
        }
        catch (Exception) { token.ThrowIfCancellationRequested(); throw; }
    }
    internal async Task<T> WaitAsync<T>(Task<T> pending)
    { await WaitAsync((Task)pending).ConfigureAwait(false); return await pending.ConfigureAwait(false); }
'''+s[i:];w(p,s)
def closing(s,start,op='(',cl=')'):
 depth=0;quote=None;escape=False
 for i in range(start,len(s)):
  c=s[i]
  if quote:
   if escape:escape=False
   elif c=='\\':escape=True
   elif c==quote:quote=None
   continue
  if c in ['"',"'"]:quote=c;continue
  if c==op:depth+=1
  elif c==cl:
   depth-=1
   if depth==0:return i
 raise ValueError('unclosed')
def prefix_budget(s,names):
 matches=list(re.finditer(r'(?<![.\w])('+ '|'.join(names)+r')\(',s))
 for m in reversed(matches):
  k=m.end();prefix=s[s.rfind('\n',0,m.start())+1:m.start()].strip()
  declaration=prefix.startswith('private ')
  ins='PrivateOwnerOperationDeadline budget' if declaration else 'budget'
  if s[k]!=')':ins+=', '
  s=s[:k]+ins+s[k:]
 return s
def external(s):
 matches=list(re.finditer(r'\b(authority|trustProvider|noIssueAuthority|originalReceiptAuthority)\.(\w+Async)\(',s))
 for m in reversed(matches):
  e=closing(s,m.end()-1);call=s[m.start():e+1];s=s[:m.start()]+'budget.ReadAsync(() => '+call+')'+s[e+1:]
 return s
def state_io(s):
 matches=list(re.finditer(r'\bStateManager\.\w+(?:<[^>]+>)?\(',s))
 for m in reversed(matches):
  e=closing(s,m.end()-1);call=s[m.start():e+1];s=s[:m.start()]+'_stateIo.ReadAsync(budget, () => '+call+')'+s[e+1:]
 return s
def wrap_public(s,name,anchor,fallback,create_budget=False):
 match=re.search(r'public async Task<[^\n]+> '+name+r'\(',s);start=s.index('{',match.end());end=closing(s,start,'{','}')
 body=s[start+1:end]
 if create_budget:body='\n        var budget = new PrivateOwnerOperationDeadline(_clock, CancellationToken.None);'+body
 a=body.index(anchor)+len(anchor);a=body.index('\n',a)
 intro=body[:a];tail=body[a:]
 body=intro+'\n        try\n        {\n            budget.Check(); _stateIo.CheckReady();'+tail+'\n        }\n        catch (TimeoutException) { return '+fallback+'; }\n    '
 return s[:start+1]+body+s[end:]
for name in ['DeletionCapabilitySigningActor.cs','ExportKeyDeliveryActor.cs','CustodyKeyLifecycleActor.cs']:
 p=r/name;s=p.read_text();s=s.replace('    private const string StateKey', '    private readonly PrivateActorStateIoLifetime _stateIo = new();\n    private const string StateKey')
 methods=['ReadAsync','SaveAsync','ReadPendingAsync','PersistPendingAsync','PersistTargetAsync']
 if name.startswith('Deletion'):methods.append('ResolveTrustAsync')
 if name.startswith('Custody'):methods.append('AdmitAsync')
 s=prefix_budget(s,methods)
 s=s.replace('ReadPendingAsync, PersistPendingAsync', '() => ReadPendingAsync(budget), pendingValue => PersistPendingAsync(budget, pendingValue)')
 s=s.replace('authority, PersistTargetAsync,', 'new DeadlineAnchoredStateAuthority(authority, budget), nextValue => PersistTargetAsync(budget, nextValue),')
 s=s.replace(')), authority,\n', ')), new DeadlineAnchoredStateAuthority(authority, budget),\n')
 s=s.replace('CommitAsync(pending, authority,', 'CommitAsync(pending, new DeadlineAnchoredStateAuthority(authority, budget),')
 if name.startswith('Custody'):
  s=s.replace('=> authority?.AuthorizeOperationAsync(identity, id, method, digest) ?? Task.FromResult(false);', '=> authority is null ? Task.FromResult(false) : budget.ReadAsync(() => authority.AuthorizeOperationAsync(identity, id, method, digest));')
  # This expression was wrapped explicitly; keep one deadline around it.
  s=s.replace('budget.ReadAsync(() => authority.AuthorizeOperationAsync(identity, id, method, digest))', 'authority.AuthorizeOperationAsync(identity, id, method, digest)')
 s=external(s);s=state_io(s)
 s=s.replace('catch (Exception) { admittedTrust = null; }', 'catch (Exception) { budget.Check(); admittedTrust = null; }')
 s=s.replace('catch (Exception) { return new(', 'catch (Exception) { budget.Check(); return new(')
 s=s.replace('catch (Exception) { return pending; }', 'catch (Exception) { budget.Check(); return pending; }').replace('catch (Exception) { return original; }', 'catch (Exception) { budget.Check(); return original; }')
 if name.startswith('Deletion'):
  for method in ['SignAsync','LookupAsync']:
   s=wrap_public(s,method,'string id = Check(payload);','new(id, payload, DeletionCapabilitySigningState.Unavailable)')
  s=wrap_public(s,'ObsoleteUnissuedAsync','string id = Check(payload); var unavailable = new DeletionCapabilitySigningOutcome(id, payload, DeletionCapabilitySigningState.Unavailable);','unavailable')
  s=wrap_public(s,'ReadSuccessorProofAsync','string id = Check(payload);','null',True)
 elif name.startswith('Export'):
  for method in ['DeliverAsync','LookupAsync']:
   s=wrap_public(s,method,'Check(identity);','new(identity, ExportKeyDeliveryState.Unavailable)')
 else:
  for method,argument in [('RegisterWrappedAsync','registration'),('ApplyAsync','request'),('LookupAsync','request')]:
   anchor='var missing = Missing('+argument+'.Identity, '+argument+'.OperationId, digest);'
   s=wrap_public(s,method,anchor,'missing',method=='RegisterWrappedAsync')
 # Explicit terminal check after final permissions, before successful evidence release.
 s=s.replace('        return result;\n', '        budget.Check(); return result;\n')
 w(p,s)
