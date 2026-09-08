# Reconciliation — Hexalith Agents UX Update 2026-09-08

Inputs reconciled into `DESIGN.md` and `EXPERIENCE.md` on 2026-09-08. Three change signals were applied at full depth. Where a finding needed a decision that belongs to Product, Architecture, or Hexalith.Conversations, it is deferred with an owner rather than invented.

- **Signal A — UX validation 2026-09-08** (`validation-report.md`; lenses rubric, accessibility, governance, implementation readiness; 92 findings).
- **Signal B — PRD reconciliation 2026-09-08** (`prd.md` updated 2026-09-08: FR-2 membership, FR-4/FR-5 pricing governance, FR-7 Facilitator + Conversation-access re-check + segregation of duties + disclosure default, FR-9 Safe Context Budget, FR-11 AI-generated marker, FR-12 `Unknown` outcome, FR-16 regeneration ceiling, FR-17 approval-time safety re-check + edit provenance, FR-18 state machine, FR-29 accepted-vs-confirmed stages, FR-30 governance operations, FR-31 untrusted content, FR-32 caps/rate limits/ceiling/override, OQ-3/6/9/10 amended, OQ-14..OQ-19, EXT-CONV-UI-1).
- **Signal C — Story 5.3 and shipped-code drift** (pricing editor defaults, `ProviderCatalogMutation` family, `CapabilityVersion` concurrency token, `/agents/conversation-call` admin route, lifecycle-derived readiness, 9 of 13 nav entries).

## 1. Canonical vocabulary (binding for both spines)

### Components (22)

`agent-readiness-badge` · `provider-status-badge` · `proposal-state-badge` · `response-mode-toggle` · `agent-config-form` · `approver-policy-builder` · `provider-catalog-grid` · `proposal-queue-grid` · `proposal-editor` · `version-history` · `conversation-agent-call` · `agent-response-marker` (new, FR-11) · `conversation-context-policy-panel` · `content-safety-policy-editor` · `cost-control-editor` · `launch-readiness-panel` · `audit-governance-panel` · `proposal-notification` · `operational-status-panel` · `audit-evidence-panel` · `high-impact-confirmation` (new) · `pending-command-indicator` (new).

Every component has a frontmatter entry and a `### name` section in DESIGN.md, and a row in EXPERIENCE.md Component Patterns.

### Proposal lifecycle (aligned to PRD FR-18 and `ProposedAgentReplyState`)

`Pending` · `Edited` · `Regenerated` · `Approved` · `PostingPending` · `Posted` (terminal) · `PostingFailed` · `Rejected` (terminal) · `Abandoned` (terminal) · `Expired` (terminal). Display note: the PRD names the failed-post state `PostFailed`; the contract enum is `PostingFailed`; the spines use the contract name and note the alias once. `generated` and `pending approval` are no longer states: the initial generated version is the first `Pending` version. `PostingFailed` is non-terminal with bounded audited retry (PRD FR-18); recovery is an explicit idempotent `retry posting` reusing the deterministic `MessageId`, or a new Agent Call once retry is exhausted.

Display-only outcomes (not persisted states): `superseded by another decision`, `authority unresolved`, `expired at approval`, `duplicate submission (idempotent)`.

### Agent Call states (aligned to `AgentCallOperationStatus` plus Story 6.7)

`submitted` (local) · `authoritative pending` · `authorized` · `denied` · `context loading` · `context blocked` · `safety blocked` · `budget blocked` · `capacity queued` · `capacity rejected` · `generating` · `generation failed` · `generated` · `unknown outcome` (FR-12 typed `Unknown`, never retried) · `projection-confirmed terminal`. Contracts enum must grow by `SafetyBlocked`, `BudgetBlocked`, `CapacityQueued`, `CapacityRejected`, `UnknownOutcome` (Story 6.7 / 6.5 / 6.4).

### Agent readiness states

