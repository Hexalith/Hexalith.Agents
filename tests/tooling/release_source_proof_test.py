"""Fixture-backed tests for exact-main release source proof."""

from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "verify-release-source.py"
SPEC = importlib.util.spec_from_file_location("verify_release_source", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
VERIFIER = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = VERIFIER
SPEC.loader.exec_module(VERIFIER)

SHA = "1234567890abcdef1234567890abcdef12345678"
OTHER_SHA = "abcdef1234567890abcdef1234567890abcdef12"


def main_ref(sha: str = SHA) -> dict[str, object]:
    """Create a GitHub main-ref fixture."""
    return {"object": {"sha": sha}}


def run(conclusion: str = "success", sha: str = SHA) -> dict[str, object]:
    """Create one completed push CI run fixture."""
    return {
        "head_sha": sha,
        "head_branch": "main",
        "event": "push",
        "status": "completed",
        "conclusion": conclusion,
    }


def runs(*entries: dict[str, object]) -> dict[str, object]:
    """Create an unpaginated workflow-runs fixture."""
    return {"total_count": len(entries), "workflow_runs": list(entries)}


class ReleaseSourceProofTests(unittest.TestCase):
    """Cover stale, malformed, red, empty, and exact-success source states."""

    def test_exact_current_main_success_is_accepted(self) -> None:
        VERIFIER.verify_source_proof("refs/heads/main", SHA, main_ref(), runs(run()))

    def test_stale_main_is_rejected(self) -> None:
        with self.assertRaisesRegex(VERIFIER.SourceProofError, "stale"):
            VERIFIER.verify_source_proof("refs/heads/main", SHA, main_ref(OTHER_SHA), runs(run()))

    def test_non_main_dispatch_refs_at_current_main_sha_are_rejected(self) -> None:
        for dispatch_ref in ("refs/heads/feature", "refs/tags/v1.2.3", "refs/pull/1/merge"):
            with self.subTest(dispatch_ref=dispatch_ref), self.assertRaisesRegex(
                VERIFIER.SourceProofError, "must be dispatched from refs/heads/main"
            ):
                VERIFIER.verify_source_proof(dispatch_ref, SHA, main_ref(), runs(run()))

    def test_malformed_dispatch_sha_is_rejected(self) -> None:
        with self.assertRaisesRegex(VERIFIER.SourceProofError, "exact lowercase"):
            VERIFIER.verify_source_proof("refs/heads/main", "not-a-sha", main_ref(), runs(run()))

    def test_ci_for_the_wrong_sha_is_rejected(self) -> None:
        with self.assertRaisesRegex(VERIFIER.SourceProofError, "no successful push CI run"):
            VERIFIER.verify_source_proof(
                "refs/heads/main",
                SHA,
                main_ref(),
                runs(run(sha=OTHER_SHA)),
            )

    def test_red_and_empty_ci_are_rejected(self) -> None:
        for label, ci_runs in (("red", runs(run("failure"))), ("empty", runs())):
            with self.subTest(label=label), self.assertRaisesRegex(
                VERIFIER.SourceProofError,
                "no successful push CI run",
            ):
                VERIFIER.verify_source_proof("refs/heads/main", SHA, main_ref(), ci_runs)

    def run_cli(
        self,
        main_response: object,
        ci_response: object,
        dispatch_ref: str = "refs/heads/main",
        *,
        fixture_overrides: dict[str, str | None] | None = None,
    ) -> subprocess.CompletedProcess[str]:
        """Run the production entry point against file-backed GitHub responses."""
        with tempfile.TemporaryDirectory() as directory:
            main_path = Path(directory) / "main-ref.json"
            runs_path = Path(directory) / "runs.json"
            fixtures = {"main-ref.json": json.dumps(main_response), "runs.json": json.dumps(ci_response)}
            fixtures.update(fixture_overrides or {})
            for filename, contents in fixtures.items():
                if contents is not None:
                    (Path(directory) / filename).write_text(contents, encoding="utf-8")
            return subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT),
                    "--repository", "Hexalith/Hexalith.Agents",
                    "--workflow", "ci.yml",
                    "--dispatch-ref", dispatch_ref,
                    "--dispatch-sha", SHA,
                    "--main-ref", str(main_path),
                    "--runs", str(runs_path),
                ],
                capture_output=True,
                text=True,
                timeout=10,
            )

    def assert_cli_failure(self, result: subprocess.CompletedProcess[str], diagnostic: str) -> None:
        """Require a bounded verifier failure without a success message or traceback."""
        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertTrue(result.stderr.startswith("verify-release-source: "), result.stderr)
        self.assertIn(diagnostic, result.stderr)
        self.assertNotIn("Traceback", result.stderr)
        self.assertNotIn("Verified", result.stdout)

    def test_cli_accepts_exact_current_main_success(self) -> None:
        result = self.run_cli(main_ref(), runs(run()))
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(result.stdout, f"Verified exact successful main push CI for {SHA}.\n")
        self.assertEqual(result.stderr, "")

    def test_cli_rejects_invalid_source_proofs(self) -> None:
        cases = (
            ("non-main ref", main_ref(), runs(run()), "refs/heads/feature", "refs/heads/main"),
            ("stale source", main_ref(OTHER_SHA), runs(run()), "refs/heads/main", "stale"),
            ("failed CI", main_ref(), runs(run("failure")), "refs/heads/main", "no successful push CI run"),
            ("empty CI", main_ref(), runs(), "refs/heads/main", "no successful push CI run"),
        )
        for label, main_response, ci_response, dispatch_ref, diagnostic in cases:
            with self.subTest(label=label):
                result = self.run_cli(main_response, ci_response, dispatch_ref)
                self.assert_cli_failure(result, diagnostic)

    def test_cli_rejects_malformed_and_missing_json_fixtures(self) -> None:
        for filename, label in (("main-ref.json", "main ref"), ("runs.json", "CI runs")):
            for fault, contents in (("malformed", "not-json"), ("missing", None)):
                with self.subTest(fixture=filename, fault=fault):
                    result = self.run_cli(main_ref(), runs(run()), fixture_overrides={filename: contents})
                    self.assert_cli_failure(result, f"could not read {label} JSON")


if __name__ == "__main__":
    unittest.main()
