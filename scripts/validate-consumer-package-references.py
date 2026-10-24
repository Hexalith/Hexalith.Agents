#!/usr/bin/env python3
"""Restore and build an isolated consumer of the exact Agents package inventory."""

from __future__ import annotations

import argparse
import hashlib
import shutil
import json
import ntpath
import os
import re
import subprocess
import sys
import tempfile
import textwrap
import zipfile
from pathlib import Path
from urllib.parse import unquote, urlparse
from xml.etree import ElementTree
from xml.sax.saxutils import quoteattr


ROOT = Path(__file__).resolve().parents[1]


def package_identity(path: Path) -> tuple[str, str]:
    """Return package id and version from one archive."""
    with zipfile.ZipFile(path) as archive:
        nuspec = next(name for name in archive.namelist() if name.endswith(".nuspec"))
        root = ElementTree.fromstring(archive.read(nuspec))
        values = {element.tag.rsplit("}", 1)[-1]: (element.text or "").strip() for element in root.iter()}
        return values["id"], values["version"]


def run(command: list[str], cwd: Path, packages: Path) -> None:
    """Run a consumer command with a fresh global packages directory."""
    environment = {**os.environ, "NUGET_PACKAGES": str(packages)}
    subprocess.run(command, cwd=cwd, env=environment, check=True)


def json_strings(value: object):
    """Yield every JSON key and string value so path disclosures cannot hide in either."""
    if isinstance(value, dict):
        for key, item in value.items():
            yield str(key)
            yield from json_strings(item)
    elif isinstance(value, list):
        for item in value:
            yield from json_strings(item)
    elif isinstance(value, str):
        yield value


def normalize_disclosure_path(value: str) -> str:
    """Normalize URI escaping, Windows separators/case, and existing symlink components."""
    decoded = unquote(value.strip())
    if decoded.casefold().startswith("file:"):
        parsed = urlparse(decoded)
        decoded = parsed.path or parsed.netloc
    decoded = decoded.replace("\\", "/")
    if re.match(r"^[A-Za-z]:/", decoded):
        return ntpath.normcase(ntpath.normpath(decoded.replace("/", "\\"))).replace("\\", "/")
    try:
        return str(Path(decoded).resolve(strict=False)).replace("\\", "/").casefold()
    except (OSError, ValueError):
        return os.path.normcase(os.path.normpath(decoded)).replace("\\", "/").casefold()


def active_import_paths(expanded: str) -> list[str]:
    """Read MSBuild's active import filename comment lines, without extension filtering."""
    return [line.strip() for line in expanded.splitlines()
        if Path(line.strip()).is_absolute() and Path(line.strip()).is_file()]


