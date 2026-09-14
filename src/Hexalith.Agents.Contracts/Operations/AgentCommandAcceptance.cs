using Hexalith.Agents.Contracts.Agent;

using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Operations;

/// <summary>
/// The structured accepted identity returned when an Agent administration command is accepted (Story 5.2 AC1).
/// It names only public identities — the Agent, the command message, and the correlation used for tracing — plus
/// the truth stage the acceptance has reached. It deliberately exposes no stream name, revision, aggregate type,
/// workflow instance, projection address, or Provider SDK detail.
/// </summary>
/// <param name="AgentId">The Agent the command targeted.</param>
/// <param name="MessageId">The accepted command message identity.</param>
/// <param name="CorrelationId">The correlation identity for tracing the accepted command.</param>
/// <param name="TruthState">The truth stage reached by acceptance; it never claims projection confirmation.</param>
/// <param name="Effect">The safe effect reported by the completed domain command.</param>
/// <param name="TargetConfigurationVersion">The authoritative configuration version produced or retained by the command.</param>
[method: JsonConstructor]
public record AgentCommandAcceptance(
    string AgentId,
    string MessageId,
    string CorrelationId,
    AgentSetupTruthState TruthState,
    AgentSetupWriteEffect Effect = AgentSetupWriteEffect.Unknown,
    int? TargetConfigurationVersion = null)
{
    /// <summary>
    /// Initializes an acceptance using the original V1 CLR shape. Additive outcome evidence remains unknown when
    /// an older caller or implementation uses this constructor.
    /// </summary>
    /// <param name="agentId">The Agent the command targeted.</param>
    /// <param name="messageId">The accepted command message identity.</param>
    /// <param name="correlationId">The correlation identity for tracing the accepted command.</param>
    /// <param name="truthState">The truth stage reached by acceptance.</param>
    public AgentCommandAcceptance(
        string agentId,
        string messageId,
        string correlationId,
        AgentSetupTruthState truthState)
        : this(
            agentId,
            messageId,
            correlationId,
            truthState,
            AgentSetupWriteEffect.Unknown,
            TargetConfigurationVersion: null)
    {
    }

    /// <summary>Deconstructs an acceptance using the original V1 four-value CLR shape.</summary>
    /// <param name="agentId">The Agent the command targeted.</param>
    /// <param name="messageId">The accepted command message identity.</param>
    /// <param name="correlationId">The correlation identity for tracing the accepted command.</param>
    /// <param name="truthState">The truth stage reached by acceptance.</param>
    public void Deconstruct(
        out string agentId,
        out string messageId,
        out string correlationId,
        out AgentSetupTruthState truthState)
    {
        agentId = AgentId;
        messageId = MessageId;
        correlationId = CorrelationId;
        truthState = TruthState;
    }
}
