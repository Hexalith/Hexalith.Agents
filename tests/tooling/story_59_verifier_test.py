"""Exercise Story 5.9 verifier modes and setup failures without real builds."""

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
IMPORTS = {
    "CustomBeforeDirectoryBuildProps": "SourceMapping.targets",
    "CustomAfterMicrosoftCommonProps": "SourceMapping.props",
    "CustomBeforeDirectoryBuildTargets": "SourceMapping.before-targets.props",
    "CustomAfterDirectoryBuildTargets": "SourceMapping.late.props",
    "AfterMicrosoftNETSdkTargets": "SourceMapping.final.targets",
}
RUNNER = r'''
import json, os, sys
from pathlib import Path
from xml.etree.ElementTree import Element, SubElement, ElementTree
args = sys.argv[1:]
if args and Path(args[0]).name == "pwsh.dll":
    os.execv(os.environ["STORY_59_REAL_DOTNET"], [os.environ["STORY_59_REAL_DOTNET"], *args])
keys = [*json.loads(os.environ["STORY_59_IMPORTS"]), "HEXALITH_SOURCE_MAPPING_HASH", "Configuration"]
with open(os.environ["STORY_59_CALL_LOG"], "a") as log:
    log.write(json.dumps({"args": args, "environment": {k: os.environ.get(k) for k in keys}}) + "\n")
scenario = os.environ["STORY_59_SCENARIO"]
if args[0] == "run":
    copied = Path.cwd() / ".git/objects/retained"
    original = Path(os.environ["STORY_59_FIXTURE_ROOT"]) / ".git/objects/retained"
    assert copied.read_text() == "isolated metadata"
    assert not copied.samefile(original), "Git objects must be copied, not hard-linked"
    workspace = Path(args[args.index("--workspace") + 1])
    directory = workspace / "source-mapping"
    directory.mkdir(parents=True)
    environment = {key: str(directory / name) for key, name in json.loads(os.environ["STORY_59_IMPORTS"]).items()}
    for path in environment.values(): Path(path).write_text("<Project />")
    environment.update(Configuration="Debug", HEXALITH_SOURCE_MAPPING_HASH="a" * 64)
    print(json.dumps(dict(status="prepared", outcome=dict(exitCode="Success"), buildPreparation=dict(
        workspace=str(workspace), mode="source", mappingHash="a" * 64, environment=environment))))
    sys.exit(0)
if args[0] == "build":
    if scenario == "build-failure": sys.exit(23)
    print("Build succeeded.\n    0 Warning(s)\n    0 Error(s)")
    sys.exit(0)
assert args[0].endswith(".dll")
root = Element("assemblies")
SubElement(root, "assembly", total="1", passed="1", failed="0", errors="0", skipped="0", **{"not-run": "0"})
ElementTree(root).write(args[args.index("-result-xml") + 1])
'''


