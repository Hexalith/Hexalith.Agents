using System.Text.Json;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Shouldly;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Security;

namespace Hexalith.Agents.Tests;

/// <summary>Explicit no-content fixture; it supplies no live migration or protection attestation.</summary>
internal static class ConversationDirectoryTestData
{
    /// <summary>Gets the original permit.</summary>
    internal static InteractionPermit Permit()
    {
        const string tenant = "tenant-a"; const string conversation = "conversation-a";
        string id = ConversationAgentStateIdentity.Interaction(tenant, "hexa", conversation, "person-a", "client-a");
        return new(tenant, conversation, id, "hexa", "person-a", "client-a", ConversationAgentStateIdentity.Permit(tenant, id),
            ConversationAgentStateIdentity.CreationOutbox(tenant, id), new string('a', 64), new(1, 1, AgentResponseMode.Automatic, 1, "provider-a", "model-a", 1, 1, "full-conversation-v1"),
            new(tenant, "fixture-installation", "fixture-epoch", 0, "fixture-revocation", "fixture-enforcement"),
            new(new(tenant, id, "fixture-interaction-dek"), JsonSerializer.SerializeToElement(new { FixtureSealed = "request" })), "fixture-digest-v1");
    }
    /// <summary>Gets a trusted test envelope.</summary>
    internal static CommandEnvelope Envelope() => new("message", "tenant-a", ConversationAgentStateIdentity.Domain,
        ConversationAgentStateIdentity.Directory("tenant-a", "conversation-a"), nameof(RegisterInteractionPermit), [], "correlation", null, Permit().AgentInteractionId,
        new Dictionary<string, string>(StringComparer.Ordinal) { [ConversationAgentStateIdentity.WorkflowAuthority] = "true" });
    /// <summary>Gets the separately admitted Closing workflow envelope.</summary>
    internal static CommandEnvelope DeletionEnvelope() => Envelope() with { Extensions = new Dictionary<string, string>(StringComparer.Ordinal) { [ConversationAgentStateIdentity.DeletionAuthority] = "true" } };
    /// <summary>Replays a real permit decision.</summary>
    internal static ConversationAgentStateState State()
    {
        var state = new ConversationAgentStateState();
        foreach (var item in ConversationAgentStateAggregate.Handle(new RegisterInteractionPermit(Permit(), 0), null, Envelope()).Events) { state.Apply((dynamic)item); }
        return state;
    }
    /// <summary>Gets the exact reserved lease.</summary>
    internal static ConversationEffectLease Lease(ConversationEffectKind kind = ConversationEffectKind.WorkflowStart)
    {
        var p = Permit(); string operationId = AgentsSecurityIdentity.Derive("fixture-operation", kind.ToString());
        return new(ConversationAgentStateIdentity.Lease(p.TenantId, p.ConversationId, p.AgentInteractionId, kind, "fixture", operationId), p.AgentInteractionId, kind, "fixture", operationId,
            p.AgentInteractionId, p.DirectoryEpoch.EpochId, ConversationEffectLeaseStatus.Reserved, 0, 0, false);
    }
    /// <summary>Folds accepted events through the public replay seam.</summary>
    internal static void Apply(ConversationAgentStateState state, Hexalith.EventStore.Contracts.Results.DomainResult result)
    { foreach (var item in result.Events) { state.Apply((dynamic)item); } }
    /// <summary>Gets the automatic interaction's exact workflow envelope.</summary>
    internal static CommandEnvelope InteractionEnvelope() => Envelope() with { Domain = "agent-interaction", AggregateId = Permit().AgentInteractionId };