def executable_asset_receipt(assets: dict, packages: Path, analyzers: list[str], imports: list[str], consumer_root: Path | None = None) -> dict:
    """Separate selected compiler/import inputs from merely available package archive members."""
    owned = []
    available = []
    for identity, library in sorted(assets["libraries"].items()):
        if library.get("type") != "package":
            continue
        package_id, version = identity.rsplit("/", 1)
        package_root = (packages / library["path"]).resolve()
        owned.append((package_root, package_id, version))
        for name in library.get("files", []):
            if name.startswith(("analyzers/", "build/", "buildTransitive/", "buildMultiTargeting/")):
                path = package_root / name
                if path.is_file():
                    available.append({"package": package_id, "version": version, "path": name,
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
    selected, sdk, sdk_imports, generated = [], [], [], []
    dotnet_root = Path(shutil.which("dotnet") or "dotnet").resolve().parent
    for kind, paths in (("analyzer", analyzers), ("import", imports)):
        for value in sorted(set(paths)):
            path = Path(value).resolve()
            if not path.is_file():
                raise ValueError(f"selected {kind} does not exist: {path}")
            for package_root, package_id, version in owned:
                if path.is_relative_to(package_root):
                    selected.append({"kind": kind, "package": package_id, "version": version,
                        "path": path.relative_to(package_root).as_posix(),
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()})
                    break
            else:
                if any(path.is_relative_to(dotnet_root / area) for area in ("sdk", "packs", "sdk-manifests")):
                    entry = {"path": path.relative_to(dotnet_root).as_posix(),
                        "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}
                    (sdk if kind == "analyzer" else sdk_imports).append(entry)
                elif (kind == "import" and consumer_root is not None
                        and ((path.parent == (consumer_root / "obj").resolve()
                              and path.name.endswith((".csproj.nuget.g.props", ".csproj.nuget.g.targets")))
                             or path == (consumer_root / "Directory.Packages.props").resolve()
                             or path == (consumer_root / "PackageConsumer.csproj").resolve()
                             or path == (consumer_root / "Consumer.csproj").resolve())):
                    generated.append({"path": path.relative_to(consumer_root).as_posix()})
                else:
                    raise ValueError(f"unowned executable consumer asset: {path}")
    key = lambda entry: json.dumps(entry, sort_keys=True)
    return {"selected": sorted(selected, key=key), "available": sorted(available, key=key),
            "sdkAnalyzers": sorted(sdk, key=key), "sdkImports": sorted(sdk_imports, key=key),
            "generatedImports": sorted(generated, key=key)}


def enforce_asset_policy(receipts: list[dict], policy: dict) -> None:
    """Fail closed on unreviewed selected package code, regardless of nuspec exclusion metadata."""
    actual = sorted({json.dumps(entry, sort_keys=True) for receipt in receipts for entry in receipt["selected"]})
    expected = sorted(json.dumps(entry, sort_keys=True) for entry in policy.get("selected", []))
    per_consumer = {receipt["consumer"]: receipt["selected"] for receipt in receipts}
    if policy.get("schemaVersion") != 1 or actual != expected or per_consumer != policy.get("consumers"):
        raise ValueError("consumer executable asset baseline changed; review package/version/path/SHA-256 "
            f"before updating eng/consumer-executable-assets.json; added={sorted(set(actual)-set(expected))}; "
            f"removed={sorted(set(expected)-set(actual))}")


def validate_consumer(package_directory: Path, identities: list[tuple[str, str]], all_identities: list[tuple[str, str]],
                      label: str) -> dict:
    """Restore and compile one independent consumer with the repository-pinned SDK."""
    with tempfile.TemporaryDirectory(prefix="agents-package-consumer-") as directory_name:
        directory = Path(directory_name)
        shutil.copyfile(ROOT / "global.json", directory / "global.json")
        package_versions = "\n".join(
            f'    <PackageVersion Include="{package_id}" Version="{version}" />'
            for package_id, version in all_identities
        )
        package_references = "\n".join(
            f'    <PackageReference Include="{package_id}" />' for package_id, _ in identities
        )
        (directory / "Directory.Packages.props").write_text(
            "<Project>\n  <PropertyGroup>\n    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>\n"
            "  </PropertyGroup>\n  <ItemGroup>\n"
            f"{package_versions}\n  </ItemGroup>\n</Project>\n",
            encoding="utf-8",
        )
        project = directory / "PackageConsumer.csproj"
        project.write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TargetFramework>net10.0</TargetFramework>\n"
            "    <Nullable>enable</Nullable>\n    <ImplicitUsings>enable</ImplicitUsings>\n"
            "    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n  </PropertyGroup>\n  <ItemGroup>\n"
            f"{package_references}\n  </ItemGroup>\n</Project>\n",
            encoding="utf-8",
        )
        probes = {
            "Hexalith.Agents": "Hexalith.Agents.AgentsAssemblyMarker",
            "Hexalith.Agents.Client": "Hexalith.Agents.Client.AgentsClientAssemblyMarker",
            "Hexalith.Agents.Contracts": "Hexalith.Agents.Contracts.Agent.AgentLifecycleStatus",
            "Hexalith.Agents.EventStore": "Hexalith.Agents.EventStore.AgentsEventStoreServiceCollectionExtensions",
            "Hexalith.Agents.Testing": "Hexalith.Agents.Testing.AgentsTestingAssemblyMarker",
            "Hexalith.Agents.UI": "Hexalith.Agents.UI.AgentsUIAssemblyMarker",
        }
        types = ",".join(f"typeof({probes[package_id]})" for package_id, _ in identities)
        (directory / "ConsumerProbe.cs").write_text(
            "namespace Hexalith.Agents.PackageConsumer; public static class ConsumerProbe { "
            f"public static System.Type[] PublicPackageTypes {{ get; }} = [{types}];" + " }", encoding="utf-8")
        config = directory / "NuGet.config"
        local_patterns = "\n".join(
            f'                  <package pattern="{package_id}" />' for package_id, _ in all_identities
        )
        config.write_text(
            textwrap.dedent(f"""\
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="agents" value={quoteattr(str(package_directory))} />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <packageSource key="agents">
{local_patterns}
                </packageSource>
                <packageSource key="nuget.org"><package pattern="*" /></packageSource>
              </packageSourceMapping>
            </configuration>
            """),
            encoding="utf-8",
        )

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
        run(["dotnet", "build", str(project), "--no-restore", "-c", "Release", "/m:1", "/nr:false"], directory, packages)

        assets_path = directory / "obj" / "project.assets.json"
        assets_text = assets_path.read_text(encoding="utf-8")
        assets = json.loads(assets_text)
        project_libraries = sorted(
            name for name, value in assets["libraries"].items() if value.get("type") == "project"
        )
        if project_libraries:
            raise ValueError(f"isolated consumer resolved repository projects: {project_libraries}")

        expected_agent_libraries = {f"{package_id}/{version}".casefold() for package_id, version in identities}
        actual_agent_libraries = {
            name.casefold()
            for name, value in assets["libraries"].items()
            if value.get("type") == "package" and name.casefold().startswith("hexalith.agents/")
            or value.get("type") == "package" and name.casefold().startswith("hexalith.agents.")
        }
        if not expected_agent_libraries <= actual_agent_libraries or not actual_agent_libraries <= {
            f"{package_id}/{version}".casefold() for package_id, version in all_identities
        }:
            raise ValueError(
                "isolated consumer Agents package identity mismatch; "
                f"missing={sorted(expected_agent_libraries - actual_agent_libraries)}; "
                f"unexpected={sorted(actual_agent_libraries - expected_agent_libraries)}"
            )

        expected_source = normalize_disclosure_path(str(package_directory))
        for package_id, version in all_identities:
            if f"{package_id}/{version}".casefold() not in actual_agent_libraries:
                continue
            metadata_path = packages / package_id.casefold() / version / ".nupkg.metadata"
            if not metadata_path.is_file():
                raise ValueError(f"missing NuGet source metadata for local package {package_id}/{version}")
            source = json.loads(metadata_path.read_text(encoding="utf-8")).get("source", "")
            if normalize_disclosure_path(source) != expected_source:
                raise ValueError(
                    f"{package_id}/{version} did not resolve unambiguously from the local Agents feed: {source!r}"
                )

        disclosed_source_roots = [
            normalize_disclosure_path(str(ROOT / "src")),
            normalize_disclosure_path(str(ROOT / "test")),
            normalize_disclosure_path(str(ROOT / "references")),
        ]
        disclosed = sorted(
            {
                candidate
                for value in json_strings(assets)
                for candidate in [normalize_disclosure_path(value)]
                if any(candidate == root or candidate.startswith(f"{root}/") for root in disclosed_source_roots)
            }
        )
        if disclosed:
            raise ValueError(f"isolated consumer assets disclose repository source paths: {disclosed}")

        environment = {**os.environ, "NUGET_PACKAGES": str(packages)}
        evaluated = subprocess.run(["dotnet", "msbuild", str(project), "-nologo", "-target:Build", "-verbosity:quiet",
            "-getItem:Analyzer", "-p:Configuration=Release", "/m:1", "/nr:false"], cwd=directory,
            env=environment, check=True, capture_output=True, text=True)
        analyzers = [item["Identity"] for item in json.loads(evaluated.stdout)["Items"]["Analyzer"]]
        expanded = directory / "imports.xml"
        subprocess.run(["dotnet", "msbuild", str(project), "-nologo", f"-preprocess:{expanded}",
            "-p:Configuration=Release", "/m:1", "/nr:false"], cwd=directory, env=environment, check=True)
        # MSBuild emits each active import's absolute filename on its own comment line.
        imports = active_import_paths(expanded.read_text())
        return {"consumer": label, **executable_asset_receipt(assets, packages, analyzers, imports, directory)}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path)
    parser.add_argument("--receipt", type=Path, default=ROOT / "artifacts/consumer-executable-assets-receipt.json")
    parser.add_argument("--write-baseline", type=Path, help="write a candidate for explicit maintainer review")
    parser.add_argument("--policy", type=Path, default=ROOT / "eng/consumer-executable-assets.json")
    args = parser.parse_args()
    package_directory = args.package_directory.resolve()
    subprocess.run([sys.executable, str(ROOT / "scripts/validate-nuget-packages.py"), str(package_directory)],
                   cwd=ROOT, check=True)
    identities = sorted(package_identity(path) for path in package_directory.glob("*.nupkg"))
    versions = {version for _, version in identities}
    if len(versions) != 1:
        raise ValueError(f"package versions differ: {sorted(versions)}")
    receipts = [validate_consumer(package_directory, [identity], identities, identity[0]) for identity in identities]
    receipts.append(validate_consumer(package_directory, identities, identities, "combined"))
    receipt = {"schemaVersion": 1, "sdk": json.loads((ROOT / "global.json").read_text())["sdk"],
        "sdkVersion": subprocess.run(["dotnet", "--version"], cwd=ROOT, check=True,
            capture_output=True, text=True).stdout.strip(),
        "packages": [{"id": package_id, "version": version,
            "sha256": hashlib.sha256((package_directory / f"{package_id}.{version}.nupkg").read_bytes()).hexdigest()}
            for package_id, version in identities], "consumers": receipts}
    args.receipt.parent.mkdir(parents=True, exist_ok=True)
    args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    policy = {"schemaVersion": 1, "consumers": {result["consumer"]: result["selected"] for result in receipts}, "selected": [json.loads(entry) for entry in sorted({
        json.dumps(entry, sort_keys=True) for result in receipts for entry in result["selected"]})]}
    if args.write_baseline:
        args.write_baseline.write_text(json.dumps(policy, indent=2) + "\n", encoding="utf-8")
        print(f"Wrote candidate executable asset baseline for explicit review: {args.write_baseline}")
    else:
        enforce_asset_policy(receipts, json.loads(args.policy.read_text()))
    print(f"Validated {len(receipts)} isolated consumers at {next(iter(versions))}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - the CLI must emit the exact gate failure.
        print(f"validate-consumer-package-references: {error}", file=sys.stderr)
        raise SystemExit(1)
