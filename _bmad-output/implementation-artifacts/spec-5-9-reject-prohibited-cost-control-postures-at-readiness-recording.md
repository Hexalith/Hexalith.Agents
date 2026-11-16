---
title: '5.9 Reject prohibited cost-control postures at readiness recording'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
story_key: '5-9-reject-prohibited-cost-control-postures-at-readiness-recording'
review_loop_iteration: 0
baseline_commit: '9edf5240457ebd430c23592c47ddffb313c67481'
human_approval: 'accepted preparatory scope; user replied yes on 2026-11-16'
sprint_sync: 'preserve-backlog-full-story-incomplete'
completion_scope: 'approved preparation only; full Story 5.9 remains incomplete'
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Existing readiness recording accepts reporting-only monitoring, accepted launch risk and undefined in-memory enum values. Legacy prohibited readiness can clear inspection and an already-enabled Agent bypasses enablement validation.

Reject prohibited postures before accepted events or idempotent success, retain their historical identities, and expose a distinct actionable blocker. Only Quotas, Budgets and ProviderModelLimits may continue to remaining readiness checks.

## Boundaries & Constraints

Preserve authorization order, historical events, enum names/ordinals, unknown fallback serialization, and English/French parity. Reject commands through typed domain rejections; replay retains historical data without granting new authority. Append `ProhibitedCostControlPosture=9`; do not renumber existing blockers. Undefined values fail closed.

This proposed preparation targets existing contracts, aggregate, inspection, replay/projection and rendered UI. Stories 5.5 and 5.7 remain prerequisites for authoritative live readiness and Provider exclusion proof. Keep Story 5.9 incomplete and its sprint entry in backlog until those paths and full conformance exist. No dependency status, runtime availability or live evidence claim changes. Do not implement prerequisite stories or widen invocation contracts in this preparation.

Approved decision: implement the preparatory guards now; full Story 5.9 remains incomplete pending Stories 5.5/5.7.

## I/O & Edge-Case Matrix

