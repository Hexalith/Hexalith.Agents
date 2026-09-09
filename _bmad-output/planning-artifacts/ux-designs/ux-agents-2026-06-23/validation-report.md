# Validation Report — agents (Hexalith Agents)

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`
- **Run at:** 2026-09-09
- **Lenses:** rubric walker · accessibility · governance · implementation readiness (all with regression duty)
- **Findings:** 96 filings — 7 critical, 35 high, 33 medium, 21 low. FR-33 was filed independently by two lenses, so the criticals are **6 distinct issues**; they are consolidated into one entry below.
- **Regression** against the pre-Update run (84 prior findings): 57 resolved · 23 partial · 2 unresolved · **0 regressed** · 2 unchanged by prior decision

## Overall verdict

Mechanically this is the cleanest state the pair has been in. The 22 component names are identical *and identically ordered* across the DESIGN frontmatter, the DESIGN sections, and the EXPERIENCE Component Patterns rows (diff-verified, so the three-way parity rule holds under test); all 19 distinct `{path.to.token}` references resolve across 154 occurrences; all 36 source paths resolve on disk; every internal `§` cross-reference resolves; the `FcFluentIcons` inventory now matches its source member-for-member; and the repo's UX law is respected throughout.

**What breaks the run is source freshness, not mechanics.** Both spines declare themselves "reconciled to the PRD of 2026-09-08". `prd.md`, which both list as a source, now carries a 2026-09-09 reconciliation whose centrepiece is **FR-33, a thirteen-row authorization matrix over six named roles that OQ-21 declares authoritative for every authorization rule in the product**. The rubric and governance lenses converged on this independently. FR-33, Platform Operator, Release Operator, Compliance Inspector, the kill switch, the Agents-owned per-Conversation block, `NoEligibleApprover` and `SourceConversationUnavailable` do not occur in either spine, in `.memlog.md`, or in either reconciliation. Built strictly from these two files, a tenant administrator holding `Agents.Administrator` publishes the platform Content Safety Policy — and may *relax* it — raises their own cost caps, and grants themselves an audited override of a reached cap. `prd.md:418` says of exactly these acts "so the boundary is not self-set" and "never the Tenant Agent Administrator".

**Separately, the two rewrites the last Update was proudest of each bought their tidiness with a mechanism that does not exist.** The live-region rewrite hands the entire post-submit announcement set for the product's only V1 invocation path to a pair of nodes living inside a dialog the same sentence closes on Submit. The `aria-disabled` rule is stated spine-wide as `FluentButton.DisabledFocusable` and then applied to version radios, where that parameter does not exist. The editor's new dirty-state rules block Approve and the version radios "until you save or discard" while the action rail contains neither control. And `retry posting`, the one new Conversation write, was filed under the one operation family that skips the membership and safety gates. These are defects the Update *introduced*, and each reads as solved.

The honest summary of that Update: it closed the mechanics it aimed at, and closed them accurately — 57 of 84 prior findings are verifiably resolved, **nothing regressed**, and the areas the lenses attacked hardest (provider secrets, the currency circularity, terminal-state and double-post integrity, tenant isolation, the expiry/approval race) held under re-attack. But "every finding dispositioned" is true in form: 23 findings are partial, and several of those are fixes that read as closed yet cannot be built as written.

## Category verdicts

- Flow coverage — **adequate**
- Token completeness — **strong**
- Component coverage — **strong** (no findings)
- State coverage — **adequate**
- Visual reference coverage — **strong**
- Bloat & overspecification — **adequate**
- Inheritance discipline — **thin**
- Shape fit — **strong** (no findings)

## Findings by severity

### Critical (6 distinct, 7 filings)

**[Inheritance discipline / Governance]** — PRD FR-33, the authoritative role matrix, is absent from both spines and the spines' authorization model contradicts it on six rows (§ prd.md:406-436, OQ-21 at :874 · EXPERIENCE.md:31, :59-80)
FR-33 resolves *every* authorization rule in the PRD to one of six roles at a stated scope. Grepping both spines for `FR-33`, `Platform Operator`, `Release Operator`, `Tenant Agent Administrator` and `Compliance Inspector` returns zero hits. The IA table's five `RequiredPolicy` constants contradict FR-33 on cost caps/rate limits, cost-cap override, Content Safety Policy publication, per-tenant Provider/model enablement, approved deletion, and the two administrative `PostingFailed` rows. FR-33 also requires every authorized operation to record its role basis, which the audit field list does not carry.
Fix: add an § Authorization roles section mapping the six roles to policy constants with scopes; re-derive the `RequiredPolicy` column row by row, splitting read from write per surface; state that every confirmation's authorization-basis line and every audit record names the role and scope; add `UnretiredAssumption` as a readiness blocker class; update both header lines to name the PRD revision actually reconciled to.

**[Governance]** — Cost controls put the cap boundary and the cap override in the hands of the party they bound (§ EXPERIENCE.md:67, :187 vs prd.md:418-419, FR-32 at :625)
The tenant-scoped surface authors caps, rate limits and the audited override. The PRD reserves configuration to Platform or Release Operator with the tenant able only to *lower*, and the override to "Platform Operator only". No monotonicity rule exists anywhere in the spine, so a tenant at 100% fail-closed raises its own cap or grants itself an override, both rendering as a normal `TenantBudgetUpdate` with a justification the same actor typed.
Fix: split the surface; give `Agents.Administrator` a lower-only editor that rejects any value above the current one with a typed reason, with the override item absent rather than disabled.

**[Governance]** — The Content Safety Policy is published from a tenant-scoped surface, and the spine lets a tenant *relax* it (§ EXPERIENCE.md:66, :186 vs prd.md:420)
The PRD assigns publication to "Platform Operator with Security approval" at Platform scope and permits a tenant only to add restrictions through a stricter mode-specific policy. Permitting a restricted category is a relaxation. The Security-approval condition has no surface, no confirmation line and no pending state, so one tenant administrator publishes a policy permitting hate or sexual content with no second party involved.
Fix: platform surface is Platform Operator plus a rendered Security-approval stage; the tenant surface publishes only a stricter delta and cannot move a category toward permitted; the confirmation states the direction of change and refuses a relaxation on the tenant surface.

**[Governance]** — The two-person rule on deletion is deferred to a decision the PRD has already taken (§ EXPERIENCE.md:189, :73 vs prd.md:428-429, A-12)
`prd.md:429` already states "Platform Operator with Compliance Inspector approval", and puts legal hold and export with the Compliance Inspector — a different role from the deletion requester. The spine collapses all three onto one `Agents.AuditOperator` on one route and defers the question, so V1 as specified ships single-actor evidence deletion against a written requirement.
Fix: remove the deferral; `DeletionRequest` requires Platform Operator plus a separately authorized Compliance Inspector approval stage, the requester's own approval refused with a typed reason, and both justifications rendered on every deletion row.

**[Governance]** — `retry posting` is filed under the operation family that omits the two gates a Conversation write needs (§ EXPERIENCE.md:222, :231 vs launch-readiness-register.md § Gate Sets · prd.md:397)
`ProposalResolution` requires neither `LR-CONVERSATIONS-MEMBERSHIP-POSTING` nor `LR-SAFETY`; a separate `ConversationPosting` family requires both. The register states the matrix is the single contract consumed by API, BFF, UI, workflow and projection, and that consumers may not maintain local subsets. The spine identifies retry posting as a Conversation write "governed as one" and then assigns it the family that governs it as something else, so every retry is admitted with the membership and safety gates unevaluated. Introduced by the Update that added the feature.
Fix: `retry posting` is family `ConversationPosting`; the confirmation's authorization-basis line names both families where resolution and write differ.

**[Accessibility]** — The `ConversationAgentCallPanel` live-region pair is destroyed by the very event it exists to announce (§ EXPERIENCE.md:183, :95, :92 · DESIGN.md:315)
Both live nodes and every `FluentBadge` status row sit inside the panel, which *is* the dialog body, and the dialog closes on Submit. Every outcome of the call — including `denied` and `generation failed` — announces into a node that no longer exists, for every user, on the product's only V1 invocation path; the status disappears visually too. `EXT-CONV-UI-1`'s two declared artifact kinds can host no persistent region beside the trigger. Recorded as applied and closed in both the spine and the reconciliation.
Fix: split the artifact — a separate persistent Agents-owned status region contributed to the Conversation surface next to **Call hexa**, mounted for the lifetime of the Conversation view, added as a third artifact kind on `EXT-CONV-UI-1` alongside a focus-return contract for the Conversations-owned trigger.

### High (35)

**[Governance]** — A caller can approve the reply to their own Agent Call (§ EXPERIENCE.md:181, :617 vs prd.md:91, :234, :501)
The spine blocks approval when the Approver "is the sole Approver of their own call"; the PRD says the caller is never an Eligible Approver. The word "sole" inverts the rule: with two or more resolvable Approvers — the normal case — the caller is let through. No laundering is needed, because the Provider authors the text so the last-editor test never fires. `CanCurrentUserApproveSelectedVersion` is specified to carry the spine's predicate, so the server-side rule named is the wrong rule.
Fix: replace with the glossary predicate in full — resolved by the current Approver Policy, current Participant with read access, not the caller, not the last editor of the selected version.

**[Governance]** — There is no edit-time eligibility check, so a self-inflicted deadlock is reachable (§ EXPERIENCE.md:216 vs prd.md:235, :66)
The PRD rejects an edit "unless at least one Eligible Approver would remain after it, the editor and the caller both excluded". Under the spine, a lone Approver edits, cannot approve her own edit, and the proposal sits until `Expired` — and the audit record reads as an expiry, not a governance refusal.
Fix: Edit is `DisabledFocusable` with the typed reason whenever no Eligible Approver would remain; the reason names that a second Approver is required.

**[Governance / Inheritance discipline]** — `Caller` is retained as a live Approver Policy source though FR-7 retired it (§ EXPERIENCE.md:178 · DESIGN.md:295 vs prd.md:215, :501, OQ-14 at :867)
The builder offers a source on the FR-23 deprecate-and-reject register that every server must refuse, so an administrator can publish a policy that silently contributes no Approver. DESIGN calls the list "the closed `ApproverPolicySourceKind` list".
Fix: drop `Caller` from the buildable list; if it must stay visible for legacy policies, render it read-only with the typed retirement reason and block publication while present.

**[Governance]** — `Agents.PlatformProviderAdministrator` does not exist, and the spine states its registration as fact (§ EXPERIENCE.md:80 vs AgentsFrontComposerRegistration.cs)
The shipped file declares exactly four constants and gates `/agents/providers` on `AgentsAdministratorPolicy`. The corrections table catches nine smaller divergences and misses this one, so the single new authorization constant in the whole Update has no owning story and reads as already done.
Fix: add two corrections rows (the missing constant, the tenant-scoped `RequiredPolicy`) with absorbing story 5.3 or 5.7, and state that the constant is *to be* registered.

**[Governance]** — The Provider catalog read path shows a tenant administrator what FR-33 reserves to the Platform Operator (§ EXPERIENCE.md:63, :179, :292 vs prd.md:417, A-10)
FR-33 states a tenant sees only Providers/models enabled for it, with secret references and configured state visible only to the Platform Operator. The spine scopes the *mutation* correctly and never scopes the *read* — no per-tenant enablement filter, no suppression of configured state — and the status badge's reason vocabulary includes `Unconfigured` and `SecretUnavailable`, disclosing secret-configuration state even where the reference is masked.
Fix: filter the tenant read to enabled models; collapse configured state, `ConfigurationReferenceId` and secret-derived reason codes to one undifferentiated `Blocked` for non-Platform-Operator readers.

**[Governance]** — A zero-priced model turns budget enforcement off, and the spine treats it as a warning (§ EXPERIENCE.md:179 · DESIGN.md:203 vs prd.md:616, :620)
A model priced at zero reserves zero, settles zero, and never reaches 80% or 100% on any cap, so `BudgetBlocked` becomes unreachable for every tenant using it — from one platform-scoped edit, with no readiness consequence. The budget bypass is one field.
Fix: a zero unit price is a Provider readiness blocker, not a warning; the mutation confirmation names the affected tenant count; Cost controls renders "cost enforcement inactive for {model}".

**[Flow coverage / Governance]** — PRD FR-28's per-tenant kill switch has no surface, no state, no copy, and no flow (§ prd.md:625, :422, :622 · zero grep hits in both spines)
FR-28 specifies UI-visible semantics precisely: proposals awaiting a decision may be rejected or abandoned but *not* approved, `Approved` and `PostingPending` complete on their own terms, nothing is deleted. It is a Platform/Release Operator gesture with three named triggers, and FR-18 names it as one of four typed system-abandon reasons.
Fix: add a kill-switch control under the Release/Platform Operator role with its own confirmation family and justification; a `kill switch active` readiness state; a proposal-editor variant with Approve absent for this reason while Reject and Abandon remain.

**[Flow coverage / Governance]** — OQ-16 inverts the ownership the seam section pins, and the spine describes the design the PRD rejected (§ EXPERIENCE.md:104 vs prd.md:869, FR-2, :428 · addendum.md § Options Considered)
The addendum records that Conversations-notifies-Agents was *rejected* in favour of an Agents-owned per-Conversation block, set and cleared by the Tenant Agent Administrator or Conversation Facilitator with every transition audited. The spine specifies the rejected alternative and has no surface, control, state, confirmation family, audit rendering or copy for the block it should own. OQ-16's three-part membership step is also uncarried.
Fix: add block/re-admit as governed writes under the FR-33 authorities with justification and audit; restate the seam paragraph.

**[Governance]** — A denied or unauthorized attempt leaves no visible evidence anywhere (§ EXPERIENCE.md:191, :192 vs prd.md:826 SM-4, :622, :454 FR-20)
The `not available` state is correct as isolation, but nothing renders the other side of it: Operational status counts calls blocked by context, safety, cost, rate limit and capacity but not authorization denials, and the audit panel enumerates only acts that happened. SM-4 requires zero unauthorized actions to be *demonstrable*, and a confirmed unauthorized action is the immediate kill-switch trigger. A trigger nobody can observe is not a control.
Fix: add a denied-attempt evidence class (actor, tenant, operation family, resource class, denial reason code, timestamp — without the inaccessible resource's identity) and a per-tenant denial count to Operational status.

**[Governance / Inheritance discipline]** — Compliance inspection is absent, and the fail-closed rule makes retained evidence permanently uninspectable — which FR-24 forbids in terms (§ EXPERIENCE.md:192 vs prd.md FR-24, :94, :424, OQ-21 at :874)
FR-24: "Retained evidence is never made uninspectable by the loss of its Conversation." It requires inspection scoped to a named Conversation or case identifier, a recorded justification, second-party approval or post-hoc review, rate-visibility on an administrator-readable surface, and the inspection itself recorded as Audit Evidence. None of the five mechanics appears. The failure runs both ways: a legitimate inspection is blocked, and the one read path meant to be audited as a read is not.
Fix: add a compliance-inspection flow to `/agents/audit` with all five mechanics, and a rule that evidence whose Source Conversation is gone renders under compliance inspection only, never as `not available`.

**[Governance]** — The queue's listing rule is wider than FR-13's, so a never-resolved Approver sees and counts proposals (§ EXPERIENCE.md:180, :190 vs prd.md:338)
FR-13 adds the part the spine dropped: "Proposals on which the requester was never resolved as an Approver are not listed, counted, or otherwise disclosed." Since `Agents.Approver` is tenant-wide and read access is a Conversations fact, any holder who happens to be a Conversation participant sees, counts and opens proposals the Approver Policy never resolved them for. The pending count inherits the same rule.
Fix: the predicate is resolution under the proposal's Approver Policy snapshot **and** current read access; never-resolved requesters get `not available`, no row, no count, no filter suggestion.

**[State coverage / Governance]** — Approval freezes expiry is not carried, and the spine's rule contradicts it (§ EXPERIENCE.md:334 vs prd.md:395, OQ-3)
The spine renders `Expired` "whenever `ExpiresAt` has passed on any read" with no state carve-out; the PRD says `Approved`, `PostingPending` and `PostingFailed` never expire. Implemented literally, an `Approved` proposal whose window lapses during a slow post renders `Expired`, the rail collapses to "Start a new Agent Call", the retry path and administrative exits vanish, and audit reports a decision that expired rather than a post that failed.
Fix: scope the rule to `Pending`, `Edited`, `Regenerated`; state the freeze in the three post-decision rows.

**[State coverage / Governance]** — The exits from an exhausted `PostingFailed` contradict FR-18, stranding a proposal the PRD guarantees always has an exit (§ EXPERIENCE.md:219, :331, :69, :222 vs prd.md:396, :425-426)
FR-18 gives four exits so that "a `PostingFailed` proposal therefore always has an exit and always reaches the §9 retention clock"; the spine gives one. Since detail is `Agents.Approver` only and retry requires *current* Conversation read access, a proposal whose Source Conversation became inaccessible is unreachable by anyone and never reaches retention.
Fix: replace the row with the four exits; add administrative abandon (no Conversation read access required) and administrative retry as actions under a second policy with their own confirmation contents.

**[Governance / State coverage]** — The retry bound was moved from Architecture to tenant configuration and lost its time window (§ EXPERIENCE.md:222, :177 vs prd.md:396, A-7 at :749)
The PRD fixes it at "at most 3 attempts over 15 minutes", owner Architecture. A tenant-configurable attempt count with no upper bound and no window is a retry storm against Conversations with an audit trail saying every attempt was authorized. The spine also invents a configuration field on a form whose inventory does not list it.
Fix: 3 attempts within 15 minutes of the first failure, not tenant-configurable; render attempts-used and time remaining read-only; window expiry is a distinct exhaustion reason.

**[Accessibility]** — The editor blocks Approve and version selection "until you save or discard", and neither control exists (§ EXPERIENCE.md:216, :182, :212 · DESIGN.md:307)
The action rail is enumerated three times as Edit, Regenerate, Approve, Reject, Abandon, Retry posting. There is no Save and no Discard; the only stated way out of the dirty state is to leave the page. A user who types one character faces a blocked Approve pointing at a Save that is not there. A functional dead end before it is an accessibility one, created by the Update's own dirty-state rules.
Fix: put Save and Discard in the rail in all three enumerations, give each an announcement, and state where focus lands after each.

**[Accessibility]** — No blocked control anywhere in the spine is programmatically associated with its reason (§ EXPERIENCE.md:413, :101, :189, :216, :397 · DESIGN.md:291, :359)
Every blocked state renders its explanation as an *adjacent* element; there is no `aria-describedby` anywhere. A screen-reader user tabs to Approve, hears "Approve, unavailable", and must explore the region to learn whether they are blocked by a dirty editor, segregation of duties, expiry, a lost connection or another Approver — five different recoveries. Spans nine confirmation families, six rail actions, the version radios, **Call hexa**, export and deletion.
Fix: wherever a control is `aria-disabled`, its reason is a localized whole string referenced by `aria-describedby` from the control itself, and the same string is the visible adjacent text; assert it in the `LR-UI-CONFORMANCE` lane and the AT matrix.

**[Accessibility]** — `DisabledFocusable` does not exist on `FluentRadio`, so the version-radio rule cannot be built as written (§ EXPERIENCE.md:182, :216, :413 · DESIGN.md:311)
At the pin the parameter is declared on `FluentButton` and `FluentCompoundButton` only. The spine's hand-authored escape hatch means building a radiogroup with roving `tabindex` — which the repo UX law forbids when a Fluent component exists, and which reintroduces the original hazard, since in a radiogroup the arrow key *is* the selection. The shipped code has the opposite defect (a custom listbox), so Story 7.2 will be told to move *to* a component that cannot express the rule.
Fix: choose a buildable binding and say which — a `FluentButton`-per-version selection, or keep `FluentRadioGroup` and handle dirtiness through the unsaved-changes confirmation on selection change.

**[Accessibility]** — Nothing re-focuses the heading when a detail route changes state slot, and the previous heading element is destroyed (§ EXPERIENCE.md:442, :444 vs FcAggregateDetailPage.razor)
Each non-ready state is a *separate* caller-supplied `RenderFragment`, so the loading slot's `FcPageHeader` and the ready slot's are two instances with two `h1`s. The focus rule covers navigation, queue-to-detail and forced refresh — not a slot swap, which is exactly what the ordinary cold load of all eight Constrained routes is. Focus is lost to `document.body` with no announcement, likewise on Ready→`Unavailable` and Ready→`NotFound`. `FocusHeadingAsync()` also throws when `HeadingTabIndex` is null, so a slot omitting it fails in production on the error path.
Fix: add the transition to the focus rule and require `HeadingTabIndex="-1"` on the header in *every* slot.

**[Accessibility]** — Events that no node owns, on the exact states users wait in (§ EXPERIENCE.md:433-436, :265, :363-367, :346-349, :397, :424)
The politeness table is presented as exhaustive, yet `Submitted`, **`awaiting projection`** (the catch-up-exhaustion outcome after an 8-second wait on every high-risk write), `pending in another session`, `safety blocked at approval`, `stale proposal`, `regeneration ceiling reached`, `duplicate submission (idempotent)` and the *recovery* half of connection loss are announced by nobody. In each case the change is carried by an adjacent badge whose text mutates — and the spine forbids that badge from being a live region.
Fix: add the missing events to the table, or state explicitly which are deliberately silent and what carries them instead.

**[Accessibility / Implementation readiness]** — The Shipped-code corrections inventory undercounts the drift for both new rules (§ EXPERIENCE.md:635, :636 vs 15 `role="status"` sites across 13 files; zero `DisabledFocusable` in `src/`)
The table names five badge components; the shipped tree carries live regions in twenty files, including a private polite/assertive node per resolution command — on the exact controls whose announcements `AgentsPageStatusRegion` now owns. Double announcement at the approval moment is the defect the rewrite exists to prevent, and it survives a story-dev who works the table honestly. Same for the `Disabled` row, which names one control where eleven pages diverge.
Fix: restate both rows as classes of divergence with site counts, or split into per-story rows so each story pays its own share.

**[Accessibility]** — The justification gate does not say which binding it uses, and the wrong one empties the dialog's tab cycle (§ EXPERIENCE.md:189, :193, :413 · DESIGN.md:355)
An empty justification is a blocked-but-explicable state, so the spine's own rule requires `DisabledFocusable` — but the confirmation rows never say so. A developer reaching for `Disabled` produces a focus-trapped dialog whose only reachable controls are the textarea and Cancel, with nothing explaining that Confirm exists.
Fix: state that Confirm is `DisabledFocusable` while the justification is empty or whitespace, with an `aria-describedby` reason, and that the same rule governs every gated Confirm.

**[Accessibility]** — `superseded by another decision` and the action-rail table give opposite answers, and the loser loses focus (§ EXPERIENCE.md:342 vs :217, :364)
One rule says resolution controls become `aria-disabled` on a concurrency rejection; the other says Edit/Regenerate/Approve are *absent* once `Approved` is projection-confirmed. For a second Approver sitting on the polling detail page, the two disagree. If the control vanishes while focused, focus falls to `document.body` silently, because the deterministic focus rule is scoped to the resolution *this* session submitted.
Fix: rule for the non-actor explicitly — focus moves per the deterministic rule and the polite node announces the authoritative state; `aria-disabled` with the `superseded` reason wins for a viewer who did not act, absence is reserved for a fresh render.

**[Accessibility]** — `FcStatusFilterChips` is still mandatory on the Provider catalog, where its label contract can never be satisfied (§ EXPERIENCE.md:203, :180 vs FcStatusFilterChips.razor)
In source the label is `slot.ToString()` — always an unlocalized English enum name, for every slot, on every grid — so "where its label contract allows" describes an empty set. A developer honouring the mandate ships readiness filters visibly reading `Success`, `Warning`, `Danger` on a French tenant, breaking the parity gate, announced as "Success, filter inactive": a colour name where a readiness meaning belongs.
Fix: extend the queue's disposition to both grids and delete "where its label contract allows", which currently reads as permission.

**[Implementation readiness]** — The mandated `FcFilterResetButton` cannot reset the filters the spine actually applies (§ FcFilterResetButton.razor.cs:52 · FilterEffects.cs:140-146 vs § Grid rules)
Reset dispatches into the shell's Fluxor `DataGridNavigationState` snapshot, while the 09-09 revision moved the queue's filters into Agents-owned `aria-pressed` toggles holding their own state. Nobody is told to read the snapshot back or dispatch the shell's filter actions from the toggles. Both grids get a Reset that clears a snapshot nothing reads while the applied filters survive — and the mandated filtered-empty state is unrecoverable.
Fix: choose one and say it in § Grid rules — the Agents filter state *is* the shell snapshot, or `FcFilterResetButton` comes off the mandatory list and Agents owns the reset control.

**[Implementation readiness]** — Twelfth divergence, unlisted: the shipped UI has no CSS at all behind ~115 BEM class names (§ src/Hexalith.Agents.UI vs DESIGN.md § Layout & Spacing)
No `.css`, no `.razor.css`, no `wwwroot`, no style block, no inline style. Every layout instruction — the spacing rhythm, `scroll-padding`, logical RTL rails, reserved stable space for badges — and `{typography.mono}` has no delivery mechanism, no owning story and no corrections row. The single largest unowned build decision in the pair. Positive: zero legacy v4/FAST tokens anywhere in `src/`, so there is no migration backlog to allowlist.
Fix: add a row naming the delivery decision — Fluent component parameters plus Fluent 2 tokens per the repo law, versus one Agents stylesheet for layout the design system does not own — with an absorbing story.

**[Implementation readiness]** — Twelfth divergence, unlisted: the two `not available` keys do not exist and the keys they replace do (§ AgentsResources.resx / .fr.resx vs § Voice and Tone)
The resx files carry no `Agents.Surface.NotAvailable.*` key at all, and do carry exactly the per-cause set the rule calls "a conformance failure, because differing copy is itself a disclosure channel". Worse, the section ends "the resource file wins" — read literally, the shipped per-cause copy wins over the rule that forbids it, reopening a disclosure channel the spine believes it closed.
Fix: add a corrections row for the per-cause copy with an absorbing story, and narrow the enforced-source sentence to *wording of existing keys*, not key inventory.

**[Implementation readiness]** — The register's `EXT-CONV-UI-1` describes a seam the spine no longer wants (§ external-dependency-register.md vs § Conversation Integration Seam)
The spine requires two artifact kinds, an Agents-side provenance accessor and `GetCallabilityAsync(tenant, conversation)`. The register carries only the action contribution plus provenance markers "carried in message metadata" — Conversations rendering from its own data, an incompatible design for the same marker. Neither the decoration slot nor the callability query appears in the register. A Conversations maintainer accepting the register's fields would deliver something Story 6.7 cannot consume.
Fix: amend the register to the spine's additions, or adopt the metadata-carried design in the spine and delete the Agents-side accessor. Do not leave both standing.

**[Implementation readiness]** — `epics.md` Story 6.7 still says there is no external seam, so two of three documents call it ready (§ epics.md:1841 · register `ConsumingStories: TBD` vs § Conversation Integration Seam)
The spine blocks 6.7 from `ready-for-dev` under FR-21; the epics file and the register both read as unblocked. Two of the three documents a sprint planner reads say 6.7 is ready. The governance lens rates this blocking rather than clerical: a dev ships the invocation path without the contributed action.
Fix: land the two-line edit before Epic 6 planning; until then the spine's own rule is the only thing holding the gate.

**[Implementation readiness]** — The Provider-disable blast-radius query has no home and no interim, and the consuming story ships first (§ § Known gaps vs epics.md:1374-1391 · ARCHITECTURE-SPINE.md:151)
The spine assigns the by-Provider/model query to Story 5.5; 5.5's ACs specify an *operation-family* readiness request and no such query, and Architecture has none either. The disable action ships in Story 5.3, **before** 5.5. Every other 5.5-dependent rendering has an interim; this one has none, so a 5.3 dev must invent the query or ship a confirmation the spine forbids as incomplete.
Fix: add the interim to the `provider-catalog-grid` row (platform scope named, in-scope-Agents list and cross-tenant count as a deferred metric), or move the query's ownership to 5.3.

### Medium (33)

**[State coverage]** — Four of the 13 routes have no page-component binding and therefore no state-slot mapping (§ EXPERIENCE.md:442, :501 · DESIGN.md:120-135). Agents overview, Operational status, Launch readiness and the Audit evidence list are FullWidth non-grid routes with no page component named, so their loading, error, `not available` and `Degraded` slots and their heading/focus mechanism are unspecified — against the section's own "every route" rule. Agents overview also has no empty or cold-load treatment. *Fix:* name the page component and the state-slot rule, or state they are hand-authored and carry the domain `FcPageHeader` pattern directly.

**[State coverage / Governance]** — `Abandoned` reasons are incomplete and untyped (§ EXPERIENCE.md:333 vs prd.md:391-392, :236). FR-18 names four *typed* reasons carried in status and audit and requires system-abandoned proposals to be excluded from the SM-3 denominator and reported separately. Two reasons are missing, none is typed, and the separate-reporting requirement reaches no Operational status row; four missing tokens are four missing resource keys. *Fix:* enumerate the four with a localized label each and add system-abandoned as a separate count.

**[State coverage]** — The pre-post re-validation set is reduced to its safety third (§ EXPERIENCE.md:231 vs prd.md:397-398). FR-18 requires four checks immediately before posting, including that `hexa` is still a member of and not blocked in that Conversation. Three of the four failure causes have no typed reason, no copy row and no confirmation line. *Fix:* enumerate the four checks in the retry confirmation and give each failure cause a `PostingFailed` typed reason.

**[Inheritance discipline]** — The regeneration-ceiling deferral is stale and quotes its own source wrongly (§ EXPERIENCE.md:177 vs prd.md:634, A-6 at :752). The spine flags the numbers for Product sign-off "because FR-32 states only 'a documented default and a valid range'"; FR-32 now states 3 and 1–10, and A-6 owns them. The numbers are right; the stated reason is false against its own source. This is the run's one *partial* regression. *Fix:* cite FR-32 and A-6, drop the clause, and replace the deferral with a pointer to A-6.

**[Bloat & overspecification]** — Eight of the 22 DESIGN component sections carry behavior EXPERIENCE already owns, against that section's own preamble (§ DESIGN.md:269 vs :287, :291, :299, :307, :315, :319, :339, :359). Two-place statements drift — and the reconcile file records this exact cleanup being performed on two sections in this run, so the other eight were missed. `agent-response-marker` additionally carries two literal copy strings, which is § Voice and Tone content. *Fix:* cut each to its visual delta and point at the matching EXPERIENCE row.

**[Governance]** — `CanCurrentUserApproveSelectedVersion` carries two of the five Eligible Approver conditions (§ EXPERIENCE.md:181, :617 vs prd.md:91). The glossary names five conjuncts including current Participant membership and read access, both of which FR-7 requires re-checked at seven moments. A pre-state narrower than the server rule produces an enabled Approve the server refuses, training approvers to treat refusal as a glitch. *Fix:* state that the flag returns the full predicate with one typed reason per failing conjunct.

**[Governance]** — "Absent, not disabled" is stated three times and contradicted by the layout contract (§ EXPERIENCE.md:80, :179 · DESIGN.md:299 vs :248). The layout table maps the Provider catalog to an Editor accordion item unconditionally and the component has no no-mutation variant, so a dev renders the Editor and hides its buttons — "disabled" with extra steps, leaking the field inventory including the secret-reference control. *Fix:* add a `read-only (no mutation policy)` variant and state that the Editor accordion item itself is absent in it.

**[Governance / Accessibility]** — `/agents/audit` as an id-entry surface removes the ability to ask whether anything is missing (§ EXPERIENCE.md:72 · DESIGN.md:241 vs FR-24, SM-5 at prd.md:827). FR-24 requires compliance inspection to be "rate-visible" — a rate is a list property — and SM-5 requires completeness across a population. On a surface where you must already know each identifier, the inverse question cannot be asked. Defensible for *content* surfaces; not for the configuration-and-governance evidence class, which contains no Conversation content. *Fix:* keep id entry for interaction and proposal evidence; add an enumerable tenant-scoped list for governance evidence and compliance-inspection records.

**[Governance]** — The governance evidence contract names the actor but not the tenant, the role basis, or the outcome (§ EXPERIENCE.md:192 vs prd.md:434, :441). "The acceptance stages the command passed" describes progress, not outcome. *Fix:* add tenant, FR-33 role basis and typed outcome (accepted / rejected on concurrency / denied on authorization), and render the role basis in the confirmation so the confirmation set and the audit record still match.

**[Governance]** — The launch-readiness panel cannot render the blocker class that the deferred items produce (§ EXPERIENCE.md:188 vs prd.md:614). Any unretired §8.1 Product/Architecture/Governance assumption is an `RQ-1` blocker `UnretiredAssumption`, and every deferral in the reconciliation maps to such a row (A-14, A-6, A-7, A-12, A-13) — so the spine's own deferred list is precisely what blocks `RQ-1`, and the panel has no row class for it. *Fix:* add `UnretiredAssumption` as a blocker class with assumption id, owner and retirement condition.

**[Accessibility]** — The essential-exception argument for proposal expiry is not a 2.2.1 argument, and it will be cited as one (§ EXPERIENCE.md:468, :177). The spine rests the exception on the 15-minute nearing-expiry floor. That conflates two independent allowances: the *warning* allowance requires 20 seconds' notice **and** ten extensions, which the spine explicitly lacks; the *essential* exception requires no warning at all, so a floor neither strengthens nor is required by it. The window is also administrator-configurable and not adjustable by the Approver who meets it, so "Adjust" is unavailable too, and the essential claim itself is asserted rather than argued. Product is being asked to sign a conformance position stated on the wrong ground. *Fix:* rewrite as an essential-exception claim on its own merits, state the floor as a usability commitment, and record that none of Turn off / Adjust / Extend is satisfied.

**[Accessibility]** — The nearing-expiry chip is the one status in the system with no text (§ DESIGN.md:283, :213, :211 · EXPERIENCE.md:336). No visible string is defined and none exists in the Voice and Tone table, while DESIGN mandates "a Fluent semantic role plus one glyph plus visible whole-string text" — so a colour-plus-triangle chip breaks the spine's own no-colour-only rule at the one place a time limit is communicated, and in forced-colors the triangle carries it alone. *Fix:* define the chip's whole string and French form, and the announcement string separately.

**[Accessibility]** — The allow-list of FrontComposer speakers is written as if closed but is not (§ EXPERIENCE.md:426). Eight more Shell nodes can speak on a grid page without violating any spine rule, including `FcMaxItemsCapNotice` (the queue's page size is 25, precisely the cap case), `FcNewItemIndicator` (which would defeat the aggregated change announcement) and `FcStatusBadge`, whose `role="status"` would silently re-create the per-badge defect Story 8.6 is removing — including on the deterministic focus target. *Fix:* make the list closed and name `FcStatusBadge` and `FcNewItemIndicator` as forbidden.

**[Accessibility]** — Most announced strings have no key, and the parity gate cannot catch it (§ EXPERIENCE.md:433-436 vs :112-138, :159). Roughly twenty-four announced events; whole strings for about eight. The gate enforces parity of *keys that exist*, so a hard-coded English announcement passes. For these states the announced string is the only UI a screen-reader user has. *Fix:* require a named key per politeness-table event and add the missing rows with French forms.

**[Accessibility]** — The deterministic focus target may be nameless to AT (§ EXPERIENCE.md:364). A bare `div`/`td` has the generic role, and ARIA prohibits an accessible name on `role="generic"` — so the identity-plus-state string the whole pattern turns on can be silently dropped. *Fix:* use `role="group"` or a visually-hidden name node, and assert the announcement in the AT matrix.

**[Accessibility]** — Read literally, the every-state-slot rule puts two `h1`s and two `PageTitle`s on the Degraded state (§ EXPERIENCE.md:442 vs FcAggregateDetailPage.razor). `Degraded` is not a slot: the banner renders *above the ready body*, which already has the ready header. *Fix:* enumerate the five real slots and state that the stale/degraded banners carry no heading.

**[Accessibility]** — "Renders without a glyph" is not reachable through the registration contract (§ DESIGN.md:193, :180 vs FrontComposerNavigation.ResolveNavEntryIcon). The shell returns `Apps20` whenever `TryCreate` fails, including for a null key — and `Apps20` is Overview's own glyph. So the six entries listed "Glyph today: none" all render Overview's glyph, making seven Agents tiles identical in the icon-only rail. The spine's assertion is false against the shell it inherits, and it is the assertion that was supposed to prevent glyph reuse. *Fix:* state the truth and decide — accept the duplication with the requests raised, hold those entries out of the icon-only rail, or request a null-icon path.

**[Accessibility]** — The audit-evidence id-entry surface has no accessibility contract at all (§ EXPERIENCE.md:72 · DESIGN.md:241). Removing it from FC-TBL removed its state contract and nothing replaced it: no visible label, no statement of which reference kinds it accepts, no behavior for an unrecognised or unauthorized reference, and no announcing node. This is the entry point to every audit investigation and it is one unspecified text box. *Fix:* specify it as a form under the existing Forms rules, with `not available` on an unresolvable reference and that outcome added to the politeness table.

**[Implementation readiness]** — "The mandatory FC-TBL component set" misattributes an Agents decision to FrontComposer (§ fc-tbl contract vs three places in the pair). FC-TBL defines a frozen 14-type surface plus generated-grid envelope rules; it does not define a four-component usage mandate. A dev who opens the contract will not find it, and will not know whether the envelope rules bind a hand-authored grid. *Fix:* relabel the four as Agents-owned and state which envelope rules carry over.

**[Implementation readiness]** — `FcExpandInRowDetail` has three `[EditorRequired]` parameters the spine never supplies (§ FcExpandInRowDetail.razor.cs:40-71). `DetailPanelAriaLabel` and `HasExpanded` are unassigned. *Fix:* add the label to the localization inventory as a per-grid whole string and name `HasExpanded`'s source.

**[Implementation readiness]** — Launch readiness is a third `FluentDataGrid` governed by nothing (§ DESIGN.md § launch-readiness-panel vs § Grid rules · LaunchReadiness.razor:100-127). No wrapper, no `ViewKey`, no `ItemKey`, no distinguished states, no 320 px surviving-column set — while every other grid rule exists because those things were needed. Shipped as a raw `<table>`. *Fix:* bring it under § Grid rules, or state it is a plain read-only grid and give it the 320 px column list.

**[Implementation readiness]** — Two page-like surfaces with sibling titled sections are outside the accordion table the repo law requires (§ DESIGN.md § operational-status-panel, § audit-evidence-panel). Six recovery-action groups and two evidence classes respectively; the table naming the primary item covers only four surfaces. *Fix:* extend the table to every multi-section route, or state that the component rows are authoritative.

**[Implementation readiness]** — Two superseded shipped types are named in behavioral rows but have no absorbing story (§ AgentReadiness.cs `MapState`, `ProviderReadinessState` at :48-70). The UI-local enum has no `OperationalState`, `Callability`, `ReasonCode`, `CapabilityVersion`, `ObservedAt` or `ValidUntil`. *Fix:* one corrections row mapping both to Story 5.5.

**[Implementation readiness]** — `AgentCallOperationStatus` and `AgentReadinessStatus` growth is specified nowhere but the spine, and the additive rule is unstated (§ src/Hexalith.Agents.Contracts/Operations vs ARCHITECTURE-SPINE.md). Architecture mentions neither enum, though it specifies a versioned additive reason code whose unknown values fail closed. No Epic 6 story is blocked, but the enums carry no counterpart of that rule. *Fix:* state that both are versioned additive and an unknown value renders `authoritative pending`, never Success, never a terminal.

**[Implementation readiness]** — The projection nudge binding is correct but incomplete, and Architecture still has nothing (§ IProjectionChangeNotifier.cs:49 · EventStoreServiceExtensions.cs:88). The member exists and is DI-registered, so the row is buildable — but the spine does not say the registration arrives through the EventStore extension (a host that skipped it gives a DI failure at first render), and Architecture has zero occurrences of poll / nudge / SignalR / `catching up` while the two shipped paths run 250 ms/5 s and 200 ms/8 s. *Fix:* name the registration extension and keep the Architecture deferral visible at the point of use.

**[Implementation readiness]** — `IBadgeCountService` suppression is real but it is not a switch (§ IBadgeCountService.cs:29 · FrontComposerNavigation.razor:128-130 · ServiceCollectionExtensions.cs:338). No label member, no parameter slot, no `[Parameter]` on the navigation component, registered `AddScoped` with a required consumer — so there is no domain opt-out. The count stays suppressed *only* because Agents declares no `ActionQueue` projection, leaving the `count > 0` gate closed. The claim holds by construction, not by configuration. *Fix:* state the mechanism as a standing constraint — the day any Agents projection is marked `ActionQueue`, an unlabelled number appears in the rail and cannot be turned off.

**[Implementation readiness]** — Story 6.2 owns a route its acceptance criteria never mention (§ epics.md:1585-1607 vs § Information Architecture). No UI read model, no route, and none of the tokenizer basis, reserved output allowance, safety margin or Agent Instructions size the panel renders. The route is unbuildable and the owning story does not know it owns it. Same for the Operational status extras. *Fix (Epics):* one AC clause each.

**[Implementation readiness]** — `sprint-status.yaml` is still not regenerated and is now a full epic-set behind (§ _bmad-output/implementation-artifacts/sprint-status.yaml). It tracks superseded `5-1 … 5-18` slugs and **stops at `epic-5`; Epics 6, 7 and 8 are absent entirely**, so no story in Epics 6–8 can be picked up, sequenced or reported. The brownfield-drift review already cites stories the file does not contain. *Fix:* regenerate against Epics 5–8 before Epic 6 planning.

### Low (21)

**[Token completeness]** — `spacing` is the one token group given as literal px with no Fluent equivalence named (§ DESIGN.md § Layout & Spacing). A dev reading `{spacing.4}` has no signal whether to write `FluentStack Gap`, `--spacingVerticalL`, or `gap: 16px`. Compounds the no-CSS-delivery finding. *Fix:* one clause giving the Fluent 2 token or `FluentStack Gap` equivalent per step.

**[Visual reference coverage]** — The spine-only decision lives in one spine and carries a stale date and a stale count (§ EXPERIENCE.md:43 · .memlog.md:38). Dated "2026-08-02, reaffirmed 2026-09-08" on files updated 2026-09-09, over "14 IA surfaces" against a 15-row table, and absent from DESIGN.md entirely. *Fix:* reaffirm with the current date, align the count, and add one clause to DESIGN § Brand & Style.

**[Bloat & overspecification]** — `## FrontComposer Readiness` is a pure cross-index carrying no decision of its own (§ EXPERIENCE.md:485-503). All 13 rows resolve elsewhere. *Fix (optional):* fold the four rows carrying a real constraint into their owning sections and drop the table.

