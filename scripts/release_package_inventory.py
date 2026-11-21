"""Agents release package inventory."""
from __future__ import annotations
import json
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree
ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "eng/release-packages.json"

@dataclass(frozen=True)
class ReleasePackage:
    """One validated release-manifest entry."""

    package_id: str
    project: Path


def declared_package_id(project: Path) -> str:
    """Return the project's explicit PackageId."""
    root = ElementTree.parse(project).getroot()
    declarations = [
        (element.text or "").strip()
        for element in root.iter()
        if element.tag.rsplit("}", 1)[-1] == "PackageId" and (element.text or "").strip()
    ]
    if len(set(declarations)) != 1:
        raise ValueError(f"{project.relative_to(ROOT)} must declare exactly one PackageId")
    return declarations[0]


def load_packages() -> list[ReleasePackage]:
    """Load and validate ids and project paths from the authoritative manifest."""
    data = json.loads(MANIFEST.read_text(encoding="utf-8"))
    packages = data.get("packages") if isinstance(data, dict) else None
    if not isinstance(packages, list) or not packages:
        raise ValueError(f"{MANIFEST} must contain a non-empty packages array")

    seen_ids: set[str] = set()
    seen_projects: set[Path] = set()
    result: list[ReleasePackage] = []
    for index, package in enumerate(packages, start=1):
        if not isinstance(package, dict):
            raise ValueError(f"package entry #{index} must be an object")
        package_id = package.get("id")
        project_value = package.get("project")
        if not isinstance(package_id, str) or not package_id.strip():
            raise ValueError(f"package entry #{index} has no package id")
        if not isinstance(project_value, str) or not project_value.strip():
            raise ValueError(f"package entry #{index} has no project path")

        project = (ROOT / project_value).resolve()
        if ROOT not in project.parents or project.suffix != ".csproj" or not project.is_file():
            raise ValueError(f"package project must be an existing repository csproj: {project_value}")
        if package_id.casefold() in seen_ids or project in seen_projects:
            raise ValueError(f"duplicate package inventory entry: {package_id} / {project_value}")

        declared_id = declared_package_id(project)
        if package_id != declared_id:
            raise ValueError(
                f"manifest PackageId mismatch for {project_value}: "
                f"manifest={package_id!r}, declared={declared_id!r}"
            )
        seen_ids.add(package_id.casefold())
        seen_projects.add(project)
        result.append(ReleasePackage(package_id, project))
    return result
