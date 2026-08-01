#!/usr/bin/env python3
"""Regenerate Dev Agent Record test evidence and verify its File List.

The command deliberately fails closed. It writes only the marker-delimited
test-evidence block, and only after a fresh Release build and every xUnit v3
CTRF result have been validated. File List mismatches are reported, never
repaired automatically.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import stat
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from collections.abc import Callable, Mapping, Sequence
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path


BEGIN_MARKER = "<!-- dev-agent-test-evidence:start -->"
END_MARKER = "<!-- dev-agent-test-evidence:end -->"
SOLUTION = "Hexalith.Agents.slnx"
TEST_PROJECT_PATTERN = "*Tests.csproj"
SUMMARY_FIELDS = ("tests", "passed", "failed", "skipped", "pending", "other")
PORCELAIN_STATUS_CHARS = frozenset(" MTADRCU?!")
HEADING_PATTERN = re.compile(r"^(#{1,6})[ \t]+(.+?)[ \t]*$")
FENCE_PATTERN = re.compile(r"^[ \t]*(`{3,}|~{3,})")
BACKTICK_PATH_PATTERN = re.compile(r"`([^`]+)`")
BULLET_PATTERN = re.compile(r"^[ \t]*[-*+][ \t]+(.+?)\s*$")
COUNT_PATTERNS = (
    re.compile(r"\b\d[\d,]*\s+(?:tests?|passed|failed|skipped|pending)\b", re.IGNORECASE),
    re.compile(
        r"\b(?:tests?|passed|failed|skipped|pending)"
        r"(?:\s+total)?\s*(?::|=|->|→)?\s*\d[\d,]*\b",
        re.IGNORECASE,
    ),
    re.compile(
        r"\btests?\b[^\n]*\b\d[\d,]*(?:\s*/\s*\d[\d,]*)+",
        re.IGNORECASE,
    ),
    re.compile(
        r"\b(?:[A-Za-z0-9.]+\.Tests|domain|server|contracts|client|ui)"
        r"\s*(?::|=|->|→)?\s*\d[\d,]*\b",
        re.IGNORECASE,
    ),
)


class ValidationError(Exception):
    """An expected readiness failure with user-actionable detail."""


@dataclass(frozen=True)
class CommandResult:
    returncode: int
    stdout: bytes = b""
    stderr: bytes = b""


@dataclass(frozen=True)
class TestSummary:
    project: str
    tests: int
    passed: int
    failed: int
    skipped: int
    pending: int
    other: int


@dataclass(frozen=True)
class Heading:
    level: int
    title: str
    start: int
    content_start: int


@dataclass(frozen=True)
class StoryLayout:
    record_start: int
    record_end: int
    file_list_start: int
    file_list_content_start: int
    file_list_end: int
    generated_start: int | None
    generated_end: int | None


@dataclass(frozen=True)
class StoryEncoding:
    bom: bool
    newline: str
    mode: int


Runner = Callable[[Sequence[str], Path, Mapping[str, str] | None], CommandResult]


def command_timeout_seconds(command: Sequence[str]) -> int:
    if tuple(command[:2]) == ("dotnet", "build"):
        return 900
    if tuple(command[:2]) == ("dotnet", "msbuild"):
        return 60
    if command and command[0] == "dotnet":
        return 900
    return 30


def default_runner(
    command: Sequence[str],
    cwd: Path,
    environment: Mapping[str, str] | None = None,
) -> CommandResult:
    env = os.environ.copy()
    if environment:
        env.update(environment)
    try:
        completed = subprocess.run(
            list(command),
            cwd=cwd,
            env=env,
            capture_output=True,
            check=False,
            timeout=command_timeout_seconds(command),
        )
    except subprocess.TimeoutExpired as exc:
        raise ValidationError(
            f"Command timed out after {command_timeout_seconds(command)} seconds: {' '.join(command)}"
        ) from exc
    except OSError as exc:
        raise ValidationError(f"Unable to run required command {command[0]}: {exc}") from exc
    return CommandResult(completed.returncode, completed.stdout, completed.stderr)


def command_detail(result: CommandResult) -> str:
    payload = result.stderr.strip() or result.stdout.strip()
    return payload.decode("utf-8", errors="replace") if payload else "no diagnostic output"


def parse_args(argv: Sequence[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Regenerate a story's Release test evidence and verify its File List against Git status.",
    )
    parser.add_argument("story_file", help="Repository-relative or absolute BMad story Markdown file.")
    return parser.parse_args(argv)


def resolve_repository_root(runner: Runner) -> Path:
    result = runner(("git", "rev-parse", "--show-toplevel"), Path.cwd(), None)
    if result.returncode != 0:
        raise ValidationError(f"Unable to resolve repository root: {command_detail(result)}")
    try:
        root = Path(result.stdout.decode("utf-8").strip()).resolve(strict=True)
    except (UnicodeDecodeError, FileNotFoundError) as exc:
        raise ValidationError("Git returned an invalid repository root") from exc
    return root


def resolve_story_path(root: Path, value: str) -> Path:
    candidate = Path(value)
    candidate = candidate if candidate.is_absolute() else root / candidate
    try:
        resolved = candidate.resolve(strict=True)
        resolved.relative_to(root)
    except (FileNotFoundError, ValueError) as exc:
        raise ValidationError(f"Story file must exist inside the repository: {value}") from exc
    if not resolved.is_file():
        raise ValidationError(f"Story path is not a file: {value}")
    return resolved


def read_story(path: Path) -> tuple[str, StoryEncoding, bytes]:
    try:
        raw = path.read_bytes()
        mode = stat.S_IMODE(path.stat().st_mode)
    except OSError as exc:
        raise ValidationError(f"Unable to read story {path}: {exc}") from exc
    bom = raw.startswith(b"\xef\xbb\xbf")
    payload = raw[3:] if bom else raw
    try:
        decoded = payload.decode("utf-8")
    except UnicodeDecodeError as exc:
        raise ValidationError(f"Story is not valid UTF-8: {path}") from exc
    has_crlf = b"\r\n" in payload
    without_crlf = payload.replace(b"\r\n", b"")
    has_lf = b"\n" in without_crlf
    has_cr = b"\r" in without_crlf
    if sum((has_crlf, has_lf, has_cr)) > 1:
        raise ValidationError(f"Story mixes newline conventions: {path}")
    newline = "\r\n" if has_crlf else "\r" if has_cr else "\n"
    normalized = decoded.replace("\r\n", "\n").replace("\r", "\n")
    return normalized, StoryEncoding(bom=bom, newline=newline, mode=mode), raw


def markdown_headings(text: str) -> list[Heading]:
    headings: list[Heading] = []
    offset = 0
    fence: str | None = None
    for line in text.splitlines(keepends=True):
        content = line.rstrip("\r\n")
        fence_match = FENCE_PATTERN.match(content)
        if fence_match:
            marker = fence_match.group(1)
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
            offset += len(line)
            continue
        if fence is None:
            match = HEADING_PATTERN.match(content)
            if match:
                headings.append(
                    Heading(
                        level=len(match.group(1)),
                        title=match.group(2).strip(),
                        start=offset,
                        content_start=offset + len(line),
                    )
                )
        offset += len(line)
    return headings


def _section_end(headings: Sequence[Heading], heading: Heading, text_length: int) -> int:
    for candidate in headings:
        if candidate.start > heading.start and candidate.level <= heading.level:
            return candidate.start
    return text_length


def marker_line_positions(text: str) -> tuple[list[tuple[int, int]], list[tuple[int, int]]]:
    positions: dict[str, list[tuple[int, int]]] = {BEGIN_MARKER: [], END_MARKER: []}
    offset = 0
    fence: str | None = None
    for line in text.splitlines(keepends=True):
        content = line.rstrip("\r\n")
        fence_match = FENCE_PATTERN.match(content)
        if fence_match:
            marker = fence_match.group(1)
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
            offset += len(line)
            continue
        if fence is None:
            for marker in (BEGIN_MARKER, END_MARKER):
                if marker in content and content != marker:
                    raise ValidationError("Generated test-evidence markers must be standalone lines")
                if content == marker:
                    positions[marker].append((offset, offset + len(line)))
        offset += len(line)
    return positions[BEGIN_MARKER], positions[END_MARKER]


def parse_story_layout(text: str) -> StoryLayout:
    headings = markdown_headings(text)
    records = [heading for heading in headings if heading.level == 2 and heading.title.casefold() == "dev agent record"]
    if len(records) != 1:
        raise ValidationError(f"Expected exactly one '## Dev Agent Record' section; found {len(records)}")
    record = records[0]
    record_end = _section_end(headings, record, len(text))
    file_lists = [
        heading
        for heading in headings
        if heading.level == 3
        and heading.title.casefold() == "file list"
        and record.content_start <= heading.start < record_end
    ]
    if len(file_lists) != 1:
        raise ValidationError(f"Expected exactly one '### File List' in Dev Agent Record; found {len(file_lists)}")
    file_list = file_lists[0]
    file_list_end = _section_end(headings, file_list, record_end)

    begins, ends = marker_line_positions(text)
    if len(begins) != len(ends) or len(begins) > 1:
        raise ValidationError("Generated test-evidence markers must be absent or form exactly one pair")
    generated_start: int | None = None
    generated_end: int | None = None
    if begins:
        begin_start, _ = begins[0]
        end_start, end_line_end = ends[0]
        if begin_start >= end_start:
            raise ValidationError("Generated test-evidence markers are reversed or nested")
        if not (record.content_start <= begin_start < end_start < record_end):
            raise ValidationError("Generated test-evidence block must be inside Dev Agent Record")
        if begin_start < file_list_end and end_line_end > file_list.start:
            raise ValidationError("Generated test-evidence block must not overlap the File List section")
        generated_start = begin_start
        generated_end = end_line_end

    return StoryLayout(
        record_start=record.content_start,
        record_end=record_end,
        file_list_start=file_list.start,
        file_list_content_start=file_list.content_start,
        file_list_end=file_list_end,
        generated_start=generated_start,
        generated_end=generated_end,
    )


def validate_repository_path(value: str, source: str = "File List") -> str:
    if not value or value.startswith("/") or re.match(r"^[A-Za-z]:[\\/]", value):
        raise ValidationError(f"{source} path must be repository-relative: {value!r}")
    parts = value.split("/")
    if any(part in ("", ".", "..") for part in parts):
        raise ValidationError(f"{source} path is not normalized: {value!r}")
    return value


def parse_file_list(text: str, layout: StoryLayout) -> set[str]:
    section = text[layout.file_list_content_start : layout.file_list_end]
    paths: set[str] = set()
    fence: str | None = None
    for raw_line in section.splitlines():
        fence_match = FENCE_PATTERN.match(raw_line)
        if fence_match:
            marker = fence_match.group(1)
            if fence is None:
                fence = marker
            elif marker[0] == fence[0] and len(marker) >= len(fence):
                fence = None
            continue
        if fence is not None:
            continue
        bullet = BULLET_PATTERN.match(raw_line)
        if not bullet:
            continue
        path_matches = BACKTICK_PATH_PATTERN.findall(bullet.group(1))
        if len(path_matches) != 1:
            raise ValidationError(
                f"File List bullet must contain exactly one backticked repository path: {raw_line.strip()}"
            )
        path = validate_repository_path(path_matches[0])
        if path in paths:
            raise ValidationError(f"Duplicate File List path: {path}")
        paths.add(path)
    if not paths:
        raise ValidationError("File List contains no repository paths")
    return paths


def unmanaged_count_claims(text: str, layout: StoryLayout) -> list[tuple[int, str]]:
    failures: list[tuple[int, str]] = []
    offset = 0
    for number, line in enumerate(text.splitlines(keepends=True), start=1):
        content = line.rstrip("\r\n")
        in_record = layout.record_start <= offset < layout.record_end
        in_generated = (
            layout.generated_start is not None
            and layout.generated_end is not None
            and layout.generated_start <= offset < layout.generated_end
        )
        in_file_list = layout.file_list_start <= offset < layout.file_list_end
        if in_record and not in_generated and not in_file_list:
            candidate = re.sub(r"[*_]", "", content)
            if any(pattern.search(candidate) for pattern in COUNT_PATTERNS):
                failures.append((number, content.strip()))
        offset += len(line)
    return failures


def discover_test_projects(root: Path) -> list[Path]:
    test_root = root / "test"
    projects = sorted(test_root.rglob(TEST_PROJECT_PATTERN)) if test_root.is_dir() else []
    if not projects:
        raise ValidationError("No root test projects matched test/**/*Tests.csproj")
    return projects


def validate_solution_membership(root: Path, projects: Sequence[Path]) -> None:
    solution_path = root / SOLUTION
    try:
        solution = ET.parse(solution_path)
    except (OSError, ET.ParseError) as exc:
        raise ValidationError(f"Unable to parse solution {SOLUTION}: {exc}") from exc
    solution_projects: set[str] = set()
    for element in solution.iter("Project"):
        value = element.get("Path")
        if value is None:
            raise ValidationError(f"Solution {SOLUTION} contains a Project without a Path")
        solution_projects.add(validate_repository_path(value, "Solution"))
    discovered = {project.relative_to(root).as_posix() for project in projects}
    omitted = sorted(discovered - solution_projects)
    if omitted:
        details = "\n".join(f"  - {path}" for path in omitted)
        raise ValidationError(
            f"Discovered test projects are not included in the freshly built {SOLUTION}:\n{details}"
        )


def shared_package_property_args(root: Path) -> tuple[str, ...]:
    """Point nested root submodules at this workspace's root-declared Builds checkout."""
    package_props = (root / "references" / "Hexalith.Builds" / "Props" / "Directory.Packages.props").resolve()
    if not package_props.is_file():
        raise ValidationError(
            "Required root submodule package props are missing: "
            "references/Hexalith.Builds/Props/Directory.Packages.props"
        )
    return tuple(
        f"-property:Hexalith{index}BuildPackageProps={package_props}"
        for index in range(1, 5)
    )


