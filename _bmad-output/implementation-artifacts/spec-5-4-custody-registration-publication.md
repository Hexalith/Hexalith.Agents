---
title: '5.4 Custody registration original publication prerequisite'
type: 'feature'
created: '2026-10-27'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '9a3a9884fe99f4d03880f7ef5da2be6a78069532'
owner_baseline_commit: 'b117945e2ba7911fd2c47843f866487341300398'
authorization: 'Execution slice of the approved owner-prerequisite work; user requested autonomous continuation on 2026-10-27.'
context:
  - '_bmad-output/planning-artifacts/external-dependency-register.md'
  - '_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-25-resumed.md'
  - 'references/Hexalith.Platform/docs/implementation/story-5-4-coupled-delivery-2026-10-25/installation-decision.md'
---

<frozen-after-approval reason="execution slice of existing approved intent; no new product or infrastructure decision">

## Intent

**Problem:** The protection/custody chain needs retained full-identity registration originals. Platform supplies an authenticated locator, but no production writer populates its registration index. A newly wrapped registration can therefore become undiscoverable outside the component.

**Approach:** Publish the exact verified retained wrap original into the existing independent authority and recover publication after uncertainty without wrapping again. Implement this earliest actionable source prerequisite in the owning root-declared Platform submodule; keep the broader owner deliveries open.

## Boundaries & Constraints

**Always:** Preserve complete registration identity, original operation, actor revision, KEK version, opaque reference and receipt. Source publication from the component's durable verified original, authenticate the restricted tenant-scoped publisher, and reuse independent enrollment/currentness/revocation, conditional state writes and signed exact lookup. A locator is a hint; every subsequent custody use still authenticates the actual original. Uncertain acknowledgement permits exact publication/lookup recovery only. Keep root/decryption keys out of publication, logs and evidence. Use the existing bounded retention/capacity model, without eviction. Keep one C# type per new file.

Preserve existing edits, Branch B, 365 fixed days of history, host 192.168.1.30, timing 300/30/600/86400/604800, all qualification/physical gates, the OpenBao ObservedRevival evidence and AWS exclusion. Reuse passing evidence outside affected files. The independent authority's actual installation remains required; no application-local or fixture fallback.

**Never:** Edit sibling checkouts, initialize nested submodules, commit, push, deploy, invent credentials/trust/genesis, run native key work, Live/Full/cohort qualification or consuming seams. Do not alter accepted dependency records or claim complete delivery/availability. Do not implement the separate manifest publisher from reconstructed or singleton facts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| First publication | Authenticated exact durable wrap original | Persist unchanged registration, then locate by complete identity after receiver restart | Missing original or authority denies |
| Duplicate/loss | Existing or uncertain publication | Exact original returned by recovery; zero additional native wraps | Changed original conflicts |
| Scope/authority | Wrong tenant, revoked publisher, expired current state | No index mutation or disclosure | Deny before publication |
| Substitution | Changed identity, operation, revision, KEK, reference or receipt | Original cannot be replaced or inherited | Fail closed |
| Capacity/cancellation | Full head, CAS loss, outage or cancelled request | No fabricated success, original remains recoverable | Bounded refusal/unknown; preserve cancellation |

</frozen-after-approval>

## Code Map

All owner paths below resolve beneath `references/Hexalith.Platform/`.

- `infrastructure/Hexalith.Platform.Custody.Dapr/KeyExchange.cs`: retains WrappedResult and checkpoint original before returning; RecoverWrapAsync restores that exact result without native resend.
- `infrastructure/Hexalith.Platform.Custody.Dapr/SemanticProvider.cs`: lookup-original validates the entire registration and immutable response; it performs no native operation. Reuse this verification rather than trusting submitted index facts.
- `src/Hexalith.Platform.Secrets.Runtime/IndependentAuthorityReceiver.cs` and `IndependentAuthorityState.cs`: reuse mounted channel/caller authentication, surviving enrolled head, expiry, ETag CAS, head bounds, signed nonce-bound replies and existing Registrations dictionary. Current registration method is read-only.
- `src/Hexalith.Platform.Secrets.Runtime/IndependentDaprAuthority.cs` and `DaprCustodyOriginalLocators.cs`: existing bounded authenticated transport and complete-identity locator.
- `src/Hexalith.Platform.Custody/CustodyKeyRegistration.cs`: preserve existing public registration fields unchanged.
- `infrastructure/Hexalith.Platform.Custody.Dapr/Program.cs`: explicit composition only; add required optional installed publisher/verifier wiring without activating an installation or introducing defaults.

