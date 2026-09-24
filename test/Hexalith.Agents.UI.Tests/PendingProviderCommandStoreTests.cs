using System.Security.Claims;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.State;

using Microsoft.AspNetCore.Components.Authorization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Browser-session pending command identity retention and isolation.</summary>
public sealed class PendingProviderCommandStoreTests
{
    [Fact]
    public async Task Refresh_retains_safe_identity_and_another_login_cannot_inherit_its_lock()
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        var submitted = new PendingProviderCommand("DataHandlingAcceptance", "openai/gpt-x", "tenant-a",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-alice", "corr-alice",
                AgentSetupTruthState.Submitted));

        await new BrowserSessionPendingProviderCommandStore(javascript, authentication).SaveAsync(submitted);
        PendingProviderCommand restored = (await new BrowserSessionPendingProviderCommandStore(javascript, authentication)
            .LoadAsync()).ShouldHaveSingleItem();
        restored.Acceptance.MessageId.ShouldBe("msg-alice");
        javascript.Values.ShouldHaveSingleItem().Value.ShouldNotContain("ConfigurationReferenceId");

        authentication.GetAuthenticationStateAsync().Returns(State("bob", "tenant-a"));
        (await new BrowserSessionPendingProviderCommandStore(javascript, authentication).LoadAsync()).ShouldBeEmpty();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-b"));
        (await new BrowserSessionPendingProviderCommandStore(javascript, authentication).LoadAsync()).ShouldBeEmpty();

        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        var refreshed = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        await refreshed.RemoveAsync(submitted.Family, submitted.ResourceKey, "wrong-message");
        (await refreshed.LoadAsync()).ShouldHaveSingleItem();
        await refreshed.RemoveAsync(submitted.Family, submitted.ResourceKey, "msg-alice");
        (await refreshed.LoadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Operator_without_tenant_claim_uses_reserved_system_scope_and_storage_failure_stays_closed()
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(State("operator", null, platformOperator: true));
        var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        var submitted = new PendingProviderCommand("ProviderCatalogMutation", "openai/gpt-x", "system",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-platform", "corr-platform",
                AgentSetupTruthState.Submitted));

        await store.SaveAsync(submitted);
        (await new BrowserSessionPendingProviderCommandStore(javascript, authentication).LoadAsync())
            .ShouldHaveSingleItem().Acceptance.MessageId.ShouldBe("msg-platform");

        authentication.GetAuthenticationStateAsync().Returns(State("operator", null));
        await Should.ThrowAsync<InvalidOperationException>(() => store.LoadAsync());
    }

    private static Task<AuthenticationState> State(string userId, string? tenantId, bool platformOperator = false)
    {
        List<Claim> claims = [new Claim("sub", userId)];
        if (tenantId is not null)
        {
            claims.Add(new Claim("tenantId", tenantId));
        }

        if (platformOperator)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Agents.PlatformOperator"));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))));
    }
}