**[Bloat & overspecification]** — A handful of EXPERIENCE sentences argue for a review finding rather than stating a rule (§ EXPERIENCE.md:176, :181, :194). DESIGN prose may carry editorial voice; EXPERIENCE prose should not. *Fix:* state the rule; the rationale belongs in the memlog.

**[Inheritance discipline]** — The cost-control posture vocabulary is missing from the localized token list and from the readiness panel (§ EXPERIENCE.md:163, :188 vs FR-28 / OQ-6). Five postures and the `ProhibitedCostControlPosture` blocker code are unlisted, and the panel renders `BlockerCode` generically — so it would arrive as a raw token against the spine's own "raw tokens never display" rule. *Fix:* add them to the architecture-token row and name posture as a rendered field on the gate record.

**[Governance]** — `budget blocked` recovery copy points the user at an action FR-33 forbids them (§ EXPERIENCE.md:306 vs prd.md:418-419). *Fix:* name the role that can act ("A Platform or Release Operator must raise this cap") rather than the surface.

**[Governance]** — *No fix: terminal-state integrity is airtight under re-attack* (§ EXPERIENCE.md:217, :328, :331, :183, :346, :368, :334, :345). `ApprovedVersionId` pinned read-only so no second version can be approved; double-posting closed three ways (deterministic `MessageId` reused across attempts, duplicate submission blocked per session/resource/family, idempotent duplicate showing the existing pending state) with EventStore optimistic concurrency authoritative across tabs, sessions, retries, replay and restarts; the expiry/approval race server-timed on both sides.

