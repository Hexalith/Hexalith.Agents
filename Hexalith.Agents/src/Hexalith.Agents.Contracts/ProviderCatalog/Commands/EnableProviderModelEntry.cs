namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>
/// Enables an existing provider/model catalog entry so it becomes selectable for new active Agent
/// configuration (AC1, AC2).
/// </summary>
/// <param name="ProviderId">Stable provider identifier (non-empty).</param>
/// <param name="ModelId">Stable model identifier (non-empty).</param>
public record EnableProviderModelEntry(string ProviderId, string ModelId);
