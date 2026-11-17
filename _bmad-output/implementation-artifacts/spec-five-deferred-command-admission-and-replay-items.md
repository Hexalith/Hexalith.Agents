---
title: 'Five deferred automatic command admission and replay items'
type: 'bugfix'
created: '2026-11-17'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '492a0217dfa08f1dc57b5d24840a257c1bd30861'
context:
  - '{project-root}/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five unfinished Story 6.1 review patches leave automatic command trust and retry identity insufficiently verified. The automatic intent adapter also binds payloads case-sensitively, unlike the EventStore execution reader, so ordinary camelCase submissions can lose business fields during canonicalization.

**Approach:** Complete exactly five existing review patches: payload casing, business-revision significance, trusted delivery metadata, missing/foreign handler authority, and invalid phase-fact guards. Together these establish consistent command admission and replay semantics. Independently review and verify, then commit and push to main as requested.

## Boundaries & Constraints

**Always:** Reuse registered adapters, EventStore payload reader options, admitted-proof checks and public aggregate Handle/Apply seams. Preserve canonical writer representation, authority separation, protected-content identity, exact retry semantics and rejection reasons. Use run-with-temp for disposable work and keep useful evidence outside scratch. Each C# type has its own named file.

**Never:** Invent owner authority or credentials, change AD-29 identities, reinterpret business revisions as transport restamps, rewrite arbitrary JSON inside ProtectedContent.Value, enable live dependencies, change migration/Closing policy, deploy, publish packages or weaken audits/tests. Other Story 6.1 patches and external deferrals remain separate.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Required behavior |
| --- | --- | --- |
| Payload casing | Equivalent PascalCase, camelCase and mixed-case declared members for all nine commands | Same canonical intent; a changed business value remains different under each spelling; malformed payloads refuse |
| Revision identity | Changed ExpectedRevision or Fact.Revision versus changed business revisions | Transport restamps preserve intent; CommitRevision, ResultRevision, PermitRevision, InteractionCreationRevision and Fact.LeaseCommitRevision each change intent |
| Delivery metadata | trust:deliveryNonce on each of nine commands | Claims accepts only the governed route/key; Accepts requires exact admitted proof, authenticated Agents workload and nonblank value; wrong route, caller, proof or key casing refuses |
| Handler authority | Valid command/state with correct, absent or other actor's key | Correct authority reaches its intended decision; absent/foreign authority yields the existing typed rejection without accepted domain effects |
| Invalid phase fact | One isolated stale revision, ordering violation, invalid lease commit, authority or protected output | Exact StaleRevision, InvalidPhase, LeaseCommitRequired, InvalidAuthority or ProtectedContentRequired rejection; no accepted phase change |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.EventStore/AutomaticInteractionIdempotencyIntentAdapter.cs`: default Deserialize ignores camelCase members. Use `Hexalith.EventStore.Contracts.Serialization.EventStorePayloadSerialization.Options` for reading; retain existing PascalCase normalization and removal of transport revisions.
- `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs`: reuse Registry, existing nine-command intent inventory, policy Claims/Accepts tests and proof fixtures. Current intent test changes business identities but none of the five named business revisions; delivery metadata cases currently cover other domains only.
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs`: already governs seven directory commands and two automatic interaction commands; preserve proof and workload checks.
- `test/Hexalith.Agents.Tests/ConversationDirectoryTestData.cs`: reuse valid permits, authority envelopes, leases and public event folding.
- `src/Hexalith.Agents/ConversationAgentState/ConversationAgentStateAggregate.cs` and `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.Automatic.cs`: authoritative guards; no planned policy change. Missing authority can legitimately produce LeaseNotFound on lease operations.
- `test/Hexalith.Agents.Tests/AutomaticInteractionTwinPolicyTests.cs`: tests Evaluate directly and cannot prove Handle rejection. Add dedicated `InteractionCommandAuthorityTests.cs` and `AutomaticInteractionPhaseGuardTests.cs` beside it.
- `_bmad-output/implementation-artifacts/spec-6-1-start-and-recover-an-automatic-interaction.md`: five matching unchecked Review/Patch entries are the work inventory; preserve frozen intent and all other checkboxes.

