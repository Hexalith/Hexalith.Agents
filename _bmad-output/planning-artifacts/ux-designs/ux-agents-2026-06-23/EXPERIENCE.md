---
name: Hexalith Agents
description: Behavioral spine for the Hexalith Agents FrontComposer UI. Owns surfaces, routes, policies, states, interactions, accessibility, and journeys for governed AI participation in Conversations. DESIGN.md owns visuals.
status: final
created: 2026-06-23
updated: 2026-09-09
sources:
  - ../../briefs/brief-agents-2026-06-23/brief.md
  - ../../prds/prd-agents-2026-06-23/prd.md
  - ../../prds/prd-agents-2026-06-23/addendum.md
  - ../../prds/prd-agents-2026-06-23/reconcile-brief.md
  - ../../sprint-change-proposal-2026-08-02.md
  - ../../architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ../../launch-readiness-register.md
  - ../../external-dependency-register.md
  - ../../epics.md
  - ../../../../references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs
  - ../../../../references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Program.cs
  - ../../../../references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor
  - ../../../../references/Hexalith.Tenants/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
  - ../../../../references/Hexalith.FrontComposer/_bmad-output/project-context.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/navigation.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/datagrid.md
  - ../../../../src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs
  - ./reconcile-validation-2026-09-09.md
---

# Hexalith Agents - Experience Spine

> Final spine reconciled to the PRD of 2026-09-08, the 2026-09-09 UX re-validation, and the shipped Story 5.3 code. `DESIGN.md` owns visuals; this file owns behavior, surfaces, states, accessibility, and flows. The spines win on conflict with mockups, wireframes, imports, sketches, and shipped code. Decisions are logged in `.memlog.md`; the current reconciliation, dispositions, and deferred items are in `reconcile-validation-2026-09-09.md`, its predecessor in `reconcile-validation-2026-09-08.md`, and the 2026-08-02 closure set remains in force except where a later reconciliation records a revision.

## Foundation

Hexalith Agents is a desktop-first responsive web experience composed through **Hexalith.FrontComposer**. The shell owns header, navigation, account controls, theme and density settings, command palette, skip links, keyboard shell behavior, shell localization, and shell status chrome. The Agents domain owns registered navigation entries, page bodies, domain copy, page-level live regions, Agent and proposal workflow behavior, and BFF/API-facing interaction.

The UI system is inherited: Microsoft Fluent UI Blazor v5 through FrontComposer. `DESIGN.md` is the visual identity reference and names the pinned package. This spine references `DESIGN.md` tokens by name and does not restate visual styling. Agents defines no custom theme.

This is an internal governed operational tool, not regulated UX. Product-required authorization, tenant isolation, approval, proposal versioning, provider-secret safety, cost governance, and audit evidence are first-class. Copy is plain and precise.

V1 exposes only `hexa` as product behavior. V1 excludes long-term memory, tools, project and folder content, ambient triggers, external channels, customer-facing billing, and business actions beyond posting automatic or approved replies to Conversations. These non-goals are stated here once.

By decision, no mockups or wireframes were produced; the spines are the sole reference (2026-08-02, reaffirmed 2026-09-08).

## Inspiration & Anti-patterns

The PRD addendum positions Hexalith Agents against Slack AI, Microsoft 365 Copilot in Teams, Zoom AI Companion, and Atlassian Rovo Agents.

- Lifted: clear admin control over AI availability and caller access.
- Lifted: answering in place from Conversation context rather than in a separate AI workspace.
- Rejected: generic summarization as the product promise; this is governed participation by a named Party identity.
- Rejected: unapproved generated content in the durable Conversation record.
- Rejected: Provider and model opacity; identity is in audit evidence without secrets.

## Information Architecture

FrontComposer shell navigation registers an **Agents** domain. Every surface below is bound to a route, a nav order, an authorization policy constant from `AgentsFrontComposerRegistration`, one `FcPageLayoutMode`, an owning story in Epics 5 to 8, and a read/write contract. Field inventories live in the Component Patterns rows. The provider surface is the PRD's Global Providers Aggregate, the architecture's `ProviderCatalog`, and this spine's Provider catalog; the three names denote one thing.

| Surface | Route | Nav order | RequiredPolicy | Layout | Owning story | Read/write contract | Purpose |
|---|---|---|---|---|---|---|---|
| Agents overview | `/agents` | 0 | `Agents.Administrator` | FullWidth | 5.2, 5.5, 5.7 | `AgentInspectionResult`; Story 5.5 setup/callability result; pending count via `IBadgeCountService` | See lifecycle, proven callability, pending count, recent failures, and safe blockers at one `RegistryRevision`. |
| `hexa` configuration | `/agents/configuration` | 1 | `Agents.Administrator` | Constrained | 5.2, 5.7 | `AgentSetupResult`; `AgentCommandAcceptance` (FR-29 stages) | Configure `hexa` and activate it once blockers clear. |
| Provider catalog | `/agents/providers` | 2 | Read `Agents.Administrator`; mutation `Agents.PlatformOperator` | FullWidth | 5.3, 5.5 | `ProviderCatalogEntryView`; `ProviderCatalogCommandAcceptance`; `ProviderReadinessResult` (Story 5.5) | Govern Provider/model records, pricing, and readiness without secrets. |
| Approver policy | `/agents/approver-policy` | 3 | `Agents.Administrator` | Constrained | 5.4 | `AgentApproverPolicy`; `ApproverPolicySourceKind` (closed list) | Define who may resolve proposals and how each source is disclosed. |
| Conversation context policy | `/agents/context-policy` | 4 | `Agents.Administrator` | Constrained | 6.2 | Read model defined by Story 6.2 from `AgentInteractionContextPolicy` | Read the effective context rule and Safe Context Budget. |
| Content safety policy | `/agents/content-safety` | 5 | `Agents.Administrator` | Constrained | 6.3, 8.4 | `AgentContentSafetyPolicy` (`BlockedOutputCategories`, `RestrictedOutputCategories`); `ConfigureAgentContentSafetyPolicy`; publish command by Story 8.4 | Validate and publish a versioned safety policy. |
| Cost controls | `/agents/cost-controls` | 6 | `Agents.Administrator` | Constrained | 6.4, 8.4 | Budget policy contracts defined by Story 8.4 (FR-32) | Configure caps and rate limits; inspect consumption and overrides. |
| Proposal queue | `/agents/proposals` | 7 | `Agents.Approver` | FullWidth | 7.1 | `PendingProposalsResult` | Discover proposals the Approver may currently read. |
| Proposal detail/editor | `/agents/proposals/{AgentInteractionId}` | none | `Agents.Approver` | Constrained | 7.2 to 7.6 | `ProposalDetailView`; `ProposalVersionSummary` | Review and resolve one proposal. |
| Operational status | `/agents/status` | 8 | `Agents.Operator` | FullWidth | 6.7, 8.7 | `AgentOperationalStatusSummaryView`; failure record fields by Story 6.1 | Distinguish readiness, blocked calls, failures, and posting outcomes by recovery. |
| Launch readiness | `/agents/launch-readiness` | 9 | `Agents.Operator` | FullWidth | 5.5, 8.7 | `launch-readiness` projection checkpoint (Story 8.7); the built `AgentLaunchReadinessView` is superseded evidence | Inspect every gate record and blocker at one checkpoint. |
| Audit evidence | `/agents/audit`; `/agents/proposals/{AgentInteractionId}/audit` | 10 | `Agents.AuditOperator` | List FullWidth; detail Constrained | 7.x, 8.x | `AuditEvidenceResult`; `AuditAvailabilityStatus` | Inspect support-safe evidence end to end. `/agents/audit` is an id-entry surface, not a grid: it accepts an interaction, proposal, or governance-operation reference and routes to the detail, so it declares no list contract, columns, filters, or sort and the mandatory FC-TBL set does not apply to it. |
| Audit governance | `/agents/audit-governance` | 11 | `Agents.AuditOperator` | Constrained | 8.1 to 8.3 | Legal hold, export, deletion commands and progress projections by Stories 8.1 to 8.3 | Operate retention, legal hold, export, and deletion. |
| Conversation invocation | Conversation-owned; no Agents nav entry | none | Conversation access plus Agent call permission (server) | Conversation surface | 6.7 via `EXT-CONV-UI-1` | `IConversationAgentCallGateway` | Call `hexa` through the sole V1 entry, the Conversation-owned **Call hexa** action. |
| API/client contract reference | Developer docs | none | none | none | 4.1, 5.5 | Public contracts | Omar's journey; not a FrontComposer screen. |

Rules:

- `/agents/conversation-call` is a pre-integration harness, not an alternate entry point. It is delisted from navigation now. **Story 6.7 removes it: the route is unregistered and the page deleted before 6.7 closes**, because `AlternateInvocationGuardTests` must prove absence and a merely blocked route is still an `[Authorize]` surface. While it exists it carries `Agents.Administrator`, is excluded from `LR-UI-CONFORMANCE`, and writes nothing in a production-like `EnvironmentProfile`. The terminal-state `Start a new Agent Call` action navigates to the Source Conversation, never to the harness.
- Policy constants registered in `AgentsFrontComposerRegistration`: `Agents.Administrator`, `Agents.Approver`, `Agents.Operator`, `Agents.AuditOperator` are tenant-scoped, and `Agents.PlatformOperator` is platform-scoped. The Provider catalog is a platform-scoped record, so reading it is tenant-scoped administration but every `ProviderCatalogMutation` requires the platform-scoped constant: a tenant administrator sees the grid with mutation controls absent. Showing a cross-tenant blast-radius count in the confirmation is disclosure control, not authorization, and does not substitute for this split (FR-19).
- Navigation hiding is disclosure, not authorization. Unauthorized entries are hidden. Direct navigation to an unauthorized, foreign-tenant, or non-existent id renders the single `not available` state defined under State Patterns. Every page also carries `[Authorize(Policy = ...)]` and the server decision is authoritative.
- Nav glyphs come only from `FcFluentIcons`. Missing glyphs are FrontComposer requests, never silent reuse; the request list is in `DESIGN.md § Brand & Style`.

