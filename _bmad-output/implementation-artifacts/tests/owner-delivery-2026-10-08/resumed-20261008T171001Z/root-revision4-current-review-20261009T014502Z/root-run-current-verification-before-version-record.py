"""Run fresh owner Local gates against a frozen review manifest; never run live seams."""

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
manifest_path = Path(sys.argv[1]).resolve()
phase = sys.argv[2]
if not re.fullmatch(r"[a-z0-9-]+", phase):
    raise ValueError("Use a unique simple phase name")
output = evidence / phase
output.mkdir(exist_ok=False)
artifacts = Path("/tmp") / f"hexalith-owner-{phase}-20261008"
if artifacts.exists():
    raise ValueError("Fresh artifacts are required")
manifest = json.loads(manifest_path.read_text())
commands = []
index = output / "command-index.json"
owned_classes = {}
for item in manifest["files"]:
    relative = item["relativePath"]
    path = Path(item["sourcePath"])
    if not path.exists() or not relative.startswith("tests/") or not path.stem.endswith("Tests"):
        continue
    content = path.read_text()
    namespace = re.search(r"^namespace\s+([\w.]+)\s*;", content, re.M)
    if namespace and re.search(r"\bclass\s+" + re.escape(path.stem) + r"\b", content):
        project = relative.split("/")[1]
        owned_classes.setdefault(project, set()).add(namespace.group(1) + "." + path.stem)


def save_index():
    index.write_text(json.dumps({
        "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
        "reviewManifest": str(manifest_path),
        "reviewManifestSha256": hashlib.sha256(manifest_path.read_bytes()).hexdigest(),
        "commands": commands,
        "requiredOwnedClasses": {k: sorted(v) for k, v in owned_classes.items()},
        "scope": "Fresh normal Debug source/project-reference Local checks. No complete immutable delivery target, acceptance or live qualification inferred.",
        "countingLimits": "Parties custody/SDK and dedicated owner lanes overlap. Do not sum as unique coverage.",
    }, indent=2) + "\n")


def run(name, cwd, argv, directory, build_logs=(), xml_files=(), required=()):
    directory.mkdir(parents=True, exist_ok=True)
    log = directory / "root-invocation.log"
    started = datetime.datetime.now(datetime.timezone.utc).isoformat()
    print(json.dumps({"starting": name, "log": str(log)}), flush=True)
    with log.open("w") as stream:
        result = subprocess.run(argv, cwd=cwd, stdout=stream, stderr=subprocess.STDOUT, timeout=3600)
    row = {"name": name, "cwd": str(cwd), "argv": argv, "startedUtc": started,
           "finishedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
           "exitCode": result.returncode, "invocationLog": str(log),
           "buildLogs": [str(p) for p in build_logs], "xmlFiles": [str(p) for p in xml_files],
           "requiredClasses": sorted(required)}
    commands.append(row)
    save_index()
    print(json.dumps({"finished": name, "exitCode": result.returncode}), flush=True)
    if result.returncode:
        raise RuntimeError(f"Normal gate failed: {name}; preserve its log and index")
    return row


parties = workspace.parent / "parties"
conversations = workspace.parent / "conversations"
platform = workspace.parent / "platform"
eventstore = workspace.parent / "eventstore"

parties_output = output / "parties"
row = run("Parties complete mapped Local lanes", parties, ["pwsh", "-NoProfile", "-File", "eng/verify-ext-parties-1.ps1", "-Mode", "Local",
    "-EvidenceDirectory", str(parties_output), "-ArtifactsDirectory", str(artifacts / "parties")], parties_output)
row["buildLogs"] = sorted(str(p) for p in parties_output.glob("*-build.log"))
row["xmlFiles"] = sorted(str(p) for p in parties_output.glob("*-tests.xml"))
save_index()

conversations_output = output / "conversations"
row = run("Conversations four-suite Local", conversations, ["pwsh", "-NoProfile", "-File", "eng/verify-ext-conv-ai-1.ps1", "-Mode", "Local",
    "-ArtifactsPath", str(artifacts / "conversations")], conversations_output,
    required=owned_classes.get("Hexalith.Conversations.Server.Tests", ()))