## Tasks & Acceptance

- [x] `references/Hexalith.Platform/src/Hexalith.Platform.Secrets.Runtime/`: implement authenticated durable registration publication/lookup using the existing independent owner state and actual original verification; no arbitrary caller-supplied index admission.
- [x] `references/Hexalith.Platform/infrastructure/Hexalith.Platform.Custody.Dapr/`: connect component-side exact-original publication and recovery. A failed publication must remain recoverable from retained component state even when no caller received a registration; no native resend.
- [x] Owner Runtime/component tests: execute the matrix, assert persisted state across receiver reconstruction, and prove lost-ack/recovery causes zero extra native wraps.
- [x] Owner delivery note: record exact source/commands/results, reused evidence, unapplied installation bindings and remaining physical/resource blockers. Keep parent and Story 5.4 in-progress with incomplete full tasks open.

Acceptance: Given an installed authenticated authority and retained wrap original, when publication or its acknowledgement is lost, then a restarted component/receiver recovers that same registration without reallocating or rewrapping. Given missing installation or physical proof, when reporting local completion, then no owner acceptance, qualification or availability is inferred.

## Implementation Notes

This decomposes the existing approved owner-prerequisite scope; it does not approve a backend or replace the full story. The user is considering non-AWS backend choices while source work continues. Root has already inspected the approved story/register/latest reconciliation and preserved the pre-existing 2026-10-27 Agents verification. Run affected Debug source builds only, using root-declared EventStore/Builds paths and isolated artifacts; do not rerun the full Agents matrix. Before implementation read the repository instructions under references/Hexalith.AI.Tools. Preserve generated package-lock bytes unless a deliberate owning change requires them.

The parent owns the active rendered bmad-build workflow and its review. The implementation child executes this handoff. Parent baseline attempt in root-declared Platform: `timeout 45s aspire start --non-interactive --isolated --apphost apphost.cs --format Json` exited 124 during startup and stopped; subsequent exact-target describe exited 0 with no AppHost running. No resource-readiness baseline was established. Preserve that environment limitation separately from focused source verification; do not start installed custody services to replace it. See the root preservation receipt at `tests/owner-prerequisite-2026-10-27/preservation-before.json`.

Implementation handoff: authenticated publication, durable component recovery and focused coverage are present in the owner source. Diagnostic executions passed 33 Runtime tests and nine component scenarios. Normal Runtime/component builds failed on unchanged CustodyOutsideWitness.TryRepromote CA1822; a focused analyzer-enabled Runtime build reported 106 errors in existing files, none on the new registration files. These failures remain visible and no suppression is persisted. Package locks are unchanged. The [owner delivery note](../../references/Hexalith.Platform/docs/implementation/custody-registration-publication-2026-10-27/README.md) maps all matrix rows to executed coverage and retains exact command/source/artifact evidence. This review does not establish installed qualification or complete owner acceptance.

## Spec Change Log

## Review Triage Log

All three independent layers reviewed `/tmp/story54-registration-final-review-n4euuukc.diff`; edge-case review returned no findings. Individual verdicts precede grouping below. Normal analyzer failures remain a separate unpassed gate.

