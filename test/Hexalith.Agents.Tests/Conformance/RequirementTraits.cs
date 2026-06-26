namespace Hexalith.Agents.Tests.Conformance;

/// <summary>
/// Shared xUnit trait vocabulary for the Story 4.5 consolidated governance-conformance suite (AC1, AC4). Applying
/// <c>[Trait(RequirementTraits.Architecture, "AD-17")]</c> / <c>[Trait(RequirementTraits.Gate, RequirementTraits.Gates.TenantIsolation)]</c>
/// lets a failing gate be selected and named by its governing requirement — e.g. <c>dotnet test --filter "Gate=TenantIsolation"</c>
/// selects one gate and <c>dotnet test --filter "Architecture=AD-17"</c> selects the consolidated AD-17 gates. Every
/// conformance assertion's failure message additionally embeds the governing id(s) (the AC1 "failures identify the relevant
/// FR, NFR, or UX-DR" mechanism). Trait values are plain strings (e.g. <c>"FR-19"</c>, <c>"NFR-2"</c>, <c>"UX-DR40"</c>,
/// <c>"AD-18"</c>).
/// </summary>
/// <remarks>
/// Mirrored verbatim (key names only) into <c>Hexalith.Agents.Server.Tests</c> and <c>Hexalith.Agents.UI.Tests</c> so the
/// cross-assembly conformance tests select on the same trait keys; the four keys here are the single source of truth.
/// </remarks>
internal static class RequirementTraits
{
    /// <summary>Trait key for a Functional Requirement id (e.g. <c>"FR-19"</c>).</summary>
    internal const string Requirement = "Requirement";

    /// <summary>Trait key for an Architecture Decision id (e.g. <c>"AD-17"</c>).</summary>
    internal const string Architecture = "Architecture";

    /// <summary>Trait key for a UX Design Requirement id (e.g. <c>"UX-DR40"</c>).</summary>
    internal const string UxRequirement = "UxRequirement";

    /// <summary>Trait key for the governance-gate name (e.g. <c>"TenantIsolation"</c>).</summary>
    internal const string Gate = "Gate";

    /// <summary>The AD-17 governance-gate names the consolidated suite selects on (the AC1 gate checklist).</summary>
    internal static class Gates
    {
        /// <summary>Aggregate transition purity / deterministic replay (AD-3, NFR-3).</summary>
        internal const string TransitionPurity = "TransitionPurity";

        /// <summary>Authorization fail-closed, NotAuthorized/NotFound indistinguishable (FR-19/20/21, NFR-1, AD-12).</summary>
        internal const string AuthorizationFailClosed = "AuthorizationFailClosed";

        /// <summary>Proposal version immutability / append-only preservation (FR-14, AD-5).</summary>
        internal const string ProposalImmutability = "ProposalImmutability";

        /// <summary>Replay / idempotency: re-asserted equal commands no-op; deterministic ids (AD-13).</summary>
        internal const string ReplayIdempotency = "ReplayIdempotency";

        /// <summary>Tenant isolation: cross-tenant read fails closed, indistinguishable from not-found (FR-19, NFR-2, AD-12).</summary>
        internal const string TenantIsolation = "TenantIsolation";

        /// <summary>Context-too-large blocking with no provider call / proposal / posting (FR-9, NFR-8, AD-11).</summary>
        internal const string ContextTooLarge = "ContextTooLarge";

        /// <summary>Content Safety enforcement before any Conversation side effect (FR-26/27, NFR-7, AD-14).</summary>
        internal const string ContentSafety = "ContentSafety";

        /// <summary>Audit completeness for every posted response (FR-24, NFR-5, AD-17).</summary>
        internal const string AuditCompleteness = "AuditCompleteness";

        /// <summary>Runtime ownership: exactly one durable owner at the fail-closed seam (AD-18).</summary>
        internal const string RuntimeOwnership = "RuntimeOwnership";

        /// <summary>Cross-assembly SDK purity: no framework/provider/workflow SDK in contracts or EventStore aggregates (AD-18/19).</summary>
        internal const string SdkPurity = "SdkPurity";

        /// <summary>Provider-secret non-disclosure: no secret-bearing member name on the contracts or EventStore domain surface (NFR-6, AD-9, AD-14).</summary>
        internal const string SecretNonDisclosure = "SecretNonDisclosure";

        /// <summary>FrontComposer UI floor coverage for every Agents page (UX-DR floor; AD-15).</summary>
        internal const string UiFloor = "UiFloor";
    }
}
