# Good-Spine Rubric Review — AD-4/AD-10 Capability-Version Reconciliation

**Date:** 2026-08-01  
**Scope:** The 2026-08-01 AD-4/AD-10 amendment only, including its brownfield implementation-gap note.  
**Lens:** `bmad-architecture` good-spine checklist: real initiative-level divergence, enforceability, requirements/invariant consistency, brownfield ratification, and honest treatment of deferred/non-conformant implementation.  
**Mechanical gate:** `lint_spine.py` passed with zero findings.

## Verdict

**CONDITIONAL PASS — retain the context-time capability-version floor; do not ratify the current ignore-the-snapshot behavior.** The `live CapabilityVersion >= snapshotted ProviderCapabilityVersion` decision is a non-obvious, enforceable initiative-level invariant and closes a demonstrated divergence between provider selection/snapshotting and Story 2.3 context budgeting. It is consistent with AD-4, AD-11, AD-12, and the PRD's safe-budget/fail-closed requirements. Two focused amendments are still warranted: the implementation-gap note understates the current readiness non-conformance, and the spine does not yet settle which version downstream generation/audit calls the version “backing” the attempt when a higher live version is accepted.

## Findings

### High — Discuss: the implementation-gap note understates non-conformance to the amended Rule

The note correctly records that `AgentInteractionContextOrchestrator` never compares the live version with `request.ProviderCapabilityVersion`. Its placement beside AD-10 is appropriate: this is a decided invariant with a known brownfield gap, not a design choice that belongs under Deferred.

However, the note says the Story 2.3 path becomes conformant once the floor and lower/equal/higher tests are added. AD-10 now also says that, after the floor is satisfied, **current readiness** and safe limits govern the build. The current budget read accepts any successful entry with `SupportsTextGeneration == true`; it does not reject `ProviderModelStatus.Disabled` or `ProviderConfigurationState.NotConfigured` (`AgentInteractionContextOrchestrator.cs`, `ReadModelBudgetAsync`, lines 196–218). `ProviderCatalogInspection.GetEntry` intentionally returns disabled entries as successful inspection results, so this is reachable. The existing test named `Disabled_or_non_text_capable_entry_returns_context_blocked` only supplies `SupportsTextGeneration: false`; it does not exercise a disabled text-capable entry.

This matters because `CapabilityVersion` increments for safe-metadata updates but is unchanged by enable/disable events (`ProviderCatalogState.cs`, lines 58–100). The new floor therefore cannot substitute for the separate current-readiness check required by AD-10/AD-12. A same-version disabled entry can still be treated as a usable context budget.

**Recommended disposition:** keep the note, but expand the follow-up's conformance boundary to require both (a) the lower/equal/higher floor and (b) the existing shared readiness semantics—enabled, configured, text-generation capable, and valid relevant limits. Focused tests should include lower/equal/higher versions plus disabled and unconfigured text-capable entries. Avoid wording that says the floor alone makes the path conformant.

### High — Discuss: accepting a higher version leaves downstream provenance semantics ambiguous

At context time the amended AD-10 records the **live** capability version in context evidence. Story 2.4 still passes the **snapshot** `ProviderCapabilityVersion` to `AgentGenerationProviderRequest` and records it on generation success/failure evidence, while independently reading current provider budget/readiness (`AgentInteractionGenerationOrchestrator.cs`, lines 144–190 and 210–264; `AgentInteractionGenerationRequest.cs`, lines 22–40). The generation contracts describe that value as the capability version “backing” the attempt.

For `live > snapshot`, context evidence can therefore say version 2 governed the budget while generation evidence says version 1 backed the invocation, even though generation also read the current version-2 catalog entry. This is precisely the kind of cross-feature semantic divergence an initiative spine should prevent. The context floor decision is still correct, but its altitude requires settling the two roles rather than leaving Story 2.3 and Story 2.4 to infer them independently.

**Recommended disposition:** add one invariant sentence defining provenance. Prefer distinguishing the **snapshot minimum/baseline version** from the **effective live capability version** used for context/generation, and propagate or separately record the effective version in provider invocation and generation audit evidence. If the intentional choice is instead to retain only the snapshot version downstream, say so explicitly and stop describing it as the version backing the live attempt.

### Medium — Autofix: AD-10's `Binds` and `Prevents` were not reconciled with its new temporal rule

AD-10 still says it binds only FR-4, FR-5, and OQ-7, and prevents incompatible provider metadata shapes. The amended rule now directly governs FR-9 safe context budgeting, FR-21 stale/missing dependency behavior, and FR-24 versioned audit evidence; its new primary divergence is stale/older catalog metadata versus request-time snapshot state. The memlog captures that divergence accurately, but the distilled AD does not.

**Recommended disposition:** extend `Binds` at least to FR-9, FR-21, and FR-24 (FR-10 is reasonable if context blocking is treated as generation failure), and extend `Prevents` to name stale catalog projections supplying an older model budget while avoiding unnecessary invalidation after newer metadata revisions.

## Rubric Walk

