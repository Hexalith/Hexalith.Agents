using System;

using Hexalith.Agents.Contracts.Operations;

using Hexalith.EventStore.Client.Gateway;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Translates an EventStore gateway failure into a safe public operation code (Story 5.2 AC1, AC3, AC4). Only the
/// coarse category crosses the boundary: no status text, ProblemDetails detail, stream name, revision, tenant echo,
/// or stack trace ever reaches a caller.
/// </summary>
public static class AgentCommandDispatchFailure
{
    /// <summary>Maps a dispatch exception to the safe public error code a caller may see.</summary>
    /// <param name="exception">The exception raised while dispatching.</param>
    /// <returns>The safe public error code.</returns>
    public static AgentOperationErrorCode Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is EventStoreGatewayException gateway
            ? gateway.StatusCode switch
            {
                400 or 422 => AgentOperationErrorCode.ValidationFailed,
                401 or 403 => AgentOperationErrorCode.NotAuthorized,
                404 => AgentOperationErrorCode.NotFound,
                409 => AgentOperationErrorCode.Conflict,
                412 or 428 => AgentOperationErrorCode.Stale,
                _ => AgentOperationErrorCode.Unavailable,
            }
            : AgentOperationErrorCode.Unavailable;
    }
}
