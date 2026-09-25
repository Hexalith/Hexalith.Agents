using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
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
    public void Platform_disable_without_terms_revokes_tenant_visibility()
    {
        TenantProviderEnablementState state = Enabled();
        var command = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", false, 1, CurrentTerms: null);

        var result = TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true));

        result.IsSuccess.ShouldBeTrue();
        TenantProviderModelEnablementSet disabled = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderModelEnablementSet>();
        disabled.Enabled.ShouldBeFalse();
        disabled.Revision.ShouldBe(2);
        state.Apply(disabled);
        state.Revision.ShouldBe(2);
        TenantProviderEntryState entry = state.Entries.ShouldHaveSingleItem().Value;
        entry.Enabled.ShouldBeFalse();
        TenantProviderEligibility.Evaluate(entry, [ProviderCatalogTestData.ValidTerms()], _now)
            .Status.ShouldBe("MissingEnablementOrTerms");
        TenantProviderEnablementAggregate.Handle(command with { ExpectedRevision = 2 }, state,
            Envelope(command, "tenant-a", platform: true)).IsNoOp.ShouldBeTrue();
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
    public void Enablement_retry_without_provenance_is_a_noop_after_migration()
    {
        var state = new TenantProviderEnablementState();
        state.Apply(new TenantProviderModelEnablementSet("tenant-a", "provider", "model", true, 1,
            "operator", "legacy:tenant-a"));
        var command = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", true, 1,
            ProviderCatalogTestData.ValidTerms());

        TenantProviderEnablementAggregate.Handle(command, state, Envelope(command, "tenant-a", platform: true))
            .IsNoOp.ShouldBeTrue();
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
        DecideProviderDataHandling superseded = decision with
        {
            DataHandlingVersion = 0,
            ConfirmedTerms = terms with { DataHandlingVersion = 0 },
            ExpectedRevision = 2,
        };
        TenantProviderEnablementAggregate.Handle(superseded, state,
            Envelope(superseded, "tenant-a", tenantAdmin: true)).Events[0]
            .ShouldBeOfType<TenantProviderGovernanceRejected>().Reason.ShouldBe("StaleRevision");
        state.Revision.ShouldBe(2);
    }

    [Fact]
    public void A_decision_on_a_superseded_terms_version_is_stale()
    {
        TenantProviderEnablementState state = Enabled();
        ProviderDataHandlingRecord first = ProviderCatalogTestData.ValidTerms();
        ProviderDataHandlingRecord second = first with { RetentionDays = 15, DataHandlingVersion = 2 };
        var current = new DecideProviderDataHandling("provider", "model", 2, true, "Approved v2", 1, second, _now);
        state.Apply(TenantProviderEnablementAggregate.Handle(current, state, Envelope(current, "tenant-a", tenantAdmin: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ProviderDataHandlingDecided>());

        var superseded = new DecideProviderDataHandling("provider", "model", 1, true, "Approved v1", 2, first, _now);
        TenantProviderEnablementAggregate.Handle(superseded, state, Envelope(superseded, "tenant-a", tenantAdmin: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>()
            .Reason.ShouldBe("StaleRevision");
        state.Entries.Single().Value.AcceptedTerms!.DataHandlingVersion.ShouldBe(2);
    }

    [Theory]
    [InlineData("enable", "system")]
    [InlineData("enable", "mismatched-aggregate")]
    [InlineData("decide", "system")]
    [InlineData("decide", "mismatched-aggregate")]
    public void Tenant_commands_outside_their_own_tenant_stream_are_not_authorized(string commandKind, string scope)
    {
        string tenantId = scope == "system" ? ProviderCatalogIdentity.PlatformTenantId : "tenant-a";
        TenantProviderEnablementState? state = commandKind == "decide" && scope != "system" ? Enabled() : null;
        var terms = ProviderCatalogTestData.ValidTerms();
        object command = commandKind == "enable"
            ? new SetTenantProviderModelEnablement(tenantId, "provider", "model", true, 0, terms)
            : new DecideProviderDataHandling("provider", "model", 1, true, "Approved", state?.Revision ?? 0, terms, _now);
        CommandEnvelope envelope = Envelope(command, tenantId, platform: commandKind == "enable", tenantAdmin: commandKind == "decide");
        if (scope == "mismatched-aggregate")
        {
            envelope = envelope with { AggregateId = "tenant-b" };
        }

        var result = command is SetTenantProviderModelEnablement enable
            ? TenantProviderEnablementAggregate.Handle(enable, state, envelope)
            : TenantProviderEnablementAggregate.Handle((DecideProviderDataHandling)command, state, envelope);

        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>()
            .Reason.ShouldBe("NotAuthorized");
    }

    [Fact]
    public void Tenant_governance_handlers_report_exact_applied_and_already_applied_effects()
    {
        var terms = ProviderCatalogTestData.ValidTerms();
        var enable = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", true, 0, terms);
        var state = new TenantProviderEnablementState();
        var applied = TenantProviderEnablementAggregate.Handle(enable, state, Envelope(enable, "tenant-a", platform: true));
        Effect(applied).ShouldBe("Applied");
        state.Apply(applied.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderModelEnablementSet>());
        var repeat = enable with { ExpectedRevision = 1 };
        Effect(TenantProviderEnablementAggregate.Handle(repeat, state, Envelope(repeat, "tenant-a", platform: true)))
            .ShouldBe("AlreadyApplied");

        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "Approved", 1, terms, _now);
        var decided = TenantProviderEnablementAggregate.Handle(decision, state,
            Envelope(decision, "tenant-a", tenantAdmin: true));
        Effect(decided).ShouldBe("Applied");
        state.Apply(decided.Events.ShouldHaveSingleItem().ShouldBeOfType<ProviderDataHandlingDecided>());
        var decisionRepeat = decision with { ExpectedRevision = 2 };
        Effect(TenantProviderEnablementAggregate.Handle(decisionRepeat, state,
            Envelope(decisionRepeat, "tenant-a", tenantAdmin: true))).ShouldBe("AlreadyApplied");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("disabled")]
    public void Tenant_decision_requires_an_enabled_entry(string stateKind)
    {
        var state = stateKind == "missing" ? new TenantProviderEnablementState { TenantId = "tenant-a" } : Enabled();
        if (stateKind == "disabled")
        {
            state.Apply(new TenantProviderModelEnablementSet("tenant-a", "provider", "model", false, 2, "operator", null));
        }
        var terms = ProviderCatalogTestData.ValidTerms();
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "Approved",
            state.Revision, terms, _now);

        TenantProviderEnablementAggregate.Handle(decision, state, Envelope(decision, "tenant-a", tenantAdmin: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>()
            .Reason.ShouldBe("EntryNotEnabled");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("version-zero")]
    [InlineData("invalid-reference")]
    public void Tenant_enablement_requires_valid_current_terms(string termsKind)
    {
        ProviderDataHandlingRecord? terms = termsKind switch
        {
            "missing" => null,
            "version-zero" => ProviderCatalogTestData.ValidTerms() with { DataHandlingVersion = 0 },
            _ => ProviderCatalogTestData.ValidTerms() with { TermsReferenceId = "bad reference" },
        };
        var command = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", true, 0, terms);

        TenantProviderEnablementAggregate.Handle(command, null, Envelope(command, "tenant-a", platform: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>()
            .Reason.ShouldBe("InvalidCurrentTerms");
    }

    [Fact]
    public void Tenant_decision_rejects_blank_justification()
    {
        var terms = ProviderCatalogTestData.ValidTerms();
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, " ", 1, terms, _now);
        TenantProviderEnablementAggregate.Handle(decision, Enabled(), Envelope(decision, "tenant-a", tenantAdmin: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<TenantProviderGovernanceRejected>()
            .Reason.ShouldBe("InvalidDecision");
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

    [Fact]
    public void Decline_can_be_reversed_on_the_same_current_version_by_a_new_decision()
    {
        TenantProviderEnablementState state = Enabled();
        var terms = ProviderCatalogTestData.ValidTerms();
        var decline = new DecideProviderDataHandling("provider", "model", 1, false, "Decline", 1, terms, _now);
        ProviderDataHandlingDecided first = TenantProviderEnablementAggregate.Handle(decline, state,
            Envelope(decline, "tenant-a", tenantAdmin: true)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<ProviderDataHandlingDecided>();
        state.Apply(first);
        TenantProviderEligibility.Evaluate(state.Entries.ShouldHaveSingleItem().Value, [terms], _now)
            .Status.ShouldBe("Declined");

        DecideProviderDataHandling acceptance = decline with { Accepted = true, Justification = "Approved", ExpectedRevision = 2 };
        ProviderDataHandlingDecided second = TenantProviderEnablementAggregate.Handle(acceptance, state,
            Envelope(acceptance, "tenant-a", tenantAdmin: true)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<ProviderDataHandlingDecided>();
        state.Apply(second);
        state.Revision.ShouldBe(3);
        TenantProviderEligibility.Evaluate(state.Entries.ShouldHaveSingleItem().Value, [terms], _now)
            .Status.ShouldBe("Current");
    }

    [Fact]
    public void Another_administrator_can_record_a_new_decision_on_the_current_version()
    {
        TenantProviderEnablementState state = Enabled();
        var terms = ProviderCatalogTestData.ValidTerms();
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "Approved", 1, terms, _now);
        state.Apply(TenantProviderEnablementAggregate.Handle(decision, state, Envelope(decision, "tenant-a", tenantAdmin: true))
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ProviderDataHandlingDecided>());

        DecideProviderDataHandling next = decision with { ExpectedRevision = 2 };
        ProviderDataHandlingDecided recorded = TenantProviderEnablementAggregate.Handle(next, state,
            Envelope(next, "tenant-a", tenantAdmin: true) with { UserId = "another-admin" })
            .Events.ShouldHaveSingleItem().ShouldBeOfType<ProviderDataHandlingDecided>();
        recorded.ActorUserId.ShouldBe("another-admin");
        recorded.Revision.ShouldBe(3);
    }

    private static TenantProviderEnablementState Enabled()
    {
        var state = new TenantProviderEnablementState();
        state.Apply(new TenantProviderModelEnablementSet("tenant-a", "provider", "model", true, 1, "operator", null));
        return state;
    }

    private static string Effect(Hexalith.EventStore.Contracts.Results.DomainResult result)
    {
        using JsonDocument document = JsonDocument.Parse(result.ResultPayload.ShouldNotBeNull());
        return document.RootElement.GetProperty("effect").GetString().ShouldNotBeNull();
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
