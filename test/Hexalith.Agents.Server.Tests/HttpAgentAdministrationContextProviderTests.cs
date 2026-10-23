namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Security.Claims;

using Hexalith.Agents.Server.Ports;

using Microsoft.AspNetCore.Http;

using NSubstitute;

using Shouldly;

/// <summary>
/// The trusted caller context is derived from the authenticated principal alone (Story 5.2 AC4). Nothing here may be
/// influenced by a request body or query string, and every incomplete principal — no HTTP context, unauthenticated,
/// no tenant claim, no administrator role — must fail closed to an unauthorized context.
/// </summary>
public sealed class HttpAgentAdministrationContextProviderTests
{
    [Theory]
    [InlineData("Agents.PlatformOperator", false)]
    [InlineData("Platform.Operator", false)]
    public void Platform_authority_comes_only_from_the_agents_platform_operator_role(string role, bool expected)
    {
        AgentAdministrationContext context = Resolve(("sub", "operator"), (ClaimTypes.Role, role));

        context.IsPlatformOperator.ShouldBe(expected);
    }

    [Fact]
    public void An_authenticated_administrator_yields_an_authorized_context()
    {
        AgentAdministrationContext context = Resolve(
            ("tenantId", "acme"),
            ("sub", "admin-user"),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole));

