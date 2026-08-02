---
title: Sprint Change Proposal - Readiness Rerun Remediation
status: approved
created: 2026-08-01
updated: 2026-08-01
mode: Batch
change_scope: major
recommended_path: vertical-replan-with-gated-external-dependencies
project: agents
owner: Administrator
approval_required: false
approved_by: Administrator
approved_on: 2026-08-01
trigger_report: implementation-readiness-report-2026-08-01.md
preserves:
  - sprint-change-proposal-2026-08-01.md
  - implementation-readiness-report-2026-08-01.pre-rerun.md
supersedes_if_approved:
  - the unstarted Epic 5 forward backlog in epics.md
  - Epic 5 backlog entries in sprint-status.yaml
---

# Sprint Change Proposal: Readiness Rerun Remediation

## 1. Issue Summary

The 2026-08-01 implementation-readiness rerun declared the Hexalith Agents
planning package **NOT READY**. Functional traceability is not the problem:
all 28 PRD Functional Requirements have current epic paths. The blocker is the
shape and authority of the forward delivery plan.

The prior approved Correct Course proposal remains valid historical evidence. It
resolved the V1 product, governance, hosting, workflow, safety, cost, retention,
and UX decisions and created Epic 5. The readiness rerun then found that Epic 5
translates those valid decisions into a technical release program rather than an
implementation-ready set of user-value stories.

### Trigger And Evidence

- Formal trigger:
  `implementation-readiness-report-2026-08-01.md`, status `NOT_READY`, with 29
  issue entries.
- Requirements evidence: 28/28 FRs mapped, with no missing or extra FR IDs.
- Delivery evidence: Epic 5 is an 18-story technical mega-epic with a long serial
  path and at least eight oversized stories.
- External evidence: `CONV-AI-1` blocks the posting path; platform hosting,
  Provider, safety, tokenization, secrets, and production-like topology lack
  committed delivery records.
- Release evidence: Story 5.18 combines whole-system conformance, performance,
  cohort attainment, external dependencies, and a readiness assessment in one
  development story.
- Alignment evidence: Provider degraded-state semantics, high-risk UI command
  concurrency, success styling, evidence/freshness registries, projection
  inventory, UI performance, and build baselines remain insufficiently precise.
- Historical evidence: completed Stories 1.1, 2.3, 2.6, 4.2, 4.4, and 4.5 retain
  acceptance criteria superseded by the approved 2026-08-01 decisions.

### Issue Classification

This is a failed delivery decomposition and artifact-reconciliation problem. It
does not require a product pivot, removal of a Functional Requirement, or
rollback of completed Epics 1-4.

### Twenty-Nine-Issue Disposition

| Readiness category | Entries | Proposed disposition |
| --- | ---: | --- |
| PRD completeness risks | 7 | Add external commitments, operational/UI NFRs, compatibility policy, evidence authority, and binding treatment for the two residual assumptions. |
| UX/architecture alignment issues | 5 | Define degraded Provider callability, high-risk command scope, binding UX authority, UI performance targets, and success semantics. |
| Alignment warnings | 3 | Reconcile SDK/package baselines and bind the Conversations UI/posting seam to the dependency register. |
| Critical epic-quality violations | 4 | Replace the technical mega-epic, gate `CONV-AI-1`, bound runtime ownership, and remove Story 5.18 as a development story. |
| Major epic-quality issues | 5 | Split oversized work vertically, shorten the critical path, name registries/inventories, separate metric calculation from launch attainment, and mark historical criteria as superseded. |
| Minor epic-quality concerns | 5 | Add explicit dependencies, reduce multi-outcome ACs, use outcome titles, eliminate “where applicable,” and expand range traceability into per-story evidence manifests. |

## 2. Impact Analysis

### Epic Impact

- Epics 1-4 and their completed stories remain unchanged as historical delivery
  evidence.
- The current unstarted Epic 5 and Stories 5.1-5.18 are not implementation-ready.
  If this proposal is approved, their exact text is preserved in a dated
  superseded-plan appendix and removed from the active backlog.
- The forward work is replaced by four outcome epics:
  1. live governed setup and honest readiness;
  2. one safe automatic Conversation response end to end;
  3. one complete confirmation workflow end to end;
  4. governance operations and release qualification.
- Final readiness becomes release gate `RQ-1`, not a development story. Every
  implementation story produces its own focused Level 4 evidence and contributes
  declared Level 5 evidence where it crosses systems.

### Story Impact

