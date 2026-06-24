using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read/invoke seam for the Conversation-originated Agent Call (<c>hexa</c>) surface (AC1, AC3; AD-15). The
/// invocation panel and feedback surface depend only on this abstraction and the public display contracts, never on
/// <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals. Mirrors
/// <see cref="IAgentSetupGateway"/>.
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model/command path and is
/// deferred to the dedicated Agents read-model / BFF story (Epic 4, 4.1/4.3), mirroring the deferred-binding
/// convention. The bUnit component tests substitute this seam with NSubstitute, so the
/// <see cref="DeferredConversationAgentCallGateway"/> placeholder is never exercised in tests but keeps the DI graph
/// complete. Every method returns a fail-closed result wrapper so a non-success outcome carries no interaction identity
/// (AD-12, AD-14).
/// </remarks>
public interface IConversationAgentCallGateway
{
    /// <summary>
    /// Submits the normalized Conversation-originated Agent Call. Returns a structured fail-closed result; the
    /// <see cref="AgentCallRequestResult.Reference"/> is non-null only on <see cref="AgentCallRequestStatus.Accepted"/>.
    /// </summary>
    /// <param name="request">The safe Agent Call inputs (the sensitive prompt flows here only — AD-14).</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight request.</param>
    /// <returns>The fail-closed Agent Call request result.</returns>
    Task<AgentCallRequestResult> RequestCallAsync(ConversationAgentCallRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the safe <see cref="AgentInteractionStatusView"/> for live call-status feedback. Returns a structured
    /// fail-closed result; the <see cref="AgentInteractionInspectionResult.View"/> is non-null only on
    /// <see cref="AgentInteractionInspectionStatus.Success"/>.
    /// </summary>
    /// <param name="agentInteractionId">The safe Agent Call identifier handle returned on acceptance.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed Agent Call inspection result.</returns>
    Task<AgentInteractionInspectionResult> GetCallStatusAsync(string agentInteractionId, CancellationToken cancellationToken);
}
