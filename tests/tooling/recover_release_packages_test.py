"""Exercise recovery preflight against immutable local archives and controlled remote inventory."""
from __future__ import annotations

import hashlib
import importlib
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from nuget_package_fixtures import PACKAGE_IDS, ROOT, VERSION, write_inventory

sys.path.insert(0, str(ROOT / "scripts"))
RECOVERY = importlib.import_module("recover-release-packages")
VERIFY = importlib.import_module("verify-nuget-publication")


class OriginalReleaseLookupTests(unittest.TestCase):
    repository = "Hexalith/Hexalith.Agents"
    tag = f"v{VERSION}"

    def release(self, **changes):
        return {"id": 101, "tag_name": self.tag, "draft": True,
                "target_commitish": "a" * 40, "assets": [{"digest": "original digest"}], **changes}

    def response(self, pages):
        return "\n \t".join(json.dumps(page) for page in pages) if isinstance(pages, list) else json.dumps(pages)

    def test_exact_draft_on_later_page_retains_full_original_metadata(self):
        original = self.release()
        pages = [[self.release(id=17, tag_name="v0.0.1", draft=False)], [], [original]]
        with patch.object(RECOVERY, "command", return_value=self.response(pages)) as command:
            self.assertEqual(RECOVERY.load_original_release(self.repository, self.tag), original)
        command.assert_called_once_with([
            "gh", "api", f"repos/{self.repository}/releases?per_page=100", "--paginate",
        ])

    def test_missing_or_ambiguous_exact_tags_are_rejected_across_all_pages(self):
        original = self.release()
        cases = [[], [[]], [[self.release(tag_name="v0.0.1")]],
                 [[original], [original]], [[original], [self.release(id=102, draft=False)]]]
        for pages in cases:
            with self.subTest(pages=pages), patch.object(RECOVERY, "command", return_value=self.response(pages)):
                with self.assertRaisesRegex(ValueError, "exactly one original release"):
                    RECOVERY.load_original_release(self.repository, self.tag)

    def test_malformed_pages_and_release_identities_fail_closed(self):
        cases = [None, {}, [None], [{}], [[None]], [[[]]], [["release"]],
                 [[self.release(id=0)]], [[self.release(id=True)]], [[self.release(id="101")]],
                 [[self.release(tag_name=None)]], [[self.release(tag_name={})]],
                 [[self.release(draft=None)]], [[self.release(draft="true")]],
                 [[self.release()], [self.release(id=False, tag_name="unrelated")]]]
        for pages in cases:
            with self.subTest(pages=pages), patch.object(RECOVERY, "command", return_value=self.response(pages)):
                with self.assertRaisesRegex(ValueError, "GitHub release inventory"):
                    RECOVERY.load_original_release(self.repository, self.tag)

    def test_invalid_json_is_not_treated_as_an_empty_inventory(self):
        for response in ("not JSON", self.response([[self.release()]]) + "\nnot JSON",
                         self.response([[self.release()]]) + "\u00a0"):
            with self.subTest(response=response), patch.object(RECOVERY, "command", return_value=response):
                with self.assertRaises(ValueError):
                    RECOVERY.load_original_release(self.repository, self.tag)


