---
title: '5.4 Apply minimal actor-history retention recommendation'
type: 'feature'
created: '2026-10-06'
status: 'in-review'
route: 'dispatch'
human_approval: 'accepted-engineering-scope'
approval_source: 'User: apply recommendation'
human_delivery_approval: 'accepted'
delivery_approval_source: 'User: I approve'
delivery_approved_on: '2026-10-06'
baseline_commit: 'b252fcfde51bd04317ce5843a0a42bfe4dce5170'
parties_baseline_commit: 'b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/parties/AGENTS.md'
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '/home/administrator/projects/hexalith/parties/.editorconfig'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/parties-identity-contract.md'
---

<frozen-after-approval reason="User authorized applying the engineering recommendation; production policy approval remains separate">

## Intent

**Problem:** No located policy explicitly authorizes opaque actor history after Party profile erasure. Binding writes require explicit finite policy, but current and historical queries can release bindings without checking the configured policy against retained custody expiry.

**Approach:** Record one minimal actor-history policy proposal and enforce explicit matching policy on binding reads. Reuse Party aggregates, EventStore policy/custody contracts and existing options registration. Keep production configuration unset until an owner approves a finite duration and qualifies custody/cleanup/restore.

## Boundaries & Constraints

**Always:** Preserve existing edits, original Story 5.4 and external availability gates. Use versioned policy ID and exact existing `binding-effective-at` expiry derivation. Snapshot policy per call and recheck before evidence release and after asynchronous boundaries; removal/change denies without extending or relabeling old expiry. Use fresh independent custody as well as policy. Organization Branch B remains usable without human policy. Preserve historical actor/version/original positions, half-open intervals, expiry and cancellation behavior.

**Never:** Invent an approved retention duration, register a production provider, enable a live route, modify the register, or claim cleanup/restore qualification from mocks. No policy engine, new storage/service, erasure-trigger state machine, current-profile fallback, assertion suppression, Git mutations or dependency updates. This slice does not complete full Story 5.4.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Explicit valid policy | Exact ID/purpose/derived expiry and fresh custody | Original current or historical binding | Preserve exact attribution |
| Unconfigured policy | Missing/blank ID, duration, or unsupported trigger | Unavailable without binding | Historical path performs no source/custody read |
| Mismatched retention | Wrong policy version, duration, purpose or custody flags | Unavailable without binding | No custody release or profile fallback |
| Configuration changes | Policy removed/changed during source/custody/final authority | Unavailable without binding | Preserve caller cancellation |
| Organization | Active provisioned Branch B, policy absent | Organization resolved with no human binding | No custody call |
| Existing history boundaries | Erased profile, rebind/revoke, expiry, stalled/cancelled provider | Existing exact attribution or typed denial | No changed test expectations to mask faults |

</frozen-after-approval>

## Code Map

- `../parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` — current and purpose-limited retained reads; currently lacks policy configuration dependency.
- `../parties/src/Hexalith.Parties/Authorization/PartyIdentityOptions.cs` — nullable finite policy; registration already supplies options monitoring.
- `../eventstore/src/Hexalith.EventStore.Contracts/Security/IdentityHistoryPolicy.cs` and `IdentityHistoryCustodyEvidence.cs` — exact version/purpose/expiry matching; reuse.
- `../parties/tests/Hexalith.Parties.Tests/Gateway/PartyIdentityQueryHandlerTests.cs` — existing synthetic custody/source fixture and cancellation/boundary regression cases. Rebind fixtures must derive each successor's custody expiry from its own ValidFrom rather than inheriting the predecessor expiry.
- `../parties/_bmad-output/planning-artifacts/adr-consumer-party-id-binding.md` — opaque actor extension; distinguish UI issuer/subject routing from attribution history.

## Tasks & Acceptance

- [x] `../parties/_bmad-output/planning-artifacts/actor-history-retention-policy-v1.md` — concrete minimal proposal: versioned policy reference, purpose, allowed fields, required finite duration, immutable effective-at expiry, independent profile erasure, source/derived-copy cleanup and nonrollback restore rules, approval/qualification gate. Explain why Agents 365-day terminal-content and generic Party audit/implementation-retention rules cannot be reused without explicit coverage. No runnable production defaults.
- [x] Query/options/tests — matching explicit policy and freshness guards for current human binding and historical reads; meaningful matrix tests plus existing query/admission regression suite. Keep Organization classification and existing historical outcomes intact under valid configuration.
- [x] ADR and separate owner evidence — record applied engineering decision, exact source hashes and test artifacts/commands, pending duration/provider qualification, and prior broad replay blocker separately.

**Acceptance Criteria:** A valid fixture can still attribute past actions after profile erasure with exact recorded positions. Missing/mismatched/changed policy cannot release binding evidence. Local Debug source build passes normal analyzers; all new matrix tests and query/admission regression tests execute with zero skips/failures. Policy design is reviewable, while production enablement and complete parent acceptance remain pending.

## Implementation Notes

- 2026-10-06: Implemented directly after the required context-free handoff failed with `agent thread limit reached`. Loaded all context files before changing source. Existing options registration supplies monitoring; no new service/provider or production configuration was installed.
- Matching configured policy is checked for current human bindings and retained historical reads, including after actual suspended source/custody operations and final authority. Missing/mismatched/withdrawn policy returns Unavailable with no binding. Organization remains independent of the human policy.
- Successor fixtures derive their own expiry; the unchanged rebind/revoke, profile-erasure, original-position, expiry and cancellation expectations pass. New private helper CA2007 findings were corrected without suppression.
- [Owner packet](../../../parties/_bmad-output/implementation-artifacts/ext-parties-1-retention-application-2026-10-06.md): normal Debug build 0 warnings/errors and 78 focused passes, including 26 new policy cases, with all matrix rows executed and no skips. Prior 489/490 broad replay blocker remains separate. No production duration or cleanup/restore qualification is claimed.

## Spec Change Log

- 2026-10-06: [Human acceptance](story-5-4-human-approval-2026-10-06.md) records approval of the presented local implementation and minimal policy design. Frozen intent and production prerequisites are unchanged. No retention duration or independent-review result is inferred.

## Review Triage Log

Independent review has not run: the required fresh agent launch failed with `agent thread limit reached`. No reviewer findings or completed-review claim is recorded. Standalone prompts for all three layers are prepared under `retention-review-2026-10-06/` as required by step-04. The full baseline diff includes earlier preserved work; the new recommendation's incremental diff was read completely and its matrix verified against the 78-pass result XML.

After the user approved local delivery, a fresh review launch was retried and again failed with `agent thread limit reached`. Human acceptance is recorded; independent review remains pending. All 15 recorded source/artifact hashes were rechecked and match, so the existing 78-pass evidence is reused without a redundant test run.

## Verification

Build the Parties test project in Debug using owning project-reference switches and isolated `/tmp` artifacts, then execute the built xUnit v3 assembly with `-class '*PartyIdentityQueryHandlerTests' -class '*PartyIdentityAdmissionTests'` and result XML. Preserve previous evidence; use new retention-application artifact paths. Prior full Local 489/490 strict json-redacted replay failure is not waived or in this slice.
