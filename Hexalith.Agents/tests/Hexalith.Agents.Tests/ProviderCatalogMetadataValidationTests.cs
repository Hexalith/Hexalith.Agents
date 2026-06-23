using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.ProviderCatalogTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Gap-filling validation tests for <see cref="ProviderCatalogAggregate"/> (AC1, AC4). The dev-story suite
/// covers token-limit, timeout and configuration-reference rejections; these exercise the remaining
/// fail-before-mutation branches that were untested: blank/over-long provider and model identifiers, blank and
/// over-long display labels, and the update-command validation path (invalid metadata and unsafe configuration
/// reference on update).
/// </summary>
public sealed class ProviderCatalogMetadataValidationTests
{
    // ===== Identifier validation (create) =====

    [Theory]
    [InlineData("", "gpt-4o")]
    [InlineData("   ", "gpt-4o")]
    [InlineData("openai", "")]
    [InlineData("openai", "  ")]
    public void Create_with_blank_identifier_produces_invalid_metadata(string providerId, string modelId)
    {
        CreateProviderModelEntry command = ValidCreate(providerId: providerId, modelId: modelId);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        InvalidProviderModelMetadataRejection rejection = result.Events[0].ShouldBeOfType<InvalidProviderModelMetadataRejection>();
        rejection.Reason.ShouldNotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(true)]  // over-long provider id
    [InlineData(false)] // over-long model id
    public void Create_with_over_long_identifier_produces_invalid_metadata(bool overlongProvider)
    {
        string tooLong = new('x', ProviderCatalogAggregate.MaxIdentifierLength + 1);
        CreateProviderModelEntry command = ValidCreate(
            providerId: overlongProvider ? tooLong : "openai",
            modelId: overlongProvider ? "gpt-4o" : tooLong);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidProviderModelMetadataRejection>();
    }

    // ===== Display-label validation (create) =====

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_with_blank_display_label_produces_invalid_metadata(string displayLabel)
    {
        CreateProviderModelEntry command = ValidCreate(displayLabel: displayLabel);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidProviderModelMetadataRejection>();
    }

    [Fact]
    public void Create_with_over_long_display_label_produces_invalid_metadata()
    {
        string displayLabel = new('L', ProviderCatalogAggregate.MaxDisplayLabelLength + 1);
        CreateProviderModelEntry command = ValidCreate(displayLabel: displayLabel);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidProviderModelMetadataRejection>();
    }

    [Fact]
    public void Create_with_display_label_at_max_length_is_accepted()
    {
        string displayLabel = new('L', ProviderCatalogAggregate.MaxDisplayLabelLength);
        CreateProviderModelEntry command = ValidCreate(displayLabel: displayLabel);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
    }

    // ===== Update-command validation path (previously untested) =====

    [Fact]
    public void Update_with_invalid_token_limits_produces_invalid_metadata_before_mutation()
    {
        ProviderCatalogState state = StateWith(ValidCreate());
        var command = new UpdateProviderModelEntry(
            "openai",
            "gpt-4o",
            "Renamed",
            SupportsTextGeneration: true,
            ContextWindowTokenLimit: 8_000,
            MaxOutputTokenLimit: 16_000, // exceeds context window → invalid
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.None,
            "cfg-openai-gpt4o");

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidProviderModelMetadataRejection>();

        // The original metadata is untouched (validation fails before mutation).
        state.Entries[ProviderCatalogState.EntryKey("openai", "gpt-4o")].DisplayLabel.ShouldBe("OpenAI GPT-4o");
    }

    [Fact]
    public void Update_with_unsafe_configuration_reference_produces_unsafe_rejection()
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
            "sk-live-not a safe reference!!");

        DomainResult result = ProviderCatalogAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        UnsafeProviderConfigurationInputRejection rejection = result.Events[0].ShouldBeOfType<UnsafeProviderConfigurationInputRejection>();
        rejection.Reason.ShouldNotContain("sk-live"); // never echoes the offending value
    }
}