def _validated_non_negative_int(summary: Mapping[str, object], field: str, source: Path) -> int:
    value = summary.get(field)
    if isinstance(value, bool) or not isinstance(value, int) or value < 0:
        raise ValidationError(f"CTRF {source} has invalid non-negative integer '{field}'")
    return value


def parse_ctrf(path: Path, project: str) -> TestSummary:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise ValidationError(f"CTRF result is missing or malformed: {path}") from exc
    if not isinstance(payload, dict) or payload.get("reportFormat") != "CTRF":
        raise ValidationError(f"CTRF {path} has an invalid reportFormat")
    results = payload.get("results")
    summary_payload = results.get("summary") if isinstance(results, dict) else None
    if not isinstance(summary_payload, dict):
        raise ValidationError(f"CTRF {path} is missing results.summary")
    values = {field: _validated_non_negative_int(summary_payload, field, path) for field in SUMMARY_FIELDS}
    component_total = sum(values[field] for field in ("passed", "failed", "skipped", "pending", "other"))
    if values["tests"] != component_total:
        raise ValidationError(
            f"CTRF {path} summary arithmetic is inconsistent: tests={values['tests']}, components={component_total}"
        )
    tests_payload = results.get("tests") if isinstance(results, dict) else None
    if not isinstance(tests_payload, list):
        raise ValidationError(f"CTRF {path} is missing results.tests")
    if len(tests_payload) != values["tests"]:
        raise ValidationError(f"CTRF {path} test-array length does not match summary.tests")
    if values["tests"] == 0:
        raise ValidationError(f"CTRF {path} reported zero tests")
    if values["passed"] != values["tests"]:
        raise ValidationError(
            f"CTRF {path} is not fully passing: passed={values['passed']}, tests={values['tests']}, "
            f"failed={values['failed']}, skipped={values['skipped']}, "
            f"pending={values['pending']}, other={values['other']}"
        )
    return TestSummary(project=project, **values)


