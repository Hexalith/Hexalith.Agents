namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Serialization;
using Hexalith.Agents.Server.Projections;

using Shouldly;

/// <summary>
/// Serialization conformance guard for the server's own <see cref="JsonSerializerOptions"/> (Story 5.2).
/// </summary>
/// <remarks>
/// <see cref="JsonSerializerOptions.Converters"/> is resolved <em>before</em> a type-level
/// <see cref="JsonConverterAttribute"/>, so an options bag that registers a bare
/// <see cref="JsonStringEnumConverter"/> silently overrides — and reverses — the <c>Unknown = 0</c> degradation
/// every public operation enum declares. Only the pairing is observable from outside, so the guard is structural:
/// it scans every static <see cref="JsonSerializerOptions"/> field in the server assembly, which covers options
/// bags added after this story as well as the five that exist today. Both the non-generic
/// <see cref="JsonStringEnumConverter"/> factory and its generic <see cref="JsonStringEnumConverter{TEnum}"/>
/// sibling count — the latter is a separate type, not a subclass, so matching only the former would let a bag
/// using it slip past unchecked.
/// </remarks>
public sealed class ServerSerializationConformanceTests
{
    [Fact]
    public void EveryServerJsonOptionsBagPairsTheStringEnumConverterWithTheUnknownFallback()
    {
        List<string> offenders = [];
        List<string> inspected = [];

        foreach (Type type in typeof(AgentSetupProjectionFold).Assembly.GetTypes())
        {
            const BindingFlags Flags = BindingFlags.Static
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly;

            foreach (FieldInfo field in type.GetFields(Flags))
            {
                if (field.FieldType != typeof(JsonSerializerOptions)
                    || field.GetValue(null) is not JsonSerializerOptions options)
                {
                    continue;
                }

                int stringConverter = IndexOf(options, IsStringEnumConverter);
                if (stringConverter < 0)
                {
                    continue;
                }

                inspected.Add($"{type.Name}.{field.Name}");
                int fallback = IndexOf(options, converter => converter is UnknownFallbackEnumConverterFactory);
                if (fallback < 0 || fallback > stringConverter)
                {
                    offenders.Add($"{type.Name}.{field.Name}");
                    continue;
                }

                JsonSerializer.Deserialize<AgentSetupTruthState>(
                    "\"SomeMemberAddedLater\"",
                    options).ShouldBe(AgentSetupTruthState.Unknown, $"{type.Name}.{field.Name}");
            }
        }

        // Fails loudly if the guard itself stops finding anything to guard.
        inspected.ShouldNotBeEmpty();
        offenders.ShouldBeEmpty(
            $"These options bags would reinstate the throwing enum converter: {string.Join(", ", offenders)}.");
    }

    // JsonStringEnumConverter<TEnum> is a sibling of JsonStringEnumConverter, not a subclass, so a type check
    // against the non-generic factory alone silently skips a bag that registers the generic one.
    private static bool IsStringEnumConverter(JsonConverter converter)
    {
        if (converter is JsonStringEnumConverter)
        {
            return true;
        }

        Type type = converter.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(JsonStringEnumConverter<>);
    }

    private static int IndexOf(JsonSerializerOptions options, Func<JsonConverter, bool> predicate)
    {
        for (int index = 0; index < options.Converters.Count; index++)
        {
            if (predicate(options.Converters[index]))
            {
                return index;
            }
        }

        return -1;
    }
}
