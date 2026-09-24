using System;
using System.Collections.Generic;
using System.Linq;

namespace Hexalith.Agents.Server.Projections;

/// <summary>Bounds the successful command identities retained by a projected stream.</summary>
public static class ProjectedCommandIdentityWindow
{
    /// <summary>Maximum number of recent successful command identities retained per stream.</summary>
    public const int Capacity = 64;

    /// <summary>Trims older identities and adds a newly projected command identity once.</summary>
    /// <param name="identities">The stream's mutable recent identity list.</param>
    /// <param name="messageId">The newly projected command identity.</param>
    public static void Add(List<string> identities, string? messageId)
    {
        ArgumentNullException.ThrowIfNull(identities);
        if (string.IsNullOrWhiteSpace(messageId))
        {
            Trim(identities);
            return;
        }

        if (!identities.Contains(messageId, StringComparer.Ordinal))
        {
            identities.Add(messageId);
        }

        Trim(identities);
    }

    /// <summary>Removes identities older than the retained window.</summary>
    /// <param name="identities">The stream's mutable recent identity list.</param>
    public static void Trim(List<string> identities)
    {
        ArgumentNullException.ThrowIfNull(identities);
        if (identities.Count > Capacity)
        {
            identities.RemoveRange(0, identities.Count - Capacity);
        }
    }
}
