---
title: '5.4 Owner Prerequisite Implementation'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
route: 'dispatch'
human_approval: 'accepted'
approved_on: '2026-10-06'
baseline_commit: 'b252fcfde51bd04317ce5843a0a42bfe4dce5170'
review_loop_iteration: 0
context:
  - '_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
  - '_bmad-output/specs/spec-story-5-4-dependency-unblock/parties-identity-contract.md'
  - '_bmad-output/planning-artifacts/external-dependency-register.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4 has specification approval but lacks complete owner-delivered Parties, Conversations, custody and host prerequisites.

**Approach:** The user authorized implementation across the owning repositories, with runnable verification and concrete results prepared for owner acceptance. Implement dependency-ordered owner work, including missing shared EventStore capabilities required by repository architecture. Preserve the original full Story 5.4 and its separate entry/execution gates.

## Boundaries & Constraints

**Always:** Preserve existing edits and source history. Use EventStore for domain persistence and technical modules for infrastructure. Keep Branch B's immutable Organization Party. Retained history requires independently governed expiry, encryption and restore protection; missing production policy/providers disable it. Complete owner contracts must precede availability claims. Run meaningful focused tests and retain exact verification evidence. Implement sequentially in dependency order.

**Never:** Invent Product retention/erasure decisions, owner acceptance, production keys or successful live evidence. Do not run consuming unavailable seams, deploy, post owner messages, initialize nested submodules, commit or push. No fixtures or deferred ports establish Level 4 readiness.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Branch B identity | Provisioned Organization; inactive/restricted human | Organization identity; ineligible human has no usable binding | Unknown/new classification rejected |
| Retained history | Erased profile; independently protected lifecycle events | Authorized exact-source history without profile decryption | Gaps, scope, expiry, missing custody or changed head block |
| Trusted envelope | Scoped canonical bytes; configured current/retained key | Authenticated exact operation/principal/target | Forgery, time/profile, rotation/revocation failures block |
| Conversations owner operations | Restricted service principal and exact Agent Party | Idempotent membership/posting and typed current evidence | Cross-tenant or uncertain authority blocks before lookup/effect |
| Denial recovery | Independent replicated spool; EventStore outage | Stable pending observation and exactly acknowledged recovery | Missing replication/capability prevents processed-denial claim |

</frozen-after-approval>

## Code Map

- `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` and `../parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs` — reuse tracked identity/binding work; correct Branch B and protect inactive-human evidence.
- `../eventstore/src/Hexalith.EventStore.{Contracts,Client,Server}` — add purpose-limited retained-history read/certificate, preserving original source sequences and explicit exclusions; preserve unrelated current remediation edits.
- `../conversations/src/Hexalith.Conversations.{Contracts,Client,Server}` — add the complete six restricted owner seams and pure state transitions, with EventStore source/outbox proof.
- `../platform/src/Hexalith.Platform.Custody` — shared purpose/tenant/version custody and exact AD-29/30 authentication; required production policy and key provider.
- `../platform/apphost.cs`, host composition and verifier — retain existing identity and Works work; add independent durability and restricted capability composition with truthful gates.

## Tasks & Acceptance

**Execution:**
- [x] `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` — complete current/historical correctness and regression verification.
- [x] `../eventstore/src/Hexalith.EventStore.Client/Streams/IRetainedIdentityHistoryReader.cs` and related owner files — implement bounded authenticated retained-history transport/source verification.
- [ ] `../parties/eng/verify-ext-parties-1.ps1` — bind the new history seam and executable owner verification without fabricated production readiness.
- [ ] `../conversations/src/Hexalith.Conversations.Contracts/Agents` and owner runtime/client files — deliver all six restricted seams with persisted replay/delivery verification.
- [ ] `../platform/src/Hexalith.Platform.Custody` and tests — deliver configured signing/digest/rotation/revocation and full applicable custody operations.
- [ ] `../platform/apphost.cs`, restricted replay/security durability and verification — deliver the complete owner composition and recoverable persisted outcomes.
- [ ] Owner evidence packets — record exact source/worktree evidence, contracts, runnable commands and pending acceptance inputs; reconcile Agents only when complete commitments are actually accepted.

**Acceptance Criteria:**
- Given a missing production policy, provider or owner acceptance, when implementation/verification runs, then the applicable live operation fails closed and no availability is inferred.
- Given owner implementation changes, when focused and owning verification runs, then tested source behavior and persisted evidence are reported independently of outstanding full live delivery.
- Given prior Story 5.4 approval, when prerequisite work proceeds, then its frozen intent, full scope and original acceptance criteria remain unchanged.

## Implementation Notes

- 2026-10-06: User answered yes to implementing prerequisite work across Parties, Conversations and Platform. Existing owner edits are preserved; source work proceeds separately from original Story 5.4 readiness.
- Platform isolated Aspire baseline started and describe reports no resources in the default lane. Parties isolated baseline failed on a missing nested Memories/McpCli dependency; no nested submodule was initialized. Focused owner builds use isolated artifacts.
- Historical initial Parties correctness slice (superseded by evidence below): two source and two test files; Debug builds have zero warnings/errors, 8 client and 18 domain identity tests passed. Full retained-history/custody/live delivery remains pending.

## Spec Change Log

## Review Triage Log

Resolved local findings: SDK retained-history authority/custody waits and HTTP send/body reads did not bound noncooperative providers; Conversations client did not promptly cancel outstanding transport/body reads; Parties lacked entry/post-await/terminal cancellation and could consult final authority after a cancelled false custody result; Platform scaffold verifier incorrectly reported full compatibility. Meaningful cancellation/late-disposal/gate cases now pass. Full Parties json-redacted replay compatibility, Conversations C4, production custody/retention/profile/authority and H1–H4 remain unresolved; no acceptance or availability was manufactured.

## Verification

Verification commands and evidence are recorded per owning repository; no source/test success changes external acceptance fields by itself.

## Current verified delivery — 2026-10-06

The original Story 5.4 frozen intent is byte-equivalent to the initial baseline, implementation entry remains blocked-external-commitments, the four owner records remain Uncommitted and the sprint story remains backlog. The register is unchanged. There is no production policy/profile/provider approval, no live seam invocation, and no Git mutation/deployment by this workflow. Concurrent external commits are recorded as observations and preserved.

