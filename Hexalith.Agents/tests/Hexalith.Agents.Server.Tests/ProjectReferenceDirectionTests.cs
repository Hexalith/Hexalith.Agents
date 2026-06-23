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
/// wrong-direction <em>project</em> edge before it can introduce a cycle or invert the boundary.
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
        ["Hexalith.Agents.Testing"] = ["Hexalith.Agents.Contracts", "Hexalith.Agents.Server"],
        ["Hexalith.Agents.AppHost"] = ["Hexalith.Agents.Server", "Hexalith.Agents.UI"],
        ["Hexalith.Agents.Aspire"] = [],
        ["Hexalith.Agents.ServiceDefaults"] = [],
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
        foreach ((string project, string[] allowed) in _allowedReferences)
        {
            foreach (string reference in InModuleReferences(project))
            {
                allowed.ShouldContain(
                    reference,
                    $"'{project}' references in-module project '{reference}', which violates the AD-15 dependency direction.");
            }
        }
    }

    // In-module (Hexalith.Agents*) ProjectReference targets declared by the given source project.
    private static IReadOnlyList<string> InModuleReferences(string project)
    {
        string projectFile = Path.Combine(
            ModuleLayout.ModuleRoot, "src", project, $"{project}.csproj");

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