| ID | Verdict / route | Verified evidence |
| --- | --- | --- |
| B1 | high / patch | The real authorizer prepare path writes qualification-begin-original. If that original authority shares the receiving head, every verification changes the ETag captured before verification and publication always loses CAS. Re-read the enrolled current head and reauthenticate after verification before constructing the publication update; test the actual composed boundary and preserve revocation refusal. |
| B2 | medium / patch | New publication awaits inside the wrap try/finally, retaining owned root and KEK arrays until a remote value-free call finishes. Clear them before that call and observe zeroed arrays while publication is stalled. |
| B3 | medium / patch | The new missing-publisher check follows PutImmutableAsync for admission. Rejected unconfigured wraps therefore create unreachable immutable objects without advancing capacity state. Move the existing check ahead of immutable writes and assert no objects or native effects. |
| B4 | false / reject | The cited SendCommitted delivery state is unchanged from the pre-existing recovery implementation. CompleteAsync explicitly closes that delivery once the recovered native intent is ResultDurable; the claimed disagreement does not establish a blocked or incorrect consumer outcome. This change must not silently expand into unrelated phase redesign. |
| B5 | medium / patch | Seven locally copied logs match the repository log-ignore rule and are absent from the retained source diff. Add a narrowly scoped retention exception and verify their existing hashes; no staging is authorized. |
| B6 | low / reject | A present malformed publisher binding fails closed in the existing IndependentDaprAuthority constructor during explicit service construction. Check-profile does not claim installed readiness or qualification. Additional duplicated validation guards are not justified for this cosmetic dry-check difference in this slice. |
| B7 | medium / patch | The newly accepted Closed recovery branch has no executed test; existing tests reach only SendCommitted/ResultDurable. Exercise actual holder completion, reconstruction and worker recovery, preserving Closed and native counts. |
| B8 | medium / patch | The current oversized-head case exceeds the absolute limit and cannot detect loss of completion-reserve subtraction. Exercise persisted heads on both sides of the admission ceiling while below the absolute ceiling, including duplicate recovery. |
| B9 | low / reject | Complete source verification already rejects the changed original in the current single enrolled component; separate positive-verifier collision assertions would mirror defensive dictionary conditions without demonstrating an accepted-source bypass. Existing identity conflict protection remains unchanged. |
| B10 | medium / patch | Current cancellation tests cancel before a remote commit. Add cancellation after durable publication but before acknowledgement and prove exact restart recovery, cleared buffers and no extra wrap; preserve caller cancellation. |
| V1 | medium / patch | Pre-verified reserve-boundary regression gap; removing the registration admission subtraction leaves the current over-32-MiB test passing. Same underlying gap as B8. |
| V2 | medium / patch | Pre-verified Closed recovery gap; removing the new Closed alternative leaves current tests passing. Same underlying gap as B7. |

Grouped patch roots: B1; B2; B3; B5; B7/V2; B8/V1; B10. No intent or accepted contract changes are needed. No parent review counter or dependency status changes.

All seven grouped corrections are implemented and root-verified. The composed actual publisher/receiver/verifier/authorizer case proves same-head prepare advancement, post-prepare revocation refusal, post-commit cancellation and exact restart recovery. R1: tracing that integration also found two clock reads in the authorizer's 30-second statement construction; one captured timestamp now preserves the exact bound, with a real-clock assertion. The initial refusal's timestamp was not measured, so no exclusive causal attribution is claimed.

Root's final diagnostic executions passed 35/35 Runtime tests and the entire 27/27 registered component suite, with zero failures/skips/unrun. All 23 recorded source hashes and executed Runtime dependency copies matched, package locks stayed unchanged, and four pre-existing user artifacts/register/story bytes remained exact. The [root verification receipt](../../references/Hexalith.Platform/docs/implementation/custody-registration-publication-2026-10-27/evidence/parent-final-verification.json) retains commands, exits and hashes; the initial reviewed candidate is archived separately under `tests/owner-prerequisite-2026-10-27/`.

Done denotes this reviewed source slice and diagnostic behavior only. The normal analyzer gate remains FAILED in unchanged code; no release/installed/physical qualification, complete owner commitment or availability is established. Parent and Story 5.4 remain in-progress. No commit is made because the approved owner scope explicitly forbids commits/pushes, overriding the workflow's generic commit step. Continue with the [read-only reservation-original prerequisite](spec-5-4-read-only-reserved-manifest-original.md).

## Verification

Build the affected Runtime/component test projects individually in Debug with explicit root-declared source references and external artifact paths. Execute built assemblies directly, using focused xUnit classes or the component runner's explicit registered scenario names. Retain exact commands, exit codes and test counts. Inspect the owner diff including new files and audit each matrix row against executed coverage. Reuse unaffected passing evidence; no Live/Full/native effect or physical experiment.
