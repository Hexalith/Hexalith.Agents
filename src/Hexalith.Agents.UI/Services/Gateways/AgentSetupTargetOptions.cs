namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Names the Agent (<c>hexa</c>) the single-Agent admin-setup surfaces operate on. The tenant is never configured
/// here — it is derived server-side from the authenticated principal, so a UI configuration change can never widen
/// what an administrator may see or mutate.
/// </summary>
public sealed class AgentSetupTargetOptions
{
    /// <summary>The configuration section binding the setup target.</summary>
    public const string SectionName = "Agents:Ui";

    /// <summary>Gets or sets the Agent aggregate id the setup surfaces administer.</summary>
    public string AgentId { get; set; } = string.Empty;
}