## Tasks & Acceptance

**Execution:**

- [x] Adapter and gateway tests above — bind with shared reader options; cover all nine commands and nested declared records under alternate casing, preserving opaque JSON and malformed-payload refusal.
- [x] Gateway tests above — independently vary each of the five named business revisions, with transport-restamp equality controls; exercise the registered canonical intent path.
- [x] Gateway tests above — add trust:deliveryNonce Claims/Accepts coverage across all nine commands and exact proof/workload/route/value refusals.
- [x] `test/Hexalith.Agents.Tests/InteractionCommandAuthorityTests.cs` — exercise all nine public handlers with valid prerequisites, correct authority and missing/foreign keys; compare accepted replayed business state before/after refusal.
- [x] `test/Hexalith.Agents.Tests/AutomaticInteractionPhaseGuardTests.cs` — build valid phase prefixes through Handle/Apply, isolate each requested invalid guard, assert its exact typed reason and unchanged accepted phase state; include valid neighboring controls.
- [x] Original Story 6.1 spec, this spec and new `_bmad-output/implementation-artifacts/tests/command-admission-replay-2026-11-17/README.md` — record reproduction, executed cases and limits; mark exactly these five patches complete only after verification.

**Acceptance Criteria:**

- Given equivalent accepted wire spellings, when registered adapters canonicalize them, then retry identity agrees with payload execution binding while every listed business revision remains significant.
- Given valid prerequisites and a single invalid boundary input, when the relevant policy or handler runs, then the exact refusal is observed without accepted domain effects; adjacent positive controls pass.
- Given reviewed changes, when delivery runs, then affected owning suites, applicable repository checks, commitlint, root-submodule inspection and remote divergence checks precede the authorized main push.

## Implementation Notes

- User explicitly directed pushing all changes to main after the known audit-policy and CI pin failures were disclosed. All verified code hashes remain unchanged; delivery is authorized without altering either failed gate.

- Coordinator audit independently verified all five compiled code hashes, all eleven native result hashes/counts and all 53 named focused passes. All five matrix rows have executed passing controls. The single baseline-reproduced shared audit-policy failure and Server source-mapping blocker remain explicit.

- User approved and continued on 2026-11-17. The frozen intent is unchanged.
- The implementation handoff performs local implementation and verification only. The coordinator handles independent review, staging, commits and remote delivery. Do not stage, commit, push or change submodules in the handoff.

## Spec Change Log

- 2026-11-17: Implemented and locally verified all five selected patches; marked only their matching Story 6.1 review entries complete. Frozen intent and the approved baseline remain unchanged. Independent review and delivery remain coordinator-owned.

## Review Triage Log

Final closure: BH-2 through BH-10 are corrected and verified in the final full owning suites. All 66 named controls pass. Domain passes 1,084/1,084; Server passes 2,062/2,063 with the same clean-baseline audit-policy failure. Combined package results are 6,520/6,521. Full Release solution build and 66-project package-authority validation pass. The separate existing CI pin mismatch is retained. After the workflow escalation disclosed the known failures, the user explicitly instructed push all changes to main. Delivery proceeds under that direction without weakening audits, tests or CI policy.

