using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Local admission ordering; substitutes establish no live dependency availability.</summary>
public sealed class AgentInteractionGateAdmissionTests
{
    private readonly ITenantAccessReader _tenants = Substitute.For<ITenantAccessReader>();
    private readonly IAgentPartyDirectory _parties = Substitute.For<IAgentPartyDirectory>();
    private readonly IConversationAccessReader _conversations = Substitute.For<IConversationAccessReader>();
    private readonly IAgentInvocationReadinessReader _readiness = Substitute.For<IAgentInvocationReadinessReader>();
    private readonly IProviderCatalogReader _providers = Substitute.For<IProviderCatalogReader>();
    private readonly IApproverPolicyResolver _approvers = Substitute.For<IApproverPolicyResolver>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();
    private readonly List<string> _reads = [];

    /// <summary>Missing, revoked, uncertain and malformed caller evidence stops before Conversation and Agent reads.</summary>
    [Theory]
    [InlineData(PartyLinkValidationStatus.Unknown)]
    [InlineData(PartyLinkValidationStatus.Missing)]
    [InlineData(PartyLinkValidationStatus.Disabled)]
    [InlineData(PartyLinkValidationStatus.Ambiguous)]
    [InlineData(PartyLinkValidationStatus.Unavailable)]
    [InlineData(PartyLinkValidationStatus.Unauthorized)]
    [InlineData((PartyLinkValidationStatus)999)]
    public async Task CallerDenialStopsBeforeProtectedReadsOrDispatch(PartyLinkValidationStatus status)
    {
        Configure();
        _parties.ValidateCallerPartyAsync("tenant-a", "caller", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _reads.Add("Caller");
                return new AgentPartyValidationResult(status, null);
            });

        AgentInteractionGateOutcomeResult result = await Orchestrator().ExecuteAsync(Request(), CancellationToken.None);

        result.Status.ShouldBe(AgentInteractionStatus.Denied);
        _reads.ShouldBe(["Tenant", "Caller"]);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Conversation denial or uncertainty stops before Agent, Provider and approver reads.</summary>
    [Theory]
    [InlineData(AgentInteractionGateOutcome.Unknown, true)]
    [InlineData(AgentInteractionGateOutcome.Missing, true)]
    [InlineData(AgentInteractionGateOutcome.Disabled, true)]
    [InlineData(AgentInteractionGateOutcome.Stale, true)]
    [InlineData(AgentInteractionGateOutcome.Unavailable, true)]
    [InlineData(AgentInteractionGateOutcome.Unauthorized, true)]
    [InlineData((AgentInteractionGateOutcome)999, true)]
    [InlineData(AgentInteractionGateOutcome.Satisfied, false)]
    public async Task ConversationDenialStopsBeforeProtectedReadsOrDispatch(AgentInteractionGateOutcome outcome, bool fresh)
    {
        Configure();
        _conversations.ReadAsync("tenant-a", "conversation", "caller", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                _reads.Add("Conversation");
                return new ConversationAccessReadResult(outcome, fresh);
            });

        AgentInteractionGateOutcomeResult result = await Orchestrator().ExecuteAsync(Request(), CancellationToken.None);

        result.Status.ShouldBe(AgentInteractionStatus.Denied);
        _reads.ShouldBe(["Tenant", "Caller", "Conversation"]);
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A token cancelled before admission performs no dependency read.</summary>
    [Fact]
    public async Task CancellationBeforeAdmissionPerformsNoReadOrDispatch()
    {
        Configure();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => Orchestrator().ExecuteAsync(Request(), cancellation.Token));

        _reads.ShouldBeEmpty();
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>A cancelled completed read or failed read performs no later read or command construction.</summary>
    [Theory]
    [InlineData("Tenant", false)]
    [InlineData("Caller", false)]
    [InlineData("Conversation", false)]
    [InlineData("Readiness", false)]
    [InlineData("AgentParty", false)]
    [InlineData("Provider", false)]
    [InlineData("Approver", false)]
    [InlineData("Tenant", true)]
    [InlineData("Caller", true)]
    [InlineData("Conversation", true)]
    [InlineData("Readiness", true)]
    [InlineData("AgentParty", true)]
    [InlineData("Provider", true)]
    [InlineData("Approver", true)]
    public async Task CancellationDuringDependencyReadStopsBeforeFurtherWork(string cancelledRead, bool throws)
    {
        using var cancellation = new CancellationTokenSource();
        Configure(cancelledRead, cancellation, throws);

        OperationCanceledException exception = await Should.ThrowAsync<OperationCanceledException>(
            () => Orchestrator().ExecuteAsync(Request(), cancellation.Token));

        exception.CancellationToken.ShouldBe(cancellation.Token);
        string[] order = ["Tenant", "Caller", "Conversation", "Readiness", "AgentParty", "Provider", "Approver"];
        _reads.ShouldBe(order.Take(Array.IndexOf(order, cancelledRead) + 1));
        _dispatcher.ReceivedCalls().ShouldBeEmpty();
    }

    private AgentInteractionGateOrchestrator Orchestrator()
        => new(_tenants, _conversations, _readiness, _parties, _providers, _approvers, _dispatcher);

    private void Configure(string? cancelledRead = null, CancellationTokenSource? cancellation = null, bool throws = false)
    {
        var source = new ApproverPolicySource(ApproverPolicySourceKind.TenantRole, null, "Approver");
        var policy = new AgentApproverPolicy([source], ApproverPolicyBasisDisclosure.OperatorOnly);
        _tenants.ReadAsync("tenant-a", "subject", "caller", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Tenant");
            return new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, true);
        });
        _parties.ValidateCallerPartyAsync("tenant-a", "caller", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Caller");
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "caller");
        });
        _parties.ValidateExistingPartyAsync("tenant-a", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            Complete("AgentParty");
            return new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, call.ArgAt<string>(1));
        });
        _conversations.ReadAsync("tenant-a", "conversation", "caller", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Conversation");
            return new ConversationAccessReadResult(AgentInteractionGateOutcome.Satisfied, true);
        });
        _readiness.ReadAsync("tenant-a", "hexa", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Readiness");
            return new AgentInvocationReadiness(true, AgentLifecycleStatus.Active, true, "agent-party",
                AgentResponseMode.Confirmation, true, "provider", "model", policy, true, true);
        });
        _providers.GetEntryAsync("tenant-a", "provider", "model", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Provider");
            return new ProviderCatalogEntryReadResult(ProviderCatalogInspectionStatus.EntryNotFound, null);
        });
        _approvers.ResolveAsync("tenant-a", policy, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Complete("Approver");
            return new ApproverPolicyResolutionResult([new(source.Kind, ApproverSourceOutcome.Resolved) { Source = source }]);
        });

        void Complete(string read)
        {
            _reads.Add(read);
            if (read == cancelledRead)
            {
                cancellation!.Cancel();
                if (throws)
                {
                    throw new InvalidOperationException("Unavailable source after cancellation.");
                }
            }
        }
    }

    private static AgentInteractionGateRequest Request()
        => new("message", "correlation", "tenant-a", "interaction", "hexa", "subject", "caller", "conversation",
            "provider", "model", AgentResponseMode.Confirmation, null, null);
}
