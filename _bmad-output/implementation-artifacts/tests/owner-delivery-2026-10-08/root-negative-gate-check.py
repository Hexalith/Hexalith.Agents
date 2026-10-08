"""Verify incomplete Full/Live modes refuse before child tools or consuming resources."""

import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys


evidence = Path(__file__).resolve().parent
workspace = evidence.parents[3]
output = Path(sys.argv[1]).resolve()
output.mkdir(parents=True, exist_ok=False)
stub_directory = output / "blocked-tools"
stub_directory.mkdir()
marker = output / "unexpected-child-tool.txt"
for tool in ["dotnet", "kubectl", "curl", "wget", "ssh", "aspire", "dapr"]:
    stub = stub_directory / tool
    stub.write_text("#!/bin/sh\nprintf '%s\\n' '" + tool + "' >> \"$OWNER_GATE_CHILD_MARKER\"\nexit 97\n")
    stub.chmod(0o755)
real_dotnet = Path(shutil.which("dotnet")).resolve()
pwsh_dll = Path("/home/administrator/.dotnet/tools/.store/powershell/7.6.2/powershell/7.6.2/tools/net10.0/any/unix/pwsh.dll")
if not pwsh_dll.is_file():
    raise RuntimeError("The explicitly observed PowerShell bootstrap changed; inspect before using another")
bootstrap = [str(real_dotnet), "exec", str(pwsh_dll), "-NoProfile", "-File"]
platform = workspace.parent / "platform"
cases = [
    ("parties-live", workspace.parent / "parties", bootstrap + ["eng/verify-ext-parties-1.ps1", "-Mode", "Live", "-EvidenceDirectory", str(output / "parties-live")]),
    ("conversations-live", workspace.parent / "conversations", bootstrap + ["eng/verify-ext-conv-ai-1.ps1", "-Mode", "Live"]),
    ("custody-default", platform, bootstrap + ["eng/verify-ext-secrets-1.ps1"]),
    ("custody-live", platform, bootstrap + ["eng/verify-ext-secrets-1.ps1", "-Mode", "Live"]),
    ("custody-invalid", platform, bootstrap + ["eng/verify-ext-secrets-1.ps1", "-Mode", "Invalid"]),
    ("host-default", platform, ["bash", "eng/verify-agents-host.sh"]),
    ("host-full", platform, ["bash", "eng/verify-agents-host.sh", "--mode", "Full"]),
    ("host-invalid", platform, ["bash", "eng/verify-agents-host.sh", "--mode", "Live"]),
    ("host-missing-mode", platform, ["bash", "eng/verify-agents-host.sh", "--mode"]),
    ("host-empty-artifacts", platform, ["bash", "eng/verify-agents-host.sh", "--mode", "LocalScaffold", "--artifacts-path", ""]),
    ("host-unknown-argument", platform, ["bash", "eng/verify-agents-host.sh", "--unknown"]),
]
expected_rejections = {
    "parties-live": r"Missing live inputs:|Live qualification remains blocked:",
    "conversations-live": r"Live gate closed before any calls:",
    "custody-default": r"DependencyNotAvailable: EXT-SECRETS-1",
    "custody-live": r"DependencyNotAvailable: EXT-SECRETS-1",
    "custody-invalid": r"Cannot validate argument on parameter 'Mode'",
    "host-default": r"DependencyNotAvailable: EXT-HOST-1",
    "host-full": r"DependencyNotAvailable: EXT-HOST-1",
    "host-invalid": r"unknown mode: Live",
    "host-missing-mode": r"--mode requires Full or LocalScaffold",
    "host-empty-artifacts": r"artifacts path is empty",
    "host-unknown-argument": r"unknown argument: --unknown",
}
environment = dict(os.environ)
environment["PATH"] = str(stub_directory) + os.pathsep + environment["PATH"]
environment["OWNER_GATE_CHILD_MARKER"] = str(marker)
results = []
for name, cwd, argv in cases:
    log = output / (name + ".log")
    with log.open("w") as stream:
        result = subprocess.run(argv, cwd=cwd, env=environment, stdout=stream, stderr=subprocess.STDOUT, timeout=60)
    expected_rejection = bool(re.search(expected_rejections[name], log.read_text(errors="replace")))
    results.append({"name": name, "cwd": str(cwd), "argv": argv, "exitCode": result.returncode,
                    "childToolInvoked": marker.exists(), "log": str(log),
                    "expectedRejectionObserved": expected_rejection,
                    "logSha256": hashlib.sha256(log.read_bytes()).hexdigest()})
    if not result.returncode or marker.exists() or not expected_rejection:
        break
report = {"capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "cases": results,
          "scope": "Actual incomplete verifiers invoked with blocked child tools. Real dotnet only bootstraps PowerShell; no live lane is enabled.",
          "pass": len(results) == len(cases) and all(r["exitCode"] != 0 and not r["childToolInvoked"] and r["expectedRejectionObserved"] for r in results)}
(output / "evidence.json").write_text(json.dumps(report, indent=2) + "\n")
print(json.dumps({"cases": len(results), "pass": report["pass"]}))
sys.exit(not report["pass"])
