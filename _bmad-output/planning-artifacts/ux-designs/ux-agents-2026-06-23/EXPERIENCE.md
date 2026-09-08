---
name: Hexalith Agents
description: Behavioral spine for the Hexalith Agents FrontComposer UI. Owns surfaces, routes, policies, states, interactions, accessibility, and journeys for governed AI participation in Conversations. DESIGN.md owns visuals.
status: final
created: 2026-06-23
updated: 2026-09-08
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
---

# Hexalith Agents - Experience Spine

> Final spine reconciled to the PRD of 2026-09-08, the 2026-09-08 UX validation, and the shipped Story 5.3 code. `DESIGN.md` owns visuals; this file owns behavior, surfaces, states, accessibility, and flows. The spines win on conflict with mockups, wireframes, imports, sketches, and shipped code. Decisions are logged in `.memlog.md`; the 2026-09-08 reconciliation, dispositions, and deferred items are in `reconcile-validation-2026-09-08.md`, and the 2026-08-02 closure set remains in force except where that file records a revision.

## Foundation

Hexalith Agents is a desktop-first responsive web experience composed through **Hexalith.FrontComposer**. The shell owns header, navigation, account controls, theme and density settings, command palette, skip links, keyboard shell behavior, shell localization, and shell status chrome. The Agents domain owns registered navigation entries, page bodies, domain copy, page-level live regions, Agent and proposal workflow behavior, and BFF/API-facing interaction.

The UI system is inherited: Microsoft Fluent UI Blazor v5 through FrontComposer. `DESIGN.md` is the visual identity reference and names the pinned package. This spine references `DESIGN.md` tokens by name and does not restate visual styling. Agents defines no custom theme.

This is an internal governed operational tool, not regulated UX. Product-required authorization, tenant isolation, approval, proposal versioning, provider-secret safety, cost governance, and audit evidence are first-class. Copy is plain and precise.

V1 exposes only `hexa` as product behavior. V1 excludes long-term memory, tools, project and folder content, ambient triggers, external channels, customer-facing billing, and business actions beyond posting automatic or approved replies to Conversations. These non-goals are stated here once.

By decision, no mockups or wireframes were produced; the spines are the sole reference (2026-08-02, reaffirmed 2026-09-08).

## Information Architecture

FrontComposer shell navigation registers an **Agents** domain. Every surface below is bound to a route, a nav order, an authorization policy constant from `AgentsFrontComposerRegistration`, one `FcPageLayoutMode`, an owning story in Epics 5 to 8, and a read/write contract. Field inventories live in the Component Patterns rows. The provider surface is the PRD's Global Providers Aggregate, the architecture's `ProviderCatalog`, and this spine's Provider catalog; the three names denote one thing.

