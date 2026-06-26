using System.Linq;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.AspNetCore.Components.Web;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC2, AC3 — the version-history component. It renders the append-only history as an accessible single-select listbox
/// with an explicit selected/current state, labels each version distinctly by kind, keeps prior versions listed after
/// every transition (AD-5), emits the selection to the host, and never leaks generated content into an accessible name
/// or test id (AD-14).
/// </summary>
public sealed class ProposalVersionHistoryTests : AgentsTestContext
{
    [Fact]
    public void Renders_a_listbox_with_one_option_per_version()
    {
        IRenderedComponent<ProposalVersionHistory> cut = RenderHistory(
            AgentUiTestData.VersionSummary(versionId: "v1"),
            AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Edited, sourceVersionId: "v1", editorPartyId: "editor-1"));

        cut.Find("[data-testid='history-listbox']").GetAttribute("role").ShouldBe("listbox");
        cut.FindAll("[role='option']").Count.ShouldBe(2);
    }

    [Fact]
    public void Each_version_is_labeled_distinctly_by_kind()
    {
        IRenderedComponent<ProposalVersionHistory> cut = RenderHistory(
            AgentUiTestData.VersionSummary(versionId: "v1", kind: AgentGenerationKind.Generated),
            AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Regenerated));

        cut.Find("[data-testid='history-version-0-kind']").TextContent.Trim().ShouldBe("Agents.GenerationKind.Label.Generated");
        cut.Find("[data-testid='history-version-1-kind']").TextContent.Trim().ShouldBe("Agents.GenerationKind.Label.Regenerated");
    }

    [Fact]
    public void The_selected_version_exposes_explicit_selected_state()
    {
        IRenderedComponent<ProposalVersionHistory> cut = RenderHistory(
            selectedVersionId: "v2",
            AgentUiTestData.VersionSummary(versionId: "v1"),
            AgentUiTestData.VersionSummary(versionId: "v2"));

        cut.Find("[data-testid='history-version-0']").GetAttribute("aria-selected").ShouldBe("false");
        IElement selected = cut.Find("[data-testid='history-version-1']");
        selected.GetAttribute("aria-selected").ShouldBe("true");
        cut.Find("[data-testid='history-version-1-selected']");
    }

    [Fact]
    public void Clicking_an_option_raises_on_select_version_with_the_version_id()
    {
        string? selected = null;
        IRenderedComponent<ProposalVersionHistory> cut = Render<ProposalVersionHistory>(parameters => parameters
            .Add(h => h.TestId, "history")
            .Add(h => h.SelectedVersionId, "v1")
            .Add(h => h.Versions, [AgentUiTestData.VersionSummary(versionId: "v1"), AgentUiTestData.VersionSummary(versionId: "v2")])
            .Add(h => h.OnSelectVersion, id => { selected = id; }));

        cut.Find("[data-testid='history-version-1']").Click();

        selected.ShouldBe("v2");
    }

    [Fact]
    public void Pressing_enter_on_an_option_selects_it()
    {
        string? selected = null;
        IRenderedComponent<ProposalVersionHistory> cut = Render<ProposalVersionHistory>(parameters => parameters
            .Add(h => h.TestId, "history")
            .Add(h => h.SelectedVersionId, "v1")
            .Add(h => h.Versions, [AgentUiTestData.VersionSummary(versionId: "v1"), AgentUiTestData.VersionSummary(versionId: "v2")])
            .Add(h => h.OnSelectVersion, id => { selected = id; }));

        cut.Find("[data-testid='history-version-1']").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        selected.ShouldBe("v2");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("Spacebar")]
    public void Pressing_space_on_an_option_selects_it(string spaceKey)
    {
        // AC3 — each option is a keyboard-reachable tab stop; Space activates it just like Enter (both browser spellings).
        string? selected = null;
        IRenderedComponent<ProposalVersionHistory> cut = Render<ProposalVersionHistory>(parameters => parameters
            .Add(h => h.TestId, "history")
            .Add(h => h.SelectedVersionId, "v1")
            .Add(h => h.Versions, [AgentUiTestData.VersionSummary(versionId: "v1"), AgentUiTestData.VersionSummary(versionId: "v2")])
            .Add(h => h.OnSelectVersion, id => { selected = id; }));

        cut.Find("[data-testid='history-version-1']").KeyDown(new KeyboardEventArgs { Key = spaceKey });

        selected.ShouldBe("v2");
    }

    [Fact]
    public void Re_selecting_the_already_selected_version_does_not_raise_the_callback()
    {
        // The host owns the selected-version lock; re-activating the current option must be a no-op (no redundant churn).
        int raised = 0;
        IRenderedComponent<ProposalVersionHistory> cut = Render<ProposalVersionHistory>(parameters => parameters
            .Add(h => h.TestId, "history")
            .Add(h => h.SelectedVersionId, "v1")
            .Add(h => h.Versions, [AgentUiTestData.VersionSummary(versionId: "v1"), AgentUiTestData.VersionSummary(versionId: "v2")])
            .Add(h => h.OnSelectVersion, _ => { raised++; }));

        cut.Find("[data-testid='history-version-0']").Click();

        raised.ShouldBe(0);
    }

    [Fact]
    public void Prior_versions_remain_listed_after_an_edit_and_a_regeneration()
    {
        // AD-5 append-only — the generated, edited, and regenerated versions all stay listed; none is deleted.
        IRenderedComponent<ProposalVersionHistory> cut = RenderHistory(
            AgentUiTestData.VersionSummary(versionId: "v1", kind: AgentGenerationKind.Generated),
            AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Edited, sourceVersionId: "v1", editorPartyId: "editor-1"),
            AgentUiTestData.VersionSummary(versionId: "v3", kind: AgentGenerationKind.Regenerated));

        cut.FindAll("[role='option']").Count.ShouldBe(3);
        cut.Markup.ShouldContain("v1");
        cut.Markup.ShouldContain("v2");
        cut.Markup.ShouldContain("v3");
    }

    [Fact]
    public void Approval_and_posting_markers_render_for_the_marked_versions()
    {
        IRenderedComponent<ProposalVersionHistory> cut = RenderHistory(
            AgentUiTestData.VersionSummary(versionId: "v1"),
            AgentUiTestData.VersionSummary(versionId: "v2", isApproved: true, isPosted: true));

        cut.FindAll("[data-testid='history-version-0-approved']").ShouldBeEmpty();
        cut.Find("[data-testid='history-version-1-approved']");
        cut.Find("[data-testid='history-version-1-posted']");
    }

    [Fact]
    public void No_accessible_name_or_test_id_carries_a_version_content_value()
    {
        // The summary is structurally content-free, but guard the rendering anyway: no aria-label/test-id may carry a
        // content-looking value. The author/source/provider/model ids are SAFE and may render (AD-14).
        const string contentSentinel = "top-secret-proposed-content-7f3a";
        IRenderedComponent<ProposalVersionHistory> cut = RenderHistory(
            AgentUiTestData.VersionSummary(versionId: "v1", editorPartyId: "editor-1"));

        foreach (IElement element in cut.FindAll("[aria-label]"))
        {
            (element.GetAttribute("aria-label") ?? string.Empty).ShouldNotContain(contentSentinel);
        }

        foreach (IElement element in cut.FindAll("[data-testid]"))
        {
            (element.GetAttribute("data-testid") ?? string.Empty).ShouldNotContain(contentSentinel);
        }
    }

    private IRenderedComponent<ProposalVersionHistory> RenderHistory(params ProposalVersionSummary[] versions)
        => RenderHistory(versions.FirstOrDefault()?.VersionId ?? string.Empty, versions);

    private IRenderedComponent<ProposalVersionHistory> RenderHistory(string selectedVersionId, params ProposalVersionSummary[] versions)
        => Render<ProposalVersionHistory>(parameters => parameters
            .Add(h => h.TestId, "history")
            .Add(h => h.SelectedVersionId, selectedVersionId)
            .Add(h => h.Versions, versions));
}
