#!/usr/bin/env python3
"""Replay the three independent deferred dependency experiments without changing checkout pins.

Run under run-with-temp. Exact archives, commands, native XML, mappings and input/output
hashes are retained in --evidence. A failed build is blocked evidence, never proof that a
pin is necessary. No audit/test gate is suppressed and no packages are published.
"""
from __future__ import annotations

import argparse
from collections import Counter
import ctypes
import hashlib
import io
import json
import os
import re
import signal
import sys
from pathlib import Path
import shutil
import subprocess
import tarfile
import time
import xml.etree.ElementTree as ET

RUNNER_SOURCE = Path(__file__).read_bytes()
CASE_INVENTORIES = json.loads(Path(__file__).with_name("deferred-dependency-case-inventories.json").read_text())
ROOT = Path(__file__).resolve().parents[1]
CURRENT = "f90e43f2e048f3b9ef77e7445eaa51544b76536d"
REVIEWED = "13857fd86e9fda7e2ba0f4017aadb042750e4284"
PREVIOUS = "599208dd40efadef728363c227a0f75ebd888337"
PLATFORM = "17b1f688b332020fafbe1da07a5c04a41886c7bd"
EVENTSTORE = "eecc0e4da70a0d64aaeff4fb75cdc06f94eb7563"
RELEASED = "f792bba6c0564d3c36dda6ad1c2fc55b0b0a6331"
HISTORICAL_EVENTSTORE = "4bc61d9a60fae13a65f23963c1cb731065222f2a"
OWNER_ROOTS = {}
PLATFORM_OVERLAYS = {}
FIVE = ("Builds", "Commons", "Conversations", "FrontComposer", "PolymorphicSerializations")
ORIGINAL_METHODS = (
    ("Hexalith.Platform.Composition.Tests.DomainEnrollmentBindingTests", "StaticValidityRequiresExplicitProjectAndScopedConfigurationBeforeComposition"),
    ("Hexalith.Platform.Composition.Tests.PartiesAdoptionTests", "SignedPartiesRunUsesTheRunnerJwtSettingsAsync"),
    ("Hexalith.Platform.Composition.Tests.PartiesAdoptionTests", "CompositionWiresSingularPartyGatewayAndDeclarativeTenantSubscription"),
)
REVIEWED_EDITOR_CONFIG = "references/Hexalith.EventStore/.editorconfig"
REVIEWED_EDITOR_HASHES = ("c9a43ea4f0ecac612713588347871ab1d91a15c145a705f328220bdf7e1db09d",
                          "7c92699cbde7ffdfe8a5ef2fc56f832f1b0e56919ce43bc1b39694e6df86a051")
RECORDED_ENVIRONMENT_KEYS = frozenset(("CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps",
    "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets",
    "HEXALITH_SOURCE_MAPPING_HASH", "HEXALITH_SOURCE_MAPPING_PATH", "HEXALITH_CONVERSATIONS_CATALOG_INPUTS", "Configuration"))


def digest(data):
    return hashlib.sha256(data).hexdigest()


def git(repo, *args):
    return subprocess.check_output(["git", "-C", str(repo), *args], stderr=subprocess.PIPE)


def pins(repo, revision):
    return {line.split("\t")[1].split("/")[-1]: line.split()[2]
            for line in git(repo, "ls-tree", "-r", revision, "references").decode().splitlines()
            if line.startswith("160000 ")}


def owner_repository(name, revision):
    candidates = [ROOT / "references" / name, ROOT.parent / name.removeprefix("Hexalith.").lower()]
    if name in OWNER_ROOTS:
        candidates = [OWNER_ROOTS[name]]
    if name == "Hexalith.Agents" and name not in OWNER_ROOTS:
        candidates.insert(0, ROOT)
    if name == "Hexalith.AI.Tools" and name not in OWNER_ROOTS:
        candidates.append(ROOT.parent / "aitools")
    for repo in candidates:
        try:
            git(repo, "cat-file", "-e", revision + "^{commit}")
            return repo
        except (subprocess.CalledProcessError, FileNotFoundError):
            pass
    raise RuntimeError(f"Exact local object unavailable: {name}@{revision}")


