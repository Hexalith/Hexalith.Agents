---
id: SPEC-story-5-4-dependency-unblock
status: proposal
identity_branch_for_implementation: B
created: 2026-09-27
companions:
  - parties-identity-contract.md
  - owner-implementation-plan.md
  - evidence-and-acceptance.md
  - ../../implementation-artifacts/spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md
  - ../../planning-artifacts/external-dependency-register.md
  - ../../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
sources:
  - ../../implementation-artifacts/story-5-4-dependency-recheck.md
---

# Story 5.4 dependency implementation contract

## Why

Story 5.4 must prove current principal, tenant, Party and approver authority, signed ingress, replay admission and durable denial recovery. Its four external records are still Uncommitted. This proposal gives their owners implementation work for the complete contracts, starting with Parties; it preserves the full story in draft. The adopted register and architecture remain authoritative, including all host/secrets extensions and operation gates.

## Capabilities

- **CAP-1**
  - **intent:** Provision exactly one immutable Party identity for each tenant's `hexa`.
  - **success:** Only the Agents Service Principal can provision it; exact retries preserve identity, divergent requests conflict, tenant roles cannot create or replace it, and results contain no Party PII.
- **CAP-2**
  - **intent:** Resolve a Party by its exact tenant and ID with authoritative current evidence.
  - **success:** Classification, liveness, evidence position and observed instant are mandatory; missing, ambiguous, stale, denied and unavailable evidence fail closed without disclosing another tenant.
- **CAP-3**
  - **intent:** Attribute human actions to the same stable actor both now and historically.
  - **success:** Versioned bindings reproduce the identity at an action, reject overlaps and mismatches, preserve identity across role changes, and prevent non-human Parties from becoming Approvers.
- **CAP-4**
  - **intent:** Supply the complete Conversations membership, posting, authority, denominator, content and deletion-delivery contract.
  - **success:** All six EXT-CONV-AI-1 seams pass the accepted exact-target compatibility command; a read-only subset cannot establish availability.
- **CAP-5**
  - **intent:** Supply complete platform secret custody and verification authority.
  - **success:** Every EXT-SECRETS-1 custody, replay, export, decision-trust, migration and destruction-signing obligation, including v23, has passing required evidence with no secret leakage.
- **CAP-6**
  - **intent:** Run Agents through the complete Platform-owned composition.
  - **success:** EXT-HOST-1 proves replicated durable denial capture, restricted capabilities, production protection attestation, migration and governance/destruction protocols, including v21/v23 and persisted recovery outcomes.
- **CAP-7**
  - **intent:** Enter and complete the full Story 5.4 on accepted dependencies.
  - **success:** All four complete records meet the register's readiness gate before ready-for-dev; every consuming seam execution additionally requires Available and passing evidence for the exact installed target.

## Constraints

- **Branch B is the user-selected implementation path.** Use the existing Organization-typed provisioned Party and verify its immutable ID. Parties Maintainer and formal Product acceptance of the complete launch contract remain separate register gates; this direction supplies neither acceptance nor availability.
- Every provisioned Party is verified by immutable ID. AI type is an additional check only under accepted Branch A; Organization type alone never establishes Agent identity under Branch B.
- Preserve existing Party IDs, source history, full Story 5.4 scope, Story 5.3 catalog/enablement/outcome behavior, and draft/backlog status. No replacement Party, second Agent, or local-only completion substitutes for the story.
- Domain state uses EventStore; shared persistence/authorization plumbing belongs in the technical platform. Platform owns hosting, custody and the operational spool. Agents owns its pure replay/security aggregates.
- Current authority is distinct from historical attribution. JWT roles, unsigned flags, nullable freshness, projection timestamps alone, in-memory bindings and mocks confer no live authority.
- Owner-accepted targets, dates and executable commands remain TBD. Proposed APIs, work packages and script paths below are implementation proposals, not accepted external commitments.
- Committed permits contract/package work. Only that record owner's accepted exact-target compatibility command may execute its Committed seam to establish Available. No consuming runtime/test/qualification path gains an exception.
- Missing availability returns `DependencyNotAvailable` naming the record. Changed contract/target invalidates acceptance; failed compatibility blocks use. Required Levels 4 and 5 remain unchanged.

## Non-goals

- Implement or deploy production code in this planning run; execute unavailable seams; post owner messages; select Product outcomes; declare dependencies or Story 5.4 complete.
- Add the optional Conversations retraction or UI contracts to the six core seams, or grant Approver eligibility to the Agent's Party.

## Success signal

Owners can implement the ordered work packages and Parties acceptance matrix without discovering the contract again. Actual Story 5.4 readiness occurs only after the register accepts all four complete commitments; live proof follows only after availability. This document supplies no such acceptance or runtime evidence.

## Assumptions

- Branch B is the smallest implementation path because the current public Party enum and Agents provisioning already support Organization. The user selected it after reviewing the recommendation; this does not establish the user's formal Product role or the maintainer's acceptance.
- New API names, half-open history intervals and a dedicated provisioning command are proposed designs requiring maintainer contract acceptance.

## Open Questions

- Product and Parties Maintainer: formally accept the user-selected Branch B contract and the same-ID legacy reconciliation policy described in the Parties companion.
- Parties and Platform maintainers: accept stable actor authority, login-rotation continuity, binding history/retention, freshness proof and final wire members.
- Platform Maintainer: select the secrets owner repository, numeric signing/recovery profile, spool deployment and complete custody/host targets.
- Relevant Product/Governance/Security owners: resolve only the open outcomes required by selected governance/identity branches; no implementation defaults them.
- Each record owner: accept every commitment field and its complete executable compatibility command as detailed in evidence-and-acceptance.md.
