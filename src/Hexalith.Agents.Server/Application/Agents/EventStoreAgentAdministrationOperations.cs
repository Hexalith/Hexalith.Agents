using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Live public Agent administration surface over the EventStore command path and the projected setup read model
/// (Story 5.2 AC1, AC2, AC4). Writes go through the same authorize-then-dispatch orchestrations the module already
/// uses; reads answer from the single authoritative setup read model, so the API, the public client, and
/// FrontComposer always see identical truth.
/// </summary>
/// <remarks>
/// <para>
/// A write answers with a structured accepted identity — the Agent, the command message, the correlation, and the
/// <see cref="AgentSetupTruthState.Submitted"/> stage. Acceptance is deliberately not success: no stream, revision,
/// aggregate type, workflow, projection address, or Provider SDK detail crosses the boundary, and nothing in the
/// answer says the change is durable. The caller re-reads the setup with the version it is waiting for to learn
/// whether the projection has confirmed it.
/// </para>
/// <para>
/// Authorization runs first on every operation. An unauthorized or cross-tenant caller gets the same shaped
/// not-authorized result before any dispatch or read-model lookup, so it can learn nothing about whether the
/// target Agent exists (AC4).
/// </para>
/// </remarks>
public sealed class EventStoreAgentAdministrationOperations(
    IAgentAdministrationContextProvider contextProvider,
    AgentAdministrationOrchestrator administration,
    AgentResponseModeOrchestrator responseMode,
    AgentActivationProviderRevalidation activation,
    IReadModelStore readModelStore,
    IOptions<AgentSetupReadModelOptions> options,
    IAgentCommandIdentityFactory identityFactory) : IAgentAdministrationOperations
{
    private readonly IAgentAdministrationContextProvider _contextProvider = contextProvider
        ?? throw new ArgumentNullException(nameof(contextProvider));

    private readonly AgentAdministrationOrchestrator _administration = administration
        ?? throw new ArgumentNullException(nameof(administration));

    private readonly AgentResponseModeOrchestrator _responseMode = responseMode
        ?? throw new ArgumentNullException(nameof(responseMode));

    private readonly AgentActivationProviderRevalidation _activation = activation
        ?? throw new ArgumentNullException(nameof(activation));

    private readonly IReadModelStore _readModelStore = readModelStore
        ?? throw new ArgumentNullException(nameof(readModelStore));

    private readonly IOptions<AgentSetupReadModelOptions> _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly IAgentCommandIdentityFactory _identityFactory = identityFactory
        ?? throw new ArgumentNullException(nameof(identityFactory));

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentSetupResult>> GetStatusAsync(
        string agentId,
        int? expectedConfigurationVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => ReadSetupAsync(agentId, expectedConfigurationVersion, options, cancellationToken);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentSetupResult>> GetConfigurationAsync(
        string agentId,
        int? expectedConfigurationVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => ReadSetupAsync(agentId, expectedConfigurationVersion, options, cancellationToken);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentCommandAcceptance>> CreateAsync(
        string agentId,
        CreateAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            agentId,
            options,
            (request, ct) => _administration.CreateAsync(request, command, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentCommandAcceptance>> UpdateConfigurationAsync(
        string agentId,
        UpdateAgentConfiguration command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            agentId,
            options,
            (request, ct) => _administration.UpdateConfigurationAsync(request, command, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentCommandAcceptance>> ConfigureResponseModeAsync(
        string agentId,
        ConfigureAgentResponseMode command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            agentId,
            options,
            async (request, ct) =>
            {
                AgentResponseModeOutcome outcome = await _responseMode
                    .ExecuteAsync(
                        new AgentResponseModeRequest(
                            request.MessageId,
                            request.CorrelationId,
                            request.TenantId,
                            request.AgentId,
                            request.ActorUserId,
                            request.IsAgentsAdmin,
                            command.Mode),
                        ct)
                    .ConfigureAwait(false);
                return new AgentAdministrationOutcome(outcome.Authorized, outcome.Dispatched);
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentCommandAcceptance>> ActivateAsync(
        string agentId,
        ActivateAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(agentId, options, ActivateCoreAsync, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentCommandAcceptance>> DisableAsync(
        string agentId,
        DisableAgent command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            agentId,
            options,
            (request, ct) => _administration.DisableAsync(request, command, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult> LinkPartyIdentityAsync(LinkAgentPartyIdentity command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => OutOfScope(command, options);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult> ReplacePartyIdentityAsync(ReplaceAgentPartyIdentity command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => OutOfScope(command, options);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult> SelectProviderModelAsync(SelectAgentProviderModel command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => OutOfScope(command, options);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult> ConfigureApproverPolicyAsync(ConfigureAgentApproverPolicy command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => OutOfScope(command, options);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult> ConfigureContentSafetyPolicyAsync(ConfigureAgentContentSafetyPolicy command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => OutOfScope(command, options);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult> RecordLaunchReadinessAsync(RecordAgentLaunchReadiness command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => OutOfScope(command, options);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult> EnableProductionLikeGenerationAsync(EnableProductionLikeGeneration command, AgentOperationOptions? options = null, CancellationToken cancellationToken = default)
        => OutOfScope(command, options);

    // The remaining administration commands belong to later stories. They stay fail-closed rather than reusing this
    // story's dispatch path, because each needs a dependency verdict this surface must not fabricate.
    private static ValueTask<AgentOperationResult> OutOfScope(object command, AgentOperationOptions? options)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new ValueTask<AgentOperationResult>(
            AgentOperationResult.Unavailable(options?.CorrelationId));
    }

    private async ValueTask<AgentOperationResult<AgentSetupResult>> ReadSetupAsync(
        string agentId,
        int? expectedConfigurationVersion,
        AgentOperationOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        AgentAdministrationContext context = _contextProvider.GetContext();
        string? correlationId = options?.CorrelationId;
        if (!context.IsAuthorized)
        {
            // Fail closed before addressing the read model: a denied caller learns nothing about existence (AC4).
            return AgentOperationResult<AgentSetupResult>.Succeeded(AgentSetupResult.NotAuthorized(), correlationId: correlationId);
        }

        ReadModelEntry<AgentSetupReadModel> entry;
        try
        {
            entry = await _readModelStore
                .GetAsync<AgentSetupReadModel>(
                    _options.Value.StateStoreName,
                    AgentSetupReadModelAddresses.Detail(context.TenantId, agentId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<AgentSetupResult>.Failed(
                AgentCommandDispatchFailure.Map(exception),
                correlationId);
        }

        return AgentOperationResult<AgentSetupResult>.Succeeded(
            AgentSetupViewFactory.Create(
                entry.Value,
                context.TenantId,
                agentId,
                expectedConfigurationVersion,
                isAgentsAdmin: true),
            correlationId: correlationId);
    }

    private async ValueTask<AgentOperationResult<AgentCommandAcceptance>> WriteAsync(
        string agentId,
        AgentOperationOptions? options,
        Func<AgentAdministrationRequest, CancellationToken, Task<AgentAdministrationOutcome>> dispatch,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        AgentAdministrationContext context = _contextProvider.GetContext();
        string correlationId = options?.CorrelationId is { Length: > 0 } supplied
            ? supplied
            : _identityFactory.NewCorrelationId();

        if (!context.IsAuthorized)
        {
            return AgentOperationResult<AgentCommandAcceptance>.Failed(
                AgentOperationErrorCode.NotAuthorized,
                correlationId);
        }

        // The idempotency key doubles as the command message id, so re-submitting the same caller key is an exact
        // duplicate at the gateway rather than a second appended event.
        string messageId = options?.IdempotencyKey is { Length: > 0 } key ? key : _identityFactory.NewMessageId();

        AgentAdministrationOutcome outcome;
        try
        {
            outcome = await dispatch(
                    new AgentAdministrationRequest(
                        messageId,
                        correlationId,
                        context.TenantId,
                        agentId,
                        context.ActorUserId,
                        context.IsAgentsAdmin),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<AgentCommandAcceptance>.Failed(
                AgentCommandDispatchFailure.Map(exception),
                correlationId);
        }

        return outcome switch
        {
            { Authorized: false } => AgentOperationResult<AgentCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId),
            { Dispatched: false } => AgentOperationResult<AgentCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable, correlationId),
            _ => AgentOperationResult<AgentCommandAcceptance>.Succeeded(
                new AgentCommandAcceptance(agentId, messageId, correlationId, AgentSetupTruthState.Submitted),
                correlationId: correlationId),
        };
    }

    // Activation re-validates the recorded provider selection and (in Confirmation mode) the recorded approver
    // policy before the aggregate's gates can clear. Feeding it the projected setup keeps a single source of truth
    // for what "currently recorded" means; when the projection has no record yet, the inputs stay empty and the
    // aggregate fails closed on its own gates rather than this surface guessing.
    private async Task<AgentAdministrationOutcome> ActivateCoreAsync(
        AgentAdministrationRequest request,
        CancellationToken cancellationToken)
    {
        ReadModelEntry<AgentSetupReadModel> entry = await _readModelStore
            .GetAsync<AgentSetupReadModel>(
                _options.Value.StateStoreName,
                AgentSetupReadModelAddresses.Detail(request.TenantId, request.AgentId),
                cancellationToken)
            .ConfigureAwait(false);

        AgentSetupReadModel? model = entry.Value;
        AgentActivationRevalidationOutcome outcome = await _activation
            .ExecuteAsync(
                new AgentActivationRevalidationRequest(
                    request.MessageId,
                    request.CorrelationId,
                    request.TenantId,
                    request.AgentId,
                    request.ActorUserId,
                    request.IsAgentsAdmin,
                    model?.ProviderId,
                    model?.ModelId,
                    model?.ResponseMode ?? AgentResponseMode.Unknown,
                    model is { ApproverPolicySources.Count: > 0 }
                        ? new AgentApproverPolicy(model.ApproverPolicySources, model.ApproverPolicyDisclosure)
                        : null),
                cancellationToken)
            .ConfigureAwait(false);

        return new AgentAdministrationOutcome(outcome.Authorized, outcome.Dispatched);
    }
}
