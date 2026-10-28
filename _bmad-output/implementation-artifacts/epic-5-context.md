# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let administrators configure tenant-scoped `hexa` through live public operations and receive one authoritative explanation of lifecycle, setup validity, Provider state, and callability. Preserve history and block effects whenever current authority or dependency evidence is uncertain.

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

- Platform provisions one `hexa` per tenant, create-only under Agents Service Principal. Exact retries preserve identity; divergence conflicts. Tenant administrators configure, activate, disable, and inspect; second-Agent creation, deletion, and Party replacement are forbidden.
- Selected Branch B preserves the Organization Party verified by immutable tenant-scoped ID. Require current classification/liveness, position/time, stable non-PII human actor identity, and versioned current/action-time binding. Missing, stale, ambiguous, overlapping, or mismatched evidence blocks. Approvers must be human, have current Source Conversation access, and satisfy the configured basis; separation compares stable actors across principal kinds.
- Authorize before lookup, disclosure, or mutation. Cross-tenant results reveal no existence, data, or side effects. Expected revisions, deterministic duplicate handling, replay, and persisted projections preserve truth. Active never proves callable; revocation blocks.
- Provider catalog belongs to `system`; tenant enablement, selection, and versioned data-handling acceptance remain separate. Changes apply prospectively; migration preserves history. Expose secret references/configured state only. Retired Caller, safety-override, and prohibited-cost values remain deserializable but cannot mutate or authorize; unknown values block.
- Human-binding history retains the accepted 365 fixed days from binding-effective-at with exclusive expiry; retries, erasure, rebind, and restore cannot extend deadlines. Custody, destruction, restore, and successor continuity need qualification. Interaction-content retention starts at terminal state.

## Technical Decisions

- Pure EventStore aggregates own state; Server/Dapr Workflow coordinate effects. Hexalith.Builds owns package versions; Debug uses project references, Release public packages. Platform owns hosting; modules carry no AppHost or ServiceDefaults.
- Select one operation-specific User/Administrator/Platform/Workflow principal. Platform humans require Tenants `global-administrators`; history grants no current authority. Strip reserved client extensions and verify canonical scope-bound HMAC before dispatch.
- Accepted envelope bounds: lifetime 300 seconds, future tolerance 30 seconds, healthy rotation overlap 600 seconds, recovery horizon 86,400 seconds, first-seen replay retention 604,800 seconds. Expiry is exclusive; revocation blocks immediately. Issuer/audience/versioned trust and qualified custody remain required.
- Separate logical command identity from delivery nonce. Private replay registration uses reserved `system` issuer/nonce identity before target construction. Exact replay reuses stored times; changed-field/cross-tenant nonce reuse denies and audits. Restricted recorder routes from authenticated authority, durably spools denials before reporting processed, and drains exactly once with acknowledged EventStore append. Replay/spool failure blocks dispatch. Platform owns replicated spool, worker, custody, and restricted credentials.
- Matrix-v7 readiness uses one registry checkpoint, current producer/scope-valid observations, typed blockers, and newest-record-wins without fallback. Preserve callable Degraded Providers. Recorders/custodians cannot mint independent decision approvals. Versioned routes use `/api/v1/agents/...`.
- Protect sensitive content before infrastructure; workflows carry references. Exact-target canaries prove persisted ciphertext, unseal, destruction, and Erased replay. Missing/no-op protection, uncertain legacy plaintext, or unresolved Instruction classification blocks applicable work.

## UX & Interaction Patterns

Use FrontComposer/Fluent V5. Separate lifecycle, suspension, readiness, and callability. Writes show Submitted, AuthoritativePending, then ProjectionConfirmed. Identity is inspect-only. Approver sources are Facilitator, predefined human Party, and tenant role. Hide unauthorized controls; preserve English/French parity, WCAG 2.2 AA, and 320-pixel operability.

## Cross-Story Dependencies

5.1 establishes boundaries; 5.2/5.3 supply Agent/catalog operations; 5.4 authority/security; 5.5 shared readiness; 5.6 platform composition; 5.7 activation. 5.8 protects content-bearing Epics 6–8; 5.9/5.10 tighten cost and retired-policy foundations. No forward-story dependency.

The external register alone establishes commitments. Ready-for-dev needs complete accepted Committed/Available records. Committed permits contract/package work and only the owner's accepted exact-target compatibility command to establish Available. All other consuming runtime, test, and qualification paths return `DependencyNotAvailable(record)` until Available with passing exact-target evidence. Available requires live Level 4; required Level 5 completes in consuming qualification/RQ-1. Changed contracts invalidate acceptance; fixtures cannot establish availability.

Full 5.4 is approved; completed 5.1/5.2 and four Committed records satisfy entry. The accepted 2026-10-18 replacements supersede older targets. All four pinned owner commands subsequently exited 1; none is Available. Preserve full Conversations, custody, host, and shared EventStore scope. A subset cannot advance a broader record. The bounded owner cohort OQ-5.4-1 grants no consumer exception: it requires committed protection, a host verifier with a pass path, and all pinned inputs/anchors before its single ordered attempt.

Preserve accepted environment `192.168.1.30`, open/undeleted Conversations count for `[from,to)` including zero-call Conversations, and a dedicated enrolled Conversations service Party. Actual actor/role binding, independent signing/trust, worker enrollment, custody, and live qualification remain necessary. Historical host evidence preserves 5.1 only. 5.3 consumes no Provider seam; catalog migration non-conformance remains separate. Recorder, Instruction-protection, legacy-plaintext, host/protection/topology gates cannot be resolved by local defaults.

## Approved MVP scope amendment — 2026-10-28

The later owner instruction **apply recommended** adopts [sprint-change-proposal-2026-10-28-mvp-custody](../planning-artifacts/sprint-change-proposal-2026-10-28-mvp-custody.md) for current work priority: reuse installed operational OpenBao/inventory/HMAC and retain ordinary identity, authorization, revocation, replay and durable security-audit work; defer Luna/independent-authority/WORM provisioning and custom custody-platform construction. The full-delivery requirements above remain requirements for deferred features. This is a bounded planning/development milestone, not live readiness or full 5.4 completion. Current Human/approver evidence still requires custody; history, protected content, export and destruction remain blocked where dependencies are unavailable. FR-34, full EXT-* records/commands/status, RQ-1, retention and OQ-5.4-1 remain intact.
