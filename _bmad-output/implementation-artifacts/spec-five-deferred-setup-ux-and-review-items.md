---
title: 'Resolve five deferred setup UX and architecture review entries'
type: 'bugfix'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
baseline_commit: a624d25cc0c8a7c5a4ebd8c2fc0e0ffb1731ae1e
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The deferred ledger mixes an already implemented pending-write presentation with unresolved setup attempt and draft policies. It also retains an architecture review escalation that later review and SDK changes addressed without a consolidated closure record.

**Approach:** Complete five ledger entries: DW-5; unresolved setup-write identity/payload lifetime across route navigation or reload; DW-17; its separate newer-concurrent-projection draft entry; and the concurrent v7 Architecture/.NET advisory adjudication entry. The two draft records share one implementation. Verify existing pending-write behavior, implement the chosen lifetime and draft rules, reconcile architecture evidence, review, and push main.

## Boundaries & Constraints

**Always:** Preserve exact retry identity/payload while an attempt is retained, accepted receipt and target-version evidence, authorization, instruction confidentiality, cancellation and typed failure surfaces. Distinguish submitted uncertainty, accepted pending and confirmed projection. Use FrontComposer/Fluent UI v5 with English/French copy. Preserve every original deferred entry and separate local verification from live qualification.

**Never:** Infer success from timeout or unrelated projection changes, display stale setup details after typed denied/missing reads, restore instruction text from server reads, silently overwrite local edits, or treat navigation as command cancellation. Do not change owner commitments, runtime SDK pins, public wire contracts or deployed configuration. The accepted-pending reload contract remains binding; this batch adds no durable recovery of unacknowledged submissions.

## Approved Decisions

The user approved the recommendations on 2026-10-25. Retain exact command data, identities, drafts and received acceptance in a small scoped in-memory store across ordinary navigation within the same authenticated circuit. Bind retention to user, tenant and Agent; never retain a callback capturing the disposed page. Reopening restores the unresolved attempt and reads status without automatic submission; new writes remain blocked. Resolution or explicit abandonment releases sensitive retained content; abandonment does not cancel a server command. Reload/circuit loss recovery of unacknowledged attempts is explicitly unsupported. Do not add browser/durable instruction storage, a background retry engine or a new timeout policy.

Preserve editable drafts when server values change. Show a notice only when relevant saved values changed from the draft baseline and differ from the draft, not for ordinary typing or an unrelated lifecycle revision. Offer one explicit Load saved values action: obtain a fresh authorized snapshot, replace name, description and response mode, leave instruction text untouched, and change no draft if the read fails. Disable this action while a write remains unresolved. No automatic merging or diff viewer.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Terminal read | Accepted receipt, then denied/missing/unavailable/malformed read | Pending-write notice plus typed failure; no old setup details | Stop polling; retain exact recovery target |
| Draft divergence | Local editable values differ from a confirmed newer projection | Keep drafts; show divergence notice and explicit load action | Never silently replace edits |
| Unacknowledged attempt | Transport outcome unknown when navigating/reloading | Restore exact attempt within circuit; reload recovery unsupported | No claim that leaving cancels the command |
| Review reconciliation | Historical v7 findings and subsequent adjudication | Trace each finding to current rule, retirement or still-open owner | No general security or production-readiness claim |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor`: independent write notice precedes typed setup surfaces; `ExecuteAttemptAsync` and `RefreshPendingAsync` retain acceptance; `LoadAsync` deliberately preserves drafts after writes. `Dispose` ends component lifetime.
- `src/Hexalith.Agents.UI/State/AgentSetupAttempt.cs`: replace captured delegate with explicit immutable command data; add focused state/store types under the same directory and register scoped retention through existing UI composition. Do not reuse the provider browser store blindly: its payload and confidentiality assumptions differ.
- `test/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs`: terminal-read theory already covers unavailable, denied, missing and Success/null; draft preservation and disposal regressions are reusable.
- `src/Hexalith.Agents.UI/Resources/AgentsResources{,.fr}.resx`: localized notices/actions.
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`: projection catch-up and high-risk pending rules; reload already requires accepted pending to be re-derived from authority.
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/`: current Spine includes the v7 review sources; September 10 update records later adjudication; ARCH-A-4 is retired September 16. Current `global.json` pins 10.0.402. Reconcile historical findings without claiming a fresh vulnerability audit.

## Tasks & Acceptance

**Execution:**
- [x] `AgentConfiguration.razor` and `State/AgentSetupAttempt.cs` at the paths above — implement approved retention and draft behavior; add scoped composition registration and focused store tests; document additional files in Implementation Notes.
- [x] Both resource files and `AgentConfigurationTests.cs` above — cover local draft retention, explicit actions, newer projections, unresolved departure, and no stale setup disclosure after each typed read failure; assert recovery keeps the accepted target.
- [x] `EXPERIENCE.md` and its `.memlog.md` — record the approved policies and their limits; preserve existing accepted-pending authority rules.
- [x] `_bmad-output/implementation-artifacts/deferred-setup-ux-review-2026-10-25.md` — record exact local verification and a finding-by-finding v7 adjudication map, including surviving assumptions and owner dependencies.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` — close exactly the five selected entries only after their evidence is complete; preserve original text and all unrelated statuses.

