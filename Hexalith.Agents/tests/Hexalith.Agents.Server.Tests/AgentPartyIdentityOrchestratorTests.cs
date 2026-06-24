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
/// Tests for <see cref="AgentPartyIdentityOrchestrator"/> (Story 1.4 AC1, AC2, AC3; AD-3, AD-12). Verifies the
/// orchestration authorizes fail-closed, resolves the Parties verdict through the port, builds the correct
/// link/replace command, and dispatches it with the <b>server-populated</b> trusted extensions — and, critically,
/// that a client cannot forge <c>actor:agentsAdmin</c> / <c>party:linkValidation</c> to bypass Parties validation
/// (the reserved keys are stripped and repopulated from trusted sources only).
/// </summary>
public sealed class AgentPartyIdentityOrchestratorTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";

    private readonly IAgentPartyDirectory _directory = Substitute.For<IAgentPartyDirectory>();
    private readonly IAgentCommandDispatcher _dispatcher = Substitute.For<IAgentCommandDispatcher>();

    private AgentPartyIdentityOrchestrator Orchestrator => new(_directory, _dispatcher);

    [Fact]
    public async Task Authorized_link_existing_with_valid_verdict_dispatches_link_command_with_trusted_extensions()
    {
        _directory.ValidateExistingPartyAsync(TenantId, "party-001", Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "party-001"));
        CaptureDispatch();

        AgentPartyLinkOutcome outcome = await Orchestrator.ExecuteAsync(
            Request(partyId: "party-001"), CancellationToken.None);

        outcome.Authorized.ShouldBeTrue();
        outcome.Dispatched.ShouldBeTrue();
        outcome.Verdict.ShouldBe(PartyLinkValidationStatus.Valid);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(LinkAgentPartyIdentity));
        dispatched.AggregateId.ShouldBe(AgentId);
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");
        dispatched.Extensions["party:linkValidation"].ShouldBe("Valid");
        JsonSerializer.Deserialize<LinkAgentPartyIdentity>(dispatched.Payload)!.PartyId.ShouldBe("party-001");
    }

    [Fact]
    public async Task Client_supplied_reserved_extensions_are_stripped_and_repopulated_from_trusted_sources()
    {
        // The directory's REAL verdict is Missing; the client forges a Valid verdict to try to bypass validation.
        _directory.ValidateExistingPartyAsync(TenantId, "party-001", Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Missing, null));
        CaptureDispatch();

        var clientExtensions = new Dictionary<string, string>
        {
            ["actor:agentsAdmin"] = "true",          // forged
            ["party:linkValidation"] = "Valid",      // forged bypass attempt
            ["trace"] = "abc-123",                   // benign, must be preserved
        };

        await Orchestrator.ExecuteAsync(Request(partyId: "party-001", clientExtensions: clientExtensions), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.Extensions.ShouldNotBeNull();
        dispatched.Extensions["party:linkValidation"].ShouldBe("Missing"); // the trusted verdict wins, not the forged "Valid"
        dispatched.Extensions["actor:agentsAdmin"].ShouldBe("true");        // repopulated from the trusted decision
        dispatched.Extensions["trace"].ShouldBe("abc-123");                 // non-reserved client extension preserved
    }

    [Fact]
    public async Task Unauthorized_actor_is_denied_without_calling_parties_or_dispatching()
    {
        AgentPartyLinkOutcome outcome = await Orchestrator.ExecuteAsync(
            Request(partyId: "party-001", isAgentsAdmin: false), CancellationToken.None);

        outcome.Authorized.ShouldBeFalse();
        outcome.Dispatched.ShouldBeFalse();
        await _directory.DidNotReceiveWithAnyArgs().ValidateExistingPartyAsync(default!, default!, default);
        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task Replace_operation_dispatches_a_replace_command()
    {
        _directory.ValidateExistingPartyAsync(TenantId, "party-002", Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "party-002"));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(
            Request(partyId: "party-002", operation: AgentPartyLinkOperation.Replace), CancellationToken.None);

        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        dispatched.CommandType.ShouldBe(nameof(ReplaceAgentPartyIdentity));
        JsonSerializer.Deserialize<ReplaceAgentPartyIdentity>(dispatched.Payload)!.PartyId.ShouldBe("party-002");
    }

    [Fact]
    public async Task Provision_new_links_the_provisioned_party_id()
    {
        _directory.ProvisionAgentPartyAsync(TenantId, Arg.Any<AgentPartyProvisioningRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "agent-hexa"));
        CaptureDispatch();

        AgentPartyLinkOutcome outcome = await Orchestrator.ExecuteAsync(
            Request(partyId: null, source: AgentPartyLinkSource.ProvisionNewParty), CancellationToken.None);

        await _directory.Received(1).ProvisionAgentPartyAsync(TenantId, Arg.Any<AgentPartyProvisioningRequest>(), Arg.Any<CancellationToken>());
        outcome.PartyId.ShouldBe("agent-hexa");
        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        JsonSerializer.Deserialize<LinkAgentPartyIdentity>(dispatched.Payload)!.PartyId.ShouldBe("agent-hexa");
    }

    [Fact]
    public async Task Non_valid_verdict_is_still_dispatched_for_an_auditable_rejection()
    {
        _directory.ValidateExistingPartyAsync(TenantId, "party-001", Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Unavailable, null));
        CaptureDispatch();

        AgentPartyLinkOutcome outcome = await Orchestrator.ExecuteAsync(Request(partyId: "party-001"), CancellationToken.None);

        outcome.Verdict.ShouldBe(PartyLinkValidationStatus.Unavailable);
        outcome.Dispatched.ShouldBeTrue();
        await _dispatcher.Received(1).DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>());
        LastDispatched().ShouldNotBeNull().Extensions!["party:linkValidation"].ShouldBe("Unavailable");
    }

    [Fact]
    public async Task Non_valid_verdict_dispatches_a_well_formed_command_carrying_the_requested_party_id()
    {
        // When validation yields no id (Missing → null), the orchestration falls back to the requested id so the
        // dispatched command is still well-formed and the aggregate records an auditable, attributable rejection.
        _directory.ValidateExistingPartyAsync(TenantId, "party-001", Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Missing, null));
        CaptureDispatch();

        AgentPartyLinkOutcome outcome = await Orchestrator.ExecuteAsync(Request(partyId: "party-001"), CancellationToken.None);

        outcome.PartyId.ShouldBe("party-001");
        CommandEnvelope dispatched = LastDispatched().ShouldNotBeNull();
        JsonSerializer.Deserialize<LinkAgentPartyIdentity>(dispatched.Payload)!.PartyId.ShouldBe("party-001");
        dispatched.Extensions!["party:linkValidation"].ShouldBe("Missing");
    }

    [Fact]
    public async Task Link_existing_without_a_party_id_throws_and_dispatches_nothing()
    {
        // The existing-party path requires a concrete id; a null/blank id is a programming error, not a verdict —
        // it must fail fast before any dispatch (and after the auth gate already passed).
        await Should.ThrowAsync<ArgumentException>(
            async () => await Orchestrator.ExecuteAsync(Request(partyId: null), CancellationToken.None));

        await _dispatcher.DidNotReceiveWithAnyArgs().DispatchAsync(default!, default);
    }

    [Fact]
    public async Task Provision_new_without_a_label_defaults_the_label_from_the_agent_id()
    {
        AgentPartyProvisioningRequest? captured = null;
        _directory.ProvisionAgentPartyAsync(TenantId, Arg.Do<AgentPartyProvisioningRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "agent-hexa"));
        CaptureDispatch();

        await Orchestrator.ExecuteAsync(
            Request(partyId: null, source: AgentPartyLinkSource.ProvisionNewParty, organizationLabel: null), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.AgentId.ShouldBe(AgentId);
        captured.OrganizationLabel.ShouldBe("Agent hexa"); // defaulted from the agent id when none supplied (label lives in Parties)
    }

    private static AgentPartyLinkRequest Request(
        string? partyId,
        bool isAgentsAdmin = true,
        AgentPartyLinkSource source = AgentPartyLinkSource.ExistingParty,
        AgentPartyLinkOperation operation = AgentPartyLinkOperation.Link,
        IReadOnlyDictionary<string, string>? clientExtensions = null,
        string? organizationLabel = "Agent hexa")
        => new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TenantId,
            AgentId,
            ActorUserId: "admin-user",
            isAgentsAdmin,
            source,
            operation,
            partyId,
            organizationLabel,
            clientExtensions);

    private CommandEnvelope? _lastDispatched;

    private CommandEnvelope? CaptureDispatch()
    {
        _dispatcher.DispatchAsync(Arg.Do<CommandEnvelope>(e => _lastDispatched = e), Arg.Any<CancellationToken>());
        return _lastDispatched;
    }

    private CommandEnvelope? LastDispatched() => _lastDispatched;
}