The replacement contains 24 smaller stories. Each story owns one demonstrable
outcome across the minimum required domain, API/client, adapter, UI, and evidence
surfaces. Technical-layer work is accepted only when it enables that story's
observable outcome.

No active Epic 5 implementation story file exists, and all current Epic 5 status
entries are `backlog`; therefore this replan does not abandon in-progress work.

### Artifact Conflicts

| Artifact | Conflict | Required change |
| --- | --- | --- |
| PRD | Missing operational/UI NFR precision, compatibility policy, evidence authority, and committed dependency form | Add NFR-11 through NFR-14, dependency commitment rules, contract evolution rules, and release-evidence authority. |
| Epics | Epic 5 is technical, serial, oversized, and externally blocked | Replace active Epic 5 with Epics 5-8 and archive the old plan with machine-visible supersession. |
| Architecture | Provider degraded semantics, UI concurrency, gate freshness, projection inventory, tokenizer/host ownership, and stack versions are incomplete | Amend AD-10, AD-11, AD-12/13, AD-16/17 and add an authoritative readiness registry section. |
| UX | Degraded state, active-vs-callable Success, concurrency, and UI performance are ambiguous | Align DESIGN/EXPERIENCE to the new public and architecture contracts. |
| Sprint status | One unstarted 18-story mega-epic is tracked as the forward plan | Replace its backlog entries with the four new epics after approval. |
| External delivery | Critical seams have no committed owner/version/evidence record | Add an external dependency register and make unresolved entries story readiness blockers. |
| Release qualification | Story 5.18 mixes implementation, elapsed metrics, and final assessment | Move calculation conformance into stories; move real cohort/operational attainment and the readiness rerun to `RQ-1`. |

### Technical And Delivery Impact

- EventStore persistence, Dapr Workflow, authorization, full context, safety,
  Provider execution, cost control, Conversations posting, UI, and governance
  remain required.
- The technical sequence changes from one deep layer chain to vertical slices
  with dependency gates and parallelizable governance/UI work.
- Production-like callability remains blocked until all hard readiness gates are
  current and evidenced.
- The selected package baseline changes to the versions actually selected by the
  workspace catalog and FrontComposer integration; unselected Provider SDKs are
  removed from the authoritative stack table until an adapter commitment exists.

## 3. Recommended Approach

### Selected Path

Use a **vertical replan with gated external dependencies**:

1. Preserve the approved product decisions, final PRD scope, Architecture
   invariants, UX spines, completed Epics 1-4, and prior approved proposal.
2. Supersede only the unstarted Epic 5 forward backlog.
3. Create explicit external-dependency and launch-readiness registers before any
   replacement story becomes ready for development.
4. Deliver automatic response and confirmation as separate end-to-end user
   outcomes, extending Dapr Workflow one bounded step at a time.
5. Attach evidence to each story; aggregate it later through a release gate.

### Alternatives Considered

| Option | Verdict | Rationale |
| --- | --- | --- |
| Keep current Epic 5 and merely split Story 5.18 | Rejected | Does not fix the technical mega-epic, oversized stories, serial path, or unbounded ownership. |
| Roll back Epics 1-4 | Rejected | The completed fail-closed foundation remains useful and historically valid. |
| Reduce the V1 MVP | Rejected | The readiness report found complete requirement coverage; removing features would not resolve ownership or delivery structure. |
| Replan vertically and gate external seams | Recommended | Preserves scope and prior work while making stories estimable, independently demonstrable, and evidence-producing. |

### Effort, Risk, And Timeline Impact

- **Change scope:** Major.
- **Planning effort:** Medium. Four epics, two registers, and artifact
  reconciliation must be completed before development resumes.
- **Implementation effort:** High; the required live capabilities do not shrink.
- **Risk after replan:** Medium, down from High, because external uncertainty and
  cross-system evidence become explicit entry gates.
- **Calendar impact:** a reliable calendar date cannot be set until every
  critical external dependency has an owner, repository, target version/commit,
  and target integration date. The replan intentionally makes that fact visible.
- **Parallelization:** Provider/safety/tokenizer commitments and governance design
  may proceed alongside live setup. Confirmation UI/read models may proceed after
  the shared interaction workflow contract is stable; they do not wait for real
  launch-cohort attainment.

## 4. Detailed Change Proposals

### 4.1 Preserve The Prior Approved Proposal

**Artifact:** `sprint-change-proposal-2026-08-01.md`

**OLD:** The same-date proposal is the default output path for the workflow and
could be overwritten by this rerun.

