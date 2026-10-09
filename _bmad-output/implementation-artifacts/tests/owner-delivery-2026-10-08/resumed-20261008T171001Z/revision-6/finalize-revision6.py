from pathlib import Path
import json,hashlib
from datetime import datetime,timezone
R=Path('/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/resumed-20261008T171001Z/revision-6')
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def write(p,v):p.write_text(json.dumps(v,indent=2)+'\n')
owned=json.loads((R/'current-owned-source.json').read_text())
for f in owned['files']:
 assert Path(f['sourcePath']).is_file()==f['existsNow']
 if f['existsNow']:assert sha(f['sourcePath'])==sha(f['afterCopy'])==f['currentSha256'],f
ordered=json.loads((R/'ordered-baselines.json').read_text());prior=json.loads((R.parent/'revision-5/ordered-baselines.json').read_text());assert len(ordered)==142 and ordered[:125]==prior
preserved=json.loads((R/'preservation.json').read_text())
for f in preserved['beforeCopies']:assert sha(f['beforeCopy'])==f['sha256'],f
for f in preserved['captureHashes']:assert sha(f['path'])==f['sha256'],f
for f in preserved['protectedChecks']:assert sha(f['path'])==f['sha256'],f
assert sha('/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md')==preserved['currentParentSha256']
commands=json.loads((R/'commands.json').read_text())['commands'];assert len(commands)==71
for c in commands:
 assert sha(c['archivedLog'])==c['archivedLogSha256'],c
 if c.get('archivedXml'):assert sha(c['archivedXml'])==c['archivedXmlSha256'],c
results=json.loads((R/'final-focused-results.json').read_text());assert results['distinctPassedCases']==575 and len(results['lanes'])==4
for link in results['exactSuccessfulCommandLinks']:
 cs=[c for c in commands if c['archivedLog']==link['log'] and c.get('archivedXml')==link['xml'] and c['completionExitCode']==0]
 bs=[c for c in commands if c['archivedLog']==link['buildLog'] and c['completionExitCode']==0]
 assert len(cs)==len(bs)==1 and sha(link['log'])==link['logSha256'] and sha(link['xml'])==link['xmlSha256'] and sha(link['buildLog'])==link['buildLogSha256']
h=R/'engineering-handoff.json';v=json.loads(h.read_text());v['status']='stable final source engineering handoff; packaged hash check passed; root full Local and next independent review pending';write(h,v)
paths=sorted(p for p in R.rglob('*') if p.is_file() and p.name not in ['artifact-hashes.json','handoff-verification.json'])
manifest={'capturedUtc':datetime.now(timezone.utc).isoformat(),'scope':'Every stable revision-6 file except this manifest and its separate verification seal, avoiding circular hashes. Prefix/history hashes and external exact sources are independently verified in preservation/current-owned-source.','files':[{'path':str(p),'sha256':sha(p)} for p in paths]};write(R/'artifact-hashes.json',manifest)
for f in manifest['files']:assert sha(f['path'])==f['sha256'],f
verification={'capturedUtc':datetime.now(timezone.utc).isoformat(),'status':'pass; final stable source/evidence/package hash check','artifactManifest':str(R/'artifact-hashes.json'),'artifactManifestSha256':sha(R/'artifact-hashes.json'),'packagedFilesChecked':len(paths),'currentSourcePathsChecked':len(owned['files']),'orderedCapturesChecked':len(ordered),'preservedPrefix':125,'suppliedBeforeCopiesChecked':len(preserved['beforeCopies']),'freshSuppliedBeforeCopiesChecked':preserved['freshSuppliedBeforeCopiesChecked'],'selectedSuccessfulCommandLinksChecked':4,'exactDotnetCommandsChecked':len(commands),'distinctPassingCases':575,'freshIndependentReview':'pending root','completeRootLocalMatrix':'pending root','sourceOrPacketEditsAfterFinalHandoff':'none authorized; engineering stops after delivery'};write(R/'handoff-verification.json',verification)
print(json.dumps(verification,indent=2))
