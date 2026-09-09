---
title: Sprint Change Proposal - 2026-09-09 Architecture Spine Backlog Reconciliation
status: approved
created: 2026-09-09
updated: 2026-09-09
mode: Batch
change_scope: moderate
recommended_path: direct-adjustment
project: agents
owner: Administrator
approval_required: false
approved_by: Administrator
approved_on: 2026-09-09
execution_status: complete
routed_to:
  - Product Owner
  - Developer
  - Solution Architect
  - EventStore Maintainer
  - Test Architect
  - UX Owner
trigger_artifacts:
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - architecture/architecture-agents-2026-06-23-2/UPDATE-REPORT-2026-09-09.md
amends_if_approved:
  - epics.md
  - prds/prd-agents-2026-06-23/prd.md
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
  - external-dependency-register.md
  - launch-readiness-register.md
  - ../implementation-artifacts/sprint-status.yaml
preserves:
  - PRD V1 vision and MVP scope
  - active Epic 5 through Epic 8 user outcomes
  - completed Epics 1 through 4 as historical evidence
  - Dapr Workflow as the sole V1 durable execution owner
  - RQ-1 as an operational release gate outside the story backlog
---

# Sprint Change Proposal: 2026-09-09 Architecture Spine Backlog Reconciliation

## 1. Issue Summary

The final Architecture Spine updated on 2026-09-09 adds AD-27 through AD-31 and materially revises AD-2, AD-4, AD-10, AD-17, AD-20 through AD-22, and the structural seed. The active backlog and sprint tracker still encode the earlier architecture.

This is not only documentation drift. Story 5.3 shipped a tenant-scoped `ProviderCatalogAggregate`, a tenant-keyed `provider-catalog` read model, and transitional `actor:agentsProviderAdmin` authorization. The new architecture instead requires a platform-scoped catalog in reserved EventStore tenant `system`, a separate tenant-scoped `TenantProviderEnablement` aggregate and projection, and AD-30 trusted principal ingress with HMAC-tagged reserved extensions. The shipped story is therefore useful delivery evidence but not conformant final implementation.

The architecture also names durable aggregates and external protection behavior that the active stories do not assign completely, updates `OperationGateMatrixVersion` from 1 to 2, and creates nine explicit implementation follow-ups. Until the backlog is corrected, developers can complete the current acceptance criteria and still violate final architecture.

### Trigger Evidence

| Evidence | Current fact | Delivery consequence |
| --- | --- | --- |
| AD-2 | Catalog is `system` scoped and tenant visibility is owned by `TenantProviderEnablement`; fourteen V1 aggregates are enumerated | Story 5.3 and several governance/runtime stories assign the wrong or incomplete durable owner |
| Shipped Story 5.3 | Its approved spec says “tenant-scoped governed provider/model catalog”; `ProviderCatalogProjectionFold` keys state by request tenant | The story must be reopened and migrated; an AC-only wording patch would leave shipped state non-conformant |
| AD-29 | One shared `AgentsIdentity` canonicalizer owns all deterministic identities | Current generation, proposal, edit, and regeneration paths use several incompatible derivation helpers |
| AD-30 | Principal kind is selected by operation family; reserved extensions are ingress-issued and HMAC verified; JWT roles alone never grant authority | Story 5.4 must replace transitional trusted booleans and close DW-2 |
| AD-4 | `ConfigurationVersion` increments on every accepted Agent configuration or lifecycle event | Current `AgentActivated` and `AgentDisabled` events do not carry or increment it (DW-4) |
| AD-17 and readiness register | Matrix version 2, `ScopeKind`, `AuthorizedProducer`, and a Live-Seam Matrix are authoritative | `epics.md` still cites matrix version 1 in three executable locations and does not bind live seam coverage atomically |
| AD-22 and external register | Content-bearing work requires `EXT-PROTECTION-1`; the shipped EventStore default is a no-op | Content stories lack a dedicated protection-binding prerequisite; the dependency owner is still `TBD` |
| Structural Seed | `Hexalith.Agents.IntegrationTests` is required; `Server/Aggregates` and `Application/Tools` are absent | Story 5.6 and its conformance tests must correct the repository shape and test stack |
| AD-31 and UX spine | `/agents/conversation-call` is a pre-integration harness and must be deleted before Story 6.7 closes | Story 6.7 currently declares no new external seam and does not make deletion explicit |
| Sprint tracker | It still lists the superseded 18-story Epic 5 slugs and marks current completed specs as review | Sprint status is not a faithful projection of current `epics.md` and must be regenerated atomically after approval |

