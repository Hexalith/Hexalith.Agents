# Architecture Reviewer Gate — Rubric Walker

- **Reviewed:** `ARCHITECTURE-SPINE.md`
- **Date:** 2026-08-02
- **Lens:** BMad good-spine checklist, approved Sprint Change Proposal, current PRD, external-dependency register, launch-readiness register, brownfield workspace, and initiative-altitude architecture dimensions
- **Gate verdict:** **Conditional pass.** The spine lands every approved architecture amendment and covers the initiative's owned dimensions, but one high-severity capacity-state convergence gap and three medium clarity/evidence-boundary issues should be resolved before final handoff.

## Deterministic Gate

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py \
  --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS** — zero placeholders, duplicate AD IDs, malformed ADs, or unpinned selected Stack rows.

## Tiered Findings

### High

#### RG-01 — The capacity gate has no bound shared-state and crash-recovery authority

- **Evidence:** AD-24 requires atomic tenant/system admission, bounded queues, deterministic attempt identity, and fairness, while AD-18 makes Dapr Workflow the durable owner of each interaction. The spine does not bind where the cross-interaction capacity counts, queue order, admissions, and leases are authoritatively stored, fenced, reclaimed, or reconciled across host replicas and process loss.
- **Why this is a divergence point:** one unit can implement an in-process semaphore/queue and another a distributed durable scheduler; both can claim literal compliance with "atomic capacity gate," but only one can preserve system-wide limits, retry identity, fairness, and NFR-11 recovery after failover.
- **Affected contracts:** AD-13, AD-18, AD-23, AD-24; NFR-11 and NFR-12; `LR-RECOVERY`; `LR-CAPACITY-FAIRNESS`.
- **Disposition:** **Discuss, then fix.** Bind technology-neutral invariants for one shared authoritative capacity-state owner, cross-replica atomicity, admission fencing, queued-work durability (or explicitly select reject-only until a durable queue exists), lease/crash reclamation, and workflow resume/reconciliation. The concrete storage implementation may remain downstream.

### Medium

#### RG-02 — Browser evidence qualification lacks an explicit trust/ingestion boundary

- **Evidence:** AD-26 precisely defines the monotonic timing seams and qualifying sample fields, and `EXT-TOPOLOGY-1` supplies a controlled browser profile. The diagrams nevertheless connect browser telemetry directly to `browser-ui-metrics`, without naming an authenticated ingestion/validation seam or stating that arbitrary production-browser submissions cannot qualify `LR-UI-PERFORMANCE`.
- **Why this matters:** release evidence can diverge between an attested controlled-run manifest and client-submitted metrics that can be duplicated, forged, reordered, or correlated to nonexistent projection versions.
- **Disposition:** **Autofix.** State that only the controlled `EXT-TOPOLOGY-1` runner/manifest can submit qualifying samples; the host validates clock-origin consistency, tick ordering, projection references, profile/version, and deterministic sample idempotency before evidence reaches `browser-ui-metrics`. Ordinary telemetry may remain operational evidence but cannot establish `Pass`.

#### RG-03 — The runtime sequence diagram visually weakens fail-closed and queued semantics

- **Evidence:** after readiness returns "callability or safe blockers," the diagram always creates `InteractionRequested` and starts the workflow. Its combined `queued or rejected` branch then flows to an unconditional "authoritative terminal" response, although queued work is non-terminal and should later resume admission or reach an explicit safe terminal result.
- **Why this matters:** the prose rules are stronger, but diagrams carry architecture shape; implementers can read the flow as accepting blocked calls or treating capacity queueing as terminal.
- **Disposition:** **Autofix.** Add an explicit blocked exit before interaction/workflow start, split queued from rejected, show durable queued wait/re-admission, and emit terminal browser evidence only after an authoritative terminal projection is received.

#### RG-04 — High-risk lock release terminology is ambiguous around “accepted”

- **Evidence:** AD-12 says the session lock clears from authoritative `accepted/rejected/terminal` status, while the architecture also defines an authoritative accepted *pending* acknowledgement. It is unclear whether durable acceptance ends the pending-command interval or whether the lock remains through the high-risk operation's terminal outcome.
- **Why this matters:** UI/BFF implementations can choose incompatible lock lifetimes and allow a second command for the same resource/family while the first operation is still authoritatively pending.
- **Disposition:** **Autofix.** Define the pending interval explicitly per family: identify the authoritative status that acquires the lock and the rejection/cancellation/terminal statuses that release it. If command acceptance is itself completion for a family, say so; an accepted-pending acknowledgement must not implicitly release the lock.

