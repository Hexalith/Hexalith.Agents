# Brownfield-Reality Drift Review — 2026-09-12 (v2)

## Verdict

**FAIL — the target architecture is mostly honest about its implementation backlog, but the final spine falsely labels future contract vocabulary as current and the authoritative sprint tracker contradicts the current dependency/evidence gates for Stories 5.1 and 5.2.**

Finding count: **2 High, 1 Medium**. The catalog split, versioned route, runtime binding, missing integration tier, legacy identities, and other named migrations are intentionally assigned debt and are not counted as defects by this review.

## Scope And Method

- Compared `../ARCHITECTURE-SPINE.md` with the current `src/`, `test/`, root configuration, `epics.md`, `sprint-status.yaml`, external-dependency register, and architecture memlog.
- Verified the checked-in sibling dependency commits named by the spine and inspected the root SDK/package boundary. The pinned root dependency revisions match; `global.json` is on `10.0.401` with `latestPatch`; no new technology-version drift was established.
- Inspected current public contracts, HTTP route, ProviderCatalog projection address, structural/runtime conformance tests, verification scripts, and story/dependency tracking. This was a read-only inspection; no build or test result is newly claimed by this review.
- The spine and every existing artifact were left unchanged. This report is the only file created.

## High

### H-1 — The spine's “current” public/operational parity claims describe backlog work, not the code on disk

**Classification: spine defect, not implementation debt.**

**Evidence**

- The AD-15 rule correctly says UX-required contracts grow under the stories that own them (`ARCHITECTURE-SPINE.md:256-260`). The next two paragraphs instead assert that the **current** public surface already carries all five membership states, `BlockVersion`, `CurrentMirror`, `MirrorPending`, `MirrorRefused`, `NoEligibleApprover`, resolution states, `LateConfirmed`, `ExpiredWhileSuspended`, every readiness blocker, capacity outcomes, `DataHandlingAcceptanceLapsed`, `DependencyNotAvailable`, and `PayloadProtectionUnavailable` (`ARCHITECTURE-SPINE.md:262-264`).
- The current `AgentReadinessStatus` contains only `Unknown`, `Callable`, `Checking`, `InvalidConfiguration`, `MissingPartyIdentity`, `ProviderUnavailable`, and `Disabled`; it has none of the claimed `ActiveNotProvenCallable`, `Stale`, or `AuthorityUnresolved` sibling state (`src/Hexalith.Agents.Contracts/Operations/AgentReadinessStatus.cs:7-29`).
- The current interaction status is the older requested/authorized/blocked/generation/proposal lifecycle (`src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs:18-96`). Focused repository searches find no source declaration for `CurrentMirror`, `BlockVersion`, `NoEligibleApprover`, `ApproverResolutionUnavailable`, `ResolutionEmptyPending`, `ResolutionUnavailable`, `RemovedInConversations`, `SourceConversationUnavailable`, `LateConfirmed`, `ExpiredWhileSuspended`, `DataHandlingAcceptanceLapsed`, `TenantSuspended`, `CapacityQueued`, `CapacityRejected`, `DependencyNotAvailable`, or `PayloadProtectionUnavailable`.
- This absence is intentional and assigned: Story 6.6 owns membership/mirror shapes, Story 7.1 owns approver-resolution values, Stories 7.4/7.5 own lookup and `LateConfirmed`, and Stories 7.6/8.5 own `ExpiredWhileSuspended` (`epics.md:365-376`). The same table assigns payload protection to 5.6/5.8 and context/runtime outcomes to 6.x (`epics.md:364-368`).

**Why this matters**

The implementation backlog is not the problem. The defect is the spine's brownfield assertion: a reader can treat these paragraphs as ratification that public-contract migrations have landed, skip the owning stories' work, or cite nonexistent types as current integration contracts. That defeats the purpose of a final brownfield architecture substrate.

