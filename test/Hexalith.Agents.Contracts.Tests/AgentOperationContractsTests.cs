using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.Serialization;

using Shouldly;

namespace Hexalith.Agents.Contracts.Tests;

/// <summary>
/// Public operation-envelope contract tests for Story 4.1.
/// </summary>
public sealed class AgentOperationContractsTests
{
    private static readonly string[] _poisonValues =
    [
        "prompt text",
        "generated content",
        "edited content",
        "provider secret",
        "raw provider payload",
        "System.InvalidOperationException",
        "stack trace",
        "other-tenant-id",
        "EventStore stream",
    ];

    [Fact]
    public void Operation_result_round_trips_status_enums_by_name()
    {
        AgentOperationResult<string> original = AgentOperationResult<string>.Succeeded("ok");

        string json = JsonSerializer.Serialize(original);
        AgentOperationResult<string>? roundTrip = JsonSerializer.Deserialize<AgentOperationResult<string>>(json);

        json.ShouldContain("\"Status\":\"Succeeded\"");
        roundTrip.ShouldNotBeNull();
        roundTrip.Status.ShouldBe(AgentOperationStatus.Succeeded);
        roundTrip.Value.ShouldBe("ok");
    }

    [Fact]
    public void Operation_error_factory_uses_safe_messages()
    {
        AgentOperationError error = AgentOperationError.FromCode(AgentOperationErrorCode.Unavailable);

        error.Code.ShouldBe(AgentOperationErrorCode.Unavailable);
        error.Message.ShouldBe("The operation is currently unavailable.");
    }

    [Fact]
    public void EveryDeclaredErrorCodeCarriesItsOwnMessageAndOnlyUnknownFallsBack()
    {
        // Deleting a single switch arm would otherwise pass: the code survives and the message silently collapses
        // to the generic fallback, which the server tests never read.
        const string Fallback = "The operation failed.";
        AgentOperationError.FromCode(AgentOperationErrorCode.Unknown).Message.ShouldBe(Fallback);
        AgentOperationError.FromCode(AgentOperationErrorCode.UnableToVerify)
            .Message
            .ShouldBe("The operation outcome could not be verified.");

        List<string> messages = [];
        foreach (AgentOperationErrorCode code in Enum.GetValues<AgentOperationErrorCode>())
        {
            if (code == AgentOperationErrorCode.Unknown)
            {
                continue;
            }

            string message = AgentOperationError.FromCode(code).Message;
            message.ShouldNotBe(Fallback, $"{code} must carry its own safe message.");
            messages.ShouldNotContain(message, $"{code} must not reuse another code's message.");
            messages.Add(message);
        }
    }

    [Fact]
    public void Operation_error_factory_does_not_serialize_poison_values()
    {
        string json = JsonSerializer.Serialize(AgentOperationResult.Unavailable());

        foreach (string poison in _poisonValues)
        {
            json.ShouldNotContain(poison, Case.Insensitive);
        }
    }

    // Derived, never hand-maintained: every Unknown = 0 enum reachable from this story's own public read/write
    // payloads and from the public operation envelope.
    // An enum added to either graph later is covered without editing this file, which is the point — a hand-listed
    // subset is exactly how the setup-view enums were missed. They share one sentinel, one by-name encoding, and
    // one tolerant reader, so every property below iterates this set.
    // AgentInspectionStatus is excluded by the Unknown = 0 rule itself: its zero is Success, so degrading an
    // unrecognized value there would fail open. The AgentInteraction and ProviderCatalog enums are a separate,
    // deferred migration and are excluded by namespace.
    private static readonly string[] _deferredEnumNamespaces =
    [
        "Hexalith.Agents.Contracts.AgentInteraction",
        "Hexalith.Agents.Contracts.ProviderCatalog",
    ];

    private static readonly Type[] _setupContractEnumTypes = DiscoverEnumTypes(
        typeof(AgentSetupResult),
        typeof(AgentSetupWriteResult));

    private static readonly Type[] _publicOperationEnumTypes = DiscoverPublicUnknownSentinelEnums();

