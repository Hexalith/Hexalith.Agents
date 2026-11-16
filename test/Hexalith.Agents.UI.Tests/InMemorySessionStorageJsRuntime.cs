using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Minimal browser session-storage interop boundary for persistence tests.</summary>
public sealed class InMemorySessionStorageJsRuntime : IJSRuntime, IJSObjectReference
{
    private readonly IDictionary<string, string> _values;

    /// <summary>Creates a separate runtime optionally sharing one browser storage scope.</summary>
    public InMemorySessionStorageJsRuntime(IDictionary<string, string>? values = null)
        => _values = values ?? new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>Gets or sets the boundary before the browser acquires its mutation lock.</summary>
    public Func<Task>? Comparing { get; set; }

    /// <summary>Gets the number of stale conditional writes refused by the browser.</summary>
    public int Conflicts { get; private set; }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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
        cancellationToken.ThrowIfCancellationRequested();
        if (identifier == "import")
        {
            if ((string)args[0]! != "./_content/Hexalith.Agents.UI/pendingProviderCommands.js")
            {
                throw new InvalidOperationException("Unexpected module import.");
            }
            return (TValue)(object)this;
        }
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

        if (identifier == "compareAndSet")
        {
            if (RejectWrites)
            {
                throw new JSException("Session storage writes denied.");
            }

            if (Comparing is { } comparing)
            {
                await comparing();
            }
            cancellationToken.ThrowIfCancellationRequested();
            lock (_values)
            {
                _values.TryGetValue(key, out string? current);
                if (current != (string?)args[1])
                {
                    Conflicts++;
                    return (TValue)(object)false;
                }
                _values[key] = (string)args[2]!;
            }
            if (Written is { } written)
            {
                await written();
            }
            return (TValue)(object)true;
        }

        throw new InvalidOperationException("Unexpected JavaScript call.");
    }
}
