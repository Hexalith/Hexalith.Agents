using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Hexalith.EventStore.Client.Events;
using Hexalith.EventStore.Contracts.Identity;

using Shouldly;

namespace Hexalith.EventStore.Client.Tests.Events;

/// <summary>Checks independently constructed selected logical-model bytes, strict schemas and current scoped trust.</summary>
public sealed class DaprLogicalClaimCodecTests
{
    private static readonly byte[] Registry = SHA256.HashData("local registry"u8);
    private static readonly byte[] Logical = Vector("application", "sha256");
    private static readonly byte[] SourceHash = Vector("source", "sha256");
    private static readonly DateTimeOffset Timestamp = DateTimeOffset.UnixEpoch.ToOffset(TimeSpan.FromHours(2));

    /// <summary>Compares every selected production preimage and hash with independent Python vectors.</summary>
    [Fact]
    public void IndependentSourceMetadataListAccumulatorAndClaimsMatchAllPythonVectors()
    {
        DaprLogicalSourceBinding source = Source();
        DaprLogicalClaimCodec.EncodeSource(source).ShouldBe(Vector("source", "preimage"));
        DaprLogicalClaimCodec.ComputeSourceBindingHash(source).ShouldBe(SourceHash);
        DaprLogicalClaimCodec.ComputeConsumedMetadataHash(Metadata(), Logical, SourceHash).ShouldBe(Vector("consumed", "sha256"));
        var entries = new[] { new DaprLogicalDigestEntry(1, Logical) };
        DaprLogicalClaimCodec.ComputeOrderedList(SourceHash, entries).ShouldBe(Vector("ordered_list", "sha256"));
        byte[] genesis = DaprLogicalClaimCodec.ComputeGenesis(SourceHash, Registry);
        genesis.ShouldBe(Vector("genesis", "sha256"));
        DaprLogicalClaimCodec.ComputeAccumulatorStep(SourceHash, Registry, genesis, entries[0]).ShouldBe(Vector("step", "sha256"));
        byte[] route = DaprLogicalClaimCodec.EncodeRoute(Route()); route.ShouldBe(Vector("route", "preimage"));
        DaprLogicalClaimCodec.EncodeRoute(DaprLogicalClaimCodec.DecodeRoute(route)).ShouldBe(route);
        byte[] prefix = DaprLogicalClaimCodec.EncodePrefix(Prefix()); prefix.ShouldBe(Vector("prefix", "preimage"));
        DaprLogicalClaimCodec.EncodePrefix(DaprLogicalClaimCodec.DecodePrefix(prefix)).ShouldBe(prefix);
    }

    /// <summary>Checks exact logical optional presence, offsets, extension ordering and consumed metadata binding.</summary>
    [Fact]
    public void EveryConsumedMetadataOffsetPresenceExtensionAndAggregateTypeChangesTheHash()
    {
        DaprLogicalConsumedMetadata original = Metadata(); byte[] expected = DaprLogicalClaimCodec.ComputeConsumedMetadataHash(original, Logical, SourceHash);
        DaprLogicalConsumedMetadata[] changes = [original with { AggregateType = "other" }, original with { Timestamp = Timestamp.ToUniversalTime() },
            original with { StoredApplicationDigest = "" }, original with { UserId = null }, original with { CausationId = null },
            original with { Extensions = null }, original with { Extensions = new Dictionary<string, string>() },
            original with { Extensions = new Dictionary<string, string> { ["a"] = "2", ["z"] = "2" } },
            original with { DomainServiceVersion = "2.0" }, original with { GlobalPosition = 8 }, original with { ApplicationFormat = "other" }];
        foreach (DaprLogicalConsumedMetadata change in changes) { DaprLogicalClaimCodec.ComputeConsumedMetadataHash(change, Logical, SourceHash).ShouldNotBe(expected); }
        DaprLogicalClaimCodec.ComputeConsumedMetadataHash(original with { Extensions = new Dictionary<string, string> { ["z"] = "2", ["a"] = "1" } }, Logical, SourceHash).ShouldBe(expected);
    }