- EventStore: bounded authenticated retained-history transport/source checks, strict purpose/schema/position partition and exclusive certificates; 54 focused passes (Client16 + Server27 + unchanged Contracts11). Caller cancellation/deadline tests actually suspend providers/transport and check no evidence release; late owned responses are disposed. Normal Debug builds pass with zero warnings/errors. [Owner evidence](../../../eventstore/_bmad-output/implementation-artifacts/evidence/agents54-retained-history-reviewfix/owner-source-evidence.md).
- Parties: current and retained historical identity correctness with original positions and Branch B; cancellation before lookup, during stalled reads/custody and before terminal release. Domain/admission52 + reused Client18 + Contracts3 =73 focused passes. Normal Debug builds pass with zero warnings/errors. Broad Local remains the earlier489/490 with the unchanged json-redacted strict SDK replay compatibility failure; it is not waived. Production retention/custody, cleanup/restore and full P-01–P-10 qualification remain missing. [Owner evidence](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-owner-prerequisites-2026-10-06.md).
- Conversations: pure restricted membership/current reads/deterministic posts/logical-deletion source transitions, HTTP client/handler registration and safe incomplete-provider defaults. Earlier four full local suites passed1535; the subsequent client cancellation fix passed9 focused and all39 current Client tests. Unchanged Contracts618/Domain185/Server698 evidence is explicitly reused; no whole-solution rerun is claimed after that client-only fix. Authenticated command-source/compare-append, complete catalogue/authority and full automatic C4 discovery/backfill/worker/real receiver/remote acknowledgement remain missing. [Owner evidence](../../../conversations/docs/implementation/ext-conv-ai-1-owner-implementation-2026-10-06.md).
- Platform: 68 local signing/digest tests and a normal Debug source solution build pass with zero warnings/errors. Default/Live/invalid verifier modes make zero build/live calls. Exact-purpose fresh keys/profile, AD-29 framing, closed identity, canonical HMAC, exclusive time bounds, rotation/revocation, retained digest versions, suspended-provider input mutation and cancellation/disposal are tested. S1/S2 library fixtures do not deliver actual secret custody, approved numeric policy, independent decision authority or S3/S4/v23. [Owner evidence](../../../platform/docs/implementation/ext-secrets-1-local-prerequisites-2026-10-06.md).
- Host: truthful default Full refusal, warning-free isolated Debug LocalScaffold build and six negative zero-invocation cases. An exact-source isolated host has zero application resources; explicit Agents+Works activation refuses before Works lookup. Exact owned host cleanup completed, Works/Identity source is unchanged. CLI runtime bundle/developer-certificate warnings are separately recorded. No replicated spool, private replay/recorder/worker capabilities, production protection/FR-34, or full H1–H4 persisted migration/guard/destruction/compromise/restore qualification is present. [Owner evidence](../../../platform/docs/implementation/ext-host-1-local-gates-2026-10-06.md).

Full Conversations/custody/host tasks above remain unchecked because the complete required owner artifacts are absent. Local subtask specs record only their actually verified delivery; they do not narrow this parent or the original story.

## Pragmatic decision recommendation

1. Reuse an existing approved retention policy only if it explicitly covers the Party-to-stable-human-actor history after profile erasure, with a versioned duration/trigger and expiry/backup/restore rules. This avoids a parallel policy but a generic audit policy is insufficient evidence of coverage.
2. Otherwise approve one narrow finite actor-binding policy retaining only attribution identity, binding interval/version and original source position, independently encrypted from the erasable profile. It provides past-action attribution without retaining names/contact details or granting current authority. It still requires approved expiry, cleanup and restore behavior. The prototype currently supports binding-effective-at expiry; erasure-triggered expiry needs an explicit contract adjustment.
3. Keep historical attribution disabled until one of those policies is approved. Returning Unavailable without current-profile substitution is a valid temporary state, but cannot complete historical-proof acceptance.

Recommended implementation: reuse the existing Party/Conversation aggregates and shared EventStore seams, use a small Platform library behind the existing qualified custody backend, and compose one durable denial spool on a backend already qualified for the owner's failure model. Do not create a new database/service or emulate production approvals to bypass the missing decisions. Owners still need to supply the policy ID/version, approved duration/trigger/cleanup/restore, numeric L/S/O/H/R profile, custody/authority/spool/credential/protection targets and complete exact-target qualification acceptance.

## Applied recommendation — 2026-10-06

The user instructed “apply recommendation”. Engineering now selects one minimal actor-history policy proposal because no located approved policy explicitly covers post-profile-erasure actor relationships. [Proposed policy](../../../parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md) defines allowed data, required finite duration, the supported immutable effective-at clock, independent profile erasure, cleanup and nonrollback restore obligations. No numeric production duration or owner approval was invented.

The existing Parties query service now enforces explicit matching policy on human-binding reads and rejects configuration withdrawal/change across source/custody awaits and final authority. Organization Branch B remains usable without human policy. [New source evidence](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md) records 78 current focused passes (26 new policy cases), normal Debug build with zero warnings/errors, exact hashes/commands and separate prior broad replay blocker. It supersedes the earlier query-source snapshot, without rewriting prior evidence.

The selected construction remains the existing Party/Conversation aggregates and EventStore seams, the small shared Platform signing library behind qualified custody, and one independent spool on already-qualified infrastructure. Those local aggregate/SDK/signing changes are already implemented; no qualified production custody or spool target was found or fabricated. Production history, complete live owner acceptance, and full Story 5.4 therefore remain disabled/blocked. This engineering instruction does not provide formal retention/custody/spool/host qualification.

## Human approval of local recommendation delivery — 2026-10-06

The user stated “I approve” after reviewing the implemented policy guards, minimal policy proposal and 78-pass evidence. [Acceptance record](story-5-4-human-approval-2026-10-06.md) records approval of that concrete local delivery. Numeric production retention, qualified custody/cleanup/restore and complete owner qualification remain missing; no register availability or full Story 5.4 completion is inferred. A fresh independent-review launch still fails with the agent thread limit, so review remains pending separately from human acceptance. No source change, deployment or Git mutation accompanies this approval record.

## Concrete retention/custody proposal — 2026-10-06

The user asked to supply the remaining retention/custody inputs and then requested a deeper pragmatic comparison. [Decision packet](story-5-4-retention-custody-proposal-2026-10-06.md) now proposes 365 fixed days from binding-effective-at, supplies isolated valid JSON, compares existing custody/managed adapter/Vault/local options and defines exact expiry/destruction/restore qualification exercises. Existing SDK configuration validation passes. The binding-start clock does not provide a full year after every later action; that limitation is explicit. The new duration is a recommendation prepared after the prior approval, not silently approved or loaded into production. No production custody implementation was found; Azure vault discovery failed on missing cached authentication. No qualified target, live result or owner availability is fabricated.

## Accepted 365-day lifetime application — 2026-10-07

The user's “do recommended” instruction accepts the concrete duration and scope in the October 6 proposal. [Applied policy and cleanup](story-5-4-retention-application-2026-10-07.md) now supply 365 fixed days from binding-effective-at in Parties host configuration and a bounded Platform operation through existing custody. Both Debug builds passed; 191 focused/regression tests passed. Numeric duration approval is no longer pending. A qualified production custody/lifecycle/restore target, complete copy receipts, successor-after-expired-predecessor proof and full owner compatibility commitments remain pending. No dependency record was promoted. Independent review is pending because fresh agents cannot be spawned.


## Independent review completed — 2026-10-07

The earlier pending-review/task-limit status is superseded: all three fresh Codex reviewers completed. Two groups were fixed (synchronous cleanup timeout/cancellation and stale ADR approval wording); four new regression cases failed before the fix and all 115 Platform tests passed afterward, with a zero-warning/error Debug build. The earlier 80 Parties tests are reused, with matching code/test source hashes. [Review results](lifecycle-review-2026-10-07/README.md) and [separate review-fix evidence](tests/actor-history-lifecycle-review-fixes-2026-10-07/evidence.json) record the closure. Eighteen other findings form 15 unresolved earlier prerequisite groups in the deferred ledger. The accepted local policy/cleanup child is done; full Story 5.4 and production qualification remain incomplete. No deployment, staging, commit or push occurred.


## Reviewed identity completion and readiness corrections — 2026-10-07

The resumed build completed [a separate correction child](spec-5-4-identity-completion-and-readiness-fixes.md) inside this parent's accepted scope. Current query/client completion checks reject expiry, clock rollback and authority withdrawal; retained historical replies preserve closed action-time intervals until custody expiry. The owner verifier independently reconstructs exact scope/version/interval/custody/source transitions, bounds streaming receive, rejects contract-invalid JSON/timestamps/revisions and redirects, and normalizes caller-relative paths before changing directory.

