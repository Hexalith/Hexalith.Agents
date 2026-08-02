---
title: Sprint Change Proposal - Implementation Readiness Remediation
status: approved
created: 2026-08-01
updated: 2026-08-01
mode: Incremental
change_scope: major
recommended_path: hybrid-direct-adjustment-with-targeted-retirement
project: agents
owner: Administrator
approval_required: false
approved_by: Administrator
approved_on: 2026-08-01
implementation_status: epic-5-backlog
trigger_report: implementation-readiness-report-2026-08-01.md
---

# Sprint Change Proposal: Implementation Readiness Remediation

## 1. Issue Summary

The 2026-08-01 Implementation Readiness Assessment found the Hexalith Agents
planning package **NOT READY** for continued feature implementation or
production-like enablement. Functional traceability is complete—28 of 28 PRD
Functional Requirements have epic/story coverage—but the plan contains seven
critical structural and dependency violations, unresolved product/governance
decisions, stale story acceptance criteria, and several oversized stories.

This is not a missing-feature discovery. It is a failed planning and architecture
reconciliation discovered after Epics 1–4 were completed as a fail-closed
foundation. Story 4.5 and its governance conformance report explicitly record
that the live durable runtime owner, live read models, provider adapter, content
safety engine, Conversations membership seam, and cross-system evidence remain
deferred. The formal readiness assessment then showed that the canonical planning
artifacts still describe those outcomes as though the completed stories form an
implementable production plan.

### Trigger

- Formal trigger: `_bmad-output/planning-artifacts/implementation-readiness-report-2026-08-01.md`.
- Triggering implementation context: Story 4.5, which completed conformance at
  fail-closed seams while deferring live bindings.
- Issue category: failed approach requiring a different solution, combined with
  architecture/requirements misunderstanding and unresolved stakeholder decisions.

### Evidence

1. Architecture AD-16, the Structural Seed, Story 1.1, the solution, and structural
   tests require module-owned `AppHost`, `Aspire`, and `ServiceDefaults` projects,
   while foundational Hexalith/EventStore policy forbids those projects in a
   domain module.
2. Stories 1.8, 2.1, 2.6, and Epic 3 consumers depend on stable public contracts
   that Story 4.1 publishes later.
3. Story 1.7 and Epic 2 claim callability/generation before Story 4.4 supplies the
   complete production-like launch gate.
4. Conversations contains `AddParticipantCommand` and a server handler, but the
   supported `IConversationClient` exposes no `AddParticipantAsync` method.
5. Story 4.5 proves the absence of a live durable owner and treats Dapr Workflow,
   Microsoft Agent Framework workflow/session restore, provider binding, and live
   projections as deferred seams.
6. Final AD-10 requires a durable capability high-water mark, current readiness
   revalidation, effective capability-version evidence, and prepared-attempt
   fingerprint binding; current context, generation, and regeneration paths do
   not implement those rules.
7. PRD OQ-1 through OQ-11 contain decisions that gate UX, proposal expiry,
   performance, cost, audit, safety, context, and launch readiness.
8. `sprint-status.yaml` marks Epics 1–4 and all 26 stories done, so retroactively
   rewriting those story acceptance criteria would falsify historical completion
   evidence.

## 2. Impact Analysis

### Epic Impact

Epics 1–4 remain closed historical records. They delivered reusable domain,
contract, UI, policy, and fail-closed seam foundations under their original
acceptance criteria. Their status does not establish production readiness and
will not be rewritten.

A new **Epic 5: Production-Safe Runtime And Platform Integration** is required.
It owns architecture reconciliation, launch gates, live dependency bindings,
durable orchestration, Provider and safety execution, Conversations integration,
governed audit behavior, live UI bindings, and final readiness evidence.

### Story Impact

- Completed Stories 1.1–4.5 remain unchanged as historical implementation records.
- New remediation is expressed through 18 sequenced Epic 5 stories and one
  external Conversations prerequisite.
