#!/usr/bin/env python3
"""Build and pack exactly the public Hexalith.Agents package inventory."""
from __future__ import annotations
import argparse,subprocess,sys
from pathlib import Path
from release_package_inventory import *
from release_package_inventory import load_packages as declared_packages

def evaluated_package_id(project: Path) -> str:
    """Evaluate PackageId in the same Release/package mode used for packing."""
    result = subprocess.run(
        [
            "dotnet",
            "msbuild",
            str(project),
            "-nologo",
            "-getProperty:PackageId",
            "-p:Configuration=Release",
            "-p:UseHexalithProjectReferences=false",
        ],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    values = [line.strip() for line in result.stdout.splitlines() if line.strip()]
    if len(values) != 1:
        raise ValueError(f"could not evaluate one PackageId for {project.relative_to(ROOT)}: {values}")
    return values[0]


def load_packages() -> list[ReleasePackage]:
    """Validate manifest identities against Release/package-mode evaluation."""
    packages = declared_packages()
    for package in packages:
        actual = evaluated_package_id(package.project)
        if actual != package.package_id:
            raise ValueError(f"manifest PackageId mismatch for {package.project}: {actual!r}")
    return packages


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("output_directory", type=Path)
    parser.add_argument("version")
    parser.add_argument("--no-build", action="store_true", help="pack the existing Release output without building")
    parser.add_argument("--no-restore", action="store_true", help="reuse the existing restored dependency graph")
    args = parser.parse_args()
    if args.no_build and not args.no_restore:
        parser.error("--no-build requires --no-restore")

    packages = load_packages()
    output = args.output_directory.resolve()
    validate_output_directory(output)
    output.mkdir(parents=True, exist_ok=True)
    clean_manifest_archives(output, frozenset(package.package_id for package in packages))

    for package in packages:
        common_properties = [
            f"-p:Version={args.version}",
            f"-p:PackageVersion={args.version}",
            "-p:UseHexalithProjectReferences=false",
            "/m:1",
            "/nr:false",
        ]
        if not args.no_build:
            subprocess.run(
                ["dotnet", "build", str(package.project), "--configuration", "Release",
                 *(["--no-restore"] if args.no_restore else []), *common_properties],
                cwd=ROOT,
                check=True,
            )
        subprocess.run(
            [
                "dotnet",
                "pack",
                str(package.project),
                "--no-build",
                "--no-restore",
                "--configuration",
                "Release",
                "--output",
                str(output),
                *common_properties,
            ],
            cwd=ROOT,
            check=True,
        )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"pack-release-packages: {error}", file=sys.stderr)
        raise SystemExit(1)
