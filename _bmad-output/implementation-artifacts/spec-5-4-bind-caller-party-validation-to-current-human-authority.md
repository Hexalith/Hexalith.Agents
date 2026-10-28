---
title: '5.4 Bind caller Party validation to current Human authority'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
sprint_sync: 'preserve-parent-in-progress'
baseline_commit: '98c4dda84fe4ef5ec353bd8b92929132484a6f19'
human_approval: 'accepted'
approved_on: '2026-10-28'
route: 'dispatch'
story_key: '5-4-prove-trusted-principal-tenant-party-and-approver-readiness'
investigation_commit: '98c4dda84fe4ef5ec353bd8b92929132484a6f19'
review_loop_iteration: 1
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-10-28-mvp-custody.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Caller validation omits `ResolvePartyIdentity.ExpectedActorId`, which Parties requires for eligible Humans. Tests hide this incompatibility with a resolved Human lacking its required binding.

**Approach:** Carry verified tenant-authorization evidence into caller validation. Query its stable actor ID and validate the returned binding. This bounded Story 5.4 correction leaves full acceptance pending.

## Boundaries & Constraints

**Always:** Match tenant, subject, Party and binding version exactly; distinguish subject from stable actor. Check UTC observation, exclusive expiry, cancellation and clock rollback across awaits. Keep evidence transient/server-only. Preserve Branch B and owner policy/custody enforcement.

**Never:** Infer actors from claims/request IDs, accept bare success flags or historical attribution, bypass unavailable dependencies, or fall back to legacy caller validation. No owner-contract change, runtime binding, deployment, custody provisioning or completion claim.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current caller | Subject differs from verified actor | Query names verified actor; exact current binding accepted | Safe denial on mismatch |
| Uncertain evidence | Missing/substituted actor or binding; expiry/rollback during await | Stop before subsequent protected reads or dispatch | Content-free Denied |
| Dependency unavailable | Closed owner record or package fallback | No identity-client call | Existing unavailable result |
| Agent identity | Active Organization under Branch B | Existing tenant/id validation preserved | Human cannot act as Agent |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs`, `ResolveAsync`: requires the expected actor, unique current binding and custody. Reuse the public client; preserve owner source/checks.
- `references/Hexalith.Parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs`, `Matches`: current Human responses require a closed valid interval and custody expiry covering its end. Mirror this structural check for typed adapters; positive fixtures must pass this shape, while the owner still verifies the accepted policy.
- `src/Hexalith.Agents.Server/Ports/CurrentHumanActorEvidence.cs`, `IsCurrentFor`: reuse correspondence/time checks. Its observation precedes directory lookup; do not require observation after that later start.
- [Pre-review candidate](tests/caller-party-binding-2026-10-28/pre-review-candidate.patch): retain its correct actor wiring, legacy refusal, Branch B behavior and regression coverage when re-deriving; it loses earlier tenant/binding/custody deadlines, which the tasks below correct.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Agents.Server/Ports/TenantAccessReadResult.cs` and `ProjectedTenantAccessReader.cs` — add optional current-actor evidence, populated only after successful completion-time tenant authorization.
- [x] `src/Hexalith.Agents.Server/Ports/IAgentPartyDirectory.cs` — add an actor-bearing caller-validation overload with a closed default. Preserve the legacy signature as closed; never delegate the new overload to it.
- [x] `src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs` — query verified `ExpectedActorId`; require matching Human binding scope/actor/version, current interval, positive actor revision, provenance and valid finite custody evidence through completion. Preserve non-Human behavior.
- [x] `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` — use the registered clock; bind the tenant-result actor to the request and pass it onward. Recheck the original evaluation interval after awaits before protected reads/dispatch; missing evidence stops before Party lookup.
- [x] `test/Hexalith.Agents.Server.Tests/PartiesAgentPartyDirectoryTests.cs`, `Story54CurrentEvidenceTests.cs`, `AgentInteractionGateAdmissionTests.cs` and `AuthorityCancellationAdmissionTests.cs` — cover the matrix, exact query, invalid binding/custody, exclusive boundaries and cancellation using contract-valid fixtures.
- [x] `test/Hexalith.Agents.Server.Tests/AgentInteractionGateOrchestratorTests.cs` and `ProjectedProviderCatalogReaderTests.cs` — update successful gate fixtures to supply verified evidence and use the new overload; retain existing readiness/provider assertions.
- [x] `eng/verify-story-5.4.ps1` — require the executed caller-directory suite. Record bounded results in the parent spec; preserve full tasks and sprint/dependency status.
- [x] `src/Hexalith.Agents.Server/Ports/ProjectedTenantAccessReader.cs` — bound the released actor's validity by the earlier actor/tenant-authority expiry; preserve the original subject, actor and observation.
- [x] `src/Hexalith.Agents.Server/Ports/AgentPartyValidationResult.cs`, `PartiesAgentPartyDirectory.cs` and `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` — carry a transient caller-validity deadline equal to the earliest actor, binding and custody expiry. Require a current UTC deadline for successful actor-bearing validation, retain it locally, and enforce its exclusive boundary after every subsequent read await and immediately before dispatch. A completed dispatch retains its accepted shared-policy outcome; receipt latency cannot turn it into Denied. Existing Agent identity validation needs no caller deadline.
- [x] `test/Hexalith.Agents.Server.Tests/Story54CurrentEvidenceTests.cs`, `PartiesAgentPartyDirectoryTests.cs` and `AgentInteractionGateAdmissionTests.cs` — cover an earlier tenant deadline and earlier binding/custody deadlines crossed during later gate reads, direct malformed-actor refusal before owner calls, and Valid caller results with null/substituted Party IDs. Add a local composition test joining the real reader, directory and gate with contract-valid owner substitutes and distinct subject/actor IDs.
- [x] Preserve initial evidence unchanged; capture the re-derived source, focused regression results and strict full verification separately under `tests/caller-party-binding-2026-10-28/review-1/`. Record root-declared dependency revisions/dirty fingerprints and SDK as a timestamped post-run observation, without claiming a pre-run transitive-input attestation.