- Every new story declares FR/NFR/UX-DR/AD ownership and focused negative evidence.
- Public contract additions land in the first consuming story, removing the late
  contract-publication dependency.
- The complete launch-readiness record precedes callability and all live Provider
  invocation.

### PRD Impact

The V1 product goal and MVP scope remain achievable; no feature-scope reduction is
recommended. The PRD changes from conditional open questions to explicit V1
decisions for:

- Conversation invocation and notification posture.
- Proposal ownership and expiry.
- Provider metadata and full-context behavior.
- Latency targets and cost controls.
- Audit retention, legal hold, export, and deletion.
- Content safety categories, prompt constraints, and override rules.
- SM-2 and SM-3 launch thresholds.

### Architecture Impact

- Replace module-owned hosting with platform-owned composition.
- Select Dapr Workflow as the sole V1 durable owner.
- Retain Microsoft Agent Framework only inside generation activities/adapters.
- Keep EventStore commands/events/projections as business truth.
- Implement AD-10/AD-11/AD-13 runtime reconciliation before live Provider use.
- Add explicit ownership for EventStore dispatch, Tenants access, Parties identity,
  Approver resolution, read models, and Conversations membership/posting.
- Update architecture binding metadata from FR-1..FR-25 to FR-1..FR-28.

### UX Impact

The June UX package is reconciled and ratified against the August architecture.
It selects one Conversation-owned `Call hexa` action, in-product proposal
notification only, deterministic expiry, and new policy/readiness authoring
surfaces. The revised package becomes `final` after the approved decisions are
applied.

### Technical, Infrastructure, And Deployment Impact

- Retire or re-home the minimal Agents `AppHost`, `Aspire`, and `ServiceDefaults`
  projects after the replacement platform topology is proven.
- Add early CI, package-only consumer, topology, and integration gates.
- Publish a Conversations `AddParticipantAsync` public seam in the owning module.
- Bind EventStore command/query operations and durable read models.
- Add Dapr Workflow execution, durable timers, replay/restart handling, and retry
  evidence.
- Add Provider cost reservation/reconciliation and a live content safety engine.
- Add retention/legal-hold/export/cryptographic-erasure behavior.
- Replace deferred-seam-only conformance with live component and cross-system
  evidence.

## 3. Recommended Approach

### Selected Path

**Hybrid direct adjustment with targeted retirement.**

The project should preserve reusable completed work, correct the authoritative
planning artifacts, add Epic 5, and implement the missing production bindings in
dependency order. The empty/minimal module-owned hosting projects are retired or
re-homed as a targeted correction after replacement topology evidence exists.

### Alternatives

- **Full rollback — not viable.** The aggregate, contract, policy, UI, and
  fail-closed seam work remains useful. Removing it would not resolve the
  decisions, external prerequisite, or runtime ownership gap.
- **PRD/MVP reduction — not recommended.** The V1 goal remains achievable after
  governance and architecture closure. Scope reduction would not repair hosting,
  ordering, or evidence defects.
- **Edit completed stories in place — rejected.** This would change historical
  acceptance after completion and obscure what was actually built and tested.
- **Continue from open sprint actions only — rejected.** The actions identify
  much of the work but do not provide canonical story ownership, sequencing,
  acceptance criteria, or evidence levels.

### Effort, Risk, And Timeline

- Effort: **High**—18 Agents stories plus one cross-repository Conversations
  prerequisite.
- Risk: **High** until topology authority, governance decisions, and the public
  Conversations seam are closed; Medium after Stories 5.1–5.7 establish the
  corrected foundation.
- Planning impact: approximately **three to five coordinated sprints** after
  prerequisite owners are available, subject to team capacity and Provider/safety
  integration complexity. This is a planning estimate, not a delivery commitment.