No critical findings were found.

## Good-Spine Checklist

| Checklist item | Judgment | Evidence and notes |
| --- | --- | --- |
| Fixes the real divergence points for the level below and misses none | **Conditional pass** | Aggregate ownership, EventStore mutation, orchestration, Provider readiness, external effects, authorization, safety, cost, readiness, recovery, UI evidence, topology, and dependency gates are fixed. RG-01 remains a material shared-state divergence; RG-02–RG-04 are narrower ambiguity/flow issues. |
| Every AD Rule is enforceable and prevents its stated divergence | **Conditional pass** | AD-1–AD-23 and AD-25–AD-26 carry observable boundaries and test/evidence consequences. AD-24's numeric/fairness contract is executable, but the missing shared-state/crash authority prevents independent implementations from converging fully. |
| Nothing under Deferred could let two units diverge | **Pass** | Conversation-owner resolution remains fixed to Facilitator until an explicit owner resolver exists; Dapr Conversation API is not used; all other deferred items are explicitly beyond V1 and cannot reopen V1 runtime or protocol ownership. |
| Named technology is verified-current | **Pass for the requested authority** | All selected versions match local authoritative files: SDK `10.0.301`/`latestPatch`, C# 14, Aspire 13.4.6, Dapr/Workflow 1.18.5, CommunityToolkit Dapr 13.4.1-beta.687, MediatR 14.2.0, FluentValidation 12.1.1, OpenTelemetry 1.17.0, Fluent UI 5.0.0-rc.4-26180.1, xUnit 3.2.2, Shouldly 4.3.0, and NSubstitute 5.3.0. Sibling commits also match the checked-out submodules. Provider and Agent Framework SDKs correctly remain `Unselected`. |
| Ratifies rather than contradicts the brownfield codebase | **Pass with explicit gaps** | Existing contracts/client/server/UI/testing structure is ratified. Current module-owned AppHost/Aspire/ServiceDefaults and provider capability-runtime drift are explicitly marked non-conformant implementation gaps rather than treated as evidence; Stories 5.1/5.6 own host correction. The companion command-step convention remains consistent with AD-3, AD-13, and AD-18. |
| Covers the driving specification's capabilities | **Pass** | FR-1–FR-28, OQ-1–OQ-13, NFR-1–NFR-14, evidence Levels 1–5, RQ-1, and the approved readiness/provider/concurrency/topology amendments all have architecture paths. The required Provider result, readiness schema, gate/projection inventories, browser seams, external dependencies, stack, and host gap are present. |
| Does not weaken an inherited parent spine | **Not applicable** | This is the initiative spine; no parent architecture spine is declared. Existing AD IDs remain stable and amendments preserve earlier valid domain/state boundaries. |
| Every altitude-owned dimension is decided, deferred, or open | **Conditional pass** | All initiative dimensions are represented. RG-01 leaves the shared operational state plane for capacity incomplete; RG-02 leaves the browser evidence-ingestion trust boundary implicit. No entire dimension is silent. |

## Approved Change And Register Reconciliation

