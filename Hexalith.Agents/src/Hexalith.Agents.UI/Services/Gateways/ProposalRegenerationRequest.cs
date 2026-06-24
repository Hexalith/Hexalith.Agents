namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// The safe inputs the regenerate control assembles for one authorized regeneration of a pending Proposed Agent Reply
/// (Story 3.4; AC1). It carries only the proposal/interaction references — it does NOT construct the trusted
/// regenerated-version id, the authorization verdict, the deterministic attempt id, or the requester Party reference, which
/// the server populates per <c>RegenerateProposedAgentReply</c>.
/// </summary>
/// <remarks>
/// Unlike the edit request, this request carries NO content: regeneration re-invokes the provider server-side, so there is
/// no Approver-supplied text. Nothing here is ever echoed into a version label, badge, accessible name, tooltip,
/// <c>data-testid</c>, live-region announcement, or log (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the proposal's aggregate id).</param>
/// <param name="ProposalId">The deterministic proposal identifier being regenerated (created in Story 3.1).</param>
/// <param name="ClientCorrelationId">An optional client correlation id for idempotency/telemetry.</param>
public record ProposalRegenerationRequest(
    string AgentInteractionId,
    string ProposalId,
    string? ClientCorrelationId);