def owning_path(path):
    return os.path.normcase(os.path.realpath(os.path.abspath(path)))


def validate_native(xml, assembly, minimum=1, methods=(), cases=()):
    """Require the exact owning path, exact method metadata and every fixed original case."""
    root = ET.parse(xml).getroot()
    assemblies = root.findall("assembly")
    if len(assemblies) != 1 or not assemblies[0].get("name") \
            or owning_path(assemblies[0].get("name")) != owning_path(assembly):
        raise RuntimeError("Native results do not identify the exact owning assembly")
    owning = assemblies[0]
    rows = owning.findall(".//test")
    if len(rows) < minimum or any(row.get("result") != "Pass" for row in rows) or root.findall(".//error"):
        raise RuntimeError("Failed, skipped, erroneous or insufficient native test discovery")
    if any(int(owning.get(name, "-1")) != 0 for name in ("errors", "failed", "skipped", "not-run")) \
            or int(owning.get("passed", "-1")) != len(rows) or int(owning.get("total", "-1")) != len(rows):
        raise RuntimeError("Native summary counts do not prove complete successful execution")
    identities = Counter((row.get("type"), row.get("method"), row.get("name")) for row in rows)
    if any(not all(identity) for identity in identities):
        raise RuntimeError("Native cases require stable display, type and method metadata")
    for method in methods:
        if not isinstance(method, tuple) or len(method) != 2 or not any(identity[:2] == method for identity in identities):
            raise RuntimeError(f"Original method {method} did not execute")
    for case in cases:
        if identities[(case["type"], case["method"], case["name"])] < case.get("count", 1):
            raise RuntimeError("Original case did not execute: " + case["name"])
    return rows


def stop_owned_group(process):
    """Terminate and reap only this new-session process group, including adopted descendants."""
    group = process.pid
    for sig in (signal.SIGTERM, signal.SIGKILL):
        try:
            os.killpg(group, sig)
        except ProcessLookupError:
            pass
        try:
            process.wait(timeout=0.25)
        except subprocess.TimeoutExpired:
            pass
    process.wait()
    deadline = time.monotonic() + 3
    while True:
        try:
            child, _ = os.waitpid(-group, os.WNOHANG)
        except ChildProcessError:
            return
        if child == 0:
            if time.monotonic() >= deadline:
                raise RuntimeError("Owned descendant group did not reap")
            time.sleep(0.01)


def run_owned(args, cwd, environment, output, timeout=1800):
    if not sys.platform.startswith("linux"):
        raise RuntimeError("Owned process-group verification requires the Linux subreaper contract")
    libc = ctypes.CDLL(None, use_errno=True)
    if libc.prctl(36, 1, 0, 0, 0) != 0:  # PR_SET_CHILD_SUBREAPER
        raise OSError(ctypes.get_errno(), "Cannot own descendant cleanup")
    def interrupted(signum, frame):
        raise KeyboardInterrupt(f"Owned command interrupted by signal {signum}")
    previous_term = signal.signal(signal.SIGTERM, interrupted)
    process = None
    try:
        process = subprocess.Popen(args, cwd=cwd, env=environment, stdout=output,
                                   stderr=subprocess.STDOUT, start_new_session=True)
        return process.wait(timeout=timeout)
    except subprocess.TimeoutExpired:
        output.write(b"\nVerification timeout; execution is incomplete.\n")
        return 124
    except BaseException:
        output.write(b"\nVerification interrupted; execution is incomplete.\n")
        raise
    finally:
        previous_int = signal.signal(signal.SIGINT, signal.SIG_IGN)
        signal.signal(signal.SIGTERM, signal.SIG_IGN)
        try:
            if process is not None:
                stop_owned_group(process)
        finally:
            signal.signal(signal.SIGINT, previous_int)
            signal.signal(signal.SIGTERM, previous_term)


