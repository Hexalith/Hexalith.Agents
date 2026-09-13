---
name: Hexalith Agents verified-current and PRD-conformance review v14
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: verified-current / bound-PRD-conformance / cross-artifact authority
verdict: fail
critical: 0
high: 3
medium: 2
low: 1
lint_ok: true
---

# Verified-Current / PRD-Conformance Reviewer Gate v14

## Gate Verdict

**FAIL — 0 Critical, 3 High, 2 Medium, 1 Low.** The v13 approval/pre-post lease-ordering correction is present, the original authoritative Critical/High findings remain architecturally closed, and the operator-deletion and migration-repair additions are materially complete. Three fresh High divergences prevent PASS: Confirmation-mode Eligible Approver resolution has no committed deletion-cutover lease despite requiring Conversations/Parties dependency reads; AD-13/matrix/sequence and AD-20 do not agree whether an initial generated-output safety denial records `GenerationFailed` or `SafetyFailed`; and the delivery map still commands a live separate failure record while the revised architecture says that identity is absent unless Product later authorizes retention.

## Frozen Scope And Method

I read the complete frozen spine, implementation conventions, architecture memlog, authoritative `VALIDATION-REPORT-2026-09-12.md`, bound PRD, `epics.md`, both registers, repository instructions, root manifests, exact root-declared gitlinks, relevant current source, and all available declared local sources. I traced the literal FR-8 acceptance sequence; dependency calls and same-owner effect commits; generated negative outcomes; operator and Conversation deletion origins; migration invalidation/repair; story evidence metadata; open decisions and assumptions; export lifecycle; and current-versus-target statements. Product-set behavior and unresolved Product/Governance outcomes were not replaced by architecture guesses.

The supplied SHA-256 values matched at intake and again after this report was written:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `64bb2d2ee93a41b81a2e80de9b5a34697935f8adec617d0aea1596e031fc47ed` |
| `IMPLEMENTATION-CONVENTIONS.md` | `b87de892ed928681ef2060e2523c2da7c035b8ccb3339d154e6254345b03a7a3` |
| architecture `.memlog.md` | `64587e2da955fd8bff0a00435bb736977a67784418cfc1145e4c0c91d14646b6` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `3785263498741896fa7a3f00cf226efdf39f90edab45a237cff11569160f0369` |
| `external-dependency-register.md` | `219ad266c8fe665f27f981a3992217d7c95c293a9297f742cfc904cc7cb3efbf` |
| `launch-readiness-register.md` | `ef5de17f6fb76fe9b26656752723dbb3d2d132344e853e369564d34fc94bd3da` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The deterministic spine linter returned `ok: true`, `total_findings: 0`. The spine declares 105 sources, 91 of them local. Before creation of this report, the only absent local paths were this report and the two anticipated concurrent v5 specialist reports (`security-data-integrity-v5` and `brownfield-drift-v5`); every other local source resolved. AD IDs are unique and contiguous from AD-1 through AD-31.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 3 |
| Medium | 2 |
| Low | 1 |

## Critical

None. The target contains the necessary fail-closed owners for the reviewed safety, deletion, hold, export, and migration boundaries. The blocking defects are conflicting or incomplete executable contracts rather than wholesale absence of those protective boundaries.

## High

### VC14-H1 — FR-8 Eligible Approver resolution crosses Conversations/Parties without a deletion-cutover lease

**Classification:** target-architecture unit-divergence and deletion-integrity defect; not current implementation debt and not an unresolved Product ordering choice.

**Binding contract.** Product fixes Confirmation-mode Eligible Approver resolution at FR-8 step 6, before Context at step 7 (`prd.md:286-296`). AD-8 requires current Conversations roster/access and Parties human/liveness resolution at call time (`ARCHITECTURE-SPINE.md:269`). Independently, AD-2 says a reserved lease authorizes no dependency read and the same-owner commit must win before any safety, roster, Conversations, or other dependency read; its closed lease kinds are only `WorkflowStart`, `ContextRead`, `ConversationMembership`, `ProviderInvocation`, `ProposalMutation`, and `ConversationPosting` (`ARCHITECTURE-SPINE.md:195`). AD-6 and the conventions repeat that a committed lease must precede every target dependency read (`ARCHITECTURE-SPINE.md:239`; `IMPLEMENTATION-CONVENTIONS.md:19`; `launch-readiness-register.md:246-249`).