### Problem Classification

This is a technical and planning-model correction discovered after implementation. It does not change the V1 product thesis, user journeys, or four active epic outcomes. It changes durable ownership, identity/security protocols, migration obligations, dependency gates, and verification evidence.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Viability |
| --- | --- | --- |
| Epic 5 — Live Governed Setup And Honest Readiness | Reopen Story 5.3; rewrite trusted ingress/readiness/test composition; add a payload-protection foundation story | Remains viable; story count 7 to 8 |
| Epic 6 — One Safe Automatic Conversation Response | Patch protection dependency, rewrite budget identity and Conversation membership ownership, remove the harness in 6.7 | Remains viable; story count unchanged |
| Epic 7 — Complete Confirmation And Approval | Patch proposal/attempt/version identity derivation and protected-content dependencies in 7.1–7.3 | Remains viable; story count unchanged |
| Epic 8 — Governance Operations And Release Qualification | Make governance aggregate ownership explicit, add compliance inspection as a distinct user outcome, align metrics to the current PRD | Remains viable; story count 7 to 8 |

No new epic is needed. The active backlog grows from 27 to 29 stories. `RQ-1` remains outside the backlog.

### Aggregate Ownership Disposition

An aggregate is not automatically a standalone user story. Existing vertical outcomes should own aggregates where their acceptance criteria can prove a complete behavior. Only capabilities with no complete owning outcome receive new stories.

| Aggregate | Proposed owning story | Change type | Rationale |
| --- | --- | --- | --- |
| `ProviderCatalog` | 5.3 | Full rewrite/reopen | Own platform catalog mutation, migration, replay, and platform projection |
| `TenantProviderEnablement` | 5.3 | Full rewrite/reopen | It is inseparable from tenant-visible catalog selection and migration |
| `LaunchReadinessGate` | 5.5 | Substantial AC rewrite | Existing readiness outcome already owns the aggregate; add matrix v2, scope, producer, identity, and route contracts |
| `SecurityEventLog` | 5.4 | Substantial AC rewrite | Content-free denials are part of the trusted principal/authorization outcome |
| `BudgetLedger` | 6.4 | Substantial AC rewrite | Existing hard-reservation outcome already exercises admission, reservation, settlement, and recovery |
| `ConversationAgentState` | 6.6 | Full rewrite | Existing membership/posting outcome is the correct vertical slice but lacks block/removal state |
| `ContentSafetyPolicy` | 8.4 | Substantial AC rewrite | Platform policy publication belongs to the existing policy-operation outcome |
| `TenantGovernancePolicy` | 8.4 | Substantial AC rewrite | Tenant restrictions, caps, rate limits, calling restriction, and kill switch belong together |
| `LegalHold` | 8.1 | Full rewrite | Existing hold story remains the user outcome; add scope grammar, two-phase pinning, and expected-revision decisions |
| `AuditExport` | 8.2 | Full rewrite | Existing export story remains the user outcome; add manifest sealing and protected key delivery |
| `ProtectedDeletion` | 8.3 | Full rewrite | Existing deletion story remains the user outcome; add two-role approval, DEK destruction, and restrictive completion |
| `AuditInspection` | New 8.8 | New story | No active story delivers the scoped compliance-inspection lifecycle and review obligation |

`Agent` and `AgentInteraction` remain owned by their existing setup/runtime/proposal stories.

### Story Impact Summary

#### Full or substantial rewrites

- 5.3 — platform catalog migration plus `TenantProviderEnablement`.
- 5.4 — AD-30 principals, fresh identity/role derivation, HMAC-tagged trusted ingress, `SecurityEventLog`, and DW-2 closure.
- 5.5 — `LaunchReadinessGate`, matrix v2, `ScopeKind`, `AuthorizedProducer`, `/api/v1/agents`, and retirement of Agent-owned launch-readiness state.
- 5.6 — workspace test-stack alignment, `Hexalith.Agents.IntegrationTests`, Live-Seam Matrix enforcement, and structural-seed cleanup.
- 6.4 — AD-29 attempt identities and complete `BudgetLedger` admission/reservation/settlement ownership.
- 6.6 — `ConversationAgentState`, lazy external-removal detection, block/re-admission, and non-terminal proposal abandonment.
- 8.1–8.4 — explicit `LegalHold`, `AuditExport`, `ProtectedDeletion`, `ContentSafetyPolicy`, and `TenantGovernancePolicy` semantics.

