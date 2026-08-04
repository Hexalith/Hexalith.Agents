---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
assessmentStatus: NOT READY
includedFiles:
  prd:
    - prds/prd-agents-2026-06-23/prd.md
    - prds/prd-agents-2026-06-23/addendum.md
  architecture:
    - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
    - architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md
  epics:
    - epics.md
  ux:
    - ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
    - ux-designs/ux-agents-2026-06-23/DESIGN.md
excludedFiles:
  - epic-5-superseded-2026-08-01.md
  - PRD editorial, review, reconciliation, and validation artifacts
  - Architecture review artifacts
  - UX review and validation artifacts
  - Sprint change proposals and prior readiness reports
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-03
**Project:** agents

## Document Discovery

Document discovery identified folder-based PRD, architecture, and UX document sets without `index.md` entry points, plus whole epic documents. No whole-versus-sharded duplicate formats were found. The canonical assessment inputs and exclusions are recorded in the report frontmatter.

## PRD Analysis

### Functional Requirements

FR-1: Agent Administrators can create or enable `hexa` with a stable Agent identity, display name, description, Agent Instructions, lifecycle state, and tenant scope.

FR-2: Agent Administrators can provision or link the Agent's Party identity so `hexa` appears as a known AI participant when it posts to a Conversation.

FR-3: Agent Administrators can activate, disable, and inspect `hexa` lifecycle state.

FR-4: Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata needed for Agent selection.

FR-5: Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate.

FR-6: Agent Administrators can configure `hexa` for Automatic Response Mode or Confirmation Response Mode.

FR-7: Agent Administrators can define all Approvers through the Agent's Approver Policy, including policy sources such as the Conversation owner, the caller, predefined Parties, or tenant roles.

FR-8: Authorized Conversation Participants can explicitly call `hexa` from a Source Conversation with a user prompt or request.

FR-9: The system supplies the Agent with Conversation Context according to the configured Conversation Context Policy.

FR-10: The system handles Provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses.

FR-11: When `hexa` is configured for Automatic Response Mode, the system posts successful generated content to the Source Conversation as a Conversation Message attributed to the Agent's Party identity.

FR-12: The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid.

FR-13: When `hexa` is configured for Confirmation Response Mode, successful generation creates a Proposed Agent Reply linked to the Source Conversation and Agent Call.

FR-14: The system preserves every generated, edited, and regenerated content version for each Proposed Agent Reply.

FR-15: Authorized Approvers can edit Proposed Agent Reply content before approval.

FR-16: Authorized Approvers can request regeneration of a Proposed Agent Reply before approval.

FR-17: Authorized Approvers can approve a selected proposal version, causing it to be posted to the Source Conversation as `hexa`.

FR-18: Authorized Approvers or system policy can move a Proposed Agent Reply to rejected, abandoned, or expired terminal states.

FR-19: The system enforces tenant isolation across Agent configuration, Provider selection, Agent Calls, Conversation Context, Proposed Agent Replies, Conversation posting, and Audit Evidence.

FR-20: The system enforces authorization for Agent administration, provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection.

FR-21: The system fails closed when required Party, Conversation, Provider, Agent, tenant access, or approval policy state is missing, stale, ambiguous, disabled, or unavailable.

FR-22: The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation and proposal status.

FR-23: The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection.

FR-24: The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages.

FR-25: The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes.

FR-26: Authorized administrators or release operators can define the active Content Safety Policy for `hexa`.

FR-27: The system applies Content Safety Policy to the prompt and complete authorized Conversation Context before Provider invocation, then applies it to generated output before that output becomes a Conversation Message or an approvable Proposed Agent Reply.

FR-28: V1 launch readiness requires the fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, NFR-11 through NFR-14, external dependency commitments, and the normative evidence authority in §11.

**Total FRs: 28**

### Non-Functional Requirements

NFR-1 Security: Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.

NFR-2 Privacy: Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.

NFR-3 Reliability: Agent Calls must never create partial Conversation Messages on failure. Confirmation workflows must not lose generated or edited proposal versions.