**Contradictory literal path.** AD-13 and the executable sequence perform Confirmation-only Eligible Approver resolution as step 6, then reserve and commit `ContextRead` for step 7 (`ARCHITECTURE-SPINE.md:323,614-620`). No `ApproverResolution` lease exists, and the `ContextRead` commit occurs after the roster/Party resolution. The matrix likewise has no row that binds those dependency observations to a committed lease.

**Failure construction.** A workflow begins the Conversations roster/Parties resolution at step 6. `InstallBarrierClosing` wins on `ConversationAgentState` while those reads are in flight. Because they have no reserved or committed lease, the finite Closing manifest cannot name or wait for them; convergence can terminalize the permitted interaction and reach `Effective` while the unmanifested dependency read continues. The next leased phase will fail closed, so this does not by itself authorize a proposal or post, but it contradicts the spine's claimed no-read effective cut and gives separately built units incompatible authorization rules.

**Required correction.** Preserve Product's fixed step-6/step-7 order, but give step 6 one explicit same-owner lease contract. Either add a closed `ApproverResolution` lease kind and matrix/result/settlement variant or deliberately broaden/rename the pre-step-7 lease so its committed revision authorizes the exact Conversations and Parties resolution before Context begins. Update AD-2, AD-6, AD-8, AD-13, conventions, matrix, sequence, Story 6.1, recovery, and Closing-before/after-commit tests together. This is authorization mechanics; no Product outcome must be invented.

### VC14-H2 — Initial generated-output safety denial has two incompatible public terminal statuses

**Classification:** cross-AD public-contract contradiction; the bound PRD exposes both values but does not settle this stage-specific mapping.

AD-13 says Provider error, timeout, **or output-safety denial/unavailability** appends `GenerationFailed` on `AgentInteraction` (`ARCHITECTURE-SPINE.md:327`). The sequence and readiness matrix repeat `GenerationFailed` for output-safety denial (`ARCHITECTURE-SPINE.md:652-659`; `launch-readiness-register.md:252`), and Story 6.1 now repeats it (`epics.md:1895`). AD-20 instead says an initial-generation safety failure makes the interaction terminal `SafetyFailed`, then says generated-output safety failure terminalizes on those same initial-versus-regeneration terms (`ARCHITECTURE-SPINE.md:393`). The public contract exposes both `GenerationFailed` and `SafetyFailed` (`prd.md:299-301`; `ARCHITECTURE-SPINE.md:347`), so this is observable, not interchangeable internal naming.

A generated output denied by safety can therefore be projected, counted, retried/released, and tested under different terminal facts depending on which AD a builder follows. The PRD requires a visible authorized safety/failure status and forbids the unsafe output from becoming a message or proposal, but it does not unambiguously assign this exact stage to one of the two status values (`prd.md:320-328,698-710`). Surface that narrow mapping to Product rather than choosing it in Architecture; after approval, align AD-13, AD-20, matrix, sequence, conventions, Story 6.1/6.3, metrics, ledger release, and compatibility tests on the single status and reason taxonomy.

### VC14-H3 — The delivery map still mandates a live separate failure record that the architecture says is structurally absent

**Classification:** bound-PRD/delivery-map authority contradiction; not merely missing implementation.

FR-10 is conditional: **if** failed or incomplete generated content is retained for authorized audit, it must be in a separate non-approvable record (`prd.md:320-328`). The revised AD-13 selects no retention for current V1: it discards failed bytes and says a live `GenerationFailureRecord` identity is structurally absent unless Product later approves a separate create/permit/protection/deletion protocol (`ARCHITECTURE-SPINE.md:327`; `IMPLEMENTATION-CONVENTIONS.md:19`; `launch-readiness-register.md:252`). Story 6.1 now matches that rule (`epics.md:1895`).

