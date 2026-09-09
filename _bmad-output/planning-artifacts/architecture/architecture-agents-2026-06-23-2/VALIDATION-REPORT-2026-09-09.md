---
name: Hexalith Agents spine validation
type: architecture-spine-validation
target: ARCHITECTURE-SPINE.md (updated 2026-09-09, status final, AD-1..AD-31)
repository_state: main @ 81ebe3d (2026-09-09)
date: 2026-09-09
lint: 0 findings
lenses:
  - rubric walker (good-spine checklist)
  - verified-current (configured)
  - adversarial divergence (configured)
  - brownfield drift (ad hoc)
  - security / data integrity (ad hoc)
---

# Spine Validation Report — 2026-09-09

## Gate verdict

**FAIL, fixable.** This is the first validation run against the spine as amended today (AD-27..AD-31 added, AD-2/AD-4/AD-10/AD-17/AD-20..AD-22 revised) to close yesterday's sprint change proposal. The amendment closed most of what the 2026-09-08 validation flagged, and no lens found the spine internally incoherent at scale — but adversarial review found the spine's own text now contradicts itself on one load-bearing point (catalog absent-key vs. `EntryMissing` reporting) and leaves the general command idempotency-fingerprint mechanism undefined, which is enough on its own to block Story 5.3/5.5 and every command handler built against AD-29 today. Separately, the implementation debt the sprint change proposal already catalogued (DW-2 and DW-4) is unresolved in code as of this run — expected, since that debt is scheduled for stories still ahead in the backlog, not a new finding, but worth re-confirming since it gates when Story 5.3's ProviderCatalog rework can be marked conformant.

| Lens | Verdict | Notable findings (of full set — see review file) | Review file |
| --- | --- | --- | --- |
| Rubric walker | PASS WITH FINDINGS (0 critical / 1 high / 2 medium / 3 low-info) | AD-30 vocabulary reopened in epics.md | `reviews/review-2026-09-09-rubric-v2.md` |
| Verified-current | PASS WITH FINDINGS (0 critical / 1 high / 1 medium / 2 low) | Fluent UI Blazor pinned to a prerelease | `reviews/review-2026-09-09-verified-current-v2.md` |
| Adversarial divergence | FAIL (2 critical, 1 high, 2 medium, 2 low reported) | AD-2/AD-10 textual contradiction; AD-29 fingerprint gap | `reviews/review-2026-09-09-adversarial-divergence-v2.md` |
| Brownfield drift | FAIL TO RATIFY (code unchanged since the 2026-09-09 sprint change proposal) | AD-2/AD-4/AD-29/seed still non-conformant in code | `reviews/review-2026-09-09-brownfield-drift-v2.md` |
| Security / data integrity | PASS WITH FINDINGS (0 critical, 1 high, 4 medium reported) | Legal-hold doesn't propagate to Conversations; justification-gate coverage gap | `reviews/review-2026-09-09-security-data-integrity-v2.md` |

Deterministic lint (`lint_spine.py`): 0 findings — no placeholders, no duplicate `AD` ids, every `AD` carries Binds/Prevents/Rule, Stack versions pinned.

## Critical — close before Story 5.3/5.5 or any new command handler is built against AD-29

### C-1 AD-2 and AD-10 contradict each other on catalog absent-key behavior
*Found by adversarial.* AD-2's Rule ends: "a query for a key outside the caller's tenant, or for a platform entry not enabled for it, returns **exactly** the absent-key response" — no exception is stated. AD-10's Rule says tenant-facing readiness is the platform entry joined with `TenantProviderEnablement`, and "a by-key query for a non-enabled or absent entry returns the not-found response **and `EntryMissing` is reported only for the Agent's own selected entry inside `AgentReadinessStatus`**." AD-10 is carving an exception AD-2's own wording forbids. A builder who takes AD-2 literally makes `AgentReadinessStatus` structurally unable to ever report `EntryMissing` for the Agent's own entry — it would have to return the generic absent-key response like any other caller, which is exactly the behavior AD-10 exists to distinguish from every other blocked reason.
**Fix:** amend AD-2's closing sentence to name the one exception explicitly ("...returns exactly the absent-key response, except that `AgentReadinessStatus` reports `EntryMissing` for the Agent's own selected entry per AD-10") so the two ADs read as one rule instead of two competing ones.

