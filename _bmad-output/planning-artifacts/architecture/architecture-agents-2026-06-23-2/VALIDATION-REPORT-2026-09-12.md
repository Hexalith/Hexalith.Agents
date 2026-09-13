---
name: Hexalith Agents architecture-spine validation
type: architecture-spine-validation
target: ARCHITECTURE-SPINE.md
target_status: final
target_updated: 2026-09-12
date: 2026-09-12
intent: validate-read-only
lint_ok: true
lint_findings: 0
lenses:
  - rubric walker v2
  - verified-current v2
  - adversarial divergence v2
  - security / data integrity v2
  - brownfield drift v2
offer_to_update: true
---

# Architecture Spine Validation — 2026-09-12

## Gate Verdict

**FAIL — despite a clean deterministic lint pass, the spine is not a safe convergence contract: three Critical defects can weaken active content-safety policy, make bootstrap/repair operations impossible, or irreversibly violate legal-hold/deletion guarantees.**

The five independent lenses yielded **3 Critical, 12 High, 4 Medium, and 1 Low** finding after deduplication. The safety-policy and hold/deletion Criticals were independently corroborated; the human-identity High was also corroborated. Counts below preserve the strongest reviewer severity and count each shared issue once.

## Methodology And Read-Only Scope

Validation followed the BMad Architecture Reviewer Gate:

1. The deterministic spine linter result supplied for this run was recorded.
2. The rubric walker checked the good-spine contract and binding source coverage.
3. Configured verified-current and adversarial-divergence reviewers checked current technology/repository reality and independently-buildable-unit compatibility.
4. Security/data-integrity and brownfield-drift lenses tested high-risk governance/data seams and ratification against the code and planning state.
5. This report reconciled all five review files, merged overlaps, preserved maximum severity, and separated architecture defects from tracking or already-owned implementation debt.

This was a **read-only validation**. `ARCHITECTURE-SPINE.md`, `.memlog.md`, source code, registers, and existing artifacts were not modified. No new build or test success is claimed; the verified-current reviewer records that SDK `10.0.401` is not installed in this environment.

### Deterministic Lint

| Check | Result |
| --- | --- |
| `lint_spine.py` | **PASS** |
| `ok` | `true` |
| Findings | `0` |

Mechanical correctness does not offset the semantic Critical/High findings below.

## Lens And Source-Review Matrix

| Lens | Primary stance / evidence | Raw C / H / M / L | Synthesis contribution | Review |
| --- | --- | ---: | --- | --- |
| Rubric walker | Good-spine checklist; PRD, registers, memlog, conventions, focused code | 1 / 3 / 1 / 0 | C-1; H-1..H-3; M-1 | [`review-2026-09-12-rubric-v2.md`](reviews/review-2026-09-12-rubric-v2.md) |
| Verified-current | Root manifests, exact gitlinks, official docs/releases/package registries | 0 / 1 / 0 / 1 | H-10; L-1 | [`review-2026-09-12-verified-current-v2.md`](reviews/review-2026-09-12-verified-current-v2.md) |
| Adversarial divergence | Two independently built units obeying literal ADs | 2 / 4 / 2 / 0 | C-2, C-3; H-4..H-7; M-2, M-3 | [`review-2026-09-12-adversarial-divergence-v2.md`](reviews/review-2026-09-12-adversarial-divergence-v2.md) |
| Security / data integrity | Authorization, secrets, protection, retention, deletion, audit/export, replay | 2 / 3 / 0 / 0 | Corroborates C-1, C-2, H-5; adds H-8, H-9 | [`review-2026-09-12-security-data-integrity-v2.md`](reviews/review-2026-09-12-security-data-integrity-v2.md) |
| Brownfield drift | Code, tests, epics, sprint tracker, dependency/evidence state | 0 / 2 / 1 / 0 | H-11, H-12; M-4 | [`review-2026-09-12-brownfield-drift-v2.md`](reviews/review-2026-09-12-brownfield-drift-v2.md) |
| **Deduplicated total** | Maximum severity retained | **3 / 12 / 4 / 1** | **20 findings** | |

## Critical

### C-1 — AD-20 permits weaker safety evaluation than the binding Product/Security contract

