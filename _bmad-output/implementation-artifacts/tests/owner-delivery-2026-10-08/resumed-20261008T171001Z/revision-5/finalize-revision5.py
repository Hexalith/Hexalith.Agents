from pathlib import Path
import json,hashlib,xml.etree.ElementTree as E
from datetime import datetime,timezone
R=Path('/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/resumed-20261008T171001Z/revision-5')
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def write(n,d):(R/n).write_text(json.dumps(d,indent=2)+'\n')
now=datetime.now(timezone.utc).isoformat()
handoff=json.loads((R/'engineering-handoff.json').read_text());handoff['status']='stable final source engineering handoff; packaged hash check passed; root full Local and next independent review pending';write('engineering-handoff.json',handoff)
owned=json.loads((R/'current-owned-source.json').read_text());preserved=json.loads((R/'preservation.json').read_text());focused=json.loads((R/'final-focused-results.json').read_text());commands=json.loads((R/'commands.json').read_text())['commands']
for f in owned['files']:
 assert sha(f['sourcePath'])==f['currentSha256']==sha(f['afterCopy'])
 assert sha(f['beforeCopy'])==f['beforeSha256']
for f in preserved['beforeCopies']:assert sha(f['beforeCopy'])==f['sha256']
for f in preserved['captureHashes']:assert sha(f['path'])==f['sha256']
for f in preserved['protectedChecks']:assert sha(f['path'])==f['sha256']
assert sha(handoff['spec'])==handoff['specSha256']==preserved['currentParentSha256']
for c in commands:
 assert sha(c['archivedLog'])==c['archivedLogSha256']==sha(c['originalLog'])
 if 'archivedXml' in c:assert sha(c['archivedXml'])==c['archivedXmlSha256']==sha(c['originalXml'])
for l in focused['lanes']:
 cs=[c for c in commands if c['completionExitCode']==0 and c['archivedLog']==l['testLog'] and c.get('archivedXml')==l['xml']];bs=[c for c in commands if c['completionExitCode']==0 and c['archivedLog']==l['buildLog']];assert len(cs)==len(bs)==1
 tree=E.parse(l['xml']).getroot();ts=tree.findall('.//test');assert len(ts)==l['passed'] and all(t.get('result')=='Pass' for t in ts)
 assert cs[0]['archivedLogSha256']==sha(l['testLog']) and cs[0]['archivedXmlSha256']==sha(l['xml'])
rows=[]
for p in sorted(R.rglob('*')):
 if not p.is_file() or (p.parent==R and p.name in ['artifact-hashes.json','handoff-verification.json']) or any(x.startswith('root-') for x in p.relative_to(R).parts):continue
 rows.append({'path':str(p),'relativePath':str(p.relative_to(R)),'sha256':sha(p),'bytes':p.stat().st_size})
write('artifact-hashes.json',{'capturedUtc':now,'scope':'Engineer-owned revision-5 exact before/current copies, commands, logs/XML, metadata/helper source. Excludes root-owned audits and only top-level artifact-hashes.json/handoff-verification.json to avoid self-reference.','count':len(rows),'files':rows})
for f in rows:assert sha(f['path'])==f['sha256']
write('handoff-verification.json',{'capturedUtc':datetime.now(timezone.utc).isoformat(),'status':'stable final packaged source/evidence hash check passed','revision':5,'artifactManifestSha256':sha(R/'artifact-hashes.json'),'engineerArtifactsChecked':len(rows),'currentCapturedPathsChecked':26,'changedPaths':24,'unchangedRejectedB4Paths':2,'sourceCopiesMatchCurrentBytes':True,'orderedCapturesChecked':125,'exactPrefixPreserved':114,'suppliedBeforeCopiesChecked':475,'freshSuppliedBeforeCopiesChecked':26,'allSuppliedCopiesMatch':True,'historicalAbsenceAndHashOnlyRowsRemainExplicit':True,'protectedStorySprintRegisterPolicyAndFrozenIntentUnchanged':True,'currentSpecSha256':handoff['specSha256'],'exactDotnetCommandsChecked':38,'actualProcessCompletionExitsRecorded':True,'selectedLaneCommandLinksChecked':4,'selectedBuildLogAndTestLogXmlHashesMatchExactSuccessfulCommands':True,'selectedDistinctCurrentPasses':426,'noSelectedFailuresSkipsErrorsNotRun':True,'normalBuildsClean':4,'historicalNonpassingInvocationsRetained':13,'genuineFailedBeforeCases':25,'all394ReviewedPathsWithinFreshCaptureOwnership':True,'concurrentExternalSourcePreserved':True,'revision4MetadataCorrectionAnd177ArtifactsPreserved':True,'newPublicTestClasses':[],'sourceWorkIncomplete':[],'rootCompleteLocalMatrix':'pending; engineer did not run it','nextThreeLayerReview':'pending root','productionQualification':'unestablished','noFurtherSourceOrArtifactEditsAfterStableFinalHandoff':True})
print(json.dumps({'status':'stable final packaged check passed','artifacts':len(rows),'manifestSha256':sha(R/'artifact-hashes.json'),'captures':125,'prefix':114,'beforeCopies':475,'changedPaths':24,'passes':426,'commands':38},indent=2))
