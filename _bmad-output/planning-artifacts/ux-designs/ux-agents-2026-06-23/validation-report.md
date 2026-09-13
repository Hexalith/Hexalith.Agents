# Validation Report — Hexalith Agents

- **DESIGN.md:** `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- **EXPERIENCE.md:** `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`
- **Run at:** 2026-09-12T12:42:14+02:00

## Overall verdict

This is an unusually strong spine pair on every mechanical axis: 22 components named and ordered identically in all three required places (verified by diff), every `{path.to.token}` in both files resolving, every one of 39 `sources:` paths resolving on disk, every internal and external `§` cross-reference resolving, and DESIGN.md carrying all eight canonical sections in exact canonical order. A story-dev can source-extract from it without asking a question — the 50 `UX-DR` rows already extracted into `epics.md` are evidence that extraction works at scale. Two things stop it short of clean. First, `EXPERIENCE.md`'s own currency banner (line 33) is wrong in **both** halves — it claims reconciliation to a PRD revision and an architecture assumption index that have each been superseded, while the body already carries the newer decisions, so the one field a consumer uses to judge whether the contract is current actively misleads. Second, `DESIGN.md` has not been re-verified since 2026-09-10 and carries zero verified-closure evidence from the 2026-09-12 round: three visual decisions that round created have no visual binding, and one of them directly contradicts `DESIGN.md § Layout & Spacing`. Size is the remaining cost — at 178 KB the contract is right but expensive to read, and about a third of the bytes are argument and code-audit backlog rather than rule.

The three extra lenses move the picture, and they move it in one direction. The rubric walk found the pair mechanically near-flawless — six of eight categories *strong*, not one broken cross-reference, not one unresolved token — while governance and implementation readiness between them found seven criticals, and **every one of the seven is a silence rather than a contradiction**: a governed or buildable thing the spine simply never states. The Audit evidence routes carry disclosure tiers and no route-level authorization expression. No mechanism anywhere binds a tenant to a platform-scoped operation, though six FR-33 rows need one. `ProposalEdit` and `ProposalRegeneration` are operation families in the authoritative matrix that the UI demonstrably issues and that return zero hits across both files. Route-level gating on a *disjunction* of policy tiers is mandated by a rule with no buildable mechanism behind it. `proposal-notification` is placed inside a shell nav link the spine itself proves is un-parameterizable. The default Agents surface has no field inventory. The Audit evidence list is a fourth grid § Grid rules claims to govern and cannot. Mechanical consistency checks cannot see any of these, because there is nothing there to be inconsistent with — which is exactly why the rubric's *strong* verdicts and the seven criticals are both correct at once, and why a story-dev reading only the clean parts would ship each of these seven differently from the next dev.

Round over round, the closure record is the strongest the pair has posted. Accessibility closed 22 of 22 prior findings, including both criticals and its one regression. Governance closed 20 of 21 — one resolved with a new defect in its replacement, one unchanged by decision — with all six prior criticals genuinely closed and none reopenable. Implementation readiness closed 24 of 26, with 1 partially closed and 1 carried forward. No lens found a regression: this round's criticals are new surface area exposed by the round-4 fixes, not reversals of them.

## Category verdicts

- Flow coverage — strong
- Token completeness — strong
- Component coverage — strong
- State coverage — strong
- Visual reference coverage — adequate
- Bloat & overspecification — thin
- Inheritance discipline — strong
- Shape fit — strong

## Reviewer verdicts

**Accessibility** (`review-accessibility.md`) — Keyboard operability: adequate. Focus management: strong. Live regions and announcements: adequate. Semantics and naming: adequate. Contrast: adequate. Forms and errors: strong. Motion, timing, and time limits: thin. Density and zoom: adequate. Gaps that would let an inaccessible build pass review: adequate.

**Governance & authorization** (`review-governance.md`) — FR-33 role binding: adequate. Tenant isolation: thin. Approval gating: adequate. Provider secrets: strong. Cost and quota governance: adequate. Audit evidence: adequate. Failure and denial states: adequate. Spine vs. 2026-09-12 architecture: strong.

**Implementation readiness** (`review-implementation-readiness.md`) — Overall: adequate. Layout determinacy: thin. Route and navigation completeness: thin. Component buildability: adequate. Data binding and shape: thin. State machine completeness: strong. Copy readiness: thin. Spine vs shipped code: adequate. Testability: adequate.

## Findings by severity

### Critical (8)

**[Inheritance discipline]** — The currency banner is stale in both halves and now contradicts the file beneath it (§ EXPERIENCE.md:33)

It declares the spine “reconciled to `prd.md` **as of its 2026-09-09 revision in full**” — `prd.md` is `updated: 2026-09-12`. And it declares “`ARCHITECTURE-SPINE.md` at `architecture_assumption_index_version` **5** (2026-09-10)” — the Architecture Spine is `updated: 2026-09-12` at `architecture_assumption_index_version: **7**`, two increments on, and those increments **retire `ARCH-A-13` and add `ARCH-A-15`**. The body already absorbs the 09-12 decisions (matrix v3 in all three normative places, ten lock-bearing families, four artifact kinds, the six-conjunct predicate, `CurrentMirror`, `ARCH-A-13` retirement at `:852`), so the banner now contradicts the file beneath it. This is the single field a downstream consumer reads to decide whether the contract is current, and a consumer trusting it will believe the spine predates decisions it actually carries — or re-do the reconciliation. It also has a behavioral edge: the spine rules that “every unretired §8.1 `A-n` and `ARCH-A-n` row blocks `RQ-1` whatever its owner”, so the `UnretiredAssumption` blocker set `launch-readiness-panel` renders grew with `ARCH-A-15`, against a baseline the spine says is index 5.

Fix: Update the banner to `prd.md` 2026-09-12 and `ARCH-A-INDEX-7`, add the five 2026-09-12 architecture reviews to `sources:`, and state which of their directives are absorbed.

**[Governance]** — The Audit evidence routes have no route-level authorization expression (§ EXPERIENCE.md:100 (IA row) · rules at :109, :110)

The `RequiredPolicy` cell reads “Three disclosure tiers, never merged: **operational and configuration/governance evidence** to `Agents.Administrator`, `Agents.Operator`, or `Agents.PlatformOperator`; **posted provenance** to a current Participant … **only**; **unposted protected content** to Eligible Approver authority or … `Agents.AuditOperator`.” A Conversation Participant holds **no** policy constant (`:68`); an Eligible Approver holds `Agents.Approver`, which is not listed; `Agents.AuditOperator` appears only inside the third tier's prose. The spine's own rules are that “Every page also carries `[Authorize(Policy = ...)]`” and that a two-tier surface “is gated at the route on the _disjunction_ of the tiers” — and that same sentence asserts “Operational status and **Audit evidence** already show the disjunctive form”, which for Audit evidence is false. A story-dev reading the cell top-down ships `[Authorize(Policy = Agents.Administrator)]` on `/agents/audit` and the Participant, the Eligible Approver and the Compliance Inspector are all denied the page that carries their FR-33 rows.

Fix: Write the cell the way Launch readiness is written — `Route: Agents.Administrator, Agents.Operator, Agents.PlatformOperator, Agents.Approver, or Agents.AuditOperator; a Participant with current read access reaches the posted-provenance detail through the Conversation-derived path with no Agents constant; each tier's content decided by its own predicate` — and correct the `:109` claim, which currently certifies a surface that does not have the shape it claims.

**[Governance]** — Nothing binds a tenant to a platform-scoped operation, and six FR-33 rows need one (§ EXPERIENCE.md:63, :77, :91, :94, :95, :99, :101, :285, :305)

§ Authorization roles states “`Agents.PlatformOperator` is platform-scoped … never tenant-scoped” and “Every role except Platform Operator is tenant-scoped”. FR-33's scope column reads **Tenant** for: enable a Provider/model for a tenant (A-10); declare a `DataHandlingVersion` tightening; configure cost caps and rate limits; override a reached cost cap; pull and release the per-tenant kill switch; request approved deletion. `tenant selector`, `tenant context`, `tenant switch` all return **zero hits**. § Confirmation contents by family requires “the action, resource identity, future-only effect, authorization basis” — the tenant is not among them, and only `DeletionRequest` and `TenantProviderEnablement` name a tenant. So a story-dev must invent the mechanism: a free-text tenant id, a dropdown enumerating every tenant in the platform, or reuse of the shell tenant switcher — the middle option publishes the tenant inventory to a surface FR-19 exists to protect, while the first makes the kill switch a typo away from suspending the wrong tenant.

Fix: State in § Authorization roles that a platform-scoped operation acting at tenant scope resolves its target tenant through one named mechanism, that the resolved tenant is rendered in the confirmation and carried in the audit record as part of the authorization basis, and that the selector discloses only tenants the acting principal is authorized to see; add the tenant to the required contents in § Confirmation contents by family rather than to three of its rows.

**[Governance]** — `ProposalEdit` and `ProposalRegeneration` are matrix v3 families neither spine mentions (§ launch-readiness-register.md:167-168 · EXPERIENCE.md:262, :285, :349, :351, :582)

The register carries `ProposalEdit (v2)` at the `ProposalResolution` gate set and `ProposalRegeneration (v2)` at **the same gate set as `ProviderInvocation`** — the cost-, safety-, tokenizer- and capacity-gated one — and states “consumers may not maintain local subsets”. Both return zero hits across both spines. The action rail's **Save** “commits the draft as a new preserved version” with no family, no confirmation and no audit contents row; **Regenerate** is specified as guarding “against a double Enter, **without joining the confirmation families**” although it incurs Provider cost, re-runs safety gates, and counts against the ceiling. Meanwhile § Confirmation contents by family opens “a family the UI issues and this table omits is a defect in this table”. Two teams building from this ship a Save that takes an advisory lock and a justification and a Save that takes neither.

Fix: Add `ProposalEdit` and `ProposalRegeneration` rows to § Confirmation contents by family or state explicitly that the UI declares them on the command while rendering no confirmation, name their lock classification against AD-12, and say what the audit record carries for each.

**[Implementation readiness]** — Route-level gating on a disjunction of policy tiers has no buildable mechanism (§ EXPERIENCE.md § Information Architecture · FrontComposerNavEntry.cs · FrontComposerNavigation.razor:43-45)

§ Information Architecture: “**A surface carrying operations of two authorization tiers is gated at the route on the _disjunction_ of the tiers, and each control on its own constant** … The route-level `[Authorize(Policy = ...)]` admits the union.” Verified in the shell: `FrontComposerNavEntry` declares `string? RequiredPolicy` — one nullable string — consumed as a single `<AuthorizeView Policy="@entry.RequiredPolicy">`. ASP.NET's `[Authorize(Policy = …)]` likewise names one policy. AD-30 names five constants, none of them a union, and § Authorization roles says renaming an identifier “requires an AD-30 amendment, not a UX decision” — so the dev may not mint `Agents.StatusRead`. The four affected routes are Operational status, Launch readiness, Audit evidence and (split read/write) Provider catalog, Content safety policy and Cost controls. The three paths open to a dev are: invent a composite constant (forbidden), register duplicate nav entries per constant (produces duplicate rail tiles), or gate on the narrowest constant — which is exactly the defect C-1 was written to close, and it is what the tree does today.

