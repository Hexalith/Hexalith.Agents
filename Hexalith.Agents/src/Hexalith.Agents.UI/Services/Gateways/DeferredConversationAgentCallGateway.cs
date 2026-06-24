using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IConversationAgentCallGateway"/> that keeps the DI graph complete and the UI
/// project buildable before the live Agents read/command path is wired. Both methods fail closed
/// (<see cref="AgentCallRequestResult.NotAuthorized"/> / <see cref="AgentInteractionInspectionResult.NotAuthorized"/>),
/// so an unbound host renders the permission-denied surface rather than fabricating a "posted/ready" call (AD-12).
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model/command path and lands
/// with the deferred Agents read-model / BFF story (Epic 4, 4.1/4.3) — exactly like <see cref="DeferredAgentSetupGateway"/>.
/// The bUnit tests substitute the gateway with NSubstitute, so this placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredConversationAgentCallGateway : IConversationAgentCallGateway
{
    /// <inheritdoc />
    public Task<AgentCallRequestResult> RequestCallAsync(ConversationAgentCallRequest request, CancellationToken cancellationToken)
        => Task.FromResult(AgentCallRequestResult.NotAuthorized());

    /// <inheritdoc />
    public Task<AgentInteractionInspectionResult> GetCallStatusAsync(string agentInteractionId, CancellationToken cancellationToken)
        => Task.FromResult(AgentInteractionInspectionResult.NotAuthorized());
}