**NEW:** Preserve it unchanged as an approved historical record. Write this
proposal to `sprint-change-proposal-2026-08-01-readiness-rerun.md`. State that the
new proposal supersedes only the active Epic 5 backlog if approved.

**Rationale:** The first proposal contains approved decisions that remain valid;
the rerun exposes defects in their delivery decomposition.

### 4.2 Replace The Active Epic 5 With Four Vertical Epics

**Artifact:** `epics.md`, Course Correction Authority and forward plan

**OLD:**

> Epic 5 is the only forward implementation plan.

> Epic 5: Production Binding And Live Conformance

> Stories 5.1-5.18 progress through hosting, EventStore, identity, read models,
> capability contracts, Dapr Workflow, context, safety, Provider/cost,
> Conversations, UI, governance, metrics, and whole-system reassessment.

**NEW:**

#### Epic 5: Live Governed Setup And Honest Readiness

Administrator outcome: configure `hexa` through live public operations and see
an authoritative, tenant-scoped explanation of whether it is eligible to run.

| Story | Demonstrable outcome |
| --- | --- |
| 5.1 Configure `hexa` Through Live EventStore Operations | Agent commands persist/replay; public queries and setup UI show fresh authorized state. |
| 5.2 Govern Provider Models And Pricing Through Live Operations | Provider catalog, capabilities, pricing, and readiness are durable, queryable, secret-safe, and tenant-safe. |
| 5.3 Prove Tenant, Party, And Approver Readiness | Live tenant projection, Party references, revocation, and Approver bases fail closed before side effects. |
| 5.4 Publish Authoritative Readiness And Provider-State Contracts | Gate registry, freshness, degraded semantics, callability, API/UI parity, and evidence references are explicit. |
| 5.5 Compose Agents In The Platform-Owned Production-Like Host | The designated platform host composes Agents, dependencies, Dapr Workflow, secret store, telemetry, and topology tests. |
| 5.6 Activate `hexa` Only When Setup Gates Pass | Activation/callability are separate; every required setup gate is current and evidenced before the UI/API reports callable. |

#### Epic 6: One Safe Automatic Conversation Response

Participant outcome: use the single **Call hexa** action and receive exactly one
safe, attributed response—or a precise fail-closed status before unsafe effects.

| Story | Demonstrable outcome |
| --- | --- |
| 6.1 Start And Recover An Automatic Interaction | Dapr Workflow starts from one accepted call, survives restart/replay, and records EventStore business transitions without duplicate ownership. |
| 6.2 Use The Complete Authorized Conversation Or Block | Fresh full context and provider-specific token measurement are repeated before the attempt; no bounded fallback exists. |
| 6.3 Block Unsafe Prompt, Context, Or Output | The selected live safety adapter gates before Provider and before posting with versioned evidence and no weaker retry. |
| 6.4 Generate Within Hard Cost Reservations | A prepared attempt, live Provider adapter, secret reference, atomic reservation, reconciliation, retry reuse, and safe usage evidence produce one generated version. |
| 6.5 Join And Post Exactly Once As `hexa` | `CONV-AI-1` establishes limited AI membership; deterministic append posts once with Agent Party attribution. |
| 6.6 Call `hexa` And Follow Automatic Status Accessibly | The sole Conversation action, cancel behavior, status states, focus, live regions, responsive fail-closed behavior, and localization use live contracts. |

#### Epic 7: Complete Confirmation And Approval

Approver outcome: discover, revise, resolve, and post exactly one proposal version
without losing history or bypassing current gates.

| Story | Demonstrable outcome |
| --- | --- |
| 7.1 Create And Discover A Pending Proposal | Safe generation creates one proposal, pending count, and authorized Needs my action entry outside Conversations. |
| 7.2 Edit An Immutable Proposal Version | One explicit edit preserves source version, editor, audit evidence, and all prior content. |
| 7.3 Regenerate Under Fresh Gates | Regeneration repeats full context, Provider capability, safety, and cost rules and appends one immutable version. |
| 7.4 Approve And Post One Selected Version | Current authority selects one version and posts it once through the live Conversations seam. |
| 7.5 Reject Or Abandon A Proposal | Each action is independently authorized, terminal, auditable, and reflected accessibly in API/UI. |
| 7.6 Expire A Proposal Deterministically | Stored `ExpiresAt`, workflow timer, restart, and concurrency races yield one authoritative terminal ordering. |

#### Epic 8: Governance Operations And Release Qualification

Governance/release outcome: operate sensitive content controls and determine
launch eligibility from current evidence without turning assessment into a story.