**[Governance]** — *No fix: tenant isolation is the strongest part of the spine and could not be opened* (§ EXPERIENCE.md:140, :383, :81, :395, :344, :179, :80). One indistinguishable `not available` state with the rule that per-cause copy is itself a disclosure channel; identical status class and timing; `[Authorize(Policy=…)]` on every page with nav hiding demoted to disclosure; tenant- and readability-scoped search; `existence only` rendering state and expiry with no content; cross-tenant impact as an aggregate count, never tenant names. The residual exposure is the catalog read scope, filed as high.

**[Governance]** — *No separate fix: provider secrets are airtight but for the reason-code channel already filed* (§ EXPERIENCE.md:179, :455, :467, :189, :241, :150 · DESIGN.md:351, :339). The UI never accepts a secret value; the reference is masked, autocomplete off, never echoed in validation, errors, URLs, clipboard or diagnostics; accessible names are by configured state; the audit panel never shows secrets, payloads or stack traces; export never renders contents or keys and states no plaintext secret is included; export and deletion fail closed on `EXT-SECRETS-1`.

**[Governance]** — *No fix: the pricing/currency circularity is genuinely gone with no residual loop* (§ EXPERIENCE.md:179, :187, :306-307, :313, :593-601). One direction of authority, ISO 4217 well-formedness only, per-tenant mismatch on that tenant's readiness view, the pre-8.4 interim stated so the rule is not evaluated against an absent operand, server-assigned pricing version, separate `BudgetBlocked` and `rate limited` states, thresholds over settled plus outstanding reservations, `Unknown` outcomes holding their reservation and never retried.

