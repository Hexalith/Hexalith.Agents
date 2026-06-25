using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.Client;

/// <summary>Public provider-catalog administration operations.</summary>
public interface IProviderCatalogOperations
{
    /// <summary>Lists authorized provider/model catalog entries.</summary>
    ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> ListEntriesAsync(
        bool includeDisabled,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one authorized provider/model catalog entry.</summary>
    ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> GetEntryAsync(
        string providerId,
        string modelId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a provider/model catalog entry.</summary>
    ValueTask<AgentOperationResult> CreateEntryAsync(
        CreateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates provider/model metadata.</summary>
    ValueTask<AgentOperationResult> UpdateEntryAsync(
        UpdateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Enables a provider/model catalog entry.</summary>
    ValueTask<AgentOperationResult> EnableEntryAsync(
        EnableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Disables a provider/model catalog entry.</summary>
    ValueTask<AgentOperationResult> DisableEntryAsync(
        DisableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);
}
