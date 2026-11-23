namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Break one condition on an otherwise usable catalog read for both invocation paths.</summary>
public static class GenerationCatalogRefusals
{
    private static readonly Dictionary<string, Func<ProviderCatalogEntryView, ProviderCatalogEntryView>> _changes = new()
    {
        ["foreign provider"] = entry => entry with { ProviderId = "different-provider" },
        ["case-mismatched model"] = entry => entry with { ModelId = entry.ModelId.ToUpperInvariant() },
        ["foreign disabled model"] = entry => entry with { ModelId = "different-model", Status = ProviderModelStatus.Disabled },
        ["regressed capability"] = entry => entry with { CapabilityVersion = 1 },
        ["unconfigured"] = entry => entry with { ConfigurationState = ProviderConfigurationState.NotConfigured },
        ["tenant ineligible"] = entry => entry with { IsSelectableForNewActiveUse = false },
        ["unpriced"] = entry => entry with { Pricing = null },
        ["invalid currency"] = entry => entry with { Pricing = entry.Pricing! with { Currency = "invalid" } },
        ["zero input price"] = entry => entry with { Pricing = entry.Pricing! with { InputTokenUnitPrice = 0 } },
        ["zero output price"] = entry => entry with { Pricing = entry.Pricing! with { OutputTokenUnitPrice = 0 } },
        ["unversioned price"] = entry => entry with { Pricing = entry.Pricing! with { PricingVersion = 0 } },
        ["zero output limit"] = entry => entry with { MaxOutputTokenLimit = 0 },
        ["no input space"] = entry => entry with { MaxOutputTokenLimit = entry.ContextWindowTokenLimit },
        ["missing transport limits"] = entry => entry with { TimeoutPolicy = null! },
        ["zero timeout"] = entry => entry with { TimeoutPolicy = new(0, 0) },
        ["excessive timeout"] = entry => entry with { TimeoutPolicy = new(600_001, 0) },
        ["negative retries"] = entry => entry with { TimeoutPolicy = new(1, -1) },
        ["excessive retries"] = entry => entry with { TimeoutPolicy = new(1, 11) },
    };

    public static TheoryData<string> Cases => new(_changes.Keys);

    internal static ProviderCatalogEntryView Apply(string defect, ProviderCatalogEntryView entry) => _changes[defect](entry);
}
