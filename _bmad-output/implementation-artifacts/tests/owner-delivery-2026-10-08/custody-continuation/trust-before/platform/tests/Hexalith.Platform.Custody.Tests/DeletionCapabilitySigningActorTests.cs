using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Dapr.Actors.Runtime;
using Hexalith.EventStore.Testing.Fakes;
using NSubstitute;
using Shouldly;

namespace Hexalith.Platform.Custody.Tests;

/// <summary>Actual signer intent/outcome durability with synthetic qualified guard/key ports and real BCL signatures.</summary>
public sealed class DeletionCapabilitySigningActorTests
{
    private static DeletionBatchCapabilityV1 Payload() => new("issuer", "protection-v1", "tenant-a", "request-1", "seal-1", "accepted", 0,
        "batch-1", new string('A', 64), "tenant-a:governance:guard-1", 7, 1, 1, "key-v1");
    private static DeletionCapabilitySigningResult Signed(DeletionBatchCapabilityV1 payload, ECDsa key)
    {
        var profile = new DeletionCapabilityTrustProfile(payload.Issuer, payload.Audience, payload.TenantId, payload.CapabilityKeyVersion, "anchor-1", "anchor-v1");
        return new(new(DeletionBatchCapabilityCodec.SigningRequestId(payload), payload, DeletionCapabilitySigningState.Signed,
            DeletionBatchCapabilityCodec.Sign(payload, profile, key), profile.PublicAnchorId, profile.PublicAnchorVersion), key.ExportSubjectPublicKeyInfo());
    }
    private static DeletionCapabilitySigningActor Actor(DeletionBatchCapabilityV1 payload, IActorStateManager backend,
        IDeletionCapabilitySigningAuthority? authority = null, IDeletionCapabilitySigningProvider? provider = null)
    {
        var actor = new DeletionCapabilitySigningActor(ActorHost.CreateForTest<DeletionCapabilitySigningActor>(new ActorTestOptions
            { ActorId = new(DeletionCapabilitySigningActor.GetActorId(payload)) }), authority, provider);
        typeof(Dapr.Actors.Runtime.Actor).GetProperty("StateManager", BindingFlags.Public | BindingFlags.Instance)!.SetValue(actor, backend); return actor;
    }
    private static IDeletionCapabilitySigningAuthority Authority()
    {
        var authority = Substitute.For<IDeletionCapabilitySigningAuthority>();
        authority.AuthorizeAsync(Arg.Any<DeletionBatchCapabilityV1>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true); return authority;
    }