    /// <summary>Builds a valid automatic phase prefix exclusively through public handlers and replay, including committed directory leases.</summary>
    internal static (AgentInteractionState State, CommandEnvelope Envelope, AutomaticInteractionFact Next) PhasePrefix(AutomaticInteractionStep next)
    {
        var permit = Permit(); var envelope = InteractionEnvelope(); var directory = State();
        var state = new AgentInteractionState();
        var created = AgentInteractionAggregate.Handle(new CreatePermittedAgentInteraction(permit, 1, 0), state, envelope);
        created.IsRejection.ShouldBeFalse();
        foreach (var item in created.Events) { state.Apply((dynamic)item); }
        AutomaticInteractionStep[] phases =
        [
            AutomaticInteractionStep.WorkflowStartCheckpoint, AutomaticInteractionStep.RateAdmissionAuthorized,
            AutomaticInteractionStep.OpenInteractionLeaseAuthorized, AutomaticInteractionStep.RateAdmissionRecorded,
            AutomaticInteractionStep.OpenInteractionLeaseRecorded, AutomaticInteractionStep.ContextReadRecorded,
            AutomaticInteractionStep.BudgetReservationAuthorized, AutomaticInteractionStep.BudgetReservationRecorded,
            AutomaticInteractionStep.PromptSafetyRecorded, AutomaticInteractionStep.ConversationMembershipRecorded,
            AutomaticInteractionStep.AgentCallAccepted, AutomaticInteractionStep.CapacityAdmissionAuthorized,
            AutomaticInteractionStep.CapacityAdmissionRecorded, AutomaticInteractionStep.ProviderInvocationAuthorized,
            AutomaticInteractionStep.GeneratedVersionRecorded, AutomaticInteractionStep.ConversationPostingRecorded,
        ];
        foreach (var step in phases)
        {
            string operation = AgentsSecurityIdentity.Derive("interaction-step", permit.TenantId, permit.AgentInteractionId, step.ToString());
            ConversationEffectKind kind = step switch
            {
                AutomaticInteractionStep.WorkflowStartCheckpoint => ConversationEffectKind.WorkflowStart,
                AutomaticInteractionStep.ContextReadRecorded or AutomaticInteractionStep.PromptSafetyRecorded => ConversationEffectKind.ContextRead,
                AutomaticInteractionStep.ConversationMembershipRecorded => ConversationEffectKind.ConversationMembership,
                AutomaticInteractionStep.ProviderInvocationAuthorized => ConversationEffectKind.ProviderInvocation,
                AutomaticInteractionStep.GeneratedVersionRecorded => ConversationEffectKind.ProposalMutation,
                AutomaticInteractionStep.ConversationPostingRecorded => ConversationEffectKind.ConversationPosting,
                _ => ConversationEffectKind.Unknown,
            };
            ConversationEffectLease? lease = null;
            if (kind != ConversationEffectKind.Unknown)
            {
                string id = ConversationAgentStateIdentity.Lease(permit.TenantId, permit.ConversationId, permit.AgentInteractionId, kind, step.ToString(), operation);
                lease = new(id, permit.AgentInteractionId, kind, step.ToString(), operation, permit.AgentInteractionId, permit.DirectoryEpoch.EpochId, ConversationEffectLeaseStatus.Reserved, 0, 0, false);
                var acquired = ConversationAgentStateAggregate.Handle(new AcquireConversationEffectLease(lease, directory.Revision), directory, Envelope());
                acquired.IsRejection.ShouldBeFalse(); Apply(directory, acquired);
                var committed = ConversationAgentStateAggregate.Handle(new CommitConversationEffect(id, permit.AgentInteractionId, permit.AgentInteractionId, directory.Revision), directory, Envelope());
                committed.IsRejection.ShouldBeFalse(); Apply(directory, committed);
                lease = directory.Leases[id]; lease.CommitRevision.ShouldBeGreaterThan(0);
            }
            AutomaticInteractionOutcome outcome = step switch
            {
                AutomaticInteractionStep.RateAdmissionAuthorized or AutomaticInteractionStep.OpenInteractionLeaseAuthorized or AutomaticInteractionStep.BudgetReservationAuthorized
                    or AutomaticInteractionStep.CapacityAdmissionAuthorized or AutomaticInteractionStep.ProviderInvocationAuthorized => AutomaticInteractionOutcome.Authorized,
                AutomaticInteractionStep.ConversationPostingRecorded => AutomaticInteractionOutcome.Posted,
                _ => AutomaticInteractionOutcome.Allowed,
            };
            var fact = new AutomaticInteractionFact(step, operation, outcome, state.AutomaticRevision + 1, lease?.LeaseId, lease?.CommitRevision ?? 0,
                OutputAuthority: step == AutomaticInteractionStep.ProviderInvocationAuthorized ? new(1, new string('b', 64), AgentInteractionStatus.SafetyFailed, "reason", "metric", "disposition", 1, new string('c', 64)) : null,
                Output: step == AutomaticInteractionStep.GeneratedVersionRecorded ? permit.Request : null,
                ContentDigest: step == AutomaticInteractionStep.GeneratedVersionRecorded ? new string('d', 64) : null,
                DigestKeyVersion: step == AutomaticInteractionStep.GeneratedVersionRecorded ? permit.DigestKeyVersion : null);
            if (step == next) { return (state, envelope, fact); }
            var accepted = AgentInteractionAggregate.Handle(new RecordAutomaticInteractionStep(permit.TenantId, permit.AgentInteractionId, fact, state.AutomaticRevision), state, envelope);
            accepted.IsRejection.ShouldBeFalse(); accepted.IsNoOp.ShouldBeFalse();
            foreach (var item in accepted.Events) { state.Apply((dynamic)item); }
            if (lease is not null)
            {
                var settled = ConversationAgentStateAggregate.Handle(new SettleConversationEffectLease(lease.LeaseId, permit.AgentInteractionId, permit.AgentInteractionId, lease.CommitRevision, state.AutomaticRevision, directory.Revision), directory, Envelope());
                settled.IsRejection.ShouldBeFalse(); Apply(directory, settled);
            }
        }
        throw new ArgumentOutOfRangeException(nameof(next));
    }
}
