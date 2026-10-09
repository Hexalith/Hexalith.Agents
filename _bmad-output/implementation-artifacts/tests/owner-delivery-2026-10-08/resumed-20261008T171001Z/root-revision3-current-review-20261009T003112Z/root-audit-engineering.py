"""Independently audit stable engineering handoff files and actual focused XML."""

import datetime
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


def sha(path):
    file = Path(path)
    return hashlib.sha256(file.read_bytes() if file.is_file() else b"").hexdigest()


def read(path):
    return json.loads(Path(path).read_text())


revision = Path(sys.argv[1]).resolve()
previous = Path(sys.argv[2]).resolve()
output = Path(sys.argv[3]).resolve()
if output.exists():
    raise ValueError("Use a fresh audit output")
errors = []


def check(condition, message):
    if not condition:
        errors.append(message)


ordered = read(revision / "ordered-baselines.json")
prior = read(previous / "ordered-baselines.json")
check(ordered[:len(prior)] == prior, "Prior ordered capture prefix changed")
before_copies = 0
for filename in ordered:
    capture = Path(filename)
    data = read(capture)
    rows = data if isinstance(data, list) else data.get("files", data.get("sources"))
    check(rows is not None, f"Missing captured files: {capture}")
    for row in rows or []:
        supplied = row.get("beforeCopy") or row.get("copy")
        if supplied:
            copied = Path(supplied)
            if not copied.is_absolute():
                copied = capture.parent / copied
            expected = row.get("beforeSha256") or row.get("sha256")
            check(copied.is_file(), f"Missing supplied before copy: {copied}")
            if expected:
                check(sha(copied) == expected, f"Before bytes changed: {copied}")
            before_copies += 1

owned = read(revision / "current-owned-source.json")["files"]
for row in owned:
    check(sha(row["sourcePath"]) == row["currentSha256"], f"Current handoff hash differs: {row['sourcePath']}")
    check(Path(row["sourcePath"]).is_file() == row["existsNow"], f"Current existence differs: {row['sourcePath']}")
    if row.get("beforeCopy"):
        check(sha(row["beforeCopy"]) == row["beforeSha256"], f"Fresh handoff before bytes differ: {row['beforeCopy']}")

artifacts = read(revision / "artifact-hashes.json")["files"]
for row in artifacts:
    check(Path(row["path"]).is_file(), f"Missing artifact: {row['path']}")
    check(sha(row["path"]) == row["sha256"], f"Artifact bytes differ: {row['path']}")
    if "bytes" in row and Path(row["path"]).is_file():
        check(Path(row["path"]).stat().st_size == row["bytes"], f"Artifact length differs: {row['path']}")

commands = read(revision / "commands.json")["commands"]
for row in commands:
    for kind in ("Log", "Xml"):
        key = "archived" + kind
        if row.get(key):
            check(sha(row[key]) == row[key + "Sha256"], f"Command {kind} hash differs: {row[key]}")
    check(bool(row.get("argv")) and bool(row.get("workingDirectory")), "Command invocation is incomplete")
    check("completionExitCode" in row, "Command exit code is absent")

focused = read(revision / "final-focused-results.json")
distinct = set()
lanes = []
for row in focused["lanes"]:
    tree = ET.parse(row["xml"])
    cases = list(tree.iter("test"))
    assemblies = list(tree.iter("assembly"))
    check(len(assemblies) == 1, f"Ambiguous XML assembly: {row['xml']}")
    for assembly in assemblies:
        check(int(assembly.attrib["total"]) == len(cases), f"XML case count differs: {row['xml']}")
        check(int(assembly.attrib["passed"]) == len(cases), f"Incomplete passing XML: {row['xml']}")
        check(not any(int(assembly.attrib.get(k, "0")) for k in ("failed", "errors", "skipped", "not-run")), f"Nonpassing XML: {row['xml']}")
    selected = [case for case in cases if case.attrib["type"] in row["classes"]]
    check(len(selected) == row["passed"] and len(selected) > 0, f"Selected case count differs: {row['lane']}")
    for name in row["classes"]:
        check(any(case.attrib["type"] == name for case in selected), f"Required selected class is empty: {name}")
    for case in selected:
        check(case.attrib.get("result") == "Pass", f"Selected case did not pass: {case.attrib['name']}")
        distinct.add((case.attrib["type"], case.attrib["name"]))
    for kind, path in (("build", row["buildLog"]), ("test", row["testLog"])):
        recorded = [command for command in commands if command.get("archivedLog") == path]
        check(any(command.get("completionExitCode") == 0 for command in recorded), f"No successful exact {kind} command: {path}")
    build = Path(row["buildLog"]).read_text(errors="replace")
    check(bool(re.search(r"0 Warning\(s\).*?0 Error\(s\)", build, re.S)), f"No clean selected build: {row['buildLog']}")
    check(not re.search(r"\b(?:warning|error) [A-Z]+\d+", build), f"Selected build diagnostic: {row['buildLog']}")
    lanes.append({"lane": row["lane"], "selectedPassed": len(selected), "combinedXmlTotal": len(cases), "classes": row["classes"]})
check(len(distinct) == focused["distinctPassedCases"], "Distinct selected pass count differs")
report = {
    "auditedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "revision": str(revision), "previousRevision": str(previous),
    "orderedCapturesVerified": len(ordered), "exactPriorPrefixCount": len(prior),
    "suppliedBeforeCopiesChecked": before_copies, "currentHandoffPathsChecked": len(owned),
    "engineeringArtifactsChecked": len(artifacts), "commandsChecked": len(commands),
    "historicalNonpassingCommandsRetained": sum(row.get("completionExitCode") != 0 for row in commands),
    "distinctPassingSelectedCases": len(distinct), "lanes": lanes, "errors": errors,
    "scope": "Engineering integrity and selected focused evidence only. Root source review, full Local matrix, protected-state audit and runtime qualification require separate evidence."
}
output.write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({key: value for key, value in report.items() if key != "lanes"}))
sys.exit(bool(errors))