Fix: State the mechanism. Either name the composite policy constants and file them as an AD-30 amendment with an owning story, or state that the route is gated on the **broadest** constant with every control on its own, and file the nav-entry duplication question as a FrontComposer request alongside the six outstanding glyph requests.

**[Implementation readiness]** — `proposal-notification` is specified into a location that cannot host it (§ DESIGN.md § proposal-notification · EXPERIENCE.md § Component Patterns)

`DESIGN.md`: “A domain-rendered `FluentBadge` in `{colors.status-informative}` **inside the Agents overview link**”. `EXPERIENCE.md`, same component: “The count is domain-rendered **inside the Agents overview link** as the whole string `{count} proposals pending approval`” — and, four sentences later, “`FrontComposerNavigation` exposes **no `[Parameter]` at all**, and the nav badge renders as a bare `FluentBadge` with no label slot.” Both halves verified: `FrontComposerNavigation.razor(.cs)` declares no `[Parameter]` whatsoever, and nav badges render with no label slot, gated on `count > 0` over `ReflectionActionQueueProjectionCatalog`'s `ProjectionRole.ActionQueue` enumeration. There is no seam by which the Agents domain renders anything inside a shell nav entry. So the one in-product discovery affordance FR-13 and Story 7.1 require has no location: not the rail (un-extensible), and the row explicitly rules out the shell count.

Fix: Relocate the count to a surface Agents owns — the Agents overview page body's readiness summary is the obvious candidate and the Story 7.1 AC already says “in-product pending count” without naming the rail — or file the localizable-label slot as the FrontComposer request the row already contemplates and state the interim location explicitly.

**[Implementation readiness]** — The Agents overview — the default Agents surface — has no field inventory (§ DESIGN.md § Layout & Spacing · EXPERIENCE.md § Information Architecture)

`DESIGN.md § Layout & Spacing` gives it “Readiness summary” primary / “Blockers; Recent activity” accordion. § Information Architecture gives it a route, a policy, a layout, three stories and three contracts. Nowhere in either spine is there a field list, an order, a grouping, a row shape, a count, a paging rule, an empty state or a source contract for **Recent activity**; the string “Recent activity” occurs exactly once in the pair. The nearest thing to a spec is UJ-1 step 12, which is a journey resolution, not a field inventory, and covers the readiness summary only. `AgentsOverview.razor` today ships a `<dl>` of seven facts and no accordion at all, so the dev has neither a spine table nor a conforming precedent. Two competent devs will ship visibly different landing pages, and this is the route UX-DR2 makes the domain's default.

Fix: Add an Agents overview row to a per-surface field table, or delete “Recent activity” and say the overview carries readiness and blockers only.

**[Implementation readiness]** — The Audit evidence list is a fourth grid § Grid rules claims to govern and cannot (§ EXPERIENCE.md § Information Architecture · § Grid rules · § Audit evidence rules)

§ Information Architecture: “It **additionally carries an enumerable, tenant-scoped list** … The Agents grid rules apply to that list and to it only.” § Grid rules' `ViewKey` row enumerates exactly three keys and adds “Each mandatory component takes one, **view-key mismatches fail closed**.” `FcExpandInRowDetail` marks `ViewKey`, `HasExpanded`, `ChildContent` and `DetailPanelAriaLabel` all `[Parameter][EditorRequired]`, and `FcFilterEmptyState` does the same for `ViewKey`/`TotalCount`/`EntityPlural`. A Story 8.8 dev has no key to pass, no localized detail-panel label in the inventory, no column set, no default sort, no page size, and no surviving-column set at 320 px — while the surface must render the `UnreviewedInspection` flag and “the inspection rate”. This is the identical defect round 4 closed for the Launch readiness gate grid, reopened one surface over.

Fix: Add `agents.audit-evidence` to the `ViewKey` row with its `DetailPanelAriaLabel`, and give the list a columns/sort/page-size/320 px paragraph in § Audit evidence rules the way § Proposal queue rules and § Provider catalog rules each carry one.

### High (27)

**[Flow coverage]** — UJ-3 step 2 points at a “Conversation status entry” no section defines (§ EXPERIENCE.md:784 · § Conversation Integration Seam)

UJ-3 step 2 offers Anika three discovery paths — “the pending count, queue, or **Conversation status entry**” — but no section defines a Conversation status entry. § Conversation Integration Seam defines the fourth `EXT-CONV-UI-1` artifact as caller-facing only (validation, `submitted`, `denied`, `generation failed`, `capacity rejected`, `Posted`, `EXPERIENCE.md:121`). `reconcile-prd-2026-09-12.md:137-139` records that AD-31 and the external register made that artifact **dual-purpose** — also carrying pending-proposal state for authorized Approvers under AD-8's three-tier disclosure, with caller/Approver disclosure tests required. Neither the dual purpose nor a fifth artifact is in the spine, so a journey step points at a surface the seam contract does not create, and FR-13's third discovery path has no implementable definition.

Fix: Extend the fourth artifact's row to state both purposes and the three disclosure tiers (full content with current read access; existence-and-state for a previously resolved Approver who lost it; nothing for a Party never resolved), or add a fifth artifact kind and cite it from UJ-3 step 2.

**[Component coverage]** — `DataHandlingAcceptance` is a three-button confirmation with no visual binding (§ EXPERIENCE.md:307 · DESIGN.md § high-impact-confirmation)

`DataHandlingAcceptance` became the tenth lock-bearing confirmation family and is the only one carrying **two affirmative actions** — Accept and **Decline**, where Decline “blocks the model for the tenant immediately”. `DESIGN.md § high-impact-confirmation` specifies exactly one Confirm (`ButtonAppearance.Primary`, `{colors.brand-accent}`, never the default button) plus Cancel, and “no destructive red button”. There is no visual binding for a three-button dialog whose second affirmative commits a blocking side effect — which of Accept/Decline is Primary, how Decline is distinguished from Cancel, whether Decline gets a severity treatment. This is the one confirmation in the roster where getting the button hierarchy wrong blocks a tenant's model by mis-click.

Fix: Add the Accept/Decline/Cancel binding to `DESIGN.md § high-impact-confirmation` as a named variant.

**[Component coverage]** — The audit-evidence enumerable list has no component on either side (§ EXPERIENCE.md § Information Architecture · § Grid rules)

`EXPERIENCE.md § Information Architecture` gives `/agents/audit` “an enumerable, tenant-scoped list” for configuration-and-governance change evidence and compliance-inspection records, and rules that “The Agents grid rules apply to that list and to it only.” But `§ Grid rules` enumerates per-grid values for exactly three grids — the `ViewKey` row names `agents.provider-catalog`, `agents.pending-proposals`, `agents.launch-readiness` and stops — so this fourth grid has no view key, no `DetailPanelAriaLabel`, no 320 CSS px surviving-column set, no default sort and no page size, all of which `§ Grid rules` treats as mandatory per-grid values. On the DESIGN side `audit-evidence-panel` is “Definition list plus `FluentBadge` rows”, i.e. the detail only.

Fix: Add the fourth grid's four values to `§ Grid rules` and give DESIGN.md either a variant on `audit-evidence-panel` or a 23rd component.

**[Inheritance discipline]** — The `EntryMissing` terminal-history directive is unabsorbed (§ reconcile-ux-2026-09-12.md:22 · EXPERIENCE.md `version-history` / `audit-evidence-panel`)

`reconcile-ux-2026-09-12.md:22` directs: “**Add the terminal-history rendering rule to UX** and remove the section-8 deferral” — AD-2 and AD-10 now permit the `EntryMissing` carve-out only for the Agent's current selection or an in-flight interaction's prior snapshot, with terminal history rendering from the immutable snapshot plus authorized Audit Evidence. `EntryMissing` appears **nowhere in either spine**, and none of `version-history`, `audit-evidence-panel` or `provider-catalog-grid` states how a terminal proposal renders a Provider/model whose catalog entry is gone.

Fix: Add the rule to the `version-history` and `audit-evidence-panel` rows and name the code.

**[Shape fit]** — `DESIGN.md § Layout & Spacing` directly contradicts the current `EXPERIENCE.md` (§ DESIGN.md:82 · EXPERIENCE.md § Information Architecture)

It rules that “The Audit evidence list is an id-entry surface, **not a grid**”, that the required filter component set “therefore applies to the Provider catalog and Proposal queue grids only”, and that `FcExpandInRowDetail` “additionally governs the Launch readiness gate grid, which is **a third** `FluentDataGrid`”. `EXPERIENCE.md` now gives `/agents/audit` an enumerable tenant-scoped list governed by the Agents grid rules — a fourth grid. On the pair's own rule that the spines are one contract and win on conflict, a consumer has two contradictory instructions and no tiebreak.

Fix: Update `DESIGN.md § Layout & Spacing` to four grids and give the audit list its layout row.

**[Shape fit]** — `DESIGN.md` carries no visual binding for four decisions `EXPERIENCE.md` delegates to it (§ DESIGN.md § proposal-state-badge / § proposal-editor (`updated: 2026-09-10`))

The 2026-09-12 architecture reviews cite `EXPERIENCE.md` line numbers throughout and **never cite a `DESIGN.md` line**, so `DESIGN.md` has no verified-closure evidence from that round, and its 2026-09-10 stamp precedes every closure. Concretely: (a) the **nearing-retry-window flag**, required “on the same terms as nearing expiry” with the remaining time “rendered on the proposal detail itself” — `DESIGN.md § proposal-state-badge` specifies the nearing-*expiry* chip to the character and says nothing about its twin; (b) a **paused retry clock** that “renders as paused rather than counting down”; (c) an `Approved` proposal waiting under disable-or-suspension rendering a “**waiting, not posting**” treatment visibly distinct from `PostingPending` — but `DESIGN.md` gives both the same `{colors.status-informative}` and no distinguishing device; (d) `LateConfirmed` and `ExpiredWhileSuspended` are absent from both the colors role lists and the `proposal-state-badge` display-only-outcome set. `DESIGN.md § proposal-editor`'s Variants list likewise has no `PostingFailed`/administrative-retry variant.

Fix: Re-verify `DESIGN.md` against the 2026-09-12 round and bind these four; bump its `updated:`.

**[Accessibility]** — The virtualized grid has no ARIA row-count contract, so every list misreports its size (§ EXPERIENCE.md § Grid rules · § Other criteria)

§ Grid rules carries the FC-TBL envelope rules over to the hand-authored `FluentDataGrid` — “virtualization with a density-bound `ItemSize`, stable item keys, the reserved column keys” — and neither § Grid rules nor § Other criteria ever names `aria-rowcount` or `aria-rowindex`. A virtualized grid renders only the visible window into the DOM; without those attributes a screen reader announces the DOM row count and DOM row positions, so a 25-row proposal queue reads as “row 3 of 9” and an Approver cannot tell how many proposals they are responsible for. Every other row-level rule in the section presupposes a coherent row identity that this omission destroys. It reaches all three grids, including the read-only Launch readiness gate grid.

Fix: Add a row to § Grid rules: the grid sets `aria-rowcount` to the authoritative total (not the rendered window) and each rendered row sets `aria-rowindex` to its absolute position; make both an `LR-UI-CONFORMANCE` assertion alongside the 320 px evidence, since axe does not flag a virtualized grid that simply lies.

**[Accessibility]** — The approver-policy reorder controls have no name, focus, or announcement contract (§ EXPERIENCE.md § Approver policy rules · DESIGN.md § approver-policy-builder)

