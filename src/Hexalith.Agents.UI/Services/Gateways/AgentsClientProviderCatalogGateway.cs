using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Live <see cref="IProviderCatalogGateway"/> over the public Agents client (Story 5.3). FrontComposer reads
/// exactly what the API and any other automation client read, and writes through the same public operations.
/// </summary>
public sealed class AgentsClientProviderCatalogGateway(IAgentsClient client) : IProviderCatalogGateway
{
    private readonly IAgentsClient _client = client ?? throw new ArgumentNullException(nameof(client));

    /// <inheritdoc />
    public async Task<ProviderCatalogInspectionResult> ListEntriesAsync(
        bool includeDisabled,
        string? expectedProjectionVersion,
        CancellationToken cancellationToken)
    {
        AgentOperationResult<ProviderCatalogInspectionResult> result = await _client.ProviderCatalog
            .ListEntriesAsync(includeDisabled, expectedProjectionVersion, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return result is { IsSuccess: true, Value: { } inspection } ? inspection : ToReadResult(result.Status);
    }

    /// <inheritdoc />
    public async Task<ProviderCatalogInspectionResult> GetEntryAsync(
        string providerId,
        string modelId,
        int? expectedCapabilityVersion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        AgentOperationResult<ProviderCatalogInspectionResult> result = await _client.ProviderCatalog
            .GetEntryAsync(providerId, modelId, expectedCapabilityVersion, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return result is { IsSuccess: true, Value: { } inspection } ? inspection : ToReadResult(result.Status);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> CreateAsync(CreateProviderModelEntry command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(ct => _client.ProviderCatalog.CreateEntryAsync(command, cancellationToken: ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> UpdateAsync(UpdateProviderModelEntry command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(ct => _client.ProviderCatalog.UpdateEntryAsync(command, cancellationToken: ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> EnableAsync(EnableProviderModelEntry command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(ct => _client.ProviderCatalog.EnableEntryAsync(command, cancellationToken: ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> DisableAsync(DisableProviderModelEntry command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(ct => _client.ProviderCatalog.DisableEntryAsync(command, cancellationToken: ct), cancellationToken);
    }

    private static ProviderCatalogInspectionResult ToReadResult(AgentOperationStatus status)
        => status switch
        {
            AgentOperationStatus.NotAuthorized => ProviderCatalogInspectionResult.NotAuthorized(),
            AgentOperationStatus.NotFound => ProviderCatalogInspectionResult.NotFound(),
            _ => ProviderCatalogInspectionResult.Unavailable(),
        };

    private static AgentSetupWriteStatus ToWriteStatus(AgentOperationStatus status)
        => status switch
        {
            AgentOperationStatus.NotAuthorized => AgentSetupWriteStatus.NotAuthorized,
            AgentOperationStatus.NotFound => AgentSetupWriteStatus.NotFound,
            AgentOperationStatus.ValidationFailed => AgentSetupWriteStatus.ValidationFailed,
            AgentOperationStatus.Conflict or AgentOperationStatus.Stale => AgentSetupWriteStatus.Conflict,
            _ => AgentSetupWriteStatus.Unavailable,
        };

    private static async Task<ProviderCatalogWriteResult> WriteAsync(
        Func<CancellationToken, ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>>> write,
        CancellationToken cancellationToken)
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await write(cancellationToken).ConfigureAwait(false);
        return result is { IsSuccess: true, Value: { } acceptance }
            ? ProviderCatalogWriteResult.Submitted(acceptance)
            : ProviderCatalogWriteResult.Failed(ToWriteStatus(result.Status));
    }
}
