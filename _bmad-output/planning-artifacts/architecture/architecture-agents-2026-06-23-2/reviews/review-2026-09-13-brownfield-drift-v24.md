---
review: brownfield-drift-v24
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: frozen-read-only-specialist-review
lens: ad-hoc brownfield reality / implementation drift
verdict: PASS
critical: 0
high: 0
medium: 2
low: 1
lint_ok: true
---

# Brownfield-Reality Drift Reviewer Gate — v24

## Verdict

**PASS — 0 Critical, 0 High, 2 Medium, and 1 Low.** The frozen v24 architecture package accurately separates its target Dapr-Workflow, protection, governance, and deletion contracts from the much smaller shipped Agents implementation. Root-authoritative gitlinks, the distinct locally checked-out submodule heads, current source/package dependency modes, named package versions, conditional live bindings, missing target mechanisms, and all `Uncommitted` external dependencies are represented without turning adapter presence into readiness evidence.

The matrix-v7 and Story 8.3 handoffs remain internally consistent and explicitly deferred. No current code is falsely claimed to implement the Conversation-deletion intake/feed, admission/effect cut, shared hold/seal guard, continuous content fence, manifest-batch destruction, compromise registrar, protection-owner arbitration, or completion barrier. The two Mediums are already fail-closed cross-artifact authority gaps and the Low is package maintenance; none reopens an authoritative Critical/High finding.

## Frozen Snapshot And Method

I read the complete repository instructions, BMad architecture skill, and reviewer-gate instructions before review. I inspected the frozen spine, conventions, authoritative validation report, bound PRD, Epics delivery map, external and launch registers, root manifests, solution/project files, relevant current Agents source and tests, the five cited sibling project contexts, parent-authoritative git objects, initialized checkout heads, and official package/platform metadata. Per task constraint, the architecture `.memlog.md` content was **not read**; only its hash was computed.

The report path was absent at review start. The following supplied hashes were recomputed before analysis and matched exactly:

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `3789b957d76c27c01ec363fc396335773014a7e9580fc5193c4ee3b98f388b82` |
| `IMPLEMENTATION-CONVENTIONS.md` | `ea1d311da8bef578c7fca428d6b66ef7ae91dc593eb5b3efc9cac382e2a938d9` |
| architecture `.memlog.md` — hash only, content unopened | `890d18b355b6b7d475241426641701ebe724dd6906bed15fa180a5e14a0e17e0` |
| bound `prd.md` | `4fbc21e13301bb47c87e077ec7aa884ff698121837b0e19cbf6e0ea6e4334564` |
| `epics.md` | `15f208cd8e2270dfb82352c5d7303b083ffd8f64c91ae40013a946fae6f69982` |
| `external-dependency-register.md` | `3f29c4651eb79e475bd087fff402903652984218b1c66f1d26cd86dcfbb5a4f3` |
| `launch-readiness-register.md` | `9c747bc0329da6a38c4dc56eae3e75e7408d85cb9bb91a2365547ea0ee40f3c6` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. No submodule was initialized, updated, or modified. This report is the only file written by this review.

## Deterministic Gate

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS**, `ok: true`, zero findings. An independent heading scan found exactly one each of AD-1 through AD-31, in ascending order, with no gap, duplicate, reuse, deletion, or renumbering.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 2 |
| Low | 1 |

## Critical

None.

## High

None.

## Medium

### BD24-M1 — Architecture-owned `RQ-1` assumptions still lack literal approved dates

PRD FR-28 item 9 and §8.1 require each Architecture-owned assumption whose affected scope includes `RQ-1` to carry a co-owner-approved literal calendar date. The spine accurately exposes multiple unretired `ARCH-A` rows as `Unscheduled`, including ARCH-A-1 through ARCH-A-4 where still open, ARCH-A-6 through ARCH-A-8, ARCH-A-11, ARCH-A-12, and ARCH-A-14 (`ARCHITECTURE-SPINE.md:1381-1398`). The PRD likewise records Architecture-owned A-7, A-8, A-20, and A-24 without an approved date. Because missing dates remain explicit blockers and no implementation may infer them, this is Medium rather than High. Obtain the named co-owner approvals and record literal dates; Architecture must not invent them.

