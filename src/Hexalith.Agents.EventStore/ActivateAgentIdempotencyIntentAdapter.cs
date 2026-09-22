using Hexalith.Agents.Contracts.Agent.Commands;

namespace Hexalith.Agents.EventStore;

internal sealed class ActivateAgentIdempotencyIntentAdapter()
    : AgentSetupIdempotencyIntentAdapter(nameof(ActivateAgent), "agents.setup.activate", typeof(ActivateAgent), ActivationSemanticExtensionKeys());