All three independent reviewers completed; seven narrow patch groups were applied and verified. Root's fresh final checks passed **238 source tests and 42 synthetic cases**, with both Debug builds reporting zero warnings/errors. [Current evidence](tests/identity-completion-2026-10-07/README.md) preserves initial/review snapshots, exact commands, XML/logs and hashes; no earlier passing lane was reused. The ten local lifecycle-review groups B03, E02, V01, V02, B08/E05/V04, B09, B10, B11/E03, E04 and V03 are addressed by this child. B01/B02/B04/B05/B06 and owner privacy/schema-drift qualification remain open, alongside production custody/destruction/restore, source authority freshness and complete exact-target owner qualification.

Installed Parties now includes the earlier identity APIs; newer retained-history/policy/cancellation/completion work remains selected local owner changes beyond the observed commit, not an accepted installed target. Fresh owner requests still have no complete accepted target/date/command packets. The four records remain Uncommitted, the register and original Story 5.4 spec/sprint remain byte-equivalent to baseline, and this parent remains in-progress. The historical broad Local json-redacted replay gate remains failed. Source/fixture success does not change full Story 5.4 readiness or acceptance.

## Direct owner commitment instruction — 2026-10-07

The user subsequently stated “I owner do commitments”. [Recorded owner packet](story-5-4-owner-commitments-2026-10-07.md) now names the project owner acting in the four relevant maintainer responsibilities and makes Hexalith.Platform the secret-custody repository. This later instruction authorizes updating those acceptance fields; the preceding byte-equivalent register/spec observation describes the earlier correction completion, not the state after this owner update. Full delivery contracts, evidence levels, consumers, Branch B and the accepted retention policy remain intact. Immutable complete targets, integration dates and complete executable commands are still missing, so the records remain Uncommitted and this parent remains in-progress. No new source implementation, deployment, Git mutation, owner message or live seam execution accompanies the commitment record.

## Active Parties continuation — 2026-10-07

The user explicitly resumed this parent: first resolve the Parties protected-history replay failure, preserve strict retained-history validation, and capture verification evidence; then continue custody/expiry/restore qualification toward a complete Parties delivery target. This pass prioritizes the owning Parties and shared EventStore/Platform changes required for that single delivery. Other owner tasks remain in this parent and must not be marked complete by this pass. Existing implementation approval covers this continuation. Preserve original Story 5.4 draft/backlog and dependency availability until their actual gates pass.

Active work:
- [x] Reproduce `PartyDomainProcessorValidationTests.ProcessAsync_ProtectedHistoricalPayloadWithDestroyedKey_RedactsAndContinuesRehydration` using source Debug builds. Correct the replay/protection boundary in the owning technical/domain files; retain source-envelope immutability and strict metadata/tenant/domain/aggregate/sequence/checkpoint validation. Add focused negative regressions proving an untrusted redacted label or malformed history cannot bypass validation. Do not merely rename the failing test or alter its expected valid lifecycle behavior.
- [x] Run the complete existing `../parties/eng/verify-ext-parties-1.ps1 -Mode Local` source matrix with fresh logs/XML, plus affected owning replay/security tests. Capture canonical repository HEADs, worktree changes, exact commands, source/artifact hashes, totals, and any separate environment blockers. Never suppress analyzers, skip the failed lane, or weaken the gate.
- [ ] Continue qualification using the accepted 365-day effective-at policy and existing custody/cleanup/retained-history seams. Add meaningful executable missing expiry/rebind/successor-after-expired-predecessor and restore anti-resurrection checks and correct discovered source failures. Review current custody-provider and exact-target prerequisites from tracked source; implement feasible safe missing qualification seams in their owning technical module. Use isolated synthetic custody only as explicitly labelled source evidence. No credentials, production policy choices, deployment or live destructive actions are inferred.
- [x] Produce a linked current Parties evidence packet and update this parent and the owner commitment packet with actual delivered coverage and exact remaining live target requirements. Keep incomplete production qualification and full delivery acceptance explicit. No staging, commit, push or submodule changes.


## Current Parties replay and lifecycle continuation — 2026-10-07

[Current Parties owner packet](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-replay-and-lifecycle-2026-10-07.md) and [fresh verification evidence](../../../parties/_bmad-output/implementation-artifacts/tests/owner-continuation-2026-10-07/README.md) supersede the historical broad Local replay blocker as current source evidence. The exact destroyed-profile test passed on the initial source Debug build; its existing committed JSON adapter was then hardened against stored/provider redaction labels, actor-history fallback, malformed source metadata, scope and checkpoint. Shared retained-source metadata and sealed-byte immutability were also hardened. Twelve Parties and six EventStore regressions failed before the corrections and passed afterward.

The complete original Local matrix now passes **784/784** across all ten required lanes, with zero errors/failures/skips/not-run and all Debug builds reporting zero warnings/errors. Fresh unchanged Platform custody verification adds **115/115**: **899 distinct final passes**. Focused 29/36 results overlap that matrix and are not double-counted. Three failed attempts remain captured: a concurrent Client test compile error resolved by its owner, then two UI source-root discovery attempts; the final runner passes an explicit owning source root and restores its prior environment value while retaining every assertion and gate.

New accepted-365-day source cases verify rebind boundaries and original positions through serialized restore, exclusive expiry despite restored readable bytes, and restarted source-reader denial under fresh lifecycle. The real source path explicitly denies live-successor access when an expired/destroyed predecessor prevents complete lifecycle proof. The current profile exclusion certificate cannot encode an expired transition/version/destruction receipt. A versioned actor-free continuation contract, exact custody receipts/all-copy coverage and qualified nonrollback restoration remain required; they were neither invented nor claimed complete. Active qualification work therefore remains unchecked.

Some Parties changes/evidence were included by concurrent external commits during verification. Machine evidence preserves initial/final canonical HEAD observations, owned baseline diffs, source/artifact hashes and unrelated worktree observations. This workflow performed no staging, commit, push, submodule update, deployment, owner message or live seam invocation. Original Story 5.4 remains draft/backlog; all four records retain their existing status and missing complete delivery fields. This parent remains in-progress and other owner tasks remain open.

## Continuation Review Triage Log — 2026-10-07

All three context-free reviewers completed against the scoped seven-file owner diff and current delivery claims. Each finding was assessed before grouping. Full parent status remains in-progress; review applies to the delivered Parties continuation. These narrow corrections use existing private/SDK seams and the already accepted source-completeness, strict protection and cancellation requirements; no public contract or policy changes are introduced.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | Transport object state is JsonElement; SnapshotAware runs only for identity commands, so the new typed boundary is bypassed on ordinary lifecycle commands. Use the existing normalization seam for every admitted command. |
| B2 | high | patch | Existing destroyed-key snapshot fallback returns null while retaining a positive checkpoint and tail. The new required-snapshot guarantee can be invalidated after validation; deny incomplete reconstruction instead. |
| B3 | high | patch | The new output check admits JSON with Protected/ProviderOpaque or future metadata; PayloadProtectionResult couples bytes to protection state. Require current Unprotected metadata. |
| B4 | medium | patch | DomainServiceCurrentState defines checkpoint zero as no snapshot, but a supplied snapshot is applied over a full prefix. Reject this inconsistent combination. |
| B5 | medium | patch | The missing-snapshot case also violates event count/first sequence; it does not independently exercise required snapshot presence. Correct its tail. |
| B6 | medium | patch | New LINQ validation and eager cloning do not observe cancellation until both full passes finish. Observe cancellation while validating/capturing each event. |
| B7 | medium | patch | A provider can mutate the detached envelope bytes before throwing; local redaction then parses those corrupted bytes. Keep provider input separate from the validated fallback source. |
| B8 | medium | patch | Excluded positions cover only the redacted variant; metadata-version/contract/payload-version guards at excluded positions lack independent regressions. Add those direct variants. |
| E1 | medium | patch | Same demonstrated zero-checkpoint snapshot inconsistency as B4. |
| E2 | high | patch | Same demonstrated positive-checkpoint loss after snapshot unprotection as B2, including a provider returning null. |
| E3 | high | patch | Same demonstrated inconsistent JSON/protection-metadata admission as B3. |
| E4 | medium | patch | Same demonstrated full capture cancellation gap as B6. |
| V1 | medium | patch | Preverified gap: existing processor source-byte assertions use nonmutating providers, so removal of detachment would not fail them. Add a mutating successful-provider case. |
| V2 | medium | patch | Preverified gap: the missing-snapshot fixture is rejected by independent sequence/count checks. Supply the otherwise valid sequence-2 tail. |