§ Approver policy rules ends “Row order is edited with buttons or a menu, never drag-only”, and `DESIGN.md` renders it as “move up/down `FluentButton`s” per row — which is the right answer to 2.5.7 and produces, on a six-source policy, twelve buttons whose accessible names are all “Move up” or “Move down”. § Forms names the *row* and not the buttons. Worse, the operation is stateful and silent: nothing says focus follows the moved row's button to its new position, and the politeness table — which asserts “any event this spine names and this table omits is a defect in the table” — has no row-reorder event at all. Row order is load-bearing: it is the order in which Approver sources are resolved. A keyboard user can operate this control and cannot tell what it did.

Fix: Require each reorder button's accessible name to carry the row identity (`Move {kind}: {basis} up`), require focus to follow the moved row's activated button, and add a politeness-table row announcing `{source} moved to position {n} of {total}`. While there, note that the builder specifies ordering and validation but no add or remove control — if rows can be added or deleted they need the same three contracts, and row deletion is the one focus case the ladder does not cover.

**[Accessibility]** — `version-history` announces a version that is not on screen, once per arrow key (§ EXPERIENCE.md § Component Patterns · `version-history`)

The redesign is correct — the radio group edits a draft selection and only **View version** applies it — but the sentence written for the old immediate-apply model survived intact: “On a selection change the textarea's accessible name becomes the version identity and the status node announces `Showing version {n}.`” In a native radiogroup the selection *is* the arrow key, which the same row states two sentences earlier. So arrowing from version 1 to version 4 now fires three polite announcements of `Showing version 2`, `3`, `4` while the textarea still holds version 1. That is worse than the defect the redesign fixed: the old rule at least announced something true. It is also the one announcement in the product a screen-reader user would reasonably trust to tell them which version they are about to approve.

Fix: Rescope both clauses to **View version** activation — the accessible name and the announcement change when the applied version changes, never when the draft moves.

**[Accessibility]** — The seam says nothing about what the host announces, on the only V1 invocation path (§ EXPERIENCE.md § Conversation Integration Seam · `EXT-CONV-UI-1`)

`EXT-CONV-UI-1`'s fourth artifact kind is now correct and the panel owns no node — but § Live regions' governing rule is scoped to Agents routes and to FrontComposer components. The Conversation view is neither. It is a chat surface owned by a separate module, and the single most likely thing a chat surface announces is the arrival of a new message — precisely the event Agents' persistent region announces as `Posted`. `hexa`'s reply therefore plausibly announces twice, and `agent-response-marker` is contributed into that same message. Nothing in the seam table, the ownership split, or the four artifact kinds constrains it, and NFR-14's `LiveRegionAnnouncedTick` observes the Agents node only, so the duplicate corrupts nothing measurable and ships invisible.

Fix: Add a fifth clause to `EXT-CONV-UI-1` — the host declares its own announcement behaviour for contributed content, and Agents suppresses whichever of `Posted` / new-message the host already speaks — and record it as an open seam item the way the focus-return contract was.

**[Accessibility]** — Four countdowns were added with no update cadence and no live-region exclusion (§ EXPERIENCE.md § Provider data handling · § Other criteria · § Audit evidence rules)

The queue's expiry label is explicitly disciplined — “an absolute culture-formatted timestamp plus a static relative label refreshed at most once per minute, **outside any live region**” — and every countdown added since carries nothing: the tightening-grace deadline “rendered as a countdown”; the `PausedDuration`-adjusted retry-window remaining time, which § Other criteria deliberately moves onto proposal detail; the A-19 7-day post-hoc countdown; and the trigger-review 7-day decision validity. A per-second countdown inside or adjacent to a `role="status"` node is the canonical screen-reader flood. The retry-window case is the sharpest: the same paragraph adds a once-only nearing-window announcement *and* a rendered remaining time, and only the first is bounded.

Fix: Extend the queue's sentence into a general rule — every rendered countdown or relative-time label updates at most once per minute and lives outside every live region; time-sensitive changes announce through the named once-per-item warnings in the politeness table and nowhere else.

**[Accessibility]** — WCAG 2.2.2 Pause, Stop, Hide is never claimed, dispatched, or excepted (§ EXPERIENCE.md § Other criteria)

§ Other criteria argues 2.2.1 twice at length, and names 2.5.8, 2.4.11, 2.5.7, 3.2.6 (N/A), 3.3.8 (N/A), 3.1.2 and 2.5.2 — and never 2.2.2, while the product polls every 250 ms for 8 s on every write surface, polls the queue on a cycle that re-sorts 25 rows nearest-expiry-first, auto-refreshes readiness on poll and window focus, and renders live countdowns. Some of that is defensible under the criterion's own exceptions — but the spine's whole method is to argue an exception rather than assert one, and a reviewer signing `LR-UI-CONFORMANCE` has no row to sign here. A story-dev can ship an auto-refreshing operational status page with no pause and nothing in the spine is violated.

Fix: Add a 2.2.2 bullet on the same pattern as the two 2.2.1 ones: name the auto-updating surfaces, claim the essential exception where the update *is* the information, and where it is not — the queue's periodic re-sort — state the pause or manual-refresh mechanism. The `{count} proposals changed. Refresh list.` deferral is most of that mechanism already; it needs to be claimed as one and extended past “focus inside the grid body”.

**[Governance]** — UJ-1 grants the tenant tier the boundary-setting act the lower-only rule exists to deny (§ EXPERIENCE.md:754 against :95, :234, :821)

UJ-1 step 8 reads “Readiness lists remaining blockers: cost cap and rate limit unconfigured. **She opens Cost controls and configures them.**” The IA cell says “`Agents.Administrator` may only lower them”; `cost-control-editor` says a Platform or Release Operator authors them “with no implicit defaults” and the Tenant Agent Administrator “may only lower any of them … and never raise one, so the boundary is not self-set”; UX-J5 step 4 states it correctly. Configuring an unconfigured cap is by definition not a lowering. UJ-1 is the journey Stories 5.2/5.3/5.4/5.5/5.7 are built from, so the contradiction lands in the tenant-facing activation path.

Fix: Rewrite UJ-1 step 8 as a blocker Nora observes and a Platform or Release Operator clears, with the tenant-side lower-only control named as the only thing she can do on that surface, and add the role change inline the way UX-J5 does.

**[Governance]** — The FR-11 automatic posting record has governed exits in FR-33 and no surface anywhere (§ EXPERIENCE.md:539, :262-273, :115)

FR-33 grants abandonment of an automatic `PostingFailed` record to “Tenant Agent Administrator, audited … or any current Conversation Facilitator of the Source Conversation”. The spine gives the automatic path the bounded retry lane and substitutes the Facilitator wherever FR-18 names an Eligible Approver — but § Proposal editor action rail is proposal-scoped by construction, the automatic path creates **no proposal**, and § Operational status rules carries block/clear controls and failure-record counts but no abandon or administrative-retry control for a posting record. `posting record` returns exactly one hit. Surface closure is nevertheless declared “**final for V1**. Every stated need has a surface”.

Fix: Place the automatic posting record's abandon and administrative-retry controls on Operational status beside its failure record, gated `Agents.Administrator` with the same audited, justification-required terms as the proposal path's administrative exits, state the Facilitator's half where the Facilitator's other authorities are stated, and re-scope the closure sentence.

**[Governance]** — Three Conversation Facilitator authorities are deferred to Conversations with no seam artifact (§ EXPERIENCE.md:70, :359-362, :364, :123)

§ Authorization roles is right that the Facilitator “is a Conversations role … and never become[s] a policy constant”, and § Operational status rules is right that “a Facilitator exercises these through **Conversations**, not on this route”. But `EXT-CONV-UI-1` carries exactly **four** artifact kinds and none of them is a Facilitator-facing block, clear, or abandon control. So the FR-33 rows “Block `hexa` in a Conversation”, “Clear a block …”, and the Facilitator's FR-11 abandon have an authority, a `BlockVersion` concurrency token, a confirmation-contents row and an audit obligation — and no contract, no owner, and no story. An authority with no surface and no seam entry is not governed; it is assumed.

Fix: Either add a fifth `EXT-CONV-UI-1` artifact kind for the Facilitator-exercised Conversation-scoped Agents controls, with its own tenant-scoped authorization and typed registration failure, or state plainly that the Facilitator half of A-9 and the FR-11 Facilitator abandon are **out of V1 scope** and carry that as a declared FR-33 narrowing.

**[Governance]** — Approval is irreversible in V1 and the approve confirmation never says so (§ EXPERIENCE.md:289 against :300)

`ProposalResolution` — approve renders “Selected `VersionId`, version kind, author, timestamp, full content, and the approval-time safety re-check statement”. It does not state that once `Posted` is projection-confirmed the Conversation Message cannot be withdrawn by Agents. That is the operative fact: `EXT-CONV-AI-1`'s seam 7 is recorded in the external dependency register as “requested, `Uncommitted`, and unusable”, and the deletion confirmation already concedes the consequence from the other end, stating “**a posted Conversation Message carrying erased content remains readable in Conversations** after Agents-side erasure completes”. So the spine tells the Compliance Inspector that erasure does not reach the Conversation while telling the Approver nothing at the one moment the decision is still reversible. `irreversib` occurs once in the pair, on `DeletionRequest`.

Fix: Add one irreversibility line to `ProposalResolution` — approve and to the automatic-mode **Call hexa** confirmation: the approved version becomes durable Conversation content that Agents cannot retract in V1, and Agents-side erasure does not remove it.

**[Governance]** — Audit evidence immutability is asserted nowhere in either spine (§ EXPERIENCE.md § Audit evidence rules :386-397 · § Audit governance rules :399-411)

`immutable`, `immutability`, `append-only` and `tamper` return **zero hits** across `EXPERIENCE.md` and `DESIGN.md`. § High-risk pending commands names “EventStore optimistic concurrency, deterministic command identity, and idempotency” as authoritative — a concurrency property, not an integrity one. Nowhere does the pair say that an audit record cannot be altered after it is written, that the governed `DeletionRequest` and the 365-day retention clock are the only paths by which one leaves the store, or that the surface renders any evidence of that. The brief's own standard for this area is “enough for an operator to reconstruct why an agent said what it said” — reconstruction presumes the record is trustworthy, and the spine never says it is.

Fix: State in § Audit evidence rules that Audit Evidence is append-only, that no surface offers an edit or delete affordance on a record, and that the only removals are the governed `DeletionRequest` (with its A-12 two-person rule) and the retention clock — and that a record within a live legal hold is exempt from both.

**[Governance]** — Two FR-33 launch-governance writes are rendered as read-only fields with no control (§ EXPERIENCE.md:382, :457, :115)

FR-33 grants the Release Operator “Record cost-control posture and launch readiness; record the `RQ-1` READY or NOT READY decision …; **enable production-like generation and production for a tenant** (FR-28)”. The spine renders posture as a field the panel “**renders**” and production enablement as “a separate indicator that is Success only when `RQ-1` records READY” — both reads. The `RQ-1` decision is correctly and explicitly excluded with a stated three-authority rationale, and the trigger review and SM-4 confirmation correctly became controls; posture recording and production enablement got neither a control nor an exclusion, under a closure statement that says every stated need has a surface.

