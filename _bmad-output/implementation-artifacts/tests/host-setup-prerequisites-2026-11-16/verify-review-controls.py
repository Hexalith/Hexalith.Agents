"""Focused probes of the reproducer's actual overlay and optimized XML gate; run with run-with-temp."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as E


def require(condition, message):
    if not condition:
        raise SystemExit(message)


report = Path(__file__).resolve().parent
root = report.parents[3]
script = (report / 'verify-local.sh').read_text()
gate = script.split("python3 - <<'PY'\n", 1)[1].split('\nPY\n', 1)[0]
overlay = script[script.index('rsync -a --delete'):script.index('\ncd "$TMPDIR/repo"')]
manifest = json.loads((report / 'verification.json').read_text())
totals = manifest['expectedFocusedTotals']
required = manifest['requiredMethodCaseCounts']
results = []
output = Path(sys.argv[1]).resolve()
output.mkdir(parents=True, exist_ok=True)

with tempfile.TemporaryDirectory(dir=os.environ['TMPDIR']) as temporary:
    scratch = Path(temporary)
    source = scratch / 'source'
    target = scratch / 'repo'
    for relative in ['src/deleted.cs', 'test/deleted.cs', 'src/kept.cs',
                     '_bmad-output/archive/artifacts/catalog-reader/Archived.csproj']:
        path = source / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text('tracked control\n')
    shutil.copytree(source, target)
    for relative in ['.git/retained', 'references/Hexalith.Builds/retained', 'artifacts/retained']:
        path = target / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text('excluded control\n')
    (source / 'src/deleted.cs').unlink()
    (source / 'test/deleted.cs').unlink()
    (source / 'src/kept.cs').write_text('current source\n')
    environment = dict(os.environ, PREREQUISITE_ROOT=str(source), TMPDIR=str(scratch))
    subprocess.run(['bash', '-c', overlay], env=environment, check=True)
    require(not (target / 'src/deleted.cs').exists() and not (target / 'test/deleted.cs').exists(),
            'Overlay retained a deleted source/test file')
    require((target / 'src/kept.cs').read_text() == 'current source\n', 'Overlay missed current source')
    for relative in ['.git/retained', 'references/Hexalith.Builds/retained', 'artifacts/retained',
                     '_bmad-output/archive/artifacts/catalog-reader/Archived.csproj']:
        require((target / relative).is_file(), 'Overlay removed protected/archive control: ' + relative)
    results.append({'name': 'overlay-deletion-and-preservation', 'passed': True})

    def fixture(directory):
        groups = {name: [] for name in totals}
        for identity, count in required.items():
            owner, method = identity.rsplit('.', 1)
            lane = 'ui-composition' if owner.startswith('Hexalith.Agents.UI.Tests.') else (
                'query' if owner.endswith('.ProviderQueryEndpointPrerequisiteTests') else 'host-boundaries')
            groups[lane].extend((owner, method, identity + ':' + str(i)) for i in range(count))
        for lane, total in totals.items():
            document = E.Element('assemblies')
            assembly = E.SubElement(document, 'assembly', total=str(total), passed=str(total),
                                    failed='0', errors='0', skipped='0', **{'not-run': '0'})
            collection = E.SubElement(assembly, 'collection')
            rows = groups[lane]
            rows.extend(('Harness.ExistingCases', 'Existing', lane + ':' + str(i)) for i in range(total - len(rows)))
            for owner, method, name in rows:
                E.SubElement(collection, 'test', type=owner, method=method, name=name, result='Pass')
            E.ElementTree(document).write(directory / (lane + '.xml'))

    for scenario in ['valid', 'missing-xml', 'malformed-xml', 'failed-summary',
                     'failed-row-with-forged-summary', 'malformed-summary', 'missing-method', 'wrong-method-owner']:
        directory = scratch / scenario
        directory.mkdir()
        fixture(directory)
        file = directory / 'query.xml'
        if scenario == 'missing-xml':
            file.unlink()
        elif scenario == 'malformed-xml':
            file.write_text('<assemblies>')
        elif scenario != 'valid':
            document = E.parse(file)
            assembly = document.getroot().find('assembly')
            row = assembly.find('.//test')
            if scenario == 'failed-summary':
                assembly.set('failed', '1')
                assembly.set('passed', str(totals['query'] - 1))
                row.set('result', 'Fail')
            elif scenario == 'failed-row-with-forged-summary':
                row.set('result', 'Fail')
            elif scenario == 'malformed-summary':
                assembly.set('passed', 'malformed')
            elif scenario == 'missing-method':
                row.set('method', 'SubstitutedMethod')
            elif scenario == 'wrong-method-owner':
                row.set('type', 'Harness.ForeignOwner')
            document.write(file)
        environment = dict(os.environ, PREREQUISITE_ROOT=str(root), PREREQUISITE_EVIDENCE=str(directory),
                           PYTHONOPTIMIZE='1')
        result = subprocess.run([sys.executable, '-O', '-c', gate], env=environment, capture_output=True, text=True)
        expected_success = scenario == 'valid'
        require((result.returncode == 0) == expected_success, 'Optimized XML gate accepted/refused incorrectly: ' + scenario)
        results.append({'name': 'optimized-xml-' + scenario, 'passed': True,
                        'exitCode': result.returncode, 'diagnostic': (result.stdout + result.stderr)[-2000:]})

(output / 'review-controls.json').write_text(json.dumps({'passed': len(results), 'cases': results}, indent=2) + '\n')
print(f'{len(results)}/{len(results)} focused overlay/XML controls passed with PYTHONOPTIMIZE=1')
