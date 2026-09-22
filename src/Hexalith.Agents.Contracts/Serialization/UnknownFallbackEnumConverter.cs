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
/// direction: a comma-delimited name list, an undefined ordinal, an integer outside <see cref="int"/> (including a
/// valid <see cref="long"/> or <see cref="ulong"/> token), and an unknown name all resolve to the sentinel,
/// so nothing that is not a declared member of <typeparamref name="TEnum"/> can ever be produced. A wide integer
/// is read and discarded; it is never cast down into the enum's range.
/// </para>
/// </remarks>
/// <typeparam name="TEnum">The enum being converted. Its zero value must be the <c>Unknown</c> sentinel.</typeparam>
public sealed class UnknownFallbackEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private static readonly bool _hasUnsignedBackingType
        = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(TEnum)))
            is TypeCode.Byte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64;

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            // A numeric value is accepted so a payload written by an older peer that serialized this enum
            // numerically still round-trips; an undefined ordinal degrades to the sentinel.
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out int ordinal))
                {
                    return FromOrdinal(ordinal);
                }

                // A JSON number that fits in long or ulong but not Int32 is a valid numeric token this
                // contract cannot represent. Read it so the value is consumed, then degrade. Do not cast
                // it to Int32: that would truncate a wide ordinal into a declared member.
                _ = reader.TryGetInt64(out _) || reader.TryGetUInt64(out _);
                return default;

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
    {
        if (ordinal < 0 && _hasUnsignedBackingType)
        {
            return default;
        }

        object value;
        try
        {
            value = Enum.ToObject(typeof(TEnum), ordinal);
        }
        catch (ArgumentException)
        {
            return default;
        }

        // Enum.ToObject truncates to the backing type, so 257 on a byte enum becomes Ready (1). The ordinal
        // must still equal the converted value or an undeclared payload would be produced as a defined member.
        return Enum.IsDefined(typeof(TEnum), value) && Convert.ToInt64(value) == ordinal
            ? (TEnum)value
            : default;
    }

    private static TEnum FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return default;
        }

        string trimmedName = name.Trim();

        // A peer that quoted a canonical ordinal is read exactly like a bare number, so "1" and 1 cannot
        // disagree. JSON numbers cannot carry a leading plus, surrounding whitespace, or leading zeroes, so the
        // quoted form must not accept those non-canonical spellings either.
        if (int.TryParse(trimmedName, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ordinal))
        {
            return string.Equals(name, ordinal.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
                ? FromOrdinal(ordinal)
                : default;
        }

        // A canonical or non-canonical integer outside Int32 must not fall through to Enum.TryParse, which
        // can accept a numeric string that does not fit the backing type. The token degrades to Unknown.
        if (long.TryParse(trimmedName, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _)
            || ulong.TryParse(trimmedName, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return default;
        }

        // Enum.TryParse also accepts a comma-delimited name list on a non-flags enum, which combines the ordinals
        // into a value that is not a declared member. Requiring the parsed value's own name to match the input
        // rejects that while preserving the case-insensitive, whitespace-tolerant compatibility of the converter
        // this replaces.
        return Enum.TryParse(trimmedName, ignoreCase: true, out TEnum parsed)
            && string.Equals(Enum.GetName(parsed), trimmedName, StringComparison.OrdinalIgnoreCase)
                ? parsed
                : default;
    }
}