Fix: Add the posture-recording control beside the posture field gated `Agents.Operator`, and either add the production-enablement act as a `high-impact-confirmation` with its `RQ-1` precondition rendered, or declare it register-recorded like `RQ-1` with the same three-authority rationale.

**[Implementation readiness]** — “Retry posting” is offered to Approvers in the rail inventory and in shipped copy, and granted to none (§ EXPERIENCE.md § Proposal editor action rail · § Voice and Tone)

First line: “The rail holds Edit, **Save**, **Discard**, Regenerate, Approve, Reject, Abandon, **and Retry posting**.” § Voice and Tone: “Posting failed. **Retry posting** or start a new Agent Call.” Three paragraphs down the same section: “**The only retry control in this rail is `administrative retry` for `Agents.Administrator`**”, and both `PostingFailed` rows of the availability table grant an Eligible Approver only abandon and audit. `reconcile-ux-2026-09-12.md` § Material source contradictions #3 raised exactly this and it is half-absorbed: the rule was corrected, the inventory line and the copy string were not. A 7.7 dev building the rail from its own first sentence ships an Approver-visible Retry the server will refuse, and a translator ships French telling an Approver to do something they cannot.

Fix: Strike “and Retry posting” from the inventory line (or qualify it as administrative), and reword the copy row for the Approver-facing case.

**[Implementation readiness]** — Three governed writes require a second party's approval and no surface lets them approve (§ EXPERIENCE.md § Audit governance rules · `audit-governance-panel`)

The staging table gives `DeletionRequest`, `LegalHoldRelease` and `ExportRequest` a requester and “Approver, required before execution”, and says “Each action is `DisabledFocusable` while its approval is absent”. `audit-governance-panel`'s accordion items are “legal hold, export, and deletion” — the *request* side. There is no approval queue, no approval control on a pending row, no notification to the approver, no route, no confirmation family for the approval act itself, and no mention of how the second party discovers a request awaiting them. § Confirmation contents by family renders “the identity of the second Compliance Inspector or Platform Operator who approved the release”, which presupposes the approval happened somewhere the spine does not build. Stories 8.1–8.3 own the commands; nothing owns the approver's surface.

Fix: Name where the second party acts — a pending-approvals item on `audit-governance-panel` is the cheapest — with its own discovery, control and confirmation row, or state explicitly that the approval is recorded outside Agents and rendered as a reference, as the Security approval reference already is.

**[Implementation readiness]** — `approver-policy-builder` has no add-row control and no way to choose a Party or a role (§ EXPERIENCE.md § Approver policy rules · `approver-policy-builder` (Stories 5.4, 5.10))

The rows specify the three buildable `ApproverPolicySourceKind` values, each row's basis and disclosure category, reorder buttons, the validation predicate and its two-half error copy, the retired `Caller` rendering, and the pending-proposals-by-prior-version count. They never say how a row is **added** or **removed**, what control picks the `PredefinedParty` (a search? a picker dialog? a free-text id? and against which contract — no Party lookup appears in § Known gaps or the IA read/write cell), or where the `TenantRole` option list comes from. The validation rule “names fewer than two predefined Parties” is unbuildable without a Party selection mechanism.

Fix: Add the add/remove controls and name the Party-selection contract, or file it as a § Contracts that must grow row with 5.4 as owner.

**[Implementation readiness]** — Operational status is grouped two incompatible ways and nothing maps between them (§ EXPERIENCE.md § Operational status rules · DESIGN.md § Layout & Spacing)

§ Operational status rules groups “readiness and runtime outcomes **by recovery**: configure Provider, fix policy, wait for approval, retry posting, inspect audit, start a new call” — six groups. `DESIGN.md § Layout & Spacing` gives accordion items “Blocked calls; Denials; Per-Conversation history and block; Cost consumption; Failure records” — five items named by **subject**. The prose then assigns content to subjects and never to recovery groups. A dev cannot tell whether a `budget blocked` count belongs in the primary region under “fix policy” or in the “Blocked calls” accordion item, and the surface carries the highest count of distinct renderable classes in the product.

Fix: Pick one axis for the accordion and state the mapping for the other, or give the primary region an explicit recovery-group-to-content table.

**[Implementation readiness]** — Four of fifteen surfaces have no view-model shape (§ EXPERIENCE.md § Information Architecture read/write cells · § Operational status rules)

Conversation context policy — “Read model defined by Story 6.2”; Content safety policy — “publish commands by Story 8.4”; Cost controls — “Budget policy contracts defined by Story 8.4 (FR-32)”. § Operational status rules closes with “the per-tenant blocked counts, per-Conversation safety history, cost consumption, and projection id/version on this panel **have no contract yet**.” The declaration is honest and correctly scoped as an epics deferral, and it still means a 6.2, 6.4 or 8.4 dev cannot write a view model from the spine. Separately, paging/sorting/refresh semantics are stated only for the two interactive grids, and the polling contract (250 ms / 8 s, nudge-filtered) is stated once globally with no per-surface refresh or staleness rule beyond `catching up` and `Stale`.

Fix: Nothing to fix beyond what is already declared — but the four cells should say what the *UI needs* from each contract (the field list) so the story writing the contract has a target, exactly as § Contracts that must grow does for the twelve rows it owns.

**[Implementation readiness]** — No route has a page title, a heading string, or a back-link label, in either language (§ EXPERIENCE.md § Route heading and focus · § Voice and Tone)

§ Route heading and focus requires every route to render “a non-blank localized heading with `HeadingTabIndex=-1` and a `PageTitle`”, every detail route to supply “a localized `BackLinkLabel`”, and the key convention is `Agents.<Surface>.<Item>`. `FcPageHeader.FocusHeadingAsync()` throws — verified — when `Heading` is blank or `HeadingTabIndex` is null, so this is load-bearing at runtime, not cosmetic. § Voice and Tone's table carries ~42 EN/FR pairs and not one is a page title, heading, nav label, back-link label, `DetailPanelAriaLabel`, field label, required marker, filter-toggle label, reset-button label, or accordion-item title — and the per-enum inventory row defers roughly fifteen enum vocabularies. The dev writes several hundred bilingual strings with no key list, into a `.resx` that already carries 599 keys per language that the spine neither inventories nor blesses.

Fix: The enum-inventory decision is right; add the fifteen route-level strings (title, heading, back-link label, and where applicable `DetailPanelAriaLabel`) as an explicit table, since those are finite, per-route, and blocking on every single surface story.

**[Implementation readiness]** — `content-safety-policy-editor`'s restricted-category rows have no control (§ EXPERIENCE.md § Component Patterns · DESIGN.md § content-safety-policy-editor (Stories 6.3, 8.4))

“Restricted categories (hate, harassment, sexual, violent, illegal activity, sensitive personal) are **editable only** with an explicitly permitted tenant use case and Confirmation Response Mode. Each restricted row shows the effective response mode beside it”; `DESIGN.md`: “restricted categories render as **editable rows**”. What is edited, and with what primitive, is never said: a tri-state select across blocked/restricted/permitted, a toggle, a checkbox plus a use-case textarea? The tenant-tier constraint is stated as a direction (“cannot move a category from blocked to restricted or from restricted to permitted”), which only makes sense against a value set the spine never renders.

Fix: Name the per-row control, its value set, and which values each tier may select.

**[Implementation readiness]** — Launch readiness carries two governed acts with no control, placement or confirmation family (§ EXPERIENCE.md § Launch readiness rules · DESIGN.md § launch-readiness-panel)

