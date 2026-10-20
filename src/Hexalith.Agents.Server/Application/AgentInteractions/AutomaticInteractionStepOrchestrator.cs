using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>One read/authorize/effect/result checkpoint per activity; replay returns recorded EventStore truth.</summary>
public sealed class AutomaticInteractionStepOrchestrator(IConversationAgentStateReader directoryReader, IAgentInteractionStateReader interactionReader,
    IAgentCommandDispatcher dispatcher, IAutomaticInteractionStepEffects effects)
{
    private static readonly AutomaticInteractionStep[] _sequence =
    [
        AutomaticInteractionStep.WorkflowStartCheckpoint, AutomaticInteractionStep.RateAdmissionAuthorized, AutomaticInteractionStep.OpenInteractionLeaseAuthorized,
        AutomaticInteractionStep.RateAdmissionRecorded, AutomaticInteractionStep.OpenInteractionLeaseRecorded, AutomaticInteractionStep.ApproverResolutionRecorded,
        AutomaticInteractionStep.ContextReadRecorded, AutomaticInteractionStep.BudgetReservationAuthorized, AutomaticInteractionStep.BudgetReservationRecorded,
        AutomaticInteractionStep.PromptSafetyRecorded, AutomaticInteractionStep.ConversationMembershipRecorded, AutomaticInteractionStep.AgentCallAccepted,
        AutomaticInteractionStep.CapacityAdmissionAuthorized, AutomaticInteractionStep.CapacityAdmissionRecorded, AutomaticInteractionStep.ProviderInvocationAuthorized,
        AutomaticInteractionStep.GeneratedVersionRecorded, AutomaticInteractionStep.ConversationPostingRecorded,
    ];

    /// <summary>Re-reads exact state, performs only committed effects, and dispatches at most one trusted command.</summary>
    public async Task<WorkflowExecutionState> ExecuteAsync(WorkflowExecutionState input, CancellationToken ct)
    {
        WorkflowExecutionStateValidation.Validate(input); ct.ThrowIfCancellationRequested();
        var interaction = await interactionReader.ReadAsync(input.TenantId, input.AgentInteractionId, ct).ConfigureAwait(false);
        var directory = await directoryReader.ReadAsync(input.TenantId, input.ConversationId, ct).ConfigureAwait(false);
        if (interaction?.PermittedCreation is not { } creation || directory is null || directory.TenantId != input.TenantId || directory.ConversationId != input.ConversationId
            || !directory.Permits.TryGetValue(input.AgentInteractionId, out var permit) || permit.Permit.RequestDigest != input.RequestReference.ContentDigest
            || creation.Permit.TenantId != input.TenantId || creation.Permit.ConversationId != input.ConversationId || creation.Permit.AgentInteractionId != input.AgentInteractionId
            || input.RequestReference.EventRevision != creation.Revision) { throw new InvalidOperationException("InvalidAuthoritativeInteraction"); }
        string directoryId = ConversationAgentStateIdentity.Directory(input.TenantId, input.ConversationId);
        if (interaction.AutomaticFacts.TryGetValue(AutomaticInteractionStep.CapacityAdmissionRecorded, out var capacity)
            && capacity.Outcome != AutomaticInteractionOutcome.Allowed && !interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.BudgetDispositionDecided))
        {
            var noInvocation = await effects.ExecuteAsync(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, interaction, null, ct).ConfigureAwait(false);
            if (noInvocation.Outcome != AutomaticInteractionOutcome.CancelNoInvocation) { throw new InvalidOperationException("AuthoritativeNoInvocationEvidenceRequired"); }
            await RecordAsync(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, noInvocation, null).ConfigureAwait(false);
            return await RefreshAsync().ConfigureAwait(false);
        }
        bool terminal = IsTerminal(interaction);
        foreach (ConversationEffectLease lease in directory.Leases.Values.Where(l => l.AgentInteractionId == input.AgentInteractionId).OrderBy(l => l.LeaseId, StringComparer.Ordinal))
        {
            if (lease.Status == ConversationEffectLeaseStatus.Reserved && (terminal || directory.DeletionRequestId is not null))
            {
                await SendDirectoryAsync(new CancelConversationEffectLease(lease.LeaseId, input.AgentInteractionId, input.AgentInteractionId, directory.Revision), lease.LeaseId).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
            AutomaticInteractionFact? result = LeaseResult(interaction, lease);
            if (lease.Status == ConversationEffectLeaseStatus.CommittedToEffect && result is not null)
            {
                if (lease.Kind == ConversationEffectKind.ProviderInvocation || terminal) { await effects.SettleAsync(interaction, ct).ConfigureAwait(false); }
                await SendDirectoryAsync(new SettleConversationEffectLease(lease.LeaseId, input.AgentInteractionId, input.AgentInteractionId, lease.CommitRevision, result.Revision, directory.Revision), lease.LeaseId).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
        }
        if (terminal)
        {
            await effects.SettleAsync(interaction, ct).ConfigureAwait(false);
            WorkflowExecutionStatus status = (interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.StartSuppressedByDeletion) || interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion)) ? WorkflowExecutionStatus.Suppressed
                : interaction.Status == AgentInteractionStatus.Posted || interaction.Snapshot?.ResponseMode == AgentResponseMode.Confirmation && interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded)
                    ? WorkflowExecutionStatus.Completed : WorkflowExecutionStatus.Failed;
            return input with { EventRevision = interaction.AutomaticRevision, Status = status, Outcome = AutomaticInteractionPolicy.Decide(interaction.AutomaticFacts.Values.MaxBy(f => f.Revision)!) };
        }
        if (interaction.AutomaticFacts.Values.FirstOrDefault(f => f.Outcome is AutomaticInteractionOutcome.Blocked or AutomaticInteractionOutcome.DependencyNotAvailable or AutomaticInteractionOutcome.Failed) is { } failed)
        {
            await RecordAsync(AutomaticInteractionStep.InteractionPreAcceptanceFailed, new(AutomaticInteractionOutcome.Blocked, failed.DependencyRecordId), null).ConfigureAwait(false);
            return await RefreshAsync().ConfigureAwait(false);
        }
        AutomaticInteractionStep next = _sequence.FirstOrDefault(s => !interaction.AutomaticFacts.ContainsKey(s)
            && !(s == AutomaticInteractionStep.ApproverResolutionRecorded && interaction.Snapshot?.ResponseMode == AgentResponseMode.Automatic));
        if (directory.DeletionRequestId is not null && next != AutomaticInteractionStep.WorkflowStartCheckpoint)
        {
            bool accepted = interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.AgentCallAccepted);
            bool authorized = interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationAuthorized);
            ConversationEffectLease? winningLease = directory.Leases.Values.FirstOrDefault(l => l.AgentInteractionId == input.AgentInteractionId
                && l.Kind == AgentInteractionAggregate.LeaseKind(next) && l.OperationFamily == AgentInteractionAggregate.LeaseOperationStep(next).ToString()
                && l.Status == ConversationEffectLeaseStatus.CommittedToEffect);
            if (next == AutomaticInteractionStep.ConversationPostingRecorded && winningLease is null)
            {
                var cancelledPosting = directory.Leases.Values.FirstOrDefault(l => l.AgentInteractionId == input.AgentInteractionId
                    && l.Kind == ConversationEffectKind.ConversationPosting && l.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit);
                await RecordAsync(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion, new(AutomaticInteractionOutcome.Suppressed), cancelledPosting).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
            if (!accepted && winningLease is null)
            {
                await RecordAsync(AutomaticInteractionStep.InteractionPreAcceptanceFailed, new(AutomaticInteractionOutcome.Suppressed), null).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
            if (accepted && !authorized)
            {
                AutomaticInteractionEffectResult cancellation = await effects.ExecuteAsync(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, interaction, null, ct).ConfigureAwait(false);
                if (cancellation.Outcome != AutomaticInteractionOutcome.CancelNoInvocation) { throw new NotSupportedException("AuthoritativeNoInvocationEvidenceRequired"); }
                if (winningLease?.Kind == ConversationEffectKind.ProviderInvocation)
                { await RecordAsync(AutomaticInteractionStep.ProviderInvocationNotAuthorized, new(AutomaticInteractionOutcome.Blocked), winningLease).ConfigureAwait(false); }
                else { await RecordAsync(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, cancellation, null).ConfigureAwait(false); }
                return await RefreshAsync().ConfigureAwait(false);
            }
            if (next == AutomaticInteractionStep.GeneratedVersionRecorded && winningLease is null)
            {
                var provider = directory.Leases.Values.Single(l => l.AgentInteractionId == input.AgentInteractionId && l.Kind == ConversationEffectKind.ProviderInvocation && l.CommitRevision > 0);
                await RecordAsync(AutomaticInteractionStep.GenerationFailed, new(AutomaticInteractionOutcome.Discarded), provider).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
        }
        AutomaticInteractionEffectResult? generation = null;
        if (next == AutomaticInteractionStep.GeneratedVersionRecorded)
        {
            var provider = directory.Leases.Values.Single(l => l.AgentInteractionId == input.AgentInteractionId && l.Kind == ConversationEffectKind.ProviderInvocation && l.Status == ConversationEffectLeaseStatus.CommittedToEffect);
            generation = await effects.ExecuteAsync(next, interaction, provider, ct).ConfigureAwait(false);
            if (generation.Outcome != AutomaticInteractionOutcome.Allowed || generation.Output is null)
            {
                if (generation.FailureKind == AutomaticInteractionFailureKind.Unknown) { throw new InvalidOperationException("GenerationFailureClassificationRequired"); }
                string mutationId = ConversationAgentStateIdentity.Lease(input.TenantId, input.ConversationId, input.AgentInteractionId, ConversationEffectKind.ProposalMutation, next.ToString(), Operation(next));
                directory.Leases.TryGetValue(mutationId, out var mutation);
                await RecordAsync(AutomaticInteractionStep.GenerationFailed, new(AutomaticInteractionOutcome.Failed, generation.DependencyRecordId, FailureKind: generation.FailureKind), mutation?.Status == ConversationEffectLeaseStatus.CommittedToEffect ? mutation : provider).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
        }
        AutomaticInteractionStep leaseStep = AgentInteractionAggregate.LeaseOperationStep(next);
        ConversationEffectKind kind = AgentInteractionAggregate.LeaseKind(next);
        ConversationEffectLease? phaseLease = null;
        if (kind != ConversationEffectKind.Unknown)
        {
            string leaseOperationId = Operation(leaseStep);
            string leaseId = ConversationAgentStateIdentity.Lease(input.TenantId, input.ConversationId, input.AgentInteractionId, kind, leaseStep.ToString(), leaseOperationId);
            directory.Leases.TryGetValue(leaseId, out phaseLease);
            if (phaseLease?.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit || directory.DeletionRequestId is not null && phaseLease?.CommitRevision is not > 0)
            {
                if (next == AutomaticInteractionStep.ConversationPostingRecorded)
                {
                    if (directory.DeletionRequestId is null) { throw new InvalidOperationException("PostingCancellationRequiresClosingEvidence"); }
                    await RecordAsync(AutomaticInteractionStep.ConversationPostingSuppressedByDeletion, new(AutomaticInteractionOutcome.Suppressed), phaseLease).ConfigureAwait(false);
                }
                else
                {
                    await RecordAsync(next == AutomaticInteractionStep.WorkflowStartCheckpoint ? AutomaticInteractionStep.StartSuppressedByDeletion : AutomaticInteractionStep.InteractionPreAcceptanceFailed,
                        new(AutomaticInteractionOutcome.Suppressed), null).ConfigureAwait(false);
                }
                return await RefreshAsync().ConfigureAwait(false);
            }
            if (phaseLease is null)
            {
                phaseLease = new(leaseId, input.AgentInteractionId, kind, leaseStep.ToString(), leaseOperationId, input.AgentInteractionId,
                    permit.Permit.DirectoryEpoch.EpochId, ConversationEffectLeaseStatus.Reserved, 0, 0, false);
                await SendDirectoryAsync(new AcquireConversationEffectLease(phaseLease, directory.Revision), leaseId).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
            if (phaseLease.Status == ConversationEffectLeaseStatus.Reserved)
            {
                await SendDirectoryAsync(new CommitConversationEffect(leaseId, input.AgentInteractionId, input.AgentInteractionId, directory.Revision), leaseId).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
            if (phaseLease.Status != ConversationEffectLeaseStatus.CommittedToEffect) { throw new InvalidOperationException("EffectLeaseNotCommitted"); }
        }
        AutomaticInteractionEffectResult outcome;
        if (next is AutomaticInteractionStep.WorkflowStartCheckpoint or AutomaticInteractionStep.AgentCallAccepted)
        { outcome = new(AutomaticInteractionOutcome.Allowed); }
        else if (next is AutomaticInteractionStep.OpenInteractionLeaseAuthorized or AutomaticInteractionStep.BudgetReservationAuthorized or AutomaticInteractionStep.CapacityAdmissionAuthorized)
        { outcome = new(AutomaticInteractionOutcome.Authorized); }
        else { outcome = generation ?? await effects.ExecuteAsync(next, interaction, phaseLease, ct).ConfigureAwait(false); }
        if (next == AutomaticInteractionStep.ProviderInvocationAuthorized && (outcome.Outcome != AutomaticInteractionOutcome.Authorized || outcome.Authority?.IsEffective() != true))
        { next = AutomaticInteractionStep.ProviderInvocationNotAuthorized; outcome = new(AutomaticInteractionOutcome.Blocked, outcome.DependencyRecordId); }
        else if (next == AutomaticInteractionStep.RateAdmissionAuthorized && outcome.Outcome != AutomaticInteractionOutcome.Authorized)
        { next = AutomaticInteractionStep.InteractionPreAcceptanceFailed; outcome = new(AutomaticInteractionOutcome.Blocked, outcome.DependencyRecordId); }
        await RecordAsync(next, outcome, phaseLease).ConfigureAwait(false);
        return await RefreshAsync().ConfigureAwait(false);

        string Operation(AutomaticInteractionStep step) => AgentsSecurityIdentity.Derive("interaction-step", input.TenantId, input.AgentInteractionId, step.ToString());
        Task SendDirectoryAsync(object command, string operationId) => InteractionCommandDispatch.SendAsync(dispatcher, input.TenantId, ConversationAgentStateIdentity.Domain, directoryId, operationId, command, ct);
        async Task RecordAsync(AutomaticInteractionStep step, AutomaticInteractionEffectResult outcome, ConversationEffectLease? lease)
        {
            var fact = new AutomaticInteractionFact(step, Operation(step), outcome.Outcome, interaction.AutomaticRevision + 1,
                lease?.LeaseId, lease?.CommitRevision ?? 0, outcome.DependencyRecordId, outcome.Authority, outcome.Output, outcome.ContentDigest, outcome.DigestKeyVersion, outcome.FailureKind,
                DeletionRequestId: step == AutomaticInteractionStep.ConversationPostingSuppressedByDeletion ? directory.DeletionRequestId : null,
                DirectoryClosingRevision: step == AutomaticInteractionStep.ConversationPostingSuppressedByDeletion ? directory.DeletionClosingRevision : 0);
            await InteractionCommandDispatch.SendAsync(dispatcher, input.TenantId, "agent-interaction", input.AgentInteractionId, fact.OperationId,
                new RecordAutomaticInteractionStep(input.TenantId, input.AgentInteractionId, fact, interaction.AutomaticRevision), ct).ConfigureAwait(false);
        }
        async Task<WorkflowExecutionState> RefreshAsync()
        {
            var observed = await interactionReader.ReadAsync(input.TenantId, input.AgentInteractionId, ct).ConfigureAwait(false);
            return input with { EventRevision = observed?.AutomaticRevision ?? input.EventRevision, Status = WorkflowExecutionStatus.Running,
                Outcome = observed?.AutomaticFacts.Count > 0 ? AutomaticInteractionPolicy.Decide(observed.AutomaticFacts.Values.MaxBy(f => f.Revision)!) : AutomaticInteractionOutcome.Unknown };
        }
    }
    private static bool IsTerminal(AgentInteractionState interaction) => interaction.AutomaticFacts.Keys.Any(step => step is AutomaticInteractionStep.StartSuppressedByDeletion
        or AutomaticInteractionStep.InteractionPreAcceptanceFailed or AutomaticInteractionStep.ProviderInvocationNotAuthorized or AutomaticInteractionStep.GenerationFailed or AutomaticInteractionStep.ConversationPostingRecorded
        or AutomaticInteractionStep.PreProviderCapacityDispositionDecided or AutomaticInteractionStep.ConversationPostingSuppressedByDeletion) || interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.AgentCallAccepted) && interaction.AutomaticFacts.Values.Any(f => f.Outcome is AutomaticInteractionOutcome.Blocked or AutomaticInteractionOutcome.DependencyNotAvailable or AutomaticInteractionOutcome.Failed) || interaction.Snapshot?.ResponseMode == AgentResponseMode.Confirmation && interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.GeneratedVersionRecorded);
    private static AutomaticInteractionFact? LeaseResult(AgentInteractionState interaction, ConversationEffectLease lease)
    {
        IEnumerable<AutomaticInteractionFact> facts = interaction.AutomaticFacts.Values.Where(f => f.LeaseId == lease.LeaseId);
        if (lease.Kind == ConversationEffectKind.ProviderInvocation)
        { return interaction.AutomaticFacts.TryGetValue(AutomaticInteractionStep.GenerationFailed, out var failure) ? failure
            : facts.FirstOrDefault(f => f.Step == AutomaticInteractionStep.ProviderInvocationNotAuthorized)
                ?? (interaction.AutomaticFacts.TryGetValue(AutomaticInteractionStep.GeneratedVersionRecorded, out var generated) ? generated : null); }
        return facts.FirstOrDefault();
    }
}
