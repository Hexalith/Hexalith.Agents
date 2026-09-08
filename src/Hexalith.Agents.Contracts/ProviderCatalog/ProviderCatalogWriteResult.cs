using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Structured result of a provider-catalog write (Story 5.3). A non-<see cref="AgentSetupWriteStatus.Submitted"/>
/// outcome carries no acceptance, so a denied or failed write can never be rendered as progress or callability.
/// </summary>
/// <param name="Status">The write outcome.</param>
/// <param name="Acceptance">The structured accepted identity (non-null only on <see cref="AgentSetupWriteStatus.Submitted"/>).</param>
public record ProviderCatalogWriteResult(
    AgentSetupWriteStatus Status,
    ProviderCatalogCommandAcceptance? Acceptance)
{
    /// <summary>Gets the truth stage this write has reached (never a projection claim).</summary>
    public AgentSetupTruthState TruthState
        => Acceptance?.TruthState ?? AgentSetupTruthState.Unknown;

    /// <summary>Creates a submitted result carrying the structured accepted identity.</summary>
    /// <param name="acceptance">The accepted identity.</param>
    /// <returns>A submitted result.</returns>
    public static ProviderCatalogWriteResult Submitted(ProviderCatalogCommandAcceptance acceptance)
        => new(AgentSetupWriteStatus.Submitted, acceptance);

    /// <summary>Creates a fail-closed result with no acceptance.</summary>
    /// <param name="status">The non-submitted outcome.</param>
    /// <returns>A failed result.</returns>
    public static ProviderCatalogWriteResult Failed(AgentSetupWriteStatus status)
        => new(status, null);
}
