"""Synthetic loopback fixtures for the pinned Parties read-only verifier."""

from __future__ import annotations

import base64
import copy
import datetime as dt
import http.server
import json
import os
import re
import threading
import time
from contextlib import contextmanager


ACTOR = "01HX0000000000000000000001"
TARGET_SHA = "a" * 40
POLICY = "synthetic-365-v1"
COMMAND = "synthetic compatibility command"
MIB = 1024 * 1024


def stamp(value):
    return value.isoformat(timespec="microseconds").replace("+00:00", "Z")


def instant(value):
    return dt.datetime.fromisoformat(value.replace("Z", "+00:00"))


def exact_instant(value):
    """Compare ISO instants without losing the seventh fractional second digit."""
    fraction = re.search(r"\.(\d{1,7})(?=Z$|[+-]\d{2}:\d{2}$)", value)
    ticks = int(fraction[1].ljust(7, "0")) if fraction else 0
    whole_second = value[:fraction.start()] + value[fraction.end():] if fraction else value
    return instant(whole_second), ticks


def event(position, kind, payload):
    return dict(
        sequenceNumber=position,
        eventTypeName="Hexalith.Parties.Contracts.Events.HumanActorBinding" + kind,
        payload=base64.b64encode(json.dumps(payload).encode()).decode(),
        serializationFormat="json", protectionMetadata=dict(state="Unprotected"),
        messageId="", userId=None, correlationId=None, causationId=None,
    )


def binding_fixture():
    now = dt.datetime.now(dt.timezone.utc)
    start = now - dt.timedelta(days=1)
    close = start + dt.timedelta(hours=1)
    expiry = start + dt.timedelta(days=365)
    custody = dict(
        policyId=POLICY, purpose="party-actor-history-v1", expiresAt=stamp(expiry),
        lifecycleRevision=1, evidenceId="synthetic-custody", sourceExpiryEnforced=True,
        restoreSafe=True, derivedCopiesCovered=True,
    )
    binding = dict(
        tenantId="synthetic-tenant", partyId="synthetic-party", actorId=ACTOR,
        bindingVersion=1, actorRevision=1, validFrom=stamp(start), validUntil=stamp(expiry),
        sourceId="synthetic-writer", provenanceId="synthetic-operator", custody=custody,
    )
    opening = dict(
        binding=dict(evidence=binding, logicalId="synthetic-open", intentDigest="synthetic-digest"),
        effectiveAt=stamp(start), expectedBindingVersion=0,
    )
    revoke = dict(
        logicalId="synthetic-revoke", intentDigest="synthetic-revoke-digest",
        expectedBindingVersion=1, effectiveAt=stamp(close),
        custody=dict(custody, expiresAt=stamp(close + dt.timedelta(days=365))),
    )
    source = dict(
        identity=dict(tenantId="synthetic-tenant", domain="party", aggregateId="synthetic-party"),
        purpose="party-actor-history-v1", head=4, observedAt=stamp(now - dt.timedelta(seconds=10)),
        validUntil=stamp(now + dt.timedelta(minutes=10)), authorityRevision="synthetic-r1",
        observationId="synthetic-source-observation",
        events=[event(2, "Established", opening), event(3, "Revoked", revoke)],
        excludedSequences=[1, 4],
    )
    history = dict(
        outcome="Resolved", contractVersion=1, tenantId="synthetic-tenant", partyId="synthetic-party",
        actionAt=stamp(start + dt.timedelta(minutes=15)), sourcePosition=4, bindingSourcePosition=2,
        observationId="synthetic-query-observation", evidence=copy.deepcopy(binding),
    )
    history["evidence"]["validUntil"] = stamp(close)
    return source, history, opening, revoke


def replace_with_rebind(source, opening, revoke):
    successor = copy.deepcopy(opening)
    successor["binding"]["logicalId"] = "synthetic-rebind"
    successor["expectedBindingVersion"] = 1
    successor["effectiveAt"] = revoke["effectiveAt"]
    binding = successor["binding"]["evidence"]
    binding.update(actorId="01HX0000000000000000000002", bindingVersion=2,
                   validFrom=revoke["effectiveAt"])
    expiry = stamp(instant(revoke["effectiveAt"]) + dt.timedelta(days=365))
    binding["validUntil"] = binding["custody"]["expiresAt"] = expiry
    source["events"][1] = event(3, "Rebound", successor)