**Acceptance Criteria:**
- Given a contract-valid owner response, when the local gate evaluates it, then actor/subject differences work without weakening binding or tenant isolation.
- Given a denial, when its observable result is inspected, then it reveals no foreign-tenant identity or binding details.
- Given local success, when readiness is reported, then all four unavailable dependencies and full parent acceptance remain outstanding.

## Implementation Notes

- Tenant authorization now releases its transient `CurrentActor` only after successful completion-time actor and tenant checks. The gate binds tenant, authenticated subject and Party before any Party read, then passes that exact evidence through the new actor-bearing directory overload. The new interface default and the legacy caller signature remain closed independently.
- The directory sends `ResolvePartyIdentity.ExpectedActorId` from the verified stable actor and requires exact Human binding scope, actor and binding version, positive actor revision, current UTC interval, provenance and finite current custody evidence. Owner policy/custody enforcement and source remain untouched; Branch B Organization/Agent identity handling is preserved.
- The gate uses the registered clock and retains the original evaluation interval across awaits. Each completion and the final pre-dispatch check reject exclusive expiry or backward clock movement, including rollback after forward progress. Cancellation remains cancellation rather than an ordinary denial/unavailable result.
- Successful Human owner fixtures now carry their required binding and derived finite custody. Local substitutes establish no external availability, runtime adapter, deployment or custody provisioning. Full parent acceptance and all four unavailable dependencies remain outstanding.

- 2026-10-28 review-1 completion: retain the earliest tenant/actor/binding/custody deadline through all subsequent read awaits and the final pre-dispatch boundary. Current typed responses now require the public owner client's closed interval/custody-coverage shape. A delayed successful dispatch acknowledgement preserves its accepted shared-policy outcome. Final root verification passes **5,984/5,984** full tests, **244/244** focused cases and five warning-free builds; Live refuses all four records with zero external calls. [Final results](tests/caller-party-binding-2026-10-28/review-1/contract-fixture-correction/FINAL-RESULTS.md) and [receipt](tests/caller-party-binding-2026-10-28/review-1/contract-fixture-correction/verification.json) supersede earlier source candidates. Selected source hashes match the executed candidate; dependency/SDK data is explicitly post-run only. BH5/BH7/BH8/VG1 coverage and BH9 provenance are complete. BH3 remains in the deferred ledger; full parent acceptance and all unavailable owner gates are unchanged.