### BD24-M2 — `PostingPending` has no concrete timeout authority

FR-18 requires the durable posting-attempt deadline to be no shorter than the exact `EXT-CONV-AI-1` seam-2 posting timeout, but neither the PRD nor AD-5 selects a literal duration or one exact versioned configuration field (`ARCHITECTURE-SPINE.md:291,1398`). Story 7.4 correctly blocks on retiring ARCH-A-14, so current code is not authorized as target-conformant posting/recovery. Bind one literal or exact versioned seam/profile authority and obtain the required Product confirmation before that work becomes ready-for-dev.

## Low

### BD24-L1 — Root bUnit remains behind the official current stable package

Root authority and `Hexalith.Builds@a32cb422` pin bUnit `2.9.0`; the separately checked-out, internally clean Builds head `cf52f74` and the official NuGet version index expose stable `2.10.3`. The spine accurately reports `2.9.0` and assigns alignment to Story 5.6/build maintenance (`ARCHITECTURE-SPINE.md:920,1354`). This is delivery maintenance, not a target-architecture or current-reality misstatement.

## Parent Authority Versus Local Checkout Reality

| Submodule | Root-authoritative gitlink | Initialized checkout | Brownfield disposition |
| --- | --- | --- | --- |
| Hexalith.Builds | `a32cb422` | `cf52f74c` | Checkout differs but is internally clean; root catalog remains authority. |
| Hexalith.Conversations | `73bcee6f` | `64b05083` | Checkout differs but is internally clean; required AI/deletion seams remain absent at the parent gitlink. |
| Hexalith.EventStore | `ce9e779a` | `a568af4e` | Checkout differs but is internally clean; parent source exposes protection hooks and the no-op-compatible default. |
| Hexalith.FrontComposer | `053b2008` | `1b3608c9` | Checkout differs but is internally clean; UI boundary/source rules remain advisory context, not a new target. |
| Hexalith.Memories | `3644ef63` | `42dfa26b` | Checkout differs but is internally clean and is not promoted to an Agents architecture authority. |
| Hexalith.Parties | `fa423985` | `fa423985` | Matches. |
| Hexalith.Tenants | `2fac1839` | `2fac1839` | Matches. |

The spine's Stack and debt ledger use the gitlinks rather than the checkout heads for current-version claims. Normalizing line endings, each cited Conversations, EventStore, Tenants, Parties, and FrontComposer `_bmad-output/project-context.md` working file is byte-equivalent to the version at its parent-authoritative gitlink. Thus the source chain does not accidentally import a later checkout-only project-context rule.

## Shipped Source And Live-Seam Audit

### Correctly represented shipped subset

- Root `global.json` pins SDK `10.0.401` with `latestPatch`; `Directory.Build.props` sets `net10.0`, C# 14, nullable, implicit usings, and warnings-as-errors. Microsoft's official .NET 10 metadata identifies `10.0.12` and SDK `10.0.401` for 2026-09-08. The spine's current SDK statement is accurate.
- `Hexalith.Agents.slnx` contains six source projects and five test projects. It contains no Agents-owned AppHost, Aspire helper, ServiceDefaults project, or `Hexalith.Agents.IntegrationTests`; Story 5.6 and `EXT-HOST-1` correctly own those deferred platform/evidence concerns. Existing conformance tests explicitly reject module-owned hosting.
- Agents Contracts depends only on EventStore Contracts; the domain depends on EventStore Client; Server depends on EventStore DomainService and keeps Parties/Conversations behind the Server boundary. FrontComposer appears only at the UI boundary. These project directions match AD-15 and the cited sibling contexts.
- With a valid `Agents:EventStore:BaseUrl`, `AddAgentSetupServices` replaces the deferred dispatcher and binds shipped setup/catalog projection paths. Without it, the dispatcher and public client remain fail-closed. The Live-Seam Matrix narrowly labels the shipped setup/catalog subset `Live`, while platform catalog migration, tenant enablement, the integration project, and their complete evidence stay deferred to Stories 5.3/5.6.
- Debug/source mode can compile Conversations context/post adapters when a `Conversations` section exists. The parent-authoritative Conversations client exposes create, append, read, list, and project reassignment, but no AI-membership establishment/removal, deletion feed/acknowledgement, active-count feed, or message-existence seam. The current poster itself returns `SeamUnavailable` when membership is absent. The register therefore keeps Conversations membership/posting and deletion propagation `Deferred`, `EXT-CONV-AI-1` remains `Uncommitted`, and the spine explicitly says configuration or adapter presence is not Live/readiness evidence (`ARCHITECTURE-SPINE.md:1344`).