`callable` · `active, not proven callable` · `checking` · `stale` · `invalid configuration` · `missing party identity` · `provider unavailable` · `disabled` · `authority unresolved` (Important). Source of proof: the versioned setup/callability result published by Story 5.5 through the `agent-setup-readiness` projection at a `RegistryRevision`; the built `AgentReadiness.MapState` (`Callable = Active && no blockers`) is superseded and must be replaced, not extended. `AgentReadinessStatus` must grow by `ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved`.

### Freshness vocabulary

`Stale` is reserved for readiness or provider evidence whose `ValidUntil` has passed. Projection lag on a read surface is `catching up`, never `Stale`. Post-timeout pending is `awaiting projection`.

## 2. Surface binding table (EXPERIENCE.md Information Architecture)

| Surface | Route | Nav order | RequiredPolicy | Layout | Owning story | Read/write contract |
|---|---|---|---|---|---|---|
| Agents overview | `/agents` | 0 | `Agents.Administrator` | FullWidth | 5.2, 5.5, 5.7 | `AgentInspectionResult` + Story 5.5 setup/callability result; pending count via `IBadgeCountService` |
| `hexa` configuration | `/agents/configuration` | 1 | `Agents.Administrator` | Constrained | 5.2, 5.7 | `AgentSetupResult`, `AgentCommandAcceptance` (FR-29 stages); adds proposal expiry duration and regeneration ceiling fields |
| Provider catalog | `/agents/providers` | 2 | `Agents.Administrator` | FullWidth | 5.3, 5.5 | `ProviderCatalogEntryView`, `ProviderCatalogCommandAcceptance`; `ProviderReadinessResult` from 5.5 |
| Approver policy | `/agents/approver-policy` | 3 | `Agents.Administrator` | Constrained | 5.4 | `AgentApproverPolicy`, `ApproverPolicySourceKind` (closed list; `ConversationOwner` value labelled Conversation Facilitator) |
| Conversation context policy | `/agents/context-policy` | 4 | `Agents.Administrator` | Constrained | 6.2 | read model to be defined by Story 6.2 from `AgentInteractionContextPolicy`: policy version, Safe Context Budget components, declared bounded behaviors (none in V1) |
| Content safety policy | `/agents/content-safety` | 5 | `Agents.Administrator` | Constrained | 6.3, 8.4 | `AgentContentSafetyPolicy` (`BlockedOutputCategories`, `RestrictedOutputCategories`), `ConfigureAgentContentSafetyPolicy`; publish command by Story 8.4 |
| Cost controls | `/agents/cost-controls` | 6 | `Agents.Administrator` | Constrained | 6.4, 8.4 | budget policy contracts to be defined by Story 8.4 (FR-32): caps, rate limits, consumption, reserved vs settled, override |
| Proposal queue | `/agents/proposals` | 7 | `Agents.Approver` | FullWidth | 7.1 | `PendingProposalsResult` |
| Proposal detail/editor | `/agents/proposals/{AgentInteractionId}` | — | `Agents.Approver` | Constrained | 7.2–7.6 | `ProposalDetailView`, `ProposalVersionSummary` |
| Operational status | `/agents/status` | 8 | `Agents.Operator` | FullWidth | 6.7, 8.7 | `AgentOperationalStatusSummaryView`; failure record fields by Story 6.1 |
| Launch readiness | `/agents/launch-readiness` | 9 | `Agents.Operator` | FullWidth | 5.5, 8.7 | `launch-readiness` projection checkpoint (Story 8.7); current `AgentLaunchReadinessView` is superseded evidence |
| Audit evidence | `/agents/audit`, `/agents/proposals/{AgentInteractionId}/audit` | 10 | `Agents.AuditOperator` | list FullWidth, detail Constrained | 7.x, 8.x | `AuditEvidenceResult`, `AuditAvailabilityStatus` |
| Audit governance | `/agents/audit-governance` | 11 | `Agents.AuditOperator` | Constrained | 8.1–8.3 | legal hold, export, deletion commands and progress projections by Stories 8.1–8.3 |
| Conversation invocation | Conversation-owned; no Agents nav entry | — | Conversation access + Agent call permission (server) | Conversation surface | 6.7 via `EXT-CONV-UI-1` | `IConversationAgentCallGateway` |