| Required amendment/capability | Judgment | Governing architecture |
| --- | --- | --- |
| Public Provider readiness result with all six fields | **Pass** | AD-10, class diagram, consistency convention |
| `Degraded` callable only with all hard gates and `Callability == Callable` | **Pass** | AD-10 |
| One pending high-risk command per resource/family/session; EventStore and idempotency authoritative | **Conditional pass** | AD-12/AD-13 bind scope and authority; RG-04 concerns only lock-release precision. |
| Readiness record schema, state/freshness rules, minimum gates, explicit projections | **Pass** | AD-17 and class diagram exactly mirror the readiness register. |
| Non-circular qualification and release gate sets | **Pass** | AD-17 mirrors the readiness register's controlled-execution and release-qualification sets. |
| Executable NFR-11 recovery contract | **Pass** | AD-23 binds RPO 0, no duplicate effects, preserved terminal outcomes, 15-minute recovery, Levels 4/5, and `EXT-TOPOLOGY-1`. |
| Executable NFR-12 capacity/fairness contract | **Conditional pass** | Numeric profiles, pre-Provider queue/reject, fairness evidence, and cost coexistence are bound; RG-01 remains. |
| Executable NFR-13 conformance contract | **Pass** | AD-25 binds WCAG 2.2 AA, EN/FR parity, FrontComposer/Fluent V5, viewport blocking, coverage, and insufficiency. |
| Executable NFR-14 browser evidence contract | **Conditional pass** | AD-26 binds exact monotonic seams, thresholds, samples, and insufficiency; RG-02 is the remaining evidence-trust boundary. |
| Every `EXT-*` dependency referenced by its governing decision | **Pass** | AD-6/7, AD-9/10/21, AD-11, AD-14/16/21, AD-17/23–26 and the External V1 Prerequisites table cover all seven IDs. |
| Diagrams/capability map include readiness, capacity, browser telemetry, and platform host | **Conditional pass** | All four are present in both structural views and capability map; RG-03 concerns sequence semantics. |
| Stack aligned to the selected local workspace catalog | **Pass** | Every selected row was checked against `global.json`, `Directory.Build.props`, root/imported `Directory.Packages.props`, local AppHost SDK declarations, and sibling HEADs. |
| Current module host projects recorded as Stories 5.1/5.6 gap | **Pass** | AD-16, Structural Seed gap note, and capability map all state current presence is non-conformant. |

## Initiative Architecture Dimension Audit

| Owned dimension | Status | Decision/deferment |
| --- | --- | --- |
| Paradigm and decomposition | **Decided** | Event-sourced, Dapr-Workflow-orchestrated, hexagonal domain module; AD-1–AD-3. |
| Aggregate/data ownership | **Decided** | Agent, ProviderCatalog, AgentInteraction boundaries; AD-1/AD-2. |
| Mutation and consistency | **Decided** | EventStore commands/events, pure aggregates, optimistic concurrency, deterministic IDs; AD-3/AD-13 and implementation conventions. |
| Runtime orchestration and time | **Decided** | Dapr Workflow sole owner, replay-safe activities, stored/injected time; AD-18 and conventions. |
| External integration boundaries | **Decided/gated** | Conversations, Parties, Tenants, Provider, safety, tokenizer, secrets, topology; AD-6–AD-12, AD-20 and dependency register. |
| Public contracts and compatibility | **Decided** | Adapter-safe contracts, additive-first evolution, no SDK leakage; AD-9/AD-15/AD-17. |
| Authorization and tenant isolation | **Decided** | Re-evaluated pre-side-effect fail-closed gates and same UI/API outcomes; AD-8/AD-12/AD-15. |
| Security, privacy, safety, secrets | **Decided** | Sensitive-content boundaries, two-stage safety, no override/weaker retry, secret references only; AD-14/AD-20/AD-22. |
| Idempotency, retry, and concurrency | **Decided with RG-04 clarity issue** | Deterministic attempts/posts/versions and authoritative EventStore identity; AD-12/AD-13. |
| Context/model capability | **Decided** | Complete context only, exact tokenizer, fresh capability high-water and effective version; AD-4/AD-10/AD-11. |
| Cost controls | **Decided** | Hard caps, reservation/reconciliation, retry reuse; AD-21. |
| Capacity/backpressure/fairness | **Incomplete convergence** | Profiles/evidence are decided; shared operational-state/crash authority remains RG-01. |
| Proposal lifecycle and expiry | **Decided** | Immutable versions, complete terminal states, stored expiry and durable workflow timers; AD-5/AD-18 and conventions. |
| Audit/retention/export/deletion | **Decided** | Payload protection, 365-day retention, legal hold, manifested export, cryptographic erase/redact, named purges; AD-14/AD-22. |
| Read models/projections | **Decided** | Explicit 17-ID inventory and named deletion/evidence use; AD-17. |
| Readiness/release governance | **Decided** | Normative schema, freshness, gate sets, Levels 4/5, RQ-1; AD-17. |
| Reliability/recovery | **Decided** | RPO 0, duplicate-effect inventory, terminal preservation, 15-minute exercise; AD-23. |
| Runtime and UI performance | **Decided** | `LR-RUNTIME-PERFORMANCE` delegates NFR-9's versioned contract to the register; AD-26 defines NFR-14 browser seams exactly. |
| UI/accessibility/localization/responsiveness | **Decided** | FrontComposer/Fluent V5, public-contract parity, WCAG 2.2 AA, EN/FR parity, restrictive viewport; AD-15/AD-25. |
| Browser telemetry/evidence integrity | **Decided with RG-02 trust-boundary gap** | Sample fields, monotonic seams, safe content and qualification fixture are fixed; ingestion/attestation needs one more invariant. |
| Deployment and environments | **Decided/gated** | Platform-owned `EXT-HOST-1` composition for local/test/deployed; current module host projects are explicit gaps; AD-16. |
| Infrastructure/provider strategy | **Decided/gated** | Platform hosting, Dapr/EventStore, adapter leaves; Provider/Agent Framework SDK selection correctly gated by `EXT-PROVIDER-1`. |
| Observability and operations | **Decided** | Safe status/evidence projections, telemetry boundaries, readiness blockers, health/telemetry platform ownership; AD-14/AD-16/AD-17. |
| Testing and evidence | **Decided** | Test matrix, Evidence Levels, controlled topology, recovery/capacity/UI suites, no conditional readiness; AD-17/AD-23–26. |
| Structural seed and stack | **Decided** | Conformant module/package tree and locally verified Stack; current non-conformant projects are not silently ratified. |
| Beyond-V1 protocols/capabilities | **Deferred safely** | Tools, MCP, A2A, Python DurableAgent, memory, ambient/project/folder triggers, external channels, multiple named Agents. |

