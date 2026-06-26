using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal result of reading a single provider/model catalog entry for the Agent selection / activation
/// re-validation orchestration (Story 1.5; AC1, AC2, AC4). It carries the fail-closed inspection
/// <see cref="ProviderCatalogInspectionStatus"/> plus the safe <see cref="ProviderCatalogEntryView"/> (present only
/// on a <see cref="ProviderCatalogInspectionStatus.Success"/> read). No secret value, configuration reference
/// secret, or provider SDK type ever crosses this boundary — only the safe projection (AD-9, AD-14).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>AgentPartyValidationResult</c> from
/// Story 1.4). <see cref="Entry"/> is <see langword="null"/> for any non-success read, and a successful read with a
/// <see langword="null"/> entry is treated as a degraded/unavailable read by the orchestration (fail closed, AD-12).
/// </remarks>
/// <param name="Status">The fail-closed catalog inspection outcome.</param>
/// <param name="Entry">The safe entry projection (non-null only on a successful read), or <see langword="null"/>.</param>
public sealed record ProviderCatalogEntryReadResult(
    ProviderCatalogInspectionStatus Status,
    ProviderCatalogEntryView? Entry);