`/agents/conversation-call` is a pre-integration harness: de-listed from nav now, removed or blocked before Story 6.7 closes.

Nav hiding is disclosure, not authorization: unauthorized entries are hidden; direct navigation renders one `not available` denial state with identical copy, status class, and timing for unauthorized, foreign-tenant, and non-existent ids.

## 3. Committed numbers and mechanics

| Item | Decision |
|---|---|
| Projection catch-up | Poll every 250 ms up to 8 s after an accepted write (unifies the built 250 ms/5 s and 200 ms/8 s paths). A SignalR projection nudge triggers an immediate poll and never replaces polling. On exhaustion: keep the pending state, render `awaiting projection` with a refresh action, never Success, never `Stale`. |
| Read freshness | Every authoritative read surface exposes projection id and version in `{typography.mono}`. A read whose version is below the last accepted write's expected version renders `catching up`. |
| Expiry warning | `nearing expiry` begins at 10% of the proposal's configured window or 1 hour before `ExpiresAt`, whichever is smaller. Rendered as an absolute culture-formatted timestamp plus a static relative label refreshed at most once per minute, outside any live region; announced once per proposal. WCAG 2.2.1 position: the expiry is an essential governance limit set by the tenant; no in-UI extension. Product sign-off recorded as a deferred item. |
| Target size | Every actionable target is at least 24×24 CSS px in all three FrontComposer densities, or meets the spacing exception. |
| Reflow | Every route, including proposal detail, version history, and `high-impact-confirmation`, stays operable at 320 CSS px width (400% zoom). Narrow width alone is never a reason for unavailability; unavailability requires content that cannot render, with a visible reason. |
| Contrast | AA 4.5:1 text, 3:1 non-text and focus indicators; inherited Fluent pairs are assumed AA at the pinned package and re-verified when the pin changes. |
| Fluent pin | `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1`, owned by `Directory.Packages.props`. |
| Badge appearance | `BadgeAppearance.Tint` default; `Filled` for Danger and Severe only; `Filled` does not count toward no-color-only. |
| Icons | `FcFluentIcons` curated inline-SVG factory is the only icon source. Curated glyphs today: Apps, BuildingPeople, ChevronRight, DevMode, DeveloperBoard, Navigation, People, PersonBoard, Search, Settings. Missing glyphs become FrontComposer requests, never silent reuse. |
| Live regions | Agents owns one page-level polite `role="status"` node and one `role="alert"` node per route; FrontComposer's shell live regions cover shell-owned surfaces only. |
| Localization | `IStringLocalizer<AgentsResources>`; keys `Agents.<Surface>.<Item>`; `AgentsResources.resx` / `AgentsResources.fr.resx` at exact parity, enforced by `AgentsResourcesParityTests`. Enum-keyed lookups (`Agents.<Enum>.<Value>`) are whole strings and allowed; sentence assembly from fragments is not. Every architecture token (`Ready`, `Callable`, `InsufficientEvidence`, `Stale`, `Pass`, `Block`) has a localized label. Shell dialogs with unlocalized literals (`FcDestructiveConfirmationDialog` hard-codes `Cancel`) are not reused for domain confirmations. |
| Pending-command pattern | The Agents truth-flow helper (`AgentCommandAcceptance` / `ProviderCatalogCommandAcceptance` polling) is the standard for all write surfaces; FrontComposer's `FcPendingCommandSummary` is out for V1. |
| Grids | All V1 grids are hand-authored `FluentDataGrid` inside `FcAggregateListPage`; the generated `[Projection]` lane is not used. Mandatory Fc components per grid: `FcFilterEmptyState`, `FcFilterResetButton`, `FcExpandInRowDetail`; status filters use `FcStatusFilterChips` with `needs my action` as a slot. Status cells render a visible text column plus icon, not `FcStatusIcon` alone. |
| MessageBar | `FluentMessageBar` is restricted to page-level notices (Success/Warning/Error/Info). Per-item state uses `FluentBadge` with the full role palette. |
| Page measure | One `FcPageLayoutMode` per route; hybrid measures removed. Cost controls and Audit governance are Constrained; their evidence lists live on Operational status and Audit evidence (FullWidth). |
| Proposal detail | Constrained, stacked: primary region is the editor; metadata, version history, and audit link are `FluentAccordionItem`s. The side-by-side desktop promise is dropped. |
| Accordion per surface | Overview: primary = readiness summary; Blockers and Recent activity are items. Provider catalog: primary = grid; editor is an item. Proposal detail: as above. Policy surfaces: primary = current effective policy; draft/authoring and history are items. |
| Response mode control | `FluentRadioGroup`. |
| Version selection | Radio group with kind/author/timestamp labels; V1 has no diff view (select-to-view one version at a time). |
| Confirmation surface | `high-impact-confirmation` is a focus-trapped `FluentDialog`; initial focus on the non-committing action; Enter on the body never commits; destructive action never the default button. |
| Pending control | Activated control switches to `aria-disabled="true"` and keeps focus; resolution moves focus deterministically: same-item status region, then next row, then page heading via `HeadingTabIndex=-1`. |
| Route heading | Every route renders `FcAggregateListPage`/`FcAggregateDetailPage` with a non-blank localized Heading, `HeadingTabIndex=-1`, and `PageTitle`; focus moves to the heading on navigation and when proposal detail opens from the queue. Accordion titles are h2; in-panel sections h3. |
| Command palette | Agents registers no palette commands in V1. |
| Pricing | No default values in the pricing editor. Currency is a well-formed ISO 4217 code and must equal the tenant budget currency, otherwise the row renders `status-important` and reservations are blocked. Unit prices are non-negative per FR-4; a zero unit price must be typed explicitly and the confirmation carries a `status-warning` line that a zero-priced model reserves nothing. Pricing version is a pinned column and increases strictly. Pricing edits are `ProviderCatalogMutation` high-risk: confirmation shows old → new, effective pricing version, and "applies to attempts prepared after confirmation". `CapabilityVersion` is the optimistic-concurrency token: stale or regressed submissions render `superseded by another decision`. |
| Secrets | The Agents UI never accepts a secret value. `ConfigurationReferenceId` is a reference only (EXT-SECRETS-1), operator-only visible, masked input with autocomplete off when editable; validation and error copy never echo submitted values; secret-adjacent fields never appear in URLs, clipboard actions, or diagnostics. |
| Provider catalog scope | Platform-scoped per AD-10/FR-19. Cross-tenant blast radius of a disable renders as an aggregate count of affected Agents or is omitted per disclosure category, never as tenant names. Disable confirmation lists the in-scope Agents whose callability will be blocked. |
| Session for pending lock | Authenticated user + browser tab/circuit. On reload, pending state is re-derived from the authoritative status projection and the lock re-armed. A second tab renders `pending in another session` with resolution controls `aria-disabled`. |
| Budget thresholds | 80% and 100% evaluate settled usage plus outstanding reservations; both figures are labelled. |

