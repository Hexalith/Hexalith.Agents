---
name: Hexalith Agents verified-current and PRD-conformance review v13
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: verified-current / bound-PRD-conformance / cross-artifact authority
verdict: fail
critical: 0
high: 1
medium: 3
low: 1
lint_ok: true
---

# Verified-Current / Bound-PRD-Conformance Reviewer Gate v13

## Gate Verdict

**FAIL — 0 Critical, 1 High, 3 Medium, 1 Low.** The v12 High corrections now preserve the Product-fixed FR-8 order, surface the legacy-plaintext decision without choosing it, and reconcile Story 6.1's direct dependency consumers. One fresh High contradiction remains: the normative sequence diagram performs content-bearing safety and Conversations reads before the effect lease that the ADs, conventions, and matrix say must authorize those reads. That ordering leaves the work outside the Conversation-deletion barrier manifest and can let it continue across `Effective`. PASS requires `VC13-H1` to close.

## Frozen Scope And Method

I read the frozen spine, implementation conventions, architecture memlog, authoritative `VALIDATION-REPORT-2026-09-12.md`, bound PRD, replacement `epics.md`, both registers, repository instructions, root manifests, exact root-declared gitlinks, and relevant current source. I traced literal call acceptance, proposal mutation/posting, Conversation deletion, legacy migration, export lifecycle, and dependency-evidence paths. Product-set requirements and Open Product/Governance decisions were kept distinct from architecture mechanics and current implementation debt.

The supplied SHA-256 values matched at intake and after this report was written:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `76adbcfe3dd7f07cc836f5c55b31d7b40945078171fcd53c9e30fb3929969efb` |
| `IMPLEMENTATION-CONVENTIONS.md` | `b619ea870627e299dbec0e82ecefde1b3c182bc0a27bb5a5c04653ab091f3b34` |
| architecture `.memlog.md` | `8128e951ca03c455af19965070932bffd37153c243d0afc387b64b76b43ca07d` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `ddbf1fe179ee3dd49ece7cad6fa3f5f34814f21d6669c64d9a92458954beebe9` |
| `external-dependency-register.md` | `b1d839b2a55289676e8bf09b737d135fb9c02f610ddbc11131bb10294d91ea27` |
| `launch-readiness-register.md` | `ec6eb4cf624acb88c92a919bca594de3a8f7ffc7b8b77c76f65cde7fe3e17c7e` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The deterministic spine linter returned `ok: true`, `total_findings: 0`. All 88 declared local source paths were checked. The only absent paths were the five explicitly anticipated concurrent v13/v5 review outputs, including this report before creation; every other local source resolved. AD IDs are unique and contiguous from AD-1 through AD-31.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 1 |
| Medium | 3 |
| Low | 1 |

## Critical

None. The target's normative prose and operation matrix specify a fail-closed lease cut; the remaining High is an internal executable-sequence contradiction rather than an absence of the protective protocol.

## High

### VC13-H1 — The executable sequence performs safety and Conversations reads before their deletion-cutover lease commits

**Classification:** target-architecture unit-divergence and deletion-integrity defect; not implementation debt and not an unresolved Product choice.

**Binding contract.** `ConversationAgentState` says acquisition authorizes no downstream read/effect and only `CommittedToEffect` authorizes the deterministic target step (`ARCHITECTURE-SPINE.md:192`). AD-6 applies that rule to every Context/membership/Provider/mutation/retry/posting phase and says only the committed revision may authorize the exact dependency read or external effect (`:232`). AD-7 specifically puts the pre-post three-part membership/existence check inside the committed `ConversationPosting` lease (`:242`). The implementation convention repeats the four phases and says only the commit revision authorizes the target append/read/effect (`IMPLEMENTATION-CONVENTIONS.md:19`). Matrix v4 likewise places the committed lease before `ContextAndSafety`, `ProposalResolution:ApplyUnderEffectLease`, and `ConversationPosting:BeginUnderEffectLease` (`launch-readiness-register.md:241-249`).

**Contradictory literal path.** In both automatic and confirmation posting branches, the sequence calls full current Safety and then Conversations existence/access/membership before it reserves or commits `ConversationPosting` (`ARCHITECTURE-SPINE.md:656-660,672-676`). The confirmation branch also calls approval safety before reserving/committing `ProposalMutation` (`:665-669`). These are not merely local immutable checks: they cross the Safety and Conversations dependency boundaries and may carry/read protected content.