Patch groups: transport normalization (B1), snapshot completeness (B2/B4/E1/E2), provider metadata (B3/E3), independent missing-snapshot fixture (B5/V2), cancellation during capture (B6/E4), provider mutation/fallback verification (B7/V1), and excluded metadata variants (B8). Preserve all passing original lifecycle semantics and fail closed when required checkpoint state is unreadable; never reconstruct a prefix from only its tail.

## Reviewed Parties continuation — final result

All three independent reviewers completed. All 14 individual findings above were resolved in seven narrow implementation/verification groups; a complete serialized DomainServiceRequest now enters the same strict boundary, unreadable required snapshots cannot become empty-tail reconstruction, provider metadata must describe supported plaintext, cancellation is observed during capture and provider mutation cannot corrupt local redaction. The missing-snapshot fixture now isolates its guard and excluded source metadata variants execute. Root read the final owner diff and independently verified **802/802** tests in all ten original Local lanes, each with zero-warning/error Debug builds. The unchanged Platform custody source/artifact hashes support retaining its 115/115 result: **917 distinct verified passes**. [Final evidence](../../../parties/_bmad-output/implementation-artifacts/tests/owner-continuation-2026-10-07/review-final/README.md) and [review record](parties-continuation-review-2026-10-07/README.md) preserve original and reviewed snapshots.

Actual HTTP verification is separately blocked: all 13 endpoint cases fail at the current SDK startup audit before requests because the Parties pre-mapped Dapr/actor routes lack required sidecar policy and /healthz is anonymous outside the allowed probes. The complete serialized request fallback passes; no HTTP pass or policy waiver is claimed. Parties host/fixture adoption of the SDK sidecar/workload security contract is a further owner compatibility prerequisite.

Positive successor access after predecessor destruction, qualified production custody, all-copy destruction receipts, nonrollback restore and complete P-01–P-10 exact-target qualification remain open. The active qualification checkbox remains unchecked, the full parent remains in-progress, and original Story 5.4/dependency gates are unchanged. No staging, commit, push, deployment or live seam invocation was performed by this workflow.

### Review closure evidence

| Findings | Executed coverage / final source correction |
| --- | --- |
| B1 | ProcessAsync_SerializedLifecycleTail_UsesValidatedProtectionBoundary round-trips a complete DomainServiceRequest; all validated command wrappers normalize before protection. Actual HTTP remains separately startup-blocked. |
| B2/E2 | ProcessAsync_RequiredSnapshotUnavailable_RejectsIndependentValidTail covers null/JSON null/undefined and destroyed profile/history failures; required snapshots never fall back to an empty prefix. |
| B3/E3 | Provider protected/opaque/future metadata variants reject in ProcessAsync_UntrustedReplayHistory_RejectsBeforeLifecycleEffect. |
| B4/E1 | ProcessAsync_ZeroCheckpointWithSnapshot_RejectsOtherwiseValidTail; valid positive snapshot reconstruction also passes. |
| B5/V2 | Corrected missing-snapshot case uses only sequence 2 with checkpoint 1/current 2, independently isolating snapshot presence. |
| B6/E4 | ProcessAsync_CanceledDuringTailCapture_StopsBeforeReadingRemainderOrCallingProvider stops at the second visited event and preserves cancellation identity. |
| B7/V1 | ProcessAsync_MutatingProvider_PreservesStoredBytesAndLifecycleReplay covers mutation plus successful JSON or destroyed-profile failure; source bytes/metadata stay intact. |
| B8 | All retained/excluded metadata corruption variants execute in UnsupportedStoredMetadata_DeniesBeforeCustody; the final retained-source class has 39 passes. |

Every source assertion above executed in root's final 802-test matrix. The four added HTTP cases remain part of the separately blocked 13-case class and are never counted as passing.

## Active HTTP and qualification continuation — 2026-10-07

The user explicitly requests continuation of this approved parent. First resolve the Parties HTTP startup audit blocker by adopting the existing EventStore SDK sidecar/workload security contract for Dapr subscription/configuration, actor routes and `/healthz`. Update the authenticated endpoint fixture without weakening startup audit, authorization, strict replay or retained-history validation. Run all 13 existing `PartiesProcessEndpointTests`, then rerun the complete ten-lane owner Local matrix. Capture commands, logs/XML, canonical revisions, worktree observations, source/artifact hashes and independent review in a new packet separate from every prior packet.

Then advance custody/expiry/restore qualification toward a complete Parties target using the accepted 365-day effective-at policy. Inspect current owning technical seams and add feasible executable source qualification without inventing a new public continuation contract, production custody or live destruction/restore results. Keep positive successor-after-expired-predecessor and any other unresolved contract requirements explicit. Prior owner tasks remain open; do not implement unrelated Conversations or host deliverables in this continuation. Preserve unrelated edits and the original Story 5.4 draft/backlog and external availability gates. Do not stage, commit, push, deploy, update submodules or send owner messages.

Continuation tasks:
- [x] Parties host and authenticated test fixture adopt SDK security policies; all original 13 HTTP endpoint cases execute and pass, with meaningful denial/sidecar regression evidence.
- [x] Complete owner Local source matrix executes afresh with retained assertions/gates and separate exact command/log/XML/hash evidence.
- [x] Feasible custody/expiry/restore source qualification advances with executable evidence; missing production contracts, target qualification and all-copy/restore receipts remain explicit.
- [x] Independent review and root verification are recorded in the fresh packet, parent and owner commitment record without claiming full parent completion.

For this continuation, the frozen matrix's applicable scope is Branch B identity, retained history and fail-closed missing custody/policy. Trusted-envelope, Conversations and independent spool remain other owner tasks and cannot be marked delivered by Parties source evidence.

### HTTP continuation delivered for independent review

[Fresh Parties packet](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-http-and-source-qualification-2026-10-07.md) records 850/850 across the ten owner Local lanes, all original 13 HTTP cases, 28 new real SDK security cases, seven new serialized-restore source denials, 12 additional middleware cases and fresh 115/115 Platform custody tests: 977 distinct passes. All owning source Debug builds report zero warnings/errors. Root read both scoped owner patches and independently checked every XML test, required class and warning-free build; the original 13 executed names are preserved exactly. Review is of this bounded continuation; the incomplete parent remains in-progress and the original Story 5.4 remains draft/backlog.

The fixture supplies synthetic healthy backend status and ephemeral Development credentials; only HTTP authentication, policy metadata and production replay behavior are established. Restore cases use synthetic independent custody. No production health/custody readiness, all-copy destruction receipt, qualified nonrollback restore or positive successor continuation is claimed. Independent review and final root source/artifact/gate checks are pending in a separate [root packet](tests/parties-http-qualification-2026-10-07/root-matrix-audit.json).

