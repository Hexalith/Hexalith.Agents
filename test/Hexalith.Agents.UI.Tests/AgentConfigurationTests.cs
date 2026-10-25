using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

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
    public AgentConfigurationTests()
    {
        Authorization.SetAuthorized("administrator");
        Authorization.SetClaims(new System.Security.Claims.Claim("sub", "administrator"),
            new System.Security.Claims.Claim("tenantId", "tenant-1"));
        Services.Configure<AgentSetupTargetOptions>(options => options.AgentId = "agent-1");
    }

    [Theory]
    [InlineData("unavailable")]
    [InlineData("missing")]
    [InlineData("unknown")]
    [InlineData("submitted")]
    [InlineData("loading")]
    [InlineData("deferred")]
    public async Task MissingAuthoritativeSetupRefusesStaleSubmitEventsWithoutStartingAnAttempt(string state)
    {
        IUlidFactory identities = Substitute.For<IUlidFactory>();
        Services.AddSingleton(identities);
        if (state == "deferred")
        {
            Services.AddSingleton<IAgentSetupGateway, DeferredAgentSetupGateway>();
        }
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(state switch
        {
            "unavailable" => Task.FromResult(AgentSetupResult.Unavailable()),
            "missing" => Task.FromResult(AgentSetupResult.NotFound()),
            "unknown" => Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(truthState: AgentSetupTruthState.Unknown))),
            "submitted" => Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(truthState: AgentSetupTruthState.Submitted))),
            _ => new TaskCompletionSource<AgentSetupResult>().Task,
        });
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        await cut.InvokeAsync(async () =>
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(AgentConfiguration).GetField("_instructionsDraft", flags)!.SetValue(cut.Instance, "retained unsent instructions");
            foreach (string method in new[] { "SubmitConfigurationAsync", "SubmitResponseModeAsync", "ActivateAsync", "DisableAsync" })
            {
                await ((Task)typeof(AgentConfiguration).GetMethod(method, flags)!.Invoke(cut.Instance, null)!).ConfigureAwait(false);
            }
            typeof(AgentConfiguration).GetField("_activeAttempt", flags)!.GetValue(cut.Instance).ShouldBeNull();
        });
        SetupGateway.ReceivedCalls().ShouldAllBe(call => call.GetMethodInfo().Name == nameof(IAgentSetupGateway.GetSetupAsync));
        identities.ReceivedCalls().ShouldBeEmpty();
        cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
        if (state == "unknown")
        {
            cut.Markup.ShouldContain("Agents.Surface.Unavailable.Title");
        }
    }

    [Fact]
    public async Task FreshRejectionReleasesTheAttemptAndTheNextSubmitUsesNewIdentities()
    {
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 3));
        var options = new List<AgentOperationOptions>();
        SetupGateway.UpdateConfigurationAsync(Arg.Any<UpdateAgentConfiguration>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                options.Add(call.ArgAt<AgentOperationOptions>(1));
                return Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.Rejected));
            });
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-submit']"));
        cut.Find("[data-testid='agents-config-instructions-input']").Change("instructions to correct after rejection");
        await cut.Find("[data-testid='agents-config-submit']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.Rejected");
        cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
        await cut.Find("[data-testid='agents-config-submit']").ClickAsync(new MouseEventArgs());
        options.Count.ShouldBe(2);
        options[1].IdempotencyKey.ShouldNotBe(options[0].IdempotencyKey);
        options[1].CorrelationId.ShouldNotBe(options[0].CorrelationId);
    }

    [Fact]
    public async Task FreshRejectedActivationRefreshesTheVersionWithoutReplacingDrafts()
    {
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 3))),
            Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 7))));
        var attempts = new List<AgentOperationOptions>();
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            attempts.Add(call.ArgAt<AgentOperationOptions>(0));
            return Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.Rejected));
        });
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-display-name-input']").Change("administrator draft");
        cut.Find("[data-testid='agents-config-description-input']").Change("description draft");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("unsent instructions draft");
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-configuration-version']").TextContent.ShouldBe("7");
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("administrator draft");
        cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value").ShouldBe("description draft");
        cut.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("unsent instructions draft");
        cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation);
        cut.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.Rejected");
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        attempts.Select(options => options.ExpectedConfigurationVersion).ShouldBe([3, 7]);
        attempts[1].IdempotencyKey.ShouldNotBe(attempts[0].IdempotencyKey);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedRejectionRefreshPreservesRejectionAndRefusesStaleNewWrites(bool throws)
    {
        int reads = 0;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(_ => ++reads == 1
            ? Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 3)))
            : throws ? Task.FromException<AgentSetupResult>(new HttpRequestException("refresh unavailable"))
                : Task.FromResult(AgentSetupResult.Unavailable()));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.Rejected)));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-instructions-input']").Change("draft survives unavailable refresh");
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.Rejected");
        cut.Markup.ShouldContain("Agents.Surface.Unavailable.Title");
        await cut.InvokeAsync(async () =>
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(AgentConfiguration).GetField("_instructionsDraft", flags)!.GetValue(cut.Instance)
                .ShouldBe("draft survives unavailable refresh");
            await ((Task)typeof(AgentConfiguration).GetMethod("ActivateAsync", flags)!.Invoke(cut.Instance, null)!).ConfigureAwait(false);
            await ((Task)typeof(AgentConfiguration).GetMethod("DisableAsync", flags)!.Invoke(cut.Instance, null)!).ConfigureAwait(false);
            typeof(AgentConfiguration).GetField("_activeAttempt", flags)!.GetValue(cut.Instance).ShouldBeNull();
        });
        await SetupGateway.Received(1).ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>());
        await SetupGateway.DidNotReceive().DisableAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>());
    }

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
    public void ConfigurationGroupsTitledSectionsAndKeepsActionsVisible()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() => cut.FindComponents<FluentAccordionItem>().Count.ShouldBe(10));
        cut.FindComponents<FluentAccordion>().Count.ShouldBe(1);
        foreach (IRenderedComponent<FluentAccordionItem> item in cut.FindComponents<FluentAccordionItem>())
        {
            item.Instance.Expanded.ShouldBeTrue();
            item.Instance.Header.ShouldStartWith("Agents.Config.Section.");
        }
        IElement accordion = cut.Find("[data-testid='agents-config-sections']");
        accordion.QuerySelector("[data-testid='agents-config-submit']").ShouldNotBeNull();
        accordion.QuerySelector("[data-testid='agents-config-activate']").ShouldBeNull();
        accordion.QuerySelector("[data-testid='agents-config-write-live-region']").ShouldBeNull();
        cut.Find("[data-testid='agents-config-activate']");
        cut.Find("[data-testid='agents-config-disable']");
    }

    [Fact]
    public async Task CollapsedConfigurationSectionStaysCollapsedAfterAFieldEdit()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.FindComponents<FluentAccordionItem>().Count.ShouldBe(10));
        IRenderedComponent<FluentAccordionItem> identity = cut.FindComponents<FluentAccordionItem>()[0];

        await cut.InvokeAsync(() => identity.Instance.SetExpandedAsync(false));
        identity.Instance.Expanded.ShouldBeFalse();
        cut.Find("[data-testid='agents-config-display-name-input']").Change("changed draft");

        identity.Instance.Expanded.ShouldBeFalse();
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("changed draft");
    }

    [Fact]
    public async Task Write_status_live_region_contains_the_notice_and_excludes_recovery_controls()
    {
        GivenConfiguration(AgentUiTestData.Status(AgentLifecycleStatus.Draft));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify)));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        IElement liveRegion = cut.Find("[data-testid='agents-config-write-live-region']");
        liveRegion.GetAttribute("role").ShouldBe("status");
        liveRegion.GetAttribute("aria-live").ShouldBe("polite");
        liveRegion.GetAttribute("aria-atomic").ShouldBe("true");
        liveRegion.QuerySelector("[data-testid='agents-config-write-state']")
            .ShouldNotBeNull().TextContent.ShouldContain("Agents.Config.Write.UnableToVerify");
        liveRegion.QuerySelector("[data-testid='agents-config-write-recovery']").ShouldBeNull();
        cut.Find("[data-testid='agents-config-write-recovery']");
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
    public void Activation_is_disabled_when_the_displayed_configuration_version_is_not_positive()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft),
            configurationVersion: 0));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeTrue());
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
    public async Task Submitting_a_response_mode_change_rereads_at_the_accepted_version()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3));
        SetupGateway.ConfigureResponseModeAsync(
                Arg.Any<AgentResponseMode>(),
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Submitted(
                new AgentCommandAcceptance(
                    "agent-1",
                    "message-1",
                    "correlation-1",
                    AgentSetupTruthState.AuthoritativePending,
                    AgentSetupWriteEffect.Applied,
                    7))));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        cut.Find("[data-testid='agents-config-response-mode-submit']").Click();

        // The re-read asks for the configuration version the acceptance implies, never the one already shown.
        cut.WaitForAssertion(() => SetupGateway.Received().GetSetupAsync(7, Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void A_rejected_write_reports_its_typed_status_and_triggers_no_reread()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
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

    [Theory]
    [InlineData(AgentSetupWriteStatus.ValidationFailed)]
    [InlineData(AgentSetupWriteStatus.Conflict)]
    [InlineData(AgentSetupWriteStatus.Rejected)]
    public void A_terminal_configuration_failure_restores_the_exact_draft_and_reenables_editing(
        AgentSetupWriteStatus failureStatus)
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        SetupGateway.UpdateConfigurationAsync(
                Arg.Any<UpdateAgentConfiguration>(),
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Failed(failureStatus)));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-submit']"));
        cut.Find("[data-testid='agents-config-instructions-input']").Change("exact draft to correct and resubmit");
        cut.Find("[data-testid='agents-config-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            IElement instructions = cut.Find("[data-testid='agents-config-instructions-input']");
            instructions.GetAttribute("value").ShouldBe("exact draft to correct and resubmit");
            instructions.HasAttribute("disabled").ShouldBeFalse();
            cut.Find("[data-testid='agents-config-submit']").HasAttribute("disabled").ShouldBeFalse();
            cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void An_unresolved_attempt_disables_every_mutable_configuration_control()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify)));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-display-name-input']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-description-input']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-instructions-input']").HasAttribute("disabled").ShouldBeTrue();
            cut.FindComponent<ResponseModeToggle>().Instance.Disabled.ShouldBeTrue();
        });
    }

    [Fact]
    public async Task An_already_applied_receipt_reports_the_no_op_without_polling()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3));
        SetupGateway.ConfigureResponseModeAsync(
                AgentResponseMode.Confirmation,
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Submitted(
                new AgentCommandAcceptance(
                    "agent-1",
                    "message-1",
                    "correlation-1",
                    AgentSetupTruthState.AuthoritativePending,
                    AgentSetupWriteEffect.AlreadyApplied,
                    3))));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        cut.Find("[data-testid='agents-config-response-mode-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.AlreadyApplied");
            cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-config-write-refresh']").ShouldBeEmpty();
        });
        await SetupGateway.DidNotReceive().GetSetupAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Starting_a_later_attempt_clears_a_prior_terminal_outcome_while_the_new_write_is_in_flight()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3));
        SetupGateway.ConfigureResponseModeAsync(
                AgentResponseMode.Confirmation,
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Submitted(
                new AgentCommandAcceptance(
                    "agent-1",
                    "message-1",
                    "correlation-1",
                    AgentSetupTruthState.AuthoritativePending,
                    AgentSetupWriteEffect.AlreadyApplied,
                    3))));
        var stalledWrite = new TaskCompletionSource<AgentSetupWriteResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(stalledWrite.Task);

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        await cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AlreadyApplied");

        Task activation = cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='agents-config-write-state']").ShouldBeEmpty();
            cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeTrue();
        });
        stalledWrite.SetResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.ValidationFailed));
        await activation;
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.ValidationFailed");
    }

    [Fact]
    public void Activating_rereads_at_the_receipt_target_configuration_version()
    {
        GivenLifecycleTransition(
            AgentLifecycleStatus.Draft,
            AgentLifecycleStatus.Active,
            confirmedConfigurationVersion: 7);
        GivenAcceptedWrite(
            gateway => gateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()),
            targetConfigurationVersion: 7);

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            SetupGateway.Received().GetSetupAsync(7, Arg.Any<CancellationToken>());
            SetupGateway.DidNotReceive().GetSetupAsync(4, Arg.Any<CancellationToken>());
            SetupGateway.DidNotReceive().GetSetupAsync(3, Arg.Any<CancellationToken>());
            cut.Find("[data-testid='agents-config-lifecycle']").TextContent
                .ShouldContain("Agents.Lifecycle.Active");
        });
    }

    [Fact]
    public async Task Activation_retry_reuses_the_displayed_configuration_version_and_exact_options()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft),
            configurationVersion: 7));
        var submittedOptions = new List<AgentOperationOptions>();
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                submittedOptions.Add(call.ArgAt<AgentOperationOptions>(0));
                return Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify));
            });

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-write-retry']"));

        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());

        submittedOptions.Count.ShouldBe(2);
        submittedOptions[1].ShouldBeSameAs(submittedOptions[0]);
        submittedOptions[0].ExpectedConfigurationVersion.ShouldBe(7);
        submittedOptions[0].CorrelationId.ShouldNotBeNull().ShouldMatch("^[0-9A-HJKMNP-TV-Z]{26}$");
        submittedOptions[0].IdempotencyKey.ShouldNotBeNull().ShouldMatch("^[0-9A-HJKMNP-TV-Z]{26}$");
        submittedOptions[0].CorrelationId.ShouldNotBe(submittedOptions[0].IdempotencyKey);
    }

    [Fact]
    public void Disabling_rereads_at_the_receipt_target_configuration_version()
    {
        GivenLifecycleTransition(
            AgentLifecycleStatus.Active,
            AgentLifecycleStatus.Disabled,
            confirmedConfigurationVersion: 7);
        GivenAcceptedWrite(
            gateway => gateway.DisableAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()),
            targetConfigurationVersion: 7);

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-disable']"));
        cut.Find("[data-testid='agents-config-disable']").Click();

        cut.WaitForAssertion(() =>
        {
            SetupGateway.Received().GetSetupAsync(7, Arg.Any<CancellationToken>());
            SetupGateway.DidNotReceive().GetSetupAsync(4, Arg.Any<CancellationToken>());
            SetupGateway.DidNotReceive().GetSetupAsync(3, Arg.Any<CancellationToken>());
            cut.Find("[data-testid='agents-config-lifecycle']").TextContent
                .ShouldContain("Agents.Lifecycle.Disabled");
        });
    }

    [Fact]
    public async Task An_immediately_confirmed_projection_announces_confirmation_and_does_not_retry()
    {
        GivenLifecycleTransition(
            AgentLifecycleStatus.Draft,
            AgentLifecycleStatus.Active,
            confirmedConfigurationVersion: 7);
        GivenAcceptedWrite(
            gateway => gateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()),
            targetConfigurationVersion: 7);

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        Clock.Advance(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(() => Task.CompletedTask);

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-truth-stage']").TextContent
                .ShouldContain("Agents.Config.Truth.Stage.ProjectionConfirmed");

            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Confirmed");
            SetupGateway.Received(1).GetSetupAsync(7, Arg.Any<CancellationToken>());
            SetupGateway.DidNotReceive().GetSetupAsync(4, Arg.Any<CancellationToken>());
            cut.Find("[data-testid='agents-config-disable']").HasAttribute("disabled").ShouldBeFalse();
        });
    }

    [Fact]
    public async Task A_delayed_projection_confirmation_polls_the_same_version_and_announces_confirmation()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        AgentSetupView confirmed = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Confirmation),
            configurationVersion: 4,
            projectionVersion: "8");
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)),
            Task.FromResult(AgentSetupResult.Success(pending)),
            Task.FromResult(AgentSetupResult.Success(confirmed)));
        GivenAcceptedWrite(gateway => gateway.ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            SetupGateway.Received(1).GetSetupAsync(4, Arg.Any<CancellationToken>());
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Submitted");
            cut.Find("[data-testid='agents-config-truth-stage']").TextContent
                .ShouldContain("Agents.Config.Truth.Stage.AuthoritativePending");
        });

        Clock.Advance(TimeSpan.FromMilliseconds(250));
        await submission;

        cut.Find("[data-testid='agents-config-truth-stage']").TextContent
            .ShouldContain("Agents.Config.Truth.Stage.ProjectionConfirmed");
        cut.Find("[data-testid='agents-config-projection-version']").TextContent.ShouldContain("8");
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.Confirmed");
        await SetupGateway.Received(2).GetSetupAsync(4, Arg.Any<CancellationToken>());
        await SetupGateway.DidNotReceive().GetSetupAsync(
            Arg.Is<int?>(version => version.HasValue && version.Value != 4),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Automatic_catch_up_preserves_drafts_through_the_immediate_and_later_projection_reads()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic) with
            {
                DisplayName = "initial name",
                Description = "initial description",
            },
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic) with
            {
                DisplayName = "pending authoritative name",
                Description = "pending authoritative description",
            },
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        AgentSetupView confirmed = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Active, responseMode: AgentResponseMode.Automatic) with
            {
                DisplayName = "confirmed authoritative name",
                Description = "confirmed authoritative description",
            },
            configurationVersion: 4,
            projectionVersion: "8");
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)),
            Task.FromResult(AgentSetupResult.Success(pending)),
            Task.FromResult(AgentSetupResult.Success(confirmed)));
        GivenAcceptedWrite(gateway => gateway.ActivateAsync(
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        cut.Find("[data-testid='agents-config-display-name-input']").Change("submitted administrator name");
        cut.Find("[data-testid='agents-config-description-input']").Change("submitted administrator description");
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            SetupGateway.Received(1).GetSetupAsync(4, Arg.Any<CancellationToken>());
            cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value")
                .ShouldBe("submitted administrator name");
            cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value")
                .ShouldBe("submitted administrator description");
            cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation);
        });
        // The retained attempt locks all mutable inputs. These unrelated drafts were entered before
        // submission and must survive both reads without simulating edits a user cannot perform.
        cut.Find("[data-testid='agents-config-display-name-input']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-config-description-input']").HasAttribute("disabled").ShouldBeTrue();
        cut.FindComponent<ResponseModeToggle>().Instance.Disabled.ShouldBeTrue();

        Clock.Advance(TimeSpan.FromMilliseconds(250));
        await submission;

        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value")
            .ShouldBe("submitted administrator name");
        cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value")
            .ShouldBe("submitted administrator description");
        cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation);
        cut.Find("[data-testid='agents-config-display-name-input']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='agents-config-description-input']").HasAttribute("disabled").ShouldBeFalse();
        cut.FindComponent<ResponseModeToggle>().Instance.Disabled.ShouldBeFalse();
        cut.Find("[data-testid='agents-config-truth-stage']").TextContent
            .ShouldContain("Agents.Config.Truth.Stage.ProjectionConfirmed");
    }

    [Fact]
    public async Task Polling_exhaustion_preserves_pending_truth_and_stops_further_reads()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic) with
            {
                DisplayName = "initial name",
                Description = "initial description",
            },
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic) with
            {
                DisplayName = "pending name",
                Description = "pending description",
            },
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        AgentSetupView confirmed = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Active, responseMode: AgentResponseMode.Automatic) with
            {
                DisplayName = "confirmed name",
                Description = "confirmed description",
            },
            configurationVersion: 4);
        bool confirmManualRefresh = false;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
            Task.FromResult(AgentSetupResult.Success(call.ArgAt<int?>(0) is null
                ? initial
                : confirmManualRefresh ? confirmed : pending)));
        GivenAcceptedWrite(gateway => gateway.ActivateAsync(
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        cut.Find("[data-testid='agents-config-display-name-input']").Change("unsaved administrator name");
        cut.Find("[data-testid='agents-config-description-input']").Change("unsaved administrator description");
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => SetupGateway.Received(1).GetSetupAsync(4, Arg.Any<CancellationToken>()));

        for (int tick = 0; tick < 31; tick++)
        {
            Clock.Advance(TimeSpan.FromMilliseconds(250));
            await cut.InvokeAsync(() => Task.CompletedTask);
        }

        Clock.Advance(TimeSpan.FromMilliseconds(249));
        await cut.InvokeAsync(() => Task.CompletedTask);
        submission.IsCompleted.ShouldBeFalse();
        int readsImmediatelyBeforeTimeout = ExpectedVersionReadCount(4);
        readsImmediatelyBeforeTimeout.ShouldBeGreaterThan(1);
        cut.Find("[data-testid='agents-config-truth-stage']").TextContent
            .ShouldContain("Agents.Config.Truth.Stage.AuthoritativePending");

        Clock.Advance(TimeSpan.FromMilliseconds(1));
        await submission;
        ExpectedVersionReadCount(4).ShouldBe(readsImmediatelyBeforeTimeout);
        Clock.Advance(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(() => Task.CompletedTask);

        ExpectedVersionReadCount(4).ShouldBe(readsImmediatelyBeforeTimeout);
        cut.Find("[data-testid='agents-config-truth-stage']").TextContent
            .ShouldContain("Agents.Config.Truth.Stage.AuthoritativePending");
        cut.Find("[data-testid='agents-config-freshness']").TextContent
            .ShouldContain("Agents.Config.Truth.Freshness.Stale");
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AwaitingProjection");
        cut.Find("[data-testid='agents-config-write-refresh']");
        cut.Find("[data-testid='agents-config-write-retry']");
        cut.Find("[data-testid='agents-config-write-abandon']");
        await SetupGateway.DidNotReceive().GetSetupAsync(
            Arg.Is<int?>(version => version.HasValue && version.Value != 4),
            Arg.Any<CancellationToken>());

        // Timeout retains the exact attempt, so only its recovery controls are interactive.
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value")
            .ShouldBe("unsaved administrator name");
        cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value")
            .ShouldBe("unsaved administrator description");
        cut.Find("[data-testid='agents-config-display-name-input']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-config-description-input']").HasAttribute("disabled").ShouldBeTrue();
        cut.FindComponent<ResponseModeToggle>().Instance.Disabled.ShouldBeTrue();
        confirmManualRefresh = true;
        await cut.Find("[data-testid='agents-config-write-refresh']").ClickAsync(new MouseEventArgs());

        ExpectedVersionReadCount(4).ShouldBe(readsImmediatelyBeforeTimeout + 1);
        await SetupGateway.Received(1).ActivateAsync(
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>());
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.Confirmed");
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value")
            .ShouldBe("unsaved administrator name");
        cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value")
            .ShouldBe("unsaved administrator description");
        cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation);
        cut.Find("[data-testid='agents-config-display-name-input']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='agents-config-description-input']").HasAttribute("disabled").ShouldBeFalse();
        cut.FindComponent<ResponseModeToggle>().Instance.Disabled.ShouldBeFalse();
        cut.Find("[data-testid='agents-config-truth-stage']").TextContent
            .ShouldContain("Agents.Config.Truth.Stage.ProjectionConfirmed");
    }

    [Fact]
    public async Task Refresh_announces_its_in_flight_state_before_the_single_read_completes()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        AgentSetupView confirmed = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Confirmation),
            configurationVersion: 4);
        TaskCompletionSource<AgentSetupResult> stalledRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool manualRefreshStarted = false;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.ArgAt<int?>(0) is null)
            {
                return Task.FromResult(AgentSetupResult.Success(initial));
            }

            return manualRefreshStarted
                ? stalledRefresh.Task
                : Task.FromResult(AgentSetupResult.Success(pending));
        });
        GivenAcceptedWrite(gateway => gateway.ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());

        for (int tick = 0; tick < 33; tick++)
        {
            Clock.Advance(TimeSpan.FromMilliseconds(250));
            await cut.InvokeAsync(() => Task.CompletedTask);
        }

        await submission;
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AwaitingProjection");

        manualRefreshStarted = true;
        Task refresh = cut.Find("[data-testid='agents-config-write-refresh']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Refreshing");
            cut.Find("[data-testid='agents-config-write-refresh']").HasAttribute("disabled").ShouldBeTrue();
            refresh.IsCompleted.ShouldBeFalse();
        });

        stalledRefresh.SetResult(AgentSetupResult.Success(confirmed));
        await refresh;

        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.Confirmed");
    }

    [Theory]
    [InlineData(AgentSetupWriteStatus.Conflict)]
    [InlineData(AgentSetupWriteStatus.ValidationFailed)]
    [InlineData(AgentSetupWriteStatus.AlreadyApplied)]
    [InlineData(AgentSetupWriteStatus.Unavailable)]
    [InlineData(AgentSetupWriteStatus.UnableToVerify)]
    [InlineData(AgentSetupWriteStatus.Rejected)]
    [InlineData(AgentSetupWriteStatus.Submitted)]
    public async Task Retrying_an_accepted_pending_attempt_never_erases_its_acceptance(
        AgentSetupWriteStatus retryOutcome)
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
            Task.FromResult(AgentSetupResult.Success(call.ArgAt<int?>(0) is null ? initial : pending)));
        int submissions = 0;
        SetupGateway.ConfigureResponseModeAsync(
                AgentResponseMode.Confirmation,
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                submissions++;
                if (submissions == 1)
                {
                    return Task.FromResult(Accepted());
                }

                if (retryOutcome == AgentSetupWriteStatus.Submitted)
                {
                    return Task.FromResult(AgentSetupWriteResult.Submitted(new AgentCommandAcceptance(
                        "different-agent", "different-message", "different-correlation", AgentSetupTruthState.AuthoritativePending,
                        AgentSetupWriteEffect.Applied, 99)));
                }

                return Task.FromResult(retryOutcome == AgentSetupWriteStatus.AlreadyApplied
                    ? AgentSetupWriteResult.Submitted(new AgentCommandAcceptance(
                        "agent-1",
                        "message-1",
                        "correlation-1",
                        AgentSetupTruthState.ProjectionConfirmed,
                        AgentSetupWriteEffect.AlreadyApplied,
                        3))
                    : AgentSetupWriteResult.Failed(retryOutcome));
            });

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());

        for (int tick = 0; tick < 33; tick++)
        {
            Clock.Advance(TimeSpan.FromMilliseconds(250));
            await cut.InvokeAsync(() => Task.CompletedTask);
        }

        await submission;
        int readsBeforeRetry = ExpectedVersionReadCount(4);
        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());

        submissions.ShouldBe(2);
        ExpectedVersionReadCount(4).ShouldBe(readsBeforeRetry);
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.RetryUnresolved");
        cut.Find("[data-testid='agents-config-write-refresh']");
        cut.Find("[data-testid='agents-config-write-retry']");
        cut.Find("[data-testid='agents-config-write-abandon']");
    }

    [Theory]
    [InlineData(AgentSetupWriteStatus.NotAuthorized)]
    [InlineData(AgentSetupWriteStatus.NotFound)]
    public async Task Retrying_an_accepted_pending_attempt_surfaces_a_fresh_authorization_or_existence_denial(
        AgentSetupWriteStatus retryOutcome)
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
            Task.FromResult(AgentSetupResult.Success(call.ArgAt<int?>(0) is null ? initial : pending)));
        int submissions = 0;
        SetupGateway.ConfigureResponseModeAsync(
                AgentResponseMode.Confirmation,
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                submissions++;
                return Task.FromResult(submissions == 1
                    ? Accepted()
                    : AgentSetupWriteResult.Failed(retryOutcome));
            });

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());

        for (int tick = 0; tick < 33; tick++)
        {
            Clock.Advance(TimeSpan.FromMilliseconds(250));
            await cut.InvokeAsync(() => Task.CompletedTask);
        }

        await submission;
        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());

        submissions.ShouldBe(2);
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain($"Agents.Config.Write.{retryOutcome}");
        cut.FindAll("[data-testid='agents-config-write-recovery']").ShouldBeEmpty();
    }

    [Fact]
    public async Task A_retry_that_throws_after_a_masked_retry_does_not_keep_the_earlier_announcement()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
            Task.FromResult(AgentSetupResult.Success(call.ArgAt<int?>(0) is null ? initial : pending)));
        int submissions = 0;
        SetupGateway.ConfigureResponseModeAsync(
                AgentResponseMode.Confirmation,
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                submissions++;
                return submissions switch
                {
                    1 => Task.FromResult(Accepted()),
                    2 => Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.Conflict)),
                    _ => throw new InvalidOperationException("transport failure"),
                };
            });

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());

        for (int tick = 0; tick < 33; tick++)
        {
            Clock.Advance(TimeSpan.FromMilliseconds(250));
            await cut.InvokeAsync(() => Task.CompletedTask);
        }

        await submission;

        // First retry resolves to Conflict while an acceptance is retained: masked as RetryUnresolved.
        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.RetryUnresolved");

        // Second retry throws a transport exception: the stale RetryUnresolved announcement must not survive it.
        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());

        submissions.ShouldBe(3);
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldNotContain("Agents.Config.Write.RetryUnresolved");
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AwaitingProjection");
    }

    [Fact]
    public async Task Disposing_during_polling_cancels_the_lifetime_token_and_prevents_later_reads()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        CancellationToken pollingToken = default;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.ArgAt<int?>(0) is null)
            {
                return Task.FromResult(AgentSetupResult.Success(initial));
            }

            pollingToken = call.ArgAt<CancellationToken>(1);
            return Task.FromResult(AgentSetupResult.Success(pending));
        });
        GivenAcceptedWrite(gateway => gateway.ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => ExpectedVersionReadCount(4).ShouldBe(1));

        cut.Instance.Dispose();
        await submission;
        pollingToken.IsCancellationRequested.ShouldBeTrue();
        Clock.Advance(TimeSpan.FromSeconds(10));

        ExpectedVersionReadCount(4).ShouldBe(1);
        cut.Dispose();
    }

    [Fact]
    public async Task Disposal_cancels_a_non_cooperative_expected_version_read_without_later_mutation()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        TaskCompletionSource<AgentSetupResult> stalledRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken readToken = default;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.ArgAt<int?>(0) is null)
            {
                return Task.FromResult(AgentSetupResult.Success(initial));
            }

            readToken = call.ArgAt<CancellationToken>(1);
            return stalledRead.Task;
        });
        GivenAcceptedWrite(gateway => gateway.ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() =>
        {
            ExpectedVersionReadCount(4).ShouldBe(1);
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Submitted");
        });
        string markupAtDisposal = cut.Markup;

        cut.Instance.Dispose();
        await submission;
        readToken.IsCancellationRequested.ShouldBeTrue();
        stalledRead.Task.IsCompleted.ShouldBeFalse();
        Clock.Advance(TimeSpan.FromSeconds(10));

        ExpectedVersionReadCount(4).ShouldBe(1);
        cut.Markup.ShouldBe(markupAtDisposal);
        cut.Dispose();
    }

    [Theory]
    [InlineData(AgentInspectionStatus.Unavailable, "Agents.Surface.Unavailable.Title")]
    [InlineData(AgentInspectionStatus.NotAuthorized, "Agents.Surface.PermissionDenied.Title")]
    [InlineData(AgentInspectionStatus.AgentNotFound, "Agents.Surface.Empty.Title")]
    [InlineData(AgentInspectionStatus.Success, "Agents.Surface.Empty.Title")]
    public async Task A_terminal_retry_result_stops_polling_and_renders_the_typed_failure(
        AgentInspectionStatus terminalStatus,
        string expectedSurface)
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        AgentSetupResult terminal = terminalStatus switch
        {
            AgentInspectionStatus.Unavailable => AgentSetupResult.Unavailable(),
            AgentInspectionStatus.NotAuthorized => AgentSetupResult.NotAuthorized(),
            AgentInspectionStatus.AgentNotFound => AgentSetupResult.NotFound(),
            _ => new AgentSetupResult(AgentInspectionStatus.Success, Setup: null),
        };
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)),
            Task.FromResult(AgentSetupResult.Success(pending)),
            Task.FromResult(terminal));
        GivenAcceptedWrite(gateway => gateway.ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => ExpectedVersionReadCount(4).ShouldBe(1));

        Clock.Advance(TimeSpan.FromMilliseconds(250));
        await submission;
        int readsAtTerminalOutcome = ExpectedVersionReadCount(4);
        Clock.Advance(TimeSpan.FromSeconds(5));
        await cut.InvokeAsync(() => Task.CompletedTask);

        readsAtTerminalOutcome.ShouldBe(2);
        ExpectedVersionReadCount(4).ShouldBe(readsAtTerminalOutcome);
        cut.Markup.ShouldContain(expectedSurface);
        cut.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        var retainedStore = Services.GetRequiredService<Hexalith.Agents.UI.State.AgentSetupAttemptStore>();
        retainedStore.Load(await retainedStore.GetScopeAsync()).ShouldNotBeNull().PendingWrite!.TargetConfigurationVersion.ShouldBe(4);
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AwaitingProjection");
        cut.Find("[data-testid='agents-config-write-refresh']");
        cut.Find("[data-testid='agents-config-write-retry']");
        cut.Find("[data-testid='agents-config-write-abandon']");
    }

    [Fact]
    public async Task A_thrown_catch_up_read_preserves_pending_truth_and_the_accepted_write()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        int readNumber = 0;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            readNumber++;
            return readNumber switch
            {
                1 => Task.FromResult(AgentSetupResult.Success(initial)),
                2 => Task.FromResult(AgentSetupResult.Success(pending)),
                _ => throw new InvalidOperationException("retry transport details"),
            };
        });
        GivenAcceptedWrite(gateway => gateway.ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => ExpectedVersionReadCount(4).ShouldBe(1));

        Clock.Advance(TimeSpan.FromMilliseconds(250));
        await submission;

        cut.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        cut.Markup.ShouldContain("Agents.Surface.Unavailable.Title");
        // Read unavailability does not erase the accepted write or its exact target-based recovery.
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AwaitingProjection");
        cut.Find("[data-testid='agents-config-write-refresh']");
        cut.Find("[data-testid='agents-config-write-retry']");
        cut.Find("[data-testid='agents-config-write-abandon']");
        cut.Markup.ShouldNotContain("retry transport details");
        ExpectedVersionReadCount(4).ShouldBe(2);
    }

    [Fact]
    public async Task A_gateway_that_throws_blocks_conflicting_changes_until_the_attempt_is_explicitly_abandoned()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<AgentSetupWriteResult>>(_ => throw new InvalidOperationException("transport blew up"));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-instructions-input']").Change("instructions long enough to be valid");
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Unavailable");

            // The exception is not disclosed, and the unresolved attempt blocks a different write.
            cut.Markup.ShouldNotContain("transport blew up");
            cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-disable']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-submit']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-response-mode-submit']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-write-retry']");
            cut.Find("[data-testid='agents-config-write-abandon']");
        });

        cut.Find("[data-testid='agents-config-write-abandon']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.Abandoned");
            cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeFalse();
            cut.Find("[data-testid='agents-config-disable']").HasAttribute("disabled").ShouldBeFalse();
            cut.Find("[data-testid='agents-config-submit']").HasAttribute("disabled").ShouldBeFalse();
            cut.Find("[data-testid='agents-config-response-mode-submit']").HasAttribute("disabled").ShouldBeFalse();
        });
    }

    [Fact]
    public void A_non_lifetime_gateway_cancellation_reports_the_typed_unavailable_write_outcome()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns<Task<AgentSetupWriteResult>>(_ => throw new OperationCanceledException("unrelated cancellation"));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-activate']"));
        cut.Find("[data-testid='agents-config-activate']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Unavailable");
            cut.Markup.ShouldNotContain("unrelated cancellation");
            cut.Find("[data-testid='agents-config-write-retry']");
            cut.Find("[data-testid='agents-config-write-abandon']");
        });
    }

    /// <summary>Initial gateway failures use the existing safe unavailable surface rather than breaking rendering.</summary>
    [Theory]
    [InlineData(true, "transport")]
    [InlineData(false, "transport")]
    [InlineData(true, "json")]
    [InlineData(false, "json")]
    [InlineData(true, "cancellation")]
    [InlineData(false, "cancellation")]
    public void InitialReadFailureRendersUnavailable(bool synchronousFailure, string failure)
    {
        Exception error = failure switch
        {
            "json" => new System.Text.Json.JsonException("private-initial-read-details"),
            "cancellation" => new OperationCanceledException("private-initial-read-details"),
            _ => new System.Net.Http.HttpRequestException("private-initial-read-details"),
        };
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns<Task<AgentSetupResult>>(_ => synchronousFailure
                ? throw error
                : Task.FromException<AgentSetupResult>(error));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-state']").TextContent.ShouldContain("Agents.Surface.Unavailable.Title");
            cut.Markup.ShouldNotContain("Agents.Surface.PermissionDenied.Title");
            cut.Markup.ShouldNotContain("private-initial-read-details");
            cut.FindAll("[data-testid='agents-config-submit']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-config-activate']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-config-disable']").ShouldBeEmpty();
        });
    }

    /// <summary>A failure after the loading render still completes initialization with a safe unavailable render.</summary>
    [Theory]
    [InlineData("transport")]
    [InlineData("json")]
    [InlineData("cancellation")]
    public void DelayedInitialReadFailureReplacesLoadingWithUnavailable(string failure)
    {
        TaskCompletionSource<AgentSetupResult> pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(pendingRead.Task);
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Find("[data-testid='agents-config-state']").TextContent.ShouldContain("Agents.Surface.Loading.Title");
        pendingRead.Task.IsCompleted.ShouldBeFalse();

        pendingRead.SetException(failure switch
        {
            "json" => new System.Text.Json.JsonException("private-delayed-read-details"),
            "cancellation" => new OperationCanceledException("private-delayed-read-details"),
            _ => new System.Net.Http.HttpRequestException("private-delayed-read-details"),
        });

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-state']").TextContent.ShouldContain("Agents.Surface.Unavailable.Title");
            cut.Markup.ShouldNotContain("Agents.Surface.Loading.Title");
            cut.Markup.ShouldNotContain("private-delayed-read-details");
            cut.FindAll("[data-testid='agents-config-submit']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-config-activate']").ShouldBeEmpty();
        });
    }

    /// <summary>Disposal cancels even a noncooperative initial read and prevents later results from changing the page.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DisposalDuringInitialReadPreventsLateStateUpdates(bool lateFailure)
    {
        TaskCompletionSource<AgentSetupResult> stalledRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken readToken = default;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            readToken = call.ArgAt<CancellationToken>(1);
            return stalledRead.Task;
        });
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        string markupAtDisposal = cut.Markup;
        int rendersAtDisposal = cut.RenderCount;

        cut.Instance.Dispose();

        readToken.IsCancellationRequested.ShouldBeTrue();
        // Leave the instance mounted so the final lifecycle render exposes any accidental state update.
        cut.WaitForAssertion(() => cut.RenderCount.ShouldBeGreaterThan(rendersAtDisposal));
        stalledRead.Task.IsCompleted.ShouldBeFalse();
        if (lateFailure)
        {
            stalledRead.SetException(new InvalidOperationException("private-late-read-details"));
        }
        else
        {
            stalledRead.SetResult(AgentSetupResult.Success(AgentUiTestData.Setup(
                AgentUiTestData.Status(AgentLifecycleStatus.Active))));
        }

        await cut.InvokeAsync(() => Task.CompletedTask);
        cut.Render();
        cut.Markup.ShouldBe(markupAtDisposal);
        cut.Markup.ShouldNotContain("private-late-read-details");
        await SetupGateway.Received(1).GetSetupAsync(null, Arg.Any<CancellationToken>());
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
                Arg.Any<AgentOperationOptions>(),
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
    public async Task Retrying_after_a_lost_acknowledgment_resubmits_the_exact_payload_and_ulid_pair()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft),
            configurationVersion: 3);
        AgentSetupView confirmed = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft),
            configurationVersion: 4);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
            Task.FromResult(AgentSetupResult.Success(call.ArgAt<int?>(0) is null ? initial : confirmed)));
        var commands = new List<UpdateAgentConfiguration>();
        var options = new List<AgentOperationOptions>();
        int submissions = 0;
        SetupGateway.UpdateConfigurationAsync(
                Arg.Any<UpdateAgentConfiguration>(),
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                commands.Add(call.ArgAt<UpdateAgentConfiguration>(0));
                options.Add(call.ArgAt<AgentOperationOptions>(1));
                submissions++;
                return submissions == 1
                    ? throw new InvalidOperationException("acknowledgment lost")
                    : Task.FromResult(Accepted());
            });

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-submit']"));
        cut.Find("[data-testid='agents-config-display-name-input']").Change("hexa renamed");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("sensitive exact payload");
        cut.Find("[data-testid='agents-config-submit']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-write-retry']"));

        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());

        commands.Count.ShouldBe(2);
        commands[1].ShouldBeSameAs(commands[0]);
        commands[0].DisplayName.ShouldBe("hexa renamed");
        commands[0].Instructions.ShouldBe("sensitive exact payload");
        options.Count.ShouldBe(2);
        options[1].ShouldBeSameAs(options[0]);
        options[0].CorrelationId.ShouldNotBeNull().ShouldMatch("^[0-9A-HJKMNP-TV-Z]{26}$");
        options[0].IdempotencyKey.ShouldNotBeNull().ShouldMatch("^[0-9A-HJKMNP-TV-Z]{26}$");
        options[0].CorrelationId.ShouldNotBe(options[0].IdempotencyKey);
        await SetupGateway.Received(1).GetSetupAsync(4, Arg.Any<CancellationToken>());
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.Confirmed");
        cut.Markup.ShouldNotContain("sensitive exact payload");
    }

    [Fact]
    public async Task Unable_to_verify_retains_and_blocks_the_exact_attempt_until_retry_or_abandon()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3));
        var commands = new List<UpdateAgentConfiguration>();
        var options = new List<AgentOperationOptions>();
        SetupGateway.UpdateConfigurationAsync(
                Arg.Any<UpdateAgentConfiguration>(),
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                commands.Add(call.ArgAt<UpdateAgentConfiguration>(0));
                options.Add(call.ArgAt<AgentOperationOptions>(1));
                return Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify));
            });

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-submit']"));
        cut.Find("[data-testid='agents-config-display-name-input']").Change("hexa unverifiable");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("exact unverifiable payload");
        cut.Find("[data-testid='agents-config-submit']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.UnableToVerify");
            cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-disable']").HasAttribute("disabled").ShouldBeTrue();
            cut.Find("[data-testid='agents-config-write-retry']");
            cut.Find("[data-testid='agents-config-write-abandon']");
        });
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        cut.Find("[data-testid='agents-config-response-mode-submit']").HasAttribute("disabled").ShouldBeTrue();

        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());

        commands.Count.ShouldBe(2);
        commands[1].ShouldBeSameAs(commands[0]);
        commands[0].DisplayName.ShouldBe("hexa unverifiable");
        commands[0].Instructions.ShouldBe("exact unverifiable payload");
        options.Count.ShouldBe(2);
        options[1].ShouldBeSameAs(options[0]);
        await SetupGateway.DidNotReceive().GetSetupAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.UnableToVerify");

        cut.Find("[data-testid='agents-config-write-abandon']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.Abandoned");
            cut.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value")
                .ShouldBe("exact unverifiable payload");
            cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeFalse();
            cut.Find("[data-testid='agents-config-disable']").HasAttribute("disabled").ShouldBeFalse();
            cut.Find("[data-testid='agents-config-response-mode-submit']").HasAttribute("disabled").ShouldBeFalse();
        });
    }

    [Fact]
    public async Task Response_mode_retry_reuses_the_original_mode_and_options_after_the_visible_selection_changes()
    {
        GivenSetup(AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3));
        var submittedModes = new List<AgentResponseMode>();
        var submittedOptions = new List<AgentOperationOptions>();
        int submissions = 0;
        SetupGateway.ConfigureResponseModeAsync(
                Arg.Any<AgentResponseMode>(),
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                submittedModes.Add(call.ArgAt<AgentResponseMode>(0));
                submittedOptions.Add(call.ArgAt<AgentOperationOptions>(1));
                submissions++;
                return submissions == 1
                    ? throw new InvalidOperationException("acknowledgment lost")
                    : Task.FromResult(AgentSetupWriteResult.Submitted(
                        new AgentCommandAcceptance(
                            "agent-1",
                            "message-1",
                            "correlation-1",
                            AgentSetupTruthState.AuthoritativePending,
                            AgentSetupWriteEffect.AlreadyApplied,
                            3)));
            });

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        cut.Find("[data-testid='agents-config-response-mode-submit']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-write-retry']"));

        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Automatic));
        cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Automatic);
        await cut.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());

        submittedModes.ShouldBe([AgentResponseMode.Confirmation, AgentResponseMode.Confirmation]);
        submittedOptions.Count.ShouldBe(2);
        submittedOptions[1].ShouldBeSameAs(submittedOptions[0]);
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AlreadyApplied");
    }

    [Fact]
    public async Task Sensitive_instructions_are_cleared_while_a_non_cooperative_immediate_refresh_is_in_flight()
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, hasInstructions: true, instructionsValid: true),
            configurationVersion: 3);
        TaskCompletionSource<AgentSetupResult> stalledRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken readToken = default;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            if (call.ArgAt<int?>(0) is null)
            {
                return Task.FromResult(AgentSetupResult.Success(initial));
            }

            readToken = call.ArgAt<CancellationToken>(1);
            return stalledRead.Task;
        });
        SetupGateway.UpdateConfigurationAsync(
                Arg.Any<UpdateAgentConfiguration>(),
                Arg.Any<AgentOperationOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Accepted()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-submit']"));
        cut.Find("[data-testid='agents-config-instructions-input']").Change("a very sensitive system prompt");
        Task submission = cut.Find("[data-testid='agents-config-submit']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            ExpectedVersionReadCount(4).ShouldBe(1);
            cut.Markup.ShouldNotContain("a very sensitive system prompt");
            cut.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value")
                .ShouldBeNullOrEmpty();
            cut.Find("[data-testid='agents-config-write-state']").TextContent
                .ShouldContain("Agents.Config.Write.Submitted");
            cut.Find("[data-testid='agents-config-instructions-presence']").TextContent
                .ShouldContain("Agents.Config.Instructions.Present");
            cut.Find("[data-testid='agents-config-instructions-version']");
        });

        Clock.Advance(TimeSpan.FromMilliseconds(7_999));
        await cut.InvokeAsync(() => Task.CompletedTask);
        submission.IsCompleted.ShouldBeFalse();

        Clock.Advance(TimeSpan.FromMilliseconds(1));
        await submission;

        readToken.IsCancellationRequested.ShouldBeTrue();
        stalledRead.Task.IsCompleted.ShouldBeFalse();
        ExpectedVersionReadCount(4).ShouldBe(1);
        cut.Find("[data-testid='agents-config-truth-stage']").TextContent
            .ShouldContain("Agents.Config.Truth.Stage.ProjectionConfirmed");
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AwaitingProjection");
        cut.Find("[data-testid='agents-config-write-refresh']");
        cut.Find("[data-testid='agents-config-write-retry']");
        cut.Find("[data-testid='agents-config-write-abandon']");
    }

    [Fact]
    public async Task A_throwing_refresh_keeps_the_retained_attempt_and_its_recovery_actions()
    {
        // A throwing read removes stale setup details while the accepted write retains its exact target
        // and read-only recovery action independently of the unavailable surface.
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3);
        AgentSetupView pending = AgentUiTestData.Setup(
            AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Automatic),
            configurationVersion: 3,
            freshness: AgentSetupFreshness.Stale,
            truthState: AgentSetupTruthState.AuthoritativePending);
        bool readThrows = false;
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call =>
            readThrows
                ? throw new InvalidOperationException("transient read failure")
                : Task.FromResult(AgentSetupResult.Success(call.ArgAt<int?>(0) is null ? initial : pending)));
        GivenAcceptedWrite(gateway => gateway.ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>()));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-response-mode-confirmation']"));
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(
            AgentResponseMode.Confirmation));
        Task submission = cut.Find("[data-testid='agents-config-response-mode-submit']").ClickAsync(new MouseEventArgs());

        // Run the bounded catch-up out so the attempt settles on awaiting-projection with its recovery actions.
        for (int tick = 0; tick < 33; tick++)
        {
            Clock.Advance(TimeSpan.FromMilliseconds(250));
            await cut.InvokeAsync(() => Task.CompletedTask);
        }

        await submission;
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.AwaitingProjection");
        int readsBeforeRefresh = ExpectedVersionReadCount(4);

        readThrows = true;
        await cut.Find("[data-testid='agents-config-write-refresh']").ClickAsync(new MouseEventArgs());

        // The read was attempted and failed, and the attempt survived it unchanged.
        ExpectedVersionReadCount(4).ShouldBe(readsBeforeRefresh + 1);
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldContain("Agents.Config.Write.RefreshIncomplete");
        cut.Find("[data-testid='agents-config-write-state']").TextContent
            .ShouldNotContain("Agents.Config.Write.Unavailable");
        cut.Find("[data-testid='agents-config-write-refresh']");
        cut.Find("[data-testid='agents-config-write-retry']");
        cut.Find("[data-testid='agents-config-write-abandon']");

        // The failed read removes all mutable setup controls, while recovery remains available.
        cut.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        cut.Markup.ShouldContain("Agents.Surface.Unavailable.Title");
        cut.Find("[data-testid='agents-config-write-refresh']").HasAttribute("disabled").ShouldBeFalse();
        cut.Find("[data-testid='agents-config-write-abandon']").HasAttribute("disabled").ShouldBeFalse();

        // Only the original write was ever submitted; a failed read never resubmits.
        await SetupGateway.Received(1).ConfigureResponseModeAsync(
            AgentResponseMode.Confirmation,
            Arg.Any<AgentOperationOptions>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void The_configure_save_is_disabled_until_instructions_are_entered()
    {
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(AgentLifecycleStatus.Draft), configurationVersion: 3));

        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-config-submit']").HasAttribute("disabled").ShouldBeTrue());
    }

    [Fact]
    public async Task NavigationDuringUnacknowledgedSubmissionRestoresExactIntentWithoutAutomaticRetry()
    {
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 3));
        var stalled = new TaskCompletionSource<AgentSetupWriteResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var commands = new List<UpdateAgentConfiguration>();
        var options = new List<AgentOperationOptions>();
        SetupGateway.UpdateConfigurationAsync(Arg.Any<UpdateAgentConfiguration>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                commands.Add(call.ArgAt<UpdateAgentConfiguration>(0));
                options.Add(call.ArgAt<AgentOperationOptions>(1));
                return commands.Count == 1 ? stalled.Task : Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify));
            });
        IRenderedComponent<AgentConfiguration> first = RenderPage<AgentConfiguration>();
        first.Find("[data-testid='agents-config-display-name-input']").Change("retained name");
        first.Find("[data-testid='agents-config-description-input']").Change("retained description");
        first.Find("[data-testid='agents-config-instructions-input']").Change("private exact instructions");
        await first.InvokeAsync(() => first.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        Task submission = first.Find("[data-testid='agents-config-submit']").ClickAsync(new MouseEventArgs());
        first.WaitForAssertion(() => commands.Count.ShouldBe(1));
        first.Instance.Dispose();
        first.Dispose();
        await submission;

        IRenderedComponent<AgentConfiguration> reopened = RenderPage<AgentConfiguration>();
        reopened.WaitForAssertion(() => reopened.Find("[data-testid='agents-config-write-retry']"));
        commands.Count.ShouldBe(1);
        reopened.Find("[data-testid='agents-config-write-lifetime']").TextContent.ShouldContain("Agents.Config.Write.Lifetime");
        reopened.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.UnableToVerify");
        reopened.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("retained name");
        reopened.Find("[data-testid='agents-config-description-input']").GetAttribute("value").ShouldBe("retained description");
        reopened.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation);
        reopened.Find("[data-testid='agents-config-submit']").HasAttribute("disabled").ShouldBeTrue();
        reopened.Markup.ShouldNotContain("private exact instructions");
        // A late outcome of the disposed observer cannot resolve or replace the reopened attempt.
        stalled.SetResult(Accepted(4));
        await reopened.InvokeAsync(() => Task.CompletedTask);
        await reopened.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());
        commands.Count.ShouldBe(2);
        commands[1].ShouldBeSameAs(commands[0]);
        options[1].ShouldBeSameAs(options[0]);
        await reopened.Find("[data-testid='agents-config-write-abandon']").ClickAsync(new MouseEventArgs());
        reopened.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("private exact instructions");
        var store = Services.GetRequiredService<Hexalith.Agents.UI.State.AgentSetupAttemptStore>();
        store.Load(await store.GetScopeAsync()).ShouldBeNull();
    }

    [Theory]
    [InlineData(AgentInspectionStatus.Unavailable, "Agents.Surface.Unavailable.Title")]
    [InlineData(AgentInspectionStatus.NotAuthorized, "Agents.Surface.PermissionDenied.Title")]
    [InlineData(AgentInspectionStatus.AgentNotFound, "Agents.Surface.Empty.Title")]
    [InlineData(AgentInspectionStatus.Success, "Agents.Surface.Empty.Title")]
    public async Task ReopeningAcceptedAttemptRetainsExactReceiptAndTargetAcrossTypedFailures(
        AgentInspectionStatus failure, string expectedSurface)
    {
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(call => Task.FromResult(
            call.ArgAt<int?>(0) is null ? AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 3))
                : new AgentSetupResult(failure, null)));
        AgentSetupWriteResult accepted = Accepted(9);
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(accepted));
        IRenderedComponent<AgentConfiguration> first = RenderPage<AgentConfiguration>();
        first.Find("[data-testid='agents-config-instructions-input']").Change("unsubmitted private draft");
        await first.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        first.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        first.Instance.Dispose();
        first.Dispose();

        IRenderedComponent<AgentConfiguration> reopened = RenderPage<AgentConfiguration>();
        reopened.WaitForAssertion(() => reopened.Markup.ShouldContain(expectedSurface));
        reopened.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.AwaitingProjection");
        reopened.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        reopened.Markup.ShouldNotContain("unsubmitted private draft");
        var store = Services.GetRequiredService<Hexalith.Agents.UI.State.AgentSetupAttemptStore>();
        var retained = store.Load(await store.GetScopeAsync()).ShouldNotBeNull();
        retained.PendingWrite!.Acceptance.ShouldBeSameAs(accepted.Acceptance);
        retained.PendingWrite.TargetConfigurationVersion.ShouldBe(9);
        await reopened.Find("[data-testid='agents-config-write-refresh']").ClickAsync(new MouseEventArgs());
        await SetupGateway.Received(3).GetSetupAsync(9, Arg.Any<CancellationToken>());
        await SetupGateway.Received(1).ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>());

        SetupGateway.GetSetupAsync(9, Arg.Any<CancellationToken>()).Returns(Task.FromResult(
            AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 9))));
        await reopened.Find("[data-testid='agents-config-write-refresh']").ClickAsync(new MouseEventArgs());
        reopened.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.Confirmed");
        store.Load(await store.GetScopeAsync()).ShouldBeNull();
        reopened.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("unsubmitted private draft");
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("mode")]
    public async Task ConfirmedConcurrentValuesRequireExplicitFreshLoadAndKeepInstructionDraft(string changedField)
    {
        AgentSetupView initial = AgentUiTestData.Setup(configurationVersion: 3);
        AgentSetupView concurrent = AgentUiTestData.Setup(initial.Agent with
        {
            DisplayName = changedField == "name" ? "concurrent saved" : initial.Agent.DisplayName,
            Description = changedField == "description" ? "concurrent description" : initial.Agent.Description,
            ResponseMode = changedField == "mode" ? AgentResponseMode.Confirmation : initial.Agent.ResponseMode,
        }, configurationVersion: 8);
        AgentSetupView fresh = AgentUiTestData.Setup(initial.Agent with { DisplayName = "fresh saved", Description = "fresh description", ResponseMode = AgentResponseMode.Confirmation }, configurationVersion: 10);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)), Task.FromResult(AgentSetupResult.Success(concurrent)),
            Task.FromResult(AgentSetupResult.Success(fresh)));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Accepted(4)));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Find("[data-testid='agents-config-display-name-input']").Change("my name");
        cut.Find("[data-testid='agents-config-description-input']").Change("my description");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("my private draft");
        cut.FindAll("[data-testid='agents-config-draft-divergence']").ShouldBeEmpty();
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-draft-divergence']");
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("my name");
        await cut.Find("[data-testid='agents-config-load-saved']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("fresh saved");
        cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value").ShouldBe("fresh description");
        cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation);
        cut.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("my private draft");
        cut.FindAll("[data-testid='agents-config-draft-divergence']").ShouldBeEmpty();
        await SetupGateway.Received(1).GetSetupAsync(null, Arg.Any<CancellationToken>());
        await SetupGateway.Received(1).GetSetupAsync(8, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("older")]
    [InlineData("denied")]
    [InlineData("missing")]
    [InlineData("unavailable")]
    [InlineData("malformed")]
    [InlineData("throws")]
    [InlineData("pending")]
    public async Task FailedExplicitLoadPreservesEveryDraft(string failure)
    {
        AgentSetupView initial = AgentUiTestData.Setup(configurationVersion: 3);
        AgentSetupView newer = AgentUiTestData.Setup(initial.Agent with { DisplayName = "new saved" }, configurationVersion: 4);
        Task<AgentSetupResult> failedRead = failure switch
        {
            "older" => Task.FromResult(AgentSetupResult.Success(initial with { Agent = initial.Agent with { DisplayName = "obsolete saved value" } })),
            "denied" => Task.FromResult(AgentSetupResult.NotAuthorized()),
            "missing" => Task.FromResult(AgentSetupResult.NotFound()),
            "malformed" => Task.FromResult(new AgentSetupResult(AgentInspectionStatus.Success, null)),
            "throws" => Task.FromException<AgentSetupResult>(new HttpRequestException("private load error")),
            "pending" => Task.FromResult(AgentSetupResult.Success(newer with { TruthState = AgentSetupTruthState.AuthoritativePending })),
            _ => Task.FromResult(AgentSetupResult.Unavailable()),
        };
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)), Task.FromResult(AgentSetupResult.Success(newer)), failedRead,
            Task.FromResult(AgentSetupResult.Success(newer)),
            Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(newer.Agent with
            {
                DisplayName = "fresh saved name", Description = "fresh saved description", ResponseMode = AgentResponseMode.Automatic,
            }, configurationVersion: 5))));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Accepted(4)));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Find("[data-testid='agents-config-display-name-input']").Change("my name");
        cut.Find("[data-testid='agents-config-description-input']").Change("my description");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("private instructions");
        await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        await cut.Find("[data-testid='agents-config-load-saved']").ClickAsync(new MouseEventArgs());
        cut.Markup.ShouldNotContain("private load error");
        ExpectedVersionReadCount(4).ShouldBe(2);
        if (failure != "pending")
        {
            cut.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain("private instructions");
            cut.Markup.ShouldNotContain("obsolete saved value");
        }

        // Recover authorized visibility with a read-only refresh; all editable values remain the local draft.
        await cut.Find("[data-testid='agents-config-draft-refresh']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("my name");
        cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value").ShouldBe("my description");
        cut.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("private instructions");
        cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Confirmation);
        await cut.Find("[data-testid='agents-config-load-saved']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("fresh saved name");
        cut.Find("[data-testid='agents-config-description-input']").GetAttribute("value").ShouldBe("fresh saved description");
        cut.FindComponent<ResponseModeToggle>().Instance.Value.ShouldBe(AgentResponseMode.Automatic);
        cut.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("private instructions");
        ExpectedVersionReadCount(4).ShouldBe(4);
    }

    [Fact]
    public async Task UnresolvedReturnedAttemptDisablesSavedValueLoadingEvenWhenProjectionChanged()
    {
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify)));
        IRenderedComponent<AgentConfiguration> first = RenderPage<AgentConfiguration>();
        first.Find("[data-testid='agents-config-display-name-input']").Change("my draft");
        await first.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        first.Instance.Dispose();
        first.Dispose();
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status() with { DisplayName = "another writer" }, configurationVersion: 99));
        IRenderedComponent<AgentConfiguration> reopened = RenderPage<AgentConfiguration>();
        reopened.Find("[data-testid='agents-config-load-saved']").HasAttribute("disabled").ShouldBeTrue();
        reopened.Find("[data-testid='agents-config-write-retry']");
        int reads = SetupGateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAgentSetupGateway.GetSetupAsync));
        await reopened.InvokeAsync(async () => await (Task)typeof(AgentConfiguration)
            .GetMethod("LoadSavedValuesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(reopened.Instance, null)!);
        SetupGateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAgentSetupGateway.GetSetupAsync)).ShouldBe(reads);
        reopened.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("my draft");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypingOrUnrelatedLifecycleRevisionDoesNotSignalDraftDivergence(bool matchesChangedField)
    {
        AgentSetupView initial = AgentUiTestData.Setup(configurationVersion: 3);
        AgentSetupView confirmed = AgentUiTestData.Setup(initial.Agent with
        {
            Lifecycle = AgentLifecycleStatus.Active,
            DisplayName = matchesChangedField ? "my name" : initial.Agent.DisplayName,
        }, configurationVersion: 4);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)), Task.FromResult(AgentSetupResult.Success(confirmed)));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Accepted(4)));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Find("[data-testid='agents-config-display-name-input']").Change("my name");
        cut.Find("[data-testid='agents-config-description-input']").Change("my unrelated description");
        cut.FindAll("[data-testid='agents-config-draft-divergence']").ShouldBeEmpty();
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        cut.FindAll("[data-testid='agents-config-draft-divergence']").ShouldBeEmpty();
    }

    [Fact]
    public async Task AuthenticationChangeRemovesMountedContentAndStaleAbandonCannotRestoreInstructions()
    {
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 3));
        SetupGateway.UpdateConfigurationAsync(Arg.Any<UpdateAgentConfiguration>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify)));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Find("[data-testid='agents-config-display-name-input']").Change("private prior user name");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("private prior user instructions");
        await cut.Find("[data-testid='agents-config-submit']").ClickAsync(new MouseEventArgs());
        Authorization.SetNotAuthorized();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title"));
        await cut.InvokeAsync(() => typeof(AgentConfiguration)
            .GetMethod("AbandonAttempt", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(cut.Instance, null));
        cut.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("private prior user");
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(AgentConfiguration).GetField("_instructionsDraft", flags)!.GetValue(cut.Instance).ShouldBe(string.Empty);
        var store = Services.GetRequiredService<Hexalith.Agents.UI.State.AgentSetupAttemptStore>();
        store.Load(await store.GetScopeAsync()).ShouldBeNull();
    }

    [Fact]
    public async Task ColdAcceptedPendingReadRearmsWriteBlockUntilAuthorityConfirms()
    {
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 3, truthState: AgentSetupTruthState.AuthoritativePending));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Find("[data-testid='agents-config-authority-pending']");
        cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeTrue();
        cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 4));
        await cut.Find("[data-testid='agents-config-authority-refresh']").ClickAsync(new MouseEventArgs());
        cut.FindAll("[data-testid='agents-config-authority-pending']").ShouldBeEmpty();
        cut.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeFalse();
        SetupGateway.ReceivedCalls().ShouldAllBe(call => call.GetMethodInfo().Name == nameof(IAgentSetupGateway.GetSetupAsync));
    }

    [Theory]
    [InlineData("name")]
    [InlineData("description")]
    [InlineData("mode")]
    public async Task ConfirmedDraftFieldsBecomeTheBaselineForFurtherTyping(string field)
    {
        AgentSetupView initial = AgentUiTestData.Setup(configurationVersion: 3);
        AgentSetupView confirmed = AgentUiTestData.Setup(initial.Agent with
        {
            DisplayName = field == "name" ? "saved B" : initial.Agent.DisplayName,
            Description = field == "description" ? "saved B" : initial.Agent.Description,
            ResponseMode = field == "mode" ? AgentResponseMode.Confirmation : initial.Agent.ResponseMode,
        }, configurationVersion: 4);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)), Task.FromResult(AgentSetupResult.Success(confirmed)));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Accepted(4)));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        if (field == "mode")
        {
            await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        }
        else
        {
            cut.Find(field == "name" ? "[data-testid='agents-config-display-name-input']" : "[data-testid='agents-config-description-input']").Change("saved B");
        }
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        cut.FindAll("[data-testid='agents-config-draft-divergence']").ShouldBeEmpty();
        if (field == "mode")
        {
            await cut.InvokeAsync(() => cut.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Automatic));
        }
        else
        {
            cut.Find(field == "name" ? "[data-testid='agents-config-display-name-input']" : "[data-testid='agents-config-description-input']").Change("new local C");
        }
        cut.FindAll("[data-testid='agents-config-draft-divergence']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("mode")]
    [InlineData("activate")]
    [InlineData("disable")]
    public async Task NavigationReplaysTheOriginalOperationPayloadAndIdentity(string operation)
    {
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 3));
        var attempts = new List<AgentOperationOptions>();
        var modes = new List<AgentResponseMode>();
        SetupGateway.ConfigureResponseModeAsync(Arg.Any<AgentResponseMode>(), Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                modes.Add(call.ArgAt<AgentResponseMode>(0));
                attempts.Add(call.ArgAt<AgentOperationOptions>(1));
                return Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify));
            });
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            attempts.Add(call.ArgAt<AgentOperationOptions>(0));
            return Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify));
        });
        SetupGateway.DisableAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            attempts.Add(call.ArgAt<AgentOperationOptions>(0));
            return Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify));
        });
        IRenderedComponent<AgentConfiguration> first = RenderPage<AgentConfiguration>();
        if (operation == "mode")
        {
            await first.InvokeAsync(() => first.FindComponent<ResponseModeToggle>().Instance.ValueChanged.InvokeAsync(AgentResponseMode.Confirmation));
        }
        string control = operation == "mode" ? "response-mode-submit" : operation;
        await first.Find($"[data-testid='agents-config-{control}']").ClickAsync(new MouseEventArgs());
        first.Instance.Dispose();
        first.Dispose();
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status(responseMode: AgentResponseMode.Automatic), configurationVersion: 20));
        IRenderedComponent<AgentConfiguration> reopened = RenderPage<AgentConfiguration>();
        attempts.Count.ShouldBe(1);
        await reopened.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());
        attempts.Count.ShouldBe(2);
        attempts[1].ShouldBeSameAs(attempts[0]);
        attempts[0].ExpectedConfigurationVersion.ShouldBe(operation == "activate" ? 3 : null);
        if (operation == "mode")
        {
            modes.ShouldBe([AgentResponseMode.Confirmation, AgentResponseMode.Confirmation]);
        }
        string expectedMethod = operation switch
        {
            "mode" => nameof(IAgentSetupGateway.ConfigureResponseModeAsync),
            "activate" => nameof(IAgentSetupGateway.ActivateAsync),
            _ => nameof(IAgentSetupGateway.DisableAsync),
        };
        SetupGateway.ReceivedCalls().Where(call => call.GetMethodInfo().Name != nameof(IAgentSetupGateway.GetSetupAsync))
            .Select(call => call.GetMethodInfo().Name).ShouldBe([expectedMethod, expectedMethod]);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(12)]
    public async Task ReopeningImmediatelyConfirmedAcceptanceReleasesRetentionWithoutSubmitting(int observedVersion)
    {
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 3))),
            Task.FromResult(AgentSetupResult.Unavailable()),
            Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: observedVersion))));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Accepted(9)));
        IRenderedComponent<AgentConfiguration> first = RenderPage<AgentConfiguration>();
        first.Find("[data-testid='agents-config-instructions-input']").Change("unsent instructions");
        await first.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        first.Instance.Dispose();
        first.Dispose();
        IRenderedComponent<AgentConfiguration> reopened = RenderPage<AgentConfiguration>();
        reopened.Find("[data-testid='agents-config-write-state']").TextContent.ShouldContain("Agents.Config.Write.Confirmed");
        reopened.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
        reopened.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeFalse();
        reopened.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("unsent instructions");
        var store = Services.GetRequiredService<Hexalith.Agents.UI.State.AgentSetupAttemptStore>();
        store.Load(await store.GetScopeAsync()).ShouldBeNull();
        await SetupGateway.Received(2).GetSetupAsync(9, Arg.Any<CancellationToken>());
        await SetupGateway.Received(1).ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelayedReopeningReadDisablesAndGuardsAllRecoveryActions(bool accepted)
    {
        GivenSetup(AgentUiTestData.Setup(configurationVersion: 3));
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(accepted ? Accepted(9) : AgentSetupWriteResult.Failed(AgentSetupWriteStatus.UnableToVerify)));
        IRenderedComponent<AgentConfiguration> first = RenderPage<AgentConfiguration>();
        await first.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        first.Instance.Dispose();
        first.Dispose();
        var delayed = new TaskCompletionSource<AgentSetupResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(delayed.Task);
        IRenderedComponent<AgentConfiguration> reopened = RenderPage<AgentConfiguration>();
        reopened.Find("[data-testid='agents-config-write-retry']").HasAttribute("disabled").ShouldBeTrue();
        reopened.Find("[data-testid='agents-config-write-abandon']").HasAttribute("disabled").ShouldBeTrue();
        int readsBeforeActions = SetupGateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAgentSetupGateway.GetSetupAsync));
        if (accepted)
        {
            reopened.Find("[data-testid='agents-config-write-refresh']").HasAttribute("disabled").ShouldBeTrue();
            await reopened.Find("[data-testid='agents-config-write-refresh']").ClickAsync(new MouseEventArgs());
        }
        await reopened.Find("[data-testid='agents-config-write-retry']").ClickAsync(new MouseEventArgs());
        await reopened.Find("[data-testid='agents-config-write-abandon']").ClickAsync(new MouseEventArgs());
        await SetupGateway.Received(1).ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>());
        SetupGateway.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAgentSetupGateway.GetSetupAsync)).ShouldBe(readsBeforeActions);
        var store = Services.GetRequiredService<Hexalith.Agents.UI.State.AgentSetupAttemptStore>();
        store.Load(await store.GetScopeAsync()).ShouldNotBeNull();
        delayed.SetResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 4)));
        reopened.WaitForAssertion(() => reopened.Find("[data-testid='agents-config-write-retry']").HasAttribute("disabled").ShouldBeFalse());
        reopened.Find("[data-testid='agents-config-activate']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelayedReadCannotRestoreContentAfterAuthenticationChanges(bool initialRead)
    {
        var delayed = new TaskCompletionSource<AgentSetupResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (initialRead)
        {
            SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(delayed.Task);
        }
        else
        {
            SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
                Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 3))), delayed.Task);
            SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Accepted(4)));
        }
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        Task? write = initialRead ? null : cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        Authorization.SetClaims(new System.Security.Claims.Claim("sub", "another-user"),
            new System.Security.Claims.Claim("tenantId", "tenant-1"));
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title"));
        int renderBeforeCompletion = cut.RenderCount;
        delayed.SetResult(AgentSetupResult.Success(AgentUiTestData.Setup(AgentUiTestData.Status() with
        {
            DisplayName = "old private name", Description = "old private description",
        }, configurationVersion: 4)));
        if (write is not null)
        {
            await write;
        }
        if (initialRead)
        {
            cut.WaitForAssertion(() => cut.RenderCount.ShouldBeGreaterThan(renderBeforeCompletion));
        }
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title"));
        cut.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("old private");
        cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
    }

    [Fact]
    public async Task MissingScopeCannotReadOrRecoverIntoAnotherAuthentication()
    {
        Authorization.SetNotAuthorized();
        GivenSetup(AgentUiTestData.Setup(AgentUiTestData.Status() with { DisplayName = "must not load" }));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title");
        await SetupGateway.DidNotReceive().GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
        Authorization.SetAuthorized("another-user");
        Authorization.SetClaims(new System.Security.Claims.Claim("sub", "another-user"),
            new System.Security.Claims.Claim("tenantId", "tenant-1"));
        await cut.InvokeAsync(() => Task.CompletedTask);
        cut.Markup.ShouldNotContain("must not load");
        await SetupGateway.DidNotReceive().GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PendingMountedAuthenticationHidesContentAndPreservesOnlyAnUnchangedBinding(bool changesUser)
    {
        var authentication = new ControlledSetupAuthenticationStateProvider();
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State("administrator", "tenant-1")));
        Services.AddSingleton<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(authentication);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(AgentUiTestData.Setup(configurationVersion: 3))),
            Task.FromResult(AgentSetupResult.Unavailable()));
        AgentSetupWriteResult accepted = Accepted(9);
        SetupGateway.ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(accepted));
        IRenderedComponent<AgentConfiguration> cut = RenderPage<AgentConfiguration>();
        cut.Find("[data-testid='agents-config-display-name-input']").Change("private local name");
        cut.Find("[data-testid='agents-config-instructions-input']").Change("private local instructions");
        await cut.Find("[data-testid='agents-config-activate']").ClickAsync(new MouseEventArgs());
        var pending = new TaskCompletionSource<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        authentication.Publish(pending.Task);
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty());
        cut.FindAll("[data-testid='agents-config-sections']").ShouldBeEmpty();
        cut.Markup.ShouldNotContain("private local");
        pending.SetResult(ControlledSetupAuthenticationStateProvider.State(changesUser ? "another-user" : "administrator", "tenant-1"));
        if (changesUser)
        {
            cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title"));
            cut.FindAll("[data-testid='agents-config-write-retry']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain("private local");
        }
        else
        {
            cut.WaitForAssertion(() => cut.Find("[data-testid='agents-config-write-refresh']"));
            var store = Services.GetRequiredService<Hexalith.Agents.UI.State.AgentSetupAttemptStore>();
            store.Load(await store.GetScopeAsync()).ShouldNotBeNull().PendingWrite!.Acceptance.ShouldBeSameAs(accepted.Acceptance);
            GivenSetup(AgentUiTestData.Setup(configurationVersion: 9));
            await cut.Find("[data-testid='agents-config-write-refresh']").ClickAsync(new MouseEventArgs());
            cut.Find("[data-testid='agents-config-display-name-input']").GetAttribute("value").ShouldBe("private local name");
            cut.Find("[data-testid='agents-config-instructions-input']").GetAttribute("value").ShouldBe("private local instructions");
            await SetupGateway.Received(1).ActivateAsync(Arg.Any<AgentOperationOptions>(), Arg.Any<CancellationToken>());
        }
    }

    private static AgentSetupWriteResult Accepted(int targetConfigurationVersion = 4)
        => AgentSetupWriteResult.Submitted(
            new AgentCommandAcceptance(
                "agent-1",
                "message-1",
                "correlation-1",
                AgentSetupTruthState.AuthoritativePending,
                AgentSetupWriteEffect.Applied,
                targetConfigurationVersion));

    private void GivenAcceptedWrite(
        Func<IAgentSetupGateway, Task<AgentSetupWriteResult>> write,
        int targetConfigurationVersion = 4)
        => write(SetupGateway).Returns(Task.FromResult(Accepted(targetConfigurationVersion)));

    private void GivenLifecycleTransition(
        AgentLifecycleStatus initialLifecycle,
        AgentLifecycleStatus confirmedLifecycle,
        int confirmedConfigurationVersion = 4)
    {
        AgentSetupView initial = AgentUiTestData.Setup(
            AgentUiTestData.Status(initialLifecycle),
            configurationVersion: 3);
        AgentSetupView confirmed = AgentUiTestData.Setup(
            AgentUiTestData.Status(confirmedLifecycle),
            configurationVersion: confirmedConfigurationVersion);
        SetupGateway.GetSetupAsync(Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(
            Task.FromResult(AgentSetupResult.Success(initial)),
            Task.FromResult(AgentSetupResult.Success(confirmed)));
    }

    private int ExpectedVersionReadCount(int expectedVersion)
        => SetupGateway.ReceivedCalls().Count(call =>
            call.GetMethodInfo().Name == nameof(IAgentSetupGateway.GetSetupAsync)
            && Equals(call.GetArguments()[0], expectedVersion));
}
