namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Xml.Linq;

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
