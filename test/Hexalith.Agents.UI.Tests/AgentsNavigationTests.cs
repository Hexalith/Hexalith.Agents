using System.Linq;
using System.Reflection;

using Bunit;
using Bunit.TestDoubles;

using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Composition;
using Hexalith.Agents.UI.Resources;
using Hexalith.FrontComposer.Contracts.Registration;

using Microsoft.AspNetCore.Authorization;

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
    public void RegisterDomain_registers_agents_manifest_and_nine_ordered_entries()
    {
        // Story 4.3/4.4 AC3 — the Agents domain is coherent in operational-setup → workflow → status → audit → launch
        // order, now nine ordered entries (the Story 4.4 launch-readiness surface is appended at Order 8).
        CapturingFrontComposerRegistry registry = new();

        AgentsFrontComposerRegistration.RegisterDomain(registry);

        DomainManifest manifest = registry.Manifests.ShouldHaveSingleItem();
        manifest.BoundedContext.ShouldBe("agents");
        manifest.NameKey.ShouldBe("Agents.Navigation.Agents");

        registry.NavEntries.Select(entry => entry.Href)
            .ShouldBe(["/agents", "/agents/configuration", "/agents/providers", "/agents/approver-policy", "/agents/conversation-call", "/agents/proposals", "/agents/status", "/agents/audit", "/agents/launch-readiness"]);
        registry.NavEntries.Select(entry => entry.Order)
            .ShouldBe([0, 1, 2, 3, 4, 5, 6, 7, 8]);
        registry.NavEntries.ShouldAllBe(entry => entry.BoundedContext == "agents");
        registry.NavEntries.First().Title.ShouldBe("Agents overview");
        registry.NavEntries.Last().Title.ShouldBe("Launch readiness");
    }

    [Fact]
    public void Launch_readiness_entry_is_administrator_gated_at_order_8()
    {
        // Story 4.4 AC3 — the launch-readiness surface is the ninth entry (Order 8), gated by the Agents administrator
        // policy (the Release Operator persona folds under the administrator policy for V1).
        CapturingFrontComposerRegistry registry = new();

        AgentsFrontComposerRegistration.RegisterDomain(registry);

        FrontComposerNavEntry launch = registry.NavEntries.Single(entry => entry.Href == "/agents/launch-readiness");
        launch.RequiredPolicy.ShouldBe(AgentsFrontComposerRegistration.AgentsAdministratorPolicy);
        launch.Order.ShouldBe(8);
        launch.Title.ShouldBe("Launch readiness");
        launch.TitleKey.ShouldBe("Agents.Navigation.LaunchReadiness");
    }

    [Fact]
    public void Launch_readiness_page_is_administrator_policy_gated()
    {
        // Story 4.4 AC3 — nav hiding is not authorization: the page itself carries [Authorize(Policy = …)].
        AuthorizeAttribute authorize = typeof(LaunchReadiness).GetCustomAttributes<AuthorizeAttribute>(inherit: true).ShouldHaveSingleItem();
        authorize.Policy.ShouldBe(AgentsFrontComposerRegistration.AgentsAdministratorPolicy);
    }

    [Fact]
    public void Setup_entries_are_administrator_gated_and_the_proposal_queue_is_approver_gated()
    {
        // The five setup entries are administrator-only; the approver-facing proposal queue is gated by the distinct
        // Approver policy (PRD glossary: Approver ≠ Administrator).
        CapturingFrontComposerRegistry registry = new();

        AgentsFrontComposerRegistration.RegisterDomain(registry);

        string[] adminHrefs = ["/agents", "/agents/configuration", "/agents/providers", "/agents/approver-policy", "/agents/conversation-call"];
        registry.NavEntries.Where(entry => adminHrefs.Contains(entry.Href))
            .ShouldAllBe(entry => entry.RequiredPolicy == AgentsFrontComposerRegistration.AgentsAdministratorPolicy);

        FrontComposerNavEntry proposals = registry.NavEntries.Single(entry => entry.Href == "/agents/proposals");
        proposals.RequiredPolicy.ShouldBe(AgentsFrontComposerRegistration.AgentsApproverPolicy);
        proposals.Order.ShouldBe(5);
        proposals.Title.ShouldBe("Pending proposals");
    }

    [Fact]
    public void Operational_status_and_audit_entries_carry_their_per_audience_policies()
    {
        // Story 4.3 AC3 — the two new entries are gated by the distinct Operator / Audit Operator policies (per-audience
        // precedent set by the Approver policy).
        CapturingFrontComposerRegistry registry = new();

        AgentsFrontComposerRegistration.RegisterDomain(registry);

        FrontComposerNavEntry status = registry.NavEntries.Single(entry => entry.Href == "/agents/status");
        status.RequiredPolicy.ShouldBe(AgentsFrontComposerRegistration.AgentsOperatorPolicy);
        status.Order.ShouldBe(6);
        status.Title.ShouldBe("Operational status");
        status.TitleKey.ShouldBe("Agents.Navigation.OperationalStatus");

        FrontComposerNavEntry audit = registry.NavEntries.Single(entry => entry.Href == "/agents/audit");
        audit.RequiredPolicy.ShouldBe(AgentsFrontComposerRegistration.AgentsAuditOperatorPolicy);
        audit.Order.ShouldBe(7);
        audit.Title.ShouldBe("Audit evidence");
        audit.TitleKey.ShouldBe("Agents.Navigation.AuditEvidence");

        // The two new entries carry a non-empty RequiredPolicy (policy-gated, never an ungated link).
        registry.NavEntries.Where(entry => entry.Href is "/agents/status" or "/agents/audit")
            .ShouldAllBe(entry => !string.IsNullOrWhiteSpace(entry.RequiredPolicy));
    }

    [Theory]
    [InlineData(typeof(OperationalStatus), "Agents.Operator")]
    [InlineData(typeof(AuditEvidence), "Agents.AuditOperator")]
    public void Operational_status_and_audit_pages_are_policy_gated(System.Type pageType, string expectedPolicy)
    {
        // Story 4.3 AC3 — nav hiding is not authorization: the pages themselves carry [Authorize(Policy = …)].
        AuthorizeAttribute authorize = pageType.GetCustomAttributes<AuthorizeAttribute>(inherit: true).ShouldHaveSingleItem();
        authorize.Policy.ShouldBe(expectedPolicy);
    }

    [Fact]
    public void Authorized_approver_sees_the_proposal_queue_link()
    {
        Authorization.SetAuthorized("approver");
        Authorization.SetPolicies(AgentsFrontComposerRegistration.AgentsApproverPolicy);

        IRenderedComponent<NavEntryGatingHarness> cut = RenderRegisteredEntries();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("href=\"/agents/proposals\""));
    }

    [Fact]
    public void Authorized_operator_sees_the_operational_status_link()
    {
        // Story 4.3 AC3 — the operational-status link renders only for a user granted the Operator policy.
        Authorization.SetAuthorized("operator");
        Authorization.SetPolicies(AgentsFrontComposerRegistration.AgentsOperatorPolicy);

        IRenderedComponent<NavEntryGatingHarness> cut = RenderRegisteredEntries();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("href=\"/agents/status\"");
            // The Operator policy does NOT grant the distinct Audit Operator surface.
            cut.Markup.ShouldNotContain("href=\"/agents/audit\"");
        });
    }

    [Fact]
    public void Authorized_audit_operator_sees_the_audit_evidence_link()
    {
        // Story 4.3 AC3 — the audit-evidence link renders only for a user granted the Audit Operator policy.
        Authorization.SetAuthorized("auditor");
        Authorization.SetPolicies(AgentsFrontComposerRegistration.AgentsAuditOperatorPolicy);

        IRenderedComponent<NavEntryGatingHarness> cut = RenderRegisteredEntries();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("href=\"/agents/audit\"");
            cut.Markup.ShouldNotContain("href=\"/agents/status\"");
        });
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
            // The administrator policy does NOT grant the approver-only proposal queue (Approver ≠ Administrator).
            cut.Markup.ShouldNotContain("href=\"/agents/proposals\"");
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
            cut.Markup.ShouldNotContain("href=\"/agents/proposals\"");
            cut.Markup.ShouldNotContain("href=\"/agents/status\"");
            cut.Markup.ShouldNotContain("href=\"/agents/audit\"");
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
            cut.Markup.ShouldNotContain("href=\"/agents/proposals\"");
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
            "Agents.Navigation.ProposalQueue",
            "Agents.Navigation.OperationalStatus",
            "Agents.Navigation.AuditEvidence",
            "Agents.Navigation.LaunchReadiness",
        ]);
    }

    private IRenderedComponent<NavEntryGatingHarness> RenderRegisteredEntries()
    {
        CapturingFrontComposerRegistry registry = new();
        AgentsFrontComposerRegistration.RegisterDomain(registry);
        return Render<NavEntryGatingHarness>(parameters => parameters.Add(harness => harness.Entries, registry.NavEntries));
    }
}
