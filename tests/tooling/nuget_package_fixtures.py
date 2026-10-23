"""Small synthetic release archives for package gate and publisher tests."""

from __future__ import annotations

import importlib.util
import json
import struct
import sys
import zipfile
from pathlib import Path
from typing import Callable
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "validate-nuget-packages.py"
SPEC = importlib.util.spec_from_file_location("validate_nuget_packages", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
VALIDATOR = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = VALIDATOR
SPEC.loader.exec_module(VALIDATOR)
VERSION = "1.2.3"
PACKAGE_IDS = tuple(package["id"] for package in json.loads(
    (ROOT / "eng" / "release-packages.json").read_text(encoding="utf-8")
)["packages"])


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
    group = ElementTree.SubElement(dependencies, "group", {"targetFramework": "net10.0"})
    for dependency_id in sorted(VALIDATOR.EXPECTED_DEPENDENCIES[package_id]):
        dependency_version = version if dependency_id in VALIDATOR.EXPECTED_DEPENDENCIES else "1.0.0"
        ElementTree.SubElement(group, "dependency", {"id": dependency_id, "version": dependency_version})
    if mutate:
        mutate(metadata)
    if namespaced:
        for element in root.iter():
            element.tag = "{http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd}" + element.tag
    path = directory / f"{package_id}.{version}.nupkg"
    with zipfile.ZipFile(path, "w") as archive:
        archive.writestr(f"{package_id}.nuspec", ElementTree.tostring(root, encoding="utf-8"))
        archive.writestr(dll_path or f"lib/net10.0/{package_id}.dll", b"fixture assembly")
        if package_id == "Hexalith.Agents.UI":
            archive.writestr("lib/net10.0/fr/Hexalith.Agents.UI.resources.dll", b"fixture resources")
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