class Experiment:
    def __init__(self, name, scratch, evidence):
        self.name = name
        self.work = scratch / name
        self.evidence = evidence / name
        self.evidence.mkdir(parents=True, exist_ok=False)
        self.work.mkdir()
        self.record = {"name": name, "status": "blocked", "commands": [], "exports": [], "runnerSha256": digest(RUNNER_SOURCE)}
        (self.evidence / "runner.py").write_bytes(RUNNER_SOURCE)
        cases = Path(__file__).with_name("deferred-dependency-case-inventories.json").read_bytes()
        (self.evidence / "deferred-dependency-case-inventories.json").write_bytes(cases)
        self.record["caseInventoriesSha256"] = digest(cases)

    def save(self):
        files = {}
        for path in self.evidence.rglob("*"):
            if path.is_file() and path.name != "result.json":
                files[str(path.relative_to(self.evidence))] = digest(path.read_bytes())
        self.record["evidenceSha256"] = files
        (self.evidence / "result.json").write_text(json.dumps(self.record, indent=2) + "\n")

    def run(self, label, args, cwd, environment=None, required=True):
        args = list(map(str, args))
        print(f"{self.name}: starting {label}", flush=True)
        started = time.monotonic()
        log = self.evidence / (label + ".log")
        with log.open("xb") as output:
            code = run_owned(args, cwd, environment, output)
        self.record["commands"].append({"label": label, "argv": args, "cwd": str(cwd),
            "environment": {key: value for key, value in (environment or {}).items()
                            if key in RECORDED_ENVIRONMENT_KEYS},
            "exitCode": code, "elapsedSeconds": round(time.monotonic() - started, 3),
            "logSha256": digest(log.read_bytes())})
        self.save()
        print(f"{self.name}: {label}: exit {code}", flush=True)
        if code and required:
            raise RuntimeError(f"{label} exited {code}; see {log.name}")
        return code

    def export(self, name, revision, destination):
        repo = owner_repository(name, revision)
        canonical = git(repo, "rev-parse", revision + "^{commit}").decode().strip()
        if canonical != revision:
            raise ValueError("An exact full revision is required")
        data = git(repo, "archive", "--format=tar", revision)
        destination.mkdir(parents=True, exist_ok=True)
        with tarfile.open(fileobj=io.BytesIO(data)) as archive:
            archive.extractall(destination, filter="data")
        # Real objects/index preserve producer identity checks in disposable exports.
        # No submodule update or nested checkout is performed.
        git(destination, "init", "-q")
        objects = Path(git(repo, "rev-parse", "--git-path", "objects").decode().strip())
        if not objects.is_absolute():
            objects = (repo / objects).resolve()
        alternates = destination / ".git/objects/info/alternates"
        alternates.write_text(str(objects) + "\n")
        git(destination, "update-ref", "HEAD", revision)
        git(destination, "read-tree", revision)
        remote = git(repo, "remote", "get-url", "origin").decode().strip()
        git(destination, "remote", "add", "origin", remote)
        print(f"{self.name}: exported {name}@{canonical}", flush=True)
        self.record["exports"].append({"owner": name, "revision": canonical,
            "sourceRepository": str(repo), "destination": str(destination), "archiveSha256": digest(data)})
        self.save()
        return destination

    def cohort(self, name, revision, selections=None, needed=None):
        repo = self.export(name, revision, self.work / name)
        selected = pins(owner_repository(name, revision), revision)
        selected.update(selections or {})
        for identity, commit in selected.items():
            if needed is None or identity in needed:
                self.export(identity, commit, repo / "references" / identity)
        self.record["selectedPins"] = selected
        return repo

    def tests(self, label, assembly, cwd, selectors=(), environment=None, minimum=1, methods=(), inventory=None):
        assembly_sha256 = digest(Path(assembly).read_bytes())
        xml = self.evidence / (label + ".xml")
        self.run(label, ["dotnet", assembly, "-failSkips", *selectors, "-result-xml", xml], cwd, environment)
        if digest(Path(assembly).read_bytes()) != assembly_sha256:
            raise RuntimeError("Executed assembly changed during native verification")
        original_cases = CASE_INVENTORIES[inventory]["cases"] if inventory else ()
        rows = validate_native(xml, assembly, minimum, methods, original_cases)
        self.record[label] = {"passed": len(rows), "failed": 0, "skipped": 0,
                              "assemblyPath": owning_path(assembly), "assemblySha256": assembly_sha256,
                              "originalCaseInventory": inventory,
                              "testNames": [row.get("name") for row in rows]}
        self.save()

    def prepare(self, repo):
        # Pre-materialize every declared root before invoking the producer. Its
        # initializer must never fetch missing roots or initialize nested modules.
        for name, revision in self.record["selectedPins"].items():
            owner_repository(name, revision)
        for name, revision in self.record["selectedPins"].items():
            destination = repo / "references" / name
            if not (destination / ".git").exists():
                self.export(name, revision, destination)
        builds = repo / "references/Hexalith.Builds"
        cli = builds / "src/libraries/Hexalith.Builds.Module.Cli/Hexalith.Builds.Module.Cli.csproj"
        self.run("producer-build", ["dotnet", "build", cli, "-c", "Debug", "-m:1", "-p:MinVerVersionOverride=1.0.0"], builds)
        tfm = json.loads((builds / "global.json").read_text())["sdk"]["version"].split('.')[0]
        assembly = cli.parent / f"bin/Debug/net{tfm}.0/Hexalith.Builds.Module.Cli.dll"
        # Preparation uses a generated declaration input at the exact cohort root.
        # Active identity must name that root; catalogue generation below still uses
        # the unchanged Conversations-owned declaration from the selected mapping.
        declaration = repo / "verification.module-manifest.v2.json"
        original = repo / "references/Hexalith.Conversations/module/hexalith.module-manifest.v2.json"
        declaration_input = json.loads(original.read_text())
        declaration_input["modules"][0]["identity"]["moduleId"] = repo.name.removeprefix("Hexalith.").lower()
        declaration.write_text(json.dumps(declaration_input, indent=2) + "\n")
        self.record["preparationDeclarationSha256"] = digest(declaration.read_bytes())
        shutil.copyfile(declaration, self.evidence / "preparation-declaration.json")
        workspace = self.work / "prepared"
        self.run("prepare-build", ["dotnet", assembly, "prepare-build", "--manifest", declaration,
                                   "--mode", "source", "--output", "json", "--workspace", workspace], repo)
        prepared = json.loads((self.evidence / "prepare-build.log").read_text())
        prepared = prepared.get("buildPreparation", prepared)
        (self.evidence / "preparation.json").write_text(json.dumps(prepared, indent=2) + "\n")
        shutil.copytree(workspace / "source-mapping", self.evidence / "source-mapping", dirs_exist_ok=True)
        environment = os.environ.copy()
        environment.update(prepared["environment"])
        self.record["producerMappingSha256"] = digest((workspace / "source-mapping/mapping.json").read_bytes())
        return workspace, prepared, environment


