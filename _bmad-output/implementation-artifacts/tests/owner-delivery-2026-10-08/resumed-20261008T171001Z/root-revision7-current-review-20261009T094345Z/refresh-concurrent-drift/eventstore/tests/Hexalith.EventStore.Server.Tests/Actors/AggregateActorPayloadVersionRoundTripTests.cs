using System.Text.Json;

using Dapr.Actors.Runtime;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.Server.Events;

using NSubstitute;

using Shouldly;

using static Hexalith.EventStore.Server.Tests.Actors.AggregateActorTestHelper;

using EventEnvelope = Hexalith.EventStore.Server.Events.EventEnvelope;

namespace Hexalith.EventStore.Server.Tests.Actors;

public sealed class AggregateActorPayloadVersionRoundTripTests {
    [EventPayloadVersion(2)]
    private sealed record VersionedTestEvent(int Count) : IEventPayload;

    [Fact]
    public async Task JsonStateReadAndActorWritePreserveV1BytesAndPublishV2Version() {
        ActorTestContext context = CreateActor();
        ConfigureNoDuplicate(context.StateManager);
        byte[] originalPayload = [123, 125];
        var v1 = new EventEnvelope(
            Guid.NewGuid().ToString(), "agg-001", "test-domain", "test-tenant", "test-domain",
            1, 0, DateTimeOffset.UtcNow, "correlation-1", "causation-1", "system", "v1",
            "LegacyCounterEvent", 1, "json", originalPayload, null);
        EventEnvelope stored = JsonSerializer.Deserialize<EventEnvelope>(JsonSerializer.SerializeToUtf8Bytes(v1))!;
        stored.Payload.ShouldBe(originalPayload);
        stored.PayloadVersion.ShouldBeNull();

        _ = context.StateManager.TryGetStateAsync<AggregateMetadata>(
            "test-tenant:test-domain:agg-001:metadata", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<AggregateMetadata>(true,
                new AggregateMetadata(1, DateTimeOffset.UtcNow, null)));
        _ = context.StateManager.TryGetStateAsync<EventEnvelope>(
            "test-tenant:test-domain:agg-001:events:1", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<EventEnvelope>(true, stored));

        object? routedState = null;
        _ = context.Invoker.InvokeAsync(Arg.Any<CommandEnvelope>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                routedState = call.ArgAt<object?>(1);
                return DomainResult.Success([new VersionedTestEvent(2)]);
            });

        CommandProcessingResult result = await context.Actor.ProcessCommandAsync(CreateTestEnvelope());

        result.Accepted.ShouldBeTrue(result + " " + string.Join(" | ",
            context.Logger.ReceivedCalls().SelectMany(call => call.GetArguments().OfType<Exception>())
                .Select(exception => exception.ToString())));
        var history = routedState.ShouldBeOfType<DomainServiceCurrentState>();
        history.Events.ShouldHaveSingleItem().Payload.ShouldBe(originalPayload);
        history.Events[0].Metadata.PayloadVersion.ShouldBeNull();
        await context.StateManager.Received(1).SetStateAsync(
            "test-tenant:test-domain:agg-001:events:2",
            Arg.Is<EventEnvelope>(staged => staged.PayloadVersion == 2 && staged.MetadataVersion == 1 && staged.EventContractType == null),
            Arg.Any<CancellationToken>());
        stored.Payload.ShouldBe(originalPayload);
        await context.EventPublisher.Received(1).PublishEventsAsync(
            Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(),
            Arg.Is<IReadOnlyList<EventEnvelope>>(events => events.Count == 1 && events[0].PayloadVersion == 2),
            Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
    }
}
