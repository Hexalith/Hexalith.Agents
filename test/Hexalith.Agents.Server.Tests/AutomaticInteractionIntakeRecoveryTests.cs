using System.Diagnostics;
using System.Text.Json;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState.Events;
using Hexalith.Agents.Contracts.Security;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.Contracts.Commands;
using NSubstitute;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Exact intake recovery preserves owner truth across asynchronous rejection, contention and rejection-only folds.</summary>
public sealed class AutomaticInteractionIntakeRecoveryTests
{
    /// <summary>Malformed tenant and Conversation identifiers outside the shared workflow contract.</summary>
    public static IEnumerable<object[]> InvalidIdentifiers()
    {
        foreach (bool tenant in new[] { true, false })
        {
            foreach (string value in new[] { "", " ", "tenant.a", "tenant/a", "tenant\n", "ténant", new string('a', 129) })
            { yield return [tenant, value]; }
        }
    }

    [Theory]
    [MemberData(nameof(InvalidIdentifiers))]
    public async Task InvalidIdentifiersReturnNotFoundBeforeGatesDependenciesOrDurableEffects(bool tenant, string value)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var gate = Substitute.For<IAutomaticInteractionIntakeGate>(); var availability = Substitute.For<IAgentDependencyAvailability>();
        var request = tenant ? fixture.Request with { TenantId = value, AuthenticatedTenantId = value } : fixture.Request with { ConversationId = value };
        var intake = new AutomaticInteractionIntakeOrchestrator(gate, availability, fixture, fixture, fixture, fixture.Recovery());
        (await intake.ExecuteAsync(request, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.NotFound);
        (await fixture.Recovery().RecoverAsync(request.TenantId, request.ConversationId, fixture.InteractionId, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.NotFound);
        gate.ReceivedCalls().ShouldBeEmpty(); availability.ReceivedCalls().ShouldBeEmpty();
        fixture.Dispatches.ShouldBeEmpty(); fixture.Directory.ShouldBeNull(); fixture.Interaction.ShouldBeNull(); fixture.Scheduled.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KnownWorkflowRecoveryRefusalReturnsTypedOutcomeWithoutChangingBusinessState(bool intakeRetry)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        string before = fixture.Snapshot(); int accepted = fixture.AcceptedCommandIds.Count;
        var scheduler = Substitute.For<IInteractionWorkflowScheduler>();
        scheduler.EnsureScheduledAsync(Arg.Any<WorkflowExecutionState>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("InteractionWorkflowRecoveryRequired")));
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, fixture, scheduler);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, fixture, fixture, recovery);
        var result = intakeRetry ? await intake.ExecuteAsync(fixture.Request, CancellationToken.None)
            : await recovery.RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None);
        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.RecoveryRequired); result.AgentInteractionId.ShouldBe(fixture.InteractionId);
        fixture.Restart(); fixture.Snapshot().ShouldBe(before); fixture.AcceptedCommandIds.Count.ShouldBe(accepted);
        fixture.Scheduled.ShouldBe([fixture.InteractionId]); fixture.Effects.Posts.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnrelatedSchedulerFailuresPropagateAndCancellationRemainsCancellation(bool cancellation)
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        Exception failure = cancellation ? new OperationCanceledException("InteractionWorkflowRecoveryRequired") : new InvalidOperationException("unrelated-failure");
        var scheduler = Substitute.For<IInteractionWorkflowScheduler>();
        scheduler.EnsureScheduledAsync(Arg.Any<WorkflowExecutionState>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(failure));
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, fixture, scheduler);
        var observed = await Should.ThrowAsync<Exception>(() => recovery.RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None));
        if (cancellation) { observed.ShouldBeAssignableTo<OperationCanceledException>(); }
        else { observed.ShouldBeSameAs(failure); }
    }

    [Fact]
    public async Task CancellationRequestedDuringKnownSchedulerRefusalRemainsCancellation()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync(); string before = fixture.Snapshot();
        using var cancellation = new CancellationTokenSource();
        var scheduler = Substitute.For<IInteractionWorkflowScheduler>();
        scheduler.EnsureScheduledAsync(Arg.Any<WorkflowExecutionState>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            call.ArgAt<CancellationToken>(1).ShouldBe(cancellation.Token); cancellation.IsCancellationRequested.ShouldBeFalse();
            cancellation.Cancel(); return Task.FromException(new InvalidOperationException("InteractionWorkflowRecoveryRequired"));
        });
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, fixture, scheduler);
        Task<AutomaticInteractionIntakeResult> operation = recovery.RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, cancellation.Token);
        var failure = await Should.ThrowAsync<OperationCanceledException>(() => operation);
        failure.CancellationToken.ShouldBe(cancellation.Token); operation.IsCanceled.ShouldBeTrue();
        fixture.Restart(); fixture.Snapshot().ShouldBe(before);
    }

    [Theory]
    [InlineData("permit-revision")]
    [InlineData("caller")]
    [InlineData("protected-request")]
    [InlineData("snapshot")]
    [InlineData("directory-epoch")]
    [InlineData("protection-target")]
    [InlineData("digest-key-version")]
    public async Task DivergentRecordedCreationReturnsConflictAndPreservesOwners(string field)
    {
        var fixture = new AutomaticInteractionReplayFixture { FaultAt = 1, CrashAfterAppend = true };
        await Should.ThrowAsync<AutomaticInteractionInjectedCrash>(() => fixture.StartAsync());
        string before = fixture.Snapshot(); int dispatched = fixture.Dispatches.Count;
        var original = fixture.Interaction!.PermittedCreation!;
        var p = original.Permit;
        var changedPermit = field switch
        {
            "caller" => p with { CallerPartyId = "other-person" },
            "protected-request" => p with { Request = p.Request with { Value = JsonSerializer.SerializeToElement(new { FixtureSealed = "changed-request" }) } },
            "snapshot" => p with { Snapshot = p.Snapshot with { ConfigurationVersion = p.Snapshot.ConfigurationVersion + 1 } },
            "directory-epoch" => p with { DirectoryEpoch = p.DirectoryEpoch with { EpochId = "changed-epoch" } },
            "protection-target" => p with { Request = p.Request with { Target = p.Request.Target with { TargetProtectionKeyAlias = "changed-interaction-dek" } } },
            "digest-key-version" => p with { DigestKeyVersion = "changed-digest-v2" },
            _ => p,
        };
        var divergent = new AgentInteractionState();
        divergent.Apply(field == "permit-revision" ? original with { PermitRevision = original.PermitRevision + 1 } : original with { Permit = changedPermit });
        var reader = Substitute.For<IAgentInteractionStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(divergent);
        var result = await new InteractionOutboxRecoveryOrchestrator(fixture, reader, fixture, fixture)
            .RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None);
        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.Conflict);
        fixture.Directory!.Acknowledgements.ShouldBeEmpty(); fixture.Directory.Leases.ShouldBeEmpty(); fixture.Scheduled.ShouldBeEmpty();
        fixture.Dispatches.Count.ShouldBe(dispatched); fixture.Restart(); fixture.Snapshot().ShouldBe(before);
    }

    [Fact]
    public async Task ExhaustedObservationReturnsRecoveryRequiredWithoutResubmittingAtTheSameRevision()
    {
        var fixture = new AutomaticInteractionReplayFixture(); var dispatcher = Substitute.For<IAgentCommandDispatcher>(); int submitted = 0;
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(call =>
        { submitted++; var intent = call.ArgAt<AgentCommandIntent>(0); return Task.FromResult(new SubmitCommandResponse(intent.CorrelationId, MessageId: intent.MessageId)); });
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, dispatcher, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, fixture, dispatcher, recovery);
        var result = await intake.ExecuteAsync(fixture.Request, CancellationToken.None);
        result.Status.ShouldBe(AutomaticInteractionIntakeStatus.RecoveryRequired); result.AgentInteractionId.ShouldBe(fixture.InteractionId);
        submitted.ShouldBe(1); fixture.Directory.ShouldBeNull(); fixture.Interaction.ShouldBeNull(); fixture.Scheduled.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("single-retry")]
    [InlineData("second-stale-retry")]
    [InlineData("closing-before-retry")]
    [InlineData("closing-after-accepted-retry")]
    [InlineData("invisible-accepted-retry")]
    public async Task ContendedRegistrationRetriesTheExactPermitAtTheObservedRevisionOrHonorsClosing(string scenario)
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true };
        var dispatcher = Substitute.For<IAgentCommandDispatcher>(); var registrations = new List<AgentCommandIntent>();
        var receipts = new Dictionary<string, SubmitCommandResponse>(); int cachedReplays = 0; int invisibleReads = 0;
        int staleAttempts = scenario == "second-stale-retry" ? 2 : 1;
        ConversationAgentStateState? invisibleDirectory = null; var hiddenRevisions = new List<long>();
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            await Task.Yield(); var intent = call.ArgAt<AgentCommandIntent>(0); var ct = call.ArgAt<CancellationToken>(1);
            if (receipts.TryGetValue(intent.MessageId, out var receipt)) { cachedReplays++; return receipt; }
            if (intent.CommandType == nameof(RegisterInteractionPermit))
            {
                registrations.Add(intent);
                var command = JsonSerializer.Deserialize<RegisterInteractionPermit>(intent.Payload).ShouldNotBeNull();
                if (registrations.Count <= staleAttempts)
                {
                    await RegisterCompetingPermitAsync(fixture, command.Permit, registrations.Count, ct);
                    if (scenario == "closing-before-retry") { await fixture.CloseAsync(); }
                    var envelope = new CommandEnvelope(intent.MessageId, intent.TenantId, intent.Domain, intent.AggregateId, intent.CommandType, intent.Payload,
                        intent.CorrelationId, intent.CausationId, intent.UserId, intent.Extensions);
                    ConversationAgentStateAggregate.Handle(command, fixture.Directory, envelope).Events.Single().ShouldBeOfType<ConversationDirectoryRejected>()
                        .Reason.ShouldBe(scenario == "closing-before-retry" ? ConversationDirectoryRejectionReason.ConversationClosing : ConversationDirectoryRejectionReason.StaleRevision);
                }
                else if (scenario == "invisible-accepted-retry")
                {
                    invisibleDirectory = JsonSerializer.Deserialize<ConversationAgentStateState>(JsonSerializer.Serialize(fixture.Directory)).ShouldNotBeNull();
                    invisibleReads = 3;
                }
            }
            receipt = await fixture.DispatchAsync(intent, ct); receipts.Add(intent.MessageId, receipt);
            if (intent.CommandType == nameof(RegisterInteractionPermit) && registrations.Count > staleAttempts && scenario == "closing-after-accepted-retry")
            { await fixture.CloseAsync(); }
            return receipt;
        });
        var reader = Substitute.For<IConversationAgentStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (invisibleReads > 0) { invisibleReads--; hiddenRevisions.Add(invisibleDirectory!.Revision); return invisibleDirectory; }
            return fixture.Directory;
        });
        var recovery = new InteractionOutboxRecoveryOrchestrator(reader, fixture, dispatcher, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, reader, dispatcher, recovery);
        var result = await intake.ExecuteAsync(fixture.Request, CancellationToken.None);
        var expectedStatus = scenario switch
        {
            "closing-before-retry" => AutomaticInteractionIntakeStatus.ConversationClosing,
            "closing-after-accepted-retry" => AutomaticInteractionIntakeStatus.StartSuppressedByDeletion,
            _ => AutomaticInteractionIntakeStatus.Scheduled,
        };
        result.Status.ShouldBe(expectedStatus);
        registrations.Count.ShouldBe(scenario == "closing-before-retry" ? 1 : staleAttempts + 1);
        var first = JsonSerializer.Deserialize<RegisterInteractionPermit>(registrations[0].Payload).ShouldNotBeNull();
        for (int i = 0; i < registrations.Count; i++)
        {
            var retry = JsonSerializer.Deserialize<RegisterInteractionPermit>(registrations[i].Payload).ShouldNotBeNull();
            retry.ExpectedRevision.ShouldBe(i); JsonSerializer.Serialize(retry.Permit).ShouldBe(JsonSerializer.Serialize(first.Permit));
        }
        registrations.Select(i => i.MessageId).Distinct().Count().ShouldBe(registrations.Count);
        string beforeReplay = fixture.Snapshot(); int handled = fixture.Dispatches.Count;
        await dispatcher.DispatchAsync(registrations[0], CancellationToken.None);
        cachedReplays.ShouldBe(1); fixture.Dispatches.Count.ShouldBe(handled); fixture.Snapshot().ShouldBe(beforeReplay);
        if (scenario == "closing-before-retry")
        {
            fixture.Directory!.Permits.ShouldNotContainKey(fixture.InteractionId); fixture.Directory.Acknowledgements.ShouldBeEmpty();
            fixture.Directory.Leases.ShouldBeEmpty(); fixture.Interaction.ShouldBeNull(); fixture.Scheduled.ShouldBeEmpty();
        }
        else
        {
            fixture.Directory!.Permits.Count.ShouldBe(staleAttempts + 1);
            JsonSerializer.Serialize(fixture.Directory.Permits[fixture.InteractionId].Permit).ShouldBe(JsonSerializer.Serialize(first.Permit));
            if (scenario == "closing-after-accepted-retry")
            {
                fixture.Directory.Acknowledgements.ShouldBeEmpty(); fixture.Directory.Leases.ShouldBeEmpty(); fixture.Interaction.ShouldBeNull(); fixture.Scheduled.ShouldBeEmpty();
            }
            else
            {
                fixture.Directory.Acknowledgements.Count.ShouldBe(1); fixture.Interaction!.PermittedCreation!.Permit.AgentInteractionId.ShouldBe(fixture.InteractionId);
                fixture.Scheduled.ShouldBe([fixture.InteractionId]);
                if (scenario == "invisible-accepted-retry") { hiddenRevisions.ShouldBe([1L, 1L, 1L]); }
            }
        }
        string before = fixture.Snapshot(); fixture.Restart(); fixture.Snapshot().ShouldBe(before); fixture.Effects.Posts.ShouldBeEmpty();
        int accepted = fixture.AcceptedCommandIds.Count;
        if (scenario != "closing-before-retry")
        {
            (await fixture.Recovery().RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None)).Status.ShouldBe(expectedStatus);
            fixture.AcceptedCommandIds.Count.ShouldBe(accepted); fixture.Snapshot().ShouldBe(before);
        }
    }

    [Fact]
    public async Task RepeatedRevisionContentionIsPacedAndExhaustsWithoutCreatingTheInteraction()
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true };
        var dispatcher = Substitute.For<IAgentCommandDispatcher>(); var registrations = new List<AgentCommandIntent>(); var submittedAt = new List<long>();
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var intent = call.ArgAt<AgentCommandIntent>(0); var ct = call.ArgAt<CancellationToken>(1);
            intent.CommandType.ShouldBe(nameof(RegisterInteractionPermit)); registrations.Add(intent); submittedAt.Add(Stopwatch.GetTimestamp());
            var command = JsonSerializer.Deserialize<RegisterInteractionPermit>(intent.Payload).ShouldNotBeNull();
            await RegisterCompetingPermitAsync(fixture, command.Permit, registrations.Count, ct);
            return await fixture.DispatchAsync(intent, ct);
        });
        var recovery = new InteractionOutboxRecoveryOrchestrator(fixture, fixture, dispatcher, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, fixture, dispatcher, recovery);
        long started = Stopwatch.GetTimestamp();
        (await intake.ExecuteAsync(fixture.Request, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.RecoveryRequired);
        Stopwatch.GetElapsedTime(started).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromSeconds(10)); registrations.Count.ShouldBe(65);
        for (int i = 2; i < submittedAt.Count; i++)
        { Stopwatch.GetElapsedTime(submittedAt[i - 1], submittedAt[i]).ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(20)); }
        string permit = JsonSerializer.Serialize(JsonSerializer.Deserialize<RegisterInteractionPermit>(registrations[0].Payload)!.Permit);
        for (int i = 0; i < registrations.Count; i++)
        {
            var command = JsonSerializer.Deserialize<RegisterInteractionPermit>(registrations[i].Payload).ShouldNotBeNull();
            command.ExpectedRevision.ShouldBe(i); JsonSerializer.Serialize(command.Permit).ShouldBe(permit);
        }
        registrations.Select(i => i.MessageId).Distinct().Count().ShouldBe(65);
        fixture.Directory!.Revision.ShouldBe(65); fixture.Directory.Permits.Count.ShouldBe(65); fixture.Directory.Permits.ShouldNotContainKey(fixture.InteractionId);
        fixture.Directory.Acknowledgements.ShouldBeEmpty(); fixture.Directory.Leases.ShouldBeEmpty(); fixture.Interaction.ShouldBeNull(); fixture.Scheduled.ShouldBeEmpty();
        string before = fixture.Snapshot(); fixture.Restart(); fixture.Snapshot().ShouldBe(before);
    }

    private static Task RegisterCompetingPermitAsync(AutomaticInteractionReplayFixture fixture, InteractionPermit p, int ordinal, CancellationToken ct)
    {
        string client = "other-client-" + ordinal;
        string otherId = ConversationAgentStateIdentity.Interaction(p.TenantId, p.AgentId, p.ConversationId, p.CallerPartyId, client);
        var other = p with { AgentInteractionId = otherId, ClientIdempotencyKey = client, PermitId = ConversationAgentStateIdentity.Permit(p.TenantId, otherId),
            CreationOutboxId = ConversationAgentStateIdentity.CreationOutbox(p.TenantId, otherId), Request = p.Request with { Target = p.Request.Target with { AgentInteractionId = otherId } } };
        return InteractionCommandDispatch.SendAsync(fixture, p.TenantId, ConversationAgentStateIdentity.Domain, ConversationAgentStateIdentity.Directory(p.TenantId, p.ConversationId),
            other.PermitId, new RegisterInteractionPermit(other, fixture.Directory?.Revision ?? 0), ct);
    }

    [Fact]
    public async Task RejectionOnlyOwnerHistoriesRecoverWhenTheExactPermitBecomesVisible()
    {
        var fixture = new AutomaticInteractionReplayFixture { PersistRejections = true };
        var dispatcher = Substitute.For<IAgentCommandDispatcher>(); var reader = Substitute.For<IConversationAgentStateReader>();
        AgentCommandIntent? pending = null; int observations = 0;
        dispatcher.DispatchAsync(Arg.Any<AgentCommandIntent>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var intent = call.ArgAt<AgentCommandIntent>(0); var ct = call.ArgAt<CancellationToken>(1);
            if (intent.CommandType != nameof(RegisterInteractionPermit)) { return await fixture.DispatchAsync(intent, ct); }
            var command = JsonSerializer.Deserialize<RegisterInteractionPermit>(intent.Payload).ShouldNotBeNull();
            await InteractionCommandDispatch.SendAsync(fixture, intent.TenantId, intent.Domain, intent.AggregateId, command.Permit.PermitId, command with { ExpectedRevision = 1 }, ct);
            await InteractionCommandDispatch.SendAsync(fixture, intent.TenantId, "agent-interaction", fixture.InteractionId, command.Permit.CreationOutboxId,
                new CreatePermittedAgentInteraction(command.Permit, 1, 1), ct);
            fixture.Restart(); fixture.Directory!.Revision.ShouldBe(0); fixture.Directory.TenantId.ShouldBeEmpty(); fixture.Interaction!.IsRequested.ShouldBeFalse();
            pending = intent; return new SubmitCommandResponse(intent.CorrelationId, MessageId: intent.MessageId);
        });
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            if (++observations == 4 && pending is not null) { var append = pending; pending = null; await fixture.DispatchAsync(append, call.ArgAt<CancellationToken>(2)); }
            return fixture.Directory;
        });
        var recovery = new InteractionOutboxRecoveryOrchestrator(reader, fixture, dispatcher, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, reader, dispatcher, recovery);
        (await intake.ExecuteAsync(fixture.Request, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        fixture.Directory!.Permits.Count.ShouldBe(1); fixture.Directory.Acknowledgements.Count.ShouldBe(1);
        fixture.Interaction!.PermittedCreation!.Permit.AgentInteractionId.ShouldBe(fixture.InteractionId); fixture.Scheduled.ShouldBe([fixture.InteractionId]); pending.ShouldBeNull();
        string before = fixture.Snapshot(); fixture.Restart(); fixture.Snapshot().ShouldBe(before);
        int accepted = fixture.AcceptedCommandIds.Count;
        (await fixture.Recovery().RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        fixture.AcceptedCommandIds.Count.ShouldBe(accepted);
    }

    /// <summary>Both directory entry points refuse established foreign scope before Closing or revision use.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(17)]
    [InlineData(-1)]
    public async Task EstablishedForeignDirectoryScopeRemainsNotFoundWithoutEffects(long revision)
    {
        foreach (bool intake in new[] { false, true })
        foreach (bool foreignTenant in new[] { false, true })
        foreach (bool closing in new[] { false, true })
        {
            var fixture = new AutomaticInteractionReplayFixture();
            var foreign = JsonSerializer.Deserialize<ConversationAgentStateState>(JsonSerializer.Serialize(new
            {
                TenantId = foreignTenant ? "foreign-tenant" : AutomaticInteractionReplayFixture.Tenant,
                ConversationId = foreignTenant ? AutomaticInteractionReplayFixture.Conversation : "foreign-conversation",
                Revision = revision,
                DeletionRequestId = closing ? "foreign-closing" : null,
            })).ShouldNotBeNull();
            string before = JsonSerializer.Serialize(foreign);
            var reader = Substitute.For<IConversationAgentStateReader>();
            reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(foreign);
            var recovery = new InteractionOutboxRecoveryOrchestrator(reader, fixture, fixture, fixture);
            var result = intake
                ? await new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, reader, fixture, recovery)
                    .ExecuteAsync(fixture.Request, CancellationToken.None)
                : await recovery.RecoverAsync(AutomaticInteractionReplayFixture.Tenant, AutomaticInteractionReplayFixture.Conversation, fixture.InteractionId, CancellationToken.None);
            result.Status.ShouldBe(AutomaticInteractionIntakeStatus.NotFound); result.AgentInteractionId.ShouldBeNull();
            fixture.Dispatches.ShouldBeEmpty(); fixture.Scheduled.ShouldBeEmpty(); fixture.Directory.ShouldBeNull(); fixture.Interaction.ShouldBeNull();
            JsonSerializer.Serialize(foreign).ShouldBe(before);
        }
    }

    /// <summary>Cancellation during the owner read wins over foreign-scope classification without effects.</summary>
    [Fact]
    public async Task CancellationDuringForeignDirectoryReadRemainsCancellation()
    {
        var fixture = new AutomaticInteractionReplayFixture();
        using var cancellation = new CancellationTokenSource();
        var foreign = new ConversationAgentStateState();
        foreign.Apply(new ConversationDeletionClosingEntered("foreign-tenant", AutomaticInteractionReplayFixture.Conversation, "closing", [], 1));
        var reader = Substitute.For<IConversationAgentStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            cancellation.Cancel(); return foreign;
        });
        var recovery = new InteractionOutboxRecoveryOrchestrator(reader, fixture, fixture, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, reader, fixture, recovery);
        var operation = intake.ExecuteAsync(fixture.Request, cancellation.Token);
        (await Should.ThrowAsync<OperationCanceledException>(() => operation)).CancellationToken.ShouldBe(cancellation.Token);
        operation.IsCanceled.ShouldBeTrue(); fixture.Dispatches.ShouldBeEmpty(); fixture.Scheduled.ShouldBeEmpty();
        fixture.Directory.ShouldBeNull(); fixture.Interaction.ShouldBeNull();
    }

    /// <summary>An unbound rejection-only directory is a valid initial observation, as is an absent owner.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyDirectoryObservationAllowsExactIntakeRecovery(bool rejectionOnly)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var empty = rejectionOnly ? new ConversationAgentStateState() : null;
        var reader = Substitute.For<IConversationAgentStateReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => fixture.Directory ?? empty);
        var recovery = new InteractionOutboxRecoveryOrchestrator(reader, fixture, fixture, fixture);
        var intake = new AutomaticInteractionIntakeOrchestrator(fixture, Substitute.For<IAgentDependencyAvailability>(), fixture, reader, fixture, recovery);
        (await intake.ExecuteAsync(fixture.Request, CancellationToken.None)).Status.ShouldBe(AutomaticInteractionIntakeStatus.Scheduled);
        fixture.Directory!.Permits.ShouldContainKey(fixture.InteractionId);
        fixture.Interaction!.PermittedCreation!.Permit.AgentInteractionId.ShouldBe(fixture.InteractionId);
        fixture.Scheduled.ShouldBe([fixture.InteractionId]);
        string before = fixture.Snapshot(); fixture.Restart(); fixture.Snapshot().ShouldBe(before);
    }
}
