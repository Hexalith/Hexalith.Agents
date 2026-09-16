using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.UI.State;

/// <summary>
/// Retains one exact setup intent and its command identities until it is confirmed, rejected, or explicitly
/// abandoned by the administrator.
/// </summary>
/// <param name="Options">The immutable ULID correlation and idempotency identities.</param>
/// <param name="Submit">The exact captured payload submission.</param>
/// <param name="InstructionsDraft">The exact write-only instructions draft retained for restoration on terminal failure or abandon.</param>
internal sealed record AgentSetupAttempt(
    AgentOperationOptions Options,
    Func<AgentOperationOptions, CancellationToken, Task<AgentSetupWriteResult>> Submit,
    string? InstructionsDraft = null);
