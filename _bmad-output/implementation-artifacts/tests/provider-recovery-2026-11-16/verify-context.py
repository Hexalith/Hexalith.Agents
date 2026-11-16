#!/usr/bin/env python3
"""Verify that the accepted owner constraints survive in the generated context."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parents[4]
owner_path = root / "_bmad-output/implementation-artifacts/story-5-4-owner-commitments-2026-10-07.md"
context_path = root / "_bmad-output/implementation-artifacts/epic-5-context.md"
owner = owner_path.read_text()
context = context_path.read_text()
assert "Further owner inputs accepted — 2026-10-08" in owner
for phrase in ("currently open/undeleted", "including zero Agent Calls", "dedicated Conversations-owned service Party", "explicit tenant enrollment/current machine binding"):
    assert phrase in owner, phrase
for phrase in ("currently open/undeleted", "`CreatedAt`", "`[from,to)`", "including Conversations with zero Agent Calls", "dedicated Conversations-owned service Party", "explicit tenant enrollment and current machine binding"):
    assert phrase in context, phrase
assert "none of its four owner records is Available" in context
assert "do not establish dependency availability or live qualification" in context
print(json.dumps({"result": "passed", "assertions": 13, "owner_sha256": hashlib.sha256(owner_path.read_bytes()).hexdigest(), "context_sha256": hashlib.sha256(context_path.read_bytes()).hexdigest()}, indent=2))
