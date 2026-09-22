namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Xml.Linq;

using Hexalith.Agents;
using Hexalith.Agents.Server;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.DomainService;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

/// <summary>
/// Static guard for the platform-owned hosting boundary.
/// </summary>
public sealed class AppHostSecurityTopologyTests
{
    [Fact]
    public void AgentsShouldNotOwnAnAppHostProgram()
    {
        File.Exists(ModuleLayout.ResolveModulePath("src/Hexalith.Agents.AppHost/Program.cs"))
            .ShouldBeFalse("The production-like resource graph belongs to EXT-HOST-1, not this domain module.");
    }

    [Fact]
    public void ServerShouldRetainTheReusableEventStoreDomainServiceHost()
    {
        string program = File.ReadAllText(ModuleLayout.ResolveModulePath("src/Hexalith.Agents.Server/Program.cs"));

        program.ShouldContain("AddEventStoreDomainService(");
        program.ShouldContain("UseEventStoreDomainService()");
    }

    [Fact]
    public void ServerHostShouldDelegateTheStatusReaderFallbackToSetupComposition()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.AddEventStoreDomainService(
            typeof(AgentsAssemblyMarker).Assembly,
            typeof(ServerAssemblyMarker).Assembly);
        AgentDomainHostComposition.Configure(builder);
        using ServiceProvider provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = false,
            ValidateScopes = true,
        });

        provider.GetServices<IIdempotencyIntentAdapter>().ShouldBeEmpty();
        provider.GetServices<ITrustedCommandExtensionPolicy>().ShouldBeEmpty();
        provider.GetRequiredService<IAgentCommandStatusReader>()
            .ShouldBeOfType<DeferredAgentCommandStatusReader>();
    }

    [Fact]
    public void DomainServiceShouldRemainTheOnlyOwnedExecutableWebHost()
    {
        string[] webHosts = Directory.GetFiles(ModuleLayout.SourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !ModuleLayout.IsUnderBuildOutput(path))
            .Where(path => (XDocument.Load(path).Root?.Attribute("Sdk")?.Value ?? string.Empty)
                .Equals("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .ToArray();

        webHosts.ShouldBe(["Hexalith.Agents.Server"]);
    }

    [Fact]
    public void ServerShouldNotAbsorbPlatformTopology()
    {
        string program = File.ReadAllText(ModuleLayout.ResolveModulePath("src/Hexalith.Agents.Server/Program.cs"));

        program.ShouldNotContain("DistributedApplication.CreateBuilder");
        program.ShouldNotContain("IDistributedApplicationBuilder");
        program.ShouldNotContain("Aspire.Hosting");
        program.ShouldNotContain("AddHexalithEventStoreSecurity");
        program.ShouldNotContain("Authentication__JwtBearer__");
        program.ShouldNotContain("Authentication__OpenIdConnect__");
        program.ShouldNotContain("EventStore__Authentication__");
    }
}
