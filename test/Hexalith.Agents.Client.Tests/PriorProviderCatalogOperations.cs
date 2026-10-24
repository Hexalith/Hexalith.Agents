using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.Client.Tests;

/// <summary>Compiles the provider interface surface that preceded the successor default member.</summary>
internal sealed class PriorProviderCatalogOperations : IProviderCatalogOperations
{
    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentSetupWriteStatus>> GetCommandOutcomeAsync(
        string targetTenantId,
        string messageId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<TenantProviderEnablementInspectionResult>> GetTenantEnablementAsync(
        string tenantId,
        string providerId,
        string modelId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>> ListTenantEntriesAsync(
        bool includeDisabled = false,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>> GetTenantEntryAsync(
        string providerId,
        string modelId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> SetTenantEnablementAsync(
        SetTenantProviderModelEnablement command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> DecideDataHandlingAsync(
        DecideProviderDataHandling command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> ListEntriesAsync(
        bool includeDisabled,
        string? expectedProjectionVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> GetEntryAsync(
        string providerId,
        string modelId,
        int? expectedCapabilityVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> CreateEntryAsync(
        CreateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> UpdateEntryAsync(
        UpdateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> EnableEntryAsync(
        EnableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> DisableEntryAsync(
        DisableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}