- 2026-10-28 final review patches: canonical actor ULIDs and binding effective-at correspondence now match the verified current actor. Public-client HTTP round-trips, inactive/restricted/erasing Human mapping and expiry during envelope preparation are covered. [Final root results](tests/caller-party-binding-2026-10-28/review-1/review-fixes/FINAL-RESULTS.md) pass **6,001/6,001** full tests with five warning-free builds, including **261/261** focused-suite cases; the targeted patch run passes **115/115**. Live again refuses all four dependencies with zero external calls. FB1/FB2/FB4/FB5/FB6/FB8/VG2 are resolved; carried/refuted findings and the two deferred entries remain explicitly logged. No further source change follows this verification.

- Completion is scoped to this child correction. The approved tasks explicitly preserve parent sprint/dependency status, so parent Story 5.4 remains in-progress rather than being advanced by generic workflow synchronization.

## Spec Change Log

- 2026-10-28 review loop 1, BH1/BH2/EC1/EC2: the first plan retained only actor expiry after successful authorization and caller validation. Amend non-frozen tasks to carry the earliest tenant/actor/binding/custody deadline through the gate; otherwise a later await can cross an already checked authority boundary. KEEP: distinct subject/actor mapping, exact returned binding, the closed new interface default and legacy signature, Branch B, owner policy checks, cancellation, gate rollback detection, all existing regression cases and strict validation gates. The initial candidate is archived before reverting this build's code for re-derivation. Earlier evidence remains historical, not final-source qualification.

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH1 | high — the directory currently returns only PartyId/status, losing a shorter binding/custody expiry; later awaits can outlive it while actor evidence stays current. | bad_spec, deadline propagation |
| BH2 | high — tenant evidence can expire before the unchanged actor deadline released by the reader. The new gate's repeated checks cannot see that earlier expiry. | bad_spec, deadline propagation |
| BH3 | medium — rollback between the reader's internal awaits can remain after its initial start. This time-comparison logic is unchanged from the approved baseline; the gate's own checks cannot observe internal intermediate instants. | defer, pre-existing reader monotonicity |
| BH4 | false — the real owner service checks current policy and `Custody.Satisfies(policy, ValidFrom)` before resolving an eligible Human, including the accepted retention deadline. Local typed substitutes cannot qualify custody; duplicating owner policy selection here would not establish it. | reject |
| BH5 | medium — existing focused tests split the actual reader and directory behind substitutes, leaving their joined successful caller path unverified. | patch, composition coverage |
| BH6 | false — Local forces source mode and `Directory.Build.props` CheckSourceDependencies requires PartiesFromSource=true before Build; package-only compilation cannot pass this supported Local path and silently omit source tests. | reject |
| BH7 | medium — gate negatives do not cover direct malformed-actor calls to the new directory overload or prove those calls stop before the owner client. | patch, direct admission coverage |
| BH8 | medium — Valid results with null/substituted PartyId are not exercised; removing the new equality guard leaves existing cases green. | patch, same regression as VG1 |
| BH9 | low — the new receipt records selected root source hashes but no observation of the source-built dependencies/SDK, limiting reproducibility of the recorded graph. A timestamped post-run snapshot is a direct evidence correction, not proof of earlier stability. | patch, provenance metadata |
| BH10 | low — replaying this environment-specific evidence requires the documented manual helper/launcher preparation. A portable bootstrap adds tooling branches for an occasional evidence replay; the README already preserves exact prerequisites and paths. | reject complexity |
| EC1 | high — the released actor deadline does not reflect an earlier tenant authority deadline; later work can outlive tenant evidence. | bad_spec, same defect as BH2 |
| EC2 | high — successful caller validation loses earlier binding/custody deadlines before later awaits. | bad_spec, same defect as BH1 |
| VG1 | medium — verified coverage gap: Valid/null and Valid/foreign Party IDs never exercise the newly added exact-result guard. | patch, same regression as BH8 |

