#!/usr/bin/env python3
"""Synthetic localhost script verification only; never dependency acceptance or live evidence."""
import argparse
import base64
import copy
import datetime as dt
import hashlib
import http.server
import json
import os
import re
from pathlib import Path
import subprocess
import shutil
import threading
import time

ROOT = Path('/home/administrator/projects/hexalith/parties')
ACTOR = '01HX0000000000000000000001'
SHA = 'a' * 40
POLICY = 'synthetic-365-v1'
COMMAND = 'synthetic compatibility command'

def stamp(value):
    return value.isoformat(timespec='microseconds').replace('+00:00', 'Z')

def fixture():
    now = dt.datetime.now(dt.timezone.utc)
    start = now - dt.timedelta(days=1)
    close = start + dt.timedelta(hours=1)
    expiry = start + dt.timedelta(days=365)
    custody = dict(policyId=POLICY, purpose='party-actor-history-v1', expiresAt=stamp(expiry), lifecycleRevision=1,
                   evidenceId='synthetic-custody', sourceExpiryEnforced=True, restoreSafe=True, derivedCopiesCovered=True)
    binding = dict(tenantId='synthetic-tenant', partyId='synthetic-party', actorId=ACTOR, bindingVersion=1,
                   actorRevision=1, validFrom=stamp(start), validUntil=stamp(expiry), sourceId='synthetic-writer',
                   provenanceId='synthetic-operator', custody=custody)
    opening = dict(binding=dict(evidence=binding, logicalId='synthetic-open', intentDigest='synthetic-digest'),
                   effectiveAt=stamp(start), expectedBindingVersion=0)
    revoke = dict(logicalId='synthetic-revoke', intentDigest='synthetic-revoke-digest', expectedBindingVersion=1,
                  effectiveAt=stamp(close), custody=dict(custody, expiresAt=stamp(close+dt.timedelta(days=365)), evidenceId='synthetic-revoke-custody'))
    def event(position, kind, payload):
        return dict(sequenceNumber=position, eventTypeName='Hexalith.Parties.Contracts.Events.HumanActorBinding'+kind,
                    payload=base64.b64encode(json.dumps(payload).encode()).decode(), serializationFormat='json',
                    protectionMetadata=dict(state='Unprotected'), messageId='', userId=None, correlationId=None,
                    causationId=None)
    source = dict(identity=dict(tenantId='synthetic-tenant', domain='party', aggregateId='synthetic-party'),
                  purpose='party-actor-history-v1', head=4, observedAt=stamp(now-dt.timedelta(seconds=10)),
                  validUntil=stamp(now+dt.timedelta(minutes=10)), authorityRevision='synthetic-r1',
                  observationId='synthetic-observation', events=[event(2, 'Established', opening), event(3, 'Revoked', revoke)],
                  excludedSequences=[1, 4])
    historical = dict(outcome='Resolved', contractVersion=1, tenantId='synthetic-tenant', partyId='synthetic-party',
                      actionAt=stamp(start+dt.timedelta(minutes=15)), sourcePosition=4, bindingSourcePosition=2,
                      observationId='synthetic-query-observation', evidence=copy.deepcopy(binding))
    historical['evidence']['validUntil'] = stamp(close)
    return source, historical, opening, revoke, event

class Server(http.server.ThreadingHTTPServer):
    daemon_threads = True
    def __init__(self):
        super().__init__(('127.0.0.1', 0), Handler)
        self.fixture = None
        self.requests = []
        self.sent_bytes = 0
        self.disconnected = False
        self.redirect_target = None
        self.is_redirect_destination = False

