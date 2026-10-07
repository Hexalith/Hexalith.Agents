---
title: '5.4 Identity completion and readiness verification fixes'
type: 'bugfix'
created: '2026-10-07'
status: 'done'
route: 'dispatch'
human_approval: 'accepted-parent-scope'
baseline_commit: '44f98b14188640e69991079fb4379cf17c318733'
owner_baseline_commit: 'b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/parties/AGENTS.md'
  - '/home/administrator/projects/hexalith/parties/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/parties/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="corrections within the already accepted owner-prerequisite implementation scope">

## Intent

**Problem:** Previously reviewed Parties identity queries and clients can release evidence after expiry or clock rollback. The readiness verifier can accept an incorrect binding, interval, expiry or source checkpoint, and buffers oversized responses before enforcing its limit.

**Approach:** Complete the authorized owner prerequisites by checking identity evidence at completion and making the existing read-only verifier independently validate that same exact basis. Close B03, E02, V01, V02, B08/E05/V04, B09, B10, B11/E03, E04 and V03 from the October 7 review with executed regression evidence. Preserve full Story 5.4 and the parent owner implementation.

## Boundaries & Constraints

**Always:** Preserve pre-existing owner edits and record selected-file pre-change snapshots before mutation. Keep the accepted 365 fixed days from binding-effective-at, exclusive expiry, historical action-time intervals, original source positions and Branch B Organization identity. Reuse existing SDK/domain/client seams and injected clocks. Keep the existing HttpClient constructor compatible. Use Debug project references, owning repository configuration and focused executable xUnit tests. Synthetic localhost verifier tests exercise script behavior only; they supply no dependency acceptance or live evidence.

**Never:** Stage, commit, push, update submodules, deploy, post owner messages, execute a real unavailable seam, alter dependency acceptance fields or close the original story. Do not invent deadlines, freshness tolerances, registry revocation proof or authenticated successor bootstrap evidence. B01/B02/B04/B05/B06 and production custody/destruction/restore remain separate unresolved work. Do not weaken or fix the earlier json-redacted replay gate in this child.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior |
| --- | --- | --- |
| Valid current/historical reads | Matching active basis; now within current interval or historical custody | Current succeeds; closed historical interval remains attributable before custody expiry |
| In-flight time change | Clock crosses interval/custody expiry, or rolls back below source/binding start | No usable evidence is released; exact expiry denies |
| Authority changes | Authority changes after current source or custody await | Current denies without releasing evidence |
| Incompatible readiness basis | Wrong nested scope, duration, opening evidence, later revoke/rebind interval or checkpoint | Readiness refuses and writes no success receipt |
| Oversized transport | Declared or streaming body above 32 MiB | Bound enforced during receive; resources disposed; no success |
| Receipt/path gates | Synthetic mismatched exact-target receipt; relative evidence/optional paths | Receipt rejected before HTTP; paths resolve consistently before directory changes |

</frozen-after-approval>

## Code Map

- `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` — ResolveAsync terminal check currently checks upper bounds only. Check completed source observation and binding lower bounds while retaining final SameAuthority and policy checks.
- `../parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs` — add injectable completion clock with compatible existing construction; current binding must still be current and historical custody must still be readable after response parsing. Reject future current observations and future historical actions without introducing an age threshold.
- `../parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs` and `../parties/tests/Hexalith.Parties.Client.Tests/HttpPartiesIdentityClientTests.cs` — reuse mutable clock/substitute fixtures; current source/custody authority changes, lower-bound rollback, current interval/custody boundary and delayed historical reply cases must execute. Put any new helper type in its own file.
- `../parties/eng/verify-ext-parties-history-readiness.ps1` — preserve Available/exact-target/date/command/prerequisite/policy gates. Stream HTTP response with the existing 30-second transport bound and 32 MiB wire/16 MiB decoded bounds. Independently reconstruct recorded binding transitions and compare all returned binding scope/actor/version/interval/custody/source facts. Reject a query checkpoint different from the independently verified source; a newer head requires another verified source, never an unchecked acceptance.
- `../parties/eng/verify-ext-parties-1.ps1` — normalize EvidenceDirectory, optional MemoriesRoot and other caller-relative paths before Push-Location; preserve complete Local/Live gates.

## Tasks & Acceptance

- [x] Fix current query and client completion validation; run meaningful new regressions and all affected existing identity/client classes.
- [x] Correct readiness basis, receive limit and wrapper paths; add a reproducible local harness with valid synthetic positive control and separate mutated scope/expiry/transition/checkpoint, oversized receive, exact-receipt zero-HTTP and relative-path cases.
- [x] Write a separate source/evidence packet under `tests/identity-completion-2026-10-07/` beside this spec, containing pre-change snapshots, exact commands, logs/XML, source/artifact hashes and matrix coverage. Preserve dated earlier evidence.

**Acceptance Criteria:**
- Given a previously valid read, when time or current authority changes before completion, then the service/client releases no usable evidence and preserves safe ordinary outcomes.
- Given a verifier fixture, when a returned basis differs from its independently certified history or supported policy, then verification fails; a matching fixture passes only as synthetic script verification.
- Given the four Uncommitted owner records, when local corrections pass, then the original approved Story 5.4 remains draft/backlog and production qualification remains incomplete.

## Implementation Notes

