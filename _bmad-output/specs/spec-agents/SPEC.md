---
id: SPEC-agents
companions:
  - ../../planning-artifacts/prds/prd-agents-2026-06-23/prd.md
  - ../../planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md
  - ../../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
  - ../../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ../../planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md
  - ../../planning-artifacts/external-dependency-register.md
  - ../../planning-artifacts/launch-readiness-register.md
sources: []
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and qualify. The companions remain owned by their originating workflows and retain their detailed requirement, UX, architecture, dependency, and evidence authority.

# Hexalith Agents

## Why

Hexalith Agents must make `hexa` a named, tenant-scoped AI participant inside durable Conversations without weakening Party identity, authorization, approval, safety, privacy, cost, audit, or recovery guarantees. V1 proves that an authorized participant can explicitly request contextual help and receive either an attributed automatic response or a fully governed proposed reply whose path into the Conversation is explainable and repeat-safe.

## Capabilities

- **CAP-1 — Tenant Agent setup and lifecycle**
  - **intent:** A Tenant Agent Administrator can configure, activate, disable, and inspect the tenant's single `hexa`, including its Party identity, instructions, tenant-wide response mode, Provider/model selection, Approver Policy, context policy, expiry, and regeneration bounds (FR-1..FR-7; amended AD-2, AD-4, AD-7, AD-30).
  - **success:** Activation and calls remain blocked until current identity, configuration, role, Provider, and readiness gates pass; accepted changes are versioned and audited, and later changes affect only future interactions.

- **CAP-2 — Provider governance and tenant enablement**
  - **intent:** A Platform Operator can govern the platform Provider/model catalog and the entries enabled for each tenant, while tenant administrators can select only enabled entries (FR-4, FR-5, FR-33; amended AD-2, AD-9, AD-10, amended AD-17, AD-30).
  - **success:** The platform catalog and complete aggregate inventory enforce monotonic capability and pricing versions, safe secret visibility, tenant-filtered readiness, and fail-closed selection and invocation.

- **CAP-3 — Explicit Conversation invocation and context**
  - **intent:** An authorized Conversation Participant can call `hexa` only through the Conversation-owned **Call hexa** action, using the complete authorized Source Conversation when it fits the safe context budget (FR-8..FR-10, FR-31; AD-6, AD-11, AD-12, amended AD-20, amended AD-22, AD-27, AD-29, AD-30, AD-31).
  - **success:** An unavailable, unauthorized, stale, oversized, unsafe, or blocked request creates no Provider work, proposal, or Conversation Message; an accepted request has one tenant-bound interaction identity and visible authoritative status.

- **CAP-4 — Reliable generation and automatic response**
  - **intent:** An accepted call can generate a governed response and Automatic Response Mode can post it once as the `hexa` Party (FR-10..FR-12; AD-5..AD-7, AD-13, amended AD-17, AD-18, amended AD-20, AD-21, amended AD-22, AD-24, AD-27, AD-28, AD-29, AD-30, AD-31).
  - **success:** Retry, replay, restart, timeout, and unknown Provider outcomes never duplicate attempts, versions, reservations, or messages; content never persists in execution state, time and identities remain deterministic, and only confirmed posting reaches `Posted`.

- **CAP-5 — Proposed reply review and resolution**
  - **intent:** Eligible Approvers can discover, edit, regenerate, approve, reject, abandon, and resolve immutable Proposed Agent Reply versions outside the Conversation (FR-13..FR-18; AD-4, AD-5, AD-8, AD-13, amended AD-20, amended AD-22, AD-27, AD-28, AD-29, AD-30).
  - **success:** The canonical proposal lifecycle enforces expiry, segregation of duties, regeneration and retry bounds, exact-version safety and posting, immutable history, and safe terminal recovery without stranding proposals.

- **CAP-6 — Tenant isolation and trusted authorization**
  - **intent:** Every administration, call, proposal, posting, governance, status, and audit operation enforces tenant, Party, Conversation, policy, and FR-33 role authority before side effects (FR-19..FR-21, FR-33; amended AD-2, AD-8, AD-12, amended AD-22, AD-29, AD-30).
  - **success:** Each command carries exactly one verified principal; forged reserved extensions and stale authority fail closed and create content-free audit evidence; cross-tenant or undisclosed keys are indistinguishable from absence.

- **CAP-7 — Content safety and instruction authority**
  - **intent:** The Platform Operator with Security approval can publish versioned Content Safety Policy, tenants can only tighten it, and Conversation content remains untrusted data (FR-26, FR-27, FR-31; AD-12, AD-14, amended AD-20, amended AD-22, AD-27, AD-28, AD-29, AD-30).
  - **success:** Fresh durable decisions cover prompt and context before Provider use, generated output before proposal or post, the exact approved version, and pre-post; lineage proves no retry or re-check used a weaker policy, and no Approver can override failure.

- **CAP-8 — Cost, rate, and capacity governance**
  - **intent:** Authorized operators can configure hard tenant cost caps, rate limits, regeneration bounds, and production capacity controls that govern every call and regeneration (FR-28, FR-32; amended AD-2, AD-13, amended AD-17, AD-21, AD-24, AD-28, AD-29, AD-30).
  - **success:** Atomic admission and reservation prevent overspend and starvation, every reservation reaches a bounded settlement, queues are bounded and fair across replicas, and rejection occurs before Provider cost.