| Rubric test | Result | Evidence and judgment |
| --- | --- | --- |
| Fixes a real divergence one level down | **Pass** | Story 2.3's Dev Notes asked for a match, Task 4/current code uses the live version without comparison, and its senior review explicitly deferred the reconciliation. Story 2.2 does not carry `ProviderCapabilityVersion` and checks only current selectability, so it never established this floor. The divergence is real, not speculative. |
| Belongs at initiative altitude | **Pass, with provenance gap above** | Provider catalog administration/selection (Epic 1), request snapshotting and context (Epic 2), generation (Epic 2), and audit/status (Epic 4) all consume the version. Independent feature teams could reasonably choose equality, minimum-floor, or ignore semantics. The spine should decide it. |
| Rule is enforceable | **Pass** | It names the comparison operands and identity scope, exact comparator, missing/lower failure classification (`ModelBudgetUnavailable`), equal/higher behavior, effective evidence version, and a focused lower/equal/higher test matrix. |
| Consistent with AD-4 | **Pass** | Provider/model identity and request-time baseline remain snapshotted; later selection does not retarget the interaction. Re-evaluating current readiness/limits preserves safety without rewriting identity or history. |
| Consistent with AD-11 / FR-9 | **Pass** | The budget used to decide full/bounded/blocked context is the current trusted budget for the snapshotted model. A lower/stale revision cannot silently supply obsolete limits, and no equality pin rejects a newer trusted revision merely because metadata changed. |
| Consistent with AD-12 / FR-21 | **Pass in design; current code gap remains** | Missing/lower catalog state is classified as dependency uncertainty and fails closed. Because enable/disable does not change `CapabilityVersion`, the distinct readiness check remains mandatory, as described in the first High finding. |
| Consistent with FR-5 | **Pass** | FR-5 makes later **provider/model selection** future-only; it does not require freezing runtime safety metadata. The amendment keeps identity fixed while using current safe constraints. |
| Brownfield ratification | **Pass, note needs broader scope** | The spine does not pretend current code conforms and explicitly names the orchestrator/testing gap. Recording the gap adjacent to the adopted Rule is preferable to weakening the Rule or moving the decided invariant to Deferred. |
| Nothing relevant left silently divergent | **Not yet** | Effective-versus-snapshot version semantics in generation/audit remain unspecified, and current readiness is not fully represented in the gap note. |

## Decision Recommendation

Choose the amended floor semantics:

1. The request-time `ProviderCapabilityVersion` is a minimum trusted catalog revision, not an equality pin.
2. At context build, missing or lower live state fails closed as `ModelBudgetUnavailable`.
3. Equal or higher live state may govern only after full current-readiness and safe-limit validation.
4. The spine should distinguish the snapshot baseline version from the effective live version used and recorded by downstream generation/audit.

Formally accepting the current implementation would be weaker than AD-12 and would leave `ProviderCapabilityVersion` on the snapshot behaviorally unused in Story 2.3 despite its stated temporal purpose.

## Recheck 2026-08-01

**Verdict: PASS — all prior critical/high findings are resolved; no critical or high findings remain.**

- **Prior High (implementation-gap scope): resolved.** AD-10 now requires enabled/configured/text-generation readiness and valid step-required limits independently of version, explicitly noting that enable/disable does not bump `CapabilityVersion`. The implementation-gap note accurately names the context readiness defect as well as the version comparison defect. The open sprint follow-up has a developer owner and covers lower/equal/higher versions, disabled/unconfigured entries, cross-step evidence, and changed-input retries.
- **Prior High (snapshot/effective provenance): resolved.** AD-10 now defines the snapshot version as request provenance and each runtime evidence version as the effective live version consumed by that step. Its cross-step floor prevents a later projection from regressing below the latest effective version. AD-13 binds provider/model, effective version, limits/timeout, and input fingerprint to the deterministic attempt id, requiring fail-closed handling or an explicit new attempt when retry inputs differ.
- **Prior Medium (`Binds`/`Prevents`): resolved.** AD-10 now traces the temporal/runtime rule to FR-9, FR-10, FR-16, FR-21, and FR-24 and names stale/disabled state, provenance conflation, and changed-input retry identity as the prevented divergences.
- **Representation and mechanics: pass.** The decided invariant stays in the spine, brownfield non-conformance remains an adjacent implementation-gap note rather than Deferred, and implementation work is separately tracked as open. The deterministic spine lint passes with zero findings.

## Final recheck 2026-08-01

**Verdict: PASS — the adversarial tightening introduces no rubric regression; no critical or high findings remain.**

- AD-10's durable high-water mark now advances even when a newly observed version later fails readiness/limit validation, preventing a subsequent fallback to an older apparently-ready projection. Freshness, version monotonicity, and current readiness remain independent, enforceable gates.
- AD-11 now makes `ContextReady` a progression decision rather than a frozen provider input and requires the actual authorized content, token measurement, capability state, and budget to be revalidated immediately before generation/regeneration. This is consistent with FR-9/FR-10 and AD-12's before-side-effect fail-closed rule.
- AD-13 now matches the domain's terminal-attempt semantics: one checkpointed prepared-attempt descriptor per deterministic id, retries only for transient transport/timeout failures, exact descriptor reuse, and no replacement attempt hidden under the same interaction/action.
- The provider-attempt fingerprint convention is sufficiently convergent at initiative altitude: one shared canonicalizer, stable field order, length-prefixing, SHA-256, digest-only persistence, and comparison through the same canonicalizer. It prevents independent retry paths from choosing incompatible serialization or concatenation rules without placing raw context in the descriptor.
- The implementation-gap note and open sprint follow-up cover the high-water mark, distinct effective-version contracts, immediate pre-invocation revalidation, canonical descriptor checkpoint, retry classes, and focused tests. Deterministic spine lint remains clean with zero findings.