    private static Type[] DiscoverEnumTypes(params Type[] roots)
    {
        Assembly contracts = typeof(AgentOperationResult).Assembly;
        HashSet<Type> visited = [];
        SortedDictionary<string, Type> found = [];
        Queue<Type> pending = new(roots);

        while (pending.Count > 0)
        {
            Type dequeued = pending.Dequeue();
            Type candidate = Nullable.GetUnderlyingType(dequeued) ?? dequeued;
            if (!visited.Add(candidate))
            {
                continue;
            }

            if (candidate.IsArray)
            {
                pending.Enqueue(candidate.GetElementType()!);
                continue;
            }

            if (candidate.IsGenericType)
            {
                foreach (Type argument in candidate.GetGenericArguments())
                {
                    pending.Enqueue(argument);
                }
            }

            if (candidate.IsEnum)
            {
                found[candidate.FullName!] = candidate;
                continue;
            }

            if (candidate.Assembly != contracts || candidate.IsGenericTypeDefinition || candidate.IsGenericParameter)
            {
                continue;
            }

            foreach (PropertyInfo property in candidate.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                pending.Enqueue(property.PropertyType);
            }
        }

        return [.. found.Values];
    }

    private static Type[] DiscoverPublicUnknownSentinelEnums()
    {
        Assembly contracts = typeof(AgentOperationResult).Assembly;
        SortedDictionary<string, Type> found = [];

        // The story's own read payload is walked transitively; the public operation status terms are taken as
        // declared, so an unrelated story's view type does not silently widen this story's guard.
        foreach (Type enumType in _setupContractEnumTypes
            .Where(type => Enum.GetName(type, 0) == "Unknown"
                && !_deferredEnumNamespaces.Contains(type.Namespace)))
        {
            found[enumType.FullName!] = enumType;
        }

        foreach (Type root in contracts.GetTypes()
            .Where(type => type.IsPublic
                && type.IsEnum
                && type.Namespace == typeof(AgentOperationResult).Namespace))
        {
            if (Enum.GetName(root, 0) == "Unknown")
            {
                found[root.FullName!] = root;
            }
        }

        return [.. found.Values];
    }

    [Fact]
    public void TheDiscoveredEnumSetCoversTheWholeSetupViewPayload()
    {
        // Independent of the walk above: read the setup payload's own enum-typed members directly. If the walk
        // ever stops traversing (a wrapper record, a collection shape), the theories would silently run on fewer
        // types and prove nothing; this fails instead.
        Type[] declared = new[] { typeof(AgentSetupView), typeof(AgentStatusView) }
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Select(property => property.PropertyType)
            .SelectMany(type => type.IsGenericType ? type.GetGenericArguments() : [type])
            .Select(type => Nullable.GetUnderlyingType(type) ?? type)
            .Where(type => type.IsEnum && Enum.GetName(type, 0) == "Unknown")
            .Distinct()
            .ToArray();

        declared.ShouldNotBeEmpty();
        foreach (Type enumType in declared)
        {
            _publicOperationEnumTypes.ShouldContain(enumType, $"{enumType.Name} is on the setup payload.");
        }
    }

    [Fact]
    public void TheDiscoveredEnumSetCoversTheWholeSetupWriteAcceptancePayload()
    {
        // Independent of the discovery roots above: the write receipt is also part of Story 5.2's setup payload.
        // If AgentCommandAcceptance stops being traversed, the compatibility theories must fail rather than
        // silently omitting AgentSetupWriteEffect.
        Type[] declared = typeof(AgentCommandAcceptance)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType)
            .Where(type => type.IsEnum && Enum.GetName(type, 0) == "Unknown")
            .Distinct()
            .ToArray();

