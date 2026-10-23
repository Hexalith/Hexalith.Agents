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
    def run_publisher(self, *, fault: str = "", directory_prefix: str = "publisher-", live_source: bool = False):
        with tempfile.TemporaryDirectory(prefix=directory_prefix) as directory:
            fixture = Path(directory)
            scripts = fixture / "scripts"
            scripts.mkdir()
            (fixture / "eng").mkdir()
            (fixture / "eng/release-packages.json").write_bytes((ROOT / "eng/release-packages.json").read_bytes())
            (scripts / "validate-nuget-packages.py").symlink_to(ROOT / "scripts/validate-nuget-packages.py")
            (scripts / "verify-release-source.py").symlink_to(ROOT / "scripts/verify-release-source.py")
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
            elif fault == "oversized":
                with paths[-1].open("r+b") as archive:
                    archive.truncate(65 * 1024 * 1024)
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
if name == "gh":
    stage = "main-ref" if "/git/ref/" in args[1] else "ci-runs"
push_number = 1
if stage == "push" and pathlib.Path(os.environ["TOOL_LOG"]).exists():
    push_number += sum(json.loads(line)["stage"] == "push" for line in pathlib.Path(os.environ["TOOL_LOG"]).read_text().splitlines())
with open(os.environ["TOOL_LOG"], "a") as output:
    output.write(json.dumps({{"stage": stage, "args": args}}) + "\\n")
if os.environ["FAIL_STAGE"] == stage or os.environ["FAIL_STAGE"] == f"push-{{push_number}}" and stage == "push":
    sys.exit(7)
if stage == "absence" and os.environ["FAIL_STAGE"] == "advanced-main":
    pathlib.Path(os.environ["TOOL_LOG"] + ".advanced").touch()
if stage == "source" and os.environ["LIVE_SOURCE"] == "1":
    os.execv({sys.executable!r}, [{sys.executable!r}, *args])
if stage == "main-ref":
    advanced = pathlib.Path(os.environ["TOOL_LOG"] + ".advanced").exists()
    print(json.dumps({{"object": {{"sha": ("b" if advanced else "a") * 40}}}}))
elif stage == "ci-runs":
    conclusion = "failure" if os.environ["FAIL_STAGE"] == "failed-ci" else "success"
    print(json.dumps({{"total_count": 1, "workflow_runs": [{{
        "head_sha": "a" * 40, "head_branch": "main", "event": "push",
        "status": "completed", "conclusion": conclusion,
    }}]}}))
'''
            for name in ("python3", "dotnet", "gh"):
                command = commands / name
                command.write_text(stub, encoding="utf-8")
                command.chmod(0o755)
            env = {
                **os.environ, "PATH": f"{commands}{os.pathsep}{os.environ['PATH']}",
                "TOOL_LOG": str(log), "FAIL_STAGE": fault,
                "LIVE_SOURCE": "1" if live_source else "0",
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
        for fault in ("missing", "invalid", "corrupt assembly", "oversized"):
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

    def test_real_source_proof_runs_after_absence_and_immediately_before_pushes(self) -> None:
        result, calls = self.run_publisher(live_source=True)

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual([call["stage"] for call in calls], [
            "absence", "source", "main-ref", "ci-runs", *(["push"] * len(PACKAGE_IDS)), "presence",
        ])
        self.assertIn("Verified exact successful main push CI", result.stdout)
        self.assertEqual(calls[1]["args"], [
            "scripts/verify-release-source.py", "--repository", "Hexalith/Hexalith.Agents",
            "--workflow", "ci.yml", "--dispatch-ref", "refs/heads/main", "--dispatch-sha", "a" * 40,
        ])
        self.assertEqual(calls[2]["args"], ["api", "repos/Hexalith/Hexalith.Agents/git/ref/heads/main"])
        self.assertEqual(calls[3]["args"], ["api", "repos/Hexalith/Hexalith.Agents/actions/workflows/ci.yml/runs"
            f"?branch=main&event=push&status=completed&head_sha={'a' * 40}&per_page=100"])

    def test_real_source_proof_blocks_main_advanced_after_preflight_and_failed_ci(self) -> None:
        for fault, diagnostic in (("advanced-main", "stale"), ("failed-ci", "no successful push CI run")):
            with self.subTest(fault=fault):
                result, calls = self.run_publisher(fault=fault, live_source=True)

                self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
                self.assertIn(diagnostic, result.stderr)
                self.assertNotIn("Traceback", result.stderr)
                self.assertEqual([call["stage"] for call in calls], ["absence", "source", "main-ref", "ci-runs"])

    def test_real_github_api_failure_prevents_every_push(self) -> None:
        for fault, stages in (("main-ref", ["absence", "source", "main-ref"]),
                              ("ci-runs", ["absence", "source", "main-ref", "ci-runs"])):
            with self.subTest(fault=fault):
                result, calls = self.run_publisher(fault=fault, live_source=True)

                self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
                self.assertIn("GitHub API source proof failed: exit code 7", result.stderr)
                self.assertNotIn("Traceback", result.stderr)
                self.assertEqual([call["stage"] for call in calls], stages)


if __name__ == "__main__":
    unittest.main()
