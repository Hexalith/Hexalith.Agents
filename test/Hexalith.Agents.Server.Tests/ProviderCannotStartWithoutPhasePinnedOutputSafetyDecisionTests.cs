using Hexalith.Agents.Contracts.AgentInteraction;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Malformed exact authority follows the same negative Provider branch as missing/Open authority.</summary>
public sealed class ProviderCannotStartWithoutPhasePinnedOutputSafetyDecisionTests
{
    [Theory]
    [InlineData(0, "valid", AgentInteractionStatus.GenerationFailed)]
    [InlineData(1, "malformed", AgentInteractionStatus.GenerationFailed)]
    [InlineData(1, "valid", AgentInteractionStatus.Unknown)]
    public async Task MalformedAuthorityNeverInvokesProvider(long revision, string digest, AgentInteractionStatus status)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        fixture.Effects.AuthorityOverride = new(revision, digest == "valid" ? new string('b', 64) : digest, status, "reason", "metric", "open", 1, new string('c', 64));
        await fixture.StartAsync(); await fixture.CompleteAsync();
        fixture.Interaction!.AutomaticFacts.ShouldContainKey(AutomaticInteractionStep.ProviderInvocationNotAuthorized);
        fixture.Effects.Attempts.ShouldBeEmpty(); fixture.Effects.Preparations.ShouldBeEmpty();
    }
}
