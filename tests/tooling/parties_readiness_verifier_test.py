"""Run pinned Parties readiness scripts against isolated synthetic localhost evidence."""

from __future__ import annotations

import base64
import datetime as dt
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest

from parties_readiness_fixtures import (
    ACTOR, COMMAND, MIB, POLICY, TARGET_SHA, binding_fixture, clean_environment,
    event, exact_instant, instant, owner_server, replace_with_rebind, stamp, tick_dates,
)


ROOT = Path(__file__).resolve().parents[2]
PARTIES = ROOT / "references/Hexalith.Parties"
READINESS = PARTIES / "eng/verify-ext-parties-history-readiness.ps1"
WRAPPER = PARTIES / "eng/verify-ext-parties-1.ps1"
PWSH = shutil.which("pwsh")


def quote_ps(value):
    return "'" + str(value).replace("'", "''") + "'"


@unittest.skipUnless(PWSH, "Requires PowerShell")
class PartiesReadinessVerifierTests(unittest.TestCase):
    """Fixtures exercise script behavior only, never dependency acceptance or Live qualification."""

    def setUp(self):
        if not READINESS.is_file() or not WRAPPER.is_file():
            self.skipTest("Requires initialized root-declared Hexalith.Parties submodule")
        self.temporary = tempfile.TemporaryDirectory(prefix="parties-readiness-")
        self.addCleanup(self.temporary.cleanup)
        self.caller = Path(self.temporary.name)
        self.source, self.history, self.opening, self.revoke = binding_fixture()
        self.receipt = dict(
            recordId="EXT-PARTIES-1", targetVersionOrCommit=TARGET_SHA,
            compatibilityCommand=COMMAND, outcome="Pass", evidenceLevel=4,
            prerequisitesAvailable=True, policyId=POLICY, retention="365.00:00:00",
            expiryTrigger="binding-effective-at",
        )
        self.register = self.caller / "register.md"
        self.register.write_text(
            "### EXT-PARTIES-1\n\n| Field | Value |\n|---|---|\n"
            "| `AcceptedStatus` | Available |\n"
            f"| `TargetVersionOrCommit` | {TARGET_SHA} |\n"
            "| `TargetIntegrationDate` | 2026-10-07 |\n"
            f"| `CompatibilityContractAndVerificationCommand` | Command: `{COMMAND}`. |\n"
        )

    @property
    def success_path(self):
        return self.caller / "relative-evidence/live-readiness.json"

    def run_probe(self, *, mode="normal", wrapper=False, environment=None, redirect_target=None):
        receipt_path = self.caller / "receipt.json"
        receipt_path.write_text(json.dumps(self.receipt))
        with owner_server(self.source, self.history, mode) as server:
            server.redirect_target = redirect_target
            env = clean_environment()
            env.update(
                EXT_PARTIES_GATEWAY_URL=server.url,
                EXT_PARTIES_READER_HEADERS_JSON='{"Authorization":"Bearer synthetic-only"}',
                EXT_PARTIES_TARGET_SHA=TARGET_SHA,
                EXT_PARTIES_COMPATIBILITY_RECEIPT=str(receipt_path),
                EXT_PARTIES_TENANT_A="synthetic-tenant", EXT_PARTIES_HISTORY_PARTY_ID="synthetic-party",
                EXT_PARTIES_HISTORY_ACTOR_ID=ACTOR, EXT_PARTIES_HISTORY_ACTION_AT=self.history["actionAt"],
                EXT_PARTIES_HISTORY_BINDING_VERSION="1", EXT_PARTIES_POLICY_ID=POLICY,
                EXT_PARTIES_RETENTION="365.00:00:00", EXT_PARTIES_EXPIRY_TRIGGER="binding-effective-at",
                NO_COLOR="1", TERM="dumb",
            )
            env.update(environment or {})
            arguments = [str(WRAPPER), "-Mode", "LiveReadiness"] if wrapper else [str(READINESS)]
            result = subprocess.run(
                [PWSH, "-NoProfile", "-NonInteractive", "-File", *arguments,
                 "-EvidenceDirectory", "relative-evidence", "-DependencyRegisterPath", "register.md"],
                cwd=self.caller, env=env, capture_output=True, text=True, timeout=40,
            )
            if mode == "streamed-overflow":
                self.assertTrue(server.body_finished.wait(5), "Stream sender failed to terminate")
                self.assertTrue(server.disconnected, result.stdout + result.stderr)
                self.assertLess(server.sent_bytes, 40 * MIB)
            return result, server.requests

    def assert_refusal(self, diagnostic, calls, **options):
        result, requests = self.run_probe(**options)
        output = result.stdout + result.stderr
        self.assertNotEqual(result.returncode, 0, output)
        # PowerShell wraps exception messages with a leading gutter on continuation lines.
        diagnostic_text = " ".join(line.strip().removeprefix("|").strip() for line in output.splitlines())
        self.assertIn(diagnostic, diagnostic_text)
        self.assertEqual(len(requests), calls, output)
        self.assertFalse(self.success_path.exists(), "Refusal wrote a success receipt")

    def assert_success(self, **options):
        result, requests = self.run_probe(**options)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        receipt = json.loads(self.success_path.read_text())
        self.assertEqual(receipt["qualification"], "ReadinessOnly")
        self.assertEqual(receipt["requestedTargetSha"], TARGET_SHA)
        self.assertEqual(receipt["sourceHead"], 4)
        self.assertEqual(receipt["bindingSourcePosition"], 2)
        self.assertIn("production-custody-destruction-and-copy-cleanup", receipt["incomplete"])
        self.assertIn("independent-erasure-certification", receipt["incomplete"])
        self.assertEqual([r["path"] for r in requests], [
            "/api/v1/identity-history/read", "/api/v1/queries", "/api/v1/queries",
        ])
        self.assertEqual(requests[0]["payload"], dict(
            identity=dict(tenantId="synthetic-tenant", domain="party", aggregateId="synthetic-party"),
            purpose="party-actor-history-v1"))
        historical = requests[1]["payload"]
        payload = dict(historical["payload"])
        self.assertEqual(exact_instant(payload.pop("actionAt")), exact_instant(self.history["actionAt"]))
        self.assertEqual(dict(historical, payload=payload), dict(
            tenant="synthetic-tenant", domain="party", aggregateId="synthetic-party",
            queryType="Hexalith.Parties.Contracts.Queries.ResolveHumanActorBindingAt",
            projectionType="party", entityId="synthetic-party",
            payload=dict(tenantId="synthetic-tenant", partyId="synthetic-party",
                         expectedActorId=ACTOR, expectedBindingVersion=1),
        ))
        self.assertEqual(requests[2]["payload"]["queryType"],
                         "Hexalith.Parties.Contracts.Queries.ResolvePartyIdentity")

    def test_matching_revoked_binding_passes_as_readiness_only(self):
        self.assert_success()

    def test_matching_rebound_binding_preserves_predecessor_interval(self):
        replace_with_rebind(self.source, self.opening, self.revoke)
        self.assert_success()

    def test_seven_digit_utc_ticks_preserve_exact_comparisons(self):
        self.source, self.history, self.opening, self.revoke = map(
            tick_dates, (self.source, self.history, self.opening, self.revoke))
        self.source["events"] = [event(2, "Established", self.opening), event(3, "Revoked", self.revoke)]
        self.assert_success()

    def test_wrapper_live_readiness_resolves_relative_register_and_evidence(self):
        self.assert_success(wrapper=True)

    def test_returned_expiry_must_match_original_custody(self):
        self.history["evidence"]["custody"]["expiresAt"] = stamp(
            instant(self.history["evidence"]["custody"]["expiresAt"]) + dt.timedelta(days=1))
        self.assert_refusal("returned custody lifecycle or expiry differs", 2)

    def test_returned_expiry_cannot_differ_by_one_100ns_tick(self):
        expiry = self.history["evidence"]["custody"]["expiresAt"]
        self.history["evidence"]["custody"]["expiresAt"] = expiry[:-1] + "1Z"
        self.assert_refusal("returned custody lifecycle or expiry differs", 2)

    def test_opening_custody_must_have_exact_365_day_duration(self):
        altered = stamp(instant(self.opening["effectiveAt"]) + dt.timedelta(days=366))
        self.opening["binding"]["evidence"]["custody"]["expiresAt"] = altered
        self.opening["binding"]["evidence"]["validUntil"] = altered
        self.history["evidence"]["custody"]["expiresAt"] = altered
        self.source["events"][0] = event(2, "Established", self.opening)
        self.assert_refusal("opening binding differs from its exact retention policy", 1)

    def test_revocation_custody_uses_revocation_effective_time(self):
        self.revoke["custody"]["expiresAt"] = self.opening["binding"]["evidence"]["custody"]["expiresAt"]
        self.source["events"][1] = event(3, "Revoked", self.revoke)
        self.assert_refusal("revocation differs from its exact retention policy", 1)

    def test_opening_interval_cannot_be_shortened_without_transition(self):
        self.opening["binding"]["evidence"]["validUntil"] = self.revoke["effectiveAt"]
        self.source["events"] = [event(2, "Established", self.opening)]
        self.source["excludedSequences"] = [1, 3, 4]
        self.assert_refusal("opening binding differs from its exact retention policy", 1)

    def test_unsupported_policy_fails_before_http(self):
        self.receipt["retention"] = "366.00:00:00"
        self.assert_refusal("matching explicit supported production retention policy", 0,
                            environment=dict(EXT_PARTIES_RETENTION="366.00:00:00"))

    def test_returned_interval_must_include_revocation_boundary(self):
        self.history["evidence"]["validUntil"] = self.opening["binding"]["evidence"]["validUntil"]
        self.assert_refusal("returned interval differs from the reconstructed retained transitions", 2)

    def test_returned_interval_start_must_match_retained_opening(self):
        self.history["evidence"]["validFrom"] = stamp(
            instant(self.history["evidence"]["validFrom"]) - dt.timedelta(microseconds=1))
        self.assert_refusal("returned interval differs from the reconstructed retained transitions", 2)

    def test_returned_interval_must_include_rebind_boundary(self):
        replace_with_rebind(self.source, self.opening, self.revoke)
        self.history["evidence"]["validUntil"] = self.opening["binding"]["evidence"]["validUntil"]
        self.assert_refusal("returned interval differs from the reconstructed retained transitions", 2)

    def test_later_transition_change_cannot_reuse_prior_query_interval(self):
        self.revoke["effectiveAt"] = stamp(instant(self.revoke["effectiveAt"]) - dt.timedelta(minutes=30))
        self.revoke["custody"]["expiresAt"] = stamp(instant(self.revoke["effectiveAt"]) + dt.timedelta(days=365))
        self.source["events"][1] = event(3, "Revoked", self.revoke)
        self.assert_refusal("returned interval differs from the reconstructed retained transitions", 2)

    def test_wrong_predecessor_version_fails_before_query(self):
        self.revoke["expectedBindingVersion"] = 2
        self.source["events"][1] = event(3, "Revoked", self.revoke)
        self.assert_refusal("inconsistent predecessor or effective instant", 1)

    def test_query_checkpoint_must_match_independent_source(self):
        self.history["sourcePosition"] = 3
        self.assert_refusal("exact retained action-time binding and original source position", 2)

    def test_declared_wire_overflow_is_refused_without_body_receive(self):
        self.assert_refusal("Owner response exceeds the source bound", 1, mode="declared-overflow")

    def test_response_at_exact_32_mib_wire_limit_is_accepted(self):
        self.assert_success(mode="at-wire-limit")

    def test_chunked_response_one_byte_above_wire_limit_is_refused(self):
        self.assert_refusal("Owner response exceeds the source bound", 1, mode="streamed-boundary-overflow")

    def test_chunked_wire_overflow_interrupts_receive(self):
        self.assert_refusal("Owner response exceeds the source bound", 1, mode="streamed-overflow")

    def test_decoded_payload_bound_is_independent_of_wire_bound(self):
        self.source["events"][0]["payload"] = base64.b64encode(b"x" * (16 * MIB + 1)).decode()
        self.assert_refusal("retained source exceeds the decoded payload bound", 1)

    def test_decoded_payload_limit_accumulates_multiple_events(self):
        for entry in self.source["events"]:
            decoded = base64.b64decode(entry["payload"])
            padded = decoded + b" " * (8 * MIB + 1 - len(decoded))
            entry["payload"] = base64.b64encode(padded).decode()
        self.assert_refusal("retained source exceeds the decoded payload bound", 1)

    def test_redirect_does_not_forward_owner_authorization(self):
        with owner_server(self.source, self.history) as destination:
            self.assert_refusal("HTTP 307", 1, mode="redirect",
                                redirect_target=destination.url + "/redirect-destination")
            self.assertEqual(destination.requests, [])

    def test_mismatched_exact_target_receipt_prevents_every_http_request(self):
        self.receipt["targetVersionOrCommit"] = "b" * 40
        self.assert_refusal("exact-target compatibility/prerequisite proof does not match", 0)

    def test_unavailable_dependency_prevents_every_http_request(self):
        self.register.write_text(self.register.read_text().replace("| Available |", "| Uncommitted |"))
        self.assert_refusal("requires Available, the exact accepted target/date/command", 0)

    def test_live_mode_without_inputs_cannot_claim_readiness(self):
        result = subprocess.run(
            [PWSH, "-NoProfile", "-NonInteractive", "-File", str(WRAPPER), "-Mode", "Live",
             "-EvidenceDirectory", "relative-evidence"], cwd=self.caller, env=clean_environment(),
            capture_output=True, text=True, timeout=20)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Missing live inputs", result.stdout + result.stderr)
        self.assertIn("Missing live inputs", (self.caller / "relative-evidence/live-gate.txt").read_text())
        self.assertFalse(self.success_path.exists())

    @unittest.skipUnless(os.name == "posix", "Requires POSIX executable interception")
    def test_wrapper_resolves_paths_from_process_working_directory(self):
        self.assert_wrapper_paths(False)

    @unittest.skipUnless(os.name == "posix", "Requires POSIX executable interception")
    def test_wrapper_resolves_paths_from_powershell_location(self):
        self.assert_wrapper_paths(True)

    def assert_wrapper_paths(self, change_location):
        commands = self.caller / "commands"
        commands.mkdir()
        capture = self.caller / "dotnet-calls.jsonl"
        fake = commands / "dotnet"
        fake.write_text(f"#!{sys.executable}\n" + r'''
import json, os, pathlib, sys
args = sys.argv[1:]
if args and pathlib.Path(args[0]).name == "pwsh.dll":
    os.execv(os.environ["READINESS_REAL_DOTNET"], [os.environ["READINESS_REAL_DOTNET"], *args])
with open(os.environ["READINESS_CALL_LOG"], "a") as log:
    log.write(json.dumps({"cwd": os.getcwd(), "args": args}) + "\n")
if args and args[0] == "msbuild" and "-getProperty:HexalithEventStoreVersion" in args:
    print("1.2.3")
    sys.exit(0)
print("Synthetic path probe stops before every real owner build")
sys.exit(19)
''')
        fake.chmod(0o755)
        environment = clean_environment()
        environment.update(PATH=str(commands) + os.pathsep + os.environ["PATH"],
                           READINESS_CALL_LOG=str(capture),
                           READINESS_REAL_DOTNET=shutil.which("dotnet") or "dotnet", NO_COLOR="1")
        eventstore = ROOT / "references/Hexalith.EventStore"
        platform = ROOT / "references/Hexalith.Platform"
        arguments = [str(WRAPPER), "-Mode", "Local", "-EvidenceDirectory", "relative-evidence",
                     "-MemoriesRoot", "relative-memories", "-ArtifactsDirectory", "relative-artifacts",
                     "-DependencyRegisterPath", "relative-register.md",
                     "-EventStoreRoot", os.path.relpath(eventstore, self.caller),
                     "-PlatformRoot", os.path.relpath(platform, self.caller)]
        if change_location:
            command = "Set-Location -LiteralPath " + quote_ps(self.caller) + "; & "
            command += " ".join(arg if arg.startswith("-") else quote_ps(arg) for arg in arguments)
            invocation = [PWSH, "-NoProfile", "-NonInteractive", "-Command", command]
            launch_directory = ROOT
        else:
            invocation = [PWSH, "-NoProfile", "-NonInteractive", "-File", *arguments]
            launch_directory = self.caller
        result = subprocess.run(invocation, cwd=launch_directory, env=environment,
                                capture_output=True, text=True, timeout=20)
        self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Build failed: Hexalith.EventStore.Contracts.Tests", result.stdout + result.stderr)
        calls = [json.loads(line) for line in capture.read_text().splitlines()]
        self.assertEqual(len(calls), 2, calls)
        self.assertEqual(calls[0]["args"][0], "msbuild")
        self.assertIn("-getProperty:HexalithEventStoreVersion", calls[0]["args"])
        self.assertEqual(Path(calls[0]["args"][1]),
                         eventstore / "src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj")
        self.assertEqual(calls[1]["args"][0], "build")
        self.assertEqual(Path(calls[1]["cwd"]), eventstore)
        self.assertEqual(Path(calls[1]["args"][1]), eventstore /
                         "tests/Hexalith.EventStore.Contracts.Tests/Hexalith.EventStore.Contracts.Tests.csproj")
        self.assertIn(str(self.caller / "relative-artifacts"), calls[1]["args"])
        self.assertIn("-p:HexalithMemoriesRoot=" + str(self.caller / "relative-memories"), calls[1]["args"])
        self.assertIn("-p:HexalithEventStoreRoot=" + str(eventstore), calls[1]["args"])
        self.assertIn("-p:HexalithEventStoreVersion=1.2.3", calls[1]["args"])
        evidence = self.caller / "relative-evidence"
        self.assertEqual((evidence / "eventstore-version.log").read_text().strip(), "1.2.3")
        self.assertIn("Synthetic path probe", (evidence / "Hexalith.EventStore.Contracts.Tests-build.log").read_text())
        self.assertFalse((evidence / "local-evidence.json").exists())
        self.assertFalse(self.success_path.exists())


def scope_case(source_binding, field, case_only=False):
    def test(self):
        value = self.history["evidence"][field].upper() if case_only else "synthetic-other"
        if source_binding:
            self.opening["binding"]["evidence"][field] = value
            self.source["events"][0] = event(2, "Established", self.opening)
            self.assert_refusal("opening binding is inconsistent or outside its scope", 1)
        else:
            self.history["evidence"][field] = value
            self.assert_refusal("returned binding scope or provenance differs from retained history", 2)
    return test


for _source in (False, True):
    for _field in ("tenantId", "partyId"):
        for _case_only in (False, True):
            setattr(PartiesReadinessVerifierTests,
                    f"test_{'opening' if _source else 'returned'}_nested_{_field}_must_match_scope"
                    + ("_case" if _case_only else ""), scope_case(_source, _field, _case_only))


if __name__ == "__main__":
    unittest.main()