### HTTP continuation Review Triage Log

All three context-free layers completed. Root rendered each verdict before grouping; [individual findings](tests/parties-http-qualification-2026-10-07/review/findings.md) and the initial diff remain separate from earlier reviews.

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| B1 | medium | patch | Host configuration can inherit Authority alongside the fixture's symmetric key, which the SDK rejects; isolate the fixture's credential and workload options without changing real handlers/policies. |
| B2 | medium | patch | The two switch cases share a request containing both channel and caller header. Make caller-header-only and channel-only independent vectors. |
| B3 | medium | patch | Current case executes the intended conflicting-credential guard, but supplies no valid human-only JWT. Retain conflict coverage and add human-only denial under the configured bearer contract. |
| B4 | medium | patch | Foreign signature fails signature verification; conflicting unsigned caller fails a different guard. Add a correctly signed disallowed-caller vector under the exact configured allow-list. |
| B5 | low | reject | The actual 401/empty-body checks and unchanged ASP.NET authorization middleware prove missing-channel rejection before route execution; command counts add no actor/consumer proof. Complete positive persisted actor/pubsub delivery remains explicitly unqualified. Instrumenting those backends adds complexity beyond a direct correction; no complete dispatch/state-store claim is made by this continuation. |
| B6 | medium | patch | Validation covers existing endpoints only. Require every SDK catalog POST route before checking its unchanged policies so removal cannot silently pass. |
| B7 | medium | patch | Existing variants demonstrate altered decoded evidence rejection. Preserve them and add fixed decoded restored evidence against independently changed accepted receipt/revision, explicitly remaining synthetic. |
| E1 | medium | patch | ASP.NET matches a trailing slash while the infrastructure exemption compares an exact path. Normalize trailing slashes and execute direct no-probe plus real channel cases. |
| V1 | medium | patch | Normal-CI source assertions require the removed route/ACL documentation strings. Restore accurate documentation alongside the new SDK security explanation and execute the unchanged architecture guard with source discoverable. |

Narrow patch groups: fixture isolation, independent credential vectors, expected catalog presence, independent restored-lifecycle changes, trailing-slash infrastructure normalization and preserved host documentation. No public contract, production policy or approval change is needed. Shared replay files were edited concurrently after the first matrix; the earlier source hashes remain historical evidence and final fresh verification must capture that preserved current source.

### HTTP continuation — final reviewed result

The Parties startup audit blocker is resolved through existing SDK sidecar policies on subscription/configuration/actor routes and `/healthz`, with exact workload-operation policies retained. Root's final fresh ten-lane Local matrix passes **859/859**, including all **13 original HTTP names/cases** and **35 SDK security cases**. Fresh **30 health/middleware + 1 unchanged architecture + 22 shared replay + 115 Platform custody** checks give **1,027 distinct passes**; all source Debug builds have zero warnings/errors and all tests have zero failures/errors/skips/not-run. [Final owner evidence](../../../parties/_bmad-output/implementation-artifacts/tests/http-qualification-2026-10-07/review-final/README.md) and [root packet](tests/parties-http-qualification-2026-10-07/README.md) preserve exact commands, logs/XML, canonical observations, owned dirty-baseline diffs and source/artifact hashes separately from prior packets.

All three independent reviews completed. Eight findings were corrected in six narrow groups; B5 remains explicitly rejected low with the reason above. The hostile inherited configuration, independent caller/channel/human/disallowed-caller vectors, complete catalog inventory, `/healthz/` alias, independent changed receipt/revision and unchanged architecture guard all execute. Root independently checked all required classes and original names, matched **3,740 source files** before/after/current and **4,835 artifacts**, and confirmed the SDK audit/authentication source, frozen intent, original story/register/sprint and accepted policy hashes are unchanged. Two final attempts are retained: a complete passing run invalidated by a new unowned source file, and a partial run stopped by six transient compile errors during concurrent shared replay edits. The final complete run has no source drift.

Nine new serialized-restore/lifecycle cases now advance the real source-reader qualification, including fixed restored decoded evidence against independently advanced synthetic lifecycle receipt/revision. Existing accepted-365-day rebind, exclusive-expiry, original-source-position and predecessor-destruction denial cases execute afresh. [Complete-target qualification status](tests/parties-http-qualification-2026-10-07/qualification-status.md) records remaining actor-free continuation proof, installed production custody, durable all-copy destruction/lost-ack receipts, nonrollback restore and complete live P-01–P-10 target/date/commands. The HTTP fixture has synthetic healthy backend status and ephemeral Development credentials; no production health/custody readiness or positive persisted actor/pubsub delivery is inferred.

This bounded HTTP/source continuation is complete. The earlier full custody/successor/restore qualification checkbox remains unchecked, the parent remains in-progress and other owner tasks remain open. The [owner commitment observation](story-5-4-owner-commitments-2026-10-07.md) supersedes the historical HTTP blocker without filling missing complete delivery fields. Original Story 5.4 remains draft/backlog and external availability is unchanged. All unrelated edits and concurrent external history are preserved; this workflow performed no staging, commit, push, submodule update, deployment or owner message.

After the successful stable-source run and independent root audit, unowned shared replay files changed further. An additional full attempt again passed all 859 Local and 1,027 distinct cases, but correctly exited 1 when its source-consistency assertion detected further edits and a new admission test. [Later attempt](../../../parties/_bmad-output/implementation-artifacts/tests/http-qualification-2026-10-07/review-follow-up-source-drift-03/README.md) is preserved without waiver. The stable packet remains the qualified source snapshot; the whole later worktree is not qualified. [Post-closure checks](tests/parties-http-qualification-2026-10-07/root-post-closure-check.json) confirm all ten reviewed files, SDK security source, protected gates and artifacts still match; later unowned source is explicitly outside that snapshot.


## Active dependency-ordered query deadline continuation — 2026-10-07

The user requests the first implementable unmet owner task, its implementation, independent review and required verification. This is a bounded continuation of the first open Parties verifier/qualification task, with existing full parent scope retained. Current owner investigation observes Parties EventStore package floor 3.115.0 already present; packaged release/consumer qualification remains separate. Positive successor-after-expired-predecessor requires the unresolved authenticated actor-free continuation contract; do not invent it. Rollout inventory and independent current actor-revocation authority remain qualification prerequisites. The earliest source gap implementable through existing seams is the recorded B06 Party query deadline: current and retained-history custody awaits have cancellation but no finite whole-operation bound.

Implement in the owning sibling Parties repository, reusing shared EventStore stream/custody abstractions. Preserve existing edits, source history, Branch B, accepted 365-day effective-at/exclusive-expiry policy and every prior approval. Capture the required Aspire baseline through the CLI and retain exact environmental blockers without initializing nested submodules. Add a finite whole-query deadline aligned with the existing SDK maximum bound, measured through the injected TimeProvider and shared across all awaits. A provider ignoring cancellation must not hold either query indefinitely or release late identity evidence. Preserve exact caller-cancellation propagation; timeout is a safe typed Unavailable with no identity evidence. Invalid configured operational bounds fail closed. Do not alter retention policy, startup authorization, source completeness or custody/freshness checks.