def collect_fresh_test_summaries(root: Path, runner: Runner) -> list[TestSummary]:
    projects = discover_test_projects(root)
    validate_solution_membership(root, projects)
    package_properties = shared_package_property_args(root)
    build = runner(
        (
            "dotnet",
            "build",
            SOLUTION,
            "--configuration",
            "Release",
            "-m:1",
            "--nologo",
            *package_properties,
        ),
        root,
        None,
    )
    if build.returncode != 0:
        raise ValidationError(f"Release build failed: {command_detail(build)}")

    summaries: list[TestSummary] = []
    with tempfile.TemporaryDirectory(prefix="agents-story-readiness-") as temporary:
        result_root = Path(temporary)
        for index, project in enumerate(projects):
            target_result = runner(
                (
                    "dotnet",
                    "msbuild",
                    str(project.relative_to(root)),
                    "-nologo",
                    "-property:Configuration=Release",
                    "-getProperty:TargetPath",
                    *package_properties,
                ),
                root,
                None,
            )
            if target_result.returncode != 0:
                raise ValidationError(f"Unable to resolve TargetPath for {project.relative_to(root)}: {command_detail(target_result)}")
            try:
                output_lines = [line.strip() for line in target_result.stdout.decode("utf-8").splitlines() if line.strip()]
            except UnicodeDecodeError as exc:
                raise ValidationError(f"MSBuild returned non-UTF-8 TargetPath for {project.relative_to(root)}") from exc
            if len(output_lines) != 1:
                raise ValidationError(f"MSBuild returned an ambiguous TargetPath for {project.relative_to(root)}")
            target = Path(output_lines[0])
            target = target if target.is_absolute() else (project.parent / target)
            target = target.resolve()
            if not target.is_file():
                raise ValidationError(f"Built test assembly does not exist for {project.relative_to(root)}: {target}")

            result_path = result_root / f"{index:02d}-{project.stem}.ctrf.json"
            execution = runner(
                (
                    "dotnet",
                    str(target),
                    "-automated",
                    "sync",
                    "-noLogo",
                    "-noColor",
                    "-ctrf",
                    str(result_path),
                ),
                root,
                {"DiffEngine_Disabled": "true"},
            )
            if execution.returncode != 0:
                raise ValidationError(f"Test runner failed for {project.stem}: {command_detail(execution)}")
            if not result_path.is_file():
                raise ValidationError(f"Test runner produced no CTRF result for {project.stem}")
            summary = parse_ctrf(result_path, project.stem)
            summaries.append(summary)
    return summaries


