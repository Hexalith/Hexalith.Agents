#!/usr/bin/env python3
"""Verify that every manifest package version is absent from or present on NuGet."""

from __future__ import annotations

import argparse
import gzip
import io
import json
import re
import sys
import time
from dataclasses import dataclass
from pathlib import Path
from typing import Callable
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlparse
from urllib.request import Request, urlopen


DEFAULT_REGISTRATION_BASE = "https://api.nuget.org/v3/registration5-gz-semver2"
PACKAGE_ID_PATTERN = re.compile(r"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,98}[A-Za-z0-9])?$")
VERSION_PATTERN = re.compile(
    r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
    r"(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?"
    r"(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$"
)
MAX_RESPONSE_BYTES = 1_000_000


class VerificationError(RuntimeError):
    """A fail-closed manifest or NuGet verification failure."""


class TransientProbeError(VerificationError):
    """A retryable NuGet availability failure."""


@dataclass(frozen=True)
class ProbeResult:
    """One exact NuGet package/version availability result."""

    package_id: str
    version: str
    present: bool


def load_package_ids(manifest_path: Path) -> tuple[str, ...]:
    """Load unique, valid package identities from the release manifest."""
    def reject_duplicate_keys(pairs: list[tuple[str, object]]) -> dict[str, object]:
        result: dict[str, object] = {}
        for key, value in pairs:
            if key in result:
                raise VerificationError(f"release manifest contains duplicate JSON key {key!r}")
            result[key] = value
        return result

    try:
        payload = json.loads(
            manifest_path.read_text(encoding="utf-8"),
            object_pairs_hook=reject_duplicate_keys,
        )
    except (OSError, json.JSONDecodeError) as error:
        raise VerificationError(f"could not read release manifest {manifest_path}: {error}") from error

    schema_version = payload.get("schemaVersion") if isinstance(payload, dict) else None
    if type(schema_version) is not int or schema_version != 1:
        raise VerificationError("release manifest must be an object with integer schemaVersion 1")
    packages = payload.get("packages")
    if not isinstance(packages, list) or not packages:
        raise VerificationError("release manifest must contain a non-empty packages array")

    package_ids: list[str] = []
    normalized_ids: set[str] = set()
    for index, entry in enumerate(packages, start=1):
        if not isinstance(entry, dict):
            raise VerificationError(f"release manifest package #{index} must be an object")
        package_id = entry.get("id")
        project = entry.get("project")
        if not isinstance(package_id, str) or not PACKAGE_ID_PATTERN.fullmatch(package_id):
            raise VerificationError(f"release manifest package #{index} has an invalid NuGet id")
        if not isinstance(project, str) or not project.strip():
            raise VerificationError(f"release manifest package #{index} has no project path")
        normalized = package_id.casefold()
        if normalized in normalized_ids:
            raise VerificationError(f"release manifest repeats package id {package_id!r}")
        normalized_ids.add(normalized)
        package_ids.append(package_id)
    return tuple(package_ids)


def registration_url(base_url: str, package_id: str, version: str) -> str:
    """Build the NuGet registration-leaf URL for one exact identity."""
    return (
        f"{base_url.rstrip('/')}/"
        f"{quote(package_id.casefold(), safe='')}/{quote(version.casefold(), safe='')}.json"
    )


def _open_url(url: str, timeout: float):
    request = Request(url, headers={"Accept": "application/json", "User-Agent": "Hexalith.Agents-release-verifier/1"})
    return urlopen(request, timeout=timeout)


def decode_json_response(body: bytes, package_id: str, version: str) -> object:
    """Decode a bounded plain or gzip-compressed NuGet JSON response."""
    if len(body) > MAX_RESPONSE_BYTES:
        raise VerificationError(f"NuGet response was too large for {package_id} {version}")
    if body.startswith(b"\x1f\x8b"):
        try:
            with gzip.GzipFile(fileobj=io.BytesIO(body)) as compressed:
                body = compressed.read(MAX_RESPONSE_BYTES + 1)
        except (EOFError, OSError) as error:
            raise VerificationError(f"NuGet returned malformed gzip for {package_id} {version}") from error
        if len(body) > MAX_RESPONSE_BYTES:
            raise VerificationError(f"NuGet response expanded too large for {package_id} {version}")
    try:
        return json.loads(body.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError) as error:
        raise VerificationError(f"NuGet returned malformed JSON for {package_id} {version}") from error