| Story | Demonstrable outcome |
| --- | --- |
| 8.1 Retain Sensitive Content And Apply Legal Holds | The 365-day rule, hold/release, restart, and restrictive status are live and auditable. |
| 8.2 Export Authorized Audit Content Securely | Export is tenant-scoped, encrypted, time-limited, manifested, audited, and secret-safe. |
| 8.3 Delete Protected Content And Purge Projections | Cryptographic erasure/redaction and every inventory-listed projection confirm restrictive completion. |
| 8.4 Operate Safety, Cost, And Governance Policies | Versioned policy authoring, confirmations, current authorization, budget warnings/blocks, and projection-confirmed success are coherent. |
| 8.5 Calculate Latency And Product Metrics Deterministically | Approved fixtures prove formulas, samples, windows, cohorts, insufficiency, and timestamps without waiting 30 real days. |
| 8.6 Inspect Launch Evidence And Blockers | The readiness surface aggregates existing gate/evidence records; it does not execute whole-system conformance. |

#### Release Gate RQ-1: Final Implementation Readiness

`RQ-1` is not a development story and is not estimated as one. It runs only after
Epics 5-8 and all critical dependencies are complete. It checks live operational
attainment, required Level 4/5 evidence, real or explicitly approved
production-like samples, unresolved blockers, and produces a new dated
READY/NOT READY report.

**Rationale:** Each epic now has a user/operator outcome. Whole-system evidence
is produced incrementally and aggregated by a gate rather than deferred to a
release-program story.

### 4.3 Preserve And Map The Superseded Epic 5 Plan

**Artifact:** `epics.md` historical appendix

**OLD:** Stories 5.1-5.18 remain active-looking backlog entries.

**NEW:** Preserve the exact old Epic 5 section in
`epic-5-superseded-2026-08-01.md` with:

```yaml
status: superseded
supersededOn: 2026-08-01
mustNotImplement: true
supersededBy: sprint-change-proposal-2026-08-01-readiness-rerun.md
```

Add this mapping:

| Old story | Replacement authority |
| --- | --- |
| 5.1 | 5.5 plus dependency `EXT-HOST-1` |
| 5.2 | 5.4, 5.6, 8.6, and `RQ-1` |
| 5.3 | 5.1 and 5.2 |
| 5.4 | 5.3 and proposal-action stories 7.2-7.6 |
| 5.5 | Distributed into 5.1, 7.1, and 8.6 by owned read model |
| 5.6 | 5.2, 5.4, 6.2, and 6.4 |
| 5.7 | 6.1-6.5 and 7.1, 7.3, 7.4, 7.6 |
| 5.8 | 6.2 |
| 5.9 | 6.3 |
| 5.10 | 6.4 |
| 5.11 | 6.5 plus dependency `EXT-CONV-AI-1` |
| 5.12 | 6.6 |
| 5.13 | 7.1 |
| 5.14 | 7.2-7.6 |
| 5.15 | 8.1 |
| 5.16 | 8.2 and 8.3 |
| 5.17 | 5.4, 8.4-8.6 |
| 5.18 | Per-story evidence plus release gate `RQ-1` |

**Rationale:** Nothing is silently deleted, but no obsolete criterion remains an
active implementation instruction.

### 4.4 Add An External Dependency Register

**New artifact:** `external-dependency-register.md`

**OLD:** Dependencies appear in prose or individual story dependencies without a
single readiness contract.

**NEW:** Every entry requires owner, repository, artifact, target version or
commit, compatibility contract, target integration date, verification command,
required evidence level, status, and consuming stories. Initial entries:

| ID | Owner / repository | Required artifact and consumers |
| --- | --- | --- |
| EXT-CONV-AI-1 | Conversations Maintainer / `Hexalith.Conversations` | `AddParticipantAsync` plus participant API, typed conflicts, idempotency, cross-tenant denial; required by 6.5 and 7.4. |
| EXT-HOST-1 | Platform Maintainer and Solution Architect / designated platform-host repository | Package-consuming Agents topology with Dapr Workflow, EventStore, Conversations, Parties, Tenants, Provider, safety, secrets, telemetry; required by 5.5 onward. |
| EXT-PROVIDER-1 | Agents Runtime Maintainer / selected adapter artifact | Selected Provider/model adapter and version, availability/error/usage contract; required by 6.4. |
| EXT-SAFETY-1 | Security Engineering / selected safety adapter artifact | Versioned prompt/context/output decision contract and availability behavior; required by 6.3. |
| EXT-TOKEN-1 | Provider Adapter Owner / selected tokenizer artifact | Provider/model-specific exact token measurement and compatibility version; required by 6.2. |
| EXT-SECRETS-1 | Platform Security / platform-host secret composition | Secret reference resolution, rotation, denial, and leak-scan evidence; required by 5.5 and 6.4. |
| EXT-TOPOLOGY-1 | Test Architect and Platform Maintainer / production-like fixture | Named topology, infrastructure versions, reset/seed process, evidence capture, and supported failure injection; required by each Level 5 claim and `RQ-1`. |

