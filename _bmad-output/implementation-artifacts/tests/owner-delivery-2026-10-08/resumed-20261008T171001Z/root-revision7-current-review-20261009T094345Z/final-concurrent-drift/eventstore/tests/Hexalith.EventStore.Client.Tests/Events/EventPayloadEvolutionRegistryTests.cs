using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.EventStore.Client.Events;

using NSubstitute;
using Shouldly;

namespace Hexalith.EventStore.Client.Tests.Events;

/// <summary>Exercises complete chains and fail-closed reader outcomes.</summary>
public sealed class EventPayloadEvolutionRegistryTests {
    private const string CurrentName = "Hexalith.EventStore.Client.Tests.Events.VersionedEvolutionEvent";

    [Fact]
    public void MultiStepChainRunsOnceInOrderAndKeepsStoredBytes() {
        int calls = 0;
        byte[] stored = "{\"legacy\":\"Ada\"}"u8.ToArray();
        var registry = new EventPayloadEvolutionRegistry([typeof(VersionedEvolutionEvent)], [
            Step(CurrentName, 1, json => {
                calls++;
                json["middle"] = json["legacy"]?.GetValue<string>();
                return json;
            }),
            Step(CurrentName, 2, json => {
                calls++;
                json["Name"] = json["middle"]?.GetValue<string>();
                return json;
            }),
        ]);

        EventPayloadReadResult result = registry.Read(CurrentName, stored, null, 7);

        result.EventType.ShouldBe(typeof(VersionedEvolutionEvent));
        JsonSerializer.Deserialize<VersionedEvolutionEvent>(result.Payload)!.Name.ShouldBe("Ada");
        calls.ShouldBe(2);
        stored.ShouldBe("{\"legacy\":\"Ada\"}"u8.ToArray());
    }

    [Fact]
    public void RenameResolvesTargetType() {
        var registry = new EventPayloadEvolutionRegistry([typeof(RenamedEvolutionEvent)], [
            Step("OldEvolutionEvent", 1, json => {
                json["Name"] = json["legacy"]?.GetValue<string>();
                return json;
            }, typeof(RenamedEvolutionEvent).FullName),
        ]);

        EventPayloadReadResult result = registry.Read("OldEvolutionEvent", "{\"legacy\":\"Ada\"}"u8.ToArray(), null, 1);

        result.EventType.ShouldBe(typeof(RenamedEvolutionEvent));
        result.EventTypeName.ShouldBe(typeof(RenamedEvolutionEvent).FullName);
    }

    [Fact]
    public void CurrentPayloadIsReturnedAsTheOriginalArray() {
        var registry = new EventPayloadEvolutionRegistry([typeof(VersionedEvolutionEvent)], [
            Step(CurrentName, 1, static json => json),
            Step(CurrentName, 2, static json => json),
        ]);
        byte[] source = "{\"Name\":\"Ada\"}"u8.ToArray();

        EventPayloadReadResult result = registry.Read(CurrentName, source, 3, 2);

        ReferenceEquals(result.Payload, source).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(1025)]
    public void InvalidOrFutureStoredVersionFailsWithSafeContext(int version) {
        var registry = new EventPayloadEvolutionRegistry([typeof(VersionedEvolutionEvent)], [], validateChains: false);

        EventPayloadEvolutionException error = Should.Throw<EventPayloadEvolutionException>(() =>
            registry.Read(CurrentName, "{}"u8.ToArray(), version, 11));

        error.EventTypeName.ShouldBe(CurrentName);
        error.StoredVersion.ShouldBe(version);
        error.SequenceNumber.ShouldBe(11);
        error.InnerException.ShouldBeNull();
    }

    [Fact]
    public void MissingChainFailsBeforeDeserialization() {
        var registry = new EventPayloadEvolutionRegistry([typeof(VersionedEvolutionEvent)], [], validateChains: false);

        _ = Should.Throw<EventPayloadEvolutionException>(() => registry.Read(CurrentName, "{}"u8.ToArray(), 1, 1));
    }

