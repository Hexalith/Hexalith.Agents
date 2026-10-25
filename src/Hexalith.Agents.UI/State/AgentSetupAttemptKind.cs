namespace Hexalith.Agents.UI.State;

/// <summary>The closed set of setup intents retained within an authenticated circuit.</summary>
internal enum AgentSetupAttemptKind
{
    /// <summary>Replace identity metadata and instructions.</summary>
    Configuration,
    /// <summary>Change response mode.</summary>
    ResponseMode,
    /// <summary>Activate the displayed configuration version.</summary>
    Activate,
    /// <summary>Disable the Agent.</summary>
    Disable,
}