The same delivery map still states unconditionally that generation failures create only a separate record (`epics.md:142`); Story 6.3's primary outcome says output denial creates that record, its owned clause is `UX-DR48.separate-failure-record`, and its test artifact is `GenerationFailureRecordTests` (`epics.md:1994,2048-2053`). Stories 6.7 and 7.1 likewise tell authorized users they may reach only that separate record (`epics.md:2300-2303,2416-2419`). The architecture's aggregate inventory omits a live failure-record aggregate (`epics.md:139`), making the conflict internal to `epics.md` as well.

Following the stories creates a content owner and protected/deletion inventory member for which the revised architecture intentionally supplies no live V1 identity/protocol; following AD-13 fails the stories' demonstrable outcomes. Because Product has authorized only the conditional constraint, not retention itself, reconcile the delivery map to content-free `GenerationFailed`/approved stage-specific status and Audit Evidence on `AgentInteraction`, with failure-record/content fields absent. Replace unconditional record wording and artifacts throughout the epics. If Product instead decides to retain failed bytes, record that decision first and add the complete separate owner, directory permit, protection, authorization, retention, export, deletion, and all-copy recovery design before making any story ready.

## Medium

### VC14-M1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

PRD FR-28 requires co-owner-approved literal calendar dates for Architecture-owned readiness assumptions; absence remains a blocker. `ARCH-A-1`, `-2`, `-3`, the test-stack part of `-4`, `-6`, `-7`, `-8`, `-11`, `-12`, and `-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1207-1220`). The spine correctly fails readiness closed, so this is governance incompleteness rather than an unsafe runtime default. Obtain recorded dates or an authorized bounded deferral; do not invent them.

### VC14-M2 — `PostingPending` still has no one concrete timeout authority

AD-5 fixes only a lower bound—no shorter than the committed Conversations seam-2 posting timeout—while `ARCH-A-14` confirms that neither the PRD nor spine fixes the concrete duration or definitive versioned configuration source (`ARCHITECTURE-SPINE.md:221,1220`). Story 7.4 remains blocked, so no unsafe implementation default is currently authorized. Architecture should propose the source, snapshot rule, and compatibility bound for Product confirmation before posting/recovery is ready.

## Low

### VC14-L1 — bUnit is valid but behind the current stable package

