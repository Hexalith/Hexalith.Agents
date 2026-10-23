"""Executable archive regressions for the release package gate."""

from __future__ import annotations

import subprocess
import sys
import tempfile
import unittest
import warnings
import zipfile
from pathlib import Path
from xml.etree import ElementTree

from nuget_package_fixtures import PACKAGE_IDS, SCRIPT, VALIDATOR, VERSION, corrupt_archive_member, write_inventory, write_package


class NuGetPackageValidationTests(unittest.TestCase):
    def validate(self, path: Path) -> None:
        metadata = VALIDATOR.read_metadata(path)
        VALIDATOR.validate_package(metadata, path, VERSION)

    def test_dependency_metadata_cannot_enable_ordinary_build_assets(self) -> None:
        for attrs, allowed in (({}, False), ({"exclude": ""}, False), ({"exclude": "None"}, False),
                               ({"exclude": "Analyzers"}, False), ({"exclude": "Build"}, True),
                               ({"exclude": "build,analyzers"}, True), ({"exclude": "All"}, True),
                               ({"include": "Compile,Runtime"}, True), ({"include": "None"}, True)):
            with self.subTest(attrs=attrs), tempfile.TemporaryDirectory() as directory:
                def mutate(metadata):
                    dependency = metadata.find("dependencies/group/dependency")
                    dependency.attrib.pop("exclude", None)
                    dependency.attrib.update(attrs)
                path = write_package(Path(directory), mutate=mutate)
                metadata = VALIDATOR.read_metadata(path)
                self.assertEqual(metadata.dependencies[0].include_assets, attrs.get("include"))
                self.assertEqual(metadata.dependencies[0].exclude_assets, attrs.get("exclude"))
                if allowed:
                    VALIDATOR.validate_package(metadata, path, VERSION)
                else:
                    with self.assertRaisesRegex(ValueError, "permits ordinary build assets"):
                        VALIDATOR.validate_package(metadata, path, VERSION)

    def test_rejects_unrecognized_dependency_asset_flags(self) -> None:
        for attribute in ("include", "exclude"):
            for value in ("Compile;Build", "buid", "All,Unexpected"):
                with self.subTest(attribute=attribute, value=value), tempfile.TemporaryDirectory() as directory:
                    def mutate(metadata):
                        metadata.find("dependencies/group/dependency").set(attribute, value)
                    with self.assertRaisesRegex(ValueError, "unrecognized dependency asset metadata"):
                        self.validate(write_package(Path(directory), mutate=mutate))

    def test_valid_metadata_and_harmless_current_directory_prefixes(self) -> None:
        for namespaced in (False, True):
            for prefix in ("", "./", "././"):
                with self.subTest(namespaced=namespaced, prefix=prefix), tempfile.TemporaryDirectory() as directory:
                    path = write_package(Path(directory), namespaced=namespaced,
                        dll_path=f"{prefix}lib/net10.0/Hexalith.Agents.Contracts.dll")
                    self.validate(path)

    def test_rejects_traversal_and_rooted_paths_before_normalization(self) -> None:
        dll = "lib/net10.0/Hexalith.Agents.Contracts.dll"
        paths = (
            f"../{dll}", f"../../{dll}", f"./../{dll}", f"folder/../{dll}",
            f"..\\..\\{dll.replace('/', chr(92))}", f"/{dll}", f"\\{dll}",
            f"C:/{dll}", f"c:{dll}", f"./C:/{dll}", f"././C:/{dll}", f"//server/share/{dll}",
        )
        for dll_path in paths:
            with self.subTest(path=dll_path), tempfile.TemporaryDirectory() as directory:
                with self.assertRaisesRegex(ValueError, "unsafe archive path"):
                    self.validate(write_package(Path(directory), dll_path=dll_path))

    def test_leading_dots_are_not_stripped_from_dll_paths(self) -> None:
        for prefix in (".", ".../"):
            with self.subTest(prefix=prefix), tempfile.TemporaryDirectory() as directory:
                path = write_package(Path(directory), dll_path=f"{prefix}lib/net10.0/Hexalith.Agents.Contracts.dll")
                with self.assertRaisesRegex(ValueError, "DLL inventory mismatch"):
                    self.validate(path)

    def test_rejects_unsafe_non_dll_assets(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = write_package(Path(directory), extra_assets={"../README.txt": b"unsafe"})
            with self.assertRaisesRegex(ValueError, "unsafe archive path"):
                self.validate(path)

    def test_directory_entries_cannot_satisfy_the_dll_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = write_package(Path(directory), dll_path="lib/net10.0/Hexalith.Agents.Contracts.dll/")
            with self.assertRaisesRegex(ValueError, "DLL inventory mismatch"):
                self.validate(path)

    def test_rejects_wrong_or_missing_central_metadata(self) -> None:
        def set_text(name: str, value: str):
            return lambda metadata: setattr(metadata.find(name), "text", value)

        cases = [
            ("wrong license", set_text("license", "Apache-2.0"), "license must"),
            ("file license", lambda metadata: metadata.find("license").set("type", "file"), "license must"),
            ("missing license type", lambda metadata: metadata.find("license").attrib.clear(), "license must"),
            ("wrong project", set_text("projectUrl", "https://example.invalid"), "projectUrl must"),
            ("wrong repository", lambda metadata: metadata.find("repository").set("url", "https://example.invalid"), "repository URL"),
            ("wrong repository type", lambda metadata: metadata.find("repository").set("type", "svn"), "repository URL"),
            ("wrong readme", set_text("readme", "OTHER.md"), "readme must"),
        ]
        for name in ("license", "projectUrl", "repository", "readme"):
            cases.append((f"missing {name}", lambda metadata, name=name: metadata.remove(metadata.find(name)), f"exactly one {name}"))
            cases.append((f"duplicate {name}", lambda metadata, name=name: ElementTree.SubElement(metadata, name), f"exactly one {name}"))
        for label, mutate, diagnostic in cases:
            with self.subTest(case=label), tempfile.TemporaryDirectory() as directory:
                with self.assertRaisesRegex(ValueError, diagnostic):
                    self.validate(write_package(Path(directory), mutate=mutate))

    def test_requires_one_nonempty_packaged_readme(self) -> None:
        for readme in (None, b"", b" \n"):
            with self.subTest(readme=readme), tempfile.TemporaryDirectory() as directory:
                with self.assertRaisesRegex(ValueError, "nonempty packaged readme"):
                    self.validate(write_package(Path(directory), readme=readme))
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(ValueError, "duplicate normalized archive path"):
                self.validate(write_package(Path(directory), extra_assets={"./README.md": b"ambiguous"}))
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(ValueError, "nonempty packaged readme"):
                self.validate(write_package(Path(directory), readme=None, extra_assets={"README.md/": b"directory data"}))

    def test_rejects_duplicate_normalized_dll_entries(self) -> None:
        dll = "lib/net10.0/Hexalith.Agents.Contracts.dll"
        for duplicate in (dll, f"./{dll}", dll.upper()):
            with self.subTest(duplicate=duplicate), tempfile.TemporaryDirectory() as directory:
                with warnings.catch_warnings():
                    warnings.simplefilter("ignore", UserWarning)
                    path = write_package(Path(directory), extra_assets={duplicate: b"substituted assembly"})
                with self.assertRaisesRegex(ValueError, "duplicate normalized archive path"):
                    self.validate(path)

    def test_rejects_wrong_package_root_and_duplicate_metadata_containers(self) -> None:
        for fault in ("root", "container"):
            with self.subTest(fault=fault), tempfile.TemporaryDirectory() as directory:
                path = write_package(Path(directory))
                with zipfile.ZipFile(path) as archive:
                    entries = {name: archive.read(name) for name in archive.namelist()}
                nuspec = next(name for name in entries if name.endswith(".nuspec"))
                root = ElementTree.fromstring(entries[nuspec])
                if fault == "root":
                    root.tag = "other"
                else:
                    root.insert(0, ElementTree.Element("metadata"))
                entries[nuspec] = ElementTree.tostring(root, encoding="utf-8")
                with zipfile.ZipFile(path, "w") as archive:
                    for name, body in entries.items():
                        archive.writestr(name, body)
                with self.assertRaisesRegex(ValueError, "exactly one metadata container"):
                    self.validate(path)

    def test_publication_rejects_corrupt_assembly_bytes_before_emitting_paths(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            paths = write_inventory(root)
            corrupt_archive_member(paths[-1], f"lib/net10.0/{PACKAGE_IDS[-1]}.dll")
            result = self.run_cli(root, "--publication-version", VERSION)
            self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
            self.assertEqual(result.stdout, "")
            self.assertIn("corrupt archive member", result.stderr)

    def run_cli(self, directory: Path, *args: str) -> subprocess.CompletedProcess[str]:
        return subprocess.run([sys.executable, str(SCRIPT), str(directory), *args],
            text=True, capture_output=True, timeout=10)

    def test_cli_validates_the_complete_inventory(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            write_inventory(Path(directory))
            result = self.run_cli(Path(directory))
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(result.stdout, f"Validated {len(PACKAGE_IDS)} Hexalith.Agents packages at {VERSION}.\n")

    def test_cli_rejects_extra_archives_but_publication_selects_only_the_manifest(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            paths = write_inventory(root)
            unrelated = root / "Unrelated.Package.9.9.9.nupkg"
            unrelated.write_bytes(b"unrelated archive must not be read or pushed")
            result = self.run_cli(root)
            self.assertNotEqual(result.returncode, 0)
            result = self.run_cli(root, "--publication-version", VERSION)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(result.stdout.splitlines(), sorted(str(path) for path in paths))
            self.assertEqual(unrelated.read_bytes(), b"unrelated archive must not be read or pushed")

    def test_publication_preflights_missing_wrong_identity_and_wrong_version(self) -> None:
        for fault in ("missing", "wrong identity", "wrong version", "symlink"):
            with self.subTest(fault=fault), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                paths = write_inventory(root)
                target = paths[-1]
                if fault == "missing":
                    target.unlink()
                elif fault == "symlink":
                    content = root / "outside.nupkg"
                    target.rename(content)
                    target.symlink_to(content)
                else:
                    field = "id" if fault == "wrong identity" else "version"
                    write_package(root, PACKAGE_IDS[-1], mutate=lambda metadata: setattr(metadata.find(field), "text", "Other"))
                result = self.run_cli(root, "--publication-version", VERSION)
                self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
                self.assertEqual(result.stdout, "", "preflight must finish before emitting any push paths")
                self.assertIn("validate-nuget-packages:", result.stderr)
                self.assertNotIn("Traceback", result.stderr)

    def test_cli_rejects_invalid_publication_versions_without_emitting_paths(self) -> None:
        for version in ("", "../1.2.3", "1.2", "1.2.3\nInjected", "01.2.3", "1.2.3-..", "1.2.3-01"):
            with self.subTest(version=version), tempfile.TemporaryDirectory() as directory:
                result = self.run_cli(Path(directory), "--publication-version", version)
                self.assertEqual(result.returncode, 1)
                self.assertEqual(result.stdout, "")
                self.assertIn("invalid publication version", result.stderr)

    def test_publication_accepts_valid_prerelease_and_build_metadata(self) -> None:
        for version in ("0.0.0", "1.2.3-rc.1", "1.2.3-0", "1.2.3+build.01", "1.2.3-1a+build.1"):
            with self.subTest(version=version), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                for package_id in PACKAGE_IDS:
                    write_package(root, package_id, version=version)
                result = self.run_cli(root, "--publication-version", version)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual(len(result.stdout.splitlines()), len(PACKAGE_IDS))


if __name__ == "__main__":
    unittest.main()
