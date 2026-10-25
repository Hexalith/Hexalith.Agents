using System.Security.Claims;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Services.Gateways;
using Hexalith.Agents.UI.State;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Memory-only circuit retention, scope isolation and release of sensitive setup intents.</summary>
public sealed class AgentSetupAttemptStoreTests : AgentsTestContext
{
    public AgentSetupAttemptStoreTests()
    {
        Authorization.SetAuthorized("alice");
        Authorization.SetClaims(new Claim("sub", "alice"), new Claim("tenantId", "tenant-a"));
        Services.Configure<AgentSetupTargetOptions>(options => options.AgentId = "agent-a");
    }

    [Fact]
    public async Task SameScopeRetainsExactPayloadIdentitiesDraftsAndReceiptButNewCircuitStartsEmpty()
    {
        AgentSetupAttemptStore store = Services.GetRequiredService<AgentSetupAttemptStore>();
        AgentSetupScope scope = (await store.GetScopeAsync()).ShouldNotBeNull();
        RetainedAgentSetupAttempt state = State();
        store.TryBegin(scope, state).ShouldBeTrue();
        AgentSetupScope sameScope = (await store.GetScopeAsync()).ShouldNotBeNull();
        sameScope.ShouldBeSameAs(scope);
        store.Load(sameScope).ShouldBeSameAs(state);
        store.Load(scope)!.Attempt.Options.ShouldBeSameAs(state.Attempt.Options);
        store.Load(scope)!.Attempt.Configuration!.Instructions.ShouldBe("private exact command");
        store.TryBegin(scope, State()).ShouldBeFalse();

        using var newCircuit = new AgentSetupAttemptStore(Services.GetRequiredService<AuthenticationStateProvider>(),
            Services.GetRequiredService<IOptions<AgentSetupTargetOptions>>());
        newCircuit.Load(await newCircuit.GetScopeAsync()).ShouldBeNull();
        // Neither store has any browser/durable transport dependency; unacknowledged reload recovery is unsupported.
        JSInterop.Invocations.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("user")]
    [InlineData("tenant")]
    [InlineData("agent")]
    [InlineData("logout")]
    public async Task ScopeChangeReleasesSensitiveStateAndOldPageCannotRestoreIt(string change)
    {
        AgentSetupAttemptStore store = Services.GetRequiredService<AgentSetupAttemptStore>();
        AgentSetupScope oldScope = (await store.GetScopeAsync()).ShouldNotBeNull();
        RetainedAgentSetupAttempt state = State();
        store.TryBegin(oldScope, state).ShouldBeTrue();
        switch (change)
        {
            case "user":
                Authorization.SetClaims(new Claim("sub", "bob"), new Claim("tenantId", "tenant-a"));
                break;
            case "tenant":
                Authorization.SetClaims(new Claim("sub", "alice"), new Claim("tenantId", "tenant-b"));
                break;
            case "agent":
                Services.GetRequiredService<IOptions<AgentSetupTargetOptions>>().Value.AgentId = "agent-b";
                break;
            default:
                Authorization.SetNotAuthorized();
                break;
        }

        store.IsCurrent(oldScope).ShouldBeFalse();
        AgentSetupScope? newScope = await store.GetScopeAsync();
        store.Load(newScope).ShouldBeNull();
        store.Save(oldScope, state);
        store.TryBegin(oldScope, state).ShouldBeFalse();
        store.Load(newScope).ShouldBeNull();
    }

    [Fact]
    public async Task ResolvingOrAbandoningReleasesContentAndStaleWritesCannotReplaceTheNextAttempt()
    {
        AgentSetupAttemptStore store = Services.GetRequiredService<AgentSetupAttemptStore>();
        AgentSetupScope scope = (await store.GetScopeAsync()).ShouldNotBeNull();
        RetainedAgentSetupAttempt old = State();
        store.TryBegin(scope, old).ShouldBeTrue();
        store.Release(scope, State().Attempt);
        store.Load(scope).ShouldBeSameAs(old);
        store.Release(scope, old.Attempt);
        store.Load(scope).ShouldBeNull();
        RetainedAgentSetupAttempt next = State();
        store.TryBegin(scope, next).ShouldBeTrue();
        store.Save(scope, old);
        store.Release(scope, old.Attempt);
        store.Load(scope).ShouldBeSameAs(next);
        store.Dispose();
        store.Load(scope).ShouldBeNull();
        store.TryBegin(scope, old).ShouldBeFalse();
    }

