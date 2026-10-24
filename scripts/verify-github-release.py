#!/usr/bin/env python3
"""Validate a GitHub Release tag, source commit, and exact package assets."""

from __future__ import annotations

import argparse
import hashlib
import importlib
import os
import json
import re
import sys
from pathlib import Path

from release_package_integrity import MAX_ARCHIVE_BYTES, package_contents


PROVENANCE = importlib.import_module("recover-release-packages")
MAX_PROVENANCE_BYTES = 64 * 1024


SHA_PATTERN = re.compile(r"^[0-9a-f]{40}$")
VERSION_PATTERN = re.compile(
    r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
    r"(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)


class ReleaseVerificationError(RuntimeError):
    """A GitHub Release contract failure."""


def read_json(path: Path, label: str) -> object:
    """Read one JSON document with a concise diagnostic."""
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise ReleaseVerificationError(f"could not read {label} JSON from {path}: {error}") from error


def verify_github_release(
    manifest: object,
    release: object,
    version: str,
    expected_sha: str,
    resolved_tag_sha: str,
) -> None:
    """Require the exact version tag, source SHA, and manifest package assets."""
    if not VERSION_PATTERN.fullmatch(version):
        raise ReleaseVerificationError("version must be an exact semantic version")
    if not SHA_PATTERN.fullmatch(expected_sha) or not SHA_PATTERN.fullmatch(resolved_tag_sha):
        raise ReleaseVerificationError("expected and resolved tag SHAs must be exact lowercase commits")
    if resolved_tag_sha != expected_sha:
        raise ReleaseVerificationError(
            f"release tag resolves to {resolved_tag_sha}, not dispatched source {expected_sha}"
        )

    packages = manifest.get("packages") if isinstance(manifest, dict) else None
    if not isinstance(packages, list) or not packages:
        raise ReleaseVerificationError("release manifest has no package inventory")
    package_ids = [row.get("id") if isinstance(row, dict) else None for row in packages]
    if any(not isinstance(package_id, str) or not package_id for package_id in package_ids):
        raise ReleaseVerificationError("release manifest contains a malformed package identity")
    if any(not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]*", package_id) for package_id in package_ids):
        raise ReleaseVerificationError("release manifest contains an unsafe package identity")
    if len({package_id.casefold() for package_id in package_ids}) != len(package_ids):
        raise ReleaseVerificationError("release manifest repeats a package identity")

    if not isinstance(release, dict):
        raise ReleaseVerificationError("GitHub Release response must be an object")
    expected_tag = f"v{version}"
    if release.get("tag_name") != expected_tag:
        raise ReleaseVerificationError(f"GitHub Release tag must be {expected_tag}")
    if release.get("draft") is not False:
        raise ReleaseVerificationError(f"GitHub Release {expected_tag} is missing or still a draft")
    if "-" not in version.split("+", 1)[0] and release.get("prerelease") is not False:
        raise ReleaseVerificationError(
            f"GitHub Release {expected_tag} must explicitly have prerelease=false for a stable version"
        )

    assets = release.get("assets")
    if not isinstance(assets, list) or any(not isinstance(asset, dict) for asset in assets):
        raise ReleaseVerificationError("GitHub Release assets are malformed")
    asset_names = [asset.get("name") for asset in assets]
    if any(not isinstance(name, str) or not name for name in asset_names):
        raise ReleaseVerificationError("GitHub Release contains an unnamed asset")

    expected_assets = {f"{package_id}.{version}.nupkg" for package_id in package_ids} | {"release-provenance.json"}
    actual_assets = set(asset_names)
    if len(actual_assets) != len(asset_names) or actual_assets != expected_assets:
        raise ReleaseVerificationError(
            f"GitHub Release asset mismatch: missing={sorted(expected_assets - actual_assets)}, "
            f"unexpected={sorted(actual_assets - expected_assets)}"
        )


def main() -> int:
    """Run the GitHub Release contract verifier."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("release", type=Path)
    parser.add_argument("version")
    parser.add_argument("expected_sha")
    parser.add_argument("resolved_tag_sha")
    parser.add_argument("--asset-directory", type=Path)
    parser.add_argument("--repository", default=os.environ.get("GITHUB_REPOSITORY"))
    parser.add_argument("--metadata-only", action="store_true", help="preflight API metadata before downloading")
    args = parser.parse_args()

    manifest = read_json(args.manifest.resolve(), "release manifest")
    release = read_json(args.release.resolve(), "GitHub Release")
    verify_github_release(
        manifest,
        release,
        args.version,
        args.expected_sha,
        args.resolved_tag_sha,
    )
    verify_asset_metadata(release)
    if args.metadata_only:
        print(f"Verified GitHub Release metadata for v{args.version}; archive contents are not yet verified.")
        return 0
    if args.asset_directory is None:
        raise ReleaseVerificationError("--asset-directory is required to verify archive contents")
    verify_asset_archives(manifest, release, args.asset_directory.resolve(), args.version, args.repository, args.expected_sha)
    print(f"Verified GitHub Release v{args.version} at {args.expected_sha}.")
    return 0


def verify_asset_metadata(release: dict) -> None:
    """Reject oversized or unverifiable API assets before any download."""
    for asset in release["assets"]:
        size = asset.get("size")
        digest = asset.get("digest")
        if type(size) is not int or not 0 < size <= (MAX_PROVENANCE_BYTES if asset["name"] == "release-provenance.json" else MAX_ARCHIVE_BYTES):
            raise ReleaseVerificationError(f"{asset['name']}: asset size is missing or exceeds limits")
        if not isinstance(digest, str) or not re.fullmatch(r"sha256:[0-9a-f]{64}", digest):
            raise ReleaseVerificationError(f"{asset['name']}: missing or malformed asset SHA-256 digest")


def verify_asset_archives(manifest: dict, release: dict, directory: Path, version: str,
                          repository: str, source_sha: str) -> None:
    """Require each download's API digest, length, package identity and boundaries."""
    if not isinstance(repository, str) or not PROVENANCE.SOURCE.REPOSITORY_PATTERN.fullmatch(repository):
        raise ReleaseVerificationError("expected repository owner/name is required for provenance verification")
    expected = {f"{row['id']}.{version}.nupkg": row["id"] for row in manifest["packages"]}
    verify_asset_metadata(release)
    try:
        actual = {path.name for path in directory.iterdir()}
        if actual != set(expected) | {"release-provenance.json"}:
            raise ValueError("downloaded asset inventory does not match release manifest")
        for asset in release["assets"]:
            path = directory / asset["name"]
            if path.is_symlink() or not path.is_file():
                raise ValueError(f"{path.name}: asset must be a regular file")
            if path.name in expected:
                package_contents(path, expected[path.name], version)
            size = asset.get("size")
            digest = asset.get("digest")
            if type(size) is not int or size <= 0 or size != path.stat().st_size:
                raise ValueError(f"{path.name}: asset size mismatch")
            if not isinstance(digest, str) or not re.fullmatch(r"sha256:[0-9a-f]{64}", digest):
                raise ValueError(f"{path.name}: missing or malformed asset SHA-256 digest")
            if "sha256:" + hashlib.sha256(path.read_bytes()).hexdigest() != digest:
                raise ValueError(f"{path.name}: asset SHA-256 mismatch")
        provenance_path = directory / "release-provenance.json"
        if provenance_path.stat().st_size > MAX_PROVENANCE_BYTES:
            raise ValueError("release provenance exceeds size bound")
        PROVENANCE.verify_provenance(read_json(provenance_path, "release provenance"), directory,
            tuple(expected.values()), version, repository, source_sha)
    except (OSError, ValueError) as error:
        raise ReleaseVerificationError(str(error)) from error


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ReleaseVerificationError as error:
        print(f"verify-github-release: {error}", file=sys.stderr)
        raise SystemExit(1)