def static_compiler_inputs(repo, project, evaluated):
    """Fingerprint static compiler inputs, including applicable ancestor editor settings."""
    files = {}
    def capture(path):
        path = Path(path).resolve()
        key = str(path.relative_to(repo)) if path.is_relative_to(repo) else str(path)
        files[key] = digest(path.read_bytes()) if path.is_file() else "MISSING"
    for kind in ("Compile", "EmbeddedResource", "Content", "None", "Analyzer", "AdditionalFiles", "EditorConfigFiles", "GlobalAnalyzerConfigFiles"):
        for item in evaluated["Items"].get(kind, []):
            path = Path(item.get("FullPath") or project.parent / item["Identity"])
            if kind in ("Analyzer", "AdditionalFiles", "EditorConfigFiles", "GlobalAnalyzerConfigFiles") \
                    or not any(part in ("bin", "obj") for part in path.parts):
                capture(path)
    # EditorConfig is discovered per source path, including linked source files.
    for item in evaluated["Items"].get("Compile", []):
        source = Path(item.get("FullPath") or project.parent / item["Identity"]).resolve()
        for parent in (source.parent, *source.parent.parents):
            config = parent / ".editorconfig"
            if config.is_file():
                capture(config)
                preamble = re.split(r"(?m)^\s*\[", config.read_text(), maxsplit=1)[0]
                if re.search(r"(?im)^\s*root\s*=\s*true\s*$", preamble):
                    break
    return files


