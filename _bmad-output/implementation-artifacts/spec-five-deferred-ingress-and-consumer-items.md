---
title: 'Five deferred ingress and consumer items'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '805b35da01efe4d3ac5f1402e611950ac282d0a1'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Deferred work leaves an intake scope check missing, inconsistent provider JSON interpretation, two unresolved assumptions about provider ingress authority, and incomplete qualification of executable consumer dependencies.

**Approach:** Address five existing ledger records as the requested batch: established intake directory scope; provider identity casing ambiguity; raw provider timestamp authorization; platform provider-query authentication; dynamically loaded consumer helpers/tasks. Implement demonstrated defects, verify existing safeguards through behavioral regressions, independently review, and push verified changes on main. Timestamp and query reconciliation establish local boundary behavior; DW-21 deployed composition remains a separate qualification requirement.

## Boundaries & Constraints

**Always:** Preserve exact authenticated intent, tenant isolation, current independent actor/role authority, cancellation, bounded recovery, reserved-extension admission, historical event compatibility and package/version authority. Use `run-with-temp` for disposable repositories, feeds and outputs; save concise useful evidence outside scratch. Inspect every root-declared submodule before delivery, following repository Git instructions.

**Never:** Count aliases or previously completed items toward five; invent host credentials, migrations, clocks or owner acceptance; treat workload roles as human authority; suppress audits; weaken executable-asset policy; deploy, publish packages or initialize nested submodules. Keep broader host, retained-history, migration and runtime qualifications open.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected behavior | Failure handling |
| --- | --- | --- | --- |
| Foreign directory | Established revision with wrong tenant or Conversation | Intake/recovery return NotFound before Closing disclosure or revision use | No commands or scheduling |
| Empty directory | Null or revision-zero rejection-only fold | Existing exact permit recovery remains valid | Preserve contention and Closing behavior |
| Ambiguous provider JSON | Repeated identity/enablement keys with equivalent or mixed casing | Scope, validation and retry digest cannot interpret different intents | Refuse before effects |
| Raw governance time | Backdated/future-dated payload with forged or mismatched authority | Actual gateway refuses unadmitted reserved authority | No accepted governance event |
| Verified provider query | Exact originating caller plus current global-administrators evidence | List/detail use the verified human despite workload transport identity | Forged/stale/missing authority reads no protected model |
| Consumer helper | Analyzer/task loads package-owned executable dependencies | Receipt and reviewed policy bind dependency ownership and bytes | Missing observation, unowned loads or changed helpers refuse |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.Server/Application/AgentInteractions/AutomaticInteractionIntakeOrchestrator.cs`: mirror recovery's established-scope guard before inspecting DeletionRequestId/Revision. `InteractionOutboxRecoveryOrchestrator.cs` supplies existing semantics.
- `src/Hexalith.Agents.EventStore/AgentsProviderCatalogCoordinationPolicy.cs`: ReadIdentity/ReadProperty prefer exact/camel casing while Deserialize is case-insensitive. `ProviderGovernanceCommandIntent.cs` normalizes typed retry intent; keep all six provider commands consistent.
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs`: requires one exact Dapr identity/app and proof-reader admission of the exact request. `IAgentsTrustedCommandProofReader.cs` forbids creating admission during verification.
- `src/Hexalith.Agents.Server/Application/Queries/ProviderCatalogQueryHandlerBase.cs` and `Ports/HttpAgentAdministrationContextProvider.cs`: provider platform authorization uses originating subject and current global authority. SDK DomainQueryDispatcher installs VerifiedQueryCaller; Program delegates host security to SDK registration.
- `scripts/validate-consumer-package-references.py`: current receipt fingerprints selected entry analyzers/imports, omitting dynamic helpers. `eng/consumer-executable-assets.json` binds union and individual consumer selections.
- `test/Hexalith.Agents.Server.Tests/AutomaticInteractionIntakeRecoveryTests.cs`, `ProviderCatalogCoordinationIntegrationTests.cs`, `AgentsEventStoreGatewayIntegrationTests.cs`, `VerifiedSetupQueryCallerTests.cs`: reuse Handle/Apply, gateway and real dispatcher fixtures.
- `tests/tooling/dependency_assets_restore_test.py`, `ui_consumer_assets_test.py`: reuse isolated package restore, receipt and mutation controls.