Continuation tasks:
- [x] Implement finite current/history query deadlines in `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` and any narrowly necessary owning operational options/helpers; reuse the existing SDK/custody ports.
- [x] Add meaningful suspended-provider deadline, cumulative-bound, late-result/no-evidence and cancellation regressions under `../parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs`; preserve existing original tests and assertions.
- [x] Run the complete existing ten-lane `../parties/eng/verify-ext-parties-1.ps1 -Mode Local` matrix against current sibling owner sources, plus any additional affected operational-configuration coverage, with fresh exact commands/logs/XML/source/artifact hashes and canonical VCS/worktree observations.
- [x] Record independent review and root verification in fresh packets; append actual coverage and explicit remaining qualification requirements to this parent and owner evidence/commitment observations.

Given stalled noncooperative source/custody, when the one whole-query deadline expires, then current and historical reads return Unavailable without evidence and request provider cancellation. Given a caller cancellation before or during a read, when either query exits, then the original caller cancellation remains observable without a translated successful or failed identity verdict. Given multiple provider waits or delayed completion, when the combined operation exhausts the deadline, then a late result cannot reset the budget or become resolved evidence. Given missing production providers/acceptance, when Local tests pass, then no production qualification, complete target/date, owner acceptance or availability is inferred.

The parent stays in-progress and its incomplete top-level tasks remain unchecked. The original Story 5.4 stays draft/backlog: do not synchronize its sprint status from this prerequisite continuation. Do not stage, commit, push, deploy, update submodules, send owner messages or invoke unavailable live seams. Preserve the initial state at `tests/parties-query-deadline-2026-10-07/initial-protected-state.json`. Complete and review this selected source slice; do not claim every full parent matrix row or owner prerequisite has passed.


## Query deadline Review Triage Log — 2026-10-07

All three context-free review layers completed before classification. The review target is the five-file scoped diff against the captured initial Parties source. The full parent remains in-progress.

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| B1 | medium | defer | Synchronous authority admission and local folding were already synchronous; the deadline guards refuse post-budget evidence but cannot forcibly terminate that work. Existing authority/processing responsiveness remains an owner qualification requirement, separate from the selected source/custody wait correction. |
| B2 | medium | patch | Current options access and historical deserialization precede deadline construction. Capture the monotonic start at entry and charge setup time to the same budget; preserve safe failure scope. |
| B3 | high | patch | Independent .NET 10 reproductions show a provider callback registered last can block linked-token cancellation before WaitAsync receives it. Private waiting must complete independently of provider cancellation callbacks. |
| B4 | medium | defer | Noncooperative synchronous provider invocations can retain workers after timeout. The previous direct invocation also retained a request worker indefinitely. The delivered source provider ports do not establish qualified cancellation/resource reclamation; bounded abandoned-work admission belongs to shared runtime/host qualification, not an invented local capacity policy. |
| B5 | low | patch | The second await observes an already terminal query, not actual late-provider completion. Synchronize provider completion before the terminal call-count assertions without adding an internal diagnostic API. |
| B6 | low | patch | The filed vectors deterministically cancel the caller first. Add a controlled deadline/fault-before-cancellation case where cancellation occurs before verdict release; cancellation after an already completed verdict is not required to change it. |
| B7 | medium | patch | Configured shortened budgets have only denial-path execution. Add current/historical success immediately before the exclusive budget boundary. |
| E1 | high | patch | The second independent console reproducer confirms the same shared-token callback timeout deadlock as B3. Separate wait cancellation from asynchronously requested provider cancellation. |
| V1 | medium | patch | Preverified gap: dependency expiry tests stop inside ReadAsync, while the final-authority test only cancels the caller. Add monotonic deadline expiry during final admission, with timer delayed, and assert cleared current/historical evidence. |

Patch groups: setup budgeting (B2), provider-callback isolation (B3/E1), late-completion synchronization (B5), ordered cancellation race (B6), shortened-budget success (B7), and final-authority expiry coverage (V1). Existing synchronous authority and abandoned-work resource cooperation remain explicit incomplete qualification requirements (B1/B4).

Review corrections and root fresh current-source verification are pending. The initial 893-pass matrix is a stable historical snapshot; subsequent unowned SDK and test changes were observed and preserved. No full current-worktree qualification is inferred from that earlier packet.


## Query deadline continuation — final verified result

The selected B06 source continuation is complete: current and historical source/custody reads use one monotonic budget from query entry, invalid bounds fail closed, noncooperative invocation/await and blocking cancellation callbacks cannot hold query completion, and late results cannot release identity evidence. Caller cancellation preserves its original token. The private waiting signal is independent from asynchronously requested provider cancellation; remaining setup time is deducted from the same default/maximum thirty-second budget. Existing shared EventStore/Platform ports and every retention/source/authority check are retained.

All three independent review layers completed. Seven of nine individually triaged findings were corrected in six narrow groups, with the twelve concurrent blocked-callback cases preserved. B1/B4 remain explicit deferred synchronous-authority/resource-reclamation qualification requirements; no forcible termination or production readiness is claimed. The corrected query class passes 141 focused cases. Root then ran the complete current ten-lane Local matrix afresh: **919/919 distinct passes**, zero errors/failures/skips/not-run and ten zero-warning/error Debug builds. The six configuration cases now execute within the preserved stronger owner runner and are counted once. All thirteen original HTTP names, thirty-five SDK security cases and twenty-six review cases execute. Root independently verified **3,749** before/after/current source hashes and **4,667** artifacts, protected story/register/sprint/policy hashes and frozen parent intent. [Fresh root evidence](tests/parties-query-deadline-2026-10-07/README.md), [final owner run](../../../parties/_bmad-output/implementation-artifacts/tests/query-deadline-2026-10-07/review-final/evidence.json) and [owner packet](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-query-deadline-2026-10-07.md) preserve exact commands/logs/XML, canonical observations, scoped diffs and separate failed/historical attempts.

The initial 893-pass packet remains a stable historical snapshot; subsequent concurrent SDK/test/verifier changes were preserved and the final fresh run has no source drift. The owner verifier's additional configuration class and the shared router changes were external observations, not silently attributed to this implementation; SDK/router immutability is not claimed. The failed narrow race fixture attempt remains recorded.

The pre-edit isolated Aspire baseline separately fails in package mode on the missing SDK sidecar extension at Program.cs lines 64/66, and describe confirms no AppHost runs. The 3.115.0 floor alone does not resolve packaged host/security compatibility. Source Local results do not replace this gate. Actor-free successor continuation, qualified production custody/current authority, all-copy/lost-ack destruction receipts, nonrollback restore and complete live P-01–P-10 target/date/accepted commands remain missing. No delivery fields, owner acceptance or availability were invented.

Only this selected source continuation is complete. The first full Parties owner task, full custody/successor/restore qualification and other owner tasks remain unchecked; the parent stays in-progress. Original Story 5.4 remains draft/backlog and its complete dependency register/gates, Branch B, accepted 365-day policy and prior approvals are unchanged. This workflow performed no staging, commit, push, branch, submodule update, deployment, owner message or live consuming seam invocation.


## Active full owner delivery continuation — 2026-10-08

The user resumes the approved complete prerequisite work, explicitly preserving full scope, Branch B, the accepted 365-day binding-effective-at/exclusive-expiry policy and all existing approvals. This latest instruction supersedes the earlier bounded-continuation scheduling restrictions; it does not mark earlier incomplete owner requirements delivered. Inspect current owning sibling Parties, EventStore, Conversations and Platform sources, preserve unrelated edits, then complete remaining feasible implementation in dependency order. Required independent review and fresh verification apply to delivered work. Do not repeat scope, retention or implementation approvals.

