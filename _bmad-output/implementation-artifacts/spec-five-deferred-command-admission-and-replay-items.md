---
title: 'Five deferred automatic command admission and replay items'
type: 'bugfix'
created: '2026-11-17'
status: 'in-progress'
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

- [ ] Adapter and gateway tests above — bind with shared reader options; cover all nine commands and nested declared records under alternate casing, preserving opaque JSON and malformed-payload refusal.
- [ ] Gateway tests above — independently vary each of the five named business revisions, with transport-restamp equality controls; exercise the registered canonical intent path.
- [ ] Gateway tests above — add trust:deliveryNonce Claims/Accepts coverage across all nine commands and exact proof/workload/route/value refusals.
- [ ] `test/Hexalith.Agents.Tests/InteractionCommandAuthorityTests.cs` — exercise all nine public handlers with valid prerequisites, correct authority and missing/foreign keys; compare accepted replayed business state before/after refusal.
- [ ] `test/Hexalith.Agents.Tests/AutomaticInteractionPhaseGuardTests.cs` — build valid phase prefixes through Handle/Apply, isolate each requested invalid guard, assert its exact typed reason and unchanged accepted phase state; include valid neighboring controls.
- [ ] Original Story 6.1 spec, this spec and new `_bmad-output/implementation-artifacts/tests/command-admission-replay-2026-11-17/README.md` — record reproduction, executed cases and limits; mark exactly these five patches complete only after verification.

**Acceptance Criteria:**

- Given equivalent accepted wire spellings, when registered adapters canonicalize them, then retry identity agrees with payload execution binding while every listed business revision remains significant.
- Given valid prerequisites and a single invalid boundary input, when the relevant policy or handler runs, then the exact refusal is observed without accepted domain effects; adjacent positive controls pass.
- Given reviewed changes, when delivery runs, then affected owning suites, applicable repository checks, commitlint, root-submodule inspection and remote divergence checks precede the authorized main push.

## Implementation Notes

- User approved and continued on 2026-11-17. The frozen intent is unchanged.
- The implementation handoff performs local implementation and verification only. The coordinator handles independent review, staging, commits and remote delivery. Do not stage, commit, push or change submodules in the handoff.

## Spec Change Log

## Review Triage Log

## Verification

Use a disposable complete checkout beneath wrapper TMPDIR. Build affected test projects individually in Debug/source, invoke native xUnit v3 class/method selectors, then full owning Domain and Server suites. Run `eng/verify-story-6.1.ps1 -Mode Local -DependencyMode Package -ArtifactsPath <fresh-durable-path>` in that copy. Follow the repository fallback ladder for source blockers without suppressing diagnostics. Retain exact commands, native results and source hashes. Review independently and triage before delivery. Inspect all root-declared submodules; push any required child content before parent gitlinks. Fetch and verify remote main after the authorized push.