## Positive Conclusions And Residual Delivery State

- The approved Sprint Change Proposal is represented without renumbering or weakening existing valid ADs.
- The previous readiness-schema diagram omission is fixed: all 12 fields are now present.
- The architecture and both registers agree that all seven external dependencies are currently `Uncommitted` and all 18 launch gates are presently insufficient; this is an intentional fail-closed delivery state, not an architecture inconsistency.
- The current module-owned AppHost/Aspire/ServiceDefaults presence and Provider capability-runtime drift remain explicit implementation gaps, not false conformance claims.
- After RG-01 is resolved and RG-02–RG-04 are tightened, no remaining rubric item should block final architecture handoff.

## Resolution Addendum — Delta Re-review

**Delta verdict:** **PASS at the critical/high gate; one medium diagram defect remains.** No critical or high findings remain after reviewing the current spine and both registers.

| Prior finding | Result | Delta evidence |
| --- | --- | --- |
| RG-01 shared capacity authority | **Pass / resolved** | AD-13 and AD-24 now bind one cross-replica shared allocator, linearizable dual-scope admission, durable lease/queue identities, persisted weighted-round-robin order, retry identity, cancellation/expiry, safe lease reclamation, workflow recovery, and replica/crash evidence. The readiness register mirrors these invariants. |
| RG-02 trusted browser ingress | **Pass / resolved** | AD-26 and the readiness register now restrict qualifying samples to authenticated `EXT-TOPOLOGY-1` sessions and require platform validation of sample kind, tick/order/origin, deterministic idempotency, and server trace/projection correlation. Unattested telemetry cannot qualify. |
| RG-03 blocked/queued sequence semantics | **Fail / partially resolved (Medium)** | Queued, rejected, cancelled, and expired capacity paths are now distinct, and queued work resumes with the same durable `QueueId`. However, the final terminal response/sample remains outside the outer readiness `alt`, so the diagram still shows the initial blocked/no-interaction branch continuing to an authoritative terminal projection and terminal browser sample. Move terminal emission inside the interaction path or guard it with an interaction-exists/terminal condition. |
| RG-04 accepted-pending lock release | **Pass / resolved** | AD-12 now states that the lock begins on submission, accepted-pending keeps it held, and only pre-acceptance rejection or a terminal result releases it. |

The unresolved RG-03 item is a rendering inconsistency rather than a weakened prose invariant; it does not reopen a critical/high architecture decision.

**Final verification:** **PASS** — terminal projection and browser telemetry now occur only inside the callable branch, so RG-03 is resolved and no prior reviewer-gate finding remains open.