The Release Operator “**convenes and records the kill-switch trigger review** within one business day … with the four rates and their thresholds rendered”, and the Platform Operator “**confirms an SM-4 cross-tenant or unauthorized event**, beside the denial count on Operational status, gated `Agents.PlatformOperator`”. Both are governed writes whose audit consequence is immediate (a confirmed unauthorized action is the immediate FR-28 kill-switch trigger). Neither has a Fluent primitive, a placement (`DESIGN.md`'s accordion table gives the surface exactly three items — Kill switch, NFR-14 evidence, Consumed dependencies — and neither act is one), a confirmation-family row, or a story. The `launch-readiness-panel` DESIGN section mentions neither.

Fix: Give each a placement and either an existing family or a row, and file the missing ACs at the point of use as § Operational status rules already does for the 6.2 read model.

**[Implementation readiness]** — `AgentsPageStatusRegion` is required on every route and sits outside the 22-component contract (§ EXPERIENCE.md § Live regions · DESIGN.md § Components)

§ Live regions: “The carrier is a single Agents-owned `AgentsPageStatusRegion` component placed once per page body, holding both nodes; no badge, panel, or row component carries `role="status"` of its own”, and “The NFR-14 `LiveRegionAnnouncedTick` observes the localized mutation in the Agents node after render commit.” `DESIGN.md § Components` states the governing rule: “The 22 components are named and ordered identically in three places … A component added, renamed, or reordered in one must be changed in all three, or the pair stops working as one contract.” `AgentsPageStatusRegion` is in none of the three — verified — and does not exist in `src/`. It has no props, no variants, no both-nodes-empty-on-first-paint prop contract beyond one prose sentence, no placement rule relative to `FcPageHeader`, and no visual spec. Story 8.6 is the absorbing story for the 29-site live-region migration and this is the component the migration migrates *to*.

Fix: Add it as component 23 in all three places, or state explicitly that it is infrastructure outside the component contract and give it a props/placement paragraph in § Live regions.

### Medium (34)

**[Flow coverage]** — The two highest-consequence operator paths have no walked flow (§ EXPERIENCE.md § Proposal editor action rail / § Audit evidence rules)

The two highest-consequence operator paths the 2026-09-12 round spent the most effort on have no walked flow: **administrative retry / administrative abandon** of a `PostingFailed` proposal (the `MessageId` lookup with its three outcomes, `PausedDuration` arithmetic, the shared three-attempt budget, `LateConfirmed`) and the **governed compliance inspection** (computed subject set, post-hoc window, `UnreviewedInspection`). Both are specified in dense rule prose but neither is rehearsed end-to-end, so nobody has checked that the rules compose into a path a human can complete. Anders appears only as one line inside UX-J5.

Fix: Add UX-J7 (Tenant Agent Administrator recovers a stranded `PostingFailed` proposal) and UX-J8 (Anders opens a governed inspection); both would exercise rules no current flow touches.

**[Component coverage]** — Delegation convention is inconsistent, so every component costs two lookups (§ EXPERIENCE.md § Component Patterns)

Delegation is inconsistent: 11 of the 22 EXPERIENCE rows delegate (“Full rules in § X rules”) while 18 carry inline rules of 1,500–3,500 characters, and several do both — `agent-config-form` carries ~3,300 characters inline with no companion section, while the structurally similar `provider-catalog-grid` is two sentences pointing elsewhere. A consumer cannot predict from the row whether it is complete.

Fix: Pick one convention — either every row delegates to a `§ <component> rules` section, or none do.

**[State coverage]** — Two surfaces whose read model ships later have no pre-contract rendering rule (§ EXPERIENCE.md § Component Patterns (`conversation-context-policy-panel`, `content-safety-policy-editor`))

Two surfaces whose read model ships in a later story have no pre-contract rendering rule, while three structurally identical cases do. `provider-status-badge` has one (“Interim rule before Story 5.5: render `Unknown`…”, `:221`), `provider-catalog-grid` currency has one (`:312`), the disable blast radius has one (`:312`). But `conversation-context-policy-panel` (read model owned by Story 6.2) and `content-safety-policy-editor` (contracts owned by Story 8.4) have none — a dev building either before its contract lands has no instruction, and the established pattern says they should.

Fix: Add the interim state to both rows, or state once that surfaces without a stated interim do not render until their contract ships.

**[State coverage]** — No rendering rule for a legacy prohibited cost-control posture on the editor (§ EXPERIENCE.md § Launch readiness rules · `cost-control-editor`)

`launch-readiness-panel` renders `ProhibitedCostControlPosture` for `ReportingOnlyMonitoring` and `AcceptedLaunchRisk`, but Story 5.9 requires that “no legacy client, **UI option**, default, or compatibility path can convert it into an accepted hard-control posture” — and `cost-control-editor` never mentions the posture at all: not whether it is a field, not how a persisted legacy value renders, not that the two values are unofferable. The spine has an exact precedent it did not apply — retired `Caller` on `approver-policy-builder` renders “the row read-only in `{colors.status-severe}` with the typed retirement reason, and publication is blocked while it is present.”

Fix: Apply the `Caller` pattern to the posture in the `cost-control-editor` row, and cite Story 5.9.

**[Visual reference coverage]** — The Conversation seam is the one surface that genuinely needed a visual reference (§ EXPERIENCE.md § Conversation Integration Seam)

Agents contributes **three artifacts into a foreign module's layout** — the dialog body, a persistent status region “beside **Call hexa**”, and a per-message decoration slot — plus a focus-return contract crossing the boundary. Placement is conveyed entirely by the word “beside”, to an audience (the Conversations owners) who must implement it, for a dependency that is `Uncommitted` with owner `TBD`, so there is no shipped surface to point at instead. Every other surface derives its layout from inherited chrome the reader already has; this one does not.

Fix: One annotated sketch of the Conversation surface showing the three contribution points and the focus path, linked from `§ Conversation Integration Seam` and named as illustrating the `EXT-CONV-UI-1` artifact placement.

**[Bloat & overspecification]** — Rationale is inlined with rule, and much of it re-litigates superseded drafts (§ EXPERIENCE.md, throughout)

A large fraction of the inlined rationale is not the rule's justification but the **history of the previous wording being wrong**. Examples: “the former condition `where its label contract allows` described an empty set and read as permission”; “which would drop focus to `document.body` on the very two controls added to close that dead end”; “the set is deliberately unnumbered here because every count this spine has carried for it went stale”. A reader implementing the rule needs the rule and, at most, the constraint that forces it. Re-litigation of superseded drafts belongs in `.memlog.md`, which exists and is 54 KB. Conservatively this is 15–20% of the file.

Fix: Keep one clause of justification where a rule looks arbitrary; move every “the earlier wording said X and that was wrong” clause to the memlog.

**[Bloat & overspecification]** — § Shipped-code corrections is a code-audit backlog inside a durable contract (§ EXPERIENCE.md § Known gaps → Shipped-code corrections (9.0 KB, 24 rows))

It carries file names, line-level site counts and inventories — “29 sites across 20 `.razor` files”, “15 sites across 5 files, with `DisabledFocusable` at **zero** occurrences”, “roughly 115 distinct BEM class values”, “eight files” — every one of which is invalidated by the first commit that touches those files, with no refresh mechanism and no stated owner for keeping it current. The *rules* it cites are elsewhere in the spine and remain true; only the inventory rots. A story-dev wants this, but wants it in the absorbing story, which each row already names.

Fix: Reduce each row to divergence class → owning rule → absorbing story and move the site inventories into the named stories' acceptance criteria.

**[Inheritance discipline]** — `CurrencyMismatch` is rendered twice and never named (§ DESIGN.md:292 · EXPERIENCE.md:312)

The spine renders the condition twice (“a pricing currency mismatch is `{colors.status-important}`”; “A per-tenant mismatch renders `{colors.status-important}`”) but never uses the token, although the register's `ProviderReadinessReasonCode` list now carries it and every sibling code is named (`Unconfigured`, `SecretUnavailable`, `PlatformNotReady`, `Unpriced`, `DataHandlingAcceptanceLapsed`). The spine's own localization rule requires a per-enum inventory of `Agents.<Enum>.<Value>` labels; an unnamed value cannot enter that inventory.

Fix: Name `CurrencyMismatch` in both places.

**[Inheritance discipline]** — Seven FRs whose behavior is fully present are never cited by id (§ EXPERIENCE.md, throughout (FR-1, FR-6, FR-10, FR-15, FR-22, FR-27, FR-34))

FR-34 is the notable one — the spine renders `PayloadProtectionUnavailable` as a launch blocker *and* as the `DisabledFocusable` cause on export and deletion, and specifies the A-24 two-part attestation in detail, without ever naming the requirement that demands it. `epics.md` maintains an FR Coverage Map, so a consumer doing FR→spine traceability hits seven false gaps.

Fix: Cite each at its point of use; FR-34 at the `audit-governance-panel` row and in `§ Launch readiness rules`.

**[Inheritance discipline]** — A-25's override bound is not rendered (§ EXPERIENCE.md `cost-control-editor` · § Confirmation contents by family)

The spine says the audited override “carries a numeric ceiling and an expiry, both rendered in its confirmation” and that it lapses at the earlier of the two — but A-25 fixes those as **25% of the monthly cap and 7 days**, and adds that a **second override in the same month requires Product approval**, which is a gate with a UI consequence that nothing renders. The spine renders A-6's, A-7's, A-8's and A-19's numbers; this one it leaves to the server.

Fix: Render the bound and add the second-override-in-a-month condition to the `TenantBudgetUpdate` — override confirmation row.

**[Inheritance discipline]** — Stories 5.9 and 8.5 are never cited, and their subject matter is recorded as unowned (§ EXPERIENCE.md § Operational status rules)

§ Operational status rules says the per-tenant blocked counts, per-Conversation safety history, cost consumption and projection id/version “have no contract yet… deferred items for the epics skill, so ownership lands as a story acceptance criterion rather than a spine assertion” — but **Story 8.5** exists in the active backlog and owns exactly those metric contracts, including SM-C4 blocked-call share and SM-C5. A deferral recorded as ownerless against a story that owns it will be read as a real gap.

Fix: Cite 8.5 in `§ Operational status rules` and narrow the deferral to what 8.5 genuinely does not cover.

**[Accessibility]** — The grid's keyboard navigation model is unspecified and the row action pattern multiplies tab stops (§ EXPERIENCE.md § Interaction Primitives · § Grid rules)

§ Interaction Primitives says “Hover-revealed row actions also render on row focus-within”, § Grid rules gives every row a status cell plus an open action, and the deterministic focus rule places `tabindex="-1"` on a status `gridcell` — which implies grid navigation with a roving tabindex, but the spine never says so. If the implementation leaves cells and row actions naturally tabbable instead, a 25-row queue is 50 to 75 tab stops with no skip mechanism, and the shell's skip links land on `main`, not past the grid. The two readings produce completely different keyboards from the same document.

Fix: State which model the grids use — `FluentDataGrid`'s own grid-navigation mode with a single tab stop into the grid is the reading the `tabindex="-1"` rule already assumes — and say what the arrow keys do relative to the row action and the expand control.

**[Accessibility]** — `FcExpandInRowDetail` names every expanded row's detail region identically (§ EXPERIENCE.md § Grid rules · `DetailPanelAriaLabel`)

`DetailPanelAriaLabel` is `[EditorRequired]` and specified as “a per-grid localized whole string” with one entry per view key, and the envelope rule requires “a detail panel outside the virtualized grid with an always-present `role="region"`”. With more than one row expanded — which nothing forbids — a screen-reader user's region list shows N regions all called the same thing. The expand control itself is never given `aria-expanded`, `aria-controls`, or a row-identifying name, and nothing says where focus goes on expand or returns on collapse.

Fix: Make the detail region's accessible name composite — the per-grid label plus the row identity — and specify the toggle's `aria-expanded`/`aria-controls` plus focus behaviour on expand and collapse.

**[Accessibility]** — The inline confirm strip's inertness is unspecified, leaving two live commit paths in one dialog (§ EXPERIENCE.md § Interaction Primitives)

§ Interaction Primitives correctly replaces the forbidden nested dialog with “an inline confirm strip rendered in the same `FluentDialogBody`, with focus moved to it and a second `Esc` cancelling the strip”. It does not say whether the rest of the dialog goes inert while the strip is live. If it does not, a user can `Tab` from the strip straight back to Confirm and commit the governed write while an unresolved discard prompt is on screen — and on a justification-gated confirmation that Confirm is the operation the strip exists to protect. This reaches all nineteen confirmation rows.

Fix: State that the strip is the only operable region while open (the other controls `aria-disabled` or the body marked inert), and say what Confirm and Cancel do while it is live.

**[Accessibility]** — Nothing says where focus goes after **View version** applies, or what the control becomes (§ EXPERIENCE.md § Component Patterns · `version-history`)

The row gives **View version** a `DisabledFocusable` state while the editor is dirty and a focus-return target on Cancel and on Save/Discard resolution, and is silent on the ordinary success path: the user activates it, the textarea content changes under them, and § Accessibility Floor's “after async completion” rule does not reach it because this is not a route change or a state-slot swap. Nor does it say whether the control is now a no-op that still looks activatable.

Fix: Focus moves to the textarea (whose accessible name is now the newly applied version identity), and **View version** becomes `DisabledFocusable` with a “this version is already shown” reason while the draft matches the applied selection — consistent with how `response-mode-toggle` treats its Apply.

**[Accessibility]** — The re-sort deferral is scoped to focus, which is not where a screen-reader user is reading (§ EXPERIENCE.md § Proposal queue rules)

“While focus is inside the grid body a re-sort or page shift is deferred while the status node announces `{count} proposals changed. Refresh list.`” A screen-reader user in browse mode reads with a virtual cursor and holds no DOM focus inside the grid; for them the guard never arms and rows re-sort silently under the reading position — the exact defect the rule exists to prevent, on the exact user it most affects.

Fix: Arm the deferral whenever the grid is the user's current reading context, not only when it holds focus — defer whenever the grid has been interacted with or focus is anywhere within the page's grid region, and announce the deferral regardless.

**[Accessibility]** — Filtering announces nothing unless the result is empty (§ EXPERIENCE.md § Grid rules · § Live regions)

Both interactive grids render state and `needs my action` filters as Agents-owned `aria-pressed` toggles; `FcFilterEmptyState` is a permitted speaker “for their own filter and row-expansion events only”, and `FcFilterSummary` is explicitly **silenced**. So a toggle that narrows 25 proposals to 3 produces no announcement at all, while a toggle that narrows them to 0 does — and the politeness table, which asserts its own completeness, has no filter-applied row. The Agents-owned reset control is specified as “announced” and the toggles that create the state are not.

Fix: Add a polite row — `{count} proposals match the current filters` — fired once per filter change on both grids, and say that it, not `FcFilterSummary`, is the speaker.

**[Accessibility]** — Cost, quota and grace threshold crossings are not in the politeness table (§ EXPERIENCE.md § Live regions (politeness table))

`budget blocked` and `rate limited` are (assertive, correctly), and the states that *precede* them are not: the 80% consumption warning, the audited override lapsing at the earlier of its ceiling and expiry, and the tightening grace reaching its deadline and flipping the model to `DataHandlingAcceptanceLapsed`. All three are threshold crossings that occur while an operator is on the surface, all three change an adjacent badge's text beside a control whose label does not change — the precise pattern § Live regions bans — and the table declares any such omission a defect in itself.

Fix: Add the three as polite rows (`Budget consumption reached 80% of the {scope} cap`, `The cost override expired`, `The data handling grace period ended; {model} is blocked`), with strings in § Voice and Tone.

**[Accessibility]** — Streaming or partial generated output is neither specified nor forbidden (§ EXPERIENCE.md § Live regions · § Agent call)

§ Agent call renders `generating` as “In flight; no message or proposal”, and the contract has no partial-output token — so nothing streams today. But `proposal-editor`'s primary region is a `FluentTextArea` holding generated content, the editor has a preview, § Other criteria already anticipates generated text needing its own `lang` attribute, and a story-dev filling an 8-second generation wait with incrementally rendered tokens is an obvious and unprohibited move. If that content lands anywhere a live region can see it — and Agents owns exactly one polite node per route — the result is a screen reader reading a reply one token at a time, unstoppable.

Fix: One sentence in § Live regions: generated content is rendered only on completion, never incrementally, and never inside or adjacent to a live-region node; if streaming is ever introduced it renders into a non-live region with a completion announcement.

**[Accessibility]** — Launch-readiness blocker provenance classes are distinguished by appearance for two of three (§ EXPERIENCE.md § Launch readiness rules · DESIGN.md § launch-readiness-panel)

§ Launch readiness rules requires the three classes to “stay visually distinct” and gives only the third — the PRD-declared conditions — “a visible provenance qualifier”. Classes one and two are therefore separated by chip appearance alone, in a document whose own § Colors makes no-color-only mandatory for every status. Provenance is not cosmetic here: it tells the Release Operator whether a blocker is cleared in the launch-readiness register, in the runtime, or by a Product decision — three different recovery owners.

Fix: Give all three classes a visible whole-string provenance label, not just the PRD-declared one, and drop “visually distinct” as the mechanism.

**[Accessibility]** — The contrast inheritance the spine leans on hardest is the one it never names (§ DESIGN.md § Colors)

`DESIGN.md § Colors` states the right targets per theme and says “Inherited Fluent role pairs are assumed AA at the pinned package and re-verified when the pin changes” — an acceptable form of the claim. But two lines later it makes `BadgeAppearance.Tint` the calm default for *every* status badge in the product across all seven roles, and tinted badge fills are exactly where Fluent's own AA guarantee is thinnest, because the guarantee covers the foreground/background role pairing rather than any particular appearance variant. The spine's own rule elsewhere is that an inherited binding is named and verified. This one is asserted.

Fix: Name the combination explicitly — `FluentBadge` `Tint` foreground against `Tint` fill, per role, in Light and Dark — as a distinct row in the axe lane's evidence rather than folding it into “inherited role pairs”.

**[Governance]** — The visual contract does not distinguish denial from failure; only Operational status does (§ DESIGN.md:211, :227-234 · EXPERIENCE.md:522, :355)

`DESIGN.md:211` defines the role as “`{colors.status-danger}` means failure **or** denial: denied, generation failed, `PostingFailed`, `Rejected`, Provider error, superseded by another decision” — one semantic role and one glyph (`DismissCircle16`) carrying an authorization denial, a runtime failure, a human rejection and a concurrency outcome. `{colors.status-severe}` by contrast is precise and `{colors.status-important}` is precise, so the pair *can* draw the line and does not draw it here. What saves it is the no-color-only rule and § Operational status rules, which counts authorization denials as their own per-tenant class. But an operator scanning a status page sees the same chip for “you were denied” and “the Provider errored”. `superseded by another decision` sitting under “failure or denial” is a separate small wrong: it is neither.

Fix: Either split denial out of `{colors.status-danger}` with its own role and glyph, or state in `DESIGN.md` that denial and failure share the role deliberately and are separated by the mandatory visible whole-string text and by the denial's own counted class — and move `superseded by another decision` to `{colors.status-important}`.

**[Governance]** — Launch readiness states no tenant scope (§ EXPERIENCE.md:99, :368-383)

The route is entered by `Agents.Operator` — a **tenant**-scoped role — or `Agents.PlatformOperator`, and the panel renders gate records, consumed dependency status, the `EnvironmentProfile`, the kill-switch **pull history** with actor, justification and time pulled, the running `ExpiredWhileSuspended` count, the four trigger rates, and the SM-4 confirmation act. Some of those are platform-scoped gate observations by design; the pull history, the suspension counts and the trigger rates are per-tenant. § Launch readiness rules never says whose tenant any of it is, and the panel is the one governance surface where a tenant-scoped operator and a platform operator read the same grid. Every other tenant-sensitive surface in the pair states its scope explicitly.

Fix: State the tenant scope of each rendered class on this panel — platform gate observations shared, pull history / suspension counts / trigger rates tenant-scoped to the viewer's tenant, and for a Platform Operator scoped to the tenant resolved by the mechanism the second critical asks for.

**[Governance]** — The Story 8.8 compliance inspection has no entry point (§ EXPERIENCE.md:100, :388-394, :306 · DESIGN.md:246, :263-266)

The inspection's four enforced mechanics are specified in detail, its confirmation family `AuditInspection` has a contents row naming case scope, the computed subject set, the second party, advance-versus-post-hoc mode with the 7-day A-19 countdown and the running 30-day distinct-Conversation count, and its records are rate-visible on the audit list. No surface, accordion item or control **opens** one. `DESIGN.md`'s accordion table gives Audit evidence detail “Versions; Safety decisions; Governance changes” and Audit governance “Export; Deletion; History” — neither carries an inspection control, and `/agents/audit`'s id-entry form accepts an inspection-case reference, which is a lookup rather than the opening of a new case. Combined with the first critical, the Compliance Inspector can neither reach the route nor start the flow.

Fix: Place the inspection-initiation control on the Audit evidence surface as its own item, gated `Agents.AuditOperator`, opening the `AuditInspection` confirmation, and name it in `DESIGN.md`'s accordion table.

**[Governance]** — `EditorDraft` is called a family on a spine that binds “family” to the authoritative matrix (§ EXPERIENCE.md:229 against :285, :582 · launch-readiness-register.md:145)

The unsaved-changes dialog's contract is otherwise good — focus trap, `AutoFocus` Cancel, explicit `Esc`, defined focus return, and correctly no justification and no audit record. But the spine uses **family** as a term of art for the register's operation families everywhere else, the register requires every public command to declare exactly one and fails contract tests on a command with none, and `EditorDraft` is not in the matrix. A dev who reads `:229` and `:285` in the same session can reasonably declare `EditorDraft` on a command.

Fix: Rename it — `EditorDraft` **dialog contract**, not family — and state in one clause that it is a UI dialog kind and declares no operation family because it issues no command.

**[Governance]** — The Content safety layout row does not carry the absence note its sibling does (§ DESIGN.md:258 against :259 · variants at :336, :342)

The Cost controls accordion row reads “Lower-only editor; Operator editor; Overrides; History. **The Operator editor and Overrides items are absent for a Tenant Agent Administrator**, not rendered with hidden buttons”. The Content safety row reads “The current effective policy pair | Tenant stricter delta; Platform draft and publication; History” with no such note, although the `content-safety-policy-editor` component section does carry the `tenant delta only` variant. The layout table is what a dev building the page's structure reads first, and it lists a platform publication item on a route a tenant administrator can enter.

Fix: Add the matching absence clause to the Content safety row.

**[Governance]** — The payload-protection attestation renders an identity comparison against a custodied value (§ EXPERIENCE.md:378)

`PayloadProtectionUnavailable` “renders **what clears it**: an attestation with two named parts, an identity check of the loaded engine's **signed build identity against the Security-qualified value custodied through `EXT-SECRETS-1`**, and a seal-unseal-erase liveness canary”. Everywhere else the spine is explicit that an `EXT-SECRETS-1`-custodied value is a reference only, masked, never echoed; here it is described as a comparison operand on a launch-governance panel with no such statement. The match **outcome** is what the operator needs.

Fix: State that the panel renders the comparison outcome and the reference identity only, never the custodied value, and bind it to the same never-in-URLs/clipboard/diagnostics rule.

**[Implementation readiness]** — § Shipped-code corrections reads as a complete inventory and omits four day-one divergences (§ EXPERIENCE.md § Known gaps → Shipped-code corrections)

(a) **Nav orders are wholly renumbered**, not just the harness: shipped registers Proposal queue at `Order: 5`, Operational status `6`, Audit evidence `7`, Launch readiness `8`; the spine's IA table puts them at 7, 8, 10 and 9. (b) **Two shipped routes are single-gated against the spine's disjunction**: `OperationalStatus.razor` carries `[Authorize(Policy = AgentsOperatorPolicy)]` and `AuditEvidence.razor` carries `[Authorize(Policy = AgentsAuditOperatorPolicy)]`; only Launch readiness's gate divergence is named. (c) **Neither shipped grid uses `FcExpandInRowDetail` or `FcFilterEmptyState`**, which § Grid rules makes the mandatory Agents-owned set, and neither renders a filter control of any kind. (d) **Three of the four routes the spine binds to `FcAggregateDetailPage` are not on it**: only `ProposalDetail.razor` uses it.

Fix: Add the four rows with their absorbing stories.

**[Implementation readiness]** — Four routes exist in no story's registration scope (§ EXPERIENCE.md § Information Architecture · FrontComposerNavigation.razor.cs:278-281)

`/agents/context-policy`, `/agents/content-safety`, `/agents/cost-controls` and `/agents/audit-governance` have no page, no `AddNavEntry`, and no `[Authorize]` in the tree. Stories 6.2, 6.3, 6.4, 8.1–8.4 own the read models and commands; no story cell in either spine owns the **nav registration**, and the registration is non-trivial: it renumbers the existing nine, needs four glyph decisions (all four are among the six entries `DESIGN.md § Brand & Style` lists with “none” today, which `ResolveNavEntryIcon` will render as a duplicate `Apps20` — verified), and three of the four need the disjunctive `RequiredPolicy` the first critical says has no mechanism.

Fix: Name the owning story for the nav-registration delta, the way the `Agents.PlatformOperator` row names 5.3/5.7 and flags that neither has the AC.

**[Implementation readiness]** — Back paths are specified for one route pair; breadcrumbs are mentioned once, in an exclusion (§ EXPERIENCE.md § Route heading and focus · DESIGN.md (breadcrumbs, single mention))

“`BackHref` is required and non-empty on every detail route — **the parent list**”. Proposal detail → `/agents/proposals` is inferable. The Audit evidence detail's parent (`/agents/audit`? the proposal it came from, given the second route `/agents/proposals/{id}/audit`?) is not stated, and the four Constrained form surfaces the spine says are “hand-authored and carry the domain `FcPageHeader` and the same state-slot pattern directly” are never told whether that includes a back link. `DESIGN.md` names breadcrumbs exactly once — in an exclusion — and no surface, and no shell capability row, says whether a breadcrumb exists, who renders it, or what its trail is. The Tenants precedent hand-authors a breadcrumb with a per-page `.razor.css`, so the precedent the spines cite answers this and the spines do not.

Fix: One sentence stating that breadcrumbs are not used and the back link is the only up-navigation, plus the two unstated parents.

**[Implementation readiness]** — The Agents-owned filter and reset controls have no backing primitive and no state home (§ EXPERIENCE.md § Grid rules)

§ Grid rules replaces `FcStatusFilterChips` and `FcFilterResetButton` with “Agents-owned `aria-pressed` toggle buttons inside a named `role="group"`” and “The Agents reset clears the toggles' own state, is announced, and returns focus to the first toggle.” The reasoning is sound and verified. What is not stated: which primitive backs the toggle (`FluentButton` with a splatted `aria-pressed`? `FluentToggleButton`? hand-authored `<button>`?), where filter state lives (component state, query string, Fluxor — the spine rules out the shell's `DataGridNavigationState` and names no replacement), whether filter state survives navigation or a poll, and what the group's accessible name is. Both grids need this.

