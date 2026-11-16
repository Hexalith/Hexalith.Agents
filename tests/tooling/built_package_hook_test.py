"""The built-package hook has one fixed inventory and cleans failed scratch."""
from __future__ import annotations

import importlib.util
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "scripts"))
SPEC = importlib.util.spec_from_file_location("built_package_hook", ROOT / "scripts/check-built-packages.py")
HOOK = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HOOK)


class BuiltPackageHookTests(unittest.TestCase):
    def test_fixed_inventory_packs_each_project_without_module_build_or_restore(self):
        with mock.patch.object(HOOK.subprocess, "run") as run:
            HOOK.pack_built_packages()
        calls = [call.args[0] for call in run.call_args_list]
        self.assertEqual(set(HOOK.EXPECTED_PROJECTS.values()), {call[2] for call in calls})
        self.assertEqual(6, len(calls))
        for call in calls:
            self.assertEqual(["dotnet", "pack"], call[:2])
            self.assertIn("--no-build", call)
            self.assertIn("--no-restore", call)

    def test_manifest_disagreement_fails_before_packing(self):
        with mock.patch.object(HOOK, "ROOT", Path.cwd()), \
                mock.patch.object(HOOK, "load_packages", return_value=[]), \
                mock.patch.object(HOOK, "pack_built_packages") as pack:
            with self.assertRaisesRegex(ValueError, "release manifest disagrees"):
                HOOK.main()
            pack.assert_not_called()

    def test_failed_check_cleans_its_reserved_scratch(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            scratch = root / "artifacts/cicd-agents-package-check"
            scratch.mkdir(parents=True)
            (scratch / "stale").write_text("previous failed job")
            packages = [SimpleNamespace(package_id=name, project=root / path)
                        for name, path in HOOK.EXPECTED_PROJECTS.items()]
            with mock.patch.object(HOOK, "ROOT", root), mock.patch.object(HOOK, "SCRATCH", scratch), \
                    mock.patch.object(Path, "cwd", return_value=root), \
                    mock.patch.object(HOOK, "load_packages", return_value=packages), \
                    mock.patch.object(HOOK, "pack_built_packages", side_effect=ValueError("product failure")):
                with self.assertRaisesRegex(ValueError, "product failure"):
                    HOOK.main()
            self.assertFalse(scratch.exists())


if __name__ == "__main__":
    unittest.main()
