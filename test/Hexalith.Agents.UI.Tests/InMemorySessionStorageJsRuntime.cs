using Microsoft.JSInterop;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Minimal browser session-storage interop boundary for persistence tests.</summary>
public sealed class InMemorySessionStorageJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    /// <summary>Gets stored values for safe-payload assertions.</summary>
    public IReadOnlyDictionary<string, string> Values => _values;

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string key = (string)args[0]!;
        if (identifier == "sessionStorage.getItem")
        {
            _values.TryGetValue(key, out string? value);
            return ValueTask.FromResult((TValue)(object?)value!);
        }

        if (identifier == "sessionStorage.setItem")
        {
            _values[key] = (string)args[1]!;
            return ValueTask.FromResult(default(TValue)!);
        }

        throw new InvalidOperationException("Unexpected JavaScript call.");
    }
}
