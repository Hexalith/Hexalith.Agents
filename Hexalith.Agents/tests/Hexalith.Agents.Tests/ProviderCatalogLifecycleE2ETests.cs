using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.ProviderCatalogTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// End-to-end lifecycle tests for the governed ProviderCatalog feature (Story 1.2). Unlike the focused
/// Handle/Apply unit tests, these drive the <em>real</em> aggregate pipeline — JSON command envelope →
/// reflection dispatch in <see cref="ProviderCatalogAggregate.ProcessAsync"/> → typed handler → events — and
/// thread the evolving state across successive commands through the production <c>Apply</c> replay handlers,
/// then assert the outcome through the authorized inspection read path. Each test maps to an acceptance
/// criterion: AC1 (governed state change + secret safety), AC2 (disable preserves history, blocks selection),
/// AC3 (authorization fails closed before mutation), AC4 (replay determinism + idempotency).
/// </summary>
public sealed class ProviderCatalogLifecycleE2ETests
{
    // ===== AC1: an authorized create is governed and surfaces the full AD-10 capability floor safely =====

    [Fact]
    public async Task Authorized_create_then_inspect_surfaces_full_capability_floor_with_safe_reference_only()
    {
        var aggregate = new ProviderCatalogAggregate();
        var state = new ProviderCatalogState();
        CreateProviderModelEntry create = ValidCreate();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, create);

        result.IsSuccess.ShouldBeTrue();

        // Inspect through the authorized read path — the full AD-10 floor must round-trip verbatim.
        ProviderCatalogInspectionResult inspection = ProviderCatalogInspection.GetEntry(state, isProviderAdmin: true, create.ProviderId, create.ModelId);
        inspection.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        ProviderCatalogEntryView view = inspection.Entries.ShouldHaveSingleItem();
        view.ProviderId.ShouldBe(create.ProviderId);
        view.ModelId.ShouldBe(create.ModelId);
        view.DisplayLabel.ShouldBe(create.DisplayLabel);
        view.Status.ShouldBe(ProviderModelStatus.Enabled);
        view.SupportsTextGeneration.ShouldBe(create.SupportsTextGeneration);
        view.ContextWindowTokenLimit.ShouldBe(create.ContextWindowTokenLimit);
        view.MaxOutputTokenLimit.ShouldBe(create.MaxOutputTokenLimit);
        view.TimeoutPolicy.ShouldBe(create.TimeoutPolicy);
        view.SafeCapabilityFlags.ShouldBe(create.SafeCapabilityFlags);
        view.IsSelectableForNewActiveUse.ShouldBeTrue();