**[Accessibility]** — The carrier's nodes must exist before their content changes; the spine does not say so (§ EXPERIENCE.md:424). A node inserted at the same commit as its text is not announced by most AT. The shipped `ProposalTransitionAnnouncer.razor` gets this right; the spine should pin it. *Fix:* one sentence — both nodes render on first paint, empty, and only their text mutates.

**[Accessibility]** — An embedded panel's pair can collide with a host route's carrier on exactly one surface (§ EXPERIENCE.md:79). The harness route hosts the panel until Story 6.7 deletes it, so it can hold two polite and two assertive nodes. Delisted and excluded from `LR-UI-CONFORMANCE`, but developers use it. *Fix:* state that the panel suppresses its own pair on an Agents route, or that the harness renders no carrier.

**[Accessibility]** — Three loose ends around the detail-route chrome (§ EXPERIENCE.md:140, :443 · FcPageHeader / FcAggregateDetailPage.razor). The `not available` slot's `h1` string is unbound and a blank heading makes `FocusHeadingAsync()` throw; `BackHref` is unspecified and defaults to empty, yielding a named self-referential link rendered outside the state slots; and `HeadingId` + the shell's `ContentLabelledBy` are unused, so every Agents route ships an unnamed `main` landmark. *Fix:* bind the heading string, require a non-empty `BackHref` per detail route, and set `HeadingId`.

