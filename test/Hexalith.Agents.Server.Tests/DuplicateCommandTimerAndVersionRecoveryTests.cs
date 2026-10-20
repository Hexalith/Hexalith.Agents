using Hexalith.Agents.Contracts.AgentInteraction;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Repeated recovery and intake preserve all business identities; no standalone timer owner exists.</summary>
public sealed class DuplicateCommandTimerAndVersionRecoveryTests
{
    [Fact]
    public async Task TerminalReplaysAddNoCommandVersionAttemptOrPost()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); await fixture.CompleteAsync();
        string snapshot = fixture.Snapshot(); int commands = fixture.AcceptedCommandIds.Count;
        for (int n = 0; n < 3; n++) { fixture.Restart(); await fixture.StartAsync(); await fixture.CompleteAsync(); }
        fixture.Snapshot().ShouldBe(snapshot); fixture.AcceptedCommandIds.Count.ShouldBe(commands); fixture.Effects.Attempts.Count.ShouldBe(1); fixture.Effects.Posts.Count.ShouldBe(1);
        fixture.Interaction!.AutomaticFacts.Keys.Count(s => s == AutomaticInteractionStep.GeneratedVersionRecorded).ShouldBe(1);
    }
}
