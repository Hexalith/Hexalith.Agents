using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.FrontComposer.Shell.Components.Layout;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC4/AC5/AC6 — the Approver policy builder is a constrained, row-based scaffold. In Automatic mode the builder is
/// not applicable (not blocked). In Confirmation mode it surfaces presence/disclosure/version plus the four V1 source
/// kinds (AD-8), and fails closed: Confirmation with no configured approver source renders a blocked state rather
/// than an empty success (UX-DR5, AD-12). No Party PII is rendered (AD-14).
/// </summary>
public sealed class ApproverPolicyTests : AgentsTestContext
{
    private void GivenConfiguration(AgentStatusView view)
        => SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.Success(view)));

    [Fact]
    public void Automatic_mode_renders_the_not_applicable_state_never_blocked()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Active,
            responseMode: AgentResponseMode.Automatic));

        IRenderedComponent<ApproverPolicy> cut = RenderPage<ApproverPolicy>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-approver-policy-not-applicable']").TextContent
                .ShouldContain("Agents.ApproverPolicy.NotApplicable");
            cut.FindAll("[data-testid='agents-approver-policy-blocked']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Confirmation_mode_without_a_configured_policy_fails_closed_as_blocked()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Draft,
            responseMode: AgentResponseMode.Confirmation,
            hasApproverPolicy: false));

        IRenderedComponent<ApproverPolicy> cut = RenderPage<ApproverPolicy>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-approver-policy-blocked']").TextContent
                .ShouldContain("Agents.ApproverPolicy.Blocked");
            cut.Find("[data-testid='agents-approver-policy-presence']").TextContent
                .ShouldContain("Agents.ApproverPolicy.Absent");
            cut.FindAll("[data-testid='agents-approver-policy-not-applicable']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Confirmation_mode_with_a_policy_renders_presence_version_disclosure_and_all_four_source_kinds()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Active,
            responseMode: AgentResponseMode.Confirmation,
            hasApproverPolicy: true,
            approverPolicyDisclosure: ApproverPolicyBasisDisclosure.UserVisible));

        IRenderedComponent<ApproverPolicy> cut = RenderPage<ApproverPolicy>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-approver-policy-presence']").TextContent
                .ShouldContain("Agents.ApproverPolicy.Present");
            cut.Find("[data-testid='agents-approver-policy-version']").TextContent
                .ShouldContain("Agents.ApproverPolicy.Version");
            cut.Find("[data-testid='agents-approver-policy-disclosure']").TextContent
                .ShouldContain("Agents.ApproverPolicy.Disclosure.UserVisible");

            // The exactly four V1 approver-source kinds (AD-8) each render a row; Unknown is never offered.
            cut.Find("[data-testid='agents-approver-policy-source-Caller']");
            cut.Find("[data-testid='agents-approver-policy-source-PredefinedParty']");
            cut.Find("[data-testid='agents-approver-policy-source-TenantRole']");
            cut.Find("[data-testid='agents-approver-policy-source-ConversationOwner']");
            cut.FindAll("[data-testid='agents-approver-policy-source-Unknown']").ShouldBeEmpty();

            // Per-source availability is indeterminate without the live read path, so each row renders the neutral
            // "not yet evaluated" state — never an affirmative "Available" that would show an unknown dependency as
            // ready (AD-12 fail-closed; AC5 icon+colour+text agree on "unknown").
            cut.Find("[data-testid='agents-approver-policy-source-availability-Caller']").TextContent
                .ShouldContain("Agents.ApproverPolicy.SourceAvailability.Unknown");
            cut.Markup.ShouldNotContain("Agents.ApproverPolicy.SourceAvailability.Available");
        });
    }

    [Fact]
    public void Not_authorized_approver_policy_renders_permission_denied()
    {
        SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.NotAuthorized()));

        IRenderedComponent<ApproverPolicy> cut = RenderPage<ApproverPolicy>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-approver-policy-state']");
            cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title");
        });
    }

    [Fact]
    public void Approver_policy_declares_a_constrained_layout_inside_the_shell()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Draft,
            responseMode: AgentResponseMode.Confirmation));

        IRenderedComponent<FrontComposerShell> cut = RenderInShell<ApproverPolicy>();

        cut.WaitForAssertion(() =>
            cut.Find("#fc-main-content").GetAttribute("data-fc-page-layout").ShouldBe("constrained"));
    }
}