An entry with `Uncommitted`, missing target, or missing compatibility command
blocks the consuming story from `ready-for-dev`.

**Rationale:** External work becomes a managed delivery contract rather than an
assumption discovered late in a story.

### 4.5 Strengthen The PRD Without Changing The MVP

**Artifact:** `prd.md`

#### External dependencies

**OLD:** Integration dependencies are descriptive and `CONV-AI-1` is named, but
commitment fields are not required.

**NEW:** State that a critical external dependency is implementation-ready only
when the external register contains a named owner, owning repository, artifact,
target version/commit, target integration date, compatibility test, evidence
level, and accepted status.

#### Residual assumptions

**OLD:** Complete-context exclusions and single-exposed-Agent behavior remain in
the Residual Assumptions Index.

**NEW:** Promote both to binding V1 decisions because Epics and tests already
depend on them. Keep generalized internal structures permissible but expose only
`hexa`; allow only complete authorized Conversation context and the stated
exclusions.

#### New NFRs

**OLD:** NFR-1 through NFR-10 do not define availability/recovery,
accessibility/localization/responsive authority, UI response targets, or
capacity/backpressure behavior.

**NEW:** Add:

- **NFR-11 Availability And Recovery:** EventStore business state has RPO 0;
  restart/replay cannot duplicate Provider attempts, proposal versions, timers,
  reservations, or Conversation posts. The production-like recovery exercise
  must restore interaction processing within 15 minutes and preserve every
  terminal decision.
- **NFR-12 Capacity And Backpressure:** before enablement, each environment
  records numeric per-tenant and system-wide concurrency, queue-depth, and
  backpressure limits. Exceeding a limit queues or rejects with a safe typed
  outcome before Provider invocation; cross-tenant fairness and cost caps remain
  enforced. The release profile and its numeric values live in the readiness
  registry rather than being hidden in host configuration.
- **NFR-13 Accessible, Localizable, Responsive UI:** the final UX spines are
  binding launch authority. Interactive V1 surfaces meet WCAG 2.2 AA behavior,
  use whole-string localization with English/French key parity, and fail closed
  for high-impact actions when the viewport cannot present required context.
- **NFR-14 UI Interaction Performance:** in the production-like profile, an
  authorized page reaches a usable non-loading state at p95 <= 2.5 seconds; a
  submitted command renders authoritative pending acknowledgement at p95 <= 500
  ms; a projection-visible terminal change renders and is announced at p95 <= 2
  seconds. Each gate uses at least 30 executions and reports insufficient
  evidence rather than pass when timestamps or samples are missing.

#### Contract evolution and evidence authority

**NEW:** Add a V1 compatibility rule: additive JSON members and enum values with
`Unknown = 0`; no removal/rename or semantic reuse within V1; breaking public
changes require a new major package/API version and package-consumer tests.

Move the Level 1-5 taxonomy into a normative PRD/architecture evidence section.
The Decision Register references that section; it no longer appears to define a
taxonomy that is absent from the register itself.

#### Metric calculation versus qualification

**NEW:** Approved deterministic fixtures may prove formula and insufficient-data
behavior. Real rolling-window/cohort attainment remains an operational `RQ-1`
gate and cannot block completion of the calculator implementation story.

**Rationale:** The MVP remains unchanged while product authority becomes precise
enough for release decisions.

### 4.6 Update Architecture Contracts And Inventories

**Artifact:** `ARCHITECTURE-SPINE.md`

#### Provider degraded semantics — AD-10

**OLD:** UX exposes `Degraded`; AD-10 requires `Status == Enabled` and does not
state whether degraded is callable.

**NEW:** Define a public readiness result with `OperationalState`, `Callability`,
safe `ReasonCode`, `CapabilityVersion`, `ObservedAt`, and `ValidUntil`.
`Degraded` is callable only when every AD-10 hard gate passes and the condition is
explicitly non-blocking; it is a warning layered over a trusted enabled entry.
Unknown, stale, unconfigured, unpriced, invalid-limit, failed, or disabled state
is `Blocked`, never `Degraded`.