        // AC1 / AD-14: only a safe reference + configured-state is exposed — never a secret value.
        view.ConfigurationState.ShouldBe(ProviderConfigurationState.Configured);
        view.ConfigurationReferenceId.ShouldBe(create.ConfigurationReferenceId);
    }

    // ===== AC2: disable blocks future active selection but preserves inspectable history =====

    [Fact]
    public async Task Create_disable_then_reenable_lifecycle_preserves_history_and_gates_selection()
    {
        var aggregate = new ProviderCatalogAggregate();
        var state = new ProviderCatalogState();
        CreateProviderModelEntry create = ValidCreate(enabled: true);

        (await ProcessAndApplyAsync(aggregate, state, create)).IsSuccess.ShouldBeTrue();

        // Enabled entry is listed and selectable.
        ProviderCatalogInspection.ListEntries(state, isProviderAdmin: true, includeDisabled: false)
            .Entries.ShouldContain(v => v.ProviderId == create.ProviderId && v.IsSelectableForNewActiveUse);

        // Disable it.
        (await ProcessAndApplyAsync(aggregate, state, new DisableProviderModelEntry(create.ProviderId, create.ModelId)))
            .IsSuccess.ShouldBeTrue();

        // AC2: no longer selectable for new active use → excluded from the default (enabled-only) list.
        ProviderCatalogInspection.ListEntries(state, isProviderAdmin: true, includeDisabled: false)
            .Entries.ShouldBeEmpty();

        // AC2: history preserved — still inspectable, flagged Disabled and not selectable.
        ProviderCatalogInspectionResult historical = ProviderCatalogInspection.ListEntries(state, isProviderAdmin: true, includeDisabled: true);
        ProviderCatalogEntryView disabledView = historical.Entries.ShouldHaveSingleItem();
        disabledView.Status.ShouldBe(ProviderModelStatus.Disabled);
        disabledView.IsSelectableForNewActiveUse.ShouldBeFalse();

        // The single-entry read also still finds the disabled entry (history is not deleted).
        ProviderCatalogInspection.GetEntry(state, isProviderAdmin: true, create.ProviderId, create.ModelId)
            .Status.ShouldBe(ProviderCatalogInspectionStatus.Success);

        // Re-enabling restores selectability.
        (await ProcessAndApplyAsync(aggregate, state, new EnableProviderModelEntry(create.ProviderId, create.ModelId)))
            .IsSuccess.ShouldBeTrue();
        ProviderCatalogInspection.GetEntry(state, isProviderAdmin: true, create.ProviderId, create.ModelId)
            .Entries.ShouldHaveSingleItem().IsSelectableForNewActiveUse.ShouldBeTrue();
    }

    // ===== AC3: authorization fails closed — the request fails before any mutation =====

    [Fact]
    public async Task Unauthorized_create_fails_before_mutation_and_authorized_inspection_sees_empty_catalog()
    {
        var aggregate = new ProviderCatalogAggregate();
        var state = new ProviderCatalogState();
        CreateProviderModelEntry create = ValidCreate();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, create, isProviderAdmin: false);

        // The mutation is rejected with a generic denial...
        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<ProviderCatalogAdministrationDeniedRejection>();

        // ...and no entry was ever created (fails before mutation). A later authorized read sees an empty catalog.
        ProviderCatalogInspectionResult inspection = ProviderCatalogInspection.ListEntries(state, isProviderAdmin: true, includeDisabled: true);
        inspection.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        inspection.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unauthorized_caller_cannot_inspect_even_after_authorized_create()
    {
        var aggregate = new ProviderCatalogAggregate();
        var state = new ProviderCatalogState();
        CreateProviderModelEntry create = ValidCreate();

        (await ProcessAndApplyAsync(aggregate, state, create)).IsSuccess.ShouldBeTrue();

        // AC3: unauthorized inspection fails closed with no entry data (no fingerprinting of what exists).
        ProviderCatalogInspection.ListEntries(state, isProviderAdmin: false, includeDisabled: true)
            .Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        ProviderCatalogInspection.GetEntry(state, isProviderAdmin: false, create.ProviderId, create.ModelId)
            .Entries.ShouldBeEmpty();
    }

    // ===== AC4: idempotency — duplicate delivery is a no-op; a conflicting payload is rejected, never silent =====

    [Fact]
    public async Task Duplicate_create_delivery_is_idempotent_and_conflicting_payload_is_rejected()
    {
        var aggregate = new ProviderCatalogAggregate();
        var state = new ProviderCatalogState();
        CreateProviderModelEntry create = ValidCreate();

        (await ProcessAndApplyAsync(aggregate, state, create)).IsSuccess.ShouldBeTrue();

        // Exact-duplicate re-delivery → deterministic no-op, state unchanged.
        DomainResult duplicate = await ProcessAndApplyAsync(aggregate, state, create);
        duplicate.IsNoOp.ShouldBeTrue();

        // Conflicting payload (same identity, different label) → rejection, never a silent mutation.
        CreateProviderModelEntry conflicting = create with { DisplayLabel = "Conflicting Label" };
        DomainResult conflict = await ProcessAndApplyAsync(aggregate, state, conflicting);
        conflict.IsRejection.ShouldBeTrue();
        _ = conflict.Events[0].ShouldBeOfType<ProviderModelEntryAlreadyExistsRejection>();

        // Inspection confirms the original label survived — the conflicting create did not mutate state.
        ProviderCatalogEntryView view = ProviderCatalogInspection
            .GetEntry(state, isProviderAdmin: true, create.ProviderId, create.ModelId)
            .Entries.ShouldHaveSingleItem();
        view.DisplayLabel.ShouldBe(create.DisplayLabel);
    }

    // ===== AC4: rehydration is deterministic — replaying the emitted stream reproduces identical state =====

    [Fact]
    public async Task Replaying_the_emitted_event_stream_rehydrates_identical_state()
    {
        var aggregate = new ProviderCatalogAggregate();
        var live = new ProviderCatalogState();

        // A multi-command admin journey across two providers. We collect every emitted success event so we can
        // replay the exact stream into a fresh state and prove deterministic rehydration (AC4).
        var stream = new System.Collections.Generic.List<Hexalith.EventStore.Contracts.Events.IEventPayload>();

        async Task Drive<TCommand>(TCommand command) where TCommand : notnull
        {
            DomainResult r = await ProcessAndApplyAsync(aggregate, live, command);
            r.IsRejection.ShouldBeFalse();
            stream.AddRange(r.Events);
        }

        await Drive(ValidCreate(enabled: true, providerId: "openai", modelId: "gpt-4o"));
        await Drive(ValidCreate(enabled: true, providerId: "anthropic", modelId: "claude", displayLabel: "Anthropic Claude", configurationReferenceId: "cfg-anthropic"));
        await Drive(new UpdateProviderModelEntry(
            "openai", "gpt-4o", "OpenAI GPT-4o (v2)",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 200_000,
            MaxOutputTokenLimit: 32_000,
            new ProviderModelTimeoutPolicy(45_000, 2),
            ProviderModelCapabilityFlags.Streaming | ProviderModelCapabilityFlags.Vision,
            "cfg-openai-gpt4o"));
        await Drive(new DisableProviderModelEntry("anthropic", "claude"));

        // Replay the captured stream into a brand-new state (the production Apply handlers).
        var replayed = new ProviderCatalogState();
        ApplyAll(replayed, new DomainResult(stream));

        // Both states must yield byte-for-byte identical inspection views (record structural equality, including
        // disabled history) — proving replay is deterministic regardless of how the state was built.
        ProviderCatalogInspectionResult liveViews = ProviderCatalogInspection.ListEntries(live, isProviderAdmin: true, includeDisabled: true);
        ProviderCatalogInspectionResult replayedViews = ProviderCatalogInspection.ListEntries(replayed, isProviderAdmin: true, includeDisabled: true);

        replayedViews.Entries.Count.ShouldBe(2);
        replayedViews.Entries.ShouldBe(liveViews.Entries); // element-wise record equality
        replayed.CatalogId.ShouldBe(live.CatalogId);
    }
}
