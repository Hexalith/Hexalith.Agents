namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Public automation result envelope for Agents operations that return a typed payload.
/// </summary>
/// <typeparam name="T">The safe public payload type.</typeparam>
/// <param name="Status">The canonical outer operation status.</param>
/// <param name="Value">The safe payload, present only for successful or intentionally degraded outcomes.</param>
/// <param name="Error">The safe error payload for failed outcomes.</param>
/// <param name="Page">Optional page metadata for list-shaped payloads.</param>
/// <param name="CorrelationId">Optional correlation id echoed from sanitized operation metadata.</param>
public sealed record AgentOperationResult<T>(
    AgentOperationStatus Status,
    T? Value = default,
    AgentOperationError? Error = null,
    AgentOperationPage? Page = null,
    string? CorrelationId = null)
{
    /// <summary>Gets a value indicating whether the result is a successful terminal result.</summary>
    public bool IsSuccess => Status == AgentOperationStatus.Succeeded;

    /// <summary>Creates a successful typed result.</summary>
    public static AgentOperationResult<T> Succeeded(T value, AgentOperationPage? page = null, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new AgentOperationResult<T>(AgentOperationStatus.Succeeded, value, null, page, correlationId);
    }

    /// <summary>Creates a pending typed result that must not be treated as terminal success.</summary>
    public static AgentOperationResult<T> Pending(string? correlationId = null)
        => new(AgentOperationStatus.Pending, default, null, null, correlationId);

    /// <summary>Creates a failed typed result from the supplied safe code.</summary>
    public static AgentOperationResult<T> Failed(AgentOperationErrorCode code, string? correlationId = null)
        => new(AgentOperationResult.StatusFor(code), default, AgentOperationError.FromCode(code), null, correlationId);

    /// <summary>Creates a safe unavailable typed result.</summary>
    public static AgentOperationResult<T> Unavailable(string? correlationId = null)
        => Failed(AgentOperationErrorCode.Unavailable, correlationId);
}