    /// <summary>Lost signing response is recovered only through exact result lookup; stored signature remains identical through serialized restart.</summary>
    [Fact]
    public async Task LostSigningResponseReusesDurableExactSignatureWithoutResigning()
    {
        var payload = Payload(); string id = DeletionBatchCapabilityCodec.SigningRequestId(payload); var backend = new InMemoryStateManager();
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256); var signed = Signed(payload, key);
        var provider = Substitute.For<IDeletionCapabilitySigningProvider>();
        provider.SignAsync(payload, id, Arg.Any<CancellationToken>()).Returns(Task.FromException<DeletionCapabilitySigningResult>(new HttpRequestException("Controlled response lost after backend signature retention.")));
        provider.LookupAsync(payload, id, Arg.Any<CancellationToken>()).Returns(signed);
        var actor = Actor(payload, backend, Authority(), provider);
        (await actor.SignAsync(payload)).State.ShouldBe(DeletionCapabilitySigningState.Unknown);
        backend.CommittedState.Single().Value.ShouldBeOfType<DeletionCapabilitySigningOutcome>().State.ShouldBe(DeletionCapabilitySigningState.Unknown);
        (await actor.SignAsync(payload)).ShouldBe(signed.Outcome);
        var saved = backend.CommittedState.Single(); var restored = new InMemoryStateManager();
        await restored.SetStateAsync(saved.Key, JsonSerializer.Deserialize<DeletionCapabilitySigningOutcome>(JsonSerializer.SerializeToUtf8Bytes(saved.Value))!, TestContext.Current.CancellationToken);
        await restored.SaveStateAsync(TestContext.Current.CancellationToken);
        (await Actor(payload, restored).LookupAsync(payload)).ShouldBe(signed.Outcome);
        await provider.Received(1).SignAsync(payload, id, Arg.Any<CancellationToken>()); await provider.Received(1).LookupAsync(payload, id, Arg.Any<CancellationToken>());
        JsonSerializer.Serialize(saved.Value).ShouldNotContain("CommittedIssuedGuardRevision");
    }

    /// <summary>Missing or denied independent recorded authorization never invokes signing; healthy key configuration alone grants nothing.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingOrDeniedAuthorityCannotSign(bool denied)
    {
        var payload = Payload(); var backend = new InMemoryStateManager(); var provider = Substitute.For<IDeletionCapabilitySigningProvider>();
        var authority = Authority(); authority.AuthorizeAsync(Arg.Any<DeletionBatchCapabilityV1>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        (await Actor(payload, backend, denied ? authority : null, provider).SignAsync(payload)).State
            .ShouldBe(denied ? DeletionCapabilitySigningState.Denied : DeletionCapabilitySigningState.Unavailable);
        await provider.DidNotReceive().SignAsync(Arg.Any<DeletionBatchCapabilityV1>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        if (denied) { backend.CommittedState.Single().Value.ShouldBeOfType<DeletionCapabilitySigningOutcome>().State.ShouldBe(DeletionCapabilitySigningState.Denied); }
        else { backend.CommittedState.ShouldBeEmpty(); }
    }

    /// <summary>Provider payload/header/public-key substitution or oversized public anchor cannot release a Signed artifact.</summary>
    [Theory]
    [InlineData("payload")]
    [InlineData("anchor")]
    [InlineData("header")]
    [InlineData("anchor-length")]
    public async Task MalformedProviderResultCannotBeRetainedAsSigned(string vector)
    {
        var payload = Payload(); var backend = new InMemoryStateManager(); using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256); var result = Signed(payload, key);
        result = vector switch {
            "payload" => result with { Outcome = result.Outcome with { Payload = payload with { DestructionSealId = "changed" } } },
            "anchor" => result with { PublicAnchorSubjectPublicKeyInfo = other.ExportSubjectPublicKeyInfo() },
            "header" => result with { Outcome = result.Outcome with { DetachedJws = ExportManifestSignatureCore.Sign(new("tenant-a", "export-1", 1, "key-v1"),
                DeletionBatchCapabilityCodec.CanonicalPayload(payload), key) } },
            _ => result with { PublicAnchorSubjectPublicKeyInfo = new byte[513] }
        };
        var provider = Substitute.For<IDeletionCapabilitySigningProvider>();
        provider.SignAsync(payload, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
        var outcome = await Actor(payload, backend, Authority(), provider).SignAsync(payload);
        outcome.State.ShouldBe(vector == "payload" ? DeletionCapabilitySigningState.Conflict : DeletionCapabilitySigningState.Unknown);
        outcome.DetachedJws.ShouldBeNull(); backend.CommittedState.Single().Value.ShouldBeOfType<DeletionCapabilitySigningOutcome>().DetachedJws.ShouldBeNull();
    }

    /// <summary>Failed or lost-ack reservation persistence prevents signing and never certifies cached staged state.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedIntentSaveCannotInvokeSigner(bool commitBeforeFault)
    {
        var payload = Payload(); var backend = new InMemoryStateManager(); var manager = Substitute.For<IActorStateManager>();
        var provider = Substitute.For<IDeletionCapabilitySigningProvider>();
        manager.ClearCacheAsync(Arg.Any<CancellationToken>()).Returns(call => backend.ClearCacheAsync(call.Arg<CancellationToken>()));
        manager.TryGetStateAsync<DeletionCapabilitySigningOutcome>(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => backend.TryGetStateAsync<DeletionCapabilitySigningOutcome>(call.Arg<string>(), call.Arg<CancellationToken>()));
        manager.SetStateAsync(Arg.Any<string>(), Arg.Any<DeletionCapabilitySigningOutcome>(), Arg.Any<CancellationToken>()).Returns(call => backend.SetStateAsync(call.Arg<string>(), call.Arg<DeletionCapabilitySigningOutcome>(), call.Arg<CancellationToken>()));
        manager.SaveStateAsync(Arg.Any<CancellationToken>()).Returns(async call =>
        { if (commitBeforeFault) { await backend.SaveStateAsync(call.Arg<CancellationToken>()); } throw new HttpRequestException("Controlled signing intent save fault."); });
        await Should.ThrowAsync<HttpRequestException>(() => Actor(payload, manager, Authority(), provider).SignAsync(payload));
        if (commitBeforeFault) { backend.CommittedState.Single().Value.ShouldBeOfType<DeletionCapabilitySigningOutcome>().State.ShouldBe(DeletionCapabilitySigningState.Unknown); }
        else { backend.CommittedState.ShouldBeEmpty(); }
        (await Actor(payload, backend).LookupAsync(payload)).State.ShouldBe(DeletionCapabilitySigningState.Unavailable);
        await provider.DidNotReceive().SignAsync(Arg.Any<DeletionBatchCapabilityV1>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Changed tenant/seal/compare/attempt is another exact request address and cannot overwrite the original request actor.</summary>
    [Fact]
    public async Task ChangedRequestScopeCannotOverwriteStoredResult()
    {
        var payload = Payload(); var backend = new InMemoryStateManager(); using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var result = Signed(payload, key); var provider = Substitute.For<IDeletionCapabilitySigningProvider>();
        provider.SignAsync(payload, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(result);
        var actor = Actor(payload, backend, Authority(), provider); (await actor.SignAsync(payload)).ShouldBe(result.Outcome);
        foreach (var changed in new[] { payload with { TenantId = "tenant-b" }, payload with { DestructionSealId = "new-seal" },
            payload with { IntendedIssuedGuardRevision = 8 }, payload with { SigningAttemptOrdinal = 2 } })
        { await Should.ThrowAsync<ArgumentException>(() => actor.SignAsync(changed)); }
        backend.CommittedState.Single().Value.ShouldBeOfType<DeletionCapabilitySigningOutcome>().ShouldBe(result.Outcome);
    }
}
