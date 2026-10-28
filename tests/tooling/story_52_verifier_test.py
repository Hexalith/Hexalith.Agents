"""Run the production Story 5.2 verifier against a controlled native test runner."""

from __future__ import annotations

import json
import os
import queue
import re
import shutil
import subprocess
import sys
import tempfile
import threading
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "eng" / "verify-story-5.2.ps1"
PWSH = shutil.which("pwsh")
PROJECTS = (
    "test/Hexalith.Agents.Contracts.Tests",
    "test/Hexalith.Agents.Client.Tests",
    "test/Hexalith.Agents.Tests",
    "test/Hexalith.Agents.Server.Tests",
    "test/Hexalith.Agents.UI.Tests",
)
BINDINGS = {
    "src/Hexalith.Agents.EventStore/AgentsEventStoreServiceCollectionExtensions.cs": "AddIdempotencyIntentAdapter",
    "src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs": "SubmitCommandAsync",
    "src/Hexalith.Agents.Server/Projections/AgentSetupProjectionHandler.cs": "IAsyncDomainProjectionRebuildHandler",
    "src/Hexalith.Agents.Server/Composition/AgentSetupServiceCollectionExtensions.cs": "EventStoreAgentAdministrationOperations",
    "src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs": "GetSetupAsync",
    "src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs": "AgentsClientSetupGateway",
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
    os.execv(os.environ["STORY_52_REAL_DOTNET"], [os.environ["STORY_52_REAL_DOTNET"], *args])
with open(os.environ["STORY_52_CALL_LOG"], "a", encoding="utf-8") as log:
    log.write(json.dumps(args) + "\n")
if args and args[0] == "msbuild":
    print(json.dumps({"Items": {"PackageVersion": [{"Identity": "Hexalith.EventStore.Client", "Version": "3.119.0"}]}}))
    sys.exit(0)
if args and args[0] in ("restore", "build"):
    if args[0] == os.environ.get("STORY_52_FAIL_COMMAND"):
        print("error NU1903: controlled audit failure", file=sys.stderr)
        sys.exit(23)
    print("controlled source " + args[0])
    sys.exit(0)
if not args or not args[0].endswith(".dll"):
    print("Unexpected build or restore invocation", file=sys.stderr)
    sys.exit(90)
scenario = os.environ["STORY_52_SCENARIO"]
target = os.environ["STORY_52_TARGET"]
matches = target in args
if target.startswith("all::"):
    matches = "-class" not in args and "-method" not in args and target[5:] in args
if not matches:
    scenario = "success"
if scenario == "native-error":
    print("controlled runner stderr diagnostic", file=sys.stderr)
    sys.exit(23)
if scenario == "no-summary":
    print("Runner returned without a test summary")
    sys.exit(0)
if scenario == "progress":
    print("controlled progress marker", flush=True)
    acknowledgement = Path(os.environ["STORY_52_PROGRESS_ACK"])
    deadline = time.monotonic() + 10
    while not acknowledgement.exists():
        if time.monotonic() >= deadline:
            print("Progress was buffered until runner completion", file=sys.stderr)
            sys.exit(24)
        time.sleep(0.01)
# Keep these independent expected floors aligned with the established Story 5.1 source lane.
floors = {"Hexalith.Agents.Contracts.Tests": 529, "Hexalith.Agents.Client.Tests": 6,
          "Hexalith.Agents.Tests": 787, "Hexalith.Agents.Server.Tests": 573,
          "Hexalith.Agents.UI.Tests": 1073}
if "Release" in Path(args[0]).parts:
    floors["Hexalith.Agents.Server.Tests"] = 549
method_floors = {
    "Hexalith.Agents.Server.Tests.VerifiedSetupQueryCallerTests.DispatcherUsesVerifiedHumanWithoutReplacingTransportIdentity": 12,
    "Hexalith.Agents.Server.Tests.TenantAuthorityAdmissionOrderingTests.AdministrationAuthorityIsRecheckedAfterTheRoleAwait": 4,
    "Hexalith.Agents.Server.Tests.TenantAuthorityAdmissionOrderingTests.ActorExpiringDuringTenantAuthorityAwaitCannotGrantAccess": 1,
}
minimum = (method_floors[args[args.index("-method") + 1]] if "-method" in args
           else floors[Path(args[0]).stem] if "-class" not in args else 1)
counts = {"total": minimum, "errors": 0, "failed": 0, "skipped": 0, "not-run": 0}
if scenario == "below-floor":
    counts["total"] = minimum - 1
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
if scenario == "incomplete":
    row = f"   {assembly}  Total: 1"
if scenario == "malformed":
    row = f"   {assembly}  Total: banana"
print(row)
if scenario == "duplicate-row":
    print(row)
if scenario == "duplicate-section":
    print("=== TEST EXECUTION SUMMARY ===")
    print(row)
'''


@unittest.skipUnless(PWSH, "PowerShell is required for production verifier subprocess tests")
@unittest.skipUnless(os.name == "posix", "The controlled executable uses a POSIX shebang; it cannot intercept Windows dotnet.exe safely")
class Story52VerifierTests(unittest.TestCase):
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
            release_assembly = self.root / project / "bin" / "Release" / "net10.0" / assembly.name
            release_assembly.parent.mkdir(parents=True)
            release_assembly.touch()
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
        self, scenario: str = "success", target: str = "Hexalith.Agents.Tests.AgentLifecycleConfigurationVersionTests"
    ) -> dict[str, str]:
        return dict(
            self.environment,
            STORY_52_CALL_LOG=str(self.log),
            STORY_52_SCENARIO=scenario,
            STORY_52_TARGET=target,
            STORY_52_REAL_DOTNET=shutil.which("dotnet") or "dotnet",
            STORY_52_PROGRESS_ACK=str(self.root / "progress-observed"),
            NO_COLOR="1",
        )

    def run_verifier(
        self, scenario: str = "success", target: str = "Hexalith.Agents.Tests.AgentLifecycleConfigurationVersionTests", *,
        skip_build: bool = True, configuration: str | None = None, fail_command: str = "",
    ) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [PWSH, "-NoProfile", "-NonInteractive", "-File", str(self.script),
             *(["-SkipBuild"] if skip_build else []),
             *(["-Configuration", configuration] if configuration else [])],
            env=dict(self.environment_for(scenario, target), STORY_52_FAIL_COMMAND=fail_command),
            text=True,
            capture_output=True,
            timeout=30,
            check=False,
        )

    @staticmethod
    def diagnostic(result: subprocess.CompletedProcess[str]) -> str:
        # PowerShell wraps long errors with a leading pipe on each continuation line.
        unwrapped = re.sub(r"(?m)^\s*\|\s?", "", result.stdout + result.stderr)
        return " ".join(unwrapped.split())

    def calls(self) -> list[list[str]]:
        return [json.loads(line) for line in self.log.read_text(encoding="utf-8").splitlines()] if self.log.exists() else []

    def test_all_selections_and_full_project_gates_run_with_skip_rejection(self) -> None:
        result = self.run_verifier()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Story 5.2 verification succeeded.", result.stdout)
        calls = self.calls()
        classes = [call[call.index("-class") + 1] for call in calls if "-class" in call]
        self.assertCountEqual(classes, (
            "Hexalith.Agents.Contracts.Tests.AgentOperationContractsTests",
            "Hexalith.Agents.Tests.AgentStateReplayTests",
            "Hexalith.Agents.Tests.AgentLifecycleConfigurationVersionTests",
            "Hexalith.Agents.Tests.AgentSetupDomainResultTests",
            "Hexalith.Agents.Server.Tests.AgentAdministrationOrchestratorTests",
            "Hexalith.Agents.Server.Tests.AgentProviderSelectionOrchestratorTests",
            "Hexalith.Agents.Server.Tests.EventStoreAgentCommandDispatcherTests",
            "Hexalith.Agents.Server.Tests.AgentInteractionRequestOrchestratorTests",
            "Hexalith.Agents.Server.Tests.ServerSerializationConformanceTests",
            "Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests",
            "Hexalith.Agents.Server.Tests.AgentActivationApproverRevalidationTests",
            "Hexalith.Agents.Server.Tests.EventStoreAgentAdministrationOperationsTests",
            "Hexalith.Agents.Server.Tests.AgentSetupProjectionTests",
            "Hexalith.Agents.Server.Tests.AgentSetupQueryHandlerTests",
            "Hexalith.Agents.Server.Tests.VerifiedSetupQueryCallerTests",
            "Hexalith.Agents.Server.Tests.TenantAuthorityAdmissionOrderingTests",
            "Hexalith.Agents.UI.Tests.AgentConfigurationTests",
            "Hexalith.Agents.UI.Tests.AgentsClientSetupGatewayTests",
            "Hexalith.Agents.Server.Tests.AgentSetupCompositionTests",
            "Hexalith.Agents.Server.Tests.HttpAgentAdministrationContextProviderTests",
            "Hexalith.Agents.Server.Tests.AgentsOperationEndpointsTests",
            "Hexalith.Agents.UI.Tests.AgentsUiCompositionTests",
        ))
        test_calls = [call for call in calls if call[0].endswith(".dll")]
        methods = [call[call.index("-method") + 1] for call in calls if "-method" in call]
        self.assertCountEqual(methods, (
            "Hexalith.Agents.Server.Tests.VerifiedSetupQueryCallerTests.DispatcherUsesVerifiedHumanWithoutReplacingTransportIdentity",
            "Hexalith.Agents.Server.Tests.TenantAuthorityAdmissionOrderingTests.AdministrationAuthorityIsRecheckedAfterTheRoleAwait",
            "Hexalith.Agents.Server.Tests.TenantAuthorityAdmissionOrderingTests.ActorExpiringDuringTenantAuthorityAwaitCannotGrantAccess",
        ))
        full_projects = [Path(call[0]).name for call in test_calls if "-class" not in call and "-method" not in call]
        self.assertCountEqual(full_projects, [Path(project).name + ".dll" for project in PROJECTS])
        self.assertEqual(len(calls), 31, "One package-floor check, 25 selections and five regression runs")
        for call in test_calls:
            self.assertIn("-failSkips", call)
            self.assertNotIn("-filterVSTest", call)
            self.assertTrue(Path(call[0]).is_relative_to(self.root))

    def test_default_invocation_builds_source_before_running_tests(self) -> None:
        result = self.run_verifier(skip_build=False)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        calls = self.calls()
        self.assertEqual([call[0] for call in calls[:3]], ["msbuild", "restore", "build"])
        self.assertEqual(len(calls), 33)
        for call in calls[1:3]:
            self.assertIn("-p:UseHexalithProjectReferences=true", call)
        for call in calls[:3]:
            self.assertNotIn("-p:NuGetAudit=false", call)
        self.assertIn("-p:Configuration=Debug", calls[1])
        self.assertEqual(calls[2][calls[2].index("-c") + 1], "Debug")
        self.assertIn("-warnaserror", calls[2])
        self.assertTrue(calls[3][0].endswith(".dll"))

    def test_release_invocation_builds_packages_and_checks_all_release_selections(self) -> None:
        result = self.run_verifier(skip_build=False, configuration="Release")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        calls = self.calls()
        self.assertEqual([call[0] for call in calls[:3]], ["msbuild", "restore", "build"])
        self.assertEqual(len(calls), 33)
        self.assertIn("-p:Configuration=Release", calls[1])
        self.assertEqual(calls[2][calls[2].index("-c") + 1], "Release")
        self.assertIn("-warnaserror", calls[2])
        for call in calls[1:3]:
            self.assertIn("-p:UseHexalithProjectReferences=false", call)
            self.assertNotIn("-p:NuGetAudit=false", call)
        for call in calls[3:]:
            self.assertIn("Release", Path(call[0]).parts)
            self.assertNotIn("Debug", Path(call[0]).parts)
            self.assertIn("-failSkips", call)

    def test_release_regression_rejects_server_count_below_package_floor(self) -> None:
        assembly = self.root / PROJECTS[3] / "bin/Release/net10.0/Hexalith.Agents.Server.Tests.dll"
        result = self.run_verifier(
            "below-floor", "all::" + str(assembly), configuration="Release"
        )
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("expected at least 549", self.diagnostic(result))

    def test_release_restore_and_build_audit_failures_stop_before_tests(self) -> None:
        for command in ("restore", "build"):
            with self.subTest(command=command):
                self.log.unlink(missing_ok=True)
                result = self.run_verifier(skip_build=False, configuration="Release", fail_command=command)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("NU1903", result.stdout + result.stderr)
                calls = self.calls()
                self.assertEqual([call[0] for call in calls],
                                 ["msbuild", "restore"] + (["build"] if command == "build" else []))

    def test_configuration_casing_selects_canonical_release_paths(self) -> None:
        result = self.run_verifier(configuration="release")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        for call in self.calls()[1:]:
            self.assertIn("Release", Path(call[0]).parts)

    def test_package_floor_only_does_not_invoke_tests(self) -> None:
        result = subprocess.run(
            [PWSH, "-NoProfile", "-NonInteractive", "-File", str(self.script), "-PackageFloorOnly"],
            env=self.environment_for(), text=True, capture_output=True, timeout=30, check=False,
        )
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(len(self.calls()), 1)
        self.assertEqual(self.calls()[0][0], "msbuild")
        self.assertIn("Gate: EventStore package floor", result.stdout)

    def test_each_current_authority_selection_refuses_missing_or_skipped_results(self) -> None:
        for test_type in ("VerifiedSetupQueryCallerTests", "TenantAuthorityAdmissionOrderingTests"):
            target = "Hexalith.Agents.Server.Tests." + test_type
            for scenario in ("zero", "skipped", "not-run"):
                with self.subTest(test_type=test_type, scenario=scenario):
                    self.log.unlink(missing_ok=True)
                    result = self.run_verifier(scenario, target=target)
                    self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertIn(target, result.stdout + result.stderr)
                    self.assertIn(target, self.calls()[-1])
                    self.assertFalse(any("-class" not in call and call[0].endswith(".dll")
                                         for call in self.calls()))

    def test_exact_authority_methods_require_all_current_rows(self) -> None:
        for target in (
            "VerifiedSetupQueryCallerTests.DispatcherUsesVerifiedHumanWithoutReplacingTransportIdentity",
            "TenantAuthorityAdmissionOrderingTests.AdministrationAuthorityIsRecheckedAfterTheRoleAwait",
            "TenantAuthorityAdmissionOrderingTests.ActorExpiringDuringTenantAuthorityAwaitCannotGrantAccess",
        ):
            target = "Hexalith.Agents.Server.Tests." + target
            for scenario in ("zero", "below-floor", "skipped", "not-run"):
                with self.subTest(target=target, scenario=scenario):
                    self.log.unlink(missing_ok=True)
                    result = self.run_verifier(scenario, target)
                    self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertIn(target, result.stdout + result.stderr)
                    self.assertEqual(self.calls()[-1][1:3], ["-failSkips", "-method"])
                    self.assertEqual(self.calls()[-1][3], target)
                    self.assertFalse(any("-class" not in call and "-method" not in call
                                         and call[0].endswith(".dll") for call in self.calls()))

    def test_later_empty_class_cannot_hide_behind_an_earlier_passing_class(self) -> None:
        result = self.run_verifier("zero")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("executed 0 tests", result.stdout + result.stderr)
        self.assertIn("Hexalith.Agents.Tests.AgentLifecycleConfigurationVersionTests", result.stdout + result.stderr)
        calls = self.calls()
        self.assertIn("Hexalith.Agents.Tests.AgentStateReplayTests", calls[-2])
        self.assertIn("Hexalith.Agents.Tests.AgentLifecycleConfigurationVersionTests", calls[-1])
        self.assertNotIn("Story 5.2 verification succeeded.", result.stdout)

    def test_absent_test_summary_fails(self) -> None:
        result = self.run_verifier("no-summary")
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("produced no test summary", result.stdout + result.stderr)

    def test_incomplete_or_malformed_summary_fails(self) -> None:
        for scenario in ("incomplete", "malformed"):
            with self.subTest(scenario=scenario):
                result = self.run_verifier(scenario)
                self.assertNotEqual(result.returncode, 0)
                diagnostic = "incomplete test summary" if scenario == "incomplete" else "invalid test summary"
                self.assertIn(diagnostic, result.stdout + result.stderr)

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
        self.assertEqual(len(self.calls()), 1)
        self.assertEqual(self.calls()[0][0], "msbuild")

    def test_full_project_gates_reject_zero_or_bad_summary_counts(self) -> None:
        target = "all::" + str(self.assemblies[1])
        for scenario in ("zero", "below-floor", "errors", "failed", "skipped", "not-run", "no-summary"):
            with self.subTest(scenario=scenario):
                result = self.run_verifier(scenario, target)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("all tests", result.stdout + result.stderr)
                if scenario == "below-floor":
                    self.assertIn("executed 5 tests; expected at least 6", self.diagnostic(result))
                self.assertEqual(self.calls()[-1], [str(self.assemblies[1]), "-failSkips"])
                self.assertNotIn("Story 5.2 verification succeeded.", result.stdout)

    def test_each_full_project_requires_its_existing_debug_floor(self) -> None:
        for assembly, floor in zip(self.assemblies, (529, 6, 787, 573, 1073), strict=True):
            with self.subTest(assembly=assembly.stem):
                result = self.run_verifier("below-floor", "all::" + str(assembly))
                self.assertNotEqual(result.returncode, 0)
                self.assertIn(f"executed {floor - 1} tests; expected at least {floor}", self.diagnostic(result))
                self.assertEqual(self.calls()[-1], [str(assembly), "-failSkips"])

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
            env=self.environment_for("progress", "Hexalith.Agents.Contracts.Tests.AgentOperationContractsTests"),
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
                self.assertIn("Story 5.2 verification succeeded.", "".join(lines))
            finally:
                if process.poll() is None:
                    process.kill()
                    process.wait(timeout=5)


@unittest.skipUnless(PWSH, "PowerShell is required for the real xUnit compatibility check")
class Story52RealRunnerCompatibilityTests(unittest.TestCase):
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
$ast = [System.Management.Automation.Language.Parser]::ParseFile($env:STORY_52_SOURCE, [ref] $tokens, [ref] $errors)
if ($errors.Count -gt 0) { throw 'Production verifier failed to parse.' }
$function = $ast.Find({ param($node) $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-TestGate' }, $true)
if ($null -eq $function) { throw 'Production Invoke-TestGate function is missing.' }
. ([scriptblock]::Create($function.Extent.Text))
$root = $env:STORY_52_ROOT
$parameters = @{ Name = 'real xUnit compatibility'; Assembly = $env:STORY_52_ASSEMBLY; Classes = @($env:STORY_52_SELECTOR) }
Invoke-TestGate @parameters
'''
        for kind, selector, succeeds in (
            ("class", "Hexalith.Agents.Tests.AgentStateReplayTests", True),
            ("class", "*NoSuchStory52CompatibilityTestClass", False),
        ):
            with self.subTest(kind=kind, selector=selector):
                result = subprocess.run(
                    [PWSH, "-NoProfile", "-NonInteractive", "-Command", harness],
                    env=dict(os.environ, STORY_52_SOURCE=str(SCRIPT), STORY_52_ROOT=str(ROOT),
                             STORY_52_ASSEMBLY=str(assembly), STORY_52_KIND=kind, STORY_52_SELECTOR=selector, NO_COLOR="1"),
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