Fix: Name the primitive and the state home.

**[Implementation readiness]** — § Confirmation contents by family fails its own completeness test on three families (§ EXPERIENCE.md § Confirmation contents by family · epics.md:2991)

Preamble: “The roster is the register's `OperationGateMatrixVersion = 3`; **a family the UI issues and this table omits is a defect in this table**.” Story 8.6's v3 roster includes `AgentCallAcceptance`, `ProposalEdit` and `ProposalRegeneration`, and the Agents UI demonstrably issues all three — the **Call hexa** Submit, the rail's **Save**, and **Regenerate**. None has a row. The spine elsewhere deliberately excludes them, which is the right call — but the exclusion is never reconciled with the rule, so a dev applying the rule literally files three defects or builds three unwanted dialogs.

Fix: One clause naming the three non-confirmation families the UI issues and why.

**[Implementation readiness]** — `agent-config-form`'s field inventory is prose, and DESIGN and the shipped page disagree on grouping (§ EXPERIENCE.md `agent-config-form` · DESIGN.md § Layout & Spacing · AgentConfiguration.razor)

The EXPERIENCE row carries the richest activation-blocker set in the pair and is explicit that “the set is deliberately unnumbered here because every count this spine has carried for it went stale” — correct. But there is no field table: no field order, no control type per field (only expiry duration and regeneration ceiling get ranges and defaults; Agent Instructions gets no control, no size, no limit), and no required-marker assignment, while § Forms requires “a visible required marker in addition to `aria-required`” on each. `DESIGN.md`'s accordion table gives the surface five items; `AgentConfiguration.razor` ships eleven `<section>`s with ten sibling `<h2>`s and no accordion. Two devs produce visibly different forms.

