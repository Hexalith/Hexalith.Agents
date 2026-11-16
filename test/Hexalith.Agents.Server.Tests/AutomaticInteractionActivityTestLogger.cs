using Hexalith.Agents.Server.Application.Activities;
using Microsoft.Extensions.Logging;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Snapshots safe structured log values while the generated logging state is still in scope.</summary>
internal sealed class AutomaticInteractionActivityTestLogger : ILogger<ExecuteAutomaticInteractionStepActivity>
{
    /// <summary>Gets the structured fields copied during the logging call.</summary>
    internal List<KeyValuePair<string, object?>> Fields { get; } = [];
    /// <summary>Gets the exception supplied to the logger.</summary>
    internal Exception? Failure { get; private set; }
    /// <summary>Gets the formatted message copied during the logging call.</summary>
    internal string? Message { get; private set; }
    /// <summary>Gets the number of captured log entries.</summary>
    internal int Count { get; private set; }
    /// <summary>Gets or sets whether the test sink fails while checking its level.</summary>
    internal bool ThrowWhileEnabling { get; set; }
    /// <summary>Gets or sets whether the test sink fails while recording an entry.</summary>
    internal bool ThrowWhileLogging { get; set; }
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => ThrowWhileEnabling ? throw new InvalidOperationException("logger-payload-poison") : true;
    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (ThrowWhileLogging) { throw new InvalidOperationException("logger-payload-poison"); }
        Count++; Failure = exception; Message = formatter(state, exception);
        Fields.AddRange((IEnumerable<KeyValuePair<string, object?>>)state!);
    }
}