def tick_dates(value):
    if isinstance(value, dict):
        return {key: tick_dates(item) for key, item in value.items()}
    if isinstance(value, list):
        return [tick_dates(item) for item in value]
    if isinstance(value, str) and value.endswith("Z") and "T" in value and "." in value:
        return value[:-1] + "7Z"
    return value


def clean_environment():
    return {key: value for key, value in os.environ.items()
            if not key.upper().startswith("EXT_PARTIES_")}


class OwnerServer(http.server.ThreadingHTTPServer):
    """Serve only synthetic identity replies; never contacts an owner endpoint."""

    daemon_threads = False

    def __init__(self, source, history, mode="normal"):
        super().__init__(("127.0.0.1", 0), OwnerHandler)
        self.source = source
        self.history = history
        self.mode = mode
        self.requests = []
        self.redirect_target = None
        self.sent_bytes = 0
        self.disconnected = False
        self.body_finished = threading.Event()

    @property
    def url(self):
        return f"http://127.0.0.1:{self.server_port}"


class OwnerHandler(http.server.BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def setup(self):
        super().setup()
        self.connection.settimeout(5)

    def log_message(self, *_):
        pass

    def do_POST(self):
        server = self.server
        request = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
        server.requests.append(dict(path=self.path, payload=request))
        if server.mode == "redirect":
            self.send_response(307)
            self.send_header("Location", server.redirect_target)
            self.send_header("Content-Length", "0")
            self.send_header("Connection", "close")
            self.end_headers()
            self.close_connection = True
            return
        if server.mode in ("declared-overflow", "streamed-overflow", "streamed-boundary-overflow"):
            self.send_response(200)
            if server.mode == "declared-overflow":
                self.send_header("Content-Length", str(32 * MIB + 1))
                self.end_headers()
                self.close_connection = True
                server.body_finished.set()
                return
            self.send_header("Transfer-Encoding", "chunked")
            self.end_headers()
            chunk = b"x" * (64 * 1024)
            body_size = 32 * MIB + 1 if server.mode == "streamed-boundary-overflow" else 40 * MIB
            try:
                while server.sent_bytes < body_size:
                    # A bounded receiver should disconnect well before the complete 40 MiB body.
                    if server.sent_bytes >= 32 * MIB:
                        time.sleep(.01)
                    block = chunk[:min(len(chunk), body_size - server.sent_bytes)]
                    self.wfile.write(f"{len(block):X}\r\n".encode() + block + b"\r\n")
                    self.wfile.flush()
                    server.sent_bytes += len(block)
                self.wfile.write(b"0\r\n\r\n")
            except (BrokenPipeError, ConnectionResetError):
                server.disconnected = True
            finally:
                self.close_connection = True
                server.body_finished.set()
            return
        if self.path == "/api/v1/identity-history/read":
            reply = dict(stream=server.source, failureReason=None)
        elif request["queryType"].endswith("ResolveHumanActorBindingAt"):
            reply = dict(success=True, payload=server.history,
                         metadata=dict(isDegraded=False, isStale=False))
        else:
            reply = dict(success=True, payload=dict(outcome="Unavailable", evidence=None), metadata={})
        encoded = json.dumps(reply).encode()
        if server.mode == "at-wire-limit" and self.path == "/api/v1/identity-history/read":
            encoded += b" " * (32 * MIB - len(encoded))
        self.send_response(200)
        self.send_header("Content-Type", "application/json")
        self.send_header("Content-Length", str(len(encoded)))
        self.send_header("Connection", "close")
        self.end_headers()
        self.close_connection = True
        try:
            self.wfile.write(encoded)
        except (BrokenPipeError, ConnectionResetError):
            server.disconnected = True


@contextmanager
def owner_server(source, history, mode="normal"):
    server = OwnerServer(source, history, mode)
    thread = threading.Thread(target=server.serve_forever, kwargs=dict(poll_interval=.01))
    thread.start()
    try:
        yield server
    finally:
        server.shutdown()
        server.server_close()
        thread.join(timeout=5)
