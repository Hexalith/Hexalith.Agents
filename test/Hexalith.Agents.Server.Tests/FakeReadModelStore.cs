namespace Hexalith.Agents.Server.Tests;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;

/// <summary>
/// In-module in-memory double for the persisted read-model seams used by the Agent setup projection and the setup
/// reads (Story 5.2). It keeps the ETag / first-write-wins semantics the projection depends on, so duplicate,
/// replayed, and concurrent deliveries can be exercised without a DAPR sidecar.
/// </summary>
/// <remarks>
/// Values round-trip through JSON exactly as the store does, so a test asserting on a persisted end state reads
/// back what was actually serialized rather than a shared in-memory instance.
/// </remarks>
internal sealed class FakeReadModelStore : IReadModelStore, IReadModelBatchStore
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private long _etagSequence;

    /// <summary>Gets or sets a hook invoked before a batch write evaluates its ETag preconditions.</summary>
    public Action? ConcurrentWriteBeforeBatch { get; set; }

    /// <summary>Gets the number of batch executions the fake has served.</summary>
    public int BatchCount { get; private set; }

    /// <summary>Gets the number of <see cref="GetAsync{TValue}"/> lookups the fake has served.</summary>
    public int GetCount { get; private set; }

    public Task<ReadModelEntry<TValue>> GetAsync<TValue>(string storeName, string key, CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        GetCount++;
        return Task.FromResult(_entries.TryGetValue(Compose(storeName, key), out Entry? entry)
            ? new ReadModelEntry<TValue>(JsonSerializer.Deserialize<TValue>(entry.Bytes, _json), entry.ETag)
            : new ReadModelEntry<TValue>(null, null));
    }

    public Task SaveAsync<TValue>(string storeName, string key, TValue value, CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        _entries[Compose(storeName, key)] = new Entry(JsonSerializer.SerializeToUtf8Bytes(value, _json), NextETag());
        return Task.CompletedTask;
    }

    public Task<bool> TrySaveAsync<TValue>(string storeName, string key, TValue value, string etag, CancellationToken cancellationToken = default)
        where TValue : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        string composite = Compose(storeName, key);
        if (!Matches(composite, etag))
        {
            return Task.FromResult(false);
        }

        _entries[composite] = new Entry(JsonSerializer.SerializeToUtf8Bytes(value, _json), NextETag());
        return Task.FromResult(true);
    }

    public Task<ReadModelBatchResult> ExecuteAsync(ReadModelBatch batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(batch);
        cancellationToken.ThrowIfCancellationRequested();
        BatchCount++;
        ConcurrentWriteBeforeBatch?.Invoke();

        // All-or-nothing: every precondition is checked before any key is mutated, matching the coordinated
        // batch contract the projection relies on.
        foreach (ReadModelBatchOperation operation in batch.Operations)
        {
            if (!IsSatisfied(Compose(batch.Scope.StoreName, operation.Key), operation.Concurrency))
            {
                return Task.FromResult(ReadModelBatchResult.OptimisticConflict("fake"));
            }
        }

        foreach (ReadModelBatchOperation operation in batch.Operations)
        {
            string composite = Compose(batch.Scope.StoreName, operation.Key);
            if (operation.Kind is ReadModelBatchOperationKind.Delete)
            {
                _ = _entries.Remove(composite);
                continue;
            }

            _entries[composite] = new Entry(operation.CanonicalValue.ToArray(), NextETag());
        }

        return Task.FromResult(ReadModelBatchResult.Completed("fake"));
    }

    /// <summary>Seeds a value directly, bypassing concurrency checks.</summary>
    /// <typeparam name="TValue">The read-model type.</typeparam>
    /// <param name="storeName">The store component name.</param>
    /// <param name="key">The state key.</param>
    /// <param name="value">The value to seed.</param>
    public void Seed<TValue>(string storeName, string key, TValue value)
        where TValue : class
        => _entries[Compose(storeName, key)] = new Entry(JsonSerializer.SerializeToUtf8Bytes(value, _json), NextETag());

    /// <summary>Returns the currently persisted value for end-state assertions, or <see langword="null"/>.</summary>
    /// <typeparam name="TValue">The read-model type.</typeparam>
    /// <param name="storeName">The store component name.</param>
    /// <param name="key">The state key.</param>
    /// <returns>The persisted value, or <see langword="null"/> when absent.</returns>
    public TValue? Snapshot<TValue>(string storeName, string key)
        where TValue : class
        => _entries.TryGetValue(Compose(storeName, key), out Entry? entry)
            ? JsonSerializer.Deserialize<TValue>(entry.Bytes, _json)
            : null;

    private static string Compose(string storeName, string key) => storeName + "\0" + key;

    // Mirrors the batch protocol's precondition rules so a create-only write is a real first-write race: an empty
    // expected ETag demands an absent key, a non-empty one demands that exact stored ETag, and unconditional /
    // idempotent-absent operations have nothing to check.
    private bool IsSatisfied(string composite, ReadModelBatchConcurrency concurrency)
    {
        if (concurrency.Mode is not ReadModelBatchConcurrencyMode.ExpectedETag)
        {
            return true;
        }

        bool exists = _entries.TryGetValue(composite, out Entry? current);
        return concurrency.ExpectedETag.Length == 0
            ? !exists
            : exists && string.Equals(current!.ETag, concurrency.ExpectedETag, StringComparison.Ordinal);
    }

    private bool Matches(string composite, string expectedETag)
        => IsSatisfied(composite, new ReadModelBatchConcurrency(ReadModelBatchConcurrencyMode.ExpectedETag, expectedETag));

    private string NextETag() => (++_etagSequence).ToString(CultureInfo.InvariantCulture);

    private sealed record Entry(byte[] Bytes, string ETag);
}