| Surface | Route | Nav order | RequiredPolicy | Layout | Owning story | Read/write contract | Purpose |
|---|---|---|---|---|---|---|---|
| Agents overview | `/agents` | 0 | `Agents.Administrator` | FullWidth | 5.2, 5.5, 5.7 | `AgentInspectionResult`; Story 5.5 setup/callability result; pending count via `IBadgeCountService` | See lifecycle, proven callability, pending count, recent failures, and safe blockers at one `RegistryRevision`. |
| `hexa` configuration | `/agents/configuration` | 1 | `Agents.Administrator` | Constrained | 5.2, 5.7 | `AgentSetupResult`; `AgentCommandAcceptance` (FR-29 stages) | Configure `hexa` and activate it once blockers clear. |
| Provider catalog | `/agents/providers` | 2 | `Agents.Administrator` | FullWidth | 5.3, 5.5 | `ProviderCatalogEntryView`; `ProviderCatalogCommandAcceptance`; `ProviderReadinessResult` (Story 5.5) | Govern Provider/model records, pricing, and readiness without secrets. |
| Approver policy | `/agents/approver-policy` | 3 | `Agents.Administrator` | Constrained | 5.4 | `AgentApproverPolicy`; `ApproverPolicySourceKind` (closed list) | Define who may resolve proposals and how each source is disclosed. |
| Conversation context policy | `/agents/context-policy` | 4 | `Agents.Administrator` | Constrained | 6.2 | Read model defined by Story 6.2 from `AgentInteractionContextPolicy` | Read the effective context rule and Safe Context Budget. |
| Content safety policy | `/agents/content-safety` | 5 | `Agents.Administrator` | Constrained | 6.3, 8.4 | `AgentContentSafetyPolicy` (`BlockedOutputCategories`, `RestrictedOutputCategories`); `ConfigureAgentContentSafetyPolicy`; publish command by Story 8.4 | Validate and publish a versioned safety policy. |
| Cost controls | `/agents/cost-controls` | 6 | `Agents.Administrator` | Constrained | 6.4, 8.4 | Budget policy contracts defined by Story 8.4 (FR-32) | Configure caps and rate limits; inspect consumption and overrides. |
| Proposal queue | `/agents/proposals` | 7 | `Agents.Approver` | FullWidth | 7.1 | `PendingProposalsResult` | Discover proposals the Approver may currently read. |
| Proposal detail/editor | `/agents/proposals/{AgentInteractionId}` | none | `Agents.Approver` | Constrained | 7.2 to 7.6 | `ProposalDetailView`; `ProposalVersionSummary` | Review and resolve one proposal. |
| Operational status | `/agents/status` | 8 | `Agents.Operator` | FullWidth | 6.7, 8.7 | `AgentOperationalStatusSummaryView`; failure record fields by Story 6.1 | Distinguish readiness, blocked calls, failures, and posting outcomes by recovery. |
| Launch readiness | `/agents/launch-readiness` | 9 | `Agents.Operator` | FullWidth | 5.5, 8.7 | `launch-readiness` projection checkpoint (Story 8.7); the built `AgentLaunchReadinessView` is superseded evidence | Inspect every gate record and blocker at one checkpoint. |
| Audit evidence | `/agents/audit`; `/agents/proposals/{AgentInteractionId}/audit` | 10 | `Agents.AuditOperator` | List FullWidth; detail Constrained | 7.x, 8.x | `AuditEvidenceResult`; `AuditAvailabilityStatus` | Inspect support-safe evidence end to end. |
| Audit governance | `/agents/audit-governance` | 11 | `Agents.AuditOperator` | Constrained | 8.1 to 8.3 | Legal hold, export, deletion commands and progress projections by Stories 8.1 to 8.3 | Operate retention, legal hold, export, and deletion. |
| Conversation invocation | Conversation-owned; no Agents nav entry | none | Conversation access plus Agent call permission (server) | Conversation surface | 6.7 via `EXT-CONV-UI-1` | `IConversationAgentCallGateway` | Call `hexa` through the sole V1 entry, the Conversation-owned **Call hexa** action. |
| API/client contract reference | Developer docs | none | none | none | 4.1, 5.5 | Public contracts | Omar's journey; not a FrontComposer screen. |

Rules:

- `/agents/conversation-call` is a pre-integration harness, not an alternate entry point. It is delisted from navigation now; Story 6.7 removes or blocks it before closing.
- Navigation hiding is disclosure, not authorization. Unauthorized entries are hidden. Direct navigation to an unauthorized, foreign-tenant, or non-existent id renders the single `not available` state defined under State Patterns. Every page also carries `[Authorize(Policy = ...)]` and the server decision is authoritative.
- Nav glyphs come only from `FcFluentIcons`. Missing glyphs are FrontComposer requests, never silent reuse; the request list is in `DESIGN.md § Brand & Style`.

Surface closure status: **final**. Every stated need has a surface and every surface has a journey below.

## Conversation Integration Seam

**Call hexa** lives inside a Hexalith.Conversations surface. Conversations is a separate module; the seam is a named external dependency, not an Agents route.

| Element | Binding |
|---|---|
| Conversations-side extension point | `EXT-CONV-UI-1`: a versioned Conversation action contribution and registration contract with tenant-scoped authorization and typed registration failure. Status `Uncommitted`, owner `TBD` in `external-dependency-register.md`. |
| Agents-side artifacts | Exported `ConversationAgentCallPanel` component plus `IConversationAgentCallGateway`. |
| Consuming story | 6.7. Blocked from `ready-for-dev` while `EXT-CONV-UI-1` is `Uncommitted` (FR-21). |
| Readiness | `FrontComposer Readiness` row below; `LR-UI-CONFORMANCE` covers the contributed action once registered. |

Behavior at the seam:

- **Call hexa** renders visible but `aria-disabled` with the safe blocker text whenever `hexa` is not proven callable for the tenant. It is absent for Parties without Conversation access or Agent call permission.
- `hexa` joins a Conversation as a participant at the first accepted Agent Call, before generation, under the platform Agents service principal. The join is idempotent. Membership failure fails the call closed with no Provider work, proposal, or Conversation Message. Membership is visible on the same terms as any other participant.
- Removal of `hexa` is a Conversation-owned action. Agents reacts by blocking further calls in that Conversation and moving every non-terminal proposal to `Abandoned` with reason `Agent removed from Conversation`, versions preserved.
- `agent-response-marker` is contributed for every posted Agent Response; its rules are in Component Patterns.
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
| Parity gate | `AgentsResourcesParityTests` enforces exact English/French key parity; a missing key is a conformance failure. |
| Whole strings | No runtime sentence assembly from fragments. Named placeholders only. |
| Plurals | Per-count keys (`.Zero`, `.One`, `.Other`); never a runtime `s` suffix. |
| Formatting | Dates, numbers, currency, and percentages are `CultureInfo`-formatted values passed through named placeholders. |
| Architecture tokens | Every token (`Ready`, `Callable`, `Degraded`, `Blocked`, `InsufficientEvidence`, `Stale`, `Pass`, `Block`, `Unknown`) has a localized label; raw tokens never display. |
| Shell dialogs | Shell dialogs with unlocalized literals (`FcDestructiveConfirmationDialog` hard-codes `Cancel`) are not reused for domain confirmations; `high-impact-confirmation` is domain-localized. |
| Shell chrome | Shell strings remain FrontComposer-owned per FC-L10N. |

## Component Patterns

Behavioral rules only. Visual specs live in `DESIGN.md.Components`.

