using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// Pure, dependency-free mapping from the safe <see cref="AgentGenerationKind"/> to a whole-string localization key so a
/// generated version and an edited version are labeled <b>distinctly</b> in the version-history / proposal-editor surfaces
/// (Story 3.3; AC1, AC3; FR-14). The mapping derives only the coarse, content-free classification and never exposes any
/// generated/edited content, secret, id, or PII (AD-14). Mirrors <see cref="ProposedAgentReplyStatePresentation"/> and is
/// unit-testable in isolation (no bUnit). The switch is <b>total</b> so any reserved/future kind renders through a safe
/// default until its owning story adds an explicit label.
/// </summary>
public static class AgentGenerationKindPresentation
{
    /// <summary>The whole-string localization key for a generation-kind label (UX-DR14).</summary>
    /// <param name="kind">The safe generation kind.</param>
    /// <returns>The resource key, distinct per kind so generated and edited versions are labeled differently.</returns>
    public static string LabelKeyFor(AgentGenerationKind kind)
        => kind switch
        {
            AgentGenerationKind.Generated => "Agents.GenerationKind.Label.Generated",
            AgentGenerationKind.Edited => "Agents.GenerationKind.Label.Edited",
            _ => "Agents.GenerationKind.Label.Unknown",
        };
}
