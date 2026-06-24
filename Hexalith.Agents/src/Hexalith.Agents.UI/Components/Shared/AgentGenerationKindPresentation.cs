using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// Pure, dependency-free mapping from the safe <see cref="AgentGenerationKind"/> to a whole-string localization key so a
/// generated, edited, and regenerated versions are labeled <b>distinctly</b> in the version-history / proposal-editor /
/// regenerate surfaces (Story 3.3, Story 3.4; AC1, AC2, AC3; FR-14). The mapping derives only the coarse, content-free
/// classification and never exposes any generated/edited/regenerated content, secret, id, or PII (AD-14). Mirrors
/// <see cref="ProposedAgentReplyStatePresentation"/> and is unit-testable in isolation (no bUnit). The switch is <b>total</b>
/// so any reserved/future kind renders through a safe default until its owning story adds an explicit label.
/// </summary>
public static class AgentGenerationKindPresentation
{
    /// <summary>The whole-string localization key for a generation-kind label (UX-DR14).</summary>
    /// <param name="kind">The safe generation kind.</param>
    /// <returns>The resource key, distinct per kind so generated, edited, and regenerated versions are labeled differently.</returns>
    public static string LabelKeyFor(AgentGenerationKind kind)
        => kind switch
        {
            AgentGenerationKind.Generated => "Agents.GenerationKind.Label.Generated",
            AgentGenerationKind.Edited => "Agents.GenerationKind.Label.Edited",
            AgentGenerationKind.Regenerated => "Agents.GenerationKind.Label.Regenerated",
            _ => "Agents.GenerationKind.Label.Unknown",
        };
}
