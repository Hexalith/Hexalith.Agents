using System.Text.Json;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Commands;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Shouldly;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;

namespace Hexalith.Conversations.Server.Tests.Agents;

/// <summary>Concrete owner acknowledgement/backfill across actual serialized Conversation sources; namespace and conditional index backend are synthetic.</summary>
public sealed class ConversationDeletionBackfillTests : TimeProvider, ISourcePublicationNamespaceSource, IAuthoritativeEventStreamReader, ISourcePublicationIndexStore, ISourcePublicationDelivery
{
    private readonly SourcePublicationScope _scope = new(F.Tenant.Value, "conversation", "approved-deletion-v1", "installed-fixture");
    private readonly Dictionary<AggregateIdentity, F> _sources = [];
    private readonly Dictionary<AggregateIdentity, ConversationDeletionDeliveryPumpFixture> _workers = [];
    private byte[]? _indexBytes;
    Task<SourcePublicationCut?> ISourcePublicationNamespaceSource.ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken)
        => Task.FromResult<SourcePublicationCut?>(new(scope, "fixed-authenticated-cut", GetUtcNow().AddSeconds(-1), GetUtcNow().AddHours(1), _sources.Keys.Select(id => new SourcePublicationHead(id, 2)).ToArray(), true));
    /// <inheritdoc/>
    public Task<AuthoritativeStreamReadResult> ReadAsync(AggregateIdentity identity, CancellationToken cancellationToken = default) => _sources[identity].ReadAsync(identity, cancellationToken);
    Task<SourcePublicationIndexState?> ISourcePublicationIndexStore.ReadAsync(SourcePublicationScope scope, CancellationToken cancellationToken)
        => Task.FromResult(_indexBytes is null ? null : JsonSerializer.Deserialize<SourcePublicationIndexState>(_indexBytes));
    /// <inheritdoc/>
    public Task<bool> TryWriteAsync(SourcePublicationScope scope, long expectedRevision, SourcePublicationIndexState next, CancellationToken cancellationToken = default)
    {
        if ((_indexBytes is null ? 0 : JsonSerializer.Deserialize<SourcePublicationIndexState>(_indexBytes)!.Revision) != expectedRevision) { return Task.FromResult(false); }
        _indexBytes = JsonSerializer.SerializeToUtf8Bytes(next); return Task.FromResult(true);
    }
    /// <inheritdoc/>
    public Task<SourcePublicationDeliveryStatus> LookupAcknowledgementAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
        => new ConversationDeletionPublicationDelivery(_workers[entry.Publication.Identity].Pump()).LookupAcknowledgementAsync(entry, cancellationToken);
    /// <inheritdoc/>
    public Task<SourcePublicationDeliveryStatus> DeliverAsync(SourcePublicationIndexEntry entry, CancellationToken cancellationToken = default)
        => new ConversationDeletionPublicationDelivery(_workers[entry.Publication.Identity].Pump()).DeliverAsync(entry, cancellationToken);

    /// <summary>A bounded pass delivers the first hundred distinct source originals; restart re-verifies their actual durable acknowledgements and delivers originals 101–103 exactly once.</summary>
    [Fact]
    public async Task ActualSourceAcknowledgementsAdvanceBoundedDispatcherAfterRestart()
    {
        var scope = _scope; var sources = _sources; var workers = _workers;
        var projector = new ConversationDeletionPublicationProjector();
        for (int n = 1; n <= 103; n++)
        {
            var source = new F(new ConversationId("conversation-" + n.ToString("D3", global::System.Globalization.CultureInfo.InvariantCulture)));
            var before = await source.ReplayAsync();
            source.Persist(ConversationAggregate.Handle(new ApproveConversationDeletion(new(source.CommandMetadata(F.Human, "approval"), source.CurrentConversation,
                "independent-approval", before.SourceRevision, F.At.AddMinutes(1), F.DeletionAudit(before.SourceRevision, F.At.AddMinutes(1))), "approval-event"), before));
            var identity = new AggregateIdentity(F.Tenant.Value, "conversation", source.CurrentConversation.Value);
            var stream = (await source.ReadAsync(identity, TestContext.Current.CancellationToken)).Stream!;
            var entry = new SourcePublicationIndexEntry(n, projector.Project(stream, TestContext.Current.CancellationToken).Single());
            sources.Add(identity, source); workers.Add(identity, new(source, entry));
        }
        SourcePublicationDispatcher Dispatcher() => new(new SourcePublicationFeed(this, this, projector, this, this), this, this);
        var first = await Dispatcher().DispatchAsync(scope, 100, TestContext.Current.CancellationToken); first.AcknowledgedPrefix.ShouldBe(100); first.IsComplete.ShouldBeFalse();
        workers.Values.Sum(f => f.Submissions).ShouldBe(100);
        // Each owner read reconstructs state from JSON; the index is independently serialized too.
        _indexBytes = JsonSerializer.SerializeToUtf8Bytes(JsonSerializer.Deserialize<SourcePublicationIndexState>(_indexBytes!)!);
        var second = await Dispatcher().DispatchAsync(scope, 100, TestContext.Current.CancellationToken); second.AcknowledgedPrefix.ShouldBe(103); second.IsComplete.ShouldBeTrue();
        workers.Values.Sum(f => f.Submissions).ShouldBe(103); workers.Values.All(f => f.Submissions == 1 && f.AttemptCommands.Count == 1).ShouldBeTrue();
        foreach (var owner in workers.Values) { (await owner.Source.ReplayAsync()).DeletionSource.AcknowledgedSourceRevision.ShouldBe(2); }
        (await Dispatcher().DispatchAsync(scope, 100, TestContext.Current.CancellationToken)).IsComplete.ShouldBeTrue(); workers.Values.Sum(f => f.Submissions).ShouldBe(103);
    }
}
