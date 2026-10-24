#!/usr/bin/env python3
"""Recover only missing members of an original, provenance-bound draft release."""

from __future__ import annotations

import argparse
import hashlib
import importlib
import json
import os
import subprocess
import sys
import tempfile
from pathlib import Path

VERIFY = importlib.import_module("verify-nuget-publication")
SOURCE = importlib.import_module("verify-release-source")
from release_package_integrity import MAX_ARCHIVE_BYTES, expected_archives

ROOT = Path(__file__).resolve().parents[1]
PROVENANCE_NAME = "release-provenance.json"


def command(args: list[str], **options) -> str:
    """Run a bounded external operation, retaining its failure status."""
    return subprocess.run(args, check=True, capture_output=True, text=True, timeout=120, **options).stdout


def verify_provenance(provenance: object, directory: Path, package_ids: tuple[str, ...],
                      version: str, repository: str, source_sha: str) -> dict:
    """Require exact original identity and raw archive hashes before remote publication probes."""
    if not isinstance(provenance, dict) or provenance.get("schemaVersion") != 1:
        raise ValueError("unsupported release provenance")
    for key, expected in {"version": version, "repository": repository,
                          "sourceSha": source_sha, "tag": f"v{version}"}.items():
        if provenance.get(key) != expected:
            raise ValueError(f"original release provenance {key} mismatch")
    expected_names = {f"{package_id}.{version}.nupkg" for package_id in package_ids}
    entries = provenance.get("archives")
    if not isinstance(entries, list) or len(entries) != len(expected_names):
        raise ValueError("original release archive inventory mismatch")
    seen = set()
    for entry in entries:
        if not isinstance(entry, dict) or entry.get("name") not in expected_names or entry["name"] in seen:
            raise ValueError("unexpected or duplicate original archive")
        name = entry["name"]
        seen.add(name)
        path = directory / name
        if (not path.is_file() or path.is_symlink() or path.stat().st_size > MAX_ARCHIVE_BYTES
                or type(entry.get("size")) is not int or path.stat().st_size != entry["size"]
                or hashlib.sha256(path.read_bytes()).hexdigest() != entry.get("sha256")):
            raise ValueError(f"original archive integrity mismatch: {name}")
    return provenance


