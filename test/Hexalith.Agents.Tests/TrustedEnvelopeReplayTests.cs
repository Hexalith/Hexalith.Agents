using System.Text.Json;
using Hexalith.Agents.Contracts.TrustedEnvelopeReplay;
using Hexalith.Agents.TrustedEnvelopeReplay;
using Hexalith.EventStore.Contracts.Commands;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Pure local replay folds and serialized end-state; no private host or live seam is exercised.</summary>
public sealed class TrustedEnvelopeReplayTests
{
    /// <summary>Verifies that exact later replay retains original first seen and retention after snapshot.</summary>
    [Fact]
    public void ExactLaterReplayRetainsOriginalFirstSeenAndRetentionAfterSnapshot()
    {
        var command = Original();
        var state = new TrustedEnvelopeReplayState();
        state.Apply(TrustedEnvelopeReplayAggregate.Handle(command, null, Envelope(command)).Events.Single().ShouldBeOfType<TrustedEnvelopeFirstSeen>());
        var restored = JsonSerializer.Deserialize<TrustedEnvelopeReplayState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        var later = command with { FirstSeenAt = command.FirstSeenAt.AddHours(1), RetainUntil = command.RetainUntil.AddHours(1) };
        TrustedEnvelopeReplayAggregate.Handle(later, restored, Envelope(command)).IsNoOp.ShouldBeTrue();
        restored.Registration.ShouldBe(command);
    }

    /// <summary>Verifies that reused nonce with any changed authenticated field rejects and preserves state.</summary>
    [Theory]
    [InlineData("tenant-b", "logical-a", "key-a", "b")]
    [InlineData("tenant-a", "logical-b", "key-a", "a")]
    [InlineData("tenant-a", "logical-a", "key-b", "a")]
    [InlineData("tenant-a", "logical-a", "key-a", "b")]
    public void ReusedNonceWithAnyChangedAuthenticatedFieldRejectsAndPreservesState(string tenant, string logical, string key, string digest)
    {
        ArgumentNullException.ThrowIfNull(digest);
        var original = Original();
        var state = new TrustedEnvelopeReplayState();
        state.Apply(new TrustedEnvelopeFirstSeen(original));
        var changed = original with { TargetTenantId = tenant, LogicalCommandId = logical, SigningKeyVersion = key, CanonicalAndTagDigest = new string(digest[0], 64) };
        TrustedEnvelopeReplayAggregate.Handle(changed, state, Envelope(original)).IsRejection.ShouldBeTrue();
        state.Registration.ShouldBe(original);
    }

    /// <summary>Verifies that replay is issuer wide and cannot be registered in atarget tenant.</summary>
    [Fact]
    public void ReplayIsIssuerWideAndCannotBeRegisteredInATargetTenant()
    {
        var command = Original();
        TrustedEnvelopeReplayAggregate.Handle(command, null, Envelope(command) with { TenantId = "tenant-a" }).IsRejection.ShouldBeTrue();
        TrustedEnvelopeReplayIdentity.Create("issuer-a", "nonce-a").ShouldNotBe(TrustedEnvelopeReplayIdentity.Create("issuer-b", "nonce-a"));
    }

    private static RegisterTrustedEnvelopeFirstSeen Original()
    {
        var first = new DateTimeOffset(2026, 10, 14, 10, 0, 0, TimeSpan.Zero);
        return new("issuer-a", "nonce-a", "tenant-a", "logical-a", "key-a", new string('a', 64), first, first.AddSeconds(604800));
    }

    private static CommandEnvelope Envelope(RegisterTrustedEnvelopeFirstSeen command)
        => new("receipt", "system", TrustedEnvelopeReplayIdentity.Domain,
            TrustedEnvelopeReplayIdentity.Create(command.Issuer, command.DeliveryNonce), nameof(RegisterTrustedEnvelopeFirstSeen),
            [], "correlation", null, "private-registrar", null);
}
