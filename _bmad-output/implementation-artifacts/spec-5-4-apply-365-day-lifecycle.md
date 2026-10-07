---
title: '5.4 Apply approved actor-history lifetime with bounded cleanup'
type: 'feature'
created: '2026-10-07'
status: 'done'
route: 'dispatch'
human_approval: 'accepted'
approval_source: 'User: do recommended (365-day proposal dated 2026-10-06)'
baseline_commit: '7653521e2b21825cba196a3365c53c003432c9b6'
parties_baseline_commit: 'b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca'
platform_baseline_commit: 'fd5db04d48423bc30f5a5e141a9019fede5d736f'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '/home/administrator/projects/hexalith/parties/AGENTS.md'
  - '/home/administrator/projects/hexalith/parties/.editorconfig'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/story-5-4-retention-custody-proposal-2026-10-06.md'
  - '/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IIdentityHistoryCustody.cs'
  - '/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryPolicy.cs'
  - '/home/administrator/projects/hexalith/eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryCustodyEvidence.cs'
---

<frozen-after-approval reason="User explicitly selected the concrete 365-day recommendation">

## Intent

Apply the selected `party-actor-retention-v1` policy: 365 fixed days from `binding-effective-at`, including bounded historical attribution after profile erasure. Provide a small, executable Platform cleanup operation through the existing EventStore custody interface. Production remains unavailable until independently operated custody and restore behavior are qualified. User acceptance establishes this engineering policy, not a fabricated infrastructure receipt or a statutory period.

## Boundaries & Constraints

Use the existing Parties options binding and EventStore policy/evidence records. Populate the Parties host configuration with the accepted ID, `365.00:00:00` and trigger; keep options library defaults unset. Keep the existing mandatory custody/source/authorization guards. No expiry extension on erasure, renewal, restore or retry. A long-lived binding requires an authorized new version; an action near the end of a binding has only the remainder of that binding's lifetime.

Keep shared lifecycle orchestration in Platform.Custody; source events/snapshots stay in EventStore. The cleanup operation is a library call for one owner-inventoried retention unit, not a new service, database, scheduler, discovery API or generic policy engine. Reuse `IIdentityHistoryCustody.DestroyExpiredAsync` and `CanReadAsync`. Accept a missing provider as pending; do not install a development backend. Validate exact policy, purpose, finite derived expiry and lifecycle flags before contacting custody. Never invoke destruction before exclusive expiry. Provider timeout, exception, missing receipt, readable-after-destruction and unknown outcomes remain pending. Preserve caller cancellation and observe late task faults without retaining or printing payloads. A retry uses the exact same identity and evidence reference, not a new retention unit. The provider owns authentication, independent lifecycle durability, all-copy irreversible destruction and idempotent outcome lookup.

Provider-confirmed cleanup is not independent production qualification. No provider/resource selection is invented: the workspace has no actor-history backend, and earlier Azure discovery had no usable token. Do not provision infrastructure, deploy, alter frozen original Story 5.4 or its dependency register, stage/commit/push, update submodules, add a general managed-cloud client, or claim predecessor-expiry/restore completeness. Preserve other work and prior dated evidence.

## I/O & Edge-Case Matrix

| Scenario | Expected behavior |
| --- | --- |
| Host configuration consumed | Existing options binding derives exactly 365 days; overrides can withdraw policy; library defaults remain unset |
| Unexpired retention unit | No provider call; not expired |
| Exactly at/after expiry | Destroy exact unit; confirm fresh custody denies reads before reporting provider-confirmed destruction |
| Invalid policy/evidence/scope | Typed invalid result, no provider calls; never release actor/profile data |
| Provider absent, false, throws or stalls | Pending; no claim of irreversibility or changed deadline |
| Destruction reports true but unit remains readable | Pending |
| Lost acknowledgement and repeated cleanup | Retry same immutable unit; exact evidence ID/policy/deadline passed again |
| Caller cancels, provider ignores cancellation | Prompt cancellation; late completion observed; no fabricated result |
| Another tenant or live successor | Operation targets only supplied exact expired unit; caller cannot sweep unrelated records |

</frozen-after-approval>

## Code Map