NFR-4 Observability: The system must expose enough status to debug configuration errors, Provider failures, authorization denials, pending approval bottlenecks, and posting failures.

NFR-5 Auditability: Audit Evidence must preserve all generated and edited proposal versions and link final posted responses to their source call and approval path.

NFR-6 Provider Safety: Provider secrets must be write-only or secret-backed where applicable and must never appear in logs, status payloads, audit records, or UI display.

NFR-7 Content Safety: Agent generation must be governed by an active Content Safety Policy before generated content can create Conversation side effects.

NFR-8 Context Bounds: Conversation Context must not be truncated, summarized, windowed, or otherwise reduced; oversized Conversations fail closed before Provider invocation.

NFR-9 Performance: Automatic accepted-call-to-post latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; confirmation accepted-call-to-proposal latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; approval-to-post latency is p95 ≤ 10 seconds and p99 ≤ 30 seconds; fast pre-Provider rejection latency is p95 ≤ 2 seconds. Each gate requires at least 30 production-like executions.

NFR-10 Cost Control: Hard per-tenant monthly and per-call caps warn at 80%, fail closed at 100%, and use atomic reservation plus reconciliation. Reporting-only controls do not satisfy launch readiness.

NFR-11 Availability And Recovery: EventStore business state has RPO 0. Restart or replay cannot duplicate Provider attempts, proposal versions, timers, reservations, or Conversation posts. A production-like recovery exercise restores interaction processing within 15 minutes and preserves every terminal decision.

NFR-12 Capacity And Backpressure: Before enablement, each environment records numeric per-tenant and system-wide concurrency, queue-depth, and backpressure limits. Exceeding a limit queues or rejects with a safe typed outcome before Provider invocation. Cross-tenant fairness and cost caps remain enforced. The release profile and numeric limits are visible in the readiness registry, not hidden in host configuration.

NFR-13 Accessible, Localizable, Responsive UI: The final UX spines are binding launch authority. Interactive V1 surfaces meet WCAG 2.2 AA behavior, use whole-string localization with English/French key parity, and fail closed for high-impact actions when the viewport cannot present required context.

NFR-14 UI Interaction Performance: In the production-like profile, an authorized page reaches a usable non-loading state at p95 ≤ 2.5 seconds; a submitted command renders an authoritative pending acknowledgement at p95 ≤ 500 ms; a projection-visible terminal change renders and is announced at p95 ≤ 2 seconds. Each gate uses at least 30 executions and returns `InsufficientEvidence`, not pass, when timestamps or samples are missing.

**Total NFRs: 14**

### Additional Requirements

- V1 exposes only the named Agent `hexa` and only through a Conversation-owned **Call hexa** action. Long-term memory, tools, project/folder/file retrieval, ambient triggers, agent-to-agent orchestration, external channels, and non-conversation business actions are explicitly excluded.
- Complete authorized Source Conversation context is mandatory. Calls fail closed before Provider invocation when complete context cannot be loaded, safely sent, or fit within the selected model's safe context budget; truncation, summarization, and windowing are prohibited.
- Proposed Agent Replies remain outside Conversations until approval. Every generated, edited, and regenerated version is immutable audit history; terminal proposals cannot later be approved or posted.
- Proposal expiry defaults to 24 hours and is configurable from 1 hour through 30 days for future proposals. A durable Dapr Workflow timer expires proposals from their stored `ExpiresAt`.
- Sensitive Agent content is retained for 365 days after terminal state unless legal hold applies. Authorized export must be tenant-scoped, encrypted, time-limited, manifested, and audited.
- EventStore history is never rewritten. Approved deletion or expiry cryptographically erases or redacts protected content, purges affected projections, and retains only a support-safe non-content tombstone with restrictive completion confirmation.
- Public JSON evolution is additive within V1; public enums use `Unknown = 0`; existing members and meanings cannot be removed, renamed, or reused. Breaking changes require a new major version and package-consumer compatibility tests.
- Critical dependencies are ready only when the external dependency register records owner, repository, artifact, target version/commit, integration date, compatibility contract and command, required Evidence Level, accepted status, and consuming stories. `Uncommitted`, a missing target, or a missing verification command blocks consuming stories from `ready-for-dev`.
- Initial critical dependency scope is `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`.
- Production-like readiness requires live Evidence Levels 4 and 5 for runtime behavior, authorization, tenant isolation, Provider integration, content safety, Conversations integration, audit behavior, and topology. Lower-level, skipped, placeholder, or conditional evidence cannot establish readiness.
- Each qualification metric requires a versioned measurement contract covering authoritative events and timestamps, formula or percentile method, samples/windows/cohorts, late or missing data, and `InsufficientEvidence` handling.
- Production enablement remains blocked until `RQ-1` records READY from required live evidence and launch metrics.
- Success thresholds include at least one enabled launch tenant with successful calls; at least 20% adoption across at least 50 eligible Conversations over 30 days; and at least 95% of proposals reaching terminal state within 26 hours, with expiry ≤ 20%, posting failures ≤ 2%, human resolution ≥ 70%, and audit completeness = 100%.
- The selected addendum is explicitly contextual landscape material and adds no normative product requirements.

