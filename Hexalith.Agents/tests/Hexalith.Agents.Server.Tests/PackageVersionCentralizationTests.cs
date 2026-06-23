namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Linq;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Package-version centralization guard (AC3). Central Package Management (Directory.Packages.props) must
/// own every package version: no <c>.csproj</c> in the module may carry an inline
/// <c>&lt;PackageReference ... Version="..."&gt;</c> (attribute or child element).
/// </summary>
public sealed class PackageVersionCentralizationTests
{
    [Fact]
    public void NoProjectShouldDeclareInlinePackageReferenceVersions()
    {
        string[] projectFiles = ModuleLayout.ProjectFiles;

        projectFiles.ShouldNotBeEmpty("Expected to discover the module's .csproj files for the centralization guard.");

        foreach (string projectFile in projectFiles)
        {
            XDocument document = XDocument.Load(projectFile);

            bool hasInlineVersion = document
                .Descendants()
                .Where(element => string.Equals(element.Name.LocalName, "PackageReference", StringComparison.OrdinalIgnoreCase))
                .Any(HasInlineVersion);

            hasInlineVersion.ShouldBeFalse(
                $"'{Path.GetFileName(projectFile)}' declares an inline PackageReference Version. "
                + "CPM (Directory.Packages.props) must own all package versions.");
        }
    }

    private static bool HasInlineVersion(XElement packageReference)
        => packageReference.Attributes().Any(attribute => string.Equals(attribute.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase))
            || packageReference.Elements().Any(child => string.Equals(child.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase));
}
