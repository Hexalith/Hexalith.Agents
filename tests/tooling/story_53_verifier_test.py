"""Run the production Story 5.3 verifier against a controlled native test runner."""

from __future__ import annotations

import json
import os
import queue
import shutil
import subprocess
import sys
import tempfile
import threading
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "eng" / "verify-story-5.3.ps1"
PWSH = shutil.which("pwsh")
PROJECTS = (
    "references/Hexalith.EventStore/tests/Hexalith.EventStore.Server.Tests",
    "references/Hexalith.EventStore/tests/Hexalith.EventStore.Client.Tests",
    "test/Hexalith.Agents.Contracts.Tests",
    "test/Hexalith.Agents.Client.Tests",
    "test/Hexalith.Agents.Tests",
    "test/Hexalith.Agents.Server.Tests",
    "test/Hexalith.Agents.UI.Tests",
)
BINDINGS = {
    "src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs": "ProviderCatalogCommandAcceptance",
    "src/Hexalith.Agents.Server/Projections/ProviderCatalogProjectionHandler.cs": "IAsyncDomainProjectionRebuildHandler",
    "src/Hexalith.Agents.Server/Ports/ProjectedProviderCatalogReader.cs": "IProviderCatalogReader",
    "src/Hexalith.Agents.Client/AgentsClient.cs": "WithProviderCatalog",
    "src/Hexalith.Agents.UI/Services/Gateways/AgentsClientProviderCatalogGateway.cs": "IProviderCatalogGateway",
    "src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs": "AgentsClientProviderCatalogGateway",
}
RUNNER = r'''
import json
import os
import sys
import time
from pathlib import Path

args = sys.argv[1:]
# A dotnet-installed PowerShell shim starts pwsh.dll through PATH first.
if args and Path(args[0]).name == "pwsh.dll":
    os.execv(os.environ["STORY_53_REAL_DOTNET"], [os.environ["STORY_53_REAL_DOTNET"], *args])
with open(os.environ["STORY_53_CALL_LOG"], "a", encoding="utf-8") as log:
    log.write(json.dumps(args) + "\n")
if args and args[0] in ("restore", "build"):
    if os.environ["STORY_53_SCENARIO"] == "audit-failure" and os.environ["STORY_53_TARGET"] == f"{args[0]}::{args[1]}":
        print("error NU1903: controlled high-severity advisory", file=sys.stderr)
        sys.exit(23)
    sys.exit(0)
if not args or not args[0].endswith(".dll"):
    print("Unexpected build or restore invocation", file=sys.stderr)
    sys.exit(90)
scenario = os.environ["STORY_53_SCENARIO"]
target = os.environ["STORY_53_TARGET"]
if target not in args:
    scenario = "success"
if scenario == "native-error":
    print("controlled runner stderr diagnostic", file=sys.stderr)
    sys.exit(23)
if scenario == "no-summary":
    print("Runner returned without a test summary")
    sys.exit(0)
if scenario == "progress":
    print("controlled progress marker", flush=True)
    acknowledgement = Path(os.environ["STORY_53_PROGRESS_ACK"])
    deadline = time.monotonic() + 10
    while not acknowledgement.exists():
        if time.monotonic() >= deadline:
            print("Progress was buffered until runner completion", file=sys.stderr)
            sys.exit(24)
        time.sleep(0.01)
counts = {"total": 1, "errors": 0, "failed": 0, "skipped": 0, "not-run": 0}
if scenario in ("zero", "misleading-zero"):
    counts["total"] = 0
elif scenario in counts:
    counts[scenario] = 1
assembly = Path(args[0]).stem
if scenario == "misleading-zero":
    print(f"   {assembly}  Total: 1, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0")
print("=== TEST EXECUTION SUMMARY ===")
if scenario == "wrong-assembly":
    assembly = "Unexpected.Test.Assembly"
row = f"   {assembly}  Total: 0" if counts["total"] == 0 else f"   {assembly}  " + "Total: {total}, Errors: {errors}, Failed: {failed}, Skipped: {skipped}, Not Run: {not-run}, Time: 0.001s".format(**counts)
print(row)
if scenario == "duplicate-row":
    print(row)
if scenario == "duplicate-section":
    print("=== TEST EXECUTION SUMMARY ===")
    print(row)
'''


