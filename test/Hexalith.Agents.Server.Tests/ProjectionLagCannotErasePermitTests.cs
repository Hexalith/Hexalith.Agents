using Hexalith.Agents.Server.Ports;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>No projection is a recovery input; a delayed creation remains discoverable in its owner's permit map.</summary>
public sealed class ProjectionLagCannotErasePermitTests
{
    [Fact]
    public async Task RecoveryFindsThePermitWhenTheInteractionAndAcknowledgementAreStillAbsent()
    {
        var fixture = new AutomaticInteractionReplayFixture { FaultAt = 1, CrashAfterAppend = false };
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.StartAsync()); fixture.Restart();
        fixture.Directory!.Permits.Count.ShouldBe(1); fixture.Directory.Acknowledgements.ShouldBeEmpty(); fixture.Interaction.ShouldBeNull();
        (await fixture.Recovery().RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        fixture.Directory.Permits.Count.ShouldBe(1); fixture.Directory.Acknowledgements.Count.ShouldBe(1);
    }
}
