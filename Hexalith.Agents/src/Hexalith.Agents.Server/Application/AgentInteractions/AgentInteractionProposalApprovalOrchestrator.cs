using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Application orchestration for approving exactly one proposal version and posting it through Conversations.
/// </summary>
public sealed class AgentInteractionProposalApprovalOrchestrator
{
    private const string InteractionDomain = "agent-interaction";

    private static readonly string[] _reservedExtensionKeys =
    [
        "actor:agentsAdmin",
        "provider:selectionValidation",
        "approver:policyValidation",
        "party:linkValidation",
    ];

    private readonly IAgentPartyReader _partyReader;
    private readonly IAgentGeneratedVersionReader _versionReader;
    private readonly IConversationResponsePoster _poster;
    private readonly IApproverPolicyResolver _approverPolicyResolver;
    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentInteractionProposalApprovalOrchestrator"/> class.</summary>
    public AgentInteractionProposalApprovalOrchestrator(
        IAgentPartyReader partyReader,
        IAgentGeneratedVersionReader versionReader,
        IConversationResponsePoster poster,
        IApproverPolicyResolver approverPolicyResolver,
        IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(partyReader);
        ArgumentNullException.ThrowIfNull(versionReader);
        ArgumentNullException.ThrowIfNull(poster);
        ArgumentNullException.ThrowIfNull(approverPolicyResolver);
        ArgumentNullException.ThrowIfNull(dispatcher);
        _partyReader = partyReader;
        _versionReader = versionReader;
        _poster = poster;
        _approverPolicyResolver = approverPolicyResolver;
        _dispatcher = dispatcher;
    }

    /// <summary>Approves and posts the exact selected version, returning only safe ids/status.</summary>
    public async Task<AgentInteractionProposalApprovalOutcomeResult> ExecuteAsync(
        AgentInteractionProposalApprovalRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!IsApprovableOrRetryable(request.ProposalState))
        {
            return new AgentInteractionProposalApprovalOutcomeResult(
                request.SelectedVersionId,
                string.Empty,
                AgentInteractionStatus.Unknown,
                AgentProposedReplyNotApprovableReason.ProposalNotPending);
        }

        ApproverPolicyValidationStatus verdict = await ResolveAuthorizationAsync(request, ct).ConfigureAwait(false);
        if (verdict != ApproverPolicyValidationStatus.Valid)
        {
            AgentProposalApprovalResult denied = BuildResult(
                request,
                verdict,
                verdict == ApproverPolicyValidationStatus.Unknown
                    ? AgentProposalApprovalOutcome.PolicyFailure
                    : AgentProposalApprovalOutcome.NotAuthorized,
                agentPartyId: string.Empty,
                messageId: string.Empty,
                idempotencyKey: string.Empty,
                postedMessageId: string.Empty);
            await DispatchAsync(request, denied, ct).ConfigureAwait(false);
            return new AgentInteractionProposalApprovalOutcomeResult(
                request.SelectedVersionId,
                string.Empty,
                AgentProposalApprovalPolicy.Decide(denied));
        }