### PRD Completeness Assessment

The PRD is structurally complete and unusually explicit for implementation planning. Its 28 FRs and 14 NFRs are contiguous, testable, and supported by scope boundaries, quantified performance and success thresholds, a binding decision register, public-contract rules, governance constraints, and normative evidence levels. All stated implementation-blocking product and governance questions are marked resolved. Remaining readiness risk is intentionally delegated to evidence-bearing artifacts—especially the external dependency register, launch-readiness registry, architecture, UX spines, epics, and live `RQ-1` qualification—rather than caused by missing or ambiguous PRD requirements.

## Epic Coverage Validation

### Coverage Matrix

| FR | PRD Requirement | Claimed active epic/story coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Configure `hexa` | Epic 5; Stories 5.2 and 5.7 | Covered |
| FR-2 | Link Agent to exactly one Party identity | Epics 5 and 6; Stories 5.4 and 6.6 | Covered |
| FR-3 | Manage Agent lifecycle | Epic 5; Stories 5.2 and 5.7 | Covered |
| FR-4 | Manage Global Providers Aggregate | Epics 5 and 8; Stories 5.3 and 8.4 | Covered |
| FR-5 | Select Provider and model per Agent | Epics 5 and 6; Stories 5.3, 6.2, and 6.4 | Covered |
| FR-6 | Configure response mode | Epic 5; Story 5.2 | Covered |
| FR-7 | Configure Approver Policy | Epics 5 and 7; Stories 5.4, 7.1, and 7.4 | Covered |
| FR-8 | Call Agent from Conversation | Epic 6; Stories 6.1 and 6.7 | Covered |
| FR-9 | Build complete-or-blocked Conversation Context | Epic 6; Story 6.2 | Covered |
| FR-10 | Handle generation failure safely | Epic 6; Stories 6.1–6.4 and 6.7 | Covered |
| FR-11 | Post automatic response as `hexa` | Epic 6; Stories 6.6 and 6.7 | Covered |
| FR-12 | Prevent automatic posting when any gate fails | Epic 6; Stories 6.3 and 6.5–6.7 | Covered |
| FR-13 | Create Proposed Agent Reply | Epic 7; Story 7.1 | Covered |
| FR-14 | Preserve all proposal versions | Epic 7; Stories 7.1–7.5 | Covered |
| FR-15 | Edit Proposed Reply | Epic 7; Story 7.2 | Covered |
| FR-16 | Regenerate Proposed Reply | Epic 7; Story 7.3 | Covered |
| FR-17 | Approve and post selected version | Epic 7; Story 7.4 | Covered |
| FR-18 | Reject, abandon, or expire proposal | Epics 7 and 8; Stories 7.5, 7.6, 8.1, and 8.3 | Covered |
| FR-19 | Enforce tenant isolation | Epics 5–8; cross-cutting negative evidence in active story manifests | Covered |
| FR-20 | Enforce role and policy authorization | Epics 5–8; cross-cutting authorization clauses in active story manifests | Covered |
| FR-21 | Fail closed on dependency uncertainty | Epics 5–8; dependency gates and blocked results throughout active stories | Covered |
| FR-22 | Provide admin UI | Epics 5–8; live UI slices and Story 8.6 qualification | Covered |
| FR-23 | Provide stable API/client contracts | Epics 5–8; public-contract clauses throughout active stories | Covered |
| FR-24 | Capture Agent Audit Evidence | Epics 5–8; evidence manifests throughout active stories | Covered |
| FR-25 | Expose operational status | Epics 5–8; status/readiness clauses throughout active stories | Covered |
| FR-26 | Configure Content Safety and prompt policy | Epics 6 and 8; Stories 6.3 and 8.4 | Covered |
| FR-27 | Enforce safety before Provider and Conversation side effects | Epics 6 and 7; Stories 6.3, 7.3, and 7.4 | Covered |
| FR-28 | Define launch-readiness controls | Epics 5–8; especially Stories 5.5, 5.7, and 8.5–8.7 | Covered |

