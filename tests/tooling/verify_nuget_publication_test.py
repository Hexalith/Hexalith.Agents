"""Regression tests for the fail-closed NuGet publication verifier."""

from __future__ import annotations

import gzip
import importlib.util
import io
import json
import os
import subprocess
import sys
import tempfile
import threading
import unittest
from contextlib import contextmanager
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
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


@contextmanager
def registration_server(responses: dict[str, tuple[int, object]], *, delayed_path: str | None = None):
    """Serve registration fixtures on loopback and record every requested path."""
    requests: list[str] = []

    class Handler(BaseHTTPRequestHandler):
        def do_GET(self) -> None:
            requests.append(self.path)
            status, payload = responses.get(self.path, (404, {}))
            if self.path == delayed_path and requests.count(self.path) == 1:
                status, payload = 404, {}
            body = payload if isinstance(payload, bytes) else json.dumps(payload).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def log_message(self, _format: str, *_args) -> None:
            """Keep fixture HTTP traffic out of test output."""

    with ThreadingHTTPServer(("127.0.0.1", 0), Handler) as server:
        worker = threading.Thread(target=server.serve_forever, kwargs={"poll_interval": 0.01}, daemon=True)
        worker.start()
        try:
            yield f"http://127.0.0.1:{server.server_port}", requests
        finally:
            server.shutdown()
            worker.join(timeout=5)


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

    def test_invalid_timeouts_fail_before_any_probe_or_sleep(self) -> None:
        for expect in ("absent", "present"):
            for timeout in (0, -1, float("nan"), float("inf"), float("-inf")):
                with self.subTest(expect=expect, timeout=timeout), self.assertRaisesRegex(
                    VERIFIER.VerificationError, "timeout must be a positive finite number"
                ):
                    VERIFIER.verify_publication(
                        ("Hexalith.Agents",),
                        "1.2.3",
                        expect=expect,
                        attempts=2,
                        delay_seconds=0,
                        timeout=timeout,
                        opener=lambda *_args: self.fail("invalid timeout reached the network"),
                        sleeper=lambda _seconds: self.fail("invalid timeout reached a retry wait"),
                    )

    def test_positive_finite_timeouts_are_forwarded_to_the_probe(self) -> None:
        for timeout in (0.25, 15.0):
            with self.subTest(timeout=timeout):
                received: list[float] = []

                def opener(_url: str, request_timeout: float):
                    received.append(request_timeout)
                    return FakeResponse(leaf("Hexalith.Agents", "1.2.3"))

                VERIFIER.verify_publication(
                    ("Hexalith.Agents",),
                    "1.2.3",
                    expect="present",
                    attempts=1,
                    delay_seconds=0,
                    timeout=timeout,
                    opener=opener,
                )
                self.assertEqual(received, [timeout])

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


