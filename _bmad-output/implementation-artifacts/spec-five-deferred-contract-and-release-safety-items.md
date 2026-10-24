---
title: 'Five deferred contract and release safety items'
type: 'bugfix'
created: '2026-10-24'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '4d6bb6ca763fb04ede70676d8b2c4762f0ce48c0'
context:
  - AGENTS.md
  - references/Hexalith.AI.Tools/hexalith-llm-instructions.md
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five remaining deferrals concern unsafe zero-valued setup statuses, HTTP failures reported as success, structurally incomplete projection events, partially published releases, and unqualified consumer executable assets.

**Approach:** Resolve DW-11, DW-16, and the ledger summaries beginning “Define structural event-payload validation”, “Make multi-package NuGet publication recoverable”, and “Qualify the actual consumer analyzer”. Verify each against actual code and durable evidence, then commit and push main as requested.

## Boundaries & Constraints

Preserve existing enum ordinals and declared public members. Follow PRD FR-23's successor/deprecate-and-reject rule; reconcile the contradictory additive-zero statement in the Spine. Preserve typed HTTP bodies and dependency-gate precedence. Preserve supported historical event defaults; do not apply current command eligibility rules to historical events. Release recovery must prove original source, archive integrity and remote content before a missing-package push. This task changes implementation and workflows; it does not run a release or certify a deployed host. Keep unrelated ledger entries and submodule pins unchanged.

## Approved Decisions

