---
name: Hexalith Agents verified-current and source-reconciliation review v17
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: verified-current / bound-PRD-conformance / source-reconciliation
verdict: fail
critical: 1
high: 1
medium: 2
low: 1
lint_ok: true
---

# Verified-Current / Source-Reconciliation Reviewer Gate v17

## Gate Verdict

**FAIL — 1 Critical, 1 High, 2 Medium, 1 Low.** The v17 candidate closes both v16 High findings and its source-chain omission: permit registration is now correctly outside the permit-bound lease protocol, human-origin exact-Conversation semantics are an explicit Open Product decision, operator nonterminal deletion has its own narrowly scoped Open decision, and `epics.md` is declared as a source. A complete fresh walk nevertheless found two target-architecture defects in the new admission-violation protocol. First, the clean external violation-ledger observation is not linearized with the `ProtectionFence` append of irreversible `DestructionStarted`. Second, the promised higher-ordinal recovery cycle cannot legally re-close an already `Effective` request-scoped owner cut and cannot prove an unambiguous current-ordinal zero checkpoint after an earlier append-only violation. These are architecture mechanics, not implementation debt and not Product choices.

## Frozen Scope And Method

I reviewed the frozen `ARCHITECTURE-SPINE.md`, `IMPLEMENTATION-CONVENTIONS.md`, authoritative `VALIDATION-REPORT-2026-09-12.md`, bound PRD, final clarified `epics.md`, external-dependency and launch-readiness registers, current architecture memlog, repository instructions, root manifests, root-authoritative gitlinks, relevant parent-authoritative and checked-out source, and declared source-chain existence. I re-walked all original three Critical and twelve High findings, both v16 High corrections, FR-8 ordering, Story 5.5/5.8/6.1/8.3 metadata, every deletion interleaving, migration repair, phase authorizations, Provider branch selection, external consumers, Open Decision scopes, and target-versus-shipped statements. I selected no unresolved Product, Governance, Security, dependency, or delivery outcome.

The supplied SHA-256 values matched at intake and at final verification:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `f834ae4e5d6c82b5581c55e4fa6ac06832cc758e6ff3218a227aaba0b631cd34` |
| `IMPLEMENTATION-CONVENTIONS.md` | `accc861511863f03bf27f447d2bbc9d62862b793c954b3db98b64ef496931f76` |
| architecture `.memlog.md` | `5cb60e74516a579cd131a48f9c63df11fdcc10cd278ab34899d41e4482c6b09b` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| final `epics.md` | `6f9726d366d5957ea9180c9d4d59ecf47cc1cb8a8f13bb35625a49ebf46380e1` |
| `external-dependency-register.md` | `aecbcdfa2538af9c62f70c4cb5a32690ea5811639671f681859e3203700d8788` |
| `launch-readiness-register.md` | `31f937e7f1166a71d5e9180cfad570787cf57f315c65dea41ed9bd9ef21e0fbe` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The deterministic spine linter returned `ok: true`, `total_findings: 0`, with no severity entries. AD IDs are unique and contiguous from AD-1 through AD-31.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 1 |
| Medium | 2 |
| Low | 1 |

## Critical

### VC17-C1 — Admission-ledger verification is not linearized with irreversible `DestructionStarted`

**Classification:** target irreversible-deletion/cross-owner linearization defect; not implementation debt and not a Product decision.

The spine requires the Workflow to verify “immediately before” `DeletionDestructionStarted` that the external EventStore admission-violation ledger remains at the zero checkpoint bound to the global cut, then append `DestructionStarted` on the tenant `ProtectionFence` (`ARCHITECTURE-SPINE.md:217,268-272,452,458`). Matrix v4 likewise combines `AdmissionViolationLedgerStillEqualsAcceptedBoundCheckpointAndHasZeroAcceptedViolations` with a `FenceExpectedRevisionMatches` append (`launch-readiness-register.md:325`). But the guard/ledger and `ProtectionFence` are different owners, and the architecture explicitly refuses to assume a multi-stream transaction (`ARCHITECTURE-SPINE.md:452`). No reservation, one-shot guard capability, compare-and-append port, or authorize/effect/result protocol prevents the admission ledger from advancing between the clean read and the fence append.

A literal race is therefore legal: deletion reads checkpoint N as clean; a stale/restored or defective writer obtains an EventStore-accepted matching permit, lease commit, or phase authorization and the guard records violation N+1; deletion then wins the unchanged `ProtectionFence` expected revision and appends `DestructionStarted` phase-pinned to N. Discovery after the append records `DeletionIntegrityCompromised`, but DEK destruction was already authorized and the committed external effect may already have begun. Later blocking cannot restore the key or recall that effect. This is the exact irreversible last-moment race the admission fence/ledger is meant to prevent.