### Missing Requirements

No PRD Functional Requirement is missing from the active epic coverage map. The epic document contains no additional FR identifier absent from the PRD. The epic inventory uses the compact `FR1` form while the PRD uses `FR-1`; these are treated as the same identifiers.

Historical Epics 1–4 also claim coverage, but they are explicitly historical evidence only and were not counted as the current implementation path. The active replacement authority is Epics 5–8 and their 27 stories; release gate `RQ-1` is correctly identified as a non-estimated operational gate rather than an implementation story.

### Coverage Statistics

- Total PRD FRs: 28
- FRs covered in active epics: 28
- Missing PRD FRs: 0
- Epic-only FR identifiers: 0
- Functional-requirement coverage: 100%

## UX Alignment Assessment

### UX Document Status

Found. The selected final UX authority consists of `EXPERIENCE.md` for behavior, surfaces, canonical states, accessibility, responsive behavior, interaction flows, and browser-performance evidence, plus `DESIGN.md` for the inherited FrontComposer/Fluent V5 visual and component semantics. Both are final and updated 2026-08-02.

### UX ↔ PRD Alignment

- PRD journeys UJ-1 through UJ-4 map directly to the UX flows for Nora, Milan, Anika, and Omar.
- UX adds UJ-5 for production-like launch governance; it is supported by FR-28, NFR-9 through NFR-14, PRD §9, §11, §12, and the binding V1 Decision Register rather than introducing an unrelated product capability.
- UX preserves the sole Conversation-owned **Call hexa** entry, named Agent Party attribution, automatic and confirmation response modes, full-context-or-blocked policy, proposal version immutability, in-product proposal notification, and generation-failure separation required by the PRD.
- Provider, safety, cost, retention/legal hold, encrypted export, deletion/purge, readiness, status, and audit surfaces all trace to PRD FRs or explicit additional governance requirements.
- UX uses the exact PRD NFR-13 and NFR-14 requirements: WCAG 2.2 AA; whole-string English/French parity; restrictive-viewport blocking for high-impact actions; p95 ≤ 2.5 seconds page usability; p95 ≤ 500 milliseconds authoritative-pending render; p95 ≤ 2 seconds terminal render/announcement; and at least 30 qualifying executions per sample kind.
- UX preserves the PRD truth boundaries: lifecycle `active` is not callability, `approved` is not posted, generation failure creates no proposal, and only authoritative `posted` evidence proves a Conversation Message exists.
- No UX capability was found that contradicts a PRD non-goal. Tools, memory, project/folder retrieval, ambient triggers, alternate invocation entries, external notifications/channels, and multiple named V1 Agents remain excluded.

### UX ↔ Architecture Alignment

