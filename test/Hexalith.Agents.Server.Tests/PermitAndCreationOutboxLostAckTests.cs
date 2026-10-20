using Hexalith.Agents.Server.Ports;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Directory and first-interaction append acknowledgement loss is recovered by exact stream reads.</summary>
public sealed class PermitAndCreationOutboxLostAckTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ExactRetryRecoversTheOriginalPermitCreationAndAcknowledgement(int checkpoint)
    {
        var fixture = new AutomaticInteractionReplayFixture { FaultAt = checkpoint, CrashAfterAppend = true };
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.StartAsync()); fixture.Restart();
        (await fixture.StartAsync()).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        fixture.Directory!.Permits.Count.ShouldBe(1); fixture.Directory.Acknowledgements.Count.ShouldBe(1); fixture.Scheduled.Count.ShouldBe(1);
        fixture.Interaction!.PermittedCreation!.PermitRevision.ShouldBe(1); fixture.Directory.PermitHighWater.ShouldBe(1);
    }
}
