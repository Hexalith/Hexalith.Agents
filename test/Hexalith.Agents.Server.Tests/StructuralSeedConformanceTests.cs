namespace Hexalith.Agents.Server.Tests;

using System.IO;

using Shouldly;

/// <summary>
/// Structural-seed conformance guard (AC1 / AC4). Asserts the module ships the architecture Structural Seed:
/// the root build/solution files, the named <c>src/</c> + <c>test/</c> project set (extension points), and
/// the empty <c>Hexalith.Agents.Server</c> extension folders. Required entries are checked as a subset so
/// later stories may add projects/folders without breaking the guard.
/// </summary>
public sealed class StructuralSeedConformanceTests
{
    private static readonly string[] _requiredRootFiles =
    [
        "Hexalith.Agents.slnx",
        "global.json",
        "Directory.Build.props",
        "Directory.Packages.props",
        "NuGet.config",
    ];

    private static readonly string[] _requiredSourceProjects =
    [
        "Hexalith.Agents.Contracts",
        "Hexalith.Agents.Client",
        "Hexalith.Agents.Server",
        "Hexalith.Agents",                 // main domain library
        "Hexalith.Agents.UI",
        "Hexalith.Agents.AppHost",
        "Hexalith.Agents.Aspire",
        "Hexalith.Agents.ServiceDefaults",
        "Hexalith.Agents.Testing",
    ];

    private static readonly string[] _requiredTestProjects =
    [
        "Hexalith.Agents.Contracts.Tests",
        "Hexalith.Agents.Server.Tests",
    ];

    // Named extension points inside Hexalith.Agents.Server (AC4) — no premature entities, folders only.
    private static readonly string[] _requiredServerExtensionFolders =
    [
        "Aggregates",
        "Application/Agents",
        "Application/Workflows",
        "Application/Activities",
        "Application/Tools",
        "Ports",
        "Projections",
    ];

    [Fact]
    public void ModuleShouldShipRequiredRootBuildFiles()
    {
        foreach (string fileName in _requiredRootFiles)
        {
            File.Exists(ModuleLayout.RootFile(fileName))
                .ShouldBeTrue($"Structural Seed requires the root file '{fileName}' (AC1).");
        }
    }

    [Fact]
    public void ModuleShouldExposeSrcAndTestTrees()
    {
        Directory.Exists(ModuleLayout.SourceRoot)
            .ShouldBeTrue("Structural Seed requires a 'src/' tree (AC1).");
        Directory.Exists(ModuleLayout.TestsRoot)
            .ShouldBeTrue("Structural Seed requires a 'test/' tree (AC1).");
    }

    [Fact]
    public void ModuleShouldExposeNamedSourceProjects()
    {
        foreach (string project in _requiredSourceProjects)
        {
            string projectFile = ModuleLayout.SourceProjectFile(project);

            File.Exists(projectFile)
                .ShouldBeTrue($"Structural Seed requires the named extension-point project '{project}' (AC4).");
        }
    }

    [Fact]
    public void ModuleShouldExposeBoundaryGuardTestProjects()
    {
        foreach (string project in _requiredTestProjects)
        {
            string projectFile = Path.Combine(ModuleLayout.TestsRoot, project, $"{project}.csproj");

            File.Exists(projectFile)
                .ShouldBeTrue($"Structural Seed requires the boundary-guard test project '{project}' (AC3).");
        }
    }

    [Fact]
    public void ServerShouldExposeNamedExtensionFolders()
    {
        string serverRoot = Path.Combine(ModuleLayout.SourceRoot, "Hexalith.Agents.Server");

        foreach (string folder in _requiredServerExtensionFolders)
        {
            string folderPath = Path.Combine(serverRoot, folder.Replace('/', Path.DirectorySeparatorChar));

            Directory.Exists(folderPath)
                .ShouldBeTrue($"AC4 requires the named extension folder 'Server/{folder}'.");
        }
    }
}
