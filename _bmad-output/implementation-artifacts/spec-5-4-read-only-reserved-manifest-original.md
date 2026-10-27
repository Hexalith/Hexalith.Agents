---
title: '5.4 Read-only reserved manifest original prerequisite'
type: 'feature'
created: '2026-10-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9a3a9884fe99f4d03880f7ef5da2be6a78069532'
owner_baseline_commit: 'fd327dc553169128021ee447d2a2f247fe1c33e4'
authorization: 'Execution slice of approved owner prerequisites; autonomous continuation requested on 2026-10-27.'
context:
  - '_bmad-output/planning-artifacts/external-dependency-register.md'
  - '_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-25-resumed.md'
---

<frozen-after-approval reason="execution decomposition of approved owner intent; no new product or infrastructure decision">

## Intent

**Problem:** A complete custody manifest index must authenticate its input against the protection owner's retained original. EventStore stores that request, but offers no private read-only accessor. Existing LookupAsync may call physical recovery and ConsumeAsync, making it unsuitable for verification.

**Approach:** Expose the exact retained reservation request through the existing privately authenticated owner/actor transport, with exact tenant, batch and reservation selection. This precedes manifest-index publication; it grants no destructive authority.

## Boundaries & Constraints

**Always:** Work only in root-declared `references/Hexalith.EventStore/`. Preserve complete signed request/JWS, capability, guard dispatch/issue facts and target order unchanged. Return only actual retained ConsumptionReserved or Consumed entries carrying the exact reservation receipt. Use the reserved batch's Current request (which may reflect an earlier legitimate re-attestation), never reconstruct or re-sign it. Missing authority, state, permission, changed head or mismatched identity fails closed. Apply existing whole-operation bounds, owner anchoring, before/after operation authorization and immutable owned output capture.

Preserve Branch B, 365 fixed days, host 192.168.1.30, numeric timing 300/30/600/86400/604800, accepted contracts/targets, original outcomes and every qualification gate. Reuse unaffected passing evidence. This is the next source dependency after registration-publication work, not a replacement owner commitment.

**Never:** Call RecoverAsync, any provider method, append/save/repair/enroll owner state, allocate a reservation, activate an attestation, or perform native work from this read. Do not return Unconsumed/Blocked/AlreadyDestroyedByBatch as an original reservation. Do not expose a public endpoint, Workflow/general actor capability or production registration. No siblings, nested initialization, commits, pushes, deployment, Live/Full/cohort/consuming execution, fabricated installation or availability.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Reserved original | Exact private tenant/batch/receipt | Complete unchanged request and retained receipt-bearing outcome across actor reconstruction | No writes, recovery or provider calls |
| Terminal original | Same reservation now Consumed | Same reserved request and receipt identity with retained terminal outcome | Do not fabricate an earlier outcome revision |
| Wrong/missing selection | Wrong tenant/batch/receipt or no actual reservation | No original or disclosure | Fail closed before effects |
| Authority uncertainty | Missing/revoked permission, changed head or stalled reads | Bounded unavailable/null | No recovery, mutation or provider call |
| Default/transport | Port not implemented or caller cancels | Default unavailable; private adapter preserves tenant and deadline checks | No widened capability |

</frozen-after-approval>

## Code Map

Owner paths below are under `references/Hexalith.EventStore/`.

- `src/Hexalith.EventStore.Contracts/Security/IDeletionProtectionOwner.cs`: add a fail-closed default read-only accessor and a separate public result record. Preserve existing LookupAsync recovery semantics for existing consumers.
- `src/Hexalith.EventStore.Server/Security/IDeletionConsumptionActor.cs`, `DeletionConsumptionActor.cs`: add bounded private read entry. Existing ReadBlockedReplacementEntryAsync demonstrates current anchored snapshot/recheck; ensure selected read path performs no reconciliation writes. Existing LookupAsyncCoreAsync invokes RecoverAsync and must not be reused.
- `DeletionConsumptionBatch.cs`: Original and Current retain complete requests; Outcome retains reservation ReceiptId after consumption. Do not invent an original reservation revision that is not separately stored.
- `DaprDeletionProtectionOwner.cs`: reuse exact-tenant dedicated actor transport and AuthoritativeStreamReadDeadline, without public DI activation.
- `DeletionConsumptionIdentity.cs`: reuse owned request capture/validation, unchanged order and signed bytes.
- `tests/Hexalith.EventStore.Server.Tests/Security/DeletionConsumptionFixture.cs` and `DeletionConsumptionActorTests.cs`: reuse durable fixture and independent owner authority, adding focused read tests in their own file.

