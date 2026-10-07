import gzip, hashlib, json, pathlib, subprocess, xml.etree.ElementTree as ET
root = pathlib.Path('/home/administrator/projects/hexalith/agents')
packet = root / '_bmad-output/implementation-artifacts/tests/identity-completion-2026-10-07'
owner = root.parent / 'parties'
output = packet / 'final-verification'
output.mkdir(exist_ok=True)
initial = json.loads((packet / 'evidence.json').read_text())
records = []

def digest(path):
    p=pathlib.Path(path)
    return dict(path=str(p), bytes=p.stat().st_size, sha256=hashlib.sha256(p.read_bytes()).hexdigest())

def run(label, args, cwd):
    print('Running '+label, flush=True)
    log=output/(label+'.log')
    with log.open('w') as f:
        completed=subprocess.run(args,cwd=cwd,stdout=f,stderr=subprocess.STDOUT)
    record=dict(label=label, command=args, workingDirectory=str(cwd), exitCode=completed.returncode, log=str(log))
    records.append(record)
    (output/'commands.json').write_text(json.dumps(records,indent=2)+'\n')
    print(label+': exit '+str(completed.returncode),flush=True)
    if completed.returncode:
        print(log.read_text()[-6000:],flush=True)
        raise RuntimeError(label+' failed')
    return record

snapshots={}
for directory in ['post-change', 'review-fix-post-change']:
    for p in (packet/directory).rglob('*'):
        if p.is_file():
            relative=p.relative_to(packet/directory)
            if relative.parts[0] in ('eng','src','tests'):
                snapshots[relative]=p
source_before=[digest(owner/relative) for relative in snapshots]
harness_before=digest(packet/'verify-readiness-fixtures.py')
lanes=[]
for lane in initial['focusedSourceLanes']:
    prefix='query' if lane['project']=='Hexalith.Parties.Tests' else 'client'
    build=['dotnet']+lane['build']['arguments']
    index=build.index('--artifacts-path')+1
    build[index]='/tmp/hexalith-agents54-final-artifacts'
    build_record=run(prefix+'-build',build,owner)
    log=pathlib.Path(build_record['log']).read_text()
    assert '0 Warning(s)' in log and '0 Error(s)' in log,log[-1500:]
    test=['dotnet']+lane['test']['arguments']
    test[1]=test[1].replace('/tmp/hexalith-agents54-completion-artifacts','/tmp/hexalith-agents54-final-artifacts')
    test[test.index('-result-xml')+1]=str(output/(prefix+'-tests.xml'))
    test_record=run(prefix+'-tests',test,owner)
    xml=ET.parse(output/(prefix+'-tests.xml')).getroot()
    assembly=xml.find('assembly')
    assert int(assembly.attrib['failed'])==0 and int(assembly.attrib['errors'])==0 and int(assembly.attrib['skipped'])==0 and int(assembly.attrib.get('not-run','0'))==0,assembly.attrib
    assert int(assembly.attrib['passed'])==int(assembly.attrib['total']),assembly.attrib
    lanes.append(dict(project=lane['project'],result=assembly.attrib,assembly=digest(test[1]),build=build_record,test=test_record))

run('synthetic-verifier',['python3',str(packet/'verify-readiness-fixtures.py'),'--output',str(output/'synthetic-verifier')],root)
synthetic=json.loads((output/'synthetic-verifier/results.json').read_text())
assert all(x['exitCode']==0 and x.get('successReceipt') for x in synthetic['results'] if x['scenario'].startswith('positive'))
assert all(x['exitCode']!=0 and not x.get('successReceipt') for x in synthetic['results'] if not x['scenario'].startswith('positive'))
for p in (output/'synthetic-verifier').rglob('synthetic-source.json'):
    if p.stat().st_size>1024*1024:
        with p.open('rb') as source,gzip.open(str(p)+'.gz','wb') as target:
            import shutil
            shutil.copyfileobj(source,target)
        p.unlink()

parse = "$errors = @(); foreach ($file in @('eng/verify-ext-parties-history-readiness.ps1', 'eng/verify-ext-parties-1.ps1')) { $tokens = $null; $problems = $null; $null = [System.Management.Automation.Language.Parser]::ParseFile((Join-Path (Get-Location) $file), [ref]$tokens, [ref]$problems); $errors += $problems }; if ($errors.Count) { $errors | Out-String | Write-Output; exit 1 }; Write-Output 'Both verifier scripts parsed successfully.'"
run('powershell-parse',['pwsh','-NoProfile','-Command',parse],owner)
run('owning-whitespace',['git','-c','core.whitespace=cr-at-eol','diff','--check','--','eng','src','tests'],owner)
corrected=[]
for relative,p in snapshots.items():
    live=owner/relative
    assert p.read_bytes()==live.read_bytes(),str(live)+' differs from snapshot'
    corrected.append(digest(live))
for record in source_before:
    assert digest(record['path'])==record,'Source mutated during final verification'
assert digest(packet/'verify-readiness-fixtures.py')==harness_before,'Harness mutated during verification'
protected=[]
for relative,expected in [('_bmad-output/planning-artifacts/external-dependency-register.md','6f04931762d2cc2f355dd709bd40f32fea28b72a647b31063d4014994b51bb14'),('_bmad-output/implementation-artifacts/sprint-status.yaml','b72bc45b7a645d56a5981dbd1463f50a410ec39605c17be05222f6e6e2229691'),('_bmad-output/implementation-artifacts/spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md','ee4aa0ca2ef74c47d31e24f1c2acd429e43908daf4c333c5339abbdb964fb991')]:
    actual=digest(root/relative)
    assert actual['sha256']==expected,relative+' changed'
    protected.append(actual)
summary=dict(qualification='FocusedOwnerLocalSourceAndSyntheticVerificationOnly',executedBy='root-after-review-patches',reusedPassingLanes=False,sourceLanes=lanes,focusedTotal=sum(int(x['result']['passed']) for x in lanes),syntheticCases=len(synthetic['results']),syntheticResults=str(output/'synthetic-verifier/results.json'),correctedSourceFiles=corrected,harness=harness_before,protectedOriginalArtifacts=protected,liveSeamCalls=0,dependencyAcceptanceChanged=False,productionQualified=False,originalStoryClosed=False,commands=records)
(output/'evidence.json').write_text(json.dumps(summary,indent=2)+'\n')
manifest=[digest(p) for p in output.rglob('*') if p.is_file() and p.name!='artifact-manifest.json']
(output/'artifact-manifest.json').write_text(json.dumps(dict(artifacts=manifest),indent=2)+'\n')
print(str(summary['focusedTotal'])+' source tests; '+str(summary['syntheticCases'])+' synthetic cases; all protected artifacts unchanged.',flush=True)