## 4. Finding dispositions (Signal A)

Disposition codes: **A** applied in the spines; **D** deferred with owner; **E** escalated (recorded, no spine change possible).

### Critical

| Finding | Disposition |
|---|---|
| Fail-closed viewport rule locks out approval at 400% zoom | A — Reflow rule in § 3; Responsive & Platform rewritten; LR-UI-CONFORMANCE evidence at 320 px named in Accessibility Floor. |
| Focus lost at the approval moment | A — `pending-command-indicator` keeps focus via `aria-disabled`; deterministic focus-on-resolution; polite announcements for `authoritative pending`, `Approved`, `PostingPending`, `Regenerated`; polled re-renders never steal focus. |
| Provider/model pricing has no UX rule | A — Pricing block in § 3 applied to `provider-catalog-grid` and `high-impact-confirmation`. |
| Call hexa has no Conversations integration seam; forbidden admin route shipped | A — New EXPERIENCE.md section "Conversation Integration Seam" naming `EXT-CONV-UI-1` (Conversation action contribution and registration contract), the Agents-side artifacts (exported `ConversationAgentCallPanel` + `IConversationAgentCallGateway`), the pre-integration harness rule for `/agents/conversation-call`, and a FrontComposer Readiness row. |

### High

| Finding | Disposition |
|---|---|
| High-impact confirmation and pending indicator have no named component | A — `high-impact-confirmation`, `pending-command-indicator` added to both spines; misplaced paragraphs moved out of `audit-governance-panel`. |
| Live-region delegation not backed by FrontComposer | A — Agents-owned status/alert nodes and a per-event politeness table in Accessibility Floor. |
| No route heading or post-navigation focus rule | A — § 3 Route heading. |
| Forms have no error contract | A — Forms subsection in Accessibility Floor. |
| Contrast delegated in a circle | A — AA targets in DESIGN.md § Colors; Agents-owned axe lane and manual AT matrix (NVDA+Firefox, JAWS+Chrome, VoiceOver+Safari) in Accessibility Floor. |
| French does not exist in the spines | A — FR column added to Voice and Tone; plural, culture-formatting, and architecture-token label rules; shell-dialog reuse forbidden. |
| Expiry timing has no accommodation | A — § 3 Expiry warning. D — Product sign-off on the 2.2.1 essential-exception position (owner: Product). |
| Three register families outside the high-risk list | A — `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation` added; blast-radius display on provider disable. |
| Concurrent approver conflict has no UI state | A — `superseded by another decision` outcome for every proposal action. |
| Approve with uncommitted edits | A — Approval `aria-disabled` while the editor is dirty; confirmation renders VersionId, kind, author, timestamp, full content; segregation-of-duties block when the approver last edited the version (PRD FR-7). |
| Pending-lock survival across reload/tab | A — § 3 Session; `awaiting projection`; `duplicate submission (idempotent)` outcome. |
| No `authority unresolved` state | A — Added to Agent readiness and proposal editor tables with `status-important`; added to `proposal-state-badge` palette. |
| Deep links, palette, filters lack tenant-isolation rule | A — "Detail And Deep-Link Surfaces" state rule; no palette commands in V1. |
| Provider catalog scope undeclared | A — § 3 Provider catalog scope. |
| Secret handling under-specified | A — § 3 Secrets. |
| `provider-status-badge` binds to a missing contract | A — Owning story 5.5 and target type named; interim rule: render `Unknown` with `status-subtle`, never Success, when no `Callability` field exists. |
| `agent-readiness-badge` has no proof source | A — § 1 Agent readiness states. |
| Four IA surfaces unbuildable | A — § 2 table; safety categories enumerated from PRD FR-26/OQ-9. |
| No authorization-policy matrix | A — § 2 table; Launch readiness moves to `Agents.Operator` (Priya). |
| Agent Call table missing Story 6.7 states | A — § 1 Agent Call states. |
| `launch-readiness-panel` binds to missing fields | A — Story 8.7 `launch-readiness` checkpoint named; current view marked superseded. |
| State Patterns give no numbers | A — § 3 Projection catch-up. |
| FC-TBL path undecided | A — § 3 Grids. |