@unittest.skipUnless(PWSH, "PowerShell is required for production verifier subprocess tests")
@unittest.skipUnless(os.name == "posix", "The controlled executable uses a POSIX shebang; it cannot intercept Windows dotnet.exe safely")
class Story53VerifierTests(unittest.TestCase):
    """Check every declared selection independently without a real SDK invocation."""

    def setUp(self) -> None:
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.script = self.root / "eng" / SCRIPT.name
        self.script.parent.mkdir()
        shutil.copyfile(SCRIPT, self.script)
        self.log = self.root / "runner-calls.jsonl"
        self.assemblies = []
        for project in PROJECTS:
            assembly = self.root / project / "bin" / "Debug" / "net10.0" / (Path(project).name + ".dll")
            assembly.parent.mkdir(parents=True)
            assembly.touch()
            self.assemblies.append(assembly)
        for relative, needle in BINDINGS.items():
            path = self.root / relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(needle, encoding="utf-8")
        binary = self.root / "controlled-bin"
        binary.mkdir()
        runner = binary / "dotnet"
        runner.write_text(f"#!{sys.executable}\n{RUNNER}", encoding="utf-8")
        runner.chmod(0o755)
        self.environment = dict(os.environ, PATH=str(binary) + os.pathsep + os.environ["PATH"])

    def environment_for(
        self, scenario: str = "success", target: str = "*ProviderCatalogStateReplayTests"
    ) -> dict[str, str]:
        return dict(
            self.environment,
            STORY_53_CALL_LOG=str(self.log),
            STORY_53_SCENARIO=scenario,
            STORY_53_TARGET=target,
            STORY_53_REAL_DOTNET=shutil.which("dotnet") or "dotnet",
            STORY_53_PROGRESS_ACK=str(self.root / "progress-observed"),
            NO_COLOR="1",
        )

    def run_verifier(
        self, scenario: str = "success", target: str = "*ProviderCatalogStateReplayTests", *, skip_build: bool = True
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [PWSH, "-NoProfile", "-NonInteractive", "-File", str(self.script), *(["-SkipBuild"] if skip_build else [])],
            env=self.environment_for(scenario, target),
            text=True,
            capture_output=True,
            timeout=30,
            check=False,
        )

    def calls(self) -> list[list[str]]:
        return [json.loads(line) for line in self.log.read_text(encoding="utf-8").splitlines()] if self.log.exists() else []

    def test_default_builds_keep_nuget_auditing_enabled(self) -> None:
        result = self.run_verifier(skip_build=False)

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        calls = self.calls()
        self.assertEqual([call[0] for call in calls[:6]], ["restore", "build"] * 3)
        for call in calls[:6]:
            self.assertNotIn("-p:NuGetAudit=false", call)
        self.assertIn("-p:Configuration=Debug", calls[0])
        self.assertIn("-p:UseHexalithProjectReferences=true", calls[0])
        self.assertTrue(calls[6][0].endswith(".dll"))

    def test_audit_failure_in_any_restore_or_build_stops_before_tests(self) -> None:
        targets = [self.root / "Hexalith.Agents.slnx", *[
            Path(project) / (Path(project).name + ".csproj") for project in PROJECTS[:2]
        ]]
        for index, path in enumerate(targets):
            for offset, operation in enumerate(("restore", "build")):
                with self.subTest(project=str(path), operation=operation):
                    self.log.unlink(missing_ok=True)
                    result = self.run_verifier("audit-failure", f"{operation}::{path}", skip_build=False)

                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn("NU1903", result.stdout + result.stderr)
                    calls = self.calls()
                    self.assertEqual(len(calls), index * 2 + offset + 1)
                    self.assertTrue(all(call[0] in ("restore", "build") for call in calls))

    def test_all_selections_and_full_project_gates_run_with_skip_rejection(self) -> None:
        result = self.run_verifier()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Story 5.3 verification succeeded.", result.stdout)
        calls = self.calls()
        classes = [call[call.index("-class") + 1] for call in calls if "-class" in call]
        self.assertCountEqual(classes, (
            "*CoordinatedCommandActorTests", "*CommandStatusControllerTests",
            "*ProviderCatalogAggregateTests", "*ProviderCatalogStateReplayTests",
            "*ProviderCatalogVersionRegressionTests", "*ProviderSecretLeakTests",
            "*TenantProviderEnablementAggregateTests", "*DataHandlingGraceDeadlineTests",
            "*EventStoreProviderCatalogOperationsTests", "*ProviderCatalogEventStoreIntegrationTests",
            "*ProviderCatalogCoordinationIntegrationTests", "*ProviderCatalogQueryTests",
            "*ProviderCatalogAuthorizationTests", "*TenantProviderCatalogIntegrationTests",
            "*ProviderCatalogMigrationTests", "*ProjectedProviderCatalogReaderTests",
            "*HttpAgentAdministrationContextProviderTests", "*ProviderCatalogTests",
            "*ProviderCatalogUiTests", "*ProviderCatalogGovernanceUiTests",
            "*ProviderCatalogCompositionTests", "*AgentSetupCompositionTests", "*AgentsUiCompositionTests",
        ))
        methods = [call[call.index("-method") + 1] for call in calls if "-method" in call]
        self.assertCountEqual(methods, (
            "*Fenced_stale_source_after_a_target_reconciliation_miss*",
            "*Handle_CoordinatedSourceConflict*",
            "*GetCommandStatusAsync_UsesConfiguredPathAndDeserializesRejectedStatus",
            "*GetCommandStatusAsync_PreservesCompletedZeroEventCount",
        ))
        full_projects = [Path(call[0]).name for call in calls if "-class" not in call and "-method" not in call]
        self.assertCountEqual(full_projects, [Path(project).name + ".dll" for project in PROJECTS[2:]])
        for call in calls:
            self.assertIn("-failSkips", call)
            self.assertNotIn("-filterVSTest", call)
            self.assertTrue(Path(call[0]).is_relative_to(self.root))

    def test_later_empty_class_cannot_hide_behind_an_earlier_passing_class(self) -> None:
        result = self.run_verifier("zero")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("executed 0 tests", result.stdout + result.stderr)
        self.assertIn("*ProviderCatalogStateReplayTests", result.stdout + result.stderr)
        calls = self.calls()
        self.assertIn("*ProviderCatalogAggregateTests", calls[-2])
        self.assertIn("*ProviderCatalogStateReplayTests", calls[-1])
        self.assertNotIn("Story 5.3 verification succeeded.", result.stdout)

    def test_empty_method_selection_fails_independently(self) -> None:
        target = "*GetCommandStatusAsync_PreservesCompletedZeroEventCount"
        result = self.run_verifier("zero", target)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("executed 0 tests", result.stdout + result.stderr)
        self.assertIn(target, self.calls()[-1])

    def test_absent_test_summary_fails(self) -> None:
        result = self.run_verifier("no-summary")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("produced no test summary", result.stdout + result.stderr)

    def test_failed_or_skipped_summary_fails_even_with_zero_exit(self) -> None:
        for scenario in ("errors", "failed", "skipped", "not-run"):
            with self.subTest(scenario=scenario):
                result = self.run_verifier(scenario)
                self.assertNotEqual(result.returncode, 0)
                diagnostic = "errors or failures" if scenario in ("errors", "failed") else "skipped or did not run"
                self.assertIn(diagnostic, result.stdout + result.stderr)

    def test_native_failure_preserves_diagnostics_and_exit_code(self) -> None:
        result = self.run_verifier("native-error")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("controlled runner stderr diagnostic", result.stdout + result.stderr)
        self.assertIn("exit code 23", result.stdout + result.stderr)

    def test_missing_assembly_fails_before_invoking_runner(self) -> None:
        self.assemblies[0].unlink()
        result = self.run_verifier()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("requires built test assembly", result.stdout + result.stderr)
        self.assertEqual(self.calls(), [])

    def test_full_project_gate_also_requires_nonzero_execution(self) -> None:
        target = str(self.assemblies[2])
        result = self.run_verifier("zero", target)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("all tests", result.stdout + result.stderr)
        self.assertIn("executed 0 tests", result.stdout + result.stderr)

    def test_diagnostic_before_summary_cannot_hide_a_real_zero_count(self) -> None:
        result = self.run_verifier("misleading-zero")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("executed 0 tests", result.stdout + result.stderr)

    def test_duplicate_sections_or_rows_are_ambiguous(self) -> None:
        for scenario in ("duplicate-section", "duplicate-row"):
            with self.subTest(scenario=scenario):
                result = self.run_verifier(scenario)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("ambiguous test summary", result.stdout + result.stderr)

    def test_summary_from_another_assembly_is_rejected(self) -> None:
        result = self.run_verifier("wrong-assembly")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("produced no test summary for assembly", result.stdout + result.stderr)

    def test_progress_is_visible_while_the_runner_is_still_waiting(self) -> None:
        observed: queue.Queue[str | None] = queue.Queue()
        with subprocess.Popen(
            [PWSH, "-NoProfile", "-NonInteractive", "-File", str(self.script), "-SkipBuild"],
            env=self.environment_for("progress", "*CoordinatedCommandActorTests"),
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            text=True,
        ) as process:
            def read_output() -> None:
                assert process.stdout is not None
                for line in process.stdout:
                    observed.put(line)
                observed.put(None)

            reader = threading.Thread(target=read_output, daemon=True)
            reader.start()
            lines = []
            try:
                while True:
                    line = observed.get(timeout=20)
                    self.assertIsNotNone(line, "Runner exited before producing progress: " + "".join(lines))
                    lines.append(line)
                    if "controlled progress marker" in line:
                        self.assertIsNone(process.poll(), "Runner must still await acknowledgement")
                        (self.root / "progress-observed").touch()
                        break
                self.assertEqual(process.wait(timeout=30), 0)
                reader.join(timeout=5)
                while not observed.empty():
                    line = observed.get_nowait()
                    if line is not None:
                        lines.append(line)
                self.assertEqual("".join(lines).count("controlled progress marker"), 1)
                self.assertIn("Story 5.3 verification succeeded.", "".join(lines))
            finally:
                if process.poll() is None:
                    process.kill()
                    process.wait(timeout=5)