#### High-risk command concurrency — AD-12/AD-13

**OLD:** UX suggests one high-risk action per user/session; architecture defines
only aggregate/idempotency behavior.

**NEW:** UI/BFF allows one pending high-risk command per resource and operation
family in a user session. Scope keys are proposal resolution, policy publication,
tenant budget update, legal hold, export request, and deletion request. Unrelated
resources may proceed concurrently. Client locking is advisory; EventStore
optimistic concurrency, deterministic identity, and idempotency are authoritative
across sessions. A stale client timeout forces status refresh and never implies
success.

#### Readiness registry and freshness — AD-17

**NEW:** Define a normative gate record:

```text
GateId, TenantScope, State, Owner, SourceVersion, ObservedAt, ValidUntil,
RequiredEvidenceLevel, EvidenceReference, BlockerCode
```

Allowed states are `Pass`, `Block`, `InsufficientEvidence`, and `Stale`. Missing
records and expired `ValidUntil` block. No global implicit freshness constant is
allowed; each gate declares its validity period and authoritative timestamp.

The minimum gate inventory is topology, EventStore, tenant access, Party
identity, Conversation context, Conversations membership/posting, Provider,
tokenizer, safety, secrets, cost, audit protection, UI conformance, recovery,
performance, and product metrics.

#### Projection inventory

**NEW:** Enumerate Agent setup/readiness, Provider capability/pricing,
AgentInteraction status, proposal detail/version history, pending queue/count,
audit evidence, budget reservation/usage, retention/legal-hold/export/deletion,
and launch-readiness/metric projections. Deletion and readiness criteria name the
affected entries rather than saying “every affected projection.”

#### Ownership

**NEW:** AD-16 references `EXT-HOST-1`; AD-11 references `EXT-TOKEN-1`; AD-20
references `EXT-SAFETY-1`; AD-9/AD-21 reference `EXT-PROVIDER-1` and
`EXT-SECRETS-1`; AD-6/AD-7 reference `EXT-CONV-AI-1`; AD-17 references
`EXT-TOPOLOGY-1`.

#### Stack baseline

**OLD:** `.NET 10.0.300-10.0.301`, Dapr `1.18.4`, Fluent UI
`5.0.0-rc.3-26138.1`, MediatR `14.1.0`, and Microsoft.Agents.AI `1.10.0`
“verified current.”

**NEW:** Adopt one canonical forward baseline and update `global.json`, the
central package selection, and planning docs together: SDK `10.0.302` with
`latestPatch`; Aspire `13.4.6`; Dapr .NET `1.18.5`; CommunityToolkit Aspire Dapr
`13.4.1-beta.687`; Fluent UI Blazor `5.0.0-rc.4-26180.1`; MediatR `14.2.0`;
FluentValidation `12.1.1`; xUnit v3 `3.2.2`; Shouldly `4.3.0`; root-selected
test overrides remain authoritative until deliberately changed. Provider and
Agent Framework SDK versions are `Unselected` until `EXT-PROVIDER-1` commits an
adapter and compatibility evidence.

**Rationale:** Runtime/UI behavior and reproducible builds now derive from named
contracts rather than prose interpretation or stale version claims.

### 4.7 Reconcile UX Semantics

**Artifacts:** `DESIGN.md`, `EXPERIENCE.md`

**OLD:** `Degraded` may continue when policy allows; active configuration appears
among Success examples; one high-risk action per session is advisory; no UI
performance gate exists.

**NEW:**

- Render Provider `Degraded` only from the AD-10 public readiness result. It is a
  Warning and may be callable only when `Callability == Callable`.
- Reserve Agent-readiness Success for proven `callable`; show active-but-blocked
  lifecycle independently with blocker text and no Success readiness styling.
- Apply the resource/operation-family pending-command scopes from architecture.
- Cite PRD NFR-13 and NFR-14 as binding accessibility, localization, responsive,
  and UI-performance authority.
- Add UI performance evidence to the FrontComposer conformance plan without
  weakening existing focus, live-region, grid-state, or secret-safety rules.

**Rationale:** UI state cannot authorize or visually imply a runtime condition
that the public readiness contract does not prove.

### 4.8 Make Historical Supersession Machine-Visible

**Artifact:** `epics.md`, historical authority table

**OLD:** Narrative precedence says Epic 5 wins, but stale criteria still look
executable.

**NEW:** Add `mustNotImplement: true` and exact replacement mappings for:

| Historical criterion | Replacement authority |
| --- | --- |
| Story 1.1 Agents-owned AppHost extension | AD-16, dependency `EXT-HOST-1`, Story 5.5 |
| Story 2.3 approved bounded-context behavior | PRD OQ-10, AD-11, Story 6.2 |
| Story 2.6 mention/command/combined invocation | PRD OQ-1, UX-DR24, Story 6.6 |
| Story 4.2 unresolved retention/export/deletion | PRD OQ-8, AD-22, Stories 8.1-8.3 |
| Story 4.4 reporting-only cost or accepted risk | PRD OQ-6, AD-21, Stories 6.4 and 8.4 |
| Story 4.5 Agent Framework workflow and MCP/A2A “where applicable” | AD-18/AD-19, per-story evidence, release gate `RQ-1` |

Completed story text and status remain unchanged.

### 4.9 Update Sprint Status After Approval

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD:** `epic-5` and Stories 5.1-5.18 are `backlog`.

**NEW:** After approval only:

- remove the superseded Epic 5 backlog rows;
- add replacement Epics 5-8 and their 24 stories as `backlog`;
- add optional retrospectives for Epics 5-8;
- do not represent `RQ-1` as a story; track it in the launch-readiness register;
- preserve every Epic 1-4 status and retrospective entry.

**Rationale:** Sprint status must represent executable work, while release
qualification remains a gate over evidence rather than a fake story.

## 5. Implementation Handoff

### Scope Classification

**Major.** This correction changes the forward backlog structure and adds
binding product, architecture, UX, dependency, and release contracts. It does
not change the V1 vision or rewrite completed implementation history.

### Routing And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Manager | Approve the four-epic outcome model, NFR-11 through NFR-14, metric-fixture distinction, and unchanged MVP. |
| Solution Architect | Finalize readiness schema, projection inventory, Provider degraded semantics, high-risk concurrency, stack baseline, and ownership references. |
| Product Owner | Replace the active Epic 5 backlog, add explicit dependencies/DoR fields, and synchronize sprint status after approval. |
| Conversations Maintainer | Commit `EXT-CONV-AI-1` owner, version/commit, date, compatibility command, and Level 4/5 evidence. |
| Platform Maintainer | Commit `EXT-HOST-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`; provide the production-like host/fixture. |
| Agents Runtime Maintainer | Commit Provider and tokenizer adapter selections and implement Stories 6.1-6.4 incrementally. |
| Security Engineering | Commit the live safety adapter and validate no-weaker-retry, secret, and deletion controls. |
| UX Designer | Apply callable/degraded/concurrency/performance semantics and preserve FrontComposer/Fluent V5 conformance. |
| Test Architect | Define evidence manifests per story, deterministic metric fixtures, live failure injection, and release gate `RQ-1`. |
| Developer agents | Implement only approved replacement stories whose dependency records satisfy Definition of Ready. |

### Definition Of Ready For Replacement Stories

A story may move to `ready-for-dev` only when:

1. every declared external dependency is `Committed` or `Available`;
2. its public contract and authoritative projection/gate IDs are named;
3. acceptance criteria contain one primary demonstrable outcome;
4. focused happy, denial, cross-tenant, idempotency/replay, and failure evidence is
   named where applicable;
5. required topology, fixture, and verification commands are recorded;
6. no superseded historical acceptance criterion is used as authority.

### Success Criteria

1. The approved prior proposal and Epics 1-4 remain intact.
2. The old Epic 5 is preserved but machine-marked non-executable.
3. Epics 5-8 contain independently demonstrable vertical stories with explicit
   dependency and evidence ownership.
4. `EXT-CONV-AI-1`, host, Provider, safety, tokenization, secrets, and topology
   each have committed delivery records before consuming stories start.
5. Provider degraded/callability, active-vs-callable styling, and high-risk
   command concurrency have one shared API/runtime/UX meaning.
6. Readiness freshness, gate inventory, projection inventory, and evidence levels
   are normative and machine-testable.
7. Build baselines match the selected workspace catalog and unselected Provider
   SDKs are not presented as authoritative pins.
8. Every replacement story emits focused evidence; no final story owns all
   conformance.
9. Metric calculation tests do not wait for real adoption, while `RQ-1` still
   requires approved operational attainment.
10. A later implementation-readiness rerun reports zero critical structural or
    external-ownership violations before Phase 4 authorization.

