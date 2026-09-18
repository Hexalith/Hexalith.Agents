#!/usr/bin/env python3
"""Validate a GitHub Release tag, source commit, and exact package assets."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path


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

    if not isinstance(release, dict):
        raise ReleaseVerificationError("GitHub Release response must be an object")
    expected_tag = f"v{version}"
    if release.get("tag_name") != expected_tag:
        raise ReleaseVerificationError(f"GitHub Release tag must be {expected_tag}")
    if release.get("draft") is not False:
        raise ReleaseVerificationError(f"GitHub Release {expected_tag} is missing or still a draft")

    assets = release.get("assets")
    if not isinstance(assets, list) or any(not isinstance(asset, dict) for asset in assets):
        raise ReleaseVerificationError("GitHub Release assets are malformed")
    asset_names = [asset.get("name") for asset in assets]
    if any(not isinstance(name, str) or not name for name in asset_names):
        raise ReleaseVerificationError("GitHub Release contains an unnamed asset")

    expected_assets = {f"{package_id}.{version}.nupkg" for package_id in package_ids}
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
    args = parser.parse_args()

    verify_github_release(
        read_json(args.manifest.resolve(), "release manifest"),
        read_json(args.release.resolve(), "GitHub Release"),
        args.version,
        args.expected_sha,
        args.resolved_tag_sha,
    )
    print(f"Verified GitHub Release v{args.version} at {args.expected_sha}.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except ReleaseVerificationError as error:
        print(f"verify-github-release: {error}", file=sys.stderr)
        raise SystemExit(1)
