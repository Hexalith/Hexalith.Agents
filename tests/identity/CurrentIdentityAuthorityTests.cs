using System.Text.Json;

using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Parties.Authorization;
using Hexalith.Parties.Contracts;
using Hexalith.Parties.Contracts.Events;
using Hexalith.Parties.Contracts.Models;
using Hexalith.Parties.Contracts.Queries;
using Hexalith.Parties.Contracts.ValueObjects;
using Hexalith.Parties.Queries;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Identity.Tests;

/// <summary>Exercises non-null authority changes against the pinned owner's current-read implementation.</summary>
public sealed class CurrentIdentityAuthorityTests
{
    /// <summary>Changed authority cannot release evidence after either dependency suspends.</summary>
    /// <param name="inCustody">Whether the custody read, rather than the source read, suspends.</param>
    /// <param name="change">The independently changed authority field, or an unchanged control.</param>
    [Theory]
    [InlineData(false, "source")]
    [InlineData(true, "source")]
    [InlineData(false, "actor-revision")]
    [InlineData(true, "actor-revision")]
    [InlineData(false, "authority-revision")]
    [InlineData(true, "authority-revision")]
    [InlineData(false, "unchanged")]
    [InlineData(true, "unchanged")]
    public async Task CurrentAuthorityChangeDuringReadMustNotReleaseIdentity(bool inCustody, string change)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset start = now.AddDays(-1);
        const string actor = "01HX0000000000000000000001";
        var binding = new HumanActorBindingEvidence("tenant-a", "party-1", actor, 1, 1,
            start, start.AddDays(10), "writer", actor,
            new("synthetic", "party-actor-history-v1", start.AddDays(10), 1, "custody", true, true, true));
        IEventPayload[] events = [new PartyCreated { Type = PartyType.Person, CreatedAt = start },
            new HumanActorBindingEstablished(new(binding, "logical", "digest"), start, 0)];
        StreamReadEvent[] sourceEvents = [.. events.Select((item, index) => new StreamReadEvent(index + 1,
            item.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(item, item.GetType(), PartiesJsonOptions.Default),
            "json", 1, "message", null, null, start, "writer"))];
        var stream = new AuthoritativeStreamReadResult(new(new("tenant-a", "party", "party-1"),
            sourceEvents.Length, now, sourceEvents, "observation"), null);
        var current = new IdentityAdmissionEvidence(new("tenant-a", "party", "party-1", "Read", "c", "c", "d"),
            "reader", null, actor, 1, true, now.AddMinutes(-1), now.AddMinutes(5), 1);
        IPartyIdentityAuthority authority = Substitute.For<IPartyIdentityAuthority>();
        authority.Admit(Arg.Any<QueryEnvelope>()).Returns(_ => new PartyIdentityAdmissionResult(current, null));
        IAuthoritativeEventStreamReader reader = Substitute.For<IAuthoritativeEventStreamReader>();
        IIdentityHistoryCustody custody = Substitute.For<IIdentityHistoryCustody>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingSource = new TaskCompletionSource<AuthoritativeStreamReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pendingCustody = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        reader.ReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (inCustody)
                {
                    return Task.FromResult(stream);
                }

                entered.SetResult();
                return pendingSource.Task;
            });
        custody.CanReadAsync(Arg.Any<Hexalith.EventStore.Contracts.Identity.AggregateIdentity>(),
                Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (!inCustody)
                {
                    return Task.FromResult(true);
                }

                entered.SetResult();
                return pendingCustody.Task;
            });
        IOptionsMonitor<PartyIdentityOptions> options = Substitute.For<IOptionsMonitor<PartyIdentityOptions>>();
        options.CurrentValue.Returns(new PartyIdentityOptions
        {
            PolicyId = "synthetic",
            Retention = TimeSpan.FromDays(10),
            ExpiryTrigger = "binding-effective-at",
        });
        var service = new PartyIdentityQueryService(authority, TimeProvider.System, reader, custody, identityOptions: options);
        var query = new ResolvePartyIdentity("tenant-a", "party-1", actor);
        var envelope = new QueryEnvelope("tenant-a", "party", "party-1", typeof(ResolvePartyIdentity).FullName!,
            JsonSerializer.SerializeToUtf8Bytes(query, PartiesJsonOptions.Default), "c", "reader", "party-1");
        Task<PartyIdentityResult> pending = service.ResolveAsync(envelope, TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        current = change switch
        {
            "source" => current with { SourceId = "different-authority-source" },
            "actor-revision" => current with { ActorRevision = current.ActorRevision + 1 },
            "authority-revision" => current with { AuthorityRevision = current.AuthorityRevision + 1 },
            _ => current,
        };
        pendingSource.TrySetResult(stream);
        pendingCustody.TrySetResult(true);
        PartyIdentityResult result = await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (change == "unchanged")
        {
            result.Outcome.ShouldBe(PartyIdentityOutcome.Resolved);
            result.Evidence!.HumanBinding.ShouldBe(binding);
        }
        else
        {
            result.Outcome.ShouldBe(PartyIdentityOutcome.Unavailable);
            result.Evidence.ShouldBeNull();
        }
    }
}
