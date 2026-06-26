using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Evaluates the invocation authorization + dependency-readiness gate for an existing Agent Call
/// (<c>AgentInteraction</c>) before any provider invocation (AC1–AC4; FR-20, FR-21; AD-3, AD-12). The pure aggregate
/// computes the deterministic blockers → decision from the supplied <see cref="Verdicts"/> only — it never reads any
/// dependency itself.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Verdicts"/> are <b>server-assembled from trusted dependency reads</b> by
/// <c>AgentInteractionGateOrchestrator</c> (Story 2.2): the orchestration reads tenant access, caller/Agent Party
/// state, Source Conversation access, Agent lifecycle, Provider/model readiness, response and content-safety policy,
/// and dependency freshness through their ports, maps each to one verdict, and dispatches this command. Any
/// client-supplied verdict is stripped/overwritten by the orchestrator — a direct-gateway client cannot forge a pass
/// (AD-3 round-trip; AC3 trust model).
/// </para>
/// <para>
/// The interaction's own deterministic identity is the command envelope's <c>AggregateId</c> and the tenant scope is
/// the envelope's <c>TenantId</c>; <see cref="AgentInteractionId"/> on the payload mirrors the request command's
/// redundant-id precedent. The verdicts carry only the two safe gate enums — never claims, tokens, <c>PartyId</c>
/// personal data, provider payloads, or content (AD-14).
/// </para>
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the gate targets (mirrors the envelope's aggregate id).</param>
/// <param name="Verdicts">The server-assembled per-check verdicts (one per evaluated <see cref="AgentInteractionGateCheck"/>).</param>
public record EvaluateAgentInteractionGate(
    string AgentInteractionId,
    IReadOnlyList<AgentInvocationGateVerdict> Verdicts);