### Medium

| Finding | Disposition |
|---|---|
| UJ-5 / Priya is not a PRD journey | A — Retitled `UX-J5 (UX-derived)` grounded in FR-26..FR-28, FR-30, FR-32, NFR-9..NFR-14, RQ-1. |
| FR-18 expiry configuration has no surface | A — `agent-config-form` gains proposal expiry duration (24 h default, 1 h–30 d, future-only) and regeneration ceiling; UJ-1 step added. |
| No pinned Fluent package | A — § 3 Fluent pin in Brand & Style. |
| No contrast target or badge appearance | A — § 3. |
| Icon half unspecified | A — Role→glyph table with `FcFluentIcons` names where they exist and FrontComposer requests where they do not. |
| No offline state | A — `connection lost` global row. |
| No concurrent-edit / stale-detail treatment | A — `stale proposal` row: preserve unsaved text, show authoritative state and actor where disclosure allows, refresh; resolution controls `aria-disabled` until refreshed. |
| NFR-14 restated in six places | A — One UX-owned statement in `launch-readiness-panel`; everything else references `launch-readiness-register.md § NFR-14 Browser Monotonic Timing Contract`; Browser Performance Evidence section removed. |
| Conversation owner vs Facilitator | A — Conversation Facilitator everywhere (PRD OQ-14); enum value `ConversationOwner` labelled Facilitator; no owner wording anywhere. |
| Editor keyboard choreography | A — Version radio group; regeneration keeps focus on Regenerate then announces `Version N generated` with a go-to action; explicit exit with unsaved-changes confirmation; Esc never discards editor content. |
| Confirmation dialogs under-specified | A — § 3 Confirmation surface. |
| Grid status carrier contradicts DESIGN | A — Visible text column plus icon. |
| Pending-count badge accessible name | A — `IBadgeCountService` count wrapped by the domain-named queue link `N proposals pending approval`; if the shell exposes no label slot, the count renders inside the Agents overview link instead. |
| Hover-revealed actions | A — Also render on row focus-within; tooltips hoverable and Esc-dismissible. |
| Target size under density | A — § 3. |
| Forced refresh destroys context | A — After forced refresh, focus the route heading and announce `Status refreshed. Approval is still pending.` |
| Command palette entries | A — None in V1. |
| Override diagnostics as coverage | A — Diagnostics cover overrides only; hand-authored pages rely on the Agents axe lane and AT matrix. |
| Success past ValidUntil | A — Re-evaluate on poll and window focus; demote to `Stale — re-observation required`. |
| Qualification vs production enablement | A — Badge and overview carry `EnvironmentProfile`, gate-set name, matrix version, `RegistryRevision`; production enablement is a separate indicator, Success only when `RQ-1` is READY. |
| Regeneration failure on a proposal | A — Proposal stays non-terminal with prior versions; linked failure record; approve/reject/abandon remain. |
| `posting failed` unclassified | A — Non-terminal `PostingFailed` with bounded audited retry per PRD FR-18. |
| Terminal enforcement server-side only | A — Terminal states render the editor read-only, resolution actions absent, sole action `Start a new Agent Call`. |
| Expiry threshold, race, surface | A — § 3; `expired at approval` outcome. |
| FR-7 disclosure category not carried | A — Policy basis rendered per disclosure category in `version-history` and `audit-evidence-panel`; default operator-only; redacted/omitted show a category label. |
| Projection-lag vocabulary | A — § 1 Freshness. |
| Generation-failure record schema owner | A — Story 6.1 owns; minimum safe fields: interaction id, failure class, timestamp, authorization gate. |
| MessageBar palette | A — § 3. |
| Hybrid page measure | A — § 3. |
| Pending-count seam | A — `IBadgeCountService`. |
| Accordion ambiguity | A — § 3 Accordion per surface. |
| Icon source | A — `FcFluentIcons`. |
| Expiry configuration surface | A — see above. |
| Component rows not buildable | A — Props / events / variants line for the high-impact components and grids. |
| Localization mechanics | A — § 3. |
| Pending-command pattern | A — § 3. |
| Flow-to-story mapping | A — Story column on Key Flows; capacity flow UX-J6 added for Story 6.5. D — Regenerate `sprint-status.yaml` (owner: bmad-sprint-planning; outside the UX spines). |
| Proposal-state vocabulary | A — § 1. |