**Required correction — architecture mechanics.** Add a closed destruction-start authorize/effect/result boundary. For example, `ProtectionFence` records a target-limited `DeletionDestructionStartCheckAuthorized`; the EventStore guard atomically compares the exact admission-fence ordinal/checkpoint, installs a one-shot start barrier (or returns conflict), and exposes an authenticated idempotent result; `ProtectionFence` records that exact result and only it may append `DestructionStarted` or combine result recording with the irreversible decision. Define the single linearization owner, token/revision, crash/lost-ack lookup, invalidation/cancellation rule, and exact first authority for key destruction. A violation accepted before that linearization must invalidate the result; no read-followed-by-unrelated-append implementation is conformant. Bind spine, conventions, `EXT-HOST-1`, matrix, Story 8.3, and before/at/after failure-injection evidence for both origins.

## High

### VC17-H1 — The higher-ordinal admission recut has no legal owner-cut transition or per-ordinal zero-proof identity

**Classification:** target recovery state-machine/evidence-identity defect; not implementation debt and not a Product decision.

A pre-destruction accepted admission violation preserves old restrictions, invalidates the global cut/candidate/token, and permits only a higher-ordinal admission fence followed by a complete owner Closing/Effective/global-cut cycle including the violator (`ARCHITECTURE-SPINE.md:217,266-272`; `IMPLEMENTATION-CONVENTIONS.md:23`; `epics.md:3009-3012`; `external-dependency-register.md:129-132`). At that point every owner normally already has request-scoped `DeletionEffectCutEffective`. Yet the owner-cut identity is only `(DeletionRequestId, ScopePredicateDigest)`, with Closing then Effective and no ordinal/generation (`ARCHITECTURE-SPINE.md:209`). `InstallBarrierClosing` permits only `BarrierOpenOrExistingRequestScopedClosingExact`, not an existing Effective cut or a separately keyed successor (`launch-readiness-register.md:293`). The required successor cycle therefore cannot start without inventing an undocumented `Effective -> Closing` mutation or new identity.

The evidence predicate is also ambiguous. The violation ledger is append-only and must retain the accepted prior violation, while the successor global cut again requires `AuthenticatedCompleteAdmissionViolationLedgerCheckpointHasZeroAcceptedViolations` (`launch-readiness-register.md:304`). A request-wide reading can never return zero again; a current-ordinal reading can, but no ledger key/predicate says `ZeroAcceptedViolationsSinceInstall` for the successor ordinal. One team can wedge forever; another can silently invent ordinal-keyed cuts/ledger windows; a third can overwrite Effective provenance cited by the invalidated global receipt.

**Required correction.** Make owner cuts immutable per admission-fence ordinal/cycle, or add an explicit immutable `SupersededByAdmissionViolation` transition followed by a separately keyed successor Closing. The successor manifest must carry all still-restrictive earlier obligations plus the violating winner and its outcome. Define ledger identity and the exact current-ordinal zero predicate while retaining prior violations forever. Carry the ordinal/cycle through Closing, Effective/global receipts, migration preservation, candidate/acceptance, operator-Abort release, exact lookup, conventions, external contract, Story 8.3, and tests.

## Medium

### VC17-M1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

PRD FR-28 requires every Architecture-owned `RQ-1` assumption to carry a literal calendar target; a milestone/TBD value blocks independently (`prd.md:730,898`). Numerous rows remain “Unscheduled,” including ARCH-A-1, A-2, A-3, A-6, A-7, A-8, A-11, A-12, and A-14 (`ARCHITECTURE-SPINE.md:1302-1315`). The spine correctly keeps them blocking, so this is not unsafe authorization, but its authoritative assumption register remains structurally nonconformant. Named owners must record dates; Architecture must not invent them.

### VC17-M2 — `PostingPending` still lacks one concrete timeout authority

AD-5 permits any stored attempt deadline no shorter than the Conversations seam-2 timeout and states that neither PRD nor spine fixes a duration (`ARCHITECTURE-SPINE.md:245`). ARCH-A-14 remains unscheduled (`:1315`), and Story 7.5 remains blocked until it is retired (`epics.md:2558,2614`). This safely blocks delivery but leaves incompatible timing implementations possible. Select one configuration authority/snapshot rule or explicitly bind to the committed seam timeout, then obtain the named Product confirmation.

## Low

### VC17-L1 — bUnit `2.9.0` is valid but behind stable `2.10.3`