Continuation tasks:
- [x] Inspect present progress and canonical source/worktree observations against the complete owner contracts and prior packets.
- [ ] Complete remaining feasible Parties/shared EventStore delivery and verification; identify any authenticated actor-free continuation, authority, production custody or restore decisions that cannot be inferred.
- [ ] Complete remaining feasible Conversations six-seam delivery and persisted source/publication verification, preserving independent authority/approval/receiver gates.
- [ ] Complete remaining feasible Platform custody then host composition/durability delivery and verification, preserving complete S1–S4/v23 and H1–H4 requirements.
- [ ] Prepare four complete owner delivery packets for EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1. Map every full contract requirement to actual delivered evidence or an exact remaining implementation/owner-input blocker. Include reproducible commands and exact immutable targets only when complete work supports them; distinguish current canonical source observations and Local verification from accepted full targets/live qualification.
- [ ] Independently review delivered changes and evidence claims, correct findings and perform root verification. Record unanswered integration dates and owner decisions without inventing them.

Missing integration dates, numeric trusted-envelope L/S/O/H/R profile, independent decision/actor authority, production custody/protection/spool targets or undecided public contracts must be brought to the root for a concise owner question while independent authorized work continues. Do not block unrelated source/package/packet preparation on these inputs. Do not substitute fixtures, partial checkout revisions, missing-provider adapters or Local success for full delivery or live Level 4 qualification. No target/date/command status promotion without its actual complete basis. Story 5.4 remains draft/backlog and the prerequisite parent remains in-progress until all complete tasks and gates pass.

The root initial preservation snapshot is `tests/owner-delivery-2026-10-08/initial-state.json`. Capture scoped before/after diffs and fresh exact build/test commands, logs/XML and source/artifact hashes. Existing historical evidence remains separate. No staging, commit, push, branch, submodule initialization/update, deployment, owner messages or unavailable consuming-live seam invocation is authorized.


### Owner integration date input — 2026-10-08

The owner supplied “today” for the shared integration date question. The four records now have owner-supplied integration date 2026-10-08, recorded in the register and existing commitment table. This authorized date-only register delta is audited at `tests/owner-delivery-2026-10-08/authorized-date-update.json`; every other register contract/status/target/command field is unchanged. Complete targets, full accepted commands, implementation and qualification still remain open. Original Story 5.4 stays draft/backlog; scope, Branch B, 365-day policy and prior approvals are preserved.


## Full owner continuation Review Triage Log — 2026-10-08

All three context-free layers reported before individual classification/grouping. Review input is `tests/owner-delivery-2026-10-08/independent-review-input.json`; it uses captured owned source deltas rather than externally changed Git HEADs. Full delivery is incomplete, so the prerequisite parent remains in-progress and original Story 5.4 remains draft/backlog.

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| B1 | high | patch | Page validator and evidence capture enumerate provider collections separately; records can change while preserving sequence. Capture bounded owned records/metadata and validate that same snapshot before release. |
| B2 | medium | patch | The SDK checks only ProviderOpaque. Future/unknown metadata can be certified. Use defined readable states and shared current carrier validation on the owned snapshot. |
| B3 | medium | patch | State/version alone omit field/ASCII/flag carrier rules; custom authoritative adapters may provide malformed current metadata. Reuse the shared validator. |
| B4 | medium | patch | New ToDictionary copies a provider-controlled collection without enforcing eight-entry and key/value bounds. Bound traversal before retention and validate the captured carrier. |
| B5 | medium | patch | Only selected fault types reach terminal caller-first checking. NotSupportedException from a collection after caller cancellation escapes. Check original caller cancellation on every exceptional exit; uncancelled unexpected faults still propagate. |
| B6 | low | patch | A cancelled provider task with live caller/deadline is mislabeled timeout; evidence remains denied. Distinguish this from actual monotonic/timer expiry without changing public contracts. |
| B7 | medium | defer | Private Task.Run bounds verdict wait but cannot reclaim indefinitely blocking invocations/callbacks. Prior authority/provider paths already required cooperation. No qualified concurrency/execution-isolation/failure-model contract is supplied; shared host resource admission/reclamation remains required and unqualified. |
| B8 | medium | patch | Captured JSON iteration now checks cancellation, but the subsequent ReplayVerifier foreach receives a plain List. Feed its existing IEnumerable a private cancellation-aware traversal; no public replay API is required. |
| B9 | low | patch | Private candidate Verify decodes arbitrary signature text before checking64 bytes. Require the exact expected header framing and86-character signature before split/decode allocation; no new size policy is needed. |
| B10 | medium | patch | OwnedExportKey access denial still passes if ZeroMemory is deleted. The existing test friend assembly can retain a constructor buffer and assert every byte is zero after disposal. |
| B11 | low | patch | The200ms real timer may expire before queued provider entry. Existing manual clock/timer fixture can start provider first then deterministically expire, retaining bounded observer watchdogs. |
| E1 | low | patch | The new version regex rejects a valid prerelease+build-metadata source version. Permit both suffixes in the existing validation; keep malformed versions rejected. |
| E2 | medium | defer | Pre-existing RetainedIdentityHistoryReader invokes SendAsync/body providers synchronously before bounded waits and shares provider callbacks with wait cancellation. Parties query wraps this provider under its own deadline, but standalone callers remain unqualified. This is additional feasible full-scope source work, to address after current review corrections without inventing resource reclamation. |
| V1 | medium | patch | Preverified gap: new iteration cancellation test only calls source ReadAsync; admission cancellation tests stop before replay/at provider waits. Add a direct admission mid-prefix case so reverting the token argument fails. |
| V2 | high | patch | StreamsController returns successfully unprotected JSON with outcome.Metadata; PayloadUnprotectionOutcome explicitly preserves provider provenance. Current-version valid Protected metadata must remain readable from certified source. Preserve opaque/future/malformed denial and add actual gateway unprotection-path proof. |

Patches add no new production authority, accepted policy, owner target or live capability. B7 remains qualification work. E2 is pre-existing and is retained in the ledger; its feasible private standalone transport correction can proceed under this same full-scope authorization. No retention/scope approval is reopened.


### Owner clarification and direct contract inspection — 2026-10-08

The owner confirmed use of an existing environment; its name/configuration location is still requested, without secrets. Technical profile/target questions were clarified in plain language. The owner requested deeper pragmatic comparisons for count semantics and worker provenance, and directed direct EventStore inspection instead of supplying an unseen contract. `tests/owner-delivery-2026-10-08/pragmatic-owner-decisions.md` records how each option works, pros/cons and recommendations. Creation-window count and a dedicated service Party are recommendations only, not inferred owner decisions; the separate numeric envelope proposal is not installed/accepted. `eventstore-successor-contract-inspection.json` records exact current-source inspection/search basis and the explicit expired-predecessor denial test; no implemented actor-free successor contract was found. The accepted365-day policy, Branch B, scope and original approvals remain unchanged.


### Additional E2 review and continued authorized engineering — 2026-10-08

The independent focused transport follow-up verified one residual defect; [review evidence](tests/owner-delivery-2026-10-08/additional-e2-review.json) and root code tracing agree.

| Finding | Verdict | Route | Verified evidence and resolution |
| --- | --- | --- | --- |
| E3 | medium | patch | Completed response disposal runs on the caller continuation, including an already-completed pending operation at the thirty-second boundary. A blocking provider Dispose delays timeout/cancellation until released. Move owned cleanup off that continuation and test both direct and late-result schedules; forcible reclamation remains unqualified. |