| Input/state | Expected behavior |
| --- | --- |
| Either prohibited posture submitted, including identical retry | Typed rejection; no accepted event or version change |
| Legacy prohibited readiness, including previously enabled | Preserve replayed value; distinct blocker; reject enablement before idempotent success |
| Quotas, Budgets, ProviderModelLimits | Continue remaining gates; valid retries remain idempotent |
| Missing, Unknown, undefined value | Reject recording; inspection/enablement remain blocked |
| Unauthorized caller | Deny before state disclosure or mutation |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.Contracts/Agent/CostControlPosture.cs`, `AgentLaunchReadinessBlocker.cs`: stable public enums. Existing `Serialization/UnknownFallbackEnumConverter.cs` preserves declared legacy values; reuse unchanged.
- `src/Hexalith.Agents/Agent/AgentAggregate.cs`: recording validates before equality; enablement returns early on the enabled flag. `AgentLaunchReadinessPolicy.cs` is shared by `AgentInspection` and `AgentLaunchReadinessInspection`.
- `src/Hexalith.Agents/Agent/AgentState.cs` and `src/Hexalith.Agents.Server/Projections/AgentSetupProjectionFold.cs`: historical folding and persisted projection; retain raw legacy posture and versions.
- `src/Hexalith.Agents.Server/Application/Agents/EventStoreAgentAdministrationOperations.cs`: readiness recording/enablement return Unavailable. UI uses `DeferredLaunchReadinessGateway`. Invocation port `src/Hexalith.Agents.Server/Ports/AgentInvocationReadiness.cs` exposes only an enabled boolean. None supplies live cost-posture proof.

## Tasks & Acceptance

- [x] `src/Hexalith.Agents.Contracts/Agent/CostControlPosture.cs`, `AgentLaunchReadinessBlocker.cs`: obsolete prohibited members, correct documentation and append blocker. Keep serialization compatibility. Reuse typed `AgentLaunchReadinessRejection` and `AgentProductionLikeGenerationBlockedRejection`.
- [x] `src/Hexalith.Agents/Agent/AgentLaunchReadinessPolicy.cs`, `AgentAggregate.cs`: share explicit accepted/prohibited/unknown classification, reject invalid writes before normalization/equality, and evaluate blockers before enablement no-op. Avoid introducing a second classification table.
- [x] `src/Hexalith.Agents.UI/Components/Shared/LaunchReadinessPresentation.cs` and `src/Hexalith.Agents.UI/Resources/AgentsResources.resx`, `AgentsResources.fr.resx`: map the new blocker and explain safe migration to hard controls; label historical prohibited values clearly.
- [x] `test/Hexalith.Agents.Tests/AgentLaunchReadinessTests.cs`: cover the matrix, inspection parity, unauthorized paths, no accepted mutation and legacy-enabled retries.
- [x] `test/Hexalith.Agents.Contracts.Tests/AgentContractsRoundTripTests.cs`, `AgentOperationContractsTests.cs`: verify names, ordinals, obsolescence, legacy numeric/name input and unknown fallback. Scope intentional obsolete references narrowly; preserve warnings-as-errors.
- [x] `test/Hexalith.Agents.Server.Tests/AgentSetupProjectionTests.cs`, `AgentsOperationEndpointsTests.cs`: verify persisted legacy end-state, transport compatibility and unchanged Unavailable operations. Substituted transport tests are not live evidence.
- [x] `test/Hexalith.Agents.UI.Tests/LaunchReadinessPresentationTests.cs`, `LaunchReadinessSurfaceTests.cs`, `LocalizationResourceTests.cs`: verify blocker visibility, disabled enablement and both languages.
- [x] `eng/verify-story-5.9.ps1`: provide reproducible focused and owning-project regression checks with explicit preparation-only evidence; report unmet full-story gates without claiming completion.

Given either prohibited posture, when recording, inspecting or enabling through existing policy paths, then accepted mutation is refused and inspection retains its distinct migration blocker. Given historical events, when full or incremental projection replays, then persisted posture and versions remain faithful. Given successful preparation checks, when reporting results, then full Story 5.9 and live/provider conformance remain incomplete.

## Implementation Notes

2026-11-16: User approved preparation and continuation. Preserve the full story backlog entry as explicitly agreed; generic sprint synchronization is overridden for this bounded preparation. No module AppHost exists; live prerequisites remain unavailable.

Parent acceptance audit: all eight preparatory tasks are implemented. Parsed every retained XML result and confirmed every case passed; all five matrix rows have passing domain coverage, including 18 added parameterized cases. Persisted full/incremental replay (4 cases), transport (2), unavailable administration (6), rendered legacy blocker (4), and localization (2) also ran and passed. Owning regressions total 6,293 (2,005 contracts; 1,035 domain; 1,992 server; 1,261 UI). Debug/source solution and project builds passed without warnings/errors.

Run `pwsh ./eng/verify-story-5.9.ps1 -PreparationOnly`. Local machine-readable evidence is [result.json](../../artifacts/story-5-9/result.json); attempts 1–3 remain in adjacent subdirectories. The script uses the Builds-owned source mapper and restores its environment before independent test probes. CRLF whitespace attributes preserve existing source line endings. All work remains local, with no commits or remote operations.

## Spec Change Log

## Review Triage Log

All three independent review layers completed before triage. The blind review's finding-floor arithmetic is not a defect. Each finding was checked against the actual caller and retained result behavior; duplicate causes remain separate rows before grouping.

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| BH-1 | medium | patch | `cp -al` at verifier setup requires one filesystem; a distinct TMPDIR mount causes EXDEV before any checks. Use ordinary isolated copying. |
| BH-2 | medium | patch | A linked-worktree `.git` is a pointer file; the current copy assumes directory metadata and cannot establish an isolated source mapping. Reject that demonstrated unsupported layout before copying rather than follow original metadata. Full linked-worktree support is not claimed. |
| BH-3 | medium | patch | Copy/setup precedes the report lifecycle; an early failure can leave a previous success receipt. Initialize incomplete evidence and encompass setup in the existing failure lifecycle. |
| BH-4 | medium | patch | The descendant-only lexical check permits TMPDIR equality and symlink aliases. Canonicalize artifact/TMPDIR paths and refuse equality or containment before output placement. |
| BH-5 | low | reject | Existing output retains producer mapping hash/diagnostics but not an immutable full source closure. This local preparation verifier makes no historical attestation claim; building archival source/mapping custody is disproportionate to the negligible everyday impact. No new evidence-authority claim will be added. |
| BH-6 | medium | patch | Fresh accepted-posture cases do not execute legacy-to-hard-control migration. Add both legacy values to all three accepted replacements, before/after enablement, checking exact versions and retries. |
| BH-7 | medium | patch | `LaunchReadinessPanel` colors the historical enabled flag green without considering current blockers. The new legacy blocker exposes that mismatch. Keep the flag but render a severe current-blocked label/icon whenever blockers remain. |
| BH-8 | medium | patch | The old future-posture replay test asserts only sequence advancement; the new prohibited cases do not cover unknown/missing persisted fallback and both inspections after rebuild. Add those vectors. |
| EC-1 | medium | patch | Same verified cross-filesystem hard-link failure as BH-1; ordinary copying resolves both findings. |
| EC-2 | medium | patch | Same verified TMPDIR-equality hole as BH-4; canonical containment refusal resolves it. |
| VG-1 | medium | patch | Pre-verified absence of executable verifier mode coverage is accepted. The script is new in this preparation, so defer would leave this change's own qualification command unprotected. Add subprocess checks of preparation success/default refusal and retained incomplete receipts using the established tooling-test pattern. |

BH-8 follow-up tracing: the absent-member part of the proposed fallback is disproved by `AgentEventPayloadStructure`: missing required `CostPosture` is refused before projection/rebuild. Preserve that guard and test refusal with unchanged persisted state. Only well-formed unknown-name/undefined-number payloads assert persisted Unknown and the missing-cost blocker. No validator exception is authorized.

BH-1/EC-1 and BH-4/EC-2 share fixes. All surviving corrections are direct patches with no new public product surface or prerequisite/runtime qualification claim.

## Verification

Final review corrections are implemented. Ordinary isolated Git copies support separate TMPDIR filesystems; unsupported Git pointer roots fail before following metadata. Canonical output containment and fresh failure receipts are covered by executable subprocess tests. Legacy-to-hard-control migrations, persisted unknown values and malformed-member refusal are tested. The UI preserves historical enablement while showing a severe current-blocked state with English/French labels. The existing structural validator is unchanged.

Parent final verification: `pwsh ./eng/verify-story-5.9.ps1 -PreparationOnly -ArtifactsPath ./artifacts/story-5-9/final-preparation` exited 0. Debug/source solution and owning builds passed with zero warnings/errors. All focused suites and 6,304 owning regressions passed (2,005 contracts; 1,041 domain; 1,995 server; 1,263 UI); every retained XML case was independently checked as Pass. `run-with-temp python3 -m unittest discover -s tests/tooling -p story_59_verifier_test.py -v` passed all seven tests, including preparation success/default qualification refusal, retained incomplete receipts, stale-result replacement and scratch cleanup. Whitespace checks pass.

Final local evidence: [preparation result](../../artifacts/story-5-9/final-preparation/result.json), [tooling checks](../../artifacts/story-5-9/final-tooling.log). Earlier full/focused attempts are preserved. Full Story 5.9 remains backlog, and live/provider evidence flags remain false; no dependency status or invocation contract changed. No deferred-work entries were added. This spec's done status denotes only the approved preparation. All submodules remain clean and their pointers unchanged. A local conventional commit records the reviewed work; nothing is pushed.

Use `run-with-temp` for disposable builds/tests, placing scratch work under its TMPDIR and useful logs outside it. Build Debug with source references and run test projects individually; invoke built xUnit v3 assemblies directly for focused `-class` selections. Run owning-project regressions and `git diff --check`. Preserve broad blockers separately from focused evidence; never suppress audits or weaken checks. Full `pwsh ./eng/verify-story-5.9.ps1` qualification remains dependent on Stories 5.5/5.7 and a live public readiness path.