**[Accessibility]** — Three Constrained routes are bound to no page component at all (§ EXPERIENCE.md:501 · DESIGN.md:120-135). Conversation context policy, Content safety, Cost controls and Audit governance are bound only to "Fluent form primitives inside `FluentAccordion`"; which component carries their `h1`, `PageTitle` and focus target is unstated. *Fix:* name `FcPageHeader` (or `FcAggregateDetailPage`) explicitly.

**[Implementation readiness]** — Two raw-HTML classes violate the repo UX law and are absent from the corrections table (§ LaunchReadiness.razor:100-127 · AgentConfiguration.razor). A full `<table>` where DESIGN mandates `FluentDataGrid`, and eleven `<section>` with ten `<h2>` sibling titled sections with no `FluentAccordion`. *Fix:* one corrections row per class, absorbed by 8.7 and 5.2/5.7.

**[Implementation readiness]** — The registration's own XML doc undercounts its nav entries (§ AgentsFrontComposerRegistration.cs `RegisterDomain`). Says "eight" where nine are registered; the spine's row correctly says nine. *Fix:* note that the code comment is wrong so a dev does not reconcile the spine down to eight.

**[Implementation readiness]** — The accordion table's "Policy surfaces" row collapses three routes with different section sets (§ DESIGN.md § Layout & Spacing). `conversation-context-policy-panel` is read-only with "no control of any kind", so it has no Draft and no Authoring item. *Fix:* split into the three named routes.