**Acceptance Criteria:**
- Given an accepted write followed by a typed read failure, when rendered, then pending truth and recovery remain without stale setup disclosure.
- Given confirmed server values that differ from local drafts, when refreshed, then the divergence notice and explicit action are visible and edits are preserved until an explicit permitted action.
- Given an unacknowledged setup attempt, when navigation/reload occurs, then same-circuit recovery and explicit reload limitations are enforced without implying cancellation or success.
- Given the five ledger records, when closure is reviewed, then each has specific evidence and every remaining qualification/assumption retains its original authority.

## Implementation Notes

- User's “apply recommended” resolves both choices and authorizes implementation, review and the previously requested main push. No second approval is required. No epic story key applies.
- Initial tree contains an unrelated modification to `spec-5-4-reconcile-owner-deliveries.md`; preserve it byte-for-byte and exclude it from staging/commits. Work only on this spec's implementation and evidence. The root has no AppHost.
- During implementation do not commit or push; parent stages, audits, reviews and delivers after verification. Keep evidence local-only and preserve all external qualification gates.

- Runtime additions: `State/AgentSetupAttemptKind.cs`, `State/AgentSetupScope.cs`, `State/AgentSetupEditableValues.cs`, `State/RetainedAgentSetupAttempt.cs`, and `State/AgentSetupAttemptStore.cs`; existing UI composition registers the store scoped. `AgentSetupAttemptStoreTests.cs` covers retention, scope isolation, disposal/release and absence of recovery in a new store. `ControlledSetupAuthenticationStateProvider.cs` supplies controlled incomplete, stale and faulted authentication notifications for the review regressions. `AgentsTestContext.cs`, `AgentsUiCompositionTests.cs` and `LocalizationResourceTests.cs` carry the registration and EN/FR checks.
- Local implementation verification: Debug/source UI build passed without warnings; native xUnit configuration 115/115, store 21/21, full UI 1,249/1,249, all zero failed/skipped/errors. The maintained full `eng/verify-story-5.2.ps1` gate passed. Logs and native XML are under `tests/setup-ux/`. The cold pending test proves re-arming only when authority returns `AuthoritativePending`; rediscovery of a lost acceptance by a live host remains separately qualified.

- Parent acceptance audit: all five execution tasks and all four matrix rows mapped to the changed code, native passing cases, policy notes and historical source map. All recorded source/report/log hashes verified; exactly five existing ledger records closed and original summaries preserved. Independent review follows this audit.

## Verification

Build the UI test project in Debug/source mode, run its native xUnit assembly with focused nonzero selections, then the full affected UI suite and the maintained Story 5.2 gate. Check localization and Fluent conformance. Inspect review-source links and exactly five ledger changes. Run independent BMAD review, resolve findings, validate the Conventional Commit message with commitlint, fetch/inspect divergence, commit and push main as requested. No root AppHost exists; no topology change is planned.

## Spec Change Log

## Review Triage Log


Review pass 1 (three independent context-free reviewers; all results collected before triage). Every finding below is adjudicated individually before grouping. The fixes remain local implementation corrections using existing state, controls and gateway operations; no policy, public contract or frozen intent changes are needed.

