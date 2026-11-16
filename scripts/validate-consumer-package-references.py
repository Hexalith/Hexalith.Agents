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

from release_package_integrity import expected_archives


ROOT = Path(__file__).resolve().parents[1]


from consumer_package_validation import *


def run(command: list[str], cwd: Path, packages: Path) -> None:
    """Run a consumer command with a fresh global packages directory."""
    environment = {**os.environ, "NUGET_PACKAGES": str(packages)}
    subprocess.run(command, cwd=cwd, env=environment, check=True)


def validate_consumer(package_directory: Path, identities: list[tuple[str, str]], all_identities: list[tuple[str, str]],
                      label: str) -> dict:
    """Restore and compile one independent consumer with the repository-pinned SDK."""
    with tempfile.TemporaryDirectory(prefix="agents-package-consumer-") as directory_name:
        directory = Path(directory_name)
        prepare_consumer(directory, package_directory, identities, all_identities)
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
        run(["dotnet", "build", str(project), "--no-restore", "-c", "Release", "/m:1", "/nr:false"], directory, packages)

        validate_consumer_assets(directory, package_directory, identities, all_identities)
        environment = {**os.environ, "NUGET_PACKAGES": str(packages)}
        evaluated = subprocess.run(["dotnet", "msbuild", str(project), "-nologo", "-target:Build", "-verbosity:quiet",
            "-getItem:Analyzer", "-p:Configuration=Release", "/m:1", "/nr:false"], cwd=directory,
            env=environment, check=True, capture_output=True, text=True)
        analyzers = [item["Identity"] for item in json.loads(evaluated.stdout)["Items"]["Analyzer"]]
        expanded = directory / "imports.xml"
        subprocess.run(["dotnet", "msbuild", str(project), "-nologo", f"-preprocess:{expanded}",
            "-p:Configuration=Release", "/m:1", "/nr:false"], cwd=directory, env=environment, check=True)
        return consumer_receipt(directory, label, evaluated.stdout)


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
    receipt = {"schemaVersion": 1, "sdk": json.loads((ROOT / "global.json").read_text())["sdk"],
        "sdkVersion": subprocess.run(["dotnet", "--version"], cwd=ROOT, check=True,
            capture_output=True, text=True).stdout.strip(),
        "packages": [{"id": package_id, "version": version,
            "sha256": hashlib.sha256((package_directory / f"{package_id}.{version}.nupkg").read_bytes()).hexdigest()}
            for package_id, version in identities], "consumers": receipts}
    if not args.no_receipt:
        args.receipt.parent.mkdir(parents=True, exist_ok=True)
        args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    produced_version = next(iter(versions))
    policy = {"schemaVersion": 1, "consumers": {result["consumer"]: policy_selection(result["selected"], produced_version)
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
