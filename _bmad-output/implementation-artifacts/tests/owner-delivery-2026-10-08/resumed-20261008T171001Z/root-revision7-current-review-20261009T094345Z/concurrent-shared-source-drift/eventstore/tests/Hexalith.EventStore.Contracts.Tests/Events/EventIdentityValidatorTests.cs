using Hexalith.EventStore.Contracts.Events;

using Shouldly;

namespace Hexalith.EventStore.Contracts.Tests.Events;

/// <summary>Verifies write and read identity admission including historical GUID message IDs.</summary>
public sealed class EventIdentityValidatorTests {
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void ValidWriteIdentityIsAdmitted() {
        EventIdentityValidator.ValidateWrite("tenant-1", "domain-1", "aggregate_1", "Aggregate",
            "Event", MessageId, "correlation-1", "causation-1", 1);
    }

    [Theory]
    [InlineData("TenantId")]
    [InlineData("Domain")]
    [InlineData("AggregateId")]
    [InlineData("AggregateType")]
    [InlineData("EventTypeName")]
    [InlineData("MessageId")]
    [InlineData("CorrelationId")]
    [InlineData("CausationId")]
    [InlineData("SequenceNumber")]
    public void MalformedWriteNamesComponent(string component) {
        EventIdentityException error = Should.Throw<EventIdentityException>(() =>
            EventIdentityValidator.ValidateWrite(
                component == "TenantId" ? "bad:tenant" : "tenant-1",
                component == "Domain" ? "bad:domain" : "domain-1",
                component == "AggregateId" ? "bad:aggregate" : "aggregate_1",
                component == "AggregateType" ? " " : "Aggregate",
                component == "EventTypeName" ? " " : "Event",
                component == "MessageId" ? "not-a-ulid" : MessageId,
                component == "CorrelationId" ? "bad/id" : "correlation-1",
                component == "CausationId" ? "bad/id" : "causation-1",
                component == "SequenceNumber" ? 0 : 1));

        error.Component.ShouldBe(component);
        error.Message.ShouldNotContain("bad:");
    }

    [Fact]
    public void ReadAcceptsLegacyGuidMessageIdOnly() {
        const string guidMessageId = "671e4523-0000-4000-8000-123456789abc";

        EventIdentityValidator.ValidateRead("tenant-1", "domain-1", "aggregate_1", "Aggregate",
            "Event", guidMessageId, "correlation-1", "causation-1", 1);
        Should.Throw<EventIdentityException>(() =>
            EventIdentityValidator.ValidateWrite("tenant-1", "domain-1", "aggregate_1", "Aggregate",
                "Event", guidMessageId, "correlation-1", "causation-1", 1)).Component.ShouldBe("MessageId");
        Should.Throw<EventIdentityException>(() =>
            EventIdentityValidator.ValidateRead("tenant-1", "domain-1", "aggregate_1", "Aggregate",
                "Event", guidMessageId, "bad/id", "causation-1", 1)).Component.ShouldBe("CorrelationId");
    }

    [Fact]
    public void SubscriptionAllowsAbsentOptionalMetadataAndChecksItWhenPresent() {
        EventIdentityValidator.ValidateSubscription("tenant-1", null, "aggregate_1", null,
            "Event", MessageId, "correlation-1", null, 1);

        Should.Throw<EventIdentityException>(() =>
            EventIdentityValidator.ValidateSubscription("tenant-1", "bad:domain", "aggregate_1", null,
                "Event", MessageId, "correlation-1", null, 1)).Component.ShouldBe("Domain");
        Should.Throw<EventIdentityException>(() =>
            EventIdentityValidator.ValidateSubscription("tenant-1", null, "aggregate_1", null,
                "Event", MessageId, "correlation-1", "bad/id", 1)).Component.ShouldBe("CausationId");
    }
}
