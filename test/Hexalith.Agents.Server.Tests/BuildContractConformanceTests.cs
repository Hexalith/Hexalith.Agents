namespace Hexalith.Agents.Server.Tests;

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Build-contract conformance guard (AC1). The root <c>Directory.Build.props</c>,
/// <c>Directory.Packages.props</c>, and <c>global.json</c> must keep enforcing the module's build contract,
/// including import-only shared-catalog ownership in root, sibling, and parent layouts.
/// </summary>
public sealed class BuildContractConformanceTests
{
    [Fact]
    public void RootBuildPropsShouldEnforceLanguageAndBuildContract()
    {
        XDocument props = XDocument.Load(ModuleLayout.RootFile("Directory.Build.props"));

        PropertyValue(props, "TargetFramework").ShouldBe("net10.0");
        PropertyValue(props, "LangVersion").ShouldBe("14");
        PropertyValue(props, "Nullable").ShouldBe("enable");
        PropertyValue(props, "ImplicitUsings").ShouldBe("enable");
        PropertyValue(props, "TreatWarningsAsErrors").ShouldBe("true");
        PropertyValue(props, "NuGetAudit").ShouldBe("true");
        PropertyValue(props, "NuGetAuditMode").ShouldBe("all");
        string warningsNotAsErrors = PropertyValue(props, "WarningsNotAsErrors")!;
        warningsNotAsErrors.ShouldContain("NU1901");
        warningsNotAsErrors.ShouldContain("NU1902");
        warningsNotAsErrors.ShouldContain("NU1903");
        warningsNotAsErrors.ShouldContain("NU1904");
    }