**Action: AUTOFIX.** Change the headings and prose to unambiguously describe **required completion parity**, not current parity, and point to the owning epics/debt rows. Do not accelerate the already-assigned code work. If any subset is intended to be genuinely current, list only the symbols actually present and keep the remainder explicitly future-owned.

### H-2 — Sprint state contradicts the current dependency and evidence authority for Stories 5.1 and 5.2

**Classification: tracking defect; architecture rule is clear.**

**Evidence**

- The spine states that consumers of an `Uncommitted` external record remain blocked from `ready-for-dev` (`ARCHITECTURE-SPINE.md:819-836`). The dependency register currently records `EXT-HOST-1` as `Uncommitted` with target, date, and command still `TBD` (`external-dependency-register.md:105-119`), and summarizes all ten records as `Uncommitted` (`external-dependency-register.md:231-233`).
- Current Story 5.1 says `EXT-HOST-1` must be at least Committed before ready-for-dev (`epics.md:1238-1242`), explicitly requires the story to remain backlog while that record is Uncommitted (`epics.md:1261-1264`), and records `Result | Not run — backlog` (`epics.md:1266-1277`). Its declared command is `pwsh ./eng/verify-story-5.1.ps1` (`epics.md:1275`), but the tracked `eng/` inventory contains only `verify-story.ps1`, `verify-story-5.2.ps1`, and `verify-story-5.3.ps1`; the 5.1 script does not exist.
- Nevertheless, `sprint-status.yaml` marks Story 5.1 `done` and Story 5.2 `in-progress` (`sprint-status.yaml:86-89`). Current Story 5.2 depends on 5.1 (`epics.md:1287-1291`) and its own evidence result still says `Not run — backlog` (`epics.md:1325-1336`). Story 5.2 does permit Branch-B-compatible development while `EXT-PARTIES-1` is Uncommitted, but expressly says that work is not launch evidence (`epics.md:1289-1291`); that exception does not reconcile the unmet current 5.1 prerequisite or the two artifacts' conflicting states.

**Why this matters**

The tracker can no longer be used as an authoritative implementation order/evidence ledger. A downstream session can treat 5.1's unexecuted manifest as satisfied and advance 5.2 or later stories without the dependency exception, evidence, or historical completion basis being visible.

**Action: DISCUSS.** Reconcile history before editing statuses: either (a) roll 5.1 back to the state required by the current story/register and hold 5.2 accordingly, or (b) record an owner-approved historical-completion/changed-dependency exception, provide an executable replacement for the missing 5.1 verification command, and amend the current story evidence so `done` is truthful. Then make the 5.2 status and evidence row agree. This is a tracking/evidence repair, not a demand to implement future runtime capabilities.

## Medium

### M-1 — Current “conformance” tests still prove the superseded seed and AD-18/AD-19 meanings

**Classification: intentionally tracked implementation debt; defer to Story 5.6.**

**Evidence**

- The final spine puts aggregates in `src/Hexalith.Agents`, omits `Server/Aggregates` and `Application/Tools` from the Structural Seed (`ARCHITECTURE-SPINE.md:579-630`), makes Dapr Workflow the sole V1 execution owner (`ARCHITECTURE-SPINE.md:280-284`), and says V1 reserves no tool folder (`ARCHITECTURE-SPINE.md:286-290`).
- `StructuralSeedConformanceTests` still requires both `Aggregates` and `Application/Tools` (`test/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs:39-49`, `test/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs:111-122`). Both folders remain on disk; the former describes itself as an unused original-seed placeholder (`src/Hexalith.Agents.Server/Aggregates/README.md:1-16`) and the latter contains only `.gitkeep`.
- `RuntimeOwnershipConformanceTests` says SDK packages may eventually live in module `.AppHost/.Aspire` projects (`test/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs:52-62`) and requires the empty Tools extension point while calling MCP/A2A/tool schemas deferred (`test/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs:284-305`). The traceability manifest likewise labels AD-16 “Module-local operational topology”, AD-18 “Hybrid runtime ownership”, and AD-19 tool schemas deferred (`test/Hexalith.Agents.Server.Tests/Conformance/traceability-manifest.json:61-64`), all superseded by the final spine.
- This exact cleanup is already assigned to backlog Story 5.6, whose AC forbids both folders and whose evidence names `StructuralSeedConformanceTests` (`epics.md:1573-1594`). The architecture memlog also records that ownership explicitly (`.memlog.md:137`).