## Tasks & Acceptance

**Execution:**
- [x] Intake and intake-recovery tests above — reject nonzero foreign directory scope before other use; cover both entry points, foreign tenant/Conversation, established revisions and open/Closing states, with revision-zero controls.
- [x] Coordination/intent files and coordination tests above — unify interpretation and reject duplicate case-insensitive identity/enablement keys; cover key order, casing and canonical controls across the six commands and all policy entry points.
- [x] Gateway integration tests above — exercise raw timestamp submissions through real authority admission, including missing/forged flags, wrong app, exact-proof mismatch and valid server-stamped controls. Assert aggregate/persisted outcomes; patch only demonstrated boundary defects without changing trusted-time contracts.
- [x] Add `test/Hexalith.Agents.Server.Tests/VerifiedProviderQueryCallerTests.cs` — adapt real SDK dispatcher fixtures for list/detail and current global authority; cover forged subject/tenant/authority, stripped binding, workload-role spoof, no-read refusals and context cleanup. Verify SDK security registration without replacing authentication with a permissive fixture.
- [x] Consumer verifier, policy and tooling tests above — observe actual analyzer dependency and task/helper loads during fresh isolated builds; keep package, SDK and generated provenance distinct. Add a real executable helper fixture whose bytes can change without changing its entry assembly/import; prove policy refusal. Qualify all six current packages individually and together; independently review every baseline change.
- [x] `CONTRIBUTING.md`, `deferred-work.md` and a dated evidence report — document extended receipt behavior and reconcile exactly the five selected records to proven scope. Preserve original evidence and explicitly retain installed-host qualification.

**Acceptance Criteria:**
- Given valid existing callers and consumers, when the five changes run, then normal recovery, provider operations and all seven consumer builds remain successful.
- Given adversarial boundary inputs, when processed, then refusal precedes unauthorized durable effects or disclosure and native regressions prove the boundary.
- Given only local source/package evidence, when updating the ledger, then no production availability, owner acceptance or live qualification is inferred.

## Implementation Notes

- User approved the complete five-item spec and continuation on 2026-11-16. The frozen intent is unchanged. Implementation and all child verification jobs have completed; the parent retains independent review and Git delivery.
- Intake now refuses established foreign scope before Closing/revision use. Governance commands share unambiguous typed parsing; duplicate case-insensitive properties refuse at scope, source validation and admission/digest boundaries. Real gateway and SDK-dispatcher regressions reconcile the two earlier authority assumptions to local scope while preserving DW-21 qualification.
- Consumer schema 2 observes managed/native loads from one fresh nonshared build, with generated/SDK/OS provenance separate. The parent reviewed all eight assembly additions against their existing analyzer package/version/path/hash; all 38 prior entries remain. A parent audit found and removed the second unobserved Build. Real helper-only and external-load fixtures pass.
- A pre-existing UI full-suite failure was traced to an immediate assertion after an asynchronous tenant change. One awaited assertion retains the same expected revision and product behavior; the focused case and full UI suite pass.
- Pre-integration evidence retains the Story 5.2 Release and Story 6.1 Package results, 6,363 owning cases, 219 focused cases and seven consumers. Its tooling discovery passed 337 with two missing-Debug-assembly skips, separately covered by explicit Release/package real-runner checks. These historical results are not attributed to the integrated source.
- Debug/source preparation succeeds, then the mapped build refuses two pre-existing EventStore CA1822 errors. No analyzer/audit suppression or owner API change was made. The dated evidence report preserves this blocker and prior failed attempts separately.
- Parent audit verified every named matrix method ran and passed, source hashes match the verification manifest, and every consumer selection matches the reviewed policy. Root-declared submodules were initially clean with no unpushed local commits.

