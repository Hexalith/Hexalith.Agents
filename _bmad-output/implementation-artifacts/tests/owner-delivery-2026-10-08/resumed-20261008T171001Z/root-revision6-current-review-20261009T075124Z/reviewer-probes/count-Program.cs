using System.Collections;
using Hexalith.EventStore.Contracts.Security;
using Hexalith.EventStore.Server.Security;
using System.Security.Cryptography;
using Hexalith.EventStore.Contracts.Identity;
using var release = new ManualResetEventSlim();
using var entered = new ManualResetEventSlim();
using var caller = new CancellationTokenSource();
var mutations = new SuspendedCount(entered, release);
var request = new GuardedStateCommitRequest("tenant-a", "operation-a", new("guard", 1, Convert.ToHexString(SHA256.HashData(Array.Empty<byte>())), Array.Empty<byte>()), mutations);
var transaction = new DaprGuardedStateTransaction(null!, TimeProvider.System);
var task = Task.Run(() => transaction.CommitAsync(request, caller.Token));
if (!entered.Wait(TimeSpan.FromSeconds(3))) { throw new Exception("Count boundary was not reached"); }
caller.Cancel();
await Task.Delay(250);
Console.WriteLine($"Commit completed after caller cancellation while Count suspended: {task.IsCompleted}");
release.Set();
try { await task.WaitAsync(TimeSpan.FromSeconds(3)); } catch (OperationCanceledException e) { Console.WriteLine($"Eventually observed caller token after Count resumed: {e.CancellationToken == caller.Token}"); }
using (var releaseGuard = new ManualResetEventSlim())
using (var enteredGuard = new ManualResetEventSlim())
using (var callerGuard = new CancellationTokenSource())
{
 var owner = new GovernanceScopeGuardOwner(null!, TimeProvider.System, new DisabledAuthority());
 var transition = new GovernanceGuardTransition("tenant-a", "install-a", GovernanceGuardOperation.InstallEpoch, 1, "epoch-a", "", null, null, null, null, null, null, "", "");
 var guardTask = Task.Run(() => owner.ExecuteAsync(transition, new SuspendedCount(enteredGuard, releaseGuard), callerGuard.Token));
 if (!enteredGuard.Wait(TimeSpan.FromSeconds(3))) { throw new Exception("Guard Count boundary was not reached"); }
 callerGuard.Cancel(); await Task.Delay(250);
 Console.WriteLine($"Guard Execute completed after caller cancellation while Count suspended: {guardTask.IsCompleted}");
 releaseGuard.Set();
 try { await guardTask.WaitAsync(TimeSpan.FromSeconds(3)); } catch (OperationCanceledException e) { Console.WriteLine($"Guard eventually observed caller token after Count resumed: {e.CancellationToken == callerGuard.Token}"); }
}
using (var releaseHistory = new ManualResetEventSlim())
using (var enteredHistory = new ManualResetEventSlim())
using (var callerHistory = new CancellationTokenSource())
{
 var identityHistory = new AggregateIdentity("tenant-a", "party", "party-a");
 var requestHistory = new Hexalith.EventStore.Contracts.Streams.RetainedIdentityHistoryReadRequest(identityHistory, Hexalith.EventStore.Contracts.Streams.RetainedIdentityHistoryReadRequest.AttributionPurpose);
 var grant = new RetainedIdentityHistoryGrant(identityHistory, requestHistory.Purpose, "authority-a", DateTimeOffset.UtcNow.AddMinutes(10), new SuspendedTypes(enteredHistory, releaseHistory));
 var readerHistory = new RetainedIdentityHistorySourceReader(null!, new Admission(grant), null!, TimeProvider.System);
 var historyTask = Task.Run(() => readerHistory.ReadAsync(new System.Security.Claims.ClaimsPrincipal(), requestHistory, callerHistory.Token));
 if (!enteredHistory.Wait(TimeSpan.FromSeconds(3))) { throw new Exception("History grant Count was not reached"); }
 callerHistory.Cancel(); await Task.Delay(250);
 Console.WriteLine($"History read completed after caller cancellation while grant Count suspended: {historyTask.IsCompleted}");
 releaseHistory.Set();
 try { var history = await historyTask.WaitAsync(TimeSpan.FromSeconds(3)); Console.WriteLine($"History returned after caller cancellation instead of throwing: {history.FailureReason}"); }
 catch (OperationCanceledException e) { Console.WriteLine($"History observed caller token after Count resumed: {e.CancellationToken == callerHistory.Token}"); }
}
sealed class SuspendedCount(ManualResetEventSlim entered, ManualResetEventSlim release) : IReadOnlyList<GuardedStateMutation>
{
 public int Count { get { entered.Set(); release.Wait(); return 0; } }
 public GuardedStateMutation this[int n] => throw new IndexOutOfRangeException();
 public IEnumerator<GuardedStateMutation> GetEnumerator() => Enumerable.Empty<GuardedStateMutation>().GetEnumerator();
 IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

sealed class DisabledAuthority : IGovernanceGuardAuthority
{
 public Task<GovernanceGuardEvidence?> ReadAsync(GovernanceGuardTransition transition, string intentDigest, string targetMutationDigest, TenantGovernanceGuardState state, CancellationToken cancellationToken = default) => Task.FromResult<GovernanceGuardEvidence?>(null);
}

sealed class SuspendedTypes(ManualResetEventSlim entered, ManualResetEventSlim release) : IReadOnlyList<Type>
{
 public int Count { get { entered.Set(); release.Wait(); return 1; } }
 public Type this[int index] => throw new IndexOutOfRangeException();
 public IEnumerator<Type> GetEnumerator() => Enumerable.Empty<Type>().GetEnumerator();
 System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
sealed class Admission(RetainedIdentityHistoryGrant grant) : IRetainedIdentityHistoryAdmission
{
 public Task<RetainedIdentityHistoryGrant?> AdmitAsync(System.Security.Claims.ClaimsPrincipal principal, Hexalith.EventStore.Contracts.Streams.RetainedIdentityHistoryReadRequest request, CancellationToken cancellationToken = default) => Task.FromResult<RetainedIdentityHistoryGrant?>(grant);
}