def recover_inventory(package_ids: tuple[str, ...], version: str, directory: Path, *,
                      before_push, push, probe=None, download=None, final_verify=None,
                      verify_only: bool = False) -> None:
    """Preflight the entire immutable remote set before allowing any missing-member push."""
    expected = expected_archives(directory, package_ids, version)
    probe = probe or (lambda package_id: VERIFY.probe_package(package_id, version,
        base_url=VERIFY.DEFAULT_REGISTRATION_BASE, timeout=15, opener=VERIFY._open_url))
    download = download or (lambda package_id, contents: VERIFY.verify_package_download(package_id,
        version, contents, base_url=VERIFY.DEFAULT_PACKAGE_BASE, timeout=15, opener=VERIFY._open_url))
    missing = []
    for package_id in package_ids:
        result = probe(package_id)  # Any unconfirmed/transient result refuses this run before all pushes.
        if result.present:
            download(package_id, expected[package_id])
        else:
            missing.append(package_id)
    if verify_only and missing:
        raise ValueError("cannot finalize an incomplete release inventory")
    if missing:
        before_push()  # Prove current green main again, immediately before the first missing push.
        for package_id in missing:
            push(directory / f"{package_id}.{version}.nupkg")
    (final_verify or (lambda: VERIFY.verify_publication(package_ids, version,
        expect="present", package_directory=directory, attempts=12, delay_seconds=10,
        timeout=15, base_url=VERIFY.DEFAULT_REGISTRATION_BASE, package_base_url=VERIFY.DEFAULT_PACKAGE_BASE)))()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("version")
    parser.add_argument("--source-sha", required=True)
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--verify-only", action="store_true")
    modes.add_argument("--staged-only", action="store_true", help="verify retained originals without probing or publishing")
    parser.add_argument("--local-package-directory", type=Path, help="also require local publication bytes to match retained originals")
    args = parser.parse_args()
    repository = os.environ.get("GITHUB_REPOSITORY", "")
    if not SOURCE.REPOSITORY_PATTERN.fullmatch(repository) or not SOURCE.SHA_PATTERN.fullmatch(args.source_sha):
        raise ValueError("recovery requires exact repository and original source SHA")
    if not VERIFY.VERSION_PATTERN.fullmatch(args.version):
        raise ValueError("invalid recovery version")
    if not (args.verify_only or args.staged_only) and os.environ.get("HEXALITH_RELEASE_PUBLISH_ENABLED") != "true":
        raise ValueError("release publication is frozen")
    if not (args.verify_only or args.staged_only) and not os.environ.get("NUGET_API_KEY"):
        raise ValueError("NUGET_API_KEY is required")
    package_ids = VERIFY.load_package_ids(ROOT / "eng/release-packages.json")
    tag = f"v{args.version}"
    release = json.loads(command(["gh", "api", f"repos/{repository}/releases/tags/{tag}"]))
    names = [entry.get("name") for entry in release.get("assets", [])]
    expected_names = {f"{package_id}.{args.version}.nupkg" for package_id in package_ids} | {PROVENANCE_NAME}
    if (release.get("draft") is not True or release.get("tag_name") != tag
            or release.get("target_commitish") != args.source_sha
            or len(names) != len(expected_names) or set(names) != expected_names
            or any(entry.get("state") != "uploaded" or type(entry.get("size")) is not int
                   or not 0 < entry["size"] <= MAX_ARCHIVE_BYTES for entry in release["assets"])):
        raise ValueError("recovery requires the exact original fully uploaded draft asset inventory")
    # Local tag must resolve to the original source; no release planning/tag creation is performed.
    tag_sha = command(["git", "rev-parse", f"refs/tags/{tag}^{{commit}}"], cwd=ROOT).strip()
    remote_ref = json.loads(command(["gh", "api", f"repos/{repository}/git/ref/tags/{tag}"]))
    tag_object = remote_ref.get("object", {})
    if tag_object.get("type") == "tag":
        tag_object = json.loads(command(["gh", "api", f"repos/{repository}/git/tags/{tag_object.get('sha')}"])).get("object", {})
    if tag_sha != args.source_sha or tag_object.get("type") != "commit" or tag_object.get("sha") != args.source_sha:
        raise ValueError("original source tag does not match the approved source SHA")
    def source_proof():
        command([sys.executable, str(ROOT / "scripts/verify-release-source.py"),
            "--repository", repository, "--workflow", "ci.yml",
            "--dispatch-ref", "refs/heads/main", "--dispatch-sha", args.source_sha])
    source_proof()
    with tempfile.TemporaryDirectory(prefix="agents-release-recovery-") as temporary:
        directory = Path(temporary)
        # gh buffers downloads itself; workflow additionally caps filesystem file size at 64 MiB.
        command(["gh", "release", "download", tag, "--repo", repository, "--dir", str(directory)])
        provenance_path = directory / PROVENANCE_NAME
        if provenance_path.stat().st_size > 64 * 1024:
            raise ValueError("release provenance exceeds size bound")
        for asset in release["assets"]:
            path = directory / asset["name"]
            digest = "sha256:" + hashlib.sha256(path.read_bytes()).hexdigest()
            if path.stat().st_size != asset["size"] or asset.get("digest") != digest:
                raise ValueError(f"downloaded original asset differs from GitHub metadata: {path.name}")
        provenance = json.loads(provenance_path.read_text())
        verify_provenance(provenance, directory, package_ids, args.version, repository, args.source_sha)
        if args.local_package_directory is not None:
            verify_provenance(provenance, args.local_package_directory.resolve(), package_ids,
                              args.version, repository, args.source_sha)
        if args.staged_only:
            print(f"Verified retained original release assets for {tag}.")
            return 0
        def push(path):
            command(["dotnet", "nuget", "push", str(path), "--source", "https://api.nuget.org/v3/index.json",
                "--api-key", os.environ["NUGET_API_KEY"]])
        recover_inventory(package_ids, args.version, directory, before_push=source_proof,
                          push=push, verify_only=args.verify_only)
    if not args.verify_only:
        command(["gh", "release", "edit", tag, "--repo", repository, "--draft=false"])
    print(f"Verified original release inventory for {tag}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, OSError, subprocess.SubprocessError, VERIFY.VerificationError) as error:
        # Never print subprocess arguments: a failed push carries the API key there.
        detail = f"external command failed ({type(error).__name__})" if isinstance(error, subprocess.SubprocessError) else str(error)
        print(f"recover-release-packages: {detail}", file=sys.stderr)
        raise SystemExit(1)
