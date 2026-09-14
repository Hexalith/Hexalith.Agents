# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable Agent Administrators to configure `hexa` through live authorized operations and receive one authoritative, tenant-scoped explanation of lifecycle, setup validity, Provider state, blockers, and callability. This epic establishes durable setup and readiness truth—including platform composition and payload protection—so later interaction and governance work can fail closed instead of trusting inferred, stale, or host-local state.

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

- A Platform Operator provisions exactly one `hexa` per tenant through an idempotent create-only operation. Its Party identity is immutable and verified by id. Tenant administrators may configure and inspect it but cannot create another Agent, delete it, or link or replace its Party identity.
- Authorized public operations manage versioned instructions, lifecycle, response mode, Approver Policy, and Provider/model selection. Accepted writes must be EventStore-persisted, replay-deterministic, auditable, and visible through versioned projections; response-mode and selection changes affect future interactions only.
- Every command, query, and readiness evaluation uses current tenant, Party, role, and dependency evidence before mutation, disclosure, or downstream work. Missing, stale, ambiguous, revoked, unavailable, unknown, or indeterminate state fails closed. Cross-tenant responses, counts, diagnostics, accessible text, logs, and audit summaries must not disclose existence, instructions, secrets, or Party data.
- Platform Provider/model truth is governed once in reserved tenant `system`; tenant visibility, selection, and data-handling acceptance are separate. Catalog entries require enabled/configured state, text-generation capability, positive limits, server-versioned pricing, monotonic capability versions, secret-reference state, and complete versioned data-handling terms. A tenant must accept the applicable terms except during a valid, non-extendable tightening grace. Secrets and Provider SDK types never cross public, durable, telemetry, audit, or browser boundaries.
- Lifecycle `Active`, tenant suspension, Provider readiness, callability, and production enablement are distinct. One matrix-v7 evaluation at one registry revision supplies all applicable records and blockers. Provider readiness permits only ready/callable, degraded-but-callable with the defined warning, or blocked/not-callable with a defined reason; no surface may infer a better result.
- Sensitive prompt, context, generated, edited, failed, and audit content is field-level sealed before durable or execution infrastructure, remains sealed through transport and projections, and replays as typed `Erased` after irreversible key destruction. The no-op protection service or failed identity/canary attestation keeps `PayloadProtectionUnavailable` active. Instruction protection and any legacy-plaintext disposition remain governed by their explicit open decisions.
- Only hard-enforcement cost postures may continue through readiness. Reporting-only monitoring, accepted launch risk, caller-as-Approver, and safety override remain declared for additive wire compatibility but are obsolete and rejected before mutation; persisted legacy values remain safely non-active.
- Public contracts are structured for automation, additive within V1, use `Unknown = 0` enum sentinels, and expose no EventStore stream, aggregate, projection, workflow, host, or Provider SDK details. Deterministic fixtures prove calculation only; live and production-like claims require registered integration evidence of persisted end state.

## Technical Decisions

- Agents is an EventStore-backed domain module. Aggregate handlers are pure; dependency reads, authorization, secret access, and other effects execute in fail-closed orchestration and return through deterministic commands. Trusted principal context is server-derived, scope-bound, authenticated, replay-protected, and stripped of client-supplied reserved fields.
- `Agent` owns setup, reserved-system `ProviderCatalog` owns platform catalog truth, `TenantProviderEnablement` owns tenant eligibility and data-handling decisions, and `LaunchReadinessGate` plus durable architecture-decision records own readiness inputs. Migration from legacy tenant-scoped Provider catalogs is idempotent and freezes rather than rewrites history.
- The package boundary contains domain, Contracts, Client, Server, UI, and Testing assets, but no module-owned AppHost, Aspire, or ServiceDefaults. A platform-owned host composes the DomainService and FrontComposer UI with EventStore, Tenants, Parties, Conversations, Dapr Workflow, readiness, secrets, payload protection, telemetry, health, and evidence ingress.
- Public routes live under `/api/v1/agents/...`. Accepted writes return authoritative pending identities and projection references, never callability. Readiness selects the greatest committed revision, never falls back to an older pass, and shares one freshness value and registry checkpoint across API, UI, workflows, and projections.
- Provider transport stays behind an Agents-owned adapter, credentials resolve only inside the platform secret boundary, and workflow state carries references rather than protected content.

## UX & Interaction Patterns

Use FrontComposer and Fluent UI Blazor V5 without redefining the theme. The overview separates lifecycle, suspension, proven callability, Provider readiness, freshness, and production enablement. Setup writes follow `Submitted -> AuthoritativePending -> ProjectionConfirmed`; optimistic or catch-up state is never Success. Provider and Approver controls expose only role-authorized fields and supported values, use tenant-scoped Party search rather than free-text identifiers, and show safe blockers with recovery ownership. High-impact confirmations keep focus and required context available. All surfaces require keyboard operation, deterministic focus and live-region feedback, whole-string English/French parity, operability at 320 CSS px, and no secret or cross-tenant disclosure.

## Cross-Story Dependencies

Story 5.1 establishes the boundary for 5.2 and 5.3; 5.4 builds on 5.1–5.2; 5.5 joins 5.2–5.4; 5.6 builds on 5.1 and 5.5; 5.7 consumes 5.2–5.6; 5.8 builds on 5.1 and 5.6; 5.9 follows 5.5 and 5.7; and 5.10 follows 5.4 and 5.5. Catalog governance does not consume `EXT-PROVIDER-1`; other live identity, authority, host, secrets, protection, and topology work requires the exact register targets and compatibility evidence declared by its story. Current external records are `Uncommitted`; recorder-scope and instruction-protection decisions are open, and legacy plaintext conditionally adds another open-decision blocker. Release qualification remains outside this epic and cannot be established by synthetic fixtures.
