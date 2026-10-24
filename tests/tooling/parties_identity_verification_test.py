"""Exercise the maintained owner-regression command without real builds or owner calls."""

from __future__ import annotations

import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts/verify-parties-identity.py"
DEADLINE_METHODS = (
    "BlockingProviderCancellationCallback_DoesNotDelayQueryCompletion",
    "CallerCancellationAfterDeadlineOrFaultBeforeVerdict_PreservesOriginalToken",
    "WholeQueryDeadline_NonCooperativeDependencyDeniesLateEvidence",
    "WholeQueryDeadline_SynchronousProviderInvocationIsBounded",
    "WholeQueryDeadline_SourceAndCustodyShareCumulativeBudget",
    "WholeQueryDeadline_ElapsedBeforeTimerCallbackDeniesEvidence",
    "InvalidQueryTimeout_DeniesBeforeAnyProvider",
    "WholeQueryDeadline_SetupTimeConsumesOperationBudget",
    "ShortQueryDeadline_ImmediatelyBeforeExpiryStillResolves",
    "WholeQueryDeadline_FinalAuthorityExpiryDeniesTerminalEvidence",
    "CallerCancellationRacesDeadlineAndFault_PreservesOriginalToken",
    "ProviderCancellationCallbackFaultAfterDeadline_DoesNotAffectVerdict",
)