    [Fact]
    public void RootPackagesPropsShouldBeAnImportOnlySharedCatalogWrapper()
    {
        XDocument props = XDocument.Load(ModuleLayout.RootFile("Directory.Packages.props"));

        PropertyValue(props, "ManagePackageVersionsCentrally").ShouldBe("true");
        PropertyValue(props, "CentralPackageTransitivePinningEnabled").ShouldBe("true");
        props.Descendants().ShouldNotContain(
            element => string.Equals(element.Name.LocalName, "PackageVersion", StringComparison.OrdinalIgnoreCase));
        PropertyValue(props, "HexalithEventStoreVersion").ShouldBeNull();
        XElement[] imports = props
            .Descendants()
            .Where(element => string.Equals(element.Name.LocalName, "Import", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        imports.ShouldNotBeEmpty();
        foreach (XElement import in imports)
        {
            (import.Attribute("Project")?.Value ?? string.Empty).ShouldContain("BuildPackageProps");
        }

        props.Root!.Value.ShouldContain("Hexalith.Builds/Props/Directory.Packages.props");
    }

    [Fact]
    public void RootBuildPropsShouldRequireTheLoadedSharedCatalogMarker()
    {
        XDocument props = XDocument.Load(ModuleLayout.RootFile("Directory.Build.props"));

        props.Root!.Attribute("InitialTargets")?.Value.ShouldContain("CheckBuildCatalog");
        XElement checkTarget = props
            .Descendants()
            .Single(element =>
                string.Equals(element.Name.LocalName, "Target", StringComparison.Ordinal)
                && string.Equals(element.Attribute("Name")?.Value, "CheckBuildCatalog", StringComparison.Ordinal));
        checkTarget.Attribute("BeforeTargets")?.Value.ShouldContain("GenerateNuspec;Pack;Publish");
        XElement error = checkTarget
            .Elements()
            .Single(element => string.Equals(element.Name.LocalName, "Error", StringComparison.Ordinal));
        error.Attribute("Condition")?.Value.ShouldContain("HexalithVersionsLoaded");
        error.Attribute("Text")?.Value.ShouldContain("Hexalith.Builds");
        error.Attribute("Text")?.Value.ShouldContain("git submodule update --init -- references/Hexalith.Builds");
    }

    [Fact]
    public void AgentsUiShouldScopeItsPrereleaseDependencyWarningSuppressionToThePackableProject()
    {
        XDocument project = XDocument.Load(ModuleLayout.SourceProjectFile("Hexalith.Agents.UI"));
        string noWarn = PropertyValue(project, "NoWarn")
            ?? throw new InvalidOperationException("Hexalith.Agents.UI must declare a scoped NoWarn value.");

        noWarn.ShouldContain("NU5104");
        project.ToString(SaveOptions.DisableFormatting).ShouldContain("Fluent UI v5 RC");
    }

    [Fact]
    public void RootCheckoutShouldLoadTheSharedEventStoreVersion()
    {
        using JsonDocument evaluation = EvaluateMsBuild(
            ModuleLayout.SourceProjectFile("Hexalith.Agents.EventStore"),
            "-getProperty:HexalithVersionsLoaded",
            "-getProperty:HexalithEventStoreVersion",
            "-p:Configuration=Release",
            "-p:UseHexalithProjectReferences=false",
            "-p:NuGetAudit=false",
            "/nr:false");

        JsonElement properties = evaluation.RootElement.GetProperty("Properties");
        properties.GetProperty("HexalithVersionsLoaded").GetString().ShouldBe("true");
        properties.GetProperty("HexalithEventStoreVersion").GetString().ShouldBe("3.106.0");
    }

    [Theory]
    [InlineData("true", "false", "true")]
    [InlineData("false", "true", "false")]
    public void DebugRestoreShouldUsePackageModeInGitHubActionsAndSourceModeLocally(
        string githubActions,
        string expectedProjectReferences,
        string expectedNuGetDependencies)
    {
        using JsonDocument evaluation = EvaluateMsBuild(
            ModuleLayout.SourceProjectFile("Hexalith.Agents.Contracts"),
            "-getProperty:UseHexalithProjectReferences",
            "-getProperty:UseNuGetDeps",
            "-p:Configuration=Debug",
            $"-p:GITHUB_ACTIONS={githubActions}",
            "-p:NuGetAudit=false",
            "/nr:false");

        JsonElement properties = evaluation.RootElement.GetProperty("Properties");
        properties.GetProperty("UseHexalithProjectReferences").GetString().ShouldBe(expectedProjectReferences);
        properties.GetProperty("UseNuGetDeps").GetString().ShouldBe(expectedNuGetDependencies);
    }

    [Theory]
    [InlineData("3.104.0", "requires Hexalith.EventStore 3.106.0 or later")]
    [InlineData("3.106.0-alpha", "requires Hexalith.EventStore 3.106.0 or later")]
    [InlineData("not-a-version", "is not a valid SemVer 2 version")]
    [InlineData(null, "No effective package-mode Hexalith.EventStore PackageVersion rows were found")]
    public void EventStorePackageFloorShouldFailClosedForInvalidPackageVersionRows(
        string? packageVersion,
        string expectedDiagnostic)
    {
        DirectoryInfo fixture = Directory.CreateTempSubdirectory("hexalith-agents-eventstore-floor-");
        try
        {
            string packageVersionDeclaration = packageVersion is null
                ? """<PackageVersion Include="Example.Package" Version="1.0.0" />"""
                : $"""<PackageVersion Include="Hexalith.EventStore.Contracts" Version="{packageVersion}" />""";
            string projectPath = Path.Combine(fixture.FullName, "PackageFloorFixture.proj");
            File.WriteAllText(
                projectPath,
                $"""
                <Project>
                  <ItemGroup>
                    {packageVersionDeclaration}
                  </ItemGroup>
                </Project>
                """);

            string output = RunProcess(
                "pwsh",
                out int exitCode,
                "-NoProfile",
                "-File",
                ModuleLayout.RootFile("eng/verify-story-5.2.ps1"),
                "-PackageFloorOnly",
                "-PackageFloorProjectPath",
                projectPath);

            exitCode.ShouldNotBe(0);
            output.ShouldContain(expectedDiagnostic);
        }
        finally
        {
            fixture.Delete(true);
        }
    }

    [Fact]
    public void WrapperShouldLoadTheSharedCatalogFromASiblingCheckout()
        => AssertWrapperLayoutLoadsCatalog("../Hexalith.Builds/Props/Directory.Packages.props");

    [Fact]
    public void WrapperShouldLoadTheSharedCatalogFromAParentReferencesLayout()
        => AssertWrapperLayoutLoadsCatalog("../references/Hexalith.Builds/Props/Directory.Packages.props");

    [Fact]
    public void WrapperShouldLoadTheSharedCatalogFromANestedParentReferencesLayout()
        => AssertWrapperLayoutLoadsCatalog(
            "../../references/Hexalith.Builds/Props/Directory.Packages.props",
            "host/agents/Hexalith.Agents");

    [Fact]
    public void MissingSharedCatalogShouldFailWithRootOnlyInitializationGuidance()
    {
        string missingPath = Path.Combine(
            Path.GetTempPath(),
            $"missing-hexalith-builds-{Guid.NewGuid():N}",
            "Directory.Packages.props");
        string output = RunDotNet(
            out int exitCode,
            "msbuild",
            ModuleLayout.SourceProjectFile("Hexalith.Agents.Contracts"),
            "-nologo",
            "-t:CheckBuildCatalog",
            $"-p:Hexalith1BuildPackageProps={missingPath}",
            $"-p:Hexalith2BuildPackageProps={missingPath}",
            $"-p:Hexalith3BuildPackageProps={missingPath}",
            $"-p:Hexalith4BuildPackageProps={missingPath}",
            "/nr:false");

        exitCode.ShouldNotBe(0);
        output.ShouldContain("Hexalith.Builds package catalog was not loaded.");
        output.ShouldContain("git submodule update --init -- references/Hexalith.Builds");
    }

    [Theory]
    [InlineData("Pack")]
    [InlineData("Publish")]
    public void PackageTargetsShouldFailWhenSharedCatalogIsMissing(string target)
    {
        DirectoryInfo fixture = Directory.CreateTempSubdirectory("hexalith-agents-missing-catalog-");
        try
        {
            string projectPath = Path.Combine(fixture.FullName, "CatalogGuardFixture.csproj");
            File.Copy(ModuleLayout.RootFile("Directory.Build.props"), Path.Combine(fixture.FullName, "Directory.Build.props"));
            File.Copy(ModuleLayout.RootFile("Directory.Packages.props"), Path.Combine(fixture.FullName, "Directory.Packages.props"));
            File.WriteAllText(
                projectPath,
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <IsPackable>true</IsPackable>
                  </PropertyGroup>
                </Project>
                """);
            string missingPath = Path.Combine(fixture.FullName, "missing", "Directory.Packages.props");
            string output = RunDotNet(
                out int exitCode,
                "msbuild",
                projectPath,
                "-nologo",
                $"-t:{target}",
                "-p:NoBuild=true",
                "-p:Configuration=Release",
                "-p:UseHexalithProjectReferences=false",
                "-p:NuGetAudit=false",
                $"-p:Hexalith1BuildPackageProps={missingPath}",
                $"-p:Hexalith2BuildPackageProps={missingPath}",
                $"-p:Hexalith3BuildPackageProps={missingPath}",
                $"-p:Hexalith4BuildPackageProps={missingPath}",
                $"-p:PackageOutputPath={Path.Combine(fixture.FullName, "packages")}",
                $"-p:PublishDir={Path.Combine(fixture.FullName, "publish")}",
                "/nr:false");

            exitCode.ShouldNotBe(0);
            output.ShouldContain("Hexalith.Builds package catalog was not loaded.");
            output.ShouldContain("git submodule update --init -- references/Hexalith.Builds");
        }
        finally
        {
            fixture.Delete(true);
        }
    }

    [Fact]
    public void GlobalJsonShouldPinTheSdk()
    {
        using JsonDocument global = JsonDocument.Parse(File.ReadAllText(ModuleLayout.RootFile("global.json")));

        JsonElement sdk = global.RootElement.GetProperty("sdk");

        sdk.GetProperty("version").GetString().ShouldNotBeNullOrWhiteSpace();
        sdk.GetProperty("rollForward").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    private static string? PropertyValue(XDocument document, string localName)
        => document
            .Descendants()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))?
            .Value
            .Trim();

    private static void AssertWrapperLayoutLoadsCatalog(
        string catalogRelativePath,
        string repositoryRelativePath = "Hexalith.Agents")
    {
        DirectoryInfo fixture = Directory.CreateTempSubdirectory("hexalith-agents-catalog-layout-");
        try
        {
            string repositoryRoot = Path.Combine(fixture.FullName, repositoryRelativePath);
            Directory.CreateDirectory(repositoryRoot);
            string wrapperPath = Path.Combine(repositoryRoot, "Directory.Packages.props");
            File.Copy(ModuleLayout.RootFile("Directory.Packages.props"), wrapperPath);

            string catalogPath = Path.GetFullPath(Path.Combine(repositoryRoot, catalogRelativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(catalogPath)!);
            File.WriteAllText(
                catalogPath,
                """
                <Project>
                  <PropertyGroup>
                    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                    <CentralPackageVersionOverrideEnabled>false</CentralPackageVersionOverrideEnabled>
                    <HexalithVersionsLoaded>true</HexalithVersionsLoaded>
                    <HexalithEventStoreVersion>3.106.0</HexalithEventStoreVersion>
                  </PropertyGroup>
                </Project>
                """);

            using JsonDocument evaluation = EvaluateMsBuild(
                wrapperPath,
                "-getProperty:HexalithVersionsLoaded",
                "-getProperty:HexalithEventStoreVersion",
                "/nr:false");
            JsonElement properties = evaluation.RootElement.GetProperty("Properties");
            properties.GetProperty("HexalithVersionsLoaded").GetString().ShouldBe("true");
            properties.GetProperty("HexalithEventStoreVersion").GetString().ShouldBe("3.106.0");
        }
        finally
        {
            fixture.Delete(true);
        }
    }

    private static JsonDocument EvaluateMsBuild(string projectPath, params string[] arguments)
    {
        string output = RunDotNet(
            out int exitCode,
            new[] { "msbuild", projectPath, "-nologo" }.Concat(arguments).ToArray());

        exitCode.ShouldBe(0, output);
        return JsonDocument.Parse(output);
    }

    private static string RunDotNet(out int exitCode, params string[] arguments)
        => RunProcess("dotnet", out exitCode, arguments);

    private static string RunProcess(string fileName, out int exitCode, params string[] arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        process.Start().ShouldBeTrue("The .NET SDK must be available to evaluate the package wrapper.");
        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        exitCode = process.ExitCode;
        return standardOutput.GetAwaiter().GetResult() + standardError.GetAwaiter().GetResult();
    }
}
