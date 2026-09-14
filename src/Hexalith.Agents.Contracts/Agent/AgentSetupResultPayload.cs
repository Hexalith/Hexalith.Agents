namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The property names of the bounded result payload that correlates an Agent setup command with its authoritative
/// configuration version (Story 5.2 AC1). The domain writes the payload and the operations boundary reads it from a
/// different assembly, so both sides bind to these names: renaming one without the other would otherwise compile
/// cleanly and silently degrade every setup write to <see cref="AgentSetupWriteStatus.UnableToVerify"/>.
/// </summary>
public static class AgentSetupResultPayload
{
    /// <summary>The property carrying the <see cref="AgentSetupWriteEffect"/> name.</summary>
    public const string EffectProperty = "effect";

    /// <summary>The property carrying the authoritative configuration version produced or retained by the command.</summary>
    public const string ConfigurationVersionProperty = "configurationVersion";
}
