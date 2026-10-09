from pathlib import Path
r=Path('/home/administrator/projects/hexalith/platform/src/Hexalith.Platform.Custody')
for name,outcome,enum,methods in [('DeletionCapabilitySigningActor.cs','DeletionCapabilitySigningOutcome','DeletionCapabilitySigningState',['SignAsync','LookupAsync']),('ExportKeyDeliveryActor.cs','ExportKeyDeliveryOutcome','ExportKeyDeliveryState',['DeliverAsync','LookupAsync']),('CustodyKeyLifecycleActor.cs','CustodyKeyLifecycleOutcome','CustodyKeyLifecycleStatus',['ApplyAsync','LookupAsync'])]:
 p=r/name;s=p.read_text()
 # Physical uncertainty remains the existing safe response; it carries no signed/delivered/consumed evidence.
 if name.startswith('Deletion'):
  s=s.replace('catch (Exception) { budget.Check(); return new(requestId, payload, DeletionCapabilitySigningState.Unknown); }','catch (TimeoutException) { return new(requestId, payload, DeletionCapabilitySigningState.Unknown); }\n        catch (Exception) { budget.Check(); return new(requestId, payload, DeletionCapabilitySigningState.Unknown); }')
  s=s.replace('catch (Exception) { budget.Check(); return new(id, payload, DeletionCapabilitySigningState.Unknown); }','catch (TimeoutException) { return new(id, payload, DeletionCapabilitySigningState.Unknown); }\n        catch (Exception) { budget.Check(); return new(id, payload, DeletionCapabilitySigningState.Unknown); }')
 elif name.startswith('Export'):
  s=s.replace('catch (Exception) { budget.Check(); return new(identity, ExportKeyDeliveryState.Unknown); }','catch (TimeoutException) { return new(identity, ExportKeyDeliveryState.Unknown); }\n        catch (Exception) { budget.Check(); return new(identity, ExportKeyDeliveryState.Unknown); }')
  s=s.replace('catch (Exception) { budget.Check(); return new(expected, ExportKeyDeliveryState.Unknown); }','catch (TimeoutException) { return new(expected, ExportKeyDeliveryState.Unknown); }\n        catch (Exception) { budget.Check(); return new(expected, ExportKeyDeliveryState.Unknown); }')
 else:
  s=s.replace('catch (Exception) { budget.Check(); return pending; }','catch (TimeoutException) { return pending; }\n        catch (Exception) { budget.Check(); return pending; }').replace('catch (Exception) { budget.Check(); return original; }','catch (TimeoutException) { return original; }\n        catch (Exception) { budget.Check(); return original; }')
 # Restrict timeout fallback to an uncertainty already returned by the physical recovery core.
 for method in methods:
  start=s.index('public async Task<'+outcome+'> '+method+'(');end=s.index('\n    private ',start) if '\n    private ' in s[start:] else len(s)
  # A public next method is bounded by the current method's timeout catch, so use that exact catch endpoint.
  ce=s.index('catch (TimeoutException)',start);ce=s.index('\n',ce)
  body=s[start:ce]
  body=body.replace('        try\n', '        '+outcome+'? uncertainty = null;\n        try\n',1)
  if name.startswith('Custody') and method=='LookupAsync':
   needle='            var result = state is not null && key is not null && prior?.RequestDigest == digest ? await ResolveAsync(state, key, request, prior, budget).ConfigureAwait(false) : missing;'
  else:
   core = ('SignAsyncCoreAsync(payload, budget)' if method=='SignAsync' else 'LookupAsyncCoreAsync(payload, budget)') if name.startswith('Deletion') else ('DeliverAsyncCoreAsync(identity, budget)' if method=='DeliverAsync' else 'LookupAsyncCoreAsync(identity, budget)') if name.startswith('Export') else 'ApplyCoreAsync(request, digest, budget)'
   needle='            var result = await '+core+'.ConfigureAwait(false);'
  assert needle in body,(name,method)
  property='Status' if name.startswith('Custody') else 'State'
  body=body.replace(needle,needle+'\n            if (result.'+property+' == '+enum+'.Unknown) { uncertainty = result; }')
  body=body.replace('catch (TimeoutException) { return ', 'catch (TimeoutException) { return uncertainty ?? ')
  s=s[:start]+body+s[ce:]
 p.write_bytes(s.replace('\n','\r\n').encode())
r=Path('/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests')
for name,enum,prop in [('DeletionCapabilitySigningActorTests.cs','DeletionCapabilitySigningState','State'),('ExportKeyDeliveryActorTests.cs','ExportKeyDeliveryState','State'),('CustodyKeyLifecycleTests.cs','CustodyKeyLifecycleStatus','Status')]:
 p=r/name;s=p.read_text();idx=s.index('public async Task Whole');before=s[:idx];tail=s[idx:];needle=')).'+prop+'.ShouldBe('+enum+'.Unavailable);';tail=tail.replace(needle,')).'+prop+'.ShouldBe(stage == "terminal-save" ? '+enum+'.Unknown : '+enum+'.Unavailable);',1);p.write_bytes((before+tail).replace('\n','\r\n').encode())
