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
  - ./reconcile-validation-2026-09-09-2.md
---

# Hexalith Agents - Experience Spine

> Final spine reconciled to `prd.md` as of its 2026-09-09 revision (FR-33, OQ-20 to OQ-23, A-9 to A-13, A-17, and the in-place amendments to OQ-3, OQ-6, OQ-9, OQ-11, OQ-14, OQ-16 and OQ-18), the 2026-09-09 UX re-validation and its post-Update re-validation, the approved sprint change proposal of 2026-09-09, and the shipped Story 5.3 code. `DESIGN.md` owns visuals; this file owns behavior, surfaces, states, accessibility, and flows. The spines win on conflict with mockups, wireframes, imports, sketches, and shipped code. Decisions are logged in `.memlog.md`; the current reconciliation, dispositions, and deferred items are in `reconcile-validation-2026-09-09.md`, its predecessor in `reconcile-validation-2026-09-08.md`, and the 2026-08-02 closure set remains in force except where a later reconciliation records a revision.

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

## Authorization roles

Every authorization rule in this spine resolves to one of the six PRD FR-33 roles at a stated scope. FR-33 is the authoritative matrix (OQ-21); this section binds each role to the policy constant a route or control is gated on, and nothing in this spine grants an operation FR-33 withholds.

| FR-33 role | Scope | Policy constant | Resolved by |
|---|---|---|---|
| Platform Operator | Platform | `Agents.PlatformOperator` | Platform role assignment; never tenant-scoped |
| Tenant Agent Administrator | Tenant | `Agents.Administrator` | Tenant role assignment |
| Release Operator | Tenant | `Agents.Operator` | Tenant role assignment |
| Approver | Proposal | `Agents.Approver` plus per-proposal resolution under FR-7 | The proposal's Approver Policy snapshot; the constant alone authorizes no proposal action |
| Compliance Inspector | Tenant | `Agents.AuditOperator` | Tenant role assignment |
| Conversation Participant | Conversation | none | Conversation membership and current read access through the Conversations seam, plus the Agent call permission below |

Two authorities are not Agents roles and never become policy constants. The **Conversation Facilitator** is a Conversations role resolved normatively from `ParticipantRole.Facilitator` (A-3) and is an authority on two rows only: Approver Policy resolution (FR-7) and removing, blocking, or re-admitting `hexa` (FR-2, A-9). **Security approval** is a rendered acceptance stage on `PolicyPublication` (FR-26), not a role.

The Compliance Inspector role is carried on the stable policy identifier `Agents.AuditOperator` and labelled Compliance Inspector on every surface, exactly as `ApproverPolicySourceKind.ConversationOwner` is labelled Conversation Facilitator. AD-30 is the naming authority for every policy constant and maps the six FR-33 roles onto five of them; `prd.md` names the roles and no constants. Renaming the identifier requires an AD-30 amendment, not a UX decision.

Rules:

- A missing, stale, or unavailable role assignment fails closed and never falls back to a more permissive row.
- Every role except Platform Operator is tenant-scoped: an assignment in one tenant grants nothing in another (FR-19).
- Every governed write renders its **authorization basis** — the FR-33 role and scope it was permitted on — in its confirmation, and the audit record carries the same basis (FR-33, FR-20). § Confirmation contents by family states this once as a required content of every family.
- **Agent call permission.** Every Conversation Participant may call `hexa` where it is active, unless the Tenant Agent Administrator restricts calling to a tenant role (A-11). That restriction is the Agent call permission that FR-8 and FR-20 enforce, and it is a field on `agent-config-form`. The server decision is authoritative.
- Constants to be registered in `AgentsFrontComposerRegistration` (AD-30): `Agents.PlatformOperator` is platform-scoped; `Agents.Administrator`, `Agents.Operator`, `Agents.Approver`, and `Agents.AuditOperator` are tenant-scoped. The shipped registration declares four of the five and is missing only `Agents.PlatformOperator`; § Shipped-code corrections carries that one divergence.
- The rows carrying an `[ASSUMPTION]` key in FR-33 — A-9 removal authority, A-10 per-tenant Provider enablement, A-11 call permission, A-12 deletion approval — are unretired Product assumptions, so each is an `RQ-1` blocker of type `UnretiredAssumption` that `launch-readiness-panel` renders (FR-28).

## Information Architecture

FrontComposer shell navigation registers an **Agents** domain. Every surface below is bound to a route, a nav order, an authorization policy constant from `AgentsFrontComposerRegistration`, one `FcPageLayoutMode`, an owning story in Epics 5 to 8, and a read/write contract. Field inventories live in the Component Patterns rows. The provider surface is the PRD's Global Providers Aggregate, the architecture's `ProviderCatalog`, and this spine's Provider catalog; the three names denote one thing.

| Surface | Route | Nav order | RequiredPolicy | Layout | Owning story | Read/write contract | Purpose |
|---|---|---|---|---|---|---|---|
| Agents overview | `/agents` | 0 | `Agents.Administrator` | FullWidth | 5.2, 5.5, 5.7 | `AgentInspectionResult`; Story 5.5 setup/callability result; pending count via `IBadgeCountService` | See lifecycle, proven callability, pending count, recent failures, and safe blockers at one `RegistryRevision`. |
| `hexa` configuration | `/agents/configuration` | 1 | `Agents.Administrator` | Constrained | 5.2, 5.7 | `AgentSetupResult`; `AgentCommandAcceptance` (FR-29 stages) | Configure `hexa` and activate it once blockers clear. |
| Provider catalog | `/agents/providers` | 2 | Tenant read `Agents.Administrator`, filtered to models enabled for the tenant; full-catalog read and every `ProviderCatalogMutation` `Agents.PlatformOperator` | FullWidth | 5.3, 5.5 | `ProviderCatalogEntryView`; `ProviderCatalogCommandAcceptance`; `ProviderReadinessResult` (Story 5.5) | Govern Provider/model records, pricing, and readiness without secrets. |
| Approver policy | `/agents/approver-policy` | 3 | `Agents.Administrator` | Constrained | 5.4 | `AgentApproverPolicy`; `ApproverPolicySourceKind` (closed list) | Define who may resolve proposals and how each source is disclosed. |
| Conversation context policy | `/agents/context-policy` | 4 | `Agents.Administrator` | Constrained | 6.2 | Read model defined by Story 6.2 from `AgentInteractionContextPolicy` | Read the effective context rule and Safe Context Budget. |
| Content safety policy | `/agents/content-safety` | 5 | Stricter tenant restrictions `Agents.Administrator`; platform policy publication `Agents.PlatformOperator` with a Security approval stage | Constrained | 6.3, 8.4 | Platform `ContentSafetyPolicy` plus stricter tenant restrictions in `TenantGovernancePolicy`; platform and tenant publish commands by Story 8.4 | Validate and publish the applicable versioned platform/tenant safety policy pair. |
| Cost controls | `/agents/cost-controls` | 6 | Caps and rate limits `Agents.PlatformOperator` or `Agents.Operator`; `Agents.Administrator` may only lower them; the audited override `Agents.PlatformOperator` only | Constrained | 6.4, 8.4 | Budget policy contracts defined by Story 8.4 (FR-32) | Configure caps and rate limits; inspect consumption and overrides. |
| Proposal queue | `/agents/proposals` | 7 | `Agents.Approver` plus FR-7 resolution for each listed proposal | FullWidth | 7.1 | `PendingProposalsResult` | Discover proposals the Approver may currently read. |
| Proposal detail/editor | `/agents/proposals/{AgentInteractionId}` | none | `Agents.Approver` plus FR-7 resolution; administrative abandon and administrative retry of `PostingFailed` `Agents.Administrator`, audited, no Conversation read access required | Constrained | 7.2 to 7.6 | `ProposalDetailView`; `ProposalVersionSummary` | Review and resolve one proposal. |
| Operational status | `/agents/status` | 8 | `Agents.Administrator`, `Agents.Operator`, or `Agents.PlatformOperator`; a caller sees the outcome of their own Agent Calls only | FullWidth | 6.7, 8.7 | `AgentOperationalStatusSummaryView`; failure record fields by Story 6.1 | Distinguish readiness, blocked calls, failures, and posting outcomes by recovery. |
| Launch readiness | `/agents/launch-readiness` | 9 | `Agents.Operator`; the per-tenant kill switch `Agents.PlatformOperator`, or `Agents.Operator` on the recorded FR-28 trigger conditions | FullWidth | 5.5, 8.7 | `launch-readiness` projection checkpoint (Story 8.7); the built `AgentLaunchReadinessView` is superseded evidence | Inspect every gate record and blocker at one checkpoint, and pull or release the per-tenant kill switch. |
| Audit evidence | `/agents/audit`; `/agents/proposals/{AgentInteractionId}/audit` | 10 | Operational and posted-provenance evidence `Agents.Administrator`, `Agents.Operator`, or `Agents.PlatformOperator`; posted provenance also to a current Participant with read access; unposted protected content Eligible Approver authority or `Agents.AuditOperator` | List FullWidth; detail Constrained | 7.1 to 7.6, 8.1 to 8.3, 8.8 | `AuditEvidenceResult`; `AuditAvailabilityStatus`; `AuditInspectionResult` | Inspect support-safe evidence end to end. `/agents/audit` is an id-entry surface, not a grid: it accepts an interaction, proposal, governance-operation, Conversation, or inspection-case reference and routes to the authorized detail, so it declares no list contract, columns, filters, or sort and the mandatory FC-TBL set does not apply to it. Unposted protected content requires Eligible Approver authority or the governed Story 8.8 compliance-inspection flow. Because it is a form rather than a grid, it is specified under the Forms rules and not left as one unlabeled text box: a visible label naming the five accepted reference kinds, an `aria-describedby` format hint, one localized whole string per validation fault, and an unresolvable, unauthorized, or foreign-tenant reference rendering the two-key `not available` content inline with focus moved to the heading and the outcome announced by the polite node. |
| Audit governance | `/agents/audit-governance` | 11 | Legal hold and export `Agents.AuditOperator`; deletion submitted by `Agents.PlatformOperator` and approved by a distinct `Agents.AuditOperator` holder (A-12) | Constrained | 8.1 to 8.3 | Legal hold, export, deletion commands and progress projections by Stories 8.1 to 8.3 | Operate retention, legal hold, export, and deletion. |
| Conversation invocation | Conversation-owned; no Agents nav entry | none | Conversation access plus Agent call permission (server) | Conversation surface | 6.7 via `EXT-CONV-UI-1` | `IConversationAgentCallGateway` | Call `hexa` through the sole V1 entry, the Conversation-owned **Call hexa** action. |
| API/client contract reference | Developer docs | none | none | none | 4.1, 5.5 | Public contracts | Omar's journey; not a FrontComposer screen. |