Surface closure status: **final**. Every stated need has a surface and every surface has a journey below.

## Conversation Integration Seam

**Call hexa** lives inside a Hexalith.Conversations surface. Conversations is a separate module; the seam is a named external dependency, not an Agents route.

| Element | Binding |
|---|---|
| Conversations-side extension point | `EXT-CONV-UI-1`: a versioned Conversation contribution and registration contract with tenant-scoped authorization and typed registration failure. It carries **two artifact kinds**: an action contribution (the **Call hexa** trigger) and a per-message decoration slot keyed by `MessageId` (the `agent-response-marker`). Status `Uncommitted`, owner `TBD` in `external-dependency-register.md`. |
| Agents-side artifacts | The exported `ConversationAgentCallPanel` component, which is the shipped name of the `conversation-agent-call` component; `IConversationAgentCallGateway`; and an Agents-side provenance accessor keyed by `MessageId` supplying posted-by-`hexa`, generated versus human-edited, and the editing Party. |
| Gateway surface | `RequestCallAsync` and `GetCallStatusAsync` exist today. The seam must add `GetCallabilityAsync(tenant, conversation)` returning the Story 5.5 readiness result and the safe blocker, because the Conversations-owned **Call hexa** button cannot render its `aria-disabled` pre-state without it. |
| Ownership split | Conversations owns the **Call hexa** trigger button and its placement; Agents owns the dialog body, which is `ConversationAgentCallPanel`. |
| Consuming story | 6.7. Blocked from `ready-for-dev` while `EXT-CONV-UI-1` is `Uncommitted` (FR-21). `epics.md` § Story 6.7 and the `external-dependency-register.md` consuming-story field must be corrected to match; the correction is a deferred item with an owner in `reconcile-validation-2026-09-09.md`. |
| Readiness | `FrontComposer Readiness` row below; `LR-UI-CONFORMANCE` covers the contributed action once registered. |

Behavior at the seam:

- **Call hexa** renders visible but `DisabledFocusable` with the safe blocker text whenever `hexa` is not proven callable for the tenant, resolved through `GetCallabilityAsync`. It is absent for Parties without Conversation access or Agent call permission.
- None of the panel's events happen on an Agents route, so the per-route live-region rule cannot reach them. `ConversationAgentCallPanel` is therefore self-contained for announcements and post-submit focus, per the `conversation-agent-call` row, and that self-containment is an `EXT-CONV-UI-1` requirement rather than only a spine rule.
- `hexa` joins a Conversation as a participant at the first accepted Agent Call, before generation, under the platform Agents service principal. The join is idempotent. Membership failure fails the call closed with no Provider work, proposal, or Conversation Message. Membership is visible on the same terms as any other participant.
- Removal of `hexa` is a Conversation-owned action. Agents reacts by blocking further calls in that Conversation and moving every non-terminal proposal to `Abandoned` with reason `Agent removed from Conversation`, versions preserved.
- `agent-response-marker` is contributed through the per-message decoration slot, not Story 6.7's action contribution, which cannot carry per-message attribution. Its rules are in the `agent-response-marker` row.
- Conversation content is untrusted data. No contributed surface presents Conversation content as an instruction, and a recorded redirect attempt is visible in audit as a safety outcome (FR-31).

## Voice and Tone

Microcopy is operational, direct, and truth-aware. Brand posture lives in `DESIGN.md`.

| Do (EN) | Do (FR) | Don't |
|---|---|---|
| `hexa` is active. Callability is not yet proven. | `hexa` est actif. Il n'est pas encore prouvé qu'il peut être appelé. | `hexa` is callable, based on lifecycle alone. |
| `hexa` is callable. | `hexa` peut être appelé. | `hexa` is ready to help! |
| Generation failed. No proposal or Conversation Message was created. | La génération a échoué. Aucune proposition ni aucun message de conversation n'a été créé. | Something went wrong. |
| Proposal pending. | Proposition en attente. | Reply sent. |
| Approved. Posting is pending. | Approuvé. La publication est en attente. | Approved successfully, as a completed state. |
| Approved version posted to the Conversation. | La version approuvée a été publiée dans la conversation. | Posted, before the projection confirms the message. |
| Posting failed. Retry posting or start a new Agent Call. | La publication a échoué. Réessayez la publication ou lancez un nouvel appel de l'agent. | Posted, or a silent retry. |
| You do not have permission to approve this proposal. | Vous n'êtes pas autorisé à approuver cette proposition. | Forbidden 403 |
| You cannot approve a version you last edited. Another Approver must approve it. | Vous ne pouvez pas approuver une version que vous avez modifiée en dernier. Un autre approbateur doit l'approuver. | Approval blocked. |
| Provider is disabled. Calls are blocked until reconfigured. | Le fournisseur est désactivé. Les appels sont bloqués jusqu'à sa reconfiguration. | Provider unavailable, with no recovery path. |
| This proposal expired at {expiresAt}. Start a new Agent Call. | Cette proposition a expiré le {expiresAt}. Lancez un nouvel appel de l'agent. | Try approving again, after expiry. |
| Prior generated versions are preserved. | Les versions générées précédemment sont conservées. | Old draft replaced. |
| This proposal was resolved by another Approver. | Cette proposition a été traitée par un autre approbateur. | A generic error. |
| Approval authority cannot be resolved because {dependency} is unavailable. | L'autorité d'approbation ne peut pas être déterminée car {dependency} est indisponible. | Approve enabled until the server rejects. |
| Status refreshed. Approval is still pending. | Statut actualisé. L'approbation est toujours en attente. | A silent reload. |
| {count} proposals pending approval. | {count} propositions en attente d'approbation. | 3 |
| Applies to attempts prepared after confirmation. | S'applique aux tentatives préparées après confirmation. | Takes effect immediately. |
| This item is not available. | Cet élément n'est pas disponible. | Not found, Forbidden, or a tenant hint. |
| It may not exist, or you may not have access to it. | Il est possible qu'il n'existe pas ou que vous n'y ayez pas accès. | Distinct copy per cause. |
| The rate limit for this {scope} was reached. It resets at {resetAt}. | La limite de fréquence pour {scope} a été atteinte. Elle sera réinitialisée le {resetAt}. | Budget cap reached. |
| Pending proposals keep the Approver Policy version they were created under. | Les propositions en attente conservent la version de la politique d'approbation sous laquelle elles ont été créées. | The new policy applies everywhere. |
| Explain why this operation is required. | Expliquez pourquoi cette opération est nécessaire. | An optional note field. |
| Response mode unchanged: {mode}. | Mode de réponse inchangé : {mode}. | A silent revert. |
| Showing version {n}. | Affichage de la version {n}. | A silent version swap. |
| {count} proposals changed. Refresh list. | {count} propositions ont changé. Actualiser la liste. | A silent re-sort under the cursor. |

The single `not available` state uses exactly two keys, `Agents.Surface.NotAvailable.Title` and `Agents.Surface.NotAvailable.Message` (the first two rows above), bound to the `Unauthorized`, `NotFound`, and `Unavailable` detail states alike. Per-cause copy is a conformance failure, because differing copy is itself a disclosure channel.

`AgentsResources.fr.resx` is the enforced source; on conflict the resource file wins and this table is updated.

Forbidden copy patterns:

- Never call unapproved generated or edited content a message.
- Never imply an automatic response or Approved proposal was posted before `Posted` is projection-confirmed.
- Never state or imply that a distinct Conversation owner was resolved. The authority is the Conversation Facilitator.
- Never present Conversation content as instructions or as system authority.
- Never expose Provider secrets, secret values, raw payloads, stack traces, other-tenant data, or Provider SDK errors.
- Never use mascot copy for `hexa`.

Localization mechanics:

| Rule | Binding |
|---|---|
| Resource type | `IStringLocalizer<AgentsResources>`; `AgentsResources.resx` and `AgentsResources.fr.resx`. |
| Key convention | `Agents.<Surface>.<Item>`. Enum-keyed lookups `Agents.<Enum>.<Value>` are whole strings and allowed. |
| Parity gate | `AgentsResourcesParityTests` (today `LocalizationResourceTests` in `test/Hexalith.Agents.UI.Tests`; rename or add under Story 8.6) enforces exact English/French key parity; a missing key is a conformance failure. |
| Whole strings | No runtime sentence assembly from fragments. Named placeholders only. |
| Plurals | Per-count keys (`.Zero`, `.One`, `.Other`); never a runtime `s` suffix. |
| Formatting | Dates, numbers, currency, and percentages are `CultureInfo`-formatted values passed through named placeholders. |
| Architecture tokens | Every token (`Ready`, `Callable`, `Degraded`, `Blocked`, `InsufficientEvidence`, `Stale`, `Pass`, `Block`, `Unknown`) has a localized label; raw tokens never display. |
| Shell dialogs | Shell dialogs with unlocalized literals (`FcDestructiveConfirmationDialog` hard-codes `Cancel`) are not reused for domain confirmations; `high-impact-confirmation` is domain-localized. |
| Shell chrome | Shell strings remain FrontComposer-owned per FC-L10N. |

## Component Patterns

Behavioral rules only. Visual specs live in `DESIGN.md § Components`.