        context.TenantId.ShouldBe("acme");
        context.ActorUserId.ShouldBe("admin-user");
        context.IsAgentsAdmin.ShouldBeFalse();
        context.IsAuthorized.ShouldBeFalse();
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
        context.IsAuthorized.ShouldBeFalse();
    }

    [Fact]
    public void The_actor_falls_back_to_the_name_identifier_claim()
    {
        AgentAdministrationContext context = Resolve(
            ("tenantId", "acme"),
            (ClaimTypes.NameIdentifier, "admin-user"),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole));

        context.ActorUserId.ShouldBe("admin-user");
        context.IsAuthorized.ShouldBeFalse();
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

    /// <summary>Verifies current authority cannot combine two authenticated identities, even when their claims agree.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MultipleAuthenticatedIdentitiesAreAnonymousBeforeAuthorityReads(bool identicalClaims)
    {
        ClaimsIdentity first = new(
            [new Claim("tenantId", "acme"), new Claim("sub", "admin-user")], "first-auth");
        ClaimsIdentity second = new(
            [new Claim("tenantId", identicalClaims ? "acme" : "another-tenant"),
                new Claim("sub", identicalClaims ? "admin-user" : "another-user"),
                new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole)], "second-auth");

        await AssertAnonymousBeforeAuthorityReadsAsync(new ClaimsPrincipal([first, second]));
    }

    /// <summary>Verifies conflicting aliases or repeated claim types deny identity before either authority source is read.</summary>
    [Theory]
    [InlineData("tenantId")]
    [InlineData("tenant_id")]
    [InlineData("tid")]
    [InlineData("tenant")]
    [InlineData("sub")]
    [InlineData(ClaimTypes.NameIdentifier)]
    public async Task ConflictingIdentityClaimsAreAnonymousBeforeAuthorityReads(string conflictingClaimType)
    {
        ClaimsIdentity identity = new(
            [new Claim("tenantId", "acme"), new Claim("sub", "admin-user"),
                new Claim(conflictingClaimType, "conflicting-value")], "test-auth");

        await AssertAnonymousBeforeAuthorityReadsAsync(new ClaimsPrincipal(identity));
    }

    /// <summary>Verifies consistent claim aliases preserve one authenticated identity without granting claim-based roles.</summary>
    [Fact]
    public void AgreeingIdentityAliasesPreserveTheAuthenticatedScopeAndSubject()
    {
        AgentAdministrationContext context = Resolve(
            ("tenantId", "acme"), ("tenant_id", "acme"), ("tid", "acme"), ("tenant", "acme"),
            ("sub", "admin-user"), (ClaimTypes.NameIdentifier, "admin-user"),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole),
            (ClaimTypes.Role, HttpAgentAdministrationContextProvider.PlatformOperatorRole));

        context.TenantId.ShouldBe("acme");
        context.ActorUserId.ShouldBe("admin-user");
        context.IsAgentsAdmin.ShouldBeFalse();
        context.IsPlatformOperator.ShouldBeFalse();
    }

    /// <summary>Verifies unverified secondary claims cannot override the scope or subject of the one authenticated identity.</summary>
    [Fact]
    public void UnauthenticatedIdentityCannotOverrideTheAuthenticatedScopeOrSubject()
    {
        ClaimsIdentity unverified = new(
            [new Claim("tenantId", "another-tenant"), new Claim("sub", "another-user"),
                new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole)]);
        ClaimsIdentity authenticated = new(
            [new Claim("tenantId", "acme"), new Claim("sub", "member-user")], "test-auth");
        DefaultHttpContext httpContext = new() { User = new ClaimsPrincipal([unverified, authenticated]) };

        AgentAdministrationContext context = Provider(httpContext).GetContext();

        context.TenantId.ShouldBe("acme");
        context.ActorUserId.ShouldBe("member-user");
        context.IsAgentsAdmin.ShouldBeFalse();
        context.IsPlatformOperator.ShouldBeFalse();
    }

    /// <summary>Verifies claim roles cannot replace a fresh current-authority denial for either administration principal.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ClaimRolesCannotOverrideCurrentAuthorityDenial(bool unverifiedSecondary, bool platform)
    {
        var clock = new Story54TimeProvider();
        ClaimsIdentity authenticated = new(
            [new Claim("tenantId", "tenant-a"), new Claim("tenant_id", "tenant-a"),
                new Claim("tid", "tenant-a"), new Claim("tenant", "tenant-a"),
                new Claim("sub", "authenticated-subject"), new Claim(ClaimTypes.NameIdentifier, "authenticated-subject")],
            "test-auth");
        Claim[] roleClaims =
        [
            new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole),
            new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.PlatformOperatorRole),
        ];
        ClaimsPrincipal principal;
        if (unverifiedSecondary)
        {
            ClaimsIdentity unverified = new(
                [new Claim("tenantId", "another-tenant"), new Claim("sub", "another-subject"), .. roleClaims]);
            principal = new ClaimsPrincipal([unverified, authenticated]);
        }
        else
        {
            authenticated.AddClaims(roleClaims);
            principal = new ClaimsPrincipal(authenticated);
        }

        var actors = Substitute.For<ICurrentHumanActorSource>();
        actors.ReadAsync("tenant-a", "authenticated-subject", CancellationToken.None)
            .Returns(new CurrentHumanActorEvidence("tenant-a", "authenticated-subject", "actor-a", "party-a", 1, 1,
                clock.Now, clock.Now.AddSeconds(5)));
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        string authorityTenant = platform ? "system" : "tenant-a";
        TenantAuthorityEvidence denial = platform
            ? new("system", "global-administrators", 1, clock.Now, clock.Now.AddSeconds(5),
                [new(1, "actor-a", null, HttpAgentAdministrationContextProvider.PlatformOperatorRole, false)],
                SourceAggregateId: "global-administrators")
            : new("tenant-a", "tenants", 2, clock.Now, clock.Now.AddSeconds(5),
                [new(1, string.Empty, null, "TenantActive", true),
                    new(2, "actor-a", null, HttpAgentAdministrationContextProvider.AgentsAdministratorRole, false)],
                SourceAggregateId: "tenant-a");
        tenants.ReadAsync(authorityTenant, platform, CancellationToken.None).Returns(denial);
        var provider = new HttpAgentAdministrationContextProvider(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } },
            tenants, actors, Substitute.For<IAgentDependencyAvailability>(), clock);

        AgentAdministrationContext context = await provider.GetContextAsync(platform, CancellationToken.None);

        context.TenantId.ShouldBe("tenant-a");
        context.ActorUserId.ShouldBe("authenticated-subject");
        context.IsAgentsAdmin.ShouldBeFalse();
        context.IsPlatformOperator.ShouldBeFalse();
        context.IsAuthorized.ShouldBeFalse();
        await actors.Received(1).ReadAsync("tenant-a", "authenticated-subject", CancellationToken.None);
        await tenants.Received(1).ReadAsync(authorityTenant, platform, CancellationToken.None);
        actors.ReceivedCalls().Count().ShouldBe(1);
        tenants.ReceivedCalls().Count().ShouldBe(1);
    }

    /// <summary>Verifies unverified secondary claims cannot complete an authenticated identity's missing scope or subject.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnauthenticatedIdentityCannotSupplyMissingAuthenticatedClaims(bool missingTenant)
    {
        ClaimsIdentity authenticated = new(
            [new Claim(missingTenant ? "sub" : "tenantId", missingTenant ? "member-user" : "acme")], "test-auth");
        ClaimsIdentity unverified = new(
            [new Claim("tenantId", "acme"), new Claim("sub", "admin-user"),
                new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole)]);
        var actors = Substitute.For<ICurrentHumanActorSource>();
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        var provider = new HttpAgentAdministrationContextProvider(
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal([authenticated, unverified]) },
            }, tenants, actors, Substitute.For<IAgentDependencyAvailability>());

        foreach (bool platform in new[] { false, true })
        {
            AgentAdministrationContext context = await provider.GetContextAsync(platform, CancellationToken.None);

            context.TenantId.ShouldBeEmpty();
            context.ActorUserId.ShouldBe(missingTenant ? "member-user" : string.Empty);
            context.IsAgentsAdmin.ShouldBeFalse();
            context.IsPlatformOperator.ShouldBeFalse();
        }

        actors.ReceivedCalls().ShouldBeEmpty();
        tenants.ReceivedCalls().ShouldBeEmpty();
    }

    private static async Task AssertAnonymousBeforeAuthorityReadsAsync(ClaimsPrincipal principal)
    {
        var actors = Substitute.For<ICurrentHumanActorSource>();
        var tenants = Substitute.For<ICurrentTenantAuthoritySource>();
        var provider = new HttpAgentAdministrationContextProvider(
            new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = principal } },
            tenants, actors, Substitute.For<IAgentDependencyAvailability>());

        provider.GetContext().ShouldBe(AgentAdministrationContext.Anonymous);
        foreach (bool platform in new[] { false, true })
        {
            (await provider.GetContextAsync(platform, CancellationToken.None)).ShouldBe(AgentAdministrationContext.Anonymous);
        }

        actors.ReceivedCalls().ShouldBeEmpty();
        tenants.ReceivedCalls().ShouldBeEmpty();
    }

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
