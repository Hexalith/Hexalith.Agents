#!/usr/bin/env python3
"""Build and verify the pinned Parties completion-time, authority and query-deadline regressions."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from uuid import uuid4


ROOT = Path(__file__).resolve().parents[1]
QUERY_TYPE = "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests"
CLIENT_TYPE = "Hexalith.Parties.Client.Tests.HttpPartiesIdentityClientTests"
SELECTIONS = {
    "Hexalith.Parties.Tests": (
        (QUERY_TYPE, "CurrentCompletionTimeChangesDuringCustody_DenyEvidence", 4),
        (QUERY_TYPE, "CurrentAuthorityChangesDuringAwait_DenyEvidence", 2),
        (QUERY_TYPE, "CurrentHuman_RequiresCompleteSourceAndCurrentExactActor", 1),
        ("Hexalith.Agents.Identity.Tests.CurrentIdentityAuthorityTests",
         "CurrentAuthorityChangeDuringReadMustNotReleaseIdentity", 8),
        (QUERY_TYPE, "WholeQueryDeadline_NonCooperativeDependencyDeniesLateEvidence", 8),
        (QUERY_TYPE, "WholeQueryDeadline_SynchronousProviderInvocationIsBounded", 4),
        (QUERY_TYPE, "WholeQueryDeadline_SourceAndCustodyShareCumulativeBudget", 2),
        (QUERY_TYPE, "WholeQueryDeadline_ElapsedBeforeTimerCallbackDeniesEvidence", 2),
        (QUERY_TYPE, "InvalidQueryTimeout_DeniesBeforeAnyProvider", 8),
        (QUERY_TYPE, "WholeQueryDeadline_SetupTimeConsumesOperationBudget", 4),
        (QUERY_TYPE, "ShortQueryDeadline_ImmediatelyBeforeExpiryStillResolves", 2),
        (QUERY_TYPE, "WholeQueryDeadline_FinalAuthorityExpiryDeniesTerminalEvidence", 2),
        (QUERY_TYPE, "CallerCancellationRacesDeadlineAndFault_PreservesOriginalToken", 4),
        (QUERY_TYPE, "ProviderCancellationCallbackFaultAfterDeadline_DoesNotAffectVerdict", 2),
        (QUERY_TYPE, "BlockingProviderCancellationCallback_DoesNotDelayQueryCompletion", 12),
        (QUERY_TYPE, "CallerCancellationAfterDeadlineOrFaultBeforeVerdict_PreservesOriginalToken", 4),
    ),
    "Hexalith.Parties.Client.Tests": (
        (CLIENT_TYPE, "DelayedReply_ValidatesCompletionTime", 9),
    ),
}
RECEIPT_TESTS = "parties_readiness_verifier_test.ExactTargetReceiptTests"
RECEIPT_CASES = {"test_matching_receipt_passes", "test_matching_receipt_passes_through_wrapper"} | {
    f"test_mismatched_{field}_prevents_http{suffix}"
    for field in ("recordId", "targetVersionOrCommit", "compatibilityCommand", "outcome",
                  "evidenceLevel", "prerequisitesAvailable")
    for suffix in ("", "_wrapper")
}
CASE_LABELS = {
    "CurrentCompletionTimeChangesDuringCustody_DenyEvidence": tuple(
        f'(change: "{value}")' for value in ("interval-expiry", "custody-expiry", "source-rollback", "binding-rollback")),
    "CurrentAuthorityChangesDuringAwait_DenyEvidence": ("(inCustody: False)", "(inCustody: True)"),
    "CurrentHuman_RequiresCompleteSourceAndCurrentExactActor": ("",),
    "CurrentAuthorityChangeDuringReadMustNotReleaseIdentity": tuple(
        f'(inCustody: {suspended}, change: "{change}")' for suspended in ("False", "True")
        for change in ("source", "actor-revision", "authority-revision", "unchanged")),
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
    "DelayedReply_ValidatesCompletionTime": tuple(f'(scenario: "{value}")' for value in (
        "current-valid", "current-interval-expiry", "current-custody-expiry", "current-source-rollback",
        "current-binding-rollback", "current-future-observation", "historical-closed-valid",
        "historical-custody-expiry", "historical-future-action")),
}


class IdentityVerificationError(RuntimeError):
    """The local verification did not establish every required regression."""


def run(command: list[str], log: Path, *, cwd: Path = ROOT, timeout: int = 600) -> str:
    """Run a bounded command, preserving its output even when it fails."""
    print(f"Running {log.stem}", flush=True)
    try:
        with log.open("w", encoding="utf-8") as output:
            result = subprocess.run(command, cwd=cwd, stdout=output, stderr=subprocess.STDOUT,
                                    text=True, timeout=timeout,
                                    env=dict(os.environ, DOTNET_CLI_UI_LANGUAGE="en-US"))
    except (OSError, subprocess.TimeoutExpired) as error:
        raise IdentityVerificationError(f"{log.stem} could not complete; see {log}: {error}") from error
    content = log.read_text(encoding="utf-8")
    if result.returncode != 0:
        raise IdentityVerificationError(f"{log.stem} failed ({result.returncode}); see {log}")
    return content


def validate_results(path: Path, assembly: Path, test_type: str, method: str, minimum: int) -> int:
    """Require fresh, internally consistent results from the exact requested method."""
    try:
        results = ET.parse(path).getroot()
        assemblies = results.findall("assembly")
        if results.tag != "assemblies" or len(assemblies) != 1:
            raise ValueError("expected exactly one assembly")
        report = assemblies[0]
        if Path(report.attrib["name"]).resolve() != assembly.resolve():
            raise ValueError("unexpected assembly identity")
        total = int(report.attrib["total"])
        if total < minimum or int(report.attrib["passed"]) != total:
            raise ValueError("missing or unsuccessful cases")
        if any(int(report.attrib[key]) != 0 for key in ("failed", "errors", "skipped", "not-run")):
            raise ValueError("failed, skipped, errored or unrun cases")
        tests = report.findall(".//test")
        if len(tests) != total or report.findall(".//error"):
            raise ValueError("inconsistent test inventory or runner errors")
        names = set()
        for test in tests:
            if (test.attrib["type"] != test_type or test.attrib["method"] != method
                    or test.attrib["result"] != "Pass" or not test.attrib["name"]
                    or test.attrib["name"] in names):
                raise ValueError("unexpected, duplicate or unsuccessful test")
            names.add(test.attrib["name"])
        normalized = {name.replace("\\", "") for name in names}
        required = {f"{test_type}.{method}{label}" for label in CASE_LABELS[method]}
        if not required.issubset(normalized):
            raise ValueError("required theory scenarios did not execute")
        return total
    except (OSError, ET.ParseError, KeyError, ValueError) as error:
        raise IdentityVerificationError(f"invalid results for {test_type}.{method}: {error}") from error


def verify_locked(artifacts: Path) -> None:
    """Verify one exclusively owned artifact directory, discarding previous evidence first."""
    receipt = artifacts / "identity-verification.json"
    receipt.unlink(missing_ok=True)
    for selections in SELECTIONS.values():
        for _, method, _ in selections:
            for extension in ("xml", "log"):
                (artifacts / f"{method}.{extension}").unlink(missing_ok=True)
    for name in (*[f"{project}.build.log" for project in SELECTIONS], "exact-target-receipts.log",
                 "eventstore-version.log", "agents-revision.log", "agents-status.log",
                 "parties-revision.log", "parties-status.log"):
        (artifacts / name).unlink(missing_ok=True)
    evidence = {"runId": uuid4().hex, "startedAt": dt.datetime.now(dt.timezone.utc).isoformat(), "sources": {}}
    for name, directory in (("agents", ROOT), ("parties", ROOT / "references/Hexalith.Parties")):
        commit = run(["git", "rev-parse", "HEAD"], artifacts / f"{name}-revision.log", cwd=directory).strip()
        if not re.fullmatch(r"[0-9a-f]{40}", commit):
            raise IdentityVerificationError(f"could not resolve exact {name} source revision")
        status = run(["git", "status", "--porcelain", "--untracked-files=normal"],
                     artifacts / f"{name}-status.log", cwd=directory).splitlines()
        evidence["sources"][name] = {"commit": commit, "dirty": bool(status), "status": status}
    # A single version must reach every transitive project, including Tenants.
    version_project = ROOT / "references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj"
    version = run(["dotnet", "msbuild", str(version_project), "-nologo",
                   "-getProperty:HexalithEventStoreVersion"], artifacts / "eventstore-version.log").strip()
    if not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?", version):
        raise IdentityVerificationError("could not resolve an exact EventStore package version")
    properties = ["-p:UseHexalithProjectReferences=true", "-p:UseNuGetDeps=false",
                  "-p:MinVerVersionOverride=1.0.0", f"-p:HexalithEventStoreVersion={version}",
                  f"-p:CustomAfterMicrosoftCommonTargets={ROOT / 'tests/identity/identity-regressions.targets'}"]
    for module in ("Builds", "EventStore", "Commons", "Tenants", "McpCli", "FrontComposer"):
        properties.append(f"-p:Hexalith{module}Root={ROOT / 'references' / ('Hexalith.' + module)}")
    for module in ("EventStore", "Commons", "CommonsHttp", "CommonsServiceDefaults", "Tenants", "McpCli", "FrontComposer"):
        properties.append(f"-p:Hexalith{module}FromSource=true")
    properties.append("-p:HexalithMemoriesFromSource=false")
    counts = {}
    for project, selections in SELECTIONS.items():
        project_file = ROOT / "references/Hexalith.Parties/tests" / project / f"{project}.csproj"
        output = run(["dotnet", "build", str(project_file), "--configuration", "Debug", "-m:1",
                      "--artifacts-path", str(artifacts / "build"), "--nologo", *properties],
                     artifacts / f"{project}.build.log")
        if not re.search(r"(?m)^\s*0 Warning\(s\)\s*$", output):
            raise IdentityVerificationError(f"warning-free build not established: {project}")
        assembly = artifacts / "build/bin" / project / "debug" / f"{project}.dll"
        if not assembly.is_file():
            raise IdentityVerificationError(f"built test assembly missing: {assembly}")
        for test_type, method, minimum in selections:
            xml = artifacts / f"{method}.xml"
            xml.unlink(missing_ok=True)
            run(["dotnet", str(assembly), "-method", f"{test_type}.{method}", "-result-xml", str(xml)],
                artifacts / f"{method}.log", timeout=120)
            counts[f"{test_type}.{method}"] = validate_results(xml, assembly, test_type, method, minimum)
    readiness = run([sys.executable, "-m", "unittest", "-v", RECEIPT_TESTS],
                    artifacts / "exact-target-receipts.log", cwd=ROOT / "tests/tooling")
    completed = re.findall(r"(?m)^(test_\w+) \(" + re.escape(RECEIPT_TESTS) + r"(?:\.\1)?\) \.\.\. ok$", readiness)
    if (set(completed) != RECEIPT_CASES or len(completed) != len(RECEIPT_CASES)
            or not re.search(r"(?m)^Ran 14 tests in ", readiness) or not re.search(r"(?m)^OK\s*$", readiness)):
        raise IdentityVerificationError("receipt regression tests did not complete without skips")
    evidence.update(localBehaviorVerified=True, liveReadinessEstablished=False,
                    eventStoreVersion=version, ownerCases=counts, receiptSuite=RECEIPT_TESTS,
                    completedAt=dt.datetime.now(dt.timezone.utc).isoformat())
    receipt.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(f"Verified {sum(counts.values())} owner cases and exact-target receipt regressions; evidence: {receipt}")


def verify(artifacts: Path) -> None:
    """Reject overlapping invocations before touching shared outputs."""
    artifacts.mkdir(parents=True, exist_ok=True)
    lock = artifacts / ".identity-verification.lock"
    try:
        lock.mkdir()
    except FileExistsError as error:
        raise IdentityVerificationError(f"artifact directory is locked by another invocation: {lock}") from error
    try:
        verify_locked(artifacts)
    finally:
        lock.rmdir()


def main() -> int:
    """Resolve caller-relative evidence before running any owner command."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--artifacts", type=Path, required=True)
    args = parser.parse_args()
    verify(args.artifacts.resolve())
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except IdentityVerificationError as error:
        print(f"verify-parties-identity: {error}", file=sys.stderr)
        raise SystemExit(1)