DOTNET = r'''
import json, os, pathlib, sys, xml.etree.ElementTree as ET
args = sys.argv[1:]
scenario = os.environ.get("IDENTITY_SCENARIO", "success")
with open(os.environ["IDENTITY_CALLS"], "a") as log:
    log.write(json.dumps({"args": args, "cwd": os.getcwd(),
                         "uiLanguage": os.environ.get("DOTNET_CLI_UI_LANGUAGE")}) + "\n")
if args[0] == "msbuild":
    print("bad-version" if scenario == "bad-version" else "3.999.7")
    sys.exit(0)
if args[0] == "build":
    project = pathlib.Path(args[1]).stem
    if scenario == "build-failure" or (scenario == "client-build-failure" and project.endswith("Client.Tests")):
        sys.exit(9)
    output = pathlib.Path(args[args.index("--artifacts-path") + 1])
    assembly = output / "bin" / project / "debug" / (project + ".dll")
    assembly.parent.mkdir(parents=True, exist_ok=True)
    if scenario != "missing-assembly":
        assembly.write_text("synthetic")
    print("1 Warning(s)" if scenario == "build-warning" else
          "0 Avertissement(s)" if scenario == "localized-build" and os.environ.get("DOTNET_CLI_UI_LANGUAGE") != "en-US"
          else "0 Warning(s)")
    sys.exit(0)
assembly = pathlib.Path(args[0])
selection = args[args.index("-method") + 1]
test_type, method = selection.rsplit(".", 1)
if os.environ.get("IDENTITY_TARGET_METHOD") not in (None, method):
    scenario = "success"
count = {"CurrentCompletionTimeChangesDuringCustody_DenyEvidence": 4,
         "CurrentAuthorityChangesDuringAwait_DenyEvidence": 2,
         "CurrentHuman_RequiresCompleteSourceAndCurrentExactActor": 1,
         "CurrentAuthorityChangeDuringReadMustNotReleaseIdentity": 8,
         "WholeQueryDeadline_NonCooperativeDependencyDeniesLateEvidence": 8,
         "WholeQueryDeadline_SynchronousProviderInvocationIsBounded": 4,
         "WholeQueryDeadline_SourceAndCustodyShareCumulativeBudget": 2,
         "WholeQueryDeadline_ElapsedBeforeTimerCallbackDeniesEvidence": 2,
         "InvalidQueryTimeout_DeniesBeforeAnyProvider": 8,
         "WholeQueryDeadline_SetupTimeConsumesOperationBudget": 4,
         "ShortQueryDeadline_ImmediatelyBeforeExpiryStillResolves": 2,
         "WholeQueryDeadline_FinalAuthorityExpiryDeniesTerminalEvidence": 2,
         "CallerCancellationRacesDeadlineAndFault_PreservesOriginalToken": 4,
         "ProviderCancellationCallbackFaultAfterDeadline_DoesNotAffectVerdict": 2,
         "BlockingProviderCancellationCallback_DoesNotDelayQueryCompletion": 12,
         "CallerCancellationAfterDeadlineOrFaultBeforeVerdict_PreservesOriginalToken": 4,
         "DelayedReply_ValidatesCompletionTime": 9}[method]
path = pathlib.Path(args[args.index("-result-xml") + 1])
if scenario == "runner-failure":
    sys.exit(8)
if scenario == "missing-xml":
    sys.exit(0)
if scenario == "malformed-xml":
    path.write_text("<invalid")
    sys.exit(0)
if scenario == "zero-cases":
    count = 0
if scenario == "missing-case":
    count -= 1
root = ET.Element("assemblies")
attributes = dict(name=str(assembly), total=str(count), passed=str(count),
                  failed="0", errors="0", skipped="0", **{"not-run": "0"})
if scenario in ("skipped", "failed", "errors", "not-run"):
    attributes[scenario] = "1"
if scenario == "foreign-assembly":
    attributes["name"] = str(assembly.with_name("Other.dll"))
if scenario == "missing-summary":
    del attributes["not-run"]
report = ET.SubElement(root, "assembly", attributes)
collection = ET.SubElement(report, "collection")
labels = {
    "CurrentCompletionTimeChangesDuringCustody_DenyEvidence": [
        '(change: "' + value + '")' for value in ("interval-expiry", "custody-expiry", "source-rollback", "binding-rollback")],
    "CurrentAuthorityChangesDuringAwait_DenyEvidence": ["(inCustody: False)", "(inCustody: True)"],
    "CurrentHuman_RequiresCompleteSourceAndCurrentExactActor": [""],
    "CurrentAuthorityChangeDuringReadMustNotReleaseIdentity": [
        '(inCustody: ' + suspended + ', change: "' + change + '")' for suspended in ("False", "True")
        for change in ("source", "actor-revision", "authority-revision", "unchanged")],
    "WholeQueryDeadline_NonCooperativeDependencyDeniesLateEvidence": (
        '(historical: False, inCustody: False, lateFault: False)',
        '(historical: False, inCustody: False, lateFault: True)',
        '(historical: False, inCustody: True, lateFault: False)',
        '(historical: False, inCustody: True, lateFault: True)',
        '(historical: True, inCustody: False, lateFault: False)',
        '(historical: True, inCustody: False, lateFault: True)',
        '(historical: True, inCustody: True, lateFault: False)',
        '(historical: True, inCustody: True, lateFault: True)',
    ),
    "WholeQueryDeadline_SynchronousProviderInvocationIsBounded": (
        '(historical: False, inCustody: False)',
        '(historical: False, inCustody: True)',
        '(historical: True, inCustody: False)',
        '(historical: True, inCustody: True)',
    ),
    "WholeQueryDeadline_SourceAndCustodyShareCumulativeBudget": (
        '(historical: False)',
        '(historical: True)',
    ),
    "WholeQueryDeadline_ElapsedBeforeTimerCallbackDeniesEvidence": (
        '(historical: False)',
        '(historical: True)',
    ),
    "InvalidQueryTimeout_DeniesBeforeAnyProvider": (
        '(historical: False, ticks: 0)',
        '(historical: False, ticks: -1)',
        '(historical: False, ticks: 300000001)',
        '(historical: False, ticks: 9223372036854775807)',
        '(historical: True, ticks: 0)',
        '(historical: True, ticks: -1)',
        '(historical: True, ticks: 300000001)',
        '(historical: True, ticks: 9223372036854775807)',
    ),
    "WholeQueryDeadline_SetupTimeConsumesOperationBudget": (
        '(historical: False, exhausted: False)',
        '(historical: False, exhausted: True)',
        '(historical: True, exhausted: False)',
        '(historical: True, exhausted: True)',
    ),
    "ShortQueryDeadline_ImmediatelyBeforeExpiryStillResolves": (
        '(historical: False)',
        '(historical: True)',
    ),
    "WholeQueryDeadline_FinalAuthorityExpiryDeniesTerminalEvidence": (
        '(historical: False)',
        '(historical: True)',
    ),
    "CallerCancellationRacesDeadlineAndFault_PreservesOriginalToken": (
        '(historical: False, expire: False)',
        '(historical: False, expire: True)',
        '(historical: True, expire: False)',
        '(historical: True, expire: True)',
    ),
    "ProviderCancellationCallbackFaultAfterDeadline_DoesNotAffectVerdict": (
        '(historical: False)',
        '(historical: True)',
    ),
    "BlockingProviderCancellationCallback_DoesNotDelayQueryCompletion": (
        '(historical: False, inCustody: False, trigger: "timer")',
        '(historical: False, inCustody: False, trigger: "elapsed")',
        '(historical: False, inCustody: False, trigger: "caller")',
        '(historical: False, inCustody: True, trigger: "timer")',
        '(historical: False, inCustody: True, trigger: "elapsed")',
        '(historical: False, inCustody: True, trigger: "caller")',
        '(historical: True, inCustody: False, trigger: "timer")',
        '(historical: True, inCustody: False, trigger: "elapsed")',
        '(historical: True, inCustody: False, trigger: "caller")',
        '(historical: True, inCustody: True, trigger: "timer")',
        '(historical: True, inCustody: True, trigger: "elapsed")',
        '(historical: True, inCustody: True, trigger: "caller")',
    ),
    "CallerCancellationAfterDeadlineOrFaultBeforeVerdict_PreservesOriginalToken": (
        '(historical: False, trigger: "deadline")',
        '(historical: False, trigger: "fault")',
        '(historical: True, trigger: "deadline")',
        '(historical: True, trigger: "fault")',
    ),
    "DelayedReply_ValidatesCompletionTime": ['(scenario: "' + value + '")' for value in (
        "current-valid", "current-interval-expiry", "current-custody-expiry", "current-source-rollback",
        "current-binding-rollback", "current-future-observation", "historical-closed-valid",
        "historical-custody-expiry", "historical-future-action")],
}[method]
for i in range(count):
    ET.SubElement(collection, "test", dict(type="Other.Type" if scenario == "foreign-type" else test_type,
        method="OtherMethod" if scenario == "foreign-method" else method,
        name=selection + ("(same)" if scenario == "duplicate-case" else
                          f"(unrelated: {i})" if scenario == "unrelated-cases" else labels[i]),
        result="Skip" if scenario == "case-not-passed" else "Pass"))
if scenario == "missing-test":
    collection.remove(list(collection)[0])
if scenario == "runner-error":
    ET.SubElement(report, "errors").append(ET.Element("error"))
if scenario == "duplicate-assembly":
    root.append(ET.Element("assembly", attributes))
ET.ElementTree(root).write(path, encoding="utf-8")
'''

