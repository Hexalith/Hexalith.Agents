"""Executable classification tests for the side-effect-free semantic-release planner."""

from __future__ import annotations

import json
import os
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
PLANNER = ROOT / "eng" / "semantic-release-plan.mjs"


class SemanticReleasePlanTests(unittest.TestCase):
    """Prove release and no-release histories produce distinct plans."""

    def create_repository(
        self,
        next_commit: str,
        *,
        branch: str = "main",
        tag_format: str = "v${version}",
        analyzer_options: dict[str, object] | None = None,
        release_branches: list[str] | None = None,
    ) -> Path:
        """Create a release repository with one released tag and one candidate commit."""
        directory = Path(self.enterContext(tempfile.TemporaryDirectory()))
        self.run_git(directory, "init", f"--initial-branch={branch}")
        self.run_git(directory, "config", "user.name", "Release Planner Test")
        self.run_git(directory, "config", "user.email", "release-planner@example.invalid")
        configuration = json.loads((ROOT / ".releaserc.json").read_text(encoding="utf-8"))
        configuration["branches"] = release_branches or [branch]
        configuration["tagFormat"] = tag_format
        # Any publication hook accidentally admitted into planning must fail this fixture.
        for index, plugin in enumerate(configuration["plugins"]):
            name = plugin[0] if isinstance(plugin, list) else plugin
            if name == "@semantic-release/commit-analyzer" and analyzer_options is not None:
                configuration["plugins"][index] = [name, analyzer_options]
            elif name == "@semantic-release/exec":
                configuration["plugins"][index] = [name, {
                    "verifyConditionsCmd": "exit 90",
                    "verifyReleaseCmd": "exit 91",
                    "prepareCmd": "exit 92",
                    "publishCmd": "exit 93",
                }]
        (directory / ".releaserc.json").write_text(json.dumps(configuration), encoding="utf-8")
        (directory / "history.txt").write_text("released\n", encoding="utf-8")
        self.run_git(directory, "add", "history.txt", ".releaserc.json")
        self.run_git(directory, "commit", "-m", "chore: initial release")
        self.run_git(directory, "tag", tag_format.replace("${version}", "1.0.0"))
        if branch != "main":
            self.run_git(directory, "branch", "main")
        (directory / "history.txt").write_text("released\ncandidate\n", encoding="utf-8")
        self.run_git(directory, "add", "history.txt")
        self.run_git(directory, "commit", "-m", next_commit)
        return directory

    @staticmethod
    def run_git(directory: Path, *arguments: str) -> str:
        """Run one deterministic local Git command."""
        result = subprocess.run(
            ["git", *arguments],
            cwd=directory,
            check=True,
            capture_output=True,
            text=True,
            timeout=10,
        )
        return result.stdout.strip()

    def plan(self, repository: Path) -> dict[str, object]:
        """Run the real planner against the temporary repository."""
        environment = os.environ.copy()
        # These are local scratch histories, independent of the hosting CI branch.
        environment.pop("GITHUB_ACTIONS", None)
        result = subprocess.run(
            ["node", str(PLANNER)],
            cwd=repository,
            env=environment,
            check=True,
            capture_output=True,
            text=True,
            timeout=30,
        )
        return json.loads(result.stdout)

    def test_fix_commit_requires_patch_release(self) -> None:
        plan = self.plan(self.create_repository("fix: repair release boundary"))

        self.assertEqual(plan, {"release_required": True, "version": "1.0.1"})

    def test_docs_commit_requires_no_release(self) -> None:
        plan = self.plan(self.create_repository("docs: clarify release boundary"))

        self.assertEqual(plan, {"release_required": False, "version": None})

    def test_configured_tag_format_identifies_the_previous_release(self) -> None:
        plan = self.plan(self.create_repository("fix: repair boundary", tag_format="release-${version}"))

        self.assertEqual(plan, {"release_required": True, "version": "1.0.1"})

    def test_configured_release_branch_is_used(self) -> None:
        plan = self.plan(self.create_repository("fix: repair boundary", branch="release"))

        self.assertEqual(plan, {"release_required": True, "version": "1.0.1"})

    def test_configured_analyzer_options_determine_the_version(self) -> None:
        repository = self.create_repository(
            "docs: clarify release boundary",
            analyzer_options={"releaseRules": [{"type": "docs", "release": "patch"}]},
        )

        self.assertEqual(self.plan(repository), {"release_required": True, "version": "1.0.1"})

    def test_multi_branch_plan_preserves_source_refs(self) -> None:
        repository = self.create_repository(
            "fix: repair boundary", branch="release", release_branches=["main", "release"],
        )
        before = self.run_git(repository, "for-each-ref", "--format=%(refname) %(objectname)")

        self.assertEqual(self.plan(repository), {"release_required": True, "version": "1.0.1"})
        self.assertEqual(self.run_git(repository, "for-each-ref", "--format=%(refname) %(objectname)"), before)

    def test_top_level_publication_hooks_are_excluded(self) -> None:
        repository = self.create_repository("fix: repair boundary")
        configuration_path = repository / ".releaserc.json"
        configuration = json.loads(configuration_path.read_text(encoding="utf-8"))
        for hook in (
            "verifyConditions", "verifyRelease",
            "prepare", "publish", "addChannel", "success", "fail",
        ):
            configuration[hook] = [["@semantic-release/exec", {f"{hook}Cmd": "exit 94"}]]
        configuration_path.write_text(json.dumps(configuration), encoding="utf-8")

        self.assertEqual(self.plan(repository), {"release_required": True, "version": "1.0.1"})

    def test_reordered_plugins_preserve_analysis_options(self) -> None:
        repository = self.create_repository(
            "docs: clarify boundary",
            analyzer_options={"releaseRules": [{"type": "docs", "release": "patch"}]},
        )
        configuration_path = repository / ".releaserc.json"
        configuration = json.loads(configuration_path.read_text(encoding="utf-8"))
        configuration["plugins"].reverse()
        configuration_path.write_text(json.dumps(configuration), encoding="utf-8")

        self.assertEqual(self.plan(repository), {"release_required": True, "version": "1.0.1"})

    def test_unsupported_analysis_overrides_fail_without_a_plan(self) -> None:
        for key, value in (
            ("analyzeCommits", {"releaseRules": [{"type": "docs", "release": "minor"}]}),
            ("generateNotes", {"preset": "angular"}),
            ("extends", ["missing-publication-config-must-not-load"]),
        ):
            with self.subTest(key=key):
                repository = self.create_repository("docs: clarify boundary")
                configuration_path = repository / ".releaserc.json"
                configuration = json.loads(configuration_path.read_text(encoding="utf-8"))
                configuration[key] = value
                configuration_path.write_text(json.dumps(configuration), encoding="utf-8")
                before = self.run_git(repository, "for-each-ref", "--format=%(refname) %(objectname)")

                with self.assertRaises(subprocess.CalledProcessError) as raised:
                    self.plan(repository)

                self.assertEqual(raised.exception.stdout, "")
                self.assertIn(f"Release planning does not support top-level {key}", raised.exception.stderr)
                self.assertEqual(self.run_git(repository, "for-each-ref", "--format=%(refname) %(objectname)"), before)

    def test_unreviewed_plugins_and_exec_analysis_fail_without_a_plan(self) -> None:
        for plugin, diagnostic in (
            ("unreviewed-analysis-plugin", "does not support plugin unreviewed-analysis-plugin"),
            (["@semantic-release/exec", {"analyzeCommitsCmd": "echo minor"}], "does not support exec analysis hooks"),
            (["@semantic-release/exec", {"generateNotesCmd": "echo notes"}], "does not support exec analysis hooks"),
        ):
            with self.subTest(plugin=plugin):
                repository = self.create_repository("docs: clarify boundary")
                configuration_path = repository / ".releaserc.json"
                configuration = json.loads(configuration_path.read_text(encoding="utf-8"))
                configuration["plugins"].append(plugin)
                configuration_path.write_text(json.dumps(configuration), encoding="utf-8")

                with self.assertRaises(subprocess.CalledProcessError) as raised:
                    self.plan(repository)

                self.assertEqual(raised.exception.stdout, "")
                self.assertIn(diagnostic, raised.exception.stderr)


if __name__ == "__main__":
    unittest.main()
