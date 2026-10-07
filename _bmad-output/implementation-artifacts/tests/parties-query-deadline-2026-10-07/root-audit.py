from pathlib import Path
import datetime
import fnmatch
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

base = Path('/home/administrator/projects/hexalith')
root = Path(__file__).parent
owner = base / 'parties/_bmad-output/implementation-artifacts/tests/query-deadline-2026-10-07'
packet = owner / sys.argv[1]
sha = lambda path: hashlib.sha256(Path(path).read_bytes()).hexdigest()
summary = json.loads((packet / 'evidence.json').read_text())
commands = json.loads((packet / 'commands.json').read_text())
assert all(c['exitCode'] == 0 and sha(c['combinedOutput']) == c['outputSha256'] for c in commands)
before = json.loads((packet / 'source-before.json').read_text())
after = json.loads((packet / 'source-after.json').read_text())
assert before == after and not summary['sourceDrift']
drift = {p: {'verified': h, 'current': sha(p) if Path(p).is_file() else None}
         for p, h in after.items() if not Path(p).is_file() or sha(p) != h}
artifacts = json.loads((packet / 'artifact-manifest.json').read_text())
assert artifacts and all(Path(p).is_file() and sha(p) == h for p, h in artifacts.items())
lanes = json.loads((packet / 'local-matrix/local-evidence.json').read_text())
assert len(lanes) == 10
results, distinct, party_tests = [], set(), []
for lane in lanes:
    tests = ET.parse(lane['XmlLog']).getroot().findall('.//test')
    assert tests and all(t.get('result') == 'Pass' for t in tests)
    for pattern in lane['Classes']:
        assert any(fnmatch.fnmatch(t.get('type', ''), pattern) for t in tests), pattern
    assert re.search(r'Total: \d+, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0', Path(lane['TestLog']).read_text())
    build = Path(lane['BuildLog']).read_text()
    assert '0 Warning(s)' in build and '0 Error(s)' in build
    distinct.update((lane['Project'], t.get('name')) for t in tests)
    results.append({'project': lane['Project'], 'passes': len(tests), 'requiredClasses': lane['Classes'],
                    'xmlSha256': sha(lane['XmlLog']), 'buildLogSha256': sha(lane['BuildLog']),
                    'testLogSha256': sha(lane['TestLog'])})
    if lane['Project'] == 'Hexalith.Parties.Tests':
        party_tests = tests
matrix = len(distinct)
extra_path = packet / 'operational-configuration.xml'
extra = (ET.parse(extra_path).getroot().findall('.//test') if extra_path.exists() else
         [t for t in party_tests if t.get('type').endswith('.PartyIdentityRetentionConfigurationTests')])
assert len(extra) == 6 and all(t.get('result') == 'Pass' for t in extra)
distinct.update(('Hexalith.Parties.Tests', t.get('name')) for t in extra)
assert len(distinct) == summary['distinctPasses']
endpoint = {t.get('name') for t in party_tests if t.get('type').endswith('.PartiesProcessEndpointTests')}
prior = base / 'parties/_bmad-output/implementation-artifacts/tests/http-qualification-2026-10-07/pre-change-endpoints.xml'
assert len(endpoint) == 13 and endpoint == {t.get('name') for t in ET.parse(prior).getroot().findall('.//test')}
security = [t for t in party_tests if t.get('type').endswith('.PartiesDomainServiceSecurityTests')]
assert len(security) == 35
deadline = [t.get('name') for t in party_tests if any(s in t.get('name', '') for s in
            ['WholeQueryDeadline_', 'InvalidQueryTimeout_', 'CallerCancellationRacesDeadlineAndFault_'])]
assert len(deadline) >= 28
review_required = {'BlockingProviderCancellationCallback_DoesNotDelayQueryCompletion': 12,
                  'WholeQueryDeadline_SetupTimeConsumesOperationBudget': 4,
                  'CallerCancellationAfterDeadlineOrFaultBeforeVerdict_PreservesOriginalToken': 4,
                  'ShortQueryDeadline_ImmediatelyBeforeExpiryStillResolves': 2,
                  'WholeQueryDeadline_FinalAuthorityExpiryDeniesTerminalEvidence': 2,
                  'ProviderCancellationCallbackFaultAfterDeadline_DoesNotAffectVerdict': 2}
if sys.argv[1] != 'initial-verification':
    for method, expected in review_required.items():
        assert sum(method + '(' in t.get('name', '') for t in party_tests) == expected, method
baseline = json.loads((root / 'initial-protected-state.json').read_text())
protected = {p: sha(p) for p in baseline if p.startswith('/') and not p.endswith('spec-5-4-owner-prerequisites.md')}
assert all(h == baseline[p] for p, h in protected.items())
spec = base / 'agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
frozen = spec.read_text().split('<frozen-after-approval', 1)[1].split('</frozen-after-approval>', 1)[0]
assert hashlib.sha256(frozen.encode()).hexdigest() == baseline['frozen_intent_sha256']
initial_security = json.loads((root / 'initial-security-and-runner-hashes.json').read_text())
security_delta = {p: {'initial': h, 'verified': after.get(p)} for p, h in initial_security.items() if after.get(p) != h}
owned = json.loads((owner / 'review-source-hashes.json').read_text())
owned_current = {p: sha(p) for p in owned}
owned_drift = {p: {'verified': after.get(p), 'current': h}
               for p, h in owned_current.items() if after.get(p) != h}
if sys.argv[1] != 'initial-verification':
    assert not owned_drift, 'Owned source differs from verified snapshot'
result = {'recordedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'packet': str(packet),
          'matrixPasses': matrix, 'configurationPasses': len(extra), 'distinctPasses': len(distinct),
          'lanes': results, 'deadlineExecutedNames': deadline, 'original13ExactNamesPreserved': True,
          'reviewRequiredCases': review_required if sys.argv[1] != 'initial-verification' else {},
          'sdkSecurityCases': len(security), 'sourceBeforeAfterMatch': True, 'sourceFiles': len(after),
          'currentSourceDrift': drift, 'wholeCurrentSourceMatches': not drift,
          'artifactFilesVerified': len(artifacts), 'allWarningFreeDebugBuilds': True,
          'protectedFilesUnchanged': protected, 'frozenIntentUnchanged': True,
          'securityAdmissionRunnerDeltaFromInitial': security_delta,
          'ownedCurrentSourceHashes': owned_current, 'ownedCurrentSourceDrift': owned_drift,
          'commandsSha256': sha(packet / 'commands.json'),
          'sourceManifestSha256': sha(packet / 'source-after.json'),
          'artifactManifestSha256': sha(packet / 'artifact-manifest.json'),
          'headsNow': {repo: subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=base / repo, text=True).strip()
                       for repo in ['agents', 'parties', 'eventstore', 'platform']}}
(root / f'root-{sys.argv[1]}-audit.json').write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps({k: result[k] for k in ['matrixPasses', 'configurationPasses', 'distinctPasses',
      'sourceFiles', 'artifactFilesVerified', 'wholeCurrentSourceMatches', 'currentSourceDrift',
      'securityAdmissionRunnerDeltaFromInitial']}, indent=2))
