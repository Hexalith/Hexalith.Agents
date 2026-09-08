namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Administrator-supplied versioned pricing units and currency for a governed provider/model catalog entry
/// (Story 5.3). This is catalog truth, never a Provider SDK quote, FX conversion, or live price feed.
/// </summary>
/// <param name="Currency">ISO 4217 currency code (three letters).</param>
/// <param name="InputTokenUnitPrice">Non-negative unit price for input/prompt tokens in <paramref name="Currency"/>.</param>
/// <param name="OutputTokenUnitPrice">Non-negative unit price for output/completion tokens in <paramref name="Currency"/>.</param>
/// <param name="PricingVersion">
/// Monotonic pricing version (1 on create, +1 when units or currency change; unchanged when only other metadata
/// or enablement changes). Zero on a command means the aggregate assigns the next version.
/// </param>
public sealed record ProviderModelPricing(
    string Currency,
    decimal InputTokenUnitPrice,
    decimal OutputTokenUnitPrice,
    int PricingVersion);
