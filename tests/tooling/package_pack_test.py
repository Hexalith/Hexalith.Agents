"""Packing reuses built output and protects archives owned by the caller."""

from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
import tempfile
import unittest
import zipfile
from unittest import mock


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "scripts"))
SPEC = importlib.util.spec_from_file_location("pack_release_packages", ROOT / "scripts/pack-release-packages.py")
PACK = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = PACK
SPEC.loader.exec_module(PACK)


class PackagePackTests(unittest.TestCase):
    def test_no_build_packs_each_manifest_project_once_without_restore(self):
        packages = [PACK.ReleasePackage("A", ROOT / "src/A.csproj"),
                    PACK.ReleasePackage("B", ROOT / "src/B.csproj")]
        with tempfile.TemporaryDirectory() as temporary, \
                mock.patch.object(PACK, "load_packages", return_value=packages), \
                mock.patch.object(PACK.subprocess, "run") as run, \
                mock.patch.object(sys, "argv", ["pack-release-packages.py", temporary, "1.2.3", "--no-build", "--no-restore"]):
            self.assertEqual(0, PACK.main())
            self.assertEqual(2, run.call_count)
            for call in run.call_args_list:
                command = call.args[0]
                self.assertEqual(["dotnet", "pack"], command[:2])
                self.assertIn("--no-build", command)
                self.assertIn("--no-restore", command)

    def test_no_build_cannot_implicitly_restore(self):
        with tempfile.TemporaryDirectory() as temporary, \
                mock.patch.object(PACK, "load_packages") as load, \
                mock.patch.object(sys, "argv", ["pack-release-packages.py", temporary, "1.2.3", "--no-build"]):
            with self.assertRaises(SystemExit) as error:
                PACK.main()
            self.assertEqual(2, error.exception.code)
            load.assert_not_called()

    def test_cleanup_keeps_unrelated_and_malformed_caller_archives(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for name, identity in [("A.1.0.0.nupkg", "A"), ("B.1.0.0.nupkg", "B")]:
                with zipfile.ZipFile(root / name, "w") as archive:
                    archive.writestr("package.nuspec", f"<package><metadata><id>{identity}</id></metadata></package>")
            malformed = root / "A.malformed.nupkg"
            malformed.write_bytes(b"caller data")
            PACK.clean_manifest_archives(root, frozenset({"A"}))
            self.assertFalse((root / "A.1.0.0.nupkg").exists())
            self.assertTrue((root / "B.1.0.0.nupkg").exists())
            self.assertEqual(b"caller data", malformed.read_bytes())


if __name__ == "__main__":
    unittest.main()
