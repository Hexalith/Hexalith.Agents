using ByteAether.Ulid;

using Hexalith.EventStore.Contracts.Identity;

namespace Hexalith.EventStore.Contracts.Events;

/// <summary>Validates the identity carried by written and stored domain events.</summary>
public static class EventIdentityValidator {
    /// <summary>Validates an event before actor state is staged.</summary>
    public static void ValidateWrite(string? tenantId, string? domain, string? aggregateId,
        string? aggregateType, string? eventTypeName, string? messageId, string? correlationId,
        string? causationId, long sequenceNumber)
        => Validate(tenantId, domain, aggregateId, aggregateType, eventTypeName, messageId,
            correlationId, causationId, sequenceNumber, allowLegacyGuid: false,
            optionalSubscriptionMetadata: false);

    /// <summary>Validates a stored event before it reaches a domain reader.</summary>
    public static void ValidateRead(string? tenantId, string? domain, string? aggregateId,
        string? aggregateType, string? eventTypeName, string? messageId, string? correlationId,
        string? causationId, long sequenceNumber)
        => Validate(tenantId, domain, aggregateId, aggregateType, eventTypeName, messageId,
            correlationId, causationId, sequenceNumber, allowLegacyGuid: true,
            optionalSubscriptionMetadata: false);

    /// <summary>Validates a subscriber envelope; fields absent from older publications stay optional.</summary>
    public static void ValidateSubscription(string? tenantId, string? domain, string? aggregateId,
        string? aggregateType, string? eventTypeName, string? messageId, string? correlationId,
        string? causationId, long sequenceNumber)
        => Validate(tenantId, domain, aggregateId, aggregateType, eventTypeName, messageId,
            correlationId, causationId, sequenceNumber, allowLegacyGuid: true,
            optionalSubscriptionMetadata: true);

    private static void Validate(string? tenantId, string? domain, string? aggregateId,
        string? aggregateType, string? eventTypeName, string? messageId, string? correlationId,
        string? causationId, long sequenceNumber, bool allowLegacyGuid, bool optionalSubscriptionMetadata) {
        ValidateAggregateComponent(tenantId, "TenantId", value => new AggregateIdentity(value, "valid", "valid"));
        if (domain is not null || !optionalSubscriptionMetadata) {
            ValidateAggregateComponent(domain, "Domain", value => new AggregateIdentity("valid", value, "valid"));
        }

        ValidateAggregateComponent(aggregateId, "AggregateId", value => new AggregateIdentity("valid", "valid", value));
        if (aggregateType is not null || !optionalSubscriptionMetadata) {
            RequireNonBlank(aggregateType, "AggregateType");
        }

        RequireNonBlank(eventTypeName, "EventTypeName");
        if (!IsUlid(messageId) && !(allowLegacyGuid && IsLegacyGuid(messageId))) {
            throw new EventIdentityException("MessageId");
        }

        RequireDiagnosticId(correlationId, "CorrelationId");
        if (causationId is not null || !optionalSubscriptionMetadata) {
            RequireDiagnosticId(causationId, "CausationId");
        }

        if (sequenceNumber < 1) {
            throw new EventIdentityException("SequenceNumber");
        }
    }

    private static void ValidateAggregateComponent(string? value, string component, Func<string, AggregateIdentity> create) {
        if (value is null) {
            throw new EventIdentityException(component);
        }

        try {
            _ = create(value);
        }
        catch (ArgumentException) {
            throw new EventIdentityException(component);
        }
    }

    private static void RequireNonBlank(string? value, string component) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new EventIdentityException(component);
        }
    }

    private static void RequireDiagnosticId(string? value, string component) {
        if (value is null || value.Length is < 1 or > 128
            || !value.All(static character => character is >= 'A' and <= 'Z'
                or >= 'a' and <= 'z' or >= '0' and <= '9' or '-')) {
            throw new EventIdentityException(component);
        }
    }

    private static bool IsUlid(string? value)
        => value is not null && Ulid.TryParse(value, null, out _);

    private static bool IsLegacyGuid(string? value) {
        if (value is null || value.Length != 36) {
            return false;
        }

        for (int index = 0; index < value.Length; index++) {
            char character = value[index];
            if (index is 8 or 13 or 18 or 23) {
                if (character != '-') { return false; }
            }
            else if (!Uri.IsHexDigit(character)) {
                return false;
            }
        }

        return true;
    }
}
