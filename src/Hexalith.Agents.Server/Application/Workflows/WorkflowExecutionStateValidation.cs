using System.Text.RegularExpressions;

namespace Hexalith.Agents.Server.Application.Workflows;

/// <summary>Validates every persistence-bound workflow value without accepting arbitrary text.</summary>
internal static partial class WorkflowExecutionStateValidation
{
    /// <summary>Rejects malformed ids, digests and content references before scheduling or activity persistence.</summary>
    internal static void Validate(WorkflowExecutionState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (!IsOpaqueId(state.TenantId) || !IsOpaqueId(state.ConversationId) || !Digest(state.AgentInteractionId)
            || state.RequestReference is not { } reference || reference.TenantId != state.TenantId || reference.AgentInteractionId != state.AgentInteractionId
            || reference.EventRevision <= 0 || !Digest(reference.ContentDigest) || state.EventRevision <= 0 || !Enum.IsDefined(state.Status) || state.Status == WorkflowExecutionStatus.Unknown || !Enum.IsDefined(state.Outcome))
        { throw new ArgumentException("InvalidWorkflowExecutionState", nameof(state)); }
    }
    /// <summary>Checks the shared tenant and Conversation identifier contract before any durable intake effect.</summary>
    internal static bool IsOpaqueId(string? value) => value is not null && OpaqueId().IsMatch(value);
    private static bool Digest(string value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
    [GeneratedRegex(@"\A[A-Za-z0-9_-]{1,128}\z", RegexOptions.CultureInvariant)]
    private static partial Regex OpaqueId();
}