### Correctly represented missing target mechanisms

Focused source/test searches found no current `ArchitectureDecisionCatalog`, `TrustedEnvelopeReplay`, `RateLimitLedger`, `OpenInteractionLedger`, `BudgetLedger`, `SafetyVerdictEpoch`, `ConversationAgentState`, `GovernanceScopeGuard`, `ProtectionFence`, `DeletionBatchCapability`, `IDeletionCapabilityCompromiseRegistrar`, `AuditExport`, or `ProtectedDeletion`. No Agents project references `Dapr.Workflow`.

The parent-authoritative EventStore source consumes Dapr Client from Client and Dapr ASP.NET from DomainService, registers `IEventPayloadProtectionService` with a no-op default when no stronger provider is supplied, and has no implicit production implementation of the target guard/protection-owner transaction set. Current Agents `Program.cs` calls `AddDaprClient`; its approval orchestrator performs the Conversations append before EventStore dispatch, while the current pure policy can emit Approved, PostingPending, and the posting result together. The spine records each of these facts as delivery debt, never as acceptable target behavior (`ARCHITECTURE-SPINE.md:1334-1354`).

This is a sound brownfield handoff: target mechanics stay normative; absent code stays assigned debt; and current behavior cannot silently redefine an AD or an Open Product decision.

## Package And Technology Verification