- AD-15 explicitly makes public API/client and UI contracts share authorization outcomes and places the UI behind FrontComposer and Agents client/API boundaries.
- AD-25 directly supports the UX WCAG 2.2 AA, localization, route/state inventory, responsive safety, and `InsufficientEvidence` rules.
- AD-26 defines the same discriminated browser-monotonic sample kinds, authoritative ticks, thresholds, correlation rules, and minimum sample counts used by the UX spines.
- AD-10 supports the exact Provider readiness triples used by `provider-status-badge`; the UI does not infer callability.
- AD-12 and AD-13 support the UX high-risk pending-command scope and authoritative `submitted -> authoritative pending -> projection-confirmed terminal` state model.
- AD-5, AD-6, AD-7, AD-11, AD-20, AD-21, and AD-22 supply the proposal, Conversations, Party identity, complete-context, safety, cost, and audit-governance foundations required by UX.
- The architecture capability map and projection inventory cover the UX setup, Provider catalog, invocation, proposal queue/editor/history, operational status, audit, readiness, governance, and browser-evidence surfaces.
- FrontComposer FC-LYT, FC-TBL, FC-A11Y, FC-L10N, policy-gated navigation, Fluent UI Blazor V5, and the FluentAccordion multi-section pattern are consistently reflected in UX requirements and active epic stories.

### Alignment Issues

1. **Conversation owner terminology:** PRD FR-7 and UX name “Conversation owner” as an Approver Policy source. Architecture AD-8 states that current Conversations contracts expose `ParticipantRole.Facilitator` but no owner field, so V1 treats owner authority as Facilitator unless Conversations adds an explicit resolver. This is a documented, bounded substitution rather than an unnoticed conflict, but implementation and user-facing disclosure must consistently name the actual policy basis. Story 5.4 should preserve explicit evidence and API/UI parity for this mapping.

No other material PRD↔UX or UX↔architecture misalignment was found.

### Warnings

- Architecture records current implementation gaps—especially module-owned hosting projects and missing Provider capability high-water/effective-version behavior—and seven uncommitted external prerequisites. These do not make the UX specification internally inconsistent, but they block production realization and will be assessed in readiness conclusions.
- UX conformance and UI-performance success require complete live route/state inventories and production-like Levels 4–5 evidence. Component tests, deterministic timing fixtures, or partial browser coverage cannot independently establish launch readiness.

## Epic Quality Review

### Review Scope

The quality review evaluated all eight epic sections and all stories in `epics.md`. Epics 1–4 are explicitly completed historical evidence and were checked for current-authority hazards, but only active replacement Epics 5–8 and their 27 stories were evaluated as the executable forward backlog.

### Epic Structure

| Epic | User-value outcome | Independence | Assessment |
| --- | --- | --- | --- |
| 5 — Live Governed Setup And Honest Readiness | Administrator can configure `hexa` and see authoritative callability | Does not require Epics 6–8 | Pass |
| 6 — One Safe Automatic Conversation Response | Participant receives exactly one safe attributed response or a precise blocked outcome | Uses Epic 5 only; does not require Epic 7 | Pass |
| 7 — Complete Confirmation And Approval | Approver can discover, revise, resolve, and post one selected version | Builds on prior setup/runtime foundations; no dependency on Epic 8 | Pass with dependency correction required |
| 8 — Governance Operations And Release Qualification | Governance/release operators can retain, export, delete, govern, calculate, and inspect evidence | Uses prior completed capabilities; does not depend on `RQ-1` | Pass, but broad |

All active epic goals state observable user/operator value rather than technical milestones. `RQ-1` is correctly kept outside the implementation backlog. No active epic depends on a later epic.

### Story Dependency Findings

#### Critical Violations

No active story declares a direct dependency on a later story, and no circular story dependency was found.

#### Major Issues

1. **Story 7.4 underdeclares its posting dependency.** The story approves and posts through the same limited AI membership, deterministic `MessageId`, idempotency, recovery, and Conversations boundary established by Story 6.6, but its prior-story list names only Story 7.1 and treats 7.2/7.3 as optional. Story 7.1 depends on 6.1–6.5, not 6.6. This allows 7.4 to proceed before the shared posting foundation and risks duplicate or divergent membership/posting implementations.
   - Recommendation: add Story 6.6 as a prior dependency of Story 7.4 and state that 7.4 reuses its membership/posting service, deterministic identity, recovery, and negative-isolation evidence.

