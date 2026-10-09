from pathlib import Path
import json, hashlib, re
from datetime import datetime, timezone
from collections import Counter
import xml.etree.ElementTree as ET

R = Path('/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/resumed-20261008T171001Z/revision-7')

def sha(p):
    return hashlib.sha256(Path(p).read_bytes()).hexdigest()

def read(name):
    return json.loads((R / name).read_text())

def write(p, v):
    p.write_text(json.dumps(v, indent=2) + '\n')

owned = read('current-owned-source.json')
assert owned['uniqueCapturedPaths'] == owned['changedPaths'] == len(owned['files']) == 21
for f in owned['files']:
    assert Path(f['sourcePath']).is_file() == f['existsNow'], f
    assert f['existsNow'] and f['existedBefore'] and f['changed'], f
    assert sha(f['sourcePath']) == sha(f['afterCopy']) == f['currentSha256'], f
    assert sha(f['beforeCopy']) == f['beforeSha256'], f
    if Path(f['sourcePath']).suffix == '.cs':
        before = Path(f['beforeCopy']).read_bytes()
        current = Path(f['sourcePath']).read_bytes()
        assert (b'\r\n' in before) == (b'\r\n' in current), f
        if b'\r\n' in before:
            assert b'\n' not in current.replace(b'\r\n', b''), f

ordered = read('ordered-baselines.json')
prior = json.loads((R.parent / 'revision-6/ordered-baselines.json').read_text())
assert len(ordered) == 148 and len(prior) == 142 and ordered[:142] == prior
preserved = read('preservation.json')
assert len(preserved['beforeCopies']) == preserved['suppliedBeforeCopiesChecked'] == 559
assert preserved['freshSuppliedBeforeCopiesChecked'] == 22
for f in preserved['beforeCopies']:
    assert sha(f['beforeCopy']) == f['sha256'], f
for f in preserved['captureHashes']:
    assert sha(f['path']) == f['sha256'], f
assert [f['path'] for f in preserved['captureHashes']] == ordered
for f in preserved['protectedChecks']:
    assert sha(f['path']) == f['sha256'], f
assert sha('/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md') == preserved['currentParentSha256']
assert preserved['frozenParentSha256'] == '0cd11ddac96af9775593ea0aa24d969b235b32dfcc4a76a80e51d7eb9c82e2cb'
previous_manifest = R.parent / 'revision-6/artifact-hashes.json'
assert sha(previous_manifest) == preserved['revision6ArtifactManifestSha256'] == '7302f5e7d09059ddf17f24517df6fb1e724cd3e68cd8bcc461dcb6158aad98aa'
previous_files = json.loads(previous_manifest.read_text())['files']
assert len(previous_files) == 289
for f in previous_files:
    assert sha(f['path']) == f['sha256'], f
comparison = read('final-reviewed-source-comparison.json')
assert comparison['externalBytesPreserved']
assert len(comparison['outsideRevision7CapturedPaths']) == 6
for f in comparison['outsideRevision7CapturedPaths']:
    assert sha(f['sourcePath']) == f['currentSha256'], f

commands = read('commands.json')['commands']
assert len(commands) == 25
for c in commands:
    assert c['completionExitCode'] in [0, 1] and c['completionToolOutputTimestamp']
    assert c['argv'][0] == 'dotnet' and c['workingDirectory'] and c['toolCallId']
    assert sha(c['archivedLog']) == c['archivedLogSha256'], c
    assert sha(c['originalLog']) == c['archivedLogSha256'], c
    if c.get('archivedXml'):
        assert sha(c['archivedXml']) == c['archivedXmlSha256'], c
        assert sha(c['originalXml']) == c['archivedXmlSha256'], c
