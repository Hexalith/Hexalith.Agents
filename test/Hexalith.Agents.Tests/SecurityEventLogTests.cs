using System.Text.Json;
using Hexalith.Agents.Contracts.SecurityEventLog;
using Hexalith.Agents.SecurityEventLog;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;
using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Pure original daily recording and serialized deduplication; supplies no replicated spool or ACL evidence.</summary>
public sealed class SecurityEventLogTests
{
    /// <summary>Verifies that lost acknowledgement retry after snapshot produces one original daily event.</summary>
    [Fact]
    public void LostAcknowledgementRetryAfterSnapshotProducesOneOriginalDailyEvent()
    {
        var command = Original();
        var state = new SecurityEventLogState();
        state.Apply(SecurityEventLogAggregate.Handle(command, null, Envelope(command)).Events.Single().ShouldBeOfType<SecurityEventRecorded>());
        var restored = JsonSerializer.Deserialize<SecurityEventLogState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        SecurityEventLogAggregate.Handle(command, restored, Envelope(command)).IsNoOp.ShouldBeTrue();
        restored.Observations.Count.ShouldBe(1);
        restored.Observations[command.SecurityObservationId].ShouldBe(command);
    }

    /// <summary>Verifies that changed observation never overwrites original.</summary>
    [Fact]
    public void ChangedObservationNeverOverwritesOriginal()
    {
        var command = Original();
        var state = new SecurityEventLogState();
        state.Apply(new SecurityEventRecorded(command));
        SecurityEventLogAggregate.Handle(command with { ReasonCode = "invalid-tag" }, state, Envelope(command)).IsRejection.ShouldBeTrue();
        state.Observations[command.SecurityObservationId].ShouldBe(command);
    }

    /// <summary>Verifies that recording is restricted to original authenticated route and day.</summary>
    [Theory]
    [InlineData("tenant-b", "2026-10-14")]
    [InlineData("tenant-a", "2026-10-15")]
    public void RecordingIsRestrictedToOriginalAuthenticatedRouteAndDay(string tenant, string day)
    {
        var command = Original();
        var envelope = Envelope(command) with { TenantId = tenant, AggregateId = SecurityEventLogIdentity.Create(tenant, day) };
        SecurityEventLogAggregate.Handle(command, null, envelope).IsRejection.ShouldBeTrue();
    }

    /// <summary>Verifies that only a derived observation digest can identify a recorded observation.</summary>
    [Theory]
    [InlineData("observation", false)]
    [InlineData("claimed:tenant-a/hexa", false)]
    [InlineData("security-observation-claimed:tenant-a", false)]
    [InlineData("security-observation-BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", false)]
    [InlineData("security-observation-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", false)]
    [InlineData("security-observation-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", false)]
    [InlineData("SECURITY-OBSERVATION-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", false)]
    [InlineData("security-observation-bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", true)]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef", true)]
    public void OnlyADerivedObservationDigestIdentifiesARecordedObservation(string observationId, bool accepted)
    {
        var command = Original() with { SecurityObservationId = observationId };
        DomainResult result = SecurityEventLogAggregate.Handle(command, null, Envelope(command));
        result.IsSuccess.ShouldBe(accepted);
        result.IsRejection.ShouldBe(!accepted);
    }

    private static RecordSecurityEvent Original()
        => new("security-observation-" + new string('b', 64), "tenant-a", "2026-10-14", new(2026, 10, 14, 10, 0, 0, TimeSpan.Zero),
            "scope-mismatch", new string('a', 64), "retained-key");
    private static CommandEnvelope Envelope(RecordSecurityEvent command)
        => new("receipt", command.RoutingTenantId, SecurityEventLogIdentity.Domain,
            SecurityEventLogIdentity.Create(command.RoutingTenantId, command.UtcDay), nameof(RecordSecurityEvent), [],
            "correlation", null, "private-drain", null);
}
