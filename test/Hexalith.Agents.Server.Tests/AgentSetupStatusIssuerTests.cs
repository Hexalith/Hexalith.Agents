using System.Security.Cryptography;

using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Exercises composed status transport with the real SDK issuer in both dependency modes.</summary>
public sealed class AgentSetupStatusIssuerTests
{
    /// <summary>Only the exact declared domain obtains independently configured status credentials.</summary>
    [Theory]
    [InlineData("tenant-a", "agent", true)]
    [InlineData("system", "provider-catalog", true)]
    [InlineData("tenant-a", "tenant-provider-enablement", true)]
    [InlineData("tenant-a", "agent", false)]
    [InlineData("system", "provider-catalog", false)]
    [InlineData("tenant-a", "tenant-provider-enablement", false)]
    public async Task ComposedReaderUsesTheRealExactDomainIssuer(string tenant, string domain, bool credentials)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Agents:EventStore:BaseUrl"] = "https://gateway.example/",
            ["Agents:EventStore:AppId"] = "eventstore-runtime",
            ["Authentication:JwtBearer:Issuer"] = "test-status-issuer",
            ["Authentication:JwtBearer:Audience"] = "hexalith-eventstore",
            ["Authentication:JwtBearer:SigningKey"] = credentials
                ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) : null,
            ["Authentication:JwtBearer:AllowedAlgorithms:0"] = "HS256",
            ["Authentication:WorkloadIssuer:Workload"] = "agents-status-reader",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        services.AddSingleton(environment);
        var handler = new CommandStatusResponseHandler
        {
            Status = new CommandStatusQueryResponse("correlation", nameof(CommandStatus.Rejected),
                (int)CommandStatus.Rejected, "InvalidSetupRejection", "message") { TenantId = tenant, Domain = domain },
        };
        services.AddEventStoreGatewayClient(options => options.BaseAddress = new Uri("https://gateway.example/"))
            .AddEventStoreGatewayMultiDomainWorkloadTransport(["agent", "provider-catalog", "tenant-provider-enablement"])
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        services.AddAgentSetupServices(configuration);
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope operation = provider.CreateScope();
        IAgentCommandStatusReader reader = operation.ServiceProvider.GetRequiredService<IAgentCommandStatusReader>();

        CommandStatusQueryResponse? status = await reader.ReadAsync(tenant, domain, "message", "correlation",
            TestContext.Current.CancellationToken);

        if (credentials)
        {
            status.ShouldNotBeNull().IsRejected.ShouldBeTrue();
            handler.Path.ShouldBe($"/api/v1/commands/status/workload/{tenant}/message");
            handler.DaprAppId.ShouldBe("eventstore-runtime");
            var token = new JsonWebToken(handler.Assertion.ShouldNotBeNull());
            token.Issuer.ShouldBe("test-status-issuer");
            token.Audiences.ShouldBe(["eventstore"]);
            token.GetClaim(EventStoreWorkloadAuthenticationDefaults.CallerClaimType).Value.ShouldBe("agents-status-reader");
            token.GetClaim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType).Value
                .ShouldBe(EventStoreGatewayWorkloadOperations.CommandStatus);
            token.GetClaim(EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType).Value.ShouldBe(tenant);
            token.GetClaim(EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType).Value.ShouldBe(domain);
            token.TryGetClaim("global_admin", out _).ShouldBeFalse();
        }
        else
        {
            status.ShouldBeNull();
            handler.Requests.ShouldBe(0);
        }

        int requests = handler.Requests;
        (await reader.ReadAsync(tenant, "undeclared", "message", "correlation", TestContext.Current.CancellationToken))
            .ShouldBeNull();
        handler.Requests.ShouldBe(requests);
    }
}
