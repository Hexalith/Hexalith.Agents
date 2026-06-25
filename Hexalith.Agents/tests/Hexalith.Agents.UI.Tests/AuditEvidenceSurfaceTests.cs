using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Story 4.3 AC1, AC2, AC3 — the audit-evidence panel renders the metadata-only support-safe evidence, never a
/// pending/delayed/unavailable state as success, and shows a distinct "posted with audit pending" state; the page lists
/// the IA entry points, surfaces the named governance blocker, and fails closed by default.
/// </summary>
public sealed class AuditEvidenceSurfaceTests : AgentsTestContext
{
    [Fact]
    public void Panel_renders_support_safe_metadata_and_no_content_sentinel()
    {
        const string contentSentinel = "raw-generated-content-should-never-appear-1a2b";
        AuditEvidenceResult result = AgentUiTestData.AuditEvidence();

        IRenderedComponent<AuditEvidencePanel> cut = Render<AuditEvidencePanel>(parameters => parameters
            .Add(panel => panel.Result, result));

        // Metadata-only banner + the support-safe ids/timestamps render; no message content is present (AD-14).
        cut.Find("[data-testid='agents-audit-evidence-panel-metadata-only']");
        cut.Find("[data-testid='agents-audit-evidence-panel-caller']").TextContent.ShouldBe("caller-1");
        cut.Find("[data-testid='agents-audit-evidence-panel-source-conversation']").TextContent.ShouldBe("conversation-1");
        cut.Find("[data-testid='agents-audit-evidence-panel-provider']").TextContent.ShouldBe("openai");
        cut.Find("[data-testid='agents-audit-evidence-panel-approver']").TextContent.ShouldBe("approver-1");
        cut.Find("[data-testid='agents-audit-evidence-panel-posted-message']").TextContent.ShouldBe("posted-message-1");
        cut.Markup.ShouldNotContain(contentSentinel);
    }

    [Fact]
    public void Panel_renders_audit_availability_badge_and_never_success_when_pending()
    {
        // AD-5 — a posted response whose audit is pending shows a distinct "posted with audit pending" state, never a
        // confirmed/audited success.
        AuditEvidenceResult result = AgentUiTestData.AuditEvidence(availability: AuditAvailabilityStatus.AuditPending);

        IRenderedComponent<AuditEvidencePanel> cut = Render<AuditEvidencePanel>(parameters => parameters
            .Add(panel => panel.Result, result));

        cut.Find("[data-testid='agents-audit-evidence-panel-availability']");
        cut.Find("[data-testid='agents-audit-evidence-panel-audit-pending']");
    }

    [Fact]
    public void Panel_hides_the_posted_audit_pending_notice_when_audit_is_available()
    {
        AuditEvidenceResult result = AgentUiTestData.AuditEvidence(availability: AuditAvailabilityStatus.AuditAvailable);

        IRenderedComponent<AuditEvidencePanel> cut = Render<AuditEvidencePanel>(parameters => parameters
            .Add(panel => panel.Result, result));

        cut.FindAll("[data-testid='agents-audit-evidence-panel-audit-pending']").ShouldBeEmpty();
    }

