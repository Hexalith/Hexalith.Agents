namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for <see cref="AgentResponseModeOrchestrator"/> (Story 1.6 AC1; AD-3, AD-12). Verifies the orchestration
/// authorizes fail-closed, builds the <see cref="ConfigureAgentResponseMode"/> command, and dispatches it with the
/// <b>server-populated</b> <c>actor:agentsAdmin</c> extension only (no dependency verdict) — and that a client cannot
/// forge <c>actor:agentsAdmin</c> or smuggle an activation verdict onto the config command (reserved keys stripped,
/// benign extensions preserved).
/// </summary>
public sealed class AgentResponseModeOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";

    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private AgentResponseModeOrchestrator Orchestrator => new(_dispatcher);

    [Fact]
    public async Task Authorized_request_dispatches_the_configure_command_with_the_server_populated_admin_extension()
    {
        CaptureDispatch();

        AgentResponseModeOutcome outcome = await Orchestrator.ExecuteAsync(Request(AgentResponseMode.Confirmation), CancellationToken.None);

        outcome.Authorized.ShouldBeTrue();
        outcome.Dispatched.ShouldBeTrue();

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(ConfigureAgentResponseMode));
        dispatched.AggregateId.ShouldBe(AgentId);
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");
        dispatched.Extensions.ContainsKey("provider:selectionValidation").ShouldBeFalse(); // config carries no verdict
        dispatched.Extensions.ContainsKey("approver:policyValidation").ShouldBeFalse();

        ConfigureAgentResponseMode command = JsonSerializer.Deserialize<ConfigureAgentResponseMode>(dispatched.Payload)!;
        command.Mode.ShouldBe(AgentResponseMode.Confirmation);
    }

    [Fact]
    public async Task Client_supplied_reserved_extensions_are_stripped_and_benign_ones_preserved()
    {
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",             // forged
            ["approver:policyValidation"] = "Valid",    // smuggled activation verdict — must be stripped
            ["provider:selectionValidation"] = "Valid", // smuggled activation verdict — must be stripped
            ["trace"] = "abc-123",                       // benign, must be preserved
        };

        await Orchestrator.ExecuteAsync(Request(AgentResponseMode.Automatic, clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");
        dispatched.Extensions.ContainsKey("approver:policyValidation").ShouldBeFalse();
        dispatched.Extensions.ContainsKey("provider:selectionValidation").ShouldBeFalse();
        dispatched.Extensions["trace"].ShouldBe("abc-123");
    }

    [Fact]
    public async Task Unauthorized_actor_is_denied_without_dispatching()
    {
        AgentResponseModeOutcome outcome = await Orchestrator.ExecuteAsync(
            Request(AgentResponseMode.Automatic, isAgentsAdmin: false), CancellationToken.None);

        outcome.Authorized.ShouldBeFalse();
        outcome.Dispatched.ShouldBeFalse();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    private static AgentResponseModeRequest Request(
        AgentResponseMode mode,
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        bool isAgentsAdmin = true)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            isAgentsAdmin,
            mode,
            clientExtensions);

    private CommandEnvelope? _lastDispatched;

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;
}
