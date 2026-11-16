"""Focused controls for native identity, graph capture, evidence isolation and child ownership."""
import importlib.util
import json
import os
from pathlib import Path
import signal
import subprocess
import sys
import tempfile
import time
import unittest
import xml.etree.ElementTree as ET

SCRIPT = Path(__file__).resolve().parents[2] / "scripts/verify-deferred-dependency-cohort.py"
SPEC = importlib.util.spec_from_file_location("deferred_cohort", SCRIPT)
COHORT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(COHORT)
STATE_SCRIPT = SCRIPT.parents[1] / "_bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/verify-state.sh"


def native_xml(path, assembly, identities, **overrides):
    attributes = dict(name=str(assembly), errors="0", failed="0", skipped="0", **{"not-run": "0"},
                      passed=str(len(identities)), total=str(len(identities)))
    attributes.update(overrides)
    root = ET.Element("assemblies")
    collection = ET.SubElement(ET.SubElement(root, "assembly", attributes), "collection")
    for owner, method, name in identities:
        ET.SubElement(collection, "test", type=owner, method=method, name=name, result="Pass")
    path.write_bytes(ET.tostring(root))


class FocusedFixture(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)


class NativeEvidenceTests(FocusedFixture):
    def validate(self, identities=None, assembly=None, **overrides):
        self.assembly = self.root / "owned/Owner.Tests.dll"
        self.xml = self.root / "native.xml"
        identities = identities if identities is not None else [("Owner.Tests", "Original", "Owner.Tests.Original")]
        native_xml(self.xml, assembly or self.assembly, identities, **overrides)
        return COHORT.validate_native(self.xml, self.assembly, methods=(("Owner.Tests", "Original"),))

    def test_complete_exact_owning_path_and_method_execute(self):
        self.assertEqual(len(self.validate()), 1)

    def test_same_basename_in_another_tree_is_refused(self):
        with self.assertRaisesRegex(RuntimeError, "owning assembly"):
            self.validate(assembly=self.root / "other/Owner.Tests.dll")

    def test_normalized_owning_path_is_accepted(self):
        self.assertEqual(len(self.validate(assembly=self.root / "owned/child/../Owner.Tests.dll")), 1)

    def test_method_substrings_and_display_mentions_cannot_replace_metadata(self):
        with self.assertRaisesRegex(RuntimeError, "Original method"):
            self.validate(identities=[("Owner.Tests", "OriginalExtra", "Owner.Tests.Original")])

    def test_zero_discovery_is_refused(self):
        with self.assertRaises(RuntimeError):
            self.validate(identities=[])

    def test_nonpassing_rows_cannot_hide_behind_successful_summary_counts(self):
        self.validate()
        for result in ("Skip", "Fail", "NotRun"):
            tree = ET.parse(self.xml); tree.find(".//test").set("result", result); tree.write(self.xml)
            with self.subTest(result=result), self.assertRaises(RuntimeError):
                COHORT.validate_native(self.xml, self.assembly)

    def test_summary_errors_cannot_hide_behind_passing_rows(self):
        for attribute in ("errors", "failed", "skipped", "not-run"):
            with self.subTest(attribute=attribute), self.assertRaises(RuntimeError):
                self.validate(**{attribute: "1"})

    def test_every_fixed_cohort_identity_and_multiplicity_is_required(self):
        assembly = self.root / "Owner.Tests.dll"
        xml = self.root / "fixed.xml"
        for name, inventory in COHORT.CASE_INVENTORIES.items():
            identities = [(case["type"], case["method"], case["name"])
                          for case in inventory["cases"] for _ in range(case["count"])]
            self.assertEqual(inventory["total"], len(identities))
            with self.subTest(cohort=name):
                native_xml(xml, assembly, identities + [("Extra.Tests", "Additional", "Extra.Tests.Additional")])
                COHORT.validate_native(xml, assembly, cases=inventory["cases"])
                # Preserve total discovery while replacing an original case.
                native_xml(xml, assembly, identities[1:] + [("Extra.Tests", "Additional", "Extra.Tests.Additional")])
                with self.assertRaisesRegex(RuntimeError, "Original case"):
                    COHORT.validate_native(xml, assembly, cases=inventory["cases"])

    def test_executed_assembly_hash_is_retained(self):
        scratch = self.root / "scratch"; scratch.mkdir()
        experiment = COHORT.Experiment("hash", scratch, self.root / "evidence")
        assembly = self.root / "Owner.Tests.dll"; assembly.write_bytes(b"executed bytes")
        def run(label, args, *_):
            native_xml(Path(args[-1]), assembly, [("Owner.Tests", "Original", "Owner.Tests.Original")])
        experiment.run = run
        experiment.tests("native", assembly, self.root)
        self.assertEqual(experiment.record["native"]["assemblySha256"], COHORT.digest(b"executed bytes"))


