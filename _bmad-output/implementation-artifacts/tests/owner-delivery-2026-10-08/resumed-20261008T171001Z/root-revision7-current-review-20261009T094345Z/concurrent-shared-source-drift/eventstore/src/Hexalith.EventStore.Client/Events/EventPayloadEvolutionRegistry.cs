using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.EventStore.Contracts.Events;

namespace Hexalith.EventStore.Client.Events;

/// <summary>Immutable, validated event type and JSON upcaster registry for one reader host.</summary>
public sealed class EventPayloadEvolutionRegistry {
    private const int MaximumPayloadBytes = 64 * 1024 * 1024;
    private readonly IReadOnlyDictionary<string, Type> _eventTypes;
    private readonly IReadOnlyDictionary<(string Name, int Version), IEventPayloadUpcaster> _steps;

    /// <summary>Builds and validates a complete event evolution registry.</summary>
    /// <param name="eventTypes">Known event CLR types from aggregate, projection and subscriber assemblies.</param>
    /// <param name="upcasters">Explicitly registered or discovered one-version JSON steps.</param>
    /// <param name="validateChains">Whether to enforce complete startup chains.</param>
    public EventPayloadEvolutionRegistry(IEnumerable<Type> eventTypes, IEnumerable<IEventPayloadUpcaster> upcasters,
        bool validateChains = true) {
        ArgumentNullException.ThrowIfNull(eventTypes);
        ArgumentNullException.ThrowIfNull(upcasters);
        Type[] known = [.. eventTypes.Where(static type => typeof(IEventPayload).IsAssignableFrom(type)
            && !type.IsAbstract && !type.IsInterface).Distinct()];
        _eventTypes = known.Where(static type => type.FullName is not null)
            .ToDictionary(static type => type.FullName!, static type => type, StringComparer.Ordinal);

        var steps = new Dictionary<(string Name, int Version), IEventPayloadUpcaster>();
        foreach (IEventPayloadUpcaster step in upcasters) {
            if (string.IsNullOrWhiteSpace(step.EventTypeName) || step.FromVersion is < 1 or >= 1024) {
                throw new InvalidOperationException($"Upcaster '{step.GetType().FullName}' has invalid event type or version {step.FromVersion}.");
            }

            if (!steps.TryAdd((step.EventTypeName, step.FromVersion), step)) {
                throw new InvalidOperationException($"Duplicate upcaster for event type '{step.EventTypeName}' version {step.FromVersion}.");
            }

            if (step.TargetEventTypeName is not null && TryResolveType(step.TargetEventTypeName) is null) {
                throw new InvalidOperationException($"Upcaster '{step.EventTypeName}' version {step.FromVersion} targets unknown event type '{step.TargetEventTypeName}'.");
            }
        }

        _steps = steps;
        if (validateChains) { ValidateChains(known); }
    }

    /// <summary>Upcasts a known event before deserialization while retaining original stored bytes.</summary>
    /// <param name="eventTypeName">The stored event name.</param>
    /// <param name="payload">The stored or unprotected JSON bytes.</param>
    /// <param name="storedVersion">The stored version; null means one.</param>
    /// <param name="sequenceNumber">The stored sequence for support-safe diagnostics.</param>
    /// <returns>The effective name and readable bytes.</returns>
    public EventPayloadReadResult Read(string eventTypeName, byte[] payload, int? storedVersion, long sequenceNumber) {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);
        ArgumentNullException.ThrowIfNull(payload);
        int version = storedVersion ?? 1;
        Type? knownType = TryResolveType(eventTypeName);
        IEventPayloadUpcaster? firstStep = FindStep(eventTypeName, version);
        if (knownType is null && firstStep is null) {
            return new EventPayloadReadResult(eventTypeName, payload, null);
        }

        if (version is < 1 or > 1024) {
            throw new EventPayloadEvolutionException(eventTypeName, version, sequenceNumber);
        }

        if (knownType is not null && version > EventPayloadVersion.GetDeclaredVersion(knownType)) {
            throw new EventPayloadEvolutionException(eventTypeName, version, sequenceNumber);
        }