    /// <summary>Rejects altered framing and foreign or historical evidence models.</summary>
    [Theory]
    [InlineData("separator")]
    [InlineData("count")]
    [InlineData("tag")]
    [InlineData("trailing")]
    [InlineData("model")]
    public void MalformedOrHistoricalRouteClaimsRefuse(string mutation)
    {
        byte[] bytes = DaprLogicalClaimCodec.EncodeRoute(Route());
        int header = "HX-EV-DAPR-ROUTE-1\0"u8.Length;
        if (mutation == "separator") { bytes[6] = (byte)'X'; }
        if (mutation == "count") { BinaryPrimitives.WriteUInt16BigEndian(bytes.AsSpan(header + 1), 19); }
        if (mutation == "tag") { bytes[header + 3] = 2; }
        if (mutation == "trailing") { bytes = [.. bytes, 0]; }
        if (mutation == "model") { bytes[^1] ^= 1; }
        Should.Throw<ArgumentException>(() => DaprLogicalClaimCodec.DecodeRoute(bytes));
    }

    /// <summary>Checks the shared encode/decode scalar ceiling without changing legacy identity compatibility.</summary>
    [Fact]
    public void LargeAdmittedLegacyScalarRoundTripsAndNextByteRefusesConsistently()
    {
        DaprLogicalRouteClaim fields = Route() with { StoredEventType = new string('x', 512 * 1024) };
        byte[] encoded = DaprLogicalClaimCodec.EncodeRoute(fields);
        DaprLogicalClaimCodec.DecodeRoute(encoded).StoredEventType.ShouldBe(fields.StoredEventType);
        Should.Throw<InvalidOperationException>(() => DaprLogicalClaimCodec.EncodeRoute(fields with { StoredEventType = new string('x', 512 * 1024 + 1) }));
    }

    /// <summary>Rejects inconsistent V2 stored identities during both encoding and decoding.</summary>
    [Fact]
    public void V2StoredEventTypeMustMatchTheExactCanonicalTupleOnEncodeAndDecode()
    {
        DaprLogicalRouteClaim fields = Route() with { StoredMetadataVersion = 2, StoredEventType = "stored", StoredCanonicalType = "stored", StoredPayloadVersion = 1 };
        byte[] encoded = DaprLogicalClaimCodec.EncodeRoute(fields);
        DaprLogicalClaimCodec.DecodeRoute(encoded).StoredEventType.ShouldBe("stored");
        Should.Throw<ArgumentException>(() => DaprLogicalClaimCodec.EncodeRoute(fields with { StoredEventType = "other" }));
        encoded[encoded.AsSpan().IndexOf("stored"u8)] = (byte)'x';
        Should.Throw<ArgumentException>(() => DaprLogicalClaimCodec.DecodeRoute(encoded));
    }

    /// <summary>Refuses unbounded extension maps within admitted encoded size and entry count.</summary>
    [Fact]
    public void UnboundedExtensionEnumerationAndHugeFirstKeyRefuseBeforeUnboundedCapture()
    {
        var streamed = new DaprLogicalStreamingExtensions("x");
        Should.Throw<InvalidOperationException>(() => DaprLogicalClaimCodec.ComputeConsumedMetadataHash(Metadata() with { Extensions = streamed }, Logical, SourceHash));
        streamed.Enumerated.ShouldBeLessThan(65537);
        var huge = new DaprLogicalStreamingExtensions(new string('x', 512 * 1024));
        Should.Throw<InvalidOperationException>(() => DaprLogicalClaimCodec.ComputeConsumedMetadataHash(Metadata() with { Extensions = huge }, Logical, SourceHash));
        huge.Enumerated.ShouldBe(1);
    }

