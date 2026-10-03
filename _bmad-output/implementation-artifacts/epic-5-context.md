# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver tenant-scoped `hexa` setup with shared authoritative configuration, lifecycle, readiness, and callability. Preserve history; uncertainty blocks effects.

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

- Platform provisions one `hexa` per tenant, create-only under the Agents Service Principal. Exact retries preserve identity; divergence conflicts. Tenant administrators configure/activate/disable/inspect but cannot create another Agent, delete it, or replace its Party.
- Branch B is the selected implementation direction: preserve the Organization Party and verify its immutable tenant-scoped ID. Formal Product/Parties acceptance remains pending. Type alone proves no Agent identity; Uncommitted development supplies no launch evidence.
- Require authoritative Party classification/liveness and evidence position/time; humans need stable non-PII actor identity, versioned current binding, and action-time history. Missing/stale/ambiguous/overlapping/mismatched evidence blocks; non-human Parties cannot approve.
- Durable changes require expected revisions, deterministic duplicates, and persisted projection outcomes. Active never proves callable; revocation blocks effects. Authorize before lookup/disclosure/mutation; cross-tenant responses reveal no existence, counts, membership, instructions, or configuration.
- Provider catalog belongs to `system`; tenant enablement, selection, and versioned data-handling acceptance remain separate. Configuration applies prospectively; migration preserves history. Expose secret references/configured state only.
- Retired Caller/safety-override/prohibited-cost values remain deserializable but cannot mutate/authorize. Unknown values block.

## Technical Decisions

- Pure EventStore aggregates own state; Server/Dapr Workflow coordinate effects through ports. Hexalith.Builds owns versions; Debug uses project references, Release public packages. Agents owns no AppHost/Aspire/ServiceDefaults.
- Select one current operation-specific User/Administrator/Platform/Workflow principal; Platform humans require Tenants `global-administrators`. Actor identity survives role changes; history grants no authority. Strip reserved client extensions; verify scope-bound canonical HMAC before dispatch.
- Separate logical command identity/delivery nonce. Private replay registration uses reserved `system` issuer/nonce identity before target construction. Exact replay reuses stored times and reaches domain idempotency; changed fields/cross-tenant nonce reuse deny and audit.
- 5.4 owns restricted replay registration and content-free security recording. Route from authenticated authority, durably spool denials before reporting processed, and drain exactly once with durable acknowledgement. Replay/spool failure blocks target construction. Platform owns replicated spool, custody, worker, and restricted credentials.
- Matrix-v7 readiness uses one registry checkpoint, current producer/scope-valid observations, typed blockers, and newest-record-wins without fallback. Preserve callable Degraded Providers. Independent runtime decision authority governs open outcomes; recorders/custodians cannot mint approvals.
- Platform protection seals sensitive fields before infrastructure; workflows carry references. Exact-target canaries prove persisted ciphertext, unseal, destruction, and Erased replay. No-op protection, uncertain legacy plaintext, or unresolved Instruction classification blocks applicable work.
- `/api/v1/agents/...` exposes safe domain contracts.

## UX & Interaction Patterns

Use FrontComposer/Fluent V5. Separate lifecycle, suspension, readiness, and callability. Writes show Submitted, AuthoritativePending, then ProjectionConfirmed; uncertainty never shows success. Identity is inspect-only. Approver sources: Facilitator, predefined human Party, tenant role. Hide unauthorized controls; name recovery owners. Preserve English/French parity, WCAG 2.2 AA, and 320-pixel operability.

## Cross-Story Dependencies

5.1 provides the boundary; 5.2/5.3 independently provide Agent/catalog operations; 5.4 authority/security; 5.5 shared readiness; 5.6 composition; 5.7 prior-setup activation. 5.8 protects later content; 5.9/5.10 tighten foundations. No later-epic dependency.

All twelve external records remain Uncommitted. Ready-for-dev requires complete accepted fields, immutable target/date/command for every declared record. Committed permits contract/package work; only its owner's accepted exact-target compatibility command may execute to establish Available. Consumers require Available and passing exact-target evidence, otherwise DependencyNotAvailable(record). Levels 4/5 and persisted-state gates remain; fixtures prove decisions only. Historical host evidence preserves 5.1 completion without availability.

Full 5.4 remains draft/backlog with complete Parties/Conversations/host/secrets dependencies, including host/secrets migration/governance/destruction extensions. 5.3 invokes no Provider seam. Host/protection/topology gates and conditional recorder, Instruction-protection, and legacy-plaintext decisions remain; no local default or subset resolves them.
