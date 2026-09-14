using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Serialization;

/// <summary>
/// Serializes an enum by name and degrades any unrecognized value to the zero <c>Unknown</c> sentinel instead of
/// throwing (Story 5.2). A client compiled against an older contract version therefore survives additively added
/// members rather than failing the whole response with a <see cref="JsonException"/>, which is what the documented
/// <c>Unknown = 0</c> compatibility guarantee promises.
/// </summary>
/// <typeparam name="TEnum">The enum being converted. Its zero value must be the <c>Unknown</c> sentinel.</typeparam>
public sealed class UnknownFallbackEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return default;

            // A numeric value is accepted so a payload written by an older peer that serialized this enum
            // numerically still round-trips; an undefined ordinal degrades to the sentinel.
            case JsonTokenType.Number:
                return reader.TryGetInt32(out int ordinal) && Enum.IsDefined(typeof(TEnum), ordinal)
                    ? (TEnum)Enum.ToObject(typeof(TEnum), ordinal)
                    : default;

            case JsonTokenType.String:
                string? name = reader.GetString();

                // Enum.TryParse also accepts the numeric text form, which would bypass the IsDefined check above.
                return name is not null
                    && !int.TryParse(name, out _)
                    && Enum.TryParse(name, ignoreCase: false, out TEnum parsed)
                    ? parsed
                    : default;

            default:
                throw new JsonException($"Expected a string or number for {typeof(TEnum).Name}.");
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(Enum.GetName(value) ?? Enum.GetName(default(TEnum)));
    }
}
