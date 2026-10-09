from pathlib import Path
r=Path('/home/administrator/projects/hexalith/eventstore')
def w(p,s):p.write_bytes(s.replace('\r\n','\n').replace('\n','\r\n').encode())
p=r/'src/Hexalith.EventStore.Server/Security/RetainedIdentityHistorySourceReader.cs';s=p.read_text();s=s.replace('    private static readonly JsonSerializerOptions', '''    private readonly Action<byte[]>? _ownedPayloadObserved;

    internal RetainedIdentityHistorySourceReader(IActorProxyFactory actors, IRetainedIdentityHistoryAdmission admission,
        IIdentityHistoryCustody custody, TimeProvider clock, Action<byte[]> ownedPayloadObserved)
        : this(actors, admission, custody, clock) => _ownedPayloadObserved = ownedPayloadObserved;

    private static readonly JsonSerializerOptions''');s=s.replace('                        events.Add(new StreamReadEvent', '                        _ownedPayloadObserved?.Invoke(closedPayload);\n                        events.Add(new StreamReadEvent');w(p,s)
p=r/'tests/Hexalith.EventStore.Server.Tests/Security/RetainedIdentityHistorySourceReaderTests.cs';s=p.read_text();i=s.index('    private void ArrangeCompleteSource()')
s=s[:i]+'''    /// <summary>Actual closed serialized arrays remain owned until success; every later refusal clears them without touching stored ciphertext.</summary>
    [Theory]
    [InlineData("authority")][InlineData("custody")][InlineData("source")][InlineData("expiry")][InlineData("cancellation")][InlineData("success")]
    public async Task ClosedSerializedPayloadTransfersOnlyWithSuccessfulStream(string vector)
    {
        Arrange(); var clock = new RetainedHistoryTimeProvider(_now); using var caller = new CancellationTokenSource();
        EventEnvelope[] stored = [Stored(1, "Profile", "sealed-profile"), Stored(2, typeof(HistoryCustodyProbeEvent).FullName!, "sealed-history")];
        byte[][] originals = stored.Select(e => e.Payload.ToArray()).ToArray();
        _actor.ReadEventsRangeAsync(0, 2, 100).Returns(stored);
        int admissions = 0; int reads = 0; byte[]? closed = null;
        _admission.AdmitAsync(_principal, Arg.Any<RetainedIdentityHistoryReadRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (++admissions == 2)
            {
                closed.ShouldNotBeNull(); closed.Any(b => b != 0).ShouldBeTrue();
                if (vector == "authority") { return Grant() with { AuthorityRevision = "withdrawn" }; }
                if (vector == "expiry") { clock.Advance(TimeSpan.FromDays(1)); }
                if (vector == "cancellation") { caller.Cancel(); }
            }
            return Grant();
        });
        _custody.CanReadAsync(Arg.Any<AggregateIdentity>(), Arg.Any<IdentityHistoryCustodyEvidence>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++reads == 1 || vector != "custody");
        _actor.GetStreamMetadataAsync().Returns(new AggregateStreamMetadata(true, 2), new AggregateStreamMetadata(true, vector == "source" ? 3 : 2));
        var reader = new RetainedIdentityHistorySourceReader(_actors, _admission, _custody, clock, bytes => closed = bytes);
        if (vector == "cancellation")
        {
            var error = await Should.ThrowAsync<OperationCanceledException>(() => reader.ReadAsync(_principal, Request(), caller.Token));
            error.CancellationToken.ShouldBe(caller.Token);
        }
        else
        {
            var result = await reader.ReadAsync(_principal, Request(), caller.Token);
            if (vector == "success")
            {
                result.IsAuthoritative.ShouldBeTrue(); result.Stream!.Events.Single().Payload.ShouldBeSameAs(closed);
                closed.ShouldBe(JsonSerializer.SerializeToUtf8Bytes(new HistoryCustodyProbeEvent(Evidence()), new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            }
            else { result.Stream.ShouldBeNull(); }
        }
        closed.ShouldNotBeNull(); if (vector != "success") { closed.All(b => b == 0).ShouldBeTrue(); }
        for (int n = 0; n < stored.Length; n++) { stored[n].Payload.ShouldBe(originals[n]); }
    }

'''+s[i:];w(p,s)