**Location / evidence:** AD-20 says a transport retry never re-evaluates safety and selects the snapshot pair alone whenever the current and snapshot pairs are not ordered (`ARCHITECTURE-SPINE.md:292-296`). PRD FR-26/FR-27 require retry, approval, and pre-post to pass every applicable version—the snapshot and then-current active policy—and the readiness register promises no-weaker retry behavior (`prd.md:672-708`; `launch-readiness-register.md:105`). **Corroborated by rubric and security lenses.**

**Divergence / impact:** One safety/workflow implementation follows the spine and ignores a newly tightened category after an incomparable policy change; another follows the PRD and evaluates both. The first can send or post content the active policy forbids and can record incomplete Audit Evidence.

**Disposition — AUTOFIX before implementation.** Make snapshot-plus-current evaluation conjunctive for retry, regeneration, approval, and pre-post. Permit a one-pass dominance optimization only when it is formally equivalent and tested. Reconcile AD-13 so a policy change has one deterministic, fail-closed retry outcome without changing transport input under an existing attempt id.

### C-2 — Legal hold and class deletion lack an exclusion protocol capable of “rejected in full”

**Location / evidence:** AD-2 gives `LegalHold`, `ProtectedDeletion`, and each `AgentInteraction` separate streams. AD-22 performs multi-interaction hold pinning, makes `DestroyDek` irreversible, and promises a class deletion is rejected in full if any interaction is pinned (`ARCHITECTURE-SPINE.md:140-144`, `304-308`). The command convention supplies no multi-stream transaction or common fence (`IMPLEMENTATION-CONVENTIONS.md:7-20`). **Corroborated by adversarial and security lenses.**

**Divergence / impact:** Deletion can destroy A's DEK, a concurrent hold can pin B, and deletion must then reject “in full” after an irreversible partial erasure. The hold cannot become Active because A can no longer be pinned. Per-stream expected revisions cannot make the resolved set atomic.

**Disposition — DISCUSS policy, then bind it.** If all-or-nothing is required, add a durable shared prepare/fence protocol across the frozen interaction set and forbid destruction until all members are reserved. Holds and deletions must contend on the same fence, with recovery/failure-injection evidence. If partial completion is acceptable, remove “rejected in full” and define visible partial results, retry, and legal-notification semantics.

### C-3 — The operation matrix deadlocks bootstrap/repair and cannot scope some platform mutations

**Location / evidence:** AD-17 makes matrix v3 mandatory and fail-closed; AD-30 assigns `ProvisionHexa`, catalog, budget, and tenant-enablement commands to its operation families (`ARCHITECTURE-SPINE.md:276`, `362`). The matrix requires the end-state gates those commands establish: `LR-PARTY-IDENTITY` for `AgentSetupMutation`, `LR-COST` for catalog/budget writes, and `LR-PROVIDER` for tenant enablement (`launch-readiness-register.md:141-171`). Platform catalog/policy writes also require Tenant-scoped `LR-TENANT-ACCESS`, while `tenant:system` is invalid.

**Divergence / impact:** A strict evaluator blocks first-time Party provisioning because the Party is missing, blocks cap setup because caps are missing, and blocks enablement because the entry is not enabled. A permissive implementation invents exceptions and no longer consumes the single normative matrix. Global catalog/policy writers either cannot resolve a tenant gate or produce tenant-dependent global outcomes.

**Disposition — AUTOFIX before administrative API work.** Define target-aware bootstrap/recovery semantics per family: the condition a command is authorized to clear cannot itself block that command, while all unrelated controls stay fail-closed. Give every family an explicit evaluation scope; platform-global mutations must not depend on an unresolvable Tenant gate. Add empty-state and repair-state contract fixtures for each family.

## High

### H-1 — Scheduled Eligible-Approver re-check has tokens but no lifecycle protocol

**Location / evidence:** AD-8 omits scheduler owner, cadence, freshness, the two-pass empty guard, and state-specific outcomes; AD-5 names `NoEligibleApprover`; AD-15 only exposes resolution tokens (`ARCHITECTURE-SPINE.md:168`, `198-202`, `262-264`). PRD FR-7 defines a single-flight scheduled pass, cadence/freshness, two authoritative empty passes for awaiting proposals, marker-only behavior for `PostingFailed`, and unavailable-evidence handling (`prd.md:262-268`).

