# UX Update Reconciliation — Hexalith Agents — 2026-09-10

- **Spines:** `DESIGN.md`, `EXPERIENCE.md` (both `status: final`, `updated: 2026-09-10`)
- **Mode:** Update, fast path, autonomous. Reviewer Gate skipped by user decision; doc standards applied.
- **Inputs, all four confirmed by the user at activation:**
  1. `validation-report.md` — the 2026-09-09 round-3 four-lens validation, 101 filings / 88 distinct, 11 critical filings / 9 distinct, 3 regressed.
  2. Root cause 1 — the rest of the 2026-09-09 `prd.md` revision beyond FR-33: FR-3, FR-4, FR-12, FR-15, FR-18, FR-25, FR-28, FR-30 and OQ-24 to OQ-30, every one of which returned **zero hits** in both spines.
  3. Root cause 2 — `epics.md`'s 33-story active backlog against the pair's 28.
  4. `ARCHITECTURE-SPINE.md` — which had moved to **round 5 (`updated: 2026-09-10`, index version 5)** during this run, not the round 4 the user scoped.

## 1. What the extraction changed about the scope

Two findings materially reshaped the run and are recorded because they invalidate parts of the input set:

- **The architecture spine was at round 5, not round 4.** Round 5 rewrote AD-5, AD-12, AD-13, AD-22 and AD-30 again and renamed a public enum. Anything absorbed verbatim from `UPDATE-REPORT-2026-09-09-2.md` alone would have been stale on arrival. The reconcile-ux review in the architecture run folder is round-3-era and now asserts two things that are no longer true; it was **not** used as a token source.
- **The 33-vs-28 story gap is real but not a miscount.** `epics.md` holds two tiers: Epics 1–4 (26 stories) are marked completed historical evidence and declared non-executable, and the active forward backlog is exactly 33. The pair was counting neither set correctly.

## 2. Regressions closed (3 of 3)

| ID | Defect | Resolution |
|---|---|---|
| REG-1 | `Abandoned` had grown a fourth typed reason, "kill switch", in English prose beside three contract tokens. FR-18, AD-12 and Story 7.7 AC-4 all state the switch "never system-abandons". | Reverted. The system reasons are the three of Story 6.8 AC-1 plus FR-18's `PostingWindowElapsed` — **four tokens, none of them the kill switch**. The `Abandoned` row now states the contradiction's source rather than asserting the rule. |
| REG-2 | The Audit evidence IA cell granted **posted provenance** to `Agents.Administrator`, `Agents.Operator` and `Agents.PlatformOperator` without membership, contradicting its own rules section 200 lines later, the FR-33 row and AD-22 — the ambient-participation route Story 8.8 exists to prevent. | Rewritten as three tiers that never merge: operational and configuration/governance evidence to the three constants; posted provenance to a current Participant with read access **only**; unposted protected content to Eligible Approver authority or the Story 8.8 inspection. |
| REG-3 | The version-radio fix correctly established that `DisabledFocusable` is absent on `FluentRadio`, then replaced it with a rule opening a modal confirmation **on every arrow keypress** in a native radiogroup while the editor is dirty — readmitting as a named exception the exact defect the Update's own "a confirmation is never opened by a selection event" generalisation had just closed. WCAG 3.2.2, and the dialog carried no family. | Adopted the shape that already fixed `response-mode-toggle`: the radio group edits a **draft selection** and an explicit **View version** command control applies it. The exception is withdrawn. The unsaved-changes dialog is given an explicit contract — family `EditorDraft`, focus trap, `AutoFocus` Cancel, explicit `Esc`, focus return. |

## 3. Criticals closed (6 of 6)

| ID | Resolution |
|---|---|
| C-1 | Stated the rule that a surface carrying two authorization tiers is gated at the route on the **disjunction**, each control on its own constant — gating on the tenant constant alone denied the page to the very Platform Operator whose control it carried. Launch readiness rewritten as a disjunction. The `Agents.PlatformOperator` shipped-drift row gained the **fail-closed interim** it lacked: until the constant is registered, platform-scoped mutation controls are absent and platform-scoped routes render `not available`, never gated on `Agents.Administrator`; linked to `LR-TENANT-ACCESS` and `RQ-1` so it is not read as clerical. |
| C-2 | Kill-switch confirmation restated to FR-28, OQ-27 and AD-12 in full. The decisive correction: an `Approved` proposal is **held without starting a post**, not "completes on its own terms" — OQ-27 records this as a deliberate call that safety wins. Added the never-approved/edited/regenerated clause, the paused retry clock with `PausedDuration`, `ExpiredWhileSuspended` and its SM-3/SM-C5 exclusion, and the branch on release authority. |
| C-3 | Gave `LegalHoldRelease` and `ExportRequest` the same request/approve/execute staging `DeletionRequest` already had, as a table. One Compliance Inspector could previously release the hold protecting the evidence and export it, in two dialogs. |
| C-4 | Compliance inspection rewritten from prose into four enforced mechanics: computed subject set with typed refusal and the 30-day mutual-approval bar; post-hoc only at single-proposal or single-Conversation scope inside the 7-day A-19 window; Platform Operator approval before the first content read at case scope; and a persistent `UnreviewedInspection` signal that blocks the Inspector's next post-hoc inspection. |
| C-5 | The `conversation-agent-call` component row and its DESIGN section were the two places a developer builds Story 6.7 from, and both still announced seven outcomes into nodes the same Submit destroys. The panel now **owns no live-region nodes at all**; the persistent contributed region carries the pair. |
| C-6 | Story ownership re-derived against the 33-story backlog. See §5. |

