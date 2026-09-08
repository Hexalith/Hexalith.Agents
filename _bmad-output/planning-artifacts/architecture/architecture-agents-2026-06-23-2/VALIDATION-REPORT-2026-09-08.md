---
name: Hexalith Agents spine validation
type: architecture-spine-validation
target: ARCHITECTURE-SPINE.md (updated 2026-08-02, status final)
repository_state: main @ d7e9cda (2026-09-08)
date: 2026-09-08
lint: 0 findings
lenses:
  - rubric walker (good-spine checklist)
  - verified-current (configured)
  - adversarial divergence (configured)
  - brownfield drift (ad hoc)
  - security / compliance / data integrity (ad hoc)
---

# Spine Validation Report — 2026-09-08

## Gate verdict

**FAIL, fixable.** The 26 architecture decisions remain sound and no named technology has moved under them, but the spine has two critical holes that let a fully compliant build leak sensitive content and cross-tenant catalog state, and its picture of the repository is five weeks stale. Every critical and high finding is expressible as AD text; one product decision (catalog scope) must be taken first.

| Lens | Verdict | Critical | High | Medium | Low | Review file |
| --- | --- | --- | --- | --- | --- | --- |
| Rubric walker | PASS WITH FINDINGS | 0 | 3 | 7 | 6 | `reviews/review-2026-09-08-rubric.md` |
| Verified-current | PASS WITH FINDINGS | 0 | 0 | 5 | 8 | `reviews/review-2026-09-08-verified-current.md` |
| Adversarial divergence | FAIL | 1 | 9 | 7 | 3 | `reviews/review-2026-09-08-adversarial-divergence.md` |
| Brownfield drift | FAIL TO RATIFY | 0 | 4 | 8 | 6 | `reviews/review-2026-09-08-brownfield-drift.md` |
| Security / data integrity | FAIL | 2 | 5 | 6 | 2 | `reviews/review-2026-09-08-security-data-integrity.md` |

Deterministic lint (`lint_spine.py`): 0 findings. All four mermaid diagrams parse. No placeholders, no template comments, no duplicate AD ids.

## Critical — close before any Epic 6 activity contract is written

### C-1 Sensitive content can persist in Dapr Workflow history and Agent Framework session state, outside every protection and deletion rule
*Found independently by adversarial (A-1) and security (S-2).* AD-14 protects "events/projections", AD-22 erases EventStore payloads plus the AD-17 projection list, and the Data-planes convention calls workflow history "execution state only". Dapr persists every activity input and output verbatim. A context activity that returns the complete conversation and a generation activity that returns generated text are the natural shape and violate nothing, yet after a confirmed deletion the raw prompt, the whole customer conversation, and every generated version remain readable in the workflow state store indefinitely. Deletion reports complete and `LR-AUDIT-PROTECTION-DELETION` can pass.
**Fix:** new AD-27 (Execution-State Content Boundary): workflow inputs, activity inputs/outputs, timer and external-event payloads, and any Agent Framework session/checkpoint carry only identities, versions, digests, safe codes, and protected-content references; content is materialised inside an activity and discarded; completed instances are purged at terminal handling; `workflow-execution-state` joins the AD-17/AD-22 purge inventory. Disposition: autofix (Workflows and Activities folders are still empty, so the rule costs nothing now).

### C-2 `TenantId` is absent from every Agents identity, and the catalog's tenant scope contradicts itself across PRD, spine, story, and code
*Found by security (S-1), adversarial (A-2), rubric (R-9).* The Identity convention never lists `TenantId`; no deterministic id, idempotency key, projection key, or query is stated to carry it. AD-10 keys `CapabilityVersion` and readiness by the "global catalog key (`ProviderId`, `ModelId`)" and the PRD says "Global Providers Aggregate", while Story 5.3 and the shipped `ProviderCatalogAggregate` are tenant-scoped. Two compliant units can therefore build a global readiness projection over per-tenant streams: tenant B's version bump shows B's pricing and configured-secret state on tenant A's readiness surface, and A's next high-water read then sees a "decrease" and blocks every A interaction with `CapabilityVersionRegressed` forever.
**Fix:** decide the scope, then amend AD-2, AD-10, and the Identity convention. The reviewers split: rubric proposes a platform-scoped catalog under a reserved platform tenant; adversarial and security propose tenant-scoped (`TenantId`, `ProviderId`, `ModelId`), which is what the code and Story 5.3 already chose. Either way, `TenantId` becomes a mandatory component of every Agents identity and key, and a query outside the caller's tenant returns exactly the absent-key response. Disposition: discuss (one decision), then autofix.

