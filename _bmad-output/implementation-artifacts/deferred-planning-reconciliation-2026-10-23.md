# Deferred planning reconciliation — 2026-10-23

This record resolves five decision gaps raised by `spec-synchronize-second-2026-09-09-prd-downstream-planning.md`. It records contracts already present in the governing documents and one narrow Experience Spine correction. It supplies no runtime implementation, Security approval, owner acceptance, or production qualification evidence.

The source baseline is `ece9cdbc2d4648b3c629e794197bc8a2194852ef` on `main`. Line references below identify that revision; named sections and relative links identify the maintained documents. The five original ledger summaries and evidence statements are preserved.

## Platform Operator authority

The [Architecture Spine, AD-30 — Principals And Trusted Envelope](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (baseline lines 586 and 602) explicitly requires a fresh read of Hexalith.Tenants' non-tenant-scoped `global-administrators` authority for `Platform`. Tenant `User` and `Administrator` roles read the distinct tenant-scoped `tenants` authority. `ActorTenantId = system` identifies the Platform principal; it does not select a tenant-scoped role source. Token-embedded or cached roles cannot replace the fresh read, and missing, stale, revoked or ambiguous evidence fails closed.

This settles the ledger's source ambiguity. Live authority adapters and their qualification remain delivery work.

## Versioned DigestKey rotation and concurrency

The [Architecture Spine, AD-22 — Sensitive Audit Governance and AD-29 — Deterministic Identity Derivation](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (baseline lines 506 and 574) specifies versioned tenant digest keys. `EXT-SECRETS-1` retains an originating key version while any digest under it survives. Rotation creates a current version for future digests without rewriting retained or held records. Every persisted sensitive-content digest carries its `DigestKeyVersion`; re-verification and command idempotency compare using that recorded version, so a concurrent rotation cannot turn an otherwise exact retry into a conflict. Ephemeral verdict-cache digests are discarded before a rotation boundary.

Rotation is Platform Operator-initiated, audited, content-free, and outside the browser advisory-lock set. It does not require the absence of retained or legally held content. This closes the request to define the policy, including comparison across a rotation; it does not approve that policy. **ARCH-A-12 remains an Architecture/Security assumption requiring Security confirmation or a superseding key-lifecycle procedure before `RQ-1` evaluation** (baseline line 1401). No assumption or readiness record changes here.

## Overlapping Agent and tenant pauses

The [PRD, FR-18](../planning-artifacts/prds/prd-agents-2026-06-23/prd.md) (baseline line 471), [Architecture Spine, AD-12](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (line 390), and [Experience Spine, Proposal editor action rail](../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#proposal-editor-action-rail) (line 340) agree: `PausedDuration` is the union of wall-clock intervals in which the Agent is `Disabled` or the tenant is `Suspended`. Overlap counts once, and the retry clock resumes only after both conditions clear. The retry deadline remains origin plus window plus `PausedDuration`; awaiting proposals' `ExpiresAt` continues to run.

This settles duration accounting without changing the three-attempt, 15-minute retry bound or establishing runtime timer behavior.

## Retry removal/block transitions and existence lookup

The [PRD, FR-2 and FR-18](../planning-artifacts/prds/prd-agents-2026-06-23/prd.md) (baseline lines 163, 452 and 470–472) and [Architecture Spine, AD-5 and AD-7](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (lines 295 and 332) distinguish removal or an Agents-owned block from other pre-post failures: removal/block gives `Abandoned(RemovedInConversations)`; other re-validation failures retain their typed `PostingFailed` outcome. `PostingPending` remains uninterruptible.

The [Experience Spine, Proposal editor action rail](../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#proposal-editor-action-rail) already lists the removal/block abandonment exit (baseline line 346). This reconciliation completes its lookup paragraph (line 344) using the existing PRD/AD-5 contract:

- Skip lookup when no post was ever attempted, including `Approved` and pre-post-only `PostingFailed`.
- When lookup is required, a positively found message gives `Posted` with `LateConfirmed` and refuses the requested exit.
- Typed absence permits retry or abandonment; typed `ConversationDeleted` or `PrincipalRemovedFromConversation` counts as no posted message for abandonment only and never authorizes retry.
- Unavailable lookup refuses the exit and preserves `PostingFailed`. A copy that may remain in an unreadable Conversation stays governed by Conversations retention.

The retry verdict requires lookup availability only when lookup is required; administrative confirmations display the lookup result or explicitly indicate that no post was attempted and lookup was skipped. The automatic posting summary inherits these branches without an obsolete outcome count.

The correction neither grants a browser posting capability nor asserts that the UI/workflow implementation meets these transitions.

## Administrative retry and browser locking

The [Architecture Spine, AD-5 operation-family classification and AD-12](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (baseline lines 299 and 400) and [Experience Spine, Proposal editor action rail and confirmation contents](../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#proposal-editor-action-rail) (lines 338, 340, 377 and 493) already distinguish the public request from its workflow steps. Administrative retry enters through lock-bearing `ProposalResolution`, with its own authorization and audit evidence. Accepted lookup, re-validation and posting steps declare workflow-only `ConversationPosting` and use aggregate expected revisions and deterministic idempotency. `ConversationPosting` owns no browser advisory lock; the closed lock-bearing set remains ten families.

This settles the UX classification gap without changing lock families or qualifying posting authorization.

## Verification scope

All five closures are planning reconciliation, with original deferral history retained. The comparison checked the governing clauses in context, the PRD/architecture lookup exceptions, and the retained ARCH-A-12 condition. Document/link checks and independent review are recorded in [the build spec](spec-five-deferred-planning-reconciliation-items.md). No application tests were run for this documentation-only change. Other deferred entries, dependency acceptance records, sprint status, source code and submodule revisions remain unchanged.