- Implementation posture: feature implementation remains limited to approved
  remediation and evidence gathering until the corrected architecture, PRD, UX,
  epics, and sprint plan are accepted.

## 4. Detailed Change Proposals

### 4.1 Architecture: Platform-Owned Hosting

Artifact: `ARCHITECTURE-SPINE.md` — Design Paradigm, AD-16, topology diagrams,
Structural Seed, and capability map.

OLD:

```text
Hexalith.Agents owns its own AppHost/local orchestration and deployable
workloads. The Structural Seed includes Hexalith.Agents.AppHost,
Hexalith.Agents.Aspire, and Hexalith.Agents.ServiceDefaults.
```

NEW:

```text
Hexalith Agents is a domain-centric EventStore module. It owns domain
contracts, aggregates, policies, query/projection handlers, public client
surface, and domain-specific UI assets. It does not ship module-owned
AppHost, Aspire, or ServiceDefaults projects or duplicate platform hosting,
DAPR, telemetry, health, or EventStore plumbing.

A platform-owned host composes the Agents domain service and UI with
EventStore, Conversations, Parties, Tenants, Provider adapters, and the
selected durable runtime. The Agents service continues to use the shared
EventStore DomainService SDK host.
```

Justification: resolves the highest-level authority conflict in favor of the
foundational Hexalith domain-module policy while preserving the reusable domain
service.

### 4.2 Architecture: Dapr Workflow Owns V1

Artifact: `ARCHITECTURE-SPINE.md` — AD-18, runtime sequence, conventions, stack,
and deferred decisions.

OLD:

```text
Each task selects one durable owner from Microsoft Agent Framework workflow,
Dapr Workflow, or Python Dapr Agents DurableAgent.
```

NEW:

```text
Dapr Workflow is the single durable owner for every V1 AgentInteraction
lifecycle, including context preparation, generation, confirmation waits,
proposal expiry, posting, retries, and recovery.

Microsoft Agent Framework may be used inside generation activities for typed
agent/session and Provider integration, but it does not own orchestration or
domain state. Python DurableAgent, MCP tools, A2A agents, and alternative
workflow owners remain unbound and out of V1.
```

Each workflow step uses replay-safe activities, enforces AD-10/AD-11/AD-13,
dispatches at most one deterministic trusted command, and keeps EventStore as
business truth.

Justification: prevents double orchestration and selects the runtime that directly
supports V1 waits, timers, retries, and restart recovery.

### 4.3 Epics: Preserve History And Add Epic 5

Artifacts: `epics.md` and `sprint-status.yaml`.

OLD:

```text
Epics 1–4 and all 26 stories are done, while the epic document remains the
only complete V1 implementation plan.
```

NEW:

```text
Epics 1–4 remain closed historical records of the fail-closed foundation.
Their completion does not establish production readiness.

Epic 5: Production-Safe Runtime And Platform Integration

Release Operators can enable `hexa` through the approved platform topology
only after durable orchestration, live integrations, governance policy,
tenant-safe evidence, and launch gates are proven end to end.
```

Justification: adds honest ownership without altering completed-story evidence.

### 4.4 PRD/UX: Invocation And Notification

OLD:

```text
Invocation may be a mention, command, action, participant affordance, or a
combination. Active Approver notification remains unresolved.
```

NEW:

```text
The only V1 entry is a Conversation-owned “Call hexa” action that opens a
Fluent/FrontComposer prompt panel. V1 does not support mention parsing,
slash-command invocation, ambient activation, or competing entry patterns.

V1 notification is in-product only: a policy-gated pending count, the “Needs
my action” queue, and status reachable from the originating Conversation.
External notifications remain out of scope.
```

Membership is established or verified through the official Conversations seam
immediately before posting, not inferred from the invocation control.

Justification: selects one accessible, testable entry pattern and avoids new
mention-resolution and notification infrastructure.

### 4.5 PRD/UX: Proposal Expiry

OLD:

```text
Proposal expiry default and configurability are unresolved; Story 3.6 is
conditional on an expiry policy existing.
```

NEW:

```text
Default expiry is 24 hours. Agent Administrators may configure 1 hour through
30 days per Agent. Changes affect future proposals only. Each proposal
snapshots ExpiryPolicyVersion and ExpiresAt. At ExpiresAt or later, expiry
wins and edit, regeneration, approval, or posting fails closed. Dapr Workflow
owns the durable timer and deterministic expiry command.
```

Justification: creates measurable terminal behavior and restart-safe expiry.

### 4.6 PRD: Latency Targets

OLD:

```text
Latency targets are required but undefined.
```

NEW:

| Path | p95 | p99 |
| --- | ---: | ---: |
| Automatic: call accepted to Message posted | 60s | 120s |
| Confirmation: call accepted to proposal available | 60s | 120s |
| Approval accepted to Message posted | 10s | 30s |
| Known pre-Provider rejection | 2s | — |

Each supported mode requires at least 30 production-like executions. Missing
timestamps or insufficient samples yield `insufficient evidence`, not success.

Justification: supplies a practical, auditable V1 operating envelope.

### 4.7 PRD/Architecture: Cost Controls

OLD:

```text
Launch may use quotas, budgets, Provider/model limits, reporting-only
monitoring, or accepted risk; no posture is selected.
```

NEW:

```text
Production-like enablement requires Provider/model input/output/timeout
limits, a numeric monthly tenant budget, numeric per-call limits, an 80%
warning, a 100% fail-closed block, and auditable usage reconciliation.

The system reserves maximum estimated attempt cost before Provider invocation,
reconciles actual usage, releases unused reservation after no-usage failures,
and reuses the same reservation for eligible retries. Missing pricing or
budget state blocks invocation. Reporting-only monitoring is insufficient.
```

Justification: prevents concurrent overspend without embedding one universal
currency amount in the PRD.

### 4.8 PRD/Architecture: Audit Governance

OLD:

```text
Retention, legal hold, export, and deletion are unresolved. Content-bearing
audit remains metadata-only and blocked.
```

NEW:

```text
Sensitive Agent content is retained for 365 days after terminal interaction.
Legal hold suspends expiry. Authorized exports are tenant-scoped, encrypted,
time-limited, manifested, and audited. EventStore history is never rewritten;
retention expiry or approved deletion cryptographically erases/redacts
sensitive payloads and purges projections while retaining only a support-safe
non-content tombstone.
```

Deletion is not complete until payload protection and affected projections
confirm restrictive state. Posted Conversation Messages remain under
Conversations retention.

Justification: provides finite sensitive-content retention while respecting
immutable event history.

### 4.9 PRD/Architecture: Content Safety

OLD:

```text
Safety categories, prompt constraints, failure handling, and override rules
are unresolved.
```

NEW:

V1 applies prompt/context validation before Provider invocation and output
validation before proposals or Conversation side effects. Missing, stale,
unversioned, or indeterminate safety state fails closed.

Always blocked with no user/Approver override:

- Child sexual abuse or exploitation.
- Credible threats or instructions for imminent serious harm.
- Encouragement or instruction for suicide/self-harm.
- Credential theft, malware deployment, or unauthorized compromise.
- Secrets, tokens, or private credentials.
- Cross-tenant or unauthorized personal/conversation data.
- Attempts to bypass tenant, authorization, audit, retention, or safety controls.

Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive
personal content requires an explicitly permitted use case and Confirmation
Response Mode. Security Operators may publish a future policy version; failed
attempts cannot be resurrected or retried through a weaker policy.

Justification: defines a Provider-neutral pre-side-effect safety contract.

### 4.10 PRD: SM-2 And SM-3 Thresholds

OLD:

```text
SM-2 and SM-3 have no formula, threshold, window, or cohort.
```

NEW:

**SM-2 Conversation Adoption**

