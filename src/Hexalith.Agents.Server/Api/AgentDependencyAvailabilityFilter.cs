using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Ports;
using Microsoft.AspNetCore.Http;

namespace Hexalith.Agents.Server.Api;

/// <summary>Blocks consuming public API paths before any client lookup/effect while a full Story 5.4 record is unavailable.</summary>
public sealed class AgentDependencyAvailabilityFilter(IAgentDependencyAvailability? availability = null) : IEndpointFilter
{
    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        IAgentDependencyAvailability gate = availability ?? new CommittedAgentDependencyAvailability();
        foreach (string record in new[] { "EXT-HOST-1", "EXT-PARTIES-1", "EXT-CONV-AI-1", "EXT-SECRETS-1" })
        {
            if (gate.Check(record) is { } blocker)
            {
                var result = new AgentOperationResult(AgentOperationStatus.Unavailable,
                    AgentOperationError.FromCode(AgentOperationErrorCode.DependencyNotAvailable) with { Dependency = blocker });
                return ValueTask.FromResult<object?>(Results.Json(result, statusCode: StatusCodes.Status503ServiceUnavailable));
            }
        }

        return next(context);
    }
}
