# PRD Reconciliation — Architecture Spine Update — 2026-09-12

## Scope

- PRD authority: `../../prds/prd-agents-2026-06-23/prd.md` and its `.memlog.md`, both updated 2026-09-12.
- Target: `../ARCHITECTURE-SPINE.md`, updated 2026-09-12, `ARCH-A-INDEX-6`.
- Registers: `../../launch-readiness-register.md` and `../../external-dependency-register.md`, updated 2026-09-12.
- Focus: the 2026-09-09 PRD decisions, the seven Architecture/Architecture+Conversations gaps routed by the final UX reconciliation, and the 2026-09-12 Product decision on regeneration-requester eligibility.
- This review changed no source artifact.

## Verdict

**RECONCILED WITH THREE MATERIAL GAPS.** The update closes all seven gaps routed from the UX reconciliation at the architecture-contract level, and `ARCH-A-13` is mechanically retired correctly. It is not yet fully source-reconciled, however:

1. The new regeneration-requester exclusion makes PRD UJ-3 impossible as written and creates a no-Eligible-Approver path because neither FR-7 nor AD-8 applies the edit-time anti-stranding guard to regeneration.
2. The amended `EXT-CONV-UI-1` fourth artifact captures post-submit Agent Call status, but it does not preserve FR-13's distinct Conversation-level pending-proposal discovery surface for authorized Approvers.
3. Two dependencies requested by the 2026-09-09 PRD still have no authoritative register representation: the `EXT-PARTIES-1` record is absent, and `EXT-CONV-AI-1` still specifies six seams rather than including the requested seam 7.

The first two affect independently-built proposal/UI units and should be resolved before the related stories are treated as convergent. The third leaves `RQ-1`'s full dependency profile incomplete even though unretired assumptions currently fail the gate closed.

## Seven UX-Routed Gaps

| UX reconciliation item | Current disposition | Evidence |
| --- | --- | --- |
| 1. Classify `ConversationPosting` for advisory locking | **Closed** | AD-12 now states that workflow-only `ProviderInvocation`, `ConversationPosting`, and `SystemTimer` are outside browser-session advisory locking and rely on expected revisions and deterministic idempotency. |
| 2. Give administrative retry a valid operation family | **Closed** | AD-5 makes the user request lock-bearing `ProposalResolution`; after acceptance, lookup, revalidation, and posting activities declare `ConversationPosting`. The launch register contains both families with distinct gate sets. |
| 3. Resolve unsupported `CapacityQueued` position | **Closed** | AD-24 exposes durable `QueueId` and safe queued state but explicitly exposes no ordinal position in V1. This is compatible with PRD NFR-12, which requires a safe typed queue-or-reject outcome but no public position. |
| 4. Add `CurrencyMismatch` to readiness vocabulary | **Closed** | AD-10 and the launch register now include additive `ProviderReadinessReasonCode.CurrencyMismatch`; AD-21 defines the blocking tenant-currency comparison. This is compatible with FR-4/FR-28 and FR-23 additive enum evolution. |
| 5. Reconcile `EntryMissing` carve-out | **Closed** | AD-2, AD-10, and the launch register now allow it only for the Agent's current selection or an in-flight interaction's snapshot; terminal history uses immutable snapshot and authorized Audit Evidence. |
| 6. Resolve `ARCH-A-13` | **Closed mechanically; follow-through gap below** | PRD glossary and FR-7 now exclude the regeneration requester; AD-8 matches; the memlogs record the Product decision; the inline architecture assumption marker is gone; the row is retained as retired; the index increments from 5 to 6. |
| 7. Amend `EXT-CONV-UI-1` | **Closed for the UX request; incomplete against FR-13** | AD-31 and the external register now bind four artifact kinds, persistent post-dialog status, deterministic focus return, and one Agents provenance source. They omit FR-13's separate pending-proposal status entry, as finding H-2 explains. |

## Material Findings

### H-1 — Regeneration exclusion creates an impossible journey and an anti-stranding hole

**Sources**

- PRD UJ-3 has Anika request regeneration and then approve the regenerated version.
- The updated Eligible Approver definition and FR-7 approval rule now make the regeneration requester ineligible for that version.
- FR-7 guards call creation and edits against leaving no Eligible Approver, but has no equivalent regeneration-time guard.
- FR-16 permits an otherwise-authorized Approver to regenerate subject to the ceiling and fresh gates.
- AD-8 likewise names configuration, call, edit, and approval moments, but not a post-regeneration non-empty check.

**Impact**