results = read('final-focused-results.json')
assert results['distinctPassedCases'] == 400 and len(results['lanes']) == 4
assert len(results['exactSuccessfulCommandLinks']) == 4
identities = set()
for lane in results['lanes']:
    assert lane['passed'] > 0 and not any(lane[k] for k in ['failed', 'skipped', 'errors', 'notRun'])
    tests = ET.parse(lane['xml']).getroot().findall('.//test')
    assert len(tests) == lane['passed'], lane
    classes = Counter()
    for t in tests:
        assert t.attrib['result'] == 'Pass', t.attrib
        identity = (t.attrib.get('type'), t.attrib.get('name'))
        assert identity not in identities, identity
        identities.add(identity)
        classes[t.attrib['type']] += 1
    assert dict(classes) == lane['classes'], lane
for link in results['exactSuccessfulCommandLinks']:
    cs = [c for c in commands if c['archivedLog'] == link['log'] and c.get('archivedXml') == link['xml'] and c['completionExitCode'] == 0]
    bs = [c for c in commands if c['archivedLog'] == link['buildLog'] and c['completionExitCode'] == 0]
    assert len(cs) == len(bs) == 1
    assert sha(link['log']) == link['logSha256'] and sha(link['xml']) == link['xmlSha256'] and sha(link['buildLog']) == link['buildLogSha256']
    build = Path(link['buildLog']).read_text()
    assert 'Build succeeded.' in build and '0 Warning(s)' in build and '0 Error(s)' in build
    assert '-c' in bs[0]['argv'] and 'Debug' in bs[0]['argv'] and '-p:UseHexalithProjectReferences=true' in bs[0]['argv']
    assert not any('RunAnalyzers=false' in a or 'GenerateDocumentationFile=false' in a or a.startswith('-p:NoWarn=') for a in bs[0]['argv'])
assert len(identities) == 400
classes = read('required-class-audit.json')
assert classes['allEditedClassesActuallyExecutedInCorrectProject'] and classes['editedPublicTestClassesRemainExisting']
assert not classes['newPublicTestClasses'] and not classes['ownerRunnerSourceChanges']
findings = read('finding-status.json')
assert len(findings['requiredFindings']) == 10 and not findings['sourceWorkIncomplete']

h = R / 'engineering-handoff.json'
v = json.loads(h.read_text())
v['status'] = 'stable final source engineering handoff; packaged hash check passed; root full Local and next independent review pending'
v['finalCheckUtc'] = datetime.now(timezone.utc).isoformat()
write(h, v)
paths = sorted(p for p in R.rglob('*') if p.is_file() and p.name not in ['artifact-hashes.json', 'handoff-verification.json'])
manifest = {
    'capturedUtc': datetime.now(timezone.utc).isoformat(),
    'scope': 'Every stable revision-7 file except this manifest and its separate verification seal, avoiding circular hashes. Ordered prefix/history hashes and current exact source are independently verified in preservation/current-owned-source.',
    'files': [{'path': str(p), 'sha256': sha(p)} for p in paths]
}
write(R / 'artifact-hashes.json', manifest)
for f in manifest['files']:
    assert sha(f['path']) == f['sha256'], f
verification = {
    'capturedUtc': datetime.now(timezone.utc).isoformat(),
    'status': 'pass; final stable source/evidence/package hash check',
    'artifactManifest': str(R / 'artifact-hashes.json'),
    'artifactManifestSha256': sha(R / 'artifact-hashes.json'),
    'packagedFilesChecked': len(paths),
    'currentSourcePathsChecked': len(owned['files']),
    'orderedCapturesChecked': len(ordered),
    'preservedPrefix': 142,
    'suppliedBeforeCopiesChecked': len(preserved['beforeCopies']),
    'freshSuppliedBeforeCopiesChecked': preserved['freshSuppliedBeforeCopiesChecked'],
    'previousRevisionArtifactsChecked': len(previous_files),
    'classifiedConcurrentPathsPreserved': 6,
    'selectedSuccessfulCommandLinksChecked': 4,
    'exactDotnetCommandsChecked': len(commands),
    'distinctPassingCases': 400,
    'freshIndependentReview': 'pending root',
    'completeRootLocalMatrix': 'pending root',
    'sourceOrPacketEditsAfterFinalHandoff': 'none authorized; engineering stops after delivery'
}
write(R / 'handoff-verification.json', verification)
print(json.dumps(verification, indent=2))