- Cohort: pilot tenants where `hexa` was callable for the complete window.
- Denominator: Conversations with at least two human messages and one authorized
  potential caller.
- Numerator: eligible Conversations with at least one accepted Agent Call.
- Target: at least 20% over a rolling 30 days with at least 50 eligible
  Conversations.

**SM-3 Approval Workflow Completion**

- Denominator: confirmation proposals old enough for a 26-hour observation or
  terminal earlier.
- Numerator: posted after approval, rejected, abandoned, or expired within 26 hours.
- Target: at least 95%.
- Guardrails: expiry at most 20%, posting failure at most 2%, human resolution at
  least 70%, and complete required audit evidence.

Justification: defines measurable adoption and completion without allowing expiry
alone to manufacture success.

### 4.11 PRD/Architecture: Close OQ-2, OQ-7, And OQ-10

OLD:

```text
Proposal ownership, required Provider metadata, and oversized-context behavior
remain open or conditional.
```

NEW:

```text
OQ-2: AgentInteraction owns durable proposal business state and immutable
versions in EventStore. Dapr Workflow owns execution state only.

OQ-7: Provider/model metadata includes identity, display label, enabled and
configured state, text generation, context/output limits, timeout, safe flags,
versioned pricing, and monotonic CapabilityVersion.

OQ-10: V1 is full-conversation-or-fail-closed. It does not summarize,
truncate, window, sample, or retrieve substitute context.
```

Justification: removes three incompatible implementation branches from V1.

### 4.12 Conversations: Publish The Membership Seam

External prerequisite: `CONV-AI-1`, owned by Hexalith.Conversations.

OLD:

```text
AddParticipantCommand and its handler exist, but the supported public client
does not expose membership mutation.
```

NEW public contract:

```csharp
Task<ConversationClientResult<ConversationCommandAcceptedResult>>
    AddParticipantAsync(
        AddParticipantCommand command,
        CancellationToken cancellationToken = default);
```

Route: `POST /api/v1/conversations/{conversationId}/participants`.

Agents receives a least-privilege delegated capability for
`ParticipantType.AiAgent` (`AIAgent`) and `ParticipantRole.Member`, with stable
Party identity and deterministic idempotency. Exact retries are no-ops;
conflicting type/role returns a typed conflict. Cross-tenant or general participant
management remains forbidden.

Justification: makes the existing prerequisite public, owned, testable, and
least-privilege.

### 4.13 Epics: Decompose Epic 5

The final sequence is:

| Story | Outcome | Primary dependencies |
| --- | --- | --- |
| 5.1 | Correct module boundary; establish CI, package-consumer, and platform-topology gates | Approved AD-16 correction |
| 5.2 | Enforce complete launch-readiness record before callability | Approved decisions |
| 5.3 | Bind EventStore command/query operations and persist Agent/Provider readiness read models | 5.1 |
| 5.4 | Bind tenant access, Party identity, and Approver resolution | 5.1, 5.3 |
| 5.5 | Persist AgentInteraction, proposal, status, and audit read models | 5.1, 5.3 |
| 5.6 | Implement AD-10/AD-11/AD-13 high-water, effective-version, revalidation, and prepared-attempt contracts | 5.3–5.5 |
| 5.7 | Bind Dapr Workflow as sole durable owner, including expiry and restart recovery | 5.2, 5.6 |
| 5.8 | Bind authorized full-Conversation context with full-or-blocked behavior | 5.4, 5.6–5.7 |
| 5.9 | Bind live Content Safety engine and versioned policy | 5.2, 5.7–5.8 |
| 5.10 | Bind live Provider adapter and atomic tenant cost reservations | 5.2, 5.6–5.9 |
| 5.11 | Bind AI membership and idempotent automatic/approved posting | CONV-AI-1, 5.4–5.10 |
| 5.12 | Bind Conversation-owned `Call hexa` action and live status | 5.3–5.11 |
| 5.13 | Bind pending-proposal counts and `Needs my action` queue | 5.5, 5.7 |
| 5.14 | Bind proposal detail, actions, expiry presentation, and accessibility | 5.5, 5.7, 5.11 |
| 5.15 | Implement retention and legal-hold enforcement | 5.5, 5.7 |
| 5.16 | Implement authorized audit export and cryptographic deletion | 5.15 |
| 5.17 | Bind operator policy authoring, budgets, latency, metrics, and launch status | 5.2–5.16 |
| 5.18 | Run live cross-system conformance and repeat implementation readiness | All prior stories |

