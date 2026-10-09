using Dapr.Actors;
using Dapr.Actors.Client;
using Dapr.Actors.Runtime;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Events;
using Hexalith.EventStore.Server.Tests.TestUtilities;
using Hexalith.EventStore.Server.Streams;
using Hexalith.EventStore.Testing.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static Hexalith.EventStore.Server.Tests.Actors.AggregateActorTestHelper;

namespace Hexalith.EventStore.Server.Tests.Actors;

/// <summary>Actual aggregate persistence never commits events ahead of a configured namespace registration.</summary>
public sealed class AggregateActorPublicationRegistrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfiguredRegistrationPrecedesEventCommitAndRefusalPreventsAppend(bool refuse)
    {
        var backend = new InMemoryStateManager(); var context = CreateActor(stateManager: backend);
        context.Invoker.InvokeAsync(Arg.Any<Hexalith.EventStore.Contracts.Commands.CommandEnvelope>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(DomainResult.Success([new TestEvent()]));
        var registration = Substitute.For<ISourcePublicationWriterRegistration>();
        int registered = 0;
        registration.RegisterBeforeWriteAsync(Arg.Any<AggregateIdentity>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            registered++; backend.CommittedState.Values.OfType<EventEnvelope>().ShouldBeEmpty();
            call.Arg<AggregateIdentity>().ActorId.ShouldBe("test-tenant:test-domain:agg-001");
            return refuse ? Task.FromException(new InvalidOperationException("Controlled namespace registration refusal.")) : Task.CompletedTask;
        });
        using var services = new ServiceCollection().AddSingleton(registration).BuildServiceProvider();
        var actor = new AggregateActor(ActorHost.CreateForTest<AggregateActor>(new ActorTestOptions { ActorId = new("test-tenant:test-domain:agg-001") }),
            context.Logger, context.Invoker, context.SnapshotManager, new NoOpEventPayloadProtectionService(), context.StatusStore,
            context.EventPublisher, Options.Create(new EventDrainOptions()), Options.Create(new BackpressureOptions()), context.DeadLetterPublisher,
            serviceProvider: services, commandAggregateTypeResolver: context.AggregateTypeResolver);
        ActorStateManagerTestHelper.SetStateManager(actor, backend);
        var result = await actor.ProcessCommandAsync(CreateTestEnvelope());
        registered.ShouldBe(1);
        if (refuse) { backend.CommittedState.Values.OfType<EventEnvelope>().ShouldBeEmpty(); result.Accepted.ShouldBeFalse(); }
        else { backend.CommittedState.Values.OfType<EventEnvelope>().Count().ShouldBe(1); result.Accepted.ShouldBeTrue(); }
    }

    /// <summary>Actual registration cannot release an aggregate append after independently qualified coverage changes during its final transport awaits.</summary>
    [Theory]
    [InlineData("register", "withdraw")]
    [InlineData("register", "expire")]
    [InlineData("read", "withdraw")]
    [InlineData("read", "expire")]
    [InlineData("read", "current")]
    public async Task FinalRegistrationRosterRequiresFreshIndependentQualification(string suspendedOperation, string change)
    {
        ArgumentNullException.ThrowIfNull(suspendedOperation); ArgumentNullException.ThrowIfNull(change);
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-08T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var identity = new AggregateIdentity("test-tenant", "test-domain", "agg-001");
        var scope = new SourcePublicationScope(identity.TenantId, identity.Domain, "deletion", "installation-1");
        var namespaceBackend = new InMemoryStateManager(); var operations = Substitute.For<ISourcePublicationOperationAuthority>();
        operations.ReadNamespaceAsync(scope).Returns(true);
        operations.InstallNamespaceAsync(Arg.Any<SourcePublicationNamespaceState>()).Returns(true);
        operations.RegisterSourceAsync(scope, 1, identity).Returns(true);
        var namespaceActor = new SourcePublicationNamespaceActor(ActorHost.CreateForTest<SourcePublicationNamespaceActor>(
            new ActorTestOptions { ActorId = new(scope.ActorId) }), operations);
        ActorStateManagerTestHelper.SetStateManager(namespaceActor, namespaceBackend);
        (await namespaceActor.InstallAsync(new(scope, 1, "installed", "coverage", "writers", []))).ShouldBeTrue();
        bool qualified = true; var authorization = new SourcePublicationNamespaceAuthorization("qualified", clock.GetUtcNow().AddSeconds(10));
        var authority = Substitute.For<ISourcePublicationNamespaceAuthority>(); var authorizedRosters = new List<SourcePublicationNamespaceState>();
        authority.AuthorizeAsync(Arg.Any<SourcePublicationNamespaceState>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            authorizedRosters.Add(call.Arg<SourcePublicationNamespaceState>());
            return qualified ? authorization : null;
        });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = Substitute.For<ISourcePublicationNamespaceActor>(); int reads = 0;
        transport.ReadAsync(scope).Returns(async _ =>
        {
            var state = await namespaceActor.ReadAsync(scope);
            if (++reads == 2 && suspendedOperation == "read") { entered.TrySetResult(); await release.Task; }
            return state;
        });
        transport.RegisterAsync(scope, 1, identity).Returns(async _ =>
        {
            bool result = await namespaceActor.RegisterAsync(scope, 1, identity);
            if (suspendedOperation == "register") { entered.TrySetResult(); await release.Task; }
            return result;
        });
        var proxies = Substitute.For<IActorProxyFactory>();
        proxies.CreateActorProxy<ISourcePublicationNamespaceActor>(new ActorId(scope.ActorId), SourcePublicationNamespaceActor.ActorTypeName).Returns(transport);
        var registration = new DaprSourcePublicationWriterRegistration([scope], proxies, authority, clock);
        var backend = new InMemoryStateManager(); var context = CreateActor(stateManager: backend);
        context.Invoker.InvokeAsync(Arg.Any<Hexalith.EventStore.Contracts.Commands.CommandEnvelope>(), Arg.Any<object?>(), Arg.Any<CancellationToken>())
            .Returns(DomainResult.Success([new TestEvent()]));
        using var services = new ServiceCollection().AddSingleton<ISourcePublicationWriterRegistration>(registration).BuildServiceProvider();
        var actor = new AggregateActor(ActorHost.CreateForTest<AggregateActor>(new ActorTestOptions { ActorId = new(identity.ActorId) }),
            context.Logger, context.Invoker, context.SnapshotManager, new NoOpEventPayloadProtectionService(), context.StatusStore,
            context.EventPublisher, Options.Create(new EventDrainOptions()), Options.Create(new BackpressureOptions()), context.DeadLetterPublisher,
            serviceProvider: services, commandAggregateTypeResolver: context.AggregateTypeResolver);
        ActorStateManagerTestHelper.SetStateManager(actor, backend);
        var pending = actor.ProcessCommandAsync(CreateTestEnvelope());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        backend.CommittedState.Values.OfType<EventEnvelope>().ShouldBeEmpty();
        if (change == "withdraw") { qualified = false; }
        if (change == "expire") { clock.Advance(TimeSpan.FromSeconds(11)); }
        release.TrySetResult();
        var result = await pending.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.Accepted.ShouldBe(change == "current");
        backend.CommittedState.Values.OfType<EventEnvelope>().Count().ShouldBe(change == "current" ? 1 : 0);
        var retained = namespaceBackend.CommittedState.Single().Value.ShouldBeOfType<SourcePublicationNamespaceState>();
        retained.Revision.ShouldBe(2); retained.Sources.ShouldBe(new[] { identity });
        authorizedRosters.Count.ShouldBe(2); authorizedRosters[0].Sources.ShouldBeEmpty();
        System.Text.Json.JsonSerializer.Serialize(authorizedRosters[1]).ShouldBe(System.Text.Json.JsonSerializer.Serialize(retained));
    }
}
