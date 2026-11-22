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

    [Theory]
    [InlineData(AutomaticInteractionStep.ProviderInvocationNotAuthorized)]
    [InlineData(AutomaticInteractionStep.PreProviderCapacityDispositionDecided)]
    public void TerminalProviderRefusalsStayPubliclyBlockedThroughDispositionsAndReplay(AutomaticInteractionStep step)
    {
        var state = Created();
        var fact = new AutomaticInteractionFact(step, new string('b', 64),
            step == AutomaticInteractionStep.PreProviderCapacityDispositionDecided ? AutomaticInteractionOutcome.CancelNoInvocation : AutomaticInteractionOutcome.Blocked, 2);
        if (step == AutomaticInteractionStep.ProviderInvocationNotAuthorized) { state.Apply(new ProviderInvocationNotAuthorized(fact)); }
        else { state.Apply(new PreProviderCapacityDispositionDecided(fact)); }
        AssertBlocked(state);
        state.Apply(new BudgetDispositionDecided(new(AutomaticInteractionStep.BudgetDispositionDecided, new string('c', 64), AutomaticInteractionOutcome.NotInvoked, 3)));
        AssertBlocked(state);
        var restored = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        AssertBlocked(restored);
        restored.AutomaticFacts[step].ShouldBe(fact);

        static void AssertBlocked(AgentInteractionState inspected)
        {
            AgentInteractionAuditInspection.GetStatus(inspected, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Blocked);
            AgentInteractionAuditInspection.GetStatus(inspected, false).View.ShouldBeNull();
        }
    }

    [Fact]
    public void DeletionSuppressedStartIsPubliclyBlockedAfterReplay()
    {
        var state = Created();
        AgentInteractionAuditInspection.GetStatus(state, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Requested);

        var fact = new AutomaticInteractionFact(
            AutomaticInteractionStep.StartSuppressedByDeletion,
            new string('b', 64),
            AutomaticInteractionOutcome.Suppressed,
            2);
        state.Apply(new StartSuppressedByDeletion(fact));

        AgentInteractionAuditInspection.GetStatus(state, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Blocked);
        var restored = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        AgentInteractionAuditInspection.GetStatus(restored, true).View.ShouldNotBeNull().Status.ShouldBe(AgentInteractionStatus.Blocked);
        AgentInteractionAuditInspection.GetStatus(restored, false).View.ShouldBeNull();
        restored.AutomaticFacts[AutomaticInteractionStep.StartSuppressedByDeletion].ShouldBe(fact);
    }

    [Theory]
    [InlineData(AutomaticInteractionFailureKind.ProviderError, AgentInteractionStatus.GenerationFailed)]
    [InlineData(AutomaticInteractionFailureKind.OutputSafetyDenied, AgentInteractionStatus.Blocked)]
    [InlineData(AutomaticInteractionFailureKind.OutputSafetyUnavailable, AgentInteractionStatus.Blocked)]
    public void GenerationFailuresKeepTheirPhasePinnedSafetyMapping(AutomaticInteractionFailureKind kind, AgentInteractionStatus expected)
    {
        var state = Created();
        state.Apply(new ProviderInvocationAuthorized(new(AutomaticInteractionStep.ProviderInvocationAuthorized, new string('b', 64), AutomaticInteractionOutcome.Authorized, 2,
            OutputAuthority: new(1, new string('c', 64), AgentInteractionStatus.Blocked, "reason", "metric", "disposition", 1, new string('d', 64)))));
        state.Apply(new GenerationFailed(new(AutomaticInteractionStep.GenerationFailed, new string('e', 64), AutomaticInteractionOutcome.Failed, 3, FailureKind: kind)));
        AgentInteractionAuditInspection.GetStatus(state, true).View.ShouldNotBeNull().Status.ShouldBe(expected);
        var restored = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        AgentInteractionAuditInspection.GetStatus(restored, true).View.ShouldNotBeNull().Status.ShouldBe(expected);
    }

    private static AgentInteractionState Created()
    {
        var p = ConversationDirectoryTestData.Permit(); var e = ConversationDirectoryTestData.Envelope() with { Domain = "agent-interaction", AggregateId = p.AgentInteractionId };
        var state = new AgentInteractionState(); state.Apply(AgentInteractionAggregate.Handle(new CreatePermittedAgentInteraction(p, 1, 0), null, e).Events.Single().ShouldBeOfType<PermittedAgentInteractionCreated>()); return state;
    }
}
