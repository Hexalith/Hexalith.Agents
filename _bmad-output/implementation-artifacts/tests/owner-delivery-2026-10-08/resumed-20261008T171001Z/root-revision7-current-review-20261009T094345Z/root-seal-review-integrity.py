"""Seal source/evidence integrity after the completed revision7 root review."""
import datetime
import hashlib
import json
from pathlib import Path
import re
import sys

review = Path(__file__).resolve().parent
workspace = review.parents[5]
base = review.parents[1]
resume = review.parent
refresh_dir = workspace / "_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/root-revision7-final-refresh-20261009t1036"
manifest = json.loads((review / "review-input.json").read_text())
errors = []

def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

frozen = []
for row in manifest["files"]:
    path = Path(row["sourcePath"])
    actual = digest(path) if path.is_file() else hashlib.sha256(b"").hexdigest()
    copied = digest(row["reviewCopy"])
    if actual != row["currentSha256"] or copied != row["currentSha256"]:
        errors.append("Frozen reviewed source or copy differs: " + str(path))
    frozen.append({"path": str(path), "expected": row["currentSha256"], "actual": actual, "copySha256": copied})
capture = []
for row in manifest["inputs"]:
    actual = digest(row["path"])
    capture.append({"path": row["path"], "expected": row["sha256"], "actual": actual})
    if actual != row["sha256"]:
        errors.append("Frozen capture input differs: " + row["path"])
if digest(manifest["diffPath"]) != manifest["diffSha256"]:
    errors.append("Raw cumulative review diff differs")
staged = json.loads((review / "root-staged-diff.json").read_text())
if digest(staged["diffFile"]) != staged["diffSha256"]:
    errors.append("Staged cumulative review diff differs")
packages = []
for revision, count in (("revision-6", 289), ("revision-7", 100)):
    path = resume / revision / "artifact-hashes.json"
    package = json.loads(path.read_text())
    failures = [row["path"] for row in package["files"] if digest(row["path"]) != row["sha256"]]
    if len(package["files"]) != count or failures:
        errors.append("Sealed engineering artifacts differ: " + revision)
    packages.append({"revision": revision, "manifestSha256": digest(path), "checked": len(package["files"]), "mismatches": failures})
old_audit = json.loads((review / "root-full-matrix-shape-and-drift-audit.json").read_text())
old_artifacts = [name for name, expected in old_audit["artifactHashes"].items() if digest(name) != expected]
if old_artifacts:
    errors.append("Initial root command/build/XML artifacts differ")
negative = json.loads((review / "negative-live-gates/evidence.json").read_text())
if not negative["pass"] or len(negative["cases"]) != 11:
    errors.append("Fresh negative gates incomplete")
individual = json.loads((review / "current-review-individual-verdicts.json").read_text())
triage = json.loads((review / "current-review-triage.json").read_text())
if triage["individualVerdictsSha256"] != digest(review / "current-review-individual-verdicts.json"):
    errors.append("Grouped triage does not identify exact individual verdicts")
if len(individual["individualVerdicts"]) != 16 or triage["counts"] != {"bad_spec": 6, "patch": 5, "defer": 2, "reject": 3}:
    errors.append("Review triage membership/count mismatch")
required = {
 "_bmad-output/implementation-artifacts/spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md": "5b1de576274f93a464322c18ee977e8bc4d489c2b8569a64986de3c4fc5304b1",
 "_bmad-output/implementation-artifacts/sprint-status.yaml": "b72bc45b7a645d56a5981dbd1463f50a410ec39605c17be05222f6e6e2229691",
 "_bmad-output/planning-artifacts/external-dependency-register.md": "184e16c05b57527abcdbd8588ae0c9fa10fe4c6093a53e66a509e9aa39fbcccb",
 "../parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md": "8f6780d252154803343432e2126e596209e432284cfddf1c7f5b9eb596aa2fe1"
}
protected = {}
for relative, expected in required.items():
    actual = digest(workspace / relative)
    protected[relative] = {"expected": expected, "actual": actual}
    if actual != expected:
        errors.append("Protected story/sprint/register/policy changed: " + relative)
parent_path = workspace / "_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md"
parent = parent_path.read_bytes()
intent = re.search(rb"<frozen-after-approval.*?</frozen-after-approval>", parent, re.S).group()
intent_hash = hashlib.sha256(intent).hexdigest()
if intent_hash != "0cd11ddac96af9775593ea0aa24d969b235b32dfcc4a76a80e51d7eb9c82e2cb":
    errors.append("Frozen intent changed")
frontmatter = parent.split(b"---", 2)[1]
for value in (b"status: 'in-progress'", b"human_approval: 'accepted'", b"baseline_commit: 'b252fcfde51bd04317ce5843a0a42bfe4dce5170'"):
    if value not in frontmatter:
        errors.append("Protected parent metadata changed: " + value.decode())
