"""Exercise the maintained Story 5.4 verifier's producer and process boundaries."""

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
    os.execv(os.environ["STORY_54_REAL_DOTNET"], [os.environ["STORY_54_REAL_DOTNET"], *args])
imports = json.loads(os.environ["STORY_54_IMPORTS"])
keys = [*imports, "HEXALITH_SOURCE_MAPPING_HASH", "Configuration"]
with open(os.environ["STORY_54_CALL_LOG"], "a") as log:
    log.write(json.dumps({"args": args, "environment": {k: os.environ.get(k) for k in keys}}) + "\n")
scenario = os.environ["STORY_54_SCENARIO"]
if args[:2] == ["tool", "exec"]:
    assert "prepare-build" in args and args[args.index("--mode") + 1] == "source"
    workspace = Path(args[args.index("--workspace") + 1])
    assert not workspace.exists()
    directory = workspace / "source-mapping"
    directory.mkdir(parents=True)
    environment = {key: str(directory / name) for key, name in imports.items()}
    for name in imports.values():
        (directory / name).write_text("<Project />")
    environment.update(Configuration="Debug", HEXALITH_SOURCE_MAPPING_HASH="a" * 64)
    prepared = dict(workspace=str(workspace), mode="source", mappingHash="a" * 64, environment=environment)
    result = dict(status="prepared", outcome=dict(exitCode="Success"), diagnostics=[dict(message="retained producer diagnostic")], buildPreparation=prepared)
    if scenario == "producer-failure":
        print(json.dumps(result))
        print("controlled producer failure", file=sys.stderr)
        sys.exit(23)
    if scenario == "malformed":
        print("invalid JSON")
        sys.exit(0)
    if scenario == "wrong-status": result["status"] = "failed"
    if scenario == "wrong-outcome": result["outcome"]["exitCode"] = "PrerequisiteUnavailable"
    if scenario == "wrong-workspace": prepared["workspace"] += "-other"
    if scenario == "wrong-mode": prepared["mode"] = "package"
    if scenario == "wrong-hash": environment["HEXALITH_SOURCE_MAPPING_HASH"] = "b" * 64
    if scenario == "missing-environment": prepared.pop("environment")
    if scenario == "extra-environment": environment["UseHexalithProjectReferences"] = "true"
    if scenario == "missing-import": (directory / next(iter(imports.values()))).unlink()
    print(json.dumps(result))
    sys.exit(0)
if args[0] == "build":
    if scenario == "build-failure": sys.exit(23)
    print("Build succeeded.\n    0 Warning(s)\n    0 Error(s)")
    sys.exit(0)
