"""Regression tests for tools/check-story-review-readiness.py."""

from __future__ import annotations

import importlib.util
import json
import os
import stat
import subprocess
import sys
import tempfile
import unittest
from datetime import datetime, timezone
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL = REPO_ROOT / "tools" / "check-story-review-readiness.py"


def load_tool():
    spec = importlib.util.spec_from_file_location("check_story_review_readiness", TOOL)
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


V = load_tool()
FIXED_NOW = datetime(2026, 7, 31, 10, 11, 12, tzinfo=timezone.utc)


def summary(project: str = "Demo.Tests", tests: int = 2) -> object:
    return V.TestSummary(project, tests, tests, 0, 0, 0, 0)


def generated_block() -> str:
    return V.render_test_evidence([summary()], FIXED_NOW)


def story_text(
    paths: tuple[str, ...] = ("_bmad-output/implementation-artifacts/story.md",),
    record_text: str = "No manually recorded totals.\n",
    block: str = "",
) -> str:
    listed = "".join(f"- `{path}`\n" for path in paths)
    managed = f"{block}\n\n" if block else ""
    return (
        "# Story 9.9: Fixture\n\n"
        "Status: review\n\n"
        "## Dev Agent Record\n\n"
        "### Debug Log References\n\n"
        f"{record_text}\n"
        f"{managed}"
        "### File List\n\n"
        f"{listed}\n"
        "### Change Log\n\n"
        "No count assertions here.\n"
    )


def ctrf_payload(
    tests: int = 2,
    passed: int = 2,
    failed: int = 0,
    skipped: int = 0,
    pending: int = 0,
    other: int = 0,
) -> dict[str, object]:
    return {
        "reportFormat": "CTRF",
        "results": {
            "summary": {
                "tests": tests,
                "passed": passed,
                "failed": failed,
                "skipped": skipped,
                "pending": pending,
                "other": other,
            },
            "tests": [{} for _ in range(tests)],
        },
    }


class FakeRunner:
    def __init__(
        self,
        root: Path,
        *,
        git_payload: bytes,
        build_returncode: int = 0,
        test_returncode: int = 0,
        report: dict[str, object] | str | None = None,
    ) -> None:
        self.root = root
        self.git_payload = git_payload
        self.build_returncode = build_returncode
        self.test_returncode = test_returncode
        self.report = ctrf_payload() if report is None else report
        self.commands: list[tuple[str, ...]] = []
        self.result_directories: list[Path] = []

    def __call__(self, command, cwd, environment=None):
        values = tuple(command)
        self.commands.append(values)
        if values[:2] == ("dotnet", "build"):
            return V.CommandResult(self.build_returncode, stderr=b"build failed" if self.build_returncode else b"")
        if values[:2] == ("dotnet", "msbuild"):
            project = self.root / values[2]
            target = project.parent / "bin" / "Release" / "fixture" / f"{project.stem}.dll"
            target.parent.mkdir(parents=True, exist_ok=True)
            target.touch()
            return V.CommandResult(0, stdout=f"{target}\n".encode())
        if values and values[0] == "dotnet" and "-ctrf" in values:
            result_path = Path(values[values.index("-ctrf") + 1])
            self.result_directories.append(result_path.parent)
            if self.report is not None:
                if isinstance(self.report, str):
                    result_path.write_text(self.report, encoding="utf-8")
                else:
                    result_path.write_text(json.dumps(self.report), encoding="utf-8")
            return V.CommandResult(self.test_returncode, stderr=b"runner failed" if self.test_returncode else b"")
        if values[:2] == ("git", "status"):
            return V.CommandResult(0, stdout=self.git_payload)
        raise AssertionError(f"Unexpected command: {values}")


class CommandRunnerTests(unittest.TestCase):
    def test_timeout_and_process_os_errors_are_actionable_validation_failures(self):
        cases = (
            subprocess.TimeoutExpired(["git", "status"], 30),
            PermissionError("blocked"),
        )
        for failure in cases:
            with self.subTest(failure=failure):
                with mock.patch.object(V.subprocess, "run", side_effect=failure):
                    with self.assertRaises(V.ValidationError):
                        V.default_runner(("git", "status"), REPO_ROOT)


