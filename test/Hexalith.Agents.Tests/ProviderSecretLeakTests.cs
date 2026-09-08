using System.Linq;
using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.ProviderCatalog;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.ProviderCatalogTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Story 5.3 poison-secret sweep: a raw secret value is rejected before mutation and never appears on the
/// persisted events or the safe inspection view.
/// </summary>
public sealed class ProviderSecretLeakTests
{
    private const string PoisonSecret = "sk-live-POISON-SECRET-do-not-leak!!!!";

    [Fact]
    public void A_poison_secret_value_is_rejected_and_absent_from_every_captured_artifact()
    {
        var command = ValidCreate(configurationReferenceId: PoisonSecret);

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        UnsafeProviderConfigurationInputRejection rejection =
            result.Events[0].ShouldBeOfType<UnsafeProviderConfigurationInputRejection>();
        rejection.Reason.ShouldNotContain(PoisonSecret);

        string serialized = SerializeEvents(result);
        serialized.ShouldNotContain(PoisonSecret);

        ProviderCatalogInspectionResult inspection = ProviderCatalogInspection.GetEntry(
            state: null,
            isProviderAdmin: true,
            command.ProviderId,
            command.ModelId);
        JsonSerializer.Serialize(inspection).ShouldNotContain(PoisonSecret);
        inspection.Entries.ShouldBeEmpty();
    }

    [Fact]
    public void A_safe_configuration_reference_is_the_only_secret_adjacent_field_on_the_created_event()
    {
        var command = ValidCreate(configurationReferenceId: "cfg-openai-gpt4o");

        DomainResult result = ProviderCatalogAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        string serialized = SerializeEvents(result);
        serialized.ShouldContain("cfg-openai-gpt4o");
        serialized.ShouldNotContain("sk-");
        serialized.ShouldNotContain(PoisonSecret);
        serialized.ShouldNotContain("ApiKey", Case.Insensitive);
    }

    private static string SerializeEvents(DomainResult result)
        => string.Join(
            ",",
            result.Events.Select(payload => JsonSerializer.Serialize(payload, payload.GetType())));
}
