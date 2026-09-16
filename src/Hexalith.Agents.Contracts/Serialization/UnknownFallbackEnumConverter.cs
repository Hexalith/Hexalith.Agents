using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Serialization;

/// <summary>
/// Serializes an enum by name and degrades any unrecognized value to the zero <c>Unknown</c> sentinel instead of
/// throwing (Story 5.2). A client compiled against an older contract version therefore survives additively added
/// members rather than failing the whole response with a <see cref="JsonException"/>, which is what the documented
/// <c>Unknown = 0</c> compatibility guarantee promises.
/// </summary>
/// <remarks>
/// This guarantee is prospective: a consumer compiled against an earlier package that still uses the throwing
/// converter cannot be made tolerant by a newer producer.
/// <para>
/// Reading is deliberately tolerant and total: a name (in any casing, matching the case-insensitive behaviour of
/// the <see cref="JsonStringEnumConverter"/> this type replaces), an ordinal written as a number or as a quoted
/// number, and every structurally wrong token all resolve without throwing. It is never permissive in the other
/// direction: a comma-delimited name list, an undefined ordinal, and an unknown name all resolve to the sentinel,
/// so nothing that is not a declared member of <typeparamref name="TEnum"/> can ever be produced.
/// </para>
/// </remarks>
/// <typeparam name="TEnum">The enum being converted. Its zero value must be the <c>Unknown</c> sentinel.</typeparam>
public sealed class UnknownFallbackEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            // A numeric value is accepted so a payload written by an older peer that serialized this enum
            // numerically still round-trips; an undefined ordinal degrades to the sentinel.
            case JsonTokenType.Number:
                return reader.TryGetInt32(out int ordinal) ? FromOrdinal(ordinal) : default;

            case JsonTokenType.String:
                return FromName(reader.GetString());

            // A structurally wrong value is consumed whole before degrading, so one malformed member cannot
            // desynchronize the reader and fail the entire response the Unknown sentinel exists to preserve.
            case JsonTokenType.StartObject:
            case JsonTokenType.StartArray:
                reader.Skip();
                return default;

            default:
                return default;
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(Enum.GetName(value) ?? Enum.GetName(default(TEnum)) ?? "Unknown");
    }

    private static TEnum FromOrdinal(int ordinal)
        => Enum.IsDefined(typeof(TEnum), ordinal)
            ? (TEnum)Enum.ToObject(typeof(TEnum), ordinal)
            : default;

    private static TEnum FromName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return default;
        }

        // A peer that quoted the ordinal is read exactly like a bare number, so "1" and 1 cannot disagree.
        if (int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ordinal))
        {
            return FromOrdinal(ordinal);
        }

        // Enum.TryParse also accepts a comma-delimited name list on a non-flags enum, which combines the ordinals
        // into a value that is not a declared member. Requiring the parsed value's own name to match the input
        // rejects that while preserving the case-insensitive compatibility of the converter this replaces.
        return Enum.TryParse(name, ignoreCase: true, out TEnum parsed)
            && string.Equals(Enum.GetName(parsed), name, StringComparison.OrdinalIgnoreCase)
                ? parsed
                : default;
    }
}
