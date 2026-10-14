using Hexalith.Agents.Server.Ports;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Proves a legacy provider cannot promote synchronous role claims through the current-authority default.</summary>
public sealed class LegacyAdministrationContextProviderTests
{
    /// <summary>Verifies that legacy role claims cannot establish either current principal.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsyncDefaultPreservesIdentityButClearsBothLegacyRoleClaims(bool platform)
    {
        IAgentAdministrationContextProvider legacy = new LegacyRoleClaimAdministrationContextProvider();
        legacy.GetContext().IsAuthorized.ShouldBeTrue();
        legacy.GetContext().IsPlatformOperator.ShouldBeTrue();

        AgentAdministrationContext current = await legacy.GetContextAsync(platform, CancellationToken.None);

        current.TenantId.ShouldBe("tenant-a");
        current.ActorUserId.ShouldBe("authenticated-subject");
        current.IsAgentsAdmin.ShouldBeFalse();
        current.IsPlatformOperator.ShouldBeFalse();
        current.IsAuthorized.ShouldBeFalse();
    }

    /// <summary>Verifies that cancellation prevents legacy context resolution.</summary>
    [Fact]
    public async Task CancelledLegacyResolutionGrantsNoAuthority()
    {
        IAgentAdministrationContextProvider legacy = new LegacyRoleClaimAdministrationContextProvider();
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await legacy.GetContextAsync(false, new CancellationToken(canceled: true)));
    }
}
