#!/usr/bin/env python3
"""Check built Agents packages with fully explicit, scan-discoverable compiler commands."""
from __future__ import annotations

import json
from pathlib import Path
import shutil
import subprocess
import sys

from consumer_package_validation import (consumer_receipt, enforce_asset_policy, prepare_consumer,
                                         validate_consumer_assets)
from nuget_package_validation import validate_directory
from release_package_integrity import expected_archives
from release_package_inventory import load_packages

ROOT = Path(__file__).resolve().parents[1]
SCRATCH = ROOT / "artifacts/cicd-agents-package-check"
EXPECTED_PROJECTS = {
    "Hexalith.Agents": "src/Hexalith.Agents/Hexalith.Agents.csproj",
    "Hexalith.Agents.Client": "src/Hexalith.Agents.Client/Hexalith.Agents.Client.csproj",
    "Hexalith.Agents.Contracts": "src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj",
    "Hexalith.Agents.EventStore": "src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj",
    "Hexalith.Agents.Testing": "src/Hexalith.Agents.Testing/Hexalith.Agents.Testing.csproj",
    "Hexalith.Agents.UI": "src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj",
}


def pack_built_packages() -> None:
    """Pack the fixed public inventory once, with no module restore or compilation."""
    subprocess.run([
        "dotnet", "pack", "src/Hexalith.Agents/Hexalith.Agents.csproj", "--no-build", "--no-restore", "--configuration", "Release",
        "--output", "artifacts/cicd-agents-package-check/packages", "-p:Version=0.0.0-ci-check",
        "-p:PackageVersion=0.0.0-ci-check", "-p:UseHexalithProjectReferences=false", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    subprocess.run([
        "dotnet", "pack", "src/Hexalith.Agents.Client/Hexalith.Agents.Client.csproj", "--no-build", "--no-restore", "--configuration", "Release",
        "--output", "artifacts/cicd-agents-package-check/packages", "-p:Version=0.0.0-ci-check",
        "-p:PackageVersion=0.0.0-ci-check", "-p:UseHexalithProjectReferences=false", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    subprocess.run([
        "dotnet", "pack", "src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj", "--no-build", "--no-restore", "--configuration", "Release",
        "--output", "artifacts/cicd-agents-package-check/packages", "-p:Version=0.0.0-ci-check",
        "-p:PackageVersion=0.0.0-ci-check", "-p:UseHexalithProjectReferences=false", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    subprocess.run([
        "dotnet", "pack", "src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj", "--no-build", "--no-restore", "--configuration", "Release",
        "--output", "artifacts/cicd-agents-package-check/packages", "-p:Version=0.0.0-ci-check",
        "-p:PackageVersion=0.0.0-ci-check", "-p:UseHexalithProjectReferences=false", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    subprocess.run([
        "dotnet", "pack", "src/Hexalith.Agents.Testing/Hexalith.Agents.Testing.csproj", "--no-build", "--no-restore", "--configuration", "Release",
        "--output", "artifacts/cicd-agents-package-check/packages", "-p:Version=0.0.0-ci-check",
        "-p:PackageVersion=0.0.0-ci-check", "-p:UseHexalithProjectReferences=false", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    subprocess.run([
        "dotnet", "pack", "src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj", "--no-build", "--no-restore", "--configuration", "Release",
        "--output", "artifacts/cicd-agents-package-check/packages", "-p:Version=0.0.0-ci-check",
        "-p:PackageVersion=0.0.0-ci-check", "-p:UseHexalithProjectReferences=false", "/m:1", "/nr:false",
    ], cwd=".", check=True)


def check_consumer() -> str:
    """Compile only the scratch consumer; every executable and operand is literal."""
    subprocess.run([
        "dotnet", "restore", "artifacts/cicd-agents-package-check/consumer/PackageConsumer.csproj",
        "--configfile", "artifacts/cicd-agents-package-check/consumer/NuGet.config",
        "--packages", "artifacts/cicd-agents-package-check/consumer/packages",
        "--no-cache", "--force-evaluate", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    subprocess.run([
        "dotnet", "build", "artifacts/cicd-agents-package-check/consumer/PackageConsumer.csproj",
        "--no-restore", "-c", "Release", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    evaluated = subprocess.run([
        "dotnet", "msbuild", "artifacts/cicd-agents-package-check/consumer/PackageConsumer.csproj",
        "-nologo", "-target:Build", "-verbosity:quiet", "-getItem:Analyzer",
        "-p:Configuration=Release", "/m:1", "/nr:false",
    ], cwd=".", check=True, capture_output=True, text=True)
    subprocess.run([
        "dotnet", "msbuild", "artifacts/cicd-agents-package-check/consumer/PackageConsumer.csproj",
        "-nologo", "-preprocess:artifacts/cicd-agents-package-check/consumer/imports.xml",
        "-p:Configuration=Release", "/m:1", "/nr:false",
    ], cwd=".", check=True)
    return evaluated.stdout


def main() -> int:
    if Path.cwd().resolve() != ROOT:
        raise ValueError("Run the package hook from the Agents repository root.")
    actual = {package.package_id: package.project.relative_to(ROOT).as_posix() for package in load_packages()}
    if actual != EXPECTED_PROJECTS:
        raise ValueError("release manifest disagrees with the six supported package commands")
    if SCRATCH.is_symlink() or not SCRATCH.resolve().is_relative_to(ROOT):
        raise ValueError("refusing a symlink or escaping package-check scratch directory")
    # This reserved ignored directory belongs exclusively to this checker. Both
    # stale scratch and every successful/failing invocation are cleaned here.
    if SCRATCH.exists():
        shutil.rmtree(SCRATCH)
    SCRATCH.mkdir(parents=True)
    try:
        pack_built_packages()
        feed = SCRATCH / "packages"
        metadata, version = validate_directory(feed)
        if version != "0.0.0-ci-check":
            raise ValueError("unexpected check-package version")
        identities = sorted((package.package_id, package.version) for package in metadata)
        expected_archives(feed, tuple(identity for identity, _ in identities), version)
        selections = [(identity[0], [identity]) for identity in identities] + [("combined", identities)]
        receipts = []
        for label, selection in selections:
            consumer = SCRATCH / "consumer"
            if consumer.exists():
                shutil.rmtree(consumer)
            prepare_consumer(consumer, feed, selection, identities)
            evaluation = check_consumer()
            validate_consumer_assets(consumer, feed, selection, identities)
            receipts.append(consumer_receipt(consumer, label, evaluation))
        enforce_asset_policy(receipts, json.loads((ROOT / "eng/consumer-executable-assets.json").read_text()), version)
        print("Validated six built packages and seven isolated package consumers.")
        return 0
    finally:
        shutil.rmtree(SCRATCH)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"check-built-packages: {error}", file=sys.stderr)
        raise SystemExit(1)