## Tasks & Acceptance

- [x] Owner Contracts record/interface: expose nullable exact reservation-original read; omitted implementations remain unavailable.
- [x] Owner Server actor/transport: implement exact selection with current private authorization and non-recovering anchored reads; no provider or durable mutation.
- [x] Focused tests: assert persisted ledger unchanged across actor reconstruction, complete reserved and consumed originals, actual re-attested Current selection, wrong receipt/tenant/missing authority, output ownership, whole-call bound and adapter cancellation/default refusal. Configure a provider that would consume on ordinary lookup recovery and prove this accessor never touches it.
- [x] Owner delivery note: exact source/commands/results and explicit missing installed acceptance/qualification; no manifest publisher yet.

Acceptance: Given a genuine retained reservation, when an independently authorized verifier reads its original, then it obtains the unchanged complete request without starting physical work. Given uncertain state/authority or an unreserved batch, when queried, then it returns no usable original and makes no durable change.

## Implementation Notes

Parent owns the rendered bmad-build workflow/review. This child executes only the approved source handoff after the preceding unit's correction/verification stage. Read EventStore AGENTS.md and root Hexalith instructions before editing. Preserve unrelated edits and package locks. Normal Debug source builds use root-declared Builds/Commons/serialization dependencies and isolated artifact paths; no nested submodules. The prior Platform Aspire baseline timed out and showed no running AppHost; do not replace it with installed services or claim readiness.

Handoff: the additive Contracts record/optional port, actor and private adapter are implemented. The actor bypasses the reconciling ReadAsync helper and refuses pending transitions. Normal Debug build passed with zero warnings/errors, with no suppressions. All 246 selected cases passed: 29 new read-only tests, 58 actor, 19 owner-state authority, 42 consumption authority and 98 replacement-authority cases. No package locks existed in the affected graph and none were created. The [owner delivery note](../../references/Hexalith.EventStore/docs/implementation/reserved-manifest-original-delivery-2026-10-27.md) and its adjacent linked evidence identify exact commands, source/binaries and limits. Independent review is pending; installed qualification and manifest publication are not delivered by this slice.

## Spec Change Log

## Review Triage Log

All three independent layers reviewed `/tmp/story54-reserved-original-review-jenytgxb.diff`; the edge-case layer returned no findings. Individual verdicts below precede grouping.

