using System.Text.Json;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>An accepted asynchronous receipt cannot turn a rejected divergent request into original-request success.</summary>
public sealed class AutomaticInteractionIntakeIntentTests
{
    [Fact]
    public async Task DivergentPermitWithAnAsynchronousRejectedReceiptCannotRecoverTheOriginalRequest()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        string original = JsonSerializer.Serialize(fixture.Directory!.Permits[fixture.InteractionId].Permit);
        int accepted = fixture.AcceptedCommandIds.Count; int scheduled = fixture.Scheduled.Count;
        var dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            await Task.Yield(); var intent = call.ArgAt<AgentCommandIntent>(0);
            intent.CommandType.ShouldBe(nameof(RegisterInteractionPermit));
            var error = await Should.ThrowAsync<InvalidOperationException>(() => fixture.DispatchAsync(intent, call.ArgAt<CancellationToken>(1)));
            error.Message.ShouldStartWith("Rejected:");
            var command = JsonSerializer.Deserialize<RegisterInteractionPermit>(intent.Payload).ShouldNotBeNull();
            var envelope = new CommandEnvelope(intent.MessageId, intent.TenantId, intent.Domain, intent.AggregateId, intent.CommandType, intent.Payload, intent.CorrelationId, intent.CausationId, intent.UserId, intent.Extensions);
            Hexalith.Agents.ConversationAgentState.ConversationAgentStateAggregate.Handle(command, fixture.Directory, envelope)
                .Events.Single().ShouldBeOfType<Hexalith.Agents.Contracts.ConversationAgentState.Events.ConversationDirectoryRejected>().Reason.ShouldBe(Hexalith.Agents.Contracts.ConversationAgentState.ConversationDirectoryRejectionReason.DivergentDuplicate);
            return new SubmitCommandResponse(intent.CorrelationId, MessageId: intent.MessageId);
        });
        var availability = Substitute.For<IAgentDependencyAvailability>();
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, dispatcher, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, availability, fixture, fixture, dispatcher, recovery);
        var request = fixture.Request with { RequestDigest = new string('b', 64) };
        var failure = await Should.ThrowAsync<InvalidOperationException>(() => intake.ExecuteAsync(request, CancellationToken.None));
        failure.Message.ShouldBe("DivergentDuplicate");
        JsonSerializer.Serialize(fixture.Directory.Permits[fixture.InteractionId].Permit).ShouldBe(original);
        fixture.AcceptedCommandIds.Count.ShouldBe(accepted); fixture.Scheduled.Count.ShouldBe(scheduled); fixture.Effects.Posts.ShouldBeEmpty();
    }
}
