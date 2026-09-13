# UX Update Reconciliation — Hexalith Agents — 2026-09-12

- **Spines:** `DESIGN.md`, `EXPERIENCE.md` (both `status: final`, `updated: 2026-09-12`)
- **Mode:** Update, following the round-5 validation. Sequenced by user decision at activation: currency banner first, then the seven governance/buildability silences, then the bloat pass.
- **Input:** `validation-report.md` — the 2026-09-12 round-5 four-lens validation. **87 findings: 8 critical / 27 high / 34 medium / 18 low.** Category verdicts: flow, tokens, components, states, inheritance and shape all *strong*; visual references *adequate*; **bloat & overspecification *thin***.
- **Prior-round closure carried into this run:** accessibility 22/22, governance 20/21, implementation readiness 24/26 (+1 partial, +1 carried). No lens found a regression.
- **Scope worked:** criticals and highs only. The 34 medium and 18 low findings are unworked and remain open.

## 1. What the round-5 report established about the pair

The eight criticals are **silences, not contradictions** — governed or buildable things neither spine states. That is why the rubric's six *strong* verdicts and the eight criticals are both correct at once: a mechanical consistency check cannot see a rule that was never written. Seven of the eight came from the governance and implementation-readiness lenses; the eighth is the currency banner, and it is the one case where the spine contradicts itself.

This shapes the whole run. Nothing was reverted. Every critical closure is new text filling a gap, and the round therefore **grew** both spines before the bloat pass ran against them — see § 5.

## 2. Criticals closed (8 of 8)

| ID | Finding | Resolution |
|---|---|---|
| C-1 | `EXPERIENCE.md:33` currency banner stale in both halves | Corrected: `prd.md` **2026-09-09 → 2026-09-12**; `ARCHITECTURE-SPINE.md` `architecture_assumption_index_version` **5 → 7** (2026-09-12, retiring `ARCH-A-13`, adding `ARCH-A-15`); validation reference **round 3 → round 5**. The banner now states that **it is itself a checkable claim** — each cited denominator is the `updated:` or version field of the named file, and a reconciliation that does not re-verify all four leaves the claim false rather than merely old. `:33` was verified to be the **only** stale currency site in the pair. The "33-story active backlog" figure was verified correct against `epics.md` (Epics 5–8 = 10 + 8 + 7 + 8). |
| C-2 | Audit evidence routes carry disclosure tiers and no route-level authorization expression | The `RequiredPolicy` cell rewritten in the **Launch readiness shape**: an enumerated route-admitting constant set, the Participant path stated as Conversation-derived with no Agents constant, and each tier's content decided by its own predicate. The `:109` claim that Audit evidence "already shows the disjunctive form" — false when written — is corrected rather than left certifying a shape the surface did not have. |
| C-3 | Nothing binds a tenant to a platform-scoped operation, and six FR-33 rows need one | A platform-scoped operation acting at tenant scope resolves its target tenant through the **shell tenant switcher**. The resolved tenant renders in every confirmation and is carried in the audit record as part of the authorization basis. Added to the required contents in § Confirmation contents by family rather than to individual rows. See § 3, decision 2. |
| C-4 | `ProposalEdit` and `ProposalRegeneration` are matrix v3 families neither spine mentions | Both added as rows in § Confirmation contents by family, with AD-12 lock classification and audit contents: editor Party + `ProposalVersionId` for edit; requesting Party, ceiling position and chargeability for regeneration. See § 3, decision 3. |
| C-5 | Route-level gating on a disjunction of policy tiers has no buildable mechanism | Resolved to the **broadest admitting constant** at the route, every control on its own narrow constant, the server decision authoritative. No composite constant minted; **no AD-30 amendment**. Nav-entry duplication filed as a FrontComposer request alongside the six outstanding glyph requests. See § 3, decision 1. |
| C-6 | `proposal-notification` is specified into a location that cannot host it | The count **relocates out of the shell nav rail** into the Agents overview page body readiness summary, per the Story 7.1 AC wording ("in-product pending count", which never named the rail). `FrontComposerNavigation` exposes no `[Parameter]` and nav badges carry no label slot — both halves verified — so the rail was never a candidate. |
| C-7 | The Agents overview — the default Agents surface — has no field inventory | § Agents overview rules created: a full per-surface field inventory, with Recent activity specified (row shape, count, paging, empty state, source contract). The relocated pending count lands in the readiness summary here. |
| C-8 | The Audit evidence list is a fourth grid § Grid rules claims to govern and cannot | `agents.audit-evidence` given its fourth-grid rules. `DESIGN.md § Layout & Spacing` is reconciled to four grids. |

## 3. User decisions, recorded verbatim from `.memlog.md` entries 88 and 89

These four are decisions, not derivations. They are recorded here so a later round does not re-open them as open questions.

1. **Disjunctive route gating.** Route gated on the **broadest** admitting constant, every control on its own narrow constant, server decision authoritative; nav-entry duplication filed as a FrontComposer request alongside the six outstanding glyph requests. **No AD-30 amendment.**
2. **Tenant binding.** Platform-scoped operations acting at tenant scope resolve the target tenant through the **shell tenant switcher**; the resolved tenant renders in every confirmation and is carried in the audit record as part of the authorization basis. **No in-surface selector, no free-text id.**
3. **Proposal families.** `ProposalEdit` **and** `ProposalRegeneration` both get confirmation rows, with AD-12 lock classification and audit contents. Both are **not lock-bearing** and require **no typed justification** — confirmation only. Derived from AD-22/AD-12 scoping the justification rule to the **ten lock-bearing families**; this keeps the "ten, no exempt family" claim true. Audit still carries editor Party + `ProposalVersionId` (edit) and requesting Party + ceiling position + chargeability (regeneration).
4. **Agents overview.** A **full** per-surface field inventory including a specified Recent activity; the `proposal-notification` count **relocates** from the shell nav rail into the overview page body readiness summary.