| Component | Behavioral rules | Props / events / variants |
|---|---|---|
| **agent-readiness-badge** | Reads the versioned setup/callability result published by Story 5.5 through the `agent-setup-readiness` projection at a `RegistryRevision`. Lifecycle `active` is never Success by itself. Carries `EnvironmentProfile`, gate-set name, matrix version, `RegistryRevision`. Re-evaluates `ValidUntil` on every poll and window focus; lapse renders `stale`. The built `AgentReadiness.MapState` (`Callable = Active && no blockers`) is superseded and replaced. | States: the nine states in § Agent readiness, which also owns the production-enablement rule. |
| **provider-status-badge** | Consumes `ProviderReadinessResult` (`OperationalState / Callability / ReasonCode`, Story 5.5) without inference. Interim rule before Story 5.5: render `Unknown` with `{colors.status-subtle}`, never Success, when no `Callability` field exists. State treatment is in § Provider and model. | Inputs: the readiness triple, `CapabilityVersion`, `ValidUntil`. |
| **proposal-state-badge** | Renders the ten `ProposedAgentReplyState` values and the display-only outcomes per § Proposal lifecycle. Nearing expiry is computed per § Proposal lifecycle. | Inputs: state, `ExpiresAt`, configured window, server read time, outcome. |
| **response-mode-toggle** | `FluentRadioGroup` editing a **draft** value only, because radio selection moves on arrow keys. A change never opens a confirmation; a separate Apply button, `DisabledFocusable` until the draft differs from the published mode, does. Cancel reverts the draft and the status node announces `Response mode unchanged: {mode}`. This rule governs every "change opens a confirmation" binding in this spine: the confirmation is always opened by an explicit command control, never by a selection event. Changes apply to future Agent Calls only; the confirmation states that pending proposals remain proposals when switching to Automatic Response Mode, and that any permitted restricted safety category is blocked for the whole time Automatic Response Mode is active (FR-26), so the operator sees the consequence before the setting takes effect. | Events: Apply opens `high-impact-confirmation` (`AgentSetupMutation`). |
| **agent-config-form** | Constrained form inside `FcAggregateDetailPage` with a domain-supplied `FcPageHeader`. `hexa` is pre-provisioned once per tenant, so there is no not-yet-created state; the form always edits an existing Agent. Validates required fields before activation. Activation blockers are inline and actionable and include the six-part Provider eligibility set (enabled, configured, text generation, valid limits, valid pricing, non-regressed `CapabilityVersion`), unconfigured cost cap, and unconfigured rate limit. The Agent Party identity is a read-only status with its own blocker (`HasPartyIdentity` / `MissingPartyIdentity`); Agents issues no command to create or link it, and an unresolved identity is a platform provisioning task. Proposal expiry duration: default 24 hours, range 1 hour to 30 days, future proposals only. Regeneration ceiling per proposal: default 3, range 1 to 10, future proposals only, flagged for Product sign-off because FR-32 states only "a documented default and a valid range". **Enable** and **Disable** are both `AgentActivation` and sit in the same action rail; only the action that would change the current lifecycle state renders. The Disable confirmation names the Agent, states that in-flight calls complete and new calls are rejected before Provider invocation, and states that pending proposals stay resolvable. Shows what changed where safe to expose. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. Activation and deactivation are `AgentActivation`; other writes are `AgentSetupMutation`. |
| **approver-policy-builder** | Builds authority from `ApproverPolicySourceKind`: `ConversationOwner` is labeled Conversation Facilitator (resolved from `ParticipantRole.Facilitator`), `Caller`, `PredefinedParty`, `TenantRole`. Each row declares a disclosure category; the default is operator-only; a source revealing restricted-role membership cannot be user-visible. Validation rejects a source that cannot satisfy Conversation access at call time and a configuration that cannot satisfy segregation of duties. Ambiguous or unavailable sources fail closed and block Confirmation Response Mode activation. Row order is edited with buttons or a menu, never drag-only. | Row accessible name includes source kind and basis. Publish is `PolicyPublication`; because AD-12 snapshots the policy per interaction, publication is future-only and the confirmation states that pending proposals keep the policy version they were created under. The surface shows the count of pending proposals under each prior version, linking to the queue filtered by that policy version, so an admin revoking an approver can see who still holds authority. |
| **provider-catalog-grid** | Platform-scoped record: `Agents.Administrator` reads it, every mutation requires the platform-scoped `Agents.PlatformOperator`, and mutation controls are absent for an administrator without it. Columns include enablement, configured state, capability limits, pricing version (pinned, server-assigned, read-only), currency, unit prices, `CapabilityVersion`, readiness triple, freshness. The pricing editor has no default values. One platform-scoped model serves many tenants with many budget currencies, so the catalog validates only ISO 4217 well-formedness. The **tenant budget currency on Cost controls is authoritative**. A per-tenant mismatch renders `{colors.status-important}` on that tenant's readiness view and blocks reservations there, never on the shared catalog row. Before Story 8.4 ships the budget policy read model there is no comparison value, so currency renders as entered with no mismatch state. A zero unit price must be typed explicitly, and the confirmation carries a warning line that a zero-priced model reserves nothing. Pricing edits, enable, and disable are `ProviderCatalogMutation`. The disable confirmation lists in-scope Agents whose callability will be blocked, supplied by the Story 5.5 readiness result queried by Provider and model; it renders cross-tenant impact as an aggregate count or omits it per disclosure category, never as tenant names, and it names the platform scope of the action. `CapabilityVersion` is the concurrency token; outdated or regressed submissions render `superseded by another decision`. The UI never accepts a secret value. `ConfigurationReferenceId` is a reference only (`EXT-SECRETS-1`), operator-only visible, and masked with autocomplete off when editable. It is never echoed in validation or errors and never appears in URLs, clipboard actions, or diagnostics. | Per § Grid rules. Default sort: Provider name, then model. At 320 CSS px the surviving columns are Provider name, model, readiness, and the open action. |
| **proposal-queue-grid** | Lists only proposals whose Source Conversation the Approver can currently read. **`needs my action`** means either of two things: a proposal in `Pending`, `Edited`, or `Regenerated` where the viewer is an authorized Approver who did not last edit the selected version; or a proposal in `PostingFailed` with retries remaining. A proposal another Approver has already moved to `Approved`, `Posted`, or a terminal state is neither counted nor listed under it. `FcStatusFilterChips` cannot express it: its `AvailableSlots` is typed as the closed `BadgeSlot` enum and its visible chip text is `slot.ToString()`, an unlocalized English enum name. Until a FrontComposer request for domain-keyed slots with localized labels lands, state and `needs my action` filters render as Agents-owned `aria-pressed` toggle buttons inside a named `role="group"`, and `FcStatusFilterChips` is not mandatory on this grid. Filters: state, Agent, Source Conversation, caller, expiry. Empty and filtered-empty are distinct. Expiry renders as an absolute culture-formatted timestamp plus a static relative label refreshed at most once per minute, outside any live region. Polled re-renders never steal focus: `ItemKey` is bound, and while focus is inside the grid body a re-sort or page shift is deferred while the status node announces `{count} proposals changed. Refresh list.` with an explicit refresh action. | Per § Grid rules. At 320 CSS px the surviving columns are state, expiry, and the open action. Default sort: nearest `ExpiresAt` first among `needs my action`, then most recent. Page size 25. Age is computed from `CreatedAt` server time. |
| **proposal-editor** | Exists only after successful generation. Authorized Approvers edit, regenerate, approve a selected version, reject, or abandon. Editing creates a new preserved version with editor Party and timestamp; an edited version is never presented as generated. Approval is `DisabledFocusable` while the editor is dirty. Approval is blocked when the Approver last edited the selected version or is the sole Approver of their own call; the server rejects either way. The disabled pre-state needs the comparison client-side, so Story 7.4 supplies a per-version `CanCurrentUserApproveSelectedVersion` flag with its reason; the UI never compares raw `EditorPartyId` or `CallerPartyId` against a current-Party accessor. Regenerate is `aria-disabled` at the regeneration ceiling with the reason, and copy states that a regeneration is a chargeable call under cost caps and rate limits. On regeneration failure the editor keeps the proposal non-terminal with prior versions intact, records a linked failure record, announces the failure in the status region, and keeps approve, reject, and abandon available. Approval re-runs the then-current Content Safety Policy on the exact selected version. Which actions are available in which state is in § Proposal editor action rail. An Approver who lost Conversation access sees existence and state only. | Exit is an explicit button with unsaved-changes confirmation. No client-side length limit. Regenerate while any resolution is pending is `DisabledFocusable`; regeneration is chargeable, so while one is in flight Regenerate is also `DisabledFocusable` with a local pending badge, guarding against a double Enter, without joining the nine confirmation families. Resolution actions open `high-impact-confirmation` (`ProposalResolution`). |
| **version-history** | Lists every generated, edited, and regenerated version with kind, author or Provider/model, timestamp, safety outcome, approval and posting markers. Selection is a radio group labeled by kind, author, and timestamp. V1 has no diff view: one version is viewed at a time. Because selection is a radio group whose value moves on arrow keys, version radios are `DisabledFocusable` with the reason while the editor is dirty; the reader saves or discards first. On a selection change the textarea's accessible name becomes the version identity and the status node announces `Showing version {n}.` Approval decisions show the policy basis rendered per its FR-7 disclosure category; redacted and omitted show a category label, never the source. Failure records are not versions. | Inputs: `ProposalVersionSummary` list, `ApprovedVersionId`, `SelectedVersionId`. |
| **conversation-agent-call** | Shipped as the exported `ConversationAgentCallPanel`; the two names denote one artifact. Conversations owns the **Call hexa** trigger, Agents owns the dialog body. The action opens a `FluentDialog` naming `hexa` and the effective response mode. Prompt required; no client-side length limit because the Safe Context Budget is server-owned. Captures Source Conversation, caller, Agent, prompt, effective response mode, authorization decision, timestamp. Visible but `DisabledFocusable` with the safe blocker when `hexa` is not callable, resolved through the seam's `GetCallabilityAsync`. The panel owns one polite `role="status"` node and one `role="alert"` node under the same politeness table as an Agents route, placed after Submit in DOM order. On Submit the dialog closes, focus returns to **Call hexa**, and that button is `DisabledFocusable` while the call is pending; `submitted`, `authoritative pending`, `denied`, `capacity queued`, `generation failed`, `PostingPending`, and `Posted` each announce through those nodes. Because the prompt textarea holds typed content, `Esc` confirms before discarding. | States: `submitted`, `authoritative pending`, then the states in § Agent call. Duplicate submission is blocked per session, resource, and family. |
| **agent-response-marker** | Contributed through the `EXT-CONV-UI-1` per-message decoration slot for every posted Agent Response, with provenance from the Agents-side accessor keyed by `MessageId`; Story 6.7's action contribution cannot carry per-message attribution. States AI-generated from Conversation Context and not human-verified (FR-11); adds human-edited provenance and editing Party where disclosure allows (FR-17). Never appears on unapproved content. | Variants: generated; human-edited. |
| **conversation-context-policy-panel** | Read-only. Shows policy version, Safe Context Budget components (model input limit at the applicable `CapabilityVersion`, reserved output allowance, Agent Instructions size, configured safety margin), tokenizer or named approximation, declared Approved Bounded Context Behaviors (none in V1), and the safe reason an oversized call fails closed. Offers no truncation, summarization, windowing, or retrieval control. | Inputs: Story 6.2 read model. |
| **content-safety-policy-editor** | Shows the fixed always-blocked categories from FR-26 and OQ-9 as a read-only list: child sexual abuse or exploitation; credible threats or instructions for imminent serious harm; encouragement or instruction for suicide or self-harm; credential theft, malware, or unauthorized compromise; secrets and private credentials; cross-tenant or unauthorized personal or Conversation data; control-bypass attempts; impersonation of a named Party or assertion of a decision on a Party's behalf. Restricted categories (hate, harassment, sexual, violent, illegal activity, sensitive personal) are editable only with an explicitly permitted tenant use case and Confirmation Response Mode. Each restricted row shows the effective response mode beside it, so an operator can see that permitting a category under Automatic Response Mode has no effect. Validates a draft version, then publishes a future-only version through `high-impact-confirmation` (`PolicyPublication`). Copy states that approval-time and pre-post re-checks always use the then-current policy and that a re-check can only tighten. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. |
| **cost-control-editor** | Authors per-tenant monthly and per-call caps (`FluentNumberInput` with units), per-Party and per-Conversation rate limits over a stated rolling window, with no implicit defaults; an unconfigured value is a readiness blocker. The **tenant budget currency set here is authoritative** and the catalog validates only ISO 4217 well-formedness against it. Changing the budget currency after settled spend exists is a `TenantBudgetUpdate` whose confirmation states that settled spend is not converted. Shows consumption, reserved versus settled spend, and held `Unknown` reservations. The 80% warning and the 100% fail-closed state are evaluated on settled usage plus outstanding reservations, and both figures are labeled. The audited override (actor, justification, scope, expiry) is `TenantBudgetUpdate` and is bounded: it carries a numeric ceiling and an expiry, both rendered in its confirmation, and while it is active Operational status shows it with remaining amount and remaining time. Changes apply to future calls only. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. Consumption evidence lists live on Operational status. |
| **launch-readiness-panel** | Reads one `launch-readiness` projection checkpoint (Story 8.7). Renders `Pass`, `Block`, `InsufficientEvidence`, `Stale` per GateId with Owner, `SourceVersion`, `ObservedAt`, exclusive `ValidUntil`, `RequiredEvidenceLevel`, `EvidenceReference`, `BlockerCode`, `RegistryRevision`, `EnvironmentProfile`, and consumed dependency status. Blockers precede passing records. The single UX-owned NFR-14 statement: the panel shows the three sample kinds, their ticks, sample counts against the minimum, and whether the localized live-region mutation was observed; thresholds and seams are per `launch-readiness-register.md § NFR-14 Browser Monotonic Timing Contract`. Never fills a missing record, never falls back to an older `Pass`, never issues the `RQ-1` decision. | Inputs: Story 8.7 checkpoint. |
| **audit-governance-panel** | Callable operations (FR-30): apply and release legal hold, request export, request deletion, each through `high-impact-confirmation` (`LegalHold`, `ExportRequest`, `DeletionRequest`). Per FR-30, each of these plus `PolicyPublication` and `TenantBudgetUpdate` requires a justification typed into the confirmation before Confirm becomes available; the justification is part of the audit record, and an empty or whitespace value is rejected client-side and server-side. Confirmation contents are in § Confirmation contents by family. Story 8.3 distinguishes request, confirm, and cancel, so deletion is a request, not a single click. Deletion is `DisabledFocusable` while a matching legal hold is active or while `EXT-SECRETS-1` is not `Available`; releasing a hold and requesting deletion are never the same gesture, and a hold release names the records it exposes. Export is likewise `DisabledFocusable` with `authority unresolved` treatment while `EXT-SECRETS-1` is not `Available`; export rows show manifest state, artifact expiry, and download availability, and the panel never renders artifact contents or keys. Whether deletion additionally requires a second confirm stage by a different Party is deferred to Product and Security; until that decision lands, single-actor deletion with mandatory justification, scope rendering, and hold interlock is the specified behavior. Shows the 365-day rule, hold state, export manifest state, deletion progress including projection purge and tombstone confirmation. Success only after authoritative evidence; partial failure stays restrictive and actionable. | Progress projections by Stories 8.1 to 8.3. Evidence lists live on Audit evidence. |
| **proposal-notification** | In-product only. `IBadgeCountService` is **not** used in V1: it is `Type`-keyed over projection runtime types and shell-rendered as an unlabeled `FluentBadge`, and Agents runs no generated projection lane. The count is domain-rendered inside the Agents overview link as the whole string `{count} proposals pending approval`, and the shell nav count stays suppressed rather than emitting a bare number. A localizable label slot on the shell badge is a FrontComposer request and a deferred item. Counts only proposals matching `needs my action` for the viewer. No content, never implies posting. | Variants: zero (no badge), count. |
| **operational-status-panel** | Groups readiness and runtime outcomes by recovery: configure Provider, fix policy, wait for approval, retry posting, inspect audit, start a new call. Per-item state uses `FluentBadge`; `FluentMessageBar` is page-level notice only. Shows per-tenant counts of calls blocked by context policy, safety, cost cap, rate limit, and capacity. Shows per-Conversation `safety blocked` history, so a permanently blocked Conversation is visible; because that is Conversation-derived information and `Agents.Operator` may not be a participant, the row renders an opaque Conversation reference and a count by default, and the Conversation name or link renders only for a viewer with current read access, otherwise `existence only`. Shows any active cost override with remaining amount and remaining time. Shows cost-cap consumption and failure records. A deferred metric renders `{colors.status-subtle}` with a whole-string label, never an empty cell. | Failure record fields per § Generation failure record. Story 6.2 owns the read model and route for the context-policy panel, and the per-tenant blocked counts, per-Conversation safety history, cost consumption, and projection id/version on this panel have no contract yet. Both the read-model ownership and these missing contracts are deferred items for the epics skill, so ownership lands as a story acceptance criterion rather than a spine assertion. |
| **audit-evidence-panel** | Support-safe evidence: caller, Agent, Source Conversation, Provider/model, response mode, computed Safe Context Budget, measured context size, `CapabilityVersion`, context mode, safety decisions at each of the four points, versions, editing Party per edited version, approving Party and policy basis per disclosure category, projection references, timestamps, posting outcome, final Conversation Message. Evidence containing Conversation-derived content requires current Conversation read access and fails closed otherwise. Named authority is the Conversation Facilitator where that source applied. A second evidence class, **configuration and governance change evidence**, satisfies FR-24 and FR-32 and is inspectable both at `/agents/audit` and in the History item of each write surface: actor, operation family, resource identity, old-to-new values where safe to expose (including prior and new cap values), published version, concurrency token, projection id and version, justification, and the acceptance stages the command passed. An override additionally shows its scope, ceiling, and expiry. | Per-item state uses `FluentBadge`. |
| **high-impact-confirmation** | Focus-trapped `FluentDialog` for the nine high-risk families, opened only by an explicit command control, never by a selection event. Body contents are in § Confirmation contents by family. Cancel carries `AutoFocus` and the domain restores focus on close, because Fluent v5 documents modal inertness and `PreventDismissOnEscape` but neither behavior; custom action buttons disable Fluent's default Enter and Escape handling, so `Esc` is wired to cancel explicitly and Enter on the body never commits. The destructive action is never the default button. The body is a `FluentDialogBody` with `FixedHeaderFooter="true"` and a focusable, accessibly named scroll region, so Confirm and Cancel stay reachable at 320 CSS px even when the body renders a full proposal version. **There is no type-to-confirm**, for deletion or anything else. The `DeletionRequest` confirmation instead carries scope, counts, hold state, and a required justification (WCAG 3.3.7). | Events: confirm, cancel. Returns focus to the trigger or, if the trigger is gone, per the deterministic focus rule in § High-risk pending commands. |
| **pending-command-indicator** | Rendered on the activated control and the item's status cell while a high-risk command is pending; the control binds `FluentButton.DisabledFocusable="true"`, which is the v5 binding that emits `aria-disabled` while keeping focus. `Disabled` and `Loading` are forbidden on an activated control: both remove focusability and would reintroduce the focus-loss defect this pattern exists to prevent. Sequence, lock scope, session, and timeout rules are in § High-risk pending commands and § Projection catch-up contract. Never Success. | Variants: same session; `pending in another session`. |

