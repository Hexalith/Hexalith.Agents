# Security And Data-Integrity Review — 2026-09-12 v2

## Verdict

**CHANGES REQUIRED.** The spine has unusually strong fail-closed, deterministic-id, protected-content, and at-least-once-effect rules, but it is not yet a safe implementation contract. One content-safety rule directly weakens the binding Product/Security requirement, and the legal-hold/deletion protocol cannot deliver its promised all-or-nothing outcome across separate streams. Three further high-severity gaps leave separation of duties, trusted-envelope integrity, and export-copy custody open to incompatible implementations.

Finding count: **2 Critical, 3 High, 0 Medium, 0 Low**.

## Scope And Method

This lens reviewed the initiative-level `ARCHITECTURE-SPINE.md` for authorization and tenant isolation, secret and key lifecycle, content protection, retention, deletion and legal hold, idempotency and replay, at-least-once side effects, audit/export integrity, fail-closed behavior, and data ownership. It checked the binding PRD, implementation convention, launch-readiness register, and external-dependency register where those sources sharpen a seam. The target spine and existing artifacts were not modified.

A finding is reported only where literal compliance can produce an unsafe result or two materially incompatible implementations. Already-declared assumptions are not re-labelled as hidden defects; they are summarized under Lower-Tier Disposition.

## Critical

### C1 — AD-20 explicitly permits retries and later decisions to bypass the then-current safety policy

**Evidence**

- AD-20 says a Provider transport retry “never re-evaluates safety.” It also directs regeneration, approval, and pre-post to use the current policy pair only when it is provably at least as restrictive as the snapshot pair, and to use the snapshot pair alone otherwise (`ARCHITECTURE-SPINE.md:292-296`).
- The binding PRD requires every retry, approval-time check, and pre-post check to pass **every applicable version**: the attempt snapshot and the then-current active policy, with any mode-specific policy added rather than substituted (`prd.md:672-684`). FR-27 repeats the then-current-policy requirement for the exact version approved and before posting (`prd.md:696-708`).
- The normative readiness authority promises a “no-weaker retry” and invalidates safety evidence on policy changes (`launch-readiness-register.md:105`).

**Failure scenario**

A policy becomes stricter in one category but looser or incomparable in another after an attempt is prepared. An implementation following AD-20 uses only the snapshot on transport retry and, because the pairs are not ordered, uses only the snapshot at approval/pre-post. Content newly forbidden by the active policy can reach the Provider or Conversation. A second implementation following the PRD evaluates both and blocks. Both cannot satisfy the claimed architecture/source contract.

**Impact**

The fail-closed safety boundary is weakened on precisely the replay/retry path the rule says it protects, and Audit Evidence can attest to fewer policy evaluations than Product and Security require.

**Action classification: AUTOFIX BEFORE IMPLEMENTATION.** Replace the selection rule with a conjunctive rule: retry, regeneration, approval, and pre-post must pass both the attempt snapshot pair and the then-current active pair, plus any applicable stricter mode policy. A single evaluation may replace two only when the architecture binds and tests a formally equivalent dominance optimization. Reconcile AD-13's fixed attempt fingerprint so a policy change has one deterministic outcome: no changed transport request under the same attempt id, and no continuation until the required current-policy result is durably recorded.

### C2 — Legal hold and class deletion have no exclusion protocol capable of delivering “rejected in full”

**Evidence**

- AD-2 assigns `LegalHold`, `ProtectedDeletion`, and each `AgentInteraction` to separate EventStore aggregate streams and says unrecoverable decisions read owning-aggregate state at an expected revision (`ARCHITECTURE-SPINE.md:140-144`).
- AD-22 makes hold application a multi-interaction two-phase operation, makes deletion check each interaction's pinned-hold set, makes `DestroyDek` irreversible, and nevertheless promises that a class deletion is “rejected in full if any is pinned” (`ARCHITECTURE-SPINE.md:304-308`).
- The normative command-step convention allows at most one trusted command for one durable step; it defines no multi-stream transaction, common fence, prepare/commit barrier, or lock shared by hold and deletion (`IMPLEMENTATION-CONVENTIONS.md:7-17`).

**Failure scenario**

Hold H and class deletion D both resolve interactions A and B while neither is pinned. D destroys A's DEK. H pins B. D then finds B pinned and must reject “in full,” although A is already irreversibly erased. H cannot become Active because A can no longer be pinned. Per-interaction expected revisions serialize local writes but cannot make the resolved set atomic.

**Impact**

The system can violate both legal preservation and its deletion-result contract while every component follows the literal rules. Recovery cannot undo the key destruction.