The updated UJ-3 cannot execute. More importantly, a valid policy can yield one current Eligible Approver after the caller is excluded. That Party may request regeneration, become ineligible for the resulting version, and leave an empty Eligible Approver set after Provider cost has already been incurred. With the predicate used by every proposal action and by the FR-18 actor rows, the requester may also lose edit, reject, abandon, and further-regeneration authority, leaving only scheduled system resolution.

**Required reconciliation**

- Amend UJ-3 so one Approver requests regeneration and a different Eligible Approver approves it.
- Add a regeneration-time guard, parallel to the edit-time guard: refuse regeneration before Provider work unless at least one Eligible Approver will remain for the resulting version after excluding caller and regeneration requester.
- State explicitly whether the new exclusion is approval-only or applies to every action whose actor is `Eligible Approver`. The current glossary/FR-18 composition applies it to all such actions, while the recorded Product rationale names approval self-dealing.

Until those points land, `ARCH-A-13` is retired as an assumption, but the decision's downstream lifecycle consequences are not fully specified.

### H-2 — `EXT-CONV-UI-1` replaces rather than preserves the FR-13 Conversation proposal-status requirement

**Sources**

- PRD FR-13 requires pending proposal discovery for authorized Approvers through a pending count, queue, and **Conversation status entry**.
- PRD section 8 repeats that `EXT-CONV-UI-1` supplies a Conversation-level status entry showing pending-proposal state to authorized Approvers.
- Updated AD-31 and the external register's fourth artifact instead define a persistent region beside **Call hexa** for the caller-facing `submitted`, `authoritative-pending`, and terminal Agent Call outcomes after the dialog closes.

**Impact**

These are different audiences and states. Delivering the amended register literally gives the caller durable call feedback but does not give a resolved Approver the FR-13 Conversation-level indication that a proposal awaits action. The general Notifications convention still names a Conversation status entry, but no owner, payload, authorization predicate, or external artifact binds it.

**Required reconciliation**

Either make the fourth artifact explicitly serve both contracts—with separate, authorization-filtered call-status and pending-proposal views—or add a fifth artifact/slot for FR-13 proposal discovery. Its contract must apply AD-8's three-tier disclosure: full content only with current read access, existence/state only for a previously resolved Approver who lost access, and no disclosure to a Party never resolved.

### H-3 — The external dependency authority remains narrower than PRD section 8

**`EXT-PARTIES-1`**

The PRD requires an external record for the AI Party type and Agents Service Principal creation authorization (`A-27`), but the external dependency register has no `EXT-PARTIES-1` record, and the spine's External V1 Prerequisites table omits it. Consequently, the register's “all nine” summary is internally accurate but does not cover the PRD's ten-entry scope.

**`EXT-CONV-AI-1` seam 7**

PRD section 8 and A-28 request the message retraction/deletion/flag signal as seam 7. AD-6 says the external register additionally names it. The register's `RequiredArtifact` and compatibility contract still define only six seams and contain no retraction signal.

**Impact**

`RQ-1` is defined over the full section 8 dependency profile, but that profile cannot be evaluated solely from its declared authority. Current assumptions A-27/A-28 keep readiness from silently passing today, yet retiring either assumption could create a false closure because no matching authoritative record exists.

**Required reconciliation**

- Add `EXT-PARTIES-1` with owner/repository/artifact/target/date/verification/evidence/status/consumers, and add it to the spine prerequisite map.
- Amend `EXT-CONV-AI-1` with seam 7, or remove the current AD-6 claim and record an explicit Product disposition that V1 launches without it. Any material artifact amendment leaves the record `Uncommitted` until reaccepted.

## Source-Authority Corrections Worth Folding With The Above

These do not change the architecture behavior, but they currently misstate authoritative cross-document state:

- PRD FR-11 says AD-5 still gives automatic mode no retries; current AD-5 gives the automatic posting record the FR-18 bounded retry.
- PRD FR-2 says AD-7 carries no re-admission mirror; current AD-7 carries the clear-time re-admission mirror.
- PRD section 8 says the dependency register still carries an open Product decision for Story 5.3/`EXT-PROVIDER-1`; the register records Product's resolved Branch B decision dated 2026-09-10.

The dated section 8.1 divergence list is historical and need not be rewritten as current architecture, but these present-tense claims should be removed or dated so builders do not follow superseded guidance.

## `ARCH-A-13` Retirement Assessment

**Correctly retired as an assumption.** All required mechanics are present:

- Product authority is recorded in the PRD memlog on 2026-09-12.
- The PRD glossary and FR-7 carry the rule.
- AD-8 carries the same exclusion without `[ASSUMPTION ARCH-A-13]`.
- The retired row remains under stable ID `ARCH-A-13` and states its retirement evidence.
- `architecture_assumption_index_version` and the rendered identifier both advance from 5 to 6.

The retirement does not certify that all consequences of the adopted rule are complete. H-1 must still be reconciled before the regeneration/approval stories are implementation-ready.

## Compact Coverage Conclusion

Apart from H-1 through H-3, no material 2026-09-09 PRD invariant inspected here was lost by this update. The operation-family split, capacity non-disclosure, readiness vocabulary, tenant-safe `EntryMissing` scope, and Conversations UI ownership additions are compatible with FR-19, FR-21, FR-23, FR-28, NFR-12, and NFR-13. The updated launch register remains fail-closed and `CurrencyMismatch` is a valid additive public enum member.

## Post-Fix Verification Addendum — 2026-09-12

This addendum rechecks only H-1 through H-3 against the current PRD, Architecture Spine, and external dependency register. It supersedes the original three-gap verdict for current-state handoff; no source artifact was changed by this verification.

### Current verdict

**PARTIALLY CLOSED — H-1 and H-2 are closed; H-3 remains open in the external dependency register.**

### H-1 — Closed

- PRD UJ-3 now has Anika request regeneration and a second Eligible Approver approve it.
- PRD FR-7 now defines five evaluation moments and rejects regeneration before Provider work unless another Eligible Approver remains for the prospective version.
- AD-8 carries the matching prospective-version guard and leaves the existing proposal decidable on rejection.
- The PRD continues to define Eligible Approver as the one predicate used by proposal actions, so the adopted rule's scope is no longer left as an approval-only inference.

No residual found for H-1.

### H-2 — Closed

- AD-31 now binds FR-13 and makes the persistent contributed region serve both caller-visible submitted/authoritative-pending/terminal outcomes and pending-proposal state for authorized Approvers.
- `EXT-CONV-UI-1` carries the same dual-purpose fourth artifact and requires caller/Approver disclosure tests.
- AD-8 remains the single detailed source for the three-tier Approver disclosure contract; AD-31 need not duplicate it.

No residual found for H-2.

### H-3 — Partially closed; exact residual remains

- **Closed in the spine:** the External V1 Prerequisites map now includes `EXT-PARTIES-1`, and it identifies `EXT-CONV-AI-1` seam 7 as requested, uncommitted, and unusable.
- **Still absent from the authoritative register:** `external-dependency-register.md` contains only nine records and no `EXT-PARTIES-1` record. Its Current Blocking Summary still says “All nine dependency records”.
- **Still absent from the authoritative register:** `EXT-CONV-AI-1.RequiredArtifact` still begins “Six seams”, its compatibility contract contains no message-retraction/deletion/flag signal, and no seam-7 amendment note exists.
- **Resulting contradiction:** AD-6 says `EXT-CONV-AI-1` additionally names seam 7, but the register does not. Because the spine declares the register authoritative for status and consumers, adding rows only to the spine does not close this dependency-authority gap.

H-3 closes only when the register adds the `EXT-PARTIES-1` commitment record and either adds seam 7 to `EXT-CONV-AI-1` or records the Product-approved alternative that removes it from the launch profile. Both records remain `Uncommitted` unless and until their required commitment fields are accepted.

## H-3 Final Post-Fix Verification — 2026-09-12

**CLOSED — no residual material mismatch.**

- The authoritative external dependency register now contains the complete nine-field `EXT-PARTIES-1` commitment record, with `Uncommitted` status and the A-27 transitional Organization-typed identity explicitly preserved until the new dependency becomes `Available`.
- `EXT-CONV-AI-1.RequiredArtifact` now defines seven seams, including seam 7's typed message-retraction/deletion/human-flag signal and its actor/instant payload; the compatibility contract includes the same signal.
- The register's 2026-09-12 amendment explicitly states that seam 7 remains requested, `Uncommitted`, and unusable, and that the whole record stays `Uncommitted` pending acceptance of all seven seams.
- The register summary now counts ten dependency records. Its status and blocking posture agree with AD-6 and the spine's External V1 Prerequisites map for both `EXT-CONV-AI-1` and `EXT-PARTIES-1`.

The former H-3 authority gap is closed. This is a documentation-consistency closure only: both dependencies remain launch blockers until their commitment fields are accepted and their statuses advance under the register's rules.