### Grid rules

These govern both Agents grids; each component row states only its own delta.

| Rule | Binding |
|---|---|
| Wrapper | Hand-authored `FluentDataGrid` inside `FcAggregateListPage`. The generated `[Projection]` lane is not used. |
| Mandatory components | `FcFilterEmptyState`, `FcFilterResetButton`, `FcExpandInRowDetail`, and `FcStatusFilterChips` where its label contract allows. |
| `ViewKey` | `agents.provider-catalog` and `agents.pending-proposals`. Each mandatory component takes one, view-key mismatches fail closed, and Agents inherits none from a generated lane. |
| `ItemKey` | The row's `AgentInteractionId` or catalog entry id, so a polled re-render cannot move the focused row. |
| Status cells | Visible text plus icon, never `FcStatusIcon` alone. |
| States | Loading, empty, filtered-empty, error, permission-denied, and `catching up` are distinguished; empty never leaks unauthorized records. |
| 320 CSS px | The grid scrolls inside its own container under the WCAG 1.4.10 data-table exception and never scrolls the page. Surviving columns per grid are in each component row; the rest move into `FcExpandInRowDetail`. |

### Proposal editor action rail

The rail holds Edit, Regenerate, Approve, Reject, Abandon, and Retry posting. Availability is a function of state, not of policy alone.

