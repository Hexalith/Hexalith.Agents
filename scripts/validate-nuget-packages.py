#!/usr/bin/env python3
"""Validate exact package inventory, metadata, assets, and dependency direction."""

from __future__ import annotations

import argparse
import json
import posixpath
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "eng" / "release-packages.json"

EXPECTED_DEPENDENCIES = {
    "Hexalith.Agents.Contracts": frozenset(
        {"ByteAether.Ulid", "Hexalith.Commons.UniqueIds", "Hexalith.EventStore.Contracts"}
    ),
    "Hexalith.Agents.Client": frozenset(
        {"ByteAether.Ulid", "Hexalith.Agents.Contracts", "Hexalith.Commons.UniqueIds", "Hexalith.EventStore.Contracts"}
    ),
    "Hexalith.Agents": frozenset(
        {
            "ByteAether.Ulid",
            "Dapr.Client",
            "Hexalith.Agents.Contracts",
            "Hexalith.Commons.UniqueIds",
            "Hexalith.EventStore.Client",
            "Hexalith.EventStore.Contracts",
            "Microsoft.AspNetCore.DataProtection.Abstractions",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.Configuration.Binder",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Diagnostics.Abstractions",
            "Microsoft.Extensions.Hosting.Abstractions",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
        }
    ),
    "Hexalith.Agents.UI": frozenset(
        {
            "Hexalith.Agents.Client",
            "Hexalith.Agents.Contracts",
            "Hexalith.FrontComposer.Contracts",
            "Hexalith.FrontComposer.Shell",
            "ByteAether.Ulid",
            "Fluxor",
            "Fluxor.Blazor.Web",
            "Hexalith.Commons.UniqueIds",
            "Hexalith.EventStore.Contracts",
            "Microsoft.AspNetCore.Authentication.OpenIdConnect",
            "Microsoft.AspNetCore.Authorization",
            "Microsoft.AspNetCore.Components.Web",
            "Microsoft.AspNetCore.SignalR.Client",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.Configuration.Binder",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Diagnostics.Abstractions",
            "Microsoft.Extensions.Hosting.Abstractions",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
            "Microsoft.FluentUI.AspNetCore.Components",
            "Microsoft.FluentUI.AspNetCore.Components.Icons",
            "Microsoft.IdentityModel.Abstractions",
            "Microsoft.IdentityModel.JsonWebTokens",
            "Microsoft.IdentityModel.Logging",
            "Microsoft.IdentityModel.Protocols",
            "Microsoft.IdentityModel.Protocols.OpenIdConnect",
            "Microsoft.IdentityModel.Tokens",
            "NUlid",
            "System.IdentityModel.Tokens.Jwt",
            "System.Reactive",
        }
    ),
    "Hexalith.Agents.Testing": frozenset(
        {"ByteAether.Ulid", "Hexalith.Agents.Contracts", "Hexalith.Commons.UniqueIds", "Hexalith.EventStore.Contracts"}
    ),
}

FORBIDDEN_DEPENDENCY_FRAGMENTS = (
    ".Server",
    ".AppHost",
    ".Aspire",
    ".ServiceDefaults",
    ".Tests",
    "Aspire.Hosting",
    "SemanticKernel",
    "Azure.AI",
    "OpenAI",
    "Anthropic",
)


@dataclass(frozen=True)
class PackageDependency:
    """One dependency identity and its retained NuGet version/range."""

    package_id: str
    version: str


@dataclass(frozen=True)
class PackageMetadata:
    """Relevant package metadata extracted from one archive."""

    package_id: str
    version: str
    dependencies: tuple[PackageDependency, ...]
    archive_names: tuple[str, ...]


def expected_package_ids() -> frozenset[str]:
    """Return the manifest package ids."""
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    return frozenset(package["id"] for package in data["packages"])


def read_metadata(package_path: Path) -> PackageMetadata:
    """Read package identity, dependency ids, and archive names."""
    with zipfile.ZipFile(package_path) as archive:
        names = tuple(archive.namelist())
        nuspecs = [name for name in names if name.endswith(".nuspec")]
        if len(nuspecs) != 1:
            raise ValueError(f"{package_path.name}: expected one nuspec, found {len(nuspecs)}")
        root = ElementTree.fromstring(archive.read(nuspecs[0]))
        namespace = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}

        def text(name: str) -> str:
            element = root.find(f".//n:metadata/n:{name}", namespace) if namespace else root.find(f".//metadata/{name}")
            return element.text.strip() if element is not None and element.text else ""

        dependencies = root.findall(".//n:dependency", namespace) if namespace else root.findall(".//dependency")
        dependency_entries = tuple(
            sorted(
                (
                    PackageDependency(item.attrib["id"].strip(), item.attrib.get("version", "").strip())
                    for item in dependencies
                    if item.attrib.get("id", "").strip()
                ),
                key=lambda dependency: (dependency.package_id.casefold(), dependency.version),
            )
        )
        duplicate_ids = sorted(
            {
                dependency.package_id
                for dependency in dependency_entries
                if sum(
                    other.package_id.casefold() == dependency.package_id.casefold()
                    for other in dependency_entries
                )
                > 1
            }
        )
        if duplicate_ids:
            raise ValueError(f"{package_path.name}: duplicate dependency declarations: {duplicate_ids}")
        missing_versions = sorted(
            dependency.package_id for dependency in dependency_entries if not dependency.version
        )
        if missing_versions:
            raise ValueError(f"{package_path.name}: dependencies have no version/range: {missing_versions}")
        package_id = text("id")
        version = text("version")
        if not package_id or not version or not text("license"):
            raise ValueError(f"{package_path.name}: id, version, and license metadata are required")
        return PackageMetadata(package_id, version, dependency_entries, names)


