namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// The safe inputs the proposal-editor assembles for one authorized edit of a pending Proposed Agent Reply (Story 3.3;
/// AC1). It carries the proposal/interaction/source-version references and the Approver's edited content — it does NOT
/// construct the trusted edited-version id, the authorization verdict, or the editor Party reference, which the server
/// populates per <c>EditProposedAgentReply</c>.
/// </summary>
/// <remarks>
/// The <see cref="EditedContent"/> is sensitive content (AD-14): it flows to the gateway only and is never echoed into a
/// version label, badge, accessible name, tooltip, <c>data-testid</c>, live-region announcement, or log.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the proposal's aggregate id).</param>
/// <param name="ProposalId">The deterministic proposal identifier being edited (created in Story 3.1).</param>
/// <param name="SourceVersionId">The id of the version being edited from (its provenance).</param>
/// <param name="EditedContent">The Approver's edited content (sensitive — carried to the gateway only; AD-14).</param>
/// <param name="ClientCorrelationId">An optional client correlation id for idempotency/telemetry.</param>
public record ProposalEditRequest(
    string AgentInteractionId,
    string ProposalId,
    string SourceVersionId,
    string EditedContent,
    string? ClientCorrelationId);