        AgentProposalApprovalResult result = await ApproveAndPostAsync(request, verdict, ct).ConfigureAwait(false);
        await DispatchAsync(request, result, ct).ConfigureAwait(false);
        return new AgentInteractionProposalApprovalOutcomeResult(
            request.SelectedVersionId,
            result.MessageId,
            AgentProposalApprovalPolicy.Decide(result));
    }

    private static bool IsApprovableOrRetryable(ProposedAgentReplyState state)
        => state is ProposedAgentReplyState.Pending
            or ProposedAgentReplyState.Edited
            or ProposedAgentReplyState.Regenerated
            or ProposedAgentReplyState.Approved
            or ProposedAgentReplyState.PostingPending
            or ProposedAgentReplyState.PostingFailed;

    private async Task<ApproverPolicyValidationStatus> ResolveAuthorizationAsync(
        AgentInteractionProposalApprovalRequest request,
        CancellationToken ct)
    {
        if (request.ApproverPolicy is not { Sources.Count: > 0 } policy)
        {
            return ApproverPolicyValidationStatus.Incomplete;
        }

        try
        {
            ApproverPolicyResolutionResult resolution = await _approverPolicyResolver
                .ResolveAsync(request.TenantId, policy, ct)
                .ConfigureAwait(false);
            return ApproverPolicyVerdict.Evaluate(policy, resolution);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApproverPolicyValidationStatus.Unavailable;
        }
    }

    private async Task<AgentProposalApprovalResult> ApproveAndPostAsync(
        AgentInteractionProposalApprovalRequest request,
        ApproverPolicyValidationStatus verdict,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SelectedVersionId))
        {
            return BuildResult(request, verdict, AgentProposalApprovalOutcome.SelectedVersionMissing, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        AgentGeneratedVersionReadResult version = await ReadVersionAsync(request, ct).ConfigureAwait(false);
        if (version.Outcome != AgentGeneratedVersionReadOutcome.Available || version.GeneratedContent is null)
        {
            return BuildResult(request, verdict, AgentProposalApprovalOutcome.SelectedVersionMissing, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        AgentPartyReadResult party = await ReadPartyAsync(request, ct).ConfigureAwait(false);
        if (party.Outcome != AgentPartyReadOutcome.Available || string.IsNullOrWhiteSpace(party.PartyId))
        {
            return BuildResult(request, verdict, AgentProposalApprovalOutcome.PartyIdentityUnavailable, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        string messageId = AgentResponsePostingIdentity.DeriveMessageId(request.AgentInteractionId, request.SelectedVersionId);
        string idempotencyKey = AgentResponsePostingIdentity.DeriveIdempotencyKey(request.AgentInteractionId, request.SelectedVersionId);

        ConversationMembershipResult membership = await EnsureMembershipAsync(request, party.PartyId, ct).ConfigureAwait(false);
        if (membership.Outcome is not (ConversationMembershipOutcome.Present or ConversationMembershipOutcome.Established))
        {
            return BuildResult(request, verdict, MapMembershipFailure(membership.Outcome), party.PartyId, messageId, idempotencyKey, string.Empty);
        }

        ConversationAppendResult append = await AppendAsync(request, party.PartyId, version.GeneratedContent, messageId, idempotencyKey, ct).ConfigureAwait(false);
        AgentProposalApprovalOutcome outcome = MapAppendOutcome(append.Outcome);
        string postedMessageId = outcome == AgentProposalApprovalOutcome.Posted ? messageId : string.Empty;
        return BuildResult(request, verdict, outcome, party.PartyId, messageId, idempotencyKey, postedMessageId);
    }

    private async Task<AgentGeneratedVersionReadResult> ReadVersionAsync(
        AgentInteractionProposalApprovalRequest request,
        CancellationToken ct)
    {
        try
        {
            return await _versionReader
                .ReadVersionAsync(request.TenantId, request.AgentInteractionId, request.SelectedVersionId, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AgentGeneratedVersionReadResult.NotAvailable;
        }
    }

    private async Task<AgentPartyReadResult> ReadPartyAsync(AgentInteractionProposalApprovalRequest request, CancellationToken ct)
    {
        try
        {
            return await _partyReader.ReadAsync(request.TenantId, request.AgentId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AgentPartyReadResult.Unavailable;
        }
    }

    private async Task<ConversationMembershipResult> EnsureMembershipAsync(
        AgentInteractionProposalApprovalRequest request,
        string agentPartyId,
        CancellationToken ct)
    {
        try
        {
            var membershipRequest = new ConversationMembershipRequest(
                request.TenantId,
                request.SourceConversationId,
                agentPartyId,
                request.ActorUserId,
                request.CorrelationId);
            return await _poster.EnsureAiAgentParticipantAsync(membershipRequest, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ConversationMembershipResult.ConversationUnavailable;
        }
    }

    private async Task<ConversationAppendResult> AppendAsync(
        AgentInteractionProposalApprovalRequest request,
        string agentPartyId,
        string content,
        string messageId,
        string idempotencyKey,
        CancellationToken ct)
    {
        try
        {
            var appendRequest = new ConversationAppendRequest(
                request.TenantId,
                request.SourceConversationId,
                agentPartyId,
                content,
                messageId,
                idempotencyKey,
                request.CorrelationId);
            return await _poster.AppendAgentMessageAsync(appendRequest, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ConversationAppendResult.AdapterFailure;
        }
    }

    private async Task DispatchAsync(
        AgentInteractionProposalApprovalRequest request,
        AgentProposalApprovalResult result,
        CancellationToken ct)
    {
        var command = new ApproveProposedAgentReply(request.AgentInteractionId, result);
        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            InteractionDomain,
            request.AgentInteractionId,
            nameof(ApproveProposedAgentReply),
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));
        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
    }

    private static AgentProposalApprovalResult BuildResult(
        AgentInteractionProposalApprovalRequest request,
        ApproverPolicyValidationStatus verdict,
        AgentProposalApprovalOutcome outcome,
        string agentPartyId,
        string messageId,
        string idempotencyKey,
        string postedMessageId)
        => new(
            outcome,
            request.ProposalId,
            request.SourceConversationId,
            request.SelectedVersionId,
            request.ApproverPartyId,
            request.ApproverPolicyVersion,
            verdict,
            request.ApproverPolicy?.DisclosureCategory ?? ApproverPolicyBasisDisclosure.Omitted,
            agentPartyId,
            messageId,
            idempotencyKey,
            postedMessageId);

    private static AgentProposalApprovalOutcome MapMembershipFailure(ConversationMembershipOutcome outcome) => outcome switch
    {
        ConversationMembershipOutcome.MembershipRejected => AgentProposalApprovalOutcome.MembershipRejected,
        ConversationMembershipOutcome.ConversationUnavailable => AgentProposalApprovalOutcome.ConversationUnavailable,
        ConversationMembershipOutcome.SeamUnavailable => AgentProposalApprovalOutcome.MembershipUnavailable,
        _ => AgentProposalApprovalOutcome.MembershipUnavailable,
    };

    private static AgentProposalApprovalOutcome MapAppendOutcome(ConversationAppendOutcome outcome) => outcome switch
    {
        ConversationAppendOutcome.Posted => AgentProposalApprovalOutcome.Posted,
        ConversationAppendOutcome.PostRejected => AgentProposalApprovalOutcome.PostRejected,
        ConversationAppendOutcome.ConversationUnavailable => AgentProposalApprovalOutcome.ConversationUnavailable,
        _ => AgentProposalApprovalOutcome.AdapterFailure,
    };

    private static Dictionary<string, string>? BuildTrustedExtensions(IReadOnlyDictionary<string, string>? clientSupplied)
    {
        if (clientSupplied is null)
        {
            return null;
        }

        var extensions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string key, string value) in clientSupplied)
        {
            if (Array.IndexOf(_reservedExtensionKeys, key) < 0)
            {
                extensions[key] = value;
            }
        }

        return extensions.Count > 0 ? extensions : null;
    }
}