        declared.ShouldContain(typeof(AgentSetupWriteEffect));
        foreach (Type enumType in declared)
        {
            _publicOperationEnumTypes.ShouldContain(enumType, $"{enumType.Name} is on the setup write acceptance payload.");
        }
    }

    [Fact]
    public void Every_unknown_fallback_converter_is_self_typed_and_has_unknown_at_zero()
    {
        Type[] guardedEnums = typeof(AgentOperationResult).Assembly.GetTypes()
            .Where(type => type.IsEnum)
            .Where(type => type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType is { } converter
                && converter.IsGenericType
                && converter.GetGenericTypeDefinition() == typeof(UnknownFallbackEnumConverter<>))
            .ToArray();

        guardedEnums.ShouldNotBeEmpty();
        foreach (Type enumType in guardedEnums)
        {
            Type converterType = enumType.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType
                ?? throw new InvalidOperationException($"{enumType.FullName} has no fallback converter type.");
            converterType.GetGenericArguments().Single().ShouldBe(
                enumType,
                $"{enumType.FullName} must use its own fallback converter.");
            Enum.GetName(enumType, 0).ShouldBe(
                "Unknown",
                $"{enumType.FullName} must fail closed to an Unknown zero sentinel.");
        }
    }

    public static TheoryData<Type> PublicOperationEnumTypes()
    {
        TheoryData<Type> data = [];
        foreach (Type enumType in _publicOperationEnumTypes)
        {
            data.Add(enumType);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PublicOperationEnumTypes))]
    public void Operation_enums_use_unknown_zero_and_serialize_by_name(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);
        Enum.ToObject(enumType, 0).ToString().ShouldBe("Unknown", $"{enumType.Name} must reserve 0 for Unknown.");

        object nonZero = Enum.GetValues(enumType).Cast<object>().First(value => Convert.ToInt32(value) != 0);
        string json = JsonSerializer.Serialize(nonZero, enumType);
        json.ShouldBe($"\"{nonZero}\"", $"{enumType.Name} must serialize by enum name.");
    }

    [Theory]
    [MemberData(nameof(UnrecognizedEnumPayloads))]
    public void UnrecognizedOperationEnumValuesDegradeToUnknownInsteadOfThrowing(Type enumType, string json)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        // An older client must survive additive enum members. A plain JsonStringEnumConverter throws on an
        // unrecognized name, which would fail the whole response instead of honouring the Unknown = 0 contract.
        object? value = JsonSerializer.Deserialize(json, enumType);

        Convert.ToInt32(value.ShouldNotBeNull()).ShouldBe(0, $"{enumType.Name} must degrade '{json}' to Unknown.");
    }

    public static TheoryData<Type, string> UnrecognizedEnumPayloads()
    {
        TheoryData<Type, string> data = [];
        string[] payloads =
        [
            "\"SomeMemberAddedLater\"",
            "9999",
            "\"9999\"",
            "2147483648",
            "\"2147483648\"",
            "9223372036854775808",
            "\"9223372036854775808\"",
            "18446744073709551615",
            "\"18446744073709551615\"",
            "null",
            "true",
            "{}",
            "[]",
        ];

        foreach (Type enumType in _publicOperationEnumTypes)
        {
            foreach (string payload in payloads)
            {
                data.Add(enumType, payload);
            }

            // Enum.TryParse accepts a comma-delimited name list even on a non-flags enum, combining the ordinals
            // into a value that is not a declared member (Applied|AlreadyApplied is ordinal 3, which nothing
            // declares). Nothing but a declared member may ever be produced.
            string[] nonZeroNames = Enum.GetValues(enumType)
                .Cast<object>()
                .Where(value => Convert.ToInt32(value) != 0)
                .Select(value => value.ToString()!)
                .ToArray();
            if (nonZeroNames.Length < 2)
            {
                throw new InvalidOperationException(
                    $"Compatibility enum '{enumType.FullName}' must declare at least two non-zero members for the comma-list guard.");
            }

            data.Add(enumType, $"\"{nonZeroNames[0]}, {nonZeroNames[1]}\"");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PublicOperationEnumTypes))]
    public void AQuotedOrdinalReadsExactlyLikeABareOrdinal(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        // Both forms are legacy numeric encodings of the same value; disagreeing on one of them would turn a
        // recognized member into Unknown purely because a peer quoted it.
        foreach (object declared in Enum.GetValues(enumType))
        {
            int ordinal = Convert.ToInt32(declared);
            JsonSerializer.Deserialize($"{ordinal}", enumType).ShouldBe(declared, enumType.Name);
            JsonSerializer.Deserialize($"\"{ordinal}\"", enumType).ShouldBe(declared, enumType.Name);
        }
    }

    [Theory]
    [MemberData(nameof(PublicOperationEnumTypes))]
    public void NoncanonicalQuotedOrdinalsDegradeToUnknown(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        int ordinal = Enum.GetValues(enumType)
            .Cast<object>()
            .Select(Convert.ToInt32)
            .First(value => value > 0);

        string[] payloads =
        [
            $"\"+{ordinal}\"",
            $"\" {ordinal}\"",
            $"\"{ordinal} \"",
            $"\"0{ordinal}\"",
        ];

        foreach (string payload in payloads)
        {
            object value = JsonSerializer.Deserialize(payload, enumType).ShouldNotBeNull();
            Convert.ToInt32(value).ShouldBe(0, $"{enumType.Name} must reject noncanonical ordinal {payload}.");
        }
    }

    [Fact]
    public void ShippedSetupEnumsKeepTheirPinnedOrdinals()
    {
        HashSet<Type> pinnedEnumTypes = [];

        void Pin<TEnum>(params (TEnum Member, int Ordinal)[] expected)
            where TEnum : struct, Enum
        {
            pinnedEnumTypes.Add(typeof(TEnum)).ShouldBeTrue($"{typeof(TEnum).Name} is pinned more than once.");
            AssertPinnedOrdinals(expected);
        }

        Pin(
            (AgentSetupWriteEffect.Unknown, 0),
            (AgentSetupWriteEffect.Applied, 1),
            (AgentSetupWriteEffect.AlreadyApplied, 2));
        Pin(
            (AgentOperationStatus.Unknown, 0),
            (AgentOperationStatus.Succeeded, 1),
            (AgentOperationStatus.Pending, 2),
            (AgentOperationStatus.Checking, 3),
            (AgentOperationStatus.Degraded, 4),
            (AgentOperationStatus.NotAuthorized, 5),
            (AgentOperationStatus.ValidationFailed, 6),
            (AgentOperationStatus.NotFound, 7),
            (AgentOperationStatus.Conflict, 8),
            (AgentOperationStatus.Stale, 9),
            (AgentOperationStatus.Unavailable, 10),
            (AgentOperationStatus.Rejected, 11),
            (AgentOperationStatus.Blocked, 12),
            (AgentOperationStatus.UnableToVerify, 13));
        Pin(
            (AgentOperationErrorCode.Unknown, 0),
            (AgentOperationErrorCode.NotAuthorized, 1),
            (AgentOperationErrorCode.ValidationFailed, 2),
            (AgentOperationErrorCode.NotFound, 3),
            (AgentOperationErrorCode.Conflict, 4),
            (AgentOperationErrorCode.Stale, 5),
            (AgentOperationErrorCode.Unavailable, 6),
            (AgentOperationErrorCode.Rejected, 7),
            (AgentOperationErrorCode.Blocked, 8),
            (AgentOperationErrorCode.UnableToVerify, 9));
        Pin(
            (AgentResponseMode.Unknown, 0),
            (AgentResponseMode.Automatic, 1),
            (AgentResponseMode.Confirmation, 2));
        Pin(
            (AgentLifecycleStatus.Unknown, 0),
            (AgentLifecycleStatus.Draft, 1),
            (AgentLifecycleStatus.Active, 2),
            (AgentLifecycleStatus.Disabled, 3));
        Pin(
            (AgentInspectionStatus.Success, 0),
            (AgentInspectionStatus.NotAuthorized, 1),
            (AgentInspectionStatus.AgentNotFound, 2),
            (AgentInspectionStatus.Unavailable, 3));
        Pin(
            (ApproverPolicyBasisDisclosure.Unknown, 0),
            (ApproverPolicyBasisDisclosure.UserVisible, 1),
            (ApproverPolicyBasisDisclosure.OperatorOnly, 2),
            (ApproverPolicyBasisDisclosure.Redacted, 3),
            (ApproverPolicyBasisDisclosure.Omitted, 4));
        Pin(
            (AgentSetupFreshness.Unknown, 0),
            (AgentSetupFreshness.Current, 1),
            (AgentSetupFreshness.Stale, 2));
        Pin(
            (AgentSetupTruthState.Unknown, 0),
            (AgentSetupTruthState.Submitted, 1),
            (AgentSetupTruthState.AuthoritativePending, 2),
            (AgentSetupTruthState.ProjectionConfirmed, 3));
        Pin(
            (AgentSetupWriteStatus.Submitted, 0),
            (AgentSetupWriteStatus.NotAuthorized, 1),
            (AgentSetupWriteStatus.NotFound, 2),
            (AgentSetupWriteStatus.ValidationFailed, 3),
            (AgentSetupWriteStatus.Conflict, 4),
            (AgentSetupWriteStatus.Unavailable, 5),
            (AgentSetupWriteStatus.AlreadyApplied, 6),
            (AgentSetupWriteStatus.AwaitingProjection, 7),
            (AgentSetupWriteStatus.UnableToVerify, 8));
        Pin(
            (AgentActivationBlocker.Unknown, 0),
            (AgentActivationBlocker.MissingDisplayName, 1),
            (AgentActivationBlocker.MissingInstructions, 2),
            (AgentActivationBlocker.InvalidInstructions, 3),
            (AgentActivationBlocker.MissingPartyIdentity, 4),
            (AgentActivationBlocker.MissingProviderSelection, 5),
            (AgentActivationBlocker.ProviderUnavailable, 6),
            (AgentActivationBlocker.MissingResponseMode, 7),
            (AgentActivationBlocker.MissingApproverPolicy, 8),
            (AgentActivationBlocker.ApproverPolicyUnresolvable, 9),
            (AgentActivationBlocker.MissingContentSafetyPolicy, 10));
        Pin(
            (AgentLaunchReadinessBlocker.Unknown, 0),
            (AgentLaunchReadinessBlocker.MissingContentSafetyPolicy, 1),
            (AgentLaunchReadinessBlocker.MissingContextPolicy, 2),
            (AgentLaunchReadinessBlocker.MissingLaunchMetrics, 3),
            (AgentLaunchReadinessBlocker.IncompleteLaunchMetricDefinition, 4),
            (AgentLaunchReadinessBlocker.MissingAutomaticLatencyTarget, 5),
            (AgentLaunchReadinessBlocker.MissingConfirmationLatencyTarget, 6),
            (AgentLaunchReadinessBlocker.MissingCostControlPosture, 7),
            (AgentLaunchReadinessBlocker.UnresolvedAuditGovernance, 8));

        foreach (Type enumType in _setupContractEnumTypes)
        {
            pinnedEnumTypes.ShouldContain(enumType, $"{enumType.Name} is part of the setup contract graph.");
        }
    }

    private static void AssertPinnedOrdinals<TEnum>(params (TEnum Member, int Ordinal)[] expected)
        where TEnum : struct, Enum
    {
        Enum.GetValues<TEnum>().Length.ShouldBe(expected.Length, typeof(TEnum).Name);
        string[] declared = Enum.GetValues<TEnum>().Select(value => value.ToString()!).ToArray();
        declared.ShouldBe(expected.Select(pair => pair.Member.ToString()!).ToArray());
        foreach ((TEnum member, int ordinal) in expected)
        {
            Convert.ToInt32(member).ShouldBe(ordinal, member.ToString());
        }
    }

    [Fact]
    public void A_byte_backed_enum_reads_declared_ordinals_without_throwing()
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new UnknownFallbackEnumConverter<ByteBackedToleranceStatus>() },
        };

        JsonSerializer.Deserialize<ByteBackedToleranceStatus>("1", options)
            .ShouldBe(ByteBackedToleranceStatus.Ready);
        JsonSerializer.Deserialize<ByteBackedToleranceStatus>("255", options)
            .ShouldBe(ByteBackedToleranceStatus.Unknown);
        JsonSerializer.Deserialize<ByteBackedToleranceStatus>("257", options)
            .ShouldBe(ByteBackedToleranceStatus.Unknown);
        JsonSerializer.Deserialize<ByteBackedToleranceStatus>("513", options)
            .ShouldBe(ByteBackedToleranceStatus.Unknown);
        JsonSerializer.Deserialize<ByteBackedToleranceStatus>("\"257\"", options)
            .ShouldBe(ByteBackedToleranceStatus.Unknown);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("\"-1\"")]
    public void An_unsigned_backed_enum_rejects_negative_ordinals_without_throwing(string json)
    {
        var options = new JsonSerializerOptions
        {
            Converters = { new UnknownFallbackEnumConverter<UnsignedBackedToleranceStatus>() },
        };

        JsonSerializer.Deserialize<UnsignedBackedToleranceStatus>(json, options)
            .ShouldBe(UnsignedBackedToleranceStatus.Unknown);
    }

    [Theory]
    [MemberData(nameof(PublicOperationEnumTypes))]
    public void RecognizedOperationEnumNamesStillParseCaseInsensitively(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        // The converter replaced a case-insensitive JsonStringEnumConverter, so narrowing to exact casing would
        // silently turn an already-shipped peer's payload into Unknown.
        object canonical = Enum.GetValues(enumType).Cast<object>().First(value => Convert.ToInt32(value) != 0);
        string name = canonical.ToString().ShouldNotBeNull();

        JsonSerializer.Deserialize($"\"{name.ToUpperInvariant()}\"", enumType).ShouldBe(canonical, enumType.Name);
        JsonSerializer.Deserialize($"\"{name.ToLowerInvariant()}\"", enumType).ShouldBe(canonical, enumType.Name);
        JsonSerializer.Deserialize($"\"{name}\"", enumType).ShouldBe(canonical, enumType.Name);
        JsonSerializer.Deserialize($"\" {name} \"", enumType).ShouldBe(canonical, enumType.Name);
    }

    [Theory]
    [MemberData(nameof(PublicOperationEnumTypes))]
    public void OperationEnumsKeepTheUnknownFallbackInsideOptionsThatRegisterTheStringConverter(Type enumType)
    {
        ArgumentNullException.ThrowIfNull(enumType);

        // JsonSerializerOptions.Converters is resolved before a type-level [JsonConverter], so an options bag that
        // registers a plain JsonStringEnumConverter silently reinstates the throwing behaviour. Every server-side
        // options bag pairs the factory with it for exactly this reason.
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            Converters = { new UnknownFallbackEnumConverterFactory(), new JsonStringEnumConverter() },
        };

        object? value = JsonSerializer.Deserialize("\"SomeMemberAddedLater\"", enumType, options);

        Convert.ToInt32(value.ShouldNotBeNull()).ShouldBe(0, enumType.Name);
    }

    [Fact]
    public void AnOptionsBagWithOnlyTheStringConverterStillThrows()
    {
        // Pins the reason the factory exists: without it, the same options bag fails the whole payload.
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() },
        };

        _ = Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<AgentSetupTruthState>("\"SomeMemberAddedLater\"", options));
    }

    // The public routes are minimal APIs, which serialize with JsonSerializerDefaults.Web (camelCase names,
    // case-insensitive reads). Proving compatibility only against the default PascalCase shape would prove it
    // against a fixture no deployed peer ever emits, so every acceptance-compatibility property runs on both.
    private static JsonSerializerOptions ShapeOptions(bool web)
        => web ? new JsonSerializerOptions(JsonSerializerDefaults.Web) : new JsonSerializerOptions();

    private static string PropertyName(string pascalCase, bool web)
        => web ? char.ToLowerInvariant(pascalCase[0]) + pascalCase[1..] : pascalCase;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AcceptanceEnumsShareOneEncodingAndStillReadTheNumericLegacyForm(bool web)
    {
        var acceptance = new AgentCommandAcceptance(
            "hexa",
            "01K50HTM6DK2F7CXEXAMPLE01",
            "01K50HTM6DK2F7CXEXAMPLE02",
            AgentSetupTruthState.ProjectionConfirmed,
            AgentSetupWriteEffect.AlreadyApplied,
            7);

        string json = JsonSerializer.Serialize(acceptance, ShapeOptions(web));

        // Both enums in the record now serialize by name; neither is written as a bare ordinal.
        json.ShouldContain($"\"{PropertyName("TruthState", web)}\":\"ProjectionConfirmed\"");
        json.ShouldContain($"\"{PropertyName("Effect", web)}\":\"AlreadyApplied\"");

        // A payload written by a peer that serialized numerically still round-trips, in either shape.
        AgentCommandAcceptance legacy = JsonSerializer.Deserialize<AgentCommandAcceptance>(
            LegacyAcceptanceJson(web, truthState: 2),
            ShapeOptions(web)).ShouldNotBeNull();
        legacy.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);

        AgentCommandAcceptance roundTrip = JsonSerializer
            .Deserialize<AgentCommandAcceptance>(json, ShapeOptions(web))
            .ShouldNotBeNull();
        roundTrip.ShouldBe(acceptance);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LegacyAgentCommandAcceptanceDeserializesWithUnknownEffectAndNoTargetVersion(bool web)
    {
        AgentCommandAcceptance acceptance = JsonSerializer
            .Deserialize<AgentCommandAcceptance>(LegacyAcceptanceJson(web, truthState: 1), ShapeOptions(web))
            .ShouldNotBeNull();

        acceptance.Effect.ShouldBe(AgentSetupWriteEffect.Unknown);
        acceptance.TargetConfigurationVersion.ShouldBeNull();
    }

    private static string LegacyAcceptanceJson(bool web, int truthState)
        => string.Concat(
            "{\"",
            PropertyName("AgentId", web),
            "\":\"hexa\",\"",
            PropertyName("MessageId", web),
            "\":\"01K50HTM6DK2F7CXEXAMPLE01\",\"",
            PropertyName("CorrelationId", web),
            "\":\"01K50HTM6DK2F7CXEXAMPLE02\",\"",
            PropertyName("TruthState", web),
            "\":",
            truthState.ToString(CultureInfo.InvariantCulture),
            "}");

    [Fact]
    public void LegacyAgentCommandAcceptanceConstructorAndDeconstructShapesRemainAvailable()
    {
        var acceptance = new AgentCommandAcceptance(
            "hexa",
            "message-1",
            "correlation-1",
            AgentSetupTruthState.Submitted);

        (string agentId, string messageId, string correlationId, AgentSetupTruthState truthState) = acceptance;

        agentId.ShouldBe("hexa");
        messageId.ShouldBe("message-1");
        correlationId.ShouldBe("correlation-1");
        truthState.ShouldBe(AgentSetupTruthState.Submitted);
        acceptance.Effect.ShouldBe(AgentSetupWriteEffect.Unknown);
        acceptance.TargetConfigurationVersion.ShouldBeNull();
    }

    [Fact]
    public void LegacyAgentOperationOptionsConstructorShapeRemainsAvailable()
    {
        IReadOnlyDictionary<string, string> options = new Dictionary<string, string> { ["trace"] = "value" };
        var operationOptions = new AgentOperationOptions("correlation", "idempotency", options)
        {
            ExpectedConfigurationVersion = 7,
        };

        operationOptions.CorrelationId.ShouldBe("correlation");
        operationOptions.IdempotencyKey.ShouldBe("idempotency");
        operationOptions.Options.ShouldBeSameAs(options);
        operationOptions.ExpectedConfigurationVersion.ShouldBe(7);
        typeof(AgentOperationOptions).GetConstructor(
            [typeof(string), typeof(string), typeof(IReadOnlyDictionary<string, string>)])
            .ShouldNotBeNull();
    }

    [Fact]
    public void Operation_contract_members_do_not_include_sensitive_names()
    {
        string[] forbiddenNameParts =
        [
            "Secret",
            "ApiKey",
            "Credential",
            "Password",
            "ConnectionString",
            "Prompt",
            "GeneratedContent",
            "EditedContent",
            "StackTrace",
            "StreamName",
            "TenantFingerprint",
        ];

        Type[] operationTypes = typeof(AgentOperationResult).Assembly.GetTypes()
            .Where(type => type.Namespace == "Hexalith.Agents.Contracts.Operations")
            .ToArray();

        foreach (Type type in operationTypes)
        {
            foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (string forbidden in forbiddenNameParts)
                {
                    member.Name.ShouldNotContain(forbidden, Case.Sensitive);
                }
            }
        }
    }

    [Fact]
    public void Pending_and_degraded_status_terms_are_not_success()
    {
        AgentOperationResult.Pending().IsSuccess.ShouldBeFalse();
        new AgentOperationResult(AgentOperationStatus.Checking).IsSuccess.ShouldBeFalse();
        new AgentOperationResult(AgentOperationStatus.Degraded).IsSuccess.ShouldBeFalse();

        AgentOperationResult<ProposalOperationStatus>.Succeeded(ProposalOperationStatus.PostingPending)
            .Value
            .ShouldBe(ProposalOperationStatus.PostingPending);
        ProposalOperationStatus.PostingPending.ShouldNotBe(ProposalOperationStatus.Posted);
        AuditAvailabilityStatus.AuditPending.ShouldNotBe(AuditAvailabilityStatus.AuditAvailable);
        AgentReadinessStatus.Checking.ShouldNotBe(AgentReadinessStatus.Callable);
    }

    private enum ByteBackedToleranceStatus : byte
    {
        Unknown = 0,
        Ready = 1,
    }

    private enum UnsignedBackedToleranceStatus : ulong
    {
        Unknown = 0,
        Maximum = ulong.MaxValue,
    }
}
