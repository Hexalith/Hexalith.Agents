using Hexalith.Agents.Server.Ports;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Creation and schedule acknowledgements are resolved by exact owner/runtime lookup.</summary>
public sealed class WorkflowStartOutboxTests
{
    [Fact]
    public async Task LostScheduleAcknowledgementDoesNotCreateASecondInstance()
    {
        var fixture = new AutomaticInteractionReplayFixture { LoseScheduleAcknowledgement = true };
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.StartAsync());
        fixture.Restart();
        (await fixture.Recovery().RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        fixture.Scheduled.Count.ShouldBe(1); fixture.Directory.ShouldNotBeNull().Permits.Count.ShouldBe(1); fixture.Directory.Acknowledgements.Count.ShouldBe(1);
    }
}
