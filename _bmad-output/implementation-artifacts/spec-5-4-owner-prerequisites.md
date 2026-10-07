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
