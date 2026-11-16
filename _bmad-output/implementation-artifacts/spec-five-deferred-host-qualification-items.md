---
title: 'Five deferred host and setup prerequisites'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '528c9375ecc1f528bc1ef0fac171f4dd853c23d2'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred host/setup obligations lack sufficient executable local prerequisites: UI registration order (DW-1), gateway integration (DW-21), exact command-status observation (DW-19), setup receipt replay/projection, and actual provider `/query` verification.

**Approach:** Complete local prerequisite work for these five obligations and push verified changes on main. Fix demonstrated local defects, add behavioral boundary coverage, and document the remaining installed-host inputs. User authorized this reduced scope on 2026-11-16; live qualification and all five complete deferred obligations remain open.

## Boundaries & Constraints

**Always:** Reuse SDK hosting, current authority checks, scoped status transport, real registration APIs and projection persistence seams. Preserve custom host bindings and cancellation. Keep local synthetic fixtures explicitly separate from accepted owner/live evidence. Use `run-with-temp` for disposable builds and test workspaces. Keep audits enabled and each C# type in its own named file.

**Never:** Invent accepted hosts/credentials, install a permissive production verifier, change dependency acceptance, relax Completed payload gating, add an AppHost, initialize nested submodules, deploy or publish packages.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Expected behavior |
| --- | --- | --- |
| UI opt-in | Plain UI registration precedes live setup | Configured target and catalog resolve live gateways; missing target stays unavailable; explicit custom gateways survive either order |
| Gateway enrollment | Missing integration/proof versus exact registered proof | Real gateway refuses unadmitted reserved metadata without effects; registered exact intent succeeds; wrong workload/proof refuses |
| Status | Completed, rejected, pending, mismatched scope or unavailable credentials | Reader preserves exact status and scope; only valid terminal evidence confirms; reserved system route remains unavailable |
| Replay/projection | Original setup command, equivalent retry, changed intent, lag and duplicate projection | Original receipt/effect survives retry; conflict cannot append; persisted read model changes once and matches accepted version |
| Actual query | Authenticated bound caller with synthetic current verifier; missing/false/throwing verifier, wrong caller or expired authority | Real SDK `/query` positive control works locally; refusals disclose no protected model; unavailable verifier is unavailable, not success |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs`: `TryAdd` currently prevents live opt-in after `AddAgentsUi`. Replace only known deferred descriptors; preserve host/custom descriptors and scoped lifetimes.
- `test/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs`: add order, repeat-registration, missing-target and custom-binding controls.
- `src/Hexalith.Agents.EventStore/AgentsEventStoreServiceCollectionExtensions.cs` and `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs`: existing registration, real gateway fixture and in-memory admission/history. Extend behavioral prerequisite coverage without inventing proof provenance.
- `test/Hexalith.Agents.Server.Tests/{EventStoreAgentCommandStatusReaderTests,AgentSetupStatusIssuerTests}.cs`: real SDK client and exact-domain issuer; extend completion/pending/scope controls. Preserve capable host overrides and SDK system refusal.
- `test/Hexalith.Agents.Server.Tests/{AgentsEventStoreGatewayIntegrationTests,AgentSetupProjectionTests}.cs`: connect setup receipt/retry to production projection handler and `FakeReadModelStore`; assert stored end-state. Existing callback history is local model evidence, not durable EventStore qualification.
- `test/Hexalith.Agents.Server.Tests/VerifiedProviderQueryCallerTests.cs`: currently calls dispatcher directly. Add `ProviderQueryEndpointPrerequisiteTests.cs` exercising `UseEventStoreDomainService` and actual HTTP `/query`, real SDK security policy, bound caller and production query handlers; use separate named helper files as needed.
- `references/Hexalith.EventStore/src/Hexalith.EventStore.DomainService/{DomainQueryDispatcher,DomainServiceAdministratorAssertions,IDomainServiceAdministratorVerifier}.cs`: read existing API; no SDK modification. Missing verifier strips IsGlobalAdmin; verifier exceptions yield unavailable before handlers.
- `eng/verify-story-6.1.ps1`: existing full owning-suite gate; native xUnit selectors support focused verification. Source dependencies have previously failed CA1822; preserve exact blocker and use required package fallback without suppressions.

## Tasks & Acceptance

**Execution:**
- [x] Fix UI registration order and cover every UI matrix branch.
- [x] Add/execute gateway enrollment prerequisite coverage through actual HTTP admission, with no-effect refusals.
- [x] Add/execute exact status observation coverage through SDK transport, preserving explicit system refusal.
- [x] Add/execute connected receipt replay/projection coverage and verify persisted local read-model end-state.
- [x] Add/execute actual SDK query endpoint coverage with synthetic verifier controls and current authority checks.
- [x] Update `CONTRIBUTING.md`, the five existing ledger records and a concise dated evidence report with commands, case totals and remaining owner inputs. Keep all five obligations open. Do not append duplicate obligations.

**Acceptance Criteria:**
- Given local fixtures, when every matrix branch runs, then it passes with no skipped or missing selected cases and no invented live claims.
- Given existing host customizations, when setup registrations run in either order, then custom services and authorization behavior survive.
- Given verified local work, when delivery occurs, then affected owning suites and applicable repository checks pass or precise unsuppressed blockers are reported; submodule state is inspected before main is committed/pushed by the parent.

## Implementation Notes

- User's authorization selects local prerequisites; the former draft's owner-input question is resolved. No live dependency execution is authorized by this amendment.
- Implementation agent must not commit, push, change submodules or run unavailable owner/live commands. Retain useful logs under `artifacts/host-setup-prerequisites-2026-11-16`; keep disposable workspaces/build outputs beneath wrapper TMPDIR and wait for children.
- Observed baseline owner checkouts: Platform `fecdcbc309e19c95808673ff7c5549d993b19b76`; EventStore `f85419d501f279e9a813435aa73538185ad26ada`. These are not replacement accepted targets.

- Parent audit: read the complete staged implementation diff; independently verified all seven changed C# source hashes, all recorded artifact hashes, 6,420 passing native cases, all 55 added cases and five warning-free builds. Every matrix row is represented by executed passing tests. Source/CI limitations and optional partial tooling remain separately recorded.

## Spec Change Log

- 2026-11-16: User authorized local prerequisites with full host qualification left open; rewrote draft tasks and matrix to that concrete scope before freezing.

## Review Triage Log

- B1 — low / patch: Known public deferred implementation types are replaced even when explicitly host-registered. This matches live opt-in but makes the blanket preservation wording imprecise. Clarify that exception and pin it with one local registration control; do not introduce descriptor-provenance machinery or change gateway behavior.
- B2 — medium / patch: The new reproducer overlays without deletions. A private tracked-source probe retained deleted.cs under the old command and removed it with --delete. Synchronize ordinary deletions while preserving excluded Git/submodule state and the complete tracked archive inventory.
- B3 — false: Source validation is an explicitly recorded attempt, not a required source-pass claim: its complete diagnostic log and nonzero exit are retained before the separately labeled package fallback. README and manifest state source blocked, not passed. Inferring source success from the package-only result is contrary to their contract.
- B4 — medium / patch: PYTHONOPTIMIZE disables the reproducer's assert gates while leaving printed success lines. Replace assertions with explicit conditional refusal and exercise malformed results with optimization enabled.
- B5 — low / patch: Exact aggregate totals alone do not enforce the named-method inventory already recorded in verification.json. Check those method identities and passing counts in the reproduced native results without broadening the test matrix.
- B6 — low / patch: Outer response-text checks cannot inspect base64 PayloadBytes. Although zero read-model reads independently prevent the demonstrated fixture disclosure, inspect decoded raw payload bytes as well so unknown JSON members cannot evade the wire assertion.
- B7 — low / patch: The successful endpoint case lacks a raw payload check for its seeded cfg-ref. Add that check while correcting B6; preserve normal authorized labels and typed result assertions.
- B8 — low / reject: Additional signature/issuer/audience cases are an optional expansion of unchanged SDK authentication coverage; no bypass was demonstrated in this diff. The fixture already executes SDK authentication with positive and missing/expired/wrong-workload controls. Adding another SDK validation matrix is beyond a direct correction of an everyday defect.
- B9 — low / reject: The existing incorrect-root control proves the real catalog receiver is active; complete epoch/header permutations would expand coverage of unchanged SDK guards. No missing generation guard was demonstrated; extra fixture branches are not needed for the bounded prerequisite result.
- B10 — low / reject: Current actor admission is exercised by the HTTP positive control and the full HttpAgentAdministrationContextProviderTests/verified-caller suites. More missing/foreign/throwing actor combinations at HTTP are optional coverage of unchanged production logic, not a demonstrated new defect.
- B11 — low / reject: Existing dispatcher tests cover subject/tenant/authority mutations, and the new HTTP wrong-human control demonstrates that binding validation is reached. Independently mutating every digest field would add SDK test permutations without a demonstrated binding defect.
- B12 — low / reject: SDK VerifyAsync explicitly preserves caller cancellation through its filtered catch and token checks; query handlers likewise exclude OperationCanceledException. No cancellation behavior changes in this batch. A further HTTP cancellation matrix is an optional expansion, not a direct correction.
- E1 — medium / patch: Same demonstrated deleted-file defect as B2; group it with B2 after recording this individual verdict.
- B7 correction — false / rejected after following the public contract: ProviderCatalogEntryView.ConfigurationReferenceId is explicitly a safe reference permitted for authorized inspection, and ProviderCatalogViewFactory intentionally forwards it. The requested success cfg-ref absence assertion was wrong and failed both legitimate positive controls (24/26). Preserve the exact authorized reference; retain B6 decoded-wire absence checks only for denials. No disclosure behavior or frozen intent changes are warranted.
- Verification-gap layer — no findings; all requested matrix behaviors have executed coverage.


## Verification

Build Debug/source first, run affected native xUnit classes with nonzero XML summaries, and run owning full suites. If source is blocked, retain exact diagnostics, try focused builds, and use serialized Release/package fallback with audits intact. Use existing Story 6.1 Package gate for full regression evidence if needed. Verify every matrix row against executed results. No tests of workflow YAML. Parent independently reviews, validates Conventional Commit, fetches/inspects divergence, inspects all root submodules, then pushes completed changes on main.

- Final review closure: B1/B2/E1/B4/B5/B6 patches verified; B7 corrected to false against the safe-reference contract. Final parent reproduction passes 6,421 full cases, 308 focused cases and all 56 additions with five warning-free builds. Nine independent optimized XML/overlay controls and seven decoded-payload controls pass. All five full host obligations remain open. Source/CI blockers remain separately recorded; direct submodules are clean and remotely reachable.