class CompilerAndAssetTests(FocusedFixture):
    def test_only_the_reviewed_configuration_delta_is_compatible(self):
        import copy
        left = {"Owner.csproj": {"files": {COHORT.REVIEWED_EDITOR_CONFIG: COHORT.REVIEWED_EDITOR_HASHES[0],
                                         "Owner.cs": "unchanged"}, "packages": []}}
        right = copy.deepcopy(left)
        right["Owner.csproj"]["files"][COHORT.REVIEWED_EDITOR_CONFIG] = COHORT.REVIEWED_EDITOR_HASHES[1]
        compatible, reviewed = COHORT.compare_consumed_inputs(left, right)
        self.assertTrue(compatible)
        self.assertEqual(len(reviewed), 1)
        right["Owner.csproj"]["files"]["Owner.cs"] = "changed"
        self.assertFalse(COHORT.compare_consumed_inputs(left, right)[0])
        right = copy.deepcopy(left)
        right["Owner.csproj"]["files"][COHORT.REVIEWED_EDITOR_CONFIG] = "unreviewed"
        self.assertFalse(COHORT.compare_consumed_inputs(left, right)[0])

    def setUp(self):
        super().setUp()
        self.project = self.root / "src/Owner.csproj"; self.project.parent.mkdir()
        self.project.write_text("project")
        self.source = self.root / "src/Owner.cs"; self.source.write_text("source")
        self.analyzer = self.root / "tools/bin/Guard.dll"; self.analyzer.parent.mkdir(parents=True); self.analyzer.write_bytes(b"analyzer")
        self.additional = self.root / "src/policy.txt"; self.additional.write_text("policy")
        self.editor = self.root / ".editorconfig"; self.editor.write_text("root = true\n[*]\ndotnet_diagnostic.CA2007.severity = error\n")
        self.global_editor = self.root / "settings.globalconfig"; self.global_editor.write_text("is_global = true\n")
        self.evaluated = {"Items": {kind: [{"FullPath": str(path)}] for kind, path in (
            ("Compile", self.source), ("Analyzer", self.analyzer), ("AdditionalFiles", self.additional),
            ("GlobalAnalyzerConfigFiles", self.global_editor))}}

    def test_analyzer_additional_and_editor_mutations_change_static_inventory(self):
        initial = COHORT.static_compiler_inputs(self.root, self.project, self.evaluated)
        for path in (self.analyzer, self.additional, self.editor, self.global_editor):
            with self.subTest(path=path):
                original = path.read_bytes(); path.write_bytes(original + b"changed")
                self.assertNotEqual(initial, COHORT.static_compiler_inputs(self.root, self.project, self.evaluated))
                path.write_bytes(original)

    def test_editorconfig_root_inside_a_section_cannot_hide_ancestor_settings(self):
        child = self.root / "src/.editorconfig"; child.write_text("[*.cs]\nroot = true\n")
        files = COHORT.static_compiler_inputs(self.root, self.project, self.evaluated)
        self.assertIn(".editorconfig", files); self.assertIn("src/.editorconfig", files)

    def test_unrelated_archive_editor_and_stale_assets_are_excluded(self):
        initial = COHORT.static_compiler_inputs(self.root, self.project, self.evaluated)
        unrelated = self.root / "docs/archived/.editorconfig"; unrelated.parent.mkdir(parents=True)
        unrelated.write_text("root=true\n")
        self.assertEqual(initial, COHORT.static_compiler_inputs(self.root, self.project, self.evaluated))
        selected = self.root / "artifacts/obj/Owner/debug/project.assets.json"; selected.parent.mkdir(parents=True)
        selected.write_text(json.dumps({"project": {"restore": {"projectPath": str(self.project)}}, "libraries": {"selected/1": {}}, "targets": {"net11.0": {}}}))
        stale = self.root / "docs/archived/obj/project.assets.json"; stale.parent.mkdir()
        stale.write_text("malformed unrelated asset")
        inventory = {str(self.project.relative_to(self.root)): {"evaluation": {"configuration": "Debug", "assetsPath": str(selected)}}}
        result = COHORT.collect_graph_assets(self.root, inventory, "Debug")
        self.assertEqual(1, len(result)); self.assertIn("selected/1", next(iter(result.values()))["libraries"])
        with self.assertRaisesRegex(RuntimeError, "configuration"):
            COHORT.collect_graph_assets(self.root, inventory, "Release")
        data = json.loads(selected.read_text()); data["project"]["restore"]["projectPath"] = str(self.root / "Other.csproj")
        selected.write_text(json.dumps(data))
        with self.assertRaisesRegex(RuntimeError, "owner"):
            COHORT.collect_graph_assets(self.root, inventory, "Debug")

    def test_actual_sdk_graph_includes_explicit_compiler_inputs_and_scoped_assets(self):
        project = self.root / "src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj"
        project.parent.mkdir()
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework></PropertyGroup><ItemGroup><Analyzer Include="../../tools/bin/Guard.dll" /><AdditionalFiles Include="../policy.txt" /></ItemGroup></Project>')
        (project.parent / "Entry.cs").write_text("public class Entry {}")
        experiment = type("GraphFixture", (), {})()
        experiment.work = self.root / "work"; experiment.work.mkdir()
        experiment.evidence = self.root / "evidence"; experiment.evidence.mkdir()
        def run(label, args, cwd):
            with (experiment.evidence / (label + ".log")).open("wb") as output:
                code = COHORT.run_owned(list(map(str, args)), cwd, None, output, timeout=30)
            self.assertEqual(0, code, (experiment.evidence / (label + ".log")).read_text())
        experiment.run = run
        inventory = COHORT.graph(experiment, self.root, "static")
        files = inventory[str(project.relative_to(self.root))]["files"]
        self.assertIn(str(self.analyzer.relative_to(self.root)), files)
        self.assertIn(str(self.additional.relative_to(self.root)), files)
        self.assertIn(".editorconfig", files)
        self.assertEqual(1, len(COHORT.collect_graph_assets(self.root, inventory, "Debug")))

    def test_graph_reads_structured_sdk_result_without_dropping_diagnostic_logs(self):
        project = self.root / "src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj"
        project.parent.mkdir(); project.write_text("<Project />")
        experiment = type("GraphFixture", (), {})()
        experiment.work = self.root / "work"; experiment.work.mkdir()
        experiment.evidence = self.root / "evidence"; experiment.evidence.mkdir()
        evaluated = {"Items": self.evaluated["Items"], "Properties": {"Configuration": "Debug",
            "ProjectAssetsFile": str(self.root / "project.assets.json"), "TargetDir": str(self.root / "bin"),
            "TargetPath": str(self.root / "bin/Owner.dll")}}
        diagnostic = "MSBuild server unavailable: falling back to an in-process build.\n"
        def run(label, args, cwd):
            args = list(map(str, args))
            (experiment.evidence / (label + ".log")).write_text(diagnostic + json.dumps(evaluated))
            for argument in args:
                if argument.startswith("-getResultOutputFile:"):
                    Path(argument.split(":", 1)[1]).write_text(json.dumps(evaluated))
                if argument.startswith("-preprocess:"):
                    Path(argument.split(":", 1)[1]).write_text("<Project />")
        experiment.run = run
        inventory = COHORT.graph(experiment, self.root, "diagnostic")
        self.assertIn("tools/bin/Guard.dll", inventory[str(project.relative_to(self.root))]["files"])
        log = experiment.evidence / ("diagnostic-graph-" + project.stem + ".log")
        self.assertTrue(log.read_text().startswith(diagnostic))
        result = log.with_name(log.stem + "-evaluation.json")
        self.assertEqual(evaluated, json.loads(result.read_text()))

    def test_graph_refuses_missing_or_malformed_result_even_with_a_json_console_log(self):
        experiment = type("GraphFixture", (), {})()
        experiment.work = self.root / "work"; experiment.work.mkdir()
        experiment.evidence = self.root / "evidence"; experiment.evidence.mkdir()
        for result in (None, "{malformed"):
            with self.subTest(result=result):
                def run(label, args, cwd):
                    (experiment.evidence / (label + ".log")).write_text('{"Properties":{},"Items":{}}')
                    for argument in map(str, args):
                        if argument.startswith("-getResultOutputFile:") and result is not None:
                            Path(argument.split(":", 1)[1]).write_text(result)
                experiment.run = run
                with self.assertRaises(FileNotFoundError if result is None else json.JSONDecodeError):
                    COHORT.graph(experiment, self.root, "missing" if result is None else "malformed")