**[Implementation readiness]** — The reconciliation's "full list" claim for Architecture's items is inaccurate (§ reconcile-validation-2026-09-09.md § 4). The contracts-that-must-grow table contains neither `IProjectionChangeDetailNotifier` (nothing must grow) nor `EXT-CONV-UI-1` (in the seam table instead). *Fix:* add both as pointer rows or drop the claim.

**[Implementation readiness]** — PRD FR-18's seven-versus-ten misalignment blocks no story today (§ ProposedAgentReplyState.cs · § Proposal lifecycle). The enum ships all ten plus `Unknown`, both spines render all ten with a colour role, and the fold and the `PostFailed` alias are explicit. The governance lens calls the deferral obsolete, since the PRD now enumerates ten states with `Unknown` as the FR-23 sentinel. *Fix:* say plainly that no story is blocked and the enum governs, so the Product deferral is not read as a gate.

**[Implementation readiness]** — The pending-count plural inventory contradicts the zero-renders-nothing variant (§ proposal-notification row · AgentsResources.resx). No `Agents.Overview.PendingProposals.*` key exists yet, and the plural rule requires `.Zero`/`.One`/`.Other` while the variants say "zero (no badge), count". *Fix:* state that `.One` and `.Other` are the whole inventory, or the parity gate will demand a `.Zero` string nothing can render.

