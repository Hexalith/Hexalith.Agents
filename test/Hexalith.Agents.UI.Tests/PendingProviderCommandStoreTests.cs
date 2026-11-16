using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.State;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Browser-session pending command identity retention and isolation.</summary>
public sealed class PendingProviderCommandStoreTests
{
    [Fact]
    public async Task Refresh_retains_safe_identity_and_another_login_cannot_inherit_its_lock()
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        var submitted = new PendingProviderCommand("DataHandlingAcceptance", ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "tenant-a",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "03FB4931684203D8BBB7878D8B", "09DB934C93A92F10803136E932",
                AgentSetupTruthState.Submitted));

        await new BrowserSessionPendingProviderCommandStore(javascript, authentication).SaveAsync(submitted);
        PendingProviderCommand restored = (await new BrowserSessionPendingProviderCommandStore(javascript, authentication)
            .LoadAsync()).ShouldHaveSingleItem();
        restored.Acceptance.MessageId.ShouldBe("03FB4931684203D8BBB7878D8B");
        javascript.Values.ShouldHaveSingleItem().Value.ShouldNotContain("ConfigurationReferenceId");

        authentication.GetAuthenticationStateAsync().Returns(State("bob", "tenant-a"));
        (await new BrowserSessionPendingProviderCommandStore(javascript, authentication).LoadAsync()).ShouldBeEmpty();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-b"));
        (await new BrowserSessionPendingProviderCommandStore(javascript, authentication).LoadAsync()).ShouldBeEmpty();

        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        var refreshed = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        await refreshed.RemoveAsync(submitted.Family, submitted.ResourceKey, "wrong-message");
        (await refreshed.LoadAsync()).ShouldHaveSingleItem();
        await refreshed.RemoveAsync(submitted.Family, submitted.ResourceKey, "03FB4931684203D8BBB7878D8B");
        (await refreshed.LoadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Operator_without_tenant_claim_uses_reserved_system_scope_and_storage_failure_stays_closed()
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(State("operator", null, platformOperator: true));
        var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        var submitted = new PendingProviderCommand("ProviderCatalogMutation", ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "system",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "0C74CBAF5350A3C7F1715880E0", "0420762CB0B8921FB75ABDC446",
                AgentSetupTruthState.Submitted));

        await store.SaveAsync(submitted);
        (await new BrowserSessionPendingProviderCommandStore(javascript, authentication).LoadAsync())
            .ShouldHaveSingleItem().Acceptance.MessageId.ShouldBe("0C74CBAF5350A3C7F1715880E0");

        authentication.GetAuthenticationStateAsync().Returns(State("operator", null));
        await Should.ThrowAsync<InvalidOperationException>(() => store.LoadAsync());
    }

    [Fact]
    public async Task StorageReadFailureStaysClosedAndPreservesItsCause()
    {
        var failure = new JSException("Session storage denied.");
        var javascript = new InMemorySessionStorageJsRuntime { ReadFailure = failure };
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => store.LoadAsync());

        exception.InnerException.ShouldBeSameAs(failure);
        exception.Message.ShouldContain("could not be read");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StorageWriteFailureStaysClosedAndRetainsPendingIdentity(bool remove)
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        var submitted = new PendingProviderCommand("ProviderCatalogMutation", ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "system",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "03FB4931684203D8BBB7878D8B", "09DB934C93A92F10803136E932",
                AgentSetupTruthState.Submitted));
        await store.SaveAsync(submitted);
        javascript.RejectWrites = true;

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => remove
            ? store.RemoveAsync(submitted.Family, submitted.ResourceKey, submitted.Acceptance.MessageId)
            : store.SaveAsync(submitted));

        exception.InnerException.ShouldBeOfType<JSException>();
        exception.Message.ShouldContain("could not be saved");
        (await store.LoadAsync()).ShouldHaveSingleItem().Acceptance.MessageId.ShouldBe("03FB4931684203D8BBB7878D8B");
    }

    [Fact]
    public async Task SameStoreReadsTheCurrentPrincipalOnEachOperation()
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        AuthenticationStateProvider authentication = Substitute.For<AuthenticationStateProvider>();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        var submitted = new PendingProviderCommand("ProviderCatalogMutation", ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "system",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "03FB4931684203D8BBB7878D8B", "09DB934C93A92F10803136E932",
                AgentSetupTruthState.Submitted));
        await store.SaveAsync(submitted);

        authentication.GetAuthenticationStateAsync().Returns(State("bob", "tenant-a"));
        (await store.LoadAsync()).ShouldBeEmpty();
        authentication.GetAuthenticationStateAsync().Returns(State("alice", "tenant-a"));
        (await store.LoadAsync()).ShouldHaveSingleItem().Acceptance.MessageId.ShouldBe("03FB4931684203D8BBB7878D8B");
    }

    [Theory]
    [InlineData("load", false)]
    [InlineData("load", true)]
    [InlineData("save", false)]
    [InlineData("save", true)]
    [InlineData("remove", false)]
    [InlineData("remove", true)]
    public async Task SuspendedReadRefusesDisclosureAndRedirectedSaveAfterAuthenticationChange(string operation, bool changeBack)
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        PendingProviderCommand command = ValidCommand();
        await store.SaveAsync(command);
        string original = javascript.Values.ShouldHaveSingleItem().Value;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        javascript.Reading = () => { entered.TrySetResult(); return resume.Task; };
        Task inFlight = operation switch
        {
            "load" => store.LoadAsync(),
            "save" => store.SaveAsync(command),
            _ => store.RemoveAsync(command.Family, command.ResourceKey, command.Acceptance.MessageId),
        };
        await entered.Task;
        Task queued = store.RemoveAsync(command.Family, command.ResourceKey, command.Acceptance.MessageId);
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State("bob")));
        if (changeBack)
        {
            authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
        }
        resume.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(() => inFlight);
        await Should.ThrowAsync<InvalidOperationException>(() => queued);
        javascript.Values.ShouldHaveSingleItem().Value.ShouldBe(original);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeliveredOldKeyWriteReportsTransitionWithoutWritingTheNewAccountKey(bool remove)
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        PendingProviderCommand command = ValidCommand();
        if (remove)
        {
            await store.SaveAsync(command);
        }
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        javascript.Written = () => { entered.TrySetResult(); return resume.Task; };
        Task save = remove ? store.RemoveAsync(command.Family, command.ResourceKey, command.Acceptance.MessageId)
            : store.SaveAsync(command);
        await entered.Task;
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State("bob")));
        resume.SetResult();
        await Should.ThrowAsync<InvalidOperationException>(() => save);
        javascript.Values.Count.ShouldBe(1);
        javascript.Written = null;
        (await store.LoadAsync()).ShouldBeEmpty();
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
        (await store.LoadAsync()).Count.ShouldBe(remove ? 0 : 1);
    }

    [Fact]
    public async Task AuthenticationChangeDuringStateAwaitAndDisposedStoresRefuseOperations()
    {
        var authentication = new ControlledSetupAuthenticationStateProvider();
        var suspended = new TaskCompletionSource<AuthenticationState>(TaskCreationOptions.RunContinuationsAsynchronously);
        authentication.Publish(suspended.Task);
        using var store = new BrowserSessionPendingProviderCommandStore(new InMemorySessionStorageJsRuntime(), authentication);
        Task load = store.LoadAsync();
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State("bob")));
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
        suspended.SetResult(ControlledSetupAuthenticationStateProvider.State());
        await Should.ThrowAsync<InvalidOperationException>(() => load);
        store.Dispose();
        await Should.ThrowAsync<ObjectDisposedException>(() => store.LoadAsync());
    }

    [Theory]
    [InlineData("null")]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("{}")]
    [InlineData("[null]")]
    [InlineData("[{}]")]
    [InlineData("[false]")]
    [InlineData("[")]
    public async Task CorruptRootOrMembersRefuseEveryOperationWithoutOverwriting(string corrupt)
        => await AssertCorruptionAsync(corrupt);

    [Theory]
    [InlineData("identity")]
    [InlineData("correlation")]
    [InlineData("family")]
    [InlineData("resource")]
    [InlineData("tenant")]
    [InlineData("version")]
    [InlineData("status")]
    [InlineData("truth")]
    [InlineData("missingTruth")]
    [InlineData("conflict")]
    public async Task InvalidRecordsRefuseEveryOperationWithoutOverwriting(string defect)
    {
        PendingProviderCommand valid = ValidCommand();
        PendingProviderCommand invalid = defect switch
        {
            "identity" => valid with { Acceptance = valid.Acceptance with { MessageId = "invalid" } },
            "correlation" => valid with { Acceptance = valid.Acceptance with { CorrelationId = "invalid" } },
            "family" => valid with { Family = "unknown" },
            "resource" => valid with { ResourceKey = "wrong" },
            "tenant" => valid with { TenantId = " " },
            "version" => valid with { ExpectedCapabilityVersion = 0 },
            "status" => valid with { ExpectedStatus = ProviderModelStatus.Unknown },
            "conflict" => valid with { Acceptance = valid.Acceptance with { MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAX" } },
            _ => valid,
        };
        string json = JsonSerializer.Serialize(defect == "conflict" ? new[] { valid, invalid } : [invalid]);
        json = defect switch
        {
            "truth" => json.Replace("Submitted", "future-unknown", StringComparison.Ordinal),
            "missingTruth" => json.Replace("\"TruthState\":\"Submitted\"", "\"Other\":1", StringComparison.Ordinal),
            _ => json,
        };
        await AssertCorruptionAsync(json);
    }

    [Fact]
    public async Task InvalidSaveAndReplacementCannotEraseAnExistingLockAndLegacyRecordsRemainValid()
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        using var store = new BrowserSessionPendingProviderCommandStore(javascript, new ControlledSetupAuthenticationStateProvider());
        PendingProviderCommand valid = ValidCommand();
        await store.SaveAsync(valid);
        string key = javascript.Values.ShouldHaveSingleItem().Key;
        JsonArray legacyRecords = JsonNode.Parse(JsonSerializer.Serialize(new[] { valid }))!.AsArray();
        legacyRecords[0]!.AsObject().Remove("IsPrepared");
        string legacy = legacyRecords.ToJsonString();
        legacy.ShouldNotContain("IsPrepared");
        javascript.Values[key] = legacy;
        (await store.LoadAsync()).ShouldHaveSingleItem().ShouldBe(valid);
        await Should.ThrowAsync<InvalidOperationException>(() => store.SaveAsync(valid with { ExpectedCapabilityVersion = -1 }));
        await Should.ThrowAsync<InvalidOperationException>(() => store.SaveAsync(valid with
        {
            Acceptance = valid.Acceptance with { MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAX" },
        }));
        javascript.Values[key].ShouldBe(legacy);
        javascript.Values[key] = "[]";
        (await store.LoadAsync()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ProviderCatalogMutation")]
    [InlineData("TenantProviderEnablement")]
    [InlineData("DataHandlingAcceptance")]
    public async Task PreparedUnknownRoundTripsAcrossRefreshAndSameIdentityAcknowledgement(string family)
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        var authentication = new ControlledSetupAuthenticationStateProvider();
        PendingProviderCommand prepared = PreparedCommand(family);
        using (var initial = new BrowserSessionPendingProviderCommandStore(javascript, authentication))
        {
            await initial.SaveAsync(prepared);
        }
        using var refreshed = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        (await refreshed.LoadAsync()).ShouldHaveSingleItem().ShouldBe(prepared);
        PendingProviderCommand acknowledged = prepared with
        {
            IsPrepared = false,
            Acceptance = prepared.Acceptance with { TruthState = AgentSetupTruthState.Submitted },
        };
        await refreshed.SaveAsync(acknowledged);
        using var secondRefresh = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        (await secondRefresh.LoadAsync()).ShouldHaveSingleItem().ShouldBe(acknowledged);
        await secondRefresh.RemoveAsync(family, prepared.ResourceKey, prepared.Acceptance.MessageId);
        (await secondRefresh.LoadAsync()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ProviderCatalogMutation", "subject")]
    [InlineData("ProviderCatalogMutation", "tenant")]
    [InlineData("ProviderCatalogMutation", "change-back")]
    [InlineData("TenantProviderEnablement", "subject")]
    [InlineData("TenantProviderEnablement", "tenant")]
    [InlineData("TenantProviderEnablement", "change-back")]
    [InlineData("DataHandlingAcceptance", "subject")]
    [InlineData("DataHandlingAcceptance", "tenant")]
    [InlineData("DataHandlingAcceptance", "change-back")]
    public async Task PersistedIdentityCannotBeAcknowledgedOrRemovedInAnotherAuthenticationGeneration(string family, string transition)
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        PendingProviderCommand prepared = PreparedCommand(family);
        await store.SaveAsync(prepared);
        KeyValuePair<string, string> original = javascript.Values.ShouldHaveSingleItem();
        authentication.Publish(Task.FromResult(transition == "tenant"
            ? ControlledSetupAuthenticationStateProvider.State(tenant: "tenant-b")
            : ControlledSetupAuthenticationStateProvider.State("bob")));
        if (transition == "change-back")
        {
            authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
        }
        PendingProviderCommand acknowledged = prepared with
        {
            IsPrepared = false,
            Acceptance = prepared.Acceptance with { TruthState = AgentSetupTruthState.Submitted },
        };
        await Should.ThrowAsync<InvalidOperationException>(() => store.SaveAsync(acknowledged));
        await Should.ThrowAsync<InvalidOperationException>(() => store.RemoveAsync(family, prepared.ResourceKey, prepared.Acceptance.MessageId));
        javascript.Values.ShouldHaveSingleItem().ShouldBe(original);
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
        using var refreshed = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        (await refreshed.LoadAsync()).ShouldHaveSingleItem().ShouldBe(prepared);
        await refreshed.SaveAsync(acknowledged);
        (await refreshed.LoadAsync()).ShouldHaveSingleItem().ShouldBe(acknowledged);
    }

    [Theory]
    [InlineData("Family")]
    [InlineData("ResourceKey")]
    [InlineData("Acceptance")]
    [InlineData("MessageId")]
    [InlineData("CorrelationId")]
    [InlineData("TruthState")]
    public async Task DuplicateJsonPropertyNamesRefuseEveryOperationWithoutOverwriting(string member)
    {
        string json = JsonSerializer.Serialize(new[] { ValidCommand() });
        string duplicate = member == "Acceptance" ? "null" : "\"conflicting-first-value\"";
        json = json.Replace($"\"{member}\":", $"\"{member}\":{duplicate},\"{member}\":", StringComparison.Ordinal);
        await AssertCorruptionAsync(json);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("acknowledge")]
    [InlineData("remove")]
    [InlineData("conflict")]
    public async Task DistinctRuntimesRetryValidatedStateWithoutLosingIndependentLocks(string mutation)
    {
        var shared = new Dictionary<string, string>(StringComparer.Ordinal);
        var firstRuntime = new InMemorySessionStorageJsRuntime(shared);
        var secondRuntime = new InMemorySessionStorageJsRuntime(shared);
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var first = new BrowserSessionPendingProviderCommandStore(firstRuntime, authentication);
        using var second = new BrowserSessionPendingProviderCommandStore(secondRuntime, authentication);
        PendingProviderCommand original = PreparedCommand("ProviderCatalogMutation");
        PendingProviderCommand other = original with
        {
            ResourceKey = ProviderCatalogIdentity.EntryId("openai", "other"),
            Acceptance = original.Acceptance with { ModelId = "other", MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAX" },
        };
        if (mutation is "acknowledge" or "remove")
        {
            await first.SaveAsync(original);
        }
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        firstRuntime.Comparing = () => { entered.TrySetResult(); return release.Task; };
        PendingProviderCommand candidate = mutation == "acknowledge" ? original with
        {
            IsPrepared = false,
            Acceptance = original.Acceptance with { TruthState = AgentSetupTruthState.Submitted },
        } : original;
        Task pending = mutation == "remove"
            ? first.RemoveAsync(original.Family, original.ResourceKey, original.Acceptance.MessageId)
            : first.SaveAsync(candidate);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await second.SaveAsync(mutation == "conflict" ? original with
        {
            Acceptance = original.Acceptance with { MessageId = other.Acceptance.MessageId },
        } : other);
        release.SetResult();
        if (mutation == "conflict")
        {
            await Should.ThrowAsync<InvalidOperationException>(() => pending);
            (await second.LoadAsync()).ShouldHaveSingleItem().Acceptance.MessageId.ShouldBe(other.Acceptance.MessageId);
        }
        else
        {
            await pending;
            IReadOnlyList<PendingProviderCommand> persisted = await second.LoadAsync();
            persisted.ShouldContain(other);
            persisted.Count.ShouldBe(mutation == "remove" ? 1 : 2);
            if (mutation != "remove") { persisted.ShouldContain(candidate); }
        }
        firstRuntime.Conflicts.ShouldBe(1);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("account")]
    [InlineData("corrupt")]
    [InlineData("contention")]
    public async Task ConflictedMutationRevalidatesCancellationAuthenticationAndCorruption(string interruption)
    {
        var shared = new Dictionary<string, string>(StringComparer.Ordinal);
        var firstRuntime = new InMemorySessionStorageJsRuntime(shared);
        var secondRuntime = new InMemorySessionStorageJsRuntime(shared);
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var first = new BrowserSessionPendingProviderCommandStore(firstRuntime, authentication);
        using var second = new BrowserSessionPendingProviderCommandStore(secondRuntime, authentication);
        PendingProviderCommand command = ValidCommand();
        using var cancellation = new CancellationTokenSource();
        int calls = 0;
        firstRuntime.Comparing = async () =>
        {
            calls++;
            if (interruption == "contention")
            {
                // Valid, distinct bytes force a fresh read until the bounded retry refusal.
                shared[shared.Keys.SingleOrDefault() ?? "unused"] = "[]";
                if (calls > 1) { shared[shared.Keys.Single()] = new string(' ', calls) + "[]"; }
            }
            else
            {
                await second.SaveAsync(command);
                if (interruption == "cancel") { cancellation.Cancel(); }
                if (interruption == "account")
                {
                    authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State("bob")));
                    authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
                }
                if (interruption == "corrupt") { shared[shared.Keys.Single()] = "null"; }
            }
        };
        if (interruption == "contention") { await second.SaveAsync(command); }
        if (interruption == "cancel")
        {
            await Should.ThrowAsync<OperationCanceledException>(() => first.SaveAsync(command, cancellation.Token));
        }
        else
        {
            await Should.ThrowAsync<InvalidOperationException>(() => first.SaveAsync(command, cancellation.Token));
        }
        if (interruption == "contention") { calls.ShouldBe(8); }
        if (interruption == "corrupt") { shared.Values.ShouldHaveSingleItem().ShouldBe("null"); }
        if (interruption is "cancel" or "account")
        {
            using var refreshed = new BrowserSessionPendingProviderCommandStore(secondRuntime, new ControlledSetupAuthenticationStateProvider());
            (await refreshed.LoadAsync()).ShouldHaveSingleItem().ShouldBe(command);
        }
    }

    private static PendingProviderCommand PreparedCommand(string family)
    {
        string entry = ProviderCatalogIdentity.EntryId("openai", "gpt-x");
        string tenant = family == "ProviderCatalogMutation" ? "system" : "tenant-a";
        return PendingProviderCommand.Prepare(family, family == "TenantProviderEnablement" ? $"{tenant}:{entry}" : entry,
            tenant, "openai", "gpt-x", "01ARZ3NDEKTSV4RRFFQ69G5FAV", "01ARZ3NDEKTSV4RRFFQ69G5FAW");
    }

    private static PendingProviderCommand ValidCommand()
        => new("ProviderCatalogMutation", ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "system",
            new("openai", "gpt-x", "01ARZ3NDEKTSV4RRFFQ69G5FAV", "01ARZ3NDEKTSV4RRFFQ69G5FAW", AgentSetupTruthState.Submitted));

    private static async Task AssertCorruptionAsync(string corrupt)
    {
        var javascript = new InMemorySessionStorageJsRuntime();
        using var store = new BrowserSessionPendingProviderCommandStore(javascript, new ControlledSetupAuthenticationStateProvider());
        PendingProviderCommand valid = ValidCommand();
        await store.SaveAsync(valid);
        string key = javascript.Values.ShouldHaveSingleItem().Key;
        javascript.Values[key] = corrupt;
        await Should.ThrowAsync<InvalidOperationException>(() => store.LoadAsync());
        await Should.ThrowAsync<InvalidOperationException>(() => store.SaveAsync(valid));
        await Should.ThrowAsync<InvalidOperationException>(() => store.RemoveAsync(valid.Family, valid.ResourceKey, valid.Acceptance.MessageId));
        javascript.Values[key].ShouldBe(corrupt);
    }

    private static Task<AuthenticationState> State(string userId, string? tenantId, bool platformOperator = false)
    {
        List<Claim> claims = [new Claim("sub", userId)];
        if (tenantId is not null)
        {
            claims.Add(new Claim("tenantId", tenantId));
        }

        if (platformOperator)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Agents.PlatformOperator"));
        }

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))));
    }
}
