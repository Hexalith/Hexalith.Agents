using Hexalith.Agents.Contracts.AgentInteraction;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Output safety never uses a local public-status mapping or a successor decision.</summary>
public sealed class InitialOutputSafetyUsesPhasePinnedStatusTests
{
    [Theory]
    [InlineData(AutomaticInteractionFailureKind.OutputSafetyDenied)]
    [InlineData(AutomaticInteractionFailureKind.OutputSafetyUnavailable)]
    public async Task SafetyFailureUsesOnlyTheExactProductSelectedStatusAndRetainsNoContent(AutomaticInteractionFailureKind kind)
    {
        var fixture = new AutomaticInteractionReplayFixture(); fixture.Effects.FailAt = AutomaticInteractionStep.GeneratedVersionRecorded; fixture.Effects.FailureKind = kind;
        fixture.Effects.AuthorityOverride = new(7, new string('b', 64), AgentInteractionStatus.Denied, "product-reason", "product-metric", "product-open", 3, new string('c', 64));
        await fixture.StartAsync(); await fixture.CompleteAsync(); fixture.Restart();
        fixture.Interaction!.Status.ShouldBe(AgentInteractionStatus.Denied);
        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.GenerationFailed].FailureKind.ShouldBe(kind);
        fixture.Interaction.AutomaticFacts[AutomaticInteractionStep.ProviderInvocationAuthorized].OutputAuthority!.DecisionRevision.ShouldBe(7);
        fixture.Interaction.AutomaticFacts.Values.ShouldAllBe(f => f.Output == null); fixture.Effects.Posts.ShouldBeEmpty();
        fixture.Effects.Observed.Count(s => s == AutomaticInteractionStep.ProviderInvocationAuthorized).ShouldBe(1);
    }
}
