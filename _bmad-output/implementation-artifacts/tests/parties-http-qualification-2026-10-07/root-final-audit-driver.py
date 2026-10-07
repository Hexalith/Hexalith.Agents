from pathlib import Path
import hashlib,json,re,fnmatch,datetime,xml.etree.ElementTree as E,difflib
base=Path('/home/administrator/projects/hexalith')
owner=base/'parties/_bmad-output/implementation-artifacts/tests/http-qualification-2026-10-07'
packet=owner/'review-final'
root=base/'agents/_bmad-output/implementation-artifacts/tests/parties-http-qualification-2026-10-07'
sha=lambda p:hashlib.sha256(Path(p).read_bytes()).hexdigest()
summary=json.loads((packet/'evidence.json').read_text())
assert not summary['sourceDrift']
commands=json.loads((packet/'commands.json').read_text())
assert len(commands)==6 and all(c['exitCode']==0 and sha(c['combinedOutput'])==c['outputSha256'] for c in commands)
before=json.loads((packet/'source-before.json').read_text());after=json.loads((packet/'source-after.json').read_text())
assert before==after
assert all(Path(p).is_file() and sha(p)==h for p,h in after.items()),'Source differs from verified snapshot'
artifacts=json.loads((packet/'artifact-manifest.json').read_text())
assert artifacts and all(Path(p).is_file() and sha(p)==h for p,h in artifacts.items())
old=json.loads((root/'root-matrix-audit.json').read_text())
lanes=[];distinct=set();alltests={}
for lane in old['lanes']:
 project=lane['project'];xml=packet/'local-matrix'/f'{project}-tests.xml'
 tests=E.parse(xml).getroot().findall('.//test')
 assert tests and all(t.get('result')=='Pass' for t in tests)
 for pattern in lane['requiredClasses']:assert any(fnmatch.fnmatch(t.get('type'),pattern) for t in tests),pattern
 log=packet/'local-matrix'/f'{project}-tests.log';s=log.read_text()
 assert re.search(r'Total: \d+, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0',s)
 build=packet/'local-matrix'/f'{project}-build.log';s=build.read_text()
 assert '0 Warning(s)' in s and '0 Error(s)' in s
 for t in tests:distinct.add((project,t.get('name')))
 alltests[project]=tests
 lanes.append({'project':project,'passes':len(tests),'xmlSha256':sha(xml),'testLogSha256':sha(log),'buildLogSha256':sha(build),'requiredClasses':lane['requiredClasses']})
assert len(lanes)==10
matrix=len(distinct)
original=E.parse(owner/'pre-change-endpoints.xml').getroot().findall('.//test')
original_names={t.get('name') for t in original}
endpoint=[t for t in alltests['Hexalith.Parties.Tests'] if t.get('type').endswith('.PartiesProcessEndpointTests')]
security=[t for t in alltests['Hexalith.Parties.Tests'] if t.get('type').endswith('.PartiesDomainServiceSecurityTests')]
assert len(original_names)==13 and {t.get('name') for t in endpoint}==original_names
assert len(security)==35
extras=[]
for name in ['host-health.xml','architecture.xml','shared-replay.xml','custody.xml']:
 xml=packet/name;r=E.parse(xml).getroot();tests=r.findall('.//test');assert tests and all(t.get('result')=='Pass' for t in tests)
 project=Path(r.find('.//assembly').get('name')).stem
 new={(project,t.get('name')) for t in tests}-distinct
 distinct.update((project,t.get('name')) for t in tests)
 extras.append({'xml':name,'passes':len(tests),'additionalDistinct':len(new),'sha256':sha(xml)})
assert len(distinct)==summary['distinctPasses']
baseline=json.loads((root/'root-baseline.json').read_text())
protected={k:sha(base/k) for k,h in baseline['files'].items() if not k.endswith('Program.cs')}
assert all(h==baseline['files'][k] for k,h in protected.items())
spec=base/'agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
frozen=re.search(r'<frozen-after-approval[\s\S]*?</frozen-after-approval>',spec.read_text()).group(0)
assert hashlib.sha256(frozen.encode()).hexdigest()==baseline['frozenIntentSha256']
initial=json.loads((owner/'source-manifest.json').read_text())
sdk=[p for p in after if '/eventstore/src/Hexalith.EventStore.DomainService/' in p or '/eventstore/src/Hexalith.EventStore.ServiceDefaults/Authentication/' in p]
assert sdk and all(p in initial and initial[p]['sha256']==after[p] for p in sdk),'SDK audit/auth source drift'
owned=[p for p,v in initial.items() if v.get('owned')]
diff='';owned_hashes={}
for name in owned:
 p=Path(name);repo=p.relative_to(base).parts[0];relative=p.relative_to(base/repo)
 prior=owner/'pre-change'/('eventstore' if repo=='eventstore' else '')/relative
 oldtext=prior.read_text() if prior.is_file() else ''
 diff+=''.join(difflib.unified_diff(oldtext.splitlines(True),p.read_text().splitlines(True),fromfile=f'a/{repo}/{relative}',tofile=f'b/{repo}/{relative}'))
 owned_hashes[name]={'beforeSha256':sha(prior) if prior.is_file() else None,'finalSha256':sha(p)}
(root/'review/final-reviewed.diff').write_text(diff)
(root/'review/final-owned-source-hashes.json').write_text(json.dumps(owned_hashes,indent=2)+'\n')
result={'recordedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'lanes':lanes,'matrixPasses':matrix,'original13ExactNamesPreserved':True,'securityPasses':len(security),'extras':extras,'distinctPasses':len(distinct),'sourceFiles':len(after),'sourceBeforeAfterAndCurrentMatch':True,'artifactFilesVerified':len(artifacts),'allWarningFreeDebugBuilds':True,'protectedFilesUnchanged':protected,'frozenIntentUnchanged':True,'sdkAuditAuthenticationFilesUnchanged':len(sdk),'ownerEvidenceSha256':sha(packet/'evidence.json'),'sourceManifestSha256':sha(packet/'source-after.json'),'artifactManifestSha256':sha(packet/'artifact-manifest.json'),'commandsSha256':sha(packet/'commands.json'),'reviewedDiffSha256':sha(root/'review/final-reviewed.diff')}
(root/'root-final-audit.json').write_text(json.dumps(result,indent=2)+'\n')
(root/'root-final-audit-driver.py').write_text(Path(__file__).read_text())
print(json.dumps({k:result[k] for k in ['matrixPasses','original13ExactNamesPreserved','securityPasses','extras','distinctPasses','sourceFiles','artifactFilesVerified','sdkAuditAuthenticationFilesUnchanged']},indent=2))
