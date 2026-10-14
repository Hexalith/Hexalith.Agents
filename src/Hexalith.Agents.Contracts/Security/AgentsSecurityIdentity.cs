using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Hexalith.Agents.Contracts.Security;

/// <summary>AD-29 purpose-tagged identities using the shipped character-length framing and UTF-8 bytes.</summary>
public static class AgentsSecurityIdentity
{
    /// <summary>Hashes only opaque identity components; sensitive payloads require a custody-owned HMAC.</summary>
    public static string Derive(params string[] components)
    {
        ArgumentNullException.ThrowIfNull(components);
        var text = new StringBuilder();
        foreach (string value in components)
        {
            ArgumentNullException.ThrowIfNull(value);
            text.Append(value.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(value).Append('\u001f');
        }

        return Convert.ToHexStringLower(SHA256.HashData(new UTF8Encoding(false, true).GetBytes(text.ToString())));
    }
}