The root pins `bunit` `2.9.0`, which the Stack reports accurately, while the current non-authoritative Builds checkout and official NuGet V3 index contain stable `2.10.3`. This is non-blocking test-stack delivery maintenance assigned to Story 5.6, not an architecture defect. Source: [official NuGet package index](https://api.nuget.org/v3-flatcontainer/bunit/index.json).

## Requested Focus Audit

| Focus | Result |
| --- | --- |
| Exact Product-fixed FR-8 order | **Order passes; authorization fails at VC14-H1.** Rate/open is step 5, Confirmation-only Approver is step 6, Context is step 7, descriptor/Budget is step 8, safety is step 9, and separately leased membership is step 10 immediately before acceptance. Directory permit/create/start is explicitly intake, not acceptance. |
| Effect commit before approval/pre-post protected reads | **Pass.** The v13 High is closed: both posting branches commit `ConversationPosting` before pre-post Safety/Conversations reads, and the confirmation branch commits `ProposalMutation` before protected-version/approval-safety reads (`ARCHITECTURE-SPINE.md:674-710`; `launch-readiness-register.md:254-255`; `IMPLEMENTATION-CONVENTIONS.md:19`). |
| Generated failure semantics versus FR-10 | **FAIL at VC14-H2 and VC14-H3.** No proposal/message and byte discard are fail-closed, but public status and separate-record ownership remain contradictory. |
| Operator deletion authority | **Pass.** Only a Platform-Operator request plus distinct Inspector approval and durable workflow-start revision creates the operator-origin capability; candidate/refreeze/accept/acknowledge stays inside approved scope and exact owner/fence revisions (`ARCHITECTURE-SPINE.md:481`; `launch-readiness-register.md:279,281-282`; `epics.md:2957-3027`). |
| Migration epoch repair | **Pass.** Invalidation blocks intake/acquire/commit; repair freezes the finite old-epoch cohort, bridges only exact pending outboxes and committed leases, suppresses/cancels uncommitted work, drains committed outcomes, then atomically activates the successor and revokes the old epoch/bridge before reconciliation restores readiness (`ARCHITECTURE-SPINE.md:197-201`; `launch-readiness-register.md:240-244`; `epics.md:1898-1904`). |
| Story/dependency metadata | **Pass except VC14-H3's failure-contract story authority.** Story 5.5 now includes `EXT-PROVIDER-1`; Story 5.8 retains the conditional legacy-plaintext blocker; Story 6.1 names host/protection/secrets/Parties/Conversations/topology and both decisions; Story 8.3 separates unconditional common dependencies from legacy/export/armed-contention branches. Register consumers match those direct dependencies. |
| Open decisions and assumptions | **Pass with VC14-M1/M2.** The rate/concurrency, Dapr security, recorder scope, legacy plaintext, OQ-31, export lifecycle, hold/deletion precedence, and hold-cancellation outcomes remain Open on their recorded scopes. The architecture does not select them. |
| Architecture changes versus shipped/deferred work | **Pass.** Directory/effect leases, protection engine, migration guard, deletion propagation, decision catalog, Dapr Workflow, export machinery, and public vocabulary are stated as target/deferred; current raw prompt, no-op protection default, direct streams, and post-before-dispatch approval behavior remain implementation debt. |

## Authoritative Validation-Finding Closure Audit

| Authoritative finding | v14 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot/current policies are conjunctive; collapse needs machine-checkable semantic dominance. |
| C-2 hold/deletion exclusion | **Closed at the irreversible ownership boundary.** One `ProtectionFence`, immutable accepted sets, prepare/arm/destroy separation, phase-pinned exports, receipts, and recovery linearize destructive work. VC14-H1 is a remaining read-cut divergence, not loss of the destructive fence. |
| C-3 bootstrap/matrix deadlock | **Closed.** Target-aware Platform/Tenant bootstrap, containment, recorder, migration, repair, and recovery variants exist without circular readiness. |
| H-1 scheduled Approver recheck | **Closed.** Durable cadence/single-flight ownership, two-pass empty evidence, state-specific outcomes, and unavailable retry are explicit. |
| H-2 safety rescan ownership | **Closed.** Durable epoch/index owners, finite manifests, fencing, initialization, and `RescanPending` are bound. |
| H-3 human-only Approvers | **Closed.** Parties-owned human type/liveness and stable actor binding fail closed. |
| H-4 conflated ledgers | **Closed architecturally.** Rate, original-caller concurrency, and monthly monetary reservation have distinct owners/lifetimes; joint consumption remains explicitly Open. |
| H-5 human separation identity | **Closed.** Stable `AuthenticatedHumanActorId` spans Party-bearing and Party-free principals. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append outbox, source high-water, fixed-point reconciliation, and acknowledgement recovery are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Six core seams and optional retraction have separate records, readiness, and consumers. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated bytes, replay owner, issuer-wide nonce, expiry, rotation/revocation, and safe denial spool are bound. |
| H-9 export ownership/lifecycle/signature | **Closed architecturally.** Immutable encrypted store/index, fence commit, canonical ES256 manifest, principal-bound key delivery, phase pinning, purge, and all-copy receipts are explicit while policy stays Open. |
| H-10 current Dapr exposure | **Closed as verified-current truth/debt.** Parent-authoritative Client/ASP.NET `1.18.5`, official/non-authoritative `1.18.7`, and future Workflow adoption are distinguished. |
| H-11 overstated shipped parity | **Closed.** Missing target contracts are classified as delivery debt, never shipped behavior. |
| H-12 tracking/dependency conflict | **Closed as surfaced governance debt.** Sprint history cannot override external/dependency/evidence authority. |

## v13 Finding Closure Audit

| v13 item | v14 disposition |
| --- | --- |
| VC13-H1 approval/pre-post reads before lease commit | **Closed.** Reservation/commit now precedes protected-version, approval-safety, pre-post-safety, and Conversations reads in both branches, with typed negative results and settlement inside the committed lease. |
| VC13-M1 Story 5.5 missing Provider ID | **Closed.** Requirements now names `EXT-PROVIDER-1` consistently with External, Dependencies, Result, and the register. |
| VC13-M2 assumption dates | **Open as VC14-M1.** The blocker remains explicit. |
| VC13-M3 posting timeout | **Open as VC14-M2.** No default was invented. |
| VC13-L1 bUnit lag | **Open as VC14-L1.** Correctly classified as delivery maintenance. |

## Verified Repository And Technology Reality

- Root-authoritative gitlinks remain Builds `a32cb422`, EventStore `ce9e779a`, Conversations `73bcee6f`, Parties `fa423985`, Tenants `2fac1839`, FrontComposer `053b2008`, and Memories `3644ef63`. Initialized Builds `cf52f74`, EventStore `a568af4`, Conversations `64b05083`, FrontComposer `1b3608c`, and Memories `42dfa26` checkout HEADs differ from their parent gitlinks; Parties and Tenants match. The spine does not promote dirty checkout HEADs to root authority. No submodule was initialized, edited, cleaned, or advanced by this review.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; `Directory.Build.props` targets `net10.0` and C# 14; the solution is `.slnx`; Central Package Management and root overrides match the Stack. Official .NET metadata lists current SDK `10.0.401` and runtime `10.0.12`, including the spine's named security floor. Source: [official .NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json).
- Parent-authoritative Builds pins Dapr Client/ASP.NET/Workflow `1.18.5`; parent EventStore Client/DomainService consume Client/ASP.NET. The different Builds checkout pins `1.18.7`, and official NuGet contains stable `1.18.7` for Client, ASP.NET, and Workflow. Current Agents projects consume EventStore transitively and do not reference Dapr Workflow. Sources: [Dapr.Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), [Dapr.AspNetCore](https://api.nuget.org/v3-flatcontainer/dapr.aspnetcore/index.json), [Dapr.Workflow](https://api.nuget.org/v3-flatcontainer/dapr.workflow/index.json).
- Current `InteractionRequested`/interaction state retain raw `Prompt`; the parent-authoritative EventStore default protection service is no-op/unprotected. Current code has no `ConversationAgentState`, effect lease, target protection-key alias, migration fence, deletion barrier, durable Dapr Workflow owner, or target governance aggregates. The spine correctly blocks live content/deletion evidence and assigns implementation work rather than claiming these structures exist.
- Current approval orchestration still posts to Conversations before durable aggregate dispatch. That is accurately listed as Story 7.4 implementation debt and does not weaken the target AD-5 protocol.

## Architecture Defects Versus Implementation Debt

| Item | Classification |
| --- | --- |
| VC14-H1 | Target authorization/deletion-cutover mechanics divergence; Architecture can fix without choosing Product ordering. |
| VC14-H2 | Product-visible status ambiguity across target ADs; Product must confirm the narrow mapping before Architecture synchronizes all consumers. |
| VC14-H3 | Cross-artifact delivery-authority contradiction; remove unconditional retention semantics unless Product first authorizes retention and Architecture adds the missing protocol. |
| VC14-M1/M2 | Explicit governance/architecture inputs that already fail closed; do not invent dates or timeout policy. |
| Missing directory, leases, protection, migration guard, durable deletion feed, export, decision catalog, and workflows | Correctly stated implementation/external delivery debt assigned to stories/register records. |
| Current plaintext/direct-stream/post-before-dispatch behavior | Correctly stated brownfield debt, not target architecture truth. |
| Dirty/different submodule checkout HEADs | Correctly separated from exact root gitlink authority; no repository update is implied. |
| bUnit lag | Low build/test maintenance debt. |

## Final Hash, Source, And Lint Verification

**PASS for freeze integrity and deterministic lint; FAIL for the semantic reviewer gate.** All eight reviewed input hashes exactly matched the supplied values after the report write. A final `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` returned `ok: true`, `total_findings: 0`, with no severity entries. After this report was created, only the two anticipated concurrent specialist paths remained absent from the declared local source chain. Mechanical/source checks do not override `VC14-H1` through `VC14-H3`.
