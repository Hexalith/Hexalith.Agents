using Hexalith.Agents.Contracts.Agent.Commands;

namespace Hexalith.Agents.EventStore;

internal sealed class UpdateAgentConfigurationIdempotencyIntentAdapter()
    : AgentSetupIdempotencyIntentAdapter(nameof(UpdateAgentConfiguration), "agents.setup.update", StandardSemanticExtensionKeys());
