using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Composition;

using Microsoft.AspNetCore.Authorization;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1/AC2 — the Conversation call page is policy-gated and fails closed: under the deferred gateway it renders the
/// permission-denied surface rather than a fabricated invocation surface (AD-12). When authorized it hosts the
/// pattern-agnostic invocation panel.
/// </summary>
public sealed class ConversationCallTests : AgentsTestContext
{
    [Fact]
    public void Page_is_gated_by_the_agents_administrator_policy()
    {
        AuthorizeAttribute attribute = typeof(ConversationCall)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .ShouldHaveSingleItem();

        attribute.Policy.ShouldBe(AgentsFrontComposerRegistration.AgentsAdministratorPolicy);
    }

    [Fact]
    public void Deferred_gateway_renders_the_permission_denied_surface()
    {
        // The harness default is the fail-closed NotAuthorized inspection result.
        IRenderedComponent<ConversationCall> cut = RenderPage<ConversationCall>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-conversation-call-state']");
            cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title");
            cut.FindAll("[data-testid='agents-conversation-call-panel']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Authorized_caller_sees_the_invocation_panel()
    {
        // An authorized caller with no current interaction reads NotFound — the page unlocks the invocation panel.
        CallGateway.GetCallStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInteractionInspectionResult.NotFound()));

        IRenderedComponent<ConversationCall> cut = RenderPage<ConversationCall>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-conversation-call-panel']");
            cut.FindAll("[data-testid='agents-conversation-call-state']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Authorized_caller_with_a_current_interaction_also_sees_the_invocation_panel()
    {
        // A Success inspection (an authorized caller with an existing interaction) is the other non-NotAuthorized
        // outcome — it must also unlock the panel, not only NotFound. The safe status view carries no prompt/content.
        AgentInteractionStatusView view = new(
            "interaction-1",
            AgentInteractionStatus.Requested,
            "hexa",
            "party-1",
            "conversation-1",
            AgentResponseMode.Automatic,
            ConfigurationVersion: 1,
            InstructionsVersion: 1,
            ApproverPolicyVersion: 1,
            ProviderId: "openai",
            ModelId: "gpt-4o",
            ProviderCapabilityVersion: 1,
            ContentSafetyPolicyVersion: 1);
        CallGateway.GetCallStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInteractionInspectionResult.Success(view)));

        IRenderedComponent<ConversationCall> cut = RenderPage<ConversationCall>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-conversation-call-panel']");
            cut.FindAll("[data-testid='agents-conversation-call-state']").ShouldBeEmpty();
        });
    }
}