class NuGetPublicationCliTests(unittest.TestCase):
    """Exercise the real CLI against controlled HTTP registration responses."""

    def setUp(self) -> None:
        self.manifest = json.loads((ROOT / "eng" / "release-packages.json").read_text(encoding="utf-8"))
        self.paths = {
            f"/{row['id'].casefold()}/1.2.3.json": row["id"]
            for row in self.manifest["packages"]
        }

    def run_cli(
        self,
        responses: dict[str, tuple[int, object]],
        *,
        expect: str = "present",
        timeout: str = "1",
        attempts: int = 1,
        delayed_path: str | None = None,
    ) -> tuple[subprocess.CompletedProcess[str], list[str]]:
        """Run an isolated subprocess with a manifest and bounded loopback transport."""
        with tempfile.TemporaryDirectory() as directory, registration_server(
            responses, delayed_path=delayed_path
        ) as (base_url, requests):
            manifest_path = Path(directory) / "release-packages.json"
            manifest_path.write_text(json.dumps(self.manifest), encoding="utf-8")
            result = subprocess.run(
                [
                    sys.executable, str(SCRIPT), str(manifest_path), "1.2.3",
                    "--expect", expect, "--attempts", str(attempts), "--delay-seconds", "0",
                    f"--timeout={timeout}", "--base-url", base_url,
                ],
                cwd=directory,
                env={**os.environ, "NO_PROXY": "127.0.0.1", "no_proxy": "127.0.0.1"},
                capture_output=True,
                text=True,
                timeout=10,
            )
        return result, requests

    def assert_cli_failure(self, result: subprocess.CompletedProcess[str], diagnostic: str) -> None:
        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertIn(f"verify-nuget-publication: {diagnostic}", result.stderr)
        self.assertNotIn("Verified", result.stdout)
        self.assertNotIn("Traceback", result.stderr)

    def test_cli_accepts_present_and_absent_complete_inventories(self) -> None:
        for expect in ("present", "absent"):
            with self.subTest(expect=expect):
                responses = (
                    {path: (200, leaf(package_id, "1.2.3")) for path, package_id in self.paths.items()}
                    if expect == "present" else {}
                )
                result, requests = self.run_cli(responses, expect=expect)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertIn(f"Verified {len(self.paths)} package versions are {expect} on NuGet.", result.stdout)
                self.assertEqual(result.stderr, "")
                self.assertEqual(requests, list(self.paths))

    def test_cli_rejects_a_collision_anywhere_in_the_inventory(self) -> None:
        path, package_id = list(self.paths.items())[-1]
        result, requests = self.run_cli({path: (200, leaf(package_id, "1.2.3"))}, expect="absent")
        self.assert_cli_failure(result, "publication collision:")
        self.assertIn(package_id, result.stderr)
        self.assertEqual(requests, list(self.paths))

    def test_cli_retries_only_the_pending_inventory_until_it_is_present(self) -> None:
        responses = {path: (200, leaf(package_id, "1.2.3")) for path, package_id in self.paths.items()}
        delayed_path = list(self.paths)[-1]
        result, requests = self.run_cli(responses, attempts=2, delayed_path=delayed_path)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn(f"Verified {len(self.paths)} package versions are present on NuGet.", result.stdout)
        self.assertIn("NuGet indexing incomplete after attempt 1/2", result.stderr)
        self.assertEqual(requests, [*self.paths, delayed_path])

    def test_cli_rejects_partial_publication_after_bounded_attempts(self) -> None:
        responses = {path: (200, leaf(package_id, "1.2.3")) for path, package_id in self.paths.items()}
        missing_path, missing_package = list(self.paths.items())[-1]
        del responses[missing_path]
        result, requests = self.run_cli(responses)
        self.assert_cli_failure(result, "NuGet presence retry exhaustion after 1 attempts;")
        self.assertIn(missing_package, result.stderr)
        self.assertEqual(requests, list(self.paths))

    def test_cli_rejects_malformed_and_mismatched_registration_leaves(self) -> None:
        path = next(iter(self.paths))
        for payload, diagnostic in (
            (b"not-json", "NuGet returned malformed JSON"),
            (leaf("Different.Package", "1.2.3"), "NuGet returned a mismatched or malformed registration leaf"),
        ):
            with self.subTest(diagnostic=diagnostic):
                result, requests = self.run_cli({path: (200, payload)})
                self.assert_cli_failure(result, diagnostic)
                self.assertEqual(requests, [path])

    def test_cli_rejects_invalid_timeouts_without_http_requests(self) -> None:
        for expect in ("absent", "present"):
            for timeout in ("0", "-1", "nan", "inf", "-inf"):
                with self.subTest(expect=expect, timeout=timeout):
                    result, requests = self.run_cli({}, expect=expect, timeout=timeout)
                    self.assert_cli_failure(result, "timeout must be a positive finite number")
                    self.assertEqual(requests, [])

    def test_cli_reports_unsupported_finite_timeout_without_a_traceback(self) -> None:
        for expect in ("absent", "present"):
            with self.subTest(expect=expect):
                result, requests = self.run_cli({}, expect=expect, timeout="1e300")
                self.assert_cli_failure(result, "timeout exceeds the transport's supported range")
                self.assertEqual(requests, [])


if __name__ == "__main__":
    unittest.main()