#### Acceptance-criteria and dependency patches

- 5.2 — increment `ConfigurationVersion` on `ActivateAgent` and `DisableAgent`, including additive event/schema migration and replay tests (DW-4).
- 5.7 — replace matrix version 1 with version 2 and require exact `RegistryRevision`, `ScopeKind`, and producer-valid observations.
- 6.1 — depend on new Story 5.8 and `EXT-PROTECTION-1`; prove reference-only workflow state under AD-27.
- 6.7 — declare `EXT-CONV-UI-1` as an external dependency and delete/unregister `/agents/conversation-call` before completion.
- 7.1–7.3 — use the shared AD-29 canonicalizer for proposal, generated, edited, regenerated version, and attempt identities; add protection dependency where plaintext is materialized.
- 8.5 — replace superseded SM-3/SM-1–SM-6 gate text with the current PRD split: pre-enablement SM-1/4/5/6 and post-enablement SM-2/3/7 plus counter-metrics.
- 8.6 — use the complete matrix-v2 high-impact family vocabulary in conformance coverage.

#### New stories

1. **Story 5.8: Protect Sensitive Agent Content At The EventStore Boundary.** Bind `EXT-PROTECTION-1` and `EXT-SECRETS-1`; implement the field-level `ProtectedContent` envelope, per-interaction DEK/per-tenant KEK contract, sealed broker/read-model behavior, typed `Erased` replay, hold pins, no-op-default rejection, and the AD-27 workflow content sweep. This becomes a prerequisite for content-bearing Epic 6/7 work and Stories 8.1–8.3.
2. **Story 8.8: Inspect Audit Evidence Under Durable Compliance Governance.** Implement `AuditInspection` with Conversation/case scope, required justification, distinct second-party pre-approval or post-hoc review, rate visibility, posted-provenance versus unposted-content disclosure, survival after Source Conversation loss, tenant isolation, and durable self-audit.

### Artifact Conflicts

| Artifact | Finding | Required change if approved |
| --- | --- | --- |
| PRD | Product scope is aligned, but §8 does not yet include `EXT-PROTECTION-1`, and the Architecture Assumptions remain outside its single assumptions index | Add the dependency and register or normatively cross-index ARCH-A-1..ARCH-A-7 without changing MVP scope |
| Architecture Spine | `ARCH-A-5` provisionally assigns catalog migration to 5.5, while this proposal assigns durable catalog/enablement migration to 5.3 and route/readiness consumption to 5.5 | Retire/amend ARCH-A-5 when sprint planning accepts the assignment |
| UX Experience | Provider and safety platform/tenant splits are already directionally aligned; Audit evidence still has a generic `7.x, 8.x` owner and content-safety contracts use Agent-owned names | Assign audit inspection to 8.8 and align safety contract names/authority with 8.4; no redesign or new journey |
| External dependency register | `EXT-PROTECTION-1` has a repository and consumers but no accepted owner; consumers omit several content-materializing stories | Set Owner to `EventStore Maintainer`; expand consumers to 5.8, 6.1–6.4, 7.1–7.4, 8.1–8.3, 8.8, and `RQ-1`; leave status `Uncommitted` until all fields are accepted |
| Launch-readiness register | Normative matrix is already v2, but the Live-Seam row combines payload protection with hold/export/deletion and does not name new story ownership | Split/map payload protection to 5.8, hold/export/deletion to 8.1–8.3, and audit inspection to 8.8; do not change gate semantics |
| `epics.md` | Requirements inventory is stale; matrix version 1 appears in the global requirements and Stories 5.5/5.7; story ownership and dependencies are incomplete | Apply the detailed edits below, update counts/topology/maps, and remove all executable version-1 citations |
| `sprint-status.yaml` | Tracks obsolete 18-story slugs and stale review statuses | Regenerate only after `epics.md` lands, preserving historical Epics 1–4 and current action items |

### Technical Impact