- Authorization: the October 6 parent records the user's yes to implementation across owning repositories; the October 7 instruction resumes Story 5.4. These corrections fall within that accepted implementation, not a new Product choice. The original full story approval, full-scope preference, Branch B and 365-day decision persist.
- Parties contains pre-existing local implementation plus six unrelated planning edits. Preserve them. Both recorded baseline identifiers are immutable observations, not accepted dependency targets. Snapshot each selected dirty/untracked file before editing and derive the correction diff from those snapshots without staging.
- Prior isolated Parties Aspire baseline failed due missing nested Memories/McpCli dependency; no nested initialization is authorized. This child changes library/read-verification code, with no AppHost edit or live execution.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- |
| Blind 1: event arrays | medium | patch | PowerShell enumerates a singleton JSON array into a transition that passes the new fold; the typed Contracts event reader requires an object. Validate the raw event root. |
| Blind 2: string versions | medium | patch | Get-Version coerces strings to numbers, accepting contract-invalid predecessor/binding/actor revisions in the new independent fold. Require JSON integer values. |
| Blind 3: timestamp grammar | medium | patch | Get-Instant uses permissive TryParse for strings; the typed DateTimeOffset contract rejects slash-form timestamps accepted by the script. Use the contract parser for string instants. |
| Blind 4: revocation expiry | medium | patch | PartyAggregate.Identity CanBind requires custody.Satisfies(policy, effectiveAt) for revoke as well as bind; the new revoke branch omits that exact expiry check. |
| Blind 5: original opening end | medium | patch | PartyAggregate.Bind emits ValidUntil equal to custody expiry. The new fold accepts an originally shortened interval with no closing event; require equality before reconstructing closures. |
| Blind 6: additional profile fields | medium | defer | The pre-change verifier also ignores unknown payload members. PartiesJsonOptions allows unknown fields and the SDK source closes payloads through typed deserialize/serialize; malicious provider/schema drift and privacy qualification remain an earlier owner obligation. The current change creates no extra disclosure. |
| Blind 7: fixed harness owner path | low | reject | This evidence runner explicitly targets the sibling owner checkout used by the packet and its recorded commands. Cross-workspace portability is negligible for this local packet; adding a configurable public option is unnecessary complexity. |
| Blind 8: DI clock coverage | medium | patch | The added DI test asserts only type; direct-construction time cases cannot establish that production activation uses the registered clock. Execute a controlled response through the resolved client. |
| Edge 1: revocation expiry | medium | patch | Verified the same missing exact policy comparison against the owning aggregate and the documented successful invalid-revocation probe. |
| Edge 2: positive revocation fixture | medium | patch | The fixture copies opening custody to a later revoke, contradicting the revoke's effective-at policy. Correct the fixture when closing the same retention defect. |
| Verification 1: DI clock adoption | medium | patch | Pre-verified gap: the registered clock is never exercised through the resolved IPartiesIdentityClient; type assertions miss use of the compatible system-clock constructor. |
| Verification 2: redirect refusal | medium | patch | Pre-verified gap: no harness response is a redirect; reverting AllowAutoRedirect remains invisible to all current cases. Add a separate destination and assert zero destination requests. |

## Verification

Use normal focused Debug builds with `--artifacts-path /tmp/hexalith-agents54-completion-artifacts -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:HexalithEventStoreRoot=/home/administrator/projects/hexalith/eventstore -p:HexalithCommonsRoot=/home/administrator/projects/hexalith/parties/references/Hexalith.Commons -p:HexalithMemoriesRoot=/tmp/parties-no-memories-source -p:NuGetAudit=false -m:1 -p:MinVerVersionOverride=1.0.0`. Run individual built test assemblies with single-dash `-class` and `-result-xml`. Capture new evidence; unchanged lanes may only be reused explicitly with matching hashes. Record the earlier broad Local replay blocker separately; no whole-story or production pass is inferred.


## Review closure and final verification — 2026-10-07

All three independent review layers completed. Twelve raw findings were triaged separately into seven patch groups, one deferred privacy/schema-drift qualification group and one rejected local-harness portability concern. No intent gap or bad-spec route was needed. [Review closure](tests/identity-completion-2026-10-07/review-triage.md) records the layer task IDs and per-finding resolutions. The same implementing agent applied the patches sequentially; root read the incremental diff and independently reran the full affected verification matrix.

Both final Debug project-reference builds passed with zero warnings/errors. Root executed 140 Parties query/admission/retention/SDK-query tests and 98 client identity/DI/command/query tests: **238 passed**, zero errors/failures/skips/not-run. All **42 synthetic HTTP/path/gate cases** met their expected outcomes, including object/integer/timestamp contract refusals, exact opening/revocation expiry, seven-digit UTC ticks, controlled production-DI clock use and 307 refusal with zero destination requests. Both verifier scripts parsed and owning whitespace checks passed. [Final commands, logs/XML and hashes](tests/identity-completion-2026-10-07/final-verification/evidence.json) use separate isolated artifacts and reuse no passing lane. Selected owner files match the initial snapshots overlaid by review-fix snapshots; pre-existing owner edits were preserved.

The original full Story 5.4 spec, sprint status and dependency register remain byte-equivalent to the workflow baseline. A fresh read-only owner-issue recheck found all four requests OPEN with zero comments and no complete acceptance packets. No production seam or real compatibility command ran. The earlier broad Local json-redacted replay failure remains separate and unresolved. Earlier B01/B02/B04/B05/B06 and complete custody/destruction/restore/owner qualification remain outside this completed correction child.

This child is done; the owner-prerequisite parent remains in-progress and full Story 5.4 remains draft/backlog with blocked-external-commitments. The accepted parent's frozen boundaries prohibit commit or push, so the generic terminal local-commit instruction was not applied. No staging, commit, push, deployment, submodule mutation or owner message occurred.