2. **Story 8.1 underdeclares the terminal-state producers governed by retention.** Its acceptance criteria apply the 365-day policy to every terminal interaction and protected prompt/context/generated/proposal/audit payload, but its prior-story list names only Story 5.2 and Story 7.6. Automatic posts/failures and proposal approval/rejection/abandonment terminals are produced across Epic 6 and Stories 7.4–7.6.
   - Recommendation: declare the exact terminal-event/status contracts consumed from Epic 6 and Stories 7.4–7.6, or narrow Story 8.1's demonstrable scope and add later retention-enrollment stories for missing terminal families.

3. **Contradictory historical acceptance criteria remain co-located with the active backlog.** The replacement-authority preamble is clear, but historical Story 2.3 still permits an “approved bounded-context behavior,” Story 2.6 permits mentions/commands/alternate invocation affordances, Story 4.4 permits reporting-only cost posture or accepted risk, and Story 4.5 references MCP/A2A/tool-schema behavior. All conflict with current V1 authority and rely on readers honoring the global `mustNotImplement` instruction.
   - Recommendation: move historical Epics 1–4 to a clearly archived evidence document, or attach explicit per-story `historical: true` and localized `mustNotImplement` markers beside conflicting criteria so machine consumers cannot mistake them for active requirements.

4. **Several active stories combine multiple independently verifiable implementation slices and production-like evidence gates.** Highest-risk examples are:
   - Story 5.4: Tenants projection correctness, Party identity readiness, Approver Policy resolution, and cross-tenant disclosure testing.
   - Story 5.6: platform topology, secret resolution/rotation, access-control routes, evidence ingress, and repository/package ownership proof.
   - Story 6.1: durable workflow foundation plus checkpoint-wide recovery and NFR-11 production-like qualification.
   - Story 6.4: prepared-attempt fingerprinting, budget-ledger concurrency, Provider invocation/idempotency, outcome recovery, secret safety, and tenant authorization.
   - Story 6.5: capacity-profile contracts, shared allocator, durable queues, fencing, weighted fairness, replica/crash recovery, and Level 5 saturation evidence.
   - Story 8.4: Content Safety publication, tenant budget publication, generic high-impact command semantics, and governance parity.
   - Story 8.5: four NFR-9 latency families plus SM-1 through SM-6 calculations, windows, cohorts, late data, and live-evidence classification.
   - Story 8.6: every V1 route/state for WCAG, localization, responsive safety, three browser timing kinds, evidence ingress, and cross-tenant denial.
   - Recommendation: split these along durable capability versus qualification-evidence boundaries, or provide explicit team-sized estimates and prove each remains finishable within one implementation iteration. Preserve the current primary demonstrable outcome and evidence manifest in each resulting story.

### Acceptance Criteria Quality

- Active stories consistently use Given/When/Then structure.
- Happy paths, typed failures, idempotency/replay, stale/concurrent state, tenant isolation, secret/content leakage, UI truth states, and evidence obligations are unusually explicit.
- Every active story contains a Primary Demonstrable Outcome, dependency list, evidence level, named artifacts, executable verification command, negative evidence, and current result.
- Acceptance criteria are measurable and generally avoid vague “works” or “is supported” language.
- External-dependency statuses are fail-closed rather than silently assumed.

### Database/Entity Timing, Starter, And Project Context

- The architecture specifies no external starter template. Story 5.1 correctly establishes the corrected structural seed and early source/package/boundary/CI gates.
- EventStore aggregates and projections are introduced with the stories that need them; Story 5.1 explicitly forbids pre-creating all future entities.
- The backlog correctly treats this as a brownfield ecosystem integration/correction effort: package compatibility, platform-host migration, external seam commitments, and live cross-system evidence are explicit.

### Minor Concerns

- Literal standalone `+` lines appear before the Epic 6 and Epic 7 headings and should be removed.
- The requirements inventory heading uses `NonFunctional Requirements`, while the PRD uses “Non-Functional Requirements.”
- PRD identifiers use `FR-1`/`NFR-1`, while the epic inventory and coverage map often use `FR1`/`NFR1`. The mapping is unambiguous but normalization would improve machine traceability.