class StoryParsingTests(unittest.TestCase):
    def test_generated_block_is_inserted_before_file_list(self):
        original = story_text()
        layout = V.parse_story_layout(original)
        updated = V.update_generated_block(original, layout, generated_block())

        self.assertEqual(updated.count(V.BEGIN_MARKER), 1)
        self.assertLess(updated.index(V.BEGIN_MARKER), updated.index("### File List"))
        self.assertIn("Run (UTC): 2026-07-31T10:11:12Z", updated)

    def test_existing_generated_block_is_replaced_not_duplicated(self):
        original = story_text(block=generated_block().replace("2026-07-31", "2026-07-30"))
        updated = V.update_generated_block(original, V.parse_story_layout(original), generated_block())

        self.assertEqual(updated.count(V.BEGIN_MARKER), 1)
        self.assertNotIn("2026-07-30", updated)

    def test_duplicate_or_orphaned_markers_fail(self):
        cases = (
            story_text(record_text=f"{V.BEGIN_MARKER}\n{V.BEGIN_MARKER}\n{V.END_MARKER}"),
            story_text(record_text=V.BEGIN_MARKER),
            story_text(record_text=f"{V.END_MARKER}\n{V.BEGIN_MARKER}"),
        )
        for text in cases:
            with self.subTest(text=text):
                with self.assertRaises(V.ValidationError):
                    V.parse_story_layout(text)

    def test_file_list_requires_unique_backticked_normal_paths(self):
        duplicate = story_text(paths=("src/a.cs", "src/a.cs"))
        plain = story_text().replace("- `_bmad-output/implementation-artifacts/story.md`", "- src/a.cs")
        parent = story_text(paths=("../src/a.cs",))

        for text in (duplicate, plain, parent):
            with self.subTest(text=text):
                layout = V.parse_story_layout(text)
                with self.assertRaises(V.ValidationError):
                    V.parse_file_list(text, layout)

    def test_file_list_accepts_annotations_spaces_and_unicode(self):
        text = story_text(paths=("src/a file.cs", "docs/évidence.md"))
        text = text.replace("`src/a file.cs`", "`src/a file.cs` (modified)")
        paths = V.parse_file_list(text, V.parse_story_layout(text))
        self.assertEqual(paths, {"src/a file.cs", "docs/évidence.md"})

    def test_file_list_preserves_significant_spaces_and_backslashes(self):
        text = story_text(paths=("src/ leading.cs", "src/trailing .cs", r"src/a\b.cs"))
        paths = V.parse_file_list(text, V.parse_story_layout(text))
        self.assertEqual(paths, {"src/ leading.cs", "src/trailing .cs", r"src/a\b.cs"})

    def test_file_list_bullet_rejects_multiple_backticked_paths(self):
        text = story_text().replace(
            "`_bmad-output/implementation-artifacts/story.md`",
            "`src/old.cs` -> `src/new.cs`",
        )
        with self.assertRaisesRegex(V.ValidationError, "exactly one"):
            V.parse_file_list(text, V.parse_story_layout(text))

    def test_unmanaged_count_shapes_are_reported(self):
        shapes = (
            "327 tests pass.",
            "Tests: 327.",
            "327 passed / 0 failed.",
            "Server **367** · UI **968**.",
        )
        for shape in shapes:
            with self.subTest(shape=shape):
                text = story_text(record_text=shape)
                failures = V.unmanaged_count_claims(text, V.parse_story_layout(text))
                self.assertEqual(len(failures), 1)
                self.assertIn(shape, failures[0][1])

    def test_versions_dates_file_list_and_generated_counts_are_ignored(self):
        text = story_text(
            paths=("test/net10.0/Story2.Tests.cs",),
            record_text="Run on 2026-07-31 with xUnit v3.2.2 for AC4.",
            block=generated_block(),
        )
        self.assertEqual(V.unmanaged_count_claims(text, V.parse_story_layout(text)), [])

    def test_fenced_numeric_count_is_still_unmanaged(self):
        text = story_text(record_text="```text\n327 tests pass\n```")
        self.assertEqual(len(V.unmanaged_count_claims(text, V.parse_story_layout(text))), 1)

    def test_markers_must_be_standalone_and_cannot_overlap_file_list(self):
        mid_line = story_text(record_text=f"prefix {V.BEGIN_MARKER}\n{V.END_MARKER}")
        wrapped_file_list = story_text().replace(
            "### File List\n",
            f"{V.BEGIN_MARKER}\n### File List\n",
        ).replace("### Change Log", f"{V.END_MARKER}\n\n### Change Log")
        for text in (mid_line, wrapped_file_list):
            with self.subTest(text=text):
                with self.assertRaises(V.ValidationError):
                    V.parse_story_layout(text)

    def test_marker_examples_inside_fences_do_not_control_replacement(self):
        text = story_text(record_text=f"```text\n{V.BEGIN_MARKER}\n{V.END_MARKER}\n```")
        layout = V.parse_story_layout(text)
        self.assertIsNone(layout.generated_start)

    def test_missing_or_duplicate_governed_sections_fail(self):
        missing = "# Story\n\n### File List\n"
        duplicate = story_text() + "\n## Dev Agent Record\n"
        for text in (missing, duplicate):
            with self.subTest(text=text):
                with self.assertRaises(V.ValidationError):
                    V.parse_story_layout(text)