class Handler(http.server.BaseHTTPRequestHandler):
    protocol_version = 'HTTP/1.1'
    def log_message(self, *_):
        pass
    def do_POST(self):
        request = json.loads(self.rfile.read(int(self.headers['Content-Length'])))
        self.server.requests.append(dict(path=self.path, payload=request))
        if self.server.is_redirect_destination:
            self.send_response(200)
            self.send_header('Content-Length', '0')
            self.end_headers()
            return
        scenario, source, history = self.server.fixture
        if scenario == 'redirect-307':
            self.send_response(307)
            self.send_header('Location', self.server.redirect_target)
            self.send_header('Content-Length', '0')
            self.end_headers()
            return
        if scenario.startswith('oversized-'):
            self.send_response(200)
            self.send_header('Content-Type', 'application/json')
            if scenario == 'oversized-declared':
                self.send_header('Content-Length', str(40*1024*1024))
                self.end_headers()
                self.close_connection = True
                return
            self.send_header('Transfer-Encoding', 'chunked')
            self.end_headers()
            chunk = b'x'*(64*1024)
            try:
                for _ in range(640):
                    if self.server.sent_bytes >= 32*1024*1024:
                        time.sleep(.01)
                    self.wfile.write(('%X\r\n' % len(chunk)).encode()+chunk+b'\r\n')
                    self.wfile.flush()
                    self.server.sent_bytes += len(chunk)
                self.wfile.write(b'0\r\n\r\n')
            except (BrokenPipeError, ConnectionResetError):
                self.server.disconnected = True
            self.close_connection = True
            return
        if self.path == '/api/v1/identity-history/read':
            reply = dict(stream=source, failureReason=None)
        elif request['queryType'].endswith('ResolveHumanActorBindingAt'):
            reply = dict(success=True, payload=history, metadata=dict(isDegraded=False, isStale=False))
        else:
            reply = dict(success=True, payload=dict(outcome='Unavailable', evidence=None), metadata={})
        encoded = json.dumps(reply).encode()
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(encoded)))
        self.end_headers()
        self.wfile.write(encoded)