**Action classification: DISCUSS POLICY, THEN BIND A PROTOCOL.** If all-or-nothing deletion is required, introduce a durable shared exclusion/fencing protocol: freeze the interaction set at a checkpoint, acquire a monotonic hold/deletion intent on every member, reject and release all intents if any prepare fails, and permit key destruction only after the complete prepare set is committed. Hold requests must contend on the same fence, and failure-injection tests must prove that no `DestroyDek` occurs before prepare completion. If partial deletion is acceptable, remove “rejected in full” and define visible partial outcomes, retry, and legal notification semantics.

## High

### H1 — The principal model discards the human identity needed to enforce separation of duties

**Evidence**

- AD-7 states that `Administrator` and `Platform` principals carry no `PartyId` (`ARCHITECTURE-SPINE.md:180-184`).
- AD-22 computes a subject set of specific people, forbids the Inspector or a subject-set member from serving as second party, and permits a Platform Operator or second Compliance Inspector to approve governed content access or hold release (`ARCHITECTURE-SPINE.md:304-308`). The PRD requires the same person-level separation (`prd.md:509-518`, `prd.md:654-663`).
- AD-30 represents a `Platform` approver with `ActorTenantId = system`, grants that principal the second-party `LegalHoldRelease` path, and supplies no stable authenticated-human identifier (`ARCHITECTURE-SPINE.md:358-364`). The audit convention persists only that AD-30 principal as actor (`ARCHITECTURE-SPINE.md:542`).

**Failure scenario**

The same human acts first as `User(PartyId=P)`/Compliance Inspector and then through a Platform Operator route. The aggregate receives a `Platform` principal with no identity comparable to P. One compliant implementation treats the different principal kind as a different person and accepts self-approval; another fails closed because distinctness is unprovable and makes the Platform-approval branch unusable.

**Impact**

Inspection, export, and legal-hold separation of duties can be bypassed, while configuration and governance audit records cannot reliably answer which administrator acted.

**Action classification: AUTOFIX.** Carry a stable, tenant-safe authenticated actor identifier on every human-originated principal, independent of authorization role and target tenant. Compare that identity with requester, Inspector, subject set, and prior approvers. Give non-human platform automation a distinct principal kind and explicitly decide whether it may ever satisfy a human second-party requirement.

### H2 — The trusted-envelope HMAC authenticates an unspecified object and has no replay or key-lifecycle contract

**Evidence**

- AD-30 says every reserved extension has an HMAC tag issued through `EXT-SECRETS-1`, and that extensions are restricted to command sets and target scopes, but it never defines the MAC input, audience, issuance/expiry, nonce or replay treatment, key identifier/version, rotation overlap, revocation, or compromised-key response (`ARCHITECTURE-SPINE.md:358-362`).
- AD-29 gives exact canonicalization, HMAC-key versioning, and rotation-safe comparison for sensitive payload fingerprints, showing that these details are architecture-level where replay and integrity depend on them (`ARCHITECTURE-SPINE.md:352-356`). No equivalent rule exists for the authorization tag.
- `EXT-SECRETS-1` promises only generic resolution/rotation/denial/leak evidence for Provider and export/deletion operations; its repository, target, date, and verification command are all `TBD`, and it does not bind trusted-envelope signing or verification (`external-dependency-register.md:163-175`).

**Failure scenario**

One Server implementation MACs only the serialized `actor:*` extension. A captured valid extension can then be cut and pasted onto another payload within its allowed family and target scope. Another implementation MACs the entire canonical command envelope and rejects that substitution. Both satisfy the current words. Rotation creates a second divergence: immediate old-key rejection can strand in-flight workflow commands, while indefinite acceptance leaves a compromised authorization key usable forever.

**Impact**

The control intended to stop forged reserved extensions does not have a common integrity or freshness guarantee. Authorization decisions and audit attribution can be substituted or replayed across asynchronous boundaries.

**Action classification: AUTOFIX.** Define one canonical MAC input including at least principal kind and authenticated actor id, operation family and concrete command type, target tenant and resource identity, command/idempotency identity, payload fingerprint, audience, issued/expiry instants, and key version. Bind replay behavior, constant-time verification, allowed clock skew, rotation overlap, emergency revocation, and fail-closed outcomes. Expand `EXT-SECRETS-1` and `LR-TENANT-ACCESS` verification to exercise substitution, replay, old-key, wrong-audience, wrong-tenant, and compromised-key cases.

### H3 — Export manifests are specified, but the sensitive export bytes have no owner or deletion/hold lifecycle

**Evidence**

