namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Host-supplied configuration for the Agent setup read model (Story 5.2). Bound from the
/// <c>Agents:ReadModel</c> configuration section.
/// </summary>
public sealed class AgentSetupReadModelOptions
{
    /// <summary>The configuration section the host binds these options from.</summary>
    public const string SectionName = "Agents:ReadModel";

    /// <summary>Gets or sets the DAPR state-store component name holding the Agent setup read model.</summary>
    public string StateStoreName { get; set; } = "statestore";
}