        string currentName = eventTypeName;
        byte[] currentBytes = payload;
        JsonObject? workingPayload = null;
        while (true) {
            Type? resolved = TryResolveType(currentName);
            if (resolved is not null && version == EventPayloadVersion.GetDeclaredVersion(resolved)) {
                return new EventPayloadReadResult(currentName, currentBytes, resolved);
            }

            IEventPayloadUpcaster? step = FindStep(currentName, version);
            if (step is null || version >= 1024) {
                throw new EventPayloadEvolutionException(eventTypeName, storedVersion ?? 1, sequenceNumber);
            }

            try {
                if (currentBytes.Length > MaximumPayloadBytes) {
                    throw new JsonException("Payload exceeds the readable limit.");
                }

                if (workingPayload is null) {
                    workingPayload = JsonNode.Parse(currentBytes,
                        nodeOptions: new JsonNodeOptions { PropertyNameCaseInsensitive = true },
                        documentOptions: new JsonDocumentOptions {
                            MaxDepth = 64,
                            CommentHandling = JsonCommentHandling.Disallow,
                            AllowTrailingCommas = false,
                        }) as JsonObject ?? throw new JsonException("Event payload must be a JSON object.");
                }

                JsonObject output = step.Upcast(workingPayload)
                    ?? throw new InvalidOperationException("Upcaster returned null.");
                workingPayload = output;
                currentBytes = JsonSerializer.SerializeToUtf8Bytes(output);
                if (currentBytes.Length > MaximumPayloadBytes) {
                    throw new JsonException("Upcast payload exceeds the readable limit.");
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException) {
                throw new EventPayloadEvolutionException(eventTypeName, storedVersion ?? 1, sequenceNumber,
                    step.GetType().FullName, exception.GetType().Name);
            }

            currentName = step.TargetEventTypeName ?? currentName;
            version++;
        }
    }

    private Type? TryResolveType(string name) {
        if (_eventTypes.TryGetValue(name, out Type? exact)) { return exact; }
        Type[] shortMatches = [.. _eventTypes.Values.Where(type => string.Equals(type.Name, name, StringComparison.Ordinal))];
        if (shortMatches.Length == 1) { return shortMatches[0]; }
        if (shortMatches.Length > 1) { throw new InvalidOperationException($"Ambiguous event type '{name}'."); }

        Type[] suffixMatches = [.. _eventTypes.Values.Where(type => NameMatches(name, type.FullName!) || NameMatches(name, type.Name))];
        if (suffixMatches.Length == 1) { return suffixMatches[0]; }
        if (suffixMatches.Length > 1) { throw new InvalidOperationException($"Ambiguous event type '{name}'."); }
        return null;
    }

    private IEventPayloadUpcaster? FindStep(string name, int version) {
        if (_steps.TryGetValue((name, version), out IEventPayloadUpcaster? exact)) { return exact; }
        IEventPayloadUpcaster[] matches = [.. _steps.Where(pair => pair.Key.Version == version && NameMatches(name, pair.Key.Name))
            .Select(static pair => pair.Value)];
        if (matches.Length > 1) { throw new InvalidOperationException($"Ambiguous upcaster for event type '{name}' version {version}."); }
        return matches.SingleOrDefault();
    }

    private static bool NameMatches(string storedName, string candidate)
        => string.Equals(storedName, candidate, StringComparison.Ordinal)
            || storedName.EndsWith($".{candidate}", StringComparison.Ordinal)
            || storedName.EndsWith($"+{candidate}", StringComparison.Ordinal);

    private void ValidateChains(Type[] known) {
        foreach (Type type in known) {
            int declared = EventPayloadVersion.GetDeclaredVersion(type);
            if (declared == 1) { continue; }
            string currentName = type.FullName ?? type.Name;
            bool complete = _steps.Keys.Where(static key => key.Version == 1)
                .Select(static key => key.Name)
                .Distinct(StringComparer.Ordinal)
                .Any(name => Reaches(name, currentName, declared));
            if (!complete) {
                throw new InvalidOperationException($"Event type '{currentName}' version {declared} has an incomplete upcaster chain from version 1.");
            }
        }

        foreach (((string name, int version), IEventPayloadUpcaster step) in _steps) {
            string outputName = step.TargetEventTypeName ?? name;
            Type? outputType = TryResolveType(outputName);
            int outputVersion = version + 1;
            if ((outputType is null || EventPayloadVersion.GetDeclaredVersion(outputType) != outputVersion)
                && FindStep(outputName, outputVersion) is null) {
                throw new InvalidOperationException($"Dangling upcaster for event type '{name}' version {version}.");
            }
        }
    }

    private bool Reaches(string sourceName, string targetName, int targetVersion) {
        string currentName = sourceName;
        for (int version = 1; version < targetVersion; version++) {
            IEventPayloadUpcaster? step = FindStep(currentName, version);
            if (step is null) { return false; }
            currentName = step.TargetEventTypeName ?? currentName;
        }

        Type? reached = TryResolveType(currentName);
        return reached is not null && string.Equals(reached.FullName, targetName, StringComparison.Ordinal);
    }
}
