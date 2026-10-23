using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Serialization;

/// <summary>Retains a legacy numeric writer while providing the tolerant Unknown-zero reader.</summary>
/// <typeparam name="TEnum">The signed enum whose original wire encoding is numeric.</typeparam>
internal sealed class UnknownFallbackNumericEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    private readonly UnknownFallbackEnumConverter<TEnum> _reader = new();

    /// <summary>Initializes a converter for attribute-based serializer activation.</summary>
    public UnknownFallbackNumericEnumConverter()
    {
    }

    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => _reader.Read(ref reader, typeToConvert, options);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(options);
        // Before the type had an attribute, an explicitly registered string converter controlled its writer.
        // Preserve that options-level encoding as well as the default numeric encoding.
        if (options.Converters.Any(converter => converter is JsonStringEnumConverter or JsonStringEnumConverter<TEnum>))
        {
            _reader.Write(writer, value, options);
            return;
        }
        writer.WriteNumberValue(Enum.IsDefined(value) ? Convert.ToInt64(value) : 0);
    }

    /// <inheritdoc />
    public override TEnum ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => _reader.ReadAsPropertyName(ref reader, typeToConvert, options);

    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        => _reader.WriteAsPropertyName(writer, value, options);
}
