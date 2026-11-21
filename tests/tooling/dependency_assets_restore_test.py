"""Prove dependency asset metadata behavior with real isolated NuGet restores."""

from __future__ import annotations

import json
import importlib
from unittest.mock import patch
import sys
import os
import subprocess
import tempfile
import unittest
import zipfile
from pathlib import Path
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "scripts"))
CONSUMER = importlib.import_module("validate-consumer-package-references")


class DependencyAssetRestoreTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        CONSUMER.check_consumer_prerequisites()
        cls.fixture_directory = tempfile.TemporaryDirectory(prefix="agents-executable-fixtures-")
        cls.fixture = Path(cls.fixture_directory.name)
        (cls.fixture / "global.json").write_bytes((ROOT / "global.json").read_bytes())
        cls.native_original = cls.compile_native("NativeOriginal", 1)
        cls.native_changed = cls.compile_native("NativeChanged", 2)
        cls.helper_original = cls.compile_fixture("HelperOriginal", "Probe.Helper",
            'using System.Runtime.InteropServices; namespace Probe.Helper; public static class Values { [DllImport("ProbeNative")] private static extern int ProbeValue(); public static string Message => "original" + ProbeValue(); }')
        cls.helper_changed = cls.compile_fixture("HelperChanged", "Probe.Helper",
            'using System.Runtime.InteropServices; namespace Probe.Helper; public static class Values { [DllImport("ProbeNative")] private static extern int ProbeValue(); public static string Message => "changed" + ProbeValue(); }')
        cls.analyzer = cls.compile_fixture("Analyzer", "Probe", r'''using System; using System.IO; using System.Reflection;
using Microsoft.CodeAnalysis;
[Generator] public sealed class ProbeGenerator : ISourceGenerator {
 public void Initialize(GeneratorInitializationContext context) { }
 public void Execute(GeneratorExecutionContext context) {
  var helper = Assembly.LoadFrom(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(ProbeGenerator).Assembly.Location)!, "../../../tools/Probe.Helper.dll")));
  var value = (string)helper.GetType("Probe.Helper.Values")!.GetProperty("Message")!.GetValue(null)!;
  context.AddSource("ProbeGenerated.g.cs", "public static class ProbeGenerated { public const string Value = \"" + value + "\"; }");
 }
}''', ["Roslyn/bincore/Microsoft.CodeAnalysis.dll"])
        cls.contracts = cls.compile_fixture("Contracts", "Probe.Contracts",
            'namespace Hexalith.Agents.Contracts.Agent; public enum AgentLifecycleStatus { Unknown }')
        cls.byte_helper = cls.compile_fixture("ByteHelper", "Probe.ByteHelper",
            'using System.IO; public static class ByteExecution { public static void Execute(string marker) => File.WriteAllText(marker, "executed"); }')
        cls.native_processes = {}
        for label, flags in (("static", ["-static"]), ("sanitized", [])):
            source = cls.fixture / (label + ".c")
            source.write_text('#include <stdio.h>\nint main(int argc, char **argv) { if(argc != 2) return 2; FILE *f=fopen(argv[1],"w"); if(!f)return 3; fputs("executed",f); fclose(f); return 0; }')
            target = cls.fixture / label
            built = subprocess.run(["cc", *flags, str(source), "-o", str(target)], capture_output=True, text=True, timeout=30)
            if built.returncode:
                raise AssertionError("Executable qualification fixtures require cc and static libc development files: " + built.stdout + built.stderr)
            cls.native_processes[label] = target
        cls.task = cls.compile_fixture("Task", "Probe.Task", r'''using System; using System.IO; using System.Reflection;
public sealed class ProbeTask : Microsoft.Build.Utilities.Task {
 public override bool Execute() {
  var helper = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(typeof(ProbeTask).Assembly.Location)!, "Probe.Helper.dll"));
  var value = (string)helper.GetType("Probe.Helper.Values")!.GetProperty("Message")!.GetValue(null)!;
  File.WriteAllText("task-helper-observed.txt", value);
  var marker = Environment.GetEnvironmentVariable("PROBE_TASK_MARKER");
  if (marker is not null) File.AppendAllText(marker, "run\n");
  string? action = Environment.GetEnvironmentVariable("PROBE_ACTION");
  string? effect = Environment.GetEnvironmentVariable("PROBE_EFFECT");
  string directory = Path.GetDirectoryName(typeof(ProbeTask).Assembly.Location)!;
  if (action is "static" or "sanitized") {
   var start = new System.Diagnostics.ProcessStartInfo(Environment.GetEnvironmentVariable("PROBE_PROCESS")!) { UseShellExecute = false };
   start.ArgumentList.Add(effect!);
   if (action == "sanitized") start.Environment.Clear();
   using var child = System.Diagnostics.Process.Start(start)!; child.WaitForExit();
   if (child.ExitCode != 0) throw new Exception("child failed");
  }
  if (action == "restore-native") {
   File.WriteAllText(effect!, value);
   string replacement = Path.Combine(directory, "replacement-native.so");
   File.Copy(Environment.GetEnvironmentVariable("PROBE_REVIEWED_NATIVE")!, replacement);
   File.Move(replacement, Path.Combine(directory, "libProbeNative.so"), true);
  }
  if (action == "byte-loaded") {
   Assembly bytes = Assembly.Load(File.ReadAllBytes(Path.Combine(directory, "Probe.ByteHelper.dll")));
   bytes.GetType("ByteExecution")!.GetMethod("Execute")!.Invoke(null, new object[] { effect! });
  }
  if (action == "dynamic") {
   var assembly = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Probe.Dynamic"), System.Reflection.Emit.AssemblyBuilderAccess.Run);
   var type = assembly.DefineDynamicModule("Probe.Dynamic").DefineType("DynamicExecution", TypeAttributes.Public);
   var method = type.DefineMethod("Execute", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
   var il = method.GetILGenerator();
   il.Emit(System.Reflection.Emit.OpCodes.Ldstr, effect!); il.Emit(System.Reflection.Emit.OpCodes.Ldstr, "executed");
   il.Emit(System.Reflection.Emit.OpCodes.Call, typeof(File).GetMethod("WriteAllText", new[] { typeof(string), typeof(string) })!);
   il.Emit(System.Reflection.Emit.OpCodes.Ret); type.CreateType()!.GetMethod("Execute")!.Invoke(null, null);
  }
  return true;
 }
}''', ["Microsoft.Build.Framework.dll", "Microsoft.Build.Utilities.Core.dll"])

    @classmethod
    def tearDownClass(cls) -> None:
        cls.fixture_directory.cleanup()

    @classmethod
    def compile_native(cls, label: str, value: int) -> bytes:
        directory = cls.fixture / label
        directory.mkdir()
        source = directory / "helper.c"
        source.write_text(f"int ProbeValue(void) {{ return {value}; }}")
        library = directory / "libProbeNative.so"
        subprocess.run(["cc", "-shared", "-fPIC", str(source), "-o", str(library)], check=True, timeout=30)
        return library.read_bytes()

    @classmethod
    def compile_fixture(cls, label: str, assembly: str, source: str, references: list[str] = ()) -> bytes:
        directory = cls.fixture / label
        directory.mkdir()
        (directory / "Fixture.cs").write_text(source)
        refs = "".join(f'<Reference Include="{Path(path).stem}"><HintPath>$(MSBuildBinPath)/{path}</HintPath></Reference>' for path in references)
        project = directory / "Fixture.csproj"
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework>'
            f'<AssemblyName>{assembly}</AssemblyName><Nullable>enable</Nullable><NuGetAudit>true</NuGetAudit>'
            '<UseSharedCompilation>false</UseSharedCompilation></PropertyGroup><ItemGroup>' + refs + '</ItemGroup></Project>')
        result = subprocess.run(["dotnet", "build", str(project), "-c", "Release", "/m:1", "/nr:false"],
            cwd=directory, text=True, capture_output=True, timeout=60)
        if result.returncode:
            raise AssertionError(result.stdout + result.stderr)
        return (directory / f"bin/Release/net11.0/{assembly}.dll").read_bytes()

    def test_actual_messagepack_archive_matches_nuget_normalized_file_inventory(self) -> None:
        with tempfile.TemporaryDirectory(prefix="agents-messagepack-membership-") as temporary:
            root = Path(temporary)
            (root / "global.json").write_bytes((ROOT / "global.json").read_bytes())
            project = root / "Consumer.csproj"
            project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework><NuGetAudit>true</NuGetAudit></PropertyGroup>'
                '<ItemGroup><PackageReference Include="MessagePackAnalyzer" Version="3.1.12"/></ItemGroup></Project>')
            config = root / "NuGet.config"
            config.write_text('<configuration><packageSources><clear/><add key="nuget.org" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>')
            packages = root / "packages"
            self.run_command(root, "restore", str(project), "--configfile", str(config), "--packages", str(packages), "--no-cache", "--force-evaluate")
            assets = json.loads((root / "obj/project.assets.json").read_text())
            member = "analyzers/roslyn4.3/cs/MessagePack.Analyzers.CodeFixes.dll"
            self.assertIn(member, assets["libraries"]["MessagePackAnalyzer/3.1.12"]["files"])
            package = packages / "messagepackanalyzer/3.1.12"
            with zipfile.ZipFile(package / "messagepackanalyzer.3.1.12.nupkg") as archive:
                self.assertIn("analyzers/roslyn4.3/cs//MessagePack.Analyzers.CodeFixes.dll", archive.namelist())
            observation = {"loads": {}, "nativeLoads": {}, "processes": [], "observer": ""}
            receipt = CONSUMER.executable_asset_receipt(assets, packages, [str(package / member)], [], root, observation)
            self.assertEqual(receipt["selected"][0]["path"], member)
            self.assertEqual(receipt["selected"][0]["package"], "MessagePackAnalyzer")
            injected = package / "analyzers/roslyn4.3/cs/Injected.dll"; injected.write_bytes((package / member).read_bytes())
            assets["libraries"]["MessagePackAnalyzer/3.1.12"]["files"].append("analyzers/roslyn4.3/cs/Injected.dll")
            with self.assertRaisesRegex(ValueError, "exact archive/library inventory"):
                CONSUMER.executable_asset_receipt(assets, packages, [str(injected)], [], root, observation)

    def test_actual_product_hook_observes_consumer_and_refuses_executable_adversaries(self) -> None:
        import importlib.util
        import consumer_package_validation as shared
        spec = importlib.util.spec_from_file_location("actual_product_hook", ROOT / "scripts/check-built-packages.py")
        hook = importlib.util.module_from_spec(spec); spec.loader.exec_module(hook)
        shared.check_hook_prerequisites()
        for action in ("valid", "static", "sanitized", "byte-loaded", "dynamic", "restore-native"):
            with self.subTest(action=action), tempfile.TemporaryDirectory(prefix="agents-actual-package-hook-") as temporary:
                root = Path(temporary); feed = root / "feed"; feed.mkdir()
                package_id = "Hexalith.Agents.Contracts"
                with zipfile.ZipFile(feed / (package_id + ".1.0.0.nupkg"), "w") as archive:
                    archive.writestr(package_id + ".nuspec", '<package><metadata><id>' + package_id + '</id><version>1.0.0</version><authors>Fixture</authors><description>Actual hook fixture</description></metadata></package>')
                    archive.writestr("lib/net11.0/Probe.Contracts.dll", self.contracts)
                    archive.writestr("build/" + package_id + ".targets", '<Project><UsingTask TaskName="ProbeTask" AssemblyFile="../tools/Probe.Task.dll"/>'
                        '<Import Project="conditional.xml" Condition="' + "'$(UseSharedCompilation)' == 'false'" + '"/>'
                        '<Target Name="RunProbeTask" BeforeTargets="CoreCompile"><ProbeTask/></Target></Project>')
                    archive.writestr("build/conditional.xml", '<Project><PropertyGroup><ProbeConditional>Present</ProbeConditional></PropertyGroup></Project>')
                    archive.writestr("tools/Probe.Task.dll", self.task)
                    archive.writestr("tools/Probe.Helper.dll", self.helper_original)
                    archive.writestr("tools/Probe.ByteHelper.dll", self.byte_helper)
                    archive.writestr("tools/libProbeNative.so", self.native_changed if action == "restore-native" else self.native_original)
                scratch = root / "artifacts/cicd-agents-package-check"; consumer = scratch / "consumer"
                shared.prepare_consumer(consumer, feed, [(package_id, "1.0.0")], [(package_id, "1.0.0")])
                effect = root / "executed.txt"; marker = root / "task-count.txt"
                reviewed = root / "reviewed-native.so"; reviewed.write_bytes(self.native_original)
                environment = {"PROBE_ACTION": action, "PROBE_EFFECT": str(effect), "PROBE_TASK_MARKER": str(marker),
                    "PROBE_REVIEWED_NATIVE": str(reviewed), "PROBE_PROCESS": str(self.native_processes.get(action, "unused"))}
                previous = Path.cwd(); before_hooks = os.environ.get("DOTNET_STARTUP_HOOKS")
                try:
                    os.chdir(root)
                    with patch.object(hook, "SCRATCH", scratch), patch.dict(os.environ, environment):
                        if action == "valid":
                            observation = hook.check_consumer()
                            shared.validate_consumer_assets(consumer, feed, [(package_id, "1.0.0")], [(package_id, "1.0.0")])
                            receipt = shared.consumer_receipt(consumer, package_id, observation)
                            self.assertTrue(any(item["kind"] == "assembly" and item["path"] == "tools/Probe.Task.dll" for item in receipt["selected"]))
                            self.assertTrue(any(item["kind"] == "native" and item["path"] == "tools/libProbeNative.so" for item in receipt["selected"]))
                            self.assertTrue(any(item["path"] == "build/conditional.xml" for item in receipt["selected"]))
                            self.assertEqual(receipt["systemProcessObservers"][0]["name"], "strace")
                            baseline = {"schemaVersion": 3, "selected": receipt["selected"], "consumers": {package_id: receipt["selected"]}}
                            shared.enforce_asset_policy([receipt], baseline)
                        else:
                            expected = "mutation" if action == "restore-native" else "unattributed executable load" if action in ("byte-loaded", "dynamic") else "unobserved"
                            with self.assertRaisesRegex(ValueError, expected): hook.check_consumer()
                            self.assertEqual(effect.read_text(), "original2" if action == "restore-native" else "executed")
                            if action == "restore-native":
                                self.assertEqual((consumer / "packages/hexalith.agents.contracts/1.0.0/tools/libProbeNative.so").read_bytes(), self.native_original)
                finally:
                    os.chdir(previous)
                self.assertEqual(os.environ.get("DOTNET_STARTUP_HOOKS"), before_hooks)
                self.assertEqual(marker.read_text().splitlines(), ["run"])

    def test_actual_system_tracer_exit_kills_detached_descendants(self) -> None:
        import signal
        import time
        import consumer_package_validation as shared
        shared.check_hook_prerequisites()
        with tempfile.TemporaryDirectory(prefix="agents-strace-cleanup-") as temporary:
            root = Path(temporary); source = root / "detached.c"; program = root / "detached"
            source.write_text('#include <unistd.h>\n#include <stdio.h>\nint main(int argc,char **argv) {'
                'if(argc!=2)return 2; pid_t child=fork(); if(child<0)return 3; if(child>0)_exit(0); '
                'if(setsid()<0)return 4; FILE *f=fopen(argv[1],"w"); if(!f)return 5; '
                'fprintf(f,"%ld",(long)getpid()); fclose(f); for(;;)pause(); }')
            subprocess.run(["cc", str(source), "-o", str(program)], check=True, timeout=30)
            marker = root / "child.pid"; child = None
            tracer = subprocess.Popen(["strace", "--kill-on-exit", "-ff", "-q", "-e", "trace=process",
                "-o", str(root / "process"), str(program), str(marker)], stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
            try:
                deadline = time.monotonic() + 10
                while (not marker.exists() or not marker.stat().st_size) and tracer.poll() is None and time.monotonic() < deadline:
                    time.sleep(0.02)
                self.assertTrue(marker.exists(), "strace must support --kill-on-exit and follow detached descendants")
                child = int(marker.read_text())
                self.assertIsNone(tracer.poll(), "strace returned before its detached descendant completed")
                tracer.kill(); tracer.communicate(timeout=10)
                status = Path(f"/proc/{child}/stat")
                def running():
                    try: return status.read_text().split()[2] != "Z"
                    except FileNotFoundError: return False
                while running() and time.monotonic() < deadline:
                    time.sleep(0.02)
                self.assertFalse(running(), "detached traced child survived tracer exit")
            finally:
                if tracer.poll() is None: tracer.kill()
                tracer.communicate(timeout=10)
                if child is not None:
                    try: os.kill(child, signal.SIGKILL)
                    except ProcessLookupError: pass

    def test_full_consumer_runs_package_tasks_only_in_the_observed_build(self) -> None:
        with tempfile.TemporaryDirectory(prefix="agents-single-observed-build-") as temporary:
            root = Path(temporary); feed = root / "feed"; feed.mkdir()
            package_id = "Hexalith.Agents.Contracts"
            nuspec = '<package><metadata><id>' + package_id + '</id><version>1.0.0</version><authors>Fixture</authors><description>Observed build task</description></metadata></package>'
            with zipfile.ZipFile(feed / (package_id + ".1.0.0.nupkg"), "w") as archive:
                archive.writestr(package_id + ".nuspec", nuspec)
                archive.writestr("lib/net11.0/Probe.Contracts.dll", self.contracts)
                archive.writestr("build/" + package_id + ".targets", '<Project><UsingTask TaskName="ProbeTask" AssemblyFile="../tools/Probe.Task.dll"/>'
                    '<Import Project="conditional.xml" Condition="' + "'$(UseSharedCompilation)' == 'false'" + '"/>'
                    '<Target Name="RunProbeTask" BeforeTargets="CoreCompile"><ProbeTask/></Target></Project>')
                archive.writestr("build/conditional.xml", '<Project><PropertyGroup><ProbeConditional>Present</ProbeConditional></PropertyGroup></Project>')
                archive.writestr("tools/Probe.Task.dll", self.task)
                archive.writestr("tools/Probe.Helper.dll", self.helper_original)
                archive.writestr("tools/libProbeNative.so", self.native_original)
            marker = root / "task-executions.txt"
            with patch.dict(os.environ, {"PROBE_TASK_MARKER": str(marker)}):
                receipt = CONSUMER.validate_consumer(feed, [(package_id, "1.0.0")], [(package_id, "1.0.0")], package_id)
            self.assertEqual(marker.read_text().splitlines(), ["run"])
            self.assertTrue(any(item["kind"] == "import" and item["path"] == "build/conditional.xml" for item in receipt["selected"]))
            self.assertTrue(any(item["kind"] == "assembly" and item["path"] == "tools/Probe.Task.dll" for item in receipt["selected"]))
            self.assertTrue(any(item["kind"] == "native" and item["path"] == "tools/libProbeNative.so" for item in receipt["selected"]))

    def test_actual_unobserved_processes_and_fileless_assemblies_refuse(self) -> None:
        for action in ("static", "sanitized", "byte-loaded", "dynamic", "restore-native"):
            with self.subTest(action=action), tempfile.TemporaryDirectory(prefix="agents-observation-adversary-") as temporary:
                root = Path(temporary); feed = root / "feed"; feed.mkdir()
                (root / "global.json").write_bytes((ROOT / "global.json").read_bytes())
                self.write_package(feed, "Probe.Leaf", None)
                project = root / "Consumer.csproj"
                project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework><NuGetAudit>true</NuGetAudit></PropertyGroup>'
                    '<ItemGroup><PackageReference Include="Probe.Leaf" Version="1.0.0"/></ItemGroup></Project>')
                (root / "Use.cs").write_text('public class Use { public string Value => ProbeGenerated.Value; }')
                config = root / "NuGet.Config"
                config.write_text('<configuration><packageSources><clear/><add key="local" value="' + str(feed) + '"/></packageSources></configuration>')
                packages = root / "packages"
                self.run_command(root, "restore", str(project), "--configfile", str(config), "--packages", str(packages))
                observer = CONSUMER.build_load_observer(root, packages)
                native = packages / "probe.leaf/1.0.0/tools/libProbeNative.so"
                reviewed = root / "reviewed-native.so"; reviewed.write_bytes(self.native_original)
                if action == "restore-native": native.write_bytes(self.native_changed)
                effect = root / "executed.txt"
                environment = {"PROBE_ACTION": action, "PROBE_EFFECT": str(effect), "PROBE_REVIEWED_NATIVE": str(reviewed),
                    "PROBE_PROCESS": str(self.native_processes.get(action, "unused"))}
                expected = "mutation" if action == "restore-native" else "unattributed executable load" if action in ("byte-loaded", "dynamic") else "unobserved"
                with patch.dict(os.environ, environment), self.assertRaisesRegex(ValueError, expected):
                    CONSUMER.observe_build(project, root, packages, observer)
                self.assertEqual(effect.read_text(), "original2" if action == "restore-native" else "executed")
                if action == "restore-native": self.assertEqual(native.read_bytes(), self.native_original)

    def test_package_provenance_requires_archive_and_library_membership(self) -> None:
        with tempfile.TemporaryDirectory(prefix="agents-package-provenance-") as temporary:
            root = Path(temporary); package = root / "probe/1.0.0"; package.mkdir(parents=True)
            asset = package / "helper.dll"; asset.write_bytes(b"executable")
            with zipfile.ZipFile(package / "probe.1.0.0.nupkg", "w") as archive: archive.writestr("reviewed.dll", b"reviewed")
            observation = {"loads": {}, "nativeLoads": {}, "observer": "unused", "processes": []}
            for library_files in ([], ["helper.dll"]):
                assets = {"libraries": {"Probe/1.0.0": {"type": "package", "path": "probe/1.0.0", "files": library_files}}}
                with self.assertRaisesRegex(ValueError, "exact archive/library inventory"):
                    CONSUMER.executable_asset_receipt(assets, root, [str(asset)], [], observation=observation)
            with zipfile.ZipFile(package / "probe.1.0.0.nupkg", "a") as archive: archive.writestr("helper.dll", b"executable")
            assets["libraries"]["Probe/1.0.0"]["files"] = []
            with self.assertRaisesRegex(ValueError, "exact archive/library inventory"):
                CONSUMER.executable_asset_receipt(assets, root, [str(asset)], [], observation=observation)

    def test_removing_generated_exclusions_imports_a_dependency_build_target(self) -> None:
        variants = (
            ("generated", {"exclude": "Build,Analyzers"}, False),
            ("omitted", {}, True),
            ("analyzers-only", {"exclude": "Analyzers"}, True),
            ("include-runtime", {"include": "Compile,Runtime"}, False),
        )
        sdk = json.loads((Path(__file__).resolve().parents[2] / "global.json").read_text())["sdk"]
        with tempfile.TemporaryDirectory(prefix="agents-dependency-assets-") as temporary:
            (Path(temporary) / "global.json").write_text(json.dumps({"sdk": sdk}), encoding="utf-8")
            for name, attrs, imports_build in variants:
                with self.subTest(variant=name):
                    root = Path(temporary) / name
                    feed = root / "feed"
                    feed.mkdir(parents=True)
                    self.write_package(feed, "Probe.Leaf", None)
                    self.write_package(feed, "Probe.Transitive", None)
                    self.write_package(feed, "Probe.Wrapper", attrs)
                    project = root / "Consumer.csproj"
                    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                        '<TargetFramework>net11.0</TargetFramework><NuGetAudit>true</NuGetAudit>'
                        '<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles><CompilerGeneratedFilesOutputPath>obj/generated</CompilerGeneratedFilesOutputPath>'
                        '</PropertyGroup><ItemGroup><PackageReference Include="Probe.Wrapper" Version="1.0.0"/>'
                        '</ItemGroup></Project>', encoding="utf-8")
                    (root / "ProbeUse.cs").write_text("public class ProbeUse { public string Value => ProbeGenerated.Value; }")
                    config = root / "NuGet.Config"
                    configuration = ElementTree.Element("configuration")
                    sources = ElementTree.SubElement(configuration, "packageSources")
                    ElementTree.SubElement(sources, "clear")
                    ElementTree.SubElement(sources, "add", {"key": "local", "value": str(feed)})
                    ElementTree.ElementTree(configuration).write(config, encoding="utf-8")
                    self.run_command(root, "restore", str(project), "--configfile", str(config),
                                     "--packages", str(root / "packages"), "--verbosity", "quiet")
                    assets = json.loads((root / "obj/project.assets.json").read_text())
                    target = assets["targets"]["net11.0"]["Probe.Leaf/1.0.0"]
                    selected = "build/Probe.Leaf.targets" in target.get("build", {})
                    self.assertEqual(selected, imports_build)
                    result = self.run_command(root, "msbuild", str(project), "-nologo",
                        "-target:ResolveLockFileAnalyzers", "-getProperty:ProbeBuildImported", "-getItem:Analyzer")
                    evaluated = json.loads(result.stdout)
                    self.assertEqual(evaluated["Properties"]["ProbeBuildImported"], "Present" if imports_build else "")
                    # This metadata does not suppress transitive analyzer items; avoid a false purity claim.
                    self.assertTrue(any(Path(item["Identity"]).name == "Probe.dll"
                        for item in evaluated["Items"]["Analyzer"]))
                    expanded = root / "imports.xml"
                    self.run_command(root, "msbuild", str(project), "-nologo", f"-preprocess:{expanded}")
                    imports = CONSUMER.active_import_paths(expanded.read_text())
                    observer = CONSUMER.build_load_observer(root, root / "packages")
                    observation = CONSUMER.observe_build(project, root, root / "packages", observer)
                    receipt = CONSUMER.executable_asset_receipt(assets, root / "packages",
                        [item["Identity"] for item in evaluated["Items"]["Analyzer"]], imports, root, observation)
                    selected = receipt["selected"]
                    self.assertTrue(any(item["path"].endswith("libhostfxr.so") for item in receipt["sdkNativeLibraries"]))
                    self.assertTrue(any("libc.so" in item["path"] for item in receipt["systemNativeLibraries"]))
                    self.assertTrue(any(item["kind"] == "native" and item["path"] == "tools/libProbeNative.so" for item in selected))
                    generated_sources = list((root / "obj/generated").rglob("ProbeGenerated.g.cs"))
                    self.assertEqual(len(generated_sources), 1)
                    self.assertIn('"original1"', generated_sources[0].read_text())
                    self.assertTrue(any(item["path"] == "analyzers/dotnet/cs/Probe.dll" for item in selected))
                    self.assertEqual(any(item["path"] == "build/Probe.Leaf.targets" for item in selected), imports_build)
                    self.assertEqual(any(item["path"] == "buildTransitive/Probe.Transitive.targets" for item in selected),
                        name != "include-runtime")
                    self.assertTrue(any(item["path"] == "build/Probe.Leaf.targets" for item in receipt["available"]))
                    self.assertEqual(any(item["path"] == "build/helper.xml" for item in selected), imports_build)
                    self.assertTrue(all(item["package"] in {"Probe.Leaf", "Probe.Transitive"} and item["version"] == "1.0.0"
                        and len(item["sha256"]) == 64 for item in selected))
                    receipt["consumer"] = name
                    reviewed = CONSUMER.policy_selection(selected)
                    baseline = {"schemaVersion": 3, "selected": reviewed, "consumers": {name: reviewed}}
                    CONSUMER.enforce_asset_policy([receipt], baseline)
                    if imports_build:
                        helper = root / "packages/probe.leaf/1.0.0/build/helper.xml"
                        helper.write_text('<Project><PropertyGroup><ProbeHelper>Changed</ProbeHelper></PropertyGroup></Project>')
                        changed_receipt = CONSUMER.executable_asset_receipt(assets, root / "packages",
                            [item["Identity"] for item in evaluated["Items"]["Analyzer"]], imports, root, observation)
                        with self.assertRaisesRegex(ValueError, "baseline changed"):
                            CONSUMER.enforce_asset_policy([{**changed_receipt, "consumer": name}], baseline)
                        helper.write_text('<Project><PropertyGroup><ProbeHelper>Original</ProbeHelper></PropertyGroup></Project>')
                        restored = CONSUMER.executable_asset_receipt(assets, root / "packages",
                            [item["Identity"] for item in evaluated["Items"]["Analyzer"]], imports, root, observation)
                        CONSUMER.enforce_asset_policy([{**restored, "consumer": name}], baseline)
                    self.assertTrue(any(item["kind"] == "assembly" and item["path"] == "tools/Probe.Helper.dll" for item in selected))
                    self.assertEqual(any(item["kind"] == "assembly" and item["path"] == "tools/Probe.Task.dll" for item in selected), imports_build)
                    if name == "omitted":
                        entry_hash = next(item["sha256"] for item in selected if item["path"] == "analyzers/dotnet/cs/Probe.dll")
                        task_hash = next(item["sha256"] for item in selected if item["path"] == "tools/Probe.Task.dll")
                        (root / "packages/probe.leaf/1.0.0/tools/Probe.Helper.dll").write_bytes(self.helper_changed)
                        self.run_command(root, "clean", str(project), "-c", "Release", "/m:1", "/nr:false")
                        changed_observation = CONSUMER.observe_build(project, root, root / "packages", observer)
                        changed_receipt = CONSUMER.executable_asset_receipt(assets, root / "packages",
                            [item["Identity"] for item in evaluated["Items"]["Analyzer"]], imports, root, changed_observation)
                        self.assertEqual((root / "task-helper-observed.txt").read_text(), "changed1")
                        self.assertIn('"changed1"', generated_sources[0].read_text())
                        self.assertEqual(next(item["sha256"] for item in changed_receipt["selected"] if item["path"] == "analyzers/dotnet/cs/Probe.dll"), entry_hash)
                        self.assertEqual(next(item["sha256"] for item in changed_receipt["selected"] if item["path"] == "tools/Probe.Task.dll"), task_hash)
                        with self.assertRaisesRegex(ValueError, "baseline changed"):
                            CONSUMER.enforce_asset_policy([{**changed_receipt, "consumer": name}], baseline)
                        helper = root / "packages/probe.leaf/1.0.0/tools/Probe.Helper.dll"
                        helper.write_bytes(self.helper_original)
                        native = helper.parent / "libProbeNative.so"
                        native.write_bytes(self.native_changed)
                        self.run_command(root, "clean", str(project), "-c", "Release", "/m:1", "/nr:false")
                        native_observation = CONSUMER.observe_build(project, root, root / "packages", observer)
                        native_receipt = CONSUMER.executable_asset_receipt(assets, root / "packages",
                            [item["Identity"] for item in evaluated["Items"]["Analyzer"]], imports, root, native_observation)
                        self.assertEqual((root / "task-helper-observed.txt").read_text(), "original2")
                        with self.assertRaisesRegex(ValueError, "baseline changed"):
                            CONSUMER.enforce_asset_policy([{**native_receipt, "consumer": name}], baseline)
                        native.write_bytes(self.native_original)
                        external = root / "unowned-helper.dll"
                        external.write_bytes(self.helper_original)
                        helper.unlink(); helper.symlink_to(external)
                        self.run_command(root, "clean", str(project), "-c", "Release", "/m:1", "/nr:false")
                        unowned = CONSUMER.observe_build(project, root, root / "packages", observer)
                        with self.assertRaisesRegex(ValueError, "unowned executable consumer asset"):
                            CONSUMER.executable_asset_receipt(assets, root / "packages",
                                [item["Identity"] for item in evaluated["Items"]["Analyzer"]], imports, root, unowned)
                        helper.unlink(); helper.write_bytes(self.helper_original)
                        native.unlink()
                        external_native = root / "unowned-native.so"
                        external_native.write_bytes(self.native_original)
                        native.symlink_to(external_native)
                        self.run_command(root, "clean", str(project), "-c", "Release", "/m:1", "/nr:false")
                        unowned_native = CONSUMER.observe_build(project, root, root / "packages", observer)
                        with self.assertRaisesRegex(ValueError, "unowned executable consumer asset"):
                            CONSUMER.executable_asset_receipt(assets, root / "packages",
                                [item["Identity"] for item in evaluated["Items"]["Analyzer"]], imports, root, unowned_native)
                    for field, value in (("sha256", "0" * 64), ("path", "analyzers/unreviewed.dll")):
                        changed = [*selected[:-1], {**selected[-1], field: value}]
                        with self.assertRaisesRegex(ValueError, "baseline changed"):
                            CONSUMER.enforce_asset_policy([{**receipt, "selected": changed}], baseline)
                    # Builds-catalog package versions are not part of the reviewed executable identity.
                    CONSUMER.enforce_asset_policy([{**receipt, "selected": [*selected[:-1], {**selected[-1], "version": "2.0.0"}]}], baseline)
                    with self.assertRaisesRegex(ValueError, "baseline changed"):
                        CONSUMER.enforce_asset_policy([{**receipt, "consumer": "unreviewed-consumer"}], baseline)

    def run_command(self, root: Path, *args: str):
        result = subprocess.run(["dotnet", *args], cwd=root,
            env={**os.environ, "DOTNET_CLI_UI_LANGUAGE": "en-US"},
            text=True, capture_output=True, timeout=60)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result

    @classmethod
    def write_package(cls, feed: Path, package_id: str, attrs: dict[str, str] | None) -> None:
        package = ElementTree.Element("package")
        metadata = ElementTree.SubElement(package, "metadata")
        for key, value in (("id", package_id), ("version", "1.0.0"), ("authors", "Fixture"),
                           ("description", "Isolated dependency metadata fixture")):
            ElementTree.SubElement(metadata, key).text = value
        if attrs is not None:
            dependencies = ElementTree.SubElement(metadata, "dependencies")
            group = ElementTree.SubElement(dependencies, "group", {"targetFramework": "net11.0"})
            ElementTree.SubElement(group, "dependency", {"id": "Probe.Leaf", "version": "[1.0.0]", **attrs})
            ElementTree.SubElement(group, "dependency", {"id": "Probe.Transitive", "version": "[1.0.0]", **attrs})
        with zipfile.ZipFile(feed / f"{package_id}.1.0.0.nupkg", "w") as archive:
            archive.writestr(f"{package_id}.nuspec", ElementTree.tostring(package))
            archive.writestr("lib/net11.0/_._", b"")
            if package_id == "Probe.Leaf":
                archive.writestr("build/Probe.Leaf.targets", '<Project><PropertyGroup>'
                    '<ProbeBuildImported>Present</ProbeBuildImported></PropertyGroup>'
                    '<Import Project="helper.xml" /><UsingTask TaskName="ProbeTask" AssemblyFile="../tools/Probe.Task.dll"/>'
                    '<Target Name="RunProbeTask" BeforeTargets="CoreCompile"><ProbeTask/></Target></Project>')
                archive.writestr("build/helper.xml", '<Project><PropertyGroup><ProbeHelper>Original</ProbeHelper></PropertyGroup></Project>')
                archive.writestr("analyzers/dotnet/cs/Probe.dll", cls.analyzer)
                archive.writestr("tools/Probe.Task.dll", cls.task)
                archive.writestr("tools/Probe.ByteHelper.dll", cls.byte_helper)
                archive.writestr("tools/Probe.Helper.dll", cls.helper_original)
                archive.writestr("tools/libProbeNative.so", cls.native_original)
            elif package_id == "Probe.Transitive":
                archive.writestr("buildTransitive/Probe.Transitive.targets", '<Project><PropertyGroup>'
                    '<ProbeTransitiveImported>Present</ProbeTransitiveImported></PropertyGroup></Project>')