def validate_package(metadata: PackageMetadata, path: Path, produced_version: str) -> None:
    """Validate one package's dependency and asset boundary."""
    expected = EXPECTED_DEPENDENCIES.get(metadata.package_id)
    if expected is None:
        raise ValueError(f"{path.name}: unexpected package id {metadata.package_id}")
    dependency_ids = frozenset(dependency.package_id for dependency in metadata.dependencies)
    if dependency_ids != expected:
        missing = sorted(expected - dependency_ids)
        unexpected = sorted(dependency_ids - expected)
        raise ValueError(f"{path.name}: dependency mismatch; missing={missing}; unexpected={unexpected}")

    forbidden = sorted(
        dependency.package_id
        for dependency in metadata.dependencies
        if any(fragment.casefold() in dependency.package_id.casefold() for fragment in FORBIDDEN_DEPENDENCY_FRAGMENTS)
    )
    if forbidden:
        raise ValueError(f"{path.name}: infrastructure/reverse dependency leak: {forbidden}")

    internal_dependencies = [
        dependency
        for dependency in metadata.dependencies
        if dependency.package_id in EXPECTED_DEPENDENCIES
    ]
    mismatched_internal_versions = sorted(
        f"{dependency.package_id}={dependency.version}"
        for dependency in internal_dependencies
        if dependency.version not in {produced_version, f"[{produced_version}]", f"[{produced_version},{produced_version}]"}
    )
    if mismatched_internal_versions:
        raise ValueError(
            f"{path.name}: internal Agents dependencies must match produced version {produced_version}: "
            f"{mismatched_internal_versions}"
        )

    normalized_names: list[str] = []
    for name in metadata.archive_names:
        normalized = posixpath.normpath(name.replace("\\", "/")).lstrip("./")
        if normalized == ".." or normalized.startswith("../") or name.startswith(("/", "\\")):
            raise ValueError(f"{path.name}: unsafe archive path: {name}")
        normalized_names.append(normalized)

    expected_dlls = {f"lib/net10.0/{metadata.package_id}.dll".casefold()}
    if metadata.package_id == "Hexalith.Agents.UI":
        expected_dlls.add("lib/net10.0/fr/hexalith.agents.ui.resources.dll")
    actual_dlls = {name.casefold() for name in normalized_names if name.casefold().endswith(".dll")}
    if actual_dlls != expected_dlls:
        raise ValueError(
            f"{path.name}: package DLL inventory mismatch; "
            f"missing={sorted(expected_dlls - actual_dlls)}; unexpected={sorted(actual_dlls - expected_dlls)}"
        )

    forbidden_asset_segments = {"analyzers", "build", "buildtransitive", "content", "contentfiles", "source", "src", "tools"}
    forbidden_asset_fragments = (
        ".server",
        ".apphost",
        ".aspire",
        ".servicedefaults",
        ".tests",
        "appsettings",
        "launchsettings",
    )
    leaked_assets = sorted(
        name
        for name in normalized_names
        if name.casefold().endswith((".cs", ".csproj", ".sln", ".slnx", ".props", ".targets", ".pfx", ".snk"))
        or bool(forbidden_asset_segments & {segment.casefold() for segment in name.split("/")})
        or any(fragment in name.casefold() for fragment in forbidden_asset_fragments)
    )
    if leaked_assets:
        raise ValueError(f"{path.name}: repository source, secret, or host asset leak: {leaked_assets}")


def validate_directory(package_directory: Path) -> tuple[list[PackageMetadata], str]:
    """Validate the full exact inventory and return metadata plus common version."""
    packages = sorted(path for path in package_directory.glob("*.nupkg") if not path.name.endswith(".snupkg"))
    metadata = [read_metadata(package) for package in packages]
    actual_ids = frozenset(item.package_id for item in metadata)
    expected_ids = expected_package_ids()
    if actual_ids != expected_ids or len(packages) != len(expected_ids):
        raise ValueError(
            f"package inventory mismatch; missing={sorted(expected_ids - actual_ids)}; "
            f"unexpected={sorted(actual_ids - expected_ids)}; files={[path.name for path in packages]}"
        )
    versions = {item.version for item in metadata}
    if len(versions) != 1:
        raise ValueError(f"all packages must share one version, found {sorted(versions)}")
    common_version = next(iter(versions))
    for item, path in zip(metadata, packages, strict=True):
        validate_package(item, path, common_version)
    return metadata, common_version


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path)
    args = parser.parse_args()
    metadata, version = validate_directory(args.package_directory.resolve())
    print(f"Validated {len(metadata)} Hexalith.Agents packages at {version}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - the CLI must emit the exact gate failure.
        print(f"validate-nuget-packages: {error}", file=sys.stderr)
        raise SystemExit(1)