    [Fact]
    public void ThrowingUpcasterDoesNotChainOrExposeItsMessage() {
        var registry = new EventPayloadEvolutionRegistry([typeof(RenamedEvolutionEvent)], [
            Step("OldEvolutionEvent", 1,
                static _ => throw new InvalidOperationException("secret from payload"), typeof(RenamedEvolutionEvent).FullName),
        ]);

        EventPayloadEvolutionException error = Should.Throw<EventPayloadEvolutionException>(() =>
            registry.Read("OldEvolutionEvent", "{}"u8.ToArray(), 1, 5));

        error.InnerExceptionTypeName.ShouldBe(nameof(InvalidOperationException));
        error.InnerException.ShouldBeNull();
        error.Message.ShouldNotContain("secret from payload");
    }

    [Fact]
    public void DuplicateStepFailsStartup() {
        InvalidOperationException error = Should.Throw<InvalidOperationException>(() =>
            new EventPayloadEvolutionRegistry([typeof(VersionedEvolutionEvent)], [
                Step(CurrentName, 1, static json => json),
                Step(CurrentName, 1, static json => json),
            ]));

        error.Message.ShouldContain(CurrentName);
        error.Message.ShouldContain("1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1024)]
    public void InvalidStepVersionFailsStartup(int version) {
        InvalidOperationException error = Should.Throw<InvalidOperationException>(() =>
            new EventPayloadEvolutionRegistry([typeof(VersionedEvolutionEvent)], [
                Step(CurrentName, version, static json => json),
            ]));

        error.Message.ShouldContain(version.ToString());
    }

    [Fact]
    public void IncompleteAndDanglingChainsFailStartup() {
        _ = Should.Throw<InvalidOperationException>(() =>
            new EventPayloadEvolutionRegistry([typeof(VersionedEvolutionEvent)], [
                Step(CurrentName, 1, static json => json),
            ]));
        _ = Should.Throw<InvalidOperationException>(() =>
            new EventPayloadEvolutionRegistry([typeof(RenamedEvolutionEvent)], [
                Step("OldEvolutionEvent", 1, static json => json, "missing-event"),
            ]));
    }

    [Fact]
    public void InvalidJsonAndNullResultFailWithTypedSafeContext() {
        var invalidJson = new EventPayloadEvolutionRegistry([typeof(RenamedEvolutionEvent)], [
            Step("OldEvolutionEvent", 1, static json => json, typeof(RenamedEvolutionEvent).FullName),
        ]);
        EventPayloadEvolutionException malformed = Should.Throw<EventPayloadEvolutionException>(() =>
            invalidJson.Read("OldEvolutionEvent", "{"u8.ToArray(), null, 9));
        malformed.EventTypeName.ShouldBe("OldEvolutionEvent");
        malformed.SequenceNumber.ShouldBe(9);
        malformed.InnerException.ShouldBeNull();

        var nullResult = new EventPayloadEvolutionRegistry([typeof(RenamedEvolutionEvent)], [
            Step("OldEvolutionEvent", 1, static _ => null!, typeof(RenamedEvolutionEvent).FullName),
        ]);
        EventPayloadEvolutionException failure = Should.Throw<EventPayloadEvolutionException>(() =>
            nullResult.Read("OldEvolutionEvent", "{}"u8.ToArray(), null, 10));
        failure.InnerExceptionTypeName.ShouldBe(nameof(InvalidOperationException));
        failure.InnerException.ShouldBeNull();
    }

    [Fact]
    public void UnknownStoredNameDoesNotRunKnownChain() {
        int calls = 0;
        var registry = new EventPayloadEvolutionRegistry([typeof(RenamedEvolutionEvent)], [
            Step("OldEvolutionEvent", 1, json => { calls++; return json; }, typeof(RenamedEvolutionEvent).FullName),
        ]);
        byte[] source = "{}"u8.ToArray();

        EventPayloadReadResult result = registry.Read("OtherEvolutionEvent", source, 8, 1);

        result.EventType.ShouldBeNull();
        ReferenceEquals(result.Payload, source).ShouldBeTrue();
        calls.ShouldBe(0);
    }

    private static IEventPayloadUpcaster Step(string name, int version,
        Func<JsonObject, JsonObject> convert, string? target = null) {
        IEventPayloadUpcaster step = Substitute.For<IEventPayloadUpcaster>();
        step.EventTypeName.Returns(name);
        step.FromVersion.Returns(version);
        step.TargetEventTypeName.Returns(target);
        step.Upcast(Arg.Any<JsonObject>()).Returns(call => convert(call.Arg<JsonObject>()));
        return step;
    }
}