Every story uses measurable Given/When/Then acceptance criteria, explicit
FR/NFR/UX-DR/AD tags, focused tenant-denial evidence, and slice-first contracts.

Justification: removes reverse dependencies and separates architecture, runtime,
UI, governance, and evidence into testable slices.

### 4.14 UX: Ratify Policy And Readiness Surfaces

OLD:

```text
DESIGN.md and EXPERIENCE.md are draft and omit complete authoring flows for
context, safety, cost, audit governance, and launch readiness.
```

NEW:

- Conversation policy: read-only full-or-blocked behavior and effective budget.
- Content safety: fixed blocked categories, restricted handling, policy version,
  validation, and publish.
- Cost controls: monthly budget, per-call caps, usage/reservations, warning/block.
- Launch readiness: latency, SM-2/SM-3, cohort/window, sufficiency, blockers.
- Audit governance: retention, legal hold, export, deletion, and restrictive
  partial-failure states.
- Approved `Call hexa` and in-product proposal notification experiences.

High-impact actions require confirmation, authorization, future-only policy copy,
projection/evidence-confirmed success, keyboard/focus/live-region behavior, and
FrontComposer/Fluent V5 conformance.

The reconciled UX documents become `status: final`, `updated: 2026-08-01`.

Justification: turns launch blockers into operable and testable workflows.

### 4.15 Epic 5: Identity And Access Binding

Story 5.3 explicitly binds EventStore command/query operations. Story 5.4 owns:

- Durable local Tenants access projection with duplicate, out-of-order, gap,
  revocation, and freshness handling.
- Parties-backed Agent/caller identity validation using stable Party references.
- Approver resolution across snapshotted policy, tenant roles, predefined Parties,
  caller identity, and Conversation Facilitator authority.
- Denial before Provider, membership, posting, export, or deletion side effects.
- Focused cross-tenant and event-driven revocation evidence.

Justification: prevents critical identity/authorization seams from being hidden in
later runtime stories.

### 4.16 Authority, Traceability, And Evidence Levels

Architecture binds update to FR-1..FR-28. The PRD open-question table becomes a
dated decision register. Every Epic 5 story declares FR/NFR/UX-DR/AD ownership and
cites the command-step convention where relevant. The 2026-08-01 readiness report
remains unchanged; Story 5.18 creates a new assessment.

Conformance evidence levels:

| Level | Evidence |
| --- | --- |
| 1 | Contract/structure |
| 2 | Pure domain/unit behavior |
| 3 | Fail-closed deferred seam |
| 4 | Live component integration |
| 5 | Cross-system production-like evidence |

Production-like readiness requires Levels 4–5 for runtime, authorization, tenant
isolation, Provider, safety, Conversations, audit, and topology. Lower levels,
skips, placeholders, and “where applicable” results remain visible blockers.

Justification: prevents coverage or deferred-seam evidence from being mistaken for
live readiness.

### 4.17 Provider-Binding Order

OLD:

```text
Live Provider binding preceded full-context and safety bindings.
```

NEW:

```text
5.7 proves Dapr Workflow with deterministic fake activities.
5.8 binds authorized full context.
5.9 binds safety.
5.10 activates the live Provider adapter and cost reservation only after those
gates exist.
```

Justification: prevents live Provider side effects from depending on future context
or safety stories.

