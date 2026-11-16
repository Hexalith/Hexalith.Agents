using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

/// <summary>Records actual compiler and MSBuild assembly loads during an isolated, nonshared build.</summary>
internal static class StartupHook
{
    private static readonly object _gate = new();
    [ThreadStatic] private static bool _writing;
    private static string? _path;

    /// <summary>Installs the observer before the MSBuild or compiler entry point executes.</summary>
    public static void Initialize()
    {
        string directory = Environment.GetEnvironmentVariable("AGENTS_CONSUMER_LOAD_TRACE")
            ?? throw new InvalidOperationException("Consumer load observation directory is missing.");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, Environment.ProcessId + ".loads");
        Write("start", Assembly.GetEntryAssembly()?.Location ?? string.Empty, string.Empty);
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) => Observe(args.LoadedAssembly);
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Observe(assembly);
        }
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Write("complete", string.Empty, string.Empty);
    }

    private static void Observe(Assembly assembly)
    {
        if (_writing) { return; }
        _writing = true;
        try
        {
            if (assembly.IsDynamic)
            {
                Write("dynamic", assembly.FullName ?? string.Empty, string.Empty);
            }
            else if (string.IsNullOrEmpty(assembly.Location))
            {
                Write("unlocated", assembly.FullName ?? string.Empty, string.Empty);
            }
            else
            {
                string path = Path.GetFullPath(assembly.Location);
                Write("assembly", path, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant());
            }
        }
        catch (Exception exception)
        {
            Write("error", exception.GetType().FullName ?? "error", string.Empty);
        }
        finally { _writing = false; }
    }

    private static void Write(string kind, string value, string digest)
    {
        lock (_gate)
        {
            File.AppendAllText(_path!, kind + "\t" + Convert.ToBase64String(Encoding.UTF8.GetBytes(value)) + "\t" + digest + "\n");
        }
    }
}