| Finding | Verdict and verified evidence | Route |
| --- | --- | --- |
| BH-1 | maybe-false, medium if true: the old reader produces different camelCase descriptors, but the only inspected first-party dispatcher writes PascalCase, and no accepted installed host or retained camelCase cohort is supplied. Owner publication/retention inventory and an exact retained-descriptor retry would settle upgrade exposure. Do not invent migration compatibility for ambiguous old intents. | defer |
| BH-2 | medium: the shared RecordAutomaticInteractionStep fixture mixes ProviderInvocationAuthorized with Output, arbitrary operation identity and a WorkflowStart lease, so it is not a handler-accepted business command. Use independently valid authorization and generated-output facts. | patch |
| BH-3 | medium: descriptor-to-descriptor equality does not execute or compare the actual SDK-bound command. Add execution-binding agreement against the valid command fixtures; keep opaque content significant. | patch |
| BH-4 | medium: shared Web options also accept quoted numbers, and current controls do not establish quoted revision equivalence or malformed-number refusal. | patch |
| BH-5 | medium: Should.Throw<Exception> can pass on an unrelated registry/null-reference error. Assert the binding exception for each malformed case. | patch |
| BH-6 | medium: nonce proof acceptance and authority significance are exercised separately, but no current case establishes delivery-nonce neutrality in all nine canonical intents. Add that control. | patch |
| BH-7 | medium: authority cases establish refusals before initial acceptance, without the accepted duplicate/NoOp state. Assert the same refusals after acceptance so retry shortcuts cannot precede authority. | patch |
| BH-8 | medium: ProviderOutputAuthority.IsEffective independently requires additional ids/revisions and a defined status; the current invalid fact theory omits those inputs. | patch |
| BH-9 | medium: invalid lease cases target only WorkflowStartCheckpoint, leaving later-phase capability substitution unpinned. Add a later phase using another actual committed phase lease. Existing GenerationFailureProviderLeaseTests already cover Provider and ProposalMutation failure bindings and pass in the full suite; preserve them. | patch |
| BH-10 | medium: the misplaced-output branch tests Output only; independent ContentDigest and DigestKeyVersion prohibitions can regress undetected. | patch |

The edge-case and verification-gap reviewers returned no findings. All three review layers completed before this triage. The baseline-reproduced shared audit-policy failure is an external delivery limitation, not an in-scope implementation failure.

## Verification

Use a disposable complete checkout beneath wrapper TMPDIR. Build affected test projects individually in Debug/source, invoke native xUnit v3 class/method selectors, then full owning Domain and Server suites. Run `eng/verify-story-6.1.ps1 -Mode Local -DependencyMode Package -ArtifactsPath <fresh-durable-path>` in that copy. Follow the repository fallback ladder for source blockers without suppressing diagnostics. Retain exact commands, native results and source hashes. Review independently and triage before delivery. Inspect all root-declared submodules; push any required child content before parent gitlinks. Fetch and verify remote main after the authorized push.


### Executed implementation verification — 2026-11-17

- Shared EventStore read options are the sole production change. All nine registered commands retain canonical writer representation, authority separation, protected-content identity and transport-restamp semantics. Five C# input hashes and every named focused native case are bound in [the evidence manifest](tests/command-admission-replay-2026-11-17/verification.json).
- Debug/source Domain: zero-warning build, 28/28 focused cases, 1,075/1,075 full owning cases. Debug/source Server: blocked by Conversations' unchanged Platform tool-selected source mapping requirement before Agents compilation.
- Release/package: zero-warning builds and 53/53 focused cases. Maintained package verifier exits 1 at existing `BuildContractConformanceTests.OrdinaryReleaseShouldRejectAllAuditFindings`; Builds `717e5ab2` exempts NU1901/NU1902. The same assertion fails on clean approved baseline with identical policy-input hashes. All five package suites, including separately completed UI, execute 6,508 cases: 6,507 pass, one reproduced external policy failure, zero errors/skips/not-run. No policy or test is weakened.
- Old-reader reproduction: PascalCase passes; camelCase and mixed case fail. Final reader passes the focused casing/revision/opaque-content controls. Native results, commands, exact source revisions, source hashes and limitations are recorded in [the evidence README](tests/command-admission-replay-2026-11-17/README.md). All disposable jobs and their children finished; no staging, commit, push or submodule mutation occurred in the handoff.
