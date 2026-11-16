#!/usr/bin/env python3
"""Restore and qualify isolated consumers of the exact Agents package inventory."""
from __future__ import annotations
import argparse,hashlib,json,os,shutil,subprocess,sys,tempfile
from pathlib import Path
from consumer_package_validation import *
from release_package_integrity import expected_archives
ROOT = Path(__file__).resolve().parents[1]

def run(command: list[str], cwd: Path, packages: Path) -> None:
    """Run a consumer command with a fresh global packages directory."""
    environment = {**os.environ, "NUGET_PACKAGES": str(packages)}
    subprocess.run(command, cwd=cwd, env=environment, check=True)


def build_process_observer(directory: Path) -> Path:
    """Compile and probe kernel process tracing before restoring or building a consumer."""
    check_consumer_prerequisites()
    observer = directory / "obj/process-observer"
    if observer.exists():
        return observer
    observer.parent.mkdir(parents=True, exist_ok=True)
    subprocess.run(["cc", "-O2", "-Wall", "-Wextra", "-Werror", str(ROOT / "scripts/consumer-process-observer.c"),
        "-o", str(observer)], check=True)
    probe = subprocess.run([str(observer), str(observer.parent / "process-probe.log"),
        str(observer.parent / "native-probe"), "/bin/true", "probe"], capture_output=True, text=True)
    if probe.returncode:
        raise ValueError("consumer qualification requires permitted Linux ptrace process tracing: " + probe.stderr)
    return observer


def build_load_observer(directory: Path, packages: Path) -> Path:
    """Compile the repository-owned observer without package dependencies or shared compiler state."""
    build_process_observer(directory)
    observer = directory / "obj/load-observer"
    observer.mkdir(parents=True)
    shutil.copyfile(ROOT / "scripts/StartupHook.cs", observer / "StartupHook.cs")
    project = observer / "Observer.csproj"
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
        '<TargetFramework>net11.0</TargetFramework><Nullable>enable</Nullable>'
        '<UseSharedCompilation>false</UseSharedCompilation><NuGetAudit>true</NuGetAudit>'
        '</PropertyGroup></Project>', encoding="utf-8")
    run(["dotnet", "build", str(project), "-c", "Release", "/m:1", "/nr:false"], directory, packages)
    return observer / "bin/Release/net11.0/Observer.dll"


def observe_build(project: Path, directory: Path, packages: Path, observer: Path) -> dict:
    """Observe every fresh MSBuild/compiler process; missing or incomplete observation is a refusal."""
    check_consumer_prerequisites()
    tracer = build_process_observer(directory)
    traces = Path(tempfile.mkdtemp(prefix="assembly-loads-", dir=directory / "obj"))
    environment = {**os.environ, "NUGET_PACKAGES": str(packages), "DOTNET_STARTUP_HOOKS": os.pathsep.join(filter(None, [os.environ.get("DOTNET_STARTUP_HOOKS"), str(observer)])),
        "AGENTS_CONSUMER_LOAD_TRACE": str(traces), "DOTNET_CLI_USE_MSBUILD_SERVER": "0", "MSBUILDDISABLENODEREUSE": "1"}
    environment.pop("LD_DEBUG", None)
    environment.pop("LD_DEBUG_OUTPUT", None)
    common = ["dotnet", "msbuild", str(project), "-nologo", "-p:Configuration=Release",
        "-p:UseSharedCompilation=false", "/m:1", "/nr:false"]
    expanded = directory / "obj/observed-imports.xml"
    def execute(arguments):
        result = subprocess.run([str(tracer), str(traces / "processes.log"), str(traces / "native"), *arguments],
            cwd=directory, env=environment, capture_output=True, text=True)
        if result.returncode:
            print(result.stdout + result.stderr, file=sys.stderr)
            raise subprocess.CalledProcessError(result.returncode, result.args)
        return result
    with immutable_package_inputs(packages):
        execute([*common, f"-preprocess:{expanded}"])
        built = execute([*common, "-target:Build", "-verbosity:quiet", "-getItem:Analyzer"])
    observation = read_load_observation(traces, observer)
    observation["analyzers"] = [item["Identity"] for item in json.loads(built.stdout)["Items"]["Analyzer"]]
    observation["imports"] = active_import_paths(expanded.read_text())
    observation["processObserverSha256"] = hashlib.sha256(tracer.read_bytes()).hexdigest()
    return observation


