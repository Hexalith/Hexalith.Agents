---
title: 'Resolve five deferred state and dependency items'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'f90e43f2e048f3b9ef77e7445eaa51544b76536d'
context:
  - /home/administrator/projects/hexalith/agents/AGENTS.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Pending browser commands can overwrite each other across store instances, historical spool snapshots can fail digest verification, and three deferred dependency questions lack fresh evidence.

**Approach:** Complete the requested five-item batch: browser concurrency; historical spool compatibility; EventStore source/package reconciliation; the historical five-pin counterfactual; and the Platform source dependency tuple. Independently review and verify, then commit and push each owning repository on main.

## Boundaries & Constraints

**Always:** Preserve authentication-generation fences, corruption refusal, exact command locks and safe browser payloads. Preserve anchored spool integrity and both historical/current serialized representations. Use producer-selected source mappings and Builds package authority. Retain exact revisions, commands, logs and native test results. Use `run-with-temp` for disposable work; keep evidence outside its scratch directory. Close each ledger item only when its own evidence supports closure.

**Never:** Clear ambiguous commands, accept arbitrary alternate digests, weaken audits or tests, infer pin necessity from restore failures, replace owner-selected sources with convenient umbrella revisions, initialize nested submodules, change Builds-owned policy, deploy, publish packages or promote live owner qualification.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Required outcome |
| --- | --- | --- |
| Overlapping stores | Concurrent additions, acknowledgement/removal, or conflicting identities | Preserve unrelated locks; reject conflicting replacement; retry only against freshly validated state |
| Interrupted browser work | Authentication change, corruption, cancellation or storage denial | Existing scoped refusal; no redirected write or empty-state fallback |
| Retained spool | Historical head, archive or pending transition | Preserve original digest through restart/recovery; promote encoding only with a real reservation transition |
| Invalid spool | Tampering, partial/duplicate reservation members | Refuse without mutation; preserve current-format defaults |
| Dependency comparisons | Current source/packages, reviewed versus old pins, exact Platform owners | Record separate reproducible outcomes; incomplete execution remains blocked |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.UI/State/BrowserSessionPendingProviderCommandStore.cs`: instance-local semaphore surrounds separate browser calls; reuse validation and scope guards.
- `test/Hexalith.Agents.UI.Tests/PendingProviderCommandStoreTests.cs` and `InMemorySessionStorageJsRuntime.cs`: reuse suspended interop and corruption controls.
- `references/Hexalith.Platform/src/Hexalith.Platform.Custody/SecuritySpoolSnapshot.cs`: reservation fields change reserialized bytes; generic `RecoverableAnchoredState` also hashes that serialization.
- Platform `ReplicatedSecurityObservationSpool.cs` and `tests/Hexalith.Platform.Custody.Tests/ReplicatedSecurityObservationSpoolTests.cs`: cover head/archive/recovery paths; existing legacy fixture constructs the current shape.
- `src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj`, `eng/verify-story-5.2.ps1`: integration entry and existing floor/gateway checks.
- Platform `Directory.Build.targets`, `eng/SourceRestore.props` and Composition.Tests: existing source propagation and original related selections.
- [Investigation notes](five-deferred-state-and-dependency-investigation-2026-11-16.md): canonical comparison revisions, exact owners and original failing cases.

## Tasks & Acceptance

**Execution:**
- [x] Browser store above and `src/Hexalith.Agents.UI/wwwroot/pendingProviderCommands.js` — coordinate mutations in the browser storage scope, with validated optimistic retries and existing authentication guards — prevent cross-instance lost updates.
- [x] UI test files above and `tests/tooling/pending_provider_storage_test.mjs` — deterministically interleave distinct runtimes/stores, additions, removals, conflicts, cancellation and account transitions; execute the actual JavaScript boundary — verify persisted contents.
- [x] Platform snapshot above and new `SecuritySpoolSnapshotJsonConverter.cs` — preserve historical/current layout identity through deserialization, copies and serialization — keep anchored and pending-transition hashes valid.
- [x] Platform spool tests above — retain provenance-backed historical fixtures; cover restart/drain, archive chains, pending recovery, rollover, current defaults and malformed/tampered refusal — prove compatibility without weakening integrity.
- [x] New `scripts/verify-deferred-dependency-cohort.py` — export exact revisions into disposable trees; compare the selected EventStore graph and run source/package integration; rebuild the historical integration with only the five pins varied; run exact Platform source owners and original related cases — settle three separate questions.
- [x] `CONTRIBUTING.md`, `_bmad-output/implementation-artifacts/deferred-work.md` and new `tests/deferred-state-dependencies-2026-11-16/` under implementation artifacts — document reproduction and retain hashed evidence; reconcile exactly five selected entries after verification — make closure auditable.

**Acceptance Criteria:**
- Given two stores sharing browser storage, when their mutations overlap, then both valid independent locks survive and a conflicting identity cannot replace either lock.
- Given genuine historical or current spool bytes, when the store restarts, drains or recovers a pending transition, then authenticated state and archive links remain verifiable; malformed or tampered state refuses.
- Given the recorded comparison revisions and owner mappings, when all three dependency experiments run, then native results answer each deferred question independently and preserve unrelated broad-gate failures.

## Implementation Notes

- The user approved this spec and continued implementation on 2026-11-16. The frozen intent is unchanged.
- The implementation handoff performs local changes and verification only. The coordinator handles staging, independent review, owner checks, commits and remote operations after that handoff. Do not stage, commit or push from the implementation handoff.

## Spec Change Log

- 2026-11-16: Completing the exact Platform experiment exposed an unconditional Tooling SDK-removal edge and an equal-sized foreign adapter inventory diagnostic. Conditional edge metadata and full route-ID inventory comparison resolve the actual gate failures. Three explicit local Platform inputs are retained over the immutable baseline; exact dependency pins and every producer root remain unchanged.

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH-1 | medium — The new Web Lock wait can outlive canceled interop or a scope refusal and later remove the captured lock. Avoid queued browser mutations; retain the existing documented ambiguity of an already delivered synchronous write. | patch |
| BH-2 | medium — The six cited small `.log` files are present locally but ignored by Git and have no staged compressed counterparts. Preserve and stage their exact bytes with compression bindings. | patch |
| BH-3 | low — Coordinator status and fixture-attribute edits changed two non-runtime inputs after the implementation hash manifest. Retain the original tested manifest and bind final files separately after corrections. | patch |
| BH-4 | medium — The documented state reproduction command overwrites the original evidence directory. Allocate a fresh durable output directory and bind its own results. | patch |
| BH-5 | medium — Native validation accepts a minimum count rather than all original case identities; its substring method check also accepts a different method containing the required name. Require the original display/type/method identities and exact method equality, while permitting additional cases. | patch |
| BH-6 | medium — Only the assembly basename is checked, so a foreign full path passes. Compare normalized owning paths and record the executed assembly fingerprint. | patch |
| BH-7 | medium — The claimed static input inventory omits compiler/analyzer configuration inputs used by these projects. Capture the evaluated analyzer/additional/configuration inputs and applicable editor configuration; keep dynamic-helper qualification separately governed. | patch |
| BH-8 | medium — Recursive asset discovery includes eleven archived proof projects and stale source assets in the package lane. Traverse only the configuration's evaluated project-reference graph. | patch |
| BH-9 | medium — Existing experiment directories overwrite runner bytes and absorb stale files. Reject reused evidence destinations before writing. | patch |
| BH-10 | medium — Timeout kills only the immediate process, allowing descendant verification work to survive scratch cleanup. Terminate and reap the owned process group and prove descendant termination. | patch |
| EC-1 | medium — Same queued-mutation defect as BH-1; the shipped module's delayed callback can run after a cancellation/refusal. | patch with BH-1 |
| EC-2 | medium — A valid legacy head near the 32 MiB bound gains reservation fields before rollover and exceeds the unchanged pending-state limit. Keep the legacy reservation representation bounded, using the existing authenticated revision/phase relationship, without relaxing size or integrity gates. | patch |
| Coordinator package check | The new browser module causes Razor to emit static-asset registration targets. Enroll only the four exact SDK templates, one module and its fully bound manifest; preserve rejection of arbitrary build code. All six real packages and seven isolated consumers pass. | patched |
| Package follow-up review | A coordinated but incorrect fingerprint could satisfy the manifest's internal references. Require the installed SDK's exact SHA-256/base36 fingerprint algorithm; independently inspected SDK vectors and a coordinated-mutation negative regression pass. | patched |
| Coordinator provenance | Record only allowlisted producer environment values, preventing unrelated environment credentials from entering evidence. The final 21 runner controls include this regression. | patched |

The verification-gap layer reported no additional verification gaps. All three independent layers completed before triage.

## Implementation Handoff — 2026-11-16

The initial handoff passed 1,352 UI and 803 Custody cases. The coordinator's results below supersede those pre-review runs. Current EventStore integration passes both source/package builds, the maintained package floor and 142 native gateway cases per lane. Expanded compiler-input capture found one reviewed EventStore `.editorconfig` difference: only non-C# LF save rules differ, while C# rules and the other consumed inputs match. Both historical five-pin lanes pass 42 gateway cases, proving the previous pins sufficient for that integration.

The coordinator retrieved exact Works `b2985d22b3bfbc6d86bcde28a5da722ae7f930c3`. The completed Platform run passes default and mapped source builds, all eight original related cases in both modes, all 38 producer-mapped catalog cases, and 29 source-propagation controls. Three explicit local corrections are bound with baseline/corrected hashes; the original baseline HXW006 and intermediate native refusal remain immutable. All 18 producer entries and owner dependency pins are preserved. All five selected ledger entries close locally; no live qualification is claimed.

[Durable verification and reproduction](tests/deferred-state-dependencies-2026-11-16/README.md) records matrix coverage, native results, initial failed harness attempts, exact selections, explicit local inputs, and source/mapping/evidence hashes. No staging, commits, pushes, remote operations or independent review ran in this handoff.

## Verification

Run each affected test project individually, using Debug/source and necessary Release/package compatibility lanes. Invoke built xUnit v3 assemblies with native class/method filters and reject failures, skips and zero discovery. Run the actual browser module tests and focused maintained gateway/floor checks; retain source/mapping/output hashes. Follow the repository fallback ladder for broad blockers. Review independently and triage every finding. Before each push, require the owning Builds `check.py`, validate Conventional Commits, inspect all root submodules and remote divergence, push submodule content first, and separately record only remotely available gitlinks. Verify main's remote SHA after delivery.

## Coordinator completion and delivery

All review findings are patched and verified; no implementation findings are deferred. Final full UI and Custody runs pass 1,352 and 806 cases respectively, with warning-free builds and no failures or skips. Custody's serialized rerun passes after one unchanged signing-actor deadline case failed during concurrent verification; both results are retained. The actual browser module passes five cases. A fresh dependency replay passes both 142-case current lanes, both 42-case historical lanes, the eight default and eight mapped Platform cases, all 38 catalog cases and 29 propagation controls. All 18 producer-selected entries remain intact.

Broad tooling discovery passes 333 of 335 cases and skips two real-runner cases because the root Domain assembly is absent. Both unchanged cases subsequently pass against a genuinely built disposable Domain assembly. The final focused runner, UI package/consumer and existing package-validation suites pass 21, 11 and 20 cases. Exact SDK fingerprint vectors cover the final package guard tightening after the six-package/seven-consumer run.

Platform's current main was fast-forwarded before the local Conventional Commit `38d6c81fc7d8058bb93414a96993986e96da1058`; the tested product inputs are unchanged by that fast-forward. Delivery is blocked: the mandatory `python3 references/Hexalith.Builds/check.py` fails because both Agents and Platform lack CiCd-owned `.github/hexalith-ci.json`. Clean baseline and final Platform-commit checks retain the same failure. No push occurred, and the parent commit/gitlink update waits for successful child publication. CI onboarding is outside the approved five-item implementation and its policy boundaries.
