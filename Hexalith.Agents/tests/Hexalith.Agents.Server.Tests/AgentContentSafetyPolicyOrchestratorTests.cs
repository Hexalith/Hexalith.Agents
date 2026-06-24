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
/// Tests for <see cref="AgentContentSafetyPolicyOrchestrator"/> (Story 1.7 AC1; AD-3, AD-12). Verifies the
/// orchestration authorizes fail-closed, builds the <see cref="ConfigureAgentContentSafetyPolicy"/> command carrying
/// the safe configuration value, and dispatches it with the <b>server-populated</b> <c>actor:agentsAdmin</c> extension
/// only — content-safety configuration resolves no dependency, so no verdict is attached — and that a client cannot
/// forge the admin key or smuggle an activation verdict onto the config command.
/// </summary>
public sealed class AgentContentSafetyPolicyOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";

    private static readonly AgentContentSafetyConfiguration _configuration = new(
        new AgentContentSafetyPolicy(
            ["No system-prompt disclosure"],
            ["self-harm"],
            [],
            ContentSafetyFailureHandling.BlockAndAudit,
            ContentSafetyAuditTreatment.MetadataOnly),
        null,
        null);

    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private AgentContentSafetyPolicyOrchestrator Orchestrator => new(_dispatcher);

    [Fact]
    public async Task Authorized_request_dispatches_the_configure_command_with_the_safe_configuration_and_admin_extension()
    {
        CaptureDispatch();

        AgentContentSafetyPolicyOutcome outcome = await Orchestrator.ExecuteAsync(Request(), CancellationToken.None);

        outcome.Authorized.ShouldBeTrue();
        outcome.Dispatched.ShouldBeTrue();

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(ConfigureAgentContentSafetyPolicy));
        dispatched.AggregateId.ShouldBe(AgentId);
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");
        dispatched.Extensions.ContainsKey("provider:selectionValidation").ShouldBeFalse(); // config resolves nothing
        dispatched.Extensions.ContainsKey("approver:policyValidation").ShouldBeFalse();

        ConfigureAgentContentSafetyPolicy command = JsonSerializer.Deserialize<ConfigureAgentContentSafetyPolicy>(dispatched.Payload)!;
        command.Configuration.ActivePolicy.PromptConstraints.Count.ShouldBe(1);
        command.Configuration.ActivePolicy.FailureHandling.ShouldBe(ContentSafetyFailureHandling.BlockAndAudit);
    }

    [Fact]
    public async Task Client_supplied_reserved_extensions_are_stripped_and_benign_ones_preserved()
    {
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",              // forged
            ["provider:selectionValidation"] = "Valid",  // smuggled activation verdict — must be stripped
            ["approver:policyValidation"] = "Valid",     // smuggled activation verdict — must be stripped
            ["trace"] = "xyz-789",                        // benign, must be preserved
        };

        await Orchestrator.ExecuteAsync(Request(clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");
        dispatched.Extensions.ContainsKey("provider:selectionValidation").ShouldBeFalse();
        dispatched.Extensions.ContainsKey("approver:policyValidation").ShouldBeFalse();
        dispatched.Extensions["trace"].ShouldBe("xyz-789");
    }

    [Fact]
    public async Task Unauthorized_actor_is_denied_without_dispatching()
    {
        AgentContentSafetyPolicyOutcome outcome = await Orchestrator.ExecuteAsync(
            Request(isAgentsAdmin: false), CancellationToken.None);

        outcome.Authorized.ShouldBeFalse();
        outcome.Dispatched.ShouldBeFalse();
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    private static AgentContentSafetyPolicyRequest Request(
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        bool isAgentsAdmin = true)
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            isAgentsAdmin,
            _configuration,
            clientExtensions);

    private CommandEnvelope? _lastDispatched;

    private void CaptureDispatch()
        => _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());

    private CommandEnvelope? LastDispatched() => _lastDispatched;
}