### C-2 AD-29 defines the fingerprint for provider attempts but not for general commands
*Found by adversarial.* AD-29 fully specifies the identity canonicalizer and the AD-13 provider-attempt fingerprint, but never states how an arbitrary command payload (`TenantBudgetUpdate`, `ProposalEdit`, `LegalHold`, etc.) maps to canonicalizer "components" for the idempotency fingerprint every other command handler needs. Two compliant handler implementations can legitimately fingerprint the same retried command differently (a false conflict) or two different commands identically (a false replay) — the exact hole AD-29 exists to close, reopened one level down for every command outside the one path it fully specifies.
**Fix:** extend AD-29 with a general rule for command-fingerprint component selection (e.g., "every field on the command's public contract except correlation/causation/timestamp metadata, in declared field order") so a builder cannot choose ad hoc.

## High — close before the consuming stories enter ready-for-dev

| # | Finding | Source | Note |
| --- | --- | --- | --- |
| H-1 | `AD-30` names exactly four principal kinds (`User`, `Administrator`, `Platform`, `Workflow`); `epics.md` Story 5.4 (line 1360) tells implementers to pick from `User`, `Platform`, `Service`, `System` — the divergence AD-30 exists to prevent has already reopened in the artifact stories are built from. | Rubric | Spine-consistent artifact defect — fix `epics.md`, not the spine. |
| H-2 | `ConfigurationVersion`, `InstructionsVersion`, and `ApproverPolicyVersion` are three distinct snapshot fields with no AD stating whether they're independent counters or which event types bump which; a plausible build collapses them, corrupting AD-8's "in-flight interactions keep their snapshot" and AD-15's "queryable by `ApproverPolicyVersion`." | Adversarial | Spine defect — add a clarifying clause to AD-4 or AD-8. |
| H-3 | Fluent UI Blazor is pinned at `5.0.0-rc.5-26219.1`, a release candidate (stable line is still 4.14.4; v5 RC5 shipped 2026-08-07 with no GA found). `external-dependency-register.md` has no entry tracking this prerelease-for-production risk, unlike every other named external dependency. | Verified-current | Spine/register gap — add an `EXT-*` or register row so the risk is tracked, not silently absorbed. |
| H-4 | `AD-4` requires `ConfigurationVersion` to increment on every accepted `Agent` configuration or lifecycle event; `AgentActivated`/`AgentDisabled` in `src/Hexalith.Agents/...AgentAggregate.cs` still carry no version argument. | Brownfield (tracked as DW-4) | Implementation debt, already on the backlog per the 2026-09-09 sprint change proposal — re-confirmed still open, not a new finding. |
| H-5 | `AD-29`'s single `AgentsIdentity` canonicalizer has no counterpart in code: five-plus separate identity helper classes (`AgentProposalIdentity`, `AgentResponsePostingIdentity`, `AgentProposalEditIdentity`, `AgentProposalRegenerationIdentity`, `AgentInteractionIdentity`) remain, unconsolidated. | Brownfield | Implementation debt, already tracked — re-confirmed still open. |
| H-6 | Structural seed section omits `Server/Aggregates/` and `Server/Application/Tools/`, but both still exist on disk (placeholder-only); `Hexalith.Agents.IntegrationTests` still doesn't exist in `test/` or the `.slnx`. | Brownfield | Implementation/seed mismatch, already tracked under Story 5.6 per the sprint change proposal. |

## Medium

