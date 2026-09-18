"""Fixture-backed tests for exact-main release source proof."""

from __future__ import annotations

import importlib.util
import sys
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


if __name__ == "__main__":
    unittest.main()
