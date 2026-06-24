using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// The deferred gateway placeholders keep the DI graph complete before the live read path is wired (Epic 4). They
/// must fail closed (AD-12): a host that has not yet bound the real read path renders the permission-denied surface
/// rather than fabricating a "ready/healthy" Agent or an empty "no providers configured" success. The component
/// tests substitute the gateways, so this contract is otherwise unexercised.
/// </summary>
public sealed class DeferredGatewayTests
{
    [Fact]
    public async Task DeferredAgentSetupGateway_status_fails_closed_with_not_authorized_and_no_agent()
    {
        DeferredAgentSetupGateway gateway = new();

        AgentInspectionResult result = await gateway.GetStatusAsync(CancellationToken.None);

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Agent.ShouldBeNull();
    }

    [Fact]
    public async Task DeferredAgentSetupGateway_configuration_fails_closed_with_not_authorized_and_no_agent()
    {
        DeferredAgentSetupGateway gateway = new();

        AgentInspectionResult result = await gateway.GetConfigurationAsync(CancellationToken.None);

        result.Status.ShouldBe(AgentInspectionStatus.NotAuthorized);
        result.Agent.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeferredProviderCatalogGateway_fails_closed_with_not_authorized_and_no_entries(bool includeDisabled)
    {
        DeferredProviderCatalogGateway gateway = new();

        ProviderCatalogInspectionResult result = await gateway.ListEntriesAsync(includeDisabled, CancellationToken.None);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        result.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeferredConversationAgentCallGateway_request_fails_closed_with_not_authorized_and_no_reference()
    {
        DeferredConversationAgentCallGateway gateway = new();

        AgentCallRequestResult result = await gateway.RequestCallAsync(
            new ConversationAgentCallRequest("conversation-1", "prompt", null), CancellationToken.None);

        result.Status.ShouldBe(AgentCallRequestStatus.NotAuthorized);
        result.Reference.ShouldBeNull();
    }

    [Fact]
    public async Task DeferredConversationAgentCallGateway_status_fails_closed_with_not_authorized_and_no_view()
    {
        DeferredConversationAgentCallGateway gateway = new();

        AgentInteractionInspectionResult result = await gateway.GetCallStatusAsync("interaction-1", CancellationToken.None);

        result.Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
        result.View.ShouldBeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeferredProposalQueueGateway_fails_closed_with_not_authorized_no_rows_and_zero_count(bool includeHistorical)
    {
        DeferredProposalQueueGateway gateway = new();

        PendingProposalsResult result = await gateway.ListPendingProposalsAsync(includeHistorical, CancellationToken.None);

        result.Status.ShouldBe(PendingProposalsInspectionStatus.NotAuthorized);
        result.Proposals.ShouldBeEmpty();
        result.PendingCount.ShouldBe(0);
    }

    [Fact]
    public async Task DeferredProposalEditGateway_fails_closed_with_not_authorized_and_no_version()
    {
        DeferredProposalEditGateway gateway = new();

        ProposalEditResult result = await gateway.EditProposalAsync(
            new ProposalEditRequest("interaction-1", "proposal-1", "version-1", "edited content", null), CancellationToken.None);

        result.Status.ShouldBe(ProposalEditStatus.NotAuthorized);
        result.EditedVersionId.ShouldBeNull();
    }

    [Fact]
    public async Task DeferredProposalRegenerationGateway_fails_closed_with_not_authorized_and_no_version()
    {
        DeferredProposalRegenerationGateway gateway = new();

        ProposalRegenerationResult result = await gateway.RegenerateProposalAsync(
            new ProposalRegenerationRequest("interaction-1", "proposal-1", null), CancellationToken.None);

        result.Status.ShouldBe(ProposalRegenerationStatus.NotAuthorized);
        result.RegeneratedVersionId.ShouldBeNull();
    }

    [Fact]
    public async Task DeferredProposalApprovalGateway_fails_closed_with_not_authorized_and_no_message()
    {
        DeferredProposalApprovalGateway gateway = new();

        ProposalApprovalResult result = await gateway.ApproveProposalAsync(
            new ProposalApprovalRequest("interaction-1", "proposal-1", "version-1", null), CancellationToken.None);

        result.Status.ShouldBe(ProposalApprovalStatus.NotAuthorized);
        result.SelectedVersionId.ShouldBeNull();
        result.MessageId.ShouldBeNull();
    }
}
