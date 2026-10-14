using System.Globalization;
using System.Security.Cryptography;
using System.Text;

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
        string[] components = ["trusted-envelope-replay", issuer, nonce];
        string framed = string.Join('\u001f', components.Select(component => Encoding.UTF8.GetByteCount(component).ToString(CultureInfo.InvariantCulture) + ":" + component));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(framed)));
    }
}