RECEIPT_SUITE = '''
import json, os, unittest
with open(os.environ["IDENTITY_CALLS"], "a") as log:
    log.write(json.dumps({"stage": "receipt"}) + "\\n")
class ExactTargetReceiptTests(unittest.TestCase):
    def check_receipt(self):
        scenario = os.environ.get("IDENTITY_SCENARIO")
        if scenario == "receipt-skipped":
            self.skipTest("synthetic skip")
        self.assertNotEqual(scenario, "receipt-failure")
for name in ["test_matching_receipt_passes", "test_matching_receipt_passes_through_wrapper"] + [
    "test_mismatched_" + field + "_prevents_http" + suffix
    for field in ("recordId", "targetVersionOrCommit", "compatibilityCommand", "outcome",
                  "evidenceLevel", "prerequisitesAvailable") for suffix in ("", "_wrapper")]:
    if os.environ.get("IDENTITY_SCENARIO") == "receipt-missing-case" and name.endswith("recordId_prevents_http"):
        continue
    setattr(ExactTargetReceiptTests, name, ExactTargetReceiptTests.check_receipt)
'''


@unittest.skipUnless(os.name == "posix", "Requires executable PATH fixtures")
class PartiesIdentityVerificationTests(unittest.TestCase):
    """Fail closed on incomplete results and keep source mapping portable."""

    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="identity-command-")
        self.addCleanup(temporary.cleanup)
        self.directory = Path(temporary.name)
        self.root = self.directory / "repository with spaces"
        (self.root / "scripts").mkdir(parents=True)
        self.script = self.root / "scripts" / SCRIPT.name
        shutil.copyfile(SCRIPT, self.script)
        suite = self.root / "tests/tooling"
        suite.mkdir(parents=True)
        (suite / "parties_readiness_verifier_test.py").write_text(RECEIPT_SUITE)
        commands = self.directory / "commands"
        commands.mkdir()
        fake = commands / "dotnet"
        fake.write_text(f"#!{sys.executable}\n" + DOTNET)
        fake.chmod(0o755)
        git = commands / "git"
        git.write_text(f"#!{sys.executable}\n" + '''
import json, os, sys
with open(os.environ["IDENTITY_CALLS"], "a") as log:
    log.write(json.dumps({"args": sys.argv[1:], "cwd": os.getcwd()}) + "\\n")
if sys.argv[1] == "rev-parse":
    print("a" * 40 if not os.getcwd().endswith("Hexalith.Parties") else "b" * 40)
else:
    if not os.getcwd().endswith("Hexalith.Parties"):
        print(" M synthetic-test-change.cs")
''')
        git.chmod(0o755)
        (self.root / "references/Hexalith.Parties").mkdir(parents=True)
        self.environment = dict(os.environ, PATH=str(commands) + os.pathsep + os.environ["PATH"])
        # Prevent inherited Python import settings from hiding our controlled suite.
        self.environment.pop("PYTHONPATH", None)
        self.environment.pop("PYTHONHOME", None)
        self.calls = self.directory / "calls.jsonl"
        self.environment["IDENTITY_CALLS"] = str(self.calls)
        self.caller = self.directory / "caller"
        self.caller.mkdir()
        self.artifacts = self.caller / "relative evidence"
        self.artifacts.mkdir()
        self.receipt = self.artifacts / "identity-verification.json"
        self.receipt.write_text('{"stale": true}')

    def invoke(self, scenario="success"):
        self.environment["IDENTITY_SCENARIO"] = scenario
        result = subprocess.run([sys.executable, str(self.script), "--artifacts", "relative evidence"],
                                cwd=self.caller, env=self.environment, capture_output=True,
                                text=True, timeout=20)
        calls = [json.loads(line) for line in self.calls.read_text().splitlines()]
        return result, calls

    def test_success_builds_exact_owner_projects_and_emits_local_evidence(self):
        result, calls = self.invoke()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        receipt = json.loads(self.receipt.read_text())
        self.assertTrue(receipt["localBehaviorVerified"])
        self.assertFalse(receipt["liveReadinessEstablished"])
        self.assertEqual(receipt["eventStoreVersion"], "3.999.7")
        self.assertEqual(receipt["sources"]["agents"]["commit"], "a" * 40)
        self.assertTrue(receipt["sources"]["agents"]["dirty"])
        self.assertEqual(receipt["sources"]["parties"], dict(commit="b" * 40, dirty=False, status=[]))
        self.assertEqual(len(receipt["runId"]), 32)
        self.assertLessEqual(receipt["startedAt"], receipt["completedAt"])
        self.assertEqual(sorted(receipt["ownerCases"].values()), [1, 2, 2, 2, 2, 2, 2, 4, 4, 4, 4, 4, 8, 8, 8, 9, 12])
        self.assertEqual(set(receipt["ownerCases"]), {
            "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests." + method
            for method in ("CurrentCompletionTimeChangesDuringCustody_DenyEvidence",
                           "CurrentAuthorityChangesDuringAwait_DenyEvidence",
                           "CurrentHuman_RequiresCompleteSourceAndCurrentExactActor",
                           *DEADLINE_METHODS)
        } | {"Hexalith.Agents.Identity.Tests.CurrentIdentityAuthorityTests.CurrentAuthorityChangeDuringReadMustNotReleaseIdentity",
             "Hexalith.Parties.Client.Tests.HttpPartiesIdentityClientTests.DelayedReply_ValidatesCompletionTime"})
        builds = [call for call in calls if call.get("args", [None])[0] == "build"]
        self.assertEqual([Path(call["args"][1]).stem for call in builds],
                         ["Hexalith.Parties.Tests", "Hexalith.Parties.Client.Tests"])
        for call in builds:
            args = call["args"]
            self.assertEqual(Path(call["cwd"]), self.root)
            self.assertEqual(call["uiLanguage"], "en-US")
            self.assertEqual(args[args.index("--configuration") + 1], "Debug")
            self.assertEqual(Path(args[args.index("--artifacts-path") + 1]), self.artifacts / "build")
            self.assertIn("-p:HexalithEventStoreVersion=3.999.7", args)
            self.assertIn("-p:UseHexalithProjectReferences=true", args)
            self.assertIn("-p:UseNuGetDeps=false", args)
            self.assertIn("-p:HexalithMemoriesFromSource=false", args)
            self.assertIn(f"-p:CustomAfterMicrosoftCommonTargets={self.root / 'tests/identity/identity-regressions.targets'}", args)
            self.assertNotIn("-p:NuGetAudit=false", args)
            for module in ("Builds", "EventStore", "Commons", "Tenants", "McpCli", "FrontComposer"):
                self.assertIn(f"-p:Hexalith{module}Root={self.root / 'references' / ('Hexalith.' + module)}", args)
        self.assertEqual(calls[-1], {"stage": "receipt"})
        self.assertEqual(len(calls), 25)

    def test_each_deadline_selection_requires_complete_named_results(self):
        for method in DEADLINE_METHODS:
            for scenario in ("missing-case", "unrelated-cases", "skipped"):
                with self.subTest(method=method, scenario=scenario):
                    self.calls.unlink(missing_ok=True)
                    self.environment["IDENTITY_TARGET_METHOD"] = method
                    result, calls = self.invoke(scenario)
                    self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertIn("invalid results for Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests." + method,
                                  result.stderr)
                    self.assertFalse(self.receipt.exists())
                    self.assertEqual(calls[-1]["args"][2],
                                     "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests." + method)
                    self.assertFalse(any(call.get("stage") == "receipt" for call in calls))

    def test_localized_caller_gets_deterministic_build_language(self):
        self.environment["DOTNET_CLI_UI_LANGUAGE"] = "fr-FR"
        result, _ = self.invoke("localized-build")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def test_existing_lock_prevents_calls_and_preserves_active_run_evidence(self):
        (self.artifacts / ".identity-verification.lock").mkdir()
        result = subprocess.run([sys.executable, str(self.script), "--artifacts", str(self.artifacts)],
                                cwd=self.caller, env=self.environment, capture_output=True, text=True, timeout=10)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn("locked by another invocation", result.stderr)
        self.assertFalse(self.calls.exists())
        self.assertEqual(json.loads(self.receipt.read_text()), {"stale": True})

    def assert_refused(self, scenario):
        # A stale successful XML must never substitute for this execution.
        stale = self.artifacts / "CurrentCompletionTimeChangesDuringCustody_DenyEvidence.xml"
        stale.write_text('<assemblies><assembly total="999" passed="999" /></assemblies>')
        later = self.artifacts / "DelayedReply_ValidatesCompletionTime.xml"
        later.write_text("stale later results")
        result, calls = self.invoke(scenario)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("verify-parties-identity:", result.stderr)
        self.assertFalse(self.receipt.exists(), result.stdout + result.stderr)
        self.assertFalse((self.artifacts / ".identity-verification.lock").exists())
        if scenario in ("bad-version", "build-failure"):
            self.assertFalse(later.exists())
        if scenario.startswith("receipt-"):
            self.assertEqual(calls[-1], {"stage": "receipt"})
        else:
            self.assertFalse(any(call.get("stage") == "receipt" for call in calls))
        if scenario == "missing-xml":
            self.assertFalse(stale.exists())


def refusal_case(scenario):
    def test(self):
        self.assert_refused(scenario)
    return test


for _scenario in (
    "bad-version", "build-failure", "client-build-failure", "build-warning", "missing-assembly",
    "runner-failure", "missing-xml", "malformed-xml", "zero-cases", "missing-case", "skipped",
    "failed", "errors", "not-run", "foreign-assembly", "foreign-type", "foreign-method",
    "missing-summary", "duplicate-case", "case-not-passed", "missing-test", "runner-error",
    "duplicate-assembly", "unrelated-cases", "receipt-failure", "receipt-skipped", "receipt-missing-case",
):
    setattr(PartiesIdentityVerificationTests, "test_refuses_" + _scenario.replace("-", "_"),
            refusal_case(_scenario))


if __name__ == "__main__":
    unittest.main()