**Also user-confirmed: the Audit evidence layout.** Id-entry primary, with the change-and-inspection grid as the **single expanded accordion item**. Rationale as recorded: `DESIGN.md:254` expands a sole accordion item by default, so the grid is never hidden; `UnreviewedInspection` and the inspection rate already render outside the accordion; and the list is tier-one content only, so inverting the arrangement would render `not available` as the primary region for the Eligible Approver and the Participant who reach the route for id lookup.

## 4. Highs closed (27 of 27)

Closed in two waves. Itemized in `.memlog.md` entry 90; summarized here by what they changed.

**Wave 1 — 12 accessibility and governance.** `aria-rowcount`/`aria-rowindex` contract across all four grids; approver-policy reorder button naming, focus and announcement; version-history "Showing version {n}" rescoped to **View version** activation rather than selection; the `EXT-CONV-UI-1` announcement division (host declares, Agents suppresses the overlap); five countdowns brought under one once-per-minute outside-live-region rule; WCAG 2.2.2 disposed with an essential-exception argument; the UJ-1 cost-cap tier reconciled to FR-33; the FR-11 posting record surfaced on Operational status; Facilitator authorities named as the **fifth** `EXT-CONV-UI-1` artifact kind; approval irreversibility stated at the point of decision; audit append-only immutability; the launch-governance posture-recording control.

**Wave 2 — 9 implementation readiness + 1 rubric.** Audience-scoped retry-posting copy; the Pending approvals second-party approval surface; approver-policy add/remove and per-kind basis controls (Party search-and-select, **never** enumerating); the Operational status counts-vs-rows arrangement; four missing view-model shapes filed as *Contracts that must grow*; per-surface refresh and staleness contracts; 14-route title / back-link / `DetailPanelAriaLabel` string tables; content-safety per-row select with the looser-options-absent tier rule; the launch-readiness three recording acts; `AgentsPageStatusRegion` declared infrastructure.

`DESIGN.md` handoffs from both waves were closed by hand or by the concurrent `DESIGN.md` pass.

## 5. The bloat pass under-delivered

Stated plainly, because the rubric verdict remains *thin* and a later round must not read this section as closure.

`EXPERIENCE.md` went **258,175 → 251,973 bytes: −6,202, or −2.4%**. The rubric finding asked for far more than that.

The reason is arithmetic about what the rubric measured. The rubric walked the **178 KB committed version**. Roughly **79 KB landed earlier the same day**, closing 35 findings — the criticals and highs of §§ 2 and 4 above. The bloat pass was deliberately forbidden from trimming that new material: trimming immediately after a large additive run is exactly where such a pass does damage, cutting rules before anyone has read them once. What remained available to trim was the pre-existing 178 KB, and −6,202 bytes is what came out of it without cutting rule.

**Net, the spine grew.** The **Bloat & overspecification** finding is therefore **carried, not closed**. A future round should re-run it once today's additions have settled and can be judged on their own.

Two further bloat findings were addressed in place rather than by the trim: the `IProjectionChangeDetailNotifier` normative gap (misfiled in § FrontComposer Readiness, a navigation aid) fits **neither** § Known gaps subsection, so it was **compressed in place** instead of moved.

## 6. Invariants held, and one defect caught mid-run

- **Component name and order parity held 22/22 byte-identical across all three required places through every pass**, verified by `diff` at each one.
- **`AgentsPageStatusRegion` was declared infrastructure outside the 22-component contract** rather than admitted as a 23rd component. This is the decision that kept the parity claim above true.
- **A circular reference was introduced mid-run in `DESIGN.md § launch-readiness-panel`** — a control specified "on the same terms as" a control the file never specified anywhere. Caught within the run; both controls are now specified jointly.

## 7. Deferred, with named owners

**Architecture + Product**

1. Whether `launch-readiness-register.md` adds a **register operation family** for the three launch-readiness recording acts, or leaves them declaring none. The trade is explicit: adding one raises `OperationGateMatrixVersion` past 3; making it lock-bearing breaks the ten-family claim. **Rendering holds either way** — this is not blocking the surface.

**Epics**

2. Neither **Story 8.7 nor Story 5.9** has an acceptance criterion building those three launch-readiness recording controls.

**Architecture**

3. The **fifth `EXT-CONV-UI-1` artifact kind** (Facilitator authorities) and the **host/Agents announcement division** are recorded in `EXPERIENCE.md` as open seam items with owner `TBD`, but are **not synchronized** into `external-dependency-register.md` or `ARCHITECTURE-SPINE.md`. Until they are, the register asks a maintainer for four kinds while the spine consumes five.

**Left deliberately, needing an editorial convention no source establishes**

4. Rubric low — the `sources:` **asymmetry** between the two spines (`DESIGN.md` lists `reconcile-validation-2026-09-08.md`, `EXPERIENCE.md` does not).
5. Rubric low — `DESIGN.md`'s "**counts that will rot**": hard counts carried in prose where the spine's own stated principle is to cite by version rather than by count.

**Out of scope for a bloat pass**

6. Rubric low asking that **§ Inspiration & Anti-patterns move before § Key Flows**. That is a restructure, not a trim.

**Cosmetic, noted not fixed**

7. `DESIGN.md` names the Audit evidence surface "**Audit evidence list**" though it is lookup + list.

## 8. Not worked this round

**34 medium and 18 low findings remain unworked.** This round covered criticals and highs only, by the sequencing decision recorded in `.memlog.md` entry 87 and the scope confirmation in entry 89(c). They are not dispositioned, not judged, and not closed — they are simply outside what this run touched, and a following round inherits all 52.
