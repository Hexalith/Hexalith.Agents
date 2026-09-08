# Validation Report — Hexalith Agents

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`
- **Run at:** 2026-09-08T18:13:16Z
- **Lenses:** rubric walker, accessibility, governance & audit, implementation readiness

## Overall verdict

The pair is a consumable contract. Every token reference resolves, all 19 component names are identical across both spines, all four PRD journeys are carried verbatim with protagonist, numbered steps, climax, and failure path, and the Fluent-by-name colour inheritance is a defensible application of the UI-system rule (all eight BadgeColor names verified against Fluent UI Blazor v5). Shape fit is strong. What keeps the rubric from strong is one uncommitted load-bearing surface, the high-impact confirmation and pending-command indicator used by six operation families, which has no named component and whose rules sit under an unrelated heading, plus a cluster of medium gaps a story-dev would otherwise decide alone: no home for proposal-expiry configuration, no offline or concurrency treatment, no pinned Fluent package or per-role icon and appearance statement, and NFR-14 thresholds restated in six places.

The three additional lenses shift the picture materially. The spines are strong on meaning (what Success may never mean, which states are terminal, what must never leak) and weak on binding and mechanics. Accessibility finds two critical blockers to a keyboard or low-vision approver completing UJ-3: the fail-closed viewport rule locks out 400% zoom, and disabling the activated control drops focus at the approval moment. Governance finds pricing has no UX rule at all (the shipped Story 5.3 editor defaults to 0 USD, which counts as priced) and that concurrent approvers, reload mid-pending, cross-tenant deep links, and provider disable are unspecified. Implementation readiness finds the sole V1 invocation entry, Call hexa, has no integration seam with the Conversations module while the codebase already shipped the forbidden admin-only alternate, and that four of fourteen IA surfaces have no route, nav entry, policy, or query. Several findings converge from independent lenses (expiry threshold and configuration surface, the unnamed confirmation component, projection-lag vocabulary colliding with Stale, unstated polling numbers, contrast delegated in a circle), which makes them the highest-confidence items to roll into an Update.

## Category verdicts

- Flow coverage — adequate
- Token completeness — adequate
- Component coverage — adequate
- State coverage — adequate
- Visual reference coverage — adequate
- Bloat & overspecification — thin
- Inheritance discipline — adequate
- Shape fit — strong

## Findings by severity

| Severity | Rubric | Accessibility | Governance | Impl. readiness | Total |
|---|---|---|---|---|---|
| critical | 0 | 2 | 1 | 1 | 4 |
| high | 1 | 6 | 8 | 8 | 23 |
| medium | 9 | 9 | 8 | 12 | 38 |
| low | 15 | 4 | 4 | 4 | 27 |
| **total** | 25 | 21 | 21 | 25 | 92 |

### Critical (4)

**[Accessibility]** — Fail-closed viewport rule locks out approval at 400% zoom (§ EXPERIENCE.md § Responsive & Platform; DESIGN.md § Layout & Spacing)
A desktop user at 400% browser zoom (WCAG 1.4.10 Reflow, 320 CSS px) lands in the Phone tier and loses the ability to approve. The rule is written as a lockout, not a last resort, and no essential exception is claimed.
Fix: Require proposal detail, version history, and confirmation controls to stack to one column and remain operable at 320 CSS px; reserve unavailability for content that genuinely cannot render, with a visible reason. Add 1.4.10 evidence at 320 px to LR-UI-CONFORMANCE.

**[Accessibility]** — Focus is lost at the approval moment (§ EXPERIENCE.md § Interaction Primitives, § High-Risk Pending Commands, § Accessibility Floor)
The pending-lock rule disables the activated control; a disabled FluentButton is unfocusable, so focus drops to body. Polling may re-sort or remove the row, the dialog return-focus contract has no target when the trigger is gone, and `approved` / `posting pending` are never announced.
Fix: Use `aria-disabled` or a replacement pending control that keeps focus; on resolution move focus deterministically (same-proposal status region, then next row, then page heading via `FcPageHeader.FocusHeadingAsync`); polite announcements for authoritative pending, approved, posting pending, regenerated; polled re-renders must not steal focus.

**[Governance & audit]** — Provider/model pricing has no UX rule (§ DESIGN.md § Components › provider-catalog-grid, cost-control-editor; EXPERIENCE.md § Component Patterns › provider-catalog-grid, § Interaction Primitives)
No validation (positive, non-zero, currency matches tenant budget), no confirmation, no future-only copy, no visible pricing version. The shipped Story 5.3 editor defaults to `0` / `USD`; a zero price is 'priced' under AD-10/AD-21, so reservations are 0 and the 80%/100% caps never fire while the grid reads healthy.
Fix: No default values; units > 0; currency must equal tenant budget currency or the row renders `status-important` with reservations blocked; pricing edits are `ProviderCatalogMutation` high-risk with confirmation showing old → new, effective version, and 'applies to attempts prepared after confirmation'; pin a pricing-version column.

**[Implementation readiness]** — Call hexa has no Conversations integration seam; code shipped the forbidden alternate entry (§ EXPERIENCE.md § IA › Conversation invocation, › conversation-agent-call, § UX Closure Decisions)
The spine says Call hexa is Conversation-owned and the sole V1 entry, but Conversations is a separate module and nothing names the extension point. `src` shipped `/agents/conversation-call` as an Administrator-gated admin nav entry, exactly what UX-DR24 forbids. Story 6.7 cannot be built from the spine.
Fix: Add a 'Conversation integration seam' subsection naming the Conversations-side extension point and the Agents-side artifact (exported Razor component + `IConversationAgentCallGateway`); state the admin route is a pre-integration harness to be de-listed before 6.7 closes; add a FrontComposer Readiness row for the seam.

### High (23)

**[Component coverage]** — High-impact confirmation and pending-command indicator have no named component (§ DESIGN.md:258 under ### audit-governance-panel; EXPERIENCE.md § Interaction Primitives lines 215, 221)
Six operation families (ProposalResolution, PolicyPublication, TenantBudgetUpdate, LegalHold, ExportRequest, DeletionRequest) depend on a confirmation surface and a pending lock whose rules exist only as prose under an unrelated heading. Nothing says whether it is a dialog or inline panel, what it contains, or how the lock renders. Six stories will each invent one.
Fix: Add two components, `high-impact-confirmation` (action name, resource identity, future-only effect line, authorization basis, confirm/cancel; focus-trapped; Esc non-committing) and `pending-command-indicator`, with frontmatter entries, DESIGN.md sections, and EXPERIENCE.md rows; move the line-258 paragraph out of `audit-governance-panel`.

**[Accessibility]** — Live-region delegation not backed by FrontComposer docs (§ EXPERIENCE.md § Accessibility Floor, § FrontComposer Readiness)
front-composer-shell.md documents aria-live only for shell-owned surfaces; project-context.md tells adopters to keep their own sr-only aria-live div. The domain must build what the spine delegates. Politeness per event is unspecified.
Fix: Agents owns one page-level polite `role=status` node and one `role=alert` node; give a per-event table (alert: denied, generation failed, posting failed, expired; status: pending, created, approved, posting pending, posted, regenerated, context blocked, expiry warning). Note NFR-14 `LiveRegionAnnouncedTick` depends on this node.

**[Accessibility]** — No route heading or post-navigation focus rule (§ EXPERIENCE.md § Information Architecture, § Accessibility Floor)
`FcPageHeader.FocusHeadingAsync()` is page-invoked and requires `HeadingTabIndex=-1`; the shell does not move focus on route change. No h1 per route, heading hierarchy inside accordions, or focus placement when the editor opens from the queue.
Fix: Every route renders `FcPageHeader` with a non-blank Heading, `HeadingTabIndex=-1`, and a localized PageTitle; focus the heading on navigation and on opening proposal detail; accordion titles h2, in-panel sections h3.

**[Accessibility]** — Forms have no error contract (§ EXPERIENCE.md § Component Patterns; DESIGN.md § Components (five form components))
'Inline blockers' and 'validates' never specify error association (`aria-describedby`, `aria-invalid`), error summary and focus, numeric units, suggestion text, how a write-only secret field exposes its configured state, or accessible names for policy-source row controls (WCAG 3.3.1–3.3.3, 4.1.2).
Fix: Add a Forms subsection to § Accessibility Floor: label every control, associate errors, focus the first error on submit, state units, name secret fields with their configured state, give policy-source rows an accessible name including their basis.

**[Accessibility]** — Contrast delegated in a circle and never verified (§ EXPERIENCE.md § Accessibility Floor; DESIGN.md § Colors)
EXPERIENCE points to DESIGN; DESIGN says only 'Inherit Fluent'. Load-bearing combinations unchecked: badge text on Warning/Severe/Important/Subtle in light and dark, caption and mono metadata, focus ring on the brand-accent toggle, forced-colors. FrontComposer's CI gate proves only its own specimen routes.
Fix: DESIGN.md states AA targets explicitly; LR-UI-CONFORMANCE requires an Agents-owned axe lane over every interactive V1 route in Light/Dark/forced-colors plus the manual AT matrix (NVDA+Firefox, JAWS+Chrome, VoiceOver+Safari).

**[Accessibility]** — French does not exist in the spines (§ EXPERIENCE.md § Voice and Tone; DESIGN.md § Typography)
Parity is binding, yet every string and state name is English-only and FR is neither deferred nor delivered. No rules for pluralization, culture-formatted dates, currency/percent, or whether architecture tokens (Ready / Callable / None, InsufficientEvidence, Stale) display raw. `FcDestructiveConfirmationDialog` hard-codes `Cancel`.
Fix: Add the FR column or mark FR authoring a launch blocker with an owner; require per-count keys for plurals; require CultureInfo-formatted dates/numbers/currency via named placeholders; require a localized label for every architecture token; forbid reusing shell dialogs with unlocalized literals.

**[Accessibility]** — Expiry timing has no accommodation and an undefined trigger (§ DESIGN.md § Colors; EXPERIENCE.md § UX Closure Decisions; proposal-queue-grid expiry column)
'Nearing expiry' never defines threshold, presentation (static timestamp vs ticking countdown), or announcement. A live countdown in a grid cell is auto-updating content (2.2.2) and must not sit inside aria-live; 2.2.1 needs an essential-exception claim or warn-and-extend. The 1 h floor does not clear the 20-hour exception.
Fix: Define the threshold (10% of window or 1 h, whichever is smaller); render expiry as an absolute localized timestamp plus a static relative label refreshed at most once per minute outside live regions; announce once per proposal; record a 2.2.1 position with Product sign-off.

**[Governance & audit]** — Three register operation families outside the high-risk list (§ EXPERIENCE.md § High-Risk Pending Commands, § Interaction Primitives; DESIGN.md › provider-catalog-grid)
`ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation` are in the register's OperationGateMatrix but get no confirmation, pending lock, or double-submit protection. Disabling a provider blocks every Agent selecting it (FR-4) yet the shipped page has no dialog or blast-radius display.
Fix: Extend the high-risk list to the register's full inventory or state which families are exempt and why; disable confirmation lists the Agents whose callability will be blocked; future-only copy for enable/disable.

**[Governance & audit]** — Concurrent approver conflict has no UI state (§ EXPERIENCE.md § Proposal Lifecycle, § Accessibility Floor)
No `superseded`/`conflict` entry, the advisory lock is per-session so the second approver is never locked, and the live-region list omits 'state changed by another Party'. A dev shows a generic error or leaves a stale row with an enabled Approve.
Fix: Add a `superseded by another decision` outcome for every proposal action: render authoritative state, name the actor where disclosure allows, disable resolution controls, announce, never show the rejected local action as pending.

**[Governance & audit]** — Approve-with-uncommitted-edits unspecified (§ EXPERIENCE.md § Component Patterns › proposal-editor, § Key Flows › UJ-3; DESIGN.md › proposal-editor)
Nothing forbids approving while the editor holds unsaved text, and the confirmation never has to show the exact VersionId, kind, author, and content that will post.
Fix: Approval disabled while the editor is dirty; the confirmation renders the selected version's id, kind, author, timestamp, and full content; the approved-version marker must match that id after projection confirmation.

**[Governance & audit]** — Pending-lock survival across reload, navigation, and second tab undefined (§ EXPERIENCE.md § High-Risk Pending Commands, § FrontComposer Readiness)
'User session' is undefined, the timeout has no value, and there is no post-refresh rule. Story 5.3 invented an 8 s timeout and sets `Freshness = Stale` on lag, reusing the register's `Stale` (contract expired).
Fix: Session = authenticated user + tab/circuit; on reload re-derive pending state from the authoritative status projection and re-arm the lock; a second tab renders 'pending in another session' with actions disabled; name the timeout and the post-timeout state (`awaiting projection`, not `Stale`); add a `duplicate submission (idempotent)` outcome.

**[Governance & audit]** — No 'approval authority indeterminate' state (§ DESIGN.md § Colors, › proposal-state-badge, › proposal-editor; EXPERIENCE.md § Proposal Lifecycle)
AD-12 resolves approver rights from the policy snapshot plus current dependency availability. `status-important` is reserved for stale policy basis but no proposal component includes it. When Tenants/Conversations are unavailable the UI fails open until the server rejects.
Fix: Add `authority unresolved` to the editor/state table using `status-important`; all resolution controls disabled; copy names the unavailable dependency safely; add the colour to proposal-state-badge.

**[Governance & audit]** — Deep links, command palette, and filters have no tenant-isolation rule (§ EXPERIENCE.md § List And Detail Surfaces, § Interaction Primitives, § FrontComposer Readiness)
'Empty must not leak unauthorized records' covers grids only. Foreign route ids, palette entries, search, and Party filter suggestions are not covered.
Fix: Add a 'Detail And Deep-Link Surfaces' rule: unauthorized or foreign ids render one `not available` state with identical copy, status class, and timing; palette/search obey the nav policy gate and never return records outside tenant scope.

**[Governance & audit]** — Provider catalog scope undeclared (§ EXPERIENCE.md § Information Architecture › Provider catalog; DESIGN.md › provider-catalog-grid, audit-evidence-panel)
The catalog key is global (AD-10) and FR-19 allows platform-scoped records. The spines never say whether the grid, its audit trail, or the disable blast radius are tenant- or platform-scoped; Story 5.3 had to invent the no-disclosure rule.
Fix: State the scope; if platform-scoped, cross-tenant impact renders as an aggregate count or is omitted per disclosure category, never as tenant names.

**[Governance & audit]** — Secret handling under-specified at the UI level (§ DESIGN.md › provider-catalog-grid; EXPERIENCE.md › provider-catalog-grid, § Accessibility Floor)
'Write-only or configured-state presentation' invites a secret-value input. No masking, never-echo, never-in-URL/error/clipboard rule, and no statement whether the secret reference is displayable. The shipped editor uses a plain text input for `ConfigurationReferenceId`.
Fix: Forbid secret-value entry in the Agents UI (reference only via EXT-SECRETS-1); reference id operator-only; masked input with autocomplete off if any secret-adjacent field is editable; validation and error copy never echo submitted values.

**[Implementation readiness]** — provider-status-badge binds to a contract that does not exist (§ DESIGN.md › provider-status-badge; EXPERIENCE.md § Provider And Model)
`ProviderReadinessResult` (OperationalState / Callability / ReasonCode) is defined only in Story 5.5 AC (backlog, blocked on EXT-PROVIDER-1). Contracts have `ProviderModelStatus` and `ProviderConfigurationState`; the built badge maps Enabled+Configured → Success with no Callability to obey.
Fix: Name the owning story and target contract type in the component row; state the interim rule (render Unknown/Subtle, never Success, when no Callability field is present).

**[Implementation readiness]** — agent-readiness-badge has no named source of readiness proof (§ EXPERIENCE.md § Agent Readiness; DESIGN.md › agent-readiness-badge)
The spine forbids deriving callability from lifecycle but names no projection or field. Built `AgentReadiness.MapState` does exactly that: `Callable = Active && no blockers`. No enum has 'active, not proven callable' or 'stale' despite UX-DR25.
Fix: Specify the readiness projection/field the badge reads (Story 5.5), add `ActiveNotProvenCallable` and `Stale` to the state table, and note the existing mapping must be replaced.

**[Implementation readiness]** — Four IA surfaces unbuildable as specified (§ EXPERIENCE.md § IA rows 5–7 and 13; DESIGN.md › conversation-context-policy-panel, content-safety-policy-editor, cost-control-editor, audit-governance-panel)
Conversation context policy, Content safety policy, Cost controls, Audit governance each lack route, nav entry, authorization policy, and read/write contract. Content safety never enumerates its fixed categories; Cost controls has no units; Audit governance names no commands. UX-DR1 lists 13 nav entries; the registration has 9.
Fix: Per surface add route, nav order, RequiredPolicy, FcPageLayoutMode, owning story, and contract types (or 'to be defined by Story N.N'); enumerate safety categories from `AgentContentSafetyPolicy`.

**[Implementation readiness]** — No authorization-policy matrix (§ EXPERIENCE.md § FrontComposer Readiness › Policy-gated nav)
The spine never names a policy or maps surfaces to personas. `src` invented four (`Agents.Administrator`, `Approver`, `Operator`, `AuditOperator`) and gated Priya's Launch readiness as Administrator with an in-code apology.
Fix: Add a surface × policy table (persona, RequiredPolicy constant, page Authorize policy, fail-closed empty state) reconciled with the four existing constants.

**[Implementation readiness]** — Agent Call state table missing Story 6.7 states (§ EXPERIENCE.md § Agent Call; epics.md Story 6.7)
6.7 AC lists `safety blocked`, `budget blocked`, `capacity queued`, `capacity rejected`; neither the spine table nor `AgentCallOperationStatus` has them.
Fix: Add the four rows (all Severe, non-success, pre-Provider) with a 'start a new call' recovery note for capacity rejected; note the Contracts enum must grow.

**[Implementation readiness]** — launch-readiness-panel binds to fields that do not exist (§ DESIGN.md › launch-readiness-panel; EXPERIENCE.md § Launch Readiness Evidence)
Spine requires Pass/Block/InsufficientEvidence/Stale, ObservedAt, exclusive ValidUntil, sample counts, cohorts, evidence levels. `AgentLaunchReadinessView` has Metrics, LatencyTargets, CostPosture, three bools, and string Blockers.
Fix: Name the registry/projection contract (Story 5.5/8.7) the panel reads and mark the current page as superseded evidence.

**[Implementation readiness]** — State Patterns give no numbers (§ EXPERIENCE.md § State Patterns, § High-Risk Pending Commands)
Polling cadence, catch-up timeout, retry, stale threshold, and what 'a client timeout forces refresh' means are absent. Two built write paths already diverge (250 ms/5 s vs 200 ms/8 s); DW-5 records a product ambiguity the spine should have answered; SignalR nudge subscription is unstated.
Fix: Add a 'Projection catch-up contract' block: interval, max wait, what renders on exhaustion, freshness threshold, and whether nudges shorten the poll.

**[Implementation readiness]** — FC-TBL path undecided (§ EXPERIENCE.md § Interaction Primitives, § FrontComposer Readiness › FC-TBL; DESIGN.md › provider-catalog-grid, proposal-queue-grid)
'FC-TBL / FluentDataGrid' conflates the generated `[Projection]` lane (Fluxor ViewKey, FcStatusFilterChips, FcFilterSummary, FcFilterEmptyState) with a hand-authored grid. Every built grid is the latter in `FcAggregateListPage`, which neither spine mentions. 'Needs my action' has no FC-TBL slot mapping.
Fix: Decide per grid; for hand-authored grids list mandatory Fc components (FcFilterEmptyState, FcFilterResetButton, FcExpandInRowDetail) and how 'needs my action' maps to FcStatusFilterChips.AvailableSlots.

### Medium (38)

**[Flow coverage]** — UJ-5 / Priya is not a PRD journey (§ EXPERIENCE.md § Key Flows, line 347)
`UJ-5 — Priya governs a production-like launch` is numbered as a PRD journey but PRD §2.3 stops at UJ-4 and no Priya persona exists. A consumer tracing UJ-5 to the PRD finds nothing.
Fix: Keep the flow, retitle it `UX-J5 (UX-derived)` and name the FRs it is grounded in (FR-26..FR-28, NFR-9..NFR-14, RQ-1).

**[Flow coverage]** — FR-18 proposal-expiry configuration has no surface (§ EXPERIENCE.md § UX Closure Decisions, line 365)
The 24 h default, 1 h–30 d range, future-only rule is recorded but absent from the `hexa` configuration IA row, `agent-config-form` in both spines, and every flow step.
Fix: Add 'proposal expiry duration' to the `hexa` configuration IA row and the `agent-config-form` field list in both spines; add a UJ-1 step or an explicit 'not in UJ-1' note.

**[Token completeness]** — No pinned Fluent UI Blazor package version (§ DESIGN.md § Colors, line 157)
'Verify against the pinned Fluent UI package' is stated but no pin is named. Tenants' DESIGN.md pins `5.0.0-rc.4-26180.1` inline. Without the pin, inherit-by-name has no fixed referent.
Fix: One line in `DESIGN.md § Brand & Style`: the package/version or the central-package-management file that owns it.

**[Token completeness]** — No contrast target and no per-role badge appearance (§ DESIGN.md § Colors, line 157)
WCAG 2.2 AA is mandated but 4.5:1 text / 3:1 non-text is never stated, nor which `BadgeAppearance` (Tint vs Filled) each role uses. Tenants specified Tint default, Filled for Danger and Severe.
Fix: Add an `appearance` line per badge component and one sentence under § Colors: 'AA 4.5:1 text, 3:1 non-text; inherited Fluent pairs assumed AA at the pinned version and re-verified when the pin changes'.

**[Token completeness]** — Icon half of 'role + icon + text' unspecified (§ DESIGN.md § Brand & Style, § Colors)
No icon is named for any of the seven status roles or the readiness/provider/proposal badge states. Downstream will pick eight icons ad hoc across three badge components.
Fix: A role→icon table in `DESIGN.md § Colors` (Fluent icon names) or an explicit delegation naming the owning story and the one-icon-per-role rule.

**[State coverage]** — No offline / connection-lost / reconnecting state (§ EXPERIENCE.md § State Patterns)
The entire `submitted → authoritative pending → projection-confirmed` contract depends on receiving projections; the only treatment is 'a client timeout forces refresh'. Unstated: what the pending indicator shows while disconnected, whether Call hexa and the six high-impact actions are disabled, whether the advisory lock survives reconnect.
Fix: One global row: 'connection lost → status region notice, pending items stay pending, high-impact submit disabled with reason, lock re-evaluated from authoritative status on reconnect'.

**[State coverage]** — No concurrent-edit / stale-detail treatment for Proposal detail (§ EXPERIENCE.md line 185)
Two approvers on one proposal is the normal Confirmation-mode case; the spine says EventStore concurrency is authoritative but not what the UI does on version conflict or when the proposal goes terminal under the viewer.
Fix: Add a `stale proposal` row: preserve unsaved edit text, show authoritative state and who changed it, offer refresh; approve/reject disabled until refreshed.

**[Bloat & overspecification]** — NFR-14 thresholds restated in six places (§ DESIGN.md lines 248–250; EXPERIENCE.md lines 54, 108, 273–283, UJ-5 steps 6–7, line 373)
`§ Browser Performance Evidence` is a near-verbatim copy of the readiness register's NFR-14 contract, which is already a listed source. One threshold change requires six edits.
Fix: Keep one UX-owned statement (what the panel shows, the three ticks, the live-region mutation rule) and replace the others with 'per `launch-readiness-register.md § NFR-14`'.

**[Inheritance discipline]** — 'Conversation owner' never reconciled with AD-8 Facilitator (§ EXPERIENCE.md § Information Architecture line 47; both spines › approver-policy-builder)
Architecture AD-8 states Conversations exposes no owner and V1 resolves 'owner' as `ParticipantRole.Facilitator`. Neither spine mentions Facilitator.
Fix: One clause in `approver-policy-builder`: 'source label `Conversation owner` (resolved as Conversation Facilitator per AD-8 until Conversations adds an owner resolver)'.

**[Accessibility]** — Proposal editor keyboard choreography stops at a capability list (§ EXPERIENCE.md § Accessibility Floor; § Interaction Primitives)
Version selection control, focus and announcement when regeneration replaces editor content, what 'compare metadata' is, and whether Esc in the textarea discards edits are unspecified.
Fix: Version selection is a radio group with kind/author/timestamp labels; regeneration keeps focus on Regenerate then announces 'Version N generated' with a go-to action; exit is an explicit button with unsaved-changes confirmation; Esc never discards editor content.

**[Accessibility]** — High-risk confirmation dialogs under-specified (§ DESIGN.md § Components; EXPERIENCE.md § Interaction Primitives)
Initial focus on Cancel, Enter on the body must not commit, and what 'incomplete restrictive deletion confirmation' looks like are unstated.
Fix: Initial focus on the non-committing action; destructive action never the default button; body names resource and future-only effect in one localized string; note 3.3.7 security exception if type-to-confirm is chosen.

**[Accessibility]** — Grid status carrier contradicts DESIGN (§ datagrid.md vs DESIGN.md § Colors; DESIGN.md § Components (two grids))
FC-TBL's `[ProjectionBadge]` renders `FcStatusIcon` (icon + accessible name + tooltip); DESIGN mandates role + icon + visible text for every status. Developer must choose without guidance.
Fix: Decide per grid: accept `FcStatusIcon` or require a visible text column; do not leave both rules standing.

**[Accessibility]** — Pending-count badge has no accessible name contract (§ DESIGN.md › proposal-notification; navigation.md § Parameters)
Shell badge labels come from `IBadgeCountService`; AT will read '3' unless the domain can name it.
Fix: Verify the shell exposes a localizable badge label or wrap the count in a domain-named link ('3 proposals pending approval').

**[Accessibility]** — Hover-revealed row actions not required to reveal on focus (§ EXPERIENCE.md § Interaction Primitives)
Only required actions are guaranteed non-hover-only; secondary actions vanish for keyboard users (2.1.1, 1.4.13).
Fix: Any hover-revealed action also renders on row focus-within and is dismissible; tooltips hoverable and Esc-dismissible.

**[Accessibility]** — WCAG 2.5.8 Target Size unaddressed under density settings (§ FrontComposer settings.md; DESIGN.md § Components)
Compact density can drop chips, row-action icons, version rows, and badge triggers below 24×24 CSS px.
Fix: Require 24 px minimum (or spacing exception) for every actionable target in all three densities; add a Compact check to the conformance lane.

**[Accessibility]** — Forced refresh after timeout destroys context silently (§ EXPERIENCE.md § High-Risk Pending Commands)
Nothing about restoring focus or announcing what happened.
Fix: After forced refresh, focus the route heading and announce 'Status refreshed. Approval is still pending.'

**[Accessibility]** — Command palette entries not specified (§ EXPERIENCE.md § Interaction Primitives)
Whether Agents registers palette commands, how they are policy-gated, and that sequence shortcuts are subject to 2.1.4 are unstated.
Fix: List Agents palette entries (or state none for V1) and their authorization behaviour.

**[Accessibility]** — Override diagnostics presented as coverage for hand-authored pages (§ EXPERIENCE.md § FrontComposer Readiness)
HFC1050–1055 apply only to FrontComposer customization overrides; plain Agents .razor pages get no build-time accessibility check.
Fix: State that diagnostics cover overrides only; hand-authored surfaces rely on the Agents axe/keyboard lane and manual AT matrix.

**[Governance & audit]** — Pass and Ready/Callable can render Success past ValidUntil (§ EXPERIENCE.md § Launch Readiness Evidence, § Provider And Model; DESIGN.md › provider-status-badge, launch-readiness-panel)
No rule tells the client to re-evaluate validity on a cadence or demote when it lapses on an open page.
Fix: Re-evaluate on poll/focus; when T ≥ ValidUntil with no newer observation, show `Stale — re-observation required`, never the prior Success.

**[Governance & audit]** — Qualification callability vs production enablement not distinguished (§ EXPERIENCE.md § Agent Readiness, § UJ-5; DESIGN.md › agent-readiness-badge)
A production-like profile can show `hexa is callable` in Success while the register says NOT READY.
Fix: Badge and overview carry EnvironmentProfile, gate-set name, matrix version, RegistryRevision; production enablement is a separate indicator never Success unless RQ-1 is READY.

**[Governance & audit]** — Regeneration failure on an existing proposal undefined (§ EXPERIENCE.md § Proposal Lifecycle, § Generation Failure Record)
A dev may mark the proposal failed and strand approvable versions.
Fix: Regeneration failure leaves the proposal non-terminal with prior versions intact, records a linked failure record, announces it, and keeps approve/reject/abandon available.

**[Governance & audit]** — posting failed is unclassified (§ EXPERIENCE.md § Proposal Lifecycle; DESIGN.md › proposal-state-badge, operational-status-panel)
Not marked terminal; 'show recovery/status' never says whether an approver may retry, re-approve, or must start a new call.
Fix: Declare it terminal-for-this-approval; recovery is a new Agent Call or an explicit idempotent `retry posting` reusing the deterministic MessageId. Pick one.

**[Governance & audit]** — Terminal-proposal enforcement is server-side only (§ EXPERIENCE.md › proposal-editor, § Proposal Lifecycle)
'Cannot post' is a treatment note; nothing says controls are removed on rejected/abandoned/expired/posted, and FR-16's regeneration block is not restated.
Fix: On any terminal state the editor is read-only, resolution actions are absent, and the sole action is 'Start a new Agent Call'.

**[Governance & audit]** — Expiry: threshold, race with approval, and configuration surface (§ DESIGN.md § Colors, › agent-config-form; EXPERIENCE.md § IA, § UX Closure Decisions)
'Nearing expiry' has no threshold (cannot be fixed when expiry ranges 1 h–30 d); expiry racing approval has no UI outcome; the FR-18 configuration surface is missing.
Fix: Nearing = percentage of the proposal's lifetime; add 'approval rejected — proposal expired at ExpiresAt'; add expiry duration to `hexa` configuration with future-only copy and ExpiresAt on every proposal.

**[Governance & audit]** — FR-7 disclosure category not carried into version-history or audit panel (§ DESIGN.md › audit-evidence-panel, version-history; EXPERIENCE.md › approver-policy-builder)
The audit panel lists approver and timestamp but not the policy basis that authorized the decision, and no component says how redacted/omitted renders.
Fix: Each approval decision row shows policy basis rendered by disclosure category with a whole-string placeholder for redacted/omitted; API and UI share the category.

**[Governance & audit]** — Projection-lag vocabulary collides with register Stale (§ EXPERIENCE.md § List And Detail Surfaces; DESIGN.md › operational-status-panel)
Read surfaces never show projection version or 'as of'; 'stale/degraded where relevant' is undefined; the shipped page maps not-caught-up → `Freshness.Stale`.
Fix: Reserve `Stale` for expired evidence; use `catching up` for projection lag; every authoritative read surface exposes projection id + version in mono.

**[Implementation readiness]** — Generation-failure record has no schema owner (§ EXPERIENCE.md § Generation Failure Record)
The spine refuses to define it yet requires two panels to render it. No Contracts type exists.
Fix: Name the owning story and the minimum safe fields (interaction id, failure class, timestamp, authorization gate).

**[Implementation readiness]** — FluentMessageBar cannot carry the DESIGN palette (§ DESIGN.md › operational-status-panel, audit-evidence-panel; frontmatter colors)
MessageBar Intent is Success/Warning/Error/Info/Custom only; Severe, Important, Subtle have no mapping.
Fix: State the mapping (Severe→Error + blocked icon, Important→Warning + question icon, Subtle→Info) or restrict MessageBar to page notices and use FluentBadge per item.

**[Implementation readiness]** — Hybrid page measure not expressible in FC-LYT (§ DESIGN.md › cost-control-editor, audit-governance-panel; front-composer-shell.md § Layout)
'Constrained authoring plus full-width evidence' vs one FcPageLayoutMode per page.
Fix: Pick one measure per page or split into two routes.

**[Implementation readiness]** — Pending-count notification seam not named (§ DESIGN.md › proposal-notification; navigation.md)
FrontComposer's rail renders count badges from `IBadgeCountService`; the spine never names it, so a dev hand-rolls a badge or never finds the seam.
Fix: Name `IBadgeCountService` as the producer and the queue route as the status entry.

**[Implementation readiness]** — Accordion rule ambiguous where it matters and violated where clear (§ DESIGN.md § Layout & Spacing; EXPERIENCE.md § Interaction Primitives)
Overview and Proposal detail each have 2+ titled sibling sections with no FluentAccordion; the spine never says whether the editor's editable region is the 'single primary content region'.
Fix: List per surface which region is primary and which sections become FluentAccordionItems.

**[Implementation readiness]** — Icon source contradicts FrontComposer (§ DESIGN.md § Colors 'icon vocabulary')
DESIGN says verify against the Fluent package; FrontComposer says use the curated `FcFluentIcons` factory, never the icons NuGet. The built registration reuses four glyphs across five nav entries because the curated set lacks them.
Fix: Name `FcFluentIcons` as the only icon source and list required glyphs so missing ones become a FrontComposer request.

**[Implementation readiness]** — Proposal-expiry configuration has no surface (§ EXPERIENCE.md § UX Closure Decisions)
Same gap as Flow coverage and Governance; no IA row, component, or config field.
Fix: Add the field to `agent-config-form` with future-only copy.

**[Implementation readiness]** — Component rows are behavioural prose, not buildable specs (§ EXPERIENCE.md § Component Patterns; DESIGN.md § Components)
proposal-editor: no length limit, dirty state, confirmation surface, regenerate-while-pending rule. version-history: no selection control. proposal-queue-grid: no default sort, page size, age computation. cost-control-editor: no units.
Fix: Add a 'Props / events / variants' line per row, at minimum for the six high-impact families.

**[Implementation readiness]** — Localization mechanics under-specified (§ EXPERIENCE.md § Voice and Tone; DESIGN.md § Typography)
No resource type, key convention, or enforcement named. Built practice: `IStringLocalizer<AgentsResources>`, keys `Agents.<Surface>.<Item>`, resx at 599/599 parity, plus enum-keyed lookups the 'no fragments' rule neither allows nor forbids.
Fix: Codify resource type, key convention, the enum-keyed allowance, and `AgentsResourcesParityTests` as the conformance gate.

**[Implementation readiness]** — Pending-command pattern not chosen (§ EXPERIENCE.md § FrontComposer Readiness)
FrontComposer ships a pending-outcome resolver / FcPendingCommandSummary; Agents hand-rolls `AgentSetupTruthState` per page.
Fix: Name the Agents truth-flow helper as the standard and state whether FrontComposer's resolver is in or out.

**[Implementation readiness]** — Flow-to-story mapping broken by the tracker (§ EXPERIENCE.md § Key Flows; sprint-status.yaml)
Epics say the 18-story Epic 5 is superseded by Epics 5–8; sprint-status.yaml still tracks the old slugs. Story 6.5 (capacity backpressure) has no flow.
Fix: Add a Story column to Key Flows and regenerate sprint-status against the replacement epics.

**[Implementation readiness]** — Proposal-state vocabulary mismatch with contract (§ EXPERIENCE.md § Proposal Lifecycle; DESIGN.md › proposal-state-badge)
Spine lists `generated` and `pending approval` as distinct; `ProposedAgentReplyState` collapses both into `Pending`.
Fix: Align the table to the contract enum or state that `generated` is the initial Pending version.

### Low (27)

**[Flow coverage]** — FR-7 disclosure category never rendered (§ EXPERIENCE.md § Component Patterns › approver-policy-builder, audit-evidence-panel)
The user-visible / operator-only / redacted / omitted category for policy basis appears nowhere in either spine.
Fix: One sentence per component: 'policy basis renders per its FR-7 disclosure category; redacted/omitted show a category label, never the source'.

**[Flow coverage]** — Version comparison scope uncommitted (§ EXPERIENCE.md § Accessibility Floor, line 231)
PRD UJ-3 says Anika 'compares the versions'; the spine offers only 'compare metadata'. Whether V1 has side-by-side content comparison is open.
Fix: State it either way in `version-history` (e.g., 'V1: select-to-view one version at a time; no diff view').

**[Token completeness]** — Pipe-separated colour lists are not resolver-flattenable (§ DESIGN.md lines 59, 63, 67)
`components.*.color` values like `'{a} | {b} | {c}'` and free-text `base`/`layout` will not flatten mechanically. Same convention as Tenants; note only.
Fix: None required; known convention.

**[Component coverage]** — conversation-agent-call: dialog vs panel unresolved (§ DESIGN.md frontmatter line 100)
'Fluent prompt dialog/panel' leaves focus, dismissal, and mobile behaviour open.
Fix: Commit to one.

**[Component coverage]** — proposal-notification has no visual anatomy (§ DESIGN.md § Components, line 262)
Where the pending-count badge lives (shell nav entry, overview header) is not stated.
Fix: Name the shell slot, e.g., 'count badge on the Agents › Proposal queue nav entry, policy-gated'.

**[State coverage]** — Call hexa treatment when hexa is not callable unspecified (§ EXPERIENCE.md lines 130–136, 220)
Hidden, shown-disabled with reason, or shown and rejected on submit is not stated.
Fix: One clause: visible-but-disabled with the safe blocker text.

**[State coverage]** — Prompt surface states unspecified (§ EXPERIENCE.md § Component Patterns › conversation-agent-call)
Empty prompt, over-length prompt, submit-disabled are not covered.
Fix: 'Prompt required; no client-side length limit (context budget is server-owned)'.

**[Visual reference coverage]** — Spine-only decision not recorded (§ .memlog.md line 20; EXPERIENCE.md § Foundation)
The memlog leaves 'mock coverage decision' pending and no later entry closes it. The spines assert precedence over mockups that do not exist but never say none were produced by decision.
Fix: One memlog `(decision)` line and one clause in Foundation: 'No mockups or wireframes; the spines are the sole reference (2026-08-02)'.

**[Bloat & overspecification]** — UX Closure Decisions is a changelog (§ EXPERIENCE.md § UX Closure Decisions)
Every bullet is already normative elsewhere in the file and in the memlog.
Fix: Move to `.memlog.md` or keep as a 3-line pointer.

**[Bloat & overspecification]** — PRD non-goals restated twice (§ EXPERIENCE.md § Foundation para 4; § Inspiration & Anti-patterns bullet 5)
Both restate no memory, tools, project/folder, ambient triggers, external channels.
Fix: Keep one.

**[Inheritance discipline]** — 'pending approval' state absent from AD-5 (§ EXPERIENCE.md § Proposal Lifecycle line 168; DESIGN.md › proposal-state-badge line 200)
Architecture's append-only state list has no such state; consumers find an eleventh state.
Fix: Mark it 'display state = any non-terminal version awaiting an approver; not persisted' or drop it.

**[Inheritance discipline]** — PRD term 'Global Providers Aggregate' absent (§ EXPERIENCE.md § Information Architecture › Provider catalog)
Spines say 'Provider catalog', architecture says `ProviderCatalog`, PRD says Global Providers Aggregate.
Fix: One parenthetical in the IA row naming all three.

**[Inheritance discipline]** — Casing drift (§ EXPERIENCE.md lines 96/370 vs 169/327; 297; 355 vs 108)
`Approved`/`approved`, `Approver Policy`/`approver policy`, `Level 4–5`/`Level 4/5` vs PRD 'Levels 4 and 5'.
Fix: Normalise to PRD forms.

**[Shape fit]** — Browser Performance Evidence adds no UX decision beyond its first paragraph (§ EXPERIENCE.md § Browser Performance Evidence)
Source restatement; see Bloat.
Fix: Shrink to the first paragraph plus a register reference.

**[Shape fit]** — EXPERIENCE.md frontmatter lacks description (§ EXPERIENCE.md frontmatter)
DESIGN.md has one; the calibration examples omit it too. Cosmetic.
Fix: Optional.

**[Accessibility]** — WCAG 2.4.11 Focus Not Obscured (§ DESIGN.md § Components (pinned columns, action rail, MessageBar regions))
Sticky elements could overlap focused rows.
Fix: Declare no sticky element overlaps the content scroll area, or set scroll-padding equal to sticky heights.

**[Accessibility]** — WCAG 2.5.7 Dragging Movements (§ EXPERIENCE.md § Component Patterns › approver-policy-builder)
Policy-source row ordering is unspecified and could be drag-only.
Fix: Row order is edited with buttons or a menu, never drag-only.

**[Accessibility]** — WCAG 3.2.6 and 3.3.8 not recorded as N/A (§ EXPERIENCE.md § Accessibility Floor)
Neither applies as written, but the conformance lane will report them as gaps.
Fix: Record both as N/A with the reason.

**[Accessibility]** — RTL not described in logical terms (§ DESIGN.md § Layout & Spacing)
'Version and action rail' and pinned columns should use inline-start/end.
Fix: One sentence in DESIGN.md § Layout & Spacing.

**[Governance & audit]** — agent-readiness-badge includes status-important with no mapped state (§ DESIGN.md › agent-readiness-badge; EXPERIENCE.md § Agent Readiness)
A dev has no trigger for it.
Fix: Add `ambiguous Party identity` / `policy basis unresolved` rows mapped to Important, or drop the colour.

**[Governance & audit]** — 'hide or deny' for policy-gated nav produces different disclosure profiles (§ EXPERIENCE.md § FrontComposer Readiness)
The two choices leak differently.
Fix: Hide unauthorized entries; deny-with-safe-copy on direct navigation.

**[Governance & audit]** — Budget 80%/100% basis unspecified (§ DESIGN.md › cost-control-editor)
Usage only vs usage plus open reservations.
Fix: Thresholds evaluate usage plus outstanding reservations; label both figures.

**[Governance & audit]** — Response-mode change copy silent on pending proposals (§ EXPERIENCE.md › response-mode-toggle)
Switching to Automatic does not say pending proposals remain proposals.
Fix: Add that sentence to the confirmation copy.

**[Implementation readiness]** — 'Fluent segmented control' does not exist (§ DESIGN.md › response-mode-toggle; UX-DR23)
`FluentRadioGroup` is what exists and what the code uses.
Fix: Say FluentRadioGroup.

**[Implementation readiness]** — 'Fluent list' and 'Fluent numeric form controls' are not component names (§ DESIGN.md › version-history, cost-control-editor)
Unverifiable against the package.
Fix: Name FluentListbox (or custom listbox) and FluentNumberInput.

**[Implementation readiness]** — Side-by-side promise conflicts with Constrained editor (§ EXPERIENCE.md § Responsive & Platform)
Desktop promises side-by-side metadata and version history while the editor is mandated 75 rem Constrained. Built stacks them.
Fix: Allow FullWidth for detail on wide desktop or drop the promise.

**[Implementation readiness]** — Overview 'Not available yet' stubs unsanctioned (§ EXPERIENCE.md § State Patterns)
A conformance reviewer cannot tell stub from defect.
Fix: Add a 'deferred metric' treatment (Subtle role, whole-string label).

## Mechanical notes

- Misplaced general rules: DESIGN.md lines 256 and 258 (FluentAccordion rule; high-impact confirmation / pending-styling rule) sit under `### audit-governance-panel` although they apply to every component.
- Contradictory wording: EXPERIENCE.md § Information Architecture opens with 'Candidate entries' (line 40) and closes with 'Surface closure status: final' (line 60).
- Hedge in a final spine: EXPERIENCE.md line 116 'Exact implementation token names can be refined by architecture' while AD-5 and AD-10 already name them.
- Frontmatter: both spines carry name, status: final, created, updated 2026-08-02, sources; all 16 paths resolve; DESIGN.md additionally carries description.
- Cross-refs: all {colors.*}, {spacing.*}, {typography.*} references resolve; FC-LYT/FC-TBL/FC-A11Y/FC-L10N resolve; six operation-family names and seven NFR-14 tick names match architecture AD-25/AD-26 and the register; BadgeColor names verified 8/8 against Fluent UI Blazor v5.
- Name inconsistencies: Approved/approved, Approver Policy/approver policy, Level 4–5 / Level 4/5; 'pending approval' absent from AD-5; 'conversation owner' vs AD-8 Facilitator; 'Global Providers Aggregate' absent from spines.
- Mermaid: none used. The single ```text fence (EXPERIENCE.md lines 118–122) is correctly closed.
- Visual references: none exist; spines-win statement present once (EXPERIENCE.md line 26); spine-only decision not explicitly recorded.
- Fluent v5 fidelity: 'Fluent segmented control' does not exist; FluentMessageBar intents cannot express Severe/Important/Subtle; all 14 reference paths cited by EXPERIENCE.md exist on disk.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-governance.md`
- `review-implementation-readiness.md`