## 5. Implementation Handoff

### Scope Classification

**Major** — fundamental replan with cross-module architecture, Product, UX,
Governance, Security, Developer, Release, and Test Architecture coordination.

### Route And Responsibilities

**Product Manager**

- Ratify the PRD decision register and unchanged V1 scope.
- Own launch cohort and policy-value acceptance.
- Ensure no resolved decision returns to conditional story language.

**Solution Architect**

- Apply AD-16 and AD-18 corrections and update all dependent diagrams, seed,
  capability maps, and conventions.
- Produce implementation-grade Epic 5 story context in dependency order.
- Coordinate the platform host owner and Conversations prerequisite.

**Hexalith.Conversations Owner**

- Deliver and publish `CONV-AI-1` through a separate submodule change.
- Attach authorization, idempotency, package, and integration evidence.

**Developer**

- Implement Epic 5 only after its prerequisite decisions and story context are
  ready.
- Preserve completed Epics 1–4 and unrelated worktree/submodule changes.
- Retire/re-home module hosting shells only after replacement topology evidence.
- Attach exact focused test commands/results to every tenant-sensitive story.

**UX Designer**

- Apply the approved UX decisions and ratify both UX spines as final.
- Ensure policy authoring and restrictive states remain FrontComposer/Fluent V5
  conformant and accessible.

**Security And Governance**

- Validate the safety category contract and audit governance decision.
- Approve the live safety engine and payload-protection/erasure evidence.

**Test Architect**

- Implement evidence-level enforcement and live integration lanes.
- Reject Level 3 seam evidence where Levels 4–5 are required.
- Produce the final cross-system traceability and readiness evidence.

**Release Operator**

- Configure numeric budgets, cohort, and policy values.
- Enforce latency, SM-2/SM-3, audit, safety, and cost gates before pilot expansion.

### Success Criteria

1. Canonical architecture and repository policy agree on the hosting boundary.
2. Dapr Workflow is the only live V1 durable owner.
3. All OQ-1..OQ-11 decisions are recorded with no conditional implementation ACs.
4. `CONV-AI-1` is published and proven before posting binding.
5. AD-10/AD-11/AD-13 runtime reconciliation passes focused tests before Provider
   activation.
6. Agents command/query, Tenants, Parties, Approver, and read-model seams are live
   and fail closed on uncertainty.
7. Provider, safety, context, posting, audit, and UI paths have Level 4 evidence.
8. Production-like end-to-end behavior has Level 5 tenant-denial, retry, restart,
   and state-store evidence.
9. The final implementation-readiness assessment reports zero critical violations.
10. Production-like enablement remains blocked until every required gate passes.