def validate_consumer(package_directory: Path, identities: list[tuple[str, str]], all_identities: list[tuple[str, str]],
                      label: str) -> dict:
    """Restore and compile one independent consumer with the repository-pinned SDK."""
    check_consumer_prerequisites()
    with tempfile.TemporaryDirectory(prefix="agents-package-consumer-") as directory_name:
        directory = Path(directory_name)
        prepare_consumer(directory, package_directory, identities, all_identities)
        build_process_observer(directory)
        project = directory / "PackageConsumer.csproj"
        config = directory / "NuGet.config"
        packages = directory / "packages"
        run(
            [
                "dotnet",
                "restore",
                str(project),
                "--configfile",
                str(config),
                "--no-cache",
                "--force-evaluate",
                "/m:1",
                "/nr:false",
            ],
            directory,
            packages,
        )
        observer = build_load_observer(directory, packages)
        observation = observe_build(project, directory, packages, observer)
        validate_consumer_assets(directory, package_directory, identities, all_identities)
        assets = json.loads((directory / "obj" / "project.assets.json").read_text(encoding="utf-8"))
        analyzers = observation["analyzers"]
        imports = observation["imports"]
        return {"consumer": label, **executable_asset_receipt(assets, packages, analyzers, imports, directory, observation)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path)
    parser.add_argument("--receipt", type=Path, default=ROOT / "artifacts/consumer-executable-assets-receipt.json")
    parser.add_argument("--no-receipt", action="store_true", help="validate without retaining consumer evidence")
    parser.add_argument("--write-baseline", type=Path, help="write a candidate for explicit maintainer review")
    parser.add_argument("--policy", type=Path, default=ROOT / "eng/consumer-executable-assets.json")
    args = parser.parse_args()
    package_directory = args.package_directory.resolve()
    subprocess.run([sys.executable, "scripts/validate-nuget-packages.py", str(package_directory)],
                   cwd=ROOT, check=True)
    identities = sorted(package_identity(path) for path in package_directory.glob("*.nupkg"))
    versions = {version for _, version in identities}
    if len(versions) != 1:
        raise ValueError(f"package versions differ: {sorted(versions)}")
    expected_archives(package_directory, tuple(package_id for package_id, _ in identities), next(iter(versions)))
    receipts = [validate_consumer(package_directory, [identity], identities, identity[0]) for identity in identities]
    receipts.append(validate_consumer(package_directory, identities, identities, "combined"))
    receipt = {"schemaVersion": 2, "sdk": json.loads((ROOT / "global.json").read_text())["sdk"],
        "sdkVersion": subprocess.run(["dotnet", "--version"], cwd=ROOT, check=True,
            capture_output=True, text=True).stdout.strip(),
        "packages": [{"id": package_id, "version": version,
            "sha256": hashlib.sha256((package_directory / f"{package_id}.{version}.nupkg").read_bytes()).hexdigest()}
            for package_id, version in identities], "consumers": receipts}
    if not args.no_receipt:
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    produced_version = next(iter(versions))
    policy = {"schemaVersion": 2, "consumers": {result["consumer"]: policy_selection(result["selected"], produced_version)
              for result in receipts}, "selected": [json.loads(entry) for entry in sorted({
        json.dumps(entry, sort_keys=True) for result in receipts for entry in policy_selection(result["selected"], produced_version)})]}
    if args.write_baseline:
        args.write_baseline.write_text(json.dumps(policy, indent=2) + "\n", encoding="utf-8")
        print(f"Wrote candidate executable asset baseline for explicit review: {args.write_baseline}")
    else:
        enforce_asset_policy(receipts, json.loads(args.policy.read_text()), produced_version)
    print(f"Validated {len(receipts)} isolated consumers at {next(iter(versions))}.")
    return 0

if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - the CLI must emit the exact gate failure.
        print(f"validate-consumer-package-references: {error}", file=sys.stderr)
        raise SystemExit(1)
