#!/usr/bin/env python3
"""Validate embedded local verification evidence without executing its commands.

Original XML reports and tested binaries may have lived in temporary directories.
The embedded report is protected by its canonical digest; an available binary is
also checked, while an absent binary is permitted for offline record validation.
These checks establish consistency and source agreement, not provenance or live
readiness, and a digest is not an independent attestation of test execution.
"""

from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sys


SUMMARY_KEYS = {"total", "passed", "failed", "errors", "skipped", "not-run"}
CANONICAL_KEYS = ("assemblySha256", "command", "summary", "cases")


def require(condition: bool, message: str) -> None:
    """Refuse a record that does not satisfy the evidence contract."""
    if not condition:
        raise ValueError(message)


def valid_digest(value: object) -> bool:
    """Check the complete hexadecimal SHA-256 representation."""
    return isinstance(value, str) and re.fullmatch(r"[0-9a-fA-F]{64}", value) is not None


def file_digest(path: Path) -> str:
    """Hash file bytes without loading an assembly or executing code."""
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def local_only(record: dict, label: str) -> None:
    """Require an explicit distinction between local evidence and live readiness."""
    require(record.get("localBehaviorVerified") is True, f"{label}: localBehaviorVerified must be true")
    require(record.get("liveReadinessEstablished") is False, f"{label}: liveReadinessEstablished must be false")


def source_path(root: Path, name: object) -> Path:
    """Keep source inventory paths relative to and contained within the root."""
    require(isinstance(name, str) and bool(name), "sourceSha256: path must be a nonempty string")
    path = PurePosixPath(name)
    require(not path.is_absolute() and ".." not in path.parts and "\\" not in name,
            f"sourceSha256: unsafe relative path {name!r}")
    resolved = (root / name).resolve()
    require(resolved.is_relative_to(root), f"sourceSha256: path escapes source root: {name!r}")
    require(resolved.is_file(), f"sourceSha256: missing source file {name!r}")
    return resolved


def validate_reports(reports: object, root: Path, label: str) -> tuple[dict[str, int], int]:
    """Check exact test selection, outcomes, embedded digests and available binaries."""
    require(isinstance(reports, list) and bool(reports), f"{label}: expected nonempty report list")
    inventory: dict[str, int] = {}
    missing_binaries = 0
    for index, report in enumerate(reports):
        at = f"{label}[{index}]"
        require(isinstance(report, dict), f"{at}: expected object")
        command = report.get("command")
        require(isinstance(command, list) and len(command) >= 4
                and all(isinstance(part, str) and part for part in command)
                and command[0] == "dotnet" and command[2] == "-method"
                and command.count("-method") == 1,
                f"{at}: command must select one exact dotnet -method")
        method = command[3]
        require(method not in inventory, f"{at}: duplicate method report {method!r}")
        require(report.get("assembly") == command[1], f"{at}: assembly differs from command")
        require(valid_digest(report.get("assemblySha256")), f"{at}: invalid assemblySha256")
        if "exitCode" in report:
            require(type(report["exitCode"]) is int and report["exitCode"] == 0,
                    f"{at}: exitCode must be zero")

        summary = report.get("summary")
        require(isinstance(summary, dict) and set(summary) == SUMMARY_KEYS,
                f"{at}: summary must contain the six native result counters")
        require(all(type(value) is int and value >= 0 for value in summary.values()),
                f"{at}: summary counters must be nonnegative integers")
        require(summary["total"] == summary["passed"] > 0
                and all(summary[key] == 0 for key in SUMMARY_KEYS - {"total", "passed"}),
                f"{at}: native summary must contain only passing cases")

        cases = report.get("cases")
        require(isinstance(cases, list) and len(cases) == summary["total"],
                f"{at}: embedded case count differs from summary")
        seen: set[str] = set()
        for case in cases:
            require(isinstance(case, dict), f"{at}: case must be an object")
            name = case.get("name")
            require(isinstance(name, str) and bool(name), f"{at}: case name must be nonempty")
            require(name == method or name.startswith(method + "("),
                    f"{at}: case does not match selected method: {name!r}")
            require(name not in seen, f"{at}: duplicate case name {name!r}")
            require(case.get("result") == "Pass", f"{at}: every case result must be Pass")
            seen.add(name)

        require(valid_digest(report.get("canonicalSha256")), f"{at}: invalid canonicalSha256")
        canonical = json.dumps({key: report[key] for key in CANONICAL_KEYS},
                               sort_keys=True, separators=(",", ":")).encode()
        require(hashlib.sha256(canonical).hexdigest() == report["canonicalSha256"].lower(),
                f"{at}: canonicalSha256 mismatch")

        assembly = Path(command[1])
        if not assembly.is_absolute():
            assembly = root / assembly
        if assembly.exists():
            require(assembly.is_file(), f"{at}: assembly path is not a file")
            require(file_digest(assembly) == report["assemblySha256"].lower(),
                    f"{at}: tested assembly hash mismatch")
        else:
            missing_binaries += 1
        inventory[method] = len(cases)
    return inventory, missing_binaries


