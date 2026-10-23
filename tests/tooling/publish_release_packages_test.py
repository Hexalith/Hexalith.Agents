"""Run the real publisher with local archives and controlled external commands."""

from __future__ import annotations

import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from nuget_package_fixtures import PACKAGE_IDS, ROOT, VERSION, corrupt_archive_member, write_inventory, write_package


class PublishReleasePackagesTests(unittest.TestCase):
    def run_publisher(self, *, fault: str = "", directory_prefix: str = "publisher-"):
        with tempfile.TemporaryDirectory(prefix=directory_prefix) as directory:
            fixture = Path(directory)
            scripts = fixture / "scripts"
            scripts.mkdir()
            (fixture / "eng").mkdir()
            (fixture / "eng/release-packages.json").write_bytes((ROOT / "eng/release-packages.json").read_bytes())
            (scripts / "validate-nuget-packages.py").symlink_to(ROOT / "scripts/validate-nuget-packages.py")
            packages = fixture / "nupkgs"
            packages.mkdir()
            paths = write_inventory(packages)
            unrelated = packages / "Unrelated.9.9.9.nupkg"
            unrelated.write_bytes(b"caller-owned sentinel")
            write_package(packages, version="9.9.9")
            if fault == "missing":
                paths[-1].unlink()
            elif fault == "invalid":
                paths[-1].write_bytes(b"bad archive")
            elif fault == "corrupt assembly":
                corrupt_archive_member(paths[-1], f"lib/net10.0/{PACKAGE_IDS[-1]}.dll")
            commands = fixture / "commands"
            commands.mkdir()
            log = fixture / "commands.jsonl"
            stub = f'''#!{sys.executable}
import json, os, pathlib, sys
name = pathlib.Path(sys.argv[0]).name
args = sys.argv[1:]
if name == "python3" and args[0].endswith("validate-nuget-packages.py"):
    os.execv({sys.executable!r}, [{sys.executable!r}, *args])
stage = ("absence" if "absent" in args else "presence") if name == "python3" and "verify-nuget-publication.py" in args[0] else ("source" if name == "python3" else "push")
push_number = 1
if stage == "push" and pathlib.Path(os.environ["TOOL_LOG"]).exists():
    push_number += sum(json.loads(line)["stage"] == "push" for line in pathlib.Path(os.environ["TOOL_LOG"]).read_text().splitlines())
with open(os.environ["TOOL_LOG"], "a") as output:
    output.write(json.dumps({{"stage": stage, "args": args}}) + "\\n")
if os.environ["FAIL_STAGE"] == stage or os.environ["FAIL_STAGE"] == f"push-{{push_number}}" and stage == "push":
    sys.exit(7)
'''
            for name in ("python3", "dotnet"):
                command = commands / name
                command.write_text(stub, encoding="utf-8")
                command.chmod(0o755)
            env = {
                **os.environ, "PATH": f"{commands}{os.pathsep}{os.environ['PATH']}",
                "TOOL_LOG": str(log), "FAIL_STAGE": fault,
                "GITHUB_REPOSITORY": "Hexalith/Hexalith.Agents", "GITHUB_SHA": "a" * 40,
                "HEXALITH_RELEASE_SOURCE_BRANCH": "main", "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW": "ci.yml",
                "NUGET_API_KEY": "fixture-key",
            }
            result = subprocess.run(["bash", str(ROOT / "scripts/publish-release-packages.sh"), VERSION],
                cwd=fixture, env=env, capture_output=True, text=True, timeout=15)
            calls = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
            self.assertEqual(unrelated.read_bytes(), b"caller-owned sentinel")
            return result, calls

    def test_publishes_only_validated_manifest_archives_in_order(self) -> None:
        result, calls = self.run_publisher(directory_prefix="publisher with spaces ")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual([call["stage"] for call in calls], ["absence", "source", *(["push"] * len(PACKAGE_IDS)), "presence"])
        pushed = [call["args"][2] for call in calls if call["stage"] == "push"]
        self.assertEqual([Path(path).name for path in pushed], [f"{package_id}.{VERSION}.nupkg" for package_id in sorted(PACKAGE_IDS)])
        for call in calls:
            self.assertNotIn("--skip-duplicate", call["args"])
        source_args = calls[1]["args"]
        self.assertIn("refs/heads/main", source_args)
        self.assertIn("a" * 40, source_args)

    def test_complete_local_preflight_precedes_network_and_pushes(self) -> None:
        for fault in ("missing", "invalid", "corrupt assembly"):
            with self.subTest(fault=fault):
                result, calls = self.run_publisher(fault=fault)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(calls, [])

    def test_failed_absence_or_source_proof_prevents_every_push(self) -> None:
        for fault, expected in (("absence", ["absence"]), ("source", ["absence", "source"])):
            with self.subTest(fault=fault):
                result, calls = self.run_publisher(fault=fault)
                self.assertEqual(result.returncode, 7)
                self.assertEqual([call["stage"] for call in calls], expected)

    def test_push_and_presence_failures_remain_failures(self) -> None:
        result, calls = self.run_publisher(fault="push")
        self.assertEqual(result.returncode, 7)
        self.assertEqual([call["stage"] for call in calls], ["absence", "source", "push"])
        result, calls = self.run_publisher(fault="push-3")
        self.assertEqual(result.returncode, 7)
        self.assertEqual([call["stage"] for call in calls], ["absence", "source", "push", "push", "push"])
        result, calls = self.run_publisher(fault="presence")
        self.assertEqual(result.returncode, 7)
        self.assertEqual(calls[-1]["stage"], "presence")


if __name__ == "__main__":
    unittest.main()
