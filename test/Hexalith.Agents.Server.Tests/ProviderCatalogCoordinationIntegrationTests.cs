using System.Reflection;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.EventStore;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.Server.Pipeline.Commands;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Exercises the shared provider coordinator against platform events and a persisted tenant projection.</summary>
public sealed class ProviderCatalogCoordinationIntegrationTests
{
    private const string StoreName = "statestore";
    private static readonly DateTimeOffset _now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Platform_update_and_tenant_decision_share_one_turn_and_persist_only_a_current_decision(bool updateFirst)
    {
        var termsV1 = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, _now);
        var termsV2 = new ProviderDataHandlingRecord(40, false, ["EU"], "terms-v2", 2, _now.AddMinutes(1));
        string entryId = ProviderCatalogIdentity.EntryId("provider", "model");
        var policy = new AgentsProviderCatalogCoordinationPolicy();
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "approved", 1, termsV1, _now);
        var update = new UpdateProviderModelEntry("provider", "model", "Model", true, 1000, 500,
            new ProviderModelTimeoutPolicy(30000, 3), ProviderModelCapabilityFlags.Streaming, "cfg-ref",
            new ProviderModelPricing("USD", 0.002m, 0.008m, 2), 1, termsV2);
        SubmitCommand decisionSubmit = Submit("decision-msg", "tenant-a", TenantProviderEnablementAggregate.Domain,
            "tenant-a", decision, TenantProviderEnablementAggregate.TenantAdministratorExtensionKey);
        SubmitCommand updateSubmit = Submit("update-msg", ProviderCatalogIdentity.PlatformTenantId,
            ProviderCatalogAggregate.Domain, entryId, update, ProviderCatalogAggregate.ProviderAdminExtensionKey);

        string actorId = policy.GetScope(decisionSubmit.ToCommandEnvelope()).Source.ActorId;
        policy.GetScope(updateSubmit.ToCommandEnvelope()).Source.ActorId.ShouldBe(actorId);

        var created = new ProviderModelEntryCreated(entryId, "provider", "model", "Model", true, true,
            1000, 500, new ProviderModelTimeoutPolicy(30000, 3), ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured, "cfg-ref", new ProviderModelPricing("USD", 0.002m, 0.008m, 1),
            1, termsV1);
        var changed = new ProviderModelEntryMetadataUpdated(entryId, "provider", "model", "Model", true,
            1000, 500, new ProviderModelTimeoutPolicy(30000, 3), ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured, "cfg-ref", new ProviderModelPricing("USD", 0.002m, 0.008m, 2),
            2, termsV2);
        List<EventEnvelope> sourceEvents = [Persisted("created-msg", 1, ProviderCatalogAggregate.Domain,
            ProviderCatalogIdentity.PlatformTenantId, entryId, created)];
        IAggregateActor source = Substitute.For<IAggregateActor>();
        source.GetEventsAsync(0).Returns(_ => Task.FromResult(sourceEvents.ToArray()));
        source.ProcessCommandAsync(Arg.Any<CommandEnvelope>()).Returns(call =>
        {
            CommandEnvelope command = call.Arg<CommandEnvelope>();
            sourceEvents.Add(Persisted(command.MessageId, 2, ProviderCatalogAggregate.Domain,
                ProviderCatalogIdentity.PlatformTenantId, entryId, changed));
            return Task.FromResult(new CommandProcessingResult(true, CorrelationId: command.CorrelationId, EventCount: 1));
        });

        var tenantState = new TenantProviderEnablementState();
        var enabled = new TenantProviderModelEnablementSet("tenant-a", "provider", "model", true, 1, "operator", null);
        tenantState.Apply(enabled);
        var store = new FakeReadModelStore();
        var handler = new TenantProviderEnablementProjectionHandler(store, store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }));
        DomainProjectionHandlerResult seed = await handler.ProjectAsync(new ProjectionRequest("tenant-a",
            TenantProviderEnablementAggregate.Domain, "tenant-a", [Projected("enable-msg", 1, enabled)]),
            "seed", CancellationToken.None);
        seed.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        List<EventEnvelope> targetEvents = [Persisted("enable-msg", 1, TenantProviderEnablementAggregate.Domain,
            "tenant-a", "tenant-a", enabled)];
        IAggregateActor target = Substitute.For<IAggregateActor>();
        target.GetEventsAsync(0).Returns(_ => Task.FromResult(targetEvents.ToArray()));
        target.ProcessCommandAsync(Arg.Any<CommandEnvelope>()).Returns(async call =>
        {
            CommandEnvelope command = call.Arg<CommandEnvelope>();
            var payload = JsonSerializer.Deserialize<DecideProviderDataHandling>(command.Payload).ShouldNotBeNull();
            var result = TenantProviderEnablementAggregate.Handle(payload, tenantState, command);
            result.IsSuccess.ShouldBeTrue();
            var accepted = result.Events.ShouldHaveSingleItem().ShouldBeOfType<ProviderDataHandlingDecided>();
            tenantState.Apply(accepted);
            targetEvents.Add(Persisted(command.MessageId, 2, TenantProviderEnablementAggregate.Domain,
                "tenant-a", "tenant-a", accepted));
            DomainProjectionHandlerResult projected = await handler.ProjectAsync(new ProjectionRequest("tenant-a",
                TenantProviderEnablementAggregate.Domain, "tenant-a", [Projected(command.MessageId, 2, accepted)]),
                "decision", CancellationToken.None);
            projected.Status.ShouldBe(ProjectionDispatchStatus.Completed);
            return new CommandProcessingResult(true, CorrelationId: command.CorrelationId, EventCount: 1);
        });

        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        factory.CreateActorProxy<IAggregateActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(call => call.ArgAt<ActorId>(0).ToString() == actorId ? source : target);
        var host = ActorHost.CreateForTest<CoordinatedCommandActor>(new ActorTestOptions { ActorId = new ActorId(actorId) });
        var coordinator = new CoordinatedCommandActor(host, factory, Options.Create(new EventStoreActorOptions()),
            [policy], new NoOpEventPayloadProtectionService());
        typeof(Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(coordinator, Substitute.For<IActorStateManager>());
        factory.CreateActorProxy<ICoordinatedCommandActor>(Arg.Any<ActorId>(), Arg.Any<string>())
            .Returns(coordinator);
        var router = new CommandRouter(factory, Options.Create(new EventStoreActorOptions()),
            NullLogger<CommandRouter>.Instance, [policy]);

        if (updateFirst)
        {
            (await router.RouteCommandAsync(updateSubmit)).Accepted.ShouldBeTrue();
            CommandProcessingResult rejected = await router.RouteCommandAsync(decisionSubmit);
            rejected.Accepted.ShouldBeFalse();
            rejected.FailureReason.ShouldBe("ConcurrencyConflict");
            await target.DidNotReceive().ProcessCommandAsync(Arg.Any<CommandEnvelope>());
        }
        else
        {
            DecideProviderDataHandling altered = decision with
            {
                ConfirmedTerms = termsV1 with { EffectiveAt = _now.AddDays(1) },
            };
            SubmitCommand alteredSubmit = Submit("altered-terms-msg", "tenant-a", TenantProviderEnablementAggregate.Domain,
                "tenant-a", altered, TenantProviderEnablementAggregate.TenantAdministratorExtensionKey);
            CommandProcessingResult blocked = await router.RouteCommandAsync(alteredSubmit);
            blocked.Accepted.ShouldBeFalse();
            blocked.FailureReason.ShouldBe("ConcurrencyConflict");
            await target.DidNotReceive().ProcessCommandAsync(Arg.Any<CommandEnvelope>());

            (await router.RouteCommandAsync(decisionSubmit)).Accepted.ShouldBeTrue();
            (await router.RouteCommandAsync(updateSubmit)).Accepted.ShouldBeTrue();
            await target.Received(1).ProcessCommandAsync(Arg.Any<CommandEnvelope>());
        }

        _ = factory.Received(updateFirst ? 2 : 3).CreateActorProxy<ICoordinatedCommandActor>(
            Arg.Is<ActorId>(id => id.ToString() == actorId), CoordinatedCommandActor.ActorTypeName);
        TenantProviderEnablementReadModel persisted = store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
            TenantProviderEnablementReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        persisted.State.Revision.ShouldBe(updateFirst ? 1 : 2);
        (persisted.State.Entries.ShouldHaveSingleItem().Value.AcceptedTerms?.DataHandlingVersion)
            .ShouldBe(updateFirst ? null : (int?)1);
    }

    [Fact]
    public void Exact_decision_retry_ignores_server_clock_and_trace_headers_but_detects_divergent_intent()
    {
        var policy = new AgentsProviderCatalogCoordinationPolicy();
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, _now);
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "approved", 1, terms, _now);
        CommandEnvelope first = Submit("msg", "tenant-a", TenantProviderEnablementAggregate.Domain,
            "tenant-a", decision, TenantProviderEnablementAggregate.TenantAdministratorExtensionKey).ToCommandEnvelope();
        CommandEnvelope retry = first with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(decision with { DecidedAt = _now.AddMinutes(1) }),
            Extensions = new Dictionary<string, string>(first.Extensions!) { ["traceparent"] = "different-trace" },
        };
        CommandEnvelope divergent = retry with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(decision with { Justification = "different" }),
        };

        policy.GetCommandDigest(retry).ShouldBe(policy.GetCommandDigest(first));
        policy.GetCommandDigest(divergent).ShouldNotBe(policy.GetCommandDigest(first));
    }

    private static SubmitCommand Submit<T>(string messageId, string tenant, string domain, string aggregateId,
        T payload, string authorityKey)
        => new(messageId, tenant, domain, aggregateId, typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(payload), "correlation", "operator",
            new Dictionary<string, string> { [authorityKey] = "true" });

    private static EventEnvelope Persisted<T>(string messageId, long sequence, string domain, string tenant,
        string aggregateId, T payload)
        => new(messageId, aggregateId, "aggregate", tenant, domain, sequence, sequence, _now.AddSeconds(sequence),
            "correlation", messageId, "operator", "test", typeof(T).Name, 1, "json",
            JsonSerializer.SerializeToUtf8Bytes(payload), null);

    private static ProjectionEventDto Projected<T>(string messageId, long sequence, T payload)
        => new(typeof(T).Name, JsonSerializer.SerializeToUtf8Bytes(payload), "json", sequence,
            _now.AddSeconds(sequence), "correlation", messageId);
}