| Proposal state | Available actions |
|---|---|
| `Pending`, `Edited`, `Regenerated` | Edit, Regenerate, Approve, Reject, Abandon. Approve is `DisabledFocusable` while the editor is dirty, or when `CanCurrentUserApproveSelectedVersion` is false. Regenerate is `DisabledFocusable` at the ceiling, while a generation is in flight, or while any resolution is pending. |
| `Approved`, `PostingPending` | Retry posting is not yet applicable; abandon where permitted; audit. Edit, Regenerate, and Approve are **absent** once `Approved` is projection-confirmed, and the editor pins `ApprovedVersionId` read-only, so no other version can be approved or posted and no edit can land while posting is pending (FR-17, FR-18). |
| `PostingFailed`, retries remaining | Retry posting; abandon where permitted; audit. |
| `PostingFailed`, retries exhausted | `Start a new Agent Call` only. |
| `Posted`, `Rejected`, `Abandoned`, `Expired` | Read-only. `Start a new Agent Call` only. |

`Start a new Agent Call` navigates to the Source Conversation. **`retry posting`** is a Conversation write and is governed as one. It sits in this rail for an `Agents.Approver` who holds current Conversation read access, under family `ProposalResolution`. Each attempt is a row in version history and in audit, and the maximum number of attempts is a `hexa` configuration field. A new decision, after any exhausted or terminal outcome, requires a new Agent Call.

### Confirmation contents by family

Every `high-impact-confirmation` renders the action, resource identity, future-only effect, and authorization basis. Each family adds the following, and the rendered set is what the audit record carries.

| Family | Additional required contents |
|---|---|
| `ProposalResolution` — approve | Selected `VersionId`, version kind, author, timestamp, full content, and the approval-time safety re-check statement. |
| `ProposalResolution` — retry posting | `ApprovedVersionId`, the typed failure reason, attempts used of the configured maximum, and the pre-post re-check statement. |
| `ProposalResolution` — reject, abandon | Selected `VersionId` and the statement that every version is preserved. |
| `ProviderCatalogMutation` — pricing | Old-to-new values per field, the server-assigned `next pricing version {n}`, the zero-price warning where a price is zero, and the platform scope. |
| `ProviderCatalogMutation` — enable, disable | In-scope Agents whose callability changes, the cross-tenant aggregate count per disclosure category, and the platform scope. |
| `PolicyPublication` | The draft's validation result, the version being published, and the statement that pending proposals keep the policy version they were created under. |
| `TenantBudgetUpdate` — cap change | Prior and new cap values with currency and units (FR-32), and the effect on outstanding reservations. |
| `TenantBudgetUpdate` — override | Numeric ceiling, expiry, scope, and required justification. |
| `AgentActivation` | The evaluated gate set with each gate's outcome, the `RegistryRevision` it was evaluated at, and for a disable the statement that in-flight calls complete while new calls are rejected. |
| `AgentSetupMutation` | The changed fields, old-to-new, where safe to expose, and for a response-mode change the pending-proposal statement plus the restricted-category consequence. |
| `LegalHold` | The records entering or leaving hold as a scope and count, required justification, and for a release the statement of what becomes deletable. |
| `ExportRequest` | Export scope, manifest state, artifact expiry, required justification, and the statement that no plaintext secret is included. |
| `DeletionRequest` | Exact scope (tenant, resource class, date range, record counts), hold state of everything in scope, the confirming projections, required justification, and one irreversibility line. |

## State Patterns

Canonical states are used consistently. PascalCase names are contract tokens; lowercase names are UX states that have no contract token yet.

Contract tokens this spine needs that do not exist yet are listed in § Known gaps.

```text
Submitted -> AuthoritativePending -> ProjectionConfirmed
Approved -> PostingPending -> Posted
Approved -> PostingPending -> PostingFailed -> (retry posting) -> PostingPending
generation failed -> separate failure record; no proposal
```

`AuthoritativePending` exists only after the UI receives and renders a server/EventStore-accepted pending identity plus projection/version reference; it is a durable accepted outcome and is never re-submitted. `ProjectionConfirmed` exists only after the authoritative projection/version is received and rendered. Every read exposes the projection id, version, and freshness it was served at. Optimistic client state, a timeout, a SignalR nudge, or an unrelated projection change proves neither stage.

### Projection catch-up contract

| Item | Rule |
|---|---|
| Poll | Every 250 ms for up to 8 s after an accepted write, on every write surface. |
| Nudge | `IProjectionChangeDetailNotifier.ProjectionChangedDetail`, filtered to the authoritative projection ids the surface reads, triggers an immediate poll and never replaces polling. There is no Agents-owned hub: the notifier is the seam, and pinning the transport in the Architecture Spine is a deferred item. |
| Exhaustion | Keep the pending state, render `awaiting projection` with a refresh action; never Success, never `Stale`. |
| Read freshness | A read whose projection version is below the last accepted write's expected version renders `catching up`. |
| `Stale` | Reserved for readiness or Provider evidence whose `ValidUntil` has passed. |
| Standard | The Agents truth-flow helper (`AgentCommandAcceptance`, `ProviderCatalogCommandAcceptance` polling); `FcPendingCommandSummary` is out for V1. |

### Agent readiness

| State | Meaning | Treatment |
|---|---|---|
| `callable` | The current Story 5.5 readiness result at the shown `RegistryRevision` proves callability within `ValidUntil` | Allow calls; `{colors.status-success}`. |
| `active, not proven callable` | Lifecycle active but a gate is missing, blocked, or `InsufficientEvidence` | Keep lifecycle visible; block calls; show the safe blocker; `{colors.status-severe}`. |
| `checking` | Readiness being evaluated | `{colors.status-informative}`; calls not confirmed. |
| `stale` | `ValidUntil` passed with no newer observation, detected on poll or focus | `Stale, re-observation required`; `{colors.status-severe}`; never the prior Success. |
| `invalid configuration` | Required fields, Instructions, mode, policy, expiry, ceiling, cap, or rate limit missing | Block activation and calls with inline blockers. |
| `missing party identity` | Party identity missing, disabled, ambiguous, or unauthorized | Block posting and calls. |
| `provider unavailable` | Provider/model missing, disabled, failed, or not eligible | Block Provider invocation. |
| `disabled` | Lifecycle disabled | Calls rejected before Provider invocation. |
| `authority unresolved` | Tenants or Conversations dependency unavailable so policy basis cannot be resolved | `{colors.status-important}`; resolution controls `aria-disabled`; copy names the dependency safely. |

Production enablement is a separate indicator that is Success only when `RQ-1` records READY for the shown `EnvironmentProfile`.

### Provider and model

| State | Meaning | Treatment |
|---|---|---|
| `Ready / Callable / None` | Every hard gate passes | Eligible; `{colors.status-success}`. |
| `Degraded / Callable / NonBlockingOperationalWarning` | Passes with the single defined non-blocking warning | `{colors.status-warning}`; calls available only because `Callability == Callable`. |
| `Blocked / Blocked / <ReasonCode>` | Missing, past `ValidUntil`, disabled, unconfigured, unpriced, invalid-limit, currency mismatch, unavailable, failed, regressed, unknown, or indeterminate | Block selection or invocation with the safe reason; never relabel as Degraded. |
| `Unknown` (interim) | No `Callability` field present before Story 5.5 | `{colors.status-subtle}`; never Success. |

### Agent call

| State | Contract | Treatment |
|---|---|---|
| `submitted` | Local only | Progress without Success; no server acceptance claimed. |
| `authoritative pending` | `Requested`, or any accepted identity plus projection reference | Follow the authoritative status projection. The shipped `Requested` value renders here. |
| `authorized` | `Authorized` | Continue to membership and context build. |
| `denied` | `Denied` | Stop before Provider invocation; safe reason; `{colors.status-danger}`. |
| `context loading` | `ContextLoading` | Progress; no Provider yet. |
| `context blocked` | `ContextBlocked` | Fail closed before Provider invocation; copy names the Safe Context Budget; no proposal or message. |
| `safety blocked` | `SafetyBlocked` (to add) | Pre-Provider or pre-side-effect block; `{colors.status-severe}`; audit records the safety outcome. |
| `budget blocked` | `BudgetBlocked` (to add) | Cap reached or missing pricing/budget state; `{colors.status-severe}`; recovery via Cost controls. |
| `rate limited` | `RateLimited` (to add) | A per-Party or per-Conversation rate limit refused the call (FR-32). The safe reason names the scope and the window and when it resets; `{colors.status-severe}`; recovery is to wait for the window, never a cap change. Counted separately from `budget blocked` on Operational status. |
| `capacity queued` | `CapacityQueued` (to add) | Progress with queue position where safe; `{colors.status-informative}`. |
| `capacity rejected` | `CapacityRejected` (to add) | `{colors.status-severe}`; recovery is a new call. |
| `generating` | `Generating` | In flight; no message or proposal. |
| `generation failed` | `GenerationFailed` | No proposal, no message; separate failure record. |
| `generated` | `Generated` | In Automatic Response Mode the call proceeds to posting; in Confirmation Response Mode a `Pending` proposal is created. |
| `unknown outcome` | `UnknownOutcome` (to add) | Typed terminal `Unknown` (FR-12); never retried; reservation held then conservatively settled; `{colors.status-important}`. |
| `posting pending` | `PostingPending` (to add) | Automatic Response Mode only; the generated response is being posted. Progress, never Success, and not yet a Conversation Message. |
| `posted` | `Posted` (to add) | `{colors.status-success}`; the status carries the posted `MessageId`. The only proof of a Conversation Message on the automatic path. |
| `posting failed` | `PostingFailed` (to add) | `{colors.status-danger}`; typed reason; the same bounded audited retry rule as the proposal path, reusing the deterministic `MessageId`; exhaustion leaves a new call as the sole recovery. |
| `projection-confirmed terminal` | Authoritative terminal projection | Render the exact outcome; only `Posted` proves a Conversation Message. |