actual_runs = sorted((artifacts / "conversations").glob("run-*"))
if len(actual_runs) != 1:
    raise RuntimeError("Cannot identify the unique actual Conversations execution")
actual = actual_runs[0]
for pattern in ("*.log", "*.xml", "*.json"):
    for path in actual.glob(pattern):
        shutil.copy2(path, conversations_output / path.name)
row["actualArtifactsPath"] = str(actual)
row["buildLogs"] = sorted(str(p) for p in conversations_output.glob("*-build.log"))
row["xmlFiles"] = sorted(str(p) for p in conversations_output.glob("Hexalith.*.xml"))
save_index()

custody_output = output / "custody"
row = run("Custody expanded Local", platform, ["pwsh", "-NoProfile", "-File", "eng/verify-ext-secrets-1.ps1", "-Mode", "Local",
    "-ArtifactsDirectory", str(artifacts / "custody"), "-EvidenceDirectory", str(custody_output), "-EventStoreSourceRoot", str(eventstore)], custody_output,
    build_logs=[custody_output / "local-build.log"], xml_files=[custody_output / "local-tests.xml"],
    required=owned_classes.get("Hexalith.Platform.Custody.Tests", ()))

version_args = ["dotnet", "msbuild", str(eventstore / "src/Hexalith.EventStore.Contracts/Hexalith.EventStore.Contracts.csproj"), "-nologo",
                "-getProperty:HexalithEventStoreVersion", "-p:Configuration=Debug", "-p:UseHexalithProjectReferences=true", "-p:NuGetAudit=false"]
version = subprocess.run(version_args, cwd=eventstore, text=True, capture_output=True)
(output / "eventstore-version.stdout.log").write_text(version.stdout)
(output / "eventstore-version.stderr.log").write_text(version.stderr)
if version.returncode or not re.fullmatch(r"\d+\.\d+\.\d+(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?", version.stdout.strip()):
    raise RuntimeError("Cannot resolve current selected SDK source version")
sdk_flags = ["-c", "Debug", "--artifacts-path", str(artifacts / "eventstore"), "-m:1", "-p:NuGetAudit=false",
             "-p:MinVerVersionOverride=1.0.0", "-p:UseHexalithProjectReferences=true", "-p:UseNuGetDeps=false",
             f"-p:HexalithEventStoreRoot={eventstore}", f"-p:HexalithEventStoreVersion={version.stdout.strip()}"]
for project in ["Hexalith.EventStore.Contracts.Tests", "Hexalith.EventStore.Client.Tests", "Hexalith.EventStore.Server.Tests", "Hexalith.EventStore.PayloadProtection.Tests"]:
    classes = sorted(owned_classes.get(project, ()))
    if not classes and project != "Hexalith.EventStore.PayloadProtection.Tests":
        continue
    project_output = output / project
    build = run(project + " normal build", eventstore, ["dotnet", "build", str(eventstore / f"tests/{project}/{project}.csproj")] + sdk_flags, project_output)
    build["buildLogs"] = [build["invocationLog"]]
    assembly = artifacts / f"eventstore/bin/{project}/debug/{project}.dll"
    test_args = ["dotnet", str(assembly)]
    if project != "Hexalith.EventStore.PayloadProtection.Tests":
        for name in classes:
            test_args.extend(["-class", name])
    xml = project_output / "tests.xml"
    test_args.extend(["-result-xml", str(xml)])
    run(project + " owned consumer tests", eventstore, test_args, project_output / "execution", xml_files=[xml], required=classes)
    save_index()

host_output = output / "host-scaffold"
row = run("Host LocalScaffold", platform, ["bash", "eng/verify-agents-host.sh", "--mode", "LocalScaffold", "--artifacts-path", str(artifacts / "host")], host_output)
row["buildLogs"] = [row["invocationLog"]]
save_index()
print(json.dumps({"commandIndex": str(index), "commands": len(commands), "state": "Commands finished; independent XML/source/protected-state audit still required"}), flush=True)
