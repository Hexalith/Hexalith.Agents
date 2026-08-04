#!/usr/bin/env python3
"""Restore and build an isolated consumer of the exact Agents package inventory."""

from __future__ import annotations

import argparse
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


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path)
    args = parser.parse_args()
    package_directory = args.package_directory.resolve()

    subprocess.run(
        [sys.executable, str(ROOT / "scripts" / "validate-nuget-packages.py"), str(package_directory)],
        cwd=ROOT,
        check=True,
    )
    identities = sorted(package_identity(path) for path in package_directory.glob("*.nupkg"))
    versions = {version for _, version in identities}
    if len(versions) != 1:
        raise ValueError(f"package versions differ: {sorted(versions)}")

    with tempfile.TemporaryDirectory(prefix="agents-package-consumer-") as directory_name:
        directory = Path(directory_name)
        package_versions = "\n".join(
            f'    <PackageVersion Include="{package_id}" Version="{version}" />'
            for package_id, version in identities
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
        (directory / "ConsumerProbe.cs").write_text(
            textwrap.dedent("""\
            namespace Hexalith.Agents.PackageConsumer;

            using Hexalith.Agents;
            using Hexalith.Agents.Client;
            using Hexalith.Agents.Contracts.Agent;
            using Hexalith.Agents.Testing;
            using Hexalith.Agents.UI;

            public static class ConsumerProbe
            {
                public static Type[] PublicPackageTypes { get; } =
                [
                    typeof(AgentsAssemblyMarker),
                    typeof(AgentsClientAssemblyMarker),
                    typeof(AgentLifecycleStatus),
                    typeof(AgentsTestingAssemblyMarker),
                    typeof(AgentsUIAssemblyMarker),
                ];
            }
            """),
            encoding="utf-8",
        )
        config = directory / "NuGet.config"
        local_patterns = "\n".join(
            f'                  <package pattern="{package_id}" />' for package_id, _ in identities
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
        if actual_agent_libraries != expected_agent_libraries:
            raise ValueError(
                "isolated consumer Agents package identity mismatch; "
                f"missing={sorted(expected_agent_libraries - actual_agent_libraries)}; "
                f"unexpected={sorted(actual_agent_libraries - expected_agent_libraries)}"
            )

        expected_source = normalize_disclosure_path(str(package_directory))
        for package_id, version in identities:
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

    print(f"Validated isolated consumer for {len(identities)} package references at {next(iter(versions))}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - the CLI must emit the exact gate failure.
        print(f"validate-consumer-package-references: {error}", file=sys.stderr)
        raise SystemExit(1)
