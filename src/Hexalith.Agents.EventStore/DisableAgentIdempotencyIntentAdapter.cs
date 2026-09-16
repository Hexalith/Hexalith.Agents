using Hexalith.Agents.Contracts.Agent.Commands;

namespace Hexalith.Agents.EventStore;

internal sealed class DisableAgentIdempotencyIntentAdapter()
    : AgentSetupIdempotencyIntentAdapter(nameof(DisableAgent), "agents.setup.disable", StandardSemanticExtensionKeys());
