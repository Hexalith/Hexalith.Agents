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
                    mock.patch.object(HOOK, "check_hook_prerequisites"), \
                    mock.patch.object(HOOK, "pack_built_packages", side_effect=ValueError("product failure")):
                with self.assertRaisesRegex(ValueError, "product failure"):
                    HOOK.main()
            self.assertFalse(scratch.exists())

    def test_missing_system_tracer_refuses_before_product_commands_and_cleans_scratch(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary); scratch = root / "artifacts/cicd-agents-package-check"
            packages = [SimpleNamespace(package_id=name, project=root / path) for name, path in HOOK.EXPECTED_PROJECTS.items()]
            with mock.patch.object(HOOK, "ROOT", root), mock.patch.object(HOOK, "SCRATCH", scratch), \
                    mock.patch.object(Path, "cwd", return_value=root), mock.patch.object(HOOK, "load_packages", return_value=packages), \
                    mock.patch.object(HOOK.shutil, "which", return_value=None), mock.patch.object(HOOK, "pack_built_packages") as pack, \
                    mock.patch.object(HOOK.subprocess, "run") as command:
                with self.assertRaisesRegex(ValueError, "requires system strace"):
                    HOOK.main()
                pack.assert_not_called(); command.assert_not_called(); self.assertFalse(scratch.exists())

    def test_system_tracer_inventory_refuses_missing_and_truncated_output(self):
        from consumer_package_validation import strace_process_inventory
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            with self.assertRaisesRegex(ValueError, "strace process observation is missing"):
                strace_process_inventory(root)
            (root / "process-preprocess.2").write_text('execve("/dotnet", ["dotnet"], []) = 0\nexit_group(0) = ?\n+++ exited with 0 +++\n')
            with self.assertRaisesRegex(ValueError, "strace process observation is missing"):
                strace_process_inventory(root)
            path = root / "process-build.1"
            path.write_text('execve("/dotnet", ["dotnet"], []) = 0\n')
            with self.assertRaisesRegex(ValueError, "incomplete"):
                strace_process_inventory(root)
            path.write_text('clone(child_stack=NULL, flags=SIGCHLD <unfinished ...>\n')
            with self.assertRaisesRegex(ValueError, "incomplete"):
                strace_process_inventory(root)
            path.write_text('execve("/dotnet", ["dotnet"], []) = 0\nclone(child_stack=NULL, flags=CLONE_THREAD|CLONE_VM) = 3\nexit_group(0) = ?\n+++ exited with 0 +++\n')
            with self.assertRaisesRegex(ValueError, "incomplete"):
                strace_process_inventory(root)
            thread = root / "process-build.3"; thread.write_text('exit(0) = ?\n')
            with self.assertRaisesRegex(ValueError, "incomplete"):
                strace_process_inventory(root)
            thread.write_text('exit(0) = ?\n+++ exited with 0 +++\n')
            strace_process_inventory(root)
            self.assertTrue((root / "processes.log").read_text().endswith("complete\n"))

    def test_observation_environment_uses_absolute_paths_and_restores_inherited_settings(self):
        import os
        from consumer_package_validation import hook_observation_environment
        with tempfile.TemporaryDirectory() as temporary, mock.patch.dict(os.environ,
                {"LD_DEBUG": "libs", "LD_DEBUG_OUTPUT": "prior-output", "DOTNET_STARTUP_HOOKS": "prior-hook"}):
            root = Path(temporary); (root / "obj").mkdir()
            before = dict(os.environ)
            with hook_observation_environment(root):
                self.assertNotIn("LD_DEBUG", os.environ)
                self.assertEqual(str(root / "obj/assembly-loads/native"), os.environ["LD_DEBUG_OUTPUT"])
                self.assertEqual(before["PATH"], os.environ["PATH"])
                self.assertTrue(os.environ["DOTNET_STARTUP_HOOKS"].startswith("prior-hook" + os.pathsep))
            self.assertEqual(before, dict(os.environ))


if __name__ == "__main__":
    unittest.main()
