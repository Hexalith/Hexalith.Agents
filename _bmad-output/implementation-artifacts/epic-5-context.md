# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver a complete, governed setup foundation for the tenant-scoped `hexa` Agent: administrators configure it through live public operations, while every API and UI surface reports the same authoritative lifecycle, setup validity, Provider state, freshness, blockers, and callability. The result must be replayable, tenant-isolated, fail closed on uncertainty, and safe for later automatic-response, approval, governance, and release-qualification work to consume.

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
- Configuration, lifecycle, response mode, Provider/model selection, tenant enablement, data-handling acceptance, approver policy, and readiness must use authorized public commands and queries backed by durable EventStore state. Accepted writes expose authoritative-pending identities; projections and replay must converge deterministically.
- Lifecycle `active` is never synonymous with callable. Callability requires current, complete setup and readiness evidence at one registry checkpoint; missing, stale, ambiguous, unauthorized, uncommitted, unavailable, or unknown inputs block without Provider or Conversation side effects.
- Provider governance is platform-scoped, while tenant visibility, enablement, selection, and acceptance remain tenant-scoped. Secrets are reference-only and must never appear in events, projections, responses, logs, telemetry, UI, audit summaries, or accessibility output.
- Authorization and tenant isolation apply before mutation or disclosure. Cross-tenant and absent-resource responses must not reveal existence, counts, blocker details, timing, Party membership, instructions, or Provider data.
- Public contracts remain automation-friendly and additive within V1. Deprecated wire values stay deserializable but must be rejected before mutation; this includes caller-as-approver, auditable safety override, and reporting-only or accepted-risk cost-control postures.
- Content-bearing execution remains disabled until payload protection is available and attested. Sensitive interaction content must be sealed before durable or execution infrastructure, workflow state carries references only, and destroyed keys replay as a typed erased result.
- Clean-checkout, source-mode, package-mode, boundary, public-contract, live integration, persisted end-state, and negative fail-closed evidence are required. Skips, placeholders, local paths, conditional passes, or lower evidence levels cannot establish production-like readiness.

## Technical Decisions

- Hexalith.Builds is the sole NuGet version authority. The Agents `Directory.Packages.props` is an import-only Central Package Management wrapper for `Hexalith.Builds/Props/Directory.Packages.props`; it may contain path/policy plumbing but no `PackageVersion`, package-specific version property, `VersionOverride`, or fallback version list. Story compatibility floors are resolved in Hexalith.Builds first, then consumed through the root-declared Builds gitlink.
- The EventStore package used by the Release/package lane must meet the existing `>= 3.105.0` floor through the shared Builds catalog. An Agents-local override is a conformance failure, not completion evidence. Debug development uses intentional source references; Release validation consumes produced packages without reverse or source-only dependencies.
- Agents is an EventStore-backed domain module. Aggregates own durable business truth; pure command handling emits events, projections provide reads, and external effects return through ports and follow-up commands. The module ships no AppHost, Aspire, or ServiceDefaults project.
- The platform-owned host composes the Agents DomainService and FrontComposer UI with EventStore, Dapr Workflow, identity, Conversations, Parties, Tenants, readiness, capacity, secrets, safety, protection, telemetry, and health. Unavailable integrations remain explicit blockers rather than simulated success.
- Readiness uses one versioned operation-gate matrix, one consistent registry checkpoint, server-evaluated freshness, typed safe blocker codes, and a newest-record-wins rule with no fallback to an older pass. API, BFF, projections, workflows, and UI consume the same values and do not infer readiness locally.
- Public HTTP routes are versioned under `/api/v1/agents/...`; breaking contract changes require a major package/API version and package-consumer compatibility evidence. Unknown enum values fail closed.

## UX & Interaction Patterns

Agents UI is composed through FrontComposer and inherits Fluent UI Blazor V5 without a custom theme; the Fluent package version is selected by the shared Hexalith.Builds catalog. The primary setup surfaces are the Agents overview, `hexa` configuration, Provider catalog, Approver policy, operational status, and launch-readiness views.

Every governed write visibly progresses through `Submitted`, `AuthoritativePending`, and `ProjectionConfirmed`; optimistic state is never presented as authoritative. Lifecycle and callability are displayed separately, blockers name a safe recovery owner, and stale or uncertain state never uses success styling. Controls unavailable to a role are absent, server authorization remains authoritative, and secret values are never rendered.

Interactive surfaces must meet WCAG 2.2 AA behavior, whole-string English/French parity, keyboard and live-region requirements, and operability at 320 CSS pixels. High-impact actions fail closed when required decision context cannot be presented.

## Cross-Story Dependencies

Story 5.1 establishes the build and package boundary. Stories 5.2 and 5.3 then establish live Agent and Provider-catalog operations; Story 5.4 adds trusted identity and authorization evidence; Story 5.5 defines shared readiness truth; Story 5.6 proves the platform-hosted integration tier; and Story 5.7 consumes those results for activation. Story 5.8 depends on the package/integration foundation and becomes the content-bearing prerequisite for later epics. Stories 5.9 and 5.10 tighten the readiness and compatibility contracts after their owning foundations exist.

The approved package correction reopens Story 5.1 and keeps Story 5.2 in review until the shared EventStore floor is selected in Hexalith.Builds, the Agents Builds gitlink advances, and the Release/package lane passes. Only root-declared submodules may be initialized; nested submodules must not be introduced. External host, topology, secrets, protection, Provider, Parties, and Conversations seams gate only the stories that declare them, and unavailable seams must remain visible blockers.