- `Hexalith.Builds@a32cb422` pins Dapr Client, ASP.NET, and Workflow `1.18.5`; the separate checkout `cf52f74` pins all three at `1.18.7`. Official NuGet flat-container indexes expose `1.18.7` as the newest stable family with later `1.19.0` entries prerelease. The spine correctly distinguishes present transitive Client/ASP.NET exposure from future Workflow adoption and keeps ARCH-A-15 open.
- Root-selected versions match the architecture table: Fluent UI Blazor `5.0.0-rc.5-26219.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, and bUnit `2.9.0`; the imported catalog exposes MediatR `14.2.0`, FluentValidation `12.1.1`, and OpenTelemetry `1.18.0`. The spine says MediatR is absent from the current Agents runtime graph and leaves Provider and Agent Framework SDKs `Unselected`; source/project inspection confirms those distinctions.
- Official primary verification used the .NET 10 release metadata and NuGet package/version indexes for Dapr Client, Dapr ASP.NET, Dapr Workflow, bUnit, and OpenTelemetry. No secondary technical source was needed.

## Matrix-v7 And Story 8.3 Handoff Audit

The current register section is `OperationGateMatrixVersion 7`, and the normative table now correctly labels its column `Required GateIds in v7`. Older v1-v6 references are explicit immutable history or retained fixture descriptions, not rival current assignments.

Story 8.3, AD-22/AD-30, implementation conventions, the external dependency register, and matrix-v7 agree on the following target-only handoff:

1. Both deletion origins require Story 6.1's current directory/migration/repair guard and the same ordinal admission/effect cut before candidate construction.
2. `EXT-HOST-1` owns atomic EventStore fence and guard effects; `EXT-PROTECTION-1` owns atomic all-or-none manifest consumption and protection-owner blocked/reserved/re-attested/consumed/revoked state; `EXT-SECRETS-1` owns signing and revocation delivery; `EXT-CONV-AI-1` owns durable source signal/feed/acknowledgement.
3. A stable pre-seal `DestructionSealId` survives stale signing retries; actual committed guard/issue revisions are result evidence, not identity inputs. Signed-but-unissued attempts require exact no-issue proof before terminalization.
4. Replacement-key activation atomically compares the replacement key's block-set revision and returns activated-or-blocked; no activation can clear a global compromise block.
5. Post-arm/pre-seal and every post-start hold branch stay governed by the still-Open `OD-HOLD-DELETION-PRECEDENCE-1`; ordinary no-contender deletion remains evaluable. Story 8.3 chooses no Product outcome.
6. Story 8.3 remains backlog/blocked: its common external targets are `Uncommitted`, OQ-31 instruction protection blocks story authorization unconditionally, and class/range, human exact-Conversation, operator nonterminal, operator cancellation, legacy-plaintext, export-bearing, and hold-contention branches retain only their named conditional blockers.

Every corresponding Live-Seam row remains `Deferred`. The evidence list includes admission-fence/phase races, repeated recut gap chains, immutable owner/ordinal cycles, continuous content binding, hold/seal compare-token staleness, non-expiring batch restore, guard-owned containment issuance, multi-key all-or-none destruction, compromise/re-attestation, and completion sealing. Current source implements none of this and the architecture never implies otherwise.

## Authoritative 2026-09-12 Critical/High Closure Recheck

| Finding | Brownfield v24 disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed architecturally.** Snapshot and every applicable current policy are conjunctive; a collapse requires a machine-verifiable dominance proof. The missing runtime epoch/index remains explicit delivery debt. |
| `C-2` hold/deletion exclusion | **Closed architecturally.** Canonical scope, ordinal admission/effect cut, owner cycles, current zero proof, continuous content guard, shared hold/seal ordering, atomic manifest batches, protection-owner arbitration, and guarded completion are target requirements; none is falsely claimed shipped. |
| `C-3` bootstrap/matrix deadlock | **Closed.** Matrix v7 has closed bootstrap/repair/recorder variants and target-aware scope. Missing implementation is deferred rather than bypassed. |
| `H-1` to `H-3` — Approver schedule, safety rescan, human classification | **Closed.** Durable target protocols and owners are specified; current absence is assigned to Stories 5.4, 6.3, 6.6, and 8.4 plus external seams. |
| `H-4` to `H-6` — ledger lifetimes, stable human identity, proposal/index recovery | **Closed.** Separate owners/lifetimes, stable human evidence, and directory/outbox/lease recovery are target contracts; their absence is accurately catalogued. |
| `H-7` to `H-9` — dependency split, trusted envelope, export | **Closed.** Core/optional Conversations dependencies are separate; envelope replay/security recording is explicit; export store/index/key authorize-effect-result/copy lifecycle is bound while Product lifecycle remains Open. |
| `H-10` Dapr current exposure | **Closed.** Parent `1.18.5` transitive Client/ASP.NET exposure and future Workflow adoption are distinct; the local `1.18.7` checkout is not promoted to root authority. |
| `H-11` public-contract truth | **Closed.** Required target vocabulary is separated from incomplete shipped contracts and owned by delivery stories. |
| `H-12` sprint/dependency contradiction | **Closed as surfaced delivery/history debt.** The sprint discrepancy and missing verifier remain under `OD-SPRINT-5.1-5.2-1`; no AD or external record is inferred from tracker state. |

## Product Authority And Delivery Debt

No Product or governance outcome was selected or inferred. Initial-output status, rate/concurrency consumption, hold/deletion precedence, hold-prepare cancellation, operator-deletion cancellation/nonterminal handling, export lifecycle, historical-safety policy, automatic retraction, instruction protection, legacy plaintext disposition, class/range scope, human exact-Conversation scope, release-recorder authority, Dapr security posture, and sprint reconciliation remain explicit Open decisions with affected-evaluation scopes and fail-closed behavior.

The architecture/debt boundary is sufficiently explicit despite the target-oriented Design Paradigm synopsis: Stack says Dapr Workflow is not consumed, Hosting and Provider/Agent Framework SDKs are unselected, Live-Seam rows stay Deferred where evidence is absent, the external register keeps all twelve records `Uncommitted`, Epics mark dependent stories backlog/blocked, and the dedicated debt table names current source mismatches. No build, test, topology, dependency-availability, Story 8.3, or release success is claimed.

## Gate Conclusion

The frozen v24 ad-hoc brownfield-drift gate is **PASS**. Final counts are **Critical 0, High 0, Medium 2, Low 1**. Deterministic lint passes, AD-1 through AD-31 remain stable, the frozen inputs remained unchanged, and the target architecture is reconciled with parent-authoritative gitlinks and current code reality without converting implementation debt or unresolved Product decisions into architectural permission.
