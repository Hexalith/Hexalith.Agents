"""Prove dependency asset metadata behavior with real isolated NuGet restores."""

from __future__ import annotations

import json
import os
import subprocess
import tempfile
import unittest
import zipfile
from pathlib import Path
from xml.etree import ElementTree


class DependencyAssetRestoreTests(unittest.TestCase):
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
                    self.write_package(feed, "Probe.Wrapper", attrs)
                    project = root / "Consumer.csproj"
                    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                        '<TargetFramework>net10.0</TargetFramework><NuGetAudit>false</NuGetAudit>'
                        '</PropertyGroup><ItemGroup><PackageReference Include="Probe.Wrapper" Version="1.0.0"/>'
                        '</ItemGroup></Project>', encoding="utf-8")
                    config = root / "NuGet.Config"
                    configuration = ElementTree.Element("configuration")
                    sources = ElementTree.SubElement(configuration, "packageSources")
                    ElementTree.SubElement(sources, "clear")
                    ElementTree.SubElement(sources, "add", {"key": "local", "value": str(feed)})
                    ElementTree.ElementTree(configuration).write(config, encoding="utf-8")
                    self.run_command(root, "restore", str(project), "--configfile", str(config),
                                     "--packages", str(root / "packages"), "--verbosity", "quiet")
                    assets = json.loads((root / "obj/project.assets.json").read_text())
                    target = assets["targets"]["net10.0"]["Probe.Leaf/1.0.0"]
                    selected = "build/Probe.Leaf.targets" in target.get("build", {})
                    self.assertEqual(selected, imports_build)
                    result = self.run_command(root, "msbuild", str(project), "-nologo",
                        "-target:ResolveLockFileAnalyzers", "-getProperty:ProbeBuildImported", "-getItem:Analyzer")
                    evaluated = json.loads(result.stdout)
                    self.assertEqual(evaluated["Properties"]["ProbeBuildImported"], "Present" if imports_build else "")
                    # This metadata does not suppress transitive analyzer items; avoid a false purity claim.
                    self.assertTrue(any(Path(item["Identity"]).name == "Probe.dll"
                        for item in evaluated["Items"]["Analyzer"]))

    def run_command(self, root: Path, *args: str):
        result = subprocess.run(["dotnet", *args], cwd=root,
            env={**os.environ, "DOTNET_CLI_UI_LANGUAGE": "en-US"},
            text=True, capture_output=True, timeout=60)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result

    @staticmethod
    def write_package(feed: Path, package_id: str, attrs: dict[str, str] | None) -> None:
        package = ElementTree.Element("package")
        metadata = ElementTree.SubElement(package, "metadata")
        for key, value in (("id", package_id), ("version", "1.0.0"), ("authors", "Fixture"),
                           ("description", "Isolated dependency metadata fixture")):
            ElementTree.SubElement(metadata, key).text = value
        if attrs is not None:
            dependencies = ElementTree.SubElement(metadata, "dependencies")
            group = ElementTree.SubElement(dependencies, "group", {"targetFramework": "net10.0"})
            ElementTree.SubElement(group, "dependency", {"id": "Probe.Leaf", "version": "[1.0.0]", **attrs})
        with zipfile.ZipFile(feed / f"{package_id}.1.0.0.nupkg", "w") as archive:
            archive.writestr(f"{package_id}.nuspec", ElementTree.tostring(package))
            archive.writestr("lib/net10.0/_._", b"")
            if attrs is None:
                archive.writestr("build/Probe.Leaf.targets", '<Project><PropertyGroup>'
                    '<ProbeBuildImported>Present</ProbeBuildImported></PropertyGroup></Project>')
                archive.writestr("analyzers/dotnet/cs/Probe.dll", b"fixture analyzer; evaluation only")