    /// <summary>Checks empty, terminal and anchorless complete-prefix invariants.</summary>
    [Fact]
    public void EmptyTargetAndTerminalRangeNeverIncrementLongMaxValueAndAnchorsRefuse()
    {
        DaprLogicalPrefixClaim empty = Prefix() with { StartSequence = 1, EndSequence = 0, Count = 0, TargetSequence = 0 };
        DaprLogicalClaimCodec.DecodePrefix(DaprLogicalClaimCodec.EncodePrefix(empty)).Count.ShouldBe(0);
        DaprLogicalPrefixClaim terminal = Prefix() with { StartSequence = long.MaxValue, EndSequence = long.MaxValue, TargetSequence = long.MaxValue, ActorHead = long.MaxValue };
        DaprLogicalClaimCodec.DecodePrefix(DaprLogicalClaimCodec.EncodePrefix(terminal)).EndSequence.ShouldBe(long.MaxValue);
        Should.Throw<ArgumentException>(() => DaprLogicalClaimCodec.EncodePrefix(empty with { TargetSequence = 1 }));
        byte[] anchored = DaprLogicalClaimCodec.EncodePrefix(Prefix());
        var reader = new EventEvolutionBinaryReader(anchored); reader.ReadRaw("HX-EV-DAPR-PREFIX-1\0"u8.Length + 3);
        for (int index = 0; index < 4; index++) { reader.ReadByte(); reader.ReadString(64); }
        for (int index = 0; index < 4; index++) { reader.ReadByte(); reader.ReadInt64(); }
        reader.ReadByte(); reader.ReadInt32();
        for (int index = 0; index < 3; index++) { reader.ReadByte(); reader.ReadHash(); }
        reader.ReadByte(); anchored[reader.Position] = 1;
        Should.Throw<ArgumentException>(() => DaprLogicalClaimCodec.DecodePrefix(anchored));
    }

