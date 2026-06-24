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
    /// Registers the Agents domain and its five ordered, policy-gated nav entries, from operational setup to the V1
    /// in-product Conversation invocation surface (UX-DR1). The invariant <c>Title</c> is the English fallback that
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
    }
}