    [Theory]
    [InlineData("subject-conflict")]
    [InlineData("tenant-conflict")]
    [InlineData("missing-subject")]
    [InlineData("missing-tenant")]
    [InlineData("multiple-identities")]
    [InlineData("borrowed-tenant")]
    [InlineData("unauthenticated")]
    public async Task MissingOrAmbiguousAuthenticatedBindingCannotRetainInstructions(string failure)
    {
        var claims = new List<Claim>();
        if (failure != "missing-subject")
        {
            claims.Add(new Claim("sub", "alice"));
        }
        if (failure is not "missing-tenant" and not "borrowed-tenant")
        {
            claims.Add(new Claim("tenantId", "tenant-a"));
        }
        if (failure == "subject-conflict")
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, "bob"));
        }
        if (failure == "tenant-conflict")
        {
            claims.Add(new Claim("tid", "tenant-b"));
        }
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, failure == "unauthenticated" ? null : "test"));
        if (failure is "multiple-identities" or "borrowed-tenant")
        {
            principal.AddIdentity(new ClaimsIdentity([new Claim("tenantId", "tenant-a")],
                failure == "multiple-identities" ? "second-authentication" : null));
        }
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(Task.FromResult(new AuthenticationState(principal)));
        using var store = new AgentSetupAttemptStore(authentication, Options.Create(new AgentSetupTargetOptions { AgentId = "agent-a" }));
        (await store.GetScopeAsync()).ShouldBeNull();
        store.TryBegin(null, State()).ShouldBeFalse();
    }

    [Fact]
    public async Task SameBindingAuthenticationRefreshPreservesExactLeaseAttemptAndReceipt()
    {
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var store = new AgentSetupAttemptStore(authentication, Options.Create(new AgentSetupTargetOptions { AgentId = "agent-a" }));
        AgentSetupScope scope = (await store.GetScopeAsync()).ShouldNotBeNull();
        RetainedAgentSetupAttempt retained = State();
        store.TryBegin(scope, retained).ShouldBeTrue();
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
        (await store.GetScopeAsync()).ShouldBeSameAs(scope);
        store.Load(scope).ShouldBeSameAs(retained);
        store.Load(scope)!.PendingWrite!.Acceptance.ShouldBeSameAs(retained.PendingWrite!.Acceptance);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("changed")]
    [InlineData("faulted")]
    [InlineData("canceled")]
    [InlineData("disposed")]
    public async Task PendingAuthenticationFailsClosedUntilTheSuppliedBindingIsResolved(string completion)
    {
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var store = new AgentSetupAttemptStore(authentication, Options.Create(new AgentSetupTargetOptions { AgentId = "agent-a" }));
        AgentSetupScope scope = (await store.GetScopeAsync()).ShouldNotBeNull();
        RetainedAgentSetupAttempt retained = State();
        store.TryBegin(scope, retained).ShouldBeTrue();
        var pending = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        authentication.Publish(pending.Task);
        store.IsCurrent(scope).ShouldBeFalse();
        store.Load(scope).ShouldBeNull();
        store.TryBegin(scope, State()).ShouldBeFalse();
        Task<AgentSetupScope?> resolving = store.GetScopeAsync();
        resolving.IsCompleted.ShouldBeFalse();
        if (completion == "disposed")
        {
            store.Dispose();
        }
        if (completion == "faulted")
        {
            pending.SetException(new InvalidOperationException("private authentication failure"));
        }
        else if (completion == "canceled")
        {
            pending.SetCanceled();
        }
        else
        {
            pending.SetResult(ControlledSetupAuthenticationStateProvider.State(completion == "changed" ? "bob" : "alice"));
        }
        AgentSetupScope? resolved = await resolving;
        if (completion == "same")
        {
            resolved.ShouldBeSameAs(scope);
            store.Load(scope).ShouldBeSameAs(retained);
        }
        else
        {
            store.IsCurrent(scope).ShouldBeFalse();
            store.Load(resolved).ShouldBeNull();
            if (completion != "changed")
            {
                resolved.ShouldBeNull();
            }
        }
    }

    [Fact]
    public async Task StaleAuthenticationCompletionCannotRestoreAFormerUserOrEraseTheCurrentAttempt()
    {
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var store = new AgentSetupAttemptStore(authentication, Options.Create(new AgentSetupTargetOptions { AgentId = "agent-a" }));
        AgentSetupScope oldScope = (await store.GetScopeAsync()).ShouldNotBeNull();
        store.TryBegin(oldScope, State()).ShouldBeTrue();
        var stale = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        authentication.Publish(stale.Task);
        Task<AgentSetupScope?> staleRead = store.GetScopeAsync();
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State("bob")));
        AgentSetupScope newScope = (await store.GetScopeAsync()).ShouldNotBeNull();
        RetainedAgentSetupAttempt current = State();
        store.TryBegin(newScope, current).ShouldBeTrue();
        stale.SetResult(ControlledSetupAuthenticationStateProvider.State());
        (await staleRead).ShouldBeNull();
        store.IsCurrent(oldScope).ShouldBeFalse();
        store.Load(newScope).ShouldBeSameAs(current);
    }

    [Fact]
    public async Task ObservedTargetChangeCannotReviveTheOldLeaseWhenTheTargetChangesBack()
    {
        AgentSetupAttemptStore store = Services.GetRequiredService<AgentSetupAttemptStore>();
        AgentSetupScope oldScope = (await store.GetScopeAsync()).ShouldNotBeNull();
        store.TryBegin(oldScope, State()).ShouldBeTrue();
        AgentSetupTargetOptions options = Services.GetRequiredService<IOptions<AgentSetupTargetOptions>>().Value;
        options.AgentId = "agent-b";
        store.IsCurrent(oldScope).ShouldBeFalse();
        options.AgentId = "agent-a";
        store.IsCurrent(oldScope).ShouldBeFalse();
        store.Load(oldScope).ShouldBeNull();
        AgentSetupScope newScope = (await store.GetScopeAsync()).ShouldNotBeNull();
        newScope.ShouldNotBeSameAs(oldScope);
        store.Load(newScope).ShouldBeNull();
    }

    private static RetainedAgentSetupAttempt State()
        => new(new AgentSetupAttempt(new AgentOperationOptions("correlation", "idempotency"),
                AgentSetupAttemptKind.Configuration, new UpdateAgentConfiguration("name", "description", "private exact command")),
            AgentSetupWriteResult.AwaitingProjection(new AgentCommandAcceptance("agent-a", "message", "correlation",
                AgentSetupTruthState.AuthoritativePending, AgentSetupWriteEffect.Applied, 7)),
            new("draft name", "draft description", AgentResponseMode.Confirmation), "private unrelated draft",
            new("saved name", "saved description", AgentResponseMode.Automatic));
}