| Finding | Verdict | Evidence and route |
| --- | --- | --- |
| BH-1: ordinary editing after confirmation falsely diverges | medium | Confirmed reads leave `_draftBaseline` unchanged; A→B followed by typing C satisfies B≠A and B≠C. Patch matching fields' existing baseline without erasing real concurrent differences; regression required. |
| BH-2: failed explicit load strands drafts | medium | Failure replaces `_result`, removing the form and its only load action; the no-attempt route has no recovery control. Patch a read-only retry outside the failed surface, preserving drafts until explicit successful replacement. |
| BH-3: explicit load can regress to older saved values | medium | The null-target read is labeled confirmed by `AgentSetupViewFactory`; no minimum observed version is checked before replacing drafts. Patch monotonic version validation and cover older returns. |
| BH-4: identical authentication notification discards attempt | medium | The store handler unconditionally clears the binding and receipt despite unchanged authenticated subject/tenant/target. Patch comparison of the supplied authentication binding while keeping stale/pending authentication fail-closed. |
| BH-5: read continuation races authentication cleanup | high | `ConfigureAwait(false)` resumes component assignments outside its renderer; scope check and assignment can straddle queued cleanup. Patch renderer serialization and a delayed-read authentication regression. |
| BH-6: changed Agent target can resurrect old lease | high | `IsCurrent` returns false on mismatch but retains `_scope` and `_retained`; restoring options A after observing B validates the old reference. Patch invalidation on the observed mismatch. |
| BH-7: reopening read allows concurrent recovery actions | medium | Initialization restores `_pendingWrite` without `_busy`; Retry/Abandon can run before the outstanding read overwrites state. Patch the existing busy guard over initialization. |
| BH-8: interrupted observation asserts service outage | low | Null retained outcome is mapped to `Unavailable`, whose copy asserts outage despite no evidence. Patch to existing `UnableToVerify` status, a direct correction. |
| BH-9: other encoded operation replay cases untested | medium | New navigation coverage tests configuration replay only; the new switch also dispatches response mode, versioned activation and disable. Patch focused navigation/replay tests with exact original options. |
| BH-10: pending authentication generation/disposal untested | medium | Store tests supply only completed authentication tasks, leaving stale completion and disposal guards without regression coverage. Patch controlled pending-state tests. |
| EC-1: reopening read overwrites concurrent retry | medium | Same demonstrated missing initialization busy guard as BH-7. Patch with BH-7, retaining this individual verdict. |
| EC-2: matched field baseline remains stale | medium | Same A→B→C predicate as BH-1; advancing only matching fields preserves genuine differences. Patch with BH-1. |
| EC-3: target away/back revives lease | high | Same retained reference and target comparison as BH-6. Patch with BH-6. |
| EC-4: null initial scope permits late previous-user read | high | `LoadAsync` conditionally validates only non-null scopes, so a read begun without a binding can later restore setup after auth cleanup. Patch unconditional valid-scope gating before/after the read. |
| VG-1: immediate confirmation on reopening untested | medium | Pre-verified gap: current accepted-navigation theory confirms only on subsequent Refresh; deleting the initialization confirmation block escapes it. Patch direct confirmed reopening test including store release and write enablement. |
| VG-2: later typing falsely diverges | medium | Verified against unchanged baseline and field predicate as BH-1; current typing test never edits after confirmation. Patch with BH-1. |

Groups: baseline (BH-1/EC-2/VG-2), failed-read recovery (BH-2), read version floor (BH-3), same-binding notifications (BH-4), renderer confinement (BH-5), target invalidation (BH-6/EC-3), initialization concurrency (BH-7/EC-1), truthful unknown-outcome copy (BH-8), encoded replay coverage (BH-9), pending authentication coverage (BH-10), null-scope read gating (EC-4), immediate confirmation coverage (VG-1). All route to patch; no deferred or intent-gap findings. The implementation agent receives these corrections, then the parent reruns the specified checks and refreshes evidence.


Patch verification outcome: all 16 reviewed findings are resolved; no new deferred work. Parent read the implementation/test corrections and reran the prescribed checks: warning-free Debug/source build, 115 configuration cases, 21 store cases and 1,249 full UI cases, all passing with zero errors/skips/unrun cases. The complete Story 5.2 gate passed 5,882 owning-project cases plus its overlapping focused/composition checks. Compressed native XML/logs, source and assembly hashes in `tests/setup-ux/verification.json` were refreshed from these final runs.

| Patch group | Executed regression evidence |
| --- | --- |
| Baseline advancement | `ConfirmedDraftFieldsBecomeTheBaselineForFurtherTyping` (name, description, mode); original genuine-concurrency cases still pass. |
| Visible failure recovery and version floor | `FailedExplicitLoadPreservesEveryDraft` (older, denied, missing, unavailable, malformed, throws, pending) now recovers the visible editor and then explicitly loads fresh values without changing instructions. |
| Authentication lifetime and ordering | `SameBindingAuthenticationRefreshPreservesExactLeaseAttemptAndReceipt`, five `PendingAuthenticationFailsClosedUntilTheSuppliedBindingIsResolved` cases, `StaleAuthenticationCompletionCannotRestoreAFormerUserOrEraseTheCurrentAttempt`, and both mounted pending-auth cases pass. |
| Scope confidentiality | Both `DelayedReadCannotRestoreContentAfterAuthenticationChanges` cases, `MissingScopeCannotReadOrRecoverIntoAnotherAuthentication`, existing mounted logout/no-disclosure cases and `ObservedTargetChangeCannotReviveTheOldLeaseWhenTheTargetChangesBack` pass. |
| Initialization and immediate confirmation | Both `DelayedReopeningReadDisablesAndGuardsAllRecoveryActions` cases block all applicable recovery actions; both `ReopeningImmediatelyConfirmedAcceptanceReleasesRetentionWithoutSubmitting` cases release at/above target. |
| Truthful uncertainty and exact replay | Existing configuration-navigation case asserts `UnableToVerify`; `NavigationReplaysTheOriginalOperationPayloadAndIdentity` covers mode, versioned activation and disable with original operation/options. |

Final acceptance audit: all execution tasks and matrix rows are satisfied; exactly five original ledger records close, all original summaries/evidence are preserved, UX memory is append-only, report links and anchors resolve, and the unrelated owner-delivery spec has its original SHA-256. No AppHost or live-host qualification is claimed. Commit message validation passed. The user's explicit request to push main governs delivery despite the workflow's default no-auto-push rule.
