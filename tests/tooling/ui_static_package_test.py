"""The required Razor registration admits one bound module and no additional build code."""
import json
from pathlib import Path
import tempfile
import unittest
import zipfile

from nuget_package_fixtures import VALIDATOR, VERSION, ui_static_assets, write_package


class UiStaticPackageTests(unittest.TestCase):
    def validate(self, replacements=None):
        with tempfile.TemporaryDirectory() as directory:
            path = write_package(Path(directory), "Hexalith.Agents.UI", extra_assets=replacements)
            VALIDATOR.validate_package(VALIDATOR.read_metadata(path), path, VERSION)

    def test_actual_sdk_manifest_shape_binds_the_packaged_module(self):
        self.validate()

    def test_recorded_sdk_content_fingerprint_matches_known_vector(self):
        module = b"export function fixture() {}\n"
        self.assertEqual(VALIDATOR.static_asset_fingerprint(module), "ymbusap65w")
        self.assertEqual(VALIDATOR.static_asset_fingerprint(b""), "5ipweew5fc")

    def test_coordinated_stale_fingerprint_and_endpoint_references_refuse(self):
        name = "build/Hexalith.Agents.UI.PackageAssets.json"
        contents = ui_static_assets()[name].decode().replace("ymbusap65w", "zzzzzzzzzz")
        with self.assertRaisesRegex(ValueError, "invalid UI static asset metadata"):
            self.validate({name: contents.encode()})

    def test_every_registration_is_required_and_byte_reviewed(self):
        for name in (key for key in ui_static_assets() if key.endswith(".targets")):
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, "unreviewed UI static registration"):
                self.validate({name: b'<Project><Target Name="Injected"><Exec Command="arbitrary" /></Target></Project>'})

    def test_every_static_asset_member_is_required(self):
        for missing in ui_static_assets():
            with self.subTest(missing=missing), tempfile.TemporaryDirectory() as directory:
                path = write_package(Path(directory), "Hexalith.Agents.UI")
                with zipfile.ZipFile(path) as archive:
                    members = {name: archive.read(name) for name in archive.namelist() if name != missing}
                with zipfile.ZipFile(path, "w") as archive:
                    for name, data in members.items(): archive.writestr(name, data)
                with self.assertRaisesRegex(ValueError, "UI static asset inventory differs"):
                    VALIDATOR.validate_package(VALIDATOR.read_metadata(path), path, VERSION)

    def test_module_substitution_and_missing_or_unrelated_assets_refuse(self):
        for changes in ({"staticwebassets/pendingProviderCommands.js": b"changed"},
                        {"staticwebassets/pendingProviderCommands.js": b""},
                        {"staticwebassets/other.js": b"unrelated"},
                        {"build/extra.targets": b"<Project />"}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.validate(changes)

    def test_manifest_scope_headers_and_properties_cannot_redirect_registration(self):
        name = "build/Hexalith.Agents.UI.PackageAssets.json"
        for defect in ("source", "path", "integrity", "length", "endpoint", "headers", "property", "duplicate"):
            data = json.loads(ui_static_assets()[name])
            asset = data["Assets"]["staticwebassets/pendingProviderCommands.js"]
            if defect == "source": asset["SourceId"] = "Other.Module"
            if defect == "path": asset["RelativePath"] = "../outside.js"
            if defect == "integrity": asset["Integrity"] = "wrong"
            if defect == "length": asset["FileLength"] = True
            if defect == "endpoint": data["Endpoints"][0]["Route"] = "foreign/route.js"
            if defect == "headers": data["Endpoints"][1]["ResponseHeaders"].append({"Name": "Location", "Value": "foreign"})
            if defect == "property": asset["Unreviewed"] = "value"
            payload = json.dumps(data)
            if defect == "duplicate": payload = payload.replace('"Version": 1', '"Version": 1, "Version": 1', 1)
            with self.subTest(defect=defect), self.assertRaises(ValueError):
                self.validate({name: payload.encode()})

    def test_other_packages_cannot_enroll_the_ui_registration(self):
        with tempfile.TemporaryDirectory() as directory:
            path = write_package(Path(directory), extra_assets=ui_static_assets())
            with self.assertRaisesRegex(ValueError, "asset leak"):
                VALIDATOR.validate_package(VALIDATOR.read_metadata(path), path, VERSION)


if __name__ == "__main__":
    unittest.main()
