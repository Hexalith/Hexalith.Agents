using Hexalith.Agents.Contracts.Agent.Commands;

namespace Hexalith.Agents.EventStore;

internal sealed class ConfigureAgentResponseModeIdempotencyIntentAdapter()
    : AgentSetupIdempotencyIntentAdapter(nameof(ConfigureAgentResponseMode), "agents.setup.response-mode", StandardSemanticExtensionKeys());
