using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.EventStore.Contracts.Commands;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>The recorded authority revision is immutable even when a successor uses the same operation identity.</summary>
public sealed class OutputSafetySuccessorCannotRelabelAuthorizedAttemptTests
{
    [Fact]
    public async Task ChangedAuthorityOnTheSameBranchIsADivergentDuplicate()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.CompleteAsync();
        var original = fixture.Dispatches.Select(i => (Intent: i, Command: i.CommandType == nameof(RecordAutomaticInteractionStep) ? JsonSerializer.Deserialize<RecordAutomaticInteractionStep>(i.Payload) : null))
            .Single(p => p.Command?.Fact.Step == AutomaticInteractionStep.ProviderInvocationAuthorized);
        var c = original.Command!; var changed = c with { Fact = c.Fact with { OutputAuthority = c.Fact.OutputAuthority! with { DecisionRevision = 2 } } };
        var i = original.Intent; var envelope = new CommandEnvelope(i.MessageId, i.TenantId, i.Domain, i.AggregateId, i.CommandType, [], i.CorrelationId, null, i.UserId, i.Extensions);
        AgentInteractionAggregate.Handle(changed, fixture.Interaction, envelope).IsRejection.ShouldBeTrue();
        fixture.Interaction!.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationAuthorized].OutputAuthority!.DecisionRevision.ShouldBe(1);
    }
}