**Failure construction.** A worker begins pre-post Safety or the Conversations read using the diagram. `InstallBarrierClosing` then wins on `ConversationAgentState`; because no lease was reserved/committed, that work is absent from the finite Closing manifest. The barrier may converge known work and append `Effective` while the unmanifested dependency call continues or returns. At minimum, separately built units disagree about the authorization cut. At worst, generated/edited content is disclosed to Safety or Conversation-derived state is read after the architecture's claimed effective deletion cut, and deletion can proceed without waiting for its outcome. The same gap applies to an approval safety recheck started without a committed mutation lease.

**Required correction — AUTOFIX mechanics.** Redraw the sequence so each `ProposalMutation` or `ConversationPosting` reservation and same-owner commit occurs before the first target Safety/Conversations read, with every result and settlement inside that committed lease. If local checks genuinely must occur before commit, label their closed content-free/no-dependency set explicitly; do not place Safety, roster, existence, accessibility, or membership reads there. Keep `BeginPosting` after full leased pre-post validation and before the append exactly as AD-5 already requires. Add sequence-parity and Closing/Effective failure injection immediately before/after approval safety, pre-post safety, Conversations read, `BeginPosting`, and the external append. No Product outcome is needed.

## Medium

### VC13-M1 — Story 5.5's evidence Requirements omit its direct Provider dependency

Story 5.5's External and Dependencies sections require `EXT-PROVIDER-1`, its Result names that record as a blocker, and the authoritative dependency register lists Story 5.5 as an exact consumer (`epics.md:1499,1575,1580`; `external-dependency-register.md:151`). The evidence manifest's Requirements field omits `EXT-PROVIDER-1` while listing the other dependency and decision IDs (`epics.md:1573`). This does not bypass the prose blocker, so it is Medium, but a manifest-driven evidence tool can produce an incomplete trace. Add `EXT-PROVIDER-1` to Requirements; retain the existing at-least-`Committed` threshold and do not make Provider runtime generation part of this readiness-contract story.

### VC13-M2 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

PRD FR-28 requires a co-owner-approved literal calendar date for Architecture-owned `RQ-1` assumptions, with absence remaining a blocker. `ARCH-A-1`, `-2`, `-3`, the test-stack portion of `-4`, `-6`, `-7`, `-8`, `-11`, `-12`, and `-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1173-1186`). The spine correctly fails closed, making this governance incompleteness rather than a runtime High. Obtain approved dates or an authorized bounded deferral; do not invent them in Architecture.

### VC13-M3 — `PostingPending` still has no one concrete timeout authority

AD-5 permits a stored posting deadline no shorter than the Conversations seam timeout, while `ARCH-A-14` confirms that neither the PRD nor spine fixes the duration or final configuration authority (`ARCHITECTURE-SPINE.md:214,1186`). The owning story remains blocked, so no unsafe current default is claimed. Resolve the exact source, snapshot rule, and compatibility bound before posting/recovery implementation becomes ready.

## Low

### VC13-L1 — bUnit is valid but behind the current stable package

