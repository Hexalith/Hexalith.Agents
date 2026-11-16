using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Minimal browser session-storage interop boundary for persistence tests.</summary>
public sealed class InMemorySessionStorageJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    /// <summary>Gets stored values for safe-payload assertions.</summary>
    public IDictionary<string, string> Values => _values;

    /// <summary>Gets or sets the synthetic browser read failure.</summary>
    public JSException? ReadFailure { get; set; }

    /// <summary>Gets or sets whether browser writes are denied.</summary>
    public bool RejectWrites { get; set; }

    /// <summary>Gets or sets a suspended read boundary.</summary>
    public Func<Task>? Reading { get; set; }

    /// <summary>Gets or sets a suspended write boundary after delivery to the captured key.</summary>
    public Func<Task>? Written { get; set; }

    /// <inheritdoc />
    [SuppressMessage("Usage", "BL0016", Justification = "This in-memory test double forwards to a dictionary and deliberately propagates synthetic failures to the tested caller; no browser interop occurs.")]
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    /// <inheritdoc />
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string key = (string)args[0]!;
        if (identifier == "sessionStorage.getItem")
        {
            if (ReadFailure is not null)
            {
                throw ReadFailure;
            }

            _values.TryGetValue(key, out string? value);
            if (Reading is { } reading)
            {
                await reading();
            }
            return (TValue)(object?)value!;
        }

        if (identifier == "sessionStorage.setItem")
        {
            if (RejectWrites)
            {
                throw new JSException("Session storage writes denied.");
            }

            _values[key] = (string)args[1]!;
            if (Written is { } written)
            {
                await written();
            }
            return default!;
        }

        throw new InvalidOperationException("Unexpected JavaScript call.");
    }
}
