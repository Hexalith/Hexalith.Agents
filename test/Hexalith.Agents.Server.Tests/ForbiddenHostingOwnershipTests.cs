namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

/// <summary>Fails closed when any repository-owned path reacquires platform hosting or recursive submodules.</summary>
public sealed class ForbiddenHostingOwnershipTests
{
    private static readonly string[] _guardFiles =
    [
        "scripts/validate-nuget-packages.py",
        "test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs",
        "test/Hexalith.Agents.Server.Tests/ForbiddenHostingOwnershipTests.cs",
    ];

    private static readonly string[] _hostingProjectSuffixes = [".AppHost", ".Aspire", ".ServiceDefaults"];

    [Fact]
    public void RepositoryOwnedPathsShouldContainNoHostingProjectOrConfiguration()
    {
        string[] offenders = GovernedFiles("*")
            .Where(path =>
            {
                string fileName = Path.GetFileName(path);
                string projectName = Path.GetFileNameWithoutExtension(path);
                return fileName.Equals("aspire.config.json", StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                    && _hostingProjectSuffixes.Any(suffix => projectName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            })
            .Concat(GovernedDirectories()
                .Where(path => _hostingProjectSuffixes.Any(suffix =>
                    Path.GetFileName(path).EndsWith(suffix, StringComparison.OrdinalIgnoreCase))))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        offenders.ShouldBeEmpty($"Hosting belongs to the platform. Forbidden owned paths: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void RepositoryOwnedTextShouldContainNoHostingSdkReferenceMarkerOrTopologyApi()
    {
        string[] forbiddenTokens =
        [
            "Aspire.AppHost.Sdk",
            "IsAspireSharedProject",
            "DistributedApplication.CreateBuilder",
            "IDistributedApplicationBuilder",
            "Aspire.Hosting",
            "AddHexalithEventStoreSecurity(",
            "AddProject<",
        ];
        string[] offenders = GovernedFiles("*")
            .Where(IsOwnedTextFile)
            .Where(path => forbiddenTokens.Any(token =>
                File.ReadAllText(path).Contains(token, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        offenders.ShouldBeEmpty(
            $"Repository-owned hosting SDK/reference/topology APIs are forbidden: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void SolutionShouldContainEveryOwnedProjectButNoHostingOrLegacySolution()
    {
        string solution = File.ReadAllText(ModuleLayout.RootFile("Hexalith.Agents.slnx"));
        foreach (string suffix in _hostingProjectSuffixes)
        {
            solution.ShouldNotContain(suffix);
        }

        GovernedFiles("*.sln").ShouldBeEmpty();
        foreach (string projectFile in ModuleLayout.ProjectFiles)
        {
            string relative = Path.GetRelativePath(ModuleLayout.ModuleRoot, projectFile).Replace('\\', '/');
            solution.ShouldContain(relative);
        }
    }

    [Fact]
    public void OwnedProjectsShouldContainNoHostingReferenceEvenWhenNestedOrDifferentlyNamed()
    {
        foreach (string projectFile in ModuleLayout.ProjectFiles)
        {
            XDocument project = XDocument.Load(projectFile);
            string sdk = project.Root?.Attribute("Sdk")?.Value ?? string.Empty;
            sdk.ShouldNotContain("Aspire.AppHost.Sdk");

            foreach (XElement element in project.Descendants())
            {
                string include = element.Attribute("Include")?.Value ?? string.Empty;
                if (element.Name.LocalName is "ProjectReference" or "PackageReference")
                {
                    _hostingProjectSuffixes.Any(suffix => include.Contains(suffix, StringComparison.OrdinalIgnoreCase))
                        .ShouldBeFalse($"Forbidden hosting reference '{include}' in '{projectFile}'.");
                    include.Contains("Aspire.Hosting", StringComparison.OrdinalIgnoreCase)
                        .ShouldBeFalse($"Forbidden Aspire hosting package '{include}' in '{projectFile}'.");
                }

                element.Name.LocalName.Equals("IsAspireSharedProject", StringComparison.OrdinalIgnoreCase)
                    .ShouldBeFalse($"Forbidden IsAspireSharedProject marker in '{projectFile}'.");
            }
        }
    }

    [Fact]
    public void AutomationShouldNeverInitializeNestedSubmodulesRecursively()
    {
        var recursiveGit = new Regex(
            @"\bgit\b[^\r\n]*(?:--recursive\b|--recurse-submodules\b)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var recursiveCheckout = new Regex(
            @"\bsubmodules\s*:\s*recursive\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        string[] offenders = GovernedFiles("*")
            .Where(IsAutomationFile)
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                string joinedContinuations = Regex.Replace(text, @"(?:\\|`)\s*\r?\n\s*", " ");
                return recursiveGit.IsMatch(joinedContinuations) || recursiveCheckout.IsMatch(joinedContinuations);
            })
            .ToArray();

        offenders.ShouldBeEmpty(
            $"Nested submodule initialization is forbidden. Offending files: {string.Join(", ", offenders)}");
    }

    private static IEnumerable<string> GovernedDirectories()
        => Directory.GetDirectories(ModuleLayout.ModuleRoot, "*", SearchOption.AllDirectories)
            .Where(IsGovernedPath);

    private static IEnumerable<string> GovernedFiles(string pattern)
        => Directory.GetFiles(ModuleLayout.ModuleRoot, pattern, SearchOption.AllDirectories)
            .Where(IsGovernedPath)
            .Where(path => !_guardFiles.Contains(
                Path.GetRelativePath(ModuleLayout.ModuleRoot, path).Replace('\\', '/'),
                StringComparer.OrdinalIgnoreCase));

    private static bool IsGovernedPath(string path)
    {
        string relative = Path.GetRelativePath(ModuleLayout.ModuleRoot, path).Replace('\\', '/');
        return !relative.StartsWith("references/", StringComparison.OrdinalIgnoreCase)
            && !relative.StartsWith("_bmad", StringComparison.OrdinalIgnoreCase)
            && !relative.StartsWith(".git/", StringComparison.OrdinalIgnoreCase)
            && !relative.StartsWith("artifacts/", StringComparison.OrdinalIgnoreCase)
            && !relative.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            && !relative.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOwnedTextFile(string path)
        => Path.GetExtension(path).ToLowerInvariant() is ".cs" or ".csproj" or ".config" or ".json" or ".props"
            or ".ps1" or ".py" or ".sh" or ".slnx" or ".targets" or ".xml" or ".yaml" or ".yml";

    private static bool IsAutomationFile(string path)
        => Path.GetExtension(path).ToLowerInvariant() is ".yaml" or ".yml" or ".ps1" or ".py" or ".sh";
}
