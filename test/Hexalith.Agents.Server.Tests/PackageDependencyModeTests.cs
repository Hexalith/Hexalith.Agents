namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

/// <summary>Guards the mutually exclusive Debug/source and Release/package dependency modes.</summary>
public sealed class PackageDependencyModeTests
{
    private static readonly string[] _externalSourceFlags =
    [
        "HexalithCommonsFromSource",
        "HexalithConversationsFromSource",
        "HexalithEventStoreFromSource",
        "HexalithFrontComposerFromSource",
        "HexalithPartiesFromSource",
        "HexalithTenantsFromSource",
    ];

    [Fact]
    public void DirectoryBuildPropsShouldOwnOneMutuallyExclusiveModeAndEverySourceGuard()
    {
        XDocument props = XDocument.Load(ModuleLayout.RootFile("Directory.Build.props"));
        string text = props.ToString(SaveOptions.DisableFormatting);

        XElement validation = props.Descendants("Target")
            .Single(target => string.Equals((string?)target.Attribute("Name"), "ValidateDependencyMode", StringComparison.Ordinal));
        string validationText = validation.ToString(SaveOptions.DisableFormatting);
        validationText.ShouldContain("UseHexalithProjectReferences");
        validationText.ShouldContain("UseNuGetDeps");
        validationText.ShouldContain("must have opposite values");
        validationText.ShouldContain("Package dependency mode rejects forced Hexalith*FromSource=true properties");

        text.ShouldContain("'$(Configuration)' == 'Debug'");
        text.ShouldContain("FailReleaseWithHexalithProjectReferences");
        text.ShouldContain("FailPackWithHexalithProjectReferences");
        foreach (string flag in _externalSourceFlags)
        {
            validationText.ShouldContain($"$({flag})");
            text.ShouldContain($"$({flag})");
        }

        foreach (string dependency in new[] { "Commons", "Conversations", "EventStore", "FrontComposer", "Parties", "Tenants" })
        {
            text.ShouldContain($"references/Hexalith.{dependency}");
            text.ShouldContain($"git submodule update --init -- references/Hexalith.{dependency}");
        }
    }

    [Fact]
    public void EveryOwnedProjectAndSolutionEntryShouldUseTheCompleteConditionalExternalGraph()
    {
        string[] ownedProjects = ModuleLayout.ProjectFiles
            .Select(path => Path.GetRelativePath(ModuleLayout.ModuleRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        string[] solutionProjects = XDocument.Load(ModuleLayout.RootFile("Hexalith.Agents.slnx"))
            .Descendants("Project")
            .Select(project => (string?)project.Attribute("Path"))
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!.Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        solutionProjects.ShouldBe(ownedProjects, ignoreOrder: false);

        foreach (string projectPath in ModuleLayout.ProjectFiles)
        {
            XDocument project = XDocument.Load(projectPath);
            XElement[] externalProjects = project.Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "ProjectReference", StringComparison.OrdinalIgnoreCase))
                .Where(element => ((string?)element.Attribute("Include") ?? string.Empty).Contains("$(Hexalith", StringComparison.Ordinal))
                .ToArray();

            foreach (XElement projectReference in externalProjects)
            {
                string include = (string)projectReference.Attribute("Include")!;
                string target = Path.GetFileNameWithoutExtension(include.Replace('\\', '/'));
                string condition = EffectiveCondition(projectReference);
                Match match = Regex.Match(condition, @"\$\((Hexalith[A-Za-z]+FromSource)\)");
                match.Success.ShouldBeTrue(
                    $"External source reference '{include}' in '{projectPath}' must be guarded by its FromSource flag.");
                condition.ShouldContain("== 'true'");
                string sourceFlag = match.Groups[1].Value;

                bool hasPackageCounterpart = project.Descendants()
                    .Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.OrdinalIgnoreCase))
                    .Any(element => string.Equals((string?)element.Attribute("Include"), target, StringComparison.OrdinalIgnoreCase)
                        && EffectiveCondition(element).Contains($"$({sourceFlag})", StringComparison.Ordinal)
                        && EffectiveCondition(element).Contains("!= 'true'", StringComparison.Ordinal));

                if (target.StartsWith("Hexalith.Conversations", StringComparison.Ordinal))
                {
                    bool hasExplicitPackageRemoval = project.Descendants()
                        .Where(element => string.Equals(element.Name.LocalName, "Compile", StringComparison.OrdinalIgnoreCase))
                        .Any(element => !string.IsNullOrWhiteSpace((string?)element.Attribute("Remove"))
                            && EffectiveCondition(element).Contains($"$({sourceFlag})", StringComparison.Ordinal)
                            && EffectiveCondition(element).Contains("!= 'true'", StringComparison.Ordinal));
                    hasExplicitPackageRemoval.ShouldBeTrue(
                        $"Source-only Conversations edge '{include}' in '{projectPath}' needs explicit package-mode Compile removal.");
                }
                else
                {
                    hasPackageCounterpart.ShouldBeTrue(
                        $"External source reference '{include}' in '{projectPath}' has no package-mode counterpart '{target}'.");
                }
            }
        }
    }

    [Fact]
    public void TestingPackageShouldNotReferenceTheNonPackableServer()
    {
        string project = File.ReadAllText(ModuleLayout.SourceProjectFile("Hexalith.Agents.Testing"));
        project.ShouldNotContain("Hexalith.Agents.Server");
    }

    private static string EffectiveCondition(XElement element)
        => string.Join(
            " and ",
            element.AncestorsAndSelf()
                .Select(ancestor => (string?)ancestor.Attribute("Condition"))
                .Where(condition => !string.IsNullOrWhiteSpace(condition)));
}
