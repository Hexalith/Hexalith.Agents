"""Only the validated UI release version is variable in the executable import policy."""
import copy
import importlib.util
from pathlib import Path
import unittest
import tempfile
import sys
from unittest.mock import patch
import base64
import hashlib
import os
import subprocess

SOURCE = Path(__file__).resolve().parents[2] / "scripts/validate-consumer-package-references.py"
sys.path.insert(0, str(SOURCE.parent))
SPEC = importlib.util.spec_from_file_location("ui_consumer_assets", SOURCE)
CONSUMER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CONSUMER)


class UiConsumerAssetsTests(unittest.TestCase):
    def test_only_exact_empty_scratch_build_imports_are_generated_assets(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "Directory.Build.props"
            path.write_bytes(b"<Project />\n")
            observation = {"loads": {}, "nativeLoads": {}, "processes": [], "observer": ""}
            receipt = CONSUMER.executable_asset_receipt({"libraries": {}}, root / "packages", [], [str(path)], root, observation)
            self.assertEqual([{"path": "Directory.Build.props"}], receipt["generatedImports"])
            path.write_bytes(b"<Project><Target Name='Unexpected' /></Project>\n")
            with self.assertRaisesRegex(ValueError, "unowned executable consumer asset"):
                CONSUMER.executable_asset_receipt({"libraries": {}}, root / "packages", [], [str(path)], root, observation)

    def test_archive_membership_normalizes_only_unique_redundant_slashes(self):
        import zipfile
        from consumer_package_validation import archive_file_inventory
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "package.nupkg"
            with zipfile.ZipFile(path, "w") as archive:
                archive.writestr("analyzers/roslyn4.3/cs//MessagePack.Analyzers.CodeFixes.dll", b"reviewed")
            with zipfile.ZipFile(path) as archive:
                self.assertEqual(archive_file_inventory(archive), {"analyzers/roslyn4.3/cs/MessagePack.Analyzers.CodeFixes.dll"})
            for name in ("analyzers/../helper.dll", "/helper.dll", "analyzers\\helper.dll", "./helper.dll"):
                with zipfile.ZipFile(path, "w") as archive: archive.writestr(name, b"invalid")
                with zipfile.ZipFile(path) as archive, self.assertRaisesRegex(ValueError, "unsafe package archive member"):
                    archive_file_inventory(archive)
            with zipfile.ZipFile(path, "w") as archive:
                archive.writestr("analyzers/cs//helper.dll", b"first")
                archive.writestr("analyzers/cs/helper.dll", b"second")
            with zipfile.ZipFile(path) as archive, self.assertRaisesRegex(ValueError, "ambiguous normalized"):
                archive_file_inventory(archive)

    def test_no_receipt_still_checks_archive_inventory_and_schema2_policy(self):
        import json
        from nuget_package_fixtures import PACKAGE_IDS, VERSION, write_inventory, corrupt_archive_member
        with tempfile.TemporaryDirectory(prefix="agents-cli-contract-") as temporary:
            root = Path(temporary); feed = root / "feed"; feed.mkdir(); archives = write_inventory(feed)
            receipt = root / "absent/receipt.json"
            policy = root / "policy.json"
            policy.write_text(json.dumps({"schemaVersion": 2, "selected": [],
                "consumers": {label: [] for label in [*PACKAGE_IDS, "combined"]}}))
            def consumer(directory, identities, all_identities, label):
                return {"consumer": label, "selected": []}
            arguments = ["validate-consumer-package-references.py", str(feed), "--policy", str(policy), "--no-receipt", "--receipt", str(receipt)]
            with patch.object(sys, "argv", arguments), patch.object(CONSUMER.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "11.0.100\n")), \
                    patch.object(CONSUMER, "validate_consumer", side_effect=consumer) as build:
                self.assertEqual(CONSUMER.main(), 0)
                self.assertEqual(build.call_count, 7); self.assertFalse(receipt.exists())
                policy.write_text(json.dumps({"schemaVersion": 1, "selected": [], "consumers": {}}))
                with self.assertRaisesRegex(ValueError, "baseline changed"):
                    CONSUMER.main()
                build.reset_mock()
                corrupt_archive_member(archives[0], "lib/net11.0/" + PACKAGE_IDS[0] + ".dll")
                with self.assertRaises((ValueError, __import__("zipfile").BadZipFile)):
                    CONSUMER.main()
                build.assert_not_called(); self.assertFalse(receipt.exists())

    def fixture(self):
        actual = {"kind": "import", "package": "Hexalith.Agents.UI", "version": "1.2.3",
                  "path": "buildTransitive/Hexalith.Agents.UI.targets", "sha256": "a" * 64}
        expected = {**actual, "version": "$produced"}
        return ([{"consumer": "UI", "selected": [actual]}],
                {"schemaVersion": 2, "consumers": {"UI": [expected]}, "selected": [expected]})

    def test_only_validated_produced_version_varies_without_mutating_actual_receipt(self):
        receipts, policy = self.fixture()
        original = copy.deepcopy(receipts)
        CONSUMER.enforce_asset_policy(receipts, policy, "1.2.3")
        self.assertEqual(original, receipts)
        receipts[0]["selected"][0]["version"] = "2.0.0"
        CONSUMER.enforce_asset_policy(receipts, policy, "2.0.0")
        with self.assertRaises(ValueError):
            CONSUMER.enforce_asset_policy(receipts, policy, "1.2.3")
        with self.assertRaises(ValueError):
            CONSUMER.enforce_asset_policy(receipts, policy)
        del receipts[0]["selected"][0]["version"]
        with self.assertRaises(ValueError):
            CONSUMER.enforce_asset_policy(receipts, policy)

    def test_paths_bytes_kinds_and_package_identities_remain_exact(self):
        for field in ("path", "sha256", "kind", "package"):
            receipts, policy = self.fixture()
            receipts[0]["selected"][0][field] = "unreviewed"
            with self.subTest(field=field), self.assertRaises(ValueError):
                CONSUMER.enforce_asset_policy(receipts, policy, "1.2.3")

    def test_missing_or_incomplete_observation_refuses(self):
        import tempfile
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            with self.assertRaisesRegex(ValueError, "observation is required"):
                CONSUMER.executable_asset_receipt({"libraries": {}}, directory, [], [])
            with self.assertRaisesRegex(ValueError, "missing MSBuild"):
                CONSUMER.read_load_observation(directory, directory / "observer.dll")
            (directory / "1.loads").write_text("start\t\t\n")
            with self.assertRaisesRegex(ValueError, "incomplete"):
                CONSUMER.read_load_observation(directory, directory / "observer.dll")

    def test_unsupported_platform_refuses_before_build(self):
        with patch.object(CONSUMER.sys, "platform", "win32"):
            with self.assertRaisesRegex(ValueError, "requires the Linux dynamic-loader trace"):
                CONSUMER.observe_build(Path("missing"), Path("missing"), Path("missing"), Path("missing"))
            with patch.object(CONSUMER, "run") as run, patch.object(CONSUMER, "build_load_observer") as build, patch.object(CONSUMER.subprocess, "run") as child:
                with self.assertRaisesRegex(ValueError, "requires the Linux dynamic-loader trace"):
                    CONSUMER.validate_consumer(Path("missing"), [], [], "unsupported")
                run.assert_not_called(); build.assert_not_called(); child.assert_not_called()

    def test_missing_native_observation_or_unobserved_process_refuses(self):
        import tempfile
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            observer = directory / "Observer.dll"; observer.write_bytes(b"observer")
            msbuild = directory / "Microsoft.Build.dll"; msbuild.write_bytes(b"msbuild")
            def row(kind, path, digest=""):
                return kind + "\t" + base64.b64encode(str(path).encode()).decode() + "\t" + digest + "\n"
            (directory / "1.loads").write_text(row("start", directory / "csc.dll")
                + row("assembly", msbuild, hashlib.sha256(msbuild.read_bytes()).hexdigest())
                + row("assembly", observer, hashlib.sha256(observer.read_bytes()).hexdigest()) + "complete\t\t\n")
            with self.assertRaisesRegex(ValueError, "native load observation is missing"):
                CONSUMER.read_load_observation(directory, observer)
            (directory / "processes.log").write_text("created\t1\nexec\t1\ncomplete\n")
            (directory / "native.1").write_text("")
            with self.assertRaisesRegex(ValueError, "native executable load observation is missing"):
                CONSUMER.read_load_observation(directory, observer)
            (directory / "native.2").write_text("unobserved")
            with self.assertRaisesRegex(ValueError, "unobserved executable process"):
                CONSUMER.read_load_observation(directory, observer)

    def test_prerequisites_report_missing_native_compiler(self):
        with patch.object(CONSUMER.shutil, "which", return_value=None):
            with self.assertRaisesRegex(ValueError, "requires a native C compiler"):
                CONSUMER.check_consumer_prerequisites()

    def test_kernel_process_observer_preserves_sigtrap_and_retries_interrupted_waits(self):
        with tempfile.TemporaryDirectory(prefix="agents-kernel-observer-probe-") as temporary:
            root = Path(temporary); tracer = CONSUMER.build_process_observer(root)
            result = subprocess.run([str(tracer), str(root / "trap.log"), str(root / "trap-native"),
                "/bin/sh", "-c", "kill -TRAP $$"], cwd=root, capture_output=True, text=True)
            self.assertEqual(result.returncode, 128 + 5, result.stdout + result.stderr)
            self.assertTrue((root / "trap.log").read_text().endswith("complete\n"))
            shim = root / "interrupt.c"
            shim.write_text('#define _GNU_SOURCE\n#include <sys/types.h>\n#include <sys/wait.h>\n#include <dlfcn.h>\n#include <errno.h>\n'
                'pid_t waitpid(pid_t p,int *s,int o) { static int calls=0; int c=calls++; if(c==0 || c==2) {errno=EINTR;return -1;} '
                'pid_t (*real_wait)(pid_t,int*,int)=dlsym(RTLD_NEXT,"waitpid"); return real_wait(p,s,o); }')
            library = root / "interrupt.so"
            subprocess.run(["cc", "-shared", "-fPIC", str(shim), "-ldl", "-o", str(library)], check=True)
            result = subprocess.run([str(tracer), str(root / "interrupted.log"), str(root / "interrupted-native"),
                "/bin/true", "probe"], cwd=root, env={**os.environ, "LD_PRELOAD": str(library)}, capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertTrue((root / "interrupted.log").read_text().endswith("complete\n"))

    def test_kernel_process_observer_preserves_group_stop_and_waits_for_detached_descendants(self):
        with tempfile.TemporaryDirectory(prefix="agents-group-stop-probe-") as temporary:
            root = Path(temporary); tracer = CONSUMER.build_process_observer(root)
            source = root / "stop.c"
            source.write_text('#include <sys/types.h>\n#include <sys/wait.h>\n#include <signal.h>\n#include <unistd.h>\n'
                'int main(void) { pid_t child=fork(); if(child<0)return 1; if(child==0) {raise(SIGSTOP); _exit(0);} '
                'int status; if(waitpid(child,&status,WUNTRACED)!=child || !WIFSTOPPED(status) || WSTOPSIG(status)!=SIGSTOP)return 2; '
                'if(kill(child,SIGCONT)!=0)return 3; if(waitpid(child,&status,0)!=child || !WIFEXITED(status))return 4; return WEXITSTATUS(status); }')
            target = root / "stop"
            subprocess.run(["cc", str(source), "-o", str(target)], check=True)
            direct = subprocess.run([str(target)], cwd=root, capture_output=True, text=True, timeout=10)
            self.assertEqual(direct.returncode, 0, direct.stdout + direct.stderr)
            traced = subprocess.run([str(tracer), str(root / "stop.log"), str(root / "stop-native"), str(target), "probe"],
                cwd=root, capture_output=True, text=True, timeout=10)
            self.assertEqual(traced.returncode, direct.returncode, traced.stdout + traced.stderr)
            self.assertTrue((root / "stop.log").read_text().endswith("complete\n"))
            detached = root / "detached.c"
            detached.write_text('#include <sys/types.h>\n#include <sys/wait.h>\n#include <unistd.h>\n#include <stdio.h>\n'
                'int main(int argc,char **argv) { if(argc!=2)return 1; pid_t child=fork(); if(child<0)return 2; '
                'if(child==0) {if(setsid()<0)_exit(3); pid_t grandchild=fork(); if(grandchild<0)_exit(4); if(grandchild>0)_exit(0); '
                'usleep(100000); FILE *f=fopen(argv[1],"w"); if(!f)_exit(5); fputs("finished",f); fclose(f); _exit(0);} '
                'int status; if(waitpid(child,&status,0)!=child || !WIFEXITED(status))return 6; return WEXITSTATUS(status); }')
            detached_target = root / "detached"
            subprocess.run(["cc", str(detached), "-o", str(detached_target)], check=True)
            marker = root / "detached-finished.txt"
            result = subprocess.run([str(tracer), str(root / "detached.log"), str(root / "detached-native"),
                str(detached_target), str(marker)], cwd=root, capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
            self.assertEqual(marker.read_text(), "finished")
            self.assertTrue((root / "detached.log").read_text().endswith("complete\n"))

    def test_external_versions_are_never_normalized(self):
        receipts, policy = self.fixture()
        receipts[0]["selected"][0]["package"] = "External.Library"
        policy["consumers"]["UI"][0]["package"] = "External.Library"
        policy["selected"][0]["package"] = "External.Library"
        with self.assertRaises(ValueError):
            CONSUMER.enforce_asset_policy(receipts, policy, "1.2.3")


if __name__ == "__main__":
    unittest.main()
