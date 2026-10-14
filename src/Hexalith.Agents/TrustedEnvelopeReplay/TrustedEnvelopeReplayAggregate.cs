using Hexalith.Agents.Contracts.TrustedEnvelopeReplay;
using Hexalith.EventStore.Client.Aggregates;
using Hexalith.EventStore.Client.Attributes;
using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.TrustedEnvelopeReplay;

/// <summary>Pure issuer-wide first-seen admission. Only Platform's private registrar may invoke this domain.</summary>
[EventStoreDomain(TrustedEnvelopeReplayIdentity.Domain)]
public sealed class TrustedEnvelopeReplayAggregate : EventStoreAggregate<TrustedEnvelopeReplayState>
{
    /// <summary>Registers once, reuses original times on exact replay, and rejects any changed authenticated fields.</summary>
    public static DomainResult Handle(RegisterTrustedEnvelopeFirstSeen command, TrustedEnvelopeReplayState? state, CommandEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.TenantId != TrustedEnvelopeReplayIdentity.Tenant || envelope.Domain != TrustedEnvelopeReplayIdentity.Domain
            || string.IsNullOrWhiteSpace(command.Issuer) || string.IsNullOrWhiteSpace(command.DeliveryNonce)
            || envelope.AggregateId != TrustedEnvelopeReplayIdentity.Create(command.Issuer, command.DeliveryNonce)
            || string.IsNullOrWhiteSpace(command.TargetTenantId) || string.IsNullOrWhiteSpace(command.LogicalCommandId)
            || string.IsNullOrWhiteSpace(command.SigningKeyVersion) || !IsDigest(command.CanonicalAndTagDigest)
            || command.FirstSeenAt == default || command.FirstSeenAt.Offset != TimeSpan.Zero || command.RetainUntil.Offset != TimeSpan.Zero
            || command.RetainUntil - command.FirstSeenAt != TimeSpan.FromSeconds(604800))
        {
            return DomainResult.Rejection([new TrustedEnvelopeReplayRejected("InvalidRegistration")]);
        }

        if (state?.Registration is { } original)
        {
            bool exact = command.Issuer == original.Issuer && command.DeliveryNonce == original.DeliveryNonce
                && command.TargetTenantId == original.TargetTenantId && command.LogicalCommandId == original.LogicalCommandId
                && command.SigningKeyVersion == original.SigningKeyVersion && command.CanonicalAndTagDigest == original.CanonicalAndTagDigest;
            return exact ? DomainResult.NoOp() : DomainResult.Rejection([new TrustedEnvelopeReplayRejected("ReplayConflict")]);
        }

        return DomainResult.Success([new TrustedEnvelopeFirstSeen(command)]);
    }

    private static bool IsDigest(string value) => value is { Length: 64 } && value.All(char.IsAsciiHexDigit);
}