- AD-1 claims Agents ownership of durable export and deletion state (`ARCHITECTURE-SPINE.md:134-138`).
- AD-22 creates an encrypted, time-limited export, hashes each item's exported bytes, seals a manifest reference, and delivers the export key externally; however, it does not name the store/owner for the exported bytes, an exact expiry duration and deletion acknowledgement, immutability/conditional-write semantics, how a later hold affects the package, or how protected deletion finds and purges packages that contain an interaction (`ARCHITECTURE-SPINE.md:304-310`). It also names a `signature reference` without binding canonical manifest serialization, signing algorithm, verification key/version, verifier, or trust anchor.
- The readiness register's deletion inventory includes the `export` **projection**, but does not include the export object/package holding the bytes (`launch-readiness-register.md:265-291`). `EXT-SECRETS-1` likewise has no export-object custody or signing contract (`external-dependency-register.md:163-175`).
- The PRD requires export to be tenant-scoped, encrypted, time-limited, manifested, and audited, and deletion to purge affected copies (`prd.md:944-960`, `prd.md:975-977`).

**Failure scenario**

Implementation A stores encrypted packages in a private blob container and deletes them when its locally selected expiry elapses. Implementation B streams or retains packages through the `export` projection lifecycle and deletes only projection metadata when a protected deletion completes. Both can produce the required manifest and expiring envelope key. In B, a previously delivered key or copied key material can keep an Agents-owned exported duplicate readable after deletion is reported complete. Separately, two manifest producers can serialize/sign different byte representations while reporting the same logical fields.

**Impact**

Deletion can make a false restrictive-completion claim, legal hold can preserve the source but not the exported copy (or vice versa), and the manifest's signature is not interoperably verifiable. Tenant ownership and incident-response responsibility for the most concentrated sensitive-content artifact are unclear.

**Action classification: DISCUSS STORAGE POLICY, THEN BIND IT.** Name the export artifact owner and storage boundary; bind tenant/resource keys, immutable creation, authenticated encryption, exact expiry authority, physical purge acknowledgement, restore behavior, and lookup by every contained interaction. Add the artifact store—not only the `export` projection—to protected-deletion completion and define whether an active hold extends its lifetime. Specify canonical manifest bytes, signature algorithm, signing-key version/lifecycle, verification procedure, and durable linkage between manifest, exact exported checkpoint, package digest, and deletion status.

## Lower-Tier Disposition

- **Provider and posting at-least-once effects:** No additional finding. AD-5, AD-13, AD-23, AD-24, and AD-29 consistently bind deterministic attempt/message identities, outcome lookup before retry, no retry on indeterminate Provider usage, fence invalidation after restore, and authoritative expected-revision conflicts (`ARCHITECTURE-SPINE.md:164-172`, `240-246`, `312-322`, `352-356`). Delivery still cannot be launch evidence until the named external seams are committed and tested.
- **General tenant isolation and fail-closed behavior:** No additional finding beyond H1/H2. AD-2, AD-12, AD-17, and AD-30 consistently demand tenant-keyed state, absent-key equivalence, fresh role/dependency checks, safe blockers, and denial before side effects (`ARCHITECTURE-SPINE.md:140-150`, `228-238`, `272-278`, `358-364`).
- **Known unresolved launch blockers:** `ARCH-A-9` (SecurityEventLog burst volume), `ARCH-A-10` (Conversations-owned posted-copy retention/deletion), and `ARCH-A-12` (DigestKey version custody/rotation) remain explicit and therefore are not hidden findings. AD-17 makes every unretired `ARCH-A-*` assumption an `RQ-1` blocker (`ARCHITECTURE-SPINE.md:272-278`, `846-860`). They must remain NOT READY until their named owners retire or explicitly accept them through the recorded process.
- **Protection implementation status:** The design correctly blocks content-bearing paths when protection is unavailable, but `EXT-PROTECTION-1`, `EXT-SECRETS-1`, and the corresponding live tests remain uncommitted/deferred (`external-dependency-register.md:163-175`, `193-202`; `launch-readiness-register.md:200-205`). This is visible delivery debt rather than a separate architecture contradiction.

## Required Closure Order

1. Correct AD-20 to the PRD's conjunctive current-plus-snapshot safety rule.
2. Decide and bind the legal-hold/deletion exclusion protocol before any irreversible key-destruction path exists.
3. Preserve authenticated human identity end to end for governed approvals.
4. Define the trusted-envelope MAC and key lifecycle as an executable `EXT-SECRETS-1` contract.
5. Name and govern export-package storage, signature verification, hold behavior, and deletion completion.