| # | Finding | Source |
| --- | --- | --- |
| M-1 | AD-22's legal-hold/erasure regime governs only the Agents-side copy; its own text concedes posted Conversation Messages "remain governed by Conversations retention" with no propagation of an Agents hold into Conversations — a hold can read `Active` in Agents evidence while the disclosed posted content is independently purged. | Security |
| M-2 | AD-22's mandatory-justification clause lists 6 of AD-12's 9 lock-bearing command families, omitting `ProposalResolution`, `ProviderCatalogMutation`/`TenantProviderEnablement`, `AgentSetupMutation`/`TenantKillSwitch`, and `AgentActivation` — a kill-switch pull or catalog mutation could satisfy the letter of AD-22 with a blank justification. | Security |
| M-3 | AD-30 requires a "fresh" check hardening `Administrator`/`Platform` principals against stale JWT trust; the ordinary `User` principal's resolved FR-33 roles have no equivalent freshness requirement, and AD-12's re-read list omits role assignment — a cached role claim could outlive a mid-session revocation. | Security |
| M-4 | "PartyId reference only, never PII" is stated authoritatively only in AD-7; AD-2, AD-8, and AD-30 all carry Party-adjacent fields without restating it, leaving room for a builder to cache Party display data for UI convenience without literally violating any AD but AD-7. | Security |
| M-5 | AD-30's `OnBehalfOfPartyId` (charged by per-Party limits — the Approver at regeneration) and AD-21's `AdmitCall` ledger (naturally interaction-owner-keyed) leave it undefined who is rate-limited/released for a regeneration attempt; no release rule for a per-attempt charge is stated. | Adversarial |
| M-6 | `SecurityEventLog` is qualified "rate-bounded" in AD-2 with no numeric bound and no register cross-reference, unlike every other quantitative constraint in the spine (which states a number or points to `launch-readiness-register.md`). | Rubric |
| M-7 | Microsoft Agent Framework is now GA (since 2026-04-03; `Microsoft.Agents.AI` 1.19.0 / `Microsoft.Agents.AI.Workflows` 1.13.0) — more mature than the spine's "unselected/optional" framing implies. Not an error, but worth confirming the AD-9 deferral is a deliberate scoping choice rather than an outdated maturity read. | Verified-current |
| M-8 | AD-30's `actor:agentsProviderAdmin` extension key now matches code lexically, but the required HMAC-tag verification is still absent (plain string check only) — DW-2 remains open. | Brownfield (tracked) |

## Low

| # | Finding | Source |
| --- | --- | --- |
| L-1 | Kill-switch retry-window pause (AD-12) has no accounting rule, and AD-28's three instant kinds don't model pause/resume duration — two builds of the AD-5 15-minute `PostingFailed` retry bound can disagree after a pause. | Adversarial |
| L-2 | `ConversationAgentState`'s non-terminal-proposal index has no stated synchronization owner; automatic-mode `PostingFailed` terminality reads as self-contradictory in one edge case. | Adversarial |
| L-3 | AD-9/AD-14's log/telemetry rules govern Agents' own domain events, not generic HTTP/SDK auto-instrumentation around the outbound Provider call, where a resolved secret and raw content are briefly in memory at transport time. | Security |
| L-4 | MediatR (correctly unused by Agents) is commercially dual-licensed — a heads-up for the shared package catalog owner, not an Agents defect; `bunit` is pinned a few points behind current (2.9.0 vs 2.10.3). | Verified-current |
| L-5 | `[ASSUMPTION]` mechanism is sound (16 tags, each with an interim rule/owner/retirement condition, `RQ-1` as the release blocker), but `ARCH-A-2` (caller `PartyId` resolution) is disproportionately load-bearing since it underpins AD-30 for every command — worth prioritizing its retirement over the other 15. | Rubric |

## What already improved since 2026-09-08

Not re-litigated here in full, but worth naming: the prior report's two Criticals (execution-state content boundary, and `TenantId`/catalog-scope self-contradiction) are both closed — AD-27 now binds workflow/session execution state, and AD-2/AD-10 now commit to the platform-scoped catalog plus `TenantProviderEnablement` design the prior rubric lens had recommended. Several prior Highs (principal model, time authorities, deterministic identity, readiness registry grammar) are now AD-12/AD-28/AD-29/AD-17 respectively. The cost of committing to that design is this run's C-3-equivalent: the shipped Story 5.3 code hasn't migrated to it yet (H-4, H-5, H-6, M-8 above), which is the expected, already-tracked shape of "architecture moved, backlog migration still ahead" rather than a new defect.

## Recommendation

Two spine-text fixes (C-1, C-2) are small, mechanical, and safe to apply immediately without changing any decision already made — they only make explicit what AD-10 and AD-29 already implied. H-1 is a one-line `epics.md` correction, not a spine change. The remaining High/Medium items are either already-tracked implementation debt (re-confirmed, not new) or genuine spine refinements worth taking as new/amended `AD`s at the next Update pass rather than blocking anything in flight today.

Offered next step: roll C-1, C-2, and H-1 into a `bmad-architecture update` pass now (stable `AD` ids preserved, `epics.md` corrected in the same pass since it's the source this update would override) — or hold and address them together with the next planned amendment.