### Proposal lifecycle

Aligned to `ProposedAgentReplyState`. The PRD names the failed-post state `PostFailed`; the contract name `PostingFailed` is used here. The initial generated version is the first `Pending` version, not a separate state. FR-18 names seven states and the contract enum carries ten: `Edited`, `Regenerated`, and `PostingPending` are contract values the PRD folds into their neighbors, and the spine follows the enum because those three are separately renderable and separately audited. Aligning the PRD text to the contract is a deferred item with Product.

| State | Terminal | Treatment |
|---|---|---|
| `Pending` | No | Queue-visible; notification-eligible; `{colors.status-informative}`. |
| `Edited` | No | New preserved version with editor Party and timestamp; prior versions visible. |
| `Regenerated` | No | New generated version under fresh gates; counts toward the ceiling; prior versions visible. |
| `Approved` | No | Progress; a version is selected; posting not implied. Non-terminal for posting only: once projection-confirmed, edit, regenerate, and approve are absent and `ApprovedVersionId` is pinned read-only, so no other version can be approved or posted and no edit can land while posting is pending. |
| `PostingPending` | No | Progress; not yet a Conversation Message. |
| `Posted` | Yes | `{colors.status-success}`; link evidence to the posted message. |
| `PostingFailed` | No | `{colors.status-danger}`; typed reason; bounded audited `retry posting` reusing the deterministic `MessageId`; once retry is exhausted the sole action is a new Agent Call. |
| `Rejected` | Yes | Versions and evidence preserved. |
| `Abandoned` | Yes | Versions preserved; reason shown (Approver, policy, Agent removed from Conversation). |
| `Expired` | Yes | Rendered whenever `ExpiresAt` has passed on any read, regardless of timer delivery; sole action is a new Agent Call. The comparison uses the server time carried on the read, never the browser clock, so a skewed client neither expires a live proposal nor leaves Approve enabled past `ExpiresAt`. |

**Nearing expiry** is a rendering flag, not a state: it begins at 10% of the configured expiry window or 1 hour before `ExpiresAt`, whichever is smaller, floored at 15 minutes so a short window still leaves an actionable warning. It is computed from `ExpiresAt`, the configured window, and the server read time, adds a `{colors.status-warning}` chip to `proposal-state-badge`, and announces once per proposal per § Accessibility Floor.

Display-only outcomes (not persisted):

| Outcome | Trigger | Treatment |
|---|---|---|
| `superseded by another decision` | Concurrency rejection on any proposal or catalog action | Render authoritative state, name the actor where disclosure allows, resolution controls `aria-disabled`; the status region announces the outcome; never show the rejected local action as pending. |
| `authority unresolved` | Policy basis cannot be resolved | `{colors.status-important}`; controls `aria-disabled`; dependency named safely. |
| `existence only` | Approver lost Conversation read access | State and expiry visible; no content, context metadata, or versions. |
| `expired at approval` | `ExpiresAt` passed before the approval command was accepted | `Approval rejected. Proposal expired at {expiresAt}`; state `Expired`. |
| `duplicate submission (idempotent)` | Same command re-sent | No second pending state; show the existing pending state. |
| `safety blocked at approval` | Then-current policy fails the selected version | Approval rejected; proposal non-terminal; version marked; other versions remain selectable. |
| `regeneration ceiling reached` | Ceiling from `hexa` configuration | Regenerate `aria-disabled` with the reason. |
| `stale proposal` | Proposal changed under the viewer | Preserve unsaved text; show authoritative state and actor where disclosure allows; offer refresh; approve and reject `aria-disabled` until refreshed. |

### Generation failure record

A generation failure never creates a proposal, version, queue entry, notification, editor, or approval action. Retained content appears only in a separate non-approvable failure record under Operational status and Audit evidence, subject to the same authorization and sensitive-content rules. Story 6.1 owns the record; the minimum safe fields are interaction id, failure class, timestamp, and authorization gate. A regeneration failure links its record to the proposal without changing proposal state.

### High-risk pending commands

| Item | Rule |
|---|---|
| Families | The nine families, with their required confirmation contents, are in § Confirmation contents by family. |
| Lock scope | Advisory, per (session, resource identity, operation family). Unrelated resources and families proceed. |
| Session | Authenticated user plus browser tab or circuit. |
| Start and clear | Begins on submission; clears only on authoritative rejection before acceptance or a terminal result; `AuthoritativePending` keeps the lock held. |
| Sequence | The activated control becomes `pending-command-indicator` with `DisabledFocusable="true"` and keeps focus; it shows `Submitted`, then `AuthoritativePending` with projection reference, then `awaiting projection` on catch-up exhaustion with a refresh action. |
| Focus after resolution (the deterministic focus rule) | The same item's **status cell or badge container** with `tabindex="-1"` and an accessible name carrying the item identity and the new state, then the next row, then the page heading. The page-level live-region nodes are never a focus target and never receive `tabindex`; "status region" in this table always means the item's status cell, never a live region. |
| Reload | Pending state is re-derived from the authoritative status projection and the lock re-armed. |
| Second tab | Derived from authoritative pending alone: the projection reports the command accepted and this tab holds no local lock, so it renders `pending in another session` with resolution controls `DisabledFocusable`. This needs the accepted-by session or actor reference in § Known gaps; until it lands the second tab renders the same-session variant. |
| Timeout | Catch-up exhaustion renders `awaiting projection`; never Success, never `Stale`. |
| Authority | EventStore optimistic concurrency, deterministic command identity, and idempotency are authoritative across tabs, sessions, retries, replay, and restarts. |

### Launch readiness evidence

| State | Treatment |
|---|---|
| `Pass` | Passing semantics only for the current source and measurement contract when all required fields qualify and `ObservedAt <= now < ValidUntil`, where now is the evaluation time. |
| `Block` | Stable support-safe `BlockerCode` and owning recovery path. |
| `InsufficientEvidence` | Name the missing evidence class safely; never render as `Pass`. |
| `Stale` | `ValidUntil` passed, detected on poll or focus; require re-observation; never fall back to an older `Pass`. |

A missing required record is an implicit block. Thresholds, seams, and sample rules are per `launch-readiness-register.md`.

### List, detail, and deep-link surfaces

Every grid or list distinguishes loading, empty, filtered-empty (with `FcFilterResetButton`), error, permission-denied, and `catching up`. Empty never leaks unauthorized records. Unauthorized, foreign-tenant, and non-existent ids render one `not available` state with identical copy, status class, and timing, from the two `Agents.Surface.NotAvailable.*` keys.

Detail routes bind to `FcAggregateDetailState`, whose seven values do not map one to one onto this spine's states:

| `FcAggregateDetailState` | Agents rendering |
|---|---|
| `Loading` | Cold load only. |
| `Ready` | The ready body. `catching up` renders here with the freshness badge; it is a read-freshness condition, not a shell state. |
| `Stale` | **Unused.** The shell's `Stale` would collide with this spine's reserved meaning, evidence past `ValidUntil`; readiness staleness renders inside the ready body instead. |
| `Degraded` | A consumed dependency is unavailable; the degraded banner names it safely above the ready body. |
| `Unauthorized`, `NotFound`, `Unavailable` | The single `not available` content, identical in all three, supplied to each slot separately because the component renders one slot per state. |

Search and filter suggestions return only records in tenant scope and readable by the requester. Agents registers no command palette entries in V1.

On `connection lost`, the status region shows a notice, pending items stay pending, **Call hexa** and the nine high-risk submits become `DisabledFocusable` with the reason, and on reconnect the lock is re-evaluated from authoritative status with focus preserved.

### Audit availability

| State (`AuditAvailabilityStatus`) | Treatment |
|---|---|
| `AuditPending` | Expected, not available; never Success. |
| `AuditAvailable` | Queryable and linked. |
| `AuditDelayed` | Late; wait, retry, escalate. |
| `AuditUnavailable` | Cannot load; safe reference and recovery. |

## Interaction Primitives

- FrontComposer shell shortcuts and palette remain available. Agents registers no palette commands and no domain shortcuts in V1.
- Grids follow § Grid rules.
- High-risk actions follow § High-risk pending commands: `high-impact-confirmation`, then `pending-command-indicator`, then the deterministic focus rule.
- Wherever this spine says a control is `aria-disabled`, the binding is `FluentButton.DisabledFocusable="true"` on a Fluent control, or the `aria-disabled` attribute with the control left focusable and its activation suppressed on a hand-authored one. `Disabled` and `Loading` are never used for a blocked-but-explicable state, because both remove the control from the tab order and take the reason with it.
- Editing a proposal is explicit; regeneration is a distinct action; approval applies to the selected version only.
- `Esc` closes transient UI without committing and never discards typed content: this covers the proposal editor and equally any non-empty text input inside a dialog, including the required **Call hexa** prompt, which confirms before discarding or keeps the draft for the session. Focus returns to the trigger, or follows the deterministic focus rule when the trigger is gone.
- Hover-revealed row actions also render on row focus-within and are dismissible; tooltips are hoverable and `Esc`-dismissible. No required action or reason is hover-only.
- Accordion use and primary regions per surface are in `DESIGN.md § Layout & Spacing`.
- Policy, cap, mode, expiry, and ceiling changes state their future-only effect in the confirmation.

## Accessibility Floor

WCAG 2.2 AA is the binding behavioral floor across every interactive V1 route and high-impact state. Contrast and icon pairing live in `DESIGN.md`.

