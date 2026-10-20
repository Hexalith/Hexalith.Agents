namespace Hexalith.Agents.Server.Application.Activities;

/// <summary>Safe retry failure crossing the workflow boundary; dependency exceptions and payloads are deliberately absent.</summary>
internal sealed class AutomaticInteractionCheckpointUnavailableException() : Exception("InteractionCheckpointUnavailable");
