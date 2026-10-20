using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Open, missing or unmatched runtime evidence cannot register the Dapr runtime.</summary>
public sealed class OpenOrMismatchedDaprSecurityDecisionBlocksWorkloadTests
{
    [Fact]
    public void UnavailableExactSecurityRecordLeavesTheServiceGraphUntouched()
    {
        var services = new ServiceCollection();
        var error = Should.Throw<NotSupportedException>(() => services.AddAgentsInteractionWorkflow(new CommittedAgentDependencyAvailability()));
        error.Message.ShouldBe("DependencyNotAvailable(OD-DAPR-SECURITY-1)"); services.ShouldBeEmpty();
    }
}