Live regions: **Agents owns one node of each politeness per route, and every event is announced by exactly one node.** Mandatory FrontComposer components render live regions of their own; the rule governs which node announces an event, not how many nodes the DOM holds. The carrier is a single Agents-owned `AgentsPageStatusRegion` component placed once per page body, holding both nodes; no badge, panel, or row component carries `role="status"` of its own.

- `FcFilterEmptyState` and `FcExpandInRowDetail` speak for their own filter and row-expansion events only; Agents never duplicates those into its node.
- `FluentMessageBar` renders with `AriaLive="Off"` whenever its text is also pushed to the Agents node.
- `connection lost` is announced by the shell's `FcProjectionConnectionStatus`; Agents announces only the domain consequence and never repeats it.
- `ConversationAgentCallPanel` carries its own pair of nodes under this same table, because the seam has no Agents route.

The NFR-14 `LiveRegionAnnouncedTick` observes the localized mutation in the Agents node after render commit, so a duplicate announcement corrupts the measurement as well as the experience.

| Politeness | Events |
|---|---|
| `role="alert"` | denied; generation failed; posting failed; expired (proposal detail only, or on the queue when the expiring row is the focused one); safety blocked; capacity rejected; rate limited; the domain consequence of connection loss. |
| `role="status"` | authoritative pending; proposal created; approved; posting pending; posted; regenerated (`Version {n} generated` with a go-to action); version selection changed (`Showing version {n}`); response mode unchanged after cancel; context blocked; expiry warning (once per proposal); superseded by another decision; status refreshed; capacity queued; catching up; list changed after a deferred re-sort. |

Announcement volume must not grow with list length. The queue polls 25 rows sorted nearest-expiry-first, so a burst at the top of the hour must not produce 25 announcements: per-proposal expiry alerts are scoped to proposal detail, and on the queue an aggregate is announced at most once per poll cycle in the polite node, never as `role="alert"` unless the row that expired is the focused one.

Route heading and focus:

- Every route renders a non-blank localized heading with `HeadingTabIndex=-1` and a `PageTitle`, focused through `FocusHeadingAsync()`. `FcAggregateListPage` takes those as parameters. `FcAggregateDetailPage` does not: it has no heading parameters and its non-ready slots replace the body entirely. On the eight Constrained detail routes the domain therefore renders `FcPageHeader` as the first child of **every** state slot, so `not available`, loading, and degraded each keep an h1, a page title, and a focus target. "Heading present and focused in the `not available` state" is part of the `LR-UI-CONFORMANCE` lane.
- `FcAggregateDetailPage` renders a back link by default. Every detail route supplies a localized `BackLinkLabel` or sets `ShowBackLink="false"`; an unset label yields an anchor with no accessible name, and HFC1050 does not fire on framework components to catch it.
- Focus moves to the heading on navigation, when proposal detail opens from the queue, and after a forced refresh (with `Status refreshed. Approval is still pending.` announced).
- Heading levels inside a route follow `DESIGN.md § Layout & Spacing`.
- After regeneration, focus stays on Regenerate and the status region announces the new version.

Forms:

- Every control has a visible label. Errors are associated through `aria-describedby` and `aria-invalid`; an error summary receives focus on submit and links to the first error.
- Error text is one localized whole string naming the field, the fault, and the fix (WCAG 3.3.1, 3.3.3), never a bare "Invalid value".
- Required fields carry a visible required marker in addition to `aria-required`; the marker is never color alone.
- Validation runs on submit. After the first submit, a field re-validates on blur; before it, typing never produces an error.
- Numeric fields state units and currency in the label.
- Secret-adjacent fields are named by their configured state (`Secret reference: configured`), never by value.
- Policy-source rows have an accessible name including kind and basis.

Other criteria:

- Reflow: every route stays operable at 320 CSS px width (400% zoom), including proposal detail, version history, and `high-impact-confirmation`. Evidence at 320 px is part of `LR-UI-CONFORMANCE`.
- Target size (2.5.8): every actionable target is at least 24 by 24 CSS px in all three densities, or meets the spacing exception; Compact density is in the conformance lane.
- Focus not obscured (2.4.11): no sticky element overlaps the content scroll area, or scroll-padding equals sticky heights.
- Dragging (2.5.7): no drag-only operation.
- 3.2.6 Consistent Help: N/A, no help mechanism is repeated across pages. 3.3.8 Accessible Authentication: N/A, authentication is shell-owned.
- Reduced motion never hides generation, approval, or posting state changes. No `MessageBarAnimation` is enabled, and no control activates on `pointerdown` or `mousedown`; Fluent's click activation satisfies 2.5.2 and this states the inherited behavior so a hand-authored control cannot drift from it.
- Language of parts (3.1.2): generated content, the editor textarea, the preview, and Conversation-derived evidence carry a `lang` attribute from the Provider result or the Conversation language when either is known, because that text is not necessarily in the UI culture; where neither is known the region inherits the page language.
- Secrets, raw payloads, and other-tenant data never appear in accessible names, tooltips, copied text, diagnostics, or announcements.
- Timing (2.2.1): proposal expiry is an essential governance limit set by the tenant; no in-UI extension. Product sign-off on the essential-exception position is a deferred item. The warning that makes the limit workable is the nearing-expiry threshold in § Proposal lifecycle, and its 15-minute floor is required for the exception to hold: without the floor, a 1-hour window would give a six-minute warning for a governed approval decision.
- Conformance evidence: an Agents-owned axe lane over every interactive V1 route in Light, Dark, and forced-colors, plus the manual AT matrix (NVDA with Firefox, JAWS with Chrome, VoiceOver with Safari), is `LR-UI-CONFORMANCE` evidence. FrontComposer override diagnostics (HFC1050 to HFC1055) cover FrontComposer overrides only; hand-authored pages rely on the Agents lane.
- Whole-string English/French parity is required for every route and high-impact state. Missing routes, keys, live-region evidence, viewport evidence, or conditional skips make `LR-UI-CONFORMANCE` `InsufficientEvidence`.

## Responsive & Platform

Desktop is primary. The product is a web admin and workflow surface, not a native app.

| Breakpoint | Behavior |
|---|---|
| Phone and 320 CSS px reflow | One column. Every route, proposal detail, version history, and `high-impact-confirmation` remain operable; the confirmation's `FixedHeaderFooter` body keeps Confirm and Cancel reachable. Grids scroll inside their own container under the WCAG 1.4.10 data-table exception, never scrolling the page, and use `FcExpandInRowDetail` for secondary columns. The proposal queue's surviving column set is state, expiry, and the open action; the Provider catalog's is Provider name, model, readiness, and the open action. |
| Tablet | Navigation collapses per FrontComposer. Grids preserve critical columns. Proposal detail stacks editor, metadata, and version history. |
| Desktop | Full-width grids; Constrained forms and editors; proposal detail stacked with accordion items. |
| Wide desktop | Extra width goes to grids, not decorative panels. |

A high-risk action is unavailable only when its required decision context cannot render, and then with a visible reason and review-only access; that is the meaning of the `LR-UI-CONFORMANCE` restrictive-viewport clause.

## FrontComposer Readiness

Cross-index of shell capabilities to the section that owns each rule.

| Capability | Dependency | Owning section |
|---|---|---|
| FC-LYT | One `FcPageLayoutMode` per route | § Information Architecture; `DESIGN.md § Layout & Spacing` |
| FC-TBL | Hand-authored `FluentDataGrid` in `FcAggregateListPage`; generated `[Projection]` lane not used | § Grid rules |
| FC-A11Y | Shell skip links, focus, landmarks, shell live regions; Agents owns page live regions | § Accessibility Floor |
| FC-L10N | Shell chrome strings; Agents owns `AgentsResources` | § Voice and Tone |
| Policy-gated nav | Four policy constants; hide, then deny on direct navigation | § Information Architecture |
| Pending pattern | Agents truth-flow helper; `FcPendingCommandSummary` out for V1 | § Projection catch-up contract |
| `IBadgeCountService` | Not used in V1; see the `proposal-notification` row | `proposal-notification` row |
| `FcFluentIcons` | Sole icon source; glyph requests | `DESIGN.md § Brand & Style` |
| `EXT-CONV-UI-1` | Conversation action contribution; Uncommitted; blocks Story 6.7 | § Conversation Integration Seam |
| `FcAggregateListPage` | Route heading, `HeadingTabIndex=-1`, `PageTitle` as parameters | § Accessibility Floor |
| `FcAggregateDetailPage` | No heading parameters; the domain supplies `FcPageHeader` in every state slot, plus a localized `BackLinkLabel`; `FcAggregateDetailState` mapping | § Accessibility Floor; § List, detail, and deep-link surfaces |
| `IProjectionChangeDetailNotifier` | Projection nudge source, filtered to the authoritative projection ids | § Projection catch-up contract |
| `EXT-SECRETS-1` | Export and deletion fail closed while not `Available` | `audit-governance-panel` row |

## Key Flows

Each flow names its stories. UJ-1 to UJ-4 mirror the PRD of 2026-09-08.

### UJ-1 - Nora configures `hexa` for a tenant launch

Stories: 5.2, 5.3, 5.4, 5.5, 5.7.

1. Nora opens the Agents overview and sees `hexa` as `active, not proven callable` or `invalid configuration` with safe blockers.
2. She opens `hexa` configuration and reads the Agent Party identity status; it is read-only, and a missing or ambiguous identity is a blocker resolved by platform provisioning, not a command she can issue here.
3. She selects a Provider/model that passes the six-part eligibility set; a callable `Degraded` result stays Warning.
4. She enters Agent Instructions.
5. She chooses Automatic Response Mode or Confirmation Response Mode with `response-mode-toggle`.
6. If Confirmation Response Mode is selected, she configures the Approver Policy, including a Conversation Facilitator source and disclosure categories, and validation confirms Conversation access and segregation of duties can be satisfied.
7. She sets the proposal expiry duration and the regeneration ceiling; the confirmation states future-only effect.
8. Readiness lists remaining blockers: cost cap and rate limit unconfigured. She opens Cost controls and configures them.
9. She submits activation through `high-impact-confirmation` (`AgentActivation`); the UI shows `Submitted`, then `AuthoritativePending` with the projection reference.
10. `ProjectionConfirmed` shows lifecycle `active` separately from callability.
11. **Climax:** `agent-readiness-badge` shows `callable` only when the Story 5.5 result at the shown `RegistryRevision` proves it; active alone stays non-Success.
12. Resolution: the overview shows Provider/model, lifecycle, mode, callability, `EnvironmentProfile`, freshness, and safe blockers.

