"""Regression tests for the fail-closed NuGet publication verifier."""

from __future__ import annotations

import gzip
import importlib.util
import io
import json
import sys
import tempfile
import unittest
from pathlib import Path
from urllib.error import HTTPError, URLError


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / "scripts" / "verify-nuget-publication.py"
SPEC = importlib.util.spec_from_file_location("verify_nuget_publication", SCRIPT)
assert SPEC is not None and SPEC.loader is not None
VERIFIER = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = VERIFIER
SPEC.loader.exec_module(VERIFIER)


class FakeResponse:
    """Small context-managed HTTP response used by the probe tests."""

    def __init__(self, payload: object, status: int = 200) -> None:
        self._body = json.dumps(payload).encode("utf-8") if not isinstance(payload, bytes) else payload
        self._status = status

    def __enter__(self):
        return self

    def __exit__(self, _exception_type, _exception, _traceback) -> None:
        return None

    def getcode(self) -> int:
        return self._status

    def read(self, _size: int) -> bytes:
        return self._body


def leaf(package_id: str, version: str) -> dict[str, object]:
    """Create one valid NuGet registration leaf."""
    return {"catalogEntry": {"id": package_id, "version": version}}


def missing(url: str) -> HTTPError:
    """Create the only response interpreted as package absence."""
    return HTTPError(url, 404, "Not Found", {}, io.BytesIO())


def http_error(url: str, status: int) -> HTTPError:
    """Create a non-404 HTTP error response."""
    return HTTPError(url, status, "Failed", {}, io.BytesIO())


