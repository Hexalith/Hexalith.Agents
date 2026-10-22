"""Fixture tests for exact GitHub Release metadata and asset verification."""

from __future__ import annotations

import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "verify-github-release.py"
SPEC = importlib.util.spec_from_file_location("verify_github_release", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
VERIFIER = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = VERIFIER
SPEC.loader.exec_module(VERIFIER)

VERSION = "1.2.3"
SHA = "1234567890abcdef1234567890abcdef12345678"
OTHER_SHA = "abcdef1234567890abcdef1234567890abcdef12"
MANIFEST = {
    "packages": [
        {"id": "Hexalith.Agents.Contracts"},
        {"id": "Hexalith.Agents"},
    ]
}
EXPECTED_ASSETS = [
    {"name": "Hexalith.Agents.Contracts.1.2.3.nupkg"},
    {"name": "Hexalith.Agents.1.2.3.nupkg"},
]


def release(
    *,
    tag: str = "v1.2.3",
    draft: bool = False,
    prerelease: bool = False,
    assets: list[dict[str, str]] | None = None,
):
    """Create one GitHub Release API fixture."""
    return {
        "tag_name": tag,
        "draft": draft,
        "prerelease": prerelease,
        "assets": EXPECTED_ASSETS if assets is None else assets,
    }


class GitHubReleaseVerificationTests(unittest.TestCase):
    """Cover exact and invalid release metadata/asset inventories."""

    def verify(self, payload: object) -> None:
        """Verify a fixture against the shared exact release contract."""
        VERIFIER.verify_github_release(MANIFEST, payload, VERSION, SHA, SHA)

    def test_exact_release_is_accepted(self) -> None:
        self.verify(release())

    def test_missing_asset_is_rejected(self) -> None:
        with self.assertRaisesRegex(VERIFIER.ReleaseVerificationError, "asset mismatch.*missing"):
            self.verify(release(assets=EXPECTED_ASSETS[:1]))

    def test_unexpected_asset_is_rejected(self) -> None:
        assets = [*EXPECTED_ASSETS, {"name": "unexpected.zip"}]
        with self.assertRaisesRegex(VERIFIER.ReleaseVerificationError, "unexpected=.*unexpected.zip"):
            self.verify(release(assets=assets))

    def test_wrong_tag_is_rejected(self) -> None:
        with self.assertRaisesRegex(VERIFIER.ReleaseVerificationError, "tag must be v1.2.3"):
            self.verify(release(tag="v1.2.4"))

    def test_tag_at_wrong_source_is_rejected(self) -> None:
        with self.assertRaisesRegex(VERIFIER.ReleaseVerificationError, "not dispatched source"):
            VERIFIER.verify_github_release(MANIFEST, release(), VERSION, SHA, OTHER_SHA)

    def test_draft_release_is_rejected(self) -> None:
        with self.assertRaisesRegex(VERIFIER.ReleaseVerificationError, "still a draft"):
            self.verify(release(draft=True))

    def test_stable_release_requires_explicit_false_prerelease_flag(self) -> None:
        for flag in (True, None, 0, "false"):
            with self.subTest(prerelease=flag), self.assertRaisesRegex(
                VERIFIER.ReleaseVerificationError, "prerelease=false for a stable version"
            ):
                payload = release()
                payload["prerelease"] = flag
                self.verify(payload)

    def test_stable_release_rejects_missing_prerelease_flag(self) -> None:
        payload = release()
        del payload["prerelease"]
        with self.assertRaisesRegex(VERIFIER.ReleaseVerificationError, "prerelease=false"):
            self.verify(payload)

    def test_build_metadata_hyphen_does_not_make_a_stable_version_prerelease(self) -> None:
        version = "1.2.3+build-label"
        assets = [{"name": f"{package['id']}.{version}.nupkg"} for package in MANIFEST["packages"]]
        payload = release(tag=f"v{version}", assets=assets)
        VERIFIER.verify_github_release(MANIFEST, payload, version, SHA, SHA)

        payload["prerelease"] = True
        with self.assertRaisesRegex(VERIFIER.ReleaseVerificationError, "prerelease=false"):
            VERIFIER.verify_github_release(MANIFEST, payload, version, SHA, SHA)

    def test_prerelease_version_still_accepts_a_prerelease_release(self) -> None:
        version = "1.2.3-rc.1"
        assets = [{"name": f"{package['id']}.{version}.nupkg"} for package in MANIFEST["packages"]]
        VERIFIER.verify_github_release(
            MANIFEST, release(tag=f"v{version}", prerelease=True, assets=assets), version, SHA, SHA
        )

    def run_cli(
        self,
        payload: object,
        resolved_tag_sha: str = SHA,
        *,
        version: str = VERSION,
        fixture_overrides: dict[str, str | None] | None = None,
    ) -> subprocess.CompletedProcess[str]:
        """Run the production CLI against temporary release API fixtures."""
        with tempfile.TemporaryDirectory() as directory:
            manifest = Path(directory) / "manifest.json"
            release_path = Path(directory) / "release.json"
            fixtures = {"manifest.json": json.dumps(MANIFEST), "release.json": json.dumps(payload)}
            fixtures.update(fixture_overrides or {})
            for filename, contents in fixtures.items():
                if contents is not None:
                    (Path(directory) / filename).write_text(contents, encoding="utf-8")
            return subprocess.run(
                [sys.executable, str(SCRIPT), str(manifest), str(release_path), version, SHA, resolved_tag_sha],
                capture_output=True,
                text=True,
                timeout=10,
            )

    def assert_cli_failure(self, result: subprocess.CompletedProcess[str], diagnostic: str) -> None:
        """Require a bounded verifier failure without a success message or traceback."""
        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertTrue(result.stderr.startswith("verify-github-release: "), result.stderr)
        self.assertIn(diagnostic, result.stderr)
        self.assertNotIn("Traceback", result.stderr)
        self.assertNotIn("Verified", result.stdout)

    def test_cli_accepts_exact_release(self) -> None:
        result = self.run_cli(release())
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(result.stdout, f"Verified GitHub Release v{VERSION} at {SHA}.\n")
        self.assertEqual(result.stderr, "")

    def test_cli_accepts_stable_build_metadata_and_prerelease_versions(self) -> None:
        for version, prerelease in (("1.2.3+build-label", False), ("1.2.3-rc.1", True)):
            with self.subTest(version=version):
                assets = [{"name": f"{package['id']}.{version}.nupkg"} for package in MANIFEST["packages"]]
                payload = release(tag=f"v{version}", prerelease=prerelease, assets=assets)
                result = self.run_cli(payload, version=version)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual(result.stdout, f"Verified GitHub Release v{version} at {SHA}.\n")
                self.assertEqual(result.stderr, "")

    def test_cli_rejects_prerelease_flag_on_stable_build_metadata(self) -> None:
        version = "1.2.3+build-label"
        assets = [{"name": f"{package['id']}.{version}.nupkg"} for package in MANIFEST["packages"]]
        result = self.run_cli(release(tag=f"v{version}", prerelease=True, assets=assets), version=version)
        self.assert_cli_failure(result, "prerelease=false for a stable version")

    def test_cli_rejects_invalid_release_contracts(self) -> None:
        cases = (
            ("prerelease", release(prerelease=True), SHA, "prerelease=false"),
            ("missing asset", release(assets=EXPECTED_ASSETS[:1]), SHA, "asset mismatch"),
            ("wrong source", release(), OTHER_SHA, "not dispatched source"),
        )
        for label, payload, resolved_sha, diagnostic in cases:
            with self.subTest(label=label):
                result = self.run_cli(payload, resolved_sha)
                self.assert_cli_failure(result, diagnostic)

    def test_cli_rejects_malformed_and_missing_json_fixtures(self) -> None:
        for filename, label in (("manifest.json", "release manifest"), ("release.json", "GitHub Release")):
            for fault, contents in (("malformed", "not-json"), ("missing", None)):
                with self.subTest(fixture=filename, fault=fault):
                    result = self.run_cli(release(), fixture_overrides={filename: contents})
                    self.assert_cli_failure(result, f"could not read {label} JSON")


if __name__ == "__main__":
    unittest.main()