Failure path: no eligible Provider/model. Activation is blocked with the eligibility reason; no one can call `hexa` until the blocker is resolved.

### UJ-2 - Milan calls `hexa` from a Conversation and receives an automatic reply

Stories: 6.1 to 6.4, 6.6, 6.7.

1. Milan is in a Source Conversation he can access; **Call hexa** is enabled because `hexa` is callable.
2. He opens the dialog, reads the effective mode, submits a prompt; the UI shows `submitted`.
3. The UI shows `authoritative pending` after rendering the accepted interaction identity and projection reference.
4. `hexa` joins the Conversation at this first accepted call; membership appears as a participant.
5. Context loads within the Safe Context Budget; safety passes; cost is reserved; capacity admits.
6. Generation runs; Milan sees the in-flight state tied to the Conversation.
7. In Automatic Response Mode the call status moves to `AgentCallOperationStatus.PostingPending`; nothing is a message yet. These are call statuses, not the `ProposedAgentReplyState` values of the same name: the automatic path creates no proposal to borrow them from.
8. The client renders the projection whose call status is `Posted`, carrying the posted `MessageId`.
9. **Climax:** the response appears as durable Conversation content attributed to `hexa`, with `agent-response-marker`; only this proof is Success.
10. Resolution: participants continue the Conversation.

Failure paths: Milan lacks permission, so the action is absent or `denied` before Provider invocation. Context exceeds the budget, so `context blocked`. Safety, budget, rate limit, or capacity blocks, each distinct and non-Success. Posting fails, so `PostingFailed` with the same bounded audited retry as the proposal path. Generation fails, so a failure record only. Provider outcome unknown, so `unknown outcome` with the reservation held.

### UJ-3 - Anika approves a proposed `hexa` response before it enters the Conversation

Stories: 7.1 to 7.6.

1. A participant calls `hexa` in a Conversation configured for Confirmation Response Mode; a `Pending` proposal is created after successful generation.
2. Anika, authorized by the Approver Policy and holding Conversation read access, discovers it through the pending count, queue, or Conversation status entry.
3. She opens proposal detail; focus lands on the heading; she reviews the generated content, context metadata, caller, Provider/model, mode, and `ExpiresAt`.
4. She requests one regeneration; the proposal becomes `Regenerated`, focus stays on Regenerate, and `Version 2 generated` is announced.
5. She views versions one at a time with the version radio group and selects the generated version she did not author.
6. She approves through `high-impact-confirmation`, which renders the `VersionId`, kind, author, timestamp, full content, and the safety re-check statement; the proposal becomes `Approved`.
7. `PostingPending` renders; still not a message.
8. The client renders the projection whose outcome is `Posted`.
9. **Climax:** the approved version is a Conversation Message with approval evidence linked; only now is the proposal Success.
10. Resolution: the Conversation holds only the approved response; history preserves every version, the editing Party of any edit, and the policy basis per disclosure category. From the moment `Approved` is projection-confirmed, edit, regenerate, and approve are gone from the editor, so no later action can post a different version.

Segregation of duties: had Anika edited the draft, she could not approve that edited version; a second authorized Approver would, and the marker would show the human edit.

Failure paths: the proposal expires, so `Expired` and a new call. Her approval races expiry, so `expired at approval`. Another Approver resolves first, so `superseded by another decision`. The then-current policy fails the version, so `safety blocked at approval`. The editor is dirty, so approval is `aria-disabled` until saved or discarded. She loses Conversation read access, so `existence only`. Posting fails, so `PostingFailed` with bounded retry.

### UJ-4 - Omar integrates Agent operations through the API

Stories: 4.1, 5.5, 8.4, 8.7.

1. Omar reads the public contracts for Provider administration, Agent administration, invocation, proposals, status, audit, budget policy, safety publication, legal hold, export, deletion, and launch readiness.
2. He lists Provider options and the readiness result for an authorized tenant.
3. He configures `hexa` and observes the same `Submitted`, `AuthoritativePending`, `ProjectionConfirmed` stages and freshness as the UI.
4. He inspects proposal status and audit evidence without internal streams or SDK details.
5. He performs authorized proposal and governance operations under the same family locks and disclosure categories.
6. **Climax:** the integration observes and operates governed workflows with the same authorization outcomes as the admin UI.
7. Resolution: automation monitors readiness, failures, queues, consumption, and audit completeness.

Failure path: an unauthorized operation is denied with a typed result before any side effect, auditable without revealing inaccessible content or unrelated tenant records.

### UX-J5 (UX-derived) - Priya governs a production-like launch

Grounded in FR-26 to FR-28, FR-30, FR-32, NFR-9 to NFR-14, RQ-1. Stories: 8.1 to 8.7, 5.5.

1. Priya opens Launch readiness and sees every `Block`, `InsufficientEvidence`, `Stale`, and missing record before any `Pass`, with `EnvironmentProfile` and `RegistryRevision`.
2. She reads the Conversation context policy: Safe Context Budget components and no declared bounded behavior.
3. She validates and publishes a Content Safety Policy version through `high-impact-confirmation`; the UI waits for `ProjectionConfirmed` before Success.
4. She configures caps and rate limits, checks consumption, reserved versus settled spend, held reservations, and the 80% and 100% states; she applies a bounded audited override for a tenant at its cap.
5. She inspects the retention rule, legal hold, export, and deletion controls on Audit governance.
6. She reviews the NFR-14 sample kinds, counts, and live-region observation on `launch-readiness-panel`.
7. **Climax:** production enablement shows READY only when every gate is `Pass` at one checkpoint and `RQ-1` records it; qualification callability never implies READY.
8. Resolution: any later regression names its owner and recovery action without exposing content.

Failure path: publication, cap update, export, deletion, or projection confirmation partially fails; the surface stays pending or restrictive, the alert region announces the failure, and partial application never shows as Success.

### UX-J6 (UX-derived) - Milan meets capacity backpressure

Grounded in NFR-12, AD-24. Story: 6.5.

1. Milan submits a call during tenant saturation; the UI shows `authoritative pending`.
2. The call renders `capacity queued` with position where safe.
3. **Climax:** the queue window elapses and the call renders `capacity rejected`, a distinct non-Success outcome with no Provider work, proposal, or message.
4. Resolution: the sole recovery is a new call; Operational status counts the rejection per tenant.

Failure path: a queued call is admitted and continues UJ-2 from step 5.

## Known gaps

Where this spine is ahead of the code or the contracts. Every row names the story that closes it, so a correction is never unowned.

### Contracts that must grow

| Contract | Addition | Owner |
|---|---|---|
| `AgentReadinessStatus` | `ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved` | 5.5 |
| `AgentCallOperationStatus` | `SafetyBlocked`, `BudgetBlocked`, `RateLimited`, `CapacityQueued`, `CapacityRejected`, `UnknownOutcome` | 6.3, 6.4, 6.5 |
| `AgentCallOperationStatus` | `PostingPending`, `Posted`, and `PostingFailed` with a typed reason, plus the field carrying the posted `MessageId`. The automatic path has no proposal whose `ProposedAgentReplyState` it could reuse. | 6.7 |
| `ProviderReadinessResult` | Published, including a by-Provider/model query for the disable blast radius | 5.5 |
| `ApproverPolicySource` | `DisclosureCategory`. The spine declares one per row; `AgentApproverPolicy(Sources, DisclosureCategory)` carries one per policy. | 5.4 |
| `ProposalDetailView` | `RegenerationCount` and `RegenerationCeiling` for the ceiling check; `CanCurrentUserApproveSelectedVersion` with its reason for the segregation-of-duties pre-state | 7.3, 7.4 |
| `AgentSetupTruthState` and the acceptance contracts | An accepted-by session or actor reference. Without it, a second tab cannot distinguish `pending in another session` from a pending command of its own. | 5.7 |
| `PendingProposalView`, `ProposalDetailView` | The configured expiry window alongside `ExpiresAt`, and the server read time, so nearing expiry is computable without the browser clock | 7.1, 7.2 |

### Shipped-code corrections

The spines win on conflict with shipped code. Each row is a known divergence between `src/Hexalith.Agents.UI` and this spine, with the section that states the rule and the story that absorbs it.

| Divergence in shipped code | Rule | Absorbing story |
|---|---|---|
| Polling 250 ms / 5 s and 200 ms / 8 s, where each pair is interval then budget | § Projection catch-up contract | 5.7 |
| Harness route registered at nav order 4 | § Information Architecture, rules | 6.7 |
| Nine nav entries registered | § Information Architecture, twelve entries at nav order 0 to 11 | 5.7 |
| Launch readiness gated by `Agents.Administrator` | § Information Architecture, `Agents.Operator` | 5.7 |
| Queue default sort `CreatedAt` descending | `proposal-queue-grid` row | 7.1 |
| Version list as a custom listbox | `version-history` row | 7.2 |
| Inline confirmations | § Confirmation contents by family | 7.2 |
| `USD` and `0` pricing defaults; client sends `PricingVersion` `0` | `provider-catalog-grid` row | 5.3 |
| Per-badge `role="status"` on five badge components | § Accessibility Floor, live regions | 8.6 |
| `Disabled` on the regenerate control | § Interaction Primitives, the `aria-disabled` binding rule | 7.3 |
| `LocalizationResourceTests` | § Voice and Tone, localization mechanics | 8.6 |
