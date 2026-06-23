namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

using Shouldly;

/// <summary>
/// Build-contract conformance guard (AC1). The root <c>Directory.Build.props</c>,
/// <c>Directory.Packages.props</c>, and <c>global.json</c> must keep enforcing the module's build contract:
/// <c>net10.0</c>, C# 14, nullable, implicit usings, warnings-as-errors, Central Package Management, and a
/// pinned SDK. Guards against a silent regression (e.g. someone disabling warnings-as-errors) that a clean
/// build alone would not surface.
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
    }

    [Fact]
    public void RootPackagesPropsShouldEnableCentralPackageManagement()
    {
        XDocument props = XDocument.Load(ModuleLayout.RootFile("Directory.Packages.props"));

        PropertyValue(props, "ManagePackageVersionsCentrally").ShouldBe("true");
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
}
