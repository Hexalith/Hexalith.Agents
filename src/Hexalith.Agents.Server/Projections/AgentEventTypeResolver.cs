using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Hexalith.Agents.Contracts.Agent.Events;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Resolves a persisted Agent event type name to its public contract type. Only types under the Agent event
/// namespaces are resolvable, so a stray or hostile type name can never be materialized by the projection.
/// </summary>
public static class AgentEventTypeResolver
{
    private const string _eventNamespace = "Hexalith.Agents.Contracts.Agent.Events";

    private static readonly FrozenDictionary<string, Type> _byName = Build();

    /// <summary>Resolves the persisted event type name, or <see langword="null"/> when it is unknown.</summary>
    /// <param name="eventTypeName">The persisted event type name (short or fully qualified).</param>
    /// <returns>The resolved contract type, or <see langword="null"/>.</returns>
    public static Type? Resolve(string? eventTypeName)
        => string.IsNullOrWhiteSpace(eventTypeName)
            ? null
            : _byName.TryGetValue(eventTypeName, out Type? type) ? type : null;

    private static FrozenDictionary<string, Type> Build()
    {
        Dictionary<string, Type> map = new(StringComparer.Ordinal);
        Assembly contracts = typeof(AgentCreated).Assembly;

        foreach (Type type in contracts.GetExportedTypes().Where(IsAgentEventType))
        {
            map[type.Name] = type;
            if (type.FullName is { Length: > 0 } fullName)
            {
                map[fullName] = type;
            }
        }

        return map.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static bool IsAgentEventType(Type type)
        => type is { IsClass: true, IsAbstract: false }
            && type.Namespace is { } ns
            && (string.Equals(ns, _eventNamespace, StringComparison.Ordinal)
                || ns.StartsWith($"{_eventNamespace}.", StringComparison.Ordinal));
}