def render_test_evidence(summaries: Sequence[TestSummary], now_utc: datetime) -> str:
    if not summaries:
        raise ValidationError("Cannot render test evidence without project summaries")
    ordered = sorted(summaries, key=lambda item: item.project.casefold())
    timestamp = now_utc.astimezone(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")
    lines = [
        BEGIN_MARKER,
        "### Latest Release Test Evidence",
        "",
        f"Run (UTC): {timestamp}",
        "",
        "| Test project | Total | Passed | Failed | Skipped | Pending | Other |",
        "|---|---:|---:|---:|---:|---:|---:|",
    ]
    for item in ordered:
        lines.append(
            f"| {item.project} | {item.tests} | {item.passed} | {item.failed} | "
            f"{item.skipped} | {item.pending} | {item.other} |"
        )
    totals = {
        field: sum(getattr(item, field) for item in ordered)
        for field in SUMMARY_FIELDS
    }
    lines.extend(
        [
            f"| **Total** | {totals['tests']} | {totals['passed']} | {totals['failed']} | "
            f"{totals['skipped']} | {totals['pending']} | {totals['other']} |",
            "",
            "Result: PASS",
            END_MARKER,
        ]
    )
    return "\n".join(lines)


def update_generated_block(text: str, layout: StoryLayout, block: str) -> str:
    if layout.generated_start is not None and layout.generated_end is not None:
        suffix = text[layout.generated_end :]
        replacement = block + ("\n" if suffix and not suffix.startswith("\n") else "")
        return text[: layout.generated_start] + replacement + suffix
    prefix = text[: layout.file_list_start].rstrip("\n")
    suffix = text[layout.file_list_start :].lstrip("\n")
    return f"{prefix}\n\n{block}\n\n{suffix}"


def encode_story(text: str, encoding: StoryEncoding) -> bytes:
    payload = text.replace("\n", encoding.newline).encode("utf-8")
    return (b"\xef\xbb\xbf" if encoding.bom else b"") + payload


def atomic_write_story(
    path: Path,
    text: str,
    encoding: StoryEncoding,
    expected_bytes: bytes | None = None,
) -> None:
    data = encode_story(text, encoding)
    try:
        descriptor, temporary_name = tempfile.mkstemp(prefix=f".{path.name}.", dir=path.parent)
    except OSError as exc:
        raise ValidationError(f"Unable to create an atomic story update for {path}: {exc}") from exc
    temporary = Path(temporary_name)
    try:
        with os.fdopen(descriptor, "wb") as stream:
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, encoding.mode)
        if expected_bytes is not None:
            try:
                current_bytes = path.read_bytes()
            except OSError as exc:
                raise ValidationError(f"Unable to re-read story before update {path}: {exc}") from exc
            if current_bytes != expected_bytes:
                raise ValidationError(
                    "Story changed while Release tests were running; no generated evidence was written. "
                    "Re-run the readiness gate against the current story."
                )
        os.replace(temporary, path)
    except ValidationError:
        temporary.unlink(missing_ok=True)
        raise
    except OSError as exc:
        temporary.unlink(missing_ok=True)
        raise ValidationError(f"Unable to atomically update story {path}: {exc}") from exc