## 4. Root cause 1 — the rest of the 2026-09-09 PRD

The FR-33 Update read one requirement and treated it as the whole revision. Absorbed this run:

- **New section § Agent lifecycle and tenant suspension.** Two orthogonal axes that must never collapse into one badge: lifecycle `Draft` / `Active` / `Disabled`, and tenant `Suspended`. `Draft` is the state provisioning leaves and the state `agent-config-form` opens in; the pair previously had no word for it. A per-state degradation table for both stop conditions, and the one place they differ — ordinary `Expired` under a disable, `ExpiredWhileSuspended` under the switch.
- **New section § Conversation Agent State** (OQ-25). Five states, `BlockVersion` as the concurrency token, and the two mirror flags — `MirrorPending` informative, `MirrorRefused` terminal and actionable with exactly two remediations. `ReadmitPending` means a clear never renders as re-admitted.
- **New section § Provider data handling** (FR-4, OQ-29). The four-field record, `DataHandlingVersion`, `DataHandlingAcceptanceLapsed`, the asymmetric grace — immediate block by default, 30 days **only** for a Platform-Operator-declared tightening change carrying a recorded field-level diff — and a new `DataHandlingAcceptance` confirmation family with both Accept and Decline. None of this existed in either spine.
- **`Indeterminate`, not `Unknown`.** The cost editor rendered "held `Unknown` reservations"; FR-12 makes `Indeterminate` an additive value **explicitly distinct** from the `Unknown = 0` sentinel. Corrected, and the four reservation dispositions added, including `Unreconciled`, which the pair had no word for.
- **Launch readiness blocker vocabulary** grown from two codes to the full set in three provenance classes, with `SuspensionReviewOverdue` and `DeferredAssumption` carrying a visible PRD-declared qualifier until the register carries them, and the three authorities — register, launch-readiness register, FR-30 runtime surface — kept distinct.
- **`PostingPending` is uninterruptible**, with a stored deadline and no renderable countdown; `LateConfirmed`, `PostingWindowElapsed` and the pre-exit `MessageId` lookup with its three outcomes.
- Provisioning (OQ-28): Platform-Operator, create-only, idempotent; **no Create or Delete control for any tenant role**, Party identity inspect-only.

## 5. Root cause 2 — story ownership

| Correction | Was | Now |
|---|---|---|
| Pre-Provider rejections | 6.3, 6.4, 6.5 | 6.8 owns the **contract presence** and unknown-value fail-closed rule; 6.2, 6.3, 6.4, 6.5, 6.6, 7.1 and 7.5 own the semantics individually. `epics.md` genuinely dual-attributes this, so the split is recorded rather than resolved by picking a side. |
| `PostingFailed` recovery | 7.2 to 7.6 | 7.2 to **7.7**. The lane is itself split: 7.4 builds the seam-2 `MessageId` lookup, 7.5 consumes it for abandon, 7.7 owns retry, the ten-state parity source and kill-switch transitions. "7.7 owns the entire lane" overstates it. |
| `Caller` retirement | 5.4 | 5.4 **and 5.10**. 5.4 rejects server-side; 5.10 removes it from the authoring UI, maps the `ConversationOwner` label, and proves stale UI state cannot bypass. Assigning it to 5.4 alone was dropping two rules that exist only in 5.10. |
| Conversation membership state | 6.7, 8.7 | **6.6** added — it owns the five states, `BlockVersion` and `MirrorPending`, none of which the pair rendered. |

## 6. Architecture round 5 absorbed

- **`AgentCallOperationStatus` → `AgentInteractionStatus`**, renamed everywhere in the spine and now zero-occurrence there. Three sites corrected.
- **Automatic-path retry reversed.** The pair said a new Agent Call was the sole recovery; AD-5 now gives the automatic path the same bounded retry on the same terms, and substitutes any current Conversation Facilitator wherever FR-18 names an Eligible Approver.
- **`KillSwitchActive` deleted as invented** — zero occurrences in the spine, the registers, `prd.md` and `epics.md`; the second invented constant after `Agents.ComplianceInspector`, caught by the same check. Replaced by the tenant state `Suspended` and, at FR-8 step 2, the typed rejection `TenantSuspended`. `AgentInteractionStatus`'s sanctioned growth is **six** members; the reason codes are not enum members.
- **Block-clear authority** split into AD-7's three distinct rules — role-bound for the Administrator, identity-bound for a Facilitator and only while they still hold the role, and a third path for a system-set `ExternallyRemoved`.
- `agent-setup-readiness` corrected to `agent-setup`; `PausedDuration` added to the retry-window arithmetic; the justification rule widened from FR-30's five families to AD-22's **all nine, no exemptions**.
- **Security approval** rebound from the spine's invented two-stage acceptance sequence to AD-30's actual design — a recorded reference on the `PolicyPublication` command. The stage had no acceptance token, no authorization constant and no AC.

