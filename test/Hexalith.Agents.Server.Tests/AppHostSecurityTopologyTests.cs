namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Linq;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Static guard for the live-binding AppHost security invariant from AD-16.
/// </summary>
public sealed class AppHostSecurityTopologyTests
{
    [Fact]
    public void AppHostShouldReferenceEventStoreAspireAsAHelperProjectOnly()
    {
        string appHostProject = ModuleLayout.SourceProjectFile("Hexalith.Agents.AppHost");

        XElement[] references = ProjectReferences(appHostProject)
            .Where(reference => ReferenceInclude(reference).EndsWith(
                "Hexalith.EventStore.Aspire.csproj",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        references.Length.ShouldBe(1, "Expected Agents AppHost to reference Hexalith.EventStore.Aspire once.");
        XElement reference = references[0];

        ReferenceInclude(reference).ShouldContain("$(HexalithEventStoreRoot)");
        AttributeValue(reference, "IsAspireProjectResource").ShouldBe("false");
    }

    [Fact]
    public void AppHostShouldInitializeSharedEventStoreSecurity()
    {
        string program = File.ReadAllText(ModuleLayout.ResolveModulePath("src/Hexalith.Agents.AppHost/Program.cs"));

        program.ShouldContain("using Hexalith.EventStore.Aspire;");
        program.ShouldContain("AddHexalithEventStoreSecurity()");
    }

    [Fact]
    public void EventStoreAspireReferenceShouldStayInAppHostTopologyOnly()
    {
        foreach (string projectFile in ModuleLayout.ProjectFiles.Where(IsProductionProject))
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            bool referencesEventStoreAspire = ProjectReferences(projectFile)
                .Any(reference => ReferenceInclude(reference).EndsWith(
                    "Hexalith.EventStore.Aspire.csproj",
                    StringComparison.OrdinalIgnoreCase));

            referencesEventStoreAspire.ShouldBe(
                string.Equals(projectName, "Hexalith.Agents.AppHost", StringComparison.OrdinalIgnoreCase),
                $"Only Hexalith.Agents.AppHost may reference Hexalith.EventStore.Aspire; found project '{projectName}'.");
        }
    }

    [Fact]
    public void AppHostShouldNotDuplicateEventStoreSecurityEnvironmentWiring()
    {
        string program = File.ReadAllText(ModuleLayout.ResolveModulePath("src/Hexalith.Agents.AppHost/Program.cs"));

        program.ShouldNotContain("Authentication__JwtBearer__");
        program.ShouldNotContain("Authentication__OpenIdConnect__");
        program.ShouldNotContain("EventStore__Authentication__");
    }

    private static bool IsProductionProject(string projectFile)
        => Path.GetFullPath(projectFile).StartsWith(
            Path.GetFullPath(ModuleLayout.SourceRoot),
            StringComparison.OrdinalIgnoreCase);

    private static XElement[] ProjectReferences(string projectFile)
        => XDocument.Load(projectFile)
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static string ReferenceInclude(XElement reference)
        => AttributeValue(reference, "Include");

    private static string AttributeValue(XElement element, string localName)
        => element
            .Attributes()
            .Single(attribute => string.Equals(attribute.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
            .Value
            .Replace('\\', '/');
}
