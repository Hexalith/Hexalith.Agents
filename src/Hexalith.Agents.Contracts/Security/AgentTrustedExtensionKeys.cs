namespace Hexalith.Agents.Contracts.Security;

/// <summary>Closed reserved trust namespaces; clients cannot choose a new spelling to assert authority.</summary>
public static class AgentTrustedExtensionKeys
{
    /// <summary>Checks trust metadata case-insensitively before copying any public extensions.</summary>
    public static bool IsReserved(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.StartsWith("actor:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("trust:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("security:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("replay:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("agent:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("approver:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("party:", StringComparison.OrdinalIgnoreCase)
            || key.StartsWith("audit:", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("Validation", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith(":validation", StringComparison.OrdinalIgnoreCase);
    }
}
