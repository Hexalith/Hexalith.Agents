#!/usr/bin/env python3
"""Verify pinned identity authority, continuity, endpoint security and retained transport regressions."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
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
PROCESS_TYPE = "Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests"
SECURITY_TYPE = "Hexalith.Parties.Tests.Gateway.PartiesDomainServiceSecurityTests"
TRANSPORT_TYPE = "Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests"
SOURCE_DIRECTORIES = {"agents": ROOT} | {
    module.lower(): ROOT / "references" / ("Hexalith." + module)
    for module in ("Parties", "EventStore", "Builds", "Commons", "Tenants", "McpCli", "FrontComposer")
}
FULL_ROUTE_ARGUMENTS = (
    "/actors/PartyProjectionActor/party-1/method/Untrusted",
    "/tenants/events",
)
ROUTE_SOURCE = ("references/Hexalith.Parties/tests/Hexalith.Parties.Tests/Gateway/"
                "PartiesDomainServiceSecurityTests.cs")
PROJECT_SOURCES = {
    "Hexalith.Parties.Tests": "Hexalith.Parties",
    "Hexalith.Parties.Client.Tests": "Hexalith.Parties",
    "Hexalith.EventStore.Client.Tests": "Hexalith.EventStore",
}
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
        (QUERY_TYPE, "ExpiredPredecessorOmitted_SuccessorReadRemainsUnavailable", 1),
        (QUERY_TYPE, "ExpiredPredecessorCertifiedWithoutActor_ResolvesOnlyRetainedSuccessor", 7),
        (QUERY_TYPE, "CertifiedExpiredEstablishmentRetainedRevocationAndReboundResolveThroughQuery", 5),
        (QUERY_TYPE, "MultipleExpiredTransitionsResolveOnlyRetainedVersionThreeThroughSerializedQuery", 2),
        (PROCESS_TYPE, "PostProcess_InvokesRegisteredPartiesDomainProcessorAsync", 1),
        (PROCESS_TYPE, "PostProcess_PreservesResultPayloadAcrossPartiesHostWireSerializationAsync", 1),
        (PROCESS_TYPE, "PostProcess_WhenGdprFeaturesActive_DoesNotEmitMvpComplianceWarningHeaderAsync", 1),
        (PROCESS_TYPE, "PostProcess_InvalidPayload_UsesProductionProcessorValidationRejectionAsync", 1),
        (PROCESS_TYPE, "PostProcess_SerializedLifecycleHistory_EnforcesProductionReplayBoundaryAsync", 4),
        (PROCESS_TYPE, "ProductionAsyncProcessor_AlreadyCanceledToken_ThrowsBeforeProducingEventsAsync", 1),
        (PROCESS_TYPE, "PostProcess_CommonPartyDomainCaseVariants_ResolveProductionProcessorAsync", 3),
        (PROCESS_TYPE, "PostReplayState_UsesProductionProcessorReplayCapabilityAsync", 1),
        (SECURITY_TYPE, "PostProcess_InvalidCredentials_DenyBeforeDomainWork", 15),
        (SECURITY_TYPE, "SidecarRoute_RequiresAuthenticatedChannel", 16),
        (SECURITY_TYPE, "SidecarDelivery_MissingChannel_DeniesBeforeDispatch", 2),
        (SECURITY_TYPE, "ActualEndpointInventory_PassesUnchangedSdkAudit", 1),
        (SECURITY_TYPE, "InheritedAuthenticationSettings_CannotAlterFixtureContracts", 1),
    ),
    "Hexalith.Parties.Client.Tests": (
        (CLIENT_TYPE, "DelayedReply_ValidatesCompletionTime", 9),
    ),
    "Hexalith.EventStore.Client.Tests": (
        (TRANSPORT_TYPE, "CompleteAuthenticatedSparseSourceRetainsItsOriginalHeadAndPositions", 1),
        (TRANSPORT_TYPE, "ForeignOwnerResponseHasNoUsableHistory", 1),
        (TRANSPORT_TYPE, "MissingExcludedPositionHasNoUsableHistory", 1),
        (TRANSPORT_TYPE, "ServerFailureCannotReturnItsPartialPayload", 1),
        (TRANSPORT_TYPE, "CallerCancellationPropagates", 1),
        (TRANSPORT_TYPE, "CertificateExpiredDuringTransitCannotReleaseHistory", 1),
        (TRANSPORT_TYPE, "CancellationDuringCertificateValidationCannotReleaseHistory", 1),
        (TRANSPORT_TYPE, "MaximumReadablePayloadFitsTheSharedBase64WireBound", 1),
        (TRANSPORT_TYPE, "NoncooperativeTransportStopsBeforeCompletionAndDisposesLateMaterial", 8),
        (TRANSPORT_TYPE, "SynchronousTransportInvocationIsBoundedAndLateOwnedMaterialDisposed", 8),
        (TRANSPORT_TYPE, "BlockingProviderCancellationCannotRetainTransportDeadline", 4),
        (TRANSPORT_TYPE, "CumulativeTerminalValidationCannotReleaseAtReadBudgetBoundary", 1),
        (TRANSPORT_TYPE, "CompletedResponseBlockingDisposalCannotRetainRead", 2),
        (TRANSPORT_TYPE, "CompletedResponseCleanupFailureStillDisposesOwnedRequest", 1),
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
    "ExpiredPredecessorOmitted_SuccessorReadRemainsUnavailable": (
        '',
    ),
    "ExpiredPredecessorCertifiedWithoutActor_ResolvesOnlyRetainedSuccessor": (
        '(vector: "valid", expected: Resolved)',
        '(vector: "foreign", expected: Unavailable)',
        '(vector: "wrong-policy", expected: Unavailable)',
        '(vector: "expired", expected: Unavailable)',
        '(vector: "unknown-contract", expected: Unavailable)',
        '(vector: "wrong-transition", expected: Unavailable)',
        '(vector: "overlap", expected: Unavailable)',
    ),
    "CertifiedExpiredEstablishmentRetainedRevocationAndReboundResolveThroughQuery": (
        '(vector: "valid")',
        '(vector: "unproved")',
        '(vector: "gap")',
        '(vector: "wrong-rebound")',
        '(vector: "expired-after-readable")',
    ),
    "MultipleExpiredTransitionsResolveOnlyRetainedVersionThreeThroughSerializedQuery": (
        '(expiredTransition: "rebound")',
        '(expiredTransition: "revocation")',
    ),
    "PostProcess_InvokesRegisteredPartiesDomainProcessorAsync": (
        '',
    ),
    "PostProcess_PreservesResultPayloadAcrossPartiesHostWireSerializationAsync": (
        '',
    ),
    "PostProcess_WhenGdprFeaturesActive_DoesNotEmitMvpComplianceWarningHeaderAsync": (
        '',
    ),
    "PostProcess_InvalidPayload_UsesProductionProcessorValidationRejectionAsync": (
        '',
    ),
    "PostProcess_SerializedLifecycleHistory_EnforcesProductionReplayBoundaryAsync": (
        '(corruption: "none")',
        '(corruption: "metadata-version")',
        '(corruption: "event-contract")',
        '(corruption: "payload-version")',
    ),
    "ProductionAsyncProcessor_AlreadyCanceledToken_ThrowsBeforeProducingEventsAsync": (
        '',
    ),
    "PostProcess_CommonPartyDomainCaseVariants_ResolveProductionProcessorAsync": (
        '(domain: "Party")',
        '(domain: "PARTY")',
        '(domain: "pArTy")',
    ),
    "PostReplayState_UsesProductionProcessorReplayCapabilityAsync": (
        '',
    ),
    "PostProcess_InvalidCredentials_DenyBeforeDomainWork": (
        '(scenario: "missing", expected: Unauthorized)',
        '(scenario: "caller-header", expected: Unauthorized)',
        '(scenario: "channel-only", expected: Unauthorized)',
        '(scenario: "missing-channel", expected: Unauthorized)',
        '(scenario: "wrong-channel", expected: Unauthorized)',
        '(scenario: "duplicate-channel", expected: Unauthorized)',
        '(scenario: "forged-assertion", expected: Unauthorized)',
        '(scenario: "foreign-assertion", expected: Unauthorized)',
        '(scenario: "duplicate-assertion", expected: Unauthorized)',
        '(scenario: "conflicting-caller", expected: Unauthorized)',
        '(scenario: "human-bearer", expected: Unauthorized)',
        '(scenario: "human-bearer-only", expected: Unauthorized)',
        '(scenario: "disallowed-workload", expected: Unauthorized)',
        '(scenario: "wrong-audience", expected: Unauthorized)',
        '(scenario: "wrong-operation", expected: Forbidden)',
    ),
    "SidecarRoute_RequiresAuthenticatedChannel": (
        '(route: "/dapr/subscribe", channel: "valid")',
        '(route: "/dapr/subscribe", channel: "missing")',
        '(route: "/dapr/subscribe", channel: "wrong")',
        '(route: "/dapr/subscribe", channel: "duplicate")',
        '(route: "/dapr/config", channel: "valid")',
        '(route: "/dapr/config", channel: "missing")',
        '(route: "/dapr/config", channel: "wrong")',
        '(route: "/dapr/config", channel: "duplicate")',
        '(route: "/healthz", channel: "valid")',
        '(route: "/healthz", channel: "missing")',
        '(route: "/healthz", channel: "wrong")',
        '(route: "/healthz", channel: "duplicate")',
        '(route: "/healthz/", channel: "valid")',
        '(route: "/healthz/", channel: "missing")',
        '(route: "/healthz/", channel: "wrong")',
        '(route: "/healthz/", channel: "duplicate")',
    ),
    # Preserve xUnit's display abbreviation for the long actor route argument.
    "SidecarDelivery_MissingChannel_DeniesBeforeDispatch": (
        '(route: "/actors/PartyProjectionActor/party-1/method/Untrus"···)',
        '(route: "/tenants/events")',
    ),
    "ActualEndpointInventory_PassesUnchangedSdkAudit": (
        '',
    ),
    "InheritedAuthenticationSettings_CannotAlterFixtureContracts": (
        '',
    ),
    "CompleteAuthenticatedSparseSourceRetainsItsOriginalHeadAndPositions": (
        '',
    ),
    "ForeignOwnerResponseHasNoUsableHistory": (
        '',
    ),
    "MissingExcludedPositionHasNoUsableHistory": (
        '',
    ),
    "ServerFailureCannotReturnItsPartialPayload": (
        '',
    ),
    "CallerCancellationPropagates": (
        '',
    ),
    "CertificateExpiredDuringTransitCannotReleaseHistory": (
        '',
    ),
    "CancellationDuringCertificateValidationCannotReleaseHistory": (
        '',
    ),
    "MaximumReadablePayloadFitsTheSharedBase64WireBound": (
        '',
    ),
    "NoncooperativeTransportStopsBeforeCompletionAndDisposesLateMaterial": (
        '(stage: 0, expireDeadline: False)',
        '(stage: 1, expireDeadline: False)',
        '(stage: 2, expireDeadline: False)',
        '(stage: 3, expireDeadline: False)',
        '(stage: 0, expireDeadline: True)',
        '(stage: 1, expireDeadline: True)',
        '(stage: 2, expireDeadline: True)',
        '(stage: 3, expireDeadline: True)',
    ),
    "SynchronousTransportInvocationIsBoundedAndLateOwnedMaterialDisposed": (
        '(stage: 0, expire: False)',
        '(stage: 1, expire: False)',
        '(stage: 2, expire: False)',
        '(stage: 3, expire: False)',
        '(stage: 0, expire: True)',
        '(stage: 1, expire: True)',
        '(stage: 2, expire: True)',
        '(stage: 3, expire: True)',
    ),
    "BlockingProviderCancellationCannotRetainTransportDeadline": (
        '(stage: 0)',
        '(stage: 1)',
        '(stage: 2)',
        '(stage: 3)',
    ),
    "CumulativeTerminalValidationCannotReleaseAtReadBudgetBoundary": (
        '',
    ),
    "CompletedResponseBlockingDisposalCannotRetainRead": (
        '(completedAtDeadline: False)',
        '(completedAtDeadline: True)',
    ),
    "CompletedResponseCleanupFailureStillDisposesOwnedRequest": (
        '',
    ),
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



def run_bytes(command: list[str], log: Path, *, cwd: Path) -> bytes:
    """Preserve exact Git output bytes without exposing source diffs to the console."""
    print(f"Running {log.stem}", flush=True)
    try:
        with log.open("wb") as output:
            result = subprocess.run(command, cwd=cwd, stdout=output, stderr=subprocess.STDOUT,
                                    timeout=120, env=dict(os.environ, DOTNET_CLI_UI_LANGUAGE="en-US"))
        if result.returncode != 0:
            raise IdentityVerificationError(f"{log.stem} failed ({result.returncode}); see {log}")
        return log.read_bytes()
    except (OSError, subprocess.TimeoutExpired) as error:
        raise IdentityVerificationError(f"{log.stem} could not complete; see {log}: {error}") from error


def sha256_file(path: Path) -> str:
    """Hash the exact collected binary without loading it all into memory."""
    try:
        with path.open("rb") as stream:
            return hashlib.file_digest(stream, "sha256").hexdigest()
    except OSError as error:
        raise IdentityVerificationError(f"could not fingerprint {path}: {error}") from error


def source_state(name: str, directory: Path, artifacts: Path, phase: str) -> dict:
    """Bind dirty provenance to tracked diff bytes and every untracked file or symlink."""
    prefix = f"{phase}-{name}"
    commit = run(["git", "rev-parse", "HEAD"], artifacts / f"{prefix}-revision.log", cwd=directory).strip()
    if not re.fullmatch(r"[0-9a-f]{40}", commit):
        raise IdentityVerificationError(f"could not resolve exact {name} source revision")
    status = run(["git", "status", "--porcelain", "--untracked-files=normal"],
                 artifacts / f"{prefix}-status.log", cwd=directory).splitlines()
    diff = run_bytes(["git", "diff", "--binary", "--no-ext-diff", "--no-textconv", "HEAD", "--"],
                     artifacts / f"{prefix}-diff.log", cwd=directory)
    listed = run_bytes(["git", "ls-files", "--others", "--exclude-standard", "-z"],
                       artifacts / f"{prefix}-untracked.log", cwd=directory)
    if listed and not listed.endswith(b"\0"):
        raise IdentityVerificationError(f"incomplete untracked source inventory: {name}")
    paths = listed[:-1].split(b"\0") if listed else []
    if len(set(paths)) != len(paths):
        raise IdentityVerificationError(f"duplicate untracked source path: {name}")
    untracked = []
    for raw in sorted(paths):
        relative = Path(os.fsdecode(raw))
        if not raw or relative.is_absolute() or ".." in relative.parts or relative == Path("."):
            raise IdentityVerificationError(f"invalid untracked source path: {name}")
        path = directory / relative
        try:
            if path.is_symlink():
                kind = "symlink"
                digest = hashlib.sha256(os.fsencode(os.readlink(path))).hexdigest()
            elif path.is_file():
                kind = "file"
                digest = sha256_file(path)
            else:
                raise IdentityVerificationError(f"unsupported untracked source object: {name}/{relative}")
        except OSError as error:
            raise IdentityVerificationError(f"could not fingerprint untracked source {name}/{relative}: {error}") from error
        untracked.append({"path": os.fsdecode(raw), "kind": kind, "sha256": digest})
    return {"commit": commit, "dirty": bool(status), "status": status,
            "fingerprint": {"trackedDiffSha256": hashlib.sha256(diff).hexdigest(), "untracked": untracked}}



def source_argument_inventory() -> dict:
    """Require full route arguments that xUnit abbreviates in its native display names."""
    path = ROOT / ROUTE_SOURCE
    method = "SidecarDelivery_MissingChannel_DeniesBeforeDispatch"
    try:
        content = path.read_bytes()
        source = content.decode("utf-8-sig")
    except (OSError, UnicodeError) as error:
        raise IdentityVerificationError(f"could not inspect complete route argument inventory: {error}") from error
    pattern = (r"((?:\s*\[[^\]\r\n]*\])+\s*)public\s+async\s+Task\s+"
               + re.escape(method) + r"\s*\(\s*string\s+route\s*\)")
    matches = list(re.finditer(pattern, source))
    literal = r'"(?:[^"\\\r\n]|\\.)*"'
    attributes = (r"\s*\[\s*Theory\s*\]\s*"
                  + (r"\[\s*InlineData\s*\(\s*(" + literal + r")\s*\)\s*\]\s*") * len(FULL_ROUTE_ARGUMENTS))
    arguments = re.fullmatch(attributes, matches[0].group(1)) if len(matches) == 1 else None
    try:
        routes = tuple(json.loads(value) for value in arguments.groups()) if arguments else ()
    except (TypeError, ValueError):
        routes = ()
    if routes != FULL_ROUTE_ARGUMENTS:
        raise IdentityVerificationError("complete route argument inventory changed for " + method)
    return {f"{SECURITY_TYPE}.{method}": {"sourcePath": ROUTE_SOURCE,
            "sourceSha256": hashlib.sha256(content).hexdigest(),
            "arguments": [{"route": route} for route in routes]}}


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
                 "eventstore-version.log"):
        (artifacts / name).unlink(missing_ok=True)
    for phase in ("before", "after"):
        for name in SOURCE_DIRECTORIES:
            for suffix in ("revision", "status", "diff", "untracked"):
                (artifacts / f"{phase}-{name}-{suffix}.log").unlink(missing_ok=True)
    evidence = {"runId": uuid4().hex, "startedAt": dt.datetime.now(dt.timezone.utc).isoformat(),
                "sources": {name: source_state(name, directory, artifacts, "before")
                            for name, directory in SOURCE_DIRECTORIES.items()}}
    evidence["sourceArgumentInventory"] = source_argument_inventory()
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
    assemblies = {}
    assembly_hashes = {}
    for project, selections in SELECTIONS.items():
        project_file = ROOT / "references" / PROJECT_SOURCES[project] / "tests" / project / f"{project}.csproj"
        output = run(["dotnet", "build", str(project_file), "--configuration", "Debug", "-m:1",
                      "--artifacts-path", str(artifacts / "build"), "--nologo", *properties],
                     artifacts / f"{project}.build.log")
        if not re.search(r"(?m)^\s*0 Warning\(s\)\s*$", output):
            raise IdentityVerificationError(f"warning-free build not established: {project}")
        assembly = artifacts / "build/bin" / project / "debug" / f"{project}.dll"
        if not assembly.is_file():
            raise IdentityVerificationError(f"built test assembly missing: {assembly}")
        assemblies[project] = assembly
        assembly_hashes[project] = sha256_file(assembly)
        for test_type, method, minimum in selections:
            if sha256_file(assembly) != assembly_hashes[project]:
                raise IdentityVerificationError(f"built test assembly changed during verification: {project}")
            xml = artifacts / f"{method}.xml"
            xml.unlink(missing_ok=True)
            run(["dotnet", str(assembly), "-method", f"{test_type}.{method}", "-result-xml", str(xml)],
                artifacts / f"{method}.log", timeout=120)
            if sha256_file(assembly) != assembly_hashes[project]:
                raise IdentityVerificationError(f"built test assembly changed during verification: {project}")
            counts[f"{test_type}.{method}"] = validate_results(xml, assembly, test_type, method, minimum)
    readiness = run([sys.executable, "-m", "unittest", "-v", RECEIPT_TESTS],
                    artifacts / "exact-target-receipts.log", cwd=ROOT / "tests/tooling")
    completed = re.findall(r"(?m)^(test_\w+) \(" + re.escape(RECEIPT_TESTS) + r"(?:\.\1)?\) \.\.\. ok$", readiness)
    if (set(completed) != RECEIPT_CASES or len(completed) != len(RECEIPT_CASES)
            or not re.search(r"(?m)^Ran 14 tests in ", readiness) or not re.search(r"(?m)^OK\s*$", readiness)):
        raise IdentityVerificationError("receipt regression tests did not complete without skips")
    for name, directory in SOURCE_DIRECTORIES.items():
        current = source_state(name, directory, artifacts, "after")
        if current != evidence["sources"][name]:
            raise IdentityVerificationError(f"source changed during verification: {name}")
    for project, assembly in assemblies.items():
        if sha256_file(assembly) != assembly_hashes[project]:
            raise IdentityVerificationError(f"built test assembly changed during verification: {project}")
    evidence.update(assemblySha256=assembly_hashes, localBehaviorVerified=True, liveReadinessEstablished=False,
                    eventStoreVersion=version, ownerCases=counts, receiptSuite=RECEIPT_TESTS,
                    completedAt=dt.datetime.now(dt.timezone.utc).isoformat())
    receipt.write_text(json.dumps(evidence, indent=2) + "\n", encoding="utf-8")
    print(f"Verified {sum(counts.values())} owner cases and exact-target receipt regressions; evidence: {receipt}")


def verify(artifacts: Path) -> None:
    """Reject source-overlapping outputs and overlapping invocations before touching outputs."""
    artifacts = artifacts.resolve()
    for directory in SOURCE_DIRECTORIES.values():
        source = directory.resolve()
        if artifacts == source or source in artifacts.parents or artifacts in source.parents:
            raise IdentityVerificationError(
                "artifact directory overlaps consumed sources; choose an output directory outside every source root: "
                + str(artifacts))
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
