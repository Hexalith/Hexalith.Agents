"""Exercise the maintained owner-regression command without real builds or owner calls."""

from __future__ import annotations

import hashlib
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
SOURCE_REVISIONS = {"agents": "a", "parties": "b", "eventstore": "c", "builds": "d",
                    "commons": "e", "tenants": "f", "mcpcli": "1", "frontcomposer": "2"}
GIT_SOURCE_COMMANDS = (["rev-parse", "HEAD"], ["status", "--porcelain", "--untracked-files=normal"],
                       ["diff", "--binary", "--no-ext-diff", "--no-textconv", "HEAD", "--"],
                       ["ls-files", "--others", "--exclude-standard", "-z"])
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

# Independent native case inventory: never import the command under test.
NEW_CASES = {
    'Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ExpiredPredecessorOmitted_SuccessorReadRemainsUnavailable': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.ExpiredPredecessorCertifiedWithoutActor_ResolvesOnlyRetainedSuccessor': (
        '(vector: "valid", expected: Resolved)',
        '(vector: "foreign", expected: Unavailable)',
        '(vector: "wrong-policy", expected: Unavailable)',
        '(vector: "expired", expected: Unavailable)',
        '(vector: "unknown-contract", expected: Unavailable)',
        '(vector: "wrong-transition", expected: Unavailable)',
        '(vector: "overlap", expected: Unavailable)',
    ),
    'Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.CertifiedExpiredEstablishmentRetainedRevocationAndReboundResolveThroughQuery': (
        '(vector: "valid")',
        '(vector: "unproved")',
        '(vector: "gap")',
        '(vector: "wrong-rebound")',
        '(vector: "expired-after-readable")',
    ),
    'Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests.MultipleExpiredTransitionsResolveOnlyRetainedVersionThreeThroughSerializedQuery': (
        '(expiredTransition: "rebound")',
        '(expiredTransition: "revocation")',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.PostProcess_InvokesRegisteredPartiesDomainProcessorAsync': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.PostProcess_PreservesResultPayloadAcrossPartiesHostWireSerializationAsync': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.PostProcess_WhenGdprFeaturesActive_DoesNotEmitMvpComplianceWarningHeaderAsync': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.PostProcess_InvalidPayload_UsesProductionProcessorValidationRejectionAsync': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.PostProcess_SerializedLifecycleHistory_EnforcesProductionReplayBoundaryAsync': (
        '(corruption: "none")',
        '(corruption: "metadata-version")',
        '(corruption: "event-contract")',
        '(corruption: "payload-version")',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.ProductionAsyncProcessor_AlreadyCanceledToken_ThrowsBeforeProducingEventsAsync': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.PostProcess_CommonPartyDomainCaseVariants_ResolveProductionProcessorAsync': (
        '(domain: "Party")',
        '(domain: "PARTY")',
        '(domain: "pArTy")',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesProcessEndpointTests.PostReplayState_UsesProductionProcessorReplayCapabilityAsync': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesDomainServiceSecurityTests.PostProcess_InvalidCredentials_DenyBeforeDomainWork': (
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
    'Hexalith.Parties.Tests.Gateway.PartiesDomainServiceSecurityTests.SidecarRoute_RequiresAuthenticatedChannel': (
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
    'Hexalith.Parties.Tests.Gateway.PartiesDomainServiceSecurityTests.SidecarDelivery_MissingChannel_DeniesBeforeDispatch': (
        '(route: "/actors/PartyProjectionActor/party-1/method/Untrus"···)',
        '(route: "/tenants/events")',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesDomainServiceSecurityTests.ActualEndpointInventory_PassesUnchangedSdkAudit': (
        '',
    ),
    'Hexalith.Parties.Tests.Gateway.PartiesDomainServiceSecurityTests.InheritedAuthenticationSettings_CannotAlterFixtureContracts': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.CompleteAuthenticatedSparseSourceRetainsItsOriginalHeadAndPositions': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.ForeignOwnerResponseHasNoUsableHistory': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.MissingExcludedPositionHasNoUsableHistory': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.ServerFailureCannotReturnItsPartialPayload': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.CallerCancellationPropagates': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.CertificateExpiredDuringTransitCannotReleaseHistory': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.CancellationDuringCertificateValidationCannotReleaseHistory': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.MaximumReadablePayloadFitsTheSharedBase64WireBound': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.NoncooperativeTransportStopsBeforeCompletionAndDisposesLateMaterial': (
        '(stage: 0, expireDeadline: False)',
        '(stage: 1, expireDeadline: False)',
        '(stage: 2, expireDeadline: False)',
        '(stage: 3, expireDeadline: False)',
        '(stage: 0, expireDeadline: True)',
        '(stage: 1, expireDeadline: True)',
        '(stage: 2, expireDeadline: True)',
        '(stage: 3, expireDeadline: True)',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.SynchronousTransportInvocationIsBoundedAndLateOwnedMaterialDisposed': (
        '(stage: 0, expire: False)',
        '(stage: 1, expire: False)',
        '(stage: 2, expire: False)',
        '(stage: 3, expire: False)',
        '(stage: 0, expire: True)',
        '(stage: 1, expire: True)',
        '(stage: 2, expire: True)',
        '(stage: 3, expire: True)',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.BlockingProviderCancellationCannotRetainTransportDeadline': (
        '(stage: 0)',
        '(stage: 1)',
        '(stage: 2)',
        '(stage: 3)',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.CumulativeTerminalValidationCannotReleaseAtReadBudgetBoundary': (
        '',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.CompletedResponseBlockingDisposalCannotRetainRead': (
        '(completedAtDeadline: False)',
        '(completedAtDeadline: True)',
    ),
    'Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests.CompletedResponseCleanupFailureStillDisposesOwnedRequest': (
        '',
    ),
}

DOTNET = "NEW_CASES = " + repr(NEW_CASES) + "\n" + r'''
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
    eventstore = project == "Hexalith.EventStore.Client.Tests"
    if (scenario == "build-failure" or (scenario == "client-build-failure" and project.endswith("Client.Tests"))
            or scenario == "eventstore-build-failure" and eventstore):
        sys.exit(9)
    output = pathlib.Path(args[args.index("--artifacts-path") + 1])
    assembly = output / "bin" / project / "debug" / (project + ".dll")
    assembly.parent.mkdir(parents=True, exist_ok=True)
    assembly.unlink(missing_ok=True)
    if scenario != "missing-assembly" and not (scenario == "eventstore-missing-assembly" and eventstore):
        assembly.write_text("synthetic")
    print("1 Warning(s)" if scenario == "build-warning" or scenario == "eventstore-build-warning" and eventstore else
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
         "DelayedReply_ValidatesCompletionTime": 9}.get(method, len(NEW_CASES.get(selection, ())))
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
}.get(method, NEW_CASES.get(selection, ()))
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
if scenario == "changed-assembly" and method == "CompletedResponseCleanupFailureStillDisposesOwnedRequest":
    assembly.write_bytes(b"changed runner binary")
'''

RECEIPT_SUITE = '''
import json, os, pathlib, unittest
with open(os.environ["IDENTITY_CALLS"], "a") as log:
    log.write(json.dumps({"stage": "receipt"}) + "\\n")
scenario = os.environ.get("IDENTITY_SCENARIO")
fixture_root = pathlib.Path(__file__).resolve().parents[2]
if scenario in ("late-untracked", "late-symlink"):
    target = fixture_root / ("untracked data.bin" if scenario == "late-untracked" else "untracked link")
    if scenario == "late-untracked":
        target.write_bytes(b"changed after all owner and receipt tests")
    else:
        target.unlink()
        target.symlink_to("changed destination")
if scenario == "late-assembly":
    assembly = pathlib.Path(os.environ["IDENTITY_ARTIFACTS"]) / "build/bin/Hexalith.Parties.Tests/debug/Hexalith.Parties.Tests.dll"
    assembly.write_bytes(b"changed after all methods")
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
        git.write_text(f"#!{sys.executable}\n" + 'REVISIONS = ' + repr(SOURCE_REVISIONS) + "\n" + r'''
import json, os, pathlib, sys
calls = pathlib.Path(os.environ["IDENTITY_CALLS"])
previous = calls.read_text() if calls.exists() else ""
with calls.open("a") as log:
    log.write(json.dumps({"args": sys.argv[1:], "cwd": os.getcwd()}) + "\n")
scenario = os.environ.get("IDENTITY_SCENARIO")
directory = pathlib.Path.cwd()
name = directory.name.removeprefix("Hexalith.").lower() if directory.name.startswith("Hexalith.") else "agents"
fixture_root = directory.parents[1] if name != "agents" else directory
late = '"stage": "receipt"' in previous
target = os.environ.get("IDENTITY_DRIFT_SOURCE", "agents")
if sys.argv[1] == "rev-parse":
    if scenario == "eventstore-invalid-revision" and name == "eventstore":
        print("invalid")
    elif scenario == "late-revision" and late and name == target:
        print("9" * 40)
    else:
        print(REVISIONS[name] * 40)
elif sys.argv[1] == "status":
    if name == "agents" or scenario == "eventstore-dirty-source" and name == "eventstore":
        print(" M synthetic-test-change.cs")
elif sys.argv[1] == "diff":
    if name == "agents":
        sys.stdout.buffer.write((fixture_root / ".tracked.patch").read_bytes())
    if scenario == "late-tracked" and late and name == target:
        sys.stdout.buffer.write(b"changed tracked diff with unchanged status and HEAD")
elif sys.argv[1] == "ls-files":
    if name == "agents" and scenario in ("dirty-fingerprint", "late-untracked", "late-symlink"):
        sys.stdout.buffer.write(b"untracked data.bin\0untracked link\0")
else:
    sys.exit(7)
''')
        git.chmod(0o755)
        for module in ("Parties", "EventStore", "Builds", "Commons", "Tenants", "McpCli", "FrontComposer"):
            (self.root / "references" / ("Hexalith." + module)).mkdir(parents=True)
        (self.root / ".tracked.patch").write_bytes(b"tracked diff with binary bytes \0\xff")
        (self.root / "untracked data.bin").write_bytes(b"untracked content \0\xfe")
        (self.root / "untracked link").symlink_to("original destination")
        route_source = self.root / "references/Hexalith.Parties/tests/Hexalith.Parties.Tests/Gateway/PartiesDomainServiceSecurityTests.cs"
        route_source.parent.mkdir(parents=True)
        route_source.write_text('''
[Theory]
[InlineData("/actors/PartyProjectionActor/party-1/method/Untrusted")]
[InlineData("/tenants/events")]
public async Task SidecarDelivery_MissingChannel_DeniesBeforeDispatch(string route)
{ }
''')
        self.route_source = route_source
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
        self.environment["IDENTITY_ARTIFACTS"] = str(self.artifacts)
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
        self.assertEqual(set(receipt["sources"]), set(SOURCE_REVISIONS))
        empty = hashlib.sha256(b"").hexdigest()
        for name, revision in SOURCE_REVISIONS.items():
            source = receipt["sources"][name]
            self.assertEqual(source["commit"], revision * 40)
            self.assertEqual(source["dirty"], name == "agents")
            self.assertEqual(source["status"], [" M synthetic-test-change.cs"] if name == "agents" else [])
            self.assertEqual(source["fingerprint"], {
                "trackedDiffSha256": hashlib.sha256((self.root / ".tracked.patch").read_bytes()).hexdigest()
                if name == "agents" else empty, "untracked": []})
        self.assertEqual(receipt["assemblySha256"], {
            name: hashlib.sha256(b"synthetic").hexdigest()
            for name in ("Hexalith.Parties.Tests", "Hexalith.Parties.Client.Tests", "Hexalith.EventStore.Client.Tests")})
        self.assertEqual(receipt["sourceArgumentInventory"], {
            "Hexalith.Parties.Tests.Gateway.PartiesDomainServiceSecurityTests."
            "SidecarDelivery_MissingChannel_DeniesBeforeDispatch": {
                "sourcePath": str(self.route_source.relative_to(self.root)),
                "sourceSha256": hashlib.sha256(self.route_source.read_bytes()).hexdigest(),
                "arguments": [{"route": "/actors/PartyProjectionActor/party-1/method/Untrusted"},
                              {"route": "/tenants/events"}]}})
        self.assertEqual(len(receipt["runId"]), 32)
        self.assertLessEqual(receipt["startedAt"], receipt["completedAt"])
        self.assertEqual(sorted(receipt["ownerCases"].values()), sorted([1, 2, 2, 2, 2, 2, 2, 4, 4, 4, 4, 4, 8, 8, 8, 9, 12]
                                + [len(labels) for labels in NEW_CASES.values()]))
        self.assertEqual(set(receipt["ownerCases"]), {
            "Hexalith.Parties.Tests.Gateway.PartyIdentityQueryHandlerTests." + method
            for method in ("CurrentCompletionTimeChangesDuringCustody_DenyEvidence",
                           "CurrentAuthorityChangesDuringAwait_DenyEvidence",
                           "CurrentHuman_RequiresCompleteSourceAndCurrentExactActor",
                           *DEADLINE_METHODS)
        } | {"Hexalith.Agents.Identity.Tests.CurrentIdentityAuthorityTests.CurrentAuthorityChangeDuringReadMustNotReleaseIdentity",
             "Hexalith.Parties.Client.Tests.HttpPartiesIdentityClientTests.DelayedReply_ValidatesCompletionTime"} | set(NEW_CASES))
        builds = [call for call in calls if call.get("args", [None])[0] == "build"]
        self.assertEqual([Path(call["args"][1]).stem for call in builds],
                         ["Hexalith.Parties.Tests", "Hexalith.Parties.Client.Tests", "Hexalith.EventStore.Client.Tests"])
        self.assertEqual([Path(call["args"][1]) for call in builds], [
            self.root / "references" / module / "tests" / project / (project + ".csproj")
            for module, project in (("Hexalith.Parties", "Hexalith.Parties.Tests"),
                                    ("Hexalith.Parties", "Hexalith.Parties.Client.Tests"),
                                    ("Hexalith.EventStore", "Hexalith.EventStore.Client.Tests"))])
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
        receipt_index = calls.index({"stage": "receipt"})
        self.assertEqual(len(calls[receipt_index + 1:]), 32)
        self.assertEqual(len(calls), 117)
        git_calls = [call for call in calls if call.get("args", [None])[0] in ("rev-parse", "status", "diff", "ls-files")]
        self.assertEqual(len(git_calls), 64)
        for phase in (git_calls[:32], git_calls[32:]):
            for index, name in enumerate(SOURCE_REVISIONS):
                source = self.root if name == "agents" else self.root / "references" / {
                    "parties": "Hexalith.Parties", "eventstore": "Hexalith.EventStore", "builds": "Hexalith.Builds",
                    "commons": "Hexalith.Commons", "tenants": "Hexalith.Tenants", "mcpcli": "Hexalith.McpCli",
                    "frontcomposer": "Hexalith.FrontComposer"}[name]
                for call, command in zip(phase[index * 4:index * 4 + 4], GIT_SOURCE_COMMANDS):
                    self.assertEqual(Path(call["cwd"]), source)
                    self.assertEqual(call["args"], command)

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

    def test_each_added_owner_selection_requires_complete_named_results(self):
        for selection in NEW_CASES:
            for scenario in ("missing-case", "unrelated-cases", "skipped"):
                with self.subTest(selection=selection, scenario=scenario):
                    self.calls.unlink(missing_ok=True)
                    self.environment["IDENTITY_TARGET_METHOD"] = selection.rsplit(".", 1)[1]
                    result, calls = self.invoke(scenario)
                    self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                    self.assertIn("invalid results for " + selection, result.stderr)
                    self.assertFalse(self.receipt.exists())
                    self.assertEqual(calls[-1]["args"][2], selection)
                    self.assertFalse(any(call.get("stage") == "receipt" for call in calls))

    def test_eventstore_build_failures_stop_after_prior_owner_results(self):
        for scenario in ("eventstore-build-failure", "eventstore-build-warning", "eventstore-missing-assembly"):
            with self.subTest(scenario=scenario):
                self.calls.unlink(missing_ok=True)
                result, calls = self.invoke(scenario)
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertFalse(self.receipt.exists())
                self.assertIn("Hexalith.EventStore.Client.Tests", result.stderr)
                self.assertEqual(calls[-1]["args"][0], "build")
                self.assertEqual(Path(calls[-1]["args"][1]).stem, "Hexalith.EventStore.Client.Tests")
                methods = [call["args"][2] for call in calls if "-method" in call.get("args", [])]
                self.assertEqual(len(methods), 34)
                self.assertIn("Hexalith.Parties.Client.Tests.HttpPartiesIdentityClientTests."
                              "DelayedReply_ValidatesCompletionTime", methods)
                self.assertFalse(any(method.startswith("Hexalith.EventStore.") for method in methods))
                self.assertFalse(any(call.get("stage") == "receipt" for call in calls))

    def test_eventstore_dirty_source_is_retained_in_provenance(self):
        result, calls = self.invoke("eventstore-dirty-source")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        evidence = json.loads(self.receipt.read_text())
        self.assertEqual(evidence["sources"]["eventstore"],
                         dict(commit="c" * 40, dirty=True, status=[" M synthetic-test-change.cs"],
                              fingerprint={"trackedDiffSha256": hashlib.sha256(b"").hexdigest(), "untracked": []}))
        source_calls = [call for call in calls if call.get("cwd", "").endswith("Hexalith.EventStore")]
        self.assertEqual([call["args"] for call in source_calls],
                         list(GIT_SOURCE_COMMANDS) * 2)

    def test_invalid_eventstore_revision_refuses_before_build(self):
        result, calls = self.invoke("eventstore-invalid-revision")
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("could not resolve exact eventstore source revision", result.stderr)
        self.assertFalse(self.receipt.exists())
        self.assertEqual(calls[-1]["args"], ["rev-parse", "HEAD"])
        self.assertTrue(calls[-1]["cwd"].endswith("Hexalith.EventStore"))
        self.assertFalse(any(call.get("args", [None])[0] in ("build", "msbuild") for call in calls))

    def test_dirty_fingerprint_binds_tracked_and_untracked_bytes_and_symlink_text(self):
        result, _ = self.invoke("dirty-fingerprint")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        original = json.loads(self.receipt.read_text())["sources"]["agents"]
        self.assertEqual(original["fingerprint"]["trackedDiffSha256"],
                         hashlib.sha256((self.root / ".tracked.patch").read_bytes()).hexdigest())
        self.assertEqual(original["fingerprint"]["untracked"], [
            {"path": "untracked data.bin", "kind": "file", "sha256": hashlib.sha256(b"untracked content \0\xfe").hexdigest()},
            {"path": "untracked link", "kind": "symlink", "sha256": hashlib.sha256(b"original destination").hexdigest()}])
        (self.root / ".tracked.patch").write_bytes(b"different tracked bytes \xff")
        (self.root / "untracked data.bin").write_bytes(b"different untracked bytes")
        (self.root / "untracked link").unlink()
        (self.root / "untracked link").symlink_to("different destination")
        self.calls.unlink()
        result, _ = self.invoke("dirty-fingerprint")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        changed = json.loads(self.receipt.read_text())["sources"]["agents"]
        self.assertEqual(changed["status"], original["status"])
        self.assertEqual(changed["commit"], original["commit"])
        self.assertNotEqual(changed["fingerprint"]["trackedDiffSha256"], original["fingerprint"]["trackedDiffSha256"])
        for before, after in zip(original["fingerprint"]["untracked"], changed["fingerprint"]["untracked"]):
            self.assertEqual(before["path"], after["path"])
            self.assertNotEqual(before["sha256"], after["sha256"])
        self.assertNotIn("tracked diff with binary bytes", result.stdout)
        self.assertNotIn("different untracked bytes", result.stdout)

    def test_late_tracked_source_drift_in_each_consumed_root_refuses_success(self):
        for name in SOURCE_REVISIONS:
            with self.subTest(source=name):
                self.calls.unlink(missing_ok=True)
                self.environment["IDENTITY_DRIFT_SOURCE"] = name
                result, calls = self.invoke("late-tracked")
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertFalse(self.receipt.exists())
                self.assertIn("source changed during verification: " + name, result.stderr)
                self.assertIn({"stage": "receipt"}, calls)
                self.assertEqual(len([call for call in calls if "-method" in call.get("args", [])]), 48)

    def test_late_revision_untracked_content_and_symlink_drift_refuse_success(self):
        for scenario in ("late-revision", "late-untracked", "late-symlink"):
            with self.subTest(scenario=scenario):
                self.calls.unlink(missing_ok=True)
                result, calls = self.invoke(scenario)
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertFalse(self.receipt.exists())
                self.assertIn("source changed during verification: agents", result.stderr)
                self.assertIn({"stage": "receipt"}, calls)

    def test_changed_runner_binary_refuses_before_later_work(self):
        result, calls = self.invoke("changed-assembly")
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertFalse(self.receipt.exists())
        self.assertIn("built test assembly changed during verification: Hexalith.EventStore.Client.Tests", result.stderr)
        self.assertEqual(calls[-1]["args"][2],
                         "Hexalith.EventStore.Client.Tests.Streams.RetainedIdentityHistoryReaderTests."
                         "CompletedResponseCleanupFailureStillDisposesOwnedRequest")
        self.assertFalse(any(call.get("stage") == "receipt" for call in calls))

    def test_assembly_changed_after_method_collection_refuses_success(self):
        result, calls = self.invoke("late-assembly")
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertFalse(self.receipt.exists())
        self.assertIn("built test assembly changed during verification: Hexalith.Parties.Tests", result.stderr)
        self.assertIn({"stage": "receipt"}, calls)

    def test_full_route_suffix_change_cannot_hide_behind_native_display_abbreviation(self):
        self.route_source.write_text(self.route_source.read_text().replace("/method/Untrusted", "/method/UntrustedDifferent"))
        result, calls = self.invoke()
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertFalse(self.receipt.exists())
        self.assertIn("complete route argument inventory changed", result.stderr)
        self.assertFalse(any(call.get("args", [None])[0] == "build" for call in calls))

    def test_route_arguments_on_another_method_do_not_satisfy_exact_source_inventory(self):
        self.route_source.write_text(self.route_source.read_text().replace(
            "SidecarDelivery_MissingChannel_DeniesBeforeDispatch(string route)", "OtherMethod(string route)"))
        result, calls = self.invoke()
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertFalse(self.receipt.exists())
        self.assertIn("complete route argument inventory changed", result.stderr)
        self.assertFalse(any(call.get("args", [None])[0] == "build" for call in calls))

    def test_embedded_route_whitespace_cannot_hide_behind_native_display_abbreviation(self):
        self.route_source.write_text(self.route_source.read_text().replace("/method/Untrusted", "/method/Untrus ted"))
        result, calls = self.invoke()
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertFalse(self.receipt.exists())
        self.assertIn("complete route argument inventory changed", result.stderr)
        self.assertFalse(any(call.get("args", [None])[0] == "build" for call in calls))

    def test_route_literal_decoding_preserves_values_and_allows_only_external_whitespace(self):
        self.route_source.write_text(self.route_source.read_text().replace(
            '[InlineData("/actors/PartyProjectionActor/party-1/method/Untrusted")]',
            '[ InlineData ( "/actors/PartyProjectionActor/party-1/method/Untrus\\u0074ed" ) ]'))
        result, _ = self.invoke()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        receipt = json.loads(self.receipt.read_text())
        inventory = next(iter(receipt["sourceArgumentInventory"].values()))
        self.assertEqual(inventory["arguments"], [
            {"route": "/actors/PartyProjectionActor/party-1/method/Untrusted"}, {"route": "/tenants/events"}])

    def test_source_overlapping_artifacts_refuse_before_calls_or_existing_output_changes(self):
        for artifacts in (self.root, self.root / "local-evidence", self.root.parent,
                          self.root / "references/Hexalith.Parties/local-evidence"):
            with self.subTest(artifacts=artifacts):
                artifacts.mkdir(parents=True, exist_ok=True)
                receipt = artifacts / "identity-verification.json"
                receipt.write_bytes(b"existing user evidence")
                result = subprocess.run([sys.executable, str(self.script), "--artifacts", str(artifacts)],
                                        cwd=self.caller, env=self.environment, capture_output=True,
                                        text=True, timeout=10)
                self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn("choose an output directory outside every source root", result.stderr)
                self.assertFalse(self.calls.exists())
                self.assertEqual(receipt.read_bytes(), b"existing user evidence")
                self.assertFalse((artifacts / ".identity-verification.lock").exists())

    def test_source_overlapping_artifacts_do_not_create_missing_directory(self):
        artifacts = self.root / "missing-source-output"
        result = subprocess.run([sys.executable, str(self.script), "--artifacts", str(artifacts)],
                                cwd=self.caller, env=self.environment, capture_output=True, text=True, timeout=10)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("choose an output directory outside every source root", result.stderr)
        self.assertFalse(self.calls.exists())
        self.assertFalse(artifacts.exists())

    def test_symlinked_artifacts_cannot_bypass_source_overlap_refusal(self):
        artifacts = self.caller / "linked-source-output"
        artifacts.symlink_to(self.root, target_is_directory=True)
        result = subprocess.run([sys.executable, str(self.script), "--artifacts", str(artifacts)],
                                cwd=self.caller, env=self.environment, capture_output=True, text=True, timeout=10)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("choose an output directory outside every source root", result.stderr)
        self.assertFalse(self.calls.exists())
        self.assertFalse((self.root / ".identity-verification.lock").exists())

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