- Main advanced concurrently to c5c22620cafaa0af2625896cf41022e614e9b490 and the workspace was externally fast-forwarded with our work restored. Integrated its shared pure consumer helpers, --no-receipt and literal after-unit-tests hook, retaining every observation/policy guard. The supported hook backend uses literal strace compiler commands; real CiCd discovery and actual seven-consumer execution pass. No owner policy or deleted tooling was restored. Archived pre-integration evidence remains distinct.
- Final combined-source verification passes the maintained Story 6.1 Package gate: Contracts 2,005, Client 7, Domain 1,042, Server 1,959 and UI 1,352, totaling 6,365 with no failures/errors/skips/not-run cases. The complete Release solution build has zero warnings/errors; the current package floor and actual six-package/seven-consumer hook pass. Final affected consumer/observer units pass 12/12, hook units 6/6 and real executable fixtures 7/7. The current Story 5.3 real-runner check passes against an explicitly identified Release/package assembly.
- Combined-source tooling discovery ran 324 tests: 322 passed, one pre-existing graph-parser error and one missing-Debug-assembly skip. The graph-parser repair passes its affected 7/7 class and a further actual SDK control; the skipped real-runner check passes in the explicit package fallback. The final report preserves the failed broad run rather than relabeling it. Independent follow-up reviews found no unresolved implementation issue; the pre-existing endpoint/host gap remains deferred under DW-21. See [final verification](tests/ingress-consumer-2026-11-16/FINAL.md).

## Spec Change Log

## Review Triage Log