class CtrfTests(unittest.TestCase):
    def parse(self, payload: object):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "result.json"
            if isinstance(payload, str):
                path.write_text(payload, encoding="utf-8")
            else:
                path.write_text(json.dumps(payload), encoding="utf-8")
            return V.parse_ctrf(path, "Demo.Tests")

    def test_valid_summary_is_parsed(self):
        parsed = self.parse(ctrf_payload())
        self.assertEqual(parsed.tests, 2)
        self.assertEqual(parsed.passed, 2)

    def test_malformed_json_schema_and_format_fail(self):
        cases = ("{", {}, {"reportFormat": "JUnit", "results": {"summary": {}}})
        for payload in cases:
            with self.subTest(payload=payload):
                with self.assertRaises(V.ValidationError):
                    self.parse(payload)

    def test_boolean_negative_and_arithmetic_values_fail(self):
        boolean = ctrf_payload()
        boolean["results"]["summary"]["tests"] = True
        negative = ctrf_payload()
        negative["results"]["summary"]["failed"] = -1
        arithmetic = ctrf_payload(tests=3, passed=2)
        for payload in (boolean, negative, arithmetic):
            with self.subTest(payload=payload):
                with self.assertRaises(V.ValidationError):
                    self.parse(payload)

    def test_test_array_length_must_match_summary(self):
        payload = ctrf_payload()
        payload["results"]["tests"] = [{}]
        with self.assertRaises(V.ValidationError):
            self.parse(payload)

    def test_test_array_is_required_and_every_test_must_pass(self):
        missing_array = ctrf_payload()
        del missing_array["results"]["tests"]
        cases = (
            missing_array,
            ctrf_payload(tests=0, passed=0),
            ctrf_payload(tests=2, passed=1, skipped=1),
            ctrf_payload(tests=2, passed=1, pending=1),
            ctrf_payload(tests=2, passed=1, other=1),
        )
        for payload in cases:
            with self.subTest(payload=payload):
                with self.assertRaises(V.ValidationError):
                    self.parse(payload)

    def test_render_orders_projects_and_reconciles_totals(self):
        rendered = V.render_test_evidence([summary("Zulu.Tests", 3), summary("Alpha.Tests", 2)], FIXED_NOW)
        self.assertLess(rendered.index("Alpha.Tests"), rendered.index("Zulu.Tests"))
        self.assertIn("| **Total** | 5 | 5 | 0 | 0 | 0 | 0 |", rendered)


class PorcelainTests(unittest.TestCase):
    def test_ordinary_untracked_spaces_unicode_and_submodule_paths(self):
        payload = " M src/a.cs\0?? docs/a file.md\0 M docs/é.md\0 M references/Hexalith.Memories\0".encode()
        self.assertEqual(
            V.parse_porcelain_v1_z(payload),
            {"src/a.cs", "docs/a file.md", "docs/é.md", "references/Hexalith.Memories"},
        )

    def test_rename_and_copy_include_both_endpoints(self):
        payload = b"R  src/new.cs\0src/old.cs\0 C src/copy.cs\0src/source.cs\0"
        self.assertEqual(
            V.parse_porcelain_v1_z(payload),
            {"src/new.cs", "src/old.cs", "src/copy.cs", "src/source.cs"},
        )

    def test_delete_conflict_and_path_with_tab_or_newline_are_preserved(self):
        payload = b" D src/deleted.cs\0UU src/conflict.cs\0?? docs/a\tb.md\0?? docs/a\nb.md\0"
        self.assertEqual(len(V.parse_porcelain_v1_z(payload)), 4)

    def test_significant_spaces_and_posix_backslashes_are_preserved(self):
        payload = b"??  leading.cs\0?? trailing .cs\0?? src/a\\b.cs\0"
        self.assertEqual(
            V.parse_porcelain_v1_z(payload),
            {" leading.cs", "trailing .cs", r"src/a\b.cs"},
        )

    def test_empty_status_is_an_empty_set(self):
        self.assertEqual(V.parse_porcelain_v1_z(b""), set())

    def test_truncated_malformed_status_and_invalid_utf8_fail(self):
        cases = (b" M src/a.cs", b"XX src/a.cs\0", b" M \xff\0", b"R  src/new.cs\0")
        for payload in cases:
            with self.subTest(payload=payload):
                with self.assertRaises(V.ValidationError):
                    V.parse_porcelain_v1_z(payload)

    def test_mismatch_reports_both_directions(self):
        message = V.mismatch_message({"src/listed.cs"}, {"src/changed.cs"})
        self.assertIn("Missing from File List", message)
        self.assertIn("+ src/changed.cs", message)
        self.assertIn("Not present in Git status", message)
        self.assertIn("- src/listed.cs", message)


