"""Executable classification tests for the side-effect-free semantic-release planner."""

from __future__ import annotations

import json
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PLANNER = ROOT / "eng" / "semantic-release-plan.mjs"


class SemanticReleasePlanTests(unittest.TestCase):
    """Prove release and no-release histories produce distinct plans."""

    def create_repository(self, next_commit: str) -> Path:
        """Create a main repository with one released tag and one candidate commit."""
        directory = Path(self.enterContext(tempfile.TemporaryDirectory()))
        self.run_git(directory, "init", "--initial-branch=main")
        self.run_git(directory, "config", "user.name", "Release Planner Test")
        self.run_git(directory, "config", "user.email", "release-planner@example.invalid")
        (directory / "history.txt").write_text("released\n", encoding="utf-8")
        self.run_git(directory, "add", "history.txt")
        self.run_git(directory, "commit", "-m", "chore: initial release")
        self.run_git(directory, "tag", "v1.0.0")
        (directory / "history.txt").write_text("released\ncandidate\n", encoding="utf-8")
        self.run_git(directory, "add", "history.txt")
        self.run_git(directory, "commit", "-m", next_commit)
        return directory

    @staticmethod
    def run_git(directory: Path, *arguments: str) -> None:
        """Run one deterministic local Git command."""
        subprocess.run(
            ["git", *arguments],
            cwd=directory,
            check=True,
            capture_output=True,
            text=True,
        )

    def plan(self, repository: Path) -> dict[str, object]:
        """Run the real planner against the temporary repository."""
        result = subprocess.run(
            ["node", str(PLANNER)],
            cwd=repository,
            check=True,
            capture_output=True,
            text=True,
        )
        return json.loads(result.stdout)

    def test_fix_commit_requires_patch_release(self) -> None:
        plan = self.plan(self.create_repository("fix: repair release boundary"))

        self.assertEqual(plan, {"release_required": True, "version": "1.0.1"})

    def test_docs_commit_requires_no_release(self) -> None:
        plan = self.plan(self.create_repository("docs: clarify release boundary"))

        self.assertEqual(plan, {"release_required": False, "version": None})


if __name__ == "__main__":
    unittest.main()
