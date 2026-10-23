"""Fixture-backed tests for exact-main release source proof."""

from __future__ import annotations

import importlib.util
import json
import os
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

    def run_live_cli(
        self, replies: list[tuple[int, str, str]], *, install_gh: bool = True
    ) -> tuple[subprocess.CompletedProcess[str], list[list[str]]]:
        """Exercise real gh subprocess invocation using a controlled executable on PATH."""
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)
            (path / "replies.json").write_text(json.dumps(replies), encoding="utf-8")
            if install_gh:
                gh = path / "gh"
                gh.write_text(
                    f"#!{sys.executable}\n"
                    "import json, sys\n"
                    "from pathlib import Path\n"
                    "root = Path(__file__).parent\n"
                    "log = root / 'calls.jsonl'\n"
                    "index = len(log.read_text().splitlines()) if log.exists() else 0\n"
                    "with log.open('a') as stream:\n"
                    "    stream.write(json.dumps(sys.argv[1:]) + '\\n')\n"
                    "code, output, diagnostic = json.loads((root / 'replies.json').read_text())[index]\n"
                    "sys.stdout.write(output)\n"
                    "sys.stderr.write(diagnostic)\n"
                    "sys.exit(code)\n",
                    encoding="utf-8",
                )
                gh.chmod(0o755)
            environment = os.environ.copy()
            # An absolute shebang lets this PATH exclude the installed gh entirely.
            environment["PATH"] = directory
            result = subprocess.run(
                [
                    sys.executable, str(SCRIPT),
                    "--repository", "Hexalith/Hexalith.Agents",
                    "--workflow", "ci.yml",
                    "--dispatch-ref", "refs/heads/main",
                    "--dispatch-sha", SHA,
                ],
                env=environment,
                capture_output=True,
                text=True,
                timeout=10,
            )
            calls = path / "calls.jsonl"
            return result, [json.loads(line) for line in calls.read_text().splitlines()] if calls.exists() else []

    def test_live_cli_uses_exact_main_and_filtered_ci_endpoints(self) -> None:
        result, calls = self.run_live_cli([
            (0, json.dumps(main_ref()), ""), (0, json.dumps(runs(run())), ""),
        ])

        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(result.stdout, f"Verified exact successful main push CI for {SHA}.\n")
        self.assertEqual(result.stderr, "")
        self.assertEqual(calls, [
            ["api", "repos/Hexalith/Hexalith.Agents/git/ref/heads/main"],
            ["api", "repos/Hexalith/Hexalith.Agents/actions/workflows/ci.yml/runs"
             f"?branch=main&event=push&status=completed&head_sha={SHA}&per_page=100"],
        ])

    def test_live_cli_rejects_stale_and_failed_ci_responses(self) -> None:
        for main_response, ci_response, diagnostic in (
            (main_ref(OTHER_SHA), runs(run()), "stale"),
            (main_ref(), runs(run("failure")), "no successful push CI run"),
        ):
            with self.subTest(diagnostic=diagnostic):
                result, calls = self.run_live_cli([
                    (0, json.dumps(main_response), ""), (0, json.dumps(ci_response), ""),
                ])
                self.assert_cli_failure(result, diagnostic)
                self.assertEqual(len(calls), 2)

    def test_live_cli_reports_gh_failures_at_either_endpoint(self) -> None:
        for failure_index in (0, 1):
            for stderr in ("permission denied\n", ""):
                with self.subTest(endpoint=failure_index, stderr=stderr):
                    replies = [(0, json.dumps(main_ref()), "")] * failure_index
                    replies.append((23, "", stderr))
                    result, calls = self.run_live_cli(replies)
                    diagnostic = "permission denied" if stderr else "exit code 23"
                    self.assert_cli_failure(result, f"GitHub API source proof failed: {diagnostic}")
                    self.assertEqual(len(calls), failure_index + 1)

    def test_live_cli_reports_malformed_gh_json_at_either_endpoint(self) -> None:
        for failure_index in (0, 1):
            with self.subTest(endpoint=failure_index):
                replies = [(0, json.dumps(main_ref()), "")] * failure_index
                replies.append((0, "not-json", ""))
                result, calls = self.run_live_cli(replies)
                self.assert_cli_failure(result, "GitHub API source proof returned malformed JSON")
                self.assertEqual(len(calls), failure_index + 1)

    def test_live_cli_reports_missing_gh(self) -> None:
        result, calls = self.run_live_cli([], install_gh=False)

        self.assert_cli_failure(result, "the gh CLI is required for live source proof")
        self.assertEqual(calls, [])


if __name__ == "__main__":
    unittest.main()