| FB1 | medium — typed actor-bearing validation accepts a matching noncanonical actor ID that the real owner client rejects. A canonical ULID check before owner lookup is a direct consistency correction. | patch |
| FB2 | medium — current code compares binding start only with the later owner observation; a binding starting after the host actor observation can still pass. Require that it existed at the original verified observation. | patch |
| FB3 | medium — carried BH3: the unchanged internal reader time comparison still cannot see intermediate rollback. Its existing deferred entry remains authoritative; do not duplicate it. | carried defer |
| FB4 | medium — the final pre-dispatch check lacks an isolated envelope-preparation expiry regression; existing read callbacks expire earlier. | patch, same root as VG2 |
| FB5 | medium — the actor-bearing directory has no explicit inactive/restricted/erasing Human-result cases asserting absent Party/deadline. Existing gate status substitutes do not verify this adapter mapping. | patch |
| FB6 | medium — composed local tests still substitute the public identity client, and root audit demonstrated fixture shapes that that real client rejects. Add a local serialized HTTP-client round-trip with an intercepted handler. | patch |
| FB7 | false — carried BH6: supported Local source mode cannot omit source-only tests because CheckSourceDependencies requires PartiesFromSource=true, and the strict assembly run permits no skipped tests. | carried reject |
| FB8 | low — the initial archive index lacks a navigation pointer to superseding results. Add a prominent link while preserving the historical figures and immutable logs/receipts. | patch documentation |
| FB9 | low — the regenerated agent context omits the previously explicit Conversation denominator and dedicated service-Party constraints. Full owner contracts remain required, so current behavior is unchanged; restoring agent-context instructions routes to deferred work under this workflow. | defer agent-context correction |
| FB10 | low — no pre-execution binary attestation was recorded. The receipts explicitly claim selected source/artifact hashes and post-run observations only, with no transitive execution attestation or qualification. Adding a binary-attestation protocol is disproportionate to this bounded local correction. | reject complexity |
| VG2 | medium — pre-verified test gap: removing the final admission check leaves current tests green because expiry happens before or after that boundary. Enumerating client extensions can advance the clock exactly at envelope preparation. | patch, same root as FB4 |

## Verification

- [Executed evidence](tests/caller-party-binding-2026-10-28/README.md) and [source/command receipts](tests/caller-party-binding-2026-10-28/verification.json): five warning-free Debug/source builds; **5,947/5,947** full tests passing with zero skips/not-run/errors; the six focused Server suites pass **207/207**. `PartiesAgentPartyDirectoryTests` is now mandatory in the verifier's executed suites.
- The two regression assertions fail against the approved baseline's unchanged gate/directory in isolated build output, without reverting working sources. The exact baseline copies' hashes, temporary compile-target import and executed failure results are retained under the evidence directory's `baseline/`.
- Broad setup observations are recorded separately: unmapped source build fails Conversations' producer-mapping gate; globally inherited generated mapping causes three intentional build-mode probes to fail. The successful run uses the public materializer's complete environment scoped solely to build children through the recorded validation-only launcher. No repository guard was weakened and the verifier API is unchanged.
- Separate `-Mode Live` artifact directory: expected exit 1, all four unavailable records and **zero external calls**. No live qualification or full Story 5.4 completion is claimed.

- Run focused Server suites with the verifier's Debug/source properties; show new regression failures against prior behavior without reverting existing work.
- `pwsh -NoProfile -File eng/verify-story-5.4.ps1 -Mode Local`: five warning-free builds, all tests passing without skips. Record broad blockers separately from focused evidence.
- Run with `-Mode Live` in a separate artifact directory: expect all four unavailable records and zero external calls; no live qualification claim.