def parse_porcelain_v1_z(payload: bytes) -> set[str]:
    if not payload:
        return set()
    if not payload.endswith(b"\0"):
        raise ValidationError("Git porcelain output is truncated (missing NUL terminator)")
    records = payload.split(b"\0")
    paths: set[str] = set()
    index = 0
    while index < len(records) - 1:
        record = records[index]
        index += 1
        if len(record) < 4 or record[2:3] != b" ":
            raise ValidationError("Git porcelain output contains a malformed status record")
        try:
            status_text = record[:2].decode("ascii")
        except UnicodeDecodeError as exc:
            raise ValidationError("Git porcelain output contains a non-ASCII status") from exc
        if status_text == "  " or any(character not in PORCELAIN_STATUS_CHARS for character in status_text):
            raise ValidationError(f"Git porcelain output contains an invalid status: {status_text!r}")
        try:
            destination = record[3:].decode("utf-8")
        except UnicodeDecodeError as exc:
            raise ValidationError("Git path is not valid UTF-8") from exc
        paths.add(validate_repository_path(destination, "Git"))
        if "R" in status_text or "C" in status_text:
            if index >= len(records) - 1 or not records[index]:
                raise ValidationError("Git rename/copy record is missing its source path")
            try:
                source = records[index].decode("utf-8")
            except UnicodeDecodeError as exc:
                raise ValidationError("Git rename/copy source path is not valid UTF-8") from exc
            paths.add(validate_repository_path(source, "Git"))
            index += 1
    return paths