def run_case(scenario, output, server):
    source, history, opening, revoke, event = fixture()
    evidence = history['evidence']
    receipt = dict(recordId='EXT-PARTIES-1', targetVersionOrCommit=SHA, compatibilityCommand=COMMAND,
                   outcome='Pass', evidenceLevel=4, prerequisitesAvailable=True, policyId=POLICY,
                   retention='365.00:00:00', expiryTrigger='binding-effective-at')
    if scenario == 'transition-array': source['events'][0] = event(2, 'Established', [opening])
    elif scenario in ('string-predecessor', 'string-binding-version', 'string-actor-revision'):
        if scenario == 'string-predecessor': opening['expectedBindingVersion'] = '0'
        elif scenario == 'string-binding-version': opening['binding']['evidence']['bindingVersion'] = '1'
        else: opening['binding']['evidence']['actorRevision'] = '1'
        source['events'][0] = event(2, 'Established', opening)
    elif scenario == 'timestamp-grammar':
        opening['effectiveAt'] = dt.datetime.fromisoformat(opening['effectiveAt'].replace('Z','+00:00')).strftime('%Y/%m/%d %H:%M:%S')
        source['events'][0] = event(2, 'Established', opening)
    elif scenario == 'timestamp-source-expiry-grammar':
        source['validUntil'] = dt.datetime.fromisoformat(source['validUntil'].replace('Z','+00:00')).strftime('%Y/%m/%d %H:%M:%S')
    elif scenario == 'revocation-wrong-expiry':
        revoke['custody']['expiresAt'] = opening['binding']['evidence']['custody']['expiresAt']
        source['events'][1] = event(3, 'Revoked', revoke)
    elif scenario == 'revocation-past-expiry':
        revoke['custody']['expiresAt'] = stamp(dt.datetime.now(dt.timezone.utc)-dt.timedelta(seconds=1))
        source['events'][1] = event(3, 'Revoked', revoke)
    elif scenario == 'opening-shortened-without-close':
        opening['binding']['evidence']['validUntil'] = evidence['validUntil']
        source['events'] = [event(2, 'Established', opening)]
        source['excludedSequences'] = [1,3,4]
    elif scenario == 'positive-ticks':
        def tick_dates(value):
            if isinstance(value, dict): return {key:tick_dates(item) for key,item in value.items()}
            if isinstance(value, list): return [tick_dates(item) for item in value]
            if isinstance(value, str) and value.endswith('Z') and 'T' in value and '.' in value: return value[:-1]+'7Z'
            return value
        opening, revoke, history, source = map(tick_dates, (opening,revoke,history,source))
        evidence = history['evidence']
        source['events'] = [event(2,'Established',opening),event(3,'Revoked',revoke)]
    elif scenario == 'scope-tenant': evidence['tenantId'] = 'synthetic-other'
    elif scenario == 'scope-party': evidence['partyId'] = 'synthetic-other'
    elif scenario == 'actor-revision': evidence['actorRevision'] = 2
    elif scenario == 'source-id': evidence['sourceId'] = 'synthetic-other'
    elif scenario == 'provenance': evidence['provenanceId'] = 'synthetic-other'
    elif scenario == 'valid-from': evidence['validFrom'] = stamp(dt.datetime.fromisoformat(evidence['validFrom'].replace('Z','+00:00'))-dt.timedelta(seconds=1))
    elif scenario == 'valid-until': evidence['validUntil'] = opening['binding']['evidence']['validUntil']
    elif scenario == 'custody-expiry': evidence['custody']['expiresAt'] = stamp(dt.datetime.fromisoformat(evidence['custody']['expiresAt'].replace('Z','+00:00'))+dt.timedelta(seconds=1))
    elif scenario == 'custody-exact-expiry': evidence['custody']['expiresAt'] = stamp(dt.datetime.now(dt.timezone.utc))
    elif scenario == 'custody-lifecycle': evidence['custody']['lifecycleRevision'] = 2
    elif scenario == 'custody-evidence-id': evidence['custody']['evidenceId'] = 'synthetic-other'
    elif scenario == 'opening-duration':
        altered = stamp(dt.datetime.fromisoformat(evidence['custody']['expiresAt'].replace('Z','+00:00'))+dt.timedelta(seconds=1))
        opening['binding']['evidence']['custody']['expiresAt'] = altered
        opening['binding']['evidence']['validUntil'] = altered
        evidence['custody']['expiresAt'] = altered
        source['events'][0] = event(2, 'Established', opening)
    elif scenario == 'opening-scope':
        opening['binding']['evidence']['tenantId'] = 'synthetic-other'
        source['events'][0] = event(2, 'Established', opening)
    elif scenario == 'transition-revoke':
        revoke['effectiveAt'] = stamp(dt.datetime.fromisoformat(revoke['effectiveAt'].replace('Z','+00:00'))-dt.timedelta(minutes=30))
        revoke['custody']['expiresAt'] = stamp(dt.datetime.fromisoformat(revoke['effectiveAt'].replace('Z','+00:00'))+dt.timedelta(days=365))
        source['events'][1] = event(3, 'Revoked', revoke)
    elif scenario in ('positive-rebind', 'transition-rebind'):
        successor = copy.deepcopy(opening)
        successor['binding']['logicalId'] = 'synthetic-rebind'
        successor['expectedBindingVersion'] = 1
        successor['effectiveAt'] = revoke['effectiveAt']
        next_binding = successor['binding']['evidence']
        next_binding.update(actorId='01HX0000000000000000000002', bindingVersion=2, validFrom=revoke['effectiveAt'])
        expiry = stamp(dt.datetime.fromisoformat(revoke['effectiveAt'].replace('Z','+00:00'))+dt.timedelta(days=365))
        next_binding['validUntil'] = next_binding['custody']['expiresAt'] = expiry
        source['events'][1] = event(3, 'Rebound', successor)
        if scenario == 'transition-rebind': evidence['validUntil'] = opening['binding']['evidence']['validUntil']
    elif scenario == 'checkpoint-newer': history['sourcePosition'] = 5
    elif scenario == 'checkpoint-older': history['sourcePosition'] = 3
    elif scenario == 'observation': history['observationId'] = ''
    elif scenario == 'opening-position': history['bindingSourcePosition'] = 3
    elif scenario == 'receipt-mismatch': receipt['targetVersionOrCommit'] = 'b'*40
    elif scenario == 'decoded-oversize': source['events'][0]['payload'] = base64.b64encode(b'x'*(16*1024*1024+1)).decode()
    folder = output/scenario
    folder.mkdir(parents=True, exist_ok=True)
    register = folder/'synthetic-register.md'
    register.write_text('### EXT-PARTIES-1\n\n| Field | Value |\n|---|---|\n| `AcceptedStatus` | Available |\n'
                        f'| `TargetVersionOrCommit` | {SHA} |\n| `TargetIntegrationDate` | 2026-10-07 |\n'
                        f'| `CompatibilityContractAndVerificationCommand` | Command: `{COMMAND}`. Synthetic script fixture only. |\n')
    receipt_path = folder/'synthetic-receipt.json'
    receipt_path.write_text(json.dumps(receipt, indent=2)+'\n')
    (folder/'synthetic-source.json').write_text(json.dumps(source, indent=2)+'\n')
    (folder/'synthetic-query.json').write_text(json.dumps(history, indent=2)+'\n')
    env = os.environ.copy()
    env.update(EXT_PARTIES_GATEWAY_URL=f'http://127.0.0.1:{server.server_port}',
               EXT_PARTIES_READER_HEADERS_JSON='{"Authorization":"Bearer synthetic-only"}', EXT_PARTIES_TARGET_SHA=SHA,
               EXT_PARTIES_COMPATIBILITY_RECEIPT=str(receipt_path), EXT_PARTIES_TENANT_A='synthetic-tenant',
               EXT_PARTIES_HISTORY_PARTY_ID='synthetic-party', EXT_PARTIES_HISTORY_ACTOR_ID=ACTOR,
               EXT_PARTIES_HISTORY_ACTION_AT=history['actionAt'], EXT_PARTIES_HISTORY_BINDING_VERSION='1',
               EXT_PARTIES_POLICY_ID=POLICY, EXT_PARTIES_RETENTION='365.00:00:00', EXT_PARTIES_EXPIRY_TRIGGER='binding-effective-at')
    if scenario == 'policy-duration':
        env['EXT_PARTIES_RETENTION'] = receipt['retention'] = '366.00:00:00'
        receipt_path.write_text(json.dumps(receipt, indent=2)+'\n')
    destination = None
    destination_thread = None
    if scenario == 'redirect-307':
        destination = Server()
        destination.is_redirect_destination = True
        destination_thread = threading.Thread(target=destination.serve_forever,daemon=True)
        destination_thread.start()
        server.redirect_target = f'http://127.0.0.1:{destination.server_port}/synthetic-redirect-destination'
    server.fixture = scenario, source, history
    server.requests = []
    server.sent_bytes = 0
    server.disconnected = False
    script = ROOT/'eng/verify-ext-parties-history-readiness.ps1'
    args = ['pwsh', '-NoProfile', '-File', str(script), '-EvidenceDirectory', 'relative-evidence',
            '-DependencyRegisterPath', str(ROOT.parent/'agents/_bmad-output/planning-artifacts/external-dependency-register.md') if scenario == 'uncommitted-register' else register.name]
    if scenario == 'positive-wrapper-relative':
        args = ['pwsh', '-NoProfile', '-File', str(ROOT/'eng/verify-ext-parties-1.ps1'), '-Mode', 'LiveReadiness',
                '-EvidenceDirectory', 'relative-evidence', '-DependencyRegisterPath', register.name,
                '-MemoriesRoot', 'relative-memories', '-ArtifactsDirectory', 'relative-artifacts']
    result = subprocess.run(args, cwd=folder, env=env, capture_output=True, text=True, timeout=60)
    (folder/'execution.log').write_text(result.stdout+result.stderr)
    if destination is not None:
        destination.shutdown(); destination.server_close(); destination_thread.join()
        assert len(destination.requests) == 0, destination.requests
        assert len(server.requests) == 1 and re.search(r'HTTP\s*\|?\s*307', result.stderr) is not None, (len(server.requests),result.returncode,result.stdout,result.stderr)
    (folder/'execution.log').write_text(result.stdout+result.stderr)
    success = scenario.startswith('positive')
    receipt_exists = (folder/'relative-evidence/live-readiness.json').exists()
    expected_calls = 3 if success else None
    assert (result.returncode == 0) == success, (scenario, result.returncode, result.stdout, result.stderr)
    assert receipt_exists == success, (scenario, receipt_exists)
    if scenario in ('receipt-mismatch', 'policy-duration', 'uncommitted-register'):

        assert len(server.requests) == 0, (scenario, server.requests)
    if success: assert len(server.requests) == expected_calls
    if scenario.startswith('oversized'):
        assert 'exceeds the source bound' in result.stderr
        assert len(server.requests) == 1
        time.sleep(.15)
        if scenario == 'oversized-streamed':
            assert server.disconnected and server.sent_bytes < 40*1024*1024
    return dict(scenario=scenario, expected='synthetic-script-pass' if success else 'refused',
                exitCode=result.returncode, requestCount=len(server.requests), successReceipt=receipt_exists,
                sentBytes=server.sent_bytes, disconnected=server.disconnected, destinationRequestCount=len(destination.requests) if destination else None, command=args,
                qualification='SyntheticScriptVerificationOnly')