| ID | Verdict / route | Verified evidence |
| --- | --- | --- |
| B1 | medium / patch | The new adapter uses ReadAsync, which bounds waiting but does not cap abandoned noncooperative RPCs. The existing ReadWithAdmissionAsync holds one of sixteen shared slots until actual completion. Reuse it and verify saturation, abandonment and eventual release without starting further calls. |
| B2 | false / reject | The delivery claims a thirty-second actor entry-method budget and a separately bounded private adapter, not bounded completion of Dapr lifecycle cleanup. Existing actor tests cover post-method/failure cleanup waiting for unfinished state I/O. That deliberate quiescence guard must remain; the adapter bounds the external caller. |
| B3 | medium / patch | Raw runner logs are ignored and absent from the source diff. Preserve the complete local packet in the parent archive and add compact machine-readable runner summaries/hashes, explicitly identifying logs as local scratch. Respect EventStore's existing evidence/committed-file policy; no ignore/CI gate weakening or fabricated CI result. |
| B4 | medium / patch | Binary provenance lacks dependent source revisions/worktree identities and SDK identity. Record a bounded post-verification dependency/toolchain snapshot and its timing limits; do not invent a pre-build invariance attestation. |
| B5 | false / reject | PublishedTrustDeletionConsumptionAuthority.AuthorizeOperationAsync is a generic tenant-owner delegate, with no operation-name whitelist added by this change. The new actor tests explicitly deny this operation before/after reads. Installed permission issuance remains an explicit missing installation; a test repeating the literal through another stub would not qualify it or demonstrate a bypass. |
| B6 | medium / patch | The new transport DTO has no executed serializer round-trip. Verify the actual transport serializer can retain complete nested request/JWS/order and reserved/consumed outcomes, with independently owned deserialized vectors. No live transport qualification is inferred. |
| B7 | low / patch | Selection/currentness refusal returns null, while dependency/capture faults and adapter cancellation propagate under existing private-port conventions. Clarify that distinction in public documentation and cover fault/corrupt-state paths with no-effects assertions; do not introduce a new catch-all policy. |
| B8 | medium / patch | Positive tests always provide a physical backend even though the accessor intentionally has no dependency on it. Execute reserved/consumed success with provider null and assert unchanged ledger and no transitions. |
| V1 | medium / patch | Pre-verified exact-batch regression gap: the current missing-batch test has only an Unconsumed entry, so receipt-only selection would pass. Pair a valid reservation receipt with another batch through actor and adapter, proving refusal without effects while the matching selection succeeds. |

Patch groups: B1; B3/B4 evidence identity; B6; B7; B8; V1. No intent, retention, timing or owner-acceptance change is needed. Original logs and reviewed diff are preserved under the parent's `tests/owner-prerequisite-2026-10-27/reserved-original-before-review-patches.tar.gz` before correction.

All six patch groups are corrected and root-verified. The default Dapr provider's actual DataContract serializer first rejected the new record and then its owned read-only carriers. DataContract/DataMember metadata on the new result and five existing nested record types, with two KnownType annotations confined to the new result, resolve the observed failures. No public members or transport defaults changed. Reserved/consumed complete JSON fixtures are byte-identical to preserved, initially verified binaries.

Root executed the 40-case focused class plus all four prior regression classes: **257/257 passed**, zero errors/failures/skips/unrun. All 12 recorded source hashes remained exact; executed binary hashes match the corrected handoff. Current Contracts/Server normal source builds pass. The full test-project source graph became blocked by concurrent Tenants routing references (10 CS0234/CS0246 errors); the focused test compilation therefore reused retained dependency assets with BuildProjectReferences=false, without warning/analyzer suppression. This limitation remains explicit and no current full-graph pass is claimed.

The [parent verification receipt](../../references/Hexalith.EventStore/_bmad-output/implementation-artifacts/tests/reserved-manifest-original-2026-10-27/parent-final-verification.json) and root `tests/owner-prerequisite-2026-10-27/reserved-original-final-evidence.tar.gz` retain complete local evidence, including ignored raw logs. The EventStore ignore/CI policy is unchanged. Provenance is explicitly a post-verification snapshot, with no invented CI execution or pre-build invariance. Concurrent canonical owner commits occurred outside this build; this build created no commit and preserved their work.

Done denotes this private source accessor, required serializer compatibility and reviewed local behavior. Manifest publication, installed permission/trust, complete accepted owner deliveries, physical proof and qualification remain outstanding. Parent and original Story 5.4 remain in-progress, all accepted records and gates unchanged. No cohort or consuming path ran. The explicit approved no-commit/no-push constraint overrides the workflow's generic commit step.

## Verification

Build affected owning test projects individually with Debug/source references. Execute built xUnit assemblies with focused single-dash class/method selectors, retain commands/exit/results and source hashes, and audit the matrix against actual executions. Record normal-gate blockers separately from diagnostic evidence; do not suppress gates persistently. No qualification or physical effect.
