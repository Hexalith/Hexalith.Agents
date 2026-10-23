"""Bounded package validation and content comparison for release downloads."""

from __future__ import annotations

import hashlib
import importlib
import zipfile
import zlib
from pathlib import Path
from xml.etree import ElementTree


VALIDATOR = importlib.import_module("validate-nuget-packages")
MAX_ARCHIVE_BYTES = VALIDATOR.MAX_ARCHIVE_BYTES
MAX_CONTENT_BYTES = VALIDATOR.MAX_CONTENT_BYTES
MAX_MEMBERS = VALIDATOR.MAX_MEMBERS


def package_contents(path: Path, package_id: str, version: str) -> dict[str, str]:
    """Validate a regular bounded archive and hash every non-signature member."""
    try:
        return _package_contents(path, package_id, version)
    except (OSError, ValueError, zipfile.BadZipFile, RuntimeError, ElementTree.ParseError,
            zlib.error, EOFError, NotImplementedError) as error:
        raise ValueError(f"{path.name}: {error}") from error


def _package_contents(path: Path, package_id: str, version: str) -> dict[str, str]:
    VALIDATOR.validate_archive_limits(path, max_archive_bytes=MAX_ARCHIVE_BYTES,
        max_content_bytes=MAX_CONTENT_BYTES, max_members=MAX_MEMBERS)
    with zipfile.ZipFile(path) as archive:
        entries = archive.infolist()
        metadata = VALIDATOR.read_metadata(path)
        if metadata.package_id != package_id or metadata.version != version:
            raise ValueError(f"{path.name}: package identity does not match {package_id} {version}")
        VALIDATOR.validate_package(metadata, path, version)
        result: dict[str, str] = {}
        for entry in entries:
            if entry.is_dir():
                result[entry.filename] = "directory"
                continue
            if entry.filename == ".signature.p7s":
                if entry.file_size == 0 or entry.compress_type != zipfile.ZIP_STORED:
                    raise ValueError(f"{path.name}: malformed repository signature entry")
                continue
            digest = hashlib.sha256()
            with archive.open(entry) as member:
                for chunk in iter(lambda: member.read(64 * 1024), b""):
                    digest.update(chunk)
            result[entry.filename] = digest.hexdigest()
        return result


def expected_archives(directory: Path, package_ids: tuple[str, ...], version: str) -> dict[str, dict[str, str]]:
    """Preflight every expected local archive before any remote request."""
    return {
        package_id: package_contents(directory / f"{package_id}.{version}.nupkg", package_id, version)
        for package_id in package_ids
    }