- Existing tenant-scoped provider streams and read models must be migrated idempotently into platform catalog streams and tenant enablement streams. Old streams are frozen, not rewritten or deleted, and each migrated target records `MigratedFrom`.
- Public routes become `/api/v1/agents/...`; old unversioned routes are compatibility-handled explicitly and cannot remain the authoritative route family.
- All deterministic identity helpers converge on `AgentsIdentity`; exact replays retain original outcomes, while mismatched payload fingerprints conflict.
- Administrator authorization can no longer rely on JWT role claims or untagged booleans. The ingress strips client-reserved keys, derives a fresh principal, issues the allowlisted extension, and the command pipeline verifies its HMAC before aggregate dispatch.
- Safety configuration leaves `Agent`; in-flight interactions snapshot the platform and tenant policy version pair. Legacy Agent safety and launch-readiness values are migrated/frozen under additive compatibility rules.
- Content-bearing workflow state stores references only; production-like content work remains fail-closed while `EXT-PROTECTION-1` is not `Available`.
- Live seam claims require named passing integration tests that assert persisted EventStore/state-store/read-model end state in the same change.

## 3. Recommended Approach

Use **Direct Adjustment**: preserve Epics 5–8, add two stories, reopen Story 5.3, and revise the affected acceptance criteria and evidence manifests. Do not roll back the completed catalog work. Treat it as the migration source and historical evidence, then build the platform/tenant split additively.

### Options Considered

| Option | Viability | Assessment |
| --- | --- | --- |
| Direct adjustment | Selected | Preserves product scope and delivered evidence while making the final architecture executable |
| Roll back Story 5.3 | Not selected | Deleting the shipped implementation loses useful contracts/tests and does not solve stream migration; freeze-and-migrate is safer |
| AC-only patches | Not viable | Cannot correct durable stream scope, trusted ingress, protection, or missing aggregate ownership |
| One story per aggregate | Not selected | Produces infrastructure-shaped backlog items and separates aggregates from the user outcomes they serve |
| MVP reduction | Not viable | The new obligations implement already approved governance, security, and audit requirements rather than optional scope |

### Effort, Risk, And Timeline

- Planning effort: medium; 29-story graph, evidence manifests, dependency mappings, and sprint tracker must change atomically.
- Implementation effort: high; one shipped story is reopened, two stories are added, and several security/data-governance stories require substantive rework.
- Technical risk: high until migration, trusted ingress, and payload protection have live tests; medium after those foundations land.
- Schedule impact: at least two additional backlog stories plus Story 5.3 rework. A calendar estimate is not credible until `EXT-PROTECTION-1` and the other Uncommitted dependencies have accepted target dates.
- Critical-path change: 5.3 must be remediated before 5.5; new 5.8 must complete before content-bearing Epic 6/7 work and 8.1–8.3 can claim live conformance.

## 4. Detailed Change Proposals

### 4.1 `epics.md` — Story 5.3 Rework

**OLD**

> As an Agent Administrator, I want Provider/model capabilities and pricing governed through live public operations.
>
> EventStore records one tenant-scoped catalog; cross-tenant selection is denied; `EXT-PROVIDER-1` is a story dependency.

**NEW**

> As a Platform Operator, I want one platform Provider/model catalog with explicit tenant enablement, so catalog truth is administered once while every tenant sees and selects only entries enabled for it.

Replace the acceptance criteria with these obligations:

1. `ProviderCatalog` commands target reserved tenant `system` and one stream per (`ProviderId`, `ModelId`), require the AD-30 `Platform` principal, and preserve capability/pricing/secret-reference behavior.
2. `TenantProviderEnablement` commands target one tenant aggregate, require the Platform Operator, and record which system-catalog entries that tenant may see/select.
3. Migration reads every shipped tenant-scoped catalog entry, writes deterministic platform and enablement targets with `MigratedFrom`, treats exact repeats as no-ops, conflicts on divergent duplicates, and freezes legacy streams/read models without rewriting them.
4. Tenant queries join `provider-catalog` with `tenant-provider-enablement`; absent or non-enabled entries return the identical not-found response, and platform-only blockers render as `PlatformNotReady`.
5. Platform enable/disable and tenant enablement are distinct operations; tenant administrators cannot mutate either catalog scope and cannot infer other tenants' enablement.
6. Evidence includes `ProviderCatalogMigrationTests`, `TenantProviderEnablementAggregateTests`, platform/tenant isolation tests, persisted projection end-state tests, and Story 5.6 live-seam coverage.