| Component | Behavioral rules | Props / events / variants |
|---|---|---|
| **agent-readiness-badge** | Reads the versioned setup/callability result published by Story 5.5 through the `agent-setup-readiness` projection at a `RegistryRevision`. Lifecycle `active` is never Success by itself. Carries `EnvironmentProfile`, gate-set name, matrix version, `RegistryRevision`. Re-evaluates `ValidUntil` on every poll and window focus; lapse renders `stale`. The built `AgentReadiness.MapState` (`Callable = Active && no blockers`) is superseded and replaced. | States: the nine states in § Agent readiness. Production enablement is a separate indicator, Success only when `RQ-1` is READY. |
| **provider-status-badge** | Consumes `ProviderReadinessResult` (`OperationalState / Callability / ReasonCode`, Story 5.5) without inference. Interim rule before Story 5.5: render `Unknown` with `{colors.status-subtle}`, never Success, when no `Callability` field exists. State treatment is in § Provider and model. | Inputs: the readiness triple, `CapabilityVersion`, `ValidUntil`. |
| **proposal-state-badge** | Renders the ten `ProposedAgentReplyState` values and the display-only outcomes per § Proposal lifecycle. | Inputs: state, `ExpiresAt`, nearing-expiry flag, outcome. |
| **response-mode-toggle** | `FluentRadioGroup` with two mutually exclusive modes. Changes apply to future Agent Calls only; the confirmation states that pending proposals remain proposals when switching to Automatic Response Mode. | Events: change opens `high-impact-confirmation` (`AgentSetupMutation`). |
| **agent-config-form** | Validates required fields before activation. Activation blockers are inline and actionable and include the six-part Provider eligibility set (enabled, configured, text generation, valid limits, valid pricing, non-regressed `CapabilityVersion`), unconfigured cost cap, and unconfigured rate limit. Proposal expiry duration: default 24 hours, range 1 hour to 30 days, future proposals only. Regeneration ceiling per proposal: documented default and valid range from FR-32, future proposals only. Shows what changed where safe to expose. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. Activation is `AgentActivation`; other writes are `AgentSetupMutation`. |
| **approver-policy-builder** | Builds authority from `ApproverPolicySourceKind`: `ConversationOwner` is labeled Conversation Facilitator (resolved from `ParticipantRole.Facilitator`), `Caller`, `PredefinedParty`, `TenantRole`. Each row declares a disclosure category; the default is operator-only; a source revealing restricted-role membership cannot be user-visible. Validation rejects a source that cannot satisfy Conversation access at call time and a configuration that cannot satisfy segregation of duties. Ambiguous or unavailable sources fail closed and block Confirmation Response Mode activation. Row order is edited with buttons or a menu, never drag-only. | Row accessible name includes source kind and basis. Publish is `PolicyPublication`. |
| **provider-catalog-grid** | Hand-authored `FluentDataGrid` inside `FcAggregateListPage`. Platform-scoped. Columns include enablement, configured state, capability limits, pricing version (pinned), currency, unit prices, `CapabilityVersion`, readiness triple, freshness. The pricing editor has no default values. Currency is ISO 4217 and must equal the tenant budget currency; on mismatch the row renders `{colors.status-important}` and reservations are blocked. A zero unit price must be typed explicitly, and the confirmation carries a warning line that a zero-priced model reserves nothing. Pricing edits, enable, and disable are `ProviderCatalogMutation`. The disable confirmation lists in-scope Agents whose callability will be blocked; it renders cross-tenant impact as an aggregate count or omits it per disclosure category, never as tenant names. `CapabilityVersion` is the concurrency token; outdated or regressed submissions render `superseded by another decision`. The UI never accepts a secret value. `ConfigurationReferenceId` is a reference only (`EXT-SECRETS-1`), operator-only visible, and masked with autocomplete off when editable. It is never echoed in validation or errors and never appears in URLs, clipboard actions, or diagnostics. | Mandatory for every grid: `FcFilterEmptyState`, `FcFilterResetButton`, `FcExpandInRowDetail`, `FcStatusFilterChips`; status cells render visible text plus icon. Default sort: Provider name, then model. |
| **proposal-queue-grid** | Hand-authored `FluentDataGrid` inside `FcAggregateListPage` with the same mandatory Fc components as `provider-catalog-grid`. Lists only proposals whose Source Conversation the Approver can currently read. `needs my action` is an `FcStatusFilterChips` slot. Filters: state, Agent, Source Conversation, caller, expiry. Empty and filtered-empty are distinct. Expiry renders as an absolute culture-formatted timestamp plus a static relative label refreshed at most once per minute, outside any live region. Polled re-renders never steal focus. | Default sort: nearest `ExpiresAt` first among `needs my action`, then most recent. Page size 25. Age is computed from `CreatedAt` server time. |
| **proposal-editor** | Exists only after successful generation. Authorized Approvers edit, regenerate, approve a selected version, reject, or abandon. Editing creates a new preserved version with editor Party and timestamp; an edited version is never presented as generated. Approval is `aria-disabled` while the editor is dirty. Approval is blocked when the Approver last edited the selected version or is the sole Approver of their own call. Regenerate is `aria-disabled` at the regeneration ceiling with the reason, and copy states that a regeneration is a chargeable call under cost caps and rate limits. On regeneration failure the editor keeps the proposal non-terminal with prior versions intact, records a linked failure record, announces the failure in the status region, and keeps approve, reject, and abandon available. Approval re-runs the then-current Content Safety Policy on the exact selected version. In a terminal state the editor is read-only, resolution actions are absent, and the sole action is `Start a new Agent Call`. An Approver who lost Conversation access sees existence and state only. | Exit is an explicit button with unsaved-changes confirmation. No client-side length limit. Regenerate while any resolution is pending is `aria-disabled`. Resolution actions open `high-impact-confirmation` (`ProposalResolution`). |
| **version-history** | Lists every generated, edited, and regenerated version with kind, author or Provider/model, timestamp, safety outcome, approval and posting markers. Selection is a radio group labeled by kind, author, and timestamp. V1 has no diff view: one version is viewed at a time. Approval decisions show the policy basis rendered per its FR-7 disclosure category; redacted and omitted show a category label, never the source. Failure records are not versions. | Inputs: `ProposalVersionSummary` list, `ApprovedVersionId`, `SelectedVersionId`. |
| **conversation-agent-call** | The Conversation-owned **Call hexa** action opens a `FluentDialog` naming `hexa` and the effective response mode. Prompt required; no client-side length limit because the Safe Context Budget is server-owned. Captures Source Conversation, caller, Agent, prompt, effective response mode, authorization decision, timestamp. Visible but `aria-disabled` with the safe blocker when `hexa` is not callable. | States: `submitted`, `authoritative pending`, then the § Agent call states. Duplicate submission is blocked per session, resource, and family. |
| **agent-response-marker** | Contributed through `EXT-CONV-UI-1` for every posted Agent Response. States AI-generated from Conversation Context and not human-verified (FR-11); adds human-edited provenance and editing Party where disclosure allows (FR-17). Never appears on unapproved content. | Variants: generated; human-edited. |
| **conversation-context-policy-panel** | Read-only. Shows policy version, Safe Context Budget components (model input limit at the applicable `CapabilityVersion`, reserved output allowance, Agent Instructions size, configured safety margin), tokenizer or named approximation, declared Approved Bounded Context Behaviors (none in V1), and the safe reason an oversized call fails closed. Offers no truncation, summarization, windowing, or retrieval control. | Inputs: Story 6.2 read model. |
| **content-safety-policy-editor** | Shows the fixed always-blocked categories from FR-26 and OQ-9 as a read-only list: child sexual abuse or exploitation; credible threats or instructions for imminent serious harm; encouragement or instruction for suicide or self-harm; credential theft, malware, or unauthorized compromise; secrets and private credentials; cross-tenant or unauthorized personal or Conversation data; control-bypass attempts; impersonation of a named Party or assertion of a decision on a Party's behalf. Restricted categories (hate, harassment, sexual, violent, illegal activity, sensitive personal) are editable only with an explicitly permitted tenant use case and Confirmation Response Mode. Validates a draft version, then publishes a future-only version through `high-impact-confirmation` (`PolicyPublication`). Copy states that approval-time and pre-post re-checks always use the then-current policy and that a re-check can only tighten. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. |
| **cost-control-editor** | Authors per-tenant monthly and per-call caps (currency equal to catalog pricing currency, `FluentNumberInput` with units), per-Party and per-Conversation rate limits over a stated rolling window, with no implicit defaults; an unconfigured value is a readiness blocker. Shows consumption, reserved versus settled spend, held `Unknown` reservations, the 80% warning and 100% fail-closed state evaluated on settled usage plus outstanding reservations with both figures labeled. The audited override (actor, justification, scope, expiry) is `TenantBudgetUpdate`. Changes apply to future calls only. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. Consumption evidence lists live on Operational status. |
| **launch-readiness-panel** | Reads one `launch-readiness` projection checkpoint (Story 8.7). Renders `Pass`, `Block`, `InsufficientEvidence`, `Stale` per GateId with Owner, `SourceVersion`, `ObservedAt`, exclusive `ValidUntil`, `RequiredEvidenceLevel`, `EvidenceReference`, `BlockerCode`, `RegistryRevision`, `EnvironmentProfile`, and consumed dependency status. Blockers precede passing records. The single UX-owned NFR-14 statement: the panel shows the three sample kinds, their ticks, sample counts against the minimum, and whether the localized live-region mutation was observed; thresholds and seams are per `launch-readiness-register.md § NFR-14 Browser Monotonic Timing Contract`. Never fills a missing record, never falls back to an older `Pass`, never issues the `RQ-1` decision. | Inputs: Story 8.7 checkpoint. |
| **audit-governance-panel** | Callable operations (FR-30): apply and release legal hold, request export, request deletion, each through `high-impact-confirmation` (`LegalHold`, `ExportRequest`, `DeletionRequest`). Shows the 365-day rule, hold state, export manifest state, deletion progress including projection purge and tombstone confirmation. Success only after authoritative evidence; partial failure stays restrictive and actionable. | Progress projections by Stories 8.1 to 8.3. Evidence lists live on Audit evidence. |
| **proposal-notification** | In-product only. `IBadgeCountService` supplies the count on the Proposal queue nav entry; the domain wraps it in the named link `{count} proposals pending approval`. If the shell exposes no label slot, the count renders inside the Agents overview link instead. Counts only proposals the Approver can currently read. No content, never implies posting. | Variants: zero (no badge), count. |
| **operational-status-panel** | Groups readiness and runtime outcomes by recovery: configure Provider, fix policy, wait for approval, retry posting, inspect audit, start a new call. Per-item state uses `FluentBadge`; `FluentMessageBar` is page-level notice only. Shows per-tenant counts of calls blocked by context policy, safety, cost cap, and capacity. Shows per-Conversation `safety blocked` history, so a permanently blocked Conversation is visible. Shows cost-cap consumption and failure records. A deferred metric renders `{colors.status-subtle}` with a whole-string label, never an empty cell. | Failure record fields per § Generation failure record. |
| **audit-evidence-panel** | Support-safe evidence: caller, Agent, Source Conversation, Provider/model, response mode, computed Safe Context Budget, measured context size, `CapabilityVersion`, context mode, safety decisions at each of the four points, versions, editing Party per edited version, approving Party and policy basis per disclosure category, projection references, timestamps, posting outcome, final Conversation Message. Evidence containing Conversation-derived content requires current Conversation read access and fails closed otherwise. Named authority is the Conversation Facilitator where that source applied. | Per-item state uses `FluentBadge`. |
| **high-impact-confirmation** | Focus-trapped `FluentDialog` for the nine high-risk families. Body is one localized string naming the action, the resource identity, the future-only effect, and the authorization basis. For proposal approval it renders the selected `VersionId`, kind, author, timestamp, and full content, and states the safety re-check. For pricing it renders old to new values and the effective pricing version. For Provider disable it lists affected Agents. Initial focus on the non-committing action; Enter on the body never commits; the destructive action is never the default button; `Esc` cancels. If type-to-confirm is used for deletion, the WCAG 3.3.7 security exception is recorded. | Events: confirm, cancel. Returns focus to the trigger or, if the trigger is gone, per the deterministic focus rule in § High-risk pending commands. |
| **pending-command-indicator** | Rendered on the activated control and the item's status region while a high-risk command is pending; the control becomes `aria-disabled="true"` and keeps focus. Sequence, lock scope, session, and timeout rules are in § High-risk pending commands and § Projection catch-up contract. Never Success. | Variants: same session; `pending in another session`. |

