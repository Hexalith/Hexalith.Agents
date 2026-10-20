using Hexalith.Agents.Contracts.Security;
namespace Hexalith.Agents.Server.Ports;

/// <summary>Content-free intake outcome; denied targets disclose no identity.</summary>
/// <param name="Status">The Status binding.</param>
/// <param name="AgentInteractionId">The AgentInteractionId binding.</param>
/// <param name="Dependency">The Dependency binding.</param>
public sealed record AutomaticInteractionIntakeResult(AutomaticInteractionIntakeStatus Status, string? AgentInteractionId = null, DependencyNotAvailable? Dependency = null);
