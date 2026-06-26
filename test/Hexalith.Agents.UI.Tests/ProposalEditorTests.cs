using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1, AC3 — the proposal-editor component. An authorized Approver gets an editable content area + save action; a
/// non-authorized viewer gets a read-only render; generated and edited versions are labeled DISTINCTLY; saving calls the
/// fail-closed gateway with the safe request; and the editor never leaks the (sensitive) content into an accessible name or
/// <c>data-testid</c> (AD-14). The proposal is never styled as a posted Conversation Message.
/// </summary>
public sealed class ProposalEditorTests : AgentsTestContext
{
    private const string ContentSentinel = "top-secret-proposed-content-7f3a";

    [Fact]
    public void Authorized_viewer_gets_an_editable_content_area_and_a_save_action()
    {
        IRenderedComponent<ProposalEditor> cut = RenderEditor(canEdit: true);

        cut.Find("[data-testid='proposal-editor-content']");
        cut.Find("[data-testid='proposal-editor-save']");
        cut.FindAll("[data-testid='proposal-editor-readonly']").ShouldBeEmpty();
    }

    [Fact]
    public void Non_authorized_viewer_gets_a_read_only_render_with_no_save()
    {
        IRenderedComponent<ProposalEditor> cut = RenderEditor(canEdit: false);

        cut.Find("[data-testid='proposal-editor-readonly']");
        cut.FindAll("[data-testid='proposal-editor-content']").ShouldBeEmpty();
        cut.FindAll("[data-testid='proposal-editor-save']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(AgentGenerationKind.Generated, "Agents.GenerationKind.Label.Generated")]
    [InlineData(AgentGenerationKind.Edited, "Agents.GenerationKind.Label.Edited")]
    public void The_version_is_labeled_distinctly_by_kind(AgentGenerationKind kind, string expectedKey)
    {
        IRenderedComponent<ProposalEditor> cut = Render<ProposalEditor>(parameters => parameters
            .Add(e => e.CanEdit, true)
            .Add(e => e.Kind, kind)
            .Add(e => e.Content, "some content"));

        cut.Find("[data-testid='proposal-editor-version-label']").TextContent.Trim().ShouldBe(expectedKey);
    }

    [Fact]
    public void Saving_calls_the_gateway_with_the_safe_request_and_shows_the_edited_status()
    {
        ProposalEditGateway.EditProposalAsync(Arg.Any<ProposalEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalEditResult.Edited("edited-version-2")));

        IRenderedComponent<ProposalEditor> cut = Render<ProposalEditor>(parameters => parameters
            .Add(e => e.CanEdit, true)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1")
            .Add(e => e.SourceVersionId, "v1")
            .Add(e => e.Content, "corrected reply text"));

        cut.Find("[data-testid='proposal-editor-save']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalEditor.Status.Edited"));
        ProposalEditGateway.Received(1).EditProposalAsync(
            Arg.Is<ProposalEditRequest>(r =>
                r.AgentInteractionId == "i1"
                && r.ProposalId == "p1"
                && r.SourceVersionId == "v1"
                && r.EditedContent == "corrected reply text"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void A_fail_closed_gateway_shows_the_not_authorized_status()
    {
        // The default substituted gateway returns NotAuthorized (AgentsTestContext) — an authorized-looking editor that
        // saves against a denying seam shows the denial, never a fabricated success (AD-12).
        IRenderedComponent<ProposalEditor> cut = RenderEditor(canEdit: true);

        cut.Find("[data-testid='proposal-editor-save']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalEditor.Status.NotAuthorized"));
    }

    [Fact]
    public void An_unavailable_gateway_shows_the_unavailable_status()
    {
        // A faulted / unreachable edit seam returns Unavailable — the editor surfaces the distinct error state, never a
        // fabricated success and never the NotAuthorized denial (the fail-closed UI error surface).
        ProposalEditGateway.EditProposalAsync(Arg.Any<ProposalEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalEditResult.Unavailable()));

        IRenderedComponent<ProposalEditor> cut = RenderEditor(canEdit: true);

        cut.Find("[data-testid='proposal-editor-save']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalEditor.Status.Unavailable"));
    }

    [Fact]
    public void A_successful_save_raises_on_edited_with_the_result_so_the_host_can_refresh()
    {
        // The editor's output contract to its (Story 3.7) host: a successful save hands back the safe result so the host can
        // refresh the version history. The result carries only the safe edited-version id, never the content.
        ProposalEditGateway.EditProposalAsync(Arg.Any<ProposalEditRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalEditResult.Edited("edited-version-9")));
        ProposalEditResult? captured = null;

        IRenderedComponent<ProposalEditor> cut = Render<ProposalEditor>(parameters => parameters
            .Add(e => e.CanEdit, true)
            .Add(e => e.Content, "corrected reply text")
            .Add(e => e.OnEdited, r => { captured = r; }));

        cut.Find("[data-testid='proposal-editor-save']").Click();

        cut.WaitForAssertion(() => captured.ShouldNotBeNull());
        captured!.Status.ShouldBe(ProposalEditStatus.Edited);
        captured.EditedVersionId.ShouldBe("edited-version-9");
    }

    [Fact]
    public void Clicking_cancel_invokes_the_on_cancel_callback_without_saving()
    {
        // The Cancel button (shipped in 3.3; Story 3.7 completes the Esc-without-commit accessibility) raises OnCancel so the
        // host can return focus to the trigger, and never calls the edit seam.
        bool cancelled = false;
        IRenderedComponent<ProposalEditor> cut = Render<ProposalEditor>(parameters => parameters
            .Add(e => e.CanEdit, true)
            .Add(e => e.Content, "a generated reply")
            .Add(e => e.OnCancel, () => { cancelled = true; }));

        cut.Find("[data-testid='proposal-editor-cancel']").Click();

        cancelled.ShouldBeTrue();
        ProposalEditGateway.DidNotReceive().EditProposalAsync(Arg.Any<ProposalEditRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void An_empty_edit_is_never_submitted()
    {
        IRenderedComponent<ProposalEditor> cut = Render<ProposalEditor>(parameters => parameters
            .Add(e => e.CanEdit, true)
            .Add(e => e.Content, "   ")); // whitespace-only is not a real edit

        cut.Find("[data-testid='proposal-editor-save']").Click();

        ProposalEditGateway.DidNotReceive().EditProposalAsync(Arg.Any<ProposalEditRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void The_editor_never_leaks_the_content_into_an_accessible_name_or_test_id()
    {
        IRenderedComponent<ProposalEditor> cut = Render<ProposalEditor>(parameters => parameters
            .Add(e => e.CanEdit, true)
            .Add(e => e.Content, ContentSentinel));

        // The content is legitimately shown in the editable area, but it must NEVER appear in an aria-label or a
        // data-testid (AD-14: no content in accessible names / diagnostics).
        foreach (IElement element in cut.FindAll("[aria-label]"))
        {
            (element.GetAttribute("aria-label") ?? string.Empty).ShouldNotContain(ContentSentinel);
        }

        foreach (IElement element in cut.FindAll("[data-testid]"))
        {
            (element.GetAttribute("data-testid") ?? string.Empty).ShouldNotContain(ContentSentinel);
        }
    }

    private IRenderedComponent<ProposalEditor> RenderEditor(bool canEdit)
        => Render<ProposalEditor>(parameters => parameters
            .Add(e => e.CanEdit, canEdit)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1")
            .Add(e => e.SourceVersionId, "v1")
            .Add(e => e.Kind, AgentGenerationKind.Generated)
            .Add(e => e.Content, "a generated reply"));
}
