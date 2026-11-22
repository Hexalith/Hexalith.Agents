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

    [Fact]
    public async Task ClosingAfterPermitAppendSuppressesCreationAcrossRestarts()
    {
        var fixture = new AutomaticInteractionReplayFixture { FaultAt = 0, CrashAfterAppend = true };
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.StartAsync());
        fixture.Directory.ShouldNotBeNull().Permits.Keys.ShouldBe([fixture.InteractionId]);
        fixture.Interaction.ShouldBeNull();
        await fixture.CloseAsync();
        fixture.Restart();
        fixture.Directory.ShouldNotBeNull().DeletionRequestId.ShouldNotBeNull();

        string before = fixture.Snapshot();
        int dispatchCount = fixture.Dispatches.Count;
        int appendCount = fixture.AcceptedCommandIds.Count;
        for (int restart = 0; restart < 2; restart++)
        {
            var result = await fixture.Recovery().RecoverAsync(
                AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None);

            result.Status.ShouldBe(AutomaticInteractionIntakeStatus.StartSuppressedByDeletion);
            result.AgentInteractionId.ShouldBe(fixture.InteractionId);
            result.Dependency.ShouldBeNull();
            fixture.Dispatches.Count.ShouldBe(dispatchCount);
            fixture.AcceptedCommandIds.Count.ShouldBe(appendCount);
            fixture.Snapshot().ShouldBe(before);
            fixture.Directory.ShouldNotBeNull().Acknowledgements.ShouldBeEmpty();
            fixture.Directory.Leases.ShouldBeEmpty();
            fixture.Interaction.ShouldBeNull();
            fixture.Scheduled.ShouldBeEmpty();
            fixture.Restart();
        }
    }
}