class RecoverReleasePackagesTests(unittest.TestCase):
    def run_recovery(self, fault="", verify_only=False):
        calls = []
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            write_inventory(directory)
            def probe(package_id):
                calls.append(("probe", package_id))
                if fault == "unconfirmed" and package_id == PACKAGE_IDS[-1]:
                    raise VERIFY.TransientProbeError("HTTP 503")
                return VERIFY.ProbeResult(package_id, VERSION, package_id in PACKAGE_IDS[:2])
            def download(package_id, contents):
                calls.append(("compare", package_id))
                if fault == "conflict" and package_id == PACKAGE_IDS[1]:
                    raise VERIFY.VerificationError("content mismatch")
                self.assertTrue(contents)
            def source():
                calls.append(("source", ""))
                if fault == "source":
                    raise ValueError("main advanced")
            def push(path):
                calls.append(("push", path.name))
            def final():
                calls.append(("final", ""))
                if fault == "final":
                    raise ValueError("presence unconfirmed")
            try:
                RECOVERY.recover_inventory(PACKAGE_IDS, VERSION, directory, before_push=source,
                    probe=probe, download=download, push=push, final_verify=final, verify_only=verify_only)
                return None, calls
            except (ValueError, VERIFY.VerificationError) as error:
                return error, calls

    def test_all_existing_content_matches_before_any_missing_push(self):
        error, calls = self.run_recovery()
        self.assertIsNone(error)
        first_push = next(i for i, call in enumerate(calls) if call[0] == "push")
        self.assertEqual(sum(call[0] == "probe" for call in calls[:first_push]), len(PACKAGE_IDS))
        self.assertEqual([call[1] for call in calls if call[0] == "compare"], list(PACKAGE_IDS[:2]))
        self.assertEqual(calls[first_push - 1][0], "source")
        self.assertEqual([call[1] for call in calls if call[0] == "push"],
            [f"{package_id}.{VERSION}.nupkg" for package_id in PACKAGE_IDS[2:]])
        self.assertEqual(calls[-1][0], "final")

    def test_conflict_unconfirmed_or_changed_source_refuse_every_push(self):
        for fault in ("conflict", "unconfirmed", "source"):
            with self.subTest(fault=fault):
                error, calls = self.run_recovery(fault)
                self.assertIsNotNone(error)
                self.assertFalse(any(call[0] == "push" for call in calls))

    def test_final_proof_failure_and_verify_only_incomplete_are_failures(self):
        self.assertIsNotNone(self.run_recovery("final")[0])
        error, calls = self.run_recovery(verify_only=True)
        self.assertIsNotNone(error)
        self.assertFalse(any(call[0] == "push" for call in calls))

    def test_provenance_requires_original_source_and_each_original_archive_hash(self):
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            paths = write_inventory(directory)
            provenance = {"schemaVersion": 1, "repository": "Hexalith/Hexalith.Agents", "version": VERSION,
                "sourceSha": "a" * 40, "tag": f"v{VERSION}", "archives": [
                    {"name": path.name, "size": path.stat().st_size,
                     "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in paths]}
            RECOVERY.verify_provenance(provenance, directory, PACKAGE_IDS, VERSION,
                "Hexalith/Hexalith.Agents", "a" * 40)
            for field, changed in (("sourceSha", "b" * 40), ("version", "9.9.9")):
                with self.assertRaises(ValueError):
                    RECOVERY.verify_provenance({**provenance, field: changed}, directory,
                        PACKAGE_IDS, VERSION, "Hexalith/Hexalith.Agents", "a" * 40)
            paths[-1].write_bytes(b"repacked bytes")
            with self.assertRaisesRegex(ValueError, "integrity mismatch"):
                RECOVERY.verify_provenance(provenance, directory, PACKAGE_IDS, VERSION,
                    "Hexalith/Hexalith.Agents", "a" * 40)


class RecoverySubprocessTests(unittest.TestCase):
    def invoke_recovery(self, fault="", mode=""):
        import http.server
        import os
        import subprocess
        import threading
        from nuget_package_fixtures import write_package
        with tempfile.TemporaryDirectory(prefix="agents-recovery-process-") as temporary:
            directory = Path(temporary)
            archives = directory / "original"
            archives.mkdir()
            paths = write_inventory(archives)
            provenance = {"schemaVersion": 1, "repository": "Hexalith/Hexalith.Agents", "version": VERSION,
                "sourceSha": "a" * 40, "tag": f"v{VERSION}", "archives": [
                    {"name": path.name, "size": path.stat().st_size,
                     "sha256": hashlib.sha256(path.read_bytes()).hexdigest()} for path in paths]}
            (archives / "release-provenance.json").write_text(json.dumps(provenance))
            release = {"id": 101, "draft": True, "tag_name": f"v{VERSION}", "target_commitish": "a"*40,
                "assets": [{"name": path.name, "state": "uploaded", "size": path.stat().st_size,
                    "digest": "sha256:"+hashlib.sha256(path.read_bytes()).hexdigest()} for path in archives.iterdir()]}
            (directory / "release.json").write_text(json.dumps(release))
            pushed = directory / "pushed.jsonl"
            calls = directory / "calls.jsonl"
            remote_paths = {package_id.casefold(): archives / f"{package_id}.{VERSION}.nupkg" for package_id in PACKAGE_IDS}
            if fault == "conflict":
                other = directory / "conflict"
                other.mkdir()
                remote_paths[PACKAGE_IDS[1].casefold()] = write_package(other, PACKAGE_IDS[1], readme=b"different immutable remote content")
            class Handler(http.server.BaseHTTPRequestHandler):
                def do_GET(self):
                    parts = self.path.strip("/").split("/")
                    package_id = parts[1]
                    if parts[0] == "registration":
                        if fault == "unconfirmed" and package_id == PACKAGE_IDS[-1].casefold():
                            self.send_error(503)
                            return
                        existing = package_id in {item.casefold() for item in PACKAGE_IDS[:2]}
                        newly_pushed = pushed.exists() and package_id in pushed.read_text().casefold()
                        if not existing and not newly_pushed:
                            self.send_error(404)
                            return
                        body = json.dumps({"catalogEntry": {"id": package_id, "version": VERSION}}).encode()
                    else:
                        body = remote_paths[package_id].read_bytes()
                    self.send_response(200)
                    self.end_headers()
                    self.wfile.write(body)
                def log_message(self, *_args):
                    pass
            server = http.server.ThreadingHTTPServer(("127.0.0.1", 0), Handler)
            thread = threading.Thread(target=server.serve_forever, daemon=True)
            thread.start()
            commands = directory / "commands"
            commands.mkdir()
            stub = f'''#!{sys.executable}
import json, os, pathlib, shutil, sys
name = pathlib.Path(sys.argv[0]).name
args = sys.argv[1:]
root = pathlib.Path(os.environ['FIXTURE'])
with (root/'calls.jsonl').open('a') as output:
    output.write(json.dumps([name, *args[:2]])+'\\n')
if name == 'git':
    print(('b' if os.environ['FAULT'] == 'tag' else 'a')*40)
elif name == 'dotnet':
    with (root/'pushed.jsonl').open('a') as output:
        output.write(pathlib.Path(args[2]).name+'\\n')
elif args[0] == 'api':
    endpoint = args[1]
    if '/releases/tags/' in endpoint:
        print('Release-by-tag does not return an unpublished draft.', file=sys.stderr)
        raise SystemExit(1)
    elif endpoint == 'repos/Hexalith/Hexalith.Agents/releases?per_page=100':
        if '--slurp' in args:
            raise SystemExit('unknown flag: --slurp')
        if '--paginate' not in args:
            raise SystemExit('The complete release inventory is required.')
        draft = json.loads((root/'release.json').read_text())
        pages = [[{{'id': 17, 'tag_name': 'v0.0.1', 'draft': False}}], [draft]]
        fault = os.environ['FAULT']
        if fault == 'draft-missing': pages.pop()
        elif fault == 'draft-duplicate': pages.append([{{**draft, 'id': 102}}])
        elif fault == 'inventory-malformed': pages = {{'releases': [draft]}}
        elif fault == 'assets-malformed': draft['assets'] = {{}}
        elif fault == 'asset-entry-malformed': draft['assets'][0] = 'asset'
        elif fault == 'asset-name-malformed': draft['assets'][0]['name'] = {{}}
        elif fault == 'published': draft['draft'] = False
        elif fault == 'inventory-error':
            print('fixture-key', file=sys.stderr)
            raise SystemExit(89)
        if isinstance(pages, list):
            for page in pages:
                print(json.dumps(page))
        else:
            print(json.dumps(pages))
    elif '/git/ref/' in endpoint:
        print(json.dumps({{'object':{{'type':'commit','sha':'a'*40}}}}))
    else:
        print(json.dumps({{'total_count':1,'workflow_runs':[{{'head_sha':'a'*40,'head_branch':'main','event':'push','status':'completed','conclusion':'success'}}]}}))
elif args[:2] == ['release','download']:
    destination = pathlib.Path(args[args.index('--dir')+1])
    for path in (root/'original').iterdir():
        shutil.copyfile(path,destination/path.name)
    if os.environ['FAULT'] == 'archive':
        next(destination.glob('*.nupkg')).write_bytes(b'tampered original')
'''
            for name in ("gh", "git", "dotnet"):
                command = commands / name
                command.write_text(stub)
                command.chmod(0o755)
            base = f"http://127.0.0.1:{server.server_address[1]}"
            # Subprocess executes the real CLI; only the NuGet transport endpoints are redirected to loopback.
            harness = f'''import importlib, runpy, sys
sys.path.insert(0,{str(ROOT / 'scripts')!r})
v=importlib.import_module('verify-nuget-publication')
v.DEFAULT_REGISTRATION_BASE={base + '/registration'!r}
v.DEFAULT_PACKAGE_BASE={base + '/packages'!r}
sys.argv=['recover-release-packages.py',{VERSION!r},'--source-sha','a'*40]+{['--' + mode] if mode else []!r}
runpy.run_path({str(ROOT / 'scripts/recover-release-packages.py')!r},run_name='__main__')
'''
            try:
                result = subprocess.run([sys.executable, "-c", harness], cwd=ROOT,
                    env={**os.environ, "PATH": str(commands)+os.pathsep+os.environ["PATH"],
                         "FIXTURE":str(directory),"FAULT":fault,"HEXALITH_RELEASE_PUBLISH_ENABLED":"true",
                         "GITHUB_REPOSITORY":"Hexalith/Hexalith.Agents","NUGET_API_KEY":"fixture-key"},
                    capture_output=True,text=True,timeout=25)
                return result, calls.read_text() if calls.exists() else "", pushed.read_text() if pushed.exists() else ""
            finally:
                server.shutdown()
                server.server_close()
                thread.join()

    def test_existing_tag_and_unpublished_draft_recover_without_release_planning(self):
        result, calls, pushes = self.invoke_recovery()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(len(pushes.splitlines()), len(PACKAGE_IDS)-2)
        self.assertIn('"release", "edit"', calls)
        self.assertNotIn("semantic-release", calls)
        self.assertIn('/releases?per_page=100', calls)
        self.assertNotIn('/releases/tags/', calls)

    def test_conflicts_unconfirmed_probes_wrong_tags_and_tampered_originals_never_push(self):
        for fault in ("conflict", "unconfirmed", "tag", "archive"):
            with self.subTest(fault=fault):
                result, calls, pushes = self.invoke_recovery(fault)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(pushes, "")
                self.assertNotIn('"release", "edit"', calls)
                self.assertNotIn("fixture-key", result.stderr)

    def test_missing_ambiguous_malformed_or_published_records_never_download_or_push(self):
        for fault in ("draft-missing", "draft-duplicate", "inventory-malformed", "assets-malformed",
                      "asset-entry-malformed", "asset-name-malformed", "published", "inventory-error"):
            with self.subTest(fault=fault):
                result, calls, pushes = self.invoke_recovery(fault)
                self.assertNotEqual(result.returncode, 0)
                self.assertEqual(pushes, "")
                self.assertNotIn('"release", "download"', calls)
                self.assertNotIn('"release", "edit"', calls)
                self.assertNotIn("fixture-key", result.stderr)

    def test_staged_only_proves_original_draft_without_nuget_or_publication(self):
        result, calls, pushes = self.invoke_recovery(mode="staged-only")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Verified retained original release assets", result.stdout)
        self.assertEqual(pushes, "")
        self.assertNotIn('"release", "edit"', calls)

    def test_verify_only_keeps_refusing_an_incomplete_remote_inventory(self):
        result, calls, pushes = self.invoke_recovery(mode="verify-only")
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(pushes, "")
        self.assertNotIn('"release", "edit"', calls)