**Divergence / impact:** Separate workflows may abandon after one empty/unavailable read, wait for two, only mark `PostingFailed`, or never schedule a pass. Proposals can be destroyed prematurely or stranded.

**Disposition — AUTOFIX.** Fold the PRD protocol into AD-8/AD-5, retaining `[ASSUMPTION A-20]` for cadence, and bind owner, single-flight behavior, accepted evidence, freshness, transitions, markers, and retry on unavailability.

### H-2 — Verdict-cache invalidation does not bind distributed re-scan ownership or call behavior

**Location / evidence:** AD-20 names the cache key and invalidation but not whether state is durable/shared, who scans, how concurrency is bounded, or how calls behave during rebuild (`ARCHITECTURE-SPINE.md:296`). AD-12 only mentions `RescanPending` operationally, while AD-15 exposes coarse `ContextUnavailable` (`:234`, `:262`). PRD FR-27 requires a tenant-bounded background scan and `ContextReadUnavailable(RescanPending)` (`prd.md:708`); current contracts still carry only `ContextUnavailable`.

**Divergence / impact:** Process-local, shared-cache, and EventStore-backed implementations invalidate and unblock at different times; calls can consume stale verdicts, stay blocked indefinitely, or expose incompatible public/metric vocabularies.

**Disposition — DISCUSS.** Choose the authoritative cache/re-scan owner and cross-replica invalidation protocol, then bind worker limits and the exact public/metric mapping. Preserve the current coarse contract as explicit migration debt until its owning story lands.

### H-3 — Eligible Approvers are not constrained to human Parties

**Location / evidence:** AD-8 permits predefined Parties, tenant roles, and Facilitators but never rejects organization/AI Parties (`ARCHITECTURE-SPINE.md:198`). PRD FR-7 requires human Parties only and typed configuration rejection for non-human sources (`prd.md:254`). Current public policy shape contains no type field, so code cannot imply the missing invariant.

**Divergence / impact:** Configuration, runtime resolution, UI, and audit can disagree on whether an organization or AI Party is an Approver, enabling a non-human approval authority or inconsistent “no eligible approver” behavior.

**Disposition — AUTOFIX.** Add a fail-closed Parties-owned human-type/liveness check at configuration and every resolution point, with one typed safe rejection contract.

### H-4 — `BudgetLedger` conflates rolling-rate, open-concurrency, and monthly-cost lifetimes

**Location / evidence:** AD-2 partitions the ledger by UTC calendar month. AD-21 uses it for rolling windows and `MaxConcurrentNonterminalInteractionsPerParty`, yet releases an attempt charge when the Provider attempt terminates. AD-5 holds the caller concurrency slot until an automatic posting record terminalizes (`ARCHITECTURE-SPINE.md:144`, `168`, `302`).

**Divergence / impact:** One unit releases at Provider settlement while another holds through posting. One expires rate consumption at attempt terminal while another preserves it for the rolling window. At month rollover, one reads only the new stream while another joins prior streams. Identical traffic gets different admission decisions.

**Disposition — DISCUSS, then tighten AD-2/AD-21.** Separate immutable rolling consumption, tenant-wide open-interaction leases, and monthly monetary reservations. Give each a clear owner/partition and release/expiry event; reconcile AD-5 and AD-21 explicitly.

### H-5 — Principal envelopes discard the human identity required for separation of duties

**Location / evidence:** AD-7 says `Administrator` and `Platform` carry no `PartyId`; AD-22 requires person-level requester/subject/second-party comparisons; AD-30 permits a `Platform` principal to be the second-party hold-release approver without a comparable human identity (`ARCHITECTURE-SPINE.md:184`, `304-308`, `358-364`, `542`). **Corroborated by adversarial and security lenses.**

**Divergence / impact:** The same human may act first as Inspector P and then as Platform Operator. One aggregate accepts because principal kinds differ; another rejects all Platform approvals because distinctness is unprovable. Self-approval and unusable approval paths are both compliant outcomes.

