namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// A safe, content-free count of recent Agent Call outcomes dimensioned by one canonical
/// <see cref="AgentCallOperationStatus"/> (Story 4.3 AC4). The count is a telemetry rate dimensioned ONLY by the safe
/// status enum — never by any prompt, generated content, id, secret, or per-record summary text (AC4 second clause;
/// AD-14).
/// </summary>
/// <param name="Status">The canonical Agent Call status the count is dimensioned by.</param>
/// <param name="Count">The authorized count of recent Agent Calls in this status (zero or more).</param>
public sealed record AgentCallOutcomeCount(AgentCallOperationStatus Status, int Count);
