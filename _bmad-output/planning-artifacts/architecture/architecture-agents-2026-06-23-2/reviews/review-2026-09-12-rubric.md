# Reviewer Gate — Good-Spine Rubric

**Date:** 2026-09-12  
**Lens:** rubric walker  
**Scope:** current 2026-09-12 architecture update and material incompatibilities it exposes  
**Verdict:** **FAIL** — deterministic lint passes, but the new data-handling acceptance command is self-blocking and the update still leaves competing normative instructions across the spine, PRD, UX, and registers.

## Mechanical pass

`uv run ./.agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` passed with zero findings.

## Critical

### C1 — `DataHandlingAcceptance` requires the gate state that the command exists to repair

- **Evidence:** The spine makes missing/current-version acceptance block Provider readiness with `DataHandlingAcceptanceLapsed` ([ARCHITECTURE-SPINE.md:204](../ARCHITECTURE-SPINE.md), [ARCHITECTURE-SPINE.md:206](../ARCHITECTURE-SPINE.md)). Every operation must pass its matrix row ([ARCHITECTURE-SPINE.md:260](../ARCHITECTURE-SPINE.md)). The new matrix row requires `LR-PROVIDER` ([launch-readiness-register.md:171](../../../launch-readiness-register.md)), while the binding Provider-readiness contract classifies acceptance-lapsed input as `Blocked` ([launch-readiness-register.md:210](../../../launch-readiness-register.md), [launch-readiness-register.md:212](../../../launch-readiness-register.md)). The PRD and UX require the Tenant Agent Administrator to clear precisely that state by accepting the version ([prd.md:206](../../../prds/prd-agents-2026-06-23/prd.md), [prd.md:260](../../../prds/prd-agents-2026-06-23/prd.md), [EXPERIENCE.md:323](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [EXPERIENCE.md:330](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md)).
- **Why it matters:** A never-accepted or newly changed version makes `LR-PROVIDER` blocked, so the acceptance command cannot execute and the tenant cannot recover. Two implementations could also diverge by either honoring the matrix or special-casing around it.
- **Disposition:** **Autofix before finalization.** Make `DataHandlingAcceptance` depend only on gates needed to read and mutate the catalog/tenant record safely, not on tenant callability that includes acceptance itself. Correct matrix v3 while it is still this update's draft; if v3 has already escaped as immutable, append a corrected v4 and make it current. Add a test proving an acceptance-lapsed tenant can accept or decline without Provider invocation.

## High

### H1 — The new lock-bearing family is absent from the binding audit and UI-conformance inventories

- **Evidence:** AD-12 adds `DataHandlingAcceptance` as lock-bearing ([ARCHITECTURE-SPINE.md:222](../ARCHITECTURE-SPINE.md)), but AD-12's base roster still calls another set the lock-bearing subset ([ARCHITECTURE-SPINE.md:218](../ARCHITECTURE-SPINE.md)), and AD-22 says **all nine** lock-bearing families require justification/change evidence while listing nine that omit `DataHandlingAcceptance` ([ARCHITECTURE-SPINE.md:292](../ARCHITECTURE-SPINE.md)). The UX defines it as a high-impact confirmation with Accept and Decline ([EXPERIENCE.md:307](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md)), yet the register's restrictive-viewport conformance inventory omits it ([launch-readiness-register.md:243](../../../launch-readiness-register.md)).
- **Why it matters:** A conforming implementation could apply the advisory lock yet omit the mandatory justification, durable change evidence, or restrictive-viewport test for a tenant data-governance decision.
- **Disposition:** **Autofix.** Fold `DataHandlingAcceptance` into the single normative lock-bearing roster, remove the brittle numeric count or change it consistently, include its accept/decline evidence in AD-22, and make the NFR-13 contract cover every lock-bearing family (including this one) rather than an incomplete hand-picked list.

### H2 — AI Party provisioning and validation contradict the PRD's active transitional contract

- **Evidence:** AD-2 unconditionally obtains or creates an AI-type Party ([ARCHITECTURE-SPINE.md:144](../ARCHITECTURE-SPINE.md)); AD-7 unconditionally verifies that the linked Party remains AI-type ([ARCHITECTURE-SPINE.md:182](../ARCHITECTURE-SPINE.md)). The cited PRD and `EXT-PARTIES-1` instead say that until the dependency lands, the provisioned Organization-typed Party is the immutable identity and is verified by id, never by Party type ([prd.md:884](../../../prds/prd-agents-2026-06-23/prd.md), [prd.md:941](../../../prds/prd-agents-2026-06-23/prd.md), [external-dependency-register.md:93](../../../external-dependency-register.md)). `EXT-CONV-AI-1` also states unconditional AI-type verification despite citing the conditional A-21/A-27 behavior ([external-dependency-register.md:57](../../../external-dependency-register.md)).
- **Why it matters:** Independently built Agents and Conversations units can reject the only identity the PRD currently permits, or silently ship different identity checks.
- **Disposition:** **Autofix from the accepted PRD branch.** State the conditional rule once across AD-2, AD-7, and both dependency records: verify the immutable Party id in all cases, and additionally require AI type only after `EXT-PARTIES-1` is available. If Architecture intends AI type to be mandatory with no transition, that is a Product decision and the PRD/A-27/register must be changed together instead.

### H3 — The cited, binding UX artifact remains a competing authority after the 2026-09-12 decisions

- **Evidence:** The UX still says the dialog panel announces while open and is self-contained ([EXPERIENCE.md:126](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [EXPERIENCE.md:133](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md)), while its component rule and AD-31 say the panel owns no live-region node and routes all announcements through the persistent region ([EXPERIENCE.md:230](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [ARCHITECTURE-SPINE.md:350](../ARCHITECTURE-SPINE.md)). UJ-3 still has Anika request a regeneration and then approve without identifying a second approver ([EXPERIENCE.md:783](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [EXPERIENCE.md:785](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md)), while the updated PRD journey and AD-8 require a second Eligible Approver for the regenerated version ([prd.md:66](../../../prds/prd-agents-2026-06-23/prd.md), [ARCHITECTURE-SPINE.md:190](../ARCHITECTURE-SPINE.md), [ARCHITECTURE-SPINE.md:192](../ARCHITECTURE-SPINE.md)). The UX also still renders queue position ([EXPERIENCE.md:528](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [EXPERIENCE.md:831](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md)) despite AD-24 forbidding an ordinal V1 position ([ARCHITECTURE-SPINE.md:306](../ARCHITECTURE-SPINE.md)); its Known gaps still call the regeneration exclusion provisional, `ConversationPosting` unresolved, and `CurrencyMismatch` absent after all three were resolved ([EXPERIENCE.md:275](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [EXPERIENCE.md:857](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [EXPERIENCE.md:860](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md), [EXPERIENCE.md:861](../../../ux-designs/ux-agents-2026-06-23/EXPERIENCE.md)).
- **Why it matters:** PRD NFR-13 declares the final UX spines binding. A UI team can follow valid-looking UX instructions and violate the current architecture on segregation of duties, accessibility ownership, and queue privacy.
- **Disposition:** **Autofix the UX source or publish and cite one explicit superseding reconciliation addendum before finalization.** Update the journey to name the second approver for the regenerated version, retain exactly one persistent live-region owner, remove ordinal queue-position rendering, and close or rewrite the stale Known-gap rows.

## Medium

### M1 — The structural seed omits the data-handling state the normative rules just added

- **Evidence:** AD-2 assigns the record/version/tightening diff to `ProviderCatalog` and the accepted version/decision to `TenantProviderEnablement` ([ARCHITECTURE-SPINE.md:148](../ARCHITECTURE-SPINE.md)); AD-13 requires current and in-force versions on `ProviderAttempt` ([ARCHITECTURE-SPINE.md:230](../ARCHITECTURE-SPINE.md)). The structural class diagram exposes none of those fields ([ARCHITECTURE-SPINE.md:626](../ARCHITECTURE-SPINE.md), [ARCHITECTURE-SPINE.md:636](../ARCHITECTURE-SPINE.md), [ARCHITECTURE-SPINE.md:671](../ARCHITECTURE-SPINE.md)).
- **Why it matters:** Teams using the structural seed can produce schemas that cannot enforce or evidence the detailed AD rules.
- **Disposition:** **Autofix.** Add the data-handling record/version and tightening classification to `ProviderCatalog`, accepted version/decision to `TenantProviderEnablement`, and current/in-force data-handling versions to `ProviderAttempt`.

## Low

### L1 — AD-13's traceability header does not bind the source requirements introduced by its new rule

- **Evidence:** The new provider-attempt evidence paragraph implements PRD FR-4/FR-5 data-handling behavior ([ARCHITECTURE-SPINE.md:230](../ARCHITECTURE-SPINE.md)), but AD-13's `Binds` list begins at FR-8 and omits FR-4/FR-5 ([ARCHITECTURE-SPINE.md:226](../ARCHITECTURE-SPINE.md)).
- **Disposition:** **Autofix.** Add FR-4 and FR-5 to AD-13's `Binds` list.

## Checklist conclusion

- **Real divergence points:** not yet complete because C1 and H1–H3 permit incompatible implementations.
- **Rules enforce their stated prevention:** generally strong; C1 makes one new rule impossible to exercise.
- **Deferred/open coverage:** explicit and bounded; no new silent V1 dimension found.
- **Spec/UX coverage:** broad, but source synchronization is not yet complete.
- **Brownfield/environmental envelope:** explicitly covered by AD-16/AD-17 and the structural seed; no new omission found in this update.
- **Named technology currency:** intentionally left to the configured verified-current reviewer; no rubric finding duplicates that independent lens.

## Post-fix rerun — 2026-09-12

**Verdict:** **FAIL (substantially improved)** — all six original findings are resolved and deterministic lint remains clean, but the synchronized epics still do not own the new behavior, one readiness invalidation rule can retain evidence past a data-handling change, and the structural seed contains two duplicate fields. Remaining counts: **Critical 0, High 1, Medium 1, Low 1**.

### Original-finding disposition

- **C1 resolved:** matrix v3's `DataHandlingAcceptance` row no longer requires `LR-PROVIDER` (`launch-readiness-register.md:171`).
- **H1 resolved:** AD-12 and AD-22 explicitly make it the tenth lock-bearing family with justification/change evidence (`ARCHITECTURE-SPINE.md:234-236`, `ARCHITECTURE-SPINE.md:308`), and NFR-13 now names acceptance/decline (`launch-readiness-register.md:243`).
- **H2 resolved:** the spine and dependency register now bind immutable identity-by-id under explicit `EXT-PARTIES-1` Branch A/Branch B behavior (`ARCHITECTURE-SPINE.md:150`, `ARCHITECTURE-SPINE.md:190`, `external-dependency-register.md:69`, `external-dependency-register.md:95-98`).
- **H3 resolved:** UX now has one persistent live-region owner, a second Eligible Approver, no ordinal position, and closed gap text (`EXPERIENCE.md:123-133`, `EXPERIENCE.md:529`, `EXPERIENCE.md:784-794`, `EXPERIENCE.md:832`, `EXPERIENCE.md:858-861`).
- **M1 resolved:** the structural seed carries the required data-handling fields (`ARCHITECTURE-SPINE.md:646-713`), subject to L2 below.
- **L1 resolved:** AD-13 now binds FR-4 and FR-5 (`ARCHITECTURE-SPINE.md:240`).

### High — H4: synchronized epics do not assign the new data-handling or regeneration-segregation behavior to implementable stories

- **Evidence:** Story 5.3 records only that the catalog advances `DataHandlingVersion`, omitting the four-field record, cumulative tightening decision/diff, tenant accept/decline command, current/accepted versions, revisions, grace deadline, and recovery-gate proof (`epics.md:1361-1364`, `epics.md:1395-1401`). Story 6.4's prepared descriptor omits the current and in-force data-handling versions (`epics.md:1976-1979`). Stories 7.3/7.4 do not require the regeneration-time second-Approver guard, the atomic authorization event, or `RegenerationRequestedByPartyId` (`epics.md:2377-2395`, `epics.md:2426-2444`). Story 6.7 still scopes `EXT-CONV-UI-1` as action/decorator/callability rather than the four artifacts plus focus-return contract (`epics.md:2145-2149`).
- **Why it matters:** The architecture is a build substrate, but the independently implementable stories can close while omitting the very decisions added by this update.
- **Disposition:** **Autofix the epics.** Extend Story 5.3 to own the full data-handling record, tenant decision/grace state, `DataHandlingAcceptance` command and no-`LR-PROVIDER` recovery test; extend Story 6.4 evidence/descriptor coverage; extend Stories 7.3/7.4 with the pre-Provider second-Approver guard and durable requester check; and bind Story 6.7 explicitly to all four `EXT-CONV-UI-1` artifacts plus focus return.

### Medium — M2: `LR-PROVIDER` invalidation does not name tenant acceptance or grace expiry

- **Evidence:** `ProviderReadinessResult` is now a tenant join carrying current/in-force versions and `GraceExpiresAt` (`launch-readiness-register.md:210-212`), but the normative `LR-PROVIDER` invalidation row names catalog/adapter/secret/pricing/limit/capability changes only (`launch-readiness-register.md:103`). No rule caps readiness `ValidUntil` at an active `GraceExpiresAt`.
- **Why it matters:** A previously passing readiness observation can remain apparently current after an explicit decline, a tenant acceptance change, or grace expiry even though AD-10 requires immediate blocking.
- **Disposition:** **Autofix.** Invalidate `LR-PROVIDER` on the data-handling record/version, tenant acceptance/decline revision, and grace state; require a result issued during grace to have `ValidUntil <= GraceExpiresAt`.

### Low — L2: remediation introduced duplicate structural-seed members

- **Evidence:** `TenantProviderEnablement` lists `GraceExpiresAt` twice and `ProposalVersion` lists `RegenerationRequestedByPartyId` twice (`ARCHITECTURE-SPINE.md:664-665`, `ARCHITECTURE-SPINE.md:700-701`).
- **Disposition:** **Autofix.** Remove one duplicate of each field and extend the linter or a focused structural-seed check to reject duplicate class members.

### Rubric conclusion

The spine now resolves the original architecture/UX/PRD divergence points, preserves the operational/environmental envelope, and leaves no newly silent V1 dimension. Finalization remains blocked only by H4; M2 and L2 are clear fixes that should land in the same pass.

## Focused post-fix recheck — 2026-09-12

**Verdict:** **FAIL** — deterministic lint remains clean and five of the six H4 sub-fixes plus M2 and L2 are closed, but Story 6.4 still lacks implementable ownership of the attempt-bound data-handling evidence. Remaining counts: **Critical 0, High 1, Medium 0, Low 0**.

### Remaining High — H4a: Story 6.4 still omits attempt-bound data-handling versions

- **Evidence:** Story 5.3 now owns the governed data-handling record/version, lock-bearing accept/decline command, revision checks, cumulative grace rule, blocking behavior, and focused evidence (`epics.md:1361-1374`, `epics.md:1405-1411`). However, Story 6.4's deterministic prepared-attempt descriptor still lists Provider/model/capability/limit/policy/cost/fingerprint data without `CurrentDataHandlingVersion` or `InForceDataHandlingVersion` (`epics.md:1984-1989`), and its `OwnedClauses`, test inventory, and negative evidence likewise omit that AD-13 evidence (`epics.md:2018-2026`).
- **Why it matters:** Story 6.4 can be implemented and accepted while a Provider attempt lacks the immutable current/in-force versions needed to prove which terms governed that call or fail a changed attempt closed.
- **Required change:** Add `CurrentDataHandlingVersion` and `InForceDataHandlingVersion` to the Story 6.4 descriptor acceptance criterion and explicitly own/test their persistence plus changed-version fail-closed behavior in its evidence manifest.

### Closed residuals

- **Regeneration segregation:** Story 7.3 now requires a second eligible Approver before Provider work, atomically records `RegenerationAuthorized`, persists `RegenerationRequestedByPartyId`, and owns focused positive/negative evidence; Story 7.4 excludes that requester from approval (`epics.md:2392-2405`, `epics.md:2417-2423`, `epics.md:2442-2445`).
- **`EXT-CONV-UI-1`:** Story 6.7 now names all four artifact kinds, the persistent region as sole live-region owner, and deterministic focus return (`epics.md:2168-2171`).
- **`LR-PROVIDER` invalidation:** the register now invalidates on tenant acceptance or decline and data-handling grace expiry (`launch-readiness-register.md:103`).
- **Structural duplication:** `TenantProviderEnablement.GraceExpiresAt` and `ProposalVersion.RegenerationRequestedByPartyId` each occur once in their owning class; no consecutive duplicate structural member remains (`ARCHITECTURE-SPINE.md:659-665`, `ARCHITECTURE-SPINE.md:693-700`).

## Final focused recheck — 2026-09-12

**Verdict:** **PASS** — deterministic lint passes and every previously reported Critical, High, Medium, and Low finding is closed. Remaining counts: **Critical 0, High 0, Medium 0, Low 0**.

- **H4a resolved:** Story 6.4's deterministic descriptor now binds `CurrentDataHandlingVersion` and `InForceDataHandlingVersion` (`epics.md:1986-1989`); the evidence manifest explicitly owns FR-5/FR-24 and AD-13 attempt-bound data-handling evidence and names `ProviderAttemptDataHandlingVersionEvidenceTests` (`epics.md:2018-2026`).
- **Gate conclusion:** The update now assigns all changed behavior to implementable stories, keeps readiness invalidation and the structural seed aligned, and introduces no remaining material incompatibility in the reviewed 2026-09-12 scope.

## Final current-tree rubric recheck — 2026-09-12

**Verdict:** **PASS** — the adversarial reconciliation and final source synchronization close every reported divergence, preserve implementation ownership, and leave deterministic lint clean. Remaining counts: **Critical 0, High 0, Medium 0, Low 0**.

### Resolved — FCR-H1: administrative-retry availability

- FR-18's exhausted-budget exit list no longer includes administrative retry, and its authorization row now permits retry only while the shared attempt budget and both time bounds allow it (`prd.md:457`, `prd.md:463-469`, `prd.md:513`). This matches AD-5, UX, and Story 7.7.

### Resolved — FCR-M1: Story 5.3 / `EXT-PROVIDER-1` historical ruling

- The PRD, active Epics amendment, Story 5.3, and authoritative dependency register now all preserve Product's 2026-09-10 Branch-B ruling: historical Story 5.3 executed no `EXT-PROVIDER-1` seam, created no dependency non-conformance, and did not commit or advance that dependency (`prd.md:871`, `epics.md:361`, `epics.md:1351`, `external-dependency-register.md:209-215`). The separate `NC-5.3-PLATFORM-CATALOG-SCOPE` migration non-conformance remains correctly open.

### Rubric conclusion

The current architecture remains enforceable, covers the PRD/UX capabilities and operational envelope, and assigns the reconciled Party, mirror, data-handling, regeneration, UI-seam, retry, and lock behavior to focused stories and evidence. No material current-tree incompatibility remains in the reviewed 2026-09-12 scope.

## Data-handling tightening and activation-grace recheck — 2026-09-12

**Verdict:** **PASS** — deterministic lint passes and the current PRD/UX clarification remains coherent with the architecture, Story 5.3, and readiness register. Remaining counts: **Critical 0, High 0, Medium 0, Low 0**.

- **Tightening contract:** PRD and UX now use the same cumulative comparison against the last accepted record as AD-10: retention cannot lengthen, training use moves only toward opted out, the normalized region set only narrows, and any contractual-reference change, undeclared change, or incomparable value cannot receive grace (`prd.md:203-207`, `EXPERIENCE.md:318-331`, `ARCHITECTURE-SPINE.md:216-220`).
- **Activation under grace:** PRD and UX require acceptance before first activation, then require fresh acceptance after a version change unless a valid declared-tightening grace remains; the activation gate explicitly accepts current acceptance or that grace (`prd.md:205`, `prd.md:217`, `EXPERIENCE.md:224`, `EXPERIENCE.md:320`). This matches `LR-PROVIDER`'s complete-record plus current-acceptance-or-unexpired-grace requirement (`launch-readiness-register.md:210-212`).
- **Implementation ownership:** Story 5.3 owns the versioned four-field record, accept/decline command, cumulative non-extending grace, fail-closed cases, and focused aggregate/UI/deadline evidence (`epics.md:1361-1374`, `epics.md:1401-1411`); Story 6.4 continues to bind both current and in-force versions into each Provider attempt.