expected_counter = int(sys.argv[1])
if b"review_loop_iteration: " + str(expected_counter).encode() not in frontmatter:
    errors.append("Unexpected review counter")
refresh_shape = review / "root-final-matrix-shape-and-drift-audit.json"
refresh_xml = review / "root-final-full-verification-audit.json"
refresh = {"commandIndex": str(refresh_dir / "command-index.json")}
if refresh_shape.exists():
    audit = json.loads(refresh_shape.read_text())
    artifact_mismatches = [name for name, expected in audit["artifactHashes"].items() if digest(name) != expected]
    if artifact_mismatches:
        errors.append("Refreshed root artifacts differ")
    refresh.update({key: audit[key] for key in ("topLevelCommands", "normalBuildLogs", "actualFullSuiteXmlFiles", "passingTestExecutions", "requiredOwnedClassCount", "missingClasses", "errors", "sourceDrift")})
    refresh["artifactMismatches"] = artifact_mismatches
    refresh["artifactHashCount"] = len(audit["artifactHashes"])
if refresh_xml.exists():
    refresh["xmlAndProtectedStateAuditErrors"] = json.loads(refresh_xml.read_text())["errors"]
prior_refresh_audit = json.loads((review / "root-refreshed-matrix-shape-and-drift-audit.json").read_text())
prior_refresh_mismatches = [name for name, expected in prior_refresh_audit["artifactHashes"].items() if digest(name) != expected]
if prior_refresh_mismatches:
    errors.append("Previous refreshed root artifacts differ")
before = json.loads((review / "source-before-final-refresh.json").read_text())
after_path = review / "source-after-final-refresh.json"
drift = {}
if after_path.exists():
    after = json.loads(after_path.read_text())
    drift = {path: {"before": before.get(path), "after": after.get(path)} for path in sorted(before.keys() | after.keys()) if before.get(path) != after.get(path)}
report = {
 "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
 "parentSha256": digest(parent_path), "reviewLoopIteration": expected_counter,
 "frozenIntentSha256": intent_hash, "protected": protected,
 "frozenOwnedPathsChecked": len(frozen), "frozenSourceChecks": frozen,
 "captureInputsChecked": len(capture), "captureChecks": capture,
 "engineeringPackageIntegrity": packages,
 "initialFullLocal": {"commands": old_audit["topLevelCommands"], "normalBuildLogs": old_audit["normalBuildLogs"], "xmlFiles": old_audit["actualFullSuiteXmlFiles"], "passingExecutions": old_audit["passingTestExecutions"], "requiredClassCount": old_audit["requiredOwnedClassCount"], "artifactMismatches": old_artifacts, "strictAuditErrorsRetained": old_audit["errors"]},
 "negativeGateCount": len(negative["cases"]), "negativeGatesPassed": negative["pass"],
 "allReviewersFinalBeforeIndividualTriage": individual["reviewersAllFinal"],
 "individualVerdictCount": len(individual["individualVerdicts"]), "groups": triage["counts"],
 "previousRefreshedFullLocal": {"commands": prior_refresh_audit["topLevelCommands"], "normalBuildLogs": prior_refresh_audit["normalBuildLogs"], "xmlFiles": prior_refresh_audit["actualFullSuiteXmlFiles"], "passingExecutions": prior_refresh_audit["passingTestExecutions"], "requiredClassCount": prior_refresh_audit["requiredOwnedClassCount"], "artifactMismatches": prior_refresh_mismatches, "strictAuditErrorsRetained": prior_refresh_audit["errors"]},
 "refreshedFullLocal": refresh, "refreshSourceDrift": drift,
 "completeCurrentFullLocalClosure": bool(refresh_shape.exists() and refresh_xml.exists() and not refresh.get("errors") and not refresh.get("xmlAndProtectedStateAuditErrors") and not drift),
 "sourceAcceptance": False, "ownerAcceptance": False, "runtimeQualification": False,
 "errors": errors,
 "limits": "Integrity closure only. Full Local success and source stability are reported separately with their actual audit errors. Preserve every historical failed/drift audit. Concurrent unowned work is never attributed to this engineer or rolled back; no runtime, full Story5.4 or parent completion is inferred."
}
output = review / (sys.argv[2] if len(sys.argv) > 2 else ("root-final-integrity-audit.json" if expected_counter == 8 else "root-pretriage-integrity-audit.json"))
if output.exists():
    raise ValueError("Use a fresh closure path; preserve historical evidence")
output.write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({key: report[key] for key in ("reviewLoopIteration", "frozenOwnedPathsChecked", "captureInputsChecked", "negativeGateCount", "groups", "errors")}, indent=2))
sys.exit(bool(errors))

