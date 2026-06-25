namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// Safe public automation error payload for Agents API/client calls.
/// </summary>
/// <param name="Code">The safe error class.</param>
/// <param name="Message">A stable display/automation message derived from <paramref name="Code"/>.</param>
public sealed record AgentOperationError(
    AgentOperationErrorCode Code,
    string Message)
{
    /// <summary>Creates a safe error from the supplied code.</summary>
    /// <param name="code">The safe error class.</param>
    /// <returns>A safe error payload.</returns>
    public static AgentOperationError FromCode(AgentOperationErrorCode code)
        => new(code, MessageFor(code));

    /// <summary>Creates a safe unavailable error without exposing exception text.</summary>
    /// <returns>A safe unavailable error.</returns>
    public static AgentOperationError Unavailable()
        => FromCode(AgentOperationErrorCode.Unavailable);

    private static string MessageFor(AgentOperationErrorCode code)
        => code switch
        {
            AgentOperationErrorCode.NotAuthorized => "The operation is not authorized.",
            AgentOperationErrorCode.ValidationFailed => "The operation request is invalid.",
            AgentOperationErrorCode.NotFound => "The requested resource was not found.",
            AgentOperationErrorCode.Conflict => "The operation conflicts with the current state.",
            AgentOperationErrorCode.Stale => "The operation requires fresher state.",
            AgentOperationErrorCode.Unavailable => "The operation is currently unavailable.",
            AgentOperationErrorCode.Rejected => "The operation was rejected.",
            AgentOperationErrorCode.Blocked => "The operation is blocked.",
            _ => "The operation failed.",
        };
}
