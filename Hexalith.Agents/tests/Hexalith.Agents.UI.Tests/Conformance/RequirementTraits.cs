namespace Hexalith.Agents.UI.Tests.Conformance;

/// <summary>
/// Story 4.5 xUnit trait vocabulary, mirrored from <c>Hexalith.Agents.Tests/Conformance/RequirementTraits.cs</c> so the
/// FrontComposer UI-floor conformance tests select on the same trait keys (AC1, AC3). Trait values are plain strings
/// (UX-DR/AD ids); every conformance failure message also embeds the governing UX-DR id.
/// </summary>
internal static class RequirementTraits
{
    /// <summary>Trait key for a Functional Requirement id (e.g. <c>"FR-22"</c>).</summary>
    internal const string Requirement = "Requirement";

    /// <summary>Trait key for an Architecture Decision id (e.g. <c>"AD-15"</c>).</summary>
    internal const string Architecture = "Architecture";

    /// <summary>Trait key for a UX Design Requirement id (e.g. <c>"UX-DR40"</c>).</summary>
    internal const string UxRequirement = "UxRequirement";

    /// <summary>Trait key for the governance-gate name (e.g. <c>"UiFloor"</c>).</summary>
    internal const string Gate = "Gate";

    /// <summary>The UX-floor governance-gate names the UI conformance suite selects on.</summary>
    internal static class Gates
    {
        /// <summary>FrontComposer UI floor coverage for every Agents page (UX-DR floor; AD-15).</summary>
        internal const string UiFloor = "UiFloor";
    }
}