Rules:

- `/agents/conversation-call` is a pre-integration harness, not an alternate entry point. It is delisted from navigation now. **Story 6.7 removes it: the `Order: 4` "Conversation call" entry in `AgentsFrontComposerRegistration.cs` is unregistered and `Components/Pages/ConversationCall.razor` is deleted before 6.7 closes**, because `AlternateInvocationGuardTests` must prove absence and a merely blocked route is still an `[Authorize]` surface. While it exists it carries `Agents.Administrator`, is excluded from `LR-UI-CONFORMANCE`, and writes nothing in a production-like `EnvironmentProfile`. The terminal-state `Start a new Agent Call` action navigates to the Source Conversation, never to the harness.
- Every `RequiredPolicy` cell above is derived from the FR-33 matrix row for that operation; the role-to-constant binding and its rules are stated once in § Authorization roles. Where a surface splits read from write, the narrower constant governs the write and the mutation controls are **absent**, not disabled, for a reader who lacks it. Showing a cross-tenant blast-radius count in a confirmation is disclosure control, not authorization, and never substitutes for the split (FR-19).
- Navigation hiding is disclosure, not authorization. Unauthorized entries are hidden. Direct navigation to an unauthorized, foreign-tenant, or non-existent id renders the single `not available` state defined under State Patterns. Every page also carries `[Authorize(Policy = ...)]` and the server decision is authoritative.
- Nav glyphs come only from `FcFluentIcons`. An unset or unresolvable icon key is **not** a no-glyph path: `FrontComposerNavigation.ResolveNavEntryIcon` falls back to `Apps20`, which is the Agents overview glyph, so an entry awaiting a request renders a duplicate of Overview rather than nothing. The six affected entries, the accepted duplication, and the outstanding requests are listed in `DESIGN.md § Brand & Style`.

Two governed operations FR-33 names have no route of their own and are carried as controls on existing surfaces: the **per-tenant kill switch** (FR-28) on Launch readiness, and **removal, block, and re-admit of `hexa` in a Conversation** (FR-2, OQ-16, A-9) on Operational status, beside that Conversation's `safety blocked` history. Both are `high-impact-confirmation` families with a required justification, and every set and clear is audited.

Surface closure status: **final for V1**. Every stated need has a surface and every surface has a journey below; the two operations above are controls, not routes, by decision.

## Conversation Integration Seam

**Call hexa** lives inside a Hexalith.Conversations surface. Conversations is a separate module; the seam is a named external dependency, not an Agents route.

