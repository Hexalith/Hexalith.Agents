"""Audit recorded owner commands independently; never infer live qualification."""

import datetime
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


evidence = Path(__file__).resolve().parent
workspace = evidence.parents[3]
index = Path(sys.argv[1])
commands = json.loads(index.read_text())["commands"]
errors = []
results = []
for command in commands:
    totals = dict.fromkeys(["total", "passed", "failed", "errors", "skipped", "not-run"], 0)
    classes = {}
    for filename in command.get("xmlFiles", []):
        document = ET.parse(filename)
        assemblies = list(document.iter("assembly"))
        cases = list(document.iter("test"))
        if len(assemblies) != 1 or int(assemblies[0].attrib["total"]) != len(cases):
            errors.append(f"Missing or inconsistent execution: {filename}")
        for assembly in assemblies:
            for key in totals:
                totals[key] += int(assembly.attrib[key])
        for case in cases:
            if case.attrib.get("result") != "Pass":
                errors.append(f"Nonpass: {filename}: {case.attrib.get('name')}")
            name = case.attrib["type"]
            classes[name] = classes.get(name, 0) + 1
    if command.get("xmlFiles") and (
        totals["total"] <= 0
        or totals["passed"] != totals["total"]
        or any(totals[key] for key in ["failed", "errors", "skipped", "not-run"])
    ):
        errors.append(f"Incomplete test gate: {command['name']}")
    for filename in command.get("buildLogs", []):
        content = Path(filename).read_text(errors="replace")
        if not re.search(r"0 Warning\(s\).*?0 Error\(s\)", content, re.S):
            errors.append(f"Missing clean build summary: {filename}")
        if re.search(r"\b(?:warning|error) [A-Z]+\d+", content):
            errors.append(f"Build diagnostic: {filename}")
    if command["exitCode"] != 0:
        errors.append(f"Command failed: {command['name']}")
    results.append({"name": command["name"], "totals": totals, "classes": classes})

initial = json.loads((evidence / "initial-state.json").read_text())
date = json.loads((evidence / "authorized-date-update.json").read_text())
for relative, digest in initial["protected"].items():
    if relative.endswith("spec-5-4-owner-prerequisites.md"):
        continue
    if relative.endswith("external-dependency-register.md"):
        digest = date["register_after_sha256"]
    if sha(workspace / relative) != digest:
        errors.append(f"Protected state changed: {relative}")
parent = (workspace / "_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md").read_bytes()
frozen = re.search(rb"<frozen-after-approval.*?</frozen-after-approval>", parent, re.S).group()
if hashlib.sha256(frozen).hexdigest() != initial["frozen_parent_sha256"]:
    errors.append("Frozen prerequisite intent changed")
register = (workspace / "_bmad-output/planning-artifacts/external-dependency-register.md").read_bytes()
new_date = b"| `TargetIntegrationDate` | `2026-10-08` |"
old_date = b"| `TargetIntegrationDate` | `TBD` |"
if register.count(new_date) != 4 or hashlib.sha256(register.replace(new_date, old_date)).hexdigest() != date["register_before_sha256"]:
    errors.append("Register differs beyond four authorized date cells")

report = {
    "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "commandIndex": str(index.resolve()),
    "scope": "Root fresh Local source verification; no complete delivery target or live qualification inferred.",
    "results": results,
    "errors": errors,
}
(evidence / "root-final-verification-audit.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({"results": [{"name": r["name"], "totals": r["totals"]} for r in results], "errors": errors}, indent=2))
sys.exit(bool(errors))