**Disposition — AUTOFIX.** Carry a stable authenticated-human actor id on every human-originated reserved principal, independently of role and target tenant, and bind it into audit and authorization checks. Model non-human automation as a separate principal and decide explicitly whether it can satisfy human second-party requirements.

### H-6 — The Conversation-wide non-terminal proposal index is not crash-consistent

**Location / evidence:** `AgentInteraction` owns proposal state; `ConversationAgentState` owns the non-terminal index; AD-7 says orchestration maintains them “idempotently alongside” each transition (`ARCHITECTURE-SPINE.md:144`, `184`). AD-3/AD-13 and the command convention define no cross-stream outbox, acknowledgement, or repair protocol (`:156`, `:244`; `IMPLEMENTATION-CONVENTIONS.md:7-20`).

**Divergence / impact:** Interaction-first can crash before indexing, causing a removal sweep to miss a live proposal. Index-first can leave a phantom when the interaction write conflicts. Expected revision protects each stream but not the invariant between them.

**Disposition — AUTOFIX.** Name one truth source and delivery protocol—prefer an interaction event/outbox carrying source revision, idempotent index application, and reconciliation before removal completion—or define an explicit recoverable saga. Add failure injection at both boundaries.

### H-7 — Deferred seam 7 makes the indivisible V1 Conversations dependency unusable

**Location / evidence:** AD-6 distinguishes six V1-critical `EXT-CONV-AI-1` seams from uncommitted/deferred-beyond-V1 retraction seam 7 (`ARCHITECTURE-SPINE.md:178`, `825`, `870`). The dependency register's status is whole-record atomic and says adding seam 7 keeps every consumer blocked until all seven commit (`external-dependency-register.md:35-67`).

**Divergence / impact:** The gate team blocks all V1 Conversation work pending the beyond-V1 metric seam; the integration team treats seams 1-6 as usable and invents partial availability the register cannot represent.

**Disposition — AUTOFIX.** Split retraction into its own `EXT-CONV-RETRACTION-1` or version/status each sub-seam independently. Core V1 stories should consume 1-6; Automatic-mode enablement consumes retraction only if OQ-23 selects that branch.

### H-8 — Trusted-envelope HMAC has no canonical input, replay rule, or key lifecycle

**Location / evidence:** AD-30 requires an HMAC tag but does not bind its authenticated fields, audience, issuance/expiry, nonce/replay behavior, key version, rotation overlap, revocation, or compromise response (`ARCHITECTURE-SPINE.md:358-362`). AD-29 specifies those classes of detail for other HMAC fingerprints, while `EXT-SECRETS-1` does not contract trusted-envelope signing (`external-dependency-register.md:163-175`).

**Divergence / impact:** One server MACs only `actor:*`, allowing cut-and-paste to another command; another MACs the full envelope. Old-key rejection can strand workflows, while indefinite acceptance preserves a compromised authorization key.

**Disposition — AUTOFIX.** Define canonical MAC input, actor, command/family, target/resource, idempotency/payload fingerprint, audience, issued/expiry instants, key version, replay behavior, skew, rotation overlap, emergency revocation, and constant-time fail-closed verification. Extend `EXT-SECRETS-1` and `LR-TENANT-ACCESS` tests.

### H-9 — Sensitive export-package bytes have no owner, hold/deletion lifecycle, or interoperable signature contract

**Location / evidence:** AD-22 specifies an encrypted, time-limited manifest/export but not the byte store, exact expiry, immutable write semantics, purge acknowledgement, restore behavior, later-hold behavior, or lookup by contained interaction (`ARCHITECTURE-SPINE.md:304-310`). The readiness inventory includes only the `export` projection, not the artifact store (`launch-readiness-register.md:265-291`). Manifest canonicalization/signature algorithm and trust anchor are also unspecified.

**Divergence / impact:** Blob-backed, streamed, and projection-backed implementations can all claim compliance yet retain different readable copies after deletion. Holds may preserve source but not export or vice versa; two signers can produce incompatible signatures for the same logical manifest.

**Disposition — DISCUSS storage policy, then bind it.** Name the store/owner, immutable tenant/resource key, authenticated encryption, exact expiry and physical purge acknowledgement, restore and hold behavior, interaction-to-export index, canonical manifest bytes, algorithm/key version, verifier, and trust anchor. Add the artifact store to deletion completion.

