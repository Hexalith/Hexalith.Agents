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
    public Task<AgentSetupWriteStatus> GetCommandOutcomeAsync(
        string targetTenantId, string messageId, CancellationToken cancellationToken)
        => ReadCommandOutcomeAsync(targetTenantId, messageId, null, cancellationToken);

    /// <inheritdoc />
    public Task<AgentSetupWriteStatus> GetCommandOutcomeAsync(
        string targetTenantId, string messageId, string correlationId, CancellationToken cancellationToken)
        => ReadCommandOutcomeAsync(targetTenantId, messageId, correlationId, cancellationToken);

    private async Task<AgentSetupWriteStatus> ReadCommandOutcomeAsync(
        string targetTenantId, string messageId, string? correlationId, CancellationToken cancellationToken)
    {
        AgentOperationResult<AgentSetupWriteOutcome> result = await _client.ProviderCatalog
            .GetCommandWriteOutcomeAsync(targetTenantId, messageId, new AgentOperationOptions(correlationId), cancellationToken)
            .ConfigureAwait(false);
        return result.IsSuccess && result.Value is { } status
            ? status switch
            {
                AgentSetupWriteOutcome.Unknown => AgentSetupWriteStatus.UnableToVerify,
                >= AgentSetupWriteOutcome.Submitted and <= AgentSetupWriteOutcome.Rejected => (AgentSetupWriteStatus)((int)status - 1),
                _ => AgentSetupWriteStatus.UnableToVerify,
            } : result.Status is AgentOperationStatus.NotAuthorized
                ? AgentSetupWriteStatus.NotAuthorized : AgentSetupWriteStatus.UnableToVerify;
    }

    /// <inheritdoc />
    public async Task<TenantProviderEnablementInspectionResult> GetTenantEnablementAsync(
        string tenantId, string providerId, string modelId, CancellationToken cancellationToken)
    {
        AgentOperationResult<TenantProviderEnablementInspectionResult> result = await _client.ProviderCatalog
            .GetTenantEnablementAsync(tenantId, providerId, modelId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return result is { IsSuccess: true, Value: { } inspection }
            ? inspection : new(ToReadResult(result.Status).Status, null, null);
    }

    /// <inheritdoc />
    public async Task<TenantProviderCatalogInspectionResult> ListTenantEntriesAsync(bool includeDisabled, CancellationToken cancellationToken)
    {
        AgentOperationResult<TenantProviderCatalogInspectionResult> result = await _client.ProviderCatalog
            .ListTenantEntriesAsync(includeDisabled, cancellationToken: cancellationToken).ConfigureAwait(false);
        return result is { IsSuccess: true, Value: { } inspection }
            ? inspection : new(ToReadResult(result.Status).Status, []);
    }

    /// <inheritdoc />
    public async Task<TenantProviderCatalogInspectionResult> GetTenantEntryAsync(string providerId, string modelId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        AgentOperationResult<TenantProviderCatalogInspectionResult> result = await _client.ProviderCatalog
            .GetTenantEntryAsync(providerId, modelId, cancellationToken: cancellationToken).ConfigureAwait(false);
        return result is { IsSuccess: true, Value: { } inspection }
            ? inspection : new(ToReadResult(result.Status).Status, []);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> SetTenantEnablementAsync(SetTenantProviderModelEnablement command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(ct => _client.ProviderCatalog.SetTenantEnablementAsync(command, cancellationToken: ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> DecideDataHandlingAsync(DecideProviderDataHandling command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(ct => _client.ProviderCatalog.DecideDataHandlingAsync(command, cancellationToken: ct), cancellationToken);
    }

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

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> CreateAsync(CreateProviderModelEntry command, ProviderCatalogCommandAcceptance identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.ProviderId != command.ProviderId || identity.ModelId != command.ModelId
            || identity.TruthState != AgentSetupTruthState.Unknown
            || !IsCanonicalIdentity(identity.MessageId) || !IsCanonicalIdentity(identity.CorrelationId))
        {
            return Task.FromResult(ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.ValidationFailed));
        }
        return WritePreparedAsync(identity, ct => _client.ProviderCatalog.CreateEntryAsync(command,
            new AgentOperationOptions(identity.CorrelationId, identity.MessageId), ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> UpdateAsync(UpdateProviderModelEntry command, ProviderCatalogCommandAcceptance identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.ProviderId != command.ProviderId || identity.ModelId != command.ModelId
            || identity.TruthState != AgentSetupTruthState.Unknown
            || !IsCanonicalIdentity(identity.MessageId) || !IsCanonicalIdentity(identity.CorrelationId))
        {
            return Task.FromResult(ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.ValidationFailed));
        }
        return WritePreparedAsync(identity, ct => _client.ProviderCatalog.UpdateEntryAsync(command,
            new AgentOperationOptions(identity.CorrelationId, identity.MessageId), ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> EnableAsync(EnableProviderModelEntry command, ProviderCatalogCommandAcceptance identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.ProviderId != command.ProviderId || identity.ModelId != command.ModelId
            || identity.TruthState != AgentSetupTruthState.Unknown
            || !IsCanonicalIdentity(identity.MessageId) || !IsCanonicalIdentity(identity.CorrelationId))
        {
            return Task.FromResult(ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.ValidationFailed));
        }
        return WritePreparedAsync(identity, ct => _client.ProviderCatalog.EnableEntryAsync(command,
            new AgentOperationOptions(identity.CorrelationId, identity.MessageId), ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> DisableAsync(DisableProviderModelEntry command, ProviderCatalogCommandAcceptance identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.ProviderId != command.ProviderId || identity.ModelId != command.ModelId
            || identity.TruthState != AgentSetupTruthState.Unknown
            || !IsCanonicalIdentity(identity.MessageId) || !IsCanonicalIdentity(identity.CorrelationId))
        {
            return Task.FromResult(ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.ValidationFailed));
        }
        return WritePreparedAsync(identity, ct => _client.ProviderCatalog.DisableEntryAsync(command,
            new AgentOperationOptions(identity.CorrelationId, identity.MessageId), ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> SetTenantEnablementAsync(SetTenantProviderModelEnablement command, ProviderCatalogCommandAcceptance identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.ProviderId != command.ProviderId || identity.ModelId != command.ModelId
            || identity.TruthState != AgentSetupTruthState.Unknown
            || !IsCanonicalIdentity(identity.MessageId) || !IsCanonicalIdentity(identity.CorrelationId))
        {
            return Task.FromResult(ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.ValidationFailed));
        }
        return WritePreparedAsync(identity, ct => _client.ProviderCatalog.SetTenantEnablementAsync(command,
            new AgentOperationOptions(identity.CorrelationId, identity.MessageId), ct), cancellationToken);
    }

    /// <inheritdoc />
    public Task<ProviderCatalogWriteResult> DecideDataHandlingAsync(DecideProviderDataHandling command, ProviderCatalogCommandAcceptance identity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(identity);
        if (identity.ProviderId != command.ProviderId || identity.ModelId != command.ModelId
            || identity.TruthState != AgentSetupTruthState.Unknown
            || !IsCanonicalIdentity(identity.MessageId) || !IsCanonicalIdentity(identity.CorrelationId))
        {
            return Task.FromResult(ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.ValidationFailed));
        }
        return WritePreparedAsync(identity, ct => _client.ProviderCatalog.DecideDataHandlingAsync(command,
            new AgentOperationOptions(identity.CorrelationId, identity.MessageId), ct), cancellationToken);
    }

    private static async Task<ProviderCatalogWriteResult> WritePreparedAsync(
        ProviderCatalogCommandAcceptance identity,
        Func<CancellationToken, ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>>> write,
        CancellationToken cancellationToken)
    {
        ProviderCatalogWriteResult result = await WriteAsync(write, cancellationToken).ConfigureAwait(false);
        return result.Status != AgentSetupWriteStatus.Submitted
            || result.Acceptance is { TruthState: AgentSetupTruthState.Submitted or AgentSetupTruthState.AuthoritativePending or AgentSetupTruthState.ProjectionConfirmed } acceptance
                && acceptance.ProviderId == identity.ProviderId && acceptance.ModelId == identity.ModelId
                && acceptance.MessageId == identity.MessageId && acceptance.CorrelationId == identity.CorrelationId
            ? result : ProviderCatalogWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify);
    }

    private static bool IsCanonicalIdentity(string? value)
        => value is { Length: 26 } && NUlid.Ulid.TryParse(value, out NUlid.Ulid parsed)
            && string.Equals(value, parsed.ToString(), StringComparison.Ordinal);

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
            AgentOperationStatus.UnableToVerify => AgentSetupWriteStatus.UnableToVerify,
            AgentOperationStatus.Rejected => AgentSetupWriteStatus.Rejected,
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