- Parties `src/Hexalith.Parties/appsettings.json`, `Authorization/PartyIdentityOptions.cs`, `Extensions/PartiesServiceCollectionExtensions.cs` — existing policy configuration and guards. No new enable flag is required: absent custody denies binding admission/read.
- Parties `_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md` — record concrete approval and duration; keep independent qualification pending.
- Platform `src/Hexalith.Platform.Custody/Hexalith.Platform.Custody.csproj` and `PlatformCustodyServiceCollectionExtensions.cs` — add source/package Contracts references consistent with existing build switch; register cleanup helper with optional provider, no provider implementation/default key material.
- Platform new `IdentityHistoryCleanup.cs` and `IdentityHistoryCleanupOutcome.cs` — bounded single-unit operation with a fixed five-second bound per provider await, caller cancellation, safe late-fault observation and typed outcomes.
- Platform `tests/Hexalith.Platform.Custody.Tests/` — behavior tests with synthetic SDK custody implementation and clock; one C# type per file.
- Parties `tests/Hexalith.Parties.Tests/Gateway/PartyIdentityRetentionConfigurationTests.cs` — real host JSON/options binding and withdrawal tests; copy the host JSON into test output deliberately through the test project.

## Tasks & Acceptance

- [x] Apply approved host configuration and policy record. Given the actual host JSON and existing options binding, when consumed, then ID/trigger and exact 365-day expiry match; library defaults still deny.
- [x] Implement bounded Platform cleanup and meaningful matrix tests. Given an expired valid retention unit and provider confirmation plus fresh denial, when cleaned, then return provider-confirmed destruction; every failure/unknown case remains pending and retries preserve exact input.
- [x] Run normal Debug builds, all Platform custody tests, and focused Parties configuration/query/admission regression tests. Preserve exact source hashes/commands/results in a new dated evidence packet; do not overwrite earlier evidence. Record production/provider/restore and independent review limits separately.

## Verification (before independent review)

- Parties actual host-policy binding, withdrawal and fixed-day leap-year deadline: two tests passed. Existing query/admission suite: 78 tests passed.
- Platform cleanup matrix: 43 tests passed; existing custody suite: 68 tests passed. Both source Debug builds: zero warnings/errors. No skips, failures, errors or not-run cases.
- [Source/artifact hashes and exact commands](tests/actor-history-lifecycle-2026-10-07/evidence.json); [applied behavior and limits](story-5-4-retention-application-2026-10-07.md).
- Required fresh implementation-agent spawn failed with `agent thread limit reached`; implemented directly after loading context.
- This initial evidence predates the independent reviews completed below. Production custody/restore qualification remains pending; full Story 5.4 remains incomplete.

## Review Triage Log

All three independent reviewers completed on 2026-10-07 using fresh Codex tasks with the inherited model. Every finding is classified separately before grouping. The captured complete baseline includes earlier Parties prerequisite work; the local accepted lifetime/cleanup slice does not claim that earlier work or production qualification is complete. Raw findings are preserved in [the review packet](lifecycle-review-2026-10-07/README.md).