@unittest.skipUnless(PWSH, "PowerShell is required for the real xUnit compatibility check")
class Story53RealRunnerCompatibilityTests(unittest.TestCase):
    """Exercise the production gate against the repository's existing Domain assembly."""

    def test_native_selectors_and_real_summary_format(self) -> None:
        project = "test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj"
        assembly = ROOT / "test/Hexalith.Agents.Tests/bin/Debug/net10.0/Hexalith.Agents.Tests.dll"
        if not assembly.is_file():
            self.skipTest("Build the Domain assembly first: dotnet build " + project + " --configuration Debug")
        harness = r'''
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$tokens = $null
$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($env:STORY_53_SOURCE, [ref] $tokens, [ref] $errors)
if ($errors.Count -gt 0) { throw 'Production verifier failed to parse.' }
$function = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-TestGate' }, $true)
if ($null -eq $function) { throw 'Production Invoke-TestGate function is missing.' }
. ([scriptblock]::Create($function.Extent.Text))
$root = $env:STORY_53_ROOT
$parameters = @{ Name = 'real xUnit compatibility'; Project = $env:STORY_53_PROJECT }
if ($env:STORY_53_KIND -eq 'class') { $parameters.Classes = @($env:STORY_53_SELECTOR) }
else { $parameters.Methods = @($env:STORY_53_SELECTOR) }
Invoke-TestGate @parameters
'''
        for kind, selector, succeeds in (
            ("class", "*ProviderCatalogAggregateTests", True),
            ("method", "*Create_with_no_state_and_admin_produces_created", True),
            ("class", "*NoSuchStory53CompatibilityTestClass", False),
        ):
            with self.subTest(kind=kind, selector=selector):
                result = subprocess.run(
                    [PWSH, "-NoProfile", "-NonInteractive", "-Command", harness],
                    env=dict(os.environ, STORY_53_SOURCE=str(SCRIPT), STORY_53_ROOT=str(ROOT),
                             STORY_53_PROJECT=project, STORY_53_KIND=kind, STORY_53_SELECTOR=selector, NO_COLOR="1"),
                    capture_output=True,
                    text=True,
                    timeout=30,
                    check=False,
                )
                if succeeds:
                    self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                else:
                    self.assertNotEqual(result.returncode, 0)
                    self.assertIn("executed 0 tests", result.stdout + result.stderr)


if __name__ == "__main__":
    unittest.main()
