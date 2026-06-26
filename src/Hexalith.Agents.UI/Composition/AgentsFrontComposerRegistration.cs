using Hexalith.Agents.UI.Resources;
using Hexalith.FrontComposer.Contracts.Registration;

namespace Hexalith.Agents.UI.Composition;

/// <summary>
/// Registers the Agents domain/category and its policy-gated setup navigation with the FrontComposer shell
/// (AC1, AD-15). The shell discovers the static <see cref="Manifest"/> + <see cref="RegisterDomain"/> and owns all
/// rendering (icons, grouping, active state, responsive collapse, and the <c>AuthorizeView</c> gate for entries
/// that declare a <see cref="FrontComposerNavEntry.RequiredPolicy"/>). Mirrors
/// <c>TenantsFrontComposerRegistration</c>.
/// </summary>
public static class AgentsFrontComposerRegistration
{
    /// <summary>
    /// Authorization policy gating the Agents setup navigation entries and the setup pages themselves (AD-12,
    /// AD-15). Nav hiding alone is not authorization — the pages also carry <c>[Authorize(Policy = …)]</c> and the
    /// real authorization decision is server-side and shared with the API (UI parity). The shell evaluates this via
    /// <c>AuthorizeView</c> so unauthorized users never see (or can navigate to) the gated links — no record leak
    /// (UX-DR41). Registering the actual <c>AddAuthorizationCore(... AddPolicy(...))</c> lives in the deferred host
    /// <c>Program.cs</c>; the constant + entry/page wiring is in scope.
    /// </summary>
    public const string AgentsAdministratorPolicy = "Agents.Administrator";

    /// <summary>
    /// Authorization policy gating the approver-facing proposal-queue navigation entry and page (AD-12, AD-15). This
    /// is the first <b>Approver</b> (not Administrator) surface — the proposal queue's audience is Approvers, not only
    /// Agent Administrators (PRD glossary: Approver ≠ Administrator). Nav hiding alone is not authorization — the page
    /// also carries <c>[Authorize(Policy = …)]</c> and the real authorization decision is server-side and shared with
    /// the API (UI parity). The shell evaluates this via <c>AuthorizeView</c> so unauthorized users never see (or can
    /// navigate to) the gated link — no record leak (AD-12, AC4). Registering the actual
    /// <c>AddAuthorizationCore(... AddPolicy(...))</c> lives in the deferred host <c>Program.cs</c>; the constant +
    /// entry/page wiring is in scope.
    /// </summary>
    public const string AgentsApproverPolicy = "Agents.Approver";

    /// <summary>
    /// Authorization policy gating the operational-status navigation entry and page (AD-12, AD-15). This is the
    /// "Agent Administrator or Operator" persona of Story 4.3 — the consolidated operational surface's audience is
    /// Operators, not only Agent Administrators. Nav hiding alone is not authorization — the page also carries
    /// <c>[Authorize(Policy = …)]</c> and the real authorization decision is server-side and shared with the API
    /// (UI parity). The shell evaluates this via <c>AuthorizeView</c> so unauthorized users never see (or can navigate
    /// to) the gated link — no record leak (AD-12). Registering the actual <c>AddAuthorizationCore(... AddPolicy(...))</c>
    /// lives in the deferred host <c>Program.cs</c>; the constant + entry/page wiring is in scope. (A host may gate this
    /// with <see cref="AgentsAdministratorPolicy"/> instead — the invariant is policy-gated + fail-closed + no-leak; the
    /// distinct constant follows the per-audience precedent set by <see cref="AgentsApproverPolicy"/>.)
    /// </summary>
    public const string AgentsOperatorPolicy = "Agents.Operator";

    /// <summary>
    /// Authorization policy gating the audit-evidence navigation entry and page (AD-12, AD-15). This is the
    /// "Tenant or Compliance Operator" persona of Story 4.2/4.3 — the audit-evidence surface's audience is audit
    /// operators. Nav hiding alone is not authorization — the page also carries <c>[Authorize(Policy = …)]</c> and the
    /// real authorization decision is server-side and shared with the API (UI parity), and the audit dual-gate
    /// (Approver Policy + fresh Source Conversation access) is enforced server-side (deferred). The shell evaluates this
    /// via <c>AuthorizeView</c> so unauthorized users never see (or can navigate to) the gated link — no record leak
    /// (AD-12). Registering the actual <c>AddAuthorizationCore(... AddPolicy(...))</c> lives in the deferred host
    /// <c>Program.cs</c>; the constant + entry/page wiring is in scope. (A host may gate this with
    /// <see cref="AgentsAdministratorPolicy"/> instead — the invariant is policy-gated + fail-closed + no-leak.)
    /// </summary>
    public const string AgentsAuditOperatorPolicy = "Agents.AuditOperator";

    /// <summary>
    /// The Agents domain manifest contributed to the shell's left navigation. The shell shows the pinned Fluent
    /// glyph on the collapsed rail and resolves the localized category title ("Agents" / "Agents") from
    /// <see cref="AgentsResources"/> per the request culture; <c>Name</c> stays the invariant English fallback.
    /// </summary>
    public static DomainManifest Manifest { get; } = new(
        "Agents",
        "agents",
        [],
        [],
        // Pinned Regular.Size20 glyph resolved by the shell via FcFluentIcons.TryCreate (curated vocabulary —
        // mirrors the Tenants precedent of using an existing Regular.Size20.* name).
        Icon: "Regular.Size20.Apps",
        NameKey: "Agents.Navigation.Agents",
        Resource: typeof(AgentsResources));