1. Successor contract: `AgentSetupWriteOutcome` (`Unknown=0`, explicit ordinals 1–10), `GetCommandWriteOutcomeAsync`, and `/api/v1/agents/operations/providers/commands/{targetTenantId}/{messageId}`; retain the old operation/route with typed rejection. Add no new member to existing setup-result records.
2. Event compatibility: required member presence, nonnull required object graphs and positive versions, while retaining lifecycle omitted/zero versions, incomplete drafts, optional nulls, empty lists, and future/retired enum compatibility.
3. Release recovery: explicit protected dispatch using original archives/provenance staged in a GitHub draft release before NuGet publication, an exact source tag, and current-green-main source proof. A root-owned semantic-release hook stages/finalizes that draft, replacing the existing GitHub publisher to avoid duplicate release creation. Keep normal releases collision-failing.
4. Consumer assets: an initial explicitly reviewed package/path/version/hash baseline, with subsequent unreviewed additions or changes failing CI.

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteStatus.cs`: legacy Submitted=0; already returned publicly by provider command-outcome reads.
- `src/Hexalith.Agents.Client/IProviderCatalogOperations.cs`: preserve existing method; add the successor operation.
- `src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs`: canonical outcome implementation and legacy rejection.
- `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs`: shared HTTP mapping; dependency filter already emits 503.
- `src/Hexalith.Agents.Server/Projections/AgentSetupProjectionFold.cs`: share validation between delivery checks and folding.
- `scripts/verify-nuget-publication.py`: reuse bounded probes and archive-content comparison.
- `scripts/validate-consumer-package-references.py`: actual isolated consumer restore/build and asset graph.
- `.releaserc.json`: ordinary release absence gate remains; semantic-release may create its tag before publication fails.

## Tasks & Acceptance

- [x] Update the mapped Contracts/Client/Server paths, `src/Hexalith.Agents.UI/Services/Gateways/AgentsClientProviderCatalogGateway.cs`, and `test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs` for the approved successor contract. Given legacy/future values, when serialized or submitted, then ordinals remain stable and unknown/prohibited behavior fails closed.
- [x] Add shared result status mapping at the endpoints and extend `test/Hexalith.Agents.Server.Tests/AgentsOperationEndpointsTests.cs`. Given each typed status, when served over HTTP, then success/degraded=200, pending/checking=202, unauthorized=403, invalid=400, missing=404, conflict/stale=409, rejected=422, blocked/unavailable/unverifiable=503, unknown=500; typed bodies and correlation survive.
- [x] Update the projection fold and `test/Hexalith.Agents.Server.Tests/AgentSetupProjectionTests.cs`. Given malformed pending events, when delivered/rebuilt, then no batch or checkpoint advances; corrected redelivery converges. Positive controls preserve every approved legacy exception and checkpoint-covered duplicates.
- [x] Add `scripts/recover-release-packages.py`, `.github/workflows/recover-release.yml`, and `eng/github-release-publisher.mjs`; wire `.releaserc.json` to retain original archives/provenance and finalize the staged release only after complete publication. Given a partial immutable set, when recovery runs, then every existing archive matches before any missing push; conflicts/unconfirmed probes refuse; final complete verification passes. Handle an existing tag and unpublished draft without ordinary release planning; retain the publication freeze and shared release concurrency. Extend `tests/tooling/publish_release_packages_test.py` and add recovery/hook subprocess fixtures.
- [x] Extend the consumer verifier and `tests/tooling/dependency_assets_restore_test.py`. Given packed Agents packages, when restored individually and together under root `global.json`, then a normalized receipt identifies selected analyzers/imports, package ownership and hashes, separately from available archive assets and SDK analyzers; apply the approved policy.
- [x] Update `CONTRIBUTING.md`, retain verification evidence, and append resolutions to exactly the five selected ledger entries only after their acceptance passes.

## Verification

Run native focused and individual full Contracts, Server and UI suites in Debug source mode; run the complete tooling suite, real consumer package validation, whitespace checks and commitlint. Release tests use controlled subprocesses and HTTP fixtures; no real package publication occurs.

## Implementation Notes

Baseline: clean main at `4d6bb6ca763fb04ede70676d8b2c4762f0ce48c0`. This is the explicitly requested five-item batch, not an epic story; no sprint key applies. Investigation is complete and implementation awaits the four decisions above and the required build checkpoint. Command-status host qualification remains open: the pinned SDK assertion issuer excludes command-status operations, and local tests cannot establish the production topology.

## Spec Change Log

## Review Triage Log

User approved the four recommended decisions and continuation on 2026-10-24. Implementation handoff ends with verified working-tree changes; the parent runs final workflow review and Git delivery. No root AppHost is present.

Parent acceptance audit: all six compressed native reports and three current binaries match their manifests; full suites pass 1,997 Contracts, 1,545 Server and 1,173 UI tests. The initial 30-entry package executable-asset baseline was independently reviewed against the final seven-consumer receipt, including individual selections, normalized paths, ownership and hashes. The full tooling completion remains pending while read-only reviews run.

All three review layers completed. Findings were judged individually before grouping.

- B1 — medium / patch: Pending AgentCommandAcceptance and ProviderCatalogCommandAcceptance bypass payload mapping, so actual accepted writes still return 200. Map their truth stages; grouped with E2.
- B2 — medium / patch: AgentProposalApprovalOutcome.Approved documents posting still pending; its current shared name arm returns 200. Use 202 for that concrete outcome.
- B3 — high / patch: The versioned sibling group cannot inherit conventions chained to the returned legacy group. Use one returned parent and verify authorization/custom metadata reaches both route families.
- B4 — medium / patch: Enum structural validation accepts object/array/boolean shapes and the tolerant converter degrades them to Unknown. Require supported scalar token shapes while retaining unknown names/numbers.
- B5 — medium / patch: Active non-props/targets imports are demonstrably omitted. Inventory imports independently of extension; grouped with E3.
- B6 — maybe-false / defer: Unverified medium: the policy fingerprints selected analyzer entries/imports, not every dynamically loaded helper. All 18 current selected package imports were inspected from the restored cache and none declares UsingTask. No omitted executable helper load was demonstrated; a real dependency-load trace or helper-byte mutation fixture would settle the broader claim.
- B7 — medium / patch: Post-publication verification authenticates provenance bytes against asset metadata but does not validate the document source/archive claims. Reuse the provenance checks against the expected release identity and archives.
- B8 — medium / patch: The new required abstract interface method breaks existing implementations. Supply a fail-closed default body without changing existing members and cover an implementation of the prior surface.
- B9 — medium / patch: The successor wrapper and legacy route echo unsanitized correlation input. Enforce canonical ULIDs for the successor and omit malformed legacy correlation metadata while keeping authorization precedence.
- B10 — low / reject: A third parse/validation pass is real, but no replay timeout or user-visible slowdown was demonstrated. Caching and batch reuse require substantially more than a direct correction; retain the atomic validation behavior and reject this optimization suggestion.
- E1 — high / patch: The production final verifier omits required attempts/delay_seconds options. The real default path raises TypeError after pushes; fix the call and exercise it without a test wrapper supplying missing arguments.
- E2 — medium / patch: Same pending acceptance mapping defect as B1; preserve a separate finding row and apply one shared correction.
- E3 — medium / patch: Same arbitrary-extension active-import omission as B5; the reviewer demonstrated it with a real local MSBuild fixture.
- V1 — medium / patch: Pre-verified gap: a filename-hashing mutant passes all publisher tests. Assert exact archive names, lengths and independently computed SHA-256 values.
- V2 — medium / patch: Pre-verified gap: setup read-body tests do not assert nested outcome HTTP statuses. Add TestServer coverage for missing/unauthorized/unavailable/success setup results and body preservation.

Patch groups preserve the approved intent and public routes. Corrections are confined to demonstrated branches and test gaps; no new product decision is required. B6 is a separate unverified dependency-load follow-up.

- P1 — medium / patch: Failed<AgentSetupWriteStatus> retains the unconstrained generic enum default Submitted=0, so the deprecated HTTP route still serializes a legacy success-like Value. Emit the existing non-generic typed failure envelope on that route, retaining the public generic method and enum declarations, and assert the legacy value is absent.

Final acceptance supersedes all interim pending statements: the maintained source gate passes all 5,766 full-project tests plus focused/composition selections, the complete tooling suite passes all 266 tests with no failures or skips, and freshly packed final packages pass all seven policy-enforced consumers. All concrete review issues are corrected and independently rechecked; one unverified helper-dependency follow-up remains recorded. Exactly five original entries are closed. Final Git delivery uses the validated Conventional Commit and the explicitly authorized main push.
