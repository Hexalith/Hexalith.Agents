using System.Security.Cryptography;
using System.Text.Json;
using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.State;
using Hexalith.EventStore.Client.Streams;

namespace Hexalith.Conversations.Server.Agents;

/// <summary>Derives the complete active denominator from current persisted Conversation lifecycle prefixes.</summary>
/// <param name="registration">Explicit tenant namespace installation; missing configuration disables this adapter.</param>
/// <param name="sources">Shared bounded authenticated complete source snapshot reader.</param>
/// <param name="clock">Current authority and whole-operation budget clock.</param>
/// <remarks>Counts currently open undeleted Conversations by the owner-accepted creation window, including zero Agent Calls.
/// The outer query service independently checks the current Agents service authority before and after this read.</remarks>
public sealed class EventStoreConversationTenantCatalogue(ConversationTenantCatalogueRegistration? registration,
    SourceNamespaceSnapshotReader sources, TimeProvider clock) : IConversationTenantCatalogue
{
    /// <inheritdoc/>
    public async Task<ConversationTenantCatalogueResult> ReadAsync(ConversationActiveCountQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            long started = clock.GetTimestamp();
            ArgumentNullException.ThrowIfNull(query);
            if (registration?.Scope is not { } scope || scope.Tenant != query.TenantId.Value || scope.Domain != "conversation")
            { return new(ConversationAgentsOutcome.Unavailable); }
            var snapshot = await sources.ReadAsync(scope, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (snapshot is null || !Current()) { return new(ConversationAgentsOutcome.Unavailable); }
            var entries = new List<ConversationTenantCatalogueEntry>();
            foreach (var source in snapshot.Sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Current()) { return new(ConversationAgentsOutcome.Unavailable); }
                var replay = ConversationAgentSourceReader.ReplaySource(source, source.Identity, cancellationToken);
                if (replay is null) { return new(ConversationAgentsOutcome.Unavailable); }
                var state = replay.Value.State;
                if (!state.IsCreated) { continue; }
                if (state.CreatedAt is not { } created || created == default)
                { return new(ConversationAgentsOutcome.Unavailable); }
                entries.Add(new(new TenantId(scope.Tenant), new ConversationId(source.Identity.AggregateId),
                    created, !state.IsDeleted && state.Lifecycle == ConversationLifecycleState.Open));
            }
            string checkpoint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
            {
                snapshot.Cut.Scope, snapshot.Cut.AuthorityRevision,
                Sources = snapshot.Cut.Sources.Select(h => new { h.Identity.ActorId, h.Head }).ToArray(),
            })));
            cancellationToken.ThrowIfCancellationRequested();
            if (!Current()) { return new(ConversationAgentsOutcome.Unavailable); }
            return new(ConversationAgentsOutcome.Available, entries.AsReadOnly(), checkpoint, snapshot.Cut.ObservedAt, true);

            bool Current() => clock.GetElapsedTime(started) < TimeSpan.FromSeconds(30)
                && snapshot.Cut.ObservedAt <= clock.GetUtcNow() && snapshot.Cut.ValidUntil > clock.GetUtcNow();
        }
        catch (Exception) { cancellationToken.ThrowIfCancellationRequested(); throw; }
    }
}
