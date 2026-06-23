namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Linq;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Public-contract package-boundary guard (AC2). The public contract projects (<c>Hexalith.Agents.Contracts</c>
/// and <c>Hexalith.Agents.Client</c>) must not declare a <c>PackageReference</c> on server infrastructure,
/// provider SDKs, the Dapr runtime, EventStore <em>server</em> internals, the Aspire host stack, or the UI
/// shell. Complements the compiled-assembly boundary test by catching a forbidden package that is declared but
/// not yet referenced in code (which the assembly-reference scan would miss).
/// </summary>
public sealed class PublicContractPackageBoundaryTests
{
    private static readonly string[] _publicContractProjects =
    [
        "Hexalith.Agents.Contracts",
        "Hexalith.Agents.Client",
    ];

    private static readonly string[] _forbiddenPackagePrefixes =
    [
        "Microsoft.AspNetCore",        // server / web infrastructure
        "Aspire.",                     // Aspire host stack
        "Dapr",                        // Dapr runtime
        "Microsoft.SemanticKernel",    // provider / agent-runtime SDK
        "Microsoft.Agents",            // Microsoft.Agents.AI / .Workflows
        "Microsoft.Extensions.AI",     // AI runtime
        "Azure.AI",                    // provider SDK
        "OpenAI",                      // provider SDK
        "Anthropic",                   // provider SDK
        "ModelContextProtocol",        // tool-host runtime
        "Hexalith.EventStore.Server",  // EventStore server internals
        "Hexalith.FrontComposer",      // UI shell
    ];

    [Fact]
    public void PublicContractProjectsShouldNotReferenceForbiddenPackages()
    {
        foreach (string project in _publicContractProjects)
        {
            string projectFile = Path.Combine(
                ModuleLayout.ModuleRoot, "src", project, $"{project}.csproj");

            File.Exists(projectFile).ShouldBeTrue($"Expected public contract project '{project}' to exist.");

            foreach (string package in PackageReferenceNames(projectFile))
            {
                bool isForbidden = _forbiddenPackagePrefixes.Any(prefix =>
                    package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                isForbidden.ShouldBeFalse(
                    $"'{project}' declares a forbidden boundary package '{package}' (AC2).");
            }
        }
    }

    private static IEnumerable<string> PackageReferenceNames(string projectFile)
        => XDocument.Load(projectFile)
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.OrdinalIgnoreCase))
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => include!);
}