**Why this matters**

Until Story 5.6, a green structural/runtime “conformance” result demonstrates the old contract, not the final architecture. That is misleading evidence, but it is not untracked debt and it does not justify pulling Story 5.6 forward.

**Action: DEFER.** Keep this item on Story 5.6. In the interim, evidence reports must label these tests as legacy/partial and must not cite them as proof of current AD-16/AD-18/AD-19 conformance. Story 5.6 should remove both folders, reverse the outdated assertions, and update the traceability titles/semantics together.

## Intentionally Tracked Debt — Not Findings

| Observed brownfield state | Target spine rule | Existing owner/disposition | Classification |
| --- | --- | --- | --- |
| ProviderCatalog read-model keys remain tenant-scoped (`src/Hexalith.Agents.Server/Projections/ProviderCatalogReadModelAddresses.cs:5-27`) | Platform catalog under `system` plus tenant enablement (`ARCHITECTURE-SPINE.md:517-521`, `ARCHITECTURE-SPINE.md:794-800`) | Story 5.3 and `NC-5.3-PLATFORM-CATALOG-SCOPE`; sprint tracker explicitly explains why it remains backlog (`sprint-status.yaml:89`) | **DEFER** |
| Public HTTP group is still `/api/agents/operations` (`src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs:18-29`) | `/api/v1/agents/...` (`ARCHITECTURE-SPINE.md:533`) | Schema/stream convention identifies the unversioned-route migration; Story 5.5 owns it | **DEFER** |
| Lifecycle configuration-version bump and Party link/replace rejection are incomplete | Final Agent identity/lifecycle and deprecate-and-reject contract | Story 5.2 is explicitly in progress and its tracker annotation names the lifecycle gap (`sprint-status.yaml:88`; `epics.md:1310-1318`) | **DEFER** |
| No module Dapr/Provider/Agent-Framework owner or IntegrationTests project exists | Platform-owned host and Dapr Workflow execution (`ARCHITECTURE-SPINE.md:266-284`); IntegrationTests created by 5.6 (`ARCHITECTURE-SPINE.md:623-630`) | Correct fail-closed current seam; Stories 5.6 and 6.1 own live binding/tests | **DEFER** |
| Context blocking currently exposes coarse `ContextUnavailable` (`src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionContextBlockReason.cs:10-32`) and legacy generation/proposal identities remain | Expanded context/runtime outcomes and AD-29 identities | Stories 6.2, 6.4, 7.1, and 7.3 plus the open action assignment (`sprint-status.yaml:166-168`) | **DEFER** |

## Ratification Summary

- **Spine defect:** H-1. The target rule may be sound, but its “current” brownfield claim is false.
- **Tracking/evidence defect:** H-2. The spine's dependency rule is clear; sprint, story, register, and executable evidence do not agree.
- **Tracked debt:** M-1 and every item in the debt table. These should not be demanded outside their assigned stories.
- **Confirmed current alignment:** root package/project direction, absence of module-owned AppHost/Aspire/ServiceDefaults projects, fail-closed unbound runtime seams, SDK pin, and sibling dependency commits are consistent with the target architecture.

## Gate Recommendation

Do not use the artifact set as an authoritative current-state handoff until H-1's wording and H-2's tracker/evidence contradiction are reconciled. No broad implementation catch-up is required by this review; leave the named migrations with their assigned stories, and treat the legacy conformance tests as partial evidence until Story 5.6 replaces them.
