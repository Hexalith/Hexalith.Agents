using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Public inspections expose terminal automatic failures without relabeling generated content or safety mappings.</summary>
public sealed class AutomaticInteractionPublicStatusTests
{
    [Theory]
    [InlineData(AutomaticInteractionOutcome.Blocked)]
    [InlineData(AutomaticInteractionOutcome.DependencyNotAvailable)]
    [InlineData(AutomaticInteractionOutcome.Suppressed)]
    public void PreAcceptanceFailureIsPubliclyBlocked(AutomaticInteractionOutcome outcome)
    {
        var state = Created(); state.Apply(new InteractionPreAcceptanceFailed(new(AutomaticInteractionStep.InteractionPreAcceptanceFailed, new string('b', 64), outcome, 2)));
        AgentInteractionAuditInspection.GetStatus(state, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Blocked);
        AgentInteractionAuditInspection.GetStatus(state, false).View.ShouldBeNull();
        var restored = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        AgentInteractionAuditInspection.GetStatus(restored, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Blocked);
    }

    [Theory]
    [InlineData(AutomaticInteractionOutcome.Blocked)]
    [InlineData(AutomaticInteractionOutcome.DependencyNotAvailable)]
    [InlineData(AutomaticInteractionOutcome.Failed)]
    public void UnsuccessfulPostIsPubliclyPostingFailedAndPreservesGeneratedContent(AutomaticInteractionOutcome outcome)
    {
        var state = Created(); var p = state.PermittedCreation!.Permit;
        var generated = new AutomaticInteractionFact(AutomaticInteractionStep.GeneratedVersionRecorded, new string('b', 64), AutomaticInteractionOutcome.Allowed, 2,
            Output: p.Request, ContentDigest: new string('c', 64), DigestKeyVersion: p.DigestKeyVersion);
        state.Apply(new GeneratedVersionRecorded(generated));
        state.Apply(new ConversationPostingRecorded(new(AutomaticInteractionStep.ConversationPostingRecorded, new string('d', 64), outcome, 3)));
        var restored = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        AgentInteractionAuditInspection.GetStatus(restored, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        JsonSerializer.Serialize(restored.AutomaticFacts[AutomaticInteractionStep.GeneratedVersionRecorded]).ShouldBe(JsonSerializer.Serialize(generated));
    }

    private static AgentInteractionState Created()
    {
        var p = ConversationDirectoryTestData.Permit(); var e = ConversationDirectoryTestData.Envelope() with { Domain = "agent-interaction", AggregateId = p.AgentInteractionId };
        var state = new AgentInteractionState(); state.Apply(AgentInteractionAggregate.Handle(new CreatePermittedAgentInteraction(p, 1, 0), null, e).Events.Single().ShouldBeOfType<PermittedAgentInteractionCreated>()); return state;
    }
}
