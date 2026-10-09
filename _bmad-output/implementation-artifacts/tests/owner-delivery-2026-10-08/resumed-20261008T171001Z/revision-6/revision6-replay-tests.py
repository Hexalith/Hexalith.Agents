from pathlib import Path
p=Path('/home/administrator/projects/hexalith/platform/tests/Hexalith.Platform.Custody.Tests/TrustedEnvelopeReplayVerifierTests.cs');s=p.read_text();i=s.rfind('\n}')
s=s[:i]+'''
    /// <summary>Both actual synchronous profile boundaries share the operation budget; late profile results cannot release an envelope.</summary>
    [Theory]
    [InlineData(false, false)][InlineData(false, true)][InlineData(true, false)][InlineData(true, true)]
    public async Task SuspendedReplayProfileUsesWholeBudget(bool final, bool cancellation)
    {
        var (clock, advance) = PrivateOwnerDeadlineTestClock.Create(); var fixtureClock = new CustodyFixtureClock { Now = clock.GetUtcNow() };
        var stable = new CustodyFixtureProfileProvider(fixtureClock); var keys = new CustodyFixtureKeyProvider(fixtureClock);
        var authenticator = new TrustedEnvelopeAuthenticator(keys, stable, clock); var expected = Identity();
        var envelope = (await authenticator.IssueAsync(expected, TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken)).Envelope!;
        var profiles = Substitute.For<IPlatformSigningProfileProvider>(); int calls = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); using var release = new ManualResetEventSlim();
        profiles.GetCurrent().Returns(_ => { if (Interlocked.Increment(ref calls) == (final ? 2 : 1)) { entered.TrySetResult(); release.Wait(); } return stable.Profile; });
        var registrar = ReplayRegistrar(); using var caller = new CancellationTokenSource();
        var verifier = new TrustedEnvelopeReplayVerifier(authenticator, registrar, profiles, clock);
        // The test invokes on a worker to observe the pre-fix synchronous provider stall; production never abandons its continuation.
        var reading = Task.Run(() => verifier.VerifyAsync(envelope, expected, caller.Token), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            if (cancellation) { caller.Cancel(); var error = await Should.ThrowAsync<OperationCanceledException>(() => reading.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)); error.CancellationToken.ShouldBe(caller.Token); }
            else { advance(TimeSpan.FromSeconds(30)); var result = await reading.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken); result.Status.ShouldBe(CustodyStatus.Unavailable); result.Envelope.ShouldBeNull(); }
            int registrations = registrar.ReceivedCalls().Count(); release.Set();
            SpinWait.SpinUntil(() => reading.IsCompleted, TimeSpan.FromSeconds(2)).ShouldBeTrue(); registrar.ReceivedCalls().Count().ShouldBe(registrations);
        }
        finally { release.Set(); }
    }

    /// <summary>A final profile provider cannot cancel the original caller or advance the whole budget and then return authenticated success.</summary>
    [Theory]
    [InlineData(false)][InlineData(true)]
    public async Task FinalProfileMustReconfirmWholeBudget(bool cancellation)
    {
        var (clock, advance) = PrivateOwnerDeadlineTestClock.Create(); var fixtureClock = new CustodyFixtureClock { Now = clock.GetUtcNow() };
        var stable = new CustodyFixtureProfileProvider(fixtureClock); var authenticator = new TrustedEnvelopeAuthenticator(new CustodyFixtureKeyProvider(fixtureClock), stable, clock);
        var expected = Identity(); var envelope = (await authenticator.IssueAsync(expected, TimeSpan.FromMinutes(2), TestContext.Current.CancellationToken)).Envelope!;
        using var caller = new CancellationTokenSource(); var profiles = Substitute.For<IPlatformSigningProfileProvider>(); int calls = 0;
        profiles.GetCurrent().Returns(_ => { if (++calls == 2) { if (cancellation) { caller.Cancel(); } else { advance(TimeSpan.FromSeconds(30)); } } return stable.Profile; });
        var verifier = new TrustedEnvelopeReplayVerifier(authenticator, ReplayRegistrar(), profiles, clock);
        if (cancellation) { var error = await Should.ThrowAsync<OperationCanceledException>(() => verifier.VerifyAsync(envelope, expected, caller.Token)); error.CancellationToken.ShouldBe(caller.Token); }
        else { var result = await verifier.VerifyAsync(envelope, expected, caller.Token); result.Status.ShouldBe(CustodyStatus.Unavailable); result.Envelope.ShouldBeNull(); }
    }

    private static ITrustedEnvelopeReplayRegistrar ReplayRegistrar()
    {
        var registrar = Substitute.For<ITrustedEnvelopeReplayRegistrar>(); TrustedEnvelopeReplayReceipt? receipt = null;
        registrar.RegisterAsync(Arg.Any<TrustedEnvelopeReplayIntent>(), Arg.Any<DateTimeOffset>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(call => receipt ??= new(call.Arg<TrustedEnvelopeReplayIntent>(), call.Arg<DateTimeOffset>(), call.Arg<DateTimeOffset>() + call.Arg<TimeSpan>(), 1, "exact-original"));
        registrar.LookupAsync(Arg.Any<TrustedEnvelopeReplayIntent>(), Arg.Any<CancellationToken>()).Returns(_ => receipt);
        return registrar;
    }
'''+s[i:];p.write_bytes(s.replace('\n','\r\n').encode())