### Low

All 27 low findings applied except two: the pipe-separated colour convention (known convention, no change) and the optional EXPERIENCE.md description (added anyway). Notable applications: `conversation-agent-call` commits to a `FluentDialog`; pending-count badge lives on the Proposal queue nav entry; Call hexa is visible-but-disabled with the safe blocker when `hexa` is not callable; prompt required with no client-side length limit; spine-only decision recorded in Foundation and memlog; UX Closure Decisions reduced to a pointer; non-goals stated once; `pending approval` dropped; Global Providers Aggregate / ProviderCatalog / Provider catalog named together; casing normalised to PRD forms; 2.4.11 scroll-padding rule; 2.5.7 no drag-only ordering; 3.2.6 and 3.3.8 recorded N/A; RTL logical properties; `status-important` mapped to `authority unresolved`; hide nav then deny on direct navigation; budget threshold basis; response-mode copy on pending proposals; `FluentRadioGroup`; `FluentListbox` replaced by radio group, `FluentNumberInput`; side-by-side promise dropped; `deferred metric` treatment.

## 5. PRD change mapping (Signal B)

| PRD change | Spine change |
|---|---|
| Conversation Facilitator (OQ-14, FR-7) | `approver-policy-builder` source label; forbidden copy: no Conversation owner wording; audit copy names Facilitator. |
| FR-2 / OQ-16 membership at first accepted call; removal abandons proposals | Conversation Integration Seam: membership visible on Conversation terms; removal is Conversation-owned; Agents renders `Abandoned` with reason `Agent removed from Conversation`. |
| FR-4 / FR-5 pricing, capability limits, secret reference, `CapabilityVersion` | `provider-catalog-grid` columns and editor rules; six-part eligibility set in UJ-1 and `agent-config-form` blockers. |
| FR-7 Conversation-access re-check, disclosure default, segregation of duties | Proposal states `authority unresolved`, `existence only` (approver lost access); approval blocked when approver last edited the version or is sole approver of own call; policy validation errors in `approver-policy-builder`. |
| FR-9 Safe Context Budget, Approved Bounded Context Behavior seam | `conversation-context-policy-panel` shows budget components, tokenizer basis, declared behaviors (none); `context blocked` copy names the budget; audit panel shows computed budget, measured size, `CapabilityVersion`, context mode. |
| FR-11 AI-generated marker | `agent-response-marker` component (Conversation-rendered, Agents-contributed via `EXT-CONV-UI-1`); also marks human-edited posted versions (FR-17). |
| FR-12 `Unknown` outcome, OQ-6 bounded hold and override | Agent Call `unknown outcome` state; `cost-control-editor` shows held reservations and the audited override (`TenantBudgetUpdate` family). |
| FR-16 regeneration ceiling; regeneration is a chargeable call | `agent-config-form` field; `proposal-editor` disables Regenerate at the ceiling with the reason; copy states cost-cap and rate-limit effect. |
| FR-17 approval-time safety re-check, edit provenance | UJ-3 step; `high-impact-confirmation` states the re-check; `safety blocked at approval` outcome; edited versions never presented as generated. |
| FR-18 state machine, OQ-3 expiry on read | § 1 Proposal lifecycle; `Expired` rendered whenever `ExpiresAt` has passed regardless of timer. |
| FR-25 blocked-call counts and cost-cap consumption | Operational status and Cost controls rows. |
| FR-29 accepted vs confirmed stages | State Patterns rename: `Submitted` → `AuthoritativePending` → `ProjectionConfirmed`, with projection version and freshness on every read. |
| FR-30 governance operations | Audit governance and Cost controls are callable operations with `high-impact-confirmation`; Launch readiness inspection is read-only. |
| FR-31 untrusted Conversation content | Forbidden copy: no UI presents Conversation content as instructions; safety outcome `redirect attempt recorded` visible in audit. |
| FR-32 caps, rate limits, ceiling, override, no defaults | `cost-control-editor` props; unconfigured cap or rate limit is a readiness blocker rendered `status-severe`, never a default. |
| OQ-18 permanently blocked Conversation (deferred) | Operational status shows per-Conversation `safety blocked` history so a permanently blocked Conversation is visible; no redaction UI (Product + Security own). |
| EXT-CONV-UI-1 | Conversation Integration Seam and FrontComposer Readiness row; consuming story 6.7. |