### Overall Epic Quality Assessment

The active epic structure is user-value oriented, sequential without forward references, extensively traceable, and substantially stronger than a typical implementation backlog. It is not yet cleanly implementation-ready because the Story 7.4 and 8.1 dependency declarations need correction, the historical conflicting criteria remain in the executable document, and several high-risk stories require decomposition or explicit proof of iteration-sized feasibility.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The planning set has complete FR traceability and strong PRD↔UX↔architecture alignment, but it does not satisfy its own entry conditions for Phase 4 implementation. The decisive blocker is PRD FR-21/§8: a critical dependency with `Uncommitted` status, no immutable target, or no executable compatibility command blocks every consuming story from `ready-for-dev`. Architecture and the active story manifests identify all seven critical external dependencies as currently `Uncommitted`. In addition, two active story dependency declarations are incomplete and the current epic document co-locates contradictory historical criteria with executable work.

### Critical Issues Requiring Immediate Action

1. **Resolve all seven external dependency commitments:** `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`. Each consuming story must remain backlog—not `ready-for-dev`—until its required record contains the full PRD §8 commitment and the status required by the work (`Committed` for contract/development work; `Available` plus a passing exact command for live execution).
2. **Correct Story 7.4 dependencies** by adding Story 6.6 as the shared Conversations membership/posting foundation.
3. **Correct Story 8.1 dependencies or scope** so retention enrollment explicitly covers every applicable automatic, failure, proposal, and posting terminal-state producer.
4. **Prevent historical criteria from being executed.** Archive Epics 1–4 separately or add local machine-visible historical/`mustNotImplement` markers to their conflicting criteria.

### Recommended Next Steps

1. Update the external dependency register with a named owner, owning repository, required artifact, immutable target version/commit, target integration date, executable compatibility contract/command, required Evidence Level, accepted status, and consuming stories for each of the seven records.
2. Amend the Story 7.4 and 8.1 dependency manifests and rerun dependency-topology validation.
3. Separate or locally mark the historical Epic 1–4 criteria that conflict with current authority: bounded context, alternate invocation entries, reporting-only cost controls/accepted risk, and MCP/A2A/tool behavior.
4. Resolve and document the “Conversation owner” versus `ParticipantRole.Facilitator` mapping across PRD terminology, UX copy, public contracts, disclosure category, and Story 5.4 evidence.
5. Decompose or explicitly estimate the eight highest-risk stories—5.4, 5.6, 6.1, 6.4, 6.5, 8.4, 8.5, and 8.6—preferably splitting durable capability delivery from production-like qualification evidence.
6. Remove the stray `+` separators and normalize `FR-#`/`NFR-#` identifiers and the “Non-Functional Requirements” heading for machine traceability.
7. Rerun implementation readiness after the register and epic corrections. Do not treat deterministic fixtures, historical completion, lower evidence, skips, or conditional results as substitutes for the live Levels 4–5 evidence required by `RQ-1`.

### Positive Findings To Preserve

- 28 of 28 PRD Functional Requirements have active epic coverage: 100%.
- The PRD contains 14 explicit, quantified NFRs and a binding decision register with no unresolved product/governance question.
- Active Epics 5–8 are user-value oriented and contain no direct forward or circular dependencies.
- Active stories have detailed BDD acceptance criteria, evidence manifests, named negative evidence, executable verification commands, and fail-closed dependency states.
- UX and architecture consistently preserve FrontComposer/Fluent V5, WCAG 2.2 AA, English/French parity, restrictive responsive behavior, exact browser timing seams, proposal/message truth boundaries, tenant isolation, and secret/content safety.

### Final Note

This assessment identified **7 issue groups across 5 categories**: external dependency readiness, active-story dependency accuracy, historical-authority hygiene, story sizing, and terminology/format consistency. The seven external records and two story dependency corrections are release-planning blockers, not optional polish. Address them before moving consuming stories to `ready-for-dev`; then rerun this assessment.

**Assessment date:** 2026-08-03  
**Assessor:** Codex, acting as implementation-readiness Product Manager
