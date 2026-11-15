"""The protected release selects an existing key or requires trusted publishing."""

from __future__ import annotations

import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
WORKFLOW = ROOT / '.github/workflows/release.yml'


class ReleaseAuthenticationTests(unittest.TestCase):
    def test_existing_key_and_missing_key_select_the_expected_route_without_disclosure(self) -> None:
        workflow = WORKFLOW.read_text()
        selector = workflow.split('      - name: Select NuGet authentication\n', 1)[1].split('      - name:', 1)[0]
        self.assertIn("if: ${{ steps.prepare.outputs.publish-enabled == 'true' }}", selector)
        self.assertIn('NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}', selector)
        script = '\n'.join(line[10:] for line in selector.split('        run: |\n', 1)[1].splitlines())
        for key, expected in [('synthetic-key-for-selection-only', 'api-key'), ('', 'trusted-publishing')]:
            with self.subTest(expected=expected), tempfile.TemporaryDirectory() as directory:
                output = Path(directory) / 'output'
                environment = {**os.environ, 'NUGET_API_KEY': key, 'GITHUB_OUTPUT': str(output)}
                result = subprocess.run(['bash', '-c', script], env=environment,
                                        capture_output=True, text=True, timeout=10, check=True)
                self.assertEqual(output.read_text(), f'method={expected}\n')
                self.assertEqual(result.stdout, '')
                self.assertEqual(result.stderr, '')
                if key:
                    self.assertNotIn(key, output.read_text())

    def test_both_authentication_routes_retain_the_protected_publication_gate(self) -> None:
        workflow = WORKFLOW.read_text()
        login = workflow.split('      - name: Authenticate NuGet trusted publishing\n', 1)[1].split('      - name:', 1)[0]
        release = workflow.split('      - name: Semantic Release\n', 1)[1].split('\n  verify-publication:', 1)[0]
        self.assertIn("if: ${{ steps.prepare.outputs.publish-enabled == 'true' && steps.nuget-auth.outputs.method == 'trusted-publishing' }}", login)
        self.assertIn('uses: NuGet/login@8d196754b4036150537f80ac539e15c2f1028841', login)
        self.assertIn("if: ${{ steps.prepare.outputs.publish-enabled == 'true' }}", release)
        self.assertIn('NUGET_API_KEY: ${{ secrets.NUGET_API_KEY || steps.nuget-login.outputs.NUGET_API_KEY }}', release)
        self.assertIn('environment: production', workflow)
        self.assertNotRegex(login + release, re.compile(r'continue-on-error|always\(\)|set \+e|\|\| true'))
        self.assertIn('id-token: write', workflow)


if __name__ == '__main__':
    unittest.main()
