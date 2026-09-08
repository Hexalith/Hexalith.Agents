using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.Client;

/// <summary>Public provider-catalog administration operations.</summary>
public interface IProviderCatalogOperations
{
    /// <summary>Lists authorized provider/model catalog entries.</summary>
    /// <param name="includeDisabled">Whether disabled and otherwise ineligible entries are included.</param>
    /// <param name="expectedProjectionVersion">The projection version the caller is waiting to see, if any.</param>
    /// <param name="options">Optional operation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured catalog inspection result.</returns>
    ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> ListEntriesAsync(
        bool includeDisabled,
        string? expectedProjectionVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one authorized provider/model catalog entry.</summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="expectedCapabilityVersion">The capability version the caller is waiting to see, if any.</param>
    /// <param name="options">Optional operation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured catalog inspection result.</returns>
    ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> GetEntryAsync(
        string providerId,
        string modelId,
        int? expectedCapabilityVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a provider/model catalog entry.</summary>
    /// <param name="command">The create command.</param>
    /// <param name="options">Optional operation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity.</returns>
    ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> CreateEntryAsync(
        CreateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates provider/model metadata and pricing.</summary>
    /// <param name="command">The update command.</param>
    /// <param name="options">Optional operation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity.</returns>
    ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> UpdateEntryAsync(
        UpdateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Enables a provider/model catalog entry.</summary>
    /// <param name="command">The enable command.</param>
    /// <param name="options">Optional operation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity.</returns>
    ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> EnableEntryAsync(
        EnableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Disables a provider/model catalog entry.</summary>
    /// <param name="command">The disable command.</param>
    /// <param name="options">Optional operation options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The structured accepted identity.</returns>
    ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> DisableEntryAsync(
        DisableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);
}