def validate(record: object, root: Path) -> tuple[int, int, int]:
    """Validate a schema-one record and return owner/root cases and missing binaries."""
    require(isinstance(record, dict), "evidence must be an object")
    require(type(record.get("schemaVersion")) is int and record["schemaVersion"] == 1,
            "schemaVersion must be 1")
    local_only(record, "evidence")
    sources = record.get("sourceSha256")
    require(isinstance(sources, dict) and bool(sources), "sourceSha256 must be a nonempty object")
    for name, digest in sources.items():
        require(valid_digest(digest), f"sourceSha256: invalid hash for {name!r}")
        require(file_digest(source_path(root, name)) == digest.lower(),
                f"sourceSha256: source hash mismatch for {name!r}")

    receipt = record.get("ownerReceipt")
    require(isinstance(receipt, dict), "ownerReceipt must be an object")
    local_only(receipt, "ownerReceipt")
    expected = receipt.get("ownerCases")
    require(isinstance(expected, dict) and bool(expected)
            and all(isinstance(name, str) and bool(name) and type(count) is int and count > 0
                    for name, count in expected.items()),
            "ownerReceipt.ownerCases must contain exact method names and positive counts")
    owner, missing_owner = validate_reports(record.get("ownerNativeResults"), root, "ownerNativeResults")
    require(owner == expected, "ownerReceipt.ownerCases differs from exact native report inventory")
    if "assemblySha256" in receipt:
        binaries = receipt["assemblySha256"]
        require(isinstance(binaries, dict) and bool(binaries)
                and all(isinstance(project, str) and bool(project)
                        and project not in {".", ".."} and "/" not in project and "\\" not in project
                        and valid_digest(digest) for project, digest in binaries.items()),
                "ownerReceipt.assemblySha256 must map project names to SHA-256 hashes")
        represented: set[str] = set()
        for report in record["ownerNativeResults"]:
            project = Path(report["assembly"]).stem
            require(project in binaries,
                    f"ownerReceipt.assemblySha256 omits tested project {project!r}")
            require(report["assemblySha256"].lower() == binaries[project].lower(),
                    f"ownerReceipt.assemblySha256 differs from native binary hash for {project!r}")
            represented.add(project)
        require(represented == set(binaries),
                "ownerReceipt.assemblySha256 contains projects absent from native reports")
    native, missing_root = validate_reports(record.get("rootNativeResults"), root, "rootNativeResults")

    commands = record.get("commands", [])
    require(isinstance(commands, list), "commands must be a list")
    for index, execution in enumerate(commands):
        require(isinstance(execution, dict) and bool(execution.get("command")),
                f"commands[{index}]: execution command is required")
        require(type(execution.get("exitCode")) is int and execution["exitCode"] == 0,
                f"commands[{index}]: exitCode must be zero")
    return sum(owner.values()), sum(native.values()), missing_owner + missing_root


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("evidence", type=Path)
    parser.add_argument("--source-root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    try:
        record = json.loads(args.evidence.read_text(encoding="utf-8"))
        owner, root, missing = validate(record, args.source_root.resolve())
    except (OSError, ValueError, TypeError, KeyError) as exception:
        print(f"Deferred verification evidence rejected: {exception}", file=sys.stderr)
        return 1
    print(f"Deferred verification evidence is consistent: {owner} owner cases, {root} root cases.")
    print(f"Offline validation permits missing tested binaries ({missing} absent); original XML is historical.")
    print("Embedded commands were not executed; live readiness is not established.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