Fix: A field table for this one surface; it is the most field-dense route in V1.

**[Implementation readiness]** — Story ownership still has four holes, and one of them renders on a surface (§ EXPERIENCE.md § Launch readiness rules (owning-story cell) — Stories 5.6, 5.8, 5.9, 8.5)

`5.6`, `5.8`, `5.9` and `8.5` are named nowhere in either spine. `5.9` matters: Story 5.9 is *Reject Prohibited Cost-Control Postures At Readiness Recording*, and § Launch readiness rules renders “the recorded **cost-control posture** as a field: only `Quotas`, `Budgets`, and `ProviderModelLimits` satisfy the gate, while `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` … surface as the additive `ProhibitedCostControlPosture` blocker” — while the surface's owning-story cell reads “5.5, 8.7”. `5.6` (host composition), `5.8` (EventStore protection boundary) and `8.5` (metrics calculation) are plausibly non-UX and their absence is defensible; `5.9` is not.

Fix: Add 5.9 to the Launch readiness owning-story cell.

### Low (18)

**[Token completeness]** — `rounded:` occupies a token slot but exposes no addressable token (§ DESIGN.md § frontmatter `rounded:`)

`rounded:` is a single bare `note:` with no scale names, so unlike `colors`, `typography` and `spacing` it exposes no addressable token — `{rounded.sm}` cannot be written. Nothing references it, so the pair is internally consistent, but the key is a comment occupying a token slot.

Fix: Either drop the key (Shapes prose already carries the rule) or give it the inherited scale names so the group binds like the other three.

**[Visual reference coverage]** — The no-visual-reference denominator is inflated (§ DESIGN.md:33 § Brand & Style)

Two of the 15 rows are not Agents-rendered surfaces: “API/client contract reference” is Developer docs and the spine itself says “not a FrontComposer screen”, and “Conversation invocation” is Conversation-owned. The real claim is 13 surfaces.

Fix: State the denominator as 13 rendered surfaces (of 15 IA rows).

**[Bloat & overspecification]** — EXPERIENCE.md borrows the editorial voice the shape reserves for DESIGN.md (§ EXPERIENCE.md, prose throughout)

“because safety wins”, “rather than papered over”, “two lines after § Authorization roles forbids falling back to a more permissive row”, “the comment will send a dev the other way from the code beneath it”. `DESIGN.md`'s own voice is well-judged and in the right place; this is the behavioral file borrowing it.

Fix: Flatten to statement in `EXPERIENCE.md`.

**[Bloat & overspecification]** — § FrontComposer Readiness is a pointer table smuggling a normative gap (§ EXPERIENCE.md § FrontComposer Readiness (2.1 KB))

15 rows, 14 of which say “see § X” and add nothing. It earns a partial pass by giving a reverse index from FrontComposer's own capability ids (FC-LYT, FC-TBL, FC-A11Y, FC-L10N), which no other section provides. But one row smuggles in a normative gap found nowhere else — “the 250 ms / 8 s contract exists only in this spine: the Architecture Spine names no polling, nudge, or catch-up contract, and the two shipped paths run 250 ms / 5 s and 200 ms / 8 s” — which is a Known-gaps item misfiled in a navigation aid.

Fix: Move the `IProjectionChangeDetailNotifier` gap to `§ Known gaps`; keep the rest of the table as the capability index it is.

**[Bloat & overspecification]** — § Inspiration & Anti-patterns is placed second, ahead of Information Architecture (§ EXPERIENCE.md § Inspiration & Anti-patterns (618 bytes))

It restates the PRD addendum's competitive positioning and is placed **second**, where both reference examples place it near the end before Key Flows. No downstream consumer extracts from it, and its position costs every reader two screens before the IA table.

Fix: Move it to just before `§ Key Flows`.

**[Inheritance discipline]** — The two `sources:` lists are asymmetric without a stated reason (§ DESIGN.md / EXPERIENCE.md frontmatter `sources:`)

`DESIGN.md` lists `reconcile-validation-2026-09-08.md`, `EXPERIENCE.md` does not; `EXPERIENCE.md` lists three shipped-source files and the Tenants `EXPERIENCE.md`, `DESIGN.md` lists the Tenants `DESIGN.md`. The second asymmetry is principled and obvious; the first looks accidental.

Fix: Align or annotate.

**[Inheritance discipline]** — `DESIGN.md § Do's and Don'ts` carries one labeling rule but not its twin (§ DESIGN.md § Do's and Don'ts)

`EXPERIENCE.md` rules that `Agents.AuditOperator` is “labeled Compliance Inspector on every surface, **exactly as** `ApproverPolicySourceKind.ConversationOwner` is labeled Conversation Facilitator” — one of the pair is a visual Don't, the other is not, and `DESIGN.md` never uses the words Compliance Inspector.

Fix: Add the second row.

**[Accessibility]** — The deterministic focus target's `aria-label` outlives the moment it was written for (§ EXPERIENCE.md § High-risk pending commands)

The status `gridcell` gets “an `aria-label` carrying the item identity and the new state” — correct at the instant of resolution, and permanent thereafter, because nothing says it is removed. An `aria-label` on a `gridcell` overrides its content for all subsequent reading, so from then on that column announces “Proposal {id}, Approved” in ordinary grid navigation instead of “Approved”, duplicating the row identity in every cell traversal.

Fix: Say the composite label is applied for the focus move and reverts to the cell's own text once focus leaves it, or that the identity portion is carried by the row's `rowheader` instead.

**[Accessibility]** — The confirmation's scroll region is required to be accessibly named and no name is specified (§ EXPERIENCE.md `high-impact-confirmation` · § Voice and Tone)

`high-impact-confirmation` mandates “a focusable, accessibly named scroll region” so Confirm and Cancel stay reachable at 320 px — and neither spine supplies the string or the key, while § Voice and Tone's “announced events” and “architecture tokens” rows bound the inventory obligation to politeness-table events and contract tokens, so this name falls outside both and the parity gate will never see it.

Fix: Name it per family or generically (`{operation} details`) and add it to the localization inventory.

**[Accessibility]** — `prefers-reduced-motion` is never bound to anything Agents renders (§ EXPERIENCE.md § Other criteria)

§ Other criteria's motion rule is one sentence about what reduced motion must not hide, plus `MessageBarAnimation` disabled and no `pointerdown` activation — all correct, and none of it a reduced-motion binding. The informative role's glyph is `ArrowSync16`, the conventional rotating sync affordance, carried on `Submitted`, `AuthoritativePending`, `catching up`, `awaiting projection`, `generating` and `capacity queued`; nothing says whether it animates, and if a dev animates it, six of the product's most common states spin continuously with no reduced-motion path.

Fix: One clause — Agents renders no looping or auto-playing animation, the role glyphs are static, and any progress affordance respects `prefers-reduced-motion`.

**[Accessibility]** — Zoom is proven only at 320 px reflow; 1.4.4 and 1.4.12 are unnamed (§ EXPERIENCE.md § Other criteria · DESIGN.md § Layout & Spacing)

The reflow claim is strong and per-route, and 400% zoom at a 1280 px viewport is the 320 px case, so 200% is subsumed for layout. It does not cover 1.4.4 Resize Text (text-only enlargement, where a dense grid clips rather than reflows) or 1.4.12 Text Spacing, which is the criterion most likely to break a datagrid-heavy surface with reserved status-badge space and pinned columns — and `DESIGN.md § Layout & Spacing` explicitly reserves fixed space for badges, action slots and expiry labels.

Fix: Add both to § Other criteria with the grid named as the surface to test, and put the text-spacing override in the conformance lane beside the 320 px evidence.

**[Governance]** — The proposal queue route does not admit the administrator whose exits live on the detail route (§ EXPERIENCE.md:96 against :97, :268-269)

`/agents/proposals` is `Agents.Approver` plus FR-7 resolution; `/agents/proposals/{AgentInteractionId}` additionally admits `Agents.Administrator` for administrative abandon and administrative retry. So a Tenant Agent Administrator clearing a stuck `PostingFailed` proposal has a detail route and no list to reach it from — a deep link or an audit lookup is the only path, and the spine does not say which. Narrow and fail-closed, so low.

Fix: One clause stating that the administrator reaches those proposals through the Operational status failure records or the audit id entry, or admit `Agents.Administrator` to the queue route with the listing filtered to `PostingFailed` only.

**[Governance]** — The stated reconciliation baseline is one architecture index behind its own content (§ EXPERIENCE.md:33 against :275, :860)

The opening paragraph says the spine is reconciled to “`ARCHITECTURE-SPINE.md` at `architecture_assumption_index_version` 5 (2026-09-10)”, while the file is `updated: 2026-09-12` and carries 2026-09-12 decisions in its body — the operation-family decision at `:275` and the `ARCH-A-13` retirement at `:860`. The frontmatter `sources:` list likewise stops at `reconcile-validation-2026-09-10.md`. Harmless today because the content is current; it is the citation that goes stale, and a later reader uses it to decide what to re-verify.

