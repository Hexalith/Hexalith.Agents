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
    // The whole repository validator evaluates each project and its authoritative catalog.
    private const int RepositoryValidationTimeoutMilliseconds = 600_000;
    private const string ArchivedExclusionPrefix = "_bmad-output/implementation-artifacts/tests/owner-delivery-2026-10-08/";
    private const string ApprovedPlatformExclusionPrefix = "_bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/";

    // The only approved non-archived authority exclusions: frozen foreign Platform evidence.
    private static readonly string[] _approvedPlatformExclusions =
    [
        ApprovedPlatformExclusionPrefix + "coordinator-final-dependencies/platform/catalog/20261116T134852Z-668b8b128a6b4ff098a9ce224c0649ed/artifacts/platform-components/catalog-reader/Hexalith.Platform.CatalogBuilder.csproj",
        ApprovedPlatformExclusionPrefix + "coordinator-final-dependencies/platform/local-inputs/Hexalith.Platform/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj",
        ApprovedPlatformExclusionPrefix + "platform-corrected/platform/catalog/20261116T123849Z-724c847ff7be495aacb4b30f5f9942c3/artifacts/platform-components/catalog-reader/Hexalith.Platform.CatalogBuilder.csproj",
        ApprovedPlatformExclusionPrefix + "platform-corrected/platform/local-inputs/Hexalith.Platform/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj",
        ApprovedPlatformExclusionPrefix + "platform-final-correction/platform/catalog/20261116T124513Z-deb728e7995344b9a55088ff50964492/artifacts/platform-components/catalog-reader/Hexalith.Platform.CatalogBuilder.csproj",
        ApprovedPlatformExclusionPrefix + "platform-final-correction/platform/local-inputs/Hexalith.Platform/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj",
    ];

    [Fact]
    public async Task WholeRepositoryShouldHaveOnePackageVersionAuthorityAsync()
    {
        string[] exclusionLines = File.ReadAllLines(ModuleLayout.RootFile("eng/package-authority-exclusions.txt"));
        AssertOnlyApprovedAuthorityExclusions(exclusionLines);
        string exclusions = string.Join(';', exclusionLines
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#')));
        ProcessStartInfo startInfo = new()
        {
            FileName = "pwsh",
            WorkingDirectory = ModuleLayout.ModuleRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in new[]
        {
            "-NoProfile", "-File",
            ModuleLayout.RootFile("references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1"),
            "-RepositoryRoot", ModuleLayout.ModuleRoot,
            "-CatalogPath", ModuleLayout.RootFile("references/Hexalith.Builds/Props/Directory.Packages.props"),
            "-ExcludedPath", exclusions,
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        process.Start().ShouldBeTrue();
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(RepositoryValidationTimeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Whole-repository package authority validation timed out.");
        }

        string output = await standardOutput + await standardError;
        process.ExitCode.ShouldBe(0, output);
    }

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

    private static void AssertOnlyApprovedAuthorityExclusions(string[] lines)
    {
        string[] entries = lines
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .ToArray();

        entries.ShouldNotBeEmpty("The package authority exclusion list must not be empty.");
        entries.Distinct(StringComparer.Ordinal).Count().ShouldBe(entries.Length, "Package authority exclusions must be unique.");
        entries
            .Where(entry => !entry.StartsWith(ArchivedExclusionPrefix, StringComparison.Ordinal))
            .ShouldBe(
                _approvedPlatformExclusions,
                ignoreOrder: true,
                "Only archived evidence and the six approved Platform snapshots may be excluded from package authority validation.");
        foreach (string entry in entries)
        {
            (entry.StartsWith(ArchivedExclusionPrefix, StringComparison.Ordinal)
                || _approvedPlatformExclusions.Contains(entry, StringComparer.Ordinal))
                .ShouldBeTrue($"Package authority exclusion '{entry}' is neither archived evidence nor an approved Platform snapshot.");
            entry.EndsWith(".csproj", StringComparison.Ordinal)
                .ShouldBeTrue($"Package authority exclusion '{entry}' must name a .csproj file.");
            (entry.Contains('*') || entry.Contains('?') || entry.Split('/').Contains(".."))
                .ShouldBeFalse($"Package authority exclusion '{entry}' must not contain a wildcard or '..' segment.");
            File.Exists(ModuleLayout.RootFile(entry))
                .ShouldBeTrue($"Package authority exclusion '{entry}' must name an existing project file.");
        }
    }

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
                    <TargetFramework>net11.0</TargetFramework>
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
