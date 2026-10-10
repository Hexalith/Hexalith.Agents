# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Deliver live tenant-scoped `hexa` setup with shared authoritative configuration, lifecycle, readiness, and callability. Preserve history; uncertainty blocks effects.

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

- Platform provisions one `hexa` per tenant, create-only under Agents Service Principal. Exact retries preserve identity; divergence conflicts. Tenant administrators configure/activate/disable/inspect; second-Agent creation, deletion and Party replacement are forbidden.
- Selected Branch B preserves the Organization Party verified by immutable tenant-scoped ID. Complete launch-contract acceptance remains required; Uncommitted development supplies no launch evidence.
- Require current Party classification/liveness, position/time, stable non-PII human actor identity and versioned current/action-time binding. Missing/stale/ambiguous/overlapping/mismatched evidence blocks; non-human Parties cannot approve.
- Approvers require current Source Conversation participation/read access and the configured policy basis; separation compares stable human actors across principal kinds.
- Changes require expected revisions, deterministic duplicates and persisted projections. Active never proves callable; revocation blocks. Authorize before lookup/disclosure/mutation; cross-tenant responses reveal no existence or data.
- Provider catalog belongs to `system`; tenant enablement, selection, and versioned data-handling acceptance remain separate. Configuration applies prospectively; migration preserves history. Expose secret references/configured state only.
- Retired Caller/safety-override/prohibited-cost values remain deserializable but cannot mutate/authorize. Unknown values block.
- Human-binding history retains the accepted 365 fixed days from binding-effective-at, exclusive expiry. Custody/destruction/restore/successor continuity still need qualification. Interaction-content retention instead starts at terminal state.

## Technical Decisions

- Pure EventStore aggregates own state; Server/Dapr Workflow coordinate effects. Hexalith.Builds owns versions; Debug uses project references, Release public packages. Platform owns hosting.
- Select one current operation-specific User/Administrator/Platform/Workflow principal; Platform humans require Tenants `global-administrators`. History grants no authority. Strip reserved client extensions; verify scope-bound canonical HMAC before dispatch.
- Accepted envelope settings: maximum lifetime 300 seconds, future tolerance 30 seconds, healthy rotation overlap 600 seconds, automatic recovery horizon 86,400 seconds, first-seen replay retention 604,800 seconds. Expiry is exclusive without grace; revocation blocks immediately. Issuer/audience/versioned trust and qualified custody remain required.
- Separate logical command identity/delivery nonce. Private replay registration uses reserved `system` issuer/nonce identity before target construction. Exact replay reuses stored times; changed fields/cross-tenant nonce reuse deny and audit.
- Restricted replay/security recording precedes commands. Route from authenticated authority; spool denials before reporting processed; drain exactly once with durable acknowledgement. Replay/spool failure blocks target construction. Platform owns replicated spool, custody, worker, and restricted credentials.
- Matrix-v7 readiness uses one registry checkpoint, current producer/scope-valid observations, typed blockers and newest-record-wins without fallback. Preserve callable Degraded Providers. Recorders/custodians cannot mint decision approvals.
- Platform protection seals sensitive fields before infrastructure; workflows carry references. Exact-target canaries prove persisted ciphertext, unseal, destruction, and Erased replay. No-op protection, uncertain legacy plaintext, or unresolved Instruction classification blocks applicable work.
- `/api/v1/agents/...` exposes safe domain contracts.

## UX & Interaction Patterns

Use FrontComposer/Fluent V5. Separate lifecycle, suspension, readiness, and callability. Writes show Submitted, AuthoritativePending, then ProjectionConfirmed. Identity is inspect-only. Approver sources: Facilitator, predefined human Party, tenant role. Hide unauthorized controls; preserve English/French parity, WCAG 2.2 AA, and 320-pixel operability.

## Cross-Story Dependencies

5.1 provides the boundary; 5.2/5.3 independently provide Agent/catalog operations; 5.4 authority/security; 5.5 shared readiness; 5.6 composition; 5.7 prior-setup activation. 5.8 protects later content; 5.9/5.10 tighten foundations. No later-epic dependency.

The register alone establishes commitments. Ready-for-dev requires complete accepted Committed/Available records without TBD fields. Only an owner's accepted exact-target compatibility command may execute a Committed seam to establish Available; consumers otherwise return DependencyNotAvailable(record). Available requires live Level 4; required Level 5 completes in qualification/RQ-1. Changed contracts invalidate acceptance; failed compatibility blocks use. Fixtures supply no live attainment. Historical host evidence preserves 5.1 completion only.

Full 5.4 is specification-approved and `ready-for-dev`, consuming completed Stories 5.1 and 5.2. The register accepted EXT-CONV-AI-1, EXT-HOST-1, EXT-PARTIES-1 and EXT-SECRETS-1 as Committed on 2026-10-10 with immutable owner targets, integration date 2026-10-08 and pinned complete verification commands. None is Available; their Live/Full commands have not passed against the exact targets. Contract/package work may begin, while consuming live runtime, test and qualification paths remain blocked. Complete owner engineering remains in progress. Preserve full Conversations/custody/host/shared EventStore scope; partial qualification cannot advance a broader record.

Accepted inputs: environment 192.168.1.30; open, undeleted Conversations created in [from,to), including zero calls, as denominator; a dedicated Conversations service Party requiring enrollment/current machine binding, distinct from hexa/humans. Jérôme Piquot approved Product/Governance/Security/Architecture authority assignments. Actual actor/role binding, independent signer/trust, worker enrollment and live qualification remain unestablished.

5.3 invokes no Provider seam; its accepted historical Branch B non-consumption ruling and open platform-catalog migration non-conformance remain distinct. Host/protection/topology gates and conditional recorder, Instruction-protection, and legacy-plaintext decisions remain; no local default resolves them.
