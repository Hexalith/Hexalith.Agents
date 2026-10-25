"""Verify this static reconciliation without running any owner qualification."""

import datetime
import hashlib
import json
from pathlib import Path
import re
import subprocess


audit = Path(__file__).resolve().parent
root = audit.parents[4]
checks = []


def read(name):
    return json.loads((audit / name).read_text())


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check(name, passed, detail):
    checks.append({"name": name, "passed": bool(passed), "detail": detail})


accepted = read("accepted-records.json")
register = root / "_bmad-output/planning-artifacts/external-dependency-register.md"
register_text = register.read_text()
check("accepted register unchanged", digest(register) == accepted["registerSha256"], digest(register))
required_fields = {
    "Owner", "Repository", "RequiredArtifact", "TargetVersionOrCommit",
    "TargetIntegrationDate", "CompatibilityContractAndVerificationCommand",
    "RequiredEvidenceLevel", "AcceptedStatus", "ConsumingStories",
}
for record_id, record in accepted["records"].items():
    check(record_id + " complete section preserved", record["completeSectionVerbatim"] in register_text,
          "Includes all contract extensions and runtime bindings.")
    check(record_id + " nine commitment fields retained", required_fields <= record["acceptedFields"].keys(),
          sorted(record["acceptedFields"]))

preserved = read("preservation-before.json")["files"]
preservation_drift = [f["path"] for f in preserved
                      if not Path(f["path"]).is_file() or digest(Path(f["path"])) != f["sha256"]]
check("historical evidence and gate files unchanged", not preservation_drift,
      {"files": len(preserved), "drift": preservation_drift})
attempts = read("original-attempts.json")["files"]
attempt_drift = [f["path"] for f in attempts
                 if not Path(f["path"]).is_file() or digest(Path(f["path"])) != f["sha256"]]
check("original October 18 attempts unchanged", not attempt_drift,
      {"files": len(attempts), "drift": attempt_drift})

spec = root / "_bmad-output/implementation-artifacts/spec-5-4-reconcile-owner-deliveries.md"
before = (audit / "spec-before.md").read_text()
after = spec.read_text()
frozen = r"<frozen-after-approval[\s\S]*?</frozen-after-approval>"
check("frozen intent unchanged", re.search(frozen, before).group() == re.search(frozen, after).group(),
      "Verbatim comparison with the invocation baseline.")
direction = r"## EXT-PARTIES-1 execution direction[\s\S]*?(?=## Code Map)"
check("custody stop direction unchanged", re.search(direction, before).group() == re.search(direction, after).group(),
      "No contract amendment, backend implementation restart or replacement date.")
check("draft and blocked status retained", "status: 'draft'" in after and "qualification_blocked: true" in after,
      "No approval requested for incomplete deliveries.")

observed = {}
inconsistent = []
for name, key in [("platform-owner-packets.json", "inspectedFiles"),
                  ("identity-owner-packets.json", "inspectedFiles"),
                  ("protection-owner-packets.json", "observedFiles")]:
    for item in read(name)[key]:
        signature = (item.get("exists"), item.get("sha256"))
        if item["path"] in observed and observed[item["path"]] != signature:
            inconsistent.append(item["path"])
        observed[item["path"]] = signature
input_drift = []
for name, (exists, sha) in observed.items():
    path = Path(name)
    if sha and (not path.is_file() or digest(path) != sha):
        input_drift.append(name)
    elif exists is False and path.exists():
        input_drift.append(name)
check("inspected input observations agree", not inconsistent, inconsistent)
check("inspected public inputs unchanged", not input_drift,
      {"uniquePaths": len(observed), "hashedFiles": sum(bool(s[1]) for s in observed.values()), "drift": input_drift})

reconciliation = read("reconciliation.json")
evidence_drift = [name for name, sha in reconciliation["evidenceFilesSha256"].items()
                  if digest(audit / name) != sha]
check("consolidated evidence manifest matches", not evidence_drift, evidence_drift)
check("no incomplete commitments submitted", reconciliation["completeChangedCommitmentsReadyForAcceptance"] == [],
      "No replacement acceptance or availability advancement.")
check("accepted fields match consolidated record", all(
    reconciliation["records"][key]["acceptedFields"] == value["acceptedFields"]
    for key, value in accepted["records"].items()), "All five records.")
check("zero qualification and consumer execution", not any(reconciliation["execution"].values()),
      reconciliation["execution"])

diff = subprocess.run(["git", "diff", "--check"], cwd=root, text=True, capture_output=True)
check("git diff --check", diff.returncode == 0,
      {"exitCode": diff.returncode, "stdout": diff.stdout, "stderr": diff.stderr})
report = root / "_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-25-resumed.md"
documents = [spec, report] + [p for p in audit.glob("*.md") if p.name != "spec-before.md"]
bad_links = []
bad_whitespace = []
for document in documents:
    text = document.read_text()
    for number, line in enumerate(text.splitlines(), 1):
        if line.rstrip() != line:
            bad_whitespace.append(f"{document}:{number}")
    for link in re.findall(r"\]\(([^)]+)\)", text):
        if "://" in link or link.startswith("#"):
            continue
        target = (document.parent / link.split("#")[0]).resolve()
        if not target.exists() and target != audit / "verification.json":
            bad_links.append({"document": str(document), "link": link})
check("new and updated document links resolve", not bad_links, bad_links)
check("new and updated documents have no trailing whitespace", not bad_whitespace, bad_whitespace)

receipt = {
    "schema": "story-5-4-static-verification/v2",
    "observedAtUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "passed": all(c["passed"] for c in checks),
    "checks": checks,
    "execution": reconciliation["execution"],
    "reconciliationSha256": digest(audit / "reconciliation.json"),
    "reportSha256": digest(report),
    "specSha256": digest(spec),
    "limitations": [
        "Static public-file and source audit only; no owner compatibility command or application tests ran.",
        "No remote installation survey or secret/credential retrieval.",
        "Canonical source observations are timestamped in source inventories and owner reports.",
        "Baseline excludes build outputs, .git, node_modules and symlinks; mutable binaries are reported separately.",
    ],
}
with (audit / "verification.json").open("x") as output:
    json.dump(receipt, output, indent=2)
    output.write("\n")
print(json.dumps({"passed": receipt["passed"], "checks": len(checks),
                  "protectedFiles": len(preserved), "inspectedPaths": len(observed),
                  "failedChecks": [c for c in checks if not c["passed"]]}, indent=2))
raise SystemExit(0 if receipt["passed"] else 1)