Root first full corrected Local commands passed Parties1136, Conversations1563 and custody164, with sixteen zero-warning/error Debug builds and protected state/date-only register delta verified. These counts overlap; they are source observations, not complete owner delivery or live qualification. Broader existing source/reference snapshots checked57116 paths and found11 unrelated changes; whole-graph immutability is not asserted. The subsequent E3 correction and next engineering require separate reviewed/verified source observations.

The feasibility audit found that absence of an existing technical port is not itself an owner decision. Generic ordered source discovery/backfill/checkpoint/publication and the specified conditional/outcome/protection-owner state protocols remain authorized source engineering. Do not ask again for implementation approval or label routine technical design blocked solely on missing production providers. Continue in dependency order, beginning with the generic EventStore publication/discovery foundation and compatible Conversations approval adapter. Count semantics, service provenance, independent current authorities, minimal retained successor semantics, selected environment/failure model and conditional Product dispositions remain distinct actual inputs. Actual release/destruction/live qualification stays gated until its required authority and provider evidence exists.


### Accepted owner decisions and real environment discovery — 2026-10-08

The owner supplied192.168.1.30 and explicitly selected the existing creation-window count and a dedicated Conversations service Party. [Input record](tests/owner-delivery-2026-10-08/accepted-owner-inputs-20261008.json) preserves these accepted choices. Do not ask for them again. Exact service-Party enrollment/current machine binding still requires an actual source/configuration basis; never borrow the approving human or immutable AgentsParty.

Read-only discovery used existing native contextjpiquot@local and retained compatible kubectl1.34.12 againsthttps://192.168.1.30:6443. Six metadata reads succeeded; current server is1.34.9. The cluster has current OpenBao/keycloak/Dapr infrastructure. OpenBao, Keycloak and Memories pods are all scheduledonnode1; existing Memories telemetry PostgreSQL hasone replica and no Agents denial spool was discovered. No Agents/Parties/Conversations workload or applicable signing profile/serviceParty binding was established by this metadata capture. [Allowlisted metadata/commands](tests/owner-delivery-2026-10-08/environment/metadata-observation.json) retains exact workloadUIDs/image digests/configuration-key names, without raw configuration, Secret objects/values or credentials. Existing default SSH authentication was refused; Kubernetes metadata access succeeds independently. No resource mutation, deployment, consuming app seam or live acceptance occurred.

Existing OpenBao is a candidate backend, not already qualified all-copy actor-history destruction or nonrollback authority. Historical generic restore proof must not substitute for those requirements. Existing component scopes belong to Memories/telemetry and cannot silently become Agents credentials or an independent replicated security spool. Continue authorized source engineering; production target, independent authority/failure-model and full exact-target qualification remain separate.

### Accepted internal-request timing — 2026-10-08

After a deeper how/pros/cons comparison, the owner explicitly selected the proposed five settings: maximum request lifetime L=5 minutes; future clock tolerance S=30 seconds with no post-expiry grace; healthy routine previous-key overlap O=10 minutes; automatic recovery horizon H=24 hours; first-seen replay retention R=7 days. R exceeds max(L+S+O,H): 7 days exceeds both 15 minutes 30 seconds and 24 hours. Compromised/revoked keys fail immediately. [Accepted inputs](tests/owner-delivery-2026-10-08/accepted-owner-inputs-20261008.json) and [comparison](tests/owner-delivery-2026-10-08/pragmatic-owner-decisions.md) record the decision. Do not request it again.

These accepted numbers do not invent issuer/audience/profile version/validity, independent decision/actor authority or qualified backend/recovery proof. The 365-day Party history policy and non-expiring single-use deletion capability remain distinct and unchanged. This is a configuration input to the ongoing approved delivery, not a complete target or live qualification; Story 5.4 stays draft/backlog and this parent stays in-progress.

### Authority policy and worker enrollment inspection — 2026-10-08

The owner directed checking Hexalith.Platform for the decision-authority policy and preparing a proposal if none is established, and checking for the dedicated Conversations worker enrollment before choosing the practical fallback. [Scoped inspection](tests/owner-delivery-2026-10-08/owner-authority-enrollment-inspection.json) did not establish the applicable production policy or worker enrollment in the searched source/configuration. Existing GitHub review authentication and operator/bootstrap provenance serve different purposes; sample EventStore machine credentials do not establish a dedicated deletion worker. Runtime private configuration was not inspected, so universal absence is not claimed.

[Approval authority proposal](tests/owner-delivery-2026-10-08/approval-authority-proposal.md) and [draft policy](tests/owner-delivery-2026-10-08/approval-authority-policy-draft.json) describe reusing an independent existing issuer or one narrow separately governed signer, with real actor/role/profile/revocation references still unassigned. [Worker enrollment handoff](tests/owner-delivery-2026-10-08/deletion-worker-enrollment-proposal.md) proposes the account name conversations-deletion-worker and the existing three-field per-tenant registration; no account, Party identifier, authority grant or production target is fabricated or installed. These proposals do not reopen the accepted service-Party choice, timing, retention, implementation or scope approvals. Feasible source engineering continues independently; actual enrollment and live qualification remain required delivery work.

The owner then replied “I Jérôme Piquot approve” to the authority-role/signer assignment question. The policy proposal and named human approver for Product, Governance, Security and Architecture are recorded as accepted. Existing Platform `jpiquot` repository ownership and Conversations named-owner records provide limited existing identity references; [inspection](tests/owner-delivery-2026-10-08/named-approver-identity-inspection.json) does not establish current application actor/role bindings or an independent cryptographic issuer. The proposed separate `agents-decision-issuer` account and its real enrollment/profile remain unassigned. Preserve this approval; do not ask it again or substitute the chat declaration for live current authority, a signed exact decision manifest or complete owner qualification.


## User-requested stop — 2026-10-08T16:34:07.474742+00:00

The user requested “save and stop”. Engineering was stopped and the implementation agent interrupted. [Saved resume checkpoint](tests/owner-delivery-2026-10-08/paused-20261008T163407Z/README.md) records current source/diff/hash evidence, accepted owner inputs, focused execution history and the remaining dependency-ordered work. No final independent review, fresh full owner verification or live qualification is claimed. This parent remains unfinished/in-progress; Story 5.4 remains draft/backlog and the four dependencies remain Uncommitted. Existing approvals are preserved.

## Build resume from saved owner delivery — 2026-10-08

The user's new `bmad-build 5.4` invocation resumes this previously approved owner-prerequisite implementation. Continue the outstanding source units in [saved resume state](tests/owner-delivery-2026-10-08/paused-20261008T163407Z/resume-state.json), loading that file, its README, and [accepted owner inputs](tests/owner-delivery-2026-10-08/accepted-owner-inputs-20261008.json) before implementation. Preserve the existing baseline and frozen intent; no repeat scope, policy or implementation approval is needed. Original Story 5.4 and its sprint status remain draft/backlog until the separate complete entry/execution gates are satisfied.

The initial resumed repository inspection found clean Agents, Parties, Conversations and Platform worktrees. EventStore has unrelated event-evolution changes; preserve them. Since the saved checkpoint, EventStore and Platform have additional commits. Inspect current source and compare the saved owned-source manifest before attributing or changing code. Capture a new ordered phase baseline for each new source unit rather than overwriting earlier evidence. Complete all feasible remaining source work in dependency order, update all four truthful full-scope delivery packets, expand Local verification coverage, and prepare the complete current-source review input using the existing root utilities. Runtime enrollment, independent current authority, qualified production custody/protection/spool bindings and complete live qualification must retain explicit unestablished status unless actual evidence supplies them. Current partial checkout revisions cannot become accepted complete targets.