@unittest.skipUnless(PWSH and os.name == "posix", "PowerShell and POSIX executable interception are required")
class Story59VerifierTests(unittest.TestCase):
    def setUp(self) -> None:
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.script = self.root / "eng/verify-story-5.9.ps1"
        self.script.parent.mkdir()
        shutil.copyfile(ROOT / "eng/verify-story-5.9.ps1", self.script)
        marker = self.root / ".git/objects/retained"
        marker.parent.mkdir(parents=True)
        marker.write_text("isolated metadata")
        (self.root / "references").mkdir()
        report = self.root / "_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md"
        report.parent.mkdir(parents=True)
        report.write_text("retained governance report")
        self.log = self.root / "calls.jsonl"
        self.artifacts = self.root / "artifacts"
        scratch = tempfile.TemporaryDirectory()
        self.addCleanup(scratch.cleanup)
        self.scratch = Path(scratch.name)
        binary = self.root / "bin"
        binary.mkdir()
        runner = binary / "dotnet"
        runner.write_text(f"#!{sys.executable}\n{RUNNER}")
        runner.chmod(0o755)
        self.environment = {key: value for key, value in os.environ.items()
                            if key not in (*IMPORTS, "HEXALITH_SOURCE_MAPPING_HASH", "Configuration")}
        self.environment.update(PATH=str(binary) + os.pathsep + os.environ["PATH"],
                                TMPDIR=str(self.scratch), TMP=str(self.scratch), TEMP=str(self.scratch),
                                STORY_59_CALL_LOG=str(self.log), STORY_59_FIXTURE_ROOT=str(self.root),
                                STORY_59_IMPORTS=json.dumps(IMPORTS),
                                STORY_59_REAL_DOTNET=shutil.which("dotnet") or "dotnet")

    def run_verifier(self, *, preparation_only: bool = True, internal: bool = False,
                     artifacts: Path | None = None, scenario: str = "success") -> subprocess.CompletedProcess[str]:
        command = [PWSH, "-NoProfile", "-NonInteractive", "-File", str(self.script),
                   "-ArtifactsPath", str(artifacts or self.artifacts)]
        if preparation_only:
            command.append("-PreparationOnly")
        if internal:
            command.append("-InDisposableWorkspace")
        return subprocess.run(command, capture_output=True, text=True, timeout=30, check=False,
                              env=dict(self.environment, STORY_59_SCENARIO=scenario))

    def calls(self) -> list[dict]:
        return [json.loads(line) for line in self.log.read_text().splitlines()] if self.log.exists() else []

    def assert_incomplete_receipt(self, preparation_verified: bool) -> dict:
        receipt = json.loads((self.artifacts / "result.json").read_text())
        self.assertEqual(receipt["PreparationVerified"], preparation_verified)
        for key in ("FullStoryComplete", "LiveReadinessEstablished", "ProviderExclusionProven", "DependencyAvailabilityChanged"):
            self.assertFalse(receipt[key], key)
        self.assertEqual(receipt["SprintStatus"], "backlog")
        self.assertEqual(len(receipt["UnmetFullStoryGates"]), 3)
        return receipt

    def test_preparation_only_succeeds_with_isolated_copies_and_build_only_mapping(self) -> None:
        result = self.run_verifier()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        receipt = self.assert_incomplete_receipt(True)
        self.assertIsNone(receipt["Failure"])
        calls = self.calls()
        self.assertEqual(len(calls), 19)
        for call in calls[1:]:
            if call["args"][0] == "build":
                self.assertEqual(call["environment"]["HEXALITH_SOURCE_MAPPING_HASH"], "a" * 64)
            else:
                self.assertTrue(all(value is None for value in call["environment"].values()), call)
        self.assertEqual(list(self.scratch.iterdir()), [])

    def test_default_mode_refuses_full_qualification_after_successful_preparation(self) -> None:
        result = self.run_verifier(preparation_only=False)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("FullStoryQualificationBlocked", result.stdout + result.stderr)
        self.assert_incomplete_receipt(True)
        self.assertEqual(len(self.calls()), 19)

    def test_setup_failure_replaces_stale_success_before_any_build(self) -> None:
        self.artifacts.mkdir()
        (self.artifacts / "result.json").write_text(json.dumps(dict(PreparationVerified=True, FullStoryComplete=True)))
        (self.root / "_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md").unlink()
        result = self.run_verifier()
        self.assertNotEqual(result.returncode, 0)
        receipt = self.assert_incomplete_receipt(False)
        self.assertTrue(receipt["Failure"])
        self.assertEqual(self.calls(), [])

    def test_pointer_file_git_root_is_refused_before_following_metadata(self) -> None:
        shutil.rmtree(self.root / ".git")
        (self.root / ".git").write_text("gitdir: /original/metadata/must-not-be-followed\n")
        result = self.run_verifier()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("UnsupportedGitRoot", result.stdout + result.stderr)
        self.assert_incomplete_receipt(False)
        self.assertEqual(self.calls(), [])

    def test_canonical_tmpdir_equality_is_refused_without_outputs(self) -> None:
        result = self.run_verifier(internal=True, artifacts=self.scratch / ".")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("outside TMPDIR", result.stdout + result.stderr)
        self.assertFalse((self.scratch / "result.json").exists())
        self.assertEqual(self.calls(), [])

    def test_symlinked_artifact_ancestor_beneath_tmpdir_is_refused_without_outputs(self) -> None:
        alias = self.root / "outside-looking-alias"
        alias.symlink_to(self.scratch, target_is_directory=True)
        result = self.run_verifier(internal=True, artifacts=alias / "new-output")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("outside TMPDIR", result.stdout + result.stderr)
        self.assertFalse((self.scratch / "new-output").exists())
        self.assertEqual(self.calls(), [])

    def test_repository_local_temp_is_refused_and_the_private_directory_is_removed(self) -> None:
        local_temp = self.root / "temporary files"
        local_temp.mkdir()
        self.environment["TMPDIR"] = str(local_temp)
        result = self.run_verifier()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("outside the source checkout", result.stdout + result.stderr)
        self.assertEqual(list(local_temp.iterdir()), [])
        self.assertFalse(self.artifacts.exists())
        self.assertEqual(self.calls(), [])

    def test_failed_build_retains_failed_preparation_receipt(self) -> None:
        result = self.run_verifier(scenario="build-failure")
        self.assertNotEqual(result.returncode, 0)
        self.assertTrue(self.assert_incomplete_receipt(False)["Failure"])


if __name__ == "__main__":
    unittest.main()