Remove `EXT-PROVIDER-1` from Story 5.3 readiness, consistent with the current external register. Reopen Story 5.3 as backlog after approval; its completed specification remains historical implementation evidence until renegotiated or superseded.

### 4.2 `epics.md` — Existing Story Rewrites And Patches

#### Story 5.2 — lifecycle configuration version

**OLD:** replay reconstructs “configuration versions” without explicitly covering lifecycle transitions.

**NEW:** every accepted configuration **and lifecycle** event increments `ConfigurationVersion`; `AgentActivated` and `AgentDisabled` evolve additively, legacy events replay deterministically, duplicate lifecycle commands do not increment, and snapshot tests prove later interactions observe the new version. This closes DW-4.

#### Story 5.4 — trusted principal and security evidence

**OLD:** JWT-only, UI-only, or historical role evidence cannot grant authority; transitional trusted extensions remain unspecified.

**NEW:** define all four AD-30 principal kinds, select principal kind from operation family, resolve user `PartyId` from the authenticated subject plus current Parties/Tenants evidence, strip all client-supplied reserved keys, issue allowlisted HMAC-tagged extensions at ingress, verify tags before aggregates, reject forged/untagged/wrong-family/wrong-scope use, and append content-free denials to tenant/day `SecurityEventLog`. Retain the existing Party and Approver readiness outcome. This closes DW-2.

#### Story 5.5 — readiness, route, and legacy readiness migration

**OLD:** consumers evaluate `OperationGateMatrixVersion 1`; Provider readiness is tenant-catalog-shaped; public route versioning is unstated.

**NEW:** consumers evaluate version 2; `LaunchReadinessGate` owns (`GateId`, `TenantScope`, `EnvironmentProfile`) observations with AD-29 `ObservationId`, exact `ScopeKind`, and `AuthorizedProducer`; tenant Provider readiness joins the system catalog with `TenantProviderEnablement`; all public routes live under `/api/v1/agents/...`; and legacy `RecordAgentLaunchReadiness`/Agent-owned readiness state is retired and migrated to `LaunchReadinessGate` without rewriting history.

#### Story 5.6 — seed and live test tier

**OLD:** topology tests are named but `Hexalith.Agents.IntegrationTests`, test-stack alignment, and forbidden seed folders are not completion criteria.

**NEW:** align `global.json`, xUnit v3/Microsoft Testing Platform v2 `test.runner`, NSubstitute, and workspace catalog pins; create and add `test/Hexalith.Agents.IntegrationTests` to the `.slnx`; cover the Live Story 5.2/5.3 dispatch/query/projection seams with persisted end-state assertions; enforce the Live-Seam Matrix; and remove `src/Hexalith.Agents.Server/Aggregates` plus `src/Hexalith.Agents.Server/Application/Tools` from source and `StructuralSeedConformanceTests`.

#### Story 5.7 — current matrix only

**OLD:** `AgentActivation` evaluates matrix version 1.

**NEW:** it evaluates version 2 at one `RegistryRevision`, rejects unknown/old matrix selection where current version is required, and consumes platform versus tenant records according to `ScopeKind` from producer-valid observations.

#### Story 6.4 — AD-29 and `BudgetLedger`

**OLD:** a deterministic descriptor and reservation are required, but the shared derivation grammar and complete ledger ownership are not.

**NEW:** the shared canonicalizer assigns aggregate-counted `AttemptOrdinal`, derives `AttemptId`, `ReservationId`, `AdmissionId`, and `QueueId` exactly per AD-29, and uses `AttemptId` verbatim as Provider idempotency. `BudgetLedger(TenantId, UTC BudgetPeriod)` atomically owns rate admission, open-interaction bounds, reservation, release, settlement, `Unreconciled`, period close, and recovery; no local counter or interaction event substitutes for ledger truth.

#### Story 6.6 — `ConversationAgentState`

**OLD:** membership is a simple idempotent add followed by posting.

**NEW:** `ConversationAgentState(TenantId, ConversationId)` owns membership-established state, the Agents block, and the non-terminal proposal index. Acceptance and pre-post revalidation run the three-part membership protocol, detect external removal lazily, set `RemovedInConversations`, abandon indexed non-terminal proposals with versions preserved, and forbid silent rejoin until a Tenant Agent Administrator or Conversation Facilitator clears the block.

