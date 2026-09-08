namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Host-supplied configuration for the provider-catalog read model (Story 5.3). Bound from the same
/// <c>Agents:ReadModel</c> section as Agent setup so the module does not invent catalog-only store metadata.
/// </summary>
public sealed class ProviderCatalogReadModelOptions
{
    /// <summary>The configuration section the host binds these options from.</summary>
    public const string SectionName = AgentSetupReadModelOptions.SectionName;

    /// <summary>Gets or sets the DAPR state-store component name holding the catalog read model.</summary>
    public string StateStoreName { get; set; } = "statestore";
}