def local_path_case(output, powershell_location=False):
    folder = output/('wrapper-powershell-location' if powershell_location else 'wrapper-local-relative')
    folder.mkdir(parents=True, exist_ok=True)
    fake_bin = folder/'synthetic-bin'
    fake_bin.mkdir(exist_ok=True)
    capture = folder/'synthetic-dotnet-args.json'
    fake = fake_bin/'dotnet'
    fake.write_text('#!/usr/bin/env python3\nimport json,os,sys\nfrom pathlib import Path\nif sys.argv[1].endswith("pwsh.dll"): os.execv(os.environ["SYNTHETIC_REAL_DOTNET"], ["dotnet"]+sys.argv[1:])\nPath(os.environ["SYNTHETIC_CAPTURE"]).write_text(json.dumps({"cwd":os.getcwd(),"args":sys.argv[1:]}))\nprint("Synthetic path probe intentionally stops before any real build")\nsys.exit(1)\n')
    fake.chmod(0o755)
    env = os.environ.copy()
    env['PATH'] = str(fake_bin)+os.pathsep+env['PATH']
    env['SYNTHETIC_CAPTURE'] = str(capture)
    env['SYNTHETIC_REAL_DOTNET'] = shutil.which('dotnet')
    args = ['pwsh','-NoProfile','-File',str(ROOT/'eng/verify-ext-parties-1.ps1'),'-Mode','Local',
            '-EvidenceDirectory','relative-evidence','-MemoriesRoot','relative-memories',
            '-ArtifactsDirectory','relative-artifacts','-DependencyRegisterPath','relative-register.md',
            '-EventStoreRoot',os.path.relpath(ROOT.parent/'eventstore',folder),
            '-PlatformRoot',os.path.relpath(ROOT.parent/'platform',folder)]
    launch_folder = folder
    if powershell_location:
        def quote(value): return "'"+value.replace("'", "''")+"'"
        command = 'Set-Location -LiteralPath '+quote(str(folder))+'; & '+ ' '.join(arg if arg.startswith('-') else quote(arg) for arg in args[3:])
        args = ['pwsh', '-NoProfile', '-Command', command]
        launch_folder = output
    result = subprocess.run(args,cwd=launch_folder,env=env,capture_output=True,text=True,timeout=30)
    (folder/'execution.log').write_text(result.stdout+result.stderr)
    seen=json.loads(capture.read_text())
    assert result.returncode != 0 and 'Build failed' in result.stderr, (result.returncode, result.stdout, result.stderr)
    assert seen['cwd'] == str(ROOT.parent/'eventstore')
    assert str(folder/'relative-artifacts') in seen['args']
    assert '-p:HexalithMemoriesRoot='+str(folder/'relative-memories') in seen['args']
    assert (folder/'relative-evidence/Hexalith.EventStore.Contracts.Tests-build.log').exists()
    return dict(scenario='wrapper-powershell-location' if powershell_location else 'wrapper-local-relative',expected='normalized-caller-paths-before-owner-cwd',exitCode=result.returncode,
                command=args,qualification='SyntheticPathVerificationOnly',mockedDotnet=seen)

