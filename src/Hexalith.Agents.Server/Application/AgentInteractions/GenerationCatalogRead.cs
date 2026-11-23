using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>The current catalog entry admitted for provider invocation, or the reason the read failed.</summary>
internal sealed record GenerationCatalogRead(AgentGenerationOutcome Outcome, ProviderCatalogEntryView? Entry)
{
    private static readonly GenerationCatalogRead _unavailable = new(AgentGenerationOutcome.ProviderUnavailable, null);

    internal static async Task<GenerationCatalogRead> ReadAsync(
        IProviderCatalogReader reader, string tenantId, string providerId, string modelId, int capabilityFloor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(modelId) || capabilityFloor <= 0)
        {
            return _unavailable;
        }

        ProviderCatalogEntryReadResult result;
        try
        {
            result = await reader.GetEntryAsync(tenantId, providerId, modelId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return _unavailable;
        }

        if (result is not { Status: ProviderCatalogInspectionStatus.Success, Entry: { } entry }
            || !string.Equals(entry.ProviderId, providerId, StringComparison.Ordinal)
            || !string.Equals(entry.ModelId, modelId, StringComparison.Ordinal)
            || entry.CapabilityVersion < capabilityFloor)
        {
            return _unavailable;
        }

        ProviderSelectionValidationStatus selection = ProviderSelectionVerdict.Evaluate(result);
        if (selection is ProviderSelectionValidationStatus.Disabled or ProviderSelectionValidationStatus.NotTextGenerationCapable)
        {
            return new(AgentGenerationOutcome.ProviderDisabled, null);
        }

        if (selection != ProviderSelectionValidationStatus.Valid
            || entry.MaxOutputTokenLimit >= entry.ContextWindowTokenLimit
            || entry.TimeoutPolicy.RequestTimeoutMilliseconds > ProviderCatalogAggregate.MaxRequestTimeoutMilliseconds
            || entry.TimeoutPolicy.MaxRetries > ProviderCatalogAggregate.MaxRetryCount)
        {
            return _unavailable;
        }

        return new(AgentGenerationOutcome.Succeeded, entry);
    }
}
