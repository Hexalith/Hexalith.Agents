namespace Hexalith.Agents.Server.Tests.Conformance;

/// <summary>
/// Story 4.5 xUnit trait vocabulary, mirrored from <c>Hexalith.Agents.Tests/Conformance/RequirementTraits.cs</c> so the
/// cross-assembly runtime-ownership / SDK-purity conformance tests select on the same trait keys (AC1, AC2). Trait
/// values are plain strings (FR/NFR/UX-DR/AD ids); every conformance failure message also embeds the governing id.
/// </summary>
internal static class RequirementTraits
{
    /// <summary>Trait key for a Functional Requirement id (e.g. <c>"FR-19"</c>).</summary>
    internal const string Requirement = "Requirement";

    /// <summary>Trait key for an Architecture Decision id (e.g. <c>"AD-18"</c>).</summary>
    internal const string Architecture = "Architecture";

    /// <summary>Trait key for a UX Design Requirement id (e.g. <c>"UX-DR40"</c>).</summary>
    internal const string UxRequirement = "UxRequirement";

    /// <summary>Trait key for the governance-gate name (e.g. <c>"SdkPurity"</c>).</summary>
    internal const string Gate = "Gate";

    /// <summary>The AD-17/AD-18/AD-19 governance-gate names the conformance suite selects on.</summary>
    internal static class Gates
    {
        /// <summary>Replay / idempotency: deterministic ids; re-asserted equal commands no-op (AD-13).</summary>
        internal const string ReplayIdempotency = "ReplayIdempotency";

        /// <summary>Runtime ownership: exactly one durable owner at the fail-closed seam (AD-18/AD-19).</summary>
        internal const string RuntimeOwnership = "RuntimeOwnership";

        /// <summary>Cross-assembly SDK purity: no framework/provider/workflow SDK in contracts or EventStore aggregates (AD-18/19).</summary>
        internal const string SdkPurity = "SdkPurity";

        /// <summary>Provider-secret non-disclosure: no secret-bearing member name on the contracts or EventStore domain surface (NFR-6, AD-9, AD-14).</summary>
        internal const string SecretNonDisclosure = "SecretNonDisclosure";
    }
}
