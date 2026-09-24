using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Contracts.Commands;

using Shouldly;

namespace Hexalith.Agents.Tests;

/// <summary>Platform enablement and tenant decision aggregate behavior.</summary>
public sealed class TenantProviderEnablementAggregateTests
{
    private static readonly DateTimeOffset _now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Separator_inside_identifiers_cannot_alias_two_tenant_entries()
    {
        ProviderCatalogState.EntryKey("a\u001fb", "c").ShouldNotBe(ProviderCatalogState.EntryKey("a", "b\u001fc"));
        var state = new TenantProviderEnablementState();
        state.Apply(new TenantProviderModelEnablementSet("tenant-a", "a\u001fb", "c", true, 1, "operator", null));
        state.Apply(new TenantProviderModelEnablementSet("tenant-a", "a", "b\u001fc", false, 2, "operator", null));
        state.Entries.Count.ShouldBe(2);
        state.Entries[ProviderCatalogState.EntryKey("a\u001fb", "c")].Enabled.ShouldBeTrue();
        state.Entries[ProviderCatalogState.EntryKey("a", "b\u001fc")].Enabled.ShouldBeFalse();
    }

    [Fact]
    public void Platform_enablement_changes_only_the_target_tenant_and_replays()
    {
        var terms = ProviderCatalogTestData.ValidTerms();
        var command = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", true, 0, terms);
        var state = new TenantProviderEnablementState();
        var accepted = TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true));
        accepted.IsSuccess.ShouldBeTrue();
        state.Apply(accepted.Events[0].ShouldBeOfType<TenantProviderModelEnablementSet>());
        state.TenantId.ShouldBe("tenant-a");
        state.Revision.ShouldBe(1);
        state.Entries.Count.ShouldBe(1);
        TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>().Reason.ShouldBe("StaleRevision");
        TenantProviderEnablementAggregate.Handle(command with { ExpectedRevision = 1 }, state,
            Envelope(command, "tenant-a", platform: true)).IsNoOp.ShouldBeTrue();
        TenantProviderEnablementAggregate.Handle(command with { TenantId = "tenant-b" }, state,
            Envelope(command, "tenant-a", platform: true)).Events[0].ShouldBeOfType<TenantProviderGovernanceRejected>();
        state.Revision.ShouldBe(1);
    }

    [Fact]
    public void Migrated_enablement_with_historical_terms_version_is_repeatable()
    {
        var terms = ProviderCatalogTestData.ValidTerms() with { DataHandlingVersion = 4 };
        var command = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", true, 0, terms,
            "legacy:tenant-a");
        var state = new TenantProviderEnablementState();

        var first = TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true));

        first.IsSuccess.ShouldBeTrue();
        state.Apply(first.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderModelEnablementSet>());
        TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>().Reason.ShouldBe("StaleRevision");
        TenantProviderEnablementAggregate.Handle(command with { ExpectedRevision = 1 }, state,
            Envelope(command, "tenant-a", platform: true)).IsNoOp.ShouldBeTrue();
        state.Revision.ShouldBe(1);
    }

    [Fact]
    public void First_enablement_accepts_a_structurally_valid_current_platform_terms_version_above_one()
    {
        var terms = ProviderCatalogTestData.ValidTerms() with { DataHandlingVersion = 2 };
        var command = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", true, 0, terms);
        var state = new TenantProviderEnablementState();

        var result = TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true));

        result.IsSuccess.ShouldBeTrue();
        state.Apply(result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderModelEnablementSet>());
        state.Revision.ShouldBe(1);
        state.Entries.ShouldHaveSingleItem().Value.Enabled.ShouldBeTrue();
        TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>().Reason.ShouldBe("StaleRevision");
        TenantProviderEnablementAggregate.Handle(command with { ExpectedRevision = 1 }, state,
            Envelope(command, "tenant-a", platform: true)).IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void Tenant_decision_requires_current_revision_and_exact_duplicate()
    {
        var state = Enabled();
        var terms = ProviderCatalogTestData.ValidTerms();
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "Approved for this tenant", 1, terms, _now);
        var accepted = TenantProviderEnablementAggregate.Handle(decision, state, Envelope(decision, "tenant-a", tenantAdmin: true));
        accepted.IsSuccess.ShouldBeTrue();
        state.Apply(accepted.Events[0].ShouldBeOfType<ProviderDataHandlingDecided>());
        state.Entries.Single().Value.AcceptedTerms!.DataHandlingVersion.ShouldBe(1);
        TenantProviderEnablementAggregate.Handle(decision, state, Envelope(decision, "tenant-a", tenantAdmin: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>().Reason.ShouldBe("StaleRevision");
        TenantProviderEnablementAggregate.Handle(decision with { ExpectedRevision = 2 }, state,
            Envelope(decision, "tenant-a", tenantAdmin: true)).IsNoOp.ShouldBeTrue();
        TenantProviderEnablementAggregate.Handle(decision with { ExpectedRevision = 2, Justification = "different" }, state,
            Envelope(decision, "tenant-a", tenantAdmin: true)).Events[0].ShouldBeOfType<TenantProviderGovernanceRejected>()
            .Reason.ShouldBe("DivergentDuplicate");
        TenantProviderEnablementAggregate.Handle(decision with { DataHandlingVersion = 2 }, state,
            Envelope(decision, "tenant-a", tenantAdmin: true)).Events[0].ShouldBeOfType<TenantProviderGovernanceRejected>();
        state.Revision.ShouldBe(2);
    }

    [Fact]
    public void Tenant_admin_cannot_change_platform_enablement_and_cross_tenant_decision_rejects()
    {
        var state = Enabled();
        var terms = ProviderCatalogTestData.ValidTerms();
        var enable = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", false, 1, terms);
        TenantProviderEnablementAggregate.Handle(enable, state, Envelope(enable, "tenant-a", tenantAdmin: true))
            .Events[0].ShouldBeOfType<TenantProviderGovernanceRejected>().Reason.ShouldBe("NotAuthorized");
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "yes", 1, terms, _now);
        TenantProviderEnablementAggregate.Handle(decision, state, Envelope(decision, "tenant-b", tenantAdmin: true))
            .Events[0].ShouldBeOfType<TenantProviderGovernanceRejected>();
        state.Revision.ShouldBe(1);
    }

    private static TenantProviderEnablementState Enabled()
    {
        var state = new TenantProviderEnablementState();
        state.Apply(new TenantProviderModelEnablementSet("tenant-a", "provider", "model", true, 1, "operator", null));
        return state;
    }

    private static CommandEnvelope Envelope<T>(T command, string tenantId, bool platform = false, bool tenantAdmin = false)
        where T : notnull
        => new("01J81NJ6NFCAZBZY92YBRKFR0X", tenantId, TenantProviderEnablementAggregate.Domain, tenantId,
            typeof(T).Name, JsonSerializer.SerializeToUtf8Bytes(command), "01J81NJ6NFCAZBZY92YBRKFR0Y", null,
            "actor", new Dictionary<string, string>
            {
                [TenantProviderEnablementAggregate.PlatformOperatorExtensionKey] = platform ? "true" : "false",
                [TenantProviderEnablementAggregate.TenantAdministratorExtensionKey] = tenantAdmin ? "true" : "false",
            });
}
