namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Security.Claims;

using Hexalith.Agents.Server.Ports;

using Microsoft.AspNetCore.Http;

using Shouldly;

/// <summary>
/// The trusted caller context is derived from the authenticated principal alone (Story 5.2 AC4). Nothing here may be
/// influenced by a request body or query string, and every incomplete principal — no HTTP context, unauthenticated,
/// no tenant claim, no administrator role — must fail closed to an unauthorized context.
/// </summary>
public sealed class HttpAgentAdministrationContextProviderTests
{
    [Fact]
    public void An_authenticated_administrator_yields_an_authorized_context()
    {
        AgentAdministrationContext context = Resolve(
            ("tenantId", "acme"),
            ("sub", "admin-user"),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole));

        context.TenantId.ShouldBe("acme");
        context.ActorUserId.ShouldBe("admin-user");
        context.IsAgentsAdmin.ShouldBeTrue();
        context.IsAuthorized.ShouldBeTrue();
    }

    [Theory]
    [InlineData("tenant_id")]
    [InlineData("tid")]
    [InlineData("tenant")]
    public void The_tenant_is_read_from_any_of_the_accepted_claim_types(string claimType)
    {
        AgentAdministrationContext context = Resolve(
            (claimType, "acme"),
            ("sub", "admin-user"),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole));

        context.TenantId.ShouldBe("acme");
        context.IsAuthorized.ShouldBeTrue();
    }

    [Fact]
    public void The_actor_falls_back_to_the_name_identifier_claim()
    {
        AgentAdministrationContext context = Resolve(
            ("tenantId", "acme"),
            (ClaimTypes.NameIdentifier, "admin-user"),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole));

        context.ActorUserId.ShouldBe("admin-user");
        context.IsAuthorized.ShouldBeTrue();
    }

    [Fact]
    public void A_caller_without_the_administrator_role_is_not_an_agents_admin()
    {
        AgentAdministrationContext context = Resolve(("tenantId", "acme"), ("sub", "member-user"));

        // The tenant and actor are still known; authority is what is missing, and that alone denies the operation.
        context.TenantId.ShouldBe("acme");
        context.ActorUserId.ShouldBe("member-user");
        context.IsAgentsAdmin.ShouldBeFalse();
        context.IsAuthorized.ShouldBeFalse();
    }

    [Fact]
    public void An_unrelated_role_never_grants_agent_administration()
    {
        AgentAdministrationContext context = Resolve(
            ("tenantId", "acme"),
            ("sub", "member-user"),
            (ClaimTypes.Role, "Agents.Reader"));

        context.IsAgentsAdmin.ShouldBeFalse();
        context.IsAuthorized.ShouldBeFalse();
    }

    [Fact]
    public void An_administrator_without_a_tenant_claim_is_not_authorized()
    {
        AgentAdministrationContext context = Resolve(
            ("sub", "admin-user"),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole));

        // A tenantless administrator has no scope to administer, so the operation cannot be tenant-checked at all.
        context.TenantId.ShouldBeEmpty();
        context.IsAuthorized.ShouldBeFalse();
    }

    [Fact]
    public void An_anonymous_principal_yields_the_anonymous_context()
    {
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity()),
        };

        Provider(httpContext).GetContext().ShouldBe(AgentAdministrationContext.Anonymous);
    }

    [Fact]
    public void An_unauthenticated_principal_carrying_admin_claims_is_still_anonymous()
    {
        // Claims on an unauthenticated identity are unverified assertions; honoring them would let anyone self-grant.
        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("tenantId", "acme"),
                new Claim("sub", "impostor"),
                new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole),
            ])),
        };

        Provider(httpContext).GetContext().ShouldBe(AgentAdministrationContext.Anonymous);
    }

    [Fact]
    public void No_http_context_at_all_yields_the_anonymous_context()
        => Provider(httpContext: null).GetContext().ShouldBe(AgentAdministrationContext.Anonymous);

    private static AgentAdministrationContext Resolve(params (string Type, string Value)[] claims)
    {
        List<Claim> claimList = [];
        foreach ((string type, string value) in claims)
        {
            claimList.Add(new Claim(type, value));
        }

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claimList, "test-auth", ClaimTypes.NameIdentifier, ClaimTypes.Role)),
        };

        return Provider(httpContext).GetContext();
    }

    private static HttpAgentAdministrationContextProvider Provider(HttpContext? httpContext)
        => new(new HttpContextAccessor { HttpContext = httpContext });
}