**[Implementation readiness]** — Two IA rows have no single named owning story, and harness removal has no derivable AC (§ § Information Architecture; epics.md:1846-1876). `/agents/audit` ("7.x, 8.x") and `/agents/audit-governance` ("8.1 to 8.3") name no builder; and Story 6.7's ACs say only that "alternate invocation entries are absent" where the spine requires the `Order: 4` registration entry unregistered and `ConversationCall.razor` deleted. *Fix:* name the story that builds the id-entry surface, and name both harness artifacts in the spine's rule so the AC writes itself.

## What is genuinely closed

Verified against source, not against the reconciliation's claims.

- **The icon contract is now exact** — twelve `TryCreate` names and no others, thirteen no-arg 16 px factories matching item for item, none reachable through `TryCreate` as stated, all seven status roles bound to real methods so **no status glyph is a FrontComposer request**, and the seven glyphs visually distinct enough to survive forced-colors.
- **The route-heading split is right** — `FcAggregateListPage` has the three parameters and `FocusHeadingAsync()`; `FcAggregateDetailPage` has 18 parameters and none heading-related; `FcPageHeader` renders a real `h1`, suppresses a blank heading, emits `role="presentation"`, and throws rather than silently no-oping.
- **The `FcAggregateDetailState` mapping matches the component** — one slot per state, a degraded banner over the ready body, failing closed to `UnavailableContent`.
- **Every Fluent v5 name in the pair exists under that name.** `Appearance.Accent` does not exist and has zero occurrences in either spine or anywhere in `src/` — that prior finding is fully closed.
- **Shell ownership of connection loss is real** — `FcProjectionConnectionStatus` is rendered inside `FrontComposerShell`, not by the domain.
- **"A confirmation is never opened by a selection event"** closes the whole arrow-key-opens-a-modal class in one sentence.
- **Localization is already the shipped practice** — 599 keys at exact EN/FR parity, the `Agents.<Surface>.<Item>` convention, and the parity gate at the path the spine names.
- **Terminal-state and double-post integrity, tenant isolation, provider-secret handling and the pricing/currency circularity all held under adversarial re-attack.**
- **Reflow at 320 px / 400%** is a positive obligation on every route including the confirmation, with evidence in the launch lane.
- **Every story named anywhere in the pair exists in `epics.md`** — every IA owning story, Key Flows list, Known gaps owner and corrections absorbing story resolves to a real heading. That was not true two runs ago.
- **Repo UX law compliance:** zero legacy v4/FAST tokens anywhere in `src/` or the spines except as named prohibitions, so there is no migration backlog to allowlist.

