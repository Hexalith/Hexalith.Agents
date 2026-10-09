"""Check complete matrix shape, frozen bytes and source stability independently."""

import datetime
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


directory = Path(__file__).resolve().parent
manifest_path, index_path, before_path, after_path, output_path = map(Path, sys.argv[1:])
if output_path.exists():
    raise ValueError("Use a fresh output path; retain historical audits")
manifest = json.loads(manifest_path.read_text())
index = json.loads(index_path.read_text())
plan = json.loads((directory / "root-full-local-matrix-plan.json").read_text())
prior = json.loads((directory.parent / "root-revision5-current-review-20261009T024940Z/root-stable-protected-class-audit.json").read_text())
before, after = [json.loads(path.read_text()) for path in (before_path, after_path)]
errors = []


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


if index["reviewManifestSha256"] != digest(manifest_path):
    errors.append("Command manifest bytes differ from the frozen review manifest")
if Path(index["reviewManifest"]).resolve() != manifest_path.resolve():
    errors.append("Commands identify a different review manifest")
commands = index["commands"]
names = [command["name"] for command in commands]
if names != plan["requiredTopLevelCommands"]:
    errors.append("Incomplete or reordered required top-level matrix")
drift = {name: {"before": before.get(name), "after": after.get(name)}
         for name in sorted(set(before) | set(after)) if before.get(name) != after.get(name)}
if drift:
    errors.append("Source/build inputs changed during the full matrix; refresh affected checks")
frozen_checks = []
required = {}
for item in manifest["inputs"]:
    if digest(item["path"]) != item["sha256"]:
        errors.append(f"Frozen capture input changed: {item['path']}")
if digest(manifest["diffPath"]) != manifest["diffSha256"]:
    errors.append("Frozen cumulative review diff changed")
for item in manifest["files"]:
    source = Path(item["sourcePath"])
    expected = item["currentSha256"]
    actual = digest(source) if source.is_file() else hashlib.sha256(b"").hexdigest()
    frozen_checks.append({"path": str(source), "expected": expected, "actual": actual})
    if actual != expected:
        errors.append(f"Frozen review source changed: {source}")
    if digest(item["reviewCopy"]) != expected:
        errors.append(f"Frozen source copy changed: {item['reviewCopy']}")
    for snapshot, label in ((before, "before"), (after, "after")):
        if source.is_file() and source.suffix in {".cs", ".csproj", ".props", ".targets", ".ps1", ".sh"}:
            if snapshot.get(str(source)) != expected:
                errors.append(f"Frozen input missing or different in {label} matrix snapshot: {source}")
    relative = item["relativePath"]
    if not source.is_file() or not relative.startswith("tests/") or not source.stem.endswith("Tests"):
        continue
    content = source.read_text()
    namespace = re.search(r"^namespace\s+([\w.]+)\s*;", content, re.M)
    if namespace and re.search(r"\bclass\s+" + re.escape(source.stem) + r"\b", content):
        required.setdefault(relative.split("/")[1], set()).add(namespace.group(1) + "." + source.stem)
actual_map = {key: sorted(value) for key, value in sorted(required.items())}
if actual_map != {key: sorted(value) for key, value in sorted(index["requiredOwnedClasses"].items())}:
    errors.append("Recorded class map does not match actual frozen owning test classes")
for project, classes in prior["requiredOwnedClasses"].items():
    if not set(classes).issubset(required.get(project, set())):
        errors.append(f"Previously required class dropped from final map: {project}")
artifact_hashes = {}
all_classes = set()
passing = 0
shape = []
for command in commands:
    if command["exitCode"] != 0:
        errors.append(f"Command failed: {command['name']}")
    started, finished = [datetime.datetime.fromisoformat(command[key]) for key in ("startedUtc", "finishedUtc")]
    if finished < started or not command.get("argv") or not command.get("cwd"):
        errors.append(f"Invalid command provenance: {command['name']}")
    for key in ("invocationLog", "stderrLog"):
        if command.get(key):
            artifact_hashes[command[key]] = digest(command[key])
    for filename in command.get("buildLogs", []):
        artifact_hashes[filename] = digest(filename)
        content = Path(filename).read_text(errors="replace")
        if not re.search(r"0 Warning\(s\).*?0 Error\(s\)", content, re.S) or re.search(r"\b(?:warning|error) [A-Z]+\d+", content):
            errors.append(f"Build not independently clean: {filename}")
    classes = set()
    for filename in command.get("xmlFiles", []):
        artifact_hashes[filename] = digest(filename)
        document = ET.parse(filename)
        assemblies, tests = list(document.iter("assembly")), list(document.iter("test"))
        if len(assemblies) != 1 or not tests or int(assemblies[0].attrib["total"]) != len(tests):
            errors.append(f"Invalid actual execution XML: {filename}")
        for assembly in assemblies:
            if int(assembly.attrib["passed"]) != int(assembly.attrib["total"]) or any(int(assembly.attrib[key]) for key in ("failed", "errors", "skipped", "not-run")):
                errors.append(f"Incomplete actual execution XML: {filename}")
        for test in tests:
            if test.attrib.get("result") != "Pass":
                errors.append(f"Nonpass test: {filename}: {test.attrib.get('name')}")
            else:
                passing += 1
            classes.add(test.attrib["type"])
        all_classes.update(classes)
    if not set(command.get("requiredClasses", [])).issubset(classes):
        errors.append(f"Required command classes did not execute: {command['name']}")
    shape.append({"name": command["name"], "buildLogs": len(command.get("buildLogs", [])), "xmlFiles": len(command.get("xmlFiles", []))})
expected_shape = [(12, 12), (4, 4), (1, 1), (0, 0)] + [(1, 0), (0, 1)] * 4 + [(0, 0), (1, 0)]
if [(item["buildLogs"], item["xmlFiles"]) for item in shape] != expected_shape:
    errors.append("Nested Local lane evidence differs from complete approved runner shape; inspect any intentionally added lane explicitly")
missing = sorted(set().union(*required.values()) - all_classes)
if missing:
    errors.append("Owning test classes did not actually execute")
report = {"capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
          "auditHelperSha256": digest(__file__),
          "reviewManifest": str(manifest_path.resolve()), "commandIndex": str(index_path.resolve()),
          "shape": shape, "topLevelCommands": len(commands),
          "normalBuildLogs": sum(item["buildLogs"] for item in shape),
          "actualFullSuiteXmlFiles": sum(item["xmlFiles"] for item in shape),
          "passingTestExecutions": passing, "requiredOwnedClasses": actual_map,
          "requiredOwnedClassCount": sum(map(len, required.values())), "missingClasses": missing,
          "sourceDrift": drift, "frozenSourceChecks": frozen_checks,
          "artifactHashes": artifact_hashes, "errors": errors,
          "limits": "Local source checks only. Lanes overlap; executions are not unique coverage. Protected state is checked by the separate root XML/protected-state audit. No runtime enrollment, backend qualification, binding or full-story completion inferred."}
output_path.write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({key: report[key] for key in ("topLevelCommands", "normalBuildLogs", "actualFullSuiteXmlFiles", "passingTestExecutions", "requiredOwnedClassCount", "missingClasses", "errors")}, indent=2))
sys.exit(bool(errors))
