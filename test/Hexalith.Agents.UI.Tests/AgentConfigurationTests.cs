using System;
using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;
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
        => GivenSetup(AgentUiTestData.Setup(view));

    private void GivenSetup(AgentSetupView setup)
        => SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupResult.Success(setup)));

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
    public void Activation_affordance_is_present_and_never_claims_callability()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-activate']");
            cut.Find("[data-testid='agents-config-disable']");
            cut.Markup.ShouldContain("Agents.Config.Activation.NotCallable");
        });
    }

    [Fact]
    public void Not_authorized_configuration_renders_permission_denied()
    {
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupResult.NotAuthorized()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title"));
    }

    [Fact]
    public void Agent_not_found_configuration_renders_the_empty_surface()
    {
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupResult.NotFound()));

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

    // ===== Story 5.2: truth flow =====

    [Fact]
    public void Projection_confirmed_setup_renders_its_versions_and_freshness()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Active),
            configurationVersion: 4,
            projectionVersion: "11"));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-truth-stage']").TextContent
                .ShouldContain("Agents.Config.Truth.Stage.ProjectionConfirmed");
            cut.Find("[data-testid='agents-config-configuration-version']").TextContent.ShouldContain("4");
            cut.Find("[data-testid='agents-config-projection-version']").TextContent.ShouldContain("11");
            cut.Find("[data-testid='agents-config-freshness']").TextContent
                .ShouldContain("Agents.Config.Truth.Freshness.Current");
        });
    }

    [Fact]
    public void Lagging_projection_renders_authoritative_pending_and_stale()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Active),
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-truth-stage']").TextContent
                .ShouldContain("Agents.Config.Truth.Stage.AuthoritativePending");
            cut.Find("[data-testid='agents-config-freshness']").TextContent
                .ShouldContain("Agents.Config.Truth.Freshness.Stale");
        });
    }

    [Fact]
    public void Submitting_a_response_mode_change_rereads_at_the_accepted_version()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3));
        SetupGateway.ConfigureResponseModeAsync(Arg.Any<AgentResponseMode>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Submitted(
                new AgentCommandAcceptance("agent-1", "message-1", "correlation-1", AgentSetupTruthState.Submitted))));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        cut.Find("[data-testid='agents-config-response-mode-submit']").Click();

        // The re-read asks for the configuration version the acceptance implies, never the one already shown.
        cut.WaitForAssertion(() => SetupGateway.Received().GetSetupAsync(4, Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void A_rejected_write_reports_its_typed_status_and_triggers_no_reread()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.NotAuthorized)));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.NotAuthorized");
            SetupGateway.DidNotReceive().GetSetupAsync(4, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public void Activating_rereads_at_the_unchanged_configuration_version()
    {
        // AgentActivated does not bump the configuration version, so demanding current+1 would leave the page
        // permanently claiming the projection is behind.
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        GivenAcceptedWrite(gateway => gateway.ActivateAsync(Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            SetupGateway.Received().GetSetupAsync(3, Arg.Any<CancellationToken>());
            SetupGateway.DidNotReceive().GetSetupAsync(4, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public void Disabling_rereads_at_the_unchanged_configuration_version()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Active), configurationVersion: 3));
        GivenAcceptedWrite(gateway => gateway.DisableAsync(Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-disable']"));
        cut.Find("[data-testid='agents-config-disable']").Click();

        cut.WaitForAssertion(() =>
        {
            SetupGateway.Received().GetSetupAsync(3, Arg.Any<CancellationToken>());
            SetupGateway.DidNotReceive().GetSetupAsync(4, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public void A_catch_up_read_that_confirms_the_projection_stops_reporting_the_write_as_submitted()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        GivenAcceptedWrite(gateway => gateway.ActivateAsync(Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-truth-stage']").TextContent
                .ShouldContain("Agents.Config.Truth.Stage.ProjectionConfirmed");

            // Durable truth supersedes the in-flight notice; keeping both would say "still submitting" forever.
            cut.FindAll("[data-testid='agents-config-write-state']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void A_catch_up_read_that_is_still_pending_keeps_reporting_the_submitted_write()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending));
        GivenAcceptedWrite(gateway => gateway.ActivateAsync(Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.Submitted"));
    }

    [Fact]
    public void A_gateway_that_throws_reports_a_typed_write_failure_and_leaves_the_page_usable()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<CancellationToken>())
            .Returns<Task<AgentSetupWriteResult>>(_ => throw new InvalidOperationException("transport blew up"));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Unavailable");

            // Neither the exception text nor a busy-locked form survives the failure.
            cut.Markup.ShouldNotContain("transport blew up");
            cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeFalse();
        });
    }

    [Fact]
    public void An_unavailable_setup_read_renders_the_unavailable_surface_not_a_denial()
    {
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupResult.Unavailable()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.Unavailable.Title");
            cut.Markup.ShouldNotContain("Agents.Surface.PermissionDenied.Title");
        });
    }

    // ===== Story 5.2: the configure write =====

    [Fact]
    public void Saving_the_configuration_submits_the_typed_command_and_rereads_at_the_next_version()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        UpdateAgentConfiguration? submitted = null;
        SetupGateway
            .UpdateConfigurationAsync(
                Arg.Do<UpdateAgentConfiguration>(command => submitted = command),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-submit']"));
        cut.Find("[data-testid='agents-config-display-name-input']").Change("hexa renamed");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("instructions long enough to be valid");
        cut.Find("[data-testid='agents-config-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            UpdateAgentConfiguration command = submitted.ShouldNotBeNull();
            command.DisplayName.ShouldBe("hexa renamed");
            command.Instructions.ShouldBe("instructions long enough to be valid");

            // A configuration write does bump the version, so the catch-up read asks for the next one.
            SetupGateway.Received().GetSetupAsync(4, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public void The_saved_instructions_are_never_rendered_back_onto_the_page()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, hasInstructions: true, instructionsValid: true),
            configurationVersion: 3));
        SetupGateway.UpdateConfigurationAsync(Arg.Any<UpdateAgentConfiguration>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-submit']"));
        cut.Find("[data-testid='agents-config-instructions-input']").Change("a very sensitive system prompt");
        cut.Find("[data-testid='agents-config-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            // AD-14: after a save the page reports presence/validity/version, never the stored text.
            cut.Markup.ShouldNotContain("a very sensitive system prompt");
            cut.Find("[data-testid='agents-config-instructions-presence']").TextContent
                .ShouldContain("Agents.Config.Instructions.Present");
            cut.Find("[data-testid='agents-config-instructions-version']");
        });
    }

    [Fact]
    public void The_configure_save_is_disabled_until_instructions_are_entered()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-config-submit']").HasAttribute("disabled").ShouldBeTrue());
    }

    private static AgentSetupWriteResult Accepted()
        => AgentSetupWriteResult.Submitted(
            new AgentCommandAcceptance("agent-1", "message-1", "correlation-1", AgentSetupTruthState.Submitted));

    private void GivenAcceptedWrite(Func<IAgentSetupGateway, Task<AgentSetupWriteResult>> write)
        => write(SetupGateway).Returns(Task.FromResult(Accepted()));
}
