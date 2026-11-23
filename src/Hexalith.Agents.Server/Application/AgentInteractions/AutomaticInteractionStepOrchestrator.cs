using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.Activities;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>Owner checkpoints with one generation activity; replay returns recorded EventStore truth.</summary>
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

    /// <summary>Re-reads exact state and observes one command, except generation which also acquires and commits its mutation lease.</summary>
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
        if (interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.ProviderInvocationNotAuthorized)
            && !interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.PreProviderCapacityDispositionDecided))
        {
            await RecordNoInvocationAsync().ConfigureAwait(false);
            return await RefreshAsync().ConfigureAwait(false);
        }
        if (interaction.AutomaticFacts.TryGetValue(AutomaticInteractionStep.CapacityAdmissionRecorded, out var capacity)
            && capacity.Outcome != AutomaticInteractionOutcome.Allowed && !interaction.AutomaticFacts.ContainsKey(AutomaticInteractionStep.BudgetDispositionDecided))
        {
            await RecordNoInvocationAsync().ConfigureAwait(false);
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
                if (lease.Kind == ConversationEffectKind.ProviderInvocation || terminal) { await SettleEffectsAsync().ConfigureAwait(false); }
                await SendDirectoryAsync(new SettleConversationEffectLease(lease.LeaseId, input.AgentInteractionId, input.AgentInteractionId, lease.CommitRevision, result.Revision, directory.Revision), lease.LeaseId).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
        }
        if (terminal)
        {
            await SettleEffectsAsync().ConfigureAwait(false);
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
                if (winningLease?.Kind == ConversationEffectKind.ProviderInvocation)
                {
                    await RecordAsync(AutomaticInteractionStep.ProviderInvocationNotAuthorized, new(AutomaticInteractionOutcome.Blocked), winningLease).ConfigureAwait(false);
                    return await RefreshAsync().ConfigureAwait(false);
                }
                await RecordNoInvocationAsync().ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
            if (next == AutomaticInteractionStep.GeneratedVersionRecorded && winningLease is null)
            {
                var provider = directory.Leases.Values.Single(l => l.AgentInteractionId == input.AgentInteractionId && l.Kind == ConversationEffectKind.ProviderInvocation && l.CommitRevision > 0);
                await RecordAsync(AutomaticInteractionStep.GenerationFailed, new(AutomaticInteractionOutcome.Discarded), provider).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
        }
        if (next == AutomaticInteractionStep.GeneratedVersionRecorded)
        {
            var provider = directory.Leases.Values.Single(l => l.AgentInteractionId == input.AgentInteractionId && l.Kind == ConversationEffectKind.ProviderInvocation && l.Status == ConversationEffectLeaseStatus.CommittedToEffect);
            AutomaticInteractionEffectResult generation = await effects.ExecuteAsync(next, interaction, provider, ct).ConfigureAwait(false);
            if (generation.Outcome != AutomaticInteractionOutcome.Allowed || generation.Output is null)
            {
                if (generation.FailureKind == AutomaticInteractionFailureKind.Unknown) { throw new InvalidOperationException("GenerationFailureClassificationRequired"); }
                string mutationId = ConversationAgentStateIdentity.Lease(input.TenantId, input.ConversationId, input.AgentInteractionId, ConversationEffectKind.ProposalMutation, next.ToString(), Operation(next));
                directory.Leases.TryGetValue(mutationId, out var mutation);
                await RecordAsync(AutomaticInteractionStep.GenerationFailed, new(AutomaticInteractionOutcome.Failed, generation.DependencyRecordId, FailureKind: generation.FailureKind), mutation?.Status == ConversationEffectLeaseStatus.CommittedToEffect ? mutation : provider).ConfigureAwait(false);
                return await RefreshAsync().ConfigureAwait(false);
            }
            // Keep the protected result local to this activity while observing the exact mutation capability.
            string operationId = Operation(next);
            string leaseId = ConversationAgentStateIdentity.Lease(input.TenantId, input.ConversationId, input.AgentInteractionId,
                ConversationEffectKind.ProposalMutation, next.ToString(), operationId);
            directory.Leases.TryGetValue(leaseId, out var mutationLease);
            if (mutationLease is null && directory.DeletionRequestId is null)
            {
                mutationLease = new(leaseId, input.AgentInteractionId, ConversationEffectKind.ProposalMutation, next.ToString(), operationId,
                    input.AgentInteractionId, permit.Permit.DirectoryEpoch.EpochId, ConversationEffectLeaseStatus.Reserved, 0, 0, false);
                await SendDirectoryAsync(new AcquireConversationEffectLease(mutationLease, directory.Revision), leaseId).ConfigureAwait(false);
                directory.Leases.TryGetValue(leaseId, out mutationLease);
            }
            if (mutationLease?.Status == ConversationEffectLeaseStatus.Reserved && directory.DeletionRequestId is null)
            {
                await SendDirectoryAsync(new CommitConversationEffect(leaseId, input.AgentInteractionId, input.AgentInteractionId, directory.Revision), leaseId).ConfigureAwait(false);
                directory.Leases.TryGetValue(leaseId, out mutationLease);
            }
            if (mutationLease?.Status == ConversationEffectLeaseStatus.CommittedToEffect)
            { await RecordAsync(next, generation, mutationLease).ConfigureAwait(false); }
            else if (directory.DeletionRequestId is not null && mutationLease?.CommitRevision is not > 0)
            { await RecordAsync(AutomaticInteractionStep.GenerationFailed, new(AutomaticInteractionOutcome.Discarded), provider).ConfigureAwait(false); }
            else { throw new AutomaticInteractionCheckpointUnavailableException(); }
            return await RefreshAsync().ConfigureAwait(false);
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
        else { outcome = await effects.ExecuteAsync(next, interaction, phaseLease, ct).ConfigureAwait(false); }
        if (next == AutomaticInteractionStep.ProviderInvocationAuthorized)
        {
            ProviderOutputAuthorityRefusal refusal = ClassifyAuthorityRefusal(outcome);
            if (refusal != ProviderOutputAuthorityRefusal.Unknown)
            {
                next = AutomaticInteractionStep.ProviderInvocationNotAuthorized;
                outcome = new(AutomaticInteractionOutcome.Blocked, outcome.DependencyRecordId) { OutputAuthorityRefusal = refusal };
            }
        }
        else if (next == AutomaticInteractionStep.RateAdmissionAuthorized && outcome.Outcome != AutomaticInteractionOutcome.Authorized)
        { next = AutomaticInteractionStep.InteractionPreAcceptanceFailed; outcome = new(AutomaticInteractionOutcome.Blocked, outcome.DependencyRecordId); }
        await RecordAsync(next, outcome, phaseLease).ConfigureAwait(false);
        return await RefreshAsync().ConfigureAwait(false);

        string Operation(AutomaticInteractionStep step) => AgentsSecurityIdentity.Derive("interaction-step", input.TenantId, input.AgentInteractionId, step.ToString());
        async Task SendDirectoryAsync(object command, string operationId)
        {
            // A submission receipt or an unrelated revision is not evidence that this transition won.
            await InteractionCommandDispatch.SendAsync(dispatcher, input.TenantId, ConversationAgentStateIdentity.Domain, directoryId, operationId, command, ct).ConfigureAwait(false);
            await ObserveAsync(async () =>
            {
                var observed = await directoryReader.ReadAsync(input.TenantId, input.ConversationId, ct).ConfigureAwait(false);
                if (observed is null || observed.TenantId != input.TenantId || observed.ConversationId != input.ConversationId) { return false; }
                bool found = command switch
                {
                    AcquireConversationEffectLease acquire => observed.Leases.TryGetValue(acquire.Lease.LeaseId, out var lease)
                        ? lease with { Status = acquire.Lease.Status, CommitRevision = 0, ResultRevision = 0, MustSettle = false } == acquire.Lease
                        : observed.DeletionRequestId is not null,
                    CommitConversationEffect commit => observed.Leases.TryGetValue(commit.LeaseId, out var lease)
                        && lease.AgentInteractionId == commit.AgentInteractionId && lease.WorkflowInstanceId == commit.WorkflowInstanceId
                        && (lease.CommitRevision > 0 || lease.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit || observed.DeletionRequestId is not null),
                    SettleConversationEffectLease settle => observed.Leases.TryGetValue(settle.LeaseId, out var lease)
                        && lease.AgentInteractionId == settle.AgentInteractionId && lease.WorkflowInstanceId == settle.WorkflowInstanceId
                        && lease.Status == ConversationEffectLeaseStatus.Settled && lease.CommitRevision == settle.CommitRevision && lease.ResultRevision == settle.ResultRevision,
                    CancelConversationEffectLease cancel => observed.Leases.TryGetValue(cancel.LeaseId, out var lease)
                        && lease.AgentInteractionId == cancel.AgentInteractionId && lease.WorkflowInstanceId == cancel.WorkflowInstanceId
                        && (lease.Status == ConversationEffectLeaseStatus.CancelledBeforeCommit || lease.CommitRevision > 0),
                    _ => false,
                };
                if (found) { directory = observed; }
                return found;
            }).ConfigureAwait(false);
        }
        async Task RecordNoInvocationAsync()
        {
            var disposition = await LookupNoInvocationAsync().ConfigureAwait(false);
            await RecordAsync(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, disposition, null).ConfigureAwait(false);
        }
        async Task SettleEffectsAsync()
        {
            // Retained cancellation events predate allocator evidence. Confirm no invocation before using one for settlement.
            if (interaction.AutomaticFacts.TryGetValue(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, out var disposition)
                && (disposition.CapacityAdmissionId != creation.AdmissionId || string.IsNullOrWhiteSpace(disposition.DependencyRecordId)))
            {
                var evidence = await LookupNoInvocationAsync().ConfigureAwait(false);
                await effects.SettleAsync(interaction, evidence, ct).ConfigureAwait(false);
                return;
            }
            await effects.SettleAsync(interaction, ct).ConfigureAwait(false);
        }
        async Task<AutomaticInteractionEffectResult> LookupNoInvocationAsync()
        {
            var lookup = await effects.ExecuteAsync(AutomaticInteractionStep.PreProviderCapacityDispositionDecided, interaction, null, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            if (lookup.Outcome != AutomaticInteractionOutcome.CancelNoInvocation || lookup.CapacityAdmissionId != creation.AdmissionId
                || string.IsNullOrWhiteSpace(lookup.DependencyRecordId))
            {
                throw new InvalidOperationException("AuthoritativeNoInvocationEvidenceRequired");
            }

            return new AutomaticInteractionEffectResult(lookup.Outcome, lookup.DependencyRecordId)
            {
                CapacityAdmissionId = lookup.CapacityAdmissionId,
            };
        }
        async Task RecordAsync(AutomaticInteractionStep step, AutomaticInteractionEffectResult outcome, ConversationEffectLease? lease)
        {
            bool closingSuppression = step == AutomaticInteractionStep.ConversationPostingSuppressedByDeletion
                || step == AutomaticInteractionStep.ProviderInvocationNotAuthorized && outcome.OutputAuthorityRefusal == ProviderOutputAuthorityRefusal.Unknown;
            var fact = new AutomaticInteractionFact(step, Operation(step), outcome.Outcome, interaction.AutomaticRevision + 1,
                lease?.LeaseId, lease?.CommitRevision ?? 0, outcome.DependencyRecordId, outcome.Authority, outcome.Output, outcome.ContentDigest, outcome.DigestKeyVersion, outcome.FailureKind,
                DeletionRequestId: closingSuppression ? directory.DeletionRequestId : null,
                DirectoryClosingRevision: closingSuppression ? directory.DeletionClosingRevision : 0)
            {
                OutputAuthorityRefusal = outcome.OutputAuthorityRefusal == ProviderOutputAuthorityRefusal.Unknown ? null : outcome.OutputAuthorityRefusal,
                CapacityAdmissionId = outcome.CapacityAdmissionId,
            };
            await InteractionCommandDispatch.SendAsync(dispatcher, input.TenantId, "agent-interaction", input.AgentInteractionId, fact.OperationId,
                new RecordAutomaticInteractionStep(input.TenantId, input.AgentInteractionId, fact, interaction.AutomaticRevision), ct).ConfigureAwait(false);
            string expected = JsonSerializer.Serialize(fact with { Revision = 0 });
            await ObserveAsync(async () =>
            {
                var observed = await interactionReader.ReadAsync(input.TenantId, input.AgentInteractionId, ct).ConfigureAwait(false);
                if (observed?.PermittedCreation?.Permit.TenantId != input.TenantId || observed.AgentInteractionId != input.AgentInteractionId
                    || !observed.AutomaticFacts.TryGetValue(step, out var recorded) || JsonSerializer.Serialize(recorded with { Revision = 0 }) != expected) { return false; }
                interaction = observed;
                return true;
            }).ConfigureAwait(false);
        }
        async Task ObserveAsync(Func<Task<bool>> observed)
        {
            for (int checkpoint = 0; checkpoint < 64; checkpoint++)
            {
                ct.ThrowIfCancellationRequested();
                bool matched = await observed().ConfigureAwait(false);
                ct.ThrowIfCancellationRequested();
                if (matched) { return; }
                if (checkpoint < 63) { await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(200, 25 * (checkpoint + 1))), ct).ConfigureAwait(false); }
            }
            ct.ThrowIfCancellationRequested();
            throw new AutomaticInteractionCheckpointUnavailableException();
        }
        Task<WorkflowExecutionState> RefreshAsync() => Task.FromResult(input with
        {
            EventRevision = interaction.AutomaticRevision, Status = WorkflowExecutionStatus.Running,
            Outcome = interaction.AutomaticFacts.Count > 0 ? AutomaticInteractionPolicy.Decide(interaction.AutomaticFacts.Values.MaxBy(f => f.Revision)!) : AutomaticInteractionOutcome.Unknown,
        });
    }
    private static ProviderOutputAuthorityRefusal ClassifyAuthorityRefusal(AutomaticInteractionEffectResult result)
    {
        if (result.Output is not null || result.ContentDigest is not null || result.DigestKeyVersion is not null
            || result.FailureKind != AutomaticInteractionFailureKind.Unknown || result.CapacityAdmissionId is not null
            || !Enum.IsDefined(result.OutputAuthorityRefusal)
            || (result.OutputAuthorityRefusal == ProviderOutputAuthorityRefusal.Open && string.IsNullOrWhiteSpace(result.DependencyRecordId)))
        {
            return ProviderOutputAuthorityRefusal.Malformed;
        }

        if (result.Outcome == AutomaticInteractionOutcome.Authorized)
        {
            return result.Authority?.IsEffective() == true && result.OutputAuthorityRefusal == ProviderOutputAuthorityRefusal.Unknown
                ? ProviderOutputAuthorityRefusal.Unknown
                : ProviderOutputAuthorityRefusal.Malformed;
        }

        if (result.Authority is not null || result.Outcome is not (AutomaticInteractionOutcome.Blocked or AutomaticInteractionOutcome.DependencyNotAvailable or AutomaticInteractionOutcome.Failed))
        {
            return ProviderOutputAuthorityRefusal.Malformed;
        }

        return result.OutputAuthorityRefusal == ProviderOutputAuthorityRefusal.Unknown
            ? ProviderOutputAuthorityRefusal.Unavailable
            : result.OutputAuthorityRefusal;
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