- B1 — medium / defer: The actual SDK /query path removes IsGlobalAdmin without IDomainServiceAdministratorVerifier, and Agents registers none. Direct dispatcher tests deliberately establish originating-caller binding only; the production composition gap is pre-existing and remains under DW-21. Add an explicit endpoint-qualification follow-up and clarify the report; do not invent an owner verifier.
- B2 — medium / patch: Observed Build fixes UseSharedCompilation=false while preprocessing uses different properties/environment. A conditional import can therefore execute without its import bytes being inventoried. Collect under the same observed evaluation inputs.
- B3 — high / patch: LD_DEBUG PID equality cannot see a statically linked child. The edge reviewer reproduced changed static-helper execution with identical accepted policy; require independent process-boundary observation and refusal of unobserved children.
- B4 — high / patch: Native bytes are first hashed after Build. The edge reviewer reproduced a changed helper executing and atomically restoring reviewed bytes before exit, leaving accepted identical receipts. Prevent/detect package input mutation during observation or bind the actual loaded bytes.
- B5 — medium / patch: Directory containment alone can label task-generated files as package members even when absent from the restored archive inventory. Require exact package-file membership and input integrity before assigning package provenance.
- B6 — medium / patch: New duplicate refusals throw InvalidOperationException, which is an infrastructure error through admission, rather than request validation. The source handler wraps ordinary idempotency errors as 503 (so the review's universal 500 wording is too broad); use the existing validation path and test a raw HTTP refusal without effects.
- B7 — low / patch: Platform refusal occurs after restore and observer compilation, contrary to the documented early refusal. Move the same prerequisite check to the complete consumer entry point and verify no build/restore starts.
- B8 — medium / patch: The new foreign-scope return follows a directory await without checking cancellation. A reader can cancel the token and return foreign open state, producing NotFound rather than cancellation; check the token immediately after the await and regress no effects.
- B9 — low / patch: Gateway tests use a callback and in-memory Handle/Apply history, not a qualified state-store transaction. Their replay assertions are useful local model evidence; describe that boundary explicitly and remove durable-store claims without inventing topology evidence.
- B10 — low / patch: Native integration fixtures now require Linux/glibc and a C compiler during comprehensive discovery. Make these prerequisites explicit with a clear refusal and documented integration command; preserve unsupported-platform unit coverage and do not silently skip required qualification.
- E1 — high / patch (same root as B3): End-to-end static helper recompilation changed executed output from 1 to 2 while both policies passed. Independent execution tracing must detect this child even when no managed or loader trace exists.
- E2 — high / patch (same root as B4): End-to-end native mutation/restoration changed output from original1 to original2 while both receipts retained the original SHA. Bind input integrity through the complete observed build.
- E3 — medium / patch: Replacing ReadProperty removed the required Enabled presence check for tenant enablement. Typed deserialization defaults an omitted bool to false, exposing unintended disable behavior. Preserve case-insensitive required-member refusal at every common policy entry point.
- E4 — high / patch (claims corroborated by E1/E2): Current local helper qualification omits static executions and can attribute restored native bytes to changed execution. Repair those two demonstrated observation boundaries before claiming qualification.
- V1 — medium / patch: The pre-verified regression gap is real: file-backed fixtures do not exercise the new dynamic or locationless-assembly refusal branches. Execute byte-loaded and dynamic helpers and require refusal after their execution.
- V2 — high / patch (same root as B3): A real packaged task clearing ProcessStartInfo.Environment ran /bin/sh and still passed validation with only dotnet/csc observed. Process-boundary observation must remain effective when children strip environment instrumentation.


- P1 — medium / patch: Parent inspection found the new tracer's first version swallowed ordinary non-event SIGTRAP and treated waitpid EINTR as terminal. Both were corrected and executable probes now preserve exit 133 and recover injected initial/main wait interruptions.
- R2-1 — medium / patch: The independent native-observer recheck confirmed all earlier observer defects were addressed, but a real child raise(SIGSTOP)/parent waitpid(WUNTRACED)/SIGCONT probe changes exit 0 to 12 because all SIGSTOPs are suppressed. Preserve genuine group-stop/continue semantics while distinguishing synthetic attachment stops; the exact probe must pass before final consumer qualification.


- U1 — medium / patch: Concurrent upstream c5c22620cafaa0af2625896cf41022e614e9b490 removed NU1901/NU1902 exemptions but left the evaluated audit test expecting them. Preserve the stronger production policy: the test now requires no exemption or suppression at any audit severity, with the method named accordingly.
- U2 — false / verification harness: The new whole-repository package-authority test correctly refused 46 missing tracked archived inputs in the rsync-based native harness, which excluded _bmad-output and .git. Use a complete private Git checkout, retaining tracked evidence/exclusion inputs; do not weaken or skip the validator.
- U3 — medium / patch: Required combined-source tooling exposed a pre-existing verifier parser defect: a successful MSBuild server-fallback diagnostic precedes JSON in its combined log. Read the SDK's dedicated structured result file while retaining complete diagnostics and all exit/audit checks. The affected class passes 7/7, including missing/malformed-result refusal, and an additional real SDK graph passes with the same diagnostic preserved.

Review completion: all implementation patch groups above are corrected and verified. The independent blind and verification follow-up reviewers found no unresolved issue in the integrated observation/hook path. B1 remains the explicitly deferred owner-bound endpoint qualification; no production composition acceptance is inferred.

Patch groups: process coverage (B3/E1/E4/V2); native input integrity and provenance (B4/B5/E2/E4); consistent import observation (B2); required Enabled (E3); cancellation (B8); client validation (B6); early platform/prerequisite checks (B7/B10); fileless-load regressions (V1); model-evidence wording (B9). B1 is the separately retained pre-existing endpoint qualification gap. No intent question or public API change is required for these corrections.

## Verification

Use disposable outputs beneath the wrapper's TMPDIR and await children. Build affected test projects in Debug/source mode and run native xUnit assemblies with single-dash selectors, then owning full suites. Run maintained Story 6.1/5.2 gates appropriate to changed behavior, complete tooling tests, package validation and seven fresh Release/package consumers with audits enabled. Retain exact source revisions, commands, native totals and concise receipts; record broad-gate blockers separately and use the required fallback ladder. Run whitespace and Conventional Commit validation; fetch/inspect divergence, verify submodule remote reachability, commit and push main under repository delivery rules.

On the integrated source, upstream deleted Story 5.2 and the generic Story verifier; neither was restored. The maintained Story 6.1 Package gate and standalone EventStore package-floor check supply the current owning-suite/floor evidence. The old scripts and their results remain explicitly pre-integration records.
