using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Tests;

/// <summary>A historical directory implementation used to prove the new interface default stays closed.</summary>
internal sealed class LegacyCallerOnlyPartyDirectory : IAgentPartyDirectory
{
    /// <summary>Gets whether the historical caller method was invoked.</summary>
    internal bool LegacyCalled { get; private set; }

    /// <inheritdoc />
    public Task<AgentPartyValidationResult> ValidateCallerPartyAsync(string tenantId, string partyId, CancellationToken ct)
    {
        LegacyCalled = true;
        return Task.FromResult(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, partyId));
    }

    /// <inheritdoc />
    public Task<AgentPartyValidationResult> ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public Task<AgentPartyValidationResult> ProvisionAgentPartyAsync(string tenantId, AgentPartyProvisioningRequest request, CancellationToken ct)
        => throw new NotSupportedException();
}
