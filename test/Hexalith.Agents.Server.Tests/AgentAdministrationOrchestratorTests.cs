namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentAdministrationOrchestrator"/> (Story 5.2 AC1, AC4). The create, update, and disable
/// commands carry no dependency verdict, so the trust rule is the same one
/// <see cref="AgentResponseModeOrchestrator"/> enforces: authorize first, strip every client-supplied reserved
/// extension, and repopulate only the admin flag the server itself decided.
/// </summary>
public sealed class AgentAdministrationOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";

    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();
    private CommandEnvelope? _dispatched;

    private AgentAdministrationOrchestrator Orchestrator => new(_dispatcher);

    [Fact]
    public async Task An_authorized_update_dispatches_with_the_server_populated_admin_extension()
    {
        CaptureDispatch();

        AgentAdministrationOutcome outcome = await Orchestrator.UpdateConfigurationAsync(
            Request(),
            new UpdateAgentConfiguration("hexa", null, "instructions long enough to be valid"),
            CancellationToken.None);

        outcome.Authorized.ShouldBeTrue();
        outcome.Dispatched.ShouldBeTrue();

        CommandEnvelope envelope = _dispatched.ShouldNotBeNull();
        envelope.CommandType.ShouldBe(nameof(UpdateAgentConfiguration));
        envelope.Domain.ShouldBe("agent");
        envelope.AggregateId.ShouldBe(AgentId);
        envelope.TenantId.ShouldBe(TenantId);
        envelope.Extensions.ShouldNotBeNull()["actor:agentsAdmin"].ShouldBe("true");
    }

    [Fact]
    public async Task An_authorized_create_carries_the_safe_payload()
    {
        CaptureDispatch();

        await Orchestrator.CreateAsync(
            Request(),
            new CreateAgent(TenantId, "hexa", null, "instructions long enough to be valid"),
            CancellationToken.None);

        CommandEnvelope envelope = _dispatched.ShouldNotBeNull();
        envelope.CommandType.ShouldBe(nameof(CreateAgent));
        JsonSerializer.Deserialize<CreateAgent>(envelope.Payload)!.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task A_create_body_naming_another_tenant_is_created_in_the_callers_tenant()
    {
        CaptureDispatch();

        await Orchestrator.CreateAsync(
            Request(),
            new CreateAgent("victim-tenant", "hexa", null, "instructions long enough to be valid"),
            CancellationToken.None);

        CommandEnvelope envelope = _dispatched.ShouldNotBeNull();

        // The stored tenant scope comes from the authenticated caller, never from the body — so the envelope tenant
        // and the tenant the aggregate persists can never diverge (AC4).
        envelope.TenantId.ShouldBe(TenantId);
        JsonSerializer.Deserialize<CreateAgent>(envelope.Payload)!.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task An_authorized_disable_dispatches_the_lifecycle_command()
    {
        CaptureDispatch();

        await Orchestrator.DisableAsync(Request(), new DisableAgent(), CancellationToken.None);

        _dispatched.ShouldNotBeNull().CommandType.ShouldBe(nameof(DisableAgent));
    }

    [Fact]
    public async Task Client_supplied_reserved_extensions_are_stripped_and_benign_ones_preserved()
    {
        CaptureDispatch();

        await Orchestrator.UpdateConfigurationAsync(
            Request(new Dictionary<string, string>
            {
                ["actor:agentsAdmin"] = "true",
                ["provider:selectionValidation"] = "Valid",
                ["approver:policyValidation"] = "Valid",
                ["trace"] = "abc-123",
            }),
            new UpdateAgentConfiguration("hexa", null, "instructions long enough to be valid"),
            CancellationToken.None);

        IDictionary<string, string> extensions = _dispatched.ShouldNotBeNull().Extensions.ShouldNotBeNull();
        extensions["actor:agentsAdmin"].ShouldBe("true");
        extensions.ContainsKey("provider:selectionValidation").ShouldBeFalse();
        extensions.ContainsKey("approver:policyValidation").ShouldBeFalse();
        extensions["trace"].ShouldBe("abc-123");
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("disable")]
    public async Task An_unauthorized_actor_is_denied_before_any_dispatch(string operation)
    {
        AgentAdministrationRequest request = Request(isAgentsAdmin: false);

        AgentAdministrationOutcome outcome = operation switch
        {
            "create" => await Orchestrator.CreateAsync(request, new CreateAgent(TenantId, "hexa", null, "instructions long enough"), CancellationToken.None),
            "update" => await Orchestrator.UpdateConfigurationAsync(request, new UpdateAgentConfiguration("hexa", null, "instructions long enough"), CancellationToken.None),
            _ => await Orchestrator.DisableAsync(request, new DisableAgent(), CancellationToken.None),
        };

        outcome.Authorized.ShouldBeFalse();
        outcome.Dispatched.ShouldBeFalse();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    private static AgentAdministrationRequest Request(
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        bool isAgentsAdmin = true)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            isAgentsAdmin,
            clientExtensions);

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(envelope => _dispatched = envelope), Arg.Any<CancellationToken>());
}