## 6. Checklist Summary

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 | Done | Formal trigger is the 2026-08-01 readiness assessment; Story 4.5 is the triggering implementation context. |
| 1.2 | Done | Failed planning/architecture reconciliation and unresolved-decision approach. |
| 1.3 | Done | Readiness report, planning artifacts, sprint status, conformance report, source seams, and Conversations client provide concrete evidence. |
| 2.1 | Done | Completed Epic 4 cannot establish production readiness as originally implied; preserve it historically. |
| 2.2 | Done | Add Epic 5 and external prerequisite CONV-AI-1. |
| 2.3 | Done | No remaining planned epics existed; all future work moves to the corrected sequence. |
| 2.4 | Done | No completed epic becomes obsolete; one new production-binding epic is necessary. |
| 2.5 | Done | Architecture/gates/contracts precede runtime; context/safety precede Provider; final conformance is last. |
| 3.1 | Done | PRD Decision Register and measurable launch values applied on 2026-08-01. |
| 3.2 | Done | Corrected hosting/runtime authority, diagrams, structural seed, metadata, governance, and evidence contracts applied on 2026-08-01. |
| 3.3 | Done | UX invocation, policy authoring, expiry, notification, readiness, and audit-governance surfaces reconciled and ratified final on 2026-08-01. |
| 3.4 | Action-needed | Conversations client, topology, CI, read models, runtime, Provider, safety, audit, metrics, and evidence lanes require implementation. |
| 4.1 | Viable | Direct adjustment is viable with High effort and High initial risk. |
| 4.2 | Limited viable | Targeted retirement/re-home of hosting shells is justified; full rollback is not. |
| 4.3 | Not viable | MVP reduction is unnecessary and would not resolve structural defects. |
| 4.4 | Done | Hybrid direct adjustment with targeted retirement selected. |
| 5.1 | Done | Issue summary and evidence recorded. |
| 5.2 | Done | Epic, story, artifact, UX, and technical impacts recorded. |
| 5.3 | Done | Recommendation, rationale, trade-offs, risk, effort, and timeline recorded. |
| 5.4 | Done | MVP unchanged; 18-story sequenced action plan defined. |
| 5.5 | Done | Major-scope PM/Architect-led handoff with cross-functional responsibilities defined. |
| 6.1 | Done | Applicable checklist sections are addressed; implementation actions remain explicit. |
| 6.2 | Done | Proposal reconciled against the approved incremental edits and source evidence. |
| 6.3 | Done | Administrator explicitly approved the complete proposal on 2026-08-01. |
| 6.4 | Done | Epic 5 and all 18 stories were added to sprint status as backlog without changing Epics 1–4. |
| 6.5 | Done | Major change routed to PM/Architect leadership with Product, UX, Governance, Security, Development, Release, and Test Architecture responsibilities. |

## 7. Approval Record

Incremental mode was used. Administrator approved all 17 detailed edit proposals
on 2026-08-01:

1. Platform-owned hosting authority.
2. Dapr Workflow as V1 durable owner.
3. Preserve Epics 1–4 and add Epic 5.
4. Explicit `Call hexa` and in-product notification.
5. Proposal expiry decision.
6. Latency targets.
7. Cost controls.
8. Audit governance.
9. Content Safety Policy.
10. SM-2/SM-3 thresholds.
11. Proposal ownership, Provider metadata, and full-context behavior.
12. Conversations membership prerequisite.
13. Epic 5 decomposition.
14. UX policy/readiness surfaces and ratification.
15. EventStore/Tenants/Parties/Approver binding lane.
16. Authority, traceability, and evidence levels.
17. Context/safety-before-Provider sequencing correction.

Complete Sprint Change Proposal approval: **approved by Administrator on 2026-08-01**.

Implementation is routed as a **Major** PM/Architect-led Epic 5 backlog. Planning
approval does not make the product implementation-ready: the current state remains
NOT READY until Story 5.18 produces a new evidence-backed assessment.

## 8. Workflow Execution Log

- 2026-08-01 — Correct Course workflow activated in Incremental mode.
- 2026-08-01 — Repository instructions, resolved skill customization, persistent
  project facts, BMM configuration, and complete checklist loaded.
- 2026-08-01 — Accepted PRD, epics, architecture, implementation conventions, UX,
  readiness report, sprint status, source seams, and Story 4.5 conformance evidence
  assessed.
- 2026-08-01 — Checklist Sections 1–4 completed; Major-scope hybrid direct
  adjustment selected.
- 2026-08-01 — Seventeen detailed artifact edits reviewed and approved
  incrementally by Administrator.
- 2026-08-01 — Administrator explicitly approved the complete Sprint Change
  Proposal.
- 2026-08-01 — PRD, Architecture Spine, UX Design/Experience, epic backlog, and
  sprint status reconciled; Epics 1–4 preserved as historical done evidence.
- 2026-08-01 — Epic 5 and Stories 5.1–5.18 entered as backlog; external
  prerequisite CONV-AI-1 retained before Story 5.11.
- 2026-08-01 — Major implementation handoff routed to PM/Architect leadership.
