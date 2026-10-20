using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>Directory-first explicit intake; acceptance remains the workflow's step-10 decision.</summary>
public sealed class AutomaticInteractionIntakeOrchestrator(
    IAutomaticInteractionIntakeGate gate, IAgentDependencyAvailability availability, IInteractionDirectoryReadiness readiness,
    IConversationAgentStateReader directoryReader, IAgentCommandDispatcher dispatcher, InteractionOutboxRecoveryOrchestrator recovery)
{
    /// <summary>Runs pre-creation gates and writes one permit before recovering its exact durable start.</summary>
    public async Task<AutomaticInteractionIntakeResult> ExecuteAsync(AutomaticInteractionIntakeRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request); ct.ThrowIfCancellationRequested();
        if (request.TenantId != request.AuthenticatedTenantId || !request.IsPerson || string.IsNullOrWhiteSpace(request.ActorId)
            || string.IsNullOrWhiteSpace(request.CallerPartyId) || string.IsNullOrWhiteSpace(request.ClientIdempotencyKey)) { return new(AutomaticInteractionIntakeStatus.NotFound); }
        AutomaticInteractionGateResult gates = await gate.EvaluateAsync(request, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        if (gates.Dependency is not null) { return new(AutomaticInteractionIntakeStatus.DependencyNotAvailable, Dependency: gates.Dependency); }
        if (!gates.Authorized || gates.Snapshot is null) { return new(AutomaticInteractionIntakeStatus.NotFound); }
        if (availability.Check("EXT-PROTECTION-1") is { } protection) { return new(AutomaticInteractionIntakeStatus.PayloadProtectionUnavailable, Dependency: protection); }
        foreach (string record in new[] { "EXT-HOST-1", "EXT-SECRETS-1", "EXT-PARTIES-1", "EXT-CONV-AI-1", "EXT-TOPOLOGY-1", "OD-DAPR-SECURITY-1" })
        {
            if (availability.Check(record) is { } missing) { return new(AutomaticInteractionIntakeStatus.DependencyNotAvailable, Dependency: missing); }
        }
        var epoch = await readiness.ReadAsync(request.TenantId, ct).ConfigureAwait(false);
        if (epoch is null) { return new(AutomaticInteractionIntakeStatus.DirectoryNotReady); }
        var directory = await directoryReader.ReadAsync(request.TenantId, request.ConversationId, ct).ConfigureAwait(false);
        if (directory?.DeletionRequestId is not null) { return new(AutomaticInteractionIntakeStatus.ConversationClosing); }
        string id = ConversationAgentStateIdentity.Interaction(request.TenantId, request.AgentId, request.ConversationId, request.CallerPartyId, request.ClientIdempotencyKey);
        var permit = new InteractionPermit(request.TenantId, request.ConversationId, id, request.AgentId, request.CallerPartyId, request.ClientIdempotencyKey,
            ConversationAgentStateIdentity.Permit(request.TenantId, id), ConversationAgentStateIdentity.CreationOutbox(request.TenantId, id), request.RequestDigest, gates.Snapshot, epoch, request.Request, request.DigestKeyVersion);
        await InteractionCommandDispatch.SendAsync(dispatcher, request.TenantId, ConversationAgentStateIdentity.Domain,
            ConversationAgentStateIdentity.Directory(request.TenantId, request.ConversationId), permit.PermitId,
            new RegisterInteractionPermit(permit, directory?.Revision ?? 0), ct).ConfigureAwait(false);
        return await recovery.RecoverAsync(request.TenantId, request.ConversationId, id, ct, expectedPermit: permit).ConfigureAwait(false);
    }
}