def live_gate_case(output):
    folder = output/'wrapper-live-gate'
    folder.mkdir(parents=True, exist_ok=True)
    env = {key:value for key,value in os.environ.items() if not key.startswith('EXT_PARTIES_')}
    args = ['pwsh','-NoProfile','-File',str(ROOT/'eng/verify-ext-parties-1.ps1'),'-Mode','Live',
            '-EvidenceDirectory','relative-evidence']
    result = subprocess.run(args,cwd=folder,env=env,capture_output=True,text=True,timeout=30)
    (folder/'execution.log').write_text(result.stdout+result.stderr)
    assert result.returncode != 0 and 'Missing live inputs' in result.stderr
    assert 'Missing live inputs' in (folder/'relative-evidence/live-gate.txt').read_text()
    assert not (folder/'relative-evidence/live-readiness.json').exists()
    return dict(scenario='wrapper-live-gate',expected='complete-Live-gate-remains-refused',exitCode=result.returncode,
                command=args,qualification='SyntheticScriptVerificationOnly')

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--output',type=Path,default=Path(__file__).resolve().parent/'synthetic-verifier')
    parser.add_argument('--scenario',action='append')
    options=parser.parse_args()
    output=options.output.resolve(); output.mkdir(parents=True,exist_ok=True)
    scenarios=options.scenario or ['positive','positive-rebind','positive-wrapper-relative','scope-tenant','scope-party',
        'actor-revision','source-id','provenance','valid-from','valid-until','custody-expiry','custody-exact-expiry',
        'custody-lifecycle','custody-evidence-id','opening-duration','opening-scope','transition-revoke','transition-rebind',
        'checkpoint-newer','checkpoint-older','observation','opening-position','policy-duration','decoded-oversize',
        'oversized-declared','oversized-streamed','receipt-mismatch','uncommitted-register',
        'transition-array','string-predecessor','string-binding-version','string-actor-revision','timestamp-grammar',
        'timestamp-source-expiry-grammar','revocation-wrong-expiry','revocation-past-expiry','opening-shortened-without-close',
        'positive-ticks','redirect-307']
    results=[]
    server=Server(); thread=threading.Thread(target=server.serve_forever,daemon=True); thread.start()
    try:
        for scenario in scenarios:
            result=run_case(scenario,output,server);results.append(result)
            print(scenario+': verified',flush=True)
    finally:
        server.shutdown();server.server_close();thread.join()
    if not options.scenario:
        results.append(local_path_case(output))
        results.append(local_path_case(output, powershell_location=True))
        results.append(live_gate_case(output))
    (output/'results.json').write_text(json.dumps(dict(qualification='SyntheticScriptVerificationOnly',results=results),indent=2)+'\n')
    print(f'{len(results)} synthetic script/path cases verified; no owner dependency acceptance or live qualification.',flush=True)

if __name__=='__main__': main()
