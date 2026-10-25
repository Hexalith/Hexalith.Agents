using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Services.Gateways;

namespace Hexalith.Agents.UI.State;

/// <summary>One immutable setup intent, with no reference to a page or gateway.</summary>
/// <param name="Options">The exact correlation, idempotency and expected-version identities.</param>
/// <param name="Kind">The setup operation to replay.</param>
/// <param name="Configuration">The exact configuration payload, including write-only instructions.</param>
/// <param name="ResponseMode">The exact response mode for a mode change.</param>
internal sealed record AgentSetupAttempt(
    AgentOperationOptions Options,
    AgentSetupAttemptKind Kind,
    UpdateAgentConfiguration? Configuration = null,
    AgentResponseMode ResponseMode = AgentResponseMode.Unknown)
{
    /// <summary>Submits this intent through the current page's authorized gateway.</summary>
    /// <param name="gateway">The current gateway, never retained by the attempt.</param>
    /// <param name="cancellationToken">The caller's observation lifetime.</param>
    /// <returns>The typed write result.</returns>
    internal Task<AgentSetupWriteResult> SubmitAsync(IAgentSetupGateway gateway, CancellationToken cancellationToken)
        => Kind switch
        {
            AgentSetupAttemptKind.Configuration when Configuration is not null
                => gateway.UpdateConfigurationAsync(Configuration, Options, cancellationToken),
            AgentSetupAttemptKind.ResponseMode => gateway.ConfigureResponseModeAsync(ResponseMode, Options, cancellationToken),
            AgentSetupAttemptKind.Activate => gateway.ActivateAsync(Options, cancellationToken),
            AgentSetupAttemptKind.Disable => gateway.DisableAsync(Options, cancellationToken),
            _ => throw new InvalidOperationException("The retained setup intent is invalid."),
        };
}
