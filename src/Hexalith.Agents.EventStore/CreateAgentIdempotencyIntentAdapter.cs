using Hexalith.Agents.Contracts.Agent.Commands;

namespace Hexalith.Agents.EventStore;

internal sealed class CreateAgentIdempotencyIntentAdapter()
    : AgentSetupIdempotencyIntentAdapter(nameof(CreateAgent), "agents.setup.create", typeof(CreateAgent), StandardSemanticExtensionKeys());