#### Story 6.7 — sole entry enforcement

**OLD:** no new external seam is declared and alternate entries are merely absent from desired behavior.

**NEW:** `EXT-CONV-UI-1` must be `Available` for the contributed action/decorator/callability seam; before completion the `/agents/conversation-call` route is unregistered and its page and navigation tests are removed. `AlternateInvocationGuardTests` prove no routable harness remains.

#### Stories 7.1–7.3 — shared identities

**OLD:** each story asks for deterministic IDs but permits local helper grammars and client-provided attempt seeds.

**NEW:** use `AgentsIdentity` only. Story 7.1 records the architecture-approved proposal identity relationship and derives initial `ProposalVersionId` from (`AgentInteractionId`, aggregate-assigned `VersionOrdinal`, kind). Story 7.2 derives edited `ProposalVersionId` from the same grammar. Story 7.3 obtains the next aggregate-assigned `AttemptOrdinal`, derives `AttemptId`, and derives regenerated `ProposalVersionId`; transport retry never increments either ordinal. Exact replay is a no-op and conflicting payload is a typed conflict.

#### Stories 8.1–8.4 — governance aggregate owners

**OLD:** stories describe retention/hold/export/deletion/safety/budget behavior, but several commands, aggregates, expected revisions, roles, and protection dependencies remain implicit or use Agent-owned safety state.

**NEW:**

- 8.1 explicitly owns `LegalHold`, its `interaction:<id>`/`class:<class>` plus UTC-range scope grammar, two-phase DEK pinning, expected revisions, release events, and `EXT-PROTECTION-1`.
- 8.2 explicitly owns `AuditExport`, `ManifestSealed`, item hashes/revision ranges/key versions/signature reference, tenant-KEK-wrapped export key delivery outside Agents responses, and protection/secret dependencies.
- 8.3 explicitly owns `ProtectedDeletion`, Platform request plus Compliance Inspector approval, hold-safe expected-revision reads, DEK destruction receipt, named projection/workflow purge outcomes, and restrictive completion.
- 8.4 removes safety configuration from `Agent`; platform `ContentSafetyPolicy(system)` owns publication/version lineage, while `TenantGovernancePolicy(TenantId)` owns stricter tenant restrictions, caps, rate limits, calling restriction, concurrency limit, and kill switch. Agent retains only proposal expiry, regeneration ceiling, context policy, and response/approver configuration.

### 4.3 `epics.md` — New Story 5.8

**Story 5.8: Protect Sensitive Agent Content At The EventStore Boundary**

As a Security Engineer,
I want Agent content protected before it enters durable or execution infrastructure,
so that erasure is enforceable without breaking EventStore replay.

Acceptance summary:

1. `EXT-PROTECTION-1` and `EXT-SECRETS-1` are `Available` before any live content-bearing workflow executes.
2. The orchestrator seals only sensitive fields in `ProtectedContent` using a per-interaction DEK wrapped by the tenant KEK; every other event field stays plaintext.
3. Sealed envelopes remain sealed on pub/sub, in read models, adapters, and workflow references; workflow history contains no content.
4. `DestroyDek` produces an irreversible receipt; replay materializes the typed `Erased` value and restore/snapshot caches cannot revive content.
5. Hold pinning prevents destruction; cross-tenant keys and digests cannot unprotect or correlate another tenant's content.
6. The shipped no-op protection service blocks content-bearing work and cannot satisfy evidence.
7. Live tests cover protection, unprotection, sealed transport, hold pins, deletion, erased replay, restore, tenant isolation, and the AD-27 execution-state content sweep.

### 4.4 `epics.md` — New Story 8.8

**Story 8.8: Inspect Audit Evidence Under Durable Compliance Governance**

As a Compliance Inspector,
I want scoped inspection of protected Agent evidence recorded as its own governed case,
so that sensitive content can be examined without granting ambient Conversation participation.

Acceptance summary:

1. `AuditInspection(TenantId, InspectionId)` owns the inspection request, scope, justification, approval/review state, reads, and terminal outcome.
2. Posted provenance is available only to a current Participant with Source Conversation read access; unposted content requires a recorded Eligible Approver or compliance inspection.
3. Compliance inspection is scoped to a named Conversation or case, requires justification, and records either pre-approval by a distinct Tenant Agent Administrator or post-hoc review within the configured window.
4. Inspection rate and review status are visible to the Tenant Agent Administrator; the inspection audit survives Source Conversation deletion and inspected-content erasure.
5. Missing, stale, cross-tenant, wrong-role, unapproved, or over-rate requests fail closed before unprotection or disclosure and append content-free security evidence.
6. API, UI, projection, and `AuditInspectionLiveTests` prove the same disclosure result and persisted end state.

### 4.5 Register And Supporting Artifact Edits

#### External dependency register

**OLD**

> `Owner`: `TBD` (EventStore Maintainer expected)
>
> `ConsumingStories`: 6.1, 6.4, 7.1, 8.1, 8.2, 8.3; `RQ-1`

**NEW**

> `Owner`: EventStore Maintainer
>
> `ConsumingStories`: 5.8, 6.1–6.4, 7.1–7.4, 8.1–8.3, 8.8; `RQ-1`

Keep `AcceptedStatus: Uncommitted` and every unknown target/date/command as `TBD` until the owner explicitly accepts the full record.

#### Launch-readiness register

**OLD:** one Live-Seam Matrix row groups “Payload protection, hold, export, deletion” under Stories 8.1–8.3.

**NEW:** map payload-protection binding to Story 5.8; retain hold/export/deletion under 8.1–8.3; add AuditInspection under 8.8. Matrix semantics, GateIds, and version 2 remain unchanged.

#### PRD and Architecture

Add `EXT-PROTECTION-1` to PRD §8 and cross-index ARCH-A-1..ARCH-A-7 in the PRD assumptions authority. Amend ARCH-A-5 so Story 5.3 owns catalog/enablement migration and Story 5.5 owns route/readiness consumption; mark the provisional assignment retired by this approved sprint plan.

#### UX Experience

Change Audit evidence ownership from generic `7.x, 8.x` to the contributing proposal stories plus 8.8, and change the Content safety policy contract description from Agent-owned configuration to platform `ContentSafetyPolicy` plus tenant restrictions in `TenantGovernancePolicy`. No route, component, layout, or journey change is required.

### 4.6 `sprint-status.yaml` Regeneration

After and only after the approved `epics.md` edit lands:

1. Preserve Epics 1–4 and retrospectives as `done` historical evidence.
2. Preserve `epic-5: in-progress`; add `epic-6`, `epic-7`, and `epic-8` as `backlog` until their first story enters work.
3. Replace the obsolete 18-story Epic 5 block with the exact 29 active story slugs from updated Epics 5–8.
4. Carry current evidence accurately: Story 5.1 `done`; Story 5.2 `done`; Story 5.3 `backlog` because the architecture reopens it; all other active stories `backlog` unless a current implementation artifact proves a later status.
5. Keep all retrospective action items and update only comments whose story ownership changed.
6. Set `last_updated: 2026-09-09` and validate exact bidirectional parity between active epic/story IDs and tracker rows.

Proposed active status shape:

```yaml
development_status:
  # Epics 1-4 unchanged historical rows
  epic-5: in-progress
  5-1-establish-build-package-boundary-and-basic-ci-gates: done
  5-2-configure-hexa-through-live-eventstore-operations: done
  5-3-govern-provider-models-and-pricing-through-live-operations: backlog
  5-4-prove-trusted-principal-tenant-party-and-approver-readiness: backlog
  5-5-publish-authoritative-readiness-and-provider-state-contracts: backlog
  5-6-compose-agents-in-the-platform-owned-production-like-host: backlog
  5-7-activate-hexa-only-when-setup-gates-pass: backlog
  5-8-protect-sensitive-agent-content-at-the-eventstore-boundary: backlog
  epic-5-retrospective: optional
  epic-6: backlog
  # Stories 6.1-6.7 from updated epics.md, all backlog
  epic-6-retrospective: optional
  epic-7: backlog
  # Stories 7.1-7.6 from updated epics.md, all backlog
  epic-7-retrospective: optional
  epic-8: backlog
  # Stories 8.1-8.8 from updated epics.md, all backlog
  epic-8-retrospective: optional
```

## 5. Implementation Handoff

### Scope Classification