class VerifyNuGetPublicationTests(unittest.TestCase):
    """Cover collision, indexing, malformed-response, and inventory behavior."""

    def test_manifest_rejects_case_insensitive_duplicate_identities(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            manifest = Path(directory) / "release-packages.json"
            manifest.write_text(
                json.dumps(
                    {
                        "schemaVersion": 1,
                        "packages": [
                            {"id": "Hexalith.Agents", "project": "a.csproj"},
                            {"id": "hexalith.agents", "project": "b.csproj"},
                        ],
                    }
                ),
                encoding="utf-8",
            )

            with self.assertRaisesRegex(VERIFIER.VerificationError, "repeats package id"):
                VERIFIER.load_package_ids(manifest)

    def test_manifest_rejects_duplicate_json_keys(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            manifest = Path(directory) / "release-packages.json"
            manifest.write_text(
                '{"schemaVersion":1,"schemaVersion":1,"packages":[{"id":"Example","project":"a.csproj"}]}',
                encoding="utf-8",
            )

            with self.assertRaisesRegex(VERIFIER.VerificationError, "duplicate JSON key"):
                VERIFIER.load_package_ids(manifest)

    def test_manifest_requires_an_actual_integer_schema_version(self) -> None:
        for invalid_version in (True, 1.0):
            with self.subTest(schema_version=invalid_version), tempfile.TemporaryDirectory() as directory:
                manifest = Path(directory) / "release-packages.json"
                manifest.write_text(
                    json.dumps(
                        {
                            "schemaVersion": invalid_version,
                            "packages": [{"id": "Example", "project": "a.csproj"}],
                        }
                    ),
                    encoding="utf-8",
                )

                with self.assertRaisesRegex(VERIFIER.VerificationError, "integer schemaVersion 1"):
                    VERIFIER.load_package_ids(manifest)

    def test_http_404_proves_absence(self) -> None:
        def opener(url: str, _timeout: float):
            raise missing(url)

        VERIFIER.verify_publication(
            ("Hexalith.Agents",),
            "1.2.3",
            expect="absent",
            attempts=9,
            delay_seconds=0,
            opener=opener,
        )

    def test_exact_registration_leaf_proves_presence(self) -> None:
        def opener(_url: str, _timeout: float):
            return FakeResponse(leaf("Hexalith.Agents", "1.2.3"))

        VERIFIER.verify_publication(
            ("Hexalith.Agents",),
            "1.2.3",
            expect="present",
            attempts=1,
            delay_seconds=0,
            opener=opener,
        )

    def test_compressed_registration_catalog_link_proves_exact_presence(self) -> None:
        catalog_url = "https://api.nuget.org/v3/catalog0/data/example.json"
        calls: list[str] = []

        def opener(url: str, _timeout: float):
            calls.append(url)
            payload = (
                {"id": "Hexalith.Agents", "version": "1.2.3"}
                if url == catalog_url
                else {"catalogEntry": catalog_url}
            )
            return FakeResponse(gzip.compress(json.dumps(payload).encode("utf-8")))

        result = VERIFIER.probe_package("Hexalith.Agents", "1.2.3", opener=opener)

        self.assertTrue(result.present)
        self.assertEqual(len(calls), 2)

    def test_malformed_success_response_fails_closed(self) -> None:
        def opener(_url: str, _timeout: float):
            return FakeResponse(b"not-json")

        with self.assertRaisesRegex(VERIFIER.VerificationError, "malformed JSON"):
            VERIFIER.verify_publication(
                ("Hexalith.Agents",),
                "1.2.3",
                expect="present",
                attempts=3,
                delay_seconds=0,
                opener=opener,
            )

    def test_partial_publication_retries_until_every_identity_is_present(self) -> None:
        calls: dict[str, int] = {}
        sleeps: list[float] = []

        def opener(url: str, _timeout: float):
            package_id = "Hexalith.Agents.Client" if "agents.client" in url else "Hexalith.Agents"
            calls[package_id] = calls.get(package_id, 0) + 1
            if package_id.endswith("Client") and calls[package_id] == 1:
                raise missing(url)
            return FakeResponse(leaf(package_id, "1.2.3"))

        VERIFIER.verify_publication(
            ("Hexalith.Agents", "Hexalith.Agents.Client"),
            "1.2.3",
            expect="present",
            attempts=3,
            delay_seconds=0.25,
            opener=opener,
            sleeper=sleeps.append,
        )

        self.assertEqual(calls, {"Hexalith.Agents": 1, "Hexalith.Agents.Client": 2})
        self.assertEqual(sleeps, [0.25])

    def test_presence_retries_transient_http_and_transport_failures(self) -> None:
        transient_failures = (
            ("rate limit", lambda url: http_error(url, 429)),
            ("server error", lambda url: http_error(url, 503)),
            ("transport", lambda _url: URLError("temporary transport failure")),
        )
        for label, transient_failure in transient_failures:
            with self.subTest(label=label):
                calls = 0
                sleeps: list[float] = []

                def opener(url: str, _timeout: float):
                    nonlocal calls
                    calls += 1
                    if calls == 1:
                        raise transient_failure(url)
                    return FakeResponse(leaf("Hexalith.Agents", "1.2.3"))

                VERIFIER.verify_publication(
                    ("Hexalith.Agents",),
                    "1.2.3",
                    expect="present",
                    attempts=2,
                    delay_seconds=0.25,
                    opener=opener,
                    sleeper=sleeps.append,
                )

                self.assertEqual(calls, 2)
                self.assertEqual(sleeps, [0.25])

    def test_absence_check_fails_immediately_on_non_404_response(self) -> None:
        calls = 0

        def opener(url: str, _timeout: float):
            nonlocal calls
            calls += 1
            raise http_error(url, 503)

        with self.assertRaisesRegex(VERIFIER.VerificationError, "HTTP 503"):
            VERIFIER.verify_publication(
                ("Hexalith.Agents",),
                "1.2.3",
                expect="absent",
                attempts=4,
                delay_seconds=0,
                opener=opener,
                sleeper=lambda _seconds: self.fail("absence checks must not retry"),
            )
        self.assertEqual(calls, 1)

    def test_presence_retry_exhaustion_reports_missing_inventory(self) -> None:
        calls = 0

        def opener(url: str, _timeout: float):
            nonlocal calls
            calls += 1
            raise missing(url)

        with self.assertRaisesRegex(VERIFIER.VerificationError, "retry exhaustion.*Hexalith.Agents"):
            VERIFIER.verify_publication(
                ("Hexalith.Agents",),
                "1.2.3",
                expect="present",
                attempts=3,
                delay_seconds=0,
                opener=opener,
                sleeper=lambda _seconds: None,
            )
        self.assertEqual(calls, 3)

    def test_any_existing_version_blocks_the_inventory_before_publication(self) -> None:
        def opener(url: str, _timeout: float):
            if "agents.client" in url:
                return FakeResponse(leaf("Hexalith.Agents.Client", "1.2.3"))
            raise missing(url)

        with self.assertRaisesRegex(VERIFIER.VerificationError, "publication collision.*Agents.Client"):
            VERIFIER.verify_publication(
                ("Hexalith.Agents", "Hexalith.Agents.Client"),
                "1.2.3",
                expect="absent",
                attempts=1,
                delay_seconds=0,
                opener=opener,
            )


if __name__ == "__main__":
    unittest.main()
