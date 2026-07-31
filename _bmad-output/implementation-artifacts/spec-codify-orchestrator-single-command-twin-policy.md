---
title: 'Codify the orchestrator single-command twin-policy convention'
type: 'chore'
created: '2026-07-31'
status: 'done'
baseline_commit: '03900d9cfb269939570597c67940e505bcc84429'
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-retro-2026-06-24.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The load-bearing orchestrator to single-command to twin-policy pattern is implemented across Epic 2 and Epic 3, but it remains scattered through story Dev Notes and code comments. Future command steps must not re-derive its ownership, no-drift, visibility, and test boundaries.

**Approach:** Add one normative implementation-conventions companion beside the architecture spine, link it from the spine, and close the exact retrospective action with that artifact as evidence. Describe the shipped pattern precisely, including its legitimate no-dispatch and multi-event variants.

## Boundaries & Constraints

**Always:** Codify as-built behavior: impure fail-closed orchestration; at most one server-trusted command for one durable step decision; aggregate state/idempotency/precondition guards before policy evaluation; internal `Evaluate` and `Decide` methods sharing one private computation; domain internals visible to the domain tests and Server, but never directly to Server.Tests; domain truth-table tests plus public cross-seam Server tests. Keep links repository-relative and keep the architecture spine authoritative.

**Ask First:** Any production/test code change, new architecture decision, change to friend-assembly visibility, or expansion into the separate record-and-gate or full proposal-lifecycle conventions.

**Never:** Claim every invocation dispatches, equate one command with one event, make a twin policy public, grant the domain assembly's internals to `Hexalith.Agents.Server.Tests`, rewrite historical retrospectives, or mark the broader Epic 3/Epic 4 action items done.

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` -- AD-3 and the consistency table that must discover the convention.
- `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-24.md` -- original pattern definition and action success criterion.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- exact Epic 2 action to close; broader related actions remain open.
- `src/Hexalith.Agents/Hexalith.Agents.csproj` -- canonical domain friend boundary: own tests plus Server, not Server.Tests.
- `src/Hexalith.Agents/AgentInteraction/AgentProposalEditPolicy.cs` -- representative single-event twin policy.
- `src/Hexalith.Agents/AgentInteraction/AgentProposalApprovalPolicy.cs` -- intentional one-command/multiple-events variant.
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalEditOrchestrator.cs` -- representative fail-closed orchestrator and no-dispatch authorization guard.
- `test/Hexalith.Agents.Server.Tests/AgentInteractionProposalEditOrchestratorTests.cs` -- public cross-seam no-drift proof without direct policy access.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md` -- add the short normative convention, decision flow, permitted variants, visibility rule, test obligations, and adoption checklist using representative shipped examples.
- [x] `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` -- bump `updated`, register the companion, and add a `Command steps` consistency row linking to the convention.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- mark only the exact Epic 2 step-pattern action `done` and cite the convention path.

**Acceptance Criteria:**
- Given an engineer starts a new command step, when they follow the architecture spine's `Command steps` link, then one standalone note defines the complete orchestrator, command, aggregate, policy, visibility, and test pattern without requiring story-note archaeology.
- Given an invocation fails before a durable decision or one command emits multiple ordered events, when the note explains “single command,” then it explicitly permits zero dispatch for structural/authorization/cancellation exits and multiple events from one accepted command.
- Given Server and Server.Tests consume the pattern, when visibility and verification are described, then the note requires `InternalsVisibleTo` for Server but not Server.Tests and requires Server.Tests to prove agreement through the public orchestrator-to-aggregate seam.
- Given related retrospective actions contain extra scope, when sprint status is updated, then only the exact Epic 2 action is closed.

## Spec Change Log

## Design Notes

“Single command” is a write-round-trip ownership rule: once orchestration has an audit-worthy result, it dispatches no more than one trusted command. Pre-dispatch guards may return safely, while evaluated dependency failures may be recorded through a failure-result command. `Evaluate` converts one private decision into durable event(s); `Decide` returns the same decision's safe status. Aggregate rejections/no-ops precede policy evaluation, and the approval policy proves that a single command may emit an ordered event sequence.

## Verification

**Commands:**
- `test -f _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md` -- expected: the standalone convention exists.
- `rg -n 'IMPLEMENTATION-CONVENTIONS.md|Command steps|Evaluate|Decide|InternalsVisibleTo|Server.Tests' _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/{ARCHITECTURE-SPINE.md,IMPLEMENTATION-CONVENTIONS.md} _bmad-output/implementation-artifacts/sprint-status.yaml` -- expected: discoverability, invariant terms, and closure evidence are present.
- `git diff --check -- _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md _bmad-output/implementation-artifacts/sprint-status.yaml` -- expected: no whitespace errors.

## Suggested Review Order

**Command-step contract**

- Start with the normative orchestration, aggregate, and twin-policy sequence.
  [`IMPLEMENTATION-CONVENTIONS.md:5`](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md#L5)

- Confirm zero-dispatch, deterministic retry, and ordered multi-event qualifications.
  [`IMPLEMENTATION-CONVENTIONS.md:15`](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md#L15)

**Visibility and proof**

- Verify domain internals remain available to Server, never Server.Tests.
  [`IMPLEMENTATION-CONVENTIONS.md:19`](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md#L19)

- Review the domain truth-table and public cross-seam test obligations.
  [`IMPLEMENTATION-CONVENTIONS.md:23`](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md#L23)

**Discovery and closure**

- Follow the architecture spine's canonical entry point into the convention.
  [`ARCHITECTURE-SPINE.md:267`](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#L267)

- Confirm the companion is registered in architecture metadata.
  [`ARCHITECTURE-SPINE.md:33`](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#L33)

- Verify only the exact Epic 2 action is closed.
  [`sprint-status.yaml:95`](sprint-status.yaml#L95)
