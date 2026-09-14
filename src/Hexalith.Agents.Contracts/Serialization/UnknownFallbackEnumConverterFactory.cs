using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Serialization;

/// <summary>
/// Restores <see cref="UnknownFallbackEnumConverter{TEnum}"/> for every contract enum that declares it, inside a
/// <see cref="JsonSerializerOptions"/> that also registers a <see cref="JsonStringEnumConverter"/> (Story 5.2).
/// </summary>
/// <remarks>
/// <see cref="JsonSerializerOptions.Converters"/> is resolved <em>before</em> a type-level
/// <see cref="JsonConverterAttribute"/>, so a plain <c>Converters = { new JsonStringEnumConverter() }</c> silently
/// wins over the attribute and reinstates the throwing behaviour the <c>Unknown = 0</c> guarantee exists to
/// prevent. Registering this factory ahead of the string converter in the same list gives the attribute-declared
/// converter precedence again while leaving every other enum on the string converter, unchanged.
/// </remarks>
public sealed class UnknownFallbackEnumConverterFactory : JsonConverterFactory
{
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) => DeclaredConverterType(typeToConvert) is not null;

    /// <inheritdoc />
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => DeclaredConverterType(typeToConvert) is { } converterType
            ? (JsonConverter?)Activator.CreateInstance(converterType)
            : null;

    // Only the enum itself is claimed, exactly as JsonStringEnumConverter does: the built-in nullable factory
    // resolves Nullable<TEnum> through this same list, so claiming it here would hand back a converter whose type
    // does not match and be rejected.
    private static Type? DeclaredConverterType(Type? typeToConvert)
    {
        if (typeToConvert is not { IsEnum: true })
        {
            return null;
        }

        Type? declared = typeToConvert.GetCustomAttribute<JsonConverterAttribute>(inherit: false)?.ConverterType;
        return declared is { IsGenericType: true }
            && declared.GetGenericTypeDefinition() == typeof(UnknownFallbackEnumConverter<>)
                ? declared
                : null;
    }
}
