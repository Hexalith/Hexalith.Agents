#!/usr/bin/env python3
"""Prove a release candidate is the exact current main tip with successful push CI."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from pathlib import Path


SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$")
REPOSITORY_PATTERN = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")
WORKFLOW_PATTERN = re.compile(r"^[A-Za-z0-9_.-]+\.ya?ml$")


class SourceProofError(RuntimeError):
    """An exact-source release proof failure."""


def load_json(path: Path, label: str) -> object:
    """Load one fixture/API response with an actionable diagnostic."""
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise SourceProofError(f"could not read {label} JSON from {path}: {error}") from error


def gh_api(endpoint: str) -> object:
    """Read one authenticated GitHub API response through the installed CLI."""
    try:
        result = subprocess.run(
            ["gh", "api", endpoint],
            check=True,
            capture_output=True,
            text=True,
        )
        return json.loads(result.stdout)
    except FileNotFoundError as error:
        raise SourceProofError("the gh CLI is required for live source proof") from error
    except subprocess.CalledProcessError as error:
        diagnostic = error.stderr.strip() or f"exit code {error.returncode}"
        raise SourceProofError(f"GitHub API source proof failed: {diagnostic}") from error
    except json.JSONDecodeError as error:
        raise SourceProofError("GitHub API source proof returned malformed JSON") from error


def verify_source_proof(
    dispatch_ref: str,
    dispatch_sha: str,
    main_ref: object,
    runs: object,
) -> None:
    """Validate exact main identity and one exact successful push CI run."""
    if dispatch_ref != "refs/heads/main":
        raise SourceProofError("release must be dispatched from refs/heads/main")
    if not SHA_PATTERN.fullmatch(dispatch_sha):
        raise SourceProofError("dispatch source must be an exact lowercase 40-character commit SHA")

    main_object = main_ref.get("object") if isinstance(main_ref, dict) else None
    main_sha = main_object.get("sha") if isinstance(main_object, dict) else None
    if not isinstance(main_sha, str) or not SHA_PATTERN.fullmatch(main_sha):
        raise SourceProofError("live main response does not contain an exact lowercase commit SHA")
    if main_sha != dispatch_sha:
        raise SourceProofError("dispatched source is stale because it is not the current main tip")

    workflow_runs = runs.get("workflow_runs") if isinstance(runs, dict) else None
    total_count = runs.get("total_count") if isinstance(runs, dict) else None
    if not isinstance(workflow_runs, list) or type(total_count) is not int:
        raise SourceProofError("CI runs response is malformed")
    if total_count != len(workflow_runs):
        raise SourceProofError("CI runs response is ambiguous or paginated")

    exact_successes = [
        run
        for run in workflow_runs
        if isinstance(run, dict)
        and run.get("head_sha") == dispatch_sha
        and run.get("head_branch") == "main"
        and run.get("event") == "push"
        and run.get("status") == "completed"
        and run.get("conclusion") == "success"
    ]
    if not exact_successes:
        raise SourceProofError("no successful push CI run exists for the exact current main SHA")


def main() -> int:
    """Run fixture-backed or live exact-source verification."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repository", required=True)
    parser.add_argument("--workflow", required=True)
    parser.add_argument("--dispatch-ref", required=True)
    parser.add_argument("--dispatch-sha", required=True)
    parser.add_argument("--main-ref", type=Path)
    parser.add_argument("--runs", type=Path)
    args = parser.parse_args()

    if not REPOSITORY_PATTERN.fullmatch(args.repository):
        raise SourceProofError("repository must use owner/name syntax")
    if not WORKFLOW_PATTERN.fullmatch(args.workflow):
        raise SourceProofError("workflow must be a workflow YAML filename")
    if (args.main_ref is None) != (args.runs is None):
        raise SourceProofError("--main-ref and --runs fixtures must be supplied together")

    if args.main_ref is not None:
        main_ref = load_json(args.main_ref, "main ref")
        runs = load_json(args.runs, "CI runs")
    else:
        main_ref = gh_api(f"repos/{args.repository}/git/ref/heads/main")
        runs = gh_api(
            f"repos/{args.repository}/actions/workflows/{args.workflow}/runs"
            f"?branch=main&event=push&status=completed&head_sha={args.dispatch_sha}&per_page=100"
        )

    verify_source_proof(args.dispatch_ref, args.dispatch_sha, main_ref, runs)
    print(f"Verified exact successful main push CI for {args.dispatch_sha}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except SourceProofError as error:
        print(f"verify-release-source: {error}", file=sys.stderr)
        raise SystemExit(1)
