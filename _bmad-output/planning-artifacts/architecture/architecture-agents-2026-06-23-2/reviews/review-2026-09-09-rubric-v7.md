---
name: Hexalith Agents spine review
type: architecture-spine-review
target: ARCHITECTURE-SPINE.md (updated 2026-09-09, status final, AD-1..AD-31; round-4 Update pass applied same day, uncommitted in the working tree, diffed against HEAD 4ec626d)
date: 2026-09-09
lens: rubric walker v7
---

# Rubric Walker Review — 2026-09-09 (v7)

## Verdict

**PASS WITH FINDINGS** — round 4 genuinely closes all three round-3 Criticals and all three round-3 Highs assigned to this lens (verified against live text, not trusted from the update report), but the round's own new material introduces one fresh High (a security-sensitive operation described with a term of art — "lock-bearing" — that its own governing enumeration does not recognize) plus small Prevents-line coverage gaps, and it leaves one round-3 Medium (the AD-2/AD-10 `EntryMissing` carve-out wording asymmetry) untouched and still open.

## What's already closed (independently re-verified against the live text, not trusted from the round-3/round-4 reports)

- **C-1 (Deferred-table/AD-22 `LegalHoldRelease` contradiction):** the "Two-person rule on legal-hold release" row is gone from `Deferred Beyond V1` entirely (confirmed by re-reading the full table and by `grep -in "two-person" ARCHITECTURE-SPINE.md`, which returns nothing). AD-22's second-party `LegalHoldRelease` rule stands alone with no contradicting sibling. Genuinely closed.
- **C-2 (AD-7 block-clear authority):** AD-7's Rule now reads "...is audited, and is mirrored to the Conversations participant list through the removal seam; a block is cleared only by the specific authority that set it, re-evaluated as currently held at clear time (a Party who has since lost the Facilitator role cannot clear it) — a Conversation Facilitator can never clear a block the Tenant Agent Administrator set — or by the Tenant Agent Administrator, while a system-set `ExternallyRemoved` block, having no setting Party, stays clearable by the Tenant Agent Administrator or any current Facilitator." This now matches the pre-existing "Second-update membership reconciliation" paragraph below it (which already said "the Facilitator cannot clear an administrative block"), removing the self-contradiction round 3 found between AD-7's main Rule and its own second-update note. Genuinely closed.
- **C-3 (register `LR-AUDIT-PROTECTION-DELETION` never actually edited):** `launch-readiness-register.md` line 108 now reads "Includes evidence that a `LegalHold` **or a `DeletionRequest`** either propagates to (or has a confirmed accepted-risk waiver for) the posted copy of held **or erased** content in Conversations..." — the register file itself was edited, not just the spine's cross-reference to it. Genuinely closed.
- **H-1 (AD-12's enumeration silent on Party state):** AD-12's re-read list now reads "Agent lifecycle, tenant enablement, the per-Conversation block, `hexa`'s own linked Party state per AD-7, and the per-tenant kill switch" — a literal implementation of AD-12's own words now does check Party state before every side effect. Genuinely closed (not merely a citation fix this time).
- **H-2 (AD-12/AD-5 silent on disabled-Agent `PostingFailed`):** AD-12's disabled-Agent clause now adds "...and, on the same terms as the kill switch below, a `PostingFailed` automatic or administrative retry is suspended with its retry-window clock paused while the Agent is disabled, resuming on re-enablement [ASSUMPTION ARCH-A-11]"; the kill-switch clause was generalized to "on the same terms as the disabled-Agent suspension above; the two causes accrue against the same `PausedDuration` field"; AD-5's deadline formula now reads "AD-12's `PausedDuration`, since AD-12 pauses this clock while the tenant kill switch is pulled or the Agent is disabled." All three sites agree on one shared, dual-trigger `PausedDuration` field. Genuinely closed.
- **H-3 (AD-30 `Platform` freshness source ambiguous):** now reads "...populated only by the Agents API ingress after a fresh role check against the `system` tenant's own Tenants-projection — the authoritative source for this one non-tenant-scoped FR-19 role, never the envelope/target tenant's projection used for `User`/`Administrator` — on the same never-cached, never-token-embedded terms..." Unambiguous now. Genuinely closed.
- Medium (Stack table `Hexalith.EventStore` gitlink): now `e302432c`, confirmed against the actual committed submodule pointer (`git submodule status` reports `e302432ca6daf3aa0436c3c0011f7baa551bb449`, i.e. `v3.103.0-21-ge302432c`). Genuinely closed.
- Medium (Consistency Conventions "Provider attempt fingerprint" row plain-SHA-256 vs. AD-29's HMAC rule): row now reads "HMAC-SHA-256 under the per-tenant `DigestKey`." Closed.
- Low (`AuditInspection` classDiagram `Mode` field): now `Scope`, matching AD-22's prose ("scoped to a named Conversation or a case"). Closed.
- Frontmatter/index bookkeeping: `architecture_assumption_index_version: 3`, the rendered `ARCH-A-INDEX-3` (both the heading and the inline cross-reference sentence), and the Architecture Assumptions table's row count (`ARCH-A-1`..`ARCH-A-12` = 12 rows) all agree. No drift here.

## High

### H-1 (new this round) — AD-22 calls `DigestKey` rotation "a lock-bearing operation" but it is not one of AD-12's enumerated lock-bearing families, nor a family the register's `OperationGateMatrix` names

**Quote (AD-22, new sentence this round):** "The per-tenant `DigestKey` that keys every sensitive-content digest under AD-29 is never rotated for a tenant holding any unerased protected content, since rotation would silently break every stored digest's comparability against content it must keep matching; rotation is permitted only after full-tenant erasure or an accepted-risk waiver, and **is itself a justified, audited lock-bearing operation under `EXT-SECRETS-1`** [ASSUMPTION ARCH-A-12]."

**Quote (AD-12, the closed enumeration "lock-bearing" is defined against):** "the lock-bearing subset is `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold` (also `LegalHoldRelease`), `ExportRequest` (also `ExportDownload`), `DeletionRequest`, `ProviderCatalogMutation` (also `TenantProviderEnablement`), `AgentSetupMutation` (also `TenantKillSwitch`, and the block/clear commands that set or clear `hexa`'s Conversation block), and `AgentActivation`; ... the family vocabulary is owned by the register matrix version."

**Quote (AD-22, restating the same closed set):** "the reject-on-missing-or-whitespace-justification rule applies to all nine AD-12 lock-bearing families — `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold` (also `LegalHoldRelease`), `ExportRequest` (also `ExportDownload`), `DeletionRequest`, `ProviderCatalogMutation` (also `TenantProviderEnablement`), `AgentSetupMutation` (also `TenantKillSwitch`), and `AgentActivation` — with no exempt family."

**Why it's wrong:** "Lock-bearing" is a defined term of art in this spine: AD-12 closes the set at exactly nine named families and states the family vocabulary is owned by the register's `OperationGateMatrix`; AD-22 itself restates that closed nine-family list verbatim in the very same AD. `launch-readiness-register.md`'s `OperationGateMatrix` (checked directly) enumerates `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, `AgentCallAcceptance`, `ProviderInvocation`, `ConversationPosting`, `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, `DeletionRequest`, `ProposalEdit` (v2), `AuditInspection` (v2), `TenantKillSwitch` (v2), `TenantProviderEnablement` (v2) — no `DigestKeyRotation` family anywhere, and no mention of "digest" or "rotation" at all outside `LR-SECRETS`'s generic evidence-contract prose. So when AD-22 calls `DigestKey` rotation "a justified, audited lock-bearing operation," it invokes a UI/BFF advisory-locking and per-session one-pending-command mechanism (AD-12's actual definition of "lock-bearing") for an operation that is in none of the nine families the mechanism is defined over, is not gated by any named `OperationGateMatrix` entry, and is described as running "under `EXT-SECRETS-1`" (a Platform-owned secrets custodian, per AD-9/AD-16), not obviously an `Hexalith.Agents` command at all. Two teams implementing this literally will diverge: one will bolt rotation onto an existing family (most plausibly `TenantBudgetUpdate`-style `TenantGovernancePolicy` mutation, or a new tenth family), the other will treat "lock-bearing" as loose prose meaning only "audited," wire no advisory lock and no register gate at all, and ship a security-sensitive key-lifecycle operation with none of AD-12's or AD-17's actual controls. This is exactly the kind of unresolved divergence point the spine exists to fix at this altitude.

**Fix:** Either (a) add `DigestKeyRotation` as a tenth named family to AD-12's lock-bearing subset and to `launch-readiness-register.md`'s `OperationGateMatrix`/`LockBearingFamilies` inventory in the same change, with its own gate-set row, or (b) if rotation is meant to be a Platform/`EXT-SECRETS-1`-side procedure outside the Agents command surface entirely (consistent with "under `EXT-SECRETS-1`"), reword AD-22's sentence to drop the term "lock-bearing operation" and instead state plainly what governs it (e.g., "a justified, audited `EXT-SECRETS-1` custodian procedure, out of the AD-12 lock-bearing family model") so it stops appearing to claim membership in a set it is not part of.

## Medium

### M-1 (carried forward from round 3, unresolved by round 4) — AD-10's `EntryMissing` carve-out claims exact equivalence with AD-2's carve-out while adding a restriction AD-2's own wording does not contain

**Quote (AD-2):** "...except that a query scoped to an Agent's own previously-snapshotted or currently-selected `ProviderId`/`ModelId` may instead report `EntryMissing` or a reason-coded `Blocked` `ProviderReadinessResult` inside `AgentReadinessStatus`, per AD-10 — the one named carve-out from the absent-key rule."

**Quote (AD-10):** "...a by-key query for a non-enabled or absent entry returns the not-found response and `EntryMissing` is reported only for the Agent's own currently-selected entry, or an in-flight interaction's previously-snapshotted entry, inside `AgentReadinessStatus`/`ProviderReadinessResult` — matching AD-2's carve-out exactly, no wider."

**Why it's wrong:** AD-10 asserts its own wording is an exact, no-wider match for AD-2's — but AD-2's "previously-snapshotted" is unqualified (any interaction that once snapshotted the entry, regardless of whether that interaction is still open), while AD-10 restricts the same phrase to "an in-flight interaction's previously-snapshotted entry." A terminal interaction's stored snapshot of a now-disabled/removed entry is "previously-snapshotted" under AD-2's literal words but is excluded under AD-10's "in-flight" qualifier — the two sentences describe different sets while each claims to restate the other. This is the round-3 rubric finding this pass was asked to independently re-verify; the round-4 diff touches neither AD-2 nor AD-10, so it remains open exactly as round 3 left it.

**Fix:** Pick one scope and make both ADs say it: either add "for an in-flight interaction" to AD-2's carve-out clause, or drop "in-flight interaction's" from AD-10's and replace with AD-2's own unqualified "previously-snapshotted," so "matching AD-2's carve-out exactly, no wider" is literally true.

## Low

### L-1 — AD-22's Prevents line was not extended when the `DigestKey` non-rotation rule was added

**Quote (AD-22 Prevents, unchanged this round):** "indefinite sensitive-content retention, unaudited or unverifiable exports, apparent deletion that leaves readable copies, undecidable erasure, or rewriting immutable EventStore history."

**Why it's wrong:** The new Rule sentence (see High H-1 above) exists specifically to prevent a distinct divergence — silent breakage of stored digest comparability from an ad hoc `DigestKey` rotation while erased/unerased content still depends on it matching. None of the five items in AD-22's Prevents line name this failure mode; a reader using Prevents as the AD's table of contents (as the rubric expects) would not learn this rule exists or why.

**Fix:** Add a sixth Prevents item, e.g. "...or a `DigestKey` rotation silently invalidating stored content digests while unerased protected content still depends on them matching."

### L-2 — AD-7's Prevents line was not extended when the block-clearing-authority rule was tightened

**Quote (AD-7 Prevents, unchanged this round):** "anonymous system authors, caller-authored AI messages, duplicated Party PII, or an Agent silently re-joining a Conversation a Facilitator removed it from."

**Why it's wrong:** The C-2 fix (see "What's already closed" above) exists to prevent a Conversation Facilitator from clearing a block the Tenant Agent Administrator deliberately set — an authorization-bypass concern distinct from "silently re-joining a Conversation a Facilitator removed it from" (which describes the system's own re-add-on-detected-absence behavior, not a human overriding another human's deliberate block). The Prevents line still only names the latter.

**Fix:** Add an item naming the authorization-bypass case, e.g. "...or a Conversation Facilitator clearing a block the Tenant Agent Administrator set."

### L-3 — the Consistency Conventions "Identity" row's list of identities Agents owns omits `InspectionId` (and other AD-29-derived ids)

**Quote (Consistency Conventions, "Identity" row, unchanged this round):** "Agents owns `AgentId`, `AgentInteractionId`, `AttemptId`, `ProposalVersionId`, `ProviderId`, `ModelId`, `HoldId`, `ExportId`, `DeletionRequestId`; Parties owns `PartyId`; Conversations owns `ConversationId`..."

**Quote (AD-2):** "`AuditInspection` (`TenantId`, `InspectionId`; compliance inspections, never under an interaction key)..."

**Why it's wrong:** AD-2 names `InspectionId` as an aggregate-identity component Agents owns, but the Consistency Conventions row that is supposed to be the canonical cross-reference for "who owns which identity" omits it (along with `GateId`, `ObservationId`, and `SampleId`, all AD-29-derived Agents-owned ids). This is pre-existing (not touched by round 4) but is exactly the kind of table/AD cross-reference the rubric asks to check, and a reader relying on this row alone would not know `InspectionId` is Agents-owned.

**Fix:** Either extend the row to list the remaining Agents-owned ids (`InspectionId`, `GateId`, `ObservationId`, `SampleId`) or reword the row to say it is illustrative rather than exhaustive, so it stops reading as a closed inventory that AD-2/AD-29 then contradict by addition.

## Rubric checklist coverage note

- Divergence points for the level below: the round-4 edits close all three round-3 Criticals and the three Highs assigned to this lens without introducing a new Critical; the one new High (AD-22/AD-12 "lock-bearing" mismatch) is a fresh, self-contained divergence point, not a re-opening of a prior one.
- Deferred Beyond V1: re-read in full; no row lets two units diverge and no row contradicts a current AD's Rule (the one contradiction found in round 3 — the legal-hold-release row — is gone).
- Stack table: the one item this round specifically touched (`Hexalith.EventStore` gitlink) is now accurate; no other Stack entries were touched and none read as implausible.
- Brownfield ratification: nothing in this round's edits reads as invented rather than ratified; all five touched ADs amend existing, previously-adopted rules rather than introducing new unratified claims about existing code.
- Initiative-altitude dimension completeness: deployment/environments (AD-16), infra/provider strategy (AD-9/AD-10, `EXT-PROVIDER-1` deliberately deferred with stated reasoning), and operations (AD-17, AD-23, AD-24) all remain decided-or-explicitly-deferred; no dimension was left silent by this round's edits.
- Internal consistency: AD-5's generalized `PausedDuration` formula now parses correctly against AD-12's generalized text (both name the same dual-trigger field); AD-2's `ConversationAgentState` description still matches AD-7's clearing rule (it defers to AD-7 by name — "rejecting states on the same terms per AD-7" — rather than restating and risking drift, which is consistent, not contradictory); the frontmatter `architecture_assumption_index_version: 3` and the rendered `ARCH-A-INDEX-3` agree with each other and with the Architecture Assumptions table's 12 rows.
