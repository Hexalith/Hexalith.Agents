namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Public automation result envelope for Agents operations that do not return a typed payload.
/// </summary>
/// <param name="Status">The canonical outer operation status.</param>
/// <param name="Error">The safe error payload for failed outcomes.</param>
/// <param name="CorrelationId">Optional correlation id echoed from sanitized operation metadata.</param>
public sealed record AgentOperationResult(
    AgentOperationStatus Status,
    AgentOperationError? Error = null,
    string? CorrelationId = null)
{
    /// <summary>Gets a value indicating whether the result is a successful terminal result.</summary>
    public bool IsSuccess => Status == AgentOperationStatus.Succeeded;

    /// <summary>Creates a successful result.</summary>
    public static AgentOperationResult Succeeded(string? correlationId = null)
        => new(AgentOperationStatus.Succeeded, null, correlationId);

    /// <summary>Creates a pending result that must not be treated as terminal success.</summary>
    public static AgentOperationResult Pending(string? correlationId = null)
        => new(AgentOperationStatus.Pending, null, correlationId);

    /// <summary>Creates a failed result from the supplied safe code.</summary>
    public static AgentOperationResult Failed(AgentOperationErrorCode code, string? correlationId = null)
        => new(StatusFor(code), AgentOperationError.FromCode(code), correlationId);

    /// <summary>Creates a safe unavailable result.</summary>
    public static AgentOperationResult Unavailable(string? correlationId = null)
        => Failed(AgentOperationErrorCode.Unavailable, correlationId);

    internal static AgentOperationStatus StatusFor(AgentOperationErrorCode code)
        => code switch
        {
            AgentOperationErrorCode.NotAuthorized => AgentOperationStatus.NotAuthorized,
            AgentOperationErrorCode.ValidationFailed => AgentOperationStatus.ValidationFailed,
            AgentOperationErrorCode.NotFound => AgentOperationStatus.NotFound,
            AgentOperationErrorCode.Conflict => AgentOperationStatus.Conflict,
            AgentOperationErrorCode.Stale => AgentOperationStatus.Stale,
            AgentOperationErrorCode.Unavailable => AgentOperationStatus.Unavailable,
            AgentOperationErrorCode.Rejected => AgentOperationStatus.Rejected,
            AgentOperationErrorCode.Blocked => AgentOperationStatus.Blocked,
            _ => AgentOperationStatus.Unknown,
        };
}