def collect_graph_assets(repo, inventory, configuration):
    """Read only the assets selected by this configuration's evaluated project graph."""
    captured = {}
    for relative, project in inventory.items():
        evaluation = project["evaluation"]
        if evaluation["configuration"] != configuration:
            raise RuntimeError("Asset capture configuration differs from evaluated graph")
        path = Path(evaluation["assetsPath"])
        document = json.loads(path.read_text())
        if owning_path(document["project"]["restore"]["projectPath"]) != owning_path(repo / relative):
            raise RuntimeError("Asset owner differs from evaluated project")
        captured[relative] = {"assetsPath": str(path), "sha256": digest(path.read_bytes()),
            "configuration": configuration, "libraries": document["libraries"],
            "projectFileDependencyGroups": document.get("projectFileDependencyGroups"),
            "targets": {name: list(target) for name, target in document["targets"].items()}}
    return captured


def compare_consumed_inputs(selected, released):
    """Report the exact reviewed formatting-only delta; every other static input remains exact."""
    reviewed = []
    if not selected or set(selected) != set(released):
        return False, reviewed
    for project in selected:
        left, right = dict(selected[project]), dict(released[project])
        left_files, right_files = dict(left["files"]), dict(right["files"])
        pair = (left_files.get(REVIEWED_EDITOR_CONFIG), right_files.get(REVIEWED_EDITOR_CONFIG))
        if pair[0] != pair[1]:
            if pair != REVIEWED_EDITOR_HASHES:
                return False, reviewed
            reviewed.append({"project": project, "path": REVIEWED_EDITOR_CONFIG,
                "selectedSha256": pair[0], "releasedSha256": pair[1],
                "scope": "Only LF save rules for the config itself, Git attributes and non-C# governance files differ; C# rules are unchanged."})
            del left_files[REVIEWED_EDITOR_CONFIG]
            del right_files[REVIEWED_EDITOR_CONFIG]
        left["files"], right["files"] = left_files, right_files
        if left != right:
            return False, reviewed
    return True, reviewed


def graph(experiment, repo, label, configuration="Debug", artifacts=None):
    artifacts = artifacts or experiment.work / "graph-artifacts"
    entry = repo / "src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj"
    properties = ["-p:Configuration=" + configuration,
        "-p:UseHexalithProjectReferences=" + ("true" if configuration == "Debug" else "false"),
        "-p:ArtifactsPath=" + str(artifacts)]
    experiment.run(label + "-restore", ["dotnet", "restore", entry, *properties], repo)
    pending = [entry]
    inventory = {}
    while pending:
        project = pending.pop().resolve()
        identity = str(project.relative_to(repo))
        if identity in inventory:
            continue
        tag = label + "-graph-" + project.stem
        experiment.run(tag, ["dotnet", "msbuild", project, "-nologo", *properties,
            "-target:ResolveLockFileAnalyzers",
            "-getProperty:ProjectAssetsFile,TargetDir,TargetPath,Configuration,DefineConstants,LangVersion,Nullable,AnalysisLevel,RunAnalyzersDuringBuild,EnableNETAnalyzers",
            "-getItem:Compile,EmbeddedResource,Content,None,Analyzer,AdditionalFiles,EditorConfigFiles,GlobalAnalyzerConfigFiles,ProjectReference,PackageReference,PackageVersion"], repo)
        evaluated = json.loads((experiment.evidence / (tag + ".log")).read_text())
        files = static_compiler_inputs(repo, project, evaluated)
        preprocessed = experiment.evidence / (tag + "-expanded.xml")
        experiment.run(tag + "-imports", ["dotnet", "msbuild", project, "-nologo", *properties,
            "-preprocess:" + str(preprocessed)], repo)
        imported_paths = re.findall(r"^(/[^\n<>]+\.(?:props|targets|csproj))\s*$", preprocessed.read_text(), re.MULTILINE)
        for imported in [str(project), *imported_paths]:
            path = Path(imported.strip())
            if path.is_file():
                key = str(path.relative_to(repo)) if path.is_relative_to(repo) else str(path)
                files[key] = digest(path.read_bytes())
        values = evaluated["Properties"]
        inventory[identity] = {"files": files, "compilerProperties": {key: value for key, value in values.items()
            if key not in ("ProjectAssetsFile", "TargetDir", "TargetPath")},
            "evaluation": {"assetsPath": values["ProjectAssetsFile"], "configuration": values["Configuration"],
                           "targetDir": values["TargetDir"], "targetPath": values["TargetPath"]}, "packages": [
            {key: item.get(key) for key in ("Identity", "Version", "VersionOverride", "IncludeAssets", "ExcludeAssets", "PrivateAssets")}
            for kind in ("PackageReference", "PackageVersion") for item in evaluated["Items"].get(kind, [])]}
        for item in evaluated["Items"].get("ProjectReference", []):
            target = Path(item["FullPath"]).resolve()
            if not target.is_relative_to(repo):
                raise RuntimeError("Consumed graph escaped the exported cohort")
            pending.append(target)
    (experiment.evidence / (label + "-graph.json")).write_text(json.dumps(inventory, indent=2) + "\n")
    return inventory