class AtomicWriteTests(unittest.TestCase):
    def test_bom_crlf_mode_and_no_final_newline_are_preserved(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "story.md"
            path.write_bytes(b"\xef\xbb\xbfline one\r\nline two")
            os.chmod(path, 0o640)
            text, encoding, _ = V.read_story(path)
            V.atomic_write_story(path, text + "!", encoding)

            data = path.read_bytes()
            self.assertTrue(data.startswith(b"\xef\xbb\xbf"))
            self.assertIn(b"\r\n", data)
            self.assertFalse(data.endswith(b"\n"))
            self.assertEqual(stat.S_IMODE(path.stat().st_mode), 0o640)

    def test_replace_failure_preserves_original_and_removes_temporary_file(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "story.md"
            path.write_text("original", encoding="utf-8")
            encoding = V.StoryEncoding(False, "\n", 0o644)
            with mock.patch.object(V.os, "replace", side_effect=OSError("blocked")):
                with self.assertRaisesRegex(V.ValidationError, "atomically update"):
                    V.atomic_write_story(path, "updated", encoding)
            self.assertEqual(path.read_text(encoding="utf-8"), "original")
            self.assertEqual(list(Path(temporary).glob(".story.md.*")), [])

    def test_mixed_newlines_are_rejected_without_write(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "story.md"
            original = b"first\r\nsecond\n"
            path.write_bytes(original)
            with self.assertRaisesRegex(V.ValidationError, "mixes newline"):
                V.read_story(path)
            self.assertEqual(path.read_bytes(), original)

    def test_concurrent_change_blocks_atomic_replacement(self):
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "story.md"
            path.write_text("current", encoding="utf-8")
            encoding = V.StoryEncoding(False, "\n", 0o644)
            with self.assertRaisesRegex(V.ValidationError, "changed while"):
                V.atomic_write_story(path, "generated", encoding, expected_bytes=b"earlier")
            self.assertEqual(path.read_text(encoding="utf-8"), "current")


class GateOrchestrationTests(unittest.TestCase):
    def fixture(self, projects: int = 1):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        root = Path(temporary.name)
        story_path = root / "_bmad-output" / "implementation-artifacts" / "story.md"
        story_path.parent.mkdir(parents=True)
        story_path.write_text(story_text(), encoding="utf-8")
        for index in range(projects):
            project_dir = root / "test" / f"Demo{index}.Tests"
            project_dir.mkdir(parents=True)
            (project_dir / f"Demo{index}.Tests.csproj").write_text("<Project />", encoding="utf-8")
        solution_projects = "\n".join(
            f'  <Project Path="test/Demo{index}.Tests/Demo{index}.Tests.csproj" />'
            for index in range(projects)
        )
        (root / V.SOLUTION).write_text(
            f"<Solution>\n{solution_projects}\n</Solution>\n",
            encoding="utf-8",
        )
        package_props = root / "references" / "Hexalith.Builds" / "Props" / "Directory.Packages.props"
        package_props.parent.mkdir(parents=True)
        package_props.write_text("<Project />", encoding="utf-8")
        status = b" M _bmad-output/implementation-artifacts/story.md\0"
        return root, story_path, status

    def test_success_runs_every_project_updates_story_and_cleans_results(self):
        root, story_path, status = self.fixture(projects=5)
        runner = FakeRunner(root, git_payload=status)

        summaries, changed_count = V.execute_gate(root, story_path, runner, lambda: FIXED_NOW)

        self.assertEqual(len(summaries), 5)
        self.assertEqual(changed_count, 1)
        self.assertIn(V.BEGIN_MARKER, story_path.read_text(encoding="utf-8"))
        runner_calls = [command for command in runner.commands if "-ctrf" in command]
        self.assertEqual(len(runner_calls), 5)
        build_call = next(command for command in runner.commands if command[:2] == ("dotnet", "build"))
        self.assertTrue(any(value.startswith("-property:Hexalith4BuildPackageProps=") for value in build_call))
        self.assertTrue(all(not directory.exists() for directory in runner.result_directories))

    def test_build_failure_preserves_story_and_never_runs_tests(self):
        root, story_path, status = self.fixture()
        original = story_path.read_bytes()
        runner = FakeRunner(root, git_payload=status, build_returncode=1)

        with self.assertRaisesRegex(V.ValidationError, "Release build failed"):
            V.execute_gate(root, story_path, runner, lambda: FIXED_NOW)

        self.assertEqual(story_path.read_bytes(), original)
        self.assertFalse(any("-ctrf" in command for command in runner.commands))

    def test_runner_failure_missing_or_malformed_ctrf_preserves_story(self):
        cases = (
            {"test_returncode": 1},
            {"report": None, "test_returncode": 0, "missing": True},
            {"report": "{"},
        )
        for case in cases:
            with self.subTest(case=case):
                root, story_path, status = self.fixture()
                original = story_path.read_bytes()
                report = case.get("report", ctrf_payload())
                runner = FakeRunner(
                    root,
                    git_payload=status,
                    test_returncode=case.get("test_returncode", 0),
                    report=report,
                )
                if case.get("missing"):
                    runner.report = None
                with self.assertRaises(V.ValidationError):
                    V.execute_gate(root, story_path, runner, lambda: FIXED_NOW)
                self.assertEqual(story_path.read_bytes(), original)

    def test_failed_test_summary_preserves_story(self):
        root, story_path, status = self.fixture()
        original = story_path.read_bytes()
        runner = FakeRunner(root, git_payload=status, report=ctrf_payload(tests=2, passed=1, failed=1))
        with self.assertRaisesRegex(V.ValidationError, "not fully passing"):
            V.execute_gate(root, story_path, runner, lambda: FIXED_NOW)
        self.assertEqual(story_path.read_bytes(), original)

    def test_discovered_project_omitted_from_solution_fails_before_build(self):
        root, story_path, status = self.fixture(projects=2)
        (root / V.SOLUTION).write_text(
            '<Solution><Project Path="test/Demo0.Tests/Demo0.Tests.csproj" /></Solution>',
            encoding="utf-8",
        )
        original = story_path.read_bytes()
        runner = FakeRunner(root, git_payload=status)

        with self.assertRaisesRegex(V.ValidationError, "not included"):
            V.execute_gate(root, story_path, runner, lambda: FIXED_NOW)

        self.assertEqual(story_path.read_bytes(), original)
        self.assertEqual(runner.commands, [])

    def test_concurrent_story_edit_during_tests_is_not_overwritten(self):
        root, story_path, status = self.fixture()
        runner = FakeRunner(root, git_payload=status)

        def editing_runner(command, cwd, environment=None):
            result = runner(command, cwd, environment)
            if "-ctrf" in command:
                story_path.write_text("concurrent edit", encoding="utf-8")
            return result

        with self.assertRaisesRegex(V.ValidationError, "changed while"):
            V.execute_gate(root, story_path, editing_runner, lambda: FIXED_NOW)

        self.assertEqual(story_path.read_text(encoding="utf-8"), "concurrent edit")

    def test_unmanaged_claim_fails_before_build_and_preserves_story(self):
        root, story_path, status = self.fixture()
        story_path.write_text(story_text(record_text="327 tests pass."), encoding="utf-8")
        original = story_path.read_bytes()
        runner = FakeRunner(root, git_payload=status)

        with self.assertRaisesRegex(V.ValidationError, "Unmanaged numeric"):
            V.execute_gate(root, story_path, runner, lambda: FIXED_NOW)

        self.assertEqual(story_path.read_bytes(), original)
        self.assertEqual(runner.commands, [])

    def test_file_list_mismatch_leaves_fresh_block_but_never_edits_file_list(self):
        root, story_path, _ = self.fixture()
        runner = FakeRunner(root, git_payload=b" M src/missing.cs\0")
        original_file_list = "- `_bmad-output/implementation-artifacts/story.md`"

        with self.assertRaisesRegex(V.ValidationError, "Missing from File List") as raised:
            V.execute_gate(root, story_path, runner, lambda: FIXED_NOW)

        updated = story_path.read_text(encoding="utf-8")
        self.assertIn(V.BEGIN_MARKER, updated)
        self.assertIn(original_file_list, updated)
        self.assertIn("Not present in Git status", str(raised.exception))


if __name__ == "__main__":
    unittest.main()