## 6. Story 5.3 and code drift (Signal C)

| Drift | Resolution |
|---|---|
| Pricing editor defaults to `0` / `USD` | Forbidden; see § 3 Pricing. |
| `Freshness = Stale` on projection lag | Rename to `catching up`. |
| 8 s vs 5 s polling timeouts | 250 ms / 8 s for all write surfaces. |
| `/agents/conversation-call` admin nav entry | De-listed; harness only. |
| `AgentReadiness.MapState` lifecycle-derived | Superseded; see § 1. |
| Nine nav entries | Twelve nav entries per § 2 (Conversation invocation has none). |
| Glyph reuse across nav entries | Missing glyphs become FrontComposer requests: inbox/queue, launch/readiness, shield/safety, budget/cost, policy/document, audit/history. |

## 7. Conflicts with prior decisions

| Prior decision (memlog / 2026-08-02 spines) | New decision | Why |
|---|---|---|
| Fail-closed responsive rule: high-impact actions unavailable at the most restrictive viewport | Narrow width alone never blocks; reflow to 320 px is required; unavailability only when content cannot render, with a reason | WCAG 1.4.10 is inside the binding AA floor; the register's NFR-13 wording ("cannot be presented safely") is preserved and clarified, not contradicted. |
| Proposal lifecycle used `generated`, `pending approval`, `posting failed` (terminal-ish) | Contract-aligned lifecycle; `PostingFailed` non-terminal with bounded retry | PRD FR-18 (2026-09-08) and `ProposedAgentReplyState`. |
| Approver Policy source "conversation owner" | Conversation Facilitator | PRD OQ-14 / AD-8. |
| UJ-3: Anika edits then approves | Anika approves a version she did not author; a second approver would be needed for her own edit | PRD FR-7 segregation of duties. |
| Complete context or blocked, no seam | Full context when it fits the Safe Context Budget; otherwise fail closed or a declared Approved Bounded Context Behavior; V1 declares none | PRD OQ-10 restored wording. |
| Disabled control while pending | `aria-disabled` control that keeps focus | Accessibility critical. |
| Desktop side-by-side metadata and version history | Stacked Constrained detail with accordion | FC-LYT allows one measure per page; reflow rule. |
| Browser Performance Evidence section restates NFR-14 | Removed; reference the register | Bloat finding; the register is a listed source. |
| UJ-5 numbered as a PRD journey | UX-J5 (UX-derived) | PRD stops at UJ-4. |

## 8. Deferred items (carried in memlog)

- Product: sign off the WCAG 2.2.1 essential-exception position on proposal expiry (no in-UI extension).
- Product + Security: OQ-18 per-category handling of unsafe historical Conversation content; UX shows blocked history only.
- Architecture: pin the projection catch-up numbers (250 ms / 8 s) and the `catching up` freshness contract in the Architecture Spine, which predates the 2026-09-08 PRD.
- Architecture / Contracts: grow `AgentCallOperationStatus`, `AgentReadinessStatus`, and add `ProviderReadinessResult` (Story 5.5), `SafetyBlocked`/`BudgetBlocked`/`CapacityQueued`/`CapacityRejected`/`UnknownOutcome`.
- Hexalith.Conversations: commit `EXT-CONV-UI-1` (owner, target, verification command).
- FrontComposer: curated glyph requests listed in § 6; localizable badge label for `IBadgeCountService` counts.
- Sprint planning: regenerate `sprint-status.yaml` against replacement Epics 5–8.
