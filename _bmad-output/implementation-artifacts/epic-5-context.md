# Epic 5 Context: Live Governed Setup And Honest Readiness

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Enable administrators to configure tenant-scoped `hexa` through live public operations and receive an authoritative explanation of lifecycle, setup validity, Provider state and callability. Preserve history and refuse effects whenever current authority, protection or dependency evidence is uncertain.

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

- Platform provisions exactly one `hexa` per tenant through create-only orchestration under the Agents Service Principal. Exact retries preserve identity; divergence conflicts. Administrators configure, activate, disable and inspect; second-Agent creation, deletion and Party replacement remain forbidden.
- Selected Branch B preserves an Organization Party by immutable tenant-scoped ID. Require current classification/liveness and trustworthy observation evidence. Organization identity grants no human or approver authority. Human actions require stable non-PII actor identity and current unique binding; stale, ambiguous, overlapping, mismatched or revoked evidence blocks. Approvers also need current Source Conversation access and configured-source eligibility. Separation compares stable actors across principal kinds.
- Authorize before protected lookup, disclosure or mutation. Foreign-tenant outcomes expose no existence, count or side effect. Expected revisions, idempotency, replay and persisted projections preserve truth. Lifecycle Active never establishes callability.
- Platform Provider catalog and tenant enablement are separate. Selection, pricing, capabilities and data-handling acceptance are versioned; changes apply prospectively and migration preserves history. Expose secret references/configured state only. Historical Caller, prohibited-cost and safety-override values remain deserializable but cannot authorize mutations; unknown values block.
- Human-binding history retains 365 fixed days from binding-effective-at with exclusive expiry. Retries, erasure, rebind and restore cannot extend deadlines. Interaction-content retention starts at terminal state; existing holds and export restrictions remain enforced.
- The approved 2026-10-28 MVP amendment prioritizes ordinary identity and operational security using installed OpenBao, inventory and HMAC services. Luna HSM, independent lifecycle-authority cluster, immutable-archive provisioning and custom custody-platform construction are deferred. This is a development milestone, supplying no reduced executable contract, availability claim or full Story 5.4 completion. Current Human/approver custody checks remain required. History, protected content, exports and erasure stay blocked wherever their full dependencies are unavailable. Software-key deletion cannot establish irreversible all-copy erasure or backup non-revival.

## Technical Decisions

- Pure EventStore aggregates own domain state; Server and Dapr Workflow coordinate effects. Hexalith.Builds owns package versions; local Debug uses project references, CI Release uses public packages. Platform owns hosting; domain modules carry no AppHost or ServiceDefaults.
- Select one operation-specific User, Administrator, Platform or Workflow principal from current authority. Platform humans require Tenants global-administrators evidence. Historical attribution grants no current authority. Strip client-reserved metadata and verify canonical, scope-bound HMAC before dispatch. Business approvals require an independent issuer; the key custodian cannot mint them.
- Preserve envelope lifetime/future tolerance/rotation overlap/recovery horizon/replay retention of 300/30/600/86400/604800 seconds. Expiry is exclusive and revocation immediate. Issuer, audience, accepted trust/profile and exact credential/key scopes remain mandatory.
- Logical command identity differs from delivery nonce. Private reserved-system issuer/nonce registration precedes target-command construction. Exact replay preserves first-seen facts; changed-field or foreign-scope reuse denies and audits. Denials must reach a durable operational spool before being reported processed, then drain once to acknowledged daily security events. Replay or spool failure prevents dispatch. Platform owns restricted credentials, worker and accepted replica/failure model.
- Matrix-v7 readiness uses one registry checkpoint, current producer/scope-valid observations, typed blockers and newest-record-wins without fallback. Preserve callable Degraded Providers. Public routes are versioned under `/api/v1/agents/...`.
- Protect sensitive content before infrastructure boundaries; workflows carry references. Persisted ciphertext, unseal, destruction and Erased replay need exact-target evidence. Missing/no-op protection, uncertain legacy plaintext and unresolved Instruction classification block applicable work. MVP scope changes do not waive this boundary.

## UX & Interaction Patterns

Use FrontComposer and Fluent UI Blazor V5 without a module theme. Distinguish lifecycle, suspension, readiness and callability; writes progress through Submitted, AuthoritativePending and ProjectionConfirmed. Identity is inspect-only. Approver sources are predefined human Party, tenant role and Conversation Facilitator. Render the same safe basis in API and UI, omit unauthorized controls, and preserve English/French parity, WCAG 2.2 AA and 320-pixel operability.

## Cross-Story Dependencies

5.1 establishes boundaries; 5.2/5.3 provide Agent/catalog operations; 5.4 establishes authority/security; 5.5 supplies shared readiness; 5.6 composes the Platform host; 5.7 gates activation. 5.8 protects content-bearing Epics 6–8; 5.9/5.10 tighten cost and retired-policy foundations. No story depends on a later story.

The external register alone establishes commitments. Complete accepted Committed/Available packets satisfy development entry. Committed permits contract/package work and only the accepted owner command against its exact target to establish availability. Other consuming runtime, test and qualification paths return `DependencyNotAvailable(record)` until Available with passing live Level 4 evidence; required Level 5 completes in consuming qualification/RQ-1. Changed contracts invalidate acceptance; fixtures cannot establish availability or advance a broader record from partial success.

Full Story 5.4 is approved and its entry dependencies are Committed; none of its four owner records is Available. Preserve complete Parties, Conversations, secrets and host contracts and accepted target/date/command pins. The bounded owner cohort preserves protection → secrets → host-refusing-consumers → mutual ordering and grants no consumer exception. MVP work retains current actor/role authority, independent decision issuance, authenticated ingress, replay and denial recovery; current profiles, maintenance preservation baseline, backup lifetime, privileged restore policy and recovery evidence still need actual owner delivery. Timestamped installations are not perpetual health. Preserve host `192.168.1.30`, Branch B and existing journals; AWS remains excluded.
