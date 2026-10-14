namespace Hexalith.Agents.Server.Ports;

/// <summary>Resolves the trusted <see cref="AgentAdministrationContext"/> for the in-flight request.</summary>
public interface IAgentAdministrationContextProvider
{
    /// <summary>Resolves the current caller context, failing closed to <see cref="AgentAdministrationContext.Anonymous"/>.</summary>
    /// <returns>The trusted caller context.</returns>
    AgentAdministrationContext GetContext();

    /// <summary>Resolves current authority for one operation-specific principal.</summary>
    ValueTask<AgentAdministrationContext> GetContextAsync(bool platform, CancellationToken cancellationToken)
        => ValueTask.FromResult(GetContext());
}
