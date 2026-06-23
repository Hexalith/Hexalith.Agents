namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Linq;

/// <summary>
/// Shared file-system facts for the module-conformance guard tests. Locates the <c>Hexalith.Agents</c>
/// module root (the directory holding <c>Hexalith.Agents.slnx</c>) from the test output directory and
/// enumerates the module's project files, excluding build output. Centralizes the discovery logic the
/// structural / boundary / centralization guards share.
/// </summary>
internal static class ModuleLayout
{
    /// <summary>Absolute path to the module root (the directory containing <c>Hexalith.Agents.slnx</c>).</summary>
    internal static string ModuleRoot { get; } = FindModuleRoot();

    /// <summary>Every <c>*.csproj</c> under the module, excluding <c>bin/</c> and <c>obj/</c> output.</summary>
    internal static string[] ProjectFiles { get; } = Directory
        .GetFiles(ModuleRoot, "*.csproj", SearchOption.AllDirectories)
        .Where(path => !IsUnderBuildOutput(path))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>Returns the absolute path of a file at the module root.</summary>
    internal static string RootFile(string fileName) => Path.Combine(ModuleRoot, fileName);

    /// <summary>True when the path lives under a <c>bin/</c> or <c>obj/</c> build-output directory.</summary>
    internal static bool IsUnderBuildOutput(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindModuleRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Agents.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the Hexalith.Agents module root (Hexalith.Agents.slnx) from the test output directory.");
    }
}
