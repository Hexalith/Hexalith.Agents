namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        ["Hexalith.Agents.EventStore"] = "src/Hexalith.Agents.EventStore/Hexalith.Agents.EventStore.csproj",
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
            "scripts/verify-nuget-publication.py",
            "scripts/verify-release-source.py",
            "scripts/verify-github-release.py",
            "scripts/publish-release-packages.sh",
            "eng/verify-story.ps1",
            ".github/workflows/ci.yml",
            ".github/workflows/release.yml",
            ".github/workflows/commitlint.yml",
            ".github/workflows/codeql.yml",
            ".github/workflows/dependency-review.yml",
            ".github/dependabot.yml",
            ".releaserc.json",
            "package.json",
            "package-lock.json",
            "commitlint.config.mjs",
            "eng/semantic-release-plan.mjs",
        ];

        foreach (string requiredFile in requiredFiles)
        {
            File.Exists(ModuleLayout.ResolveModulePath(requiredFile))
                .ShouldBeTrue($"Story 5.1 requires release gate '{requiredFile}'.");
        }

        string workflow = File.ReadAllText(ModuleLayout.ResolveModulePath(".github/workflows/ci.yml"));
        workflow.ShouldContain("Hexalith/Hexalith.Builds/.github/workflows/domain-ci.yml@main");
        workflow.ShouldContain("test-platform: microsoft-testing-platform");
        workflow.ShouldContain("run-consumer-validation: true");
        workflow.ShouldContain("test/Hexalith.Agents.Contracts.Tests");
        workflow.ShouldContain("test/Hexalith.Agents.Client.Tests");
        workflow.ShouldContain("test/Hexalith.Agents.Tests");
        workflow.ShouldContain("test/Hexalith.Agents.Server.Tests");
        workflow.ShouldContain("test/Hexalith.Agents.UI.Tests");
        workflow.ShouldContain("name: Enforce shared package authority");
        workflow.ShouldContain("./references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1 -RepositoryRoot . -CatalogPath ./references/Hexalith.Builds/Props/Directory.Packages.props");
        workflow.ShouldContain("name: Enforce EventStore package floor");
        workflow.ShouldContain("./eng/verify-story-5.2.ps1 -PackageFloorOnly");
        workflow.ShouldContain("python3 -m unittest discover -s tests/tooling -p '*_test.py'");
        workflow.ShouldContain("git -c submodule.recurse=false submodule update --init");
        workflow.ShouldContain("timeout-minutes: 30");
        workflow.ShouldNotContain("dotnet test");
        workflow.ShouldNotContain("--recursive");
    }

    [Fact]
    public void ReleaseShouldBeManualProtectedPinnedAndCollisionFailing()
    {
        const string approvedBuildsSha = "cb91511794c8898b738d85dc6c751f82b832cbc9";
        string workflow = File.ReadAllText(ModuleLayout.ResolveModulePath(".github/workflows/release.yml"));
        string releaseConfiguration = File.ReadAllText(ModuleLayout.ResolveModulePath(".releaserc.json"));
        string publisher = File.ReadAllText(ModuleLayout.ResolveModulePath("scripts/publish-release-packages.sh"));
        using JsonDocument toolchain = JsonDocument.Parse(
            File.ReadAllText(ModuleLayout.ResolveModulePath("package.json")));
        JsonElement developmentDependencies = toolchain.RootElement.GetProperty("devDependencies");

        workflow.ShouldContain("workflow_dispatch:");
        workflow.ShouldNotContain("push:");
        workflow.ShouldContain("environment-name: production");
        workflow.ShouldContain("DISPATCH_REF");
        workflow.ShouldContain("scripts/verify-release-source.py");
        workflow.ShouldContain("scripts/verify-github-release.py");
        workflow.ShouldContain("timeout-minutes: 45");
        workflow.ShouldContain($"domain-release.yml@{approvedBuildsSha}");
        workflow.ShouldContain($"builds-execution-sha: {approvedBuildsSha}");
        workflow.Split(approvedBuildsSha).Length.ShouldBe(3);
        workflow.ShouldContain("expected-package-count: 6");
        workflow.ShouldContain("reserved-version: ${{ needs.plan-release.outputs.version }}");
        workflow.Split("timeout-minutes: 10").Length.ShouldBe(3);
        workflow.ShouldContain("publish-containers: false");
        workflow.ShouldContain("HEXALITH_RELEASE_PUBLISH_ENABLED");
        workflow.ShouldContain("NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}");
        workflow.ShouldNotContain("secrets: inherit");
        workflow.ShouldNotContain("--skip-duplicate");

        releaseConfiguration.ShouldContain("verify-nuget-publication.py eng/release-packages.json ${nextRelease.version} --expect absent");
        releaseConfiguration.ShouldContain("test -n \\\"$NUGET_API_KEY\\\"");
        releaseConfiguration.ShouldContain("test \\\"${nextRelease.version}\\\" = \\\"$HEXALITH_RELEASE_RESERVED_VERSION\\\"");
        releaseConfiguration.ShouldContain("bash scripts/publish-release-packages.sh ${nextRelease.version}");
        releaseConfiguration.ShouldNotContain("--skip-duplicate");
        releaseConfiguration.ShouldNotContain("@semantic-release/changelog");
        releaseConfiguration.ShouldNotContain("\"@semantic-release/git\"");
        developmentDependencies.TryGetProperty("@semantic-release/changelog", out _).ShouldBeFalse();
        developmentDependencies.TryGetProperty("@semantic-release/git", out _).ShouldBeFalse();
        publisher.ShouldContain("dotnet nuget push");
        publisher.ShouldContain("--expect present");
        publisher.ShouldNotContain("--skip-duplicate");
        publisher.IndexOf("--expect absent", StringComparison.Ordinal)
            .ShouldBeLessThan(publisher.IndexOf("scripts/verify-release-source.py", StringComparison.Ordinal));
        publisher.IndexOf("scripts/verify-release-source.py", StringComparison.Ordinal)
            .ShouldBeLessThan(publisher.IndexOf("dotnet nuget push", StringComparison.Ordinal));
    }

    [Fact]
    public void FrozenReleaseShouldRemainGreenAndSkipPublicationAssertions()
    {
        string workflow = File.ReadAllText(ModuleLayout.ResolveModulePath(".github/workflows/release.yml"));

        workflow.ShouldContain("if [ \"${HEXALITH_RELEASE_PUBLISH_ENABLED-}\" = \"true\" ]");
        workflow.ShouldContain("publish-enabled=false");
        workflow.ShouldContain("Release publication frozen");
        workflow.ShouldContain("needs.verify-source.outputs.publish-enabled == 'true'");
        workflow.ShouldContain("if: needs.verify-source.outputs.publish-enabled == 'true'");
        workflow.ShouldContain("needs.plan-release.outputs.release-required == 'true'");
        workflow.ShouldContain("publication and post-publication assertions will be skipped");
    }

    [Fact]
    public void DependencyAutomationAndCodeQlShouldCoverTheReleaseToolchain()
    {
        string commitlint = File.ReadAllText(ModuleLayout.ResolveModulePath("commitlint.config.mjs"));
        string dependabot = File.ReadAllText(ModuleLayout.ResolveModulePath(".github/dependabot.yml"));
        string codeQl = File.ReadAllText(ModuleLayout.ResolveModulePath(".github/workflows/codeql.yml"));

        commitlint.ShouldContain("'chore'");
        dependabot.Split("prefix: \"chore(deps)\"").Length.ShouldBe(3);
        dependabot.ShouldContain("prefix: \"ci(deps)\"");
        codeQl.ShouldContain("languages: csharp,javascript-typescript");
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
        packScript.ShouldNotContain("NuGetAudit=false");

        string publicationVerifier = File.ReadAllText(
            ModuleLayout.ResolveModulePath("scripts/verify-nuget-publication.py"));
        publicationVerifier.ShouldContain("HTTPError");
        publicationVerifier.ShouldContain("status == 404");
        publicationVerifier.ShouldContain("TransientProbeError");
        publicationVerifier.ShouldContain("publication collision");
        publicationVerifier.ShouldContain("retry exhaustion");

        string verifier = File.ReadAllText(ModuleLayout.ResolveModulePath("eng/verify-story.ps1"));
        verifier.ShouldContain("Unrelated.Package.1.0.0.nupkg");
        verifier.ShouldContain("Package automation deleted an unrelated caller-owned archive.");
        (string Path, int Debug, int Release)[] expectedTestFloors =
        [
            ("test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj", 529, 529),
            ("test/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj", 6, 6),
            ("test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj", 787, 787),
            ("test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj", 573, 549),
            ("test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj", 1073, 1073),
        ];
        foreach ((string testProject, int debugMinimum, int releaseMinimum) in expectedTestFloors)
        {
            string policyPattern =
                $"Path\\s*=\\s*'{Regex.Escape(testProject)}'\\s+" +
                $"DebugMinimumExpectedTests\\s*=\\s*{debugMinimum}\\s+" +
                $"ReleaseMinimumExpectedTests\\s*=\\s*{releaseMinimum}";
            Regex.IsMatch(verifier, policyPattern, RegexOptions.CultureInvariant)
                .ShouldBeTrue($"Story 5.1 must retain the Debug and Release minimum test floors for '{testProject}'.");
        }

        verifier.ShouldContain(
            "dotnet test --project $testProject.Path -c Debug --no-build -p:UseHexalithProjectReferences=true --minimum-expected-tests $testProject.DebugMinimumExpectedTests --fail-skips on");
        verifier.ShouldContain(
            "dotnet test --project $testProject.Path -c Release --no-build -p:UseHexalithProjectReferences=false --minimum-expected-tests $testProject.ReleaseMinimumExpectedTests --fail-skips on");
        verifier.Split("--fail-skips").Length.ShouldBe(3);
        verifier.Split("--minimum-expected-tests").Length.ShouldBe(3);
    }
}