## State Patterns

Canonical states are used consistently. PascalCase names are contract tokens; lowercase names are UX states that have no contract token yet.

Contracts that must grow:

- `AgentReadinessStatus`: `ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved`.
- `AgentCallOperationStatus`: `SafetyBlocked`, `BudgetBlocked`, `CapacityQueued`, `CapacityRejected`, `UnknownOutcome` (Stories 6.3, 6.4, 6.5, 6.7).
- `ProviderReadinessResult`: published by Story 5.5.

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
| Nudge | A SignalR projection nudge triggers an immediate poll and never replaces polling. |
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
| `authoritative pending` | Accepted identity plus projection reference | Follow the authoritative status projection. |
| `authorized` | `Authorized` | Continue to membership and context build. |
| `denied` | `Denied` | Stop before Provider invocation; safe reason; `{colors.status-danger}`. |
| `context loading` | `ContextLoading` | Progress; no Provider yet. |
| `context blocked` | `ContextBlocked` | Fail closed before Provider invocation; copy names the Safe Context Budget; no proposal or message. |
| `safety blocked` | `SafetyBlocked` (to add) | Pre-Provider or pre-side-effect block; `{colors.status-severe}`; audit records the safety outcome. |
| `budget blocked` | `BudgetBlocked` (to add) | Cap reached or missing pricing/budget state; `{colors.status-severe}`; recovery via Cost controls. |
| `capacity queued` | `CapacityQueued` (to add) | Progress with queue position where safe; `{colors.status-informative}`. |
| `capacity rejected` | `CapacityRejected` (to add) | `{colors.status-severe}`; recovery is a new call. |
| `generating` | `Generating` | In flight; no message or proposal. |
| `generation failed` | `GenerationFailed` | No proposal, no message; separate failure record. |
| `generated` | `Generated` | In Automatic Response Mode the call proceeds to posting; in Confirmation Response Mode a `Pending` proposal is created. |
| `unknown outcome` | `UnknownOutcome` (to add) | Typed terminal `Unknown` (FR-12); never retried; reservation held then conservatively settled; `{colors.status-important}`. |
| `projection-confirmed terminal` | Authoritative terminal projection | Render the exact outcome; only `Posted` proves a Conversation Message. |

