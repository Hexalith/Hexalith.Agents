"""Fixture tests for exact GitHub Release metadata and asset verification."""

from __future__ import annotations

import importlib.util
import sys
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


def release(*, tag: str = "v1.2.3", draft: bool = False, assets: list[dict[str, str]] | None = None):
    """Create one GitHub Release API fixture."""
    return {"tag_name": tag, "draft": draft, "assets": EXPECTED_ASSETS if assets is None else assets}


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


if __name__ == "__main__":
    unittest.main()
