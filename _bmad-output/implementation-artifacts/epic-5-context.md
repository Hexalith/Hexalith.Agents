# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver the governed setup foundation for the tenant-scoped `hexa` Agent: administrators configure it through live public operations, while API, client, projection, and UI surfaces share one authoritative explanation of lifecycle, setup validity, Provider state, evidence freshness, blockers, and callability. The result must be replayable, tenant-isolated, fail closed on uncertainty, and safe for the later automatic-response, approval, governance, and release-qualification outcomes to consume.

## Stories

- Story 5.1: Establish Build Package Boundary And Basic CI Gates
- Story 5.2: Configure hexa Through Live EventStore Operations
- Story 5.3: Govern Provider Models And Pricing Through Live Operations
- Story 5.4: Prove Trusted Principal Tenant Party And Approver Readiness
- Story 5.5: Publish Authoritative Readiness And Provider-State Contracts
- Story 5.6: Compose Agents In The Platform-Owned Production-Like Host
- Story 5.7: Activate hexa Only When Setup Gates Pass
- Story 5.8: Protect Sensitive Agent Content At The EventStore Boundary
- Story 5.9: Reject Prohibited Cost-Control Postures At Readiness Recording
- Story 5.10: Retire Prohibited Safety And Caller Policy Inputs

## Requirements & Constraints

- Provision exactly one `hexa` per tenant through an idempotent, create-only platform operation. Its Party identity is immutable; tenant administrators may configure, activate, disable, and inspect the Agent but may not create another Agent, delete it, or replace its identity.
- Configuration, lifecycle, response mode, Provider/model selection, tenant enablement, data-handling acceptance, approver policy, and readiness use authorized public commands and queries backed by durable EventStore state. Replay, duplicate delivery, projection delivery, and expected-revision conflicts must have deterministic outcomes.
- Lifecycle `Active` is never synonymous with callable. Callability requires current, complete evidence at one registry checkpoint; missing, stale, ambiguous, unauthorized, uncommitted, unavailable, or unknown inputs block before Provider or Conversation effects.
- Provider governance is platform-scoped under reserved tenant `system`; tenant visibility, enablement, selection, and data-handling acceptance remain tenant-scoped. Platform enablement is not tenant acceptance. Secret values never cross Agents boundaries: only references and configured state may appear.
- Authorization and tenant isolation apply before lookup-dependent disclosure or mutation. Cross-tenant and absent-resource behavior must not reveal existence, counts, blocker detail, Party membership, Agent instructions, or Provider configuration.
- V1 public contracts remain additive and automation-friendly. Retired wire values remain deserializable but are rejected before mutation, including Caller as approver, safety override, and reporting-only or accepted-risk cost-control postures. Unknown enum values fail closed.
- Content-bearing execution stays disabled until the host-bound protection engine passes its canary attestation. Sensitive fields are sealed before durable or execution infrastructure, workflow state carries references only, and destroyed keys replay as typed `Erased` without rewriting EventStore history.
- Production-like claims require passing live integration and persisted end-state evidence. Skips, placeholders, local paths, simulated availability, conditional passes, or deterministic fixtures alone cannot establish live readiness.

## Technical Decisions

- Hexalith.Builds is the sole package-version authority. The Agents Central Package Management wrapper imports the shared catalog and contains no local `PackageVersion`, package-specific version property, `VersionOverride`, or fallback list. Debug/source mode uses intentional project references; Release/package verification consumes produced public packages.
- Agents is an EventStore-backed domain module with pure, replay-safe aggregate decisions and impure fail-closed orchestration around external reads and effects. Durable authorization precedes any protected-content access or external effect; retries reuse deterministic command and observation identities.
- The module owns no AppHost, Aspire, or ServiceDefaults project. A platform-owned host composes the DomainService and FrontComposer UI with EventStore, Dapr Workflow, identity, Conversations, Parties, Tenants, readiness, capacity, secrets, safety, protection, telemetry, health, and the durable security-audit spool.
- Trusted ingress selects exactly one principal kind, derives tenant and Party authority from current platform evidence, strips client-reserved extension keys, and verifies the platform-issued scope-bound envelope before aggregate dispatch. Replay and authorization denials are recorded through target-limited system capabilities without constructing the denied target command.
- Readiness uses Operation Gate Matrix version 7, one consistent registry checkpoint, producer- and scope-valid observations, server-evaluated freshness, typed safe blockers, and newest-record-wins behavior with no fallback to an older pass. API, UI, workflow, and projections consume this contract rather than calculating local variants.
- Public routes are authoritative under `/api/v1/agents/...`; public results hide stream, aggregate, projection, workflow, and Provider SDK details.

## UX & Interaction Patterns

Use FrontComposer and Fluent UI Blazor V5 without a module-defined theme. The overview keeps lifecycle, tenant suspension, readiness, and proven callability as separate facts; stale, catching-up, blocked, submitted, or authoritative-pending states never use success styling. Governed writes progress visibly through `Submitted`, `AuthoritativePending`, and `ProjectionConfirmed`, and blockers name a safe recovery owner.

Controls outside the viewer's authority are absent rather than cosmetically disabled, while server authorization remains authoritative. Surfaces must preserve whole-string English/French parity, WCAG 2.2 AA keyboard and live-region behavior, and operability at 320 CSS pixels. Secret values and cross-tenant details never render, including in errors and accessible names.

## Cross-Story Dependencies

Story 5.1 establishes the completed build/package boundary. Stories 5.2 and 5.3 independently add live Agent operations and platform-catalog/tenant-enablement operations; Story 5.4 adds trusted principal, Party, approver, and security evidence; Story 5.5 publishes the shared readiness contract; Story 5.6 proves platform-hosted composition and the live integration tier; and Story 5.7 consumes only those prior setup outcomes for activation. Story 5.8 depends on the package and integration foundations and becomes the content-bearing prerequisite for later epics. Stories 5.9 and 5.10 tighten the readiness and compatibility contracts after their owning foundations exist.

External seams gate only the stories that declare them: Parties and Conversations identity/approver reads, platform host and topology, Provider commitment, secrets, and payload protection must reach the required register status before live evidence can count. Story 5.3 does not consume the Provider runtime seam. Open runtime decisions, including release-recorder scope and instruction-protection classification, remain explicit blockers; no story-local default may resolve them. Epic 5 has no dependency on Epics 6–8.
