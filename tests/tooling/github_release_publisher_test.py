"""Run the real semantic-release hooks with controlled publication commands."""
from __future__ import annotations

import json
import hashlib
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from nuget_package_fixtures import ROOT, VERSION, write_inventory


class GitHubReleasePublisherTests(unittest.TestCase):
    def invoke(self, stage, fault=""):
        with tempfile.TemporaryDirectory(prefix="agents-release-hooks-") as temporary:
            directory = Path(temporary)
            packages = directory / "nupkgs"
            packages.mkdir()
            paths = write_inventory(packages)
            commands = directory / "commands"
            commands.mkdir()
            log = directory / "calls.jsonl"
            stub = f'''#!{sys.executable}
import json, os, pathlib, sys
name = pathlib.Path(sys.argv[0]).name
args = sys.argv[1:]
stage = "validate" if name == "python3" and "validate-nuget-packages.py" in args[0] else "verify" if name == "python3" else "create" if "create" in args else "finalize"
with open(os.environ["TOOL_LOG"], "a") as output:
    output.write(json.dumps({{"stage":stage,"args":args}})+"\\n")
if os.environ["FAIL_STAGE"] == stage:
    sys.exit(7)
if stage == "validate":
    print("\\n".join({[str(path) for path in paths]!r}))
'''
            for name in ("gh", "python3"):
                path = commands / name
                path.write_text(stub)
                path.chmod(0o755)
            invocation = f'''import {{ prepare, publish }} from {json.dumps((ROOT / 'eng/github-release-publisher.mjs').as_uri())};
await {stage}({{}}, {{nextRelease: {{version: {json.dumps(VERSION)}, gitHead: 'a'.repeat(40), notes: 'notes'}}}});
'''
            result = subprocess.run(["node", "--input-type=module", "-e", invocation], cwd=directory,
                env={**os.environ, "PATH": str(commands)+os.pathsep+os.environ['PATH'],
                    "GITHUB_REPOSITORY":"Hexalith/Hexalith.Agents", "GITHUB_SHA":"a"*40,
                    "TOOL_LOG":str(log), "FAIL_STAGE":fault}, capture_output=True,text=True,timeout=15)
            calls = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
            provenance = packages / "release-provenance.json"
            document = json.loads(provenance.read_text()) if provenance.exists() else None
            if document is not None:
                self.assertEqual(document["archives"], [
                    {"name": path.name, "size": len(path.read_bytes()),
                     "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in paths])
            return result, calls, document

    def test_prepare_stages_original_archives_and_hashes_before_publication(self):
        result, calls, provenance = self.invoke("prepare")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual([call['stage'] for call in calls], ["validate", "create"])
        self.assertIn("--draft", calls[-1]['args'])
        self.assertIn("--target", calls[-1]['args'])
        self.assertEqual(provenance['sourceSha'], "a"*40)
        self.assertTrue(all(len(entry['sha256']) == 64 for entry in provenance['archives']))
        self.assertEqual(len(provenance['archives']), 6)

    def test_duplicate_draft_or_validation_failure_aborts_prepare(self):
        for fault in ("validate", "create"):
            with self.subTest(fault=fault):
                result, calls, _ = self.invoke("prepare", fault)
                self.assertNotEqual(result.returncode, 0)
                self.assertNotIn("finalize", [call['stage'] for call in calls])

    def test_publish_finalizes_only_after_complete_verification(self):
        result, calls, _ = self.invoke("publish")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual([call['stage'] for call in calls], ["verify", "finalize"])
        self.assertIn("--verify-only", calls[0]['args'])
        result, calls, _ = self.invoke("publish", "verify")
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual([call['stage'] for call in calls], ["verify"])
