using System.Security.Claims;
using System.Text.Json;
using Dapr.Actors;
using Dapr.Actors.Client;
using Hexalith.Conversations.Aggregates;
using Hexalith.Conversations.Server.Agents;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.Server.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using F = Hexalith.Conversations.Server.Tests.Agents.ConversationAgentLocalFixture;
using StoredEvent = Hexalith.EventStore.Server.Events.EventEnvelope;
using TenantValidator = Hexalith.EventStore.Authorization.ITenantValidator;

namespace Hexalith.Owner.Review.Tests;

/// <summary>Focused synthetic actor/provider simulation through actual gateway unprotection, SDK capture and Conversations replay.</summary>
public sealed class ProtectedGatewayReplayTests
{
    /// <summary>Proves readable JSON with Protected outcome provenance survives the real application path.</summary>
    [Fact]
    public async Task GatewayUnprotectionPreservesReadableProtectedConversationPrefix()
    {
        var fixture = new F();
        fixture.Persist(ConversationAggregate.Handle(fixture.Membership, await fixture.ReplayAsync()));
        fixture.Persist(ConversationAggregate.Handle(fixture.Posting, await fixture.ReplayAsync()));
        var identity = new AggregateIdentity(F.Tenant.Value, "conversation", F.Conversation.Value);
        var metadata = new EventStorePayloadProtectionMetadata(PayloadProtectionState.Protected, 1,
            "aes-gcm-256", "safe-key-reference", "application/json", new Dictionary<string, string> { ["profile"] = "current" });
        byte[][] plaintext = fixture.PersistedEvents.Select(e => JsonSerializer.SerializeToUtf8Bytes(e, e.GetType(), F.Options)).ToArray();
        StoredEvent[] stored = fixture.PersistedEvents.Select((e, index) => new StoredEvent(
            $"persisted-{index + 1}", F.Conversation.Value, "Conversation", F.Tenant.Value, "conversation", index + 1, 0,
            F.At.AddMinutes(index), "correlation-1", "causation-1", "human", "local-fixture", e.GetType().FullName!, 1,
            "json+protected", [(byte)index], EventStorePayloadProtectionMetadataCarrier.Write((IDictionary<string, string>?)null, metadata))).ToArray();
        var actor = Substitute.For<IAggregateActor>();
        actor.GetStreamMetadataAsync().Returns(new AggregateStreamMetadata(true, stored.Length));
        actor.ReadEventsRangeAsync(Arg.Any<long>(), Arg.Any<long?>(), Arg.Any<int>()).Returns(call =>
            stored.Where(e => e.SequenceNumber > call.ArgAt<long>(0) && e.SequenceNumber <= (call.ArgAt<long?>(1) ?? long.MaxValue))
                .Take(call.ArgAt<int>(2)).ToArray());
        var proxies = Substitute.For<IActorProxyFactory>();
        proxies.CreateActorProxy<IAggregateActor>(Arg.Any<ActorId>(), "AggregateActor").Returns(actor);
        var tenants = Substitute.For<TenantValidator>();
        tenants.ValidateAsync(Arg.Any<ClaimsPrincipal>(), F.Tenant.Value, Arg.Any<CancellationToken>(), F.Conversation.Value)
            .Returns(TenantValidationResult.Allowed);
        var rbac = Substitute.For<Hexalith.EventStore.Authorization.IRbacValidator>();
        rbac.ValidateAsync(Arg.Any<ClaimsPrincipal>(), F.Tenant.Value, "conversation", "StreamRead", "replay", Arg.Any<CancellationToken>(), F.Conversation.Value)
            .Returns(RbacValidationResult.Allowed);
        var protection = Substitute.For<IEventPayloadProtectionService>();
        protection.TryUnprotectEventPayloadAsync(identity, Arg.Any<string>(), Arg.Any<byte[]>(), "json+protected",
            Arg.Any<EventStorePayloadProtectionMetadata>(), Arg.Any<CancellationToken>()).Returns(call =>
                PayloadUnprotectionOutcome.Readable(plaintext[call.ArgAt<byte[]>(2)[0]], "json", metadata));
        var controller = new StreamsController(proxies, tenants, rbac, NullLogger<StreamsController>.Instance, protection)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "local-authenticated-principal")], "local-test")) } }
        };
        var gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var response = await controller.ReadStreamAsync(call.ArgAt<StreamReadRequest>(0), call.ArgAt<CancellationToken>(1));
            var page = response.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<StreamReadPage>();
            return page;
        });
        var result = await new ConversationAgentSourceReader(new AuthoritativeEventStreamReader(gateway, TimeProvider.System))
            .ReadAsync(F.Tenant, F.Conversation, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Value.State.HasCompleteEventPrefix.ShouldBeTrue();
        result.Value.State.SourceRevision.ShouldBe(3);
        result.Value.State.Participants.Count.ShouldBe(1);
        result.Value.State.Messages.Count.ShouldBe(1);
        for (int index = 0; index < plaintext.Length; index++)
        {
            result.Value.Source.Events[index].Payload.ShouldBe(plaintext[index]);
            result.Value.Source.Events[index].SerializationFormat.ShouldBe("json");
            result.Value.Source.Events[index].ProtectionMetadata.ShouldBe(metadata);
        }
        await protection.Received(3).TryUnprotectEventPayloadAsync(identity, Arg.Any<string>(), Arg.Any<byte[]>(), "json+protected",
            Arg.Any<EventStorePayloadProtectionMetadata>(), Arg.Any<CancellationToken>());
        await tenants.Received(3).ValidateAsync(Arg.Any<ClaimsPrincipal>(), F.Tenant.Value, Arg.Any<CancellationToken>(), F.Conversation.Value);
    }
}
