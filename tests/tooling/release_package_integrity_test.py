"""Adversarial archive-content checks shared by both publication gates."""

from __future__ import annotations

import stat
import sys
import tempfile
import unittest
import warnings
import zipfile
from pathlib import Path
from unittest.mock import patch

from nuget_package_fixtures import ROOT, VERSION, corrupt_archive_member, write_package

sys.path.insert(0, str(ROOT / "scripts"))
import release_package_integrity as INTEGRITY


class ReleasePackageIntegrityTests(unittest.TestCase):
    def test_all_ordinary_members_participate_and_signature_is_the_only_exception(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            original = write_package(root)
            expected = INTEGRITY.package_contents(original, "Hexalith.Agents.Contracts", VERSION)
            self.assertEqual(set(expected), {"Hexalith.Agents.Contracts.nuspec", "README.md",
                                            "lib/net10.0/Hexalith.Agents.Contracts.dll"})
            signed = write_package(root, extra_assets={".signature.p7s": b"repository signature fixture"})
            self.assertEqual(INTEGRITY.package_contents(signed, "Hexalith.Agents.Contracts", VERSION), expected)
            changed = write_package(root, readme=b"substitution")
            self.assertNotEqual(INTEGRITY.package_contents(changed, "Hexalith.Agents.Contracts", VERSION), expected)
            extra = write_package(root, extra_assets={"harmless.txt": b"still a content change"})
            self.assertNotEqual(INTEGRITY.package_contents(extra, "Hexalith.Agents.Contracts", VERSION), expected)
            extra = write_package(root, extra_assets={"empty-directory/": b""})
            self.assertNotEqual(INTEGRITY.package_contents(extra, "Hexalith.Agents.Contracts", VERSION), expected)

    def test_directory_members_cannot_hide_changed_payload_bytes(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            for payload in (b"first directory payload", b"changed directory payload"):
                with self.subTest(payload=payload):
                    path = write_package(Path(directory), extra_assets={"empty/": payload})
                    with self.assertRaisesRegex(ValueError, "malformed directory member"):
                        INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)

    def test_missing_extra_and_changed_members_cannot_match(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = write_package(Path(directory), extra_assets={"extra.txt": b"initial"})
            original = INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)
            for operation in ("remove", "change"):
                with self.subTest(operation=operation), zipfile.ZipFile(path) as archive:
                    members = {name: archive.read(name) for name in archive.namelist()}
                if operation == "remove":
                    del members["extra.txt"]
                else:
                    members["extra.txt"] = b"changed"
                with zipfile.ZipFile(path, "w") as archive:
                    for name, body in members.items():
                        archive.writestr(name, body)
                self.assertNotEqual(INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION), original)

    def test_rejects_wrong_identity_version_and_crc_corruption(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = write_package(root)
            for package_id, version in (("Hexalith.Agents.Client", VERSION), ("Hexalith.Agents.Contracts", "9.9.9")):
                with self.subTest(package_id=package_id, version=version), self.assertRaisesRegex(ValueError, "package identity"):
                    INTEGRITY.package_contents(path, package_id, version)
            corrupt_archive_member(path, "lib/net10.0/Hexalith.Agents.Contracts.dll")
            with self.assertRaisesRegex(ValueError, "corrupt archive member"):
                INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)

    def test_rejects_unsafe_duplicate_and_nonregular_entries(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            for name in ("../extra.txt", "./README.md", "README.md"):
                with self.subTest(name=name), warnings.catch_warnings():
                    warnings.simplefilter("ignore", UserWarning)
                    path = write_package(root, extra_assets={name: b"ambiguous"})
                    with self.assertRaises(ValueError):
                        INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)
            path = write_package(root)
            with zipfile.ZipFile(path, "a") as archive:
                info = zipfile.ZipInfo("link")
                info.external_attr = (stat.S_IFLNK | 0o777) << 16
                archive.writestr(info, "README.md")
            with self.assertRaisesRegex(ValueError, "unsupported package member"):
                INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)

    def test_rejects_empty_or_compressed_signature_entries(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = write_package(root, extra_assets={".signature.p7s": b""})
            with self.assertRaisesRegex(ValueError, "malformed repository signature"):
                INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)
            path = write_package(root)
            with zipfile.ZipFile(path, "a", compression=zipfile.ZIP_DEFLATED) as archive:
                archive.writestr(".signature.p7s", b"compressed")
            with self.assertRaisesRegex(ValueError, "malformed repository signature"):
                INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)

    def test_rejects_size_and_member_limits_before_decompression(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = write_package(Path(directory))
            for constant, limit, diagnostic in (("MAX_ARCHIVE_BYTES", 1, "oversized"),
                ("MAX_CONTENT_BYTES", 1, "content limits"), ("MAX_MEMBERS", 1, "content limits")):
                with self.subTest(limit=constant), patch.object(INTEGRITY, constant, limit), self.assertRaisesRegex(ValueError, diagnostic):
                    INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)

    def test_rejects_missing_empty_truncated_and_symlink_archives(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = root / "bad.nupkg"
            for body in (None, b"", b"PK truncated"):
                with self.subTest(body=body):
                    if body is not None:
                        path.write_bytes(body)
                    with self.assertRaises(ValueError):
                        INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)
            original = write_package(root)
            path.unlink()
            path.symlink_to(original)
            with self.assertRaisesRegex(ValueError, "regular package archive"):
                INTEGRITY.package_contents(path, "Hexalith.Agents.Contracts", VERSION)