class DestinationAndProcessTests(FocusedFixture):
    def test_command_provenance_records_mapping_inputs_without_unrelated_credentials(self):
        experiment = COHORT.Experiment("safe-env", self.root, self.root / "evidence")
        environment = {**os.environ, "HEXALITH_SECRET": "fixture-sensitive-value", "CustomSecret": "fixture-sensitive-value",
                       "HEXALITH_SOURCE_MAPPING_HASH": "a" * 64, "AfterMicrosoftNETSdkTargets": "/mapped/final.targets"}
        experiment.run("safe", [sys.executable, "-c", "pass"], self.root, environment)
        recorded = experiment.record["commands"][0]["environment"]
        self.assertNotIn("HEXALITH_SECRET", recorded)
        self.assertNotIn("CustomSecret", recorded)
        self.assertEqual(recorded["HEXALITH_SOURCE_MAPPING_HASH"], "a" * 64)
        self.assertEqual(recorded["AfterMicrosoftNETSdkTargets"], "/mapped/final.targets")

    def test_reused_experiment_destination_is_untouched_before_work(self):
        evidence = self.root / "evidence/job"; evidence.mkdir(parents=True)
        sentinel = evidence / "runner.py"; sentinel.write_bytes(b"retained")
        scratch = self.root / "scratch"; scratch.mkdir()
        with self.assertRaises(FileExistsError):
            COHORT.Experiment("job", scratch, evidence.parent)
        self.assertEqual(b"retained", sentinel.read_bytes()); self.assertFalse((scratch / "job").exists())

    def test_cli_rejects_existing_evidence_before_any_scratch_write(self):
        evidence = self.root / "retained"; evidence.mkdir(); (evidence / "sentinel").write_bytes(b"retained")
        scratch = self.root / "scratch"; scratch.mkdir()
        result = subprocess.run([sys.executable, str(SCRIPT), "--lane", "current", "--evidence", str(evidence)],
                                env={**os.environ, "TMPDIR": str(scratch)}, capture_output=True, text=True)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(["sentinel"], [p.name for p in evidence.iterdir()]); self.assertEqual([], list(scratch.iterdir()))

    def worker(self):
        worker = self.root / "worker.py"
        child = "import os,signal,time;signal.signal(signal.SIGTERM,signal.SIG_IGN);print(os.getpid(),flush=True);time.sleep(60)"
        worker.write_text("import os,pathlib,signal,subprocess,sys,time\nsignal.signal(signal.SIGTERM,signal.SIG_IGN)\n"
                          f"child=subprocess.Popen([sys.executable,'-c',{child!r}],stdout=subprocess.PIPE,text=True)\n"
                          "descendant=int(child.stdout.readline())\n"
                          f"pathlib.Path({str(self.root / 'pids.json')!r}).write_text(__import__('json').dumps([os.getpid(),descendant]))\n"
                          "time.sleep(60)\n")
        return worker

    def assert_reaped(self):
        for pid in json.loads((self.root / "pids.json").read_text()):
            with self.assertRaises(ProcessLookupError):
                os.kill(pid, 0)

    def test_timeout_terminates_and_reaps_a_term_ignoring_descendant(self):
        worker = self.worker()
        with (self.root / "output").open("wb") as output:
            result = COHORT.run_owned([sys.executable, str(worker)], self.root, None, output, timeout=1)
        self.assertEqual(124, result); self.assert_reaped()

    def test_interruption_terminates_and_reaps_the_owned_descendant(self):
        worker = self.worker(); controller = self.root / "controller.py"
        controller.write_text("import importlib.util,pathlib,sys\n"
            f"spec=importlib.util.spec_from_file_location('cohort',{str(SCRIPT)!r});m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)\n"
            f"output=open({str(self.root / 'output')!r},'wb')\n"
            f"try:m.run_owned([sys.executable,{str(worker)!r}],pathlib.Path({str(self.root)!r}),None,output,timeout=60)\n"
            "except KeyboardInterrupt:sys.exit(130)\n")
        for interruption in (signal.SIGINT, signal.SIGTERM):
            (self.root / "pids.json").unlink(missing_ok=True)
            process = subprocess.Popen([sys.executable, str(controller)], stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            try:
                deadline = time.monotonic() + 5
                while not (self.root / "pids.json").exists():
                    if time.monotonic() > deadline: self.fail("Owned descendants did not start")
                    time.sleep(0.01)
                process.send_signal(interruption)
                self.assertEqual(130, process.wait(timeout=5)); self.assert_reaped()
            finally:
                if process.poll() is None: process.kill(); process.wait()
                process.stdout.close(); process.stderr.close()


class StateReproductionTests(FocusedFixture):
    def setUp(self):
        super().setUp()
        self.repo = self.root / "repo"
        self.script = self.repo / STATE_SCRIPT.relative_to(SCRIPT.parents[1]); self.script.parent.mkdir(parents=True)
        self.script.write_bytes(STATE_SCRIPT.read_bytes())
        # This fixture exercises the shell orchestration only, never product builds.
        import re
        for relative in re.findall(r"'((?:src|test|tests|references|_bmad-output)/[^']+)'", STATE_SCRIPT.read_text()):
            p = self.repo / relative
            if p == self.script: continue
            p.parent.mkdir(parents=True, exist_ok=True); p.write_text("synthetic input")
        binary = self.root / "bin"; binary.mkdir()
        for command, code in {
            "rsync": "import pathlib,sys;pathlib.Path(sys.argv[-1]).mkdir(parents=True,exist_ok=True)",
            "node": "print('synthetic module result')",
            "dotnet": """import pathlib,sys,xml.etree.ElementTree as E
if sys.argv[1]=='build':
 p=pathlib.Path('test/Hexalith.Agents.UI.Tests/bin/Debug/net11.0/Hexalith.Agents.UI.Tests.dll');p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(b'synthetic assembly')
else:
 p=pathlib.Path(sys.argv[1]).resolve();root=E.Element('assemblies');owner=E.SubElement(root,'assembly',name=str(p),errors='0',failed='0',skipped='0',passed='1',total='1',**{'not-run':'0'});E.SubElement(E.SubElement(owner,'collection'),'test',name='Synthetic.Example',type='Synthetic',method='Example',result='Pass');pathlib.Path(sys.argv[sys.argv.index('-result-xml')+1]).write_bytes(E.tostring(root))
"""}.items():
            p = binary / command; p.write_text('#!/usr/bin/env python3\n' + code + '\n'); p.chmod(0o755)
        scratch = self.root / "scratch"; scratch.mkdir()
        self.environment = {**os.environ, "TMPDIR": str(scratch), "STATE_SUITES": "UI", "PATH": str(binary) + os.pathsep + os.environ['PATH']}

    def run_script(self, *args):
        return subprocess.run(['bash', str(self.script), *args], env=self.environment, cwd=self.repo,
                              capture_output=True, text=True)

    def test_defaults_allocate_distinct_durable_destinations_and_native_source_bindings(self):
        previous = None
        for _ in range(2):
            result = self.run_script()
            self.assertEqual(0, result.returncode, result.stderr)
            directory = Path(result.stdout.split('State verification evidence: ', 1)[1].splitlines()[0])
            self.assertTrue((directory / 'bindings.json').is_file()); self.assertTrue((directory / 'native-results.json').is_file())
            self.assertTrue((directory / 'ui-all.log.gz').is_file())
            if previous:
                self.assertNotEqual(previous[0], directory); self.assertEqual(previous[1], (previous[0] / 'bindings.json').read_bytes())
            previous = directory, (directory / 'bindings.json').read_bytes()

    def test_explicit_existing_destination_is_refused_without_overwrite(self):
        destination = self.root / 'retained'; destination.mkdir(); (destination / 'sentinel').write_bytes(b'original')
        result = self.run_script('--evidence', str(destination))
        self.assertNotEqual(0, result.returncode); self.assertEqual(['sentinel'], [p.name for p in destination.iterdir()])


if __name__ == "__main__":
    unittest.main()
