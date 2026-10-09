from pathlib import Path
root=Path('/home/administrator/projects/hexalith/eventstore')
def write(p,s): p.write_bytes(s.replace('\r\n','\n').replace('\n','\r\n').encode())
p=root/'src/Hexalith.EventStore.Client/Streams/DirectoryAtomicAppendPayloadLifetime.cs'
write(p,'''using System.Security.Cryptography;

namespace Hexalith.EventStore.Client.Streams;

/// <summary>Owns one captured plaintext buffer, retiring it only after every started read-only borrower terminates.</summary>
internal sealed class DirectoryAtomicAppendPayloadLifetime : IDisposable
{
    private byte[]? _payload;
    private int _borrowers;
    private int _retired;
    private int _cleared;

    internal byte[]? Payload => _payload;

    internal byte[] Capture(byte[] original)
    {
        _payload = original.ToArray();
        return _payload;
    }

    internal void BorrowUntil(Task pending)
    {
        Interlocked.Increment(ref _borrowers);
        _ = pending.ContinueWith(static (_, state) =>
        {
            var lifetime = (DirectoryAtomicAppendPayloadLifetime)state!;
            Interlocked.Decrement(ref lifetime._borrowers);
            lifetime.TryClear();
        }, this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public void Dispose()
    {
        Volatile.Write(ref _retired, 1);
        TryClear();
    }

    private void TryClear()
    {
        if (Volatile.Read(ref _retired) != 0 && Volatile.Read(ref _borrowers) == 0
            && Interlocked.Exchange(ref _cleared, 1) == 0 && _payload is not null)
        {
            CryptographicOperations.ZeroMemory(_payload);
        }
    }
}
''')
p=root/'src/Hexalith.EventStore.Client/Streams/DirectoryAtomicAppendClient.cs'; s=p.read_text()
s=s.replace('var owned = Capture(request, deadline); string id', 'using var lifetime = new DirectoryAtomicAppendPayloadLifetime();\n        var owned = Capture(request, deadline, lifetime); string id')
s=s.replace('authority.AuthorizeAsync(owned, method, digest, token)).ConfigureAwait(false)', 'authority.AuthorizeAsync(owned, method, digest, token), lifetime.BorrowUntil).ConfigureAwait(false)')
s=s.replace('owner.TryAppendAsync(owned, digest, token)).ConfigureAwait(false)', 'owner.TryAppendAsync(owned, digest, token), lifetime.BorrowUntil).ConfigureAwait(false)')
s=s.replace('authority.VerifyOutcomeAsync(owned, digest, outcome, token)).ConfigureAwait(false)', 'authority.VerifyOutcomeAsync(owned, digest, outcome, token), lifetime.BorrowUntil).ConfigureAwait(false)')
s=s.replace('private static DirectoryAtomicAppendRequest Capture(DirectoryAtomicAppendRequest request, AuthoritativeStreamReadDeadline deadline)', 'internal static DirectoryAtomicAppendRequest Capture(DirectoryAtomicAppendRequest request, AuthoritativeStreamReadDeadline deadline, DirectoryAtomicAppendPayloadLifetime lifetime)')
s=s.replace('if (request.ExpectedStreamRevision < 0', 'if (command.CausationId is not null && !ValidText(command.CausationId)) { throw new ArgumentException("Malformed private directory append causation."); }\n        if (request.ExpectedStreamRevision < 0')
s=s.replace('byte[] payload = command.Payload.ToArray();', 'byte[] payload = lifetime.Capture(command.Payload);')
write(p,s)
for name in ['IAtomicDirectoryAppendOwner.cs','IDirectoryAtomicAppendAuthority.cs']:
 p=root/'src/Hexalith.EventStore.Client/Streams'/name; s=p.read_text()
 idx=s.index('public interface')
 s=s[:idx]+'''/// <remarks>Each operation borrows the captured request Payload read-only until its returned Task actually terminates,
/// including after caller cancellation or timeout. Do not mutate, retain or share this array after termination.
/// Any independently qualified retained representation must own its own copy and lifetime; the client clears its copy
/// after termination of all started operations.</remarks>
'''+s[idx:]
 write(p,s)
p=root/'tests/Hexalith.EventStore.Client.Tests/Streams/DirectoryAtomicAppendTests.cs'; s=p.read_text()
s=s.replace('\n    /// <summary>Optional causation', '''
    /// <summary>The actual capture path retires a copy allocated before a later malformed-extension failure.</summary>
    [Fact]
    public void CaptureFailureAfterCopyRetiresOwnedBytes()
    {
        var request = Request(); var original = request.Command.Payload.ToArray();
        request = request with { Command = request.Command with { Extensions = new Dictionary<string, string> { ["valid"] = "\\uD800" } } };
        using var deadline = new AuthoritativeStreamReadDeadline(TimeSpan.FromSeconds(30), TimeProvider.System, TestContext.Current.CancellationToken, TimeProvider.System.GetTimestamp());
        var lifetime = new DirectoryAtomicAppendPayloadLifetime();
        try
        {
            Should.Throw<ArgumentException>(() => DirectoryAtomicAppendClient.Capture(request, deadline, lifetime));
            lifetime.Payload.ShouldNotBeNull(); lifetime.Payload.ShouldBe(original); lifetime.Payload.ShouldNotBeSameAs(request.Command.Payload);
        }
        finally { lifetime.Dispose(); }
        lifetime.Payload!.All(b => b == 0).ShouldBeTrue(); request.Command.Payload.ShouldBe(original);
        lifetime.Dispose(); lifetime.Payload.All(b => b == 0).ShouldBeTrue();
    }

    /// <summary>Optional causation''')
write(p,s)