## Mechanical notes

- **Frontmatter.** Both spines carry the required keys; all 36 source paths resolve (16 + 20, grown from 15 + 18 by the two reconcile files and the shipped registration). DESIGN.md carries `colors`, `typography`, `rounded`, `spacing`, `components` per spec.
- **Cross-references.** All 19 distinct `{path.to.token}` references resolve (154 occurrences); all 25 distinct `§` internal references resolve, including the two external ones.
- **Stale self-description.** `EXPERIENCE.md:31` claims reconciliation to "the PRD of 2026-09-08" — accurate as history, misleading as currency, since `prd.md`'s body carries 2026-09-09 content while its own frontmatter `updated:` still reads 2026-09-08. The PRD frontmatter is arguably the upstream defect.
- **Surface counts disagree across three places** — 15 IA rows, "14 IA surfaces" in the memlog, and a 6 FullWidth / 8 Constrained DESIGN split with Audit evidence counted twice. All defensible, no two using the same denominator.
- **Tables.** No malformed rows; the three-cell-row defect fixed last run has not recurred.
- **Mermaid.** None used. The single `text` fence is opened and closed correctly.
- **Reviewer-file arithmetic.** `review-governance.md`'s closing line states 15 high / 31 total; the findings as filed count 14 high / 30. The counts in this report are the counted values.
- **FrontComposer trivia, not findings.** `FcFluentIcons.InfoCircle16()` returns `Size16.Info()` and `DocumentSearch48` delegates to `DocumentSearch32` — quirks in FrontComposer, not in the spine, which cites the method names correctly.

## Regression detail

| Lens | Prior | Resolved | Partial | Unresolved | Regressed | Unchanged by decision |
|---|---|---|---|---|---|---|
| Rubric walker | 15 | 12 | 1 | 0 | 0 | 2 |
| Accessibility | 22 | 16 | 6 | 0 | 0 | — |
| Governance | 21 | 14 | 6 | 1 | 0 | — |
| Implementation readiness | 26 | 15 | 10 | 1 | 0 | — |
| **Total** | **84** | **57** | **23** | **2** | **0** | **2** |

The two unresolved: "operator-only visible" is still a disclosure category with no policy behind it (governance #19, listed as applied in the reconciliation but unchanged in the spine), and Story 6.2's missing UI read model (implementation #18, deferred to the Epics maintainer with nothing landed).

## Reviewer files

- `review-rubric.md` — 19 findings (1 critical, 5 high, 8 medium, 5 low)
- `review-accessibility.md` — 22 findings (1 critical, 9 high, 8 medium, 4 low)
- `review-governance.md` — 30 findings (5 critical, 14 high, 6 medium, 5 low)
- `review-implementation-readiness.md` — 25 findings (0 critical, 7 high, 11 medium, 7 low)
- Prior run archived at `.working/validation-2026-09-09-pre-update/`
