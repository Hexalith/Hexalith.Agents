using System.Text.Json;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>A terminal gateway rejection belongs to one revision-scoped submission, not the permanent lease identity.</summary>
public sealed class AutomaticInteractionSubmissionRecoveryTests
{
    [Fact]
    public async Task AnotherInteractionWinningTheDirectoryRevisionDoesNotPoisonEveryLaterSubmission()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        var cache = new Dictionary<string, SubmitCommandResponse>(); var submitted = new List<AgentCommandIntent>();
        AgentCommandIntent? stale = null; AgentCommandIntent? recovered = null; int cachedReplays = 0;
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var intent = call.ArgAt<AgentCommandIntent>(0); var ct = call.ArgAt<CancellationToken>(1); submitted.Add(intent);
            if (cache.TryGetValue(intent.MessageId, out var receipt)) { cachedReplays++; return receipt; }
            if (intent.CommandType == nameof(AcquireConversationEffectLease) && stale is null)
            {
                var p = fixture.Directory!.Permits[fixture.InteractionId].Permit;
                string otherId = ConversationAgentStateIdentity.Interaction(p.TenantId, p.AgentId, p.ConversationId, p.CallerPartyId, "other-client");
                var other = p with { AgentInteractionId = otherId, ClientIdempotencyKey = "other-client", PermitId = ConversationAgentStateIdentity.Permit(p.TenantId, otherId),
                    CreationOutboxId = ConversationAgentStateIdentity.CreationOutbox(p.TenantId, otherId), Request = p.Request with { Target = p.Request.Target with { AgentInteractionId = otherId } } };
                await InteractionCommandDispatch.SendAsync(fixture, p.TenantId, intent.Domain, intent.AggregateId, other.PermitId,
                    new RegisterInteractionPermit(other, fixture.Directory.Revision), ct);
                stale = intent;
            }
            try
            {
                await fixture.DispatchAsync(intent, ct);
                if (stale is not null && intent.CommandType == stale.CommandType && recovered is null) { recovered = intent; }
            }
            catch (InvalidOperationException error) when (error.Message.StartsWith("Rejected:", StringComparison.Ordinal))
            {
                var command = JsonSerializer.Deserialize<AcquireConversationEffectLease>(intent.Payload).ShouldNotBeNull();
                var envelope = new CommandEnvelope(intent.MessageId, intent.TenantId, intent.Domain, intent.AggregateId, intent.CommandType, intent.Payload, intent.CorrelationId, intent.CausationId, intent.UserId, intent.Extensions);
                Hexalith.Agents.ConversationAgentState.ConversationAgentStateAggregate.Handle(command, fixture.Directory, envelope)
                    .Events.Single().ShouldBeOfType<Hexalith.Agents.Contracts.ConversationAgentState.Events.ConversationDirectoryRejected>().Reason.ShouldBe(ConversationDirectoryRejectionReason.StaleRevision);
            }
            receipt = new SubmitCommandResponse(intent.CorrelationId, MessageId: intent.MessageId); cache.Add(intent.MessageId, receipt); return receipt;
        });
        var steps = new AutomaticInteractionStepOrchestrator(fixture, fixture, dispatcher, fixture.Effects);
        WorkflowExecutionState result = fixture.Input;
        for (int n = 0; n < 100 && stale is null; n++) { result = await steps.ExecuteAsync(result, CancellationToken.None); }
        stale.ShouldNotBeNull();
        int beforeReplay = fixture.Dispatches.Count;
        var exact = JsonSerializer.Deserialize<AcquireConversationEffectLease>(stale.Payload).ShouldNotBeNull();
        await InteractionCommandDispatch.SendAsync(dispatcher, stale.TenantId, stale.Domain, stale.AggregateId, exact.Lease.LeaseId, exact, CancellationToken.None);
        submitted[^1].MessageId.ShouldBe(stale.MessageId);
        cachedReplays.ShouldBe(1); fixture.Dispatches.Count.ShouldBe(beforeReplay);
        for (int n = 0; n < 100 && result.Status == WorkflowExecutionStatus.Running; n++) { result = await steps.ExecuteAsync(result, CancellationToken.None); }
        result.Status.ShouldBe(WorkflowExecutionStatus.Completed); recovered.ShouldNotBeNull();
        recovered.MessageId.ShouldNotBe(stale.MessageId);
        var first = JsonSerializer.Deserialize<AcquireConversationEffectLease>(stale.Payload).ShouldNotBeNull();
        var retry = JsonSerializer.Deserialize<AcquireConversationEffectLease>(recovered.Payload).ShouldNotBeNull();
        retry.ExpectedRevision.ShouldBeGreaterThan(first.ExpectedRevision); retry.Lease.ShouldBe(first.Lease);
        fixture.Directory!.Permits.Count.ShouldBe(2); fixture.Effects.Posts.Count.ShouldBe(1); fixture.Scheduled.ShouldBe([fixture.InteractionId]);
        fixture.Directory.Leases.Values.Where(l => l.AgentInteractionId == fixture.InteractionId).ShouldAllBe(l => l.Status == ConversationEffectLeaseStatus.Settled);
    }
}