def probe_package(
    package_id: str,
    version: str,
    *,
    base_url: str = DEFAULT_REGISTRATION_BASE,
    timeout: float = 15.0,
    opener: Callable[[str, float], object] = _open_url,
) -> ProbeResult:
    """Probe one exact package version, treating only HTTP 404 as absence."""
    url = registration_url(base_url, package_id, version)
    try:
        response = opener(url, timeout)
        with response:
            status = response.getcode()
            if status != 200:
                raise VerificationError(f"NuGet returned unexpected HTTP {status} for {package_id} {version}")
            body = response.read(MAX_RESPONSE_BYTES + 1)
    except HTTPError as error:
        status = error.code
        error.close()
        if status == 404:
            return ProbeResult(package_id, version, False)
        error_type = TransientProbeError if status == 429 or 500 <= status <= 599 else VerificationError
        raise error_type(f"NuGet returned unexpected HTTP {status} for {package_id} {version}") from error
    except URLError as error:
        raise TransientProbeError(
            f"NuGet request failed for {package_id} {version}: {error.reason}"
        ) from error
    except OSError as error:
        raise TransientProbeError(f"NuGet request failed for {package_id} {version}: {error}") from error

    payload = decode_json_response(body, package_id, version)
    catalog_entry = payload.get("catalogEntry") if isinstance(payload, dict) else None
    if isinstance(catalog_entry, str):
        catalog_url = urlparse(catalog_entry)
        if catalog_url.scheme != "https" or catalog_url.hostname != "api.nuget.org":
            raise VerificationError(
                f"NuGet returned an untrusted catalog URL for {package_id} {version}"
            )
        try:
            catalog_response = opener(catalog_entry, timeout)
            with catalog_response:
                catalog_status = catalog_response.getcode()
                if catalog_status != 200:
                    raise VerificationError(
                        f"NuGet returned unexpected catalog HTTP {catalog_status} for {package_id} {version}"
                    )
                catalog_body = catalog_response.read(MAX_RESPONSE_BYTES + 1)
        except HTTPError as error:
            status = error.code
            error.close()
            error_type = TransientProbeError if status == 429 or 500 <= status <= 599 else VerificationError
            raise error_type(
                f"NuGet returned unexpected catalog HTTP {status} for {package_id} {version}"
            ) from error
        except (URLError, OSError) as error:
            raise TransientProbeError(
                f"NuGet catalog request failed for {package_id} {version}: {error}"
            ) from error
        catalog_entry = decode_json_response(catalog_body, package_id, version)

    actual_id = catalog_entry.get("id") if isinstance(catalog_entry, dict) else None
    actual_version = catalog_entry.get("version") if isinstance(catalog_entry, dict) else None
    if (
        not isinstance(actual_id, str)
        or not isinstance(actual_version, str)
        or actual_id.casefold() != package_id.casefold()
        or actual_version.casefold() != version.casefold()
    ):
        raise VerificationError(
            f"NuGet returned a mismatched or malformed registration leaf for {package_id} {version}"
        )
    return ProbeResult(package_id, version, True)


def verify_publication(
    package_ids: tuple[str, ...],
    version: str,
    *,
    expect: str,
    attempts: int,
    delay_seconds: float,
    base_url: str = DEFAULT_REGISTRATION_BASE,
    timeout: float = 15.0,
    opener: Callable[[str, float], object] = _open_url,
    sleeper: Callable[[float], None] = time.sleep,
) -> None:
    """Verify the complete inventory with bounded retries for NuGet indexing."""
    if not VERSION_PATTERN.fullmatch(version):
        raise VerificationError(f"version is not an exact semantic version: {version!r}")
    if expect not in {"absent", "present"}:
        raise VerificationError("expect must be either 'absent' or 'present'")
    if attempts < 1:
        raise VerificationError("attempts must be at least 1")
    if delay_seconds < 0:
        raise VerificationError("delay seconds must not be negative")

    if expect == "absent":
        results = [
            probe_package(
                package_id,
                version,
                base_url=base_url,
                timeout=timeout,
                opener=opener,
            )
            for package_id in package_ids
        ]
        present = sorted(result.package_id for result in results if result.present)
        for result in results:
            state = "present" if result.present else "absent"
            print(f"{result.package_id} {version}: {state}")
        if present:
            raise VerificationError(
                "publication collision: these package versions already exist on NuGet: "
                + ", ".join(present)
            )
        return

    pending = list(package_ids)
    for attempt in range(1, attempts + 1):
        next_pending: list[str] = []
        for package_id in pending:
            try:
                result = probe_package(
                    package_id,
                    version,
                    base_url=base_url,
                    timeout=timeout,
                    opener=opener,
                )
            except TransientProbeError as error:
                print(f"{package_id} {version}: transient failure: {error}", file=sys.stderr)
                next_pending.append(package_id)
                continue

            state = "present" if result.present else "absent"
            print(f"{result.package_id} {version}: {state}")
            if not result.present:
                next_pending.append(package_id)

        pending = next_pending
        if not pending:
            return
        if attempt < attempts:
            print(
                f"NuGet indexing incomplete after attempt {attempt}/{attempts}; "
                f"retrying pending inventory: {', '.join(sorted(pending))}",
                file=sys.stderr,
            )
            sleeper(delay_seconds)

    raise VerificationError(
        f"NuGet presence retry exhaustion after {attempts} attempts; "
        f"missing or unconfirmed: {', '.join(sorted(pending))}"
    )


def main() -> int:
    """Run the command-line verifier."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("version")
    parser.add_argument("--expect", choices=("absent", "present"), required=True)
    parser.add_argument("--attempts", type=int, default=12)
    parser.add_argument("--delay-seconds", type=float, default=10.0)
    parser.add_argument("--timeout", type=float, default=15.0)
    parser.add_argument("--base-url", default=DEFAULT_REGISTRATION_BASE)
    args = parser.parse_args()

    package_ids = load_package_ids(args.manifest.resolve())
    verify_publication(
        package_ids,
        args.version,
        expect=args.expect,
        attempts=args.attempts,
        delay_seconds=args.delay_seconds,
        base_url=args.base_url,
        timeout=args.timeout,
    )
    print(f"Verified {len(package_ids)} package versions are {args.expect} on NuGet.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except VerificationError as error:
        print(f"verify-nuget-publication: {error}", file=sys.stderr)
        raise SystemExit(1)
