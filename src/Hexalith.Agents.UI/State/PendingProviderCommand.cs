using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.UI.State;

/// <summary>Safe command identity retained for one browser session while exact truth is pending.</summary>
/// <param name="Family">The governance command family.</param>
/// <param name="ResourceKey">The resource whose next write is locked.</param>
/// <param name="TenantId">The command's owning tenant.</param>
/// <param name="Acceptance">The submitted command identity.</param>
/// <param name="ExpectedCapabilityVersion">The expected platform capability version, if relevant.</param>
/// <param name="ExpectedStatus">The expected platform lifecycle status, if relevant.</param>
public sealed record PendingProviderCommand(
    string Family,
    string ResourceKey,
    string TenantId,
    ProviderCatalogCommandAcceptance Acceptance,
    int? ExpectedCapabilityVersion = null,
    ProviderModelStatus? ExpectedStatus = null)
{
    /// <summary>Gets whether this identity is a durable intention whose submission is not yet acknowledged.</summary>
    public bool IsPrepared { get; init; }

    /// <summary>Creates a safe write-ahead intention without claiming backend acceptance.</summary>
    public static PendingProviderCommand Prepare(string family, string resourceKey, string tenantId,
        string providerId, string modelId, string messageId, string correlationId,
        int? expectedCapabilityVersion = null, ProviderModelStatus? expectedStatus = null)
        => new(family, resourceKey, tenantId,
            new(providerId, modelId, messageId, correlationId, AgentSetupTruthState.Unknown),
            expectedCapabilityVersion, expectedStatus) { IsPrepared = true };

    /// <summary>Reports whether an acceptance matches this exact prepared command.</summary>
    public bool Matches(ProviderCatalogCommandAcceptance? acceptance)
        => acceptance is { TruthState: AgentSetupTruthState.Submitted or AgentSetupTruthState.AuthoritativePending or AgentSetupTruthState.ProjectionConfirmed }
            && acceptance.ProviderId == Acceptance.ProviderId && acceptance.ModelId == Acceptance.ModelId
            && acceptance.MessageId == Acceptance.MessageId && acceptance.CorrelationId == Acceptance.CorrelationId;
}