    /// <summary>Checks purpose-specific signing, scoped verified-owner charges and private image cleanup.</summary>
    [Fact]
    public void CurrentScopedKeySignsAndVerifiesBothClaimsThenClearsItsRetainedImage()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using DaprLogicalClaimTrust trust = Trust(key); var budget = new EventBufferBudget();
        byte[] source = DaprLogicalClaimCodec.EncodeRoute(Route());
        var signed = trust.Sign(1, source, key, budget, CancellationToken.None);
        MemoryMarshal.TryGetArray(signed.Claim, out ArraySegment<byte> privateBytes).ShouldBeTrue();
        Array.Fill(source, (byte)0xa5);
        using var verifiedRoute = trust.VerifyRoute(signed.Claim.Span, signed.KeyId, signed.Signature.Span, budget, CancellationToken.None);
        verifiedRoute.Value.SequenceNumber.ShouldBe(1);
        using var prefix = trust.Sign(3, DaprLogicalClaimCodec.EncodePrefix(Prefix()), key, budget, CancellationToken.None);
        using var verifiedPrefix = trust.VerifyPrefix(prefix.Claim.Span, prefix.KeyId, prefix.Signature.Span, budget, CancellationToken.None);
        verifiedPrefix.Value.Count.ShouldBe(1);
        Should.Throw<InvalidOperationException>(() => trust.VerifyPrefix(signed.Claim.Span, signed.KeyId, signed.Signature.Span, budget, CancellationToken.None));
        budget.LiveBytes.ShouldBeGreaterThan(source.Length); signed.Dispose(); privateBytes.Array!.ShouldAllBe(static b => b == 0);
        prefix.Dispose(); budget.LiveBytes.ShouldBeGreaterThan(source.Length);
        verifiedRoute.Dispose(); verifiedPrefix.Dispose(); budget.LiveBytes.ShouldBe(0); source.ShouldAllBe(static b => b == 0xa5);
    }

    /// <summary>Refuses final decode cancellation or capability loss and releases every private reservation.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FinalDecodeBoundaryCancellationOrObservedLossRefusesAndReleasesCapacity(bool loss)
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256); using DaprLogicalClaimTrust signing = Trust(key);
        var signingBudget = new EventBufferBudget(); using var signed = signing.Sign(1, DaprLogicalClaimCodec.EncodeRoute(Route()), key, signingBudget, CancellationToken.None);
        var time = new DaprLogicalClaimTimeProvider(); var capability = new EventEvolutionCapabilityLoss(); using var cancellation = new CancellationTokenSource();
        time.OnRead = count => { if (count == 4) { if (loss) { capability.ObserveViolation(); } else { cancellation.Cancel(); } } };
        using DaprLogicalClaimTrust verification = Trust(key, time, capability); var budget = new EventBufferBudget();
        if (loss) { Should.Throw<InvalidOperationException>(() => verification.VerifyRoute(signed.Claim.Span, signed.KeyId, signed.Signature.Span, budget, cancellation.Token)); }
        else { Should.Throw<OperationCanceledException>(() => verification.VerifyRoute(signed.Claim.Span, signed.KeyId, signed.Signature.Span, budget, cancellation.Token)).CancellationToken.ShouldBe(cancellation.Token); }
        budget.LiveBytes.ShouldBe(0);
    }

    /// <summary>Refuses trust disposal during the final time callback and releases decoding capacity.</summary>
    [Fact]
    public void FinalDecodeBoundaryTrustDisposalRefusesAndReleasesCapacity()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256); using DaprLogicalClaimTrust signing = Trust(key);
        var signingBudget = new EventBufferBudget(); using var signed = signing.Sign(1, DaprLogicalClaimCodec.EncodeRoute(Route()), key, signingBudget, CancellationToken.None);
        var time = new DaprLogicalClaimTimeProvider(); using DaprLogicalClaimTrust verification = Trust(key, time); var budget = new EventBufferBudget();
        time.OnRead = count => { if (count == 4) { verification.Dispose(); } };
        Should.Throw<ObjectDisposedException>(() => verification.VerifyRoute(signed.Claim.Span, signed.KeyId, signed.Signature.Span, budget, CancellationToken.None));
        budget.LiveBytes.ShouldBe(0);
    }

    /// <summary>Rejects foreign scope, stale current keys and altered signed bytes.</summary>
    [Fact]
    public void KeyRotationWrongModelDomainRegistryExpiryAndChangedBytesRefuse()
    {
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256); using DaprLogicalClaimTrust trust = Trust(key);
        var budget = new EventBufferBudget(); using var signed = trust.Sign(1, DaprLogicalClaimCodec.EncodeRoute(Route()), key, budget, CancellationToken.None);
        using var other = new DaprLogicalClaimTrust("d", DaprLogicalSourceBinding.ModelId, "new-key", key.ExportSubjectPublicKeyInfo(), Registry,
            DateTimeOffset.UnixEpoch.AddDays(-1), DateTimeOffset.UnixEpoch.AddDays(1), new EventEvolutionCapabilityLoss(), new DaprLogicalClaimTimeProvider());
        Should.Throw<InvalidOperationException>(() => other.VerifyRoute(signed.Claim.Span, signed.KeyId, signed.Signature.Span, budget, CancellationToken.None));
        Should.Throw<InvalidOperationException>(() => trust.Sign(1, DaprLogicalClaimCodec.EncodeRoute(Route() with { Domain = "foreign" }), key, budget, CancellationToken.None));
        Should.Throw<InvalidOperationException>(() => trust.Sign(1, DaprLogicalClaimCodec.EncodeRoute(Route() with { RegistryFingerprint = new byte[32] }), key, budget, CancellationToken.None));
        byte[] changed = signed.Claim.ToArray(); changed[^1] ^= 1;
        Should.Throw<InvalidOperationException>(() => trust.VerifyRoute(changed, signed.KeyId, signed.Signature.Span, budget, CancellationToken.None));
        var time = new DaprLogicalClaimTimeProvider { Now = DateTimeOffset.UnixEpoch.AddDays(1) };
        using DaprLogicalClaimTrust expired = Trust(key, time);
        Should.Throw<InvalidOperationException>(() => expired.VerifyRoute(signed.Claim.Span, signed.KeyId, signed.Signature.Span, budget, CancellationToken.None));
        Should.Throw<ArgumentException>(() => new DaprLogicalClaimTrust("d", "historical-provider", "key", key.ExportSubjectPublicKeyInfo(), Registry,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), new EventEvolutionCapabilityLoss()));
    }

    private static DaprLogicalClaimTrust Trust(ECDsa key, DaprLogicalClaimTimeProvider? time = null, EventEvolutionCapabilityLoss? loss = null)
        => new("d", DaprLogicalSourceBinding.ModelId, "local-key", key.ExportSubjectPublicKeyInfo(), Registry,
            DateTimeOffset.UnixEpoch.AddDays(-1), DateTimeOffset.UnixEpoch.AddDays(1), loss ?? new EventEvolutionCapabilityLoss(), time ?? new DaprLogicalClaimTimeProvider());
    private static DaprLogicalSourceBinding Source() => new("local-app", "local-ns", "AggregateActor", new AggregateIdentity("tenant", "d", "aggregate"),
        "r", 1, 1, 1, SHA256.HashData("local source"u8), "etag", Timestamp);
    private static DaprLogicalConsumedMetadata Metadata() => new("message", "aggregate", "r", "tenant", "d", 1, 7, Timestamp,
        "correlation", "cause", "", "1.0", "Legacy.Event", 1, "json", null, null, null,
        new Dictionary<string, string> { ["a"] = "1", ["z"] = "2" }, "json");
    private static DaprLogicalRouteClaim Route() => new(Logical, "tenant", "d", "aggregate", "r", 1, "message", "Legacy.Event", 1,
        null, null, "json", "evt", 1, Registry, SHA256.HashData("{}"u8), "json", Vector("consumed", "sha256"), SourceHash);
    private static DaprLogicalPrefixClaim Prefix() => new("tenant", "d", "aggregate", "r", 1, 1, 1, 1, 1,
        Vector("ordered_list", "sha256"), Vector("step", "sha256"), Registry, SourceHash);
    private static byte[] Vector(string name, string field) => Convert.FromHexString((name, field) switch
    {
        ("application", "sha256") => "fe0807f702e97a95fc2910984534f842724178c3a2dffb7ac6772f9ca2a0de09",
        ("source", "sha256") => "975cbcc7ea47f955612e2933a3b33d8d074c07909398ec6f68bc14fb153075ca",
        ("source", "preimage") => "48582d45562d444150522d534f555243452d310001001301000000096c6f63616c2d61707002000000086c6f63616c2d" +
            "6e73030000000e4167677265676174654163746f72040000001274656e616e743a643a61676772656761746505000000" +
            "0674656e616e7406000000016407000000096167677265676174650800000001720900000000000000010a0000000000" +
            "0000010b00000000000000010c0000001a74656e616e743a643a6167677265676174653a6576656e74733a0d0000001b" +
            "74656e616e743a643a6167677265676174653a6d657461646174610e0000001d6167677265676174652d6964656e7469" +
            "74792d646563696d616c2d76310fb79fa7c2d1a0febb11a08c276bef5e55d5200cf4403bb5ea38c469735afe05fa1000" +
            "00001d6576656e7473746f72652e6c6f676963616c2d7061796c6f61642e76311101000000046574616712089f7ff5f7" +
            "b5800000781300000001",
        ("consumed", "sha256") => "8998f880bc11a4a273689d75ba9a97f165a9f4ba53ac84f182cad00c3ce9ca6d",
        ("ordered_list", "sha256") => "ea88b62a9aec413b5843f05fe762b778e9b92dfd637b8c231018869cb1774dd2",
        ("genesis", "sha256") => "882c852344f3295674dea90c0506d4281478ee50bf286eb983066b18039b10c9",
        ("step", "sha256") => "fa164de51f1abc2108c569eb13b3516fc4290e1160a03e101118fedf86124d7c",
        ("route", "preimage") => "48582d45562d444150522d524f5554452d310001001401fe0807f702e97a95fc2910984534f842724178c3a2dffb7ac6" +
            "772f9ca2a0de09020000000674656e616e74030000000164040000000961676772656761746505000000017206000000" +
            "000000000107000000076d657373616765080000000c4c65676163792e4576656e7409000000010a000b000c00000004" +
            "6a736f6e0d000000036576740e000000010f377fafb4271fbc20103e108b9d6db17944c4f55d0c4fed74c6a359759b6c" +
            "5a181044136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a11000000046a736f6e128998f8" +
            "80bc11a4a273689d75ba9a97f165a9f4ba53ac84f182cad00c3ce9ca6d13975cbcc7ea47f955612e2933a3b33d8d074c" +
            "07909398ec6f68bc14fb153075ca1400000015646170722d6163746f722d6c6f676963616c2d7631",
        ("prefix", "preimage") => "48582d45562d444150522d5052454649582d3100010011010000000674656e616e740200000001640300000009616767" +
            "726567617465040000000172050000000000000001060000000000000001070000000000000001080000000000000001" +
            "09000000010aea88b62a9aec413b5843f05fe762b778e9b92dfd637b8c231018869cb1774dd20bfa164de51f1abc2108" +
            "c569eb13b3516fc4290e1160a03e101118fedf86124d7c0c377fafb4271fbc20103e108b9d6db17944c4f55d0c4fed74" +
            "c6a359759b6c5a180d000e000f0010975cbcc7ea47f955612e2933a3b33d8d074c07909398ec6f68bc14fb153075ca11" +
            "00000015646170722d6163746f722d6c6f676963616c2d7631",
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    });
}