| ID | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B01 | medium | defer | The pinned Client 3.113.0 has no retained-history reader while source mode supplies it. Contracts already contains the older custody seam, so the claim is narrowed to missing retained-stream/reader APIs; packaged release verification remains an earlier prerequisite. |
| B02 | high | defer | RetainedHumanActorHistoryFold starts version at zero and rejects a Rebound with version zero. Removing an expired predecessor cannot reproduce a live successor. This previously recorded production blocker requires owner-authenticated continuity evidence; qualification alone is insufficient. |
| B03 | high | defer | HttpPartiesIdentityClient.Matches validates recorded interval/custody shape but has no current-clock comparison. A delayed historical response can pass after custody expiry. This is existing client behavior, separate from the new cleanup operation. |
| B04 | maybe-false | defer | The new field defaults to zero and the client rejects zero, but these identity APIs are documented as proposed/unpublished. No deployed older v1 producer was demonstrated. Medium if such a producer exists; settle with the owner publication/deployment inventory and explicit rollout contract. |
| B05 | high | defer | PartyIdentityAuthority.Admit re-verifies the same signed proof without consulting the actor registry. SameAuthority cannot see revocation during its lifetime. This is an earlier authority seam/production qualification gap, not introduced by applying the duration. |
| B06 | medium | defer | Current and retained-history custody waits use WaitAsync(cancellationToken) without an operation deadline. An ignoring provider with CancellationToken.None can stall indefinitely. This earlier query boundary needs its own bounded operation contract. |
| B07 | high | patch | IdentityHistoryCleanup invokes operation() before WaitAsync. A synchronously blocking provider defeats the new bound and cancellation; the provider interface imposes no nonblocking restriction. Offload invocation within the same existing task boundary and test synchronous stalls in both provider phases. |
| B08 | medium | defer | The readiness script parses retention but checks only a future returned expiry, not ValidFrom + retention or its interval bound. This pre-existing ReadinessOnly verifier can accept a wrong duration; it cannot qualify production. |
| B09 | medium | defer | The verifier compares the opening actor/version/scope but does not reconstruct later revoke/rebind boundaries. An incorrectly open returned interval can pass. This is an earlier readiness verifier defect, not a cleanup/provider qualification result. |
| B10 | medium | defer | Invoke-WebRequest buffers the response before the UTF-8 byte-count check. The 32 MiB receive bound does not prevent oversized allocation; streaming enforcement is an earlier verifier prerequisite. |
| B11 | medium | defer | The wrapper creates a caller-relative EvidenceDirectory then changes directory before resolving build/test outputs. Legal relative arguments can redirect or fail output. Normalize evidence and optional Memories paths before lane execution in the owning verifier slice. |
| B12 | low | patch | The ADR still says numeric duration/post-erasure scope are unapproved and suggests a pending different trigger. The accepted policy and host now supply 365 fixed days from binding-effective-at. Correct those current sentences while keeping trust/custody/restore qualification pending. |
| E01 | high | patch | Same proven invocation gap as B07: Task is not obtained until operation() returns; neither timeout nor caller cancellation can bound a synchronous stall. |
| E02 | high | defer | After custody, the current method checks upper expiry but not completedAt >= binding.ValidFrom or source.ObservedAt <= completedAt. An earlier-issued still-valid grant plus clock rollback can release future current evidence. This pre-existing current-read boundary remains a launch blocker. |
| E03 | medium | defer | Same legal relative-path failure as B11, verified at Push-Location and the build/test Join-Path calls. |
| E04 | medium | defer | The readiness response guard checks outer tenant/Party and actor/version but not the nested evidence tenant/Party. Comparing opening source scope to the expected scope does not compare the returned nested scope; another tenant can pass the verifier. |
| E05 | medium | defer | Same missing immutable-expiry comparison as B08. The source opening custody expiry is not compared with returned expiry. |
| E06 | high | patch | The reported 6,035 ms blocking invocation is consistent with the unbounded operation() call before the 5,000 ms wait. The same minimal fix and synchronous deadline/cancellation tests resolve this falsified new cleanup claim. |
| V01 | medium | defer | Accept the pre-verified gap: authority-change tests call ResolveAtAsync, while current policy/cancellation tests do not observe removal of SameAuthority in ResolveAsync. Add current source/custody authority-change cases in the owning query verification slice. |
| V02 | medium | defer | Accept the pre-verified gap: the clock-crossing test exercises historical resolution; client invalid replies do not execute the current service. Add current binding-closure and custody-expiry crossing cases with otherwise valid authority. |
| V03 | medium | defer | Accept the pre-verified gap: existing counting-server smoke cases fail before receipt validation. A synthetic Available register with a mismatched receipt needs a no-HTTP regression test before this earlier readiness gate can be relied upon. |
| V04 | medium | defer | The verification reviewer independently confirms B08/E05: a 366-day returned deadline can pass a declared 365-day policy because only future expiry and opening actor/version/scope are checked. |

Grouped survivors: B07/E01/E06 share the synchronous cleanup invocation defect; B12 is stale ADR wording. These two groups route to patch. The other 18 findings form 15 existing prerequisite/verification groups and route to defer; B04 is explicitly unverified rather than a claimed deployed compatibility break. No intent_gap or bad_spec loopback is required. Full Story 5.4 remains incomplete and production remains disabled.


## Review Resolution

The three fresh independent Codex review tasks completed; the earlier agent-spawn failure no longer blocks review. All 22 findings were triaged before grouping. B07/E01/E06 are fixed by obtaining the provider task through `Task.Run` before applying the existing timeout/cancellation wait. Four regression cases cover synchronous blocking in destruction and the final readability check; all four failed against the original implementation, then passed with the fix. B12 is fixed by aligning the Parties ADR with the approved 365-day, binding-effective-at policy. The approved frozen intent is unchanged.

The normal Debug source build passed with zero warnings/errors. All 115 Platform custody tests passed with zero failures, errors, skips or not-run cases. The previous 80 Parties tests are reused evidence, not a new run; their recorded code/test source hashes still match. [Review-fix evidence](tests/actor-history-lifecycle-review-fixes-2026-10-07/evidence.json) preserves red/green logs, results and source hashes separately from the original dated packet.

The remaining 18 findings form 15 earlier prerequisite groups and have been appended to [deferred work](deferred-work.md). They remain unresolved and are not waived for production. This accepted policy/cleanup child is complete; original Story 5.4, owner commitments, packaged compatibility and production custody/destruction/restore qualification remain incomplete. No deployment or Git staging/commit/push occurred, consistent with the approved boundary.
