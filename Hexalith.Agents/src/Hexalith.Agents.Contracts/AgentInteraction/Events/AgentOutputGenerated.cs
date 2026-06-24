namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) generated output that passed Content Safety Policy and may proceed
/// to the response-mode branch — Story 2.5 automatic post / Story 3.1 proposal (AC2, AC4; FR-9, FR-19, FR-20). This is a
/// durable success event (NOT an <c>IRejectionEvent</c>): it transitions the interaction status to
/// <see cref="AgentInteractionStatus.Generated"/> and is the SOLE durable home of the generated content (AD-5, AD-14).
/// </summary>
/// <remarks>
/// The event carries the deterministic aggregate id and the <see cref="AgentGeneratedVersion"/> — the single approvable/
/// postable version. <b>Sensitive content (AD-14):</b> the version's <see cref="AgentGeneratedVersion.GeneratedContent"/>
/// lives here (and on the aggregate state) and NOWHERE else — never on the status view, outcome result, rejection, failure
/// event, logs, telemetry, or audit summaries. There is no wall-clock field — generation time is the EventStore event
/// metadata (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Version">The generated version (the approvable/postable unit; sole home of the generated content).</param>
public record AgentOutputGenerated(
    string AgentInteractionId,
    AgentGeneratedVersion Version) : IEventPayload;