The root pins `bunit` `2.9.0`, which the Stack reports accurately, while the official NuGet V3 index includes stable `2.10.3`. This is non-blocking test-stack delivery maintenance assigned to Story 5.6, not an architecture contradiction. Source: [official NuGet package index](https://api.nuget.org/v3-flatcontainer/bunit/index.json).

## Requested Focus Audit

| Focus | Result |
| --- | --- |
| Exact FR-8 ordering | **Pass.** Bound PRD steps 5-10 now match AD-7, AD-13, conventions, matrix v4, sequence intake, and Story 6.1: rate/open, Confirmation-only Approver, Context, Budget, safety, then leased membership immediately before acceptance. Directory registration is intake, not acceptance. |
| Story 5.5 metadata | **Medium at VC13-M1.** External/Dependencies/Result and the register block on Provider, but Requirements omits its ID. Recorder scope and conditional Parties authority remain properly Open/fail-closed. |
| Story 5.8 metadata | **Pass.** Protection, secrets, OQ-31, and conditional legacy-plaintext authority appear consistently in Requirements, Dependencies, Result, and negative evidence. |
| Story 6.1 metadata | **Pass.** `EXT-HOST-1`, protection, secrets, Parties, Conversations, topology, Dapr security, and conditional legacy disposition now appear across External, Requirements, Dependencies, Result, register consumers, and tests. |
| Story 8.3 metadata | **Pass.** Common dependencies are unconditional; legacy disposition is conditional on nonempty legacy plaintext; export store/lifecycle are conditional at the first export-bearing prepare; OQ-31 authorization is unconditional; Conversation deletion depends on Story 6.1's write fence. |
| External consumer authority | **Pass subject to VC13-M1's manifest trace.** The dependency register's exact `ConsumingStories` rows match the direct story dependencies reviewed, including Story 6.1 for host/secrets/Parties and Story 8.3 for Conversations seam 6. Every record remains `Uncommitted`; none is represented as executable. |
| Lease commit semantics | **FAIL at VC13-H1's sequence only.** Normative AD/convention/matrix text uses `Reserved -> CommittedToEffect -> Settled` or pre-commit cancellation and settles WorkflowStart at the first checkpoint. Provider result, generated-output mutation, Budget/capacity, and posting lookup settlement remain exact. |
| Legacy write fence and decision | **Pass.** Platform-Operator bootstrap, quiesced finite inventory, EventStore namespace epoch guard, legacy credential revocation, stale/disconnected/queued/restored rejection, two scans, and accepted-late-write invalidation are explicit. `OD-LEGACY-PLAINTEXT-DISPOSITION-1` blocks only nonempty legacy plaintext and chooses no migration, eradication, exception, or unsupported posture. |
| Deletion inventory ownership | **Pass.** `ProtectedDeletion` owns only reversible candidates; `ProtectionFence:AcceptDeletionInventory` is the sole immutable-set/token decision, serializes hold/export/copy/migration revisions, and supports same-id refreeze plus acknowledgement recovery. |
| Export phase pin | **Pass.** Export-bearing deletion pins the effective lifecycle version and exact store target at prepare and reuses them through destruction/purge/recovery; export-free deletion proves no covered copy and neither reads nor fabricates the decision. |
| Source delivery/origin | **Pass.** Source-atomic retained outbox/feed, stable signal/source identity, ordered backfill, poison no-skip, authenticated durable acknowledgement, target rollover, and the structurally distinct no-human Conversation origin are bound. |

## Authoritative Validation-Finding Closure Audit

| Authoritative finding | v13 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot and current policies are conjunctive; collapse is allowed only under proven semantic dominance. |
| C-2 hold/deletion exclusion | **Closed.** One `ProtectionFence`, frozen resource sets, phase separation, exact receipts, and conditional export-copy handling linearize irreversible work. |
| C-3 bootstrap/matrix deadlock | **Closed.** Matrix v4 has explicit target-aware bootstrap, containment, repair, and recovery variants. |
| H-1 scheduled Approver recheck | **Closed.** Durable single-flight scheduling, two-pass empty evidence, state-specific actions, and unavailable retry are explicit. |
| H-2 safety rescan ownership | **Closed.** Durable epoch/index owners, bounded finite manifests, fencing, initialization, and `RescanPending` are bound. |
| H-3 human-only Approvers | **Closed.** Parties-owned human type/liveness and actor binding fail closed at configuration and resolution. |
| H-4 conflated ledgers | **Closed architecturally.** Rolling rate consumption, original-caller concurrency, and monthly monetary reservations have distinct owners and lifetimes. |
| H-5 human separation identity | **Closed.** Stable `AuthenticatedHumanActorId` is independent of principal kind and Party identity. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append outbox, source high-water, fixed-point reconciliation, and acknowledgement recovery are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Six core seams and optional retraction have separate dependency authority and consumers. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated bytes, replay owner, issuer-wide nonce, timing, key version/rotation/revocation, and safe denial spooling are bound. |
| H-9 export ownership/lifecycle/signature | **Closed architecturally.** Store/index ownership, AEAD, ES256 canonical manifest, phase-pinned lifecycle, holds/restores, key delivery, purge, and receipts are defined. |
| H-10 current Dapr exposure | **Closed as verified-current truth/debt.** Parent Builds pins current transitive Client/ASP.NET `1.18.5`; official `1.18.7` and the non-authoritative checkout are distinguished; Workflow remains future adoption. |
| H-11 overstated shipped parity | **Closed.** Missing target contracts are stated as delivery debt, not current repository behavior. |
| H-12 tracking/dependency conflict | **Closed as surfaced governance debt.** The Open sprint decision cannot override external/dependency or evidence authority. |

## v12 Critical/High Closure Audit

| v12 finding | v13 disposition |
| --- | --- |
| VC12-H1 FR-8 order and unleased membership | **Closed.** Membership is now leased step 10 after rate/open, Approver, Context, Budget, and safety everywhere reviewed. `VC13-H1` concerns later approval/pre-post reads, not FR-8 membership. |
| VC12-H2 absent legacy-plaintext disposition | **Closed without choosing Product.** The stable Open decision names Product, Governance, Security, and EventStore-maintainer owners, exact affected evaluations, fail-closed safe state, protected/exact-empty evaluability, and the need for an outcome-specific all-copy procedure. Stories 5.8, 6.1, and 8.3 carry matching conditional blockers and negative evidence. |
| VC12-H3 Story 6.1 consumer omissions | **Closed.** Host, secrets, and Parties are now exact Story 6.1 consumers in the register and are present throughout the story evidence chain. |

## Verified Repository And Technology Reality

- Root gitlinks remain the architecture authority: Builds `a32cb422`, EventStore `ce9e779a`, Conversations `73bcee6f`, Parties `fa423985`, Tenants `2fac1839`, and FrontComposer `053b2008`. Initialized Builds `cf52f74`, EventStore `a568af4`, Conversations `64b05083`, Memories `42dfa26`, and FrontComposer `1b3608c` checkout HEADs differ from their root gitlinks; the spine correctly does not promote them to authority. No submodule was initialized, edited, cleaned, or advanced.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; `Directory.Build.props` says `net10.0` and C# 14; the solution is `.slnx`; root Central Package Management and overrides match the Stack. Official .NET release metadata lists runtime `10.0.12`, SDK `10.0.401`, and `CVE-2026-69522` in the security release, matching the spine's scoped fixed-floor statement. Source: [official .NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json).
- Parent-authoritative Builds pins the complete Dapr family at `1.18.5`; EventStore Client/DomainService at the parent gitlink reference Dapr Client/ASP.NET. The modified Builds checkout pins `1.18.7`. Official NuGet contains stable `1.18.7` for Client, ASP.NET, and Workflow, while no Agents project currently references Workflow. Source: [official Dapr.Client package index](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json).
- Current `InteractionRequested` and `AgentInteractionState` retain raw `Prompt`; EventStore's parent-authoritative default payload-protection service is no-op/unprotected. Current code has no `ConversationAgentState`, effect lease, target protection-key alias, migration fence, deletion barrier, Dapr Workflow owner, or target governance aggregates. The spine accurately classifies these as delivery debt and blocks live content/deletion evidence; it does not claim them shipped.
- Current approval orchestration still calls Conversations before durable aggregate dispatch, confirming the separately tracked AD-5 implementation debt. This repository gap does not weaken the target architecture contract.

## Architecture Defects Versus Implementation Debt

| Item | Classification |
| --- | --- |
| VC13-H1 sequence ordering | Target architecture/cross-unit contradiction. It needs a document correction and parity fixtures; Product has already fixed the relevant authorization/deletion semantics. |
| VC13-M1 Story 5.5 Requirements omission | Cross-artifact evidence metadata defect, not missing runtime implementation. |
| VC13-M2/M3 | Explicit unresolved governance/architecture inputs that already fail closed; do not invent dates or a timeout. |
| Missing directory, leases, write fence, protection engine, durable source feed, export store, decision catalog, and governance workflows | Correctly stated implementation/external delivery debt assigned to stories/register records; not new architecture findings. |
| Current post-before-dispatch approval implementation | Correctly tracked Story 7.4 delivery debt, not target-contract truth. |
| Dirty/different submodule working-tree HEADs | Correctly distinguished from exact root gitlink authority; no update is implied. |
| bUnit version lag | Low build/test maintenance debt. |

## Final Hash, Source, And Lint Verification

**PASS for freeze integrity, source-chain existence under the instructed anticipated-output exception, and deterministic lint.** All eight reviewed input hashes exactly matched the supplied frozen values after the report write. A final `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` returned `ok: true`, `total_findings: 0`, and no severity entries. This mechanical pass does not override the semantic **FAIL** or `VC13-H1`.