## High — close before the consuming stories enter ready-for-dev

| # | Theme | Sources | Consuming stories | Disposition |
| --- | --- | --- | --- | --- |
| H-1 | **Aggregate inventory incomplete.** AD-2 names three aggregates; the spine itself introduces a budget ledger and `LaunchReadinessGate`, and Epic 8 stories already name `LegalHold`, `AuditExport`, `ProtectedDeletion`, `ContentSafetyPolicy`, `TenantBudgetPolicy` with no owner, key, or tenant scope. The `ContentSafetyPolicyVersion` in the AD-4 snapshot references a version nobody owns; an Agent-scoped and a tenant-scoped counter would share one field name. A hold accepted at revision n but not yet projected lets deletion erase held content. | R-1, A-10 | 6.4, 8.1–8.4 | discuss list, then autofix AD-2 |
| H-2 | **No principal model.** "Server-trusted command" has no defined carrier. Code uses server-populated `actor:agentsAdmin` / `actor:agentsProviderAdmin` envelope extensions; nothing bounds which commands a workflow principal may dispatch or who strips client-supplied reserved extensions. Epic 6 could borrow the admin extension for activity dispatch. | S-3, R-15 | 6.1+, 7.x | autofix (new AD-12a) |
| H-3 | **Three legal time authorities for one instant.** `ExpiresAt` may be computed from the EventStore commit timestamp, the workflow's `CurrentUtcDateTime`, or the server `TimeProvider`; the shipped aggregate and the shipped expiry orchestrator already pick differently. A timer fired before the stored deadline finds "not elapsed", is consumed, and the proposal never expires. NFR-9 latency likewise has no bound clock. | A-3, R-7, S-6 | 7.1, 7.6, 8.1, 8.5 | autofix (new AD-28 Time Authorities) |
| H-4 | **"Fresh" and "stale" have two definitions.** Validity-window freshness (AD-10/AD-17) and revision-lag freshness (shipped `ProviderCatalogViewFactory`, Story 5.2 read model) coexist with no mapping and no single evaluation clock; UI recomputes with the browser clock. | A-4 | 5.5, 6.7, 8.7 | autofix (discriminated `Freshness` value in AD-17) |
| H-5 | **Deterministic ids are named but not derived.** Code already has two `AttemptId` derivations (`attempt-{id}` vs SHA-256). A fingerprint-based derivation makes regeneration idempotently return the old output; `ProviderIdempotencyKey` vs `AttemptId` lookup mismatch re-invokes on recovery. | A-8, S-12 | 6.4, 7.3 | autofix (new AD-29 Deterministic Identity Derivation) |
| H-6 | **Readiness registry keys are under-specified.** `ObservationId` has no formula, so hourly re-observation is either a rejected conflict or unbounded replacement. `TenantScope` has no grammar, so platform-wide gates (topology, recovery, capacity) can never satisfy a tenant-scoped `AgentActivation`. `OperationGateMatrix` v1 has no family for edit, expiry, retention, hold release, export download while "missing family blocks"; AD-12 names six families where the register has thirteen. | A-5, A-6, A-7, B-8, A-18, R-13 | 5.5, 5.6, 5.7, 7.2, 7.6, 8.1 | autofix (AD-17 grammar, scope kinds, matrix v2) |
| H-7 | **"Policy at least as restrictive" has no order.** A version comparison is the only scalar available, and a legal future-only loosening then weakens a transient retry. | A-9, S-11 | 6.3, 8.4 | autofix (`RestrictivenessRank` in AD-20) |
| H-8 | **Budget reservations have no bounded reclamation.** A 48-hour Provider outage holds every maximum-cost reservation and refuses new calls at 100 percent; month-boundary attribution and orphan reservations have no owner. | S-4, A-11, A-12 | 6.4, 8.4 | autofix structure, discuss grace defaults |
| H-9 | **Payload-protection key ownership undefined.** With the default deployment-wide key ring, per-interaction cryptographic erasure is impossible and a restore revives "deleted" content; with one key per tenant, one expiry erases everything under hold. | S-5 | 8.1–8.3 | autofix (per-interaction DEK under tenant KEK via `EXT-SECRETS-1`) |
| H-10 | **Audit envelope silent.** No rule that every event carries the principal, `OnBehalfOfPartyId`, `CorrelationId` = `AgentInteractionId`, `CausationId`, `TenantId`; no clock authority for domain `ObservedAt`; export manifest has no integrity proof. | S-6, S-15 | 6.x, 8.2 | autofix |
| H-11 | **Agent disable does not stop in-flight work.** AD-4 says configuration changes affect future interactions only, so 40 pending proposals stay approvable for up to 30 days after Security disables the Agent; no platform emergency stop exists. | S-7 | 7.4 | autofix (AD-4/AD-12 amendment) |
| H-12 | **The sole V1 entry point has no AD.** The Conversations-owned **Call hexa** action is a reverse seam (Agents UI contributing into another module's surface); dependency direction is unbound (cycle vs FrontComposer extension slot vs deep link). SCP 2026-08-03 directed an `EXT-CONV-UI-1` record that was never created. | R-2, B-11 | 6.7 | discuss, then autofix (AD-15 extension + register record) |
| H-13 | **Spine has sunk to spec altitude.** AD-10, AD-13, AD-17, AD-23, AD-24, AD-26 restate the launch-readiness register verbatim (field lists, enums, 18 GateIds, 17 projection ids, thresholds, seven-step protocols) while AD-17 declares the register normative. The register changed on 2026-08-09; the spine did not. Two documents now claim authority for the same text. | R-3 | all | discuss scope, then autofix (keep invariants, point to register) |
| H-14 | **Live projection ids are outside the authoritative list.** The only two shipped projections are `agent-setup` and `provider-catalog`; none of the 17 AD-17 ids exists in code, and AD-22 deletion completeness is defined over that list. | B-3 | 5.2, 5.3, 5.5 | discuss (rename vs ratify) |
| H-15 | **Story 5.3 executed against an `Uncommitted` dependency.** The spine and register bind 5.3 to `EXT-PROVIDER-1`; 5.3 is done, deliberately adapter-free. Either the consumer list is over-broad or the gate was bypassed. | B-4 | 5.3, 5.5 | discuss (narrow consumers to 5.5, 6.4, 7.3) |
| H-16 | **Stale reality inside rules.** AD-16 and the seed still say AppHost/Aspire/ServiceDefaults are present (removed by Story 5.1 on 2026-08-04, now guarded by tests); "all seven EXT records Uncommitted" is false (`EXT-HOST-1` Committed since 2026-08-09, Hexalith.Platform `a66cdf34`, target 2026-09-30); the AD-10 gap note is half-resolved by 5.3. | B-1, B-2, B-5, R-4, V-1, V-2 | 5.6, 5.7 | autofix (strip status notes, refresh) |

## Medium and low — the tail

Rolled up here; full text in each review file.

**Rubric (R-5..R-16):** seed places aggregates under `Server/Aggregates` while the conventions companion and the tree put them in the `Hexalith.Agents` domain assembly (R-5); the deviation from the platform vertical-slice layout is ratified only in the memlog and the platform instruction file is not in `sources` (R-6); NFR-9 and SM metrics have no bound clock (R-7); the sequence diagram releases the capacity lease before the outcome is recorded and omits membership on the approval path (R-8); AD-8's "unless Conversations adds an owner resolver" is an unenforceable escape hatch (R-10); frontmatter `binds: hexa`, AD title change-log tags (R-11); paradigm paragraph duplicates AD-18/19 (R-12); versioning mechanism only "additive-first" (R-14); OQ-4 notification posture decided in memlog but absent (R-16).

**Verified-current (V-3..V-13):** Aspire row cites AppHost declarations that no longer exist, catalog now 13.5.3 (V-3); xUnit root override 3.2.2 silently diverges from catalog 4.0.0, a breaking MTP v2 release (V-4); SDK 10.0.301 vs siblings on 10.0.400 (V-5); all five sibling commits stale (V-6); CommunityToolkit Dapr, OpenTelemetry 1.18.0, Fluent UI rc.5 with no GA, NSubstitute 6.2.0 catalog drift (V-7..V-10); Dapr 1.18.x instance-id reuse breaking change worth a note (V-11); Microsoft Agent Framework GA 1.20.0 with a Durable-Task durable story, which reinforces AD-18 and could be named as an excluded owner (V-12); seed names `IntegrationTests`, which does not exist (V-13). Verified with no drift: Dapr Workflow stable, CommunityToolkit still the Aspire Dapr choice, Dapr Agents Python-only, Conversation API alpha, all seven source URLs resolve, MediatR/FluentValidation/Shouldly unchanged.

**Adversarial (A-11..A-20):** budget period identity unbound (A-11); no owner releases a reservation when an attempt terminalises before step 6 (A-12); public error contract and HTTP mapping unbound so cross-tenant indistinguishability is per-story (A-13); command idempotency semantics backed by no AD (A-14); "versioned and additive-first" has no applicable rule (A-15); fingerprint field inventory unspecified (A-16); Identity table says Conversations owns `MessageId`, AD-13 says Agents derives it (A-17); two operation-family vocabularies (A-18); Agent version semantics and snapshot source of `ProviderCapabilityVersion` (A-19); naming drift (A-20).

**Brownfield (B-6..B-18):** `LaunchReadinessGate` not built, an Epic-4 Agent-aggregate shape exists with no gap note (B-6); approved SCP 2026-08-04 AD-17 bind-and-test atomicity amendment never applied and no IntegrationTests project while 5.2/5.3 bound live seams (B-7); Stack table drift (B-9); seed vs tree, including unlisted `Api/`, `Composition/`, `Queries/`, `Tools/` (B-10); three story-numbering views coexist (B-12); unratified conventions from 5.2/5.3 such as trusted-extension actors and the submitted → pending → confirmed write flow (B-13); `ReadModelWritePolicy` and `IQueryCursorCodec` platform seams unused (B-15); flat layout ratified only in memlog (B-16); memlog keeps superseded decisions without supersession markers (B-18).

**Security (S-8..S-14):** no per-caller or per-conversation quota before reservation, denial-of-wallet within a tenant unbounded (S-8); prompt injection through conversation context not a named safety class (S-9); secret reference shape and rotation unspecified (S-10); backup/restore consistency across EventStore, workflow state, allocator, read models silent (S-13); data residency and legal basis for 365-day retention neither decided nor deferred (S-14).

## Where the spine holds

Both adversarial lenses re-tested the 2026-08-02 closures and found them intact: `Callability` has one owner; approver policy frozen vs live roles converge; the `AdmissionFence` plus `BeginInvocation` handshake defeats reclaimed-admission invocation; pre-fault terminal decisions stay immutable under recovery; the discriminated browser sample shape is convergent; the always-blocked safety classes match OQ-9 exactly. Every altitude-owned dimension is at least partially decided; none is silent. FR-1..FR-28 and the NFRs map to ADs with three gaps (FR-26 owner, NFR-9 clock, NFR-4 partial).

## Recommended fix order

1. C-1 execution-state content boundary, before any Epic 6 activity contract exists.
2. C-2 catalog and identity tenant scope, plus H-6 registry keys, before Stories 5.3/5.5/5.6 are accepted.
3. H-3 time authorities, H-4 freshness, H-5 id derivation, before 6.1/6.4/7.6.
4. H-1 aggregate inventory, H-2 principal model, H-7 safety rank, H-8 reservations, H-9 keys, H-10 audit envelope, H-11 kill switch, before Epic 7/8 stories enter ready-for-dev.
5. H-12..H-16 governance and ratification: `EXT-CONV-UI-1`, altitude cut, projection ids, 5.3 consumer list, stale notes and stack.
6. The medium/low tail, one paragraph each, ahead of their consuming stories.

## Suggested next step

Roll these findings into an **Update** run of `bmad-architecture`: resume from `.memlog.md`, log each closure as a decision, keep AD-1..AD-26 ids stable, add AD-27..AD-29, re-distill, rerun this gate. Then refresh the spec companion through `bmad-spec` so stories cite the new AD ids.
