namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Dependency-direction guard (AC2 / AD-15). Verifies the in-module project-reference graph flows the right
/// way: <c>Hexalith.Agents.Contracts</c> references nothing outward, and every other <c>src/</c> project only
/// references projects allowed by the architecture direction matrix (client/UI/server/testing/apphost consume
/// contracts, never the reverse). The compiled-assembly boundary test catches external leaks; this catches a
/// wrong-direction <em>project</em> edge before it can introduce a cycle or invert the boundary. Discovery and the
/// allow-list must have exact key equality so a newly added source project cannot bypass this guard.
/// </summary>
public sealed class ProjectReferenceDirectionTests
{
    // Allowed in-module ProjectReference targets per source project (AD-15).
    private static readonly Dictionary<string, string[]> _allowedReferences = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Hexalith.Agents.Contracts"] = [],                                                           // inward-most
        ["Hexalith.Agents.Client"] = ["Hexalith.Agents.Contracts"],
        ["Hexalith.Agents"] = ["Hexalith.Agents.Contracts"],                                          // domain library
        ["Hexalith.Agents.Server"] = ["Hexalith.Agents.Contracts", "Hexalith.Agents.Client", "Hexalith.Agents"], // + domain library (Story 1.2: aggregate discovery)
        ["Hexalith.Agents.UI"] = ["Hexalith.Agents.Contracts", "Hexalith.Agents.Client"],
        ["Hexalith.Agents.Testing"] = ["Hexalith.Agents.Contracts"],
    };

    [Fact]
    public void ContractsShouldReferenceNoOtherProject()
    {
        IReadOnlyList<string> references = InModuleReferences("Hexalith.Agents.Contracts");

        references.ShouldBeEmpty(
            "Hexalith.Agents.Contracts is the inward-most boundary and must reference no other project (AC2/AD-15).");
    }

    [Fact]
    public void EverySourceProjectShouldOnlyReferenceAllowedProjects()
    {
        string[] discoveredProjects = Directory.GetFiles(ModuleLayout.SourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(path => !ModuleLayout.IsUnderBuildOutput(path))
            .Select(path => Path.GetFileNameWithoutExtension(path)!)
            .OrderBy(project => project, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] guardedProjects = _allowedReferences.Keys
            .OrderBy(project => project, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        guardedProjects.ShouldBe(discoveredProjects, ignoreOrder: false);

        foreach (string project in discoveredProjects)
        {
            string[] actual = InModuleReferences(project)
                .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] expected = _allowedReferences[project]
                .OrderBy(reference => reference, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            actual.ShouldBe(expected, ignoreOrder: false,
                $"'{project}' must retain its exact AD-15 in-module project-reference boundary.");
        }
    }

    // In-module (Hexalith.Agents*) ProjectReference targets declared by the given source project.
    private static IReadOnlyList<string> InModuleReferences(string project)
    {
        string projectFile = ModuleLayout.SourceProjectFile(project);

        File.Exists(projectFile).ShouldBeTrue($"Expected source project '{project}' to exist for the direction guard.");

        return XDocument.Load(projectFile)
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => include is not null)
            .Select(include => Path.GetFileNameWithoutExtension(include!.Replace('\\', '/')))
            .Where(name => name.StartsWith("Hexalith.Agents", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
