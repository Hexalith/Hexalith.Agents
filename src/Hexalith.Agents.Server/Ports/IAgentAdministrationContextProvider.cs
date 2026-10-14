namespace Hexalith.Agents.Server.Ports;

/// <summary>Resolves the trusted <see cref="AgentAdministrationContext"/> for the in-flight request.</summary>
public interface IAgentAdministrationContextProvider
{
    /// <summary>Resolves the current caller context, failing closed to <see cref="AgentAdministrationContext.Anonymous"/>.</summary>
    /// <returns>The trusted caller context.</returns>
    AgentAdministrationContext GetContext();

    /// <summary>Resolves current authority for one operation-specific principal. Legacy providers preserve identity
    /// only; their role booleans cannot establish current authority.</summary>
    /// <param name="platform">Whether the operation requires the Platform principal.</param>
    /// <param name="cancellationToken">The operation cancellation token.</param>
    /// <returns>Authenticated identity with both authority flags cleared unless a current-authority adapter overrides this method.</returns>
    ValueTask<AgentAdministrationContext> GetContextAsync(bool platform, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AgentAdministrationContext identity = GetContext();
        return ValueTask.FromResult(identity with
        {
            IsAgentsAdmin = false,
            IsPlatformOperator = false,
        });
    }
}
