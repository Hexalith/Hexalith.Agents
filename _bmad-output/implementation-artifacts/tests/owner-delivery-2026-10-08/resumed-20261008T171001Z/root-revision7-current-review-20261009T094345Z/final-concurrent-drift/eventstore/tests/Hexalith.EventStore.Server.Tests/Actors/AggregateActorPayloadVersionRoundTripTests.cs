using System.Text.Json;
using System.Text.Json.Nodes;

using Dapr.Actors.Runtime;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Events;
using Hexalith.EventStore.Client.Handlers;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.DomainServices;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using static Hexalith.EventStore.Server.Tests.Actors.AggregateActorTestHelper;

using EventEnvelope = Hexalith.EventStore.Server.Events.EventEnvelope;

namespace Hexalith.EventStore.Server.Tests.Actors;

public sealed class AggregateActorPayloadVersionRoundTripTests {
    [EventPayloadVersion(2)]
    private sealed record VersionedTestEvent(int Count) : IEventPayload;

    private sealed record EmitCurrentCountCommand;

    private sealed class CounterState {
        public int Count { get; private set; }

        public void Apply(VersionedTestEvent item) => Count += item.Count;
    }

    private sealed class CounterAggregate : EventStoreAggregate<CounterState> {
        public static DomainResult Handle(EmitCurrentCountCommand command, CounterState? state)
            => DomainResult.Success([new VersionedTestEvent(state?.Count ?? 0)]);
    }

    private sealed class LegacyCounterUpcaster : IEventPayloadUpcaster {
        public string EventTypeName => "LegacyCounterEvent";

        public int FromVersion => 1;

        public string TargetEventTypeName => typeof(VersionedTestEvent).FullName!;

        public JsonObject Upcast(JsonObject payload) {
            payload["Count"] = 1;
            return payload;
        }
    }

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
            Arg.Any<AggregateIdentity>(),
            Arg.Is<IReadOnlyList<EventEnvelope>>(events => events.Count == 1 && events[0].PayloadVersion == 2),
            Arg.Any<string>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());

        EventEnvelope written = context.StateManager.ReceivedCalls()
            .SelectMany(call => call.GetArguments().OfType<EventEnvelope>())
            .Single(item => item.SequenceNumber == 2);
        EventEnvelope writtenJsonCopy = JsonSerializer.Deserialize<EventEnvelope>(JsonSerializer.SerializeToUtf8Bytes(written))!;
        writtenJsonCopy.PayloadVersion.ShouldBe(2);
        IActorStateManager rereadState = Substitute.For<IActorStateManager>();
        var identity = new AggregateIdentity("test-tenant", "test-domain", "agg-001");
        _ = rereadState.TryGetStateAsync<AggregateMetadata>(identity.MetadataKey, Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<AggregateMetadata>(true,
                new AggregateMetadata(2, DateTimeOffset.UtcNow, null)));
        _ = rereadState.TryGetStateAsync<EventEnvelope>($"{identity.EventStreamKeyPrefix}1", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<EventEnvelope>(true, stored));
        _ = rereadState.TryGetStateAsync<EventEnvelope>($"{identity.EventStreamKeyPrefix}2", Arg.Any<CancellationToken>())
            .Returns(new ConditionalValue<EventEnvelope>(true, writtenJsonCopy));
        var reader = new EventStreamReader(rereadState, Substitute.For<ILogger<EventStreamReader>>());
        RehydrationResult reread = (await reader.RehydrateAsync(identity))!;
        reread.Events.Count.ShouldBe(2);

        var services = new ServiceCollection();
        _ = services.AddKeyedSingleton<IAsyncDomainProcessor>("test-domain", new CounterAggregate());
        _ = services.AddSingleton(new EventPayloadEvolutionRegistry(
            [typeof(VersionedTestEvent)], [new LegacyCounterUpcaster()]));
        using ServiceProvider provider = services.BuildServiceProvider();
        CommandEnvelope followup = CreateTestEnvelope() with {
            CommandType = nameof(EmitCurrentCountCommand), Payload = "{}"u8.ToArray(),
        };
        var currentState = new DomainServiceCurrentState(null,
            [.. reread.Events.Select(AggregateActor.ToContractEventEnvelope)], 0, 2);
        DomainServiceWireResult routed = await DomainServiceRequestRouter.ProcessAsync(provider,
            new DomainServiceRequest(followup, currentState));
        routed.Events.ShouldHaveSingleItem().PayloadVersion.ShouldBe(2);
        JsonDocument.Parse(routed.Events[0].Payload).RootElement.GetProperty("Count").GetInt32().ShouldBe(3);
        reread.Events[0].Payload.ShouldBe(originalPayload);
    }
}