- **CAP-9 — Governed UI and public contracts**
  - **intent:** Administrators, approvers, operators, inspectors, participants, and API clients can use the same governed capabilities and authoritative states through FrontComposer UI and versioned public contracts (FR-22, FR-23, FR-29; AD-15, amended AD-17, AD-25, AD-26, AD-28, AD-30, AD-31).
  - **success:** Every binding UX route and the Conversation contribution expose matching authorization and pending/projection states, WCAG 2.2 AA behavior, English/French parity, responsive safety, browser-monotonic timing, and no alternate production invocation route.

- **CAP-10 — Audit evidence and protected governance**
  - **intent:** Authorized users can trace calls and responses, and authorized governance roles can inspect evidence, apply or release holds, request export, and request approved deletion (FR-24, FR-25, FR-30; amended AD-2, AD-14, amended AD-17, amended AD-22, AD-27, AD-28, AD-29, AD-30).
  - **success:** Field-level protected content obeys the 365-day terminal retention clock, sealed export and two-phase hold rules, and restrictive erasure including named projections and workflow state, while immutable streams remain replayable as erased tombstones.

- **CAP-11 — Operational and release qualification**
  - **intent:** Release Operators can inspect operational status and evaluate launch readiness, recovery, capacity, UI conformance, performance, and product metrics from one versioned evidence authority (FR-25, FR-28; NFR-9..NFR-14; amended AD-17, AD-23..AD-26, AD-28, AD-29, AD-30).
  - **success:** `RQ-1` records `READY` only at one registry checkpoint when every required gate and consumed dependency qualifies at Evidence Levels 4 and 5 and every Product, Architecture, or Governance assumption is retired; otherwise it records `NOT READY` with named blockers.

## Constraints

- Hexalith Agents is a full EventStore domain module with the exact fourteen-aggregate inventory and platform-scoped `ProviderCatalog` plus `TenantProviderEnablement` defined by AD-1 and amended AD-2; projections and workflow state never become business truth.
- Aggregates remain pure, each durable orchestration step dispatches at most one trusted command under `IMPLEMENTATION-CONVENTIONS.md`, and Dapr Workflow is the sole V1 durable execution owner (AD-3, AD-18).
- Execution state carries references, identities, versions, revisions, digests, and safe classifications only; terminal workflow state is purged, and deletion completion includes `workflow-execution-state` (AD-27, amended AD-22).
- Every instant, comparison, deadline, timer, metric, and duration uses the distinct authorities in AD-28; browser clocks never decide domain state.
- Every deterministic identity, idempotency tuple, sensitive digest, and replay path follows the shared derivation in AD-29; tenant scope is included and conflicting payload reuse is rejected.
- Every command carries exactly one AD-30 principal in a stripped, allowlisted, HMAC-tagged trusted envelope; workflow and platform principals cannot borrow tenant-administrator authority.
- Safety comparison uses the amended AD-20 category-set and `LastLoosensVersion` lineage test; version order or a rank alone cannot prove that a policy is at least as restrictive.
- Sensitive content uses the amended AD-22 field-level `ProtectedContent` envelope, per-interaction keys, two-phase hold pinning, sealed transport and export, and restrictive deletion confirmation; content-bearing workflows remain disabled until `EXT-PROTECTION-1` is `Available`, and raw content and secrets never enter logs, telemetry, status, or execution state.
- The amended AD-17 registry grammar, `AuthorizedProducer` rules, operation-gate matrix, discriminated `Freshness`, projection inventory, live-seam tests, and Evidence Level 4/5 rules are normative. `RQ-1` remains `NOT READY` while required evidence or consumed dependencies are insufficient or uncommitted.
- A consuming story cannot become `ready-for-dev` until each dependency it consumes is `Committed` or `Available` with a complete accepted record; runtime and qualification may execute a consumed seam only when it is `Available` and its verification command passes (FR-21, amended AD-17).
- Conversations never references Agents packages. The **Call hexa** trigger, self-contained panel, callability query, and `MessageId`-keyed provenance decoration use only `EXT-CONV-UI-1`; the pre-integration harness is removed before its consuming story closes (AD-31).
- V1 uses only the complete authorized Source Conversation when it fits; it declares no bounded-context behavior and never truncates, summarizes, or windows silently (AD-11).
- PRD assumptions A-1..A-17 and architecture assumptions ARCH-A-1..ARCH-A-7 remain provisional. Every unretired Product, Architecture, or Governance assumption blocks `RQ-1`; this spec introduces no replacement assumption.

## Non-goals

- Long-term Agent memory; tools, MCP, A2A, remote agents, or alternate workflow owners.
- Project, folder, file, external-knowledge, or non-Conversation retrieval; ambient, project, folder, mention, command, or external-channel triggers.
- Business actions beyond posting governed replies to Conversations.
- Multiple product-visible Agents beyond `hexa`, or per-Conversation response-mode overrides.
- Customer-facing billing, invoicing, or monetization.

## Success signal

A production-like tenant demonstrates configuration, explicit Conversation invocation, automatic and confirmation posting, governance, and audit flows with no unauthorized or duplicate effect. `RQ-1` then records `READY` from one current registry checkpoint with complete qualifying evidence, committed and available consumed dependencies, and no unretired launch-blocking assumptions.

## Open Questions

- OQ-15: Will Conversations add a first-class owner contract and a true owner resolver after V1?
- OQ-18: By the provisional 2026-10-15 decision date, what per-category handling should replace the V1 fail-closed treatment of unsafe historical Conversation content, which currently has no redaction or exclusion path?
- OQ-23: What Automatic Response Mode retraction metric and Conversations retraction seam must exist before the first Automatic-mode tenant is enabled?