    /// <summary>
    /// Registers the Agents domain and its eight ordered, policy-gated nav entries, coherent through the one Agents
    /// domain in operational-setup → workflow → status → audit order (AC3; UX-DR1): provider administration, <c>hexa</c>
    /// configuration, lifecycle, approver policy, Conversation invocation, proposal operations, operational status, and
    /// audit evidence. Every page calls the public Agents UI gateways/contracts, never raw EventStore streams, provider
    /// SDKs, or aggregate internals (AC3 second clause; AD-15). The invariant <c>Title</c> is the English fallback that
    /// drives stable test ids and sort order; <c>TitleKey</c> + <c>Resource</c> localize the label per request culture.
    /// </summary>
    /// <param name="registry">The FrontComposer registry the shell exposes during composition.</param>
    public static void RegisterDomain(IFrontComposerRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        // The manifest provides the localized "Agents" category title for the shell's left navigation.
        registry.RegisterDomain(Manifest);

        // Setup is admin-only: every entry is gated by the same Agents administrator policy (AC1, AD-12/AD-15).
        // The category title ("Agents") and the first child ("Agents overview") are distinct words (UX-DR; mirrors
        // Tenants' "Tenants" / "All tenants").
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Agents overview",
            "/agents",
            Icon: "Regular.Size20.Apps",
            Order: 0,
            RequiredPolicy: AgentsAdministratorPolicy,
            TitleKey: "Agents.Navigation.Overview",
            Resource: typeof(AgentsResources)));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "hexa configuration",
            "/agents/configuration",
            Icon: "Regular.Size20.Settings",
            Order: 1,
            RequiredPolicy: AgentsAdministratorPolicy,
            TitleKey: "Agents.Navigation.Configuration",
            Resource: typeof(AgentsResources)));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Provider catalog",
            "/agents/providers",
            Icon: "Regular.Size20.DevMode",
            Order: 2,
            RequiredPolicy: AgentsAdministratorPolicy,
            TitleKey: "Agents.Navigation.ProviderCatalog",
            Resource: typeof(AgentsResources)));
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Approver policy",
            "/agents/approver-policy",
            Icon: "Regular.Size20.People",
            Order: 3,
            RequiredPolicy: AgentsAdministratorPolicy,
            TitleKey: "Agents.Navigation.ApproverPolicy",
            Resource: typeof(AgentsResources)));

        // V1 in-product Conversation invocation surface (UX-DR1). Gated by the same Agents administrator policy for the
        // demonstrable surface; the participant-facing Conversation-owned affordance and its participant-level
        // authorization are deferred to PRD OQ-1 resolution — no new auth semantics are invented in this story. The
        // Size20 "Play" glyph is not in the curated FcFluentIcons vocabulary, so the proceed/chevron glyph is reused.
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Conversation call",
            "/agents/conversation-call",
            Icon: "Regular.Size20.ChevronRight",
            Order: 4,
            RequiredPolicy: AgentsAdministratorPolicy,
            TitleKey: "Agents.Navigation.ConversationCall",
            Resource: typeof(AgentsResources)));

        // Approver-facing in-product pending-proposal discovery surface (FR-13; AC1, AC4). This is the first entry gated
        // by the Approver policy rather than the Administrator policy (PRD glossary: Approver ≠ Administrator). The
        // Size20 inbox/queue glyph is not in the curated FcFluentIcons vocabulary, so the list-style "Navigation" glyph
        // is reused (as the "Conversation call" entry reused "ChevronRight").
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Pending proposals",
            "/agents/proposals",
            Icon: "Regular.Size20.Navigation",
            Order: 5,
            RequiredPolicy: AgentsApproverPolicy,
            TitleKey: "Agents.Navigation.ProposalQueue",
            Resource: typeof(AgentsResources)));

        // Consolidated operational-status surface (Story 4.3; AC1, AC3). The first entry gated by the Operator policy
        // (the "Administrator or Operator" persona). The curated FcFluentIcons Size20 vocabulary has no dedicated
        // monitor/dashboard glyph, so the "Search" inspect glyph is reused (as the "Conversation call" entry reused
        // "ChevronRight" and "Pending proposals" reused "Navigation").
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Operational status",
            "/agents/status",
            Icon: "Regular.Size20.Search",
            Order: 6,
            RequiredPolicy: AgentsOperatorPolicy,
            TitleKey: "Agents.Navigation.OperationalStatus",
            Resource: typeof(AgentsResources)));

        // Consolidated support-safe audit-evidence surface (Story 4.3; AC1, AC3). Gated by the Audit Operator policy
        // (the "Tenant or Compliance Operator" persona). The curated FcFluentIcons Size20 vocabulary has no dedicated
        // audit glyph, so the "Search" inspect glyph is reused (audit is an inspect/search surface).
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Audit evidence",
            "/agents/audit",
            Icon: "Regular.Size20.Search",
            Order: 7,
            RequiredPolicy: AgentsAuditOperatorPolicy,
            TitleKey: "Agents.Navigation.AuditEvidence",
            Resource: typeof(AgentsResources)));

        // Launch-readiness surface (Story 4.4; AC2, AC3, AC4). The Release Operator persona folds under the Agents
        // administrator policy for V1 (the distinct release-operator authorization model is itself deferred). The
        // curated FcFluentIcons Size20 vocabulary has no dedicated launch/rocket glyph, so the "Settings" configuration
        // glyph is reused (launch readiness is a configuration/gate surface).
        registry.AddNavEntry(new FrontComposerNavEntry(
            "agents",
            "Launch readiness",
            "/agents/launch-readiness",
            Icon: "Regular.Size20.Settings",
            Order: 8,
            RequiredPolicy: AgentsAdministratorPolicy,
            TitleKey: "Agents.Navigation.LaunchReadiness",
            Resource: typeof(AgentsResources)));
    }
}
