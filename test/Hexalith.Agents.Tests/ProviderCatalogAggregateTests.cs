using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.ProviderCatalogTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for <see cref="ProviderCatalogAggregate"/> covering create/update/enable/disable,
/// authorization fail-closed (AC3), idempotent no-op vs conflicting-duplicate (AC4), invalid metadata, unsafe
/// configuration input (AC1), and lifecycle/not-found rejections.
/// </summary>
public sealed class ProviderCatalogAggregateTests
{
    // ===== Create =====

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    public void Undefined_safe_capability_bits_reject_without_mutating_catalog(string operation)
    {
        ProviderModelCapabilityFlags undefined = ProviderModelCapabilityFlags.Streaming
            | (ProviderModelCapabilityFlags)(1 << 12);
        ProviderCatalogState state = StateWith(ValidCreate());
        DomainResult result = operation == "create"
            ? ProviderCatalogAggregate.Handle(ValidCreate() with
            {
                ModelId = "new-model", SafeCapabilityFlags = undefined,
            }, state, Envelope(ValidCreate() with { ModelId = "new-model" }))
            : ProviderCatalogAggregate.Handle(ValidUpdate() with { SafeCapabilityFlags = undefined },
                state, Envelope(ValidUpdate()));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InvalidProviderModelMetadataRejection>();
        state.Entries.ShouldHaveSingleItem().Value.SafeCapabilityFlags.ShouldBe(ValidCreate().SafeCapabilityFlags);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("enable")]
    [InlineData("disable")]
    public void Legacy_tenant_catalog_commands_are_frozen_at_the_aggregate_write_boundary(string operation)
    {
        ProviderCatalogState legacy = StateWith(ValidCreate());
        DomainResult result = operation switch
        {
            "create" => ProviderCatalogAggregate.Handle(ValidCreate(), legacy,
                Envelope(ValidCreate()) with { TenantId = "tenant-a", AggregateId = "tenant-a" }),
            "update" => ProviderCatalogAggregate.Handle(ValidUpdate(), legacy,
                Envelope(ValidUpdate()) with { TenantId = "tenant-a", AggregateId = "tenant-a" }),
            "enable" => ProviderCatalogAggregate.Handle(new EnableProviderModelEntry("openai", "gpt-4o"), legacy,
                Envelope(new EnableProviderModelEntry("openai", "gpt-4o")) with { TenantId = "tenant-a", AggregateId = "tenant-a" }),
            "disable" => ProviderCatalogAggregate.Handle(new DisableProviderModelEntry("openai", "gpt-4o"), legacy,
                Envelope(new DisableProviderModelEntry("openai", "gpt-4o")) with { TenantId = "tenant-a", AggregateId = "tenant-a" }),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();
        legacy.Entries.ShouldHaveSingleItem().Value.CapabilityVersion.ShouldBe(1);
    }

    [Fact]
    public void Create_with_no_state_and_admin_produces_created()
    {
        CreateProviderModelEntry command = ValidCreate();

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        ProviderModelEntryCreated created = result.Events[0].ShouldBeOfType<ProviderModelEntryCreated>();
        created.CatalogId.ShouldBe(CatalogId);
        created.ProviderId.ShouldBe("openai");
        created.ModelId.ShouldBe("gpt-4o");
        created.Enabled.ShouldBeTrue();
        created.ContextWindowTokenLimit.ShouldBe(128_000);
        created.MaxOutputTokenLimit.ShouldBe(16_000);
        created.ConfigurationState.ShouldBe(ProviderConfigurationState.Configured);
        created.ConfigurationReferenceId.ShouldBe("cfg-openai-gpt4o");
    }

    [Fact]
    public void Migration_preserves_historical_capability_pricing_and_terms_versions()
    {
        CreateProviderModelEntry command = ValidCreate() with
        {
            MigratedFrom = "legacy:tenant-a",
            InitialCapabilityVersion = 4,
            Pricing = new ProviderModelPricing("USD", 0.002m, 0.008m, 3),
            DataHandling = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v2", 2),
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ProviderModelEntryCreated created = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ProviderModelEntryCreated>();
        created.CapabilityVersion.ShouldBe(4);
        created.Pricing.PricingVersion.ShouldBe(3);
        created.DataHandling!.DataHandlingVersion.ShouldBe(2);
        created.MigratedFrom.ShouldBe("legacy:tenant-a");
    }

    [Fact]
    public void Unknown_three_letter_currency_rejects_as_non_iso()
    {
        CreateProviderModelEntry command = ValidCreate() with
        {
            Pricing = new ProviderModelPricing("ZZZ", 0m, 0m, 1),
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InvalidProviderModelPricingRejection>();
    }

    [Fact]
    public void ISO_currency_validation_accepts_codes_without_a_host_region()
    {
        Iso4217CurrencyCodes.IsValid("XCG").ShouldBeTrue();
        Iso4217CurrencyCodes.IsValid("xau").ShouldBeTrue();
        Iso4217CurrencyCodes.IsValid("ZZZ").ShouldBeFalse();
        CreateProviderModelEntry command = ValidCreate() with { Pricing = new("XCG", 0.002m, 0.008m, 1) };
        ProviderCatalogAggregate.Handle(command, null, Envelope(command)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_without_provider_admin_produces_denied_and_no_created()
    {
        CreateProviderModelEntry command = ValidCreate();

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command, isProviderAdmin: false, actorUserId: "intruder"));

        result.IsRejection.ShouldBeTrue();
        ProviderCatalogAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();
        denied.CatalogId.ShouldBe(CatalogId);
        denied.ActorUserId.ShouldBe("intruder");
        denied.CommandName.ShouldBe(nameof(CreateProviderModelEntry));
        result.Events.ShouldNotContain(e => e is ProviderModelEntryCreated);
    }

    [Fact]
    public void Create_with_no_configuration_reference_sets_not_configured()
    {
        CreateProviderModelEntry command = ValidCreate(configurationReferenceId: null);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        ProviderModelEntryCreated created = result.Events[0].ShouldBeOfType<ProviderModelEntryCreated>();
        created.ConfigurationState.ShouldBe(ProviderConfigurationState.NotConfigured);
        created.ConfigurationReferenceId.ShouldBeNull();
    }

    [Fact]
    public void Create_exact_duplicate_produces_noop()
    {
        CreateProviderModelEntry command = ValidCreate();
        ProviderCatalogState state = StateWith(command);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Create_duplicate_ignores_a_new_server_stamped_effective_time()
    {
        CreateProviderModelEntry original = ValidCreate();
        ProviderCatalogState state = StateWith(original);
        CreateProviderModelEntry retry = original with
        {
            DataHandling = original.DataHandling! with { EffectiveAt = original.DataHandling.EffectiveAt!.Value.AddMinutes(1) },
        };

        ProviderCatalogAggregate.Handle(retry, state, Envelope(retry)).IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void Enabled_create_without_terms_rejects()
    {
        CreateProviderModelEntry command = ValidCreate() with { DataHandling = null };

        ProviderCatalogAggregate.Handle(command, null, Envelope(command)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<InvalidProviderDataHandlingRejection>();
    }

    [Fact]
    public void Enable_without_terms_rejects()
    {
        CreateProviderModelEntry create = ValidCreate(enabled: false) with { DataHandling = null };
        ProviderCatalogState state = StateWith(create);
        var command = new EnableProviderModelEntry(create.ProviderId, create.ModelId, 1);

        ProviderCatalogAggregate.Handle(command, state, Envelope(command)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<InvalidProviderDataHandlingRejection>();
    }

    [Fact]
    public void Create_with_a_client_tightening_declaration_rejects()
    {
        ProviderDataHandlingRecord prior = ValidTerms() with { DataHandlingVersion = 0, RetentionDays = 60 };
        ProviderDataHandlingRecord current = ValidTerms();
        ProviderDataHandlingTighteningDeclaration declaration = ProviderDataHandlingPolicy.DeclareTightening(
            prior, current, "operator")!;
        CreateProviderModelEntry command = ValidCreate() with
        {
            DataHandling = current with { TighteningDeclaration = declaration },
        };

        ProviderCatalogAggregate.Handle(command, null, Envelope(command)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<InvalidProviderDataHandlingRejection>();
    }

    [Fact]
    public void Create_conflicting_duplicate_produces_already_exists_and_no_mutation()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        CreateProviderModelEntry conflicting = ValidCreate(displayLabel: "A Different Label");

        DomainResult result = ProviderCatalogAggregate.Handle(conflicting, state, Envelope(conflicting));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderModelEntryAlreadyExistsRejection>();
        result.Events.ShouldNotContain(e => e is ProviderModelEntryCreated);
    }

    [Fact]
    public void Create_entries_whose_naive_concatenation_would_collide_stay_distinct()
    {
        // Catalog integrity: ("ab","c") and ("a","bc") both naively concatenate to "abc". The composite key
        // separator must keep them apart so one governed entry can never alias or silently overwrite another.
        // Without it, this second create would be swallowed as a no-op (identical default metadata) against the
        // first entry instead of creating a distinct one.
        ProviderCatalogState state = StateWith(ValidCreate(providerId: "ab", modelId: "c"));
        CreateProviderModelEntry second = ValidCreate(providerId: "a", modelId: "bc");

        DomainResult result = ProviderCatalogAggregate.Handle(second, state, Envelope(second));

        result.IsSuccess.ShouldBeTrue();
        ProviderModelEntryCreated created = result.Events[0].ShouldBeOfType<ProviderModelEntryCreated>();
        created.ProviderId.ShouldBe("a");
        created.ModelId.ShouldBe("bc");

        state.Apply(created);
        state.Entries.Count.ShouldBe(2);
        ProviderCatalogState.EntryKey("ab", "c").ShouldNotBe(ProviderCatalogState.EntryKey("a", "bc"));
    }

    [Theory]
    [InlineData(0, 16_000)]      // non-positive context window
    [InlineData(128_000, 0)]     // non-positive max output
    [InlineData(8_000, 16_000)]  // max output greater than context window
    public void Create_with_invalid_token_limits_produces_invalid_metadata(int contextWindow, int maxOutput)
    {
        CreateProviderModelEntry command = ValidCreate() with
        {
            ContextWindowTokenLimit = contextWindow,
            MaxOutputTokenLimit = maxOutput,
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        InvalidProviderModelMetadataRejection rejection = result.Events[0].ShouldBeOfType<InvalidProviderModelMetadataRejection>();
        rejection.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("", 0.002, 0.008, 1)]
    [InlineData("USD", -1, 0.008, 1)]
    [InlineData("USD", 0.002, -1, 1)]
    [InlineData("US", 0.002, 0.008, 1)]
    public void Create_with_invalid_pricing_produces_invalid_pricing_rejection(
        string currency,
        double input,
        double output,
        int version)
    {
        CreateProviderModelEntry command = ValidCreate() with
        {
            Pricing = new ProviderModelPricing(currency, (decimal)input, (decimal)output, version),
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidProviderModelPricingRejection>();
    }

    [Theory]
    [InlineData(0, 3)]                                                       // non-positive timeout
    [InlineData(ProviderCatalogAggregate.MaxRequestTimeoutMilliseconds + 1, 3)] // over-long timeout
    [InlineData(30_000, ProviderCatalogAggregate.MaxRetryCount + 1)]         // too many retries
    public void Create_with_invalid_timeout_policy_produces_invalid_metadata(int timeoutMs, int retries)
    {
        CreateProviderModelEntry command = ValidCreate() with
        {
            TimeoutPolicy = new ProviderModelTimeoutPolicy(timeoutMs, retries),
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidProviderModelMetadataRejection>();
    }

    [Theory]
    [InlineData("contains spaces")]
    [InlineData("has\tcontrol")]
    [InlineData("sk-secretlooking value!!")]
    public void Create_with_unsafe_configuration_reference_produces_unsafe_rejection(string reference)
    {
        CreateProviderModelEntry command = ValidCreate(configurationReferenceId: reference);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        UnsafeProviderConfigurationInputRejection rejection = result.Events[0].ShouldBeOfType<UnsafeProviderConfigurationInputRejection>();
        rejection.Reason.ShouldNotContain(reference); // never echoes the offending value
        result.Events.ShouldNotContain(e => e is ProviderModelEntryCreated);
    }

    [Fact]
    public void Create_with_over_long_configuration_reference_produces_unsafe_rejection()
    {
        string reference = new('a', ProviderCatalogAggregate.MaxConfigurationReferenceLength + 1);
        CreateProviderModelEntry command = ValidCreate(configurationReferenceId: reference);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<UnsafeProviderConfigurationInputRejection>();
    }

    // ===== Update =====

    [Fact]
    public void Update_without_expected_capability_version_rejects_before_changing_metadata()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        UpdateProviderModelEntry command = ValidUpdate() with
        {
            DisplayLabel = "New label",
            ExpectedCapabilityVersion = null,
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events.ShouldHaveSingleItem().ShouldBeOfType<InvalidProviderModelMetadataRejection>();
        state.Entries.ShouldHaveSingleItem().Value.DisplayLabel.ShouldBe(ValidCreate().DisplayLabel);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Terms_update_records_tightening_only_when_platform_operator_declares_it(bool declare)
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        ProviderDataHandlingRecord proposed = ValidTerms() with
        {
            RetentionDays = 14,
            DataHandlingVersion = 0,
            EffectiveAt = new DateTimeOffset(2026, 9, 24, 0, 0, 0, TimeSpan.Zero),
        };
        UpdateProviderModelEntry command = ValidUpdate() with
        {
            DataHandling = proposed,
            DeclareDataHandlingTightening = declare,
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ProviderDataHandlingRecord updated = result.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<ProviderModelEntryMetadataUpdated>().DataHandling.ShouldNotBeNull();
        updated.DataHandlingVersion.ShouldBe(2);
        if (declare)
        {
            updated.TighteningDeclaration.ShouldNotBeNull().ActorUserId.ShouldBe("admin-user");
            updated.TighteningDeclaration.FieldDiff.PreviousRetentionDays.ShouldBe(30);
            updated.TighteningDeclaration.FieldDiff.NewRetentionDays.ShouldBe(14);
        }
        else
        {
            updated.TighteningDeclaration.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    public void Declared_unchanged_or_loosened_terms_reject(int retentionDays)
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        UpdateProviderModelEntry command = ValidUpdate() with
        {
            DataHandling = ValidTerms() with
            {
                RetentionDays = retentionDays,
                DataHandlingVersion = 0,
                EffectiveAt = ValidTerms().EffectiveAt!.Value.AddDays(1),
            },
            DeclareDataHandlingTightening = true,
        };

        ProviderCatalogAggregate.Handle(command, state, Envelope(command)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<InvalidProviderDataHandlingRejection>();
    }

    [Fact]
    public void Update_with_a_client_supplied_tightening_declaration_rejects_without_changing_history()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        ProviderModelEntryState entry = state.Entries.ShouldHaveSingleItem().Value;
        ProviderDataHandlingRecord previous = entry.DataHandling.ShouldNotBeNull();
        ProviderDataHandlingRecord proposed = previous with
        {
            RetentionDays = 14,
            DataHandlingVersion = 2,
            EffectiveAt = previous.EffectiveAt!.Value.AddDays(1),
        };
        ProviderDataHandlingTighteningDeclaration supplied = ProviderDataHandlingPolicy.DeclareTightening(
            previous, proposed, "client-actor").ShouldNotBeNull();
        UpdateProviderModelEntry command = ValidUpdate() with
        {
            DataHandling = proposed with { TighteningDeclaration = supplied },
        };

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InvalidProviderDataHandlingRejection>();
        entry.CapabilityVersion.ShouldBe(1);
        entry.DataHandlingHistory.ShouldHaveSingleItem().ShouldBe(previous);
    }

    [Fact]
    public void Terms_history_fold_controls_tenant_eligibility_after_loosening()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        ProviderDataHandlingRecord accepted = state.Entries.ShouldHaveSingleItem().Value.DataHandling.ShouldNotBeNull();
        UpdateProviderModelEntry tightening = ValidUpdate() with
        {
            DataHandling = accepted with { RetentionDays = 14, DataHandlingVersion = 0, EffectiveAt = accepted.EffectiveAt!.Value.AddDays(1) },
            DeclareDataHandlingTightening = true,
        };
        ApplyAll(state, ProviderCatalogAggregate.Handle(tightening, state, Envelope(tightening)));
        ProviderDataHandlingRecord first = state.Entries.ShouldHaveSingleItem().Value.DataHandling.ShouldNotBeNull();
        UpdateProviderModelEntry loosening = ValidUpdate() with
        {
            ExpectedCapabilityVersion = 2,
            DataHandling = first with { RetentionDays = 60, DataHandlingVersion = 0, TighteningDeclaration = null,
                EffectiveAt = first.EffectiveAt!.Value.AddDays(1) },
        };
        ApplyAll(state, ProviderCatalogAggregate.Handle(loosening, state, Envelope(loosening)));

        IReadOnlyList<ProviderDataHandlingRecord> history = state.Entries.ShouldHaveSingleItem().Value.DataHandlingHistory;
        history.Select(item => item.DataHandlingVersion).ShouldBe([1, 2, 3]);
        TenantProviderEligibility.Evaluate(new TenantProviderEntryState { Enabled = true, AcceptedTerms = accepted },
            history, first.EffectiveAt!.Value.AddDays(2)).Status.ShouldBe("TermsChanged");
    }

    [Fact]
    public void Update_existing_entry_changes_metadata_produces_updated()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        var command = new UpdateProviderModelEntry(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o (renamed)",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 200_000,
            MaxOutputTokenLimit: 32_000,
            new ProviderModelTimeoutPolicy(45_000, 2),
            ProviderModelCapabilityFlags.Streaming,
            "cfg-openai-gpt4o",
            ValidPricing(),
            ExpectedCapabilityVersion: 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ProviderModelEntryMetadataUpdated updated = result.Events[0].ShouldBeOfType<ProviderModelEntryMetadataUpdated>();
        updated.DisplayLabel.ShouldBe("OpenAI GPT-4o (renamed)");
        updated.ContextWindowTokenLimit.ShouldBe(200_000);
        updated.MaxOutputTokenLimit.ShouldBe(32_000);
    }

    [Fact]
    public void Update_that_changes_pricing_units_emits_the_next_pricing_version()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        int currentPricingVersion = state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")].Pricing!.PricingVersion;
        UpdateProviderModelEntry command = ValidUpdate(pricing: new ProviderModelPricing("EUR", 0.01m, 0.02m, 0));

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ProviderModelEntryMetadataUpdated updated = result.Events[0].ShouldBeOfType<ProviderModelEntryMetadataUpdated>();
        updated.Pricing.Currency.ShouldBe("EUR");
        updated.Pricing.InputTokenUnitPrice.ShouldBe(0.01m);
        updated.Pricing.OutputTokenUnitPrice.ShouldBe(0.02m);
        updated.Pricing.PricingVersion.ShouldBe(currentPricingVersion + 1);
    }

    [Fact]
    public void Changed_pricing_rejects_when_imported_version_is_exhausted()
    {
        CreateProviderModelEntry imported = ValidCreate() with
        {
            MigratedFrom = "legacy:provider-model",
            Pricing = ValidPricing(int.MaxValue),
        };
        ProviderCatalogState state = StateWith(imported);
        UpdateProviderModelEntry unchanged = ValidUpdate(imported);
        UpdateProviderModelEntry changed = ValidUpdate(imported,
            pricing: imported.Pricing with { InputTokenUnitPrice = imported.Pricing.InputTokenUnitPrice + 0.001m,
                PricingVersion = 0 });

        ProviderCatalogAggregate.Handle(unchanged, state, Envelope(unchanged)).IsNoOp.ShouldBeTrue();
        DomainResult result = ProviderCatalogAggregate.Handle(changed, state, Envelope(changed));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InvalidProviderModelPricingRejection>()
            .Reason.ShouldBe("Pricing version is exhausted.");
        state.Entries.ShouldHaveSingleItem().Value.Pricing.ShouldNotBeNull().PricingVersion.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void Update_missing_entry_produces_not_found()
    {
        var command = new UpdateProviderModelEntry(
            "openai",
            "missing",
            "Label",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 128_000,
            MaxOutputTokenLimit: 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.None,
            null,
            ValidPricing());

        DomainResult result = ProviderCatalogAggregate.Handle(command, StateWith(ValidCreate()), Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderModelEntryNotFoundRejection>();
    }

    [Fact]
    public void Update_identical_metadata_produces_noop()
    {
        CreateProviderModelEntry create = ValidCreate();
        ProviderCatalogState state = StateWith(create);
        var command = new UpdateProviderModelEntry(
            create.ProviderId,
            create.ModelId,
            create.DisplayLabel,
            create.SupportsTextGeneration,
            create.ContextWindowTokenLimit,
            create.MaxOutputTokenLimit,
            create.TimeoutPolicy,
            create.SafeCapabilityFlags,
            create.ConfigurationReferenceId,
            ValidPricing(),
            ExpectedCapabilityVersion: 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void Changed_update_rejects_when_imported_capability_version_is_exhausted()
    {
        CreateProviderModelEntry imported = ValidCreate() with
        {
            MigratedFrom = "legacy:provider-model",
            InitialCapabilityVersion = int.MaxValue,
        };
        var state = new ProviderCatalogState();
        state.Apply(CreatedEvent(imported) with { CapabilityVersion = int.MaxValue });
        UpdateProviderModelEntry changed = ValidUpdate() with
        {
            DisplayLabel = "Changed label",
            ExpectedCapabilityVersion = int.MaxValue,
        };

        DomainResult result = ProviderCatalogAggregate.Handle(changed, state, Envelope(changed));

        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InvalidProviderModelMetadataRejection>()
            .Reason.ShouldBe("Capability version is exhausted.");
        state.Entries.ShouldHaveSingleItem().Value.CapabilityVersion.ShouldBe(int.MaxValue);
    }

    [Fact]
    public void Update_without_provider_admin_produces_denied()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        var command = new UpdateProviderModelEntry(
            "openai",
            "gpt-4o",
            "Renamed",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 128_000,
            MaxOutputTokenLimit: 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.None,
            null,
            ValidPricing());

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command, isProviderAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();
    }

    // ===== Enable / Disable =====

    [Fact]
    public void Two_operators_cannot_reverse_a_lifecycle_change_with_stale_intent()
    {
        ProviderCatalogState state = StateWith(ValidCreate(enabled: true));
        var disable = new DisableProviderModelEntry("openai", "gpt-4o", 1);
        DomainResult first = ProviderCatalogAggregate.Handle(disable, state, Envelope(disable));
        first.IsSuccess.ShouldBeTrue();
        state.Apply(first.Events[0].ShouldBeOfType<ProviderModelEntryDisabled>());
        state.Entries.Values.ShouldHaveSingleItem().LifecycleRevision.ShouldBe(2);

        var staleEnable = new EnableProviderModelEntry("openai", "gpt-4o", 1);
        ProviderCatalogAggregate.Handle(staleEnable, state, Envelope(staleEnable)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<ProviderModelLifecycleRevisionRejected>();
        var missingRevision = new EnableProviderModelEntry("openai", "gpt-4o");
        ProviderCatalogAggregate.Handle(missingRevision, state, Envelope(missingRevision)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<ProviderModelLifecycleRevisionRejected>();
        state.Entries.Values.ShouldHaveSingleItem().IsEnabled.ShouldBeFalse();

        var currentEnable = new EnableProviderModelEntry("openai", "gpt-4o", 2);
        DomainResult second = ProviderCatalogAggregate.Handle(currentEnable, state, Envelope(currentEnable));
        second.IsSuccess.ShouldBeTrue();
        state.Apply(second.Events[0].ShouldBeOfType<ProviderModelEntryEnabled>());
        state.Entries.Values.ShouldHaveSingleItem().LifecycleRevision.ShouldBe(3);
    }

    [Fact]
    public void Disable_enabled_entry_produces_disabled()
    {
        ProviderCatalogState state = StateWith(ValidCreate(enabled: true));
        var command = new DisableProviderModelEntry("openai", "gpt-4o", 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ProviderModelEntryDisabled disabled = result.Events[0].ShouldBeOfType<ProviderModelEntryDisabled>();
        disabled.ProviderId.ShouldBe("openai");
        disabled.ModelId.ShouldBe("gpt-4o");
    }

    [Fact]
    public void Disable_already_disabled_produces_lifecycle_already_set()
    {
        ProviderCatalogState state = StateWith(ValidCreate(enabled: false));
        var command = new DisableProviderModelEntry("openai", "gpt-4o", 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        ProviderModelEntryLifecycleStateAlreadySetRejection rejection =
            result.Events[0].ShouldBeOfType<ProviderModelEntryLifecycleStateAlreadySetRejection>();
        rejection.CurrentStatus.ShouldBe(ProviderModelStatus.Disabled);
        rejection.RequestedStatus.ShouldBe(ProviderModelStatus.Disabled);
        rejection.CommandName.ShouldBe(nameof(DisableProviderModelEntry));
    }

    [Fact]
    public void Disable_missing_entry_produces_not_found()
    {
        var command = new DisableProviderModelEntry("openai", "missing");

        DomainResult result = ProviderCatalogAggregate.Handle(command, StateWith(ValidCreate()), Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderModelEntryNotFoundRejection>();
    }

    [Fact]
    public void Disable_without_provider_admin_produces_denied()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        var command = new DisableProviderModelEntry("openai", "gpt-4o", 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command, isProviderAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();
    }

    [Fact]
    public void Enable_disabled_entry_produces_enabled()
    {
        ProviderCatalogState state = StateWith(ValidCreate(enabled: false));
        var command = new EnableProviderModelEntry("openai", "gpt-4o", 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderModelEntryEnabled>();
    }

    [Fact]
    public void Enable_already_enabled_produces_lifecycle_already_set()
    {
        ProviderCatalogState state = StateWith(ValidCreate(enabled: true));
        var command = new EnableProviderModelEntry("openai", "gpt-4o", 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        ProviderModelEntryLifecycleStateAlreadySetRejection rejection =
            result.Events[0].ShouldBeOfType<ProviderModelEntryLifecycleStateAlreadySetRejection>();
        rejection.CurrentStatus.ShouldBe(ProviderModelStatus.Enabled);
        rejection.RequestedStatus.ShouldBe(ProviderModelStatus.Enabled);
    }

    [Fact]
    public void Enable_missing_entry_produces_not_found()
    {
        var command = new EnableProviderModelEntry("openai", "missing");

        DomainResult result = ProviderCatalogAggregate.Handle(command, StateWith(ValidCreate()), Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderModelEntryNotFoundRejection>();
    }

    [Fact]
    public void Enable_without_provider_admin_produces_denied()
    {
        ProviderCatalogState state = StateWith(ValidCreate(enabled: false));
        var command = new EnableProviderModelEntry("openai", "gpt-4o", 1);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command, isProviderAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();
    }

    // ===== AC3: denial must not reveal whether unrelated entries exist =====

    [Fact]
    public void Denied_mutation_does_not_reveal_entry_existence()
    {
        // An entry exists, but the unauthorized caller only ever sees a generic denial — never an
        // "already exists" / "not found" signal that would fingerprint the catalog contents.
        ProviderCatalogState state = StateWith(ValidCreate());
        CreateProviderModelEntry command = ValidCreate();

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command, isProviderAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();
        result.Events.ShouldNotContain(e => e is ProviderModelEntryAlreadyExistsRejection);
        result.Events.ShouldNotContain(e => e is ProviderModelEntryNotFoundRejection);
    }

    // ===== AC4: full reflection dispatch + JSON payload round-trip via ProcessAsync =====

    [Fact]
    public async Task ProcessAsync_create_round_trips_payload_and_dispatches_handler()
    {
        var aggregate = new ProviderCatalogAggregate();
        CreateProviderModelEntry command = ValidCreate();

        DomainResult result = await aggregate.ProcessAsync(Envelope(command), currentState: null);

        result.IsSuccess.ShouldBeTrue();
        ProviderModelEntryCreated created = result.Events[0].ShouldBeOfType<ProviderModelEntryCreated>();
        created.SafeCapabilityFlags.ShouldBe(ProviderModelCapabilityFlags.Streaming | ProviderModelCapabilityFlags.ToolCalling);
        created.TimeoutPolicy.ShouldBe(new ProviderModelTimeoutPolicy(30_000, 3));
    }

    [Fact]
    public async Task ProcessAsync_create_without_admin_round_trips_to_denied()
    {
        var aggregate = new ProviderCatalogAggregate();
        CreateProviderModelEntry command = ValidCreate();

        DomainResult result = await aggregate.ProcessAsync(Envelope(command, isProviderAdmin: false), currentState: null);

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();
    }
}
