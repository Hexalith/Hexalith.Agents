"""Record every final revision-five claim separately before grouping or routing."""
import datetime
import hashlib
import json
from pathlib import Path
import shutil

review = Path(__file__).resolve().parent
workspace = review.parents[5]
spec = workspace / '_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
before = spec.read_bytes()
assert hashlib.sha256(before).hexdigest() == '60eb78110670ee4c2bebe3874e8e0b0116141db6af17566cd3309ea3ae6634f3'
assert b'review_loop_iteration: 5' in before
assert not (review / 'current-review-triage.json').exists()

def finding(identifier, location, verdict, evidence, proposed_route, smallest_fix=None, carried_from=None):
    row = dict(id=identifier, location=location, verdict=verdict,
               evidence=evidence, proposedRoute=proposed_route,
               verification='carried; claim verification skipped' if carried_from else 'root source/caller/contract tracing; no new counterexample execution claimed')
    if smallest_fix:
        row['reviewableCorrection'] = smallest_fix
    if carried_from:
        row['carriedFrom'] = carried_from
    return row

rows = [
    finding('B1', '../platform/src/Hexalith.Platform.Custody/TrustedEnvelopeReplayVerifier.cs:19,39', 'medium',
            'Both profiles.GetCurrent calls are direct. The terminal call follows budget.Check, and neither initial nor final call is wrapped by the existing budget; a blocked profile holds VerifyAsync, and a final provider canceling the caller can return an authenticated result without another cancellation check. Authenticator waits and exact retained receipt checks do not guard that final synchronous provider invocation.', 'patch',
            'Use the existing deadline for both pure profile-provider calls and check the same budget immediately before release; preserve receipt, current profile, time and original caller checks.'),
    finding('B2', '../platform/src/Hexalith.Platform.Custody/DeletionCapabilitySigningActor.cs:36; CustodyKeyLifecycleActor.cs:46; ExportKeyDeliveryActor.cs:24', 'medium',
            'Initial/final operation admission, signer trust, StateManager calls and anchored-journal authority calls are awaited directly. A suspended admission holds even a direct actor call; final admission or durable Save after the last budget.Check can complete after the operation budget and still return terminal evidence. The coordinator can bound its caller but cannot release the same-tenant non-reentrant lifecycle turn. Wrapping an entire actor continuation in Task.Run would abandon mutable actor state and violate the existing turn rule.', 'bad_spec',
            'Define and implement bounded dependency admission and terminal-release checks with safe actor-state lifetime and exact admitted-original recovery. Bound only work that can safely outlive the turn; no off-turn StateManager mutations or late continuation overwrites.'),
    finding('B3', '../platform/src/Hexalith.Platform.Custody/DeletionBatchExecutionCoordinator.cs:21,57', 'medium',
            'The public constructor accepts synchronous signer and protection-owner factories. ExecuteAsync invokes both directly, outside WaitAsync/deadline.ReadAsync; unlike the subsequent owner methods, a blocking factory prevents the caller cancellation or thirty-second operation wait from returning. No prerequisite caller guard bounds these invocations.', 'patch',
            'Resolve both factory results through the existing deadline before use; abandoned resolution must not execute owner operations or resume dispatch/protection work.'),
    finding('B4', '../conversations/src/Hexalith.Conversations.Server/Agents/ConversationDeletionDeliveryPump.cs:21,123', 'medium',
            'DeliverAsync and LookupAcknowledgementAsync expose optional CancellationToken.None and await admitted worker/receiver/source dependencies without a finite clock budget. ConfiguredConversationDeletionWorker adds token checks but no timeout. The actual SourcePublicationDispatcher caller does have a thirty-second wrapper, so its caller is bounded; that wrapper does not supply a standalone pump contract, and direct public pump calls can remain suspended indefinitely.', 'bad_spec',
            'Settle the standalone pump operation budget and ownership of abandoned work in the existing shared technical deadline design. Preserve source attempts, exact acknowledgement lookup, current worker authority, cancellation and no late source mutation; prove both direct entries and dispatcher composition.'),
    finding('B5', '../platform/src/Hexalith.Platform.Custody/PrivateOwnerOperationAuthenticator.cs:33-36,78-97', 'medium',
            'One accepts any non-whitespace machine claim and forwards it and expected scope to ResolveCurrentAsync before scope validation. PrivateOwnerOperationScope/Grant impose no text lengths; an independently matching long grant reaches ScopeBytes and Bytes, whose PlatformCanonicalBytes framing copies unbounded strings synchronously on the caller. Deadline checks on grant/key waits cannot bound those allocations. Strict UTF-8 framing rejects malformed text later but supplies no length bound.', 'bad_spec',
            'Specify consistent finite strict UTF-8 carrier limits and validation order for scope, machine claims, profile/key and returned grant identifiers. Validate before provider invocation/canonical allocations while retaining exact enrollment, grant and HMAC binding; do not invent production policy or capacity qualification.'),
    finding('B6', '../platform/src/Hexalith.Platform.Custody/ReplicatedSecurityObservationSpoolWorker.cs:10', 'low',
            'carried revision-2 E2: same oversized positive interval / Task.Delay maximum claim, same worker bytes through revision 5. Keep reject-low-complexity: ordinary configured intervals are seconds; the greater-than-49-day case does not justify added operational range policy. Skip claim verification and do not patch or defer again.', 'reject-low-complexity', carried_from='revision-2 E2'),
    finding('B7', '../eventstore/src/Hexalith.EventStore.Client/Streams/DirectoryAtomicAppendClient.cs:56', 'medium',
            'Capture validates all listed required identities using its existing 2048-byte strict UTF-8 bound but omits optional Command.CausationId. CommandEnvelope leaves CausationId as an unvalidated init property, so with-expressions preserve oversized or malformed values into the captured request sent to authority/owner. The atomic owner contract requires exact effect/intent validation, not an independent causation carrier bound; keyed content authentication does not itself validate this transport field.', 'patch',
            'Apply the existing ValidText guard to a non-null CausationId before copying/releasing the request. Keep null optional, ULID/non-whitespace conventions and the exact tenant-keyed content intent; add no raw command hash.'),
    finding('B8', '../eventstore/src/Hexalith.EventStore.Client/Streams/DirectoryAtomicAppendClient.cs:62', 'medium',
            'Capture creates a detached command Payload copy before the owner/authority presence check. Denial, success and failure paths never clear it; malformed later Extensions can also abandon it before ExecuteAsync enters try. Deadline.ReadAsync may leave authority/owner using that copy after caller return, and IAtomicDirectoryAppendOwner/IDirectoryAtomicAppendAuthority declare no input-buffer retention/lifetime rule. Clearing immediately on timeout could mutate a live provider input, so this is an ownership protocol omission, not a safe one-line finally.', 'bad_spec',
            'Define captured request-buffer borrowing/ownership across capture failure, completed operations and abandoned authority/owner calls. Clear discarded copies only after all users finish, retain the caller original, and preserve exact original unknown-outcome lookup without introducing plaintext persistence.'),
    finding('B9', '../eventstore/src/Hexalith.EventStore.Server/Security/RetainedIdentityHistorySourceReader.cs:153-158', 'medium',
            'Closed serialized attribution payloads are newly owned arrays appended to events. Final source/admission/custody/certificate/bound checks can return a null stream or throw caller cancellation after those arrays exist; the inner finally clears only readable.PayloadBytes. Existing final-custody-withdrawal and final-authority-cancellation tests exercise these unsuccessful paths but assert no cleanup of the accumulated closed arrays. Successful returned event arrays must remain intact.', 'patch',
            'Track accumulated owned event payloads and clear them on every unsuccessful/canceled exit, transferring ownership only on successful return. Preserve successful closed history bytes, sealed source/provider loans and the existing abandoned-unprotect cleanup.'),
    finding('B10', '../platform/src/Hexalith.Platform.Custody/Fr34ProtectionGate.cs:148', 'low',
            'carried revision-4 B10 (original revision-2 B6): same process-only canary cleanup and expired-original-grant claim, with the entire gate unchanged from revision 4 to 5. Keep reject-low-complexity: random canary material grants no readiness, and proposed durable tracking/fresh recovery authorization adds the already rejected subsystem. Skip claim verification and do not patch or defer again.', 'reject-low-complexity', carried_from='revision-4 B10, carried from revision-2 B6'),
    finding('B11', '../platform/src/Hexalith.Platform.Custody/ReplicatedSecurityObservationSpool.cs:136 and custody protocols', 'maybe-false',
            'The library catches do return content-free null/false/acknowledged counts without cause-specific counters; readiness/capacity refusal and retained pending records are observable through existing methods. The claim that operators cannot distinguish persistent failure depends on production Host telemetry and monitoring bindings, which are unqualified and absent from this source delivery. Inspect the actual accepted host signals and run safe persistent/transient failure exercises before claiming that operational harm; if demonstrated it would be medium. Do not infer a logging policy from the proposed fix.', 'defer'),
    finding('E1', '../eventstore/src/Hexalith.EventStore.Server/Security/GovernanceScopeGuardReducer.cs:208-211', 'high',
            'After guard replacement ordinal2 is committed but protection still owns blocked ordinal1, revoking the replacement key legitimately yields a protection receipt with an empty AffectedBatchIds snapshot. RecordKeyCompromise matches the pending guard key, overwrites ReplacementAwaitingActivation and clears its original protection block receipt. Coordinator activation requires that pending state/nonempty original receipt; retry ordinal2 skips activation, while ordinal3 fails the protection owner next-ordinal compare (still1). Even merely preserving pending state is insufficient when revocation occurs before dispatch: Dispatchable rejects the compromised key. A legitimate cross-owner revocation schedule can therefore permanently strand deletion recovery without a currently reachable reconciliation path.', 'bad_spec',
            'Define authenticated reconciliation of issued-but-unactivated replacements across key revocation, missing dispatch and restart. Preserve original block proof, immediate compromised-key refusal and owner ordinal correlation; reach independently proved blocked activation or a coherent permitted successor without skipping an ordinal or inventing receipts.'),
    finding('E2', '../platform/src/Hexalith.Platform.Custody/ReplicatedSecurityObservationSpoolWorker.cs:12', 'low',
            'carried revision-2 E2: the same oversized Task.Delay interval claim on unchanged worker code. Preserve the existing reject-low-complexity route independently of B6 before grouping; do not verify, patch or defer it again.', 'reject-low-complexity', carried_from='revision-2 E2'),
]
now = datetime.datetime.now(datetime.timezone.utc).isoformat()
report = dict(capturedUtc=now, reviewRevision=5, individualVerdictsBeforeGrouping=True,
              allThreeFinalReportsBeforeTriage=True, findings=rows,
              verificationLayer='No verification gaps found.', groups=[],
              status='individual verdicts recorded; grouping and loop-limit disposition pending')
(review / 'current-review-triage.json').write_text(json.dumps(report, indent=2) + '\n')
text = '## Review Triage Log — Current frozen revision 5\n\nAll three final review reports were received before individual verdicts. Root traced every new claim through the cited source, contracts and actual callers; carried claims retain their earlier verdict/route without repeat verification. No new counterexample execution is claimed.\n\n| Finding | Verdict | Proposed route | Evidence |\n| --- | --- | --- | --- |\n'
for row in rows:
    text += f"| {row['id']} | {row['verdict']} | {row['proposedRoute']} | {row['location']} — {row['evidence']} |\n"
(review / 'current-review-triage.md').write_text(text + '\nIndividual verdicts precede grouping; processing disposition is recorded separately.\n')
shutil.copyfile(spec, review / 'spec-before-revision5-triage.md')
newline = '\r\n' if b'\r\n' in before else '\n'
spec.write_bytes(before + ('\n\n' + text + '\n').replace('\n', newline).encode())
print(json.dumps({'individualFindings':len(rows),'verdicts':{v:sum(r['verdict']==v for r in rows) for v in ['high','medium','low','false','maybe-false']},'groupsRendered':0,'reviewLoopIteration':5,'specFrozenUnchanged':True}))
