using System.Text.Json;
using System.Security.Cryptography;
using Hexalith.EventStore.Contracts.Security;
using NSubstitute;

namespace Hexalith.Platform.Custody.Tests;

/// <summary>Synthetic independently durable exact transition journal; backend restoration cannot advance or forge it.</summary>
internal static class AnchoredFixtureJournal
{
    internal static void Attach(IAnchoredStateTransitionAuthority authority, string scope, Func<(long Revision, string Digest)> current, Action<long, string> advance, Dictionary<string, byte[]>? retained = null)
    {
        var proofs = retained ?? new Dictionary<string, byte[]>(StringComparer.Ordinal);
        authority.RecordTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var transition = call.Arg<AnchoredStateTransition>(); var original = current();
            if (transition.ScopeId != scope || transition.ExpectedRevision != original.Revision || transition.TargetRevision != original.Revision + 1
                || transition.PredecessorDigest != original.Digest || Convert.ToHexString(SHA256.HashData(transition.TargetBytes)) != transition.TargetDigest) { return false; }
            proofs[transition.TargetDigest] = JsonSerializer.SerializeToUtf8Bytes(transition); advance(transition.TargetRevision, transition.TargetDigest); return true;
        });
        authority.VerifyTransitionAsync(Arg.Any<AnchoredStateTransition>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var transition = call.Arg<AnchoredStateTransition>(); return transition.ScopeId == scope && proofs.TryGetValue(transition.TargetDigest, out var proof)
                && proof.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(transition));
        });
    }
}
