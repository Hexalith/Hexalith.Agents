using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// The deferred gateway placeholders keep the DI graph complete before the live read path is wired (Epic 4). They
/// must fail closed (AD-12): a host that has not yet bound the real read path renders the permission-denied surface
/// rather than fabricating a "ready/healthy" Agent or an empty "no providers configured" success. The component
/// tests substitute the gateways, so this contract is otherwise unexercised.
/// </summary>
public sealed class DeferredGatewayTests
{
    [Fact]
    public async Task DeferredAgentSetupGateway_status_fails_closed_with_not_authorized_and_no_agent()
    {
        DeferredAgentSetupGateway gateway = new();

        AgentInspectionResult result = await gateway.GetStatusAsync(CancellationToken.None);

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Agent.ShouldBeNull();
    }

    [Fact]
    public async Task DeferredAgentSetupGateway_configuration_fails_closed_with_not_authorized_and_no_agent()
    {
        DeferredAgentSetupGateway gateway = new();

        AgentInspectionResult result = await gateway.GetConfigurationAsync(CancellationToken.None);

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Agent.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeferredProviderCatalogGateway_fails_closed_with_not_authorized_and_no_entries(bool includeDisabled)
    {
        DeferredProviderCatalogGateway gateway = new();

        ProviderCatalogInspectionResult result = await gateway.ListEntriesAsync(includeDisabled, CancellationToken.None);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        result.Entries.ShouldBeEmpty();
    }
}