### Proposal lifecycle

Aligned to `ProposedAgentReplyState`. The PRD names the failed-post state `PostFailed`; the contract name `PostingFailed` is used here. The initial generated version is the first `Pending` version, not a separate state.

| State | Terminal | Treatment |
|---|---|---|
| `Pending` | No | Queue-visible; notification-eligible; `{colors.status-informative}`. |
| `Edited` | No | New preserved version with editor Party and timestamp; prior versions visible. |
| `Regenerated` | No | New generated version under fresh gates; counts toward the ceiling; prior versions visible. |
| `Approved` | No | Progress; a version is selected; posting not implied. |
| `PostingPending` | No | Progress; not yet a Conversation Message. |
| `Posted` | Yes | `{colors.status-success}`; link evidence to the posted message. |
| `PostingFailed` | No | `{colors.status-danger}`; typed reason; bounded audited `retry posting` reusing the deterministic `MessageId`; once retry is exhausted the sole action is a new Agent Call. |
| `Rejected` | Yes | Versions and evidence preserved. |
| `Abandoned` | Yes | Versions preserved; reason shown (Approver, policy, Agent removed from Conversation). |
| `Expired` | Yes | Rendered whenever `ExpiresAt` has passed on any read, regardless of timer delivery; sole action is a new Agent Call. |

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
| Families | `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, `DeletionRequest`, `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`. |
| Lock scope | Advisory, per (session, resource identity, operation family). Unrelated resources and families proceed. |
| Session | Authenticated user plus browser tab or circuit. |
| Start and clear | Begins on submission; clears only on authoritative rejection before acceptance or a terminal result; `AuthoritativePending` keeps the lock held. |
| Sequence | The activated control becomes `pending-command-indicator` with `aria-disabled="true"` and keeps focus; it shows `Submitted`, then `AuthoritativePending` with projection reference, then `awaiting projection` on catch-up exhaustion with a refresh action. |
| Focus after resolution (the deterministic focus rule) | The same item's status region, then the next row, then the page heading. |
| Reload | Pending state is re-derived from the authoritative status projection and the lock re-armed. |
| Second tab | Renders `pending in another session` with resolution controls `aria-disabled`. |
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

Every grid or list distinguishes loading, empty, filtered-empty (with `FcFilterResetButton`), error, permission-denied, and `catching up`. Empty never leaks unauthorized records. Unauthorized, foreign-tenant, and non-existent ids render one `not available` state with identical copy, status class, and timing. Search and filter suggestions return only records in tenant scope and readable by the requester. Agents registers no command palette entries in V1.

On `connection lost`, the status region shows a notice, pending items stay pending, **Call hexa** and the nine high-risk submits become `aria-disabled` with the reason, and on reconnect the lock is re-evaluated from authoritative status with focus preserved.

### Audit availability

| State (`AuditAvailabilityStatus`) | Treatment |
|---|---|
| `AuditPending` | Expected, not available; never Success. |
| `AuditAvailable` | Queryable and linked. |
| `AuditDelayed` | Late; wait, retry, escalate. |
| `AuditUnavailable` | Cannot load; safe reference and recovery. |

## Interaction Primitives

- FrontComposer shell shortcuts and palette remain available. Agents registers no palette commands and no domain shortcuts in V1.
- Grids follow the mandatory Fc component list in the `provider-catalog-grid` row.
- High-risk actions follow § High-risk pending commands: `high-impact-confirmation`, then `pending-command-indicator`, then the deterministic focus rule.
- Editing a proposal is explicit; regeneration is a distinct action; approval applies to the selected version only.
- `Esc` closes transient UI without committing and never discards editor content. Focus returns to the trigger, or follows the deterministic focus rule when the trigger is gone.
- Hover-revealed row actions also render on row focus-within and are dismissible; tooltips are hoverable and `Esc`-dismissible. No required action or reason is hover-only.
- Accordion use and primary regions per surface are in `DESIGN.md § Layout & Spacing`.
- Policy, cap, mode, expiry, and ceiling changes state their future-only effect in the confirmation.

## Accessibility Floor

WCAG 2.2 AA is the binding behavioral floor across every interactive V1 route and high-impact state. Contrast and icon pairing live in `DESIGN.md`.

Live regions: Agents owns one page-level polite `role="status"` node and one `role="alert"` node per route. FrontComposer's shell live regions cover shell-owned surfaces only. The NFR-14 `LiveRegionAnnouncedTick` observes the localized mutation in the Agents node after render commit.

| Politeness | Events |
|---|---|
| `role="alert"` | denied; generation failed; posting failed; expired; safety blocked; capacity rejected; connection lost. |
| `role="status"` | authoritative pending; proposal created; approved; posting pending; posted; regenerated (`Version {n} generated` with a go-to action); context blocked; expiry warning (once per proposal); superseded by another decision; status refreshed; capacity queued; catching up. |

Route heading and focus:

- Every route renders `FcAggregateListPage` or `FcAggregateDetailPage` with a non-blank localized `Heading`, `HeadingTabIndex=-1`, and `PageTitle`. Focus moves to the heading on navigation, when proposal detail opens from the queue, and after a forced refresh (with `Status refreshed. Approval is still pending.` announced).
- Accordion titles are h2; in-panel sections are h3.
- After regeneration, focus stays on Regenerate and the status region announces the new version.

Forms:

- Every control has a visible label. Errors are associated through `aria-describedby` and `aria-invalid`; an error summary receives focus on submit and links to the first error.
- Numeric fields state units and currency in the label.
- Secret-adjacent fields are named by their configured state (`Secret reference: configured`), never by value.
- Policy-source rows have an accessible name including kind and basis.

Other criteria:

- Reflow: every route stays operable at 320 CSS px width (400% zoom), including proposal detail, version history, and `high-impact-confirmation`. Evidence at 320 px is part of `LR-UI-CONFORMANCE`.
- Target size (2.5.8): every actionable target is at least 24 by 24 CSS px in all three densities, or meets the spacing exception; Compact density is in the conformance lane.
- Focus not obscured (2.4.11): no sticky element overlaps the content scroll area, or scroll-padding equals sticky heights.
- Dragging (2.5.7): no drag-only operation.
- 3.2.6 Consistent Help: N/A, no help mechanism is repeated across pages. 3.3.8 Accessible Authentication: N/A, authentication is shell-owned.
- Reduced motion never hides generation, approval, or posting state changes.
- Secrets, raw payloads, and other-tenant data never appear in accessible names, tooltips, copied text, diagnostics, or announcements.
- Timing (2.2.1): proposal expiry is an essential governance limit set by the tenant; no in-UI extension. Product sign-off on this position is recorded as a deferred item.
- Conformance evidence: an Agents-owned axe lane over every interactive V1 route in Light, Dark, and forced-colors, plus the manual AT matrix (NVDA with Firefox, JAWS with Chrome, VoiceOver with Safari), is `LR-UI-CONFORMANCE` evidence. FrontComposer override diagnostics (HFC1050 to HFC1055) cover FrontComposer overrides only; hand-authored pages rely on the Agents lane.
- Whole-string English/French parity is required for every route and high-impact state. Missing routes, keys, live-region evidence, viewport evidence, or conditional skips make `LR-UI-CONFORMANCE` `InsufficientEvidence`.

## Inspiration & Anti-patterns

The PRD addendum positions Hexalith Agents against Slack AI, Microsoft 365 Copilot in Teams, Zoom AI Companion, and Atlassian Rovo Agents.

- Lifted: clear admin control over AI availability and caller access.
- Lifted: answering in place from Conversation context rather than in a separate AI workspace.
- Rejected: generic summarization as the product promise; this is governed participation by a named Party identity.
- Rejected: unapproved generated content in the durable Conversation record.
- Rejected: Provider and model opacity; identity is in audit evidence without secrets.

## Responsive & Platform

Desktop is primary. The product is a web admin and workflow surface, not a native app.

| Breakpoint | Behavior |
|---|---|
| Phone and 320 CSS px reflow | One column. Every route, proposal detail, version history, and `high-impact-confirmation` remain operable. Grids use `FcExpandInRowDetail` for secondary columns. |
| Tablet | Navigation collapses per FrontComposer. Grids preserve critical columns. Proposal detail stacks editor, metadata, and version history. |
| Desktop | Full-width grids; Constrained forms and editors; proposal detail stacked with accordion items. |
| Wide desktop | Extra width goes to grids, not decorative panels. |

A high-risk action is unavailable only when its required decision context cannot render, and then with a visible reason and review-only access; that is the meaning of the `LR-UI-CONFORMANCE` restrictive-viewport clause.

## FrontComposer Readiness

Cross-index of shell capabilities to the section that owns each rule.

| Capability | Dependency | Owning section |
|---|---|---|
| FC-LYT | One `FcPageLayoutMode` per route | § Information Architecture; `DESIGN.md § Layout & Spacing` |
| FC-TBL | Hand-authored `FluentDataGrid` in `FcAggregateListPage`; generated `[Projection]` lane not used | `provider-catalog-grid` row |
| FC-A11Y | Shell skip links, focus, landmarks, shell live regions; Agents owns page live regions | § Accessibility Floor |
| FC-L10N | Shell chrome strings; Agents owns `AgentsResources` | § Voice and Tone |
| Policy-gated nav | Four policy constants; hide, then deny on direct navigation | § Information Architecture |
| Pending pattern | Agents truth-flow helper; `FcPendingCommandSummary` out for V1 | § Projection catch-up contract |
| `IBadgeCountService` | Pending-proposal count with a domain-named label | `proposal-notification` row |
| `FcFluentIcons` | Sole icon source; glyph requests | `DESIGN.md § Brand & Style` |
| `EXT-CONV-UI-1` | Conversation action contribution; Uncommitted; blocks Story 6.7 | § Conversation Integration Seam |
| `FcAggregateListPage` / `FcAggregateDetailPage` | Route heading, `HeadingTabIndex=-1`, `PageTitle` | § Accessibility Floor |

## Key Flows

Each flow names its stories. UJ-1 to UJ-4 mirror the PRD of 2026-09-08.

### UJ-1 - Nora configures `hexa` for a tenant launch

Stories: 5.2, 5.3, 5.4, 5.5, 5.7.

1. Nora opens the Agents overview and sees `hexa` as `active, not proven callable` or `invalid configuration` with safe blockers.
2. She opens `hexa` configuration and confirms or links the Agent Party identity.
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
7. In Automatic Response Mode the call enters `PostingPending`; nothing is a message yet.
8. The client renders the projection whose outcome is `Posted`.
9. **Climax:** the response appears as durable Conversation content attributed to `hexa`, with `agent-response-marker`; only this proof is Success.
10. Resolution: participants continue the Conversation.

Failure paths: Milan lacks permission, so the action is absent or `denied` before Provider invocation. Context exceeds the budget, so `context blocked`. Safety, budget, or capacity blocks, each distinct and non-Success. Generation fails, so a failure record only. Provider outcome unknown, so `unknown outcome` with the reservation held.

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
10. Resolution: the Conversation holds only the approved response; history preserves every version, the editing Party of any edit, and the policy basis per disclosure category.

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