Fix: Advance the baseline sentence to the current index version and date, and add the 2026-09-12 architecture reviews to `sources:`.

**[Governance]** — What holds, having tried to break it (§ EXPERIENCE.md:349, :266, :788, :794 — no fix required)

The six-conjunct Eligible Approver predicate is stated once in full and never contradicted where referenced, with the server-evaluated per-version verdict and one typed reason per failing conjunct, and the UI never comparing Parties or clocks. The edit-time and regeneration-time anti-stranding guards both run before mutation or Provider work. The `Approved` pin, the deterministic `MessageId`, the pre-exit lookup with its three outcomes and `LateConfirmed`, the five non-time exits plus `PostingWindowElapsed`, the `PausedDuration` arithmetic and the expired-at-approval race all survived every sequence I constructed. The two-person staging on deletion, hold release and export is exact, including the requester's own approval being refused. The single two-key `not available` state leaves no per-cause disclosure channel I could construct. The `Caller` source retirement, the FR-13 three-part listing predicate, the block/clear authority split across its four rows, and the content-safety direction rule are all correct against their sources.

Fix: No fix.

**[Implementation readiness]** — `DESIGN.md` cites no `OperationGateMatrixVersion`, so its confirmation section has no roster anchor (§ DESIGN.md § high-impact-confirmation)

`EXPERIENCE.md` now correctly cites `= 3` in all three normative places (lines 240, 285, 582 — verified; this closes the UX half of `review-2026-09-12-verified-current.md` VC-H3). `DESIGN.md § high-impact-confirmation` describes the dialog with no version reference, which is fine while the pair is read together and becomes a drift risk the moment v4 lands.

Fix: One cross-reference.

**[Implementation readiness]** — `FcCommandPalette` is characterized as rendering on every Agents route; it is not rendered as markup (§ EXPERIENCE.md § Live regions · FrontComposerShortcutRegistrar.cs:161)

Verified: it is a dialog body opened via `IDialogService.ShowDialogAsync<FcCommandPalette>` (ctrl/meta+K) and from `<FcPaletteTriggerButton />` in the shell header. The conclusion the spine draws — Agents duplicates none of the four and cannot silence them — is unaffected and correct; the characterization is not. `FcDensityAnnouncer` is likewise **outside** the `main` landmark while `FcProjectionConnectionStatus` and `FcPendingCommandSummary` are inside, which the spine states correctly.

Fix: One word.

**[Implementation readiness]** — The CSS delivery decision points away from the precedent the spines cite (§ DESIGN.md § Layout & Spacing)

It records “one Agents stylesheet only for layout the design system does not own”, against ~115 distinct BEM class values that are per-component (`proposal-version-history__marker--approved`, `conversation-agent-call-panel__status`). The Tenants precedent named in both `sources:` blocks ships per-component scoped CSS — `TenantDetailPage.razor.css`, `TenantAuditPage.razor.css`, `GlobalAdministratorsPage.razor.css`. Scoped `.razor.css` is the pattern the class names were authored for.

Fix: Either adopt the scoped-CSS precedent or say why one stylesheet wins over it.

**[Implementation readiness]** — `RegisterDomain`'s XML doc still says eight where nine `AddNavEntry` calls follow (§ src/Hexalith.Agents.UI · Composition/AgentsFrontComposerRegistration.cs)

Confirmed unchanged in the tree; correctly carried by the § Shipped-code corrections table and absorbed by 5.7. Noted only because the row's own wording (“the comment will send a dev the other way from the code beneath it”) remains accurate and unactioned.

Fix: Carried; no spine change required beyond the existing absorption by Story 5.7.

## Prior-round closure

- **Accessibility:** all 22 round-4 findings closed, including both criticals and the one regression; the three regressions recorded in `reconcile-validation-2026-09-10.md` § 2 are also closed. No regressions.
- **Governance:** 20 of 21 prior findings resolved, 1 resolved-with-a-new-defect-in-the-replacement (the Audit evidence IA cell, re-filed as this round's first critical), 1 unchanged-by-decision. All six prior criticals and all three regressions genuinely closed and none reopenable. Nothing regressed and nothing was left unresolved — the cleanest prior-round closure the pair has recorded.
- **Implementation readiness:** 24 of 26 round-4 findings closed, 1 mostly closed (five unknown stories, re-filed narrowed as medium), 1 open-and-carried (the stale `RegisterDomain` XML doc). No round-4 fix regressed; this round's criticals are new surface area exposed by the round-4 fixes. Of the four corrections `reconcile-ux-2026-09-12.md` asked of UX, three are absorbed and one is half-absorbed.

## Mechanical notes

- **Name consistency (rubric):** clean. The 22 component names are byte-identical and identically ordered across `DESIGN.md` frontmatter, `DESIGN.md § Components` `###` headings, and `EXPERIENCE.md § Component Patterns` (verified by diff). Shipped-versus-spine name aliases are declared rather than left to collide. One near-miss: `EXPERIENCE.md` invents confirmation family **`EditorDraft`**, a name not in `OperationGateMatrixVersion = 3` and not declared as UX-owned; the name should be marked as UI-local.
- **Cross-references (rubric):** no broken references found in either direction. All internal `§` targets resolve; all four external `§` targets resolve. All 39 `sources:` paths resolve on disk. One dangling *concept* reference: “Conversation status entry” in UJ-3.
- **Roster precision (rubric):** `§ Confirmation contents by family` and `§ High-risk pending commands` both say “the roster **is** the register's `OperationGateMatrixVersion = 3`”. The v3 matrix holds 23 families; the confirmation table covers 15. The 8 uncovered are correctly uncovered, but “the roster is v3” read literally instructs a consumer to build a `SystemTimer` confirmation dialog. State the subset rule (human-initiated, browser-issued families).
- **Frontmatter completeness (rubric):** both files carry `name`, `description`, `status: final`, `created`, `updated`, `sources`. `DESIGN.md updated: 2026-09-10`, `EXPERIENCE.md updated: 2026-09-12` — the two-day gap is the substantive lag, not a formatting issue.
- **Mermaid syntax (rubric):** no Mermaid diagrams in either file. `EXPERIENCE.md` has one fenced ```text block (lines 419–424) holding four state-flow arrows; plain text is the right tool for four transitions.
- **Counts that will rot (rubric):** the spine cites the confirmation roster by matrix version and leaves the Provider eligibility set deliberately unnumbered, then carries hard counts elsewhere with the same failure mode — “exactly twelve names”, “all 15 Information Architecture surfaces”, “**five** reference kinds”, “twelve entries at nav order 0 to 11”, “29 sites across 20 `.razor` files”, “roughly 115 BEM class names”. Apply the stated principle consistently.
- **`{colors.status-subtle}` is defined twice in `DESIGN.md § Colors`** (accessibility), and the first definition precedes the second: the `Draft` bullet opens “`{colors.status-subtle}` **also** carries lifecycle `Draft`” two lines *before* the bullet that says what the role means. Merge them.
- **Two different citation forms for the confirmation roster** (accessibility). § Audit governance rules still says “all ten AD-12 lock-bearing families” and enumerates ten parenthesised groups, while the family table carries nineteen rows. Both are true — ten lock-bearing *families*, nineteen *operations* — but nothing says so. One clause closes it.
- **`reconcile-validation-2026-09-10.md` § 8 is now stale** (accessibility). It labels all seven Architecture / Architecture + Conversations items open; `reconcile-ux-2026-09-12.md` closes 7 of 7 and, in its post-fix passes, closes M-1 through M-4 as well. Only the reconciliation file is behind, not the pair.
- The `agent-response-marker` row and `EXT-CONV-UI-1`'s four artifact kinds agree; the “three artifact kinds” wording reconciliation asked UX to replace is gone (accessibility).
- **Frontmatter and closure** (governance). Both files carry `status: final`. `EXPERIENCE.md:115` declares “Surface closure status: **final for V1**. Every stated need has a surface” while three FR-33 grants have no control and the compliance inspection has no entry point. Either the closure sentence is scoped to what is closed, or the four are added.
- **Zero-hit checks run for the governance review.** `immutable` / `immutability` / `append-only` / `tamper`: 0 across both files. `tenant selector` / `tenant context` / `tenant switch`: 0 in `EXPERIENCE.md`. `ProposalEdit` / `ProposalRegeneration` / `AgentCallAcceptance` as operation families: 0 across both files. `irreversib`: 1, on `DeletionRequest` only. `OperationGateMatrixVersion = 2`: 0 — the v2 citations the 2026-09-12 reviewers found are gone.
- **Deferred items correctly recorded at the point of use** (governance): the kill-switch story gap, the Security-approval-reference AC gap, the Story 5.5 by-Provider/model query, the Story 6.2 read-model ownership, and the `Agents.PlatformOperator` registration AC gap are each stated where a reader meets them, with an owner. The pattern is right and applied consistently.
- **`EXT-CONV-UI-1` remains `Uncommitted`** (governance). `:127` correctly states that the 2026-09-12 synchronization “closes the contract mismatch but does not unblock the story”, so Story 6.7 stays blocked from `ready-for-dev` under FR-21.
- **Component parity holds exactly** (implementation readiness): `DESIGN.md components:` = 22, `###` sections = 22, `§ Component Patterns` rows = 22. `AgentsPageStatusRegion` is the 23rd required component and is in none of the three.
- **`FcFluentIcons`** (implementation readiness): the spine's “exactly twelve `TryCreate` names” and thirteen typed 16 px factories both verify against source. `Play16(IconVariant)` is a fourteenth 16 px method but is variant-parameterized and `TryCreate`-reachable, so the spine's wording is accurate.
- **`FrontComposerNavigation.ResolveNavEntryIcon`** does fall back to `Apps20` for both an unset and an unrecognized key. `DESIGN.md § Brand & Style`'s “There is no no-glyph path” is exactly right, and the six duplicate-`Apps20` rail tiles it accepts are real.
- **`FcAggregateDetailPage`**: `BackHref` defaults to `string.Empty` (spine correct) but `BackLinkLabel` is `string?` defaulting to **`null`**. The spine's consequence — “an unset label yields an anchor with no accessible name” — is correct either way.
- **`FcPageHeader.FocusHeadingAsync()`** throws `InvalidOperationException` on both a blank `Heading` and a null `HeadingTabIndex`; the spine's five-real-state-slots rule is load-bearing and correct. `FcDestructiveConfirmationDialog.razor:18-22` does hard-code the literal `Cancel`, so the spine's non-reuse rule is correct.
- **Live-region inventory re-verified exact** (implementation readiness): 29 matches across 20 `.razor` files, plus 3 `.cs` XML-doc-comment matches correctly excluded. `DisabledFocusable` re-verified at **0** occurrences in `src/`; `Disabled=` at 15 sites across 5 files. `AgentsResources.resx` and `.fr.resx` both carry 599 `<data name=…>` entries; `Agents.Surface.NotAvailable.*` confirmed absent.
- **`IProjectionChangeDetailNotifier.ProjectionChangedDetail`** exists and is registered through `EventStoreServiceExtensions.cs:88` via `TryAddScoped`, confirming the spine's warning that a page injecting it into a host that skipped the EventStore service call fails at first render.
- **No source artifact was modified by any of the four reviews.** `DESIGN.md` and `EXPERIENCE.md` were read only.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-governance.md`
- `review-implementation-readiness.md`
