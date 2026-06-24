using System.Linq;

using Bunit;
using Bunit.TestDoubles;

using Hexalith.Agents.UI.Composition;
using Hexalith.Agents.UI.Resources;
using Hexalith.FrontComposer.Contracts.Registration;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1 — the Agents domain/category registers setup-oriented navigation, and policy-gated entries never leak to
/// unauthorized users. The registration test pins the gating INPUTS (each entry's RequiredPolicy); the gating
/// render test proves the OUTPUT through the same AuthorizeView gate the shell uses.
/// </summary>
public sealed class AgentsNavigationTests : AgentsTestContext
{
    [Fact]
    public void RegisterDomain_registers_agents_manifest_and_five_ordered_entries()
    {
        CapturingFrontComposerRegistry registry = new();

        AgentsFrontComposerRegistration.RegisterDomain(registry);

        DomainManifest manifest = registry.Manifests.ShouldHaveSingleItem();
        manifest.BoundedContext.ShouldBe("agents");
        manifest.NameKey.ShouldBe("Agents.Navigation.Agents");

        registry.NavEntries.Select(entry => entry.Href)
            .ShouldBe(["/agents", "/agents/configuration", "/agents/providers", "/agents/approver-policy", "/agents/conversation-call"]);
        registry.NavEntries.Select(entry => entry.Order)
            .ShouldBe([0, 1, 2, 3, 4]);
        registry.NavEntries.ShouldAllBe(entry => entry.BoundedContext == "agents");
        registry.NavEntries.First().Title.ShouldBe("Agents overview");
        registry.NavEntries.Last().Title.ShouldBe("Conversation call");
    }

    [Fact]
    public void Every_setup_entry_is_gated_by_the_agents_administrator_policy()
    {
        CapturingFrontComposerRegistry registry = new();

        AgentsFrontComposerRegistration.RegisterDomain(registry);

        registry.NavEntries.ShouldAllBe(entry =>
            entry.RequiredPolicy == AgentsFrontComposerRegistration.AgentsAdministratorPolicy);
    }

    [Fact]
    public void Authorized_administrator_sees_all_setup_links()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(AgentsFrontComposerRegistration.AgentsAdministratorPolicy);

        IRenderedComponent<NavEntryGatingHarness> cut = RenderRegisteredEntries();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("href=\"/agents\"");
            cut.Markup.ShouldContain("href=\"/agents/configuration\"");
            cut.Markup.ShouldContain("href=\"/agents/providers\"");
            cut.Markup.ShouldContain("href=\"/agents/approver-policy\"");
            cut.Markup.ShouldContain("href=\"/agents/conversation-call\"");
        });
    }

    [Fact]
    public void Unauthorized_user_sees_no_setup_links_and_no_record_leak()
    {
        Authorization.SetNotAuthorized();

        IRenderedComponent<NavEntryGatingHarness> cut = RenderRegisteredEntries();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("href=\"/agents\"");
            cut.Markup.ShouldNotContain("href=\"/agents/configuration\"");
            cut.Markup.ShouldNotContain("href=\"/agents/providers\"");
            cut.Markup.ShouldNotContain("href=\"/agents/approver-policy\"");
            cut.Markup.ShouldNotContain("href=\"/agents/conversation-call\"");
        });
    }

    [Fact]
    public void Authenticated_user_without_the_admin_policy_sees_no_setup_links()
    {
        // Authenticated but NOT granted the Agents administrator policy — the policy-gated AuthorizeView still hides
        // every entry, so nav hiding is genuine policy gating, not merely an authentication check (AC1, AD-12).
        Authorization.SetAuthorized("user");

        IRenderedComponent<NavEntryGatingHarness> cut = RenderRegisteredEntries();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("href=\"/agents\"");
            cut.Markup.ShouldNotContain("href=\"/agents/configuration\"");
            cut.Markup.ShouldNotContain("href=\"/agents/providers\"");
            cut.Markup.ShouldNotContain("href=\"/agents/approver-policy\"");
            cut.Markup.ShouldNotContain("href=\"/agents/conversation-call\"");
        });
    }

    [Fact]
    public void Manifest_and_every_entry_carry_localization_metadata_for_culture_aware_labels()
    {
        CapturingFrontComposerRegistry registry = new();

        AgentsFrontComposerRegistration.RegisterDomain(registry);

        AgentsFrontComposerRegistration.Manifest.Resource.ShouldBe(typeof(AgentsResources));
        AgentsFrontComposerRegistration.Manifest.Icon.ShouldNotBeNullOrWhiteSpace();

        registry.NavEntries.ShouldAllBe(entry => entry.Resource == typeof(AgentsResources));
        registry.NavEntries.ShouldAllBe(entry => !string.IsNullOrWhiteSpace(entry.TitleKey));
        registry.NavEntries.Select(entry => entry.TitleKey).ShouldBe(
        [
            "Agents.Navigation.Overview",
            "Agents.Navigation.Configuration",
            "Agents.Navigation.ProviderCatalog",
            "Agents.Navigation.ApproverPolicy",
            "Agents.Navigation.ConversationCall",
        ]);
    }

    private IRenderedComponent<NavEntryGatingHarness> RenderRegisteredEntries()
    {
        CapturingFrontComposerRegistry registry = new();
        AgentsFrontComposerRegistration.RegisterDomain(registry);
        return Render<NavEntryGatingHarness>(parameters => parameters.Add(harness => harness.Entries, registry.NavEntries));
    }
}
