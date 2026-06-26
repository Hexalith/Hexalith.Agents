namespace Hexalith.Agents.Server.Tests;

using System.IO;
using System.Linq;

/// <summary>
/// Shared file-system facts for the module-conformance guard tests. Locates the <c>Hexalith.Agents</c>
/// workspace root from the test output directory and enumerates the module's project files,
/// excluding build output. Centralizes the discovery logic the
/// structural / boundary / centralization guards share.
/// </summary>
internal static class ModuleLayout
{
    /// <summary>Absolute path to the workspace root that owns the solution, <c>src/</c>, and <c>test/</c>.</summary>
    internal static string WorkspaceRoot { get; } = FindWorkspaceRoot();

    /// <summary>Absolute path to the module root (the directory containing <c>Hexalith.Agents.slnx</c>).</summary>
    internal static string ModuleRoot { get; } = WorkspaceRoot;

    /// <summary>Absolute path to the production source tree.</summary>
    internal static string SourceRoot { get; } = FindSourceRoot();

    /// <summary>Absolute path to the workspace test tree.</summary>
    internal static string TestsRoot { get; } = Path.Combine(WorkspaceRoot, "test");

    /// <summary>Every module <c>*.csproj</c>, excluding <c>bin/</c> and <c>obj/</c> output.</summary>
    internal static string[] ProjectFiles { get; } = new[] { SourceRoot, TestsRoot }
        .Where(Directory.Exists)
        .SelectMany(root => Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories))
        .Where(path => !IsUnderBuildOutput(path))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    /// <summary>Returns the absolute path of a file at the module root.</summary>
    internal static string RootFile(string fileName) => Path.Combine(ModuleRoot, fileName);

    /// <summary>Returns the absolute path of a source project file.</summary>
    internal static string SourceProjectFile(string project)
        => Path.Combine(SourceRoot, project, $"{project}.csproj");

    /// <summary>Returns the absolute path of a module-relative path, respecting the moved source tree.</summary>
    internal static string ResolveModulePath(string relativePath)
    {
        string normalized = relativePath.Replace('\\', '/');

        if (normalized.StartsWith("src/", StringComparison.Ordinal))
        {
            return Path.Combine(SourceRoot, normalized["src/".Length..].Replace('/', Path.DirectorySeparatorChar));
        }

        if (normalized.StartsWith("test/", StringComparison.Ordinal))
        {
            return Path.Combine(TestsRoot, normalized["test/".Length..].Replace('/', Path.DirectorySeparatorChar));
        }

        return Path.Combine(ModuleRoot, normalized.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>True when the path lives under a <c>bin/</c> or <c>obj/</c> build-output directory.</summary>
    internal static bool IsUnderBuildOutput(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    private static string FindWorkspaceRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Hexalith.Agents.slnx"))
                && Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "test")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the agents workspace root from the test output directory.");
    }

    private static string FindSourceRoot()
    {
        string workspaceSourceRoot = Path.Combine(WorkspaceRoot, "src");
        if (Directory.Exists(workspaceSourceRoot))
        {
            return workspaceSourceRoot;
        }

        throw new InvalidOperationException("Could not locate the Hexalith.Agents source root.");
    }
}