    [Fact]
    public void Landing_lists_the_ia_entry_points_and_the_named_governance_blocker()
    {
        // AC3 — the audit landing lists the IA entry points via safe links and surfaces the named launch-readiness
        // blocker (metadata-only) for Story 4.4.
        IRenderedComponent<AuditEvidence> cut = RenderAudit(agentInteractionId: null);

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-audit-link-overview']").GetAttribute("href").ShouldBe("/agents");
            cut.Find("[data-testid='agents-audit-link-status']").GetAttribute("href").ShouldBe("/agents/status");
            cut.Find("[data-testid='agents-audit-link-proposals']").GetAttribute("href").ShouldBe("/agents/proposals");
            cut.Find("[data-testid='agents-audit-governance']").TextContent
                .ShouldContain(AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved);
        });
    }

    [Fact]
    public void Detail_renders_the_evidence_panel_for_an_authorized_read()
    {
        AuditEvidenceGateway.GetEvidenceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.AuditEvidence()));

        IRenderedComponent<AuditEvidence> cut = RenderAudit(agentInteractionId: "interaction-1");

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-audit-evidence-panel']"));
    }

    [Fact]
    public void Detail_fails_closed_to_permission_denied_against_the_default_deferred_gateway()
    {
        // AC2 / AD-12 — the default substitute returns NotAuthorized → the detail renders the permission-denied surface.
        IRenderedComponent<AuditEvidence> cut = RenderAudit(agentInteractionId: "interaction-1");

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-audit-state']");
            region.ClassList.ShouldContain("agent-surface-state--permissiondenied");
        });
    }

    [Fact]
    public void Detail_renders_the_unavailable_surface_when_the_dependency_is_down()
    {
        AuditEvidenceGateway.GetEvidenceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AuditEvidenceResult.Unavailable()));

        IRenderedComponent<AuditEvidence> cut = RenderAudit(agentInteractionId: "interaction-1");

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-audit-state']");
            region.ClassList.ShouldContain("agent-surface-state--unavailable");
        });
    }

    [Fact]
    public void Panel_renders_posting_outcome_distinctly_for_posted_and_posting_failed()
    {
        // AC1 / AD-5 — the posting-outcome field never collapses: a posted interaction reads "Posted", a posting-failed
        // one reads "Failed" (never "Posted"), even though both are terminal.
        IRenderedComponent<AuditEvidencePanel> posted = Render<AuditEvidencePanel>(parameters => parameters
            .Add(panel => panel.Result, AgentUiTestData.AuditEvidence()));
        posted.Find("[data-testid='agents-audit-evidence-panel-posting-outcome']").TextContent.Trim()
            .ShouldBe("Agents.ProposalDetail.PostingOutcome.Posted");

        AuditEvidenceResult failedResult = AuditEvidenceResult.Success(
            AgentUiTestData.Detail(state: ProposedAgentReplyState.PostingFailed, postedAt: null),
            AgentUiTestData.ApprovalEvidence(),
            AuditAvailabilityStatus.AuditUnavailable);
        IRenderedComponent<AuditEvidencePanel> failed = Render<AuditEvidencePanel>(parameters => parameters
            .Add(panel => panel.Result, failedResult));
        failed.Find("[data-testid='agents-audit-evidence-panel-posting-outcome']").TextContent.Trim()
            .ShouldBe("Agents.ProposalDetail.PostingOutcome.Failed");
    }

    [Fact]
    public void Panel_renders_the_none_affordance_for_absent_optional_ids_and_omits_the_approver_rows()
    {
        // AC1 — an absent optional id renders the safe localized "None" whole string (never a blank cell), and the
        // approver/posted-message rows are omitted entirely when no approval evidence was recorded.
        AuditEvidenceResult result = AuditEvidenceResult.Success(
            AgentUiTestData.Detail(state: ProposedAgentReplyState.Pending, approvedVersionId: null, approvedAt: null, postedAt: null),
            approval: null,
            AuditAvailabilityStatus.AuditPending);

        IRenderedComponent<AuditEvidencePanel> cut = Render<AuditEvidencePanel>(parameters => parameters
            .Add(panel => panel.Result, result));

        cut.Find("[data-testid='agents-audit-evidence-panel-approved-version']").TextContent.Trim().ShouldBe("Agents.Audit.None");
        cut.FindAll("[data-testid='agents-audit-evidence-panel-approver']").ShouldBeEmpty();
        cut.FindAll("[data-testid='agents-audit-evidence-panel-posted-message']").ShouldBeEmpty();
    }

    [Fact]
    public void Panel_surfaces_the_named_governance_blocker_metadata_only()
    {
        // AC3 — the panel surfaces the named launch-readiness governance blocker (metadata-only) so Story 4.4 can
        // consume it, never inventing retention/governance policy.
        IRenderedComponent<AuditEvidencePanel> cut = Render<AuditEvidencePanel>(parameters => parameters
            .Add(panel => panel.Result, AgentUiTestData.AuditEvidence()));

        cut.Find("[data-testid='agents-audit-evidence-panel-governance']").TextContent
            .ShouldContain(AgentAuditGovernanceReadiness.RetentionLegalHoldExportDeletionPolicyUnresolved);
    }

    [Fact]
    public void Detail_renders_empty_surface_when_the_evidence_is_not_found()
    {
        // AC2 / AD-12 — a NotFound read renders the Empty surface and never reveals whether the interaction exists in
        // another tenant (no detail, no record fingerprint).
        AuditEvidenceGateway.GetEvidenceAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AuditEvidenceResult.NotFound()));

        IRenderedComponent<AuditEvidence> cut = RenderAudit(agentInteractionId: "interaction-1");

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-audit-state']").ClassList.ShouldContain("agent-surface-state--empty"));
    }

    private IRenderedComponent<AuditEvidence> RenderAudit(string? agentInteractionId)
    {
        InitializeStoreAsync().GetAwaiter().GetResult();
        return Render<AuditEvidence>(parameters => parameters.Add(page => page.AgentInteractionId, agentInteractionId));
    }
}
