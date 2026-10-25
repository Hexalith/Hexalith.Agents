using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.UI.State;

/// <summary>Memory-only recovery data for one unresolved setup write; contains no server setup snapshot.</summary>
/// <param name="Attempt">The immutable command intent.</param>
/// <param name="PendingWrite">The received acceptance or unresolved transport outcome.</param>
/// <param name="Draft">Editable values preserved while the write is unresolved.</param>
/// <param name="InstructionsDraft">An unrelated local instruction draft, never populated by a server read.</param>
/// <param name="Baseline">The saved editable values from which editing began.</param>
internal sealed record RetainedAgentSetupAttempt(
    AgentSetupAttempt Attempt,
    AgentSetupWriteResult? PendingWrite,
    AgentSetupEditableValues Draft,
    string InstructionsDraft,
    AgentSetupEditableValues? Baseline);
