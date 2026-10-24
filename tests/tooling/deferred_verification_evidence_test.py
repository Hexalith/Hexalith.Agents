"""Exercise durable evidence validation through its read-only command line."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[2] / "scripts" / "validate-deferred-verification.py"


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def seal(report: dict) -> None:
    canonical = {key: report[key] for key in ("assemblySha256", "command", "summary", "cases")}
    report["canonicalSha256"] = digest(json.dumps(canonical, sort_keys=True, separators=(",", ":")).encode())


class DeferredVerificationEvidenceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.scratch = tempfile.TemporaryDirectory(prefix="deferred-evidence-test-")
        self.addCleanup(self.scratch.cleanup)
        self.root = Path(self.scratch.name)
        (self.root / "source.cs").write_text("source bytes", encoding="utf-8")
        self.evidence = self.root / "record.json"
        self.record = {
            "schemaVersion": 1,
            "localBehaviorVerified": True,
            "liveReadinessEstablished": False,
            "sourceSha256": {"source.cs": digest(b"source bytes")},
            "ownerReceipt": {
                "localBehaviorVerified": True,
                "liveReadinessEstablished": False,
                "ownerCases": {"Owner.Tests.Selected": 1},
            },
            "ownerNativeResults": [self.report("Owner.Tests.Selected", "owner")],
            "rootNativeResults": [self.report("Root.Tests.Selected", "root")],
            "commands": [{"command": "historical verification", "exitCode": 0}],
        }

    def report(self, method: str, label: str) -> dict:
        assembly = str(self.root / "removed-temporary-build" / (label + ".dll"))
        report = {
            "report": str(self.root / "removed-temporary-results" / (label + ".xml")),
            "sha256": digest(b"historical native XML"),
            "assembly": assembly,
            "assemblySha256": digest(b"historical assembly"),
            "command": ["dotnet", assembly, "-method", method, "-result-xml", "missing.xml"],
            "summary": {"total": 1, "passed": 1, "failed": 0, "errors": 0, "skipped": 0, "not-run": 0},
            "cases": [{"name": method, "result": "Pass"}],
        }
        seal(report)
        return report

    def run_validator(self, *, accepted: bool, contains: str = "") -> subprocess.CompletedProcess:
        self.evidence.write_text(json.dumps(self.record), encoding="utf-8")
        result = subprocess.run(
            [sys.executable, str(SCRIPT), str(self.evidence), "--source-root", str(self.root)],
            text=True, capture_output=True, check=False, timeout=15,
        )
        self.assertEqual(result.returncode, 0 if accepted else 1, result.stdout + result.stderr)
        self.assertIn(contains, result.stdout + result.stderr)
        return result

    def test_good_record_survives_removal_of_original_temporary_reports_and_binaries(self) -> None:
        result = self.run_validator(accepted=True, contains="1 owner cases, 1 root cases")
        self.assertIn("2 absent", result.stdout)
        self.assertIn("commands were not executed", result.stdout)

    def test_canonical_digest_detects_embedded_tampering(self) -> None:
        self.record["rootNativeResults"][0]["cases"][0]["name"] += "(changed)"
        self.run_validator(accepted=False, contains="canonicalSha256 mismatch")

    def test_source_drift_is_rejected(self) -> None:
        (self.root / "source.cs").write_text("changed source", encoding="utf-8")
        self.run_validator(accepted=False, contains="source hash mismatch")

    def test_duplicate_case_is_rejected_even_with_matching_summary_and_new_digest(self) -> None:
        report = self.record["ownerNativeResults"][0]
        report["cases"].append(dict(report["cases"][0]))
        report["summary"].update(total=2, passed=2)
        seal(report)
        self.run_validator(accepted=False, contains="duplicate case name")

    def test_failed_case_cannot_hide_behind_passing_summary(self) -> None:
        report = self.record["rootNativeResults"][0]
        report["cases"][0]["result"] = "Fail"
        seal(report)
        self.run_validator(accepted=False, contains="every case result must be Pass")

    def test_missing_case_is_rejected(self) -> None:
        report = self.record["ownerNativeResults"][0]
        report["cases"].clear()
        seal(report)
        self.run_validator(accepted=False, contains="embedded case count differs")

    def test_substituted_method_case_is_rejected(self) -> None:
        report = self.record["rootNativeResults"][0]
        report["cases"][0]["name"] = "Root.Tests.SelectedExtra"
        seal(report)
        self.run_validator(accepted=False, contains="case does not match selected method")

    def test_parameterized_exact_method_cases_are_accepted(self) -> None:
        report = self.record["ownerNativeResults"][0]
        report["cases"] = [{"name": "Owner.Tests.Selected(value: 1)", "result": "Pass"},
                           {"name": "Owner.Tests.Selected(value: 2)", "result": "Pass"}]
        report["summary"].update(total=2, passed=2)
        self.record["ownerReceipt"]["ownerCases"]["Owner.Tests.Selected"] = 2
        seal(report)
        self.run_validator(accepted=True, contains="2 owner cases")

    def test_receipt_inventory_requires_exact_method_and_count(self) -> None:
        for expected in ({"Owner.Tests.Unselected": 1}, {"Owner.Tests.Selected": 2},
                         {"Owner.Tests.Selected": 1, "Owner.Tests.Missing": 1}):
            with self.subTest(expected=expected):
                self.record["ownerReceipt"]["ownerCases"] = expected
                self.run_validator(accepted=False, contains="differs from exact native report inventory")

    def test_duplicate_method_reports_are_rejected(self) -> None:
        self.record["rootNativeResults"].append(dict(self.record["rootNativeResults"][0]))
        self.run_validator(accepted=False, contains="duplicate method report")

    def test_complete_owner_receipt_binary_inventory_is_accepted(self) -> None:
        report = self.record["ownerNativeResults"][0]
        self.record["ownerReceipt"]["assemblySha256"] = {"owner": report["assemblySha256"]}
        self.run_validator(accepted=True, contains="1 owner cases")

    def test_receipt_binary_hash_mismatch_is_rejected_after_native_report_is_resealed(self) -> None:
        report = self.record["ownerNativeResults"][0]
        self.record["ownerReceipt"]["assemblySha256"] = {"owner": report["assemblySha256"]}
        report["assemblySha256"] = digest(b"different historical assembly")
        seal(report)
        self.run_validator(accepted=False, contains="differs from native binary hash")

    def test_receipt_binary_inventory_requires_exact_project_coverage(self) -> None:
        report = self.record["ownerNativeResults"][0]
        for inventory, message in (({"extra": report["assemblySha256"]}, "omits tested project"),
                                   ({"owner": report["assemblySha256"], "extra": report["assemblySha256"]},
                                    "contains projects absent from native reports")):
            with self.subTest(inventory=inventory):
                self.record["ownerReceipt"]["assemblySha256"] = inventory
                self.run_validator(accepted=False, contains=message)

    def test_receipt_binary_inventory_rejects_invalid_projects_and_hashes(self) -> None:
        for inventory in ({}, {"owner": "invalid"}, {"../owner": digest(b"historical assembly")},
                          {"": digest(b"historical assembly")}, []):
            with self.subTest(inventory=inventory):
                self.record["ownerReceipt"]["assemblySha256"] = inventory
                self.run_validator(accepted=False, contains="must map project names")

    def test_available_tested_binary_must_match(self) -> None:
        report = self.record["rootNativeResults"][0]
        binary = Path(report["assembly"])
        binary.parent.mkdir(parents=True)
        binary.write_bytes(b"different assembly")
        self.run_validator(accepted=False, contains="tested assembly hash mismatch")
        binary.write_bytes(b"historical assembly")
        self.run_validator(accepted=True, contains="1 absent")

    def test_source_traversal_and_absolute_paths_are_rejected(self) -> None:
        for path in ("../source.cs", "sub/../../source.cs", "/tmp/source.cs", "..\\source.cs"):
            with self.subTest(path=path):
                self.record["sourceSha256"] = {path: digest(b"source bytes")}
                self.run_validator(accepted=False, contains="unsafe relative path")

    def test_source_symlink_cannot_escape_root(self) -> None:
        with tempfile.TemporaryDirectory(prefix="deferred-evidence-external-") as external:
            outside = Path(external) / "source.cs"
            outside.write_bytes(b"source bytes")
            (self.root / "link.cs").symlink_to(outside)
            self.record["sourceSha256"] = {"link.cs": digest(b"source bytes")}
            self.run_validator(accepted=False, contains="path escapes source root")

    def test_every_native_failure_counter_and_boolean_counter_is_rejected(self) -> None:
        for key in ("failed", "errors", "skipped", "not-run", "passed"):
            with self.subTest(key=key):
                report = self.record["rootNativeResults"][0]
                report["summary"] = {"total": 1, "passed": 1, "failed": 0, "errors": 0, "skipped": 0, "not-run": 0}
                report["summary"][key] = True if key == "passed" else 1
                seal(report)
                self.run_validator(accepted=False, contains="native summary" if key != "passed" else "nonnegative integers")

    def test_exit_code_must_be_an_actual_integer_zero(self) -> None:
        for code in (1, False, "0", None):
            with self.subTest(code=code):
                self.record["commands"][0]["exitCode"] = code
                self.run_validator(accepted=False, contains="exitCode must be zero")

    def test_local_only_flags_are_required_at_both_levels(self) -> None:
        for target, key, value in ((self.record, "localBehaviorVerified", False),
                                   (self.record, "liveReadinessEstablished", True),
                                   (self.record["ownerReceipt"], "liveReadinessEstablished", True)):
            with self.subTest(key=key):
                old = target[key]
                target[key] = value
                self.run_validator(accepted=False, contains=key)
                target[key] = old

    def test_embedded_commands_are_never_executed(self) -> None:
        marker = self.root / "must-not-exist"
        self.record["commands"] = [{"command": [sys.executable, "-c", f"open({str(marker)!r}, 'w').close()"], "exitCode": 0}]
        self.run_validator(accepted=True)
        self.assertFalse(marker.exists())

    def test_native_command_must_select_one_exact_method(self) -> None:
        report = self.record["rootNativeResults"][0]
        report["command"][2] = "-class"
        seal(report)
        self.run_validator(accepted=False, contains="command must select one exact")

    def test_schema_and_required_report_digest_are_checked(self) -> None:
        self.record["schemaVersion"] = True
        self.run_validator(accepted=False, contains="schemaVersion must be 1")
        self.record["schemaVersion"] = 1
        del self.record["rootNativeResults"][0]["assemblySha256"]
        self.run_validator(accepted=False, contains="invalid assemblySha256")


if __name__ == "__main__":
    unittest.main()
