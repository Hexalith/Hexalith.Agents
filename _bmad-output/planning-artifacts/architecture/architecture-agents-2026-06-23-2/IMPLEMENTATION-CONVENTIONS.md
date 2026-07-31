# Implementation Conventions - Command Steps

This companion defines the shipped command-step convention under [AD-3](ARCHITECTURE-SPINE.md#ad-3---pure-aggregates-side-effects-outside). The [architecture spine](ARCHITECTURE-SPINE.md) remains authoritative; this note specifies how to implement its pure-aggregate boundary consistently. If the two documents diverge, the architecture spine prevails.

## Normative Pattern

A command step MUST use an impure, fail-closed Server orchestrator around a pure EventStore aggregate:

1. The orchestrator performs dependency reads, authorization, measurement, external calls, and other I/O. It reduces those observations to one server-trusted result for the step.
2. Structural, response-mode, authorization, and cancellation exits MAY return or propagate without dispatch. Once the step has one audit-worthy durable result, the orchestrator MUST build and dispatch at most one trusted command and envelope. An evaluated dependency failure SHOULD travel in a content-safe failure-result command when the failure itself requires durable audit evidence; that result carries only the safe classifications and evidence allowed by [AD-14](ARCHITECTURE-SPINE.md#ad-14---sensitive-content-and-secret-safety), never raw dependency payloads or exceptions.
3. The aggregate `Handle` method MUST enforce authoritative state, idempotency, terminal-state, and command-precondition guards before policy evaluation. A rejection or no-op at this seam does not call the policy.
4. An accepted command delegates to an internal, pure, deterministic twin-policy. `Evaluate` maps the trusted result to durable event(s); `Decide` maps that same result to the safe status returned by orchestration. Both MUST call one private `Compute` implementation so their result-to-decision mapping cannot drift. This guarantee applies only after the aggregate's authoritative guards accept the command; it does not bypass a rejection/no-op or assert that dispatch was durably accepted.
5. On a dispatching path, the orchestrator dispatches that command once and returns `Decide` for the same trusted result. It MUST NOT reproduce the policy decision independently.

"Single command" is a write-round-trip ownership rule, not a claim that every invocation dispatches or that every command emits one event. A retry is a later invocation and MUST reuse the same deterministic command identity and idempotency metadata under [AD-13](ARCHITECTURE-SPINE.md#ad-13---idempotent-external-effects); it does not authorize a second distinct command for the same attempt. One accepted command MAY return multiple events together in one `DomainResult`, in deterministic order, for EventStore to append as that command's result. For example, [`AgentProposalApprovalPolicy`](../../../../src/Hexalith.Agents/AgentInteraction/AgentProposalApprovalPolicy.cs) maps one `ApproveProposedAgentReply` command to `Approved -> PostingPending -> Posted` or `Approved -> PostingPending -> PostingFailed`, while an approval failure emits one event.

Authorization handling remains operation-specific. Edit and regeneration can deny before dispatch; approval can dispatch a denied result when durable failure evidence is required. Either form obeys the at-most-one-command rule.

## Visibility Boundary

Twin-policies are domain implementation details and MUST remain `internal`. The domain assembly grants `InternalsVisibleTo` to its own domain tests and to `Hexalith.Agents.Server`, which must call `Decide`; it MUST NOT grant domain-internal access to `Hexalith.Agents.Server.Tests`. The canonical boundary is [`Hexalith.Agents.csproj`](../../../../src/Hexalith.Agents/Hexalith.Agents.csproj).

## Test Obligations

- Domain tests MUST exercise policy truth tables directly, prove `Evaluate` and `Decide` agree for every result variant, verify ordered multi-event sequences where applicable, and prove aggregate rejections/no-ops occur before policy evaluation.
- Server tests MUST prove zero-dispatch exits and at-most-one dispatch through the orchestrator.
- Server tests MUST prove no drift without calling the internal policy: capture the dispatched public envelope, deserialize its public command, seed real aggregate state, call the public aggregate `Handle` seam, and compare the emitted decision with the orchestrator's returned status. [`AgentInteractionProposalEditOrchestratorTests`](../../../../test/Hexalith.Agents.Server.Tests/AgentInteractionProposalEditOrchestratorTests.cs) is the representative cross-seam test.

## Representative Shipped Examples

- [`AgentInteractionProposalEditOrchestrator`](../../../../src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalEditOrchestrator.cs) demonstrates an impure fail-closed orchestrator, a no-dispatch authorization exit, one trusted command, and `Decide` after dispatch.
- [`AgentProposalEditPolicy`](../../../../src/Hexalith.Agents/AgentInteraction/AgentProposalEditPolicy.cs) demonstrates a single-event twin-policy whose `Evaluate` and `Decide` share one private `Compute`.
- [`AgentProposalApprovalPolicy`](../../../../src/Hexalith.Agents/AgentInteraction/AgentProposalApprovalPolicy.cs) demonstrates the permitted one-command/multiple-ordered-events variant.

## Adoption Checklist

- Identify the single durable step decision and its server-trusted result.
- Classify structural, mode, authorization, and cancellation exits that may dispatch zero commands; classify failures that require durable evidence.
- State the operation-specific authorization and cancellation recording choice instead of inheriting one implicitly.
- Keep all I/O in the orchestrator and all authoritative state/idempotency/precondition guards in the aggregate.
- Dispatch no more than one command for the step result; reuse its deterministic identity on retry, and allow the aggregate to emit the ordered event sequence the decision requires.
- Implement one internal policy with `Evaluate`, `Decide`, and a shared private `Compute`; do not duplicate decision logic.
- Preserve the domain friend boundary: own tests and Server, never Server.Tests.
- Add domain truth-table tests and public Server cross-seam agreement tests.
