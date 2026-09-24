using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Reserved platform scope and deterministic provider/model stream addresses.</summary>
public static class ProviderCatalogIdentity
{
    /// <summary>The provider catalog EventStore domain.</summary>
    public const string Domain = "provider-catalog";

    /// <summary>The reserved platform tenant.</summary>
    public const string PlatformTenantId = "system";

    /// <summary>Builds one stable stream identifier for a provider/model pair.</summary>
    public static string EntryId(string providerId, string modelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        byte[] bytes = Encoding.UTF8.GetBytes($"{providerId.Length}:{providerId}{modelId.Length}:{modelId}");
        return $"entry-{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }

    /// <summary>Returns whether the envelope targets the reserved platform entry stream.</summary>
    public static bool IsPlatformEntry(string tenantId, string aggregateId, string providerId, string modelId)
        => string.Equals(tenantId, PlatformTenantId, StringComparison.Ordinal)
            && (string.IsNullOrWhiteSpace(providerId) || string.IsNullOrWhiteSpace(modelId)
                || string.Equals(aggregateId, EntryId(providerId, modelId), StringComparison.Ordinal));
}