Root/package evidence and the Stack agree on bUnit `2.9.0`; the official NuGet version index currently lists stable `2.10.3`. No security or compatibility defect was established. Align during Story 5.6/build maintenance or record intentional retention. Source: [official bUnit package version index](https://api.nuget.org/v3-flatcontainer/bunit/index.json).

## v16 Critical/High Closure Audit

| v16 finding | v17 disposition |
| --- | --- |
| VC16-H1 permit-registration/lease circularity | **Closed.** Permit registration is the unleased directory-first EventStore append; only later permit-bound phases reserve/commit leases (`ARCHITECTURE-SPINE.md:209,280,340,359`; `IMPLEMENTATION-CONVENTIONS.md:17,59`; matrix `:251-252`; Story 6.1 `:1889-1895`). |
| VC16-H2 hidden human exact-Conversation temporal semantics | **Closed without choosing Product behavior.** `OD-HUMAN-EXACT-CONVERSATION-SCOPE-1` blocks only human hold/export/operator-deletion exact-Conversation variants; exact interactions and PRD-fixed source deletion remain evaluable (`ARCHITECTURE-SPINE.md:205-207,1246`; matrix `:109`; Stories 8.1-8.3). |
| VC16-M3 missing `epics.md` source | **Closed.** `../../epics.md` is now in frontmatter sources (`ARCHITECTURE-SPINE.md:20`). |

The operator nonterminal gap identified by prior specialist review is also closed without inventing an outcome: `OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1` keeps only the affected operator-origin owner in Closing and leaves Conversation-approved source deletion on its existing terminal transition (`ARCHITECTURE-SPINE.md:1235`; matrix `:98,294`; Story 8.3 `:2960-3057`).

## Authoritative Validation-Finding Closure Audit

| Authoritative finding | v17 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot/current policies are conjunctive; a one-pass collapse requires machine-checkable semantic dominance. |
| C-2 hold/deletion exclusion | **Reopened only at VC17-C1's final irreversible linearization.** Shared tenant fencing, accepted immutable inventory, owner/effect cuts, content fencing, export/copy accounting, and receipts are otherwise explicit. The clean admission proof can still become stale immediately before irreversible start. |
| C-3 bootstrap/matrix deadlock | **Closed.** Bootstrap, containment, recorder, migration/repair, and recovery paths have target-aware gate exceptions. |
| H-1 scheduled Approver recheck | **Closed.** Durable cadence/single-flight owner, two-pass evidence, phase lease, typed result, and retry are explicit. |
| H-2 safety rescan ownership | **Closed.** Epoch/index owners, finite manifest, fenced coordinator, bootstrap, and worker bounds are explicit. |
| H-3 human-only Approvers | **Closed.** Parties-owned human/liveness and stable actor binding fail closed. |
| H-4 conflated ledgers | **Closed.** Rate, caller-open, Budget, and capacity lifetimes have separate owners and settlement. |
| H-5 human separation identity | **Closed.** `AuthenticatedHumanActorId` spans Party-bearing and Party-free human principals. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append outbox, high-water repair, and exact acknowledgement are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Six mandatory `EXT-CONV-AI-1` seams and optional retraction are split with consumer authority. |
| H-8 trusted-envelope security | **Closed.** Canonical MAC input, audience/time/key lifecycle, platform-wide issuer nonce replay, constant-time validation, and ACL-confined denial spool are bound. |
| H-9 export ownership/lifecycle/signature | **Closed architecturally.** Fence-owned commit precedes acknowledgement/key release; immutable artifact/index, phase-pinned lifecycle/store, ES256 manifest, delivery recovery, and all-copy cleanup are explicit; Product policy stays Open. |
| H-10 current Dapr exposure | **Closed as verified-current truth/debt.** Root-authoritative `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption are distinguished. |
| H-11 overstated shipped parity | **Closed.** Required target contracts and absent implementation are separated, including the unapproved current `SafetyFailed` mapping. |
| H-12 tracking/dependency conflict | **Closed as surfaced delivery/governance debt.** Register/evidence authority overrides historical tracker labels; no uncommitted seam is presented as ready. |

## Requested Reconciliation Audit

| Focus | Result |
| --- | --- |
| Bound PRD / Product-fixed FR-8 | **Pass.** Directory intake precedes rate/open step 5, Confirmation-only Approver resolution step 6, Context step 7, descriptor/Budget step 8, safety step 9, membership step 10, then acceptance. Automatic mode performs no Approver resolution. |
| Stories and external consumers | **Pass.** Story 5.5/5.8/6.1/8.3 Requirements, OwnedClauses, Dependencies, tests, and blocked Results agree with register status. `EXT-HOST-1` correctly names Stories 5.1/5.4/5.6/6.1/8.3 and RQ-1; Story 5.8 consumes host composition through prior Story 5.6 rather than falsely claiming separate host readiness. |
| Conversation deletion / every interleaving | **Fail at VC17-C1 and VC17-H1.** Intake identity/source/acknowledgement, permit/intent/lease/phase-authorizations, Provider/posting convergence, migration preservation, content fencing, and export/copy inventory are defined; irreversible start and recut recovery are not yet executable safely. |
| Product/governance authority | **Pass.** Human exact-Conversation, operator nonterminal/cancellation, class-range, legacy plaintext, initial-output status, instruction protection, export lifecycle, hold contention/cancellation, recorder scope, rate/concurrency, and Dapr security remain explicit, narrowly scoped Open decisions. No implementation default selects an outcome. |
| Architecture versus delivery debt | **Pass.** Missing directory/leases/phase authorizations/migration/deletion/export/workflows are delivery debt against explicit ADs. Current direct/plaintext/no-op/post-before-dispatch/`SafetyFailed` behavior is current repository reality, not Product or architecture authority. VC17-C1/H1 are defects in the target contract itself. |

## Verified Repository And Technology Reality

- The root commit is `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. Root-authoritative gitlinks remain Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, and Tenants `2fac1839`. Checked-out Builds `cf52f74`, Conversations `64b05083`, EventStore `a568af4`, FrontComposer `1b3608c`, and Memories `42dfa26` differ; Parties/Tenants match. Those checked-out revisions are implementation evidence only. No submodule was initialized, advanced, cleaned, or edited by this review.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; installed SDK is `10.0.401`. Root props target `net10.0`, C# 14, `.slnx`, and Central Package Management. Official .NET 10 metadata currently lists SDK `10.0.401` and runtime `10.0.12`: [official .NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json).
- The parent-authoritative Builds catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`; parent-authoritative EventStore consumes Client/ASP.NET. The differing Builds checkout pins `1.18.7`; official NuGet lists stable `1.18.7` for [Dapr.Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), [Dapr.AspNetCore](https://api.nuget.org/v3-flatcontainer/dapr.aspnetcore/index.json), and [Dapr.Workflow](https://api.nuget.org/v3-flatcontainer/dapr.workflow/index.json). Agents has no direct Workflow reference. The Stack and ARCH-A-15 state this accurately.
- Current source still stores raw `Prompt`, uses the EventStore no-op protection default, posts before durable command dispatch in approval orchestration, maps output-safety denial to `SafetyFailed`, and has no target `ConversationAgentState`, migration/repair fences, governance scope/fence aggregates, decision catalog, or Dapr Workflow owner. The spine's delivery-debt table states these as shipped gaps and never claims target parity.

## Architecture Changes Versus Implementation Debt

| Item | Classification |
| --- | --- |
| VC17-C1 | Target architecture irreversible-boundary defect; correct the architecture/matrix/external contract before implementation. |
| VC17-H1 | Target architecture recovery identity/state defect; correct the ordinal cut/ledger protocol before implementation. |
| VC17-M1/M2 | Explicit fail-closed planning inputs requiring owner-approved dates/timeout; not authorization to implement defaults. |
| Open Product decisions | Correctly surfaced and scoped; no delivery team may implement a local outcome. |
| Missing target aggregates, workflows, guards, ledgers, protection, export, decision runtime, and Conversations deletion feed | Delivery/external dependency debt assigned to stories/register owners. |
| Current plaintext/no-op/direct-stream/post-before-dispatch/`SafetyFailed` behavior | Brownfield implementation reality/debt, never architecture authority. |
| Differing checked-out submodule HEADs | Working-tree reality distinct from root gitlink authority; no update is implied. |
| bUnit lag | Low build/test maintenance debt. |

## Final Hash, Source, And Lint Verification

**PASS for frozen-input integrity, source accounting, and deterministic lint; FAIL for the semantic gate.** All eight supplied hashes matched after this report was written. The spine declares 115 sources, 101 local. At report-write time only the anticipated concurrent v17 rubric, this v17 report itself, security-data-integrity-v5, and brownfield-drift-v5 paths were absent; adversarial-divergence-v17 and every non-concurrent local source resolved. This report resolves its own path on creation. `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` returned `ok: true`, `total_findings: 0`. AD-1 through AD-31 are unique and contiguous. No build/test success is claimed because the requested gate was read-only apart from this review file.
