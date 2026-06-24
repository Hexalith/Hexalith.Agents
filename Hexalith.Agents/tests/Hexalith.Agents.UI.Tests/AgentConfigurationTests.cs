using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.FrontComposer.Shell.Components.Layout;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC4 — the configuration form uses a constrained Fluent layout and a mutually-exclusive response-mode radio group
/// whose copy states changes affect future Agent Calls only. Instruction text and content-safety policy content are
/// never rendered (only presence/validity/version).
/// </summary>
public sealed class AgentConfigurationTests : AgentsTestContext
{
    private void GivenConfiguration(AgentStatusView view)
        => SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.Success(view)));

    [Fact]
    public void Configuration_declares_constrained_layout_inside_the_shell()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Confirmation));

        IRenderedComponent<FrontComposerShell> cut = RenderInShell<AgentConfiguration>();

        cut.WaitForAssertion(() =>
            cut.Find("#fc-main-content").GetAttribute("data-fc-page-layout").ShouldBe("constrained"));
        cut.Find("[data-testid='agents-config']");
    }

    [Fact]
    public void Response_mode_renders_both_mutually_exclusive_options_and_the_future_only_note()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-response-mode']");
            cut.Find("[data-testid='agents-response-mode-automatic']");
            cut.Find("[data-testid='agents-response-mode-confirmation']");
            cut.Find("[data-testid='agents-response-mode-future-note']").TextContent
                .ShouldContain("Agents.Config.ResponseMode.FutureOnlyNote");
        });
    }

    [Fact]
    public void Configuration_surfaces_instruction_presence_and_version_only_never_raw_text()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Draft,
            hasInstructions: true,
            instructionsValid: true,
            hasContentSafetyPolicy: true));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-instructions-presence']");
            cut.Find("[data-testid='agents-config-instructions-version']");
            cut.Find("[data-testid='agents-config-content-safety-version']");
            // Presence/validity/version keys only — the safe view carries no instruction or policy content to leak.
            cut.Markup.ShouldContain("Agents.Config.Instructions.Version");
            cut.Markup.ShouldContain("Agents.Config.ContentSafety.Version");
        });
    }

    [Fact]
    public void Activation_affordance_is_present_but_deferred()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-activate']");
            cut.Markup.ShouldContain("Agents.Config.Activation.Deferred");
        });
    }

    [Fact]
    public void Not_authorized_configuration_renders_permission_denied()
    {
        SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.NotAuthorized()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title"));
    }

    [Fact]
    public void Agent_not_found_configuration_renders_the_empty_surface()
    {
        SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.NotFound()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-state']");
            cut.Markup.ShouldContain("Agents.Surface.Empty.Title");
        });
    }

    [Fact]
    public void Loaded_response_mode_is_reflected_in_the_toggle()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Confirmation));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
            cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation));
    }

    [Fact]
    public void Content_safety_presence_version_and_mode_overrides_are_rendered_never_policy_content()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Active,
            hasContentSafetyPolicy: true,
            hasAutomaticContentSafetyOverride: true,
            hasConfirmationContentSafetyOverride: true));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-content-safety-presence']").TextContent
                .ShouldContain("Agents.Config.ContentSafety.Present");
            cut.Find("[data-testid='agents-config-content-safety-version']");
            cut.Markup.ShouldContain("Agents.Config.ContentSafety.AutomaticOverride");
            cut.Markup.ShouldContain("Agents.Config.ContentSafety.ConfirmationOverride");
        });
    }

    [Fact]
    public void Approver_policy_summary_and_link_to_the_builder_are_present()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Active,
            hasApproverPolicy: true,
            approverPolicyDisclosure: ApproverPolicyBasisDisclosure.UserVisible));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-approver-presence']").TextContent
                .ShouldContain("Agents.ApproverPolicy.Present");
            cut.Find("[data-testid='agents-config-approver-link']")
                .GetAttribute("href").ShouldBe("/agents/approver-policy");
        });
    }

    [Fact]
    public void Activation_blockers_are_listed_inline_on_the_configuration_form()
    {
        GivenConfiguration(AgentUiTestData.Status(
            AgentLifecycleStatus.Draft,
            blockers: [AgentActivationBlocker.MissingPartyIdentity, AgentActivationBlocker.MissingProviderSelection]));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-blockers']");
            cut.Find("[data-testid='agents-config-blocker-MissingPartyIdentity']").TextContent
                .ShouldContain("Agents.Readiness.Blocker.MissingPartyIdentity");
            cut.Find("[data-testid='agents-config-blocker-MissingProviderSelection']").TextContent
                .ShouldContain("Agents.Readiness.Blocker.MissingProviderSelection");
            cut.FindAll("[data-testid='agents-config-blockers-none']").ShouldBeEmpty();
        });
    }
}
