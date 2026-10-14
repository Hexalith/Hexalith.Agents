using Hexalith.Agents.Contracts.Security;

namespace Hexalith.Agents.Contracts.TrustedEnvelopeReplay;

/// <summary>Issuer-wide reserved-system stream identity using AD-29 UTF-8 framing.</summary>
public static class TrustedEnvelopeReplayIdentity
{
    /// <summary>Gets the reserved replay tenant.</summary>
    public const string Tenant = "system";
    /// <summary>Gets the replay domain.</summary>
    public const string Domain = "trusted-envelope-replay";
    /// <summary>Derives one global issuer/nonce identity; target tenant is deliberately excluded.</summary>
    public static string Create(string issuer, string nonce)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        return AgentsSecurityIdentity.Derive("trusted-envelope-replay", issuer, nonce);
    }
}
