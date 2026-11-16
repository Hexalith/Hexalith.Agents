# Host/setup local prerequisites — 2026-11-16

This implements the reduced local scope in `spec-five-deferred-host-qualification-items.md`. DW-1, DW-19, DW-21, setup receipt replay/projection qualification, and actual provider query qualification all remain **open**. No installed host, owner credential, production verifier, dependency acceptance, deployment or package publication is supplied by this work.

`AddAgentsUiSetup` now replaces known deferred setup/catalog implementation types, including explicit registrations of those built-in placeholders, so plain UI registration may precede live opt-in. Other host factories, instances, implementation types and lifetimes survive. Missing Agent target stays unavailable; catalog opt-in remains independent of an Agent target.

## Evidence before review fixes

The focused selections below passed before review fixes, with zero failures, errors, skips or not-run cases: **307/307**. Their source hashes and the complete gate are retained under `preReviewEvidence` in [verification.json](verification.json); they do not qualify the later changed test/source bytes.

| Spec row | Executed local evidence |
| --- | --- |
| UI opt-in | `AgentsUiCompositionTests` — **16/16**: both registration orders, repeat opt-in, scoped identity, missing target, and preserved host factories/instances/types/lifetimes. |
| Gateway enrollment | `AgentsEventStoreGatewayIntegrationTests` — **162/162**: actual SDK command HTTP admission, real registration/coordinator, exact local proof positive control, missing registration/proof, changed proof intent and foreign workload with zero domain executions/history additions. |
| Status | `EventStoreAgentCommandStatusReaderTests` — **31/31** and `AgentSetupStatusIssuerTests` — **14/14**: all canonical states, exact tenant/domain/message/correlation, credential/transport and malformed/foreign-evidence refusal, cancellation and composed real issuer. Reserved system status never reaches transport. Full Server regression also includes `AgentSetupCompositionTests` host overrides and `EventStoreProviderCatalogOperationsTests` terminal/no-op/event-count/identity gates. |
| Replay/projection | Gateway `SetupReceiptReplayFeedsTheProductionProjectionOnceAfterLagAndConflict`, plus `AgentSetupProjectionTests` — **58/58**: production setup aggregate result survives equivalent HTTP retry with original identity/effect/version; changed intent cannot append. The production projection handler persists accepted mode/version after lag, and duplicate delivery performs no additional batch or stored-state change. |
| Actual query | `ProviderQueryEndpointPrerequisiteTests` — **26/26**: list and detail traverse `UseEventStoreDomainService` HTTP `/query`, real workload/channel and operation policies, real SDK catalog guard with exact local generation, bound human and production handlers. Positive synthetic current verification succeeds. Missing/false verifier, forged human, expired/revoked current authority, foreign workload/operation, stale assertion, missing credentials and foreign catalog generation disclose no protected model. Verifier exceptions return empty 503 before authority/model reads. |

The local command endpoint uses callback history and an in-memory admission actor. Projection end-state is serialized through `FakeReadModelStore`, not a durable EventStore/Dapr store. Query credentials, verifier, current-authority sources and immutable catalog are synthetic fixtures; production registrations and SDK authorization are unchanged.

Before review fixes, the unchanged Story 6.1 Release/package full gate passed **6,420/6,420**: Contracts **2,005**, Client **7**, Domain **1,042**, Server **2,005**, UI **1,361**. All five builds reported zero warnings; every full native suite reported zero failures, errors, skips and not-run cases. Exact method/case-count auditing confirmed all **55 added cases** executed and passed. Server included the whole-repository package-authority check. The separate EventStore package-floor gate passed all **13** selected rows at **5.1.1** against minimum **3.128.3**.

## Review fixes and focused validation

Review clarifies that explicit built-in deferred implementation types are intentionally replaced; a new composition control pins that opt-in. The reproducer now applies `rsync --delete` while preserving its exclusions and complete archive inputs. Its XML gate uses explicit conditional failures and reads exact focused totals plus qualified method/passing-case counts from `verification.json`, including the new UI control. Query assertions inspect all decoded denial-payload JSON members before typed deserialization, including unknown members, nested arrays and escaped strings/property names. Authorized positive controls explicitly retain the public contract's safe `ConfigurationReferenceId == "cfg-ref"`.

Current hashes and focused review results are recorded under `postReviewValidation`. The earlier complete gate remains historical; the parent subsequently ran the complete corrected reproducer against the final source, as recorded below. No broad verification or tooling discovery was rerun by the implementation agent.

Final review validation passes **17/17 UI composition** and **26/26 actual query** cases, with zero failures, skips, not-run cases or errors and zero warnings in both owning test-project builds. The [focused reproducer probes](verify-review-controls.py) pass **9/9** under `PYTHONOPTIMIZE=1`, covering deletion/preservation, a valid XML control, and missing/malformed/failed/substituted method or owner results. A separate compiled probe passes **7/7** decoded-payload controls, including unknown members, nested arrays and escaped values/property names. Its scratch console emitted one `CS7022` warning from the test project's generated xUnit entry point; the owning builds remained warning-free. Raw focused logs/XML are retained under `artifacts/host-setup-prerequisites-2026-11-16/review`.

The first review query attempt passed **24/26**: the requested success-side `cfg-ref` absence check conflicted with `ProviderCatalogEntryView`'s documented safe reference disclosure. The parent refuted that review instruction, retained the authorized safe reference contract and required decoded guards on denials only. `review/query-attempt-1.log` and `.xml` preserve that failed attempt; no production disclosure or authorization behavior was changed.

## Commands and artifacts

Run from the repository root:

```bash
_bmad-output/implementation-artifacts/tests/host-setup-prerequisites-2026-11-16/verify-local.sh
```

The [reproducer](verify-local.sh) runs a Debug/source attempt with all outputs under `run-with-temp`'s private `TMPDIR`, then uses complete local shared Git clones of Agents and the root-declared Builds checkout plus the current edits. This preserves the tracked archived MSBuild inventory required by package-authority validation. It performs no network Git operation or nested submodule initialization. The unchanged owning-suite gate is:

```bash
pwsh -NoLogo -NoProfile -File eng/verify-story-6.1.ps1 \
  -DependencyMode Package -ArtifactsPath "$PREREQUISITE_EVIDENCE/package"
```

Focused native selections use the built Release assemblies with `-class` and `-result-xml`; the reproducer requires the exact focused totals and rejects failed, skipped, not-run or missing results. It leaves logs/XML outside scratch under `artifacts/host-setup-prerequisites-2026-11-16/reproduction`. The original run's raw logs/XML are retained in `artifacts/host-setup-prerequisites-2026-11-16`; [verification.json](verification.json) records hashes and totals.

The separate package-floor command was `pwsh -NoLogo -NoProfile -File eng/verify-eventstore-package-floor.ps1`. Secondary tooling discovery used `python3 -B -m unittest discover -s tests/tooling -p '*_test.py'` in the complete package copy; its limitations are recorded separately from the native gate.

## Source boundary and corrected attempts

The initial bare Debug Server source build refused the missing Conversations Platform producer mapping before Agents compilation. A focused Debug UI source build with explicit direct dependency roots reached two existing unsuppressed EventStore.Contracts `CA1822` errors in `ProductionProfileValidationResult.Authorization` (line 15) and `.Qualification` (line 18). No source test pass is claimed; no analyzer/audit gate was disabled and no owner source or submodule pointer was changed.

The first focused query attempt failed all 26 cases because its synthetic catalog route ids contained invalid characters; bounded fixture ids fixed that setup. The first combined boundary attempt passed 263/265: the proof fixture compared against extensions that the SDK deliberately removes before policy verification, and the foreign-workload expected HTTP code differed from the reserved-metadata boundary's 400. Matching the actual extension-stripped command and exact separate key/value, and asserting 400 with no effects, corrected the fixtures. These failures precede the passing focused evidence above.

The first full package attempt passed Contracts 2,005, Client 7 and Domain 1,042, then Server passed 2,004/2,005. Its package-authority failure identified three tracked archived projects omitted by the initial broad copy exclusion. That attempt is retained separately as `package-gate-attempt-1.log` and `package-attempt-1/`; the final complete Git-copy run uses the unchanged tests and gate.

Optional broad tooling discovery was **interrupted/partial**, exit **130**, at the parent's request after the required gates passed. It was running `parties_identity_verification_test.PartiesIdentityVerificationTests.test_each_added_owner_selection_requires_complete_named_results` when interrupted; two earlier `E` markers appeared, but no final error diagnostics or suite totals were produced. No tooling pass, skip count or complete result is claimed. SIGINT targeted only its owned Python process; both snapshotted children completed cleanup, with no live owned discovery processes remaining. Its log and `optional-tooling-interruption.json` are retained separately. No tooling repair or repeated discovery was performed.

## Remaining owner inputs

- **DW-1:** exact accepted Platform/FrontComposer host, real Agents client and Agent target; owner execution of its rendered live composition.
- **DW-21:** exact gateway package/host and Agents Dapr identity, independent proof provenance/admission provider, and admitted success plus no-effect refusals on that host.
- **DW-19:** accepted status capability, exact tenant/domain grants and independently provisioned credentials, with owner-run Completed/rejection/lag observations. System scope still needs an explicitly capable host override; the SDK refusal remains.
- **Replay/projection:** accepted production-like topology and durable completion, idempotency and projection stores; retain exact receipts and persisted end-state across replay and lag.
- **Actual query:** accepted owner `IDomainServiceAdministratorVerifier`, current Parties/Tenants sources and bound human credentials in the exact enrolled host, with authenticated HTTP positive/refusal controls.

## Final parent verification and delivery

The corrected reproducer completed end to end with exit **0** on the final reviewed C# source: **6,421/6,421** cases (Contracts 2,005; Client 7; Domain 1,042; Server 2,005; UI 1,362), five warning-free builds and no failures, errors, skips or not-run cases. The focused matrix passes **308/308** (query 26, gateway/status/projection 265, UI composition 17), including all **56** required added cases by qualified method identity and count. The parent independently reran the actual overlay/XML probes: **9/9** with optimization enabled. All disposable verification children exited and the final package workspace was removed. Final source and artifact hashes are under `finalParentValidation` in the manifest. Raw results are under `artifacts/host-setup-prerequisites-2026-11-16/final`; historical and failed attempts remain separate.

All three independent review layers completed. The deletion, optimized-XML, named-method, decoded-denial-payload and placeholder-documentation findings are corrected; the safe-reference removal suggestion was refuted against the public contract. No unresolved implementation finding remains in the authorized local scope.

Existing CI configuration remains a separate blocker: `.github/workflows/ci.yml` pins Builds `264e17d3185c87b72b0757dd66a13040793756bb`, while the recorded gitlink is `fdda17c0a4538b0f7c99f813c20ca24b49006530`, and its policy job references absent `package.json`, `eng/verify-story.ps1` and `eng/verify-story-5.2.ps1`. `ci-preflight.json` records this read-only inspection; no CI success is claimed and no workflow or dependency pin was changed. Debug/source remains blocked by the owner diagnostics above. All five installed-host qualification obligations remain open.