**Moderate — backlog reorganization and completed-story rework.** Product and architecture direction are already decided, so a fundamental replan is unnecessary. The change requires Product Owner/Developer coordination because it reopens delivered work, adds two stories, changes dependencies, and regenerates tracking authority.

### Recipients And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner / planning owner | Approve story boundaries, apply `epics.md` rewrites/new stories, and regenerate sprint status atomically |
| Solution Architect | Confirm ARCH-A-5 retirement, AD-29 proposal identity relationship, migration/freeze semantics, and protection prerequisites |
| EventStore Maintainer | Accept or correct ownership and complete the `EXT-PROTECTION-1` target/date/verification commitment |
| Developer | Reopen 5.3; implement migrations and rewritten/new stories in dependency order; preserve additive compatibility |
| Test Architect | Own Live-Seam Matrix parity, integration project creation, persisted end-state evidence, and matrix-v2 contract tests |
| UX owner | Apply the two ownership/contract wording patches; verify no route or journey redesign is introduced |

### Success Criteria

1. `epics.md` has exactly 29 active stories, no executable matrix-v1 references, and a named owner for every AD-2 aggregate.
2. Story 5.3 explicitly migrates tenant catalogs to system catalog plus tenant enablement and is no longer represented as conformant completed work.
3. All nine update-report follow-ups are owned by acceptance criteria or a new story, including DW-2 and DW-4.
4. `EXT-PROTECTION-1` has an accountable owner and exact consuming stories while remaining honestly `Uncommitted` until accepted.
5. Matrix-v2 `ScopeKind`, `AuthorizedProducer`, and Live-Seam obligations are represented in stories and tests.
6. `sprint-status.yaml` matches `epics.md` bidirectionally and preserves historical/action-item evidence.
7. No implementation story can claim live content, Provider, Conversation, or readiness evidence while its consumed dependency is not `Available` and its named integration test does not pass.

## 6. Checklist Record

| Checklist section | Status | Finding |
| --- | --- | --- |
| 1. Trigger and context | [x] | Architecture update and shipped Story 5.3 provide concrete trigger/evidence |
| 2. Epic impact | [x] | Epics 5–8 remain viable; add two stories and revise dependencies/order |
| 3. PRD/architecture/UX/other artifacts | [x] | PRD/register/UX/supporting edits identified; no MVP or journey redesign |
| 4. Path evaluation | [x] | Direct adjustment selected; rollback and MVP reduction rejected |
| 5. Proposal components | [x] | Issue, impact, approach, edits, dependencies, and handoff are specified |
| 6.1 Proposal review | [x] | Internal consistency and actionable ownership checked |
| 6.3 User approval | [x] | Administrator explicitly approved the proposal on 2026-09-09 |
| 6.4 Sprint-status update | [x] | Regenerated after `epics.md`; 29 active story slugs match bidirectionally and YAML parses |
| 6.5 Handoff | [x] | Moderate change routed to Product Owner, Developer, Architect, EventStore Maintainer, Test Architect, and UX Owner |

## Approval Gate

Administrator approved this proposal on 2026-09-09. The backlog, supporting authorities, and sprint tracker are authorized for the coordinated updates described above.

## Execution Record — 2026-09-09

- Applied the approved direct adjustment to `epics.md`, the PRD dependency/assumption authority, Architecture `ARCH-A-5`, UX ownership/contracts, and both operational registers.
- Reopened Story 5.3, added Stories 5.8 and 8.8, and assigned every AD-2 aggregate plus every update-report implementation follow-up.
- Assigned `EXT-PROTECTION-1` to the EventStore Maintainer and its exact consuming stories while retaining honest `Uncommitted` status until its remaining commitment fields are accepted.
- Regenerated `sprint-status.yaml`: Epics 1–4 and action items are preserved, Story 5.1 and Story 5.2 are `done`, reopened Story 5.3 is `backlog`, and all remaining active work is represented by the exact 29 current slugs.
- Validation passed: story distribution 8/7/6/8, exact epic/tracker parity, YAML parse, aggregate/follow-up ownership audit, `git diff --check`, and zero matrix-v1 references in active `epics.md` acceptance criteria.
- Handoff order: renegotiate the reopened Story 5.3 specification; obtain an accepted `EXT-PROTECTION-1` target/date/verification command; then create or update story specifications in dependency order, with Story 5.6 owning the integration tier and Live-Seam Matrix enforcement.
