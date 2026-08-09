using System;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The single authoritative setup truth an administrator sees, on the API/client and on the FrontComposer
/// configuration surface alike (Story 5.2 AC2). It wraps the existing safe <see cref="AgentStatusView"/> — so no
/// instruction text or policy content is added to the disclosure surface — and adds only the version, freshness,
/// and truth-stage metadata needed to tell "submitted" from "durably projected".
/// </summary>
/// <remarks>
/// This view deliberately carries no callability signal. <see cref="AgentStatusView.Lifecycle"/> reaching
/// <see cref="AgentLifecycleStatus.Active"/> is a lifecycle flag; whether the Agent can be called is decided by
/// the call-path readiness gates in later stories and must never be inferred from this view.
/// </remarks>
/// <param name="Agent">The safe Agent status view (never carries instructions or policy content; AD-14).</param>
/// <param name="ConfigurationVersion">The authoritative monotonic configuration version the observation reflects.</param>
/// <param name="ProjectionVersion">An opaque projection version token, or <see langword="null"/> when the read model has never been projected.</param>
/// <param name="ProjectedAt">When the projection last ran, or <see langword="null"/> when the read model has never been projected.</param>
/// <param name="Freshness">How current the projection is relative to <paramref name="ConfigurationVersion"/>.</param>
/// <param name="TruthState">The truth stage this observation has reached.</param>
public record AgentSetupView(
    AgentStatusView Agent,
    int ConfigurationVersion,
    string? ProjectionVersion,
    DateTimeOffset? ProjectedAt,
    AgentSetupFreshness Freshness,
    AgentSetupTruthState TruthState);