## 6. Checklist Summary

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 | Done | Trigger is the 2026-08-01 readiness rerun; the previous Correct Course delivery plan is the affected planning context. |
| 1.2 | Done | Failed delivery decomposition and artifact reconciliation; no product pivot. |
| 1.3 | Done | The report's 29 entries, current artifacts, package catalog, source contracts, and sprint status provide concrete evidence. |
| 2.1 | Done | The current forward Epic 5 cannot complete as an implementation-ready epic. |
| 2.2 | Done | Replace it with four vertical outcome epics and a non-story release gate. |
| 2.3 | Done | Epics 1-4 remain historical; only the unstarted forward plan changes. |
| 2.4 | Done | No completed epic is obsolete; new external/readiness registers are required. |
| 2.5 | Done | Dependencies and honest setup precede calls; shared automatic path precedes confirmation posting; governance work can parallelize. |
| 3.1 | Done | MVP and 28 FRs remain; PRD receives operational/UI NFR and authority precision. |
| 3.2 | Done | Architecture receives readiness, degraded, concurrency, ownership, projection, topology, and version contracts. |
| 3.3 | Done | UX semantics align to runtime callability and gain binding performance authority. |
| 3.4 | Action-needed | Dependency register, topology, adapters, CI/evidence lanes, and release register require implementation after approval. |
| 4.1 | Viable | Direct adjustment is viable after vertical replan; High effort, Medium residual risk. |
| 4.2 | Not viable | No completed implementation rollback is justified. |
| 4.3 | Not viable | MVP reduction would not solve the delivery defects. |
| 4.4 | Done | Vertical replan with gated external dependencies selected. |
| 5.1 | Done | Issue summary and evidence recorded. |
| 5.2 | Done | Epic, story, artifact, UX, dependency, technical, and release impacts recorded. |
| 5.3 | Done | Recommended path, trade-offs, effort, risk, and timeline conditions recorded. |
| 5.4 | Done | MVP unchanged; four epics, 24 stories, registers, and `RQ-1` defined. |
| 5.5 | Done | Major PM/Architect-led handoff and role responsibilities defined. |
| 6.1 | Done | All checklist sections are addressed; implementation actions remain explicit. |
| 6.2 | Done | Draft checked against the readiness findings and current canonical artifacts. |
| 6.3 | Done | Administrator explicitly approved the complete proposal on 2026-08-01. |
| 6.4 | Action-needed | Product Owner must synchronize sprint status when the PM/Architect replan applies the replacement epics; no executable epic IDs are added before `epics.md` contains the approved stories. |
| 6.5 | Done | Handoff recipients, Definition of Ready, sequencing, and success criteria are explicit. |

## 7. Approval Record

- Mode: Batch.
- Complete-proposal review: continued by Administrator.
- Implementation approval: approved by Administrator on 2026-08-01.
- Scope classification: Major.
- Route: Product Manager and Solution Architect, with Product Owner backlog
  synchronization and the cross-functional responsibilities in Section 5.
- No PRD, Architecture, UX, Epics, or sprint-status source artifact has been
  modified by this workflow run; those are the approved replan's implementation
  tasks and must be synchronized together.
- The prior approved same-date proposal remains unchanged.

## 8. Handoff And Workflow Execution Log

- 2026-08-01 — Correct Course activated for the implementation-readiness rerun
  and Batch mode selected by Administrator.
- 2026-08-01 — Repository instructions, resolved skill customization, seven
  persistent project-context files, BMAD configuration, and the complete change
  navigation checklist were loaded.
- 2026-08-01 — Final PRD bundle, Architecture Spine and conventions, Epics,
  final UX spines, readiness report, prior approved proposal, package baselines,
  source seams, and sprint status were assessed.
- 2026-08-01 — Checklist Sections 1-4 completed; vertical replan with gated
  external dependencies selected as the recommended path.
- 2026-08-01 — Batch edit proposals produced for four replacement epics, 24
  stories, external/readiness registers, PRD NFRs, architecture contracts, UX
  semantics, historical supersession, package baselines, and sprint status.
- 2026-08-01 — Administrator continued complete-proposal review and explicitly
  approved the Sprint Change Proposal for implementation.
- 2026-08-01 — Major-scope handoff routed to Product Manager and Solution
  Architect. Product Owner owns backlog and sprint-status synchronization after
  canonical artifact edits; Developer agents remain gated by the Definition of
  Ready and committed external dependencies.

### Handoff Completion

The proposal handoff is complete when the Product Manager and Solution Architect
accept ownership of the canonical artifact reconciliation. Their first execution
checkpoint is to create and commit the dependency/readiness registers, then
apply the PRD, architecture, UX, and epic changes as one consistent planning
change before updating `sprint-status.yaml`.
