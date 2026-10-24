using System.Reflection;
using System.Text.Json;

using Hexalith.Agents.Contracts.Agent.Events;

namespace Hexalith.Agents.Server.Projections;

/// <summary>Checks durable event structure without reapplying current command eligibility policies.</summary>
internal static class AgentEventPayloadStructure
{
    /// <summary>Checks required constructor members, nested graphs and recorded version bounds.</summary>
    /// <param name="value">The event JSON value.</param>
    /// <param name="type">The resolved contract type.</param>
    /// <returns>Whether the payload has a supported structural shape.</returns>
    internal static bool IsValid(JsonElement value, Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        if (type.IsEnum)
        {
            return value.ValueKind is JsonValueKind.String or JsonValueKind.Number;
        }

        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
        {
            return true; // Deserialization checks scalar types; future enum values remain historical data.
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
        {
            return value.ValueKind == JsonValueKind.Array
                && value.EnumerateArray().All(item => IsValid(item, type.GetGenericArguments()[0]));
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        Dictionary<string, JsonElement> members = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty member in value.EnumerateObject())
        {
            if (!members.TryAdd(member.Name, member.Value))
            {
                return false;
            }
        }

        ConstructorInfo? constructor = type.GetConstructors()
            .OrderByDescending(item => item.GetParameters().Length).FirstOrDefault();
        if (constructor is null)
        {
            return false;
        }

        NullabilityInfoContext nullability = new();
        foreach (ParameterInfo parameter in constructor.GetParameters())
        {
            bool optional = parameter.HasDefaultValue
                || nullability.Create(parameter).ReadState == NullabilityState.Nullable;
            if (!members.TryGetValue(parameter.Name!, out JsonElement member)
                || member.ValueKind == JsonValueKind.Null)
            {
                if (!optional)
                {
                    return false;
                }

                continue;
            }

            if (parameter.ParameterType == typeof(string)
                && parameter.Name!.EndsWith("Id", StringComparison.Ordinal)
                && (member.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(member.GetString())))
            {
                return false;
            }

            if (!IsValid(member, parameter.ParameterType))
            {
                return false;
            }

            if (parameter.Name!.EndsWith("Version", StringComparison.Ordinal)
                && (member.ValueKind != JsonValueKind.Number || !member.TryGetInt32(out int version)
                    || version < (parameter.Name == "InstructionsVersion" ? 0 : 1)))
            {
                return false;
            }
        }

        // These two lifecycle events predate versioned payloads. Omission and zero remain supported.
        return (type != typeof(AgentActivated) && type != typeof(AgentDisabled))
            || !members.TryGetValue("ConfigurationVersion", out JsonElement lifecycleVersion)
            || (lifecycleVersion.ValueKind == JsonValueKind.Number && lifecycleVersion.TryGetInt32(out int recordedVersion) && recordedVersion >= 0);
    }
}