def current(experiment):
    repo = experiment.cohort("Hexalith.Agents", CURRENT)
    selected = graph(experiment, repo, "selected")
    eventstore = repo / "references/Hexalith.EventStore"
    shutil.rmtree(eventstore)
    experiment.export("Hexalith.EventStore", RELEASED, eventstore)
    released = graph(experiment, repo, "released")
    experiment.record["completeConsumedGraphsEqual"] = selected == released
    compatible, reviewed = compare_consumed_inputs(selected, released)
    experiment.record["reviewedConsumedInputsCompatible"] = compatible
    experiment.record["reviewedEditorConfigurationDifferences"] = reviewed
    experiment.record["graphProjectCount"] = len(selected)
    shutil.rmtree(eventstore)
    experiment.export("Hexalith.EventStore", EVENTSTORE, eventstore)
    project = "src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj"
    for configuration in ("Debug", "Release"):
        artifacts = experiment.work / "integration-artifacts" / configuration
        experiment.run("integration-" + configuration, ["dotnet", "build", project, "-c", configuration,
                       "--artifacts-path", artifacts, "-m:1", "-p:MinVerVersionOverride=1.0.0"], repo)
        resolved = graph(experiment, repo, "resolved-" + configuration, configuration, artifacts)
        captured = collect_graph_assets(repo, resolved, configuration)
        (experiment.evidence / ("resolved-assets-" + configuration + ".json")).write_text(json.dumps(captured, indent=2) + "\n")
        outputs = Path(resolved[project]["evaluation"]["targetDir"])
        (experiment.evidence / ("integration-output-hashes-" + configuration + ".json")).write_text(json.dumps({
            str(path.relative_to(outputs)): digest(path.read_bytes()) for path in outputs.rglob("*") if path.is_file()}, indent=2) + "\n")
    experiment.run("package-floor", ["pwsh", "-NoProfile", "-File", "eng/verify-eventstore-package-floor.ps1"], repo)
    server = "test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj"
    # Preserve the default broad source failure independently of mapped integration.
    experiment.run("server-default-source", ["dotnet", "build", server, "-c", "Debug", "-m:1",
                   "-p:MinVerVersionOverride=1.0.0"], repo, required=False)
    experiment.run("server-packages", ["dotnet", "build", server, "-c", "Release",
                   "-m:1", "-p:MinVerVersionOverride=1.0.0"], repo)
    experiment.tests("gateway-Release",
        repo / "test/Hexalith.Agents.Server.Tests/bin/Release/net11.0/Hexalith.Agents.Server.Tests.dll", repo,
        ["-class", "Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests"], inventory="gateway-package")
    _, _, environment = experiment.prepare(repo)
    experiment.run("server-mapped-source", ["dotnet", "build", server, "-c", "Debug", "-m:1",
                   "-p:MinVerVersionOverride=1.0.0"], repo, environment)
    experiment.tests("gateway-Debug",
        repo / "test/Hexalith.Agents.Server.Tests/bin/Debug/net11.0/Hexalith.Agents.Server.Tests.dll", repo,
        ["-class", "Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests"], environment, inventory="gateway-source")
    if not experiment.record["reviewedConsumedInputsCompatible"]:
        raise RuntimeError("Consumed inputs differ beyond the exact reviewed formatting-only configuration; reconciliation requires analysis")
    experiment.record["status"] = "passed"


