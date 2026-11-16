"""Only the validated UI release version is variable in the executable import policy."""
import copy
import importlib.util
from pathlib import Path
import unittest
import tempfile
import sys

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
            receipt = CONSUMER.executable_asset_receipt({"libraries": {}}, root / "packages", [], [str(path)], root)
            self.assertEqual([{"path": "Directory.Build.props"}], receipt["generatedImports"])
            path.write_bytes(b"<Project><Target Name='Unexpected' /></Project>\n")
            with self.assertRaisesRegex(ValueError, "unowned executable consumer asset"):
                CONSUMER.executable_asset_receipt({"libraries": {}}, root / "packages", [], [str(path)], root)

    def fixture(self):
        actual = {"kind": "import", "package": "Hexalith.Agents.UI", "version": "1.2.3",
                  "path": "buildTransitive/Hexalith.Agents.UI.targets", "sha256": "a" * 64}
        expected = {**actual, "version": "$produced"}
        return ([{"consumer": "UI", "selected": [actual]}],
                {"schemaVersion": 1, "consumers": {"UI": [expected]}, "selected": [expected]})

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

    def test_external_versions_are_never_normalized(self):
        receipts, policy = self.fixture()
        receipts[0]["selected"][0]["package"] = "External.Library"
        policy["consumers"]["UI"][0]["package"] = "External.Library"
        policy["selected"][0]["package"] = "External.Library"
        with self.assertRaises(ValueError):
            CONSUMER.enforce_asset_policy(receipts, policy, "1.2.3")


if __name__ == "__main__":
    unittest.main()