### H-10 — Dapr is already transitive, contrary to the spine's future-adoption premise

**Location / evidence:** The Stack and `ARCH-A-15` say Agents does not reference Dapr until Story 6.1 (`ARCHITECTURE-SPINE.md:562-563`, `860`). In both source and package modes, Agents consumes EventStore Client/DomainService, whose authoritative gitlink projects reference `Dapr.Client` and `Dapr.AspNetCore`; the imported catalog pins `1.18.5`. Official `1.18.7` supersedes it. Only Dapr Workflow remains future work.

**Divergence / impact:** Security/build ownership may defer an upgrade or exception past the point where Dapr client/ASP.NET packages are already compiled and loaded, while future Workflow adoption is conflated with present exposure.

**Disposition — DISCUSS (blocking), then AUTOFIX.** Builds Maintainer and Security decide an atomic `1.18.7+` move or bounded exception now. Correct the spine to distinguish current transitive Client/ASP.NET exposure from future Workflow adoption.

### H-11 — “Current public-contract parity” asserts backlog vocabulary is already shipped

**Classification:** spine defect, not implementation debt.

**Location / evidence:** AD-15 first assigns UX-required contract growth to future stories, then says the **current** surface contains `CurrentMirror`, expanded readiness/interaction states, resolution outcomes, capacity states, and launch blockers (`ARCHITECTURE-SPINE.md:256-264`). Repository contracts lack these symbols; `epics.md` assigns them to Stories 5.6, 6.6, 7.1, 7.4-7.6, and 8.5.

**Divergence / impact:** Builders may treat nonexistent contracts as ratified and skip owning migration work, or integrations may compile against shapes not present on disk.

**Disposition — AUTOFIX wording only.** Rename/rewrite these paragraphs as **required completion parity**, explicitly pointing to owning stories. Do not accelerate the already assigned implementation work.

### H-12 — Sprint tracking contradicts dependency and evidence authority for Stories 5.1/5.2

**Classification:** tracking/evidence defect; the spine rule is clear.

**Location / evidence:** The spine blocks consumers of Uncommitted dependencies (`ARCHITECTURE-SPINE.md:819-836`); `EXT-HOST-1` remains Uncommitted. Story 5.1 itself says it remains backlog and has no existing `verify-story-5.1.ps1`, while `sprint-status.yaml:86-89` marks 5.1 done and 5.2 in progress. Story 5.2 depends on 5.1 and its evidence still says not run/backlog (`epics.md:1238-1277`, `1287-1336`).

**Divergence / impact:** The tracker can authorize downstream work against an unexecuted manifest and missing dependency without a visible exception, so it is not a reliable readiness/evidence ledger.

**Disposition — DISCUSS before editing history.** Either roll statuses back to current authority, or record an owner-approved historical-completion/changed-dependency exception, replace the missing verification command, and reconcile story evidence. This is not a demand for broad implementation catch-up.

## Medium And Low Tail

### M-1 — Historical correction layers remain in the final build substrate

**Location / evidence:** AD-2 first describes independent `MirrorPending`, then supersedes it with `CurrentMirror`; AD-12/AD-22 state nine lock families before later amendments make ten; “Second/Third update” labels persist (`ARCHITECTURE-SPINE.md:144-146`, `232-238`, `254`, `278`, `308-310`). **Impact:** builders can reproduce obsolete shapes/counts by reading the first normative-looking clause. **Disposition — AUTOFIX:** re-distill every affected AD to one present-tense rule; retain history only in `.memlog.md`.

### M-2 — `ReadmitPending` does not bind initiation or confirmation ownership

**Location / evidence:** Clear creates a pending `Readmit`; only the next accepted membership step joins; `CurrentMirror` can be Pending/Confirmed/Refused (`ARCHITECTURE-SPINE.md:146`, `186-190`). **Impact:** one worker immediately adds/confirms, another waits for the next call or a separate acknowledgement, causing incompatible status and recovery timing. **Disposition — AUTOFIX:** bind owner, trigger, outcome command, state transition, and lost-ack recovery.

