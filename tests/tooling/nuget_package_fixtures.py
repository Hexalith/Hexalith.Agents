"""Small synthetic release archives for package gate and publisher tests."""

from __future__ import annotations

import importlib.util
import base64
import hashlib
import json
import struct
import sys
import zipfile
from pathlib import Path
from typing import Callable
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "validate-nuget-packages.py"
sys.path.insert(0, str(ROOT / "scripts"))
SPEC = importlib.util.spec_from_file_location("validate_nuget_packages", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
VALIDATOR = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = VALIDATOR
SPEC.loader.exec_module(VALIDATOR)
VERSION = "1.2.3"
PACKAGE_IDS = tuple(package["id"] for package in json.loads(
    (ROOT / "eng" / "release-packages.json").read_text(encoding="utf-8")
)["packages"])


def ui_static_assets() -> dict[str, bytes]:
    """Bind synthetic module bytes through a manifest captured from the pinned Razor SDK."""
    module = b"export function fixture() {}\n"
    integrity = base64.b64encode(hashlib.sha256(module).digest()).decode("ascii")
    manifest = json.loads((ROOT / "tests/tooling/fixtures/ui-static-package-assets.json").read_text())
    asset = manifest["Assets"]["staticwebassets/pendingProviderCommands.js"]
    asset["Integrity"] = integrity
    asset["FileLength"] = len(module)
    # Independently captured pinned-SDK fingerprint for the constant fixture module.
    original_fingerprint = asset["Fingerprint"]
    asset["Fingerprint"] = "ymbusap65w"
    for endpoint in manifest["Endpoints"]:
        endpoint["Route"] = endpoint["Route"].replace(original_fingerprint, asset["Fingerprint"])
        for header in endpoint["ResponseHeaders"]:
            if header["Name"] == "Content-Length":
                header["Value"] = str(len(module))
            if header["Name"] == "ETag":
                header["Value"] = '"' + integrity + '"'
        for value in endpoint["EndpointProperties"]:
            if value["Name"] == "integrity":
                value["Value"] = "sha256-" + integrity
            if value["Name"] == "fingerprint":
                value["Value"] = asset["Fingerprint"]
    result = {str(path.relative_to(ROOT / "eng/ui-static-assets")): path.read_bytes()
              for path in (ROOT / "eng/ui-static-assets").rglob("*.targets")}
    result["staticwebassets/pendingProviderCommands.js"] = module
    result["build/Hexalith.Agents.UI.PackageAssets.json"] = json.dumps(manifest).encode()
    return result


def write_package(
    directory: Path,
    package_id: str = "Hexalith.Agents.Contracts",
    *,
    version: str = VERSION,
    mutate: Callable[[ElementTree.Element], None] | None = None,
    dll_path: str | None = None,
    readme: bytes | None = b"# Package readme\n",
    namespaced: bool = False,
    extra_assets: dict[str, bytes] | None = None,
) -> Path:
    """Write a valid inventory archive, optionally mutating one contract."""
    root = ElementTree.Element("package")
    metadata = ElementTree.SubElement(root, "metadata")
    policy = VALIDATOR.package_metadata_policy()
    for name, value in (
        ("id", package_id), ("version", version),
        ("license", policy["PackageLicenseExpression"]),
        ("projectUrl", policy["PackageProjectUrl"]),
        ("readme", policy["PackageReadmeFile"]),
    ):
        ElementTree.SubElement(metadata, name).text = value
    metadata.find("license").set("type", "expression")
    ElementTree.SubElement(metadata, "repository", {
        "type": policy["RepositoryType"], "url": policy["RepositoryUrl"], "commit": "a" * 40,
    })
    dependencies = ElementTree.SubElement(metadata, "dependencies")
    group = ElementTree.SubElement(dependencies, "group", {"targetFramework": "net11.0"})
    for dependency_id in sorted(VALIDATOR.EXPECTED_DEPENDENCIES[package_id]):
        dependency_version = version if dependency_id in VALIDATOR.EXPECTED_DEPENDENCIES else "1.0.0"
        ElementTree.SubElement(group, "dependency", {"id": dependency_id, "version": dependency_version,
                                                    "exclude": "Build,Analyzers"})
    if mutate:
        mutate(metadata)
    if namespaced:
        for element in root.iter():
            element.tag = "{http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd}" + element.tag
    path = directory / f"{package_id}.{version}.nupkg"
    with zipfile.ZipFile(path, "w") as archive:
        archive.writestr(f"{package_id}.nuspec", ElementTree.tostring(root, encoding="utf-8"))
        archive.writestr(dll_path or f"lib/net11.0/{package_id}.dll", b"fixture assembly")
        if package_id == "Hexalith.Agents.UI":
            archive.writestr("lib/net11.0/fr/Hexalith.Agents.UI.resources.dll", b"fixture resources")
            for name, contents in ui_static_assets().items():
                if name not in (extra_assets or {}):
                    archive.writestr(name, contents)
        if readme is not None:
            archive.writestr(policy["PackageReadmeFile"], readme)
        for name, content in (extra_assets or {}).items():
            archive.writestr(name, content)
    return path


def write_inventory(directory: Path) -> list[Path]:
    """Write every archive in the actual release manifest."""
    return [write_package(directory, package_id) for package_id in PACKAGE_IDS]


def corrupt_archive_member(path: Path, member: str) -> None:
    """Flip a stored member byte without updating its ZIP CRC."""
    with zipfile.ZipFile(path) as archive:
        offset = archive.getinfo(member).header_offset
    data = bytearray(path.read_bytes())
    filename_size, extra_size = struct.unpack_from("<HH", data, offset + 26)
    data[offset + 30 + filename_size + extra_size] ^= 1
    path.write_bytes(data)