assert args[0].endswith(".dll")
assembly = Path(args[0]).stem
classes = {
    "Hexalith.Agents.Tests": ["TrustedEnvelopeReplayTests", "SecurityEventLogTests", "AgentApproverPolicyTests", "AgentPartyIdentityTests"],
    "Hexalith.Agents.Server.Tests": ["Story54CurrentEvidenceTests", "Story54BoundaryTests", "PartiesAgentPartyDirectoryTests", "ApproverEvidenceAdmissionTests", "ApproverAuthorityLifetimeTests", "FixtureAgentRouteCatalogTests", "TenantAuthorityAdmissionOrderingTests", "AuthorityCancellationAdmissionTests", "AgentSetupQueryHandlerTests", "AgentSetupApproverBasisTests", "SetupReadCancellationAdmissionTests", "SetupWriteCancellationAdmissionTests", "SetupAuthorityLifetimeTests", "ProviderCatalogAuthorityLifetimeTests", "AuditQueryIsolationTests", "AgentInteractionAuditQueryHandlerTests", "AgentInteractionGateAdmissionTests", "AgentInteractionGateOrchestratorTests", "AgentPartyIdentityOrchestratorTests"],
    "Hexalith.Agents.UI.Tests": ["AgentConfigurationTests", "ApproverPolicyTests"],
}.get(assembly, ["RequiredProjectTests"])
root = Element("assemblies")
run = SubElement(root, "assembly", total=str(len(classes)), passed=str(len(classes)), failed="0", errors="0", skipped="0", **{"not-run": "0"})
if scenario == "test-failure": run.set("failed", "1")
collection = SubElement(run, "collection")
for name in classes: SubElement(collection, "test", type=f"{assembly}.{name}", result="Pass")
ElementTree(root).write(args[args.index("-result-xml") + 1])
'''


@unittest.skipUnless(PWSH and os.name == "posix", "PowerShell and POSIX executable interception are required")
class Story54VerifierTests(unittest.TestCase):
    def setUp(self) -> None:
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.script = self.root / "eng/verify-story-5.4.ps1"
        self.script.parent.mkdir()
        shutil.copyfile(ROOT / "eng/verify-story-5.4.ps1", self.script)
        register = self.root / "_bmad-output/planning-artifacts/external-dependency-register.md"
        register.parent.mkdir(parents=True)
        register.write_text("\n".join(f"### {record} — retained\n| `AcceptedStatus` | `Committed` |" for record in
                                      ("EXT-PARTIES-1", "EXT-CONV-AI-1", "EXT-SECRETS-1", "EXT-HOST-1")))
        self.ci_config = self.root / ".github/hexalith-ci.json"
        self.ci_config.parent.mkdir()
        self.ci_config.write_text(json.dumps({"buildsTag": "v7.8.9"}))
        subprocess.run(["git", "init", "-q"], cwd=self.root, check=True)
        subprocess.run(["git", "add", ".github/hexalith-ci.json"], cwd=self.root, check=True)
        self.commit_pin()
        self.ci_config.write_text(json.dumps({"buildsTag": "v0.0.1"}))
        self.log = self.root / "calls.jsonl"
        self.artifacts = self.root / "artifacts"
        binary = self.root / "bin"
        binary.mkdir()
        runner = binary / "dotnet"
        runner.write_text(f"#!{sys.executable}\n{RUNNER}")
        runner.chmod(0o755)
        self.environment = {key: value for key, value in os.environ.items() if key not in (*IMPORTS, "HEXALITH_SOURCE_MAPPING_HASH", "Configuration")}
        self.environment.update(PATH=str(binary) + os.pathsep + os.environ["PATH"], STORY_54_CALL_LOG=str(self.log),
                                STORY_54_IMPORTS=json.dumps(IMPORTS), STORY_54_REAL_DOTNET=shutil.which("dotnet") or "dotnet")

    def commit_pin(self) -> None:
        subprocess.run(["git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.test",
                        "commit", "-qam", "Pin Builds for the verifier fixture"], cwd=self.root, check=True)

    def run_verifier(self, scenario: str = "success", mode: str = "Local") -> subprocess.CompletedProcess[str]:
        return subprocess.run([PWSH, "-NoProfile", "-NonInteractive", "-File", str(self.script), "-Mode", mode,
                               "-ArtifactsPath", str(self.artifacts)], capture_output=True, text=True,
                              env=dict(self.environment, STORY_54_SCENARIO=scenario), timeout=30, check=False)

    def calls(self) -> list[dict]:
        return [json.loads(line) for line in self.log.read_text().splitlines()] if self.log.exists() else []

    def test_canonical_preparation_is_applied_only_to_builds(self) -> None:
        result = self.run_verifier()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("retained producer diagnostic", result.stdout)
        calls = self.calls()
        self.assertEqual(len(calls), 11)
        producer = calls[0]["args"]
        self.assertEqual(producer[:7], ["tool", "exec", "Hexalith.Builds.Module.Cli@7.8.9", "--configfile",
                                       str(self.root / "NuGet.config"), "--", "prepare-build"])
        self.assertEqual(producer[producer.index("--manifest") + 1], str(self.root / "module/hexalith.module-manifest.v2.json"))
        for call in calls[1:]:
            if call["args"][0] == "build":
                self.assertEqual(call["environment"]["Configuration"], "Debug")
                self.assertEqual(call["environment"]["HEXALITH_SOURCE_MAPPING_HASH"], "a" * 64)
                self.assertTrue(all(call["environment"][key] for key in IMPORTS))
            else:
                self.assertTrue(all(value is None for value in call["environment"].values()), call)
        self.assertFalse(json.loads((self.artifacts / "result.json").read_text())["StoryComplete"])

    def test_each_run_uses_a_fresh_preparation_child(self) -> None:
        for _ in range(2):
            result = self.run_verifier()
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        workspaces = [call["args"][call["args"].index("--workspace") + 1] for call in self.calls() if call["args"][0] == "tool"]
        self.assertEqual(len(set(workspaces)), 2)
        self.assertTrue(all(Path(path).parent == self.artifacts for path in workspaces))

    def test_failed_or_invalid_preparation_stops_before_build(self) -> None:
        for scenario in ("producer-failure", "malformed", "wrong-status", "wrong-outcome", "wrong-workspace", "wrong-mode",
                         "wrong-hash", "missing-environment", "extra-environment", "missing-import"):
            with self.subTest(scenario=scenario):
                self.log.unlink(missing_ok=True)
                result = self.run_verifier(scenario)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual([call["args"][0] for call in self.calls()], ["tool"])
                self.assertIn("BuildPreparation", result.stdout + result.stderr)

    def test_floating_release_pin_stops_before_tool_execution(self) -> None:
        self.ci_config.write_text(json.dumps({"buildsTag": "v7.*"}))
        self.commit_pin()
        result = self.run_verifier()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("expected an exact Builds release tag", result.stderr)
        self.assertEqual(self.calls(), [])

    def test_environment_is_restored_after_build_failure(self) -> None:
        self.environment.update(Configuration="original", CustomAfterMicrosoftCommonProps="original.props")
        # Dot-source so the caller can observe the verifier's finally block in the same process.
        wrapper = self.root / "observe.ps1"
        wrapper.write_text("try { . $args[0] -ArtifactsPath $args[1] } catch { Write-Host 'caught-build-failure' }\n"
                           "Write-Output ('RESTORED:' + $env:Configuration + ':' + $env:CustomAfterMicrosoftCommonProps)\n"
                           "if ($env:HEXALITH_SOURCE_MAPPING_HASH) { throw 'Mapping hash leaked' }\n")
        result = subprocess.run([PWSH, "-NoProfile", "-File", str(wrapper), str(self.script), str(self.artifacts)],
                                capture_output=True, text=True, timeout=30,
                                env=dict(self.environment, STORY_54_SCENARIO="build-failure"), check=False)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("caught-build-failure", result.stdout)
        self.assertIn("RESTORED:original:original.props", result.stdout)
        self.assertEqual([call["args"][0] for call in self.calls()], ["tool", "build"])

    def test_failed_test_results_still_fail_the_local_gate(self) -> None:
        result = self.run_verifier("test-failure")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("IncompleteTestRun", result.stdout + result.stderr)

    def test_live_refuses_before_preparation_or_build(self) -> None:
        result = self.run_verifier(mode="Live")
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(self.calls(), [])
        receipt = json.loads((self.artifacts / "result.json").read_text())
        self.assertEqual(receipt["ExternalCalls"], 0)
        self.assertEqual(len(receipt["Blockers"]), 4)


if __name__ == "__main__":
    unittest.main()
