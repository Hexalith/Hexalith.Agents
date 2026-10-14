using Hexalith.Agents.Contracts.Security;

namespace Hexalith.Agents.Contracts.SecurityEventLog;

/// <summary>Daily security partition routing from authenticated observations, never claimed target tenants.</summary>
public static class SecurityEventLogIdentity
{
    /// <summary>Gets the private security recording domain.</summary>
    public const string Domain = "security-event-log";
    /// <summary>Derives the exact daily stream identity.</summary>
    public static string Create(string routingTenantId, string utcDay)
        => AgentsSecurityIdentity.Derive("security-event-log", routingTenantId, utcDay);
}
