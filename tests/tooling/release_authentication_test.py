"""Both protected publication paths require guarded NuGet trusted publishing."""

from __future__ import annotations

import json
import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / '.github/workflows/release.yml'
RECOVERY_WORKFLOW = ROOT / '.github/workflows/recover-release.yml'
LOGIN_ACTION = 'NuGet/login@8d196754b4036150537f80ac539e15c2f1028841'
LOGIN_NAME = 'Authenticate NuGet trusted publishing'
RELEASE_NAME = 'Semantic Release'
RECOVERY_NAME = 'Recover original archives without release planning'
PREFLIGHT_NAME = 'Require exact publication flag and current green recovery source'


def job(workflow: str, name: str) -> str:
    """Read one complete top-level job without inheriting neighboring settings."""
    return re.split(r'\n  (?=\S)', workflow.split(f'\n  {name}:\n', 1)[1], maxsplit=1)[0]


def step(job_text: str, name: str) -> str:
    """Read one named step without inheriting neighboring guards or credentials."""
    return job_text.split(f'      - name: {name}\n', 1)[1].split('\n      - ', 1)[0].rstrip('\n') + '\n'


class ReleaseAuthenticationTests(unittest.TestCase):
    def test_both_publishers_consume_only_their_pinned_trusted_login_output(self) -> None:
        for path, job_name, publisher_name in (
            (WORKFLOW, 'release', RELEASE_NAME),
            (RECOVERY_WORKFLOW, 'recover', RECOVERY_NAME),
        ):
            with self.subTest(workflow=path.name):
                workflow = path.read_text()
                publication = job(workflow, job_name)
                login = step(publication, LOGIN_NAME)
                publisher = step(publication, publisher_name)
                self.assertIn('id: nuget-login\n', login)
                self.assertIn(f'uses: {LOGIN_ACTION} # v1.2.0\n', login)
                self.assertIn('user: ${{ vars.NUGET_TRUSTED_PUBLISHING_USER }}\n', login)
                self.assertEqual(workflow.count(f'uses: {LOGIN_ACTION}'), 1)
                self.assertEqual(re.findall(r'^\s+NUGET_API_KEY: (.+)$', workflow, re.MULTILINE),
                                 ['${{ steps.nuget-login.outputs.NUGET_API_KEY }}'])
                self.assertIn('NUGET_API_KEY: ${{ steps.nuget-login.outputs.NUGET_API_KEY }}\n', publisher)
                self.assertLess(publication.index(f'- name: {LOGIN_NAME}'),
                                publication.index(f'- name: {publisher_name}'))

    def test_legacy_secrets_and_authentication_fallbacks_are_absent(self) -> None:
        for path in (WORKFLOW, RECOVERY_WORKFLOW):
            with self.subTest(workflow=path.name):
                workflow = path.read_text()
                self.assertNotRegex(workflow, r'\bsecrets\s*(?:\.|\[)')
                self.assertNotIn('nuget-auth', workflow)
                self.assertNotIn('Select NuGet authentication', workflow)
                self.assertNotIn('method=api-key', workflow)
                self.assertNotIn('method=trusted-publishing', workflow)

    def test_each_publication_job_has_its_own_oidc_permission_and_production_environment(self) -> None:
        for path, job_name in ((WORKFLOW, 'release'), (RECOVERY_WORKFLOW, 'recover')):
            with self.subTest(workflow=path.name):
                publication = job(path.read_text(), job_name)
                header = publication.split('    steps:\n', 1)[0]
                self.assertIn('    environment: production\n', header)
                permissions = re.split(r'\n    (?=\S)', header.split('    permissions:\n', 1)[1], maxsplit=1)[0]
                self.assertIn('      id-token: write\n', permissions + '\n')

    def test_login_failures_cannot_be_ignored_or_bypassed_by_publishers(self) -> None:
        for path, job_name, publisher_name in (
            (WORKFLOW, 'release', RELEASE_NAME),
            (RECOVERY_WORKFLOW, 'recover', RECOVERY_NAME),
        ):
            with self.subTest(workflow=path.name):
                publication = job(path.read_text(), job_name)
                header = publication.split('    steps:\n', 1)[0]
                self.assertNotIn('continue-on-error', header)
                guarded_steps = step(publication, LOGIN_NAME) + step(publication, publisher_name)
                self.assertNotRegex(guarded_steps, r'continue-on-error|always\(\)|failure\(\)|!cancelled\(\)|set \+e|\|\|')

    def test_normal_release_keeps_prepare_source_and_planning_guards(self) -> None:
        workflow = WORKFLOW.read_text()
        publication = job(workflow, 'release')
        header = publication.split('    steps:\n', 1)[0]
        self.assertIn('needs: [verify-source, plan-release]', header)
        for condition in (
            "needs.verify-source.result == 'success'",
            "needs.verify-source.outputs.publish-enabled == 'true'",
            "needs.plan-release.result == 'success'",
            "needs.plan-release.outputs.release-required == 'true'",
        ):
            self.assertIn(condition, header)
        for name in (LOGIN_NAME, RELEASE_NAME):
            self.assertIn("if: ${{ steps.prepare.outputs.publish-enabled == 'true' }}\n", step(publication, name))
        self.assertLess(publication.index('- name: Prepare domain release'),
                        publication.index(f'- name: {LOGIN_NAME}'))
        prepare = step(publication, 'Prepare domain release')
        self.assertIn('publication-flag: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}', prepare)
        self.assertIn('nuget-user: ${{ vars.NUGET_TRUSTED_PUBLISHING_USER }}', prepare)
        self.assertIn('package-manifest: eng/release-packages.json', prepare)
        self.assertIn('expected-package-count: 6', prepare)
        source = step(job(workflow, 'verify-source'), 'Require the current green main tip')
        self.assertIn('python3 scripts/verify-release-source.py', source)
        self.assertIn('--dispatch-ref "$DISPATCH_REF"', source)
        self.assertIn('--dispatch-sha "$DISPATCH_SHA"', source)

    def test_recovery_keeps_main_freeze_and_exact_original_source_guards(self) -> None:
        workflow = RECOVERY_WORKFLOW.read_text()
        publication = job(workflow, 'recover')
        self.assertIn("    if: github.ref == 'refs/heads/main' && vars.HEXALITH_RELEASE_PUBLISH_ENABLED == 'true'\n",
                      publication.split('    steps:\n', 1)[0])
        preflight = step(publication, PREFLIGHT_NAME)
        self.assertLess(publication.index(f'- name: {PREFLIGHT_NAME}'),
                        publication.index(f'- name: {LOGIN_NAME}'))
        self.assertIn('GH_TOKEN: ${{ github.token }}', preflight)
        self.assertIn('REPOSITORY: ${{ github.repository }}', preflight)
        self.assertIn('DISPATCH_REF: ${{ github.ref }}', preflight)
        self.assertIn('SOURCE_SHA: ${{ inputs.source-sha }}', preflight)
        self.assertIn('HEXALITH_RELEASE_PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}', preflight)
        self.assertNotIn('continue-on-error', publication)
        # Default step execution requires success; explicit guards must preserve it.
        for name in (PREFLIGHT_NAME, LOGIN_NAME, RECOVERY_NAME):
            guarded_step = step(publication, name)
            conditions = re.findall(r'^        if: (.+)$', guarded_step, re.MULTILINE)
            for condition in conditions:
                self.assertIn(condition, ('success()', '${{ success() }}'))
            self.assertNotRegex(guarded_step, r'always\(\)|failure\(\)|cancelled\(\)|set \+e|\|\|')
        publisher = step(publication, RECOVERY_NAME)
        self.assertIn('HEXALITH_RELEASE_PUBLISH_ENABLED: ${{ vars.HEXALITH_RELEASE_PUBLISH_ENABLED }}', publisher)
        self.assertIn('SOURCE_SHA: ${{ inputs.source-sha }}', publisher)
        self.assertIn('test "$GITHUB_SHA" = "$SOURCE_SHA"', publisher)
        self.assertIn('python3 scripts/recover-release-packages.py "$VERSION" --source-sha "$SOURCE_SHA"', publisher)
        self.assertNotIn('semantic-release', workflow)
        self.assertIn('  group: release-production\n', workflow)

    def test_recovery_preflight_refuses_invalid_inputs_and_source_proof_before_login(self) -> None:
        publication = job(RECOVERY_WORKFLOW.read_text(), 'recover')
        preflight = step(publication, PREFLIGHT_NAME)
        script = '\n'.join(line[10:] for line in preflight.split('        run: |\n', 1)[1].splitlines())
        sha = 'a' * 40
        cases = [(repr(flag), flag, sha, 0) for flag in
                 (None, '', 'false', 'True', 'TRUE', ' true', 'true ', '\ttrue', 'true\n', 'true')]
        cases.extend((('mismatched source', 'true', 'b' * 40, 0),
                      ('failed source proof', 'true', sha, 17)))
        for label, flag, source, proof_status in cases:
            with self.subTest(case=label), tempfile.TemporaryDirectory() as temporary:
                directory = Path(temporary)
                scripts = directory / 'scripts'
                scripts.mkdir()
                (scripts / 'verify-release-source.py').write_text(
                    'import json, os, sys\n'
                    'from pathlib import Path\n'
                    'Path("proof.json").write_text(json.dumps({"arguments": sys.argv[1:], '
                    '"authenticated": bool(os.environ.get("GH_TOKEN"))}))\n'
                    'raise SystemExit(int(os.environ["TEST_PROOF_STATUS"]))\n',
                    encoding='utf-8')
                login = directory / 'login-requested'
                environment = {**os.environ, 'GH_TOKEN': 'fixture-token',
                               'GITHUB_SHA': sha, 'SOURCE_SHA': source,
                               'REPOSITORY': 'Hexalith/Hexalith.Agents',
                               'DISPATCH_REF': 'refs/heads/main',
                               'TEST_PROOF_STATUS': str(proof_status), 'LOGIN_MARKER': str(login)}
                environment.pop('HEXALITH_RELEASE_PUBLISH_ENABLED', None)
                if flag is not None:
                    environment['HEXALITH_RELEASE_PUBLISH_ENABLED'] = flag
                # The marker represents the next, success-guarded NuGet/login step.
                result = subprocess.run(['bash', '-c', script + '\n: > "$LOGIN_MARKER"\n'],
                                        cwd=directory, env=environment, capture_output=True,
                                        text=True, timeout=10, check=False)
                valid_input = flag == 'true' and source == sha
                self.assertEqual(result.returncode == 0, valid_input and proof_status == 0)
                self.assertEqual(login.exists(), valid_input and proof_status == 0)
                proof = directory / 'proof.json'
                self.assertEqual(proof.exists(), valid_input)
                if proof.exists():
                    self.assertEqual(json.loads(proof.read_text()), {
                        'arguments': ['--repository', 'Hexalith/Hexalith.Agents', '--workflow', 'ci.yml',
                                      '--dispatch-ref', 'refs/heads/main', '--dispatch-sha', sha],
                        'authenticated': True,
                    })

    def test_publication_freeze_enables_only_the_exact_true_string(self) -> None:
        workflow = WORKFLOW.read_text()
        gate = step(job(workflow, 'verify-source'), 'Apply publication freeze')
        script = '\n'.join(line[10:] for line in gate.split('        run: |\n', 1)[1].splitlines())
        for flag in (None, '', 'false', 'True', ' true', 'true ', 'true'):
            with self.subTest(flag=flag), tempfile.TemporaryDirectory() as directory:
                output = Path(directory) / 'output'
                environment = {**os.environ, 'GITHUB_OUTPUT': str(output),
                               'GITHUB_STEP_SUMMARY': str(Path(directory) / 'summary')}
                environment.pop('HEXALITH_RELEASE_PUBLISH_ENABLED', None)
                if flag is not None:
                    environment['HEXALITH_RELEASE_PUBLISH_ENABLED'] = flag
                result = subprocess.run(['bash', '-c', script], env=environment,
                                        capture_output=True, text=True, timeout=10, check=True)
                expected = 'true' if flag == 'true' else 'false'
                self.assertEqual(output.read_text(), f'publish-enabled={expected}\n')
                self.assertEqual(result.stderr, '')


if __name__ == '__main__':
    unittest.main()