def historical(experiment):
    old_pins = pins(ROOT, PREVIOUS)
    reviewed_pins = pins(ROOT, REVIEWED)
    outcomes = {}
    for selection, selected in (("reviewed", reviewed_pins), ("previous-five", old_pins)):
        lane = Experiment(selection, experiment.work, experiment.evidence)
        try:
            overrides = {"Hexalith." + name: selected["Hexalith." + name] for name in FIVE}
            overrides["Hexalith.EventStore"] = HISTORICAL_EVENTSTORE
            repo = lane.cohort("Hexalith.Agents", REVIEWED, overrides)
            lane.record["onlyVariedOwners"] = ["Hexalith." + name for name in FIVE]
            lane.run("integration-build", ["dotnet", "build", "test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj",
                     "-c", "Debug", "-m:1", "-p:MinVerVersionOverride=1.0.0"], repo)
            lane.tests("gateway", repo / "test/Hexalith.Agents.Server.Tests/bin/Debug/net10.0/Hexalith.Agents.Server.Tests.dll",
                       repo, ["-class", "Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests"], inventory="historical-gateway")
            lane.record["status"] = "passed"
        except Exception as error:
            lane.record["blocker"] = str(error)
        lane.save()
        outcomes[selection] = lane.record["status"]
    experiment.record["outcomes"] = outcomes
    if set(outcomes.values()) == {"passed"}:
        experiment.record["status"] = "passed"
        experiment.record["conclusion"] = "Both exact historical integrations execute; the five reviewed pin changes were not necessary for this selected integration. No current rollback is implied."
    else:
        experiment.record["blocker"] = "At least one historical lane did not execute; pin necessity is unresolved."


def apply_platform_overlays(experiment, repo):
    """Bind explicit local corrections to the immutable exported Platform baseline."""
    changes = []
    for relative, source in PLATFORM_OVERLAYS.items():
        target = repo / relative
        baseline = git(owner_repository("Hexalith.Platform", PLATFORM), "show", PLATFORM + ":" + relative)
        actual = target.read_bytes()
        if actual != baseline:
            raise RuntimeError("Platform overlay predecessor is not its exact archived baseline: " + relative)
        corrected = source.read_bytes()
        retained = experiment.evidence / "local-inputs/Hexalith.Platform" / relative
        retained.parent.mkdir(parents=True, exist_ok=True)
        retained.write_bytes(corrected)
        target.write_bytes(corrected)
        changes.append({"path": relative, "sourcePath": str(source), "baselineRevision": PLATFORM,
                        "baselineSha256": digest(baseline), "correctedSha256": digest(corrected),
                        "retainedInput": str(retained.relative_to(experiment.evidence))})
    experiment.record["platformSourceInputs"] = {"baselineRevision": PLATFORM,
        "pristineBaseline": not changes, "explicitLocalOverlays": changes}
    experiment.save()


