"""Prove ordinary verification entry points retain and propagate NuGet audit failures."""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PWSH = shutil.which("pwsh")
RUNNER = r'''
import json, os, pathlib, sys
args = sys.argv[1:]
if args and pathlib.Path(args[0]).name == "pwsh.dll":
    os.execv(os.environ["AUDIT_REAL_DOTNET"], [os.environ["AUDIT_REAL_DOTNET"], *args])
name = pathlib.Path(sys.argv[0]).name
with open(os.environ["AUDIT_LOG"], "a") as log:
    log.write(json.dumps({"name": name, "args": args}) + "\n")
if name == "pwsh":
    sys.exit(0)  # Unrelated package-authority and version-floor prerequisites.
configuration = "Release" if "-p:Configuration=Release" in args or "Release" in args else "Debug"
if args[0] in ("restore", "build") and configuration == os.environ["AUDIT_FAIL_CONFIGURATION"]:
    print("error NU1903: controlled high-severity advisory", file=sys.stderr)
    sys.exit(23)
sys.exit(0)
'''


@unittest.skipUnless(PWSH and os.name == "posix", "Requires PowerShell and POSIX executable interception")
class VerifierAuditPolicyTests(unittest.TestCase):
    def run_verifier(self, script: str, arguments: list[str], configuration: str):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            eng = root / "eng"
            eng.mkdir()
            shutil.copyfile(ROOT / "eng" / script, eng / script)
            (eng / "package-authority-exclusions.txt").write_text("# fixture\n")
            for prerequisite in ("references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1",
                                 "eng/verify-story-5.2.ps1"):
                path = root / prerequisite
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("exit 0\n")
            planning = root / "_bmad-output/planning-artifacts"
            planning.mkdir(parents=True)
            (planning / "external-dependency-register.md").write_text("# Unavailable\n")
            commands = root / "commands"
            commands.mkdir()
            for name in ("dotnet", "pwsh"):
                command = commands / name
                command.write_text(f"#!{sys.executable}\n{RUNNER}")
                command.chmod(0o755)
            log = root / "calls.jsonl"
            environment = dict(os.environ, PATH=str(commands) + os.pathsep + os.environ["PATH"],
                AUDIT_LOG=str(log), AUDIT_FAIL_CONFIGURATION=configuration,
                AUDIT_REAL_DOTNET=shutil.which("dotnet") or "dotnet", NO_COLOR="1")
            result = subprocess.run([PWSH, "-NoProfile", "-NonInteractive", "-File", str(eng / script),
                *arguments, *([] if script == "verify-story.ps1" else ["-ArtifactsPath", str(root / "evidence")])],
                env=environment, capture_output=True, text=True, timeout=20)
            calls = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
            return result, [call["args"] for call in calls if call["name"] == "dotnet"]

    def assert_audit_failure(self, result, calls) -> None:
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("NU1903", result.stdout + result.stderr)
        self.assertTrue(calls)
        for call in calls:
            self.assertNotIn("-p:NuGetAudit=false", call)
        self.assertIn(calls[-1][0], ("restore", "build"))

    def test_story_51_source_and_package_restore_failures_stop_verification(self) -> None:
        for configuration in ("Debug", "Release"):
            with self.subTest(configuration=configuration):
                result, calls = self.run_verifier("verify-story.ps1", ["-Story", "5.1"], configuration)
                self.assert_audit_failure(result, calls)
                self.assertEqual(calls[-1][0], "restore")
                self.assertIn(f"-p:Configuration={configuration}", calls[-1])
                self.assertEqual(len(calls), 1 if configuration == "Debug" else 8)

    def test_story_54_source_audit_failure_stops_before_tests(self) -> None:
        result, calls = self.run_verifier("verify-story-5.4.ps1", [], "Debug")
        self.assert_audit_failure(result, calls)
        self.assertEqual(len(calls), 1)
        self.assertIn("-p:UseHexalithProjectReferences=true", calls[0])

    def test_story_61_both_dependency_lanes_propagate_audit_failure(self) -> None:
        for lane, configuration, source in (("Source", "Debug", "true"), ("Package", "Release", "false")):
            with self.subTest(lane=lane):
                result, calls = self.run_verifier("verify-story-6.1.ps1", ["-DependencyMode", lane], configuration)
                self.assert_audit_failure(result, calls)
                self.assertEqual(len(calls), 1)
                self.assertIn(f"-p:UseHexalithProjectReferences={source}", calls[0])


if __name__ == "__main__":
    unittest.main()