def git_status_paths(root: Path, runner: Runner) -> set[str]:
    result = runner(
        (
            "git",
            "status",
            "--porcelain=v1",
            "-z",
            "--untracked-files=all",
            "--ignore-submodules=none",
            "--renames",
        ),
        root,
        None,
    )
    if result.returncode != 0:
        raise ValidationError(f"Unable to read Git status: {command_detail(result)}")
    return parse_porcelain_v1_z(result.stdout)


def mismatch_message(file_list: set[str], changed: set[str]) -> str | None:
    missing = sorted(changed - file_list)
    extra = sorted(file_list - changed)
    if not missing and not extra:
        return None
    lines = ["File List does not match Git status."]
    if missing:
        lines.append("Missing from File List:")
        lines.extend(f"  + {path}" for path in missing)
    if extra:
        lines.append("Not present in Git status:")
        lines.extend(f"  - {path}" for path in extra)
    return "\n".join(lines)


def execute_gate(
    root: Path,
    story_path: Path,
    runner: Runner = default_runner,
    now_utc: Callable[[], datetime] = lambda: datetime.now(timezone.utc),
) -> tuple[list[TestSummary], int]:
    text, encoding, original_raw = read_story(story_path)
    layout = parse_story_layout(text)
    file_list = parse_file_list(text, layout)
    claims = unmanaged_count_claims(text, layout)
    if claims:
        details = "\n".join(f"  line {line}: {content}" for line, content in claims)
        raise ValidationError(
            "Unmanaged numeric test-count claims found in Dev Agent Record; "
            f"refer to the generated block instead:\n{details}"
        )

    summaries = collect_fresh_test_summaries(root, runner)
    block = render_test_evidence(summaries, now_utc())
    updated = update_generated_block(text, layout, block)
    atomic_write_story(story_path, updated, encoding, expected_bytes=original_raw)

    changed = git_status_paths(root, runner)
    mismatch = mismatch_message(file_list, changed)
    if mismatch:
        raise ValidationError(mismatch)
    return summaries, len(changed)


def main(argv: Sequence[str] | None = None) -> int:
    args = parse_args(sys.argv[1:] if argv is None else argv)
    try:
        root = resolve_repository_root(default_runner)
        story_path = resolve_story_path(root, args.story_file)
        summaries, path_count = execute_gate(root, story_path)
    except ValidationError as exc:
        print(f"FAIL: {exc}")
        return 1
    except OSError as exc:
        print(f"FAIL: Filesystem operation failed: {exc}")
        return 1
    total = sum(summary.tests for summary in summaries)
    passed = sum(summary.passed for summary in summaries)
    print(
        "PASS: Dev Agent Record review readiness "
        f"({len(summaries)} projects, {passed}/{total} passed, File List {path_count}/{path_count})."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