| Element | Binding |
|---|---|
| Conversations-side extension point | `EXT-CONV-UI-1`: a versioned Conversation contribution and registration contract with tenant-scoped authorization and typed registration failure. It carries **three artifact kinds**: an action contribution (the **Call hexa** trigger); a per-message decoration slot keyed by `MessageId` (the `agent-response-marker`); and a **persistent Agents-owned status region** contributed beside the trigger. The third exists because the panel's own live nodes and status rows are destroyed by the Submit that closes the dialog, so without a region whose lifetime is the Conversation view rather than the dialog, every outcome of the product's only V1 invocation path — `denied`, `generation failed`, `capacity rejected`, `Posted` — would announce into a removed node and disappear visually too. The seam must also supply a **focus-return contract** for the Conversations-owned trigger, because the panel cannot focus a foreign component without one. Status `Uncommitted`, owner `TBD` in `external-dependency-register.md`. |
| Agents-side artifacts | The exported `ConversationAgentCallPanel` component, which is the shipped name of the `conversation-agent-call` component; `IConversationAgentCallGateway`; and an Agents-side provenance accessor keyed by `MessageId` supplying posted-by-`hexa`, generated versus human-edited, and the editing Party. |
| Gateway surface | `RequestCallAsync` and `GetCallStatusAsync` exist today. The seam must add `GetCallabilityAsync(tenant, conversation)` returning the Story 5.5 readiness result and the safe blocker, because the Conversations-owned **Call hexa** button cannot render its `aria-disabled` pre-state without it. |
| Ownership split | Conversations owns the **Call hexa** trigger button and its placement, and hosts the two contributed slots. Agents owns the dialog body, which is `ConversationAgentCallPanel`, and the persistent status region beside the trigger. The panel announces only what happens **while it is open**; every post-submit outcome and every status badge belongs to the persistent region, which renders both live nodes empty from first paint and mutates only their text. |
| Consuming story | 6.7. Blocked from `ready-for-dev` while `EXT-CONV-UI-1` is `Uncommitted` (FR-21). Two corrections are outstanding and are treated as blocking, not clerical, because two of the three documents a sprint planner reads currently say 6.7 needs no seam: `epics.md` § Story 6.7 still reads "No new external seam", and the register's `ConsumingStories` is still `TBD`. The register's `RequiredArtifact` additionally specifies a *narrower and incompatible* seam — provenance rendered by Conversations from message metadata, which is `EXT-CONV-AI-1`'s posting seam — and names neither the decoration slot, the status region, nor `GetCallabilityAsync`; a Conversations maintainer accepting those fields would deliver something Story 6.7 cannot consume. The register must be amended to the three artifact kinds above, or this spine changed to the metadata-carried design and the Agents-side accessor dropped; both must not stand. |
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
| **approver-policy-builder** | Builds proposal authority from the three buildable `ApproverPolicySourceKind` values, each row declaring its disclosure category. Full rules in § Approver policy rules. | See § Approver policy rules. |
| **provider-catalog-grid** | Hand-authored `FluentDataGrid` over the platform-scoped Provider/model record, with a scoped read and a scoped write: a tenant reader sees only tenant-enabled models and never configured state or the secret reference. Full rules in § Provider catalog rules. | See § Provider catalog rules. |
| **proposal-queue-grid** | Hand-authored `FluentDataGrid` of proposals the viewer was resolved as an Approver on, under the FR-13 disclosure predicate. Full rules in § Proposal queue rules. | See § Proposal queue rules. |
| **proposal-editor** | The one surface where a proposal is resolved: view a selected version, edit, regenerate, approve, reject, or abandon it. Availability by state is in § Proposal editor action rail. Full rules in § Proposal editor rules. | See § Proposal editor rules. |
| **version-history** | Lists every generated, edited, and regenerated version with kind, author or Provider/model, timestamp, safety outcome, approval and posting markers. Selection is a radio group labeled by kind, author, and timestamp. V1 has no diff view: one version is viewed at a time. `DisabledFocusable` is declared on `FluentButton` and `FluentCompoundButton` only, not on `FluentRadio`, so the radios are **never disabled**. Dirtiness is handled at the selection event instead: changing the selected version while the editor is dirty opens the unsaved-changes confirmation offering Save, Discard, or Cancel, and the selection moves only after Save or Discard resolves. This keeps the native radiogroup keyboard contract — where the arrow key *is* the selection — intact, and it is the one exception to the rule that a confirmation is never opened by a selection event: the confirmation here protects typed content rather than committing a governed write, so it carries no family and no justification. On a selection change the textarea's accessible name becomes the version identity and the status node announces `Showing version {n}.` Approval decisions show the policy basis rendered per its FR-7 disclosure category; redacted and omitted show a category label, never the source. Failure records are not versions. | Inputs: `ProposalVersionSummary` list, `ApprovedVersionId`, `SelectedVersionId`. |
| **conversation-agent-call** | Shipped as the exported `ConversationAgentCallPanel`; the two names denote one artifact. Conversations owns the **Call hexa** trigger, Agents owns the dialog body. The action opens a `FluentDialog` naming `hexa` and the effective response mode. Prompt required; no client-side length limit because the Safe Context Budget is server-owned. Captures Source Conversation, caller, Agent, prompt, effective response mode, authorization decision, timestamp. Visible but `DisabledFocusable` with the safe blocker when `hexa` is not callable, resolved through the seam's `GetCallabilityAsync`. The panel owns one polite `role="status"` node and one `role="alert"` node under the same politeness table as an Agents route, placed after Submit in DOM order. On Submit the dialog closes, focus returns to **Call hexa**, and that button is `DisabledFocusable` while the call is pending; `submitted`, `authoritative pending`, `denied`, `capacity queued`, `generation failed`, `PostingPending`, and `Posted` each announce through those nodes. Because the prompt textarea holds typed content, `Esc` confirms before discarding. | States: `submitted`, `authoritative pending`, then the states in § Agent call. Duplicate submission is blocked per session, resource, and family. |
| **agent-response-marker** | Contributed through the `EXT-CONV-UI-1` per-message decoration slot for every posted Agent Response, with provenance from the Agents-side accessor keyed by `MessageId`; Story 6.7's action contribution cannot carry per-message attribution. States AI-generated from Conversation Context and not human-verified (FR-11); adds human-edited provenance and editing Party where disclosure allows (FR-17). Never appears on unapproved content. | Variants: generated; human-edited. |
| **conversation-context-policy-panel** | Read-only. Shows policy version, Safe Context Budget components (model input limit at the applicable `CapabilityVersion`, reserved output allowance, Agent Instructions size, configured safety margin), tokenizer or named approximation, declared Approved Bounded Context Behaviors (none in V1), and the safe reason an oversized call fails closed. Offers no truncation, summarization, windowing, or retrieval control. | Inputs: Story 6.2 read model. |
| **content-safety-policy-editor** | Shows the fixed always-blocked categories from FR-26 and OQ-9 as a read-only list: child sexual abuse or exploitation; credible threats or instructions for imminent serious harm; encouragement or instruction for suicide or self-harm; credential theft, malware, or unauthorized compromise; secrets and private credentials; cross-tenant or unauthorized personal or Conversation data; control-bypass attempts; impersonation of a named Party or assertion of a decision on a Party's behalf. Restricted categories (hate, harassment, sexual, violent, illegal activity, sensitive personal) are editable only with an explicitly permitted tenant use case and Confirmation Response Mode. Each restricted row shows the effective response mode beside it, so an operator can see that permitting a category under Automatic Response Mode has no effect. Two surfaces in one route (FR-26, FR-33). **Platform policy publication** is `Agents.PlatformOperator` **with a rendered Security approval stage** — a second, separately authorized acceptance stage in the `PolicyPublication` pending sequence, never a checkbox the publisher ticks. A **tenant** holding `Agents.Administrator` may publish only a *stricter* mode-specific delta: its editor cannot move a category from blocked to restricted or from restricted to permitted, and the confirmation states which direction the change moves and refuses to render a relaxation on the tenant surface. Permitting a restricted category is a relaxation and is therefore a platform act. Both validate a draft version, then publish a future-only version through `high-impact-confirmation` (`PolicyPublication`). Copy states that approval-time and pre-post re-checks always use the then-current policy and that a re-check can only tighten. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. |
| **cost-control-editor** | Two authorization tiers on one surface (FR-32, FR-33). A **Platform or Release Operator** authors per-tenant monthly and per-call caps (`FluentNumberInput` with units) and per-Party and per-Conversation rate limits over a stated rolling window, with no implicit defaults; an unconfigured value is a readiness blocker. A **Tenant Agent Administrator may only lower** any of them for their own tenant and never raise one, so the boundary is not self-set: the input rejects any value above the current one with a typed localized reason, and the confirmation states that the boundary cannot be raised from this surface. The **audited override of a reached cap is `Agents.PlatformOperator` only and never the Tenant Agent Administrator**; for every other role the override control is absent, not disabled. The **tenant budget currency set here is authoritative** and the catalog validates only ISO 4217 well-formedness against it. Changing the budget currency after settled spend exists is a `TenantBudgetUpdate` whose confirmation states that settled spend is not converted. Shows consumption, reserved versus settled spend, and held `Unknown` reservations. The 80% warning and the 100% fail-closed state are evaluated on settled usage plus outstanding reservations, and both figures are labeled. That override is `TenantBudgetUpdate` (actor, justification, scope, expiry) and is bounded: it carries a numeric ceiling and an expiry, both rendered in its confirmation, and while it is active Operational status shows it with remaining amount and remaining time. Changes apply to future calls only. | Stages: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`. Consumption evidence lists live on Operational status. |
| **launch-readiness-panel** | Reads one `launch-readiness` projection checkpoint and renders every gate record and blocker, and carries the per-tenant kill switch. Full rules in § Launch readiness rules. | See § Launch readiness rules. |
| **audit-governance-panel** | The callable FR-30 governance operations: legal hold, export, and deletion, each justification-gated. Full rules in § Audit governance rules. | See § Audit governance rules. |
| **proposal-notification** | In-product only. `IBadgeCountService` is **not** used in V1: it is `Type`-keyed over projection runtime types and shell-rendered as an unlabeled `FluentBadge`, and Agents runs no generated projection lane. The count is domain-rendered inside the Agents overview link as the whole string `{count} proposals pending approval`, and the shell nav count stays suppressed rather than emitting a bare number. A localizable label slot on the shell badge is a FrontComposer request and a deferred item. The suppression is **by construction, not by configuration**, and that is a standing constraint: the service is registered `AddScoped` with a required consumer and exposes no opt-out, `FrontComposerNavigation` exposes no `[Parameter]` at all, and the nav badge renders as a bare `FluentBadge` with no label slot. The count stays hidden only because Agents declares no `[ProjectionRole(ProjectionRole.ActionQueue)]` projection, which is what the reflection catalog enumerates, leaving the `count > 0` gate closed. The day any Agents projection is marked `ActionQueue`, an unlabeled number appears in the rail and cannot be turned off. Counts only proposals matching `needs my action` for the viewer. No content, never implies posting. | Variants: zero (no badge), count. Because zero renders no badge, the plural inventory is `.One` and `.Other` only; there is no `.Zero` key, so the parity gate must not demand a string nothing can render. |
| **operational-status-panel** | Groups readiness and runtime outcomes by recovery, and carries the per-Conversation block and re-admit controls. Full rules in § Operational status rules. | See § Operational status rules. |
| **audit-evidence-panel** | Renders support-safe evidence and configuration-and-governance change evidence, at the two FR-24 access levels. Full rules in § Audit evidence rules. | See § Audit evidence rules. |
| **high-impact-confirmation** | Focus-trapped `FluentDialog` for the ten high-risk families, opened only by an explicit command control, never by a selection event. Body contents are in § Confirmation contents by family. Cancel carries `AutoFocus` and the domain restores focus on close, because Fluent v5 documents modal inertness and `PreventDismissOnEscape` but neither behavior; custom action buttons disable Fluent's default Enter and Escape handling, so `Esc` is wired to cancel explicitly and Enter on the body never commits. The destructive action is never the default button. The body is a `FluentDialogBody` with `FixedHeaderFooter="true"` and a focusable, accessibly named scroll region, so Confirm and Cancel stay reachable at 320 CSS px even when the body renders a full proposal version. **There is no type-to-confirm**, for deletion or anything else. The `DeletionRequest` confirmation instead carries scope, counts, hold state, and a required justification (WCAG 3.3.7). | Events: confirm, cancel. Returns focus to the trigger or, if the trigger is gone, per the deterministic focus rule in § High-risk pending commands. |
| **pending-command-indicator** | Rendered on the activated control and the item's status cell while a high-risk command is pending; the control binds `FluentButton.DisabledFocusable="true"`, which is the v5 binding that emits `aria-disabled` while keeping focus. `Disabled` and `Loading` are forbidden on an activated control: both remove focusability and would reintroduce the focus-loss defect this pattern exists to prevent. Sequence, lock scope, session, and timeout rules are in § High-risk pending commands and § Projection catch-up contract. Never Success. | Variants: same session; `pending in another session`. |

### Grid rules

These govern the two interactive Agents grids — Provider catalog and Proposal queue — and each component row states only its own delta. The Launch readiness gate grid is a third `FluentDataGrid`: it is read-only with no filters and no row expansion, so it takes the wrapper, `ItemKey`, status-cell, and state rules and none of the filter rules, and at 320 CSS px its surviving columns are GateId, state, and owner, with the remainder in `FcExpandInRowDetail`.

| Rule | Binding |
|---|---|
| Wrapper | Hand-authored `FluentDataGrid` inside `FcAggregateListPage`. The generated `[Projection]` lane is not used. |
| Required components (Agents-owned set) | `FcFilterEmptyState` and `FcExpandInRowDetail`. This four-name list was previously attributed to FC-TBL; the FC-TBL contract defines a frozen public surface and generated-grid envelope rules, not a usage mandate, so the set is Agents-owned. Of the envelope rules, virtualization with a density-bound `ItemSize`, stable item keys, the reserved column keys `__status`/`__search`/`__hidden`, and a detail panel outside the virtualized grid with an always-present `role="region"` carry over to a hand-authored `FluentDataGrid`. |
| `FcStatusFilterChips` | **Not used on either grid.** Its visible chip text is `HumanizeSlotName(slot)`, which is `slot.ToString()` — an unlocalized English `BadgeSlot` name for every slot on every grid — so the former condition `where its label contract allows` described an empty set and read as permission. Until a FrontComposer request for domain-keyed slots with localized labels lands, both grids render state and `needs my action` filters as Agents-owned `aria-pressed` toggle buttons inside a named `role="group"`. |
| `FcFilterResetButton` | **Not used**, and Agents owns the reset control. The shell button dispatches `FiltersResetAction(ViewKey)`, which `FilterEffects` turns into `ClearGridStateAction(viewKey)` against the shell's Fluxor `DataGridNavigationState.ViewStates` — state the Agents-owned toggles above do not write. Mandating it would ship a Reset that clears a snapshot nothing reads while the applied filters survive, leaving the required filtered-empty state unrecoverable. The Agents reset clears the toggles' own state, is announced, and returns focus to the first toggle. |
| `FcExpandInRowDetail` parameters | `ViewKey`, `HasExpanded`, `ChildContent`, and `DetailPanelAriaLabel` are all `[EditorRequired]`. `HasExpanded` is the grid's own per-row expansion state, and `DetailPanelAriaLabel` is a per-grid localized whole string carried in the localization inventory. |
| `ViewKey` | `agents.provider-catalog` and `agents.pending-proposals`. Each mandatory component takes one, view-key mismatches fail closed, and Agents inherits none from a generated lane. |
| `ItemKey` | The row's `AgentInteractionId` or catalog entry id, so a polled re-render cannot move the focused row. |
| Status cells | Visible text plus icon, never `FcStatusIcon` alone. |
| States | Loading, empty, filtered-empty, error, permission-denied, and `catching up` are distinguished; empty never leaks unauthorized records. |
| 320 CSS px | The grid scrolls inside its own container under the WCAG 1.4.10 data-table exception and never scrolls the page. Surviving columns per grid are in each component row; the rest move into `FcExpandInRowDetail`. |

### Proposal editor action rail

The rail holds Edit, **Save**, **Discard**, Regenerate, Approve, Reject, Abandon, and Retry posting. Availability is a function of state, not of policy alone. Save commits the draft as a new preserved version; Discard reverts it. Both render only while the editor is dirty, both are announced in the politeness table, and after either one focus returns to the control that was activated. Without them the dirty-state rules below would block Approve and version selection while pointing at controls that do not exist.

| Proposal state | Available actions |
|---|---|
| `Pending`, `Edited`, `Regenerated` | Edit, Save and Discard while dirty, Regenerate, Approve, Reject, Abandon. Approve is `DisabledFocusable` while the editor is dirty, or when `CanCurrentUserApproveSelectedVersion` is false. Regenerate is `DisabledFocusable` at the ceiling, while a generation is in flight, or while any resolution is pending. |
| `Approved`, `PostingPending` | Retry posting is not yet applicable; abandon where permitted; audit. Edit, Regenerate, and Approve are **absent** once `Approved` is projection-confirmed, and the editor pins `ApprovedVersionId` read-only, so no other version can be approved or posted and no edit can land while posting is pending (FR-17, FR-18). |
| `PostingFailed`, retries remaining | Retry posting; abandon where permitted; audit. |
| `PostingFailed`, retries exhausted | Abandon where permitted; audit; `Start a new Agent Call`. Two administrative exits also render, each `Agents.Administrator`, audited, justification required, and **not** requiring Conversation read access: `administrative abandon` and `administrative retry`. The proposal stays `PostingFailed` until one of the four FR-18 exits completes. |
| `Posted`, `Rejected`, `Abandoned`, `Expired` | Read-only. `Start a new Agent Call` only. |

`Start a new Agent Call` navigates to the Source Conversation. **`retry posting`** is a Conversation write and is governed as one. It sits in this rail for an `Agents.Approver` who holds current Conversation read access, under family **`ConversationPosting`**, not `ProposalResolution`: only that family's gate set requires `LR-CONVERSATIONS-MEMBERSHIP-POSTING` and `LR-SAFETY`, which a Conversation write must evaluate, and `launch-readiness-register.md § Gate Sets And Non-Circular Evaluation` forbids a consumer keeping a local subset. Where the resolution and the write differ, the confirmation's authorization-basis line names both families. Each attempt is a row in version history and in audit. The bound is **3 attempts within 15 minutes of the first failure** (A-7, Architecture-owned, not tenant-configurable); the confirmation renders attempts used of 3 and the time remaining in the window, and window expiry is a distinct exhaustion reason from attempt exhaustion. A new decision, after any exhausted or terminal outcome, requires a new Agent Call.

### Confirmation contents by family

Every `high-impact-confirmation` renders the action, resource identity, future-only effect, and authorization basis. Each family adds the following, and the rendered set is what the audit record carries.

| Family | Additional required contents |
|---|---|
| `ProposalResolution` — approve | Selected `VersionId`, version kind, author, timestamp, full content, and the approval-time safety re-check statement. |
| `ConversationPosting` — retry posting | `ApprovedVersionId`, the typed failure reason, attempts used of 3 and time remaining in the 15-minute window (A-7), and the four FR-18 pre-post re-validation checks named individually: the Source Conversation still exists and is accessible to the Agents Service Principal; the Agent is active and its Party identity valid; `hexa` is still a member of and not blocked in that Conversation; and the approved version still passes the then-current Content Safety Policy. Each failure cause has its own typed `PostingFailed` reason, so a failure moves the proposal to `PostingFailed` rather than posting stale content. |
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

### Provider catalog rules

Platform-scoped record with a **scoped read and a scoped write** (FR-33, A-10). A tenant reader holding `Agents.Administrator` sees only the Providers and models **enabled for that tenant**, and never sees configured state or `ConfigurationReferenceId`: for that reader a secret-derived readiness reason (`Unconfigured`, `SecretUnavailable`) collapses to one undifferentiated `Blocked`, because the reason vocabulary is otherwise a disclosure channel for secret-configuration state. The full platform catalog, the configured-state and secret-reference columns, per-tenant enablement, and every `ProviderCatalogMutation` require `Agents.PlatformOperator`; those controls and the Editor accordion item itself are **absent**, not disabled, for a reader without it. Per-tenant enablement is a first-class column and filter, and `agent-config-form`'s Provider selection offers only the tenant-enabled subset. Columns include enablement, per-tenant enablement, configured state (Platform Operator only), capability limits, pricing version (pinned, server-assigned, read-only), currency, unit prices, `CapabilityVersion`, readiness triple, freshness. The pricing editor has no default values. One platform-scoped model serves many tenants with many budget currencies, so the catalog validates only ISO 4217 well-formedness. The **tenant budget currency on Cost controls is authoritative**. A per-tenant mismatch renders `{colors.status-important}` on that tenant's readiness view and blocks reservations there, never on the shared catalog row. Before Story 8.4 ships the budget policy read model there is no comparison value, so currency renders as entered with no mismatch state. A zero unit price must be typed explicitly and is a **Provider readiness blocker**, not a warning: a zero-priced model reserves nothing, so it settles nothing and can never reach 80% or 100% on any cap, which makes `BudgetBlocked` unreachable for every tenant using it. It renders as `Blocked / Blocked / Unpriced`; the mutation confirmation states that setting zero disables cost enforcement for every tenant using the model and names the affected tenant count; and Cost controls renders an explicit `cost enforcement inactive for {model}` blocker on any tenant whose selected model is zero-priced. Pricing edits, enable, and disable are `ProviderCatalogMutation`. The disable confirmation lists in-scope Agents whose callability will be blocked, supplied by the Story 5.5 readiness result queried by Provider and model. That by-Provider/model query is not in Story 5.5's acceptance criteria and the disable action ships in Story 5.3, before it, so until that query lands the confirmation renders the in-scope-Agents list and cross-tenant count as a `{colors.status-subtle}` deferred metric rather than omitting them, and names the platform scope of the action. Cross-tenant impact renders as an aggregate count, or is omitted per disclosure category, never as tenant names. `CapabilityVersion` is the concurrency token; outdated or regressed submissions render `superseded by another decision`. The UI never accepts a secret value. `ConfigurationReferenceId` is a reference only (`EXT-SECRETS-1`), operator-only visible, and masked with autocomplete off when editable. It is never echoed in validation or errors and never appears in URLs, clipboard actions, or diagnostics.

Props, events, and variants: Per § Grid rules. Default sort: Provider name, then model. At 320 CSS px the surviving columns are Provider name, model, readiness, and the open action.

### Proposal queue rules

The listing predicate is FR-13's, in two parts plus an exclusion: full content for a proposal the viewer was **resolved as an Approver under that proposal's Approver Policy snapshot** and whose Source Conversation the viewer can currently read; **existence and state only** where the viewer was previously resolved but has since lost read access; and a proposal on which the viewer was **never resolved as an Approver is not listed, not counted, and not otherwise disclosed** — a deep link to it renders `not available`, and no filter suggests it. Holding `Agents.Approver` and happening to be a Participant is not resolution. **`needs my action`** means either of two things: a proposal in `Pending`, `Edited`, or `Regenerated` where the viewer is an authorized Approver who did not last edit the selected version; or a proposal in `PostingFailed` with retries remaining. A proposal another Approver has already moved to `Approved`, `Posted`, or a terminal state is neither counted nor listed under it. Filters render as Agents-owned `aria-pressed` toggles per § Grid rules. Filters: state, Agent, Source Conversation, caller, expiry. Empty and filtered-empty are distinct. Expiry renders as an absolute culture-formatted timestamp plus a static relative label refreshed at most once per minute, outside any live region. Polled re-renders never steal focus: `ItemKey` is bound, and while focus is inside the grid body a re-sort or page shift is deferred while the status node announces `{count} proposals changed. Refresh list.` with an explicit refresh action.

Props, events, and variants: Per § Grid rules. At 320 CSS px the surviving columns are state, expiry, and the open action. Default sort: nearest `ExpiresAt` first among `needs my action`, then most recent. Page size 25. Age is computed from `CreatedAt` server time.

### Approver policy rules

Builds authority from the three buildable `ApproverPolicySourceKind` values: `ConversationOwner`, labeled Conversation Facilitator and resolved from `ParticipantRole.Facilitator` (A-3); `PredefinedParty`; and `TenantRole`. **`Caller` is retired** and is not offered: FR-7 and OQ-14 put it on the FR-23 deprecate-and-reject register, because the Eligible Approver predicate excludes the caller of every proposal the source could apply to, and the server rejects it with a typed rejection wherever it is presented. A legacy policy still carrying it renders the row read-only in `{colors.status-severe}` with the typed retirement reason, and publication is blocked while it is present. Each row declares a disclosure category; the default is operator-only; a source revealing restricted-role membership cannot be user-visible. Validation rejects a source that cannot satisfy Conversation access at call time, and rejects a configuration that could never yield an Eligible Approver once the caller is excluded — precisely, a policy that names no Conversation-dependent source (Conversation Facilitator or a tenant role) **and** names fewer than two predefined Parties. A Facilitator-only policy is valid, because the Facilitator is eligible whenever the Facilitator is not the caller. The error copy states which of the two halves failed. Ambiguous or unavailable sources fail closed and block Confirmation Response Mode activation. Row order is edited with buttons or a menu, never drag-only.

Props, events, and variants: Row accessible name includes source kind and basis. Publish is `PolicyPublication`; because AD-12 snapshots the policy per interaction, publication is future-only and the confirmation states that pending proposals keep the policy version they were created under. The surface shows the count of pending proposals under each prior version, linking to the queue filtered by that policy version, so a Tenant Agent Administrator revoking an Approver can see who still holds authority.

### Proposal editor rules

Exists only after successful generation. Authorized Approvers edit, regenerate, approve a selected version, reject, or abandon. Editing creates a new preserved version with editor Party and timestamp; an edited version is never presented as generated. **Edit is `DisabledFocusable` whenever no Eligible Approver would remain after the edit, the editor and the caller both excluded** (FR-7); the reason names that a second Approver is required, not that approval is blocked. Without this check a lone Approver edits, cannot approve their own edit, and the proposal sits until `Expired`, which the audit record would report as an expiry rather than a governance refusal. Approval is `DisabledFocusable` while the editor is dirty. Approval requires the viewer to be an **Eligible Approver** for the selected version, which is one predicate with five conjuncts (PRD §3, FR-7): resolved by this proposal's Approver Policy snapshot; a current Participant of the Source Conversation; holding current read access to it; **not the caller of the Agent Call**; and not the Party that last edited the version under decision. A caller can therefore never approve their own Agent Call, whether or not a second Approver exists. Approval is `DisabledFocusable` when any conjunct fails, and the server applies the same predicate. Story 7.4 supplies a per-version `CanCurrentUserApproveSelectedVersion` flag returning that full predicate with one typed reason per failing conjunct, and the reason is the copy rendered on the control; the UI never compares raw `EditorPartyId` or `CallerPartyId` against a current-Party accessor. Regenerate is `aria-disabled` at the regeneration ceiling with the reason, and copy states that a regeneration is a chargeable call under cost caps and rate limits. On regeneration failure the editor keeps the proposal non-terminal with prior versions intact, records a linked failure record, announces the failure in the status region, and keeps approve, reject, and abandon available. Approval re-runs the then-current Content Safety Policy on the exact selected version. Which actions are available in which state is in § Proposal editor action rail. An Approver who lost Conversation access sees existence and state only.

Props, events, and variants: Exit is an explicit button with unsaved-changes confirmation. No client-side length limit. Regenerate while any resolution is pending is `DisabledFocusable`; regeneration is chargeable, so while one is in flight Regenerate is also `DisabledFocusable` with a local pending badge, guarding against a double Enter, without joining the ten confirmation families. Resolution actions open `high-impact-confirmation` (`ProposalResolution`).

### Operational status rules

Groups readiness and runtime outcomes by recovery: configure Provider, fix policy, wait for approval, retry posting, inspect audit, start a new call. Per-item state uses `FluentBadge`; `FluentMessageBar` is page-level notice only. Shows per-tenant counts of calls blocked by context policy, safety, cost cap, rate limit, capacity, `NoEligibleApprover`, and `RemovedInConversations` or the Agents-owned block, together with the count of **system-abandoned proposals by typed reason**, so structural unavailability is visible rather than silent (FR-25). It also shows the per-tenant count of **authorization denials** — actor, tenant, operation family, resource class, denial reason code, and timestamp, never the inaccessible resource's identity or content — because SM-4 requires zero unauthorized actions to be demonstrable and FR-28 makes a confirmed unauthorized action the immediate kill-switch trigger; a trigger nobody can observe is not a control. Shows per-Conversation `safety blocked` history, so a permanently blocked Conversation is visible; because that is Conversation-derived information and `Agents.Operator` may not be a participant, the row renders an opaque Conversation reference and a count by default, and the Conversation name or link renders only for a viewer with current read access, otherwise `existence only`. Carries the **block and re-admit controls** for `hexa` in a Conversation, beside that Conversation's history row: authority is the Tenant Agent Administrator or Conversation Facilitator (FR-2, A-9), each transition is a `high-impact-confirmation` with a required justification, and every set and clear is audited. The confirmation states that setting the block abandons non-terminal proposals for that Conversation with versions preserved, and that no join is re-established while it is set. Shows any active cost override with remaining amount and remaining time. Shows cost-cap consumption and failure records. A deferred metric renders `{colors.status-subtle}` with a whole-string label, never an empty cell.

Props, events, and variants: Failure record fields per § Generation failure record. Story 6.2 owns the read model and route for the context-policy panel, and the per-tenant blocked counts, per-Conversation safety history, cost consumption, and projection id/version on this panel have no contract yet. Both the read-model ownership and these missing contracts are deferred items for the epics skill, so ownership lands as a story acceptance criterion rather than a spine assertion.

### Launch readiness rules

Reads one `launch-readiness` projection checkpoint (Story 8.7). Renders `Pass`, `Block`, `InsufficientEvidence`, `Stale` per GateId with Owner, `SourceVersion`, `ObservedAt`, exclusive `ValidUntil`, `RequiredEvidenceLevel`, `EvidenceReference`, `BlockerCode`, `RegistryRevision`, `EnvironmentProfile`, and consumed dependency status. Blockers precede passing records. Alongside gate blockers it renders the non-GateId blocker class **`UnretiredAssumption`** (FR-28): any §8.1 assumption owned by Product, Architecture, or Governance that is not yet retired blocks `RQ-1`, and the row names the assumption id, its owner, and its retirement condition. It also renders the recorded **cost-control posture** as a field: only `Quotas`, `Budgets`, and `ProviderModelLimits` satisfy the gate, while `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` are on the FR-23 register and surface as the additive `ProhibitedCostControlPosture` blocker wherever a previously recorded posture still carries them (OQ-6). The single UX-owned NFR-14 statement: the panel shows the three sample kinds, their ticks, sample counts against the minimum, and whether the localized live-region mutation was observed; thresholds and seams are per `launch-readiness-register.md § NFR-14 Browser Monotonic Timing Contract`. Never fills a missing record, never falls back to an older `Pass`, never issues the `RQ-1` decision. Carries the **per-tenant kill switch** (FR-28): `Agents.PlatformOperator`, or `Agents.Operator` on the recorded trigger conditions, as a `high-impact-confirmation` family with a required justification. Its confirmation states the exact semantics — Agent Calls are disabled tenant-wide; proposals awaiting a decision may be rejected or abandoned **but not approved**; `Approved` and `PostingPending` proposals complete or fail on their own terms; no proposal or Audit Evidence is deleted — and names which trigger condition is being invoked. While it is pulled the panel shows it as the first blocker with the actor, justification, and time pulled.

Props, events, and variants: Inputs: Story 8.7 checkpoint.

### Audit evidence rules

Support-safe evidence: caller, Agent, Source Conversation, Provider/model, response mode, computed Safe Context Budget, measured context size, `CapabilityVersion`, context mode, safety decisions at each of the four points, versions, editing Party per edited version, approving Party and policy basis per disclosure category, projection references, timestamps, posting outcome, final Conversation Message. Every entry also carries the **tenant** it was scoped to, the **FR-33 role basis** the operation was permitted on, and the **typed terminal outcome** — accepted, rejected on concurrency, or denied on authorization — because the acceptance stages a command passed describe progress, not outcome. Conversation-derived evidence is inspectable at **two levels** (FR-24, OQ-21): posted provenance by a current Participant with read access; and unposted versions, rejected content, and context metadata by a Party resolved as an Eligible Approver for that proposal, or under a **compliance inspection** by `Agents.AuditOperator`. A compliance inspection is scoped to a named Conversation or case identifier, carries a recorded justification and either second-party approval or a declared post-hoc review by the Tenant Agent Administrator, is rate-visible on a surface the Tenant Agent Administrator can read, and is itself recorded as Audit Evidence (Story 8.8). Evidence whose Source Conversation is deleted or inaccessible remains inspectable under compliance inspection and is never rendered `not available` to that path: FR-24 states that retained evidence is never made uninspectable by the loss of its Conversation. Inspection fails closed outside these two paths. Named authority is the Conversation Facilitator where that source applied. A second evidence class, **configuration and governance change evidence**, satisfies FR-24 and FR-32 and is inspectable both at `/agents/audit` and in the History item of each write surface: actor, operation family, resource identity, old-to-new values where safe to expose (including prior and new cap values), published version, concurrency token, projection id and version, justification, and the acceptance stages the command passed. An override additionally shows its scope, ceiling, and expiry.

Props, events, and variants: Per-item state uses `FluentBadge`.

### Audit governance rules

Callable operations (FR-30): apply and release legal hold, request export, request deletion, each through `high-impact-confirmation` (`LegalHold`, `ExportRequest`, `DeletionRequest`). Per FR-30, each of these plus `PolicyPublication` and `TenantBudgetUpdate` requires a justification typed into the confirmation before Confirm becomes available; the justification is part of the audit record, and an empty or whitespace value is rejected client-side and server-side. Confirm is `DisabledFocusable` while the justification is empty or whitespace, with an `aria-describedby` reason (`Explain why this operation is required before confirming`); it is never `Disabled`, which inside a focus-trapped dialog would leave the textarea and Cancel as the only reachable controls and nothing to explain that Confirm exists or what unlocks it. The same rule governs every gated Confirm. Confirmation contents are in § Confirmation contents by family. Story 8.3 distinguishes request, approval, execute, and cancel: a Platform Operator submits deletion and a distinct Compliance Inspector must approve it before execution. Deletion is `DisabledFocusable` while that approval is absent, a matching legal hold is active, or `EXT-PROTECTION-1`/`EXT-SECRETS-1` is not `Available`; releasing a hold and requesting deletion are never the same gesture, and a hold release names the records it exposes. Export is likewise `DisabledFocusable` with `authority unresolved` treatment while either protection or secrets is unavailable; export rows show manifest state, artifact expiry, and download availability, and the panel never renders artifact contents or keys. Shows the 365-day rule, hold state, export manifest state, deletion progress including DEK destruction receipt, projection purge, workflow-state purge, and tombstone confirmation. Success only after authoritative evidence; partial failure stays restrictive and actionable.

Props, events, and variants: Progress projections by Stories 8.1 to 8.3. Evidence lists and governed inspections live on Audit evidence through Story 8.8.

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
| `no eligible approver` | `NoEligibleApprover` (to add) | Confirmation Response Mode only. The Eligible Approver set for the proposal resolved empty before any Provider work, so the call is rejected pre-Provider (FR-7). `{colors.status-severe}`; the recovery is to fix the Approver Policy, never to retry; counted per tenant and under SM-C4. |
| `kill switch active` | `KillSwitchActive` (to add) | The per-tenant kill switch is pulled (FR-28), so Agent Calls are disabled tenant-wide. `{colors.status-severe}`; no recovery from this surface; the copy names that an operator must release it. |
| `conversation blocked` | `RemovedInConversations` (to add) | The Agents-owned per-Conversation block is set, or an external removal was detected at the membership step (FR-2, OQ-16). Rejected pre-Provider with no join re-established; `{colors.status-severe}`; recovery is a re-admit by the Tenant Agent Administrator or Conversation Facilitator. |
| `context loading` | `ContextLoading` | Progress; no Provider yet. |
| `context blocked` | `ContextBlocked` | Fail closed before Provider invocation; copy names the Safe Context Budget; no proposal or message. |
| `safety blocked` | `SafetyBlocked` (to add) | Pre-Provider or pre-side-effect block; `{colors.status-severe}`; audit records the safety outcome. |
| `budget blocked` | `BudgetBlocked` (to add) | Cap reached or missing pricing/budget state; `{colors.status-severe}`. The recovery names the role that can act — a Platform or Release Operator must raise the cap, or a Platform Operator grant the audited override — never the Cost controls surface, which a Tenant Agent Administrator may only use to lower a cap (FR-32, FR-33). |
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

Aligned to `ProposedAgentReplyState`. The PRD names the failed-post state `PostFailed`; the contract name `PostingFailed` is used here. The initial generated version is the first `Pending` version, not a separate state. FR-18 names seven states and the contract enum carries ten: `Edited`, `Regenerated`, and `PostingPending` are contract values the PRD folds into their neighbors, with `Unknown` as the FR-23 sentinel that is never a recorded state. **No story is blocked by the difference: the enum governs**, both spines render all ten with a color role each, and the fold is stated here, so the Product alignment item is a documentation reconciliation and not a gate. The spine follows the enum because those three are separately renderable and separately audited. Aligning the PRD text to the contract is a deferred item with Product.

| State | Terminal | Treatment |
|---|---|---|
| `Pending` | No | Queue-visible; notification-eligible; `{colors.status-informative}`. |
| `Edited` | No | New preserved version with editor Party and timestamp; prior versions visible. |
| `Regenerated` | No | New generated version under fresh gates; counts toward the ceiling; prior versions visible. |
| `Approved` | No | Progress; a version is selected; posting not implied. Expiry no longer applies: approval freezes it. Non-terminal for posting only: once projection-confirmed, edit, regenerate, and approve are absent and `ApprovedVersionId` is pinned read-only, so no other version can be approved or posted and no edit can land while posting is pending. |
| `PostingPending` | No | Progress; not yet a Conversation Message. Expiry no longer applies. |
| `Posted` | Yes | `{colors.status-success}`; link evidence to the posted message. |
| `PostingFailed` | No | `{colors.status-danger}`; typed reason; bounded audited `retry posting` reusing the deterministic `MessageId`, at most 3 attempts within 15 minutes of the first failure (A-7, Architecture-owned). Expiry no longer applies. An exhausted proposal **remains `PostingFailed`** and always has an exit, so it always reaches the retention clock: an Eligible Approver abandons it; the Tenant Agent Administrator abandons it, audited, with no Conversation read access required; an audited administrative retry succeeds; or the system abandons it under FR-7 because the Source Conversation is gone (FR-18). |
| `Rejected` | Yes | Versions and evidence preserved. |
| `Abandoned` | Yes | Versions preserved. The reason is one of four typed values, each with a localized label: `NoEligibleApprover`, `SourceConversationUnavailable`, `RemovedInConversations`, or kill switch (FR-18). A system-abandoned proposal is excluded from the SM-3 denominator and counted separately from Approver-abandoned on Operational status (FR-25, SM-C4). |
| `Expired` | Yes | Rendered when `ExpiresAt` has passed **while `Pending`, `Edited`, or `Regenerated`**, on every read and every command, regardless of timer delivery; sole action is a new Agent Call. Approval freezes expiry, so this is never rendered over `Approved`, `PostingPending`, or `PostingFailed` (FR-18, OQ-3). The comparison uses the server time carried on the read, never the browser clock, so a skewed client neither expires a live proposal nor leaves Approve enabled past `ExpiresAt`. |

**Nearing expiry** is a rendering flag, not a state: it begins at 10% of the configured expiry window or 1 hour before `ExpiresAt`, whichever is smaller, floored at 15 minutes so a short window still leaves an actionable warning. It is computed from `ExpiresAt`, the configured window, and the server read time, adds a `{colors.status-warning}` chip to `proposal-state-badge`, and announces once per proposal per § Accessibility Floor.

Display-only outcomes (not persisted):

| Outcome | Trigger | Treatment |
|---|---|---|
| `superseded by another decision` | Concurrency rejection on any proposal or catalog action, **and** a poll-driven transition that supersedes a viewer who activated nothing | For a viewer who did not act, `aria-disabled` with the `superseded` reason wins over the action rail's absence rule, which is reserved for a fresh render. Removing a control from under a focused user drops focus to `document.body` silently, and the deterministic focus rule covers only the resolution this session submitted. On such a transition focus moves per the deterministic focus rule and the polite node announces the authoritative state. Render authoritative state, name the actor where disclosure allows, resolution controls `aria-disabled`; the status region announces the outcome; never show the rejected local action as pending. |
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
| Families | The ten families, with their required confirmation contents, are in § Confirmation contents by family. |
| Lock scope | Advisory, per (session, resource identity, operation family). Unrelated resources and families proceed. |
| Session | Authenticated user plus browser tab or circuit. |
| Start and clear | Begins on submission; clears only on authoritative rejection before acceptance or a terminal result; `AuthoritativePending` keeps the lock held. |
| Sequence | The activated control becomes `pending-command-indicator` with `DisabledFocusable="true"` and keeps focus; it shows `Submitted`, then `AuthoritativePending` with projection reference, then `awaiting projection` on catch-up exhaustion with a refresh action. |
| Focus after resolution (the deterministic focus rule) | The same item's **status cell or badge container**, given `role="group"`, `tabindex="-1"`, and an accessible name carrying the item identity and the new state; then the next row; then the page heading. `role="group"` is required because ARIA prohibits a name on `role="generic"`, so a bare `div` or `td` would have the identity-and-state string silently dropped by conforming assistive technology. The page-level live-region nodes are never a focus target and never receive `tabindex`; "status region" in this table always means the item's status cell, never a live region. |
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

Every grid or list distinguishes loading, empty, filtered-empty, error, permission-denied, and `catching up`; the filter and reset controls for the two interactive grids are Agents-owned per § Grid rules, which is where the reset control is specified. Empty never leaks unauthorized records. Unauthorized, foreign-tenant, and non-existent ids render one `not available` state with identical copy, status class, and timing, from the two `Agents.Surface.NotAvailable.*` keys.

Detail routes bind to `FcAggregateDetailState`, whose seven values do not map one to one onto this spine's states:

| `FcAggregateDetailState` | Agents rendering |
|---|---|
| `Loading` | Cold load only. |
| `Ready` | The ready body. `catching up` renders here with the freshness badge; it is a read-freshness condition, not a shell state. |
| `Stale` | **Unused.** The shell's `Stale` would collide with this spine's reserved meaning, evidence past `ValidUntil`; readiness staleness renders inside the ready body instead. |
| `Degraded` | A consumed dependency is unavailable; the degraded banner names it safely above the ready body. |
| `Unauthorized`, `NotFound`, `Unavailable` | The single `not available` content, identical in all three, supplied to each slot separately because the component renders one slot per state. |

Search and filter suggestions return only records in tenant scope and readable by the requester. Agents registers no command palette entries in V1.

On `connection lost`, the status region shows a notice, pending items stay pending, **Call hexa** and the ten high-risk submits become `DisabledFocusable` with the reason, and on reconnect the lock is re-evaluated from authoritative status with focus preserved.

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
- Wherever this spine says a control is `aria-disabled`, its **reason is programmatically associated with the control**, not merely adjacent to it: the control carries `aria-describedby` pointing at the localized whole string that states the reason, and that same string is the visible adjacent text. Visual adjacency is not an accessible description — a focusable disabled button whose reason a screen-reader user cannot reach from the control is the same dead end as an unfocusable one, reached by a longer route. This binds every blocked control in this spine: the ten confirmation families, every rail action, **Call hexa**, export, deletion, and the Provider and policy editors. `LR-UI-CONFORMANCE` asserts that every `aria-disabled` control exposes its reason through `aria-describedby`, and the AT matrix verifies it.
- Wherever this spine says a control is `aria-disabled`, the binding is `FluentButton.DisabledFocusable="true"` on a Fluent control, or the `aria-disabled` attribute with the control left focusable and its activation suppressed on a hand-authored one. `Disabled` and `Loading` are never used for a blocked-but-explicable state, because both remove the control from the tab order and take the reason with it.
- Editing a proposal is explicit; regeneration is a distinct action; approval applies to the selected version only.
- `Esc` closes transient UI without committing and never discards typed content: this covers the proposal editor and equally any non-empty text input inside a dialog, including the required **Call hexa** prompt, which confirms before discarding or keeps the draft for the session. Focus returns to the trigger, or follows the deterministic focus rule when the trigger is gone.
- Hover-revealed row actions also render on row focus-within and are dismissible; tooltips are hoverable and `Esc`-dismissible. No required action or reason is hover-only.
- Accordion use and primary regions per surface are in `DESIGN.md § Layout & Spacing`.
- Policy, cap, mode, expiry, and ceiling changes state their future-only effect in the confirmation.

## Accessibility Floor

WCAG 2.2 AA is the binding behavioral floor across every interactive V1 route and high-impact state. Contrast and icon pairing live in `DESIGN.md`.

### Live regions

**Agents owns one node of each politeness per route, and every event is announced by exactly one node.** Mandatory FrontComposer components render live regions of their own; the rule governs which node announces an event, not how many nodes the DOM holds. The carrier is a single Agents-owned `AgentsPageStatusRegion` component placed once per page body, holding both nodes; no badge, panel, or row component carries `role="status"` of its own.

- The permitted FrontComposer speakers are a **closed list**: `FcFilterEmptyState` and `FcExpandInRowDetail`, for their own filter and row-expansion events only, and Agents never duplicates those into its node. No other FrontComposer component carrying `role="status"`, `role="alert"`, or `aria-live` is used on an Agents route without being silenced. `FcStatusBadge` and `FcNewItemIndicator` are **forbidden**: the first would re-create the per-badge defect Story 8.6 removes, including on the deterministic focus target, where a `role="status"` container is the one element that must not receive focus; the second announces per new row and would defeat the aggregated `{count} proposals changed` rule. `FcFilterSummary`, `FcMaxItemsCapNotice`, `FcSlowQueryNotice`, `FcExpandedRowHiddenBanner`, `FcLifecycleWrapper`, `FcProjectionLoadingSkeleton`, and `FcProjectionEmptyPlaceholder` are silenced when used; the cap notice matters because the queue's page size is 25, which is precisely the cap case.
- `FluentMessageBar` renders with `AriaLive="Off"` whenever its text is also pushed to the Agents node.
- `connection lost` is announced by the shell's `FcProjectionConnectionStatus`; Agents announces only the domain consequence and never repeats it.
- On the Conversation surface the **persistent contributed status region** carries the pair, not the dialog: `ConversationAgentCallPanel` announces only what happens while it is open, and every post-submit outcome is announced by the region, which outlives the dialog. Both nodes render empty on first paint and only their text content mutates, because a node inserted in the same commit as its text is not announced by most assistive technology. Where the panel is hosted on an Agents route — only the `/agents/conversation-call` harness, until Story 6.7 deletes it — the panel suppresses its own pair and the route's `AgentsPageStatusRegion` speaks, so no route ever holds two nodes of one politeness.

Every **display-only outcome** listed in § Proposal lifecycle — `safety blocked at approval`, `stale proposal`, `regeneration ceiling reached`, `duplicate submission (idempotent)`, `expired at approval`, `superseded by another decision`, `authority unresolved` — is announced by the node its severity selects, because each is carried visually by an adjacent badge whose text changes, and no badge may be a live region of its own. A state change a user is waiting on is never left to a silently mutating badge beside a control whose label did not change. Any event this spine names and this table omits is a defect in the table, not a deliberate silence.

The NFR-14 `LiveRegionAnnouncedTick` observes the localized mutation in the Agents node after render commit, so a duplicate announcement corrupts the measurement as well as the experience.

| Politeness | Events |
|---|---|
| `role="alert"` | denied; generation failed; posting failed; expired (proposal detail only, or on the queue when the expiring row is the focused one); safety blocked; capacity rejected; rate limited; the domain consequence of connection loss. |
| `role="status"` | submitted; authoritative pending; proposal created; approved; posting pending; posted; regenerated (`Version {n} generated` with a go-to action); draft saved; draft discarded; version selection changed (`Showing version {n}`); response mode unchanged after cancel; context blocked; expiry warning (once per proposal); superseded by another decision; status refreshed; capacity queued; catching up; **awaiting projection** (with the refresh action named); **pending in another session**; **connection restored**; list changed after a deferred re-sort. |

Announcement volume must not grow with list length. The queue polls 25 rows sorted nearest-expiry-first, so a burst at the top of the hour must not produce 25 announcements: per-proposal expiry alerts are scoped to proposal detail, and on the queue an aggregate is announced at most once per poll cycle in the polite node, never as `role="alert"` unless the row that expired is the focused one.

### Route heading and focus

- Every route renders a non-blank localized heading with `HeadingTabIndex=-1` and a `PageTitle`, focused through `FocusHeadingAsync()`. `FcAggregateListPage` takes those as parameters. `FcAggregateDetailPage` does not: it has no heading parameters and its non-ready slots replace the body entirely. On the eight Constrained detail routes the domain therefore renders `FcPageHeader` as the first child of each of the **five real state slots** — `LoadingContent`, `UnauthorizedContent`, `NotFoundContent`, `UnavailableContent`, and `ReadyContent` — each carrying `HeadingTabIndex="-1"`, because `FocusHeadingAsync()` throws rather than no-ops when the heading is blank or the tab index is null, which would fail at the focus call in production on the error path. `StaleBanner` and `DegradedBanner` are **not slots**: they render above the ready body, which already carries the ready header, so they carry no heading and no `PageTitle`; a header added to a banner ships two `h1` elements and two competing page titles on the degraded path. The `not available` slot's heading is the route name, not `Agents.Surface.NotAvailable.Title`, which is the body copy. "Heading present and focused in the `not available` state" is part of the `LR-UI-CONFORMANCE` lane. 
- `FcAggregateDetailPage` renders a back link by default. Every detail route supplies a localized `BackLinkLabel` or sets `ShowBackLink="false"`; an unset label yields an anchor with no accessible name, and HFC1050 does not fire on framework components to catch it.
- Focus moves to the heading on navigation, when proposal detail opens from the queue, after a forced refresh (with `Status refreshed. Approval is still pending.` announced), and **whenever `FcAggregateDetailState` changes the rendered slot** — the ordinary cold load of every Constrained detail route is such a swap, as are Ready to `Unavailable` when a dependency drops and Ready to `NotFound` when access is revoked mid-session. Each slot is a separate component instance with its own `h1`, so without this rule focus falls to `document.body` silently. On the transition focus moves to the newly rendered slot's heading and the polite node announces the new state, loading excepted.
- `BackHref` is required and non-empty on every detail route — the parent list — because it defaults to `string.Empty`, which renders a named link to the current URL, outside the state slots, and therefore self-referential even on `not available`.
- Every route sets `FcPageHeader.HeadingId` and the shell's `ContentLabelledBy` to it, so the single `main` landmark is named by the route heading rather than shipping unnamed.
- The routes bound to no `Fc*` page component — Agents overview, Operational status, Launch readiness, the Audit evidence list, and the four form surfaces (Conversation context policy, Content safety, Cost controls, Audit governance) — are hand-authored and carry the domain `FcPageHeader` and the same state-slot pattern directly, including the heading, `PageTitle`, focus target, and the loading, error, `not available`, and `Degraded` treatments. The general rule above states the obligation; this states the mechanism, so no route is left without one.
- Heading levels inside a route follow `DESIGN.md § Layout & Spacing`.
- After regeneration, focus stays on Regenerate and the status region announces the new version.

### Forms

- Every control has a visible label. Errors are associated through `aria-describedby` and `aria-invalid`; an error summary receives focus on submit and links to the first error.
- Error text is one localized whole string naming the field, the fault, and the fix (WCAG 3.3.1, 3.3.3), never a bare "Invalid value".
- Required fields carry a visible required marker in addition to `aria-required`; the marker is never color alone.
- Validation runs on submit. After the first submit, a field re-validates on blur; before it, typing never produces an error.
- Numeric fields state units and currency in the label.
- Secret-adjacent fields are named by their configured state (`Secret reference: configured`), never by value.
- Policy-source rows have an accessible name including kind and basis.

### Other criteria

- Reflow: every route stays operable at 320 CSS px width (400% zoom), including proposal detail, version history, and `high-impact-confirmation`. Evidence at 320 px is part of `LR-UI-CONFORMANCE`.
- Target size (2.5.8): every actionable target is at least 24 by 24 CSS px in all three densities, or meets the spacing exception; Compact density is in the conformance lane.
- Focus not obscured (2.4.11): no sticky element overlaps the content scroll area, or scroll-padding equals sticky heights.
- Dragging (2.5.7): no drag-only operation.
- 3.2.6 Consistent Help: N/A, no help mechanism is repeated across pages. 3.3.8 Accessible Authentication: N/A, authentication is shell-owned.
- Reduced motion never hides generation, approval, or posting state changes. No `MessageBarAnimation` is enabled, and no control activates on `pointerdown` or `mousedown`; Fluent's click activation satisfies 2.5.2 and this states the inherited behavior so a hand-authored control cannot drift from it.
- Language of parts (3.1.2): generated content, the editor textarea, the preview, and Conversation-derived evidence carry a `lang` attribute from the Provider result or the Conversation language when either is known, because that text is not necessarily in the UI culture; where neither is known the region inherits the page language.
- Secrets, raw payloads, and other-tenant data never appear in accessible names, tooltips, copied text, diagnostics, or announcements.
- Timing (2.2.1): proposal expiry is claimed under the **essential exception**, and the claim is argued rather than asserted. The limit is essential because the decision being timed is a governed one: the approved version is pinned to the Conversation Context and the Content Safety Policy version it was generated under, and a reservation is held against a cost cap for its duration. Removing the limit would not slow the activity down, it would invalidate it — an approval posted against stale context, under a superseded policy, on an indefinitely held reservation is not the same act. None of the other three allowances is satisfied, and the deferral records that plainly: there is no **Turn off**; **Extend** is unavailable because there is no in-UI extension at all, so the 20-seconds-and-ten-extensions path is not open; and **Adjust** is unavailable because the window is configurable by a Tenant Agent Administrator from 1 hour to 30 days, not by the Approver who meets it. The nearing-expiry threshold and its 15-minute floor are a **usability commitment**, not a 2.2.1 mechanism: they neither strengthen nor are required by the essential exception, and the sign-off must not be recorded as though they were.
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
| Policy-gated nav | Five policy constants; hide, then deny on direct navigation | § Information Architecture |
| Pending pattern | Agents truth-flow helper; `FcPendingCommandSummary` out for V1 | § Projection catch-up contract |
| `IBadgeCountService` | Not used in V1; see the `proposal-notification` row | `proposal-notification` row |
| `FcFluentIcons` | Sole icon source; glyph requests | `DESIGN.md § Brand & Style` |
| `EXT-CONV-UI-1` | Conversation action contribution; Uncommitted; blocks Story 6.7 | § Conversation Integration Seam |
| `FcAggregateListPage` | Route heading, `HeadingTabIndex=-1`, `PageTitle` as parameters | § Accessibility Floor |
| `FcAggregateDetailPage` | No heading parameters; the domain supplies `FcPageHeader` in every state slot, plus a localized `BackLinkLabel`; `FcAggregateDetailState` mapping | § Accessibility Floor; § List, detail, and deep-link surfaces |
| `IProjectionChangeDetailNotifier` | Projection nudge source, filtered to the authoritative projection ids. Nothing must grow — the member ships — but the registration arrives through the EventStore service extension, so a page injecting it into a host that skipped that call fails at first render. The 250 ms / 8 s contract exists only in this spine: the Architecture Spine names no polling, nudge, or catch-up contract, and the two shipped paths run 250 ms / 5 s and 200 ms / 8 s | § Projection catch-up contract |
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

Both status enums are **versioned additive**, on the same terms as `ProviderReadinessReasonCode`: an unknown value renders `authoritative pending`, never Success and never a terminal, and never a raw token. § Agent call is the normative token list, so Architecture binds to it rather than restating it.

| Contract | Addition | Owner |
|---|---|---|
| `AgentReadinessStatus` | `ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved` | 5.5 |
| `AgentCallOperationStatus` | `SafetyBlocked`, `BudgetBlocked`, `RateLimited`, `CapacityQueued`, `CapacityRejected`, `UnknownOutcome`, and the three FR-33/FR-28 pre-Provider rejections `NoEligibleApprover`, `KillSwitchActive`, `RemovedInConversations` | 6.3, 6.4, 6.5 |
| `AgentCallOperationStatus` | `PostingPending`, `Posted`, and `PostingFailed` with a typed reason, plus the field carrying the posted `MessageId`. The automatic path has no proposal whose `ProposedAgentReplyState` it could reuse. | 6.7 |
| `ProviderReadinessResult` | Published, including a by-Provider/model query for the disable blast radius. The query is absent from Story 5.5's acceptance criteria and from the Architecture Spine, and the disable action ships in Story 5.3 **before** 5.5, so either 5.5's criteria gain the query or its ownership moves to 5.3; until then the `provider-catalog-grid` interim applies | 5.5, with an interim in 5.3 |
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
| Every `role="status"`, `role="alert"`, or `aria-live` node in `src/Hexalith.Agents.UI` outside `AgentsPageStatusRegion` — 15 sites across 13 files: five badges (`AgentReadinessBadge`, `ProviderStatusBadge`, `ProposedAgentReplyStateBadge`, `AgentCallStatusBadge`, `AuditAvailabilityBadge`), three panels (`OperationalStatusPanel`, `AuditEvidencePanel`, `LaunchReadinessPanel`), three inline policy rows (`ApproverPolicy.razor`), four page and action nodes (`ProposalEditor`, `ProposalRegenerator`, `ProposalDetail`, `LaunchReadiness`), plus `AgentSurfaceState` (which also nests an `h2` inside a live region), `AgentCallStatusFeedback`, `ProposalTransitionAnnouncer`, and one node each on `ProposalApprover`, `ProposalRejector`, and `ProposalAbandoner` | § Accessibility Floor, live regions. The per-command nodes are the ones that would double-announce at the approval moment, which is the defect the carrier rule exists to prevent, so the migration is an inventory with a per-file audit in the story acceptance criteria, not a five-component edit | 8.6 |
| Every `Disabled` on a blocked-but-explicable control across eleven pages — `DisabledFocusable` has **zero** occurrences anywhere in `src/`, so this is a class of divergence, not one control: `ProviderCatalog.razor` (8 sites), `AgentConfiguration.razor` (4), `LaunchReadiness.razor`, and the response-mode toggle inside `ConversationAgentCallPanel.razor` | § Interaction Primitives, the `aria-disabled` binding rule and its `aria-describedby` reason | 5.2, 5.3, 5.7, 7.2, 7.3, 8.6 — each story converts its own controls |
| `LocalizationResourceTests` | § Voice and Tone, localization mechanics | 8.6 |
| **No CSS ships at all**: no `.css`, no `.razor.css`, no `wwwroot`, no `<style>` block and no inline `style=` anywhere in `src/Hexalith.Agents.UI`, behind roughly 115 distinct BEM class values (`agents-launch-readiness__metrics-table`, `proposal-version-history__marker--approved`, `conversation-agent-call-panel__status`, a bare `class="mono"`, and so on) | `DESIGN.md § Layout & Spacing`. Every spacing, sticky-offset, logical-RTL and reserved-space instruction, and `{typography.mono}` for identifiers, needs a delivery mechanism, and the decision is stated there: Fluent component parameters and Fluent 2 design tokens first, per `hexalith-ux-instructions.md § No theme redefinition`, with one Agents stylesheet only for layout the design system does not own. The classes are authored in the 5.x and 7.x surface stories; 8.6 is the conformance lane that proves no legacy token and no recreated component styling. There is no legacy-token backlog to allowlist: `--type-ramp-*`, `--neutral-*`, `--accent-*`, `--neutral-fill-*` and `--palette-*` have zero hits across `src/` | 5.2, 5.3, 5.7, 7.1, 7.2, 8.6 |
| The two mandatory `not available` keys **do not exist** in `AgentsResources.resx` or `.fr.resx`, while the per-cause keys the rule forbids do: `Agents.Surface.PermissionDenied.*`, `Empty.*`, `FilteredEmpty.*`, `Error.*`, `Unavailable.*`, `Stale.*`, `Degraded.*` | § Voice and Tone. `Agents.Surface.NotAvailable.Title` and `.Message` are added and bound identically to `Unauthorized`, `NotFound` and `Unavailable`; the per-cause `not available` copy is removed, because differing copy is itself a disclosure channel. The enforced-source sentence governs the **wording of keys that exist**, never the key inventory, so it cannot be read as letting shipped per-cause copy win over the rule that forbids it | 8.6 |
| `Agents.PlatformOperator` is not registered; the four tenant-scoped constants are. Every platform-scoped row therefore has no constant to gate on and falls back to `Agents.Administrator` | § Authorization roles | 5.3, 5.7 |
| `AgentReadiness.MapState` (`Callable = Active && no blockers`) and the UI-local `ProviderReadinessState` enum (`Enabled, NotConfigured, Disabled, HistoricalSelection, Degraded, Failed, Unknown`), whose shape carries no `OperationalState`, `Callability`, `ReasonCode`, `CapabilityVersion`, `ObservedAt` or `ValidUntil` | `agent-readiness-badge` and `provider-status-badge` rows; both are superseded by the Story 5.5 readiness result | 5.5 |
| `LaunchReadiness.razor` renders a raw `<table>/<thead>/<tr>/<th scope>/<td>` metrics grid, and `AgentConfiguration.razor` renders eleven `<section>` with ten sibling `<h2>` and no `FluentAccordion` | `hexalith-ux-instructions.md § Reuse over hand-rolling` and `§ Page sections`; `DESIGN.md` mandates `FluentDataGrid` and the single-accordion grouping | 8.7 for the grid; 5.2 and 5.7 for the accordion |
