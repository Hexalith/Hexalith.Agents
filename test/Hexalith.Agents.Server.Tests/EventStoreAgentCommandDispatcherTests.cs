namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using NSubstitute;

using Shouldly;

/// <summary>
/// Tests for <see cref="EventStoreAgentCommandDispatcher"/> (Story 5.2 AC1, AC3): the live submit seam that
/// replaced the fail-closed placeholder. It must forward exactly what the orchestration built — including the
/// message id as the idempotency key, so an exact duplicate submission does not append a second event.
/// </summary>
public sealed class EventStoreAgentCommandDispatcherTests
{
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();
    private SubmitCommandRequest? _submitted;

    [Fact]
    public async Task The_envelope_is_submitted_with_its_message_id_as_the_idempotency_key()
    {
        CaptureSubmit();

        await Dispatcher().DispatchAsync(Envelope(), CancellationToken.None);

        SubmitCommandRequest request = _submitted.ShouldNotBeNull();
        request.MessageId.ShouldBe("msg-1");
        request.IdempotencyKey.ShouldBe("msg-1");
        request.Tenant.ShouldBe("acme");
        request.Domain.ShouldBe("agent");
        request.AggregateId.ShouldBe("hexa");
        request.CommandType.ShouldBe("DisableAgent");
        request.Extensions.ShouldNotBeNull()["actor:agentsAdmin"].ShouldBe("true");
    }

    [Fact]
    public async Task An_empty_payload_is_submitted_as_an_empty_object_rather_than_failing()
    {
        CaptureSubmit();

        await Dispatcher().DispatchAsync(Envelope(payload: []), CancellationToken.None);

        _submitted.ShouldNotBeNull().Payload.ValueKind.ShouldBe(JsonValueKind.Object);
    }

    [Fact]
    public async Task A_gateway_failure_surfaces_rather_than_being_reported_as_an_accepted_command()
    {
        _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<SubmitCommandResponse>>(_ => throw Gateway(503));

        _ = await Should.ThrowAsync<EventStoreGatewayException>(
            () => Dispatcher().DispatchAsync(Envelope(), CancellationToken.None));
    }

    [Theory]
    [InlineData(400, AgentOperationErrorCode.ValidationFailed)]
    [InlineData(403, AgentOperationErrorCode.NotAuthorized)]
    [InlineData(404, AgentOperationErrorCode.NotFound)]
    [InlineData(409, AgentOperationErrorCode.Conflict)]
    [InlineData(412, AgentOperationErrorCode.Stale)]
    [InlineData(503, AgentOperationErrorCode.Unavailable)]
    public void Gateway_failures_map_to_typed_public_codes(int statusCode, AgentOperationErrorCode expected)
        => AgentCommandDispatchFailure.Map(Gateway(statusCode)).ShouldBe(expected);

    [Fact]
    public void A_non_gateway_failure_maps_to_unavailable_rather_than_leaking_its_kind()
        => AgentCommandDispatchFailure
            .Map(new InvalidOperationException("stream agent-hexa@7 is corrupt"))
            .ShouldBe(AgentOperationErrorCode.Unavailable);

    private EventStoreAgentCommandDispatcher Dispatcher() => new(_gateway);

    private void CaptureSubmit()
        => _gateway
            .SubmitCommandAsync(Arg.Do<SubmitCommandRequest>(request => _submitted = request), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("corr-1", null, "msg-1"));

    private static EventStoreGatewayException Gateway(int statusCode)
        => new(statusCode, "gateway failure", detail: "stream agent-hexa@revision 7");

    private static CommandEnvelope Envelope(byte[]? payload = null)
        => new(
            "msg-1",
            "acme",
            "agent",
            "hexa",
            "DisableAgent",
            payload ?? JsonSerializer.SerializeToUtf8Bytes(new { agentId = "hexa" }),
            "corr-1",
            CausationId: null,
            "admin-user",
            new Dictionary<string, string>(StringComparer.Ordinal) { ["actor:agentsAdmin"] = "true" });
}