def platform(experiment):
    needed = {"Hexalith." + name for name in ("Builds", "EventStore", "Tenants", "Memories", "Parties",
              "Commons", "McpCli", "PolymorphicSerializations", "Conversations", "FrontComposer", "AI.Tools")}
    repo = experiment.cohort("Hexalith.Platform", PLATFORM, needed=needed)
    apply_platform_overlays(experiment, repo)
    project = "tests/Hexalith.Platform.Composition.Tests/Hexalith.Platform.Composition.Tests.csproj"
    # All owner paths are explicit; these are the Platform-recorded owners, never Agents umbrella pins.
    properties = [f"-p:Hexalith{name.removeprefix('Hexalith.').replace('.', '')}Root={repo / 'references' / name}"
                  for name in experiment.record["selectedPins"] if name != "Hexalith.AI.Tools"]
    common = ["dotnet", "build", project, "-c", "Debug", "-m:1", "-p:MinVerVersionOverride=1.0.0", *properties]
    default = experiment.run("default-source-build", common, repo, required=False)
    assembly = repo / "tests/Hexalith.Platform.Composition.Tests/bin/Debug/net11.0/Hexalith.Platform.Composition.Tests.dll"
    if default == 0:
        experiment.tests("default-related", assembly, repo,
            ["-class", "Hexalith.Platform.Composition.Tests.DomainEnrollmentBindingTests", "-class", "Hexalith.Platform.Composition.Tests.PartiesAdoptionTests"],
            minimum=8, methods=ORIGINAL_METHODS, inventory="platform-related")
    if PLATFORM_OVERLAYS:
        experiment.run("source-propagation-controls", ["python3", "eng/test_source_sdk_propagation.py",
            "--evidence", experiment.evidence / "source-propagation-controls"], repo)
    workspace, prepared, environment = experiment.prepare(repo)
    inputs = {"producerMappingPath": str(workspace / "source-mapping/mapping.json"), "producerMappingHash": prepared["mappingHash"],
              "mappingPath": str(workspace / "source-mapping/mapping.json"), "mappingHash": prepared["mappingHash"],
              "evidenceRoot": str(experiment.evidence / "catalog")}
    inputs_path = experiment.evidence / "catalog-inputs.json"
    inputs_path.write_text(json.dumps(inputs, indent=2) + "\n")
    environment["HEXALITH_CONVERSATIONS_CATALOG_INPUTS"] = str(inputs_path)
    experiment.run("mapped-source-build", [*common, "-p:ConversationsCatalogSourceVerification=true"], repo, environment)
    experiment.tests("mapped-catalog", assembly, repo,
        ["-class", "Hexalith.Platform.Composition.Tests.ConversationsEnrollmentTests"], environment, minimum=38, inventory="platform-catalog")
    experiment.tests("mapped-related", assembly, repo,
        ["-class", "Hexalith.Platform.Composition.Tests.DomainEnrollmentBindingTests", "-class", "Hexalith.Platform.Composition.Tests.PartiesAdoptionTests"],
        environment, minimum=8, methods=ORIGINAL_METHODS, inventory="platform-related")
    if default != 0:
        raise RuntimeError("Default exact Platform source graph remains blocked; mapped evidence is separate")
    experiment.record["status"] = "passed"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--lane", choices=("all", "current", "historical", "platform"), default="all")
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--owner-root", action="append", default=[], metavar="Hexalith.Owner=/path/to/repository",
                        help="Use an explicitly supplied local repository for one exact owner object; repeat as needed")
    parser.add_argument("--platform-overlay", action="append", default=[], metavar="relative/path=/absolute/local/file",
                        help="Bind one explicit local Platform input over the exact archived baseline; repeat as needed")
    args = parser.parse_args()
    for selection in args.platform_overlay:
        relative, separator, source = selection.partition("=")
        destination = Path(relative)
        if not separator or destination.is_absolute() or ".." in destination.parts or not destination.parts \
                or destination.parts[0] in (".git", "references") or not Path(source).is_file() or relative in PLATFORM_OVERLAYS:
            parser.error("platform-overlay requires a unique repository-relative path=/existing/local/file")
        PLATFORM_OVERLAYS[relative] = Path(source).resolve()
    for selection in args.owner_root:
        name, separator, path = selection.partition("=")
        if not separator or not name.startswith("Hexalith.") or not Path(path).is_dir() or name in OWNER_ROOTS:
            parser.error("owner-root requires a unique Hexalith.Owner=/existing/repository")
        OWNER_ROOTS[name] = Path(path).resolve()
    if not os.environ.get("TMPDIR"):
        parser.error("run under run-with-temp to provide a private disposable TMPDIR")
    scratch = Path(os.environ["TMPDIR"]).resolve() / "deferred-dependencies"
    evidence = args.evidence.resolve()
    if evidence.is_relative_to(scratch.parent):
        parser.error("evidence must survive outside TMPDIR")
    evidence.mkdir(parents=True, exist_ok=False)
    scratch.mkdir()
    results = {}
    for name, execute in (("current", current), ("historical", historical), ("platform", platform)):
        if args.lane not in ("all", name):
            continue
        experiment = Experiment(name, scratch, evidence)
        try:
            execute(experiment)
        except Exception as error:
            experiment.record["blocker"] = str(error)
        finally:
            experiment.save()
        results[name] = experiment.record
    (evidence / "dependency-results.json").write_text(json.dumps(results, indent=2) + "\n")
    return 0 if all(result["status"] == "passed" for result in results.values()) else 1


if __name__ == "__main__":
    raise SystemExit(main())
