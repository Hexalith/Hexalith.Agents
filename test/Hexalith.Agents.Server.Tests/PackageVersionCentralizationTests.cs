namespace Hexalith.Agents.Server.Tests;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Package-version centralization guard (AC3). Hexalith.Builds must own every package version, and the shared
/// consumer-authority validator must reject consumer declarations that attempt to become a second authority.
/// </summary>
public sealed class PackageVersionCentralizationTests
{
    private const int ProcessTimeoutMilliseconds = 120_000;

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
                $"'{Path.GetFileName(projectFile)}' declares inline PackageReference Version or VersionOverride metadata. "
                + "Hexalith.Builds must own all package versions.");
        }
    }

    [Fact]
    public void SharedValidatorShouldRejectConsumerPackageVersionItems()
        => AssertSharedValidatorRejects(
            "<ItemGroup><PackageVersion Include=\"Local.Package\" Version=\"1.0.0\" /></ItemGroup>",
            string.Empty,
            string.Empty,
            "Directory.Packages.props contains consumer PackageVersion Include 'Local.Package'.");

    [Fact]
    public void SharedValidatorShouldRejectAuthoritativeVersionPropertyOverrides()
        => AssertSharedValidatorRejects(
            "<PropertyGroup><HexalithEventStoreVersion>0.0.1</HexalithEventStoreVersion></PropertyGroup>",
            string.Empty,
            string.Empty,
            "Directory.Packages.props overrides authoritative version property 'HexalithEventStoreVersion'.");

    [Fact]
    public void SharedValidatorShouldRejectPackageReferenceVersionOverrides()
        => AssertSharedValidatorRejects(
            string.Empty,
            string.Empty,
            " VersionOverride=\"0.0.1\"",
            "Consumer.csproj contains PackageReference VersionOverride metadata '0.0.1'.");

    private static bool HasInlineVersion(XElement packageReference)
        => packageReference.Attributes().Any(attribute =>
            string.Equals(attribute.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase)
            || string.Equals(attribute.Name.LocalName, "VersionOverride", StringComparison.OrdinalIgnoreCase))
            || packageReference.Elements().Any(child =>
                string.Equals(child.Name.LocalName, "Version", StringComparison.OrdinalIgnoreCase)
                || string.Equals(child.Name.LocalName, "VersionOverride", StringComparison.OrdinalIgnoreCase));

    private static void AssertSharedValidatorRejects(
        string wrapperDeclaration,
        string buildDeclaration,
        string packageReferenceMetadata,
        string expectedDiagnostic)
    {
        DirectoryInfo fixture = Directory.CreateTempSubdirectory("hexalith-agents-package-authority-");
        try
        {
            string catalogPath = ModuleLayout.RootFile(
                "references/Hexalith.Builds/Props/Directory.Packages.props");
            string escapedCatalogPath = SecurityElement.Escape(catalogPath.Replace('\\', '/'))!;
            File.WriteAllText(
                Path.Combine(fixture.FullName, "Directory.Packages.props"),
                $"""
                <Project>
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                  </PropertyGroup>
                  <Import Project="{escapedCatalogPath}" />
                  {wrapperDeclaration}
                </Project>
                """);
            File.WriteAllText(
                Path.Combine(fixture.FullName, "Directory.Build.props"),
                $"<Project>{buildDeclaration}</Project>");
            File.WriteAllText(
                Path.Combine(fixture.FullName, "Consumer.csproj"),
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Shouldly"{packageReferenceMetadata} />
                  </ItemGroup>
                </Project>
                """);

            ProcessStartInfo startInfo = new()
            {
                FileName = "pwsh",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            foreach (string argument in new[]
            {
                "-NoProfile",
                "-File",
                ModuleLayout.RootFile("references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1"),
                "-RepositoryRoot",
                fixture.FullName,
                "-CatalogPath",
                catalogPath,
            })
            {
                startInfo.ArgumentList.Add(argument);
            }

            using Process process = new() { StartInfo = startInfo };
            process.Start().ShouldBeTrue("The shared Hexalith.Builds consumer-authority validator must be executable.");
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(ProcessTimeoutMilliseconds))
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException(
                    $"The shared package-authority validator did not exit within {ProcessTimeoutMilliseconds / 1000} seconds.");
            }

            string output = standardOutput.GetAwaiter().GetResult() + standardError.GetAwaiter().GetResult();

            process.ExitCode.ShouldNotBe(0, "A consumer-owned package version must fail the shared authority validator.");
            output.ShouldContain(expectedDiagnostic);
            output.ShouldContain("Consumer package authority validation failed");
        }
        finally
        {
            fixture.Delete(true);
        }
    }
}
