using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.Security;
using Hexalith.EventStore.Contracts.Commands;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>A failed generation cannot be relabelled as a generated version by a late/replayed command.</summary>
public sealed class GenerationFailureCannotInventProposalVersionOrRetainOutputTests
{
    [Fact]
    public async Task TerminalFailureRejectsLateGeneratedContent()
    {
        var fixture = new AutomaticInteractionReplayFixture(); fixture.Effects.FailAt = AutomaticInteractionStep.GeneratedVersionRecorded;
        await fixture.StartAsync(); await fixture.CompleteAsync();
        var state = fixture.Interaction!; var creation = state.PermittedCreation!;
        var fact = new AutomaticInteractionFact(AutomaticInteractionStep.GeneratedVersionRecorded,
            AgentsSecurityIdentity.Derive("interaction-step", creation.Permit.TenantId, fixture.InteractionId, AutomaticInteractionStep.GeneratedVersionRecorded.ToString()),
            AutomaticInteractionOutcome.Allowed, state.AutomaticRevision + 1, new string('a', 64), 1, Output: creation.Permit.Request, ContentDigest: new string('d', 64), DigestKeyVersion: "fixture-digest-v1");
        var envelope = new CommandEnvelope("message", creation.Permit.TenantId, "agent-interaction", fixture.InteractionId, nameof(RecordAutomaticInteractionStep), [], "correlation", null, fixture.InteractionId,
            new Dictionary<string, string>(StringComparer.Ordinal) { [ConversationAgentStateIdentity.WorkflowAuthority] = "true" });
        AgentInteractionAggregate.Handle(new RecordAutomaticInteractionStep(creation.Permit.TenantId, fixture.InteractionId, fact, state.AutomaticRevision), state, envelope).IsRejection.ShouldBeTrue();
        state.AutomaticFacts.Values.ShouldAllBe(f => f.Output == null); state.GeneratedVersions.ShouldBeNull(); fixture.Effects.Posts.ShouldBeEmpty();
    }
}
