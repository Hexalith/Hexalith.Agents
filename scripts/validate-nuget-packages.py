#!/usr/bin/env python3
"""Validate exact package inventory, metadata, assets, and dependency direction."""

from __future__ import annotations

import argparse
import json
import posixpath
import re
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "eng" / "release-packages.json"
NUMERIC_IDENTIFIER = r"(?:0|[1-9][0-9]*)"
PRERELEASE_IDENTIFIER = rf"(?:{NUMERIC_IDENTIFIER}|[0-9A-Za-z-]*[A-Za-z-][0-9A-Za-z-]*)"
VERSION_PATTERN = re.compile(
    rf"{NUMERIC_IDENTIFIER}\.{NUMERIC_IDENTIFIER}\.{NUMERIC_IDENTIFIER}"
    rf"(?:-{PRERELEASE_IDENTIFIER}(?:\.{PRERELEASE_IDENTIFIER})*)?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?"
)

EXPECTED_DEPENDENCIES = {
    "Hexalith.Agents.Contracts": frozenset(
        {"ByteAether.Ulid", "Hexalith.Commons.UniqueIds", "Hexalith.EventStore.Contracts"}
    ),
    "Hexalith.Agents.Client": frozenset(
        {"ByteAether.Ulid", "Hexalith.Agents.Contracts", "Hexalith.Commons.UniqueIds", "Hexalith.EventStore.Contracts"}
    ),
    "Hexalith.Agents.EventStore": frozenset(
        {
            "ByteAether.Ulid",
            "Dapr.Actors",
            "Dapr.Actors.AspNetCore",
            "Dapr.AspNetCore",
            "Dapr.Client",
            "FluentValidation",
            "FluentValidation.DependencyInjectionExtensions",
            "Hexalith.Agents.Contracts",
            "Hexalith.Commons.UniqueIds",
            "Hexalith.EventStore.Admin.Abstractions",
            "Hexalith.EventStore.Client",
            "Hexalith.EventStore.Contracts",
            "Hexalith.EventStore.DomainService",
            "Hexalith.EventStore.Gateway",
            "Hexalith.EventStore.Server",
            "Hexalith.EventStore.ServiceDefaults",
            "MediatR",
            "MessagePack",
            "Microsoft.AspNetCore.Authentication.JwtBearer",
            "Microsoft.AspNetCore.OpenApi",
            "Microsoft.AspNetCore.SignalR.StackExchangeRedis",
            "Microsoft.Extensions.Http.Resilience",
            "Microsoft.Extensions.ServiceDiscovery",
            "Microsoft.IdentityModel.Abstractions",
            "Microsoft.IdentityModel.JsonWebTokens",
            "Microsoft.IdentityModel.Logging",
            "Microsoft.IdentityModel.Protocols",
            "Microsoft.IdentityModel.Protocols.OpenIdConnect",
            "Microsoft.IdentityModel.Tokens",
            "Microsoft.OpenApi",
            "OpenTelemetry",
            "OpenTelemetry.Exporter.OpenTelemetryProtocol",
            "OpenTelemetry.Extensions.Hosting",
            "OpenTelemetry.Instrumentation.AspNetCore",
            "OpenTelemetry.Instrumentation.Http",
            "OpenTelemetry.Instrumentation.Runtime",
            "StackExchange.Redis",
            "Swashbuckle.AspNetCore.SwaggerUI",
            "System.IdentityModel.Tokens.Jwt",
        }
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
            "Microsoft.AspNetCore.SignalR.Common",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.Configuration.Binder",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Diagnostics.Abstractions",
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

ALLOWED_INFRASTRUCTURE_DEPENDENCIES = {
    "Hexalith.Agents.EventStore": frozenset(
        {"Hexalith.EventStore.Server", "Hexalith.EventStore.ServiceDefaults"}
    ),
}


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


def normalize_archive_path(name: str) -> str:
    """Reject traversal and rooted paths before normalizing harmless prefixes."""
    slashed = name.replace("\\", "/")
    segments = slashed.split("/")
    if slashed.startswith("/") or any(re.match(r"^[A-Za-z]:", segment) for segment in segments) or ".." in segments:
        raise ValueError(f"unsafe archive path: {name}")
    return posixpath.normpath(slashed)


def package_metadata_policy() -> dict[str, str]:
    """Read expected release metadata from the central build properties."""
    props = ElementTree.parse(ROOT / "Directory.Build.props").getroot()
    policy: dict[str, str] = {}
    for name in ("PackageLicenseExpression", "PackageProjectUrl", "RepositoryUrl", "RepositoryType", "PackageReadmeFile"):
        values = {(element.text or "").strip() for element in props.iter(name)}
        if len(values) != 1 or not next(iter(values)):
            raise ValueError(f"central package policy must declare exactly one nonempty {name}")
        policy[name] = next(iter(values))
    return policy


def read_metadata(package_path: Path) -> PackageMetadata:
    """Read package identity, dependency ids, and archive names."""
    with zipfile.ZipFile(package_path) as archive:
        names = tuple(archive.namelist())
        normalized_names = tuple(normalize_archive_path(name) for name in names)
        seen_names: set[str] = set()
        for name in normalized_names:
            if name.casefold() in seen_names:
                raise ValueError(f"{package_path.name}: duplicate normalized archive path: {name}")
            seen_names.add(name.casefold())
        corrupt_member = archive.testzip()
        if corrupt_member is not None:
            raise ValueError(f"{package_path.name}: corrupt archive member: {corrupt_member}")
        nuspecs = [name for name in names if name.endswith(".nuspec")]
        if len(nuspecs) != 1:
            raise ValueError(f"{package_path.name}: expected one nuspec, found {len(nuspecs)}")
        root = ElementTree.fromstring(archive.read(nuspecs[0]))
        namespace = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}
        containers = root.findall("n:metadata", namespace) if namespace else root.findall("metadata")
        if root.tag.rsplit("}", 1)[-1] != "package" or len(containers) != 1:
            raise ValueError(f"{package_path.name}: expected package root with exactly one metadata container")

        def element(name: str) -> ElementTree.Element:
            elements = root.findall(f"n:metadata/n:{name}", namespace) if namespace else root.findall(f"metadata/{name}")
            if len(elements) != 1:
                raise ValueError(f"{package_path.name}: expected exactly one {name} metadata element")
            return elements[0]

        def text(name: str) -> str:
            return (element(name).text or "").strip()

        policy = package_metadata_policy()
        if text("license") != policy["PackageLicenseExpression"] or element("license").get("type") != "expression":
            raise ValueError(f"{package_path.name}: license must be the central expression {policy['PackageLicenseExpression']}")
        if text("projectUrl") != policy["PackageProjectUrl"]:
            raise ValueError(f"{package_path.name}: projectUrl must match central PackageProjectUrl")
        repository = element("repository")
        if repository.get("url") != policy["RepositoryUrl"] or repository.get("type") != policy["RepositoryType"]:
            raise ValueError(f"{package_path.name}: repository URL and type must match central policy")
        readme = policy["PackageReadmeFile"]
        if text("readme") != readme:
            raise ValueError(f"{package_path.name}: readme must match central PackageReadmeFile {readme}")
        readme_entries = [name for name, normalized in zip(names, normalized_names, strict=True) if normalized == readme]
        if (
            len(readme_entries) != 1
            or archive.getinfo(readme_entries[0]).is_dir()
            or not archive.read(readme_entries[0]).strip()
        ):
            raise ValueError(f"{package_path.name}: expected one nonempty packaged readme {readme}")

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
        if not package_id or not version:
            raise ValueError(f"{package_path.name}: id and version metadata are required")
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
        and dependency.package_id not in ALLOWED_INFRASTRUCTURE_DEPENDENCIES.get(metadata.package_id, frozenset())
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
        normalized_names.append(normalize_archive_path(name))

    expected_dlls = {f"lib/net10.0/{metadata.package_id}.dll".casefold()}
    if metadata.package_id == "Hexalith.Agents.UI":
        expected_dlls.add("lib/net10.0/fr/hexalith.agents.ui.resources.dll")
    actual_dlls = {
        name.casefold()
        for name, raw_name in zip(normalized_names, metadata.archive_names, strict=True)
        if name.casefold().endswith(".dll") and not raw_name.endswith(("/", "\\"))
    }
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


def publication_paths(package_directory: Path, version: str) -> list[Path]:
    """Preflight the complete manifest/version set, ignoring unrelated archives."""
    if not VERSION_PATTERN.fullmatch(version):
        raise ValueError(f"invalid publication version: {version!r}")
    paths: list[Path] = []
    for package_id in sorted(expected_package_ids()):
        path = package_directory / f"{package_id}.{version}.nupkg"
        if path.is_symlink() or not path.is_file():
            raise ValueError(f"missing regular publication archive: {path.name}")
        metadata = read_metadata(path)
        if metadata.package_id != package_id or metadata.version != version:
            raise ValueError(f"{path.name}: archive identity does not match manifest/version")
        validate_package(metadata, path, version)
        paths.append(path)
    return paths


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package_directory", type=Path)
    parser.add_argument("--publication-version", help="print only validated manifest archive paths for this version")
    args = parser.parse_args()
    if args.publication_version is not None:
        for path in publication_paths(args.package_directory.resolve(), args.publication_version):
            print(path)
        return 0
    metadata, version = validate_directory(args.package_directory.resolve())
    print(f"Validated {len(metadata)} Hexalith.Agents packages at {version}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - the CLI must emit the exact gate failure.
        print(f"validate-nuget-packages: {error}", file=sys.stderr)
        raise SystemExit(1)
