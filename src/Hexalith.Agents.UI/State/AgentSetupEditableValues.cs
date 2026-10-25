using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.UI.State;

/// <summary>The three editable values obtainable from an authorized setup read; never instruction text.</summary>
/// <param name="DisplayName">The safe display name.</param>
/// <param name="Description">The description, normalized to empty when absent.</param>
/// <param name="ResponseMode">The saved or drafted response mode.</param>
internal sealed record AgentSetupEditableValues(string DisplayName, string Description, AgentResponseMode ResponseMode)
{
    /// <summary>Extracts only the editable, readable values from a safe setup view.</summary>
    /// <param name="agent">The authorized view.</param>
    /// <returns>The readable editable values.</returns>
    internal static AgentSetupEditableValues From(AgentStatusView agent)
        => new(agent.DisplayName, agent.Description ?? string.Empty, agent.ResponseMode);

    /// <summary>Advances baseline fields that authority confirmed equal to the draft, retaining concurrent differences.</summary>
    /// <param name="saved">The newly confirmed saved values.</param>
    /// <param name="draft">The current administrator edits.</param>
    /// <returns>The baseline for further editing.</returns>
    internal AgentSetupEditableValues AdvanceConfirmedFields(AgentSetupEditableValues saved, AgentSetupEditableValues draft)
        => new(
            saved.DisplayName == draft.DisplayName ? saved.DisplayName : DisplayName,
            saved.Description == draft.Description ? saved.Description : Description,
            saved.ResponseMode == draft.ResponseMode ? saved.ResponseMode : ResponseMode);

    /// <summary>Tests field by field whether saved values changed from the baseline and differ from the draft.</summary>
    /// <param name="baseline">The values from which editing began.</param>
    /// <param name="draft">The administrator's current edits.</param>
    /// <returns>Whether at least one relevant saved value diverged.</returns>
    internal bool DivergesFrom(AgentSetupEditableValues baseline, AgentSetupEditableValues draft)
        => (DisplayName != baseline.DisplayName && DisplayName != draft.DisplayName)
            || (Description != baseline.Description && Description != draft.Description)
            || (ResponseMode != baseline.ResponseMode && ResponseMode != draft.ResponseMode);
}
