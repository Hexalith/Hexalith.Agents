from pathlib import Path
p=Path('/home/administrator/projects/hexalith/eventstore/tests/Hexalith.EventStore.Client.Tests/Streams/DirectoryAtomicAppendTests.cs');s=p.read_text();i=s.rfind('\n}')
n='''
    /// <summary>Discarded captured request plaintext is cleared on every completed provider path; the caller's original bytes remain immutable.</summary>
    [Theory]
    [InlineData("accepted")][InlineData("denied")][InlineData("unknown")][InlineData("fault")][InlineData("withdrawn")]
    public async Task CompletedCapturedPayloadIsRetired(string vector)
    {
        var request = Request(); var original = request.Command.Payload.ToArray(); byte[]? captured = null;
        var owner = Substitute.For<IAtomicDirectoryAppendOwner>(); var authority = Authority(); int admissions = 0;
        authority.AuthorizeAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            captured ??= call.Arg<DirectoryAtomicAppendRequest>().Command.Payload;
            captured.ShouldNotBeSameAs(request.Command.Payload); captured.ShouldBe(original);
            return vector != "denied" && (vector != "withdrawn" || ++admissions == 1);
        });
        owner.TryAppendAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            call.Arg<DirectoryAtomicAppendRequest>().Command.Payload.ShouldBe(original);
            if (vector == "fault") { throw new IOException("controlled owner fault"); }
            return vector == "unknown" ? Accepted(request) with { State = DirectoryAtomicAppendState.Unknown, CommittedTargetRevision = 0, AcceptedAtAdmissionFenceOrdinal = 0, AcceptedAtGuardHighWater = 0, AuthenticatedReceiptId = null } : Accepted(request);
        });
        var client = new DirectoryAtomicAppendClient(TimeProvider.System, owner, authority);
        if (vector == "fault") { await Should.ThrowAsync<IOException>(() => client.AppendAsync(request, TestContext.Current.CancellationToken)); }
        else
        {
            var result = await client.AppendAsync(request, TestContext.Current.CancellationToken);
            result.State.ShouldBe(vector == "accepted" ? DirectoryAtomicAppendState.Accepted : vector == "unknown" ? DirectoryAtomicAppendState.Unknown : DirectoryAtomicAppendState.Unavailable);
        }
        captured.ShouldNotBeNull(); captured.All(b => b == 0).ShouldBeTrue(); request.Command.Payload.ShouldBe(original);
    }

    /// <summary>Abandoned authority/owner borrowers retain unchanged input until their task ends, then retire it without another phase or effect.</summary>
    [Theory]
    [InlineData("admission", false)][InlineData("admission", true)]
    [InlineData("owner", false)][InlineData("owner", true)]
    [InlineData("verification", false)][InlineData("verification", true)]
    [InlineData("final-admission", false)][InlineData("final-admission", true)]
    public async Task AbandonedCapturedPayloadRetiresOnlyAfterBorrowerCompletion(string stage, bool expires)
    {
        var clock = new AuthoritativeReadTimeProvider(); var request = Request(); var original = request.Command.Payload.ToArray();
        var owner = Substitute.For<IAtomicDirectoryAppendOwner>(); var authority = Authority(); byte[]? captured = null; int admissions = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var outcome = new TaskCompletionSource<DirectoryAtomicAppendOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        authority.AuthorizeAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            captured ??= call.Arg<DirectoryAtomicAppendRequest>().Command.Payload;
            if (stage == "admission" || stage == "final-admission" && ++admissions == 2) { entered.TrySetResult(); return allowed.Task; }
            return Task.FromResult(true);
        });
        owner.TryAppendAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            captured = call.Arg<DirectoryAtomicAppendRequest>().Command.Payload;
            if (stage == "owner") { entered.TrySetResult(); return outcome.Task; }
            return Task.FromResult(Accepted(request));
        });
        authority.VerifyOutcomeAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<DirectoryAtomicAppendOutcome>(), Arg.Any<CancellationToken>()).Returns(_ =>
        { if (stage == "verification") { entered.TrySetResult(); return allowed.Task; } return Task.FromResult(true); });
        using var caller = new CancellationTokenSource(); var reading = new DirectoryAtomicAppendClient(clock, owner, authority).AppendAsync(request, caller.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        try
        {
            if (expires) { clock.Advance(TimeSpan.FromSeconds(30)); (await reading.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).State.ShouldBe(DirectoryAtomicAppendState.Unavailable); }
            else { caller.Cancel(); var error = await Should.ThrowAsync<OperationCanceledException>(() => reading.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)); error.CancellationToken.ShouldBe(caller.Token); }
            captured.ShouldNotBeNull(); captured.ShouldBe(original); request.Command.Payload.ShouldBe(original);
            int ownerCalls = owner.ReceivedCalls().Count(); int authorityCalls = authority.ReceivedCalls().Count();
            allowed.TrySetResult(true); outcome.TrySetResult(Accepted(request));
            SpinWait.SpinUntil(() => captured.All(b => b == 0), TimeSpan.FromSeconds(2)).ShouldBeTrue();
            owner.ReceivedCalls().Count().ShouldBe(ownerCalls); authority.ReceivedCalls().Count().ShouldBe(authorityCalls); request.Command.Payload.ShouldBe(original);
            if (stage == "owner")
            {
                owner.LookupAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Accepted(request));
                var restored = new DirectoryAtomicAppendClient(TimeProvider.System, owner, Authority());
                (await restored.LookupAsync(request, TestContext.Current.CancellationToken)).ShouldBe(Accepted(request));
                await owner.Received(1).TryAppendAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            }
        }
        finally { allowed.TrySetResult(true); outcome.TrySetResult(Accepted(request)); }
    }

    /// <summary>Optional causation retains strict 2048 UTF-8 bytes, with malformed/oversized denial before provider calls and exact original payload.</summary>
    [Theory]
    [InlineData("oversized")][InlineData("malformed")][InlineData("null")][InlineData("boundary")]
    public async Task OptionalCausationUsesExistingIdentityCarrier(string vector)
    {
        var request = Request(); string? causation = vector switch { "oversized" => new string('x', 2049), "malformed" => "\\uD800", "boundary" => new string('é', 1024), _ => null };
        request = request with { Command = request.Command with { CausationId = causation } }; var original = request.Command.Payload.ToArray();
        var owner = Substitute.For<IAtomicDirectoryAppendOwner>(); var authority = Authority();
        owner.TryAppendAsync(Arg.Any<DirectoryAtomicAppendRequest>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Accepted(request));
        var client = new DirectoryAtomicAppendClient(TimeProvider.System, owner, authority);
        if (vector is "oversized" or "malformed")
        {
            await Should.ThrowAsync<ArgumentException>(() => client.AppendAsync(request, TestContext.Current.CancellationToken));
            owner.ReceivedCalls().ShouldBeEmpty(); authority.ReceivedCalls().ShouldBeEmpty();
        }
        else
        {
            (await client.AppendAsync(request, TestContext.Current.CancellationToken)).ShouldBe(Accepted(request));
            await owner.Received(1).TryAppendAsync(Arg.Is<DirectoryAtomicAppendRequest>(r => r.Command.CausationId == causation), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }
        request.Command.CausationId.ShouldBe(causation); request.Command.Payload.ShouldBe(original);
    }
'''
p.write_bytes((s[:i]+n+s[i:]).replace('\n','\r\n').encode())