## 7. Verified against the code, not the report

| Claim | Report said | Tree holds |
|---|---|---|
| Live-region sites | 15 across 13 files | **29 across 20 `.razor` files**; `ProviderCatalog.razor` (3 nodes) was absent from the row. Three further `.cs` matches are XML doc comments, now excluded explicitly so a later verifier does not re-inflate the count. |
| `Disabled` sites | "eleven pages", 4 files enumerated | **15 across 5 files**; `ProposalRegenerator.razor` added, the two enum-member matches excluded. |
| `Agents.Surface.NotAvailable.*` | absent | Confirmed absent; all seven forbidden per-cause key pairs confirmed present. |
| Policy constants registered | four, `Agents.PlatformOperator` missing | Confirmed. |
| `aria-describedby` binding | unverified, "most load-bearing rule with no verified binding" | **Resolved empirically**: `data-testid` is already splatted onto `FluentButton` in eight files, which only compiles because unmatched attributes are captured. The binding is a plain attribute; no typed parameter is needed. |
| `Constrained` viewport lockout | alleged | **Confirmed in source.** `ProposalApprover.razor` renders Approve only under `CanApprove && !Constrained` and replaces it with a `<p tabindex="0">` otherwise; `AgentCallStatusFeedback.razor` does the same to the Call action. Both carry a self-referential `aria-describedby`. Filed as a new drift row — a WCAG 1.4.10 failure on the product's highest-consequence action. |

## 8. Deferred, with named owners

**Architecture**

1. Is `ConversationPosting` lock-bearing? It appears in neither AD-12's nine-family roster nor its non-lock-bearing trio. Rendered on the conservative reading meanwhile.
2. **Administrative retry has no valid family.** It cannot be `ConversationPosting` — that gate set requires Conversation membership and posting access, which the administrative exit explicitly does not require — and no other family fits.
3. `CapacityQueued` queue position has no contract. AD-24 binds `AdmissionId`, `AdmissionFence` and `QueueId` only, so "queue position where safe" has nothing behind it.
4. `CurrencyMismatch` is named by AD-10 and AD-21 but absent from the register's enumerated `ProviderReadinessReasonCode`, while AD-10 makes the register binding.
5. AD-10's `EntryMissing` carve-out versus AD-2's unqualified wording — deferred by architecture rounds 4 **and** 5. No UX rule written for historical interaction views.
6. `ARCH-A-13`: AD-8's fourth Approver exclusion (the regeneration requester) has no FR-7 basis. Its rendered copy is marked provisional.

**Architecture + Conversations**

7. `EXT-CONV-UI-1` must be amended to **four artifact kinds plus the focus-return contract**. The register's third kind is `GetCallabilityAsync`; the persistent status region appears nowhere in it, so a maintainer delivering exactly what the register asks still cannot support Story 6.7. The register's provenance clause also double-specifies the source.

**Epics**

8. **The per-tenant kill switch has no story that builds it.** Story 8.4 AC-2 places the flag on `TenantGovernancePolicy` and stops — no actor, authorization, approval, runtime enforcement, UI or audit record — while 7.6 and 7.7 consume it and 8.6 requires the family to exist for conformance.
9. **The Security approval stage on Content Safety Policy publication has no story either.** "Security approval" appears only in FR-26 and FR-33 requirement text; zero hits in any AC, OwnedClauses or test artifact. Notable because epics.md models two-party approval carefully in 8.1, 8.2, 8.3 and 8.8.
10. Story 5.3 / 5.7 have no AC that registers a policy constant or regates a surface, which is what C-1's interim is waiting on.
11. Story 7.2 has no AC for the dirty state, Save/Discard, or the unsaved-changes confirmation.
12. Story 8.4's budget AC never records the raise/lower asymmetry.

**Product**

13. A-13 and A-17 thresholds are provisional until Product's 2026-09-30 confirmation (A-26) and render with that qualifier.
14. A role-to-policy-name column on the PRD FR-33 table — carried from the previous run; FR-33 names roles and no constants, which is how `Agents.ComplianceInspector` became reachable.

**Product + Security**

15. OQ-18 unsafe historical content; the two-person rule beyond what A-12 already decides.

## 9. Not re-verified

Per the round-3 report, verified sound and deliberately left alone: the `Agents.AuditOperator` revert, 22/22/22 component parity, token-reference resolution, the eight MCP-verified Fluent v5 API claims, `FluentDialogBody.FixedHeaderFooter` defaulting true, and the `FcFluentIcons` thirteen-versus-fourteen factory count, which is an accurate characterisation rather than drift.
