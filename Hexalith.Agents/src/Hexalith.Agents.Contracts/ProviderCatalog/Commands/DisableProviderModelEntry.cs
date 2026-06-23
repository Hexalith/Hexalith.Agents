namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>
/// Disables an existing provider/model catalog entry. A disabled entry is no longer selectable for new active
/// Agent configuration, but its historical catalog state remains inspectable (AC2).
/// </summary>
/// <param name="ProviderId">Stable provider identifier (non-empty).</param>
/// <param name="ModelId">Stable model identifier (non-empty).</param>
public record DisableProviderModelEntry(string ProviderId, string ModelId);