### M-3 — `PostingPending` timeout remains an acknowledged implementation divergence

**Location / evidence:** AD-5 allows any deadline no shorter than the Conversations timeout; `ARCH-A-14` fixes no duration and has no calendar retirement date (`ARCHITECTURE-SPINE.md:168`, `859`). **Impact:** workflow and recovery units can transition at different instants. **Disposition — DISCUSS or DEFER as a hard pre-story blocker:** select one duration/configuration authority, snapshot rule, and seam compatibility constraint before posting/recovery implementation.

### M-4 — Legacy conformance tests prove superseded structure/runtime semantics

**Classification:** already-tracked implementation debt.

**Location / evidence:** Tests still require `Server/Aggregates`, `Application/Tools`, module-host language, and old AD-18/AD-19 traceability, while the spine forbids/rescopes them. Story 5.6 already owns removal and test replacement (`StructuralSeedConformanceTests.cs:39-49,111-122`; `RuntimeOwnershipConformanceTests.cs:52-62,284-305`; `epics.md:1573-1594`). **Impact:** a green legacy test can be misreported as current conformance. **Disposition — DEFER to Story 5.6:** label interim evidence partial/legacy; do not count it as present AD-16/18/19 proof.

### L-1 — bUnit `2.9.0` is valid but behind stable `2.10.3`

**Location / evidence:** Stack line 573 and root/catalog/project all pin `2.9.0`; official NuGet lists `2.10.3` with a parallel-parser teardown fix. **Impact:** non-blocking UI-test reliability/maintenance drift; no incompatibility or security defect was established. **Disposition — DEFER:** align with focused UI-test evidence or add an owner/revisit point alongside Story 5.6 test-stack work.

## Already-Tracked Implementation Debt — Not Additional Findings

The brownfield lens verified that the following gaps are assigned work, not hidden architecture defects, and they do **not** inflate the severity totals:

| Current implementation gap | Architecture target | Existing disposition |
| --- | --- | --- |
| Tenant-scoped ProviderCatalog read-model keys | Platform catalog plus tenant enablement | Story 5.3 / `NC-5.3-PLATFORM-CATALOG-SCOPE` |
| Unversioned `/api/agents/operations` | `/api/v1/agents/...` | Story 5.5 |
| Incomplete lifecycle version bump and Party-link rejection | AD-4/AD-7 final contract | Story 5.2 |
| No module Dapr Workflow owner or IntegrationTests project | Platform host + Dapr Workflow + live seam tests | Stories 5.6 and 6.1 |
| Coarse `ContextUnavailable` and legacy deterministic ids | Expanded context states and AD-29 identities | Stories 6.2, 6.4, 7.1, 7.3 |

The uncommitted `EXT-PROTECTION-1`, `EXT-SECRETS-1`, and live tests also remain visible delivery blockers rather than new architecture contradictions. Existing `ARCH-A-*` assumptions remain `RQ-1` blockers under their recorded process.

## Recommended Update Order

1. **Safety first:** correct C-1 to the PRD's conjunctive current-plus-snapshot rule and reconcile retry fingerprint semantics.
2. **Irreversible governance:** decide C-2's legal-hold/deletion atomicity and bind the protocol before any key destruction; then close H-5, H-8, and H-9 as one identity/envelope/export-governance design pass.
3. **Administrative viability:** repair C-3's operation matrix scope and bootstrap/recovery rules with executable empty-state fixtures.
4. **Approval and safety coordination:** land H-1 through H-3, including the scheduled re-check, distributed re-scan owner, and human-only Approver rule.
5. **State ownership:** separate H-4 ledger lifetimes and define H-6's crash-consistent proposal index.
6. **External/current reality:** split H-7's retraction seam, resolve H-10's current Dapr exposure, correct H-11's “current” wording, and reconcile H-12's tracker/evidence history.
7. **Polish and scheduled debt:** re-distill M-1, close M-2/M-3 before their stories, retain M-4 and L-1 with their owners, then rerun lint and all five lenses.

## Validation Decision

The spine should not remain the authoritative implementation handoff in its current form. Roll these findings into a BMad Architecture **Update**, preserving AD ids, and rerun the full Reviewer Gate. The report offers an update; it applies none.
