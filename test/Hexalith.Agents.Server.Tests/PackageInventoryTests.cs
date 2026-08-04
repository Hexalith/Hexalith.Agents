namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Text.Json;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Pins the exact release package inventory and its clean-consumer automation.
/// </summary>
public sealed class PackageInventoryTests
{
    private static readonly Dictionary<string, string> _expectedPackages = new(StringComparer.Ordinal)
    {
        ["Hexalith.Agents"] = "src/Hexalith.Agents/Hexalith.Agents.csproj",
        ["Hexalith.Agents.Client"] = "src/Hexalith.Agents.Client/Hexalith.Agents.Client.csproj",
        ["Hexalith.Agents.Contracts"] = "src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj",
        ["Hexalith.Agents.Testing"] = "src/Hexalith.Agents.Testing/Hexalith.Agents.Testing.csproj",
        ["Hexalith.Agents.UI"] = "src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj",
    };

    [Fact]
    public void ReleaseManifestShouldDeclareTheExactPublicPackageSet()
    {
        string path = ModuleLayout.ResolveModulePath("eng/release-packages.json");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(path));
        Dictionary<string, string> packages = manifest.RootElement.GetProperty("packages")
            .EnumerateArray()
            .ToDictionary(
                package => package.GetProperty("id").GetString()!,
                package => package.GetProperty("project").GetString()!,
                StringComparer.Ordinal);

        packages.Count.ShouldBe(_expectedPackages.Count);
        foreach ((string packageId, string projectPath) in _expectedPackages)
        {
            packages.ContainsKey(packageId).ShouldBeTrue();
            packages[packageId].ShouldBe(projectPath);
        }

        foreach ((string packageId, string projectPath) in packages)
        {
            XDocument project = XDocument.Load(ModuleLayout.ResolveModulePath(projectPath));
            string[] declaredIds = project.Descendants("PackageId")
                .Select(element => element.Value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            declaredIds.ShouldBe([packageId]);
        }

        string[] explicitlyPackableProjects = ModuleLayout.ProjectFiles
            .Where(project => XDocument.Load(project).Descendants("IsPackable")
                .Any(element => element.Value.Equals("true", StringComparison.OrdinalIgnoreCase)))
            .Select(project => Path.GetRelativePath(ModuleLayout.ModuleRoot, project).Replace('\\', '/'))
            .OrderBy(project => project, StringComparer.Ordinal)
            .ToArray();
        explicitlyPackableProjects.ShouldBe(
            _expectedPackages.Values.OrderBy(project => project, StringComparer.Ordinal),
            ignoreOrder: false);
    }

    [Fact]
    public void ReleaseAutomationShouldPackInspectAndBuildAnIsolatedConsumer()
    {
        string[] requiredFiles =
        [
            "scripts/pack-release-packages.py",
            "scripts/validate-nuget-packages.py",
            "scripts/validate-consumer-package-references.py",
            "eng/verify-story.ps1",
            ".github/workflows/ci.yml",
        ];

        foreach (string requiredFile in requiredFiles)
        {
            File.Exists(ModuleLayout.ResolveModulePath(requiredFile))
                .ShouldBeTrue($"Story 5.1 requires release gate '{requiredFile}'.");
        }

        string workflow = File.ReadAllText(ModuleLayout.ResolveModulePath(".github/workflows/ci.yml"));
        workflow.ShouldContain("package-build:");
        workflow.ShouldContain("contracts-tests:");
        workflow.ShouldContain("client-tests:");
        workflow.ShouldContain("domain-tests:");
        workflow.ShouldContain("server-tests:");
        workflow.ShouldContain("ui-tests:");
        workflow.ShouldContain("boundary-checks:");
        workflow.ShouldContain("package-consumer:");
        workflow.ShouldNotContain("--filter-class");
        workflow.ShouldNotContain("UseHexalithProjectReferences=true");
        workflow.ShouldNotContain("-c Debug");

        foreach (string command in workflow.Split('\n').Where(line => line.Contains("dotnet ", StringComparison.Ordinal)))
        {
            command.ShouldContain("UseHexalithProjectReferences=false");
            command.ShouldContain("Release");
        }
    }

    [Fact]
    public void PackageToolingShouldValidateIdentityAndProtectCallerOwnedOutput()
    {
        string packScript = File.ReadAllText(ModuleLayout.ResolveModulePath("scripts/pack-release-packages.py"));
        packScript.ShouldContain("declared_package_id");
        packScript.ShouldContain("evaluated_package_id");
        packScript.ShouldContain("archive_package_id");
        packScript.ShouldContain("validate_output_directory");
        packScript.ShouldContain("clean_manifest_archives");
        packScript.ShouldContain("\"dotnet\", \"build\"");
        packScript.ShouldContain("\"--no-build\"");

        string verifier = File.ReadAllText(ModuleLayout.ResolveModulePath("eng/verify-story.ps1"));
        verifier.ShouldContain("Unrelated.Package.1.0.0.nupkg");
        verifier.ShouldContain("Package automation deleted an unrelated caller-owned archive.");
    }
}
