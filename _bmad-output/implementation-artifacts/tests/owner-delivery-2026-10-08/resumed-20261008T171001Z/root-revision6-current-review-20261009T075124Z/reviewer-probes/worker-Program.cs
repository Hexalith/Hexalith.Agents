using Hexalith.Conversations.Contracts.Agents;
using Hexalith.Conversations.Contracts.Identifiers;
using Hexalith.Conversations.Server.Agents;
using var release = new ManualResetEventSlim();
using var entered = new ManualResetEventSlim();
using var caller = new CancellationTokenSource();
var tenant = new TenantId("tenant-a"); var conversation = new ConversationId("conversation-a");
var worker = new ConfiguredConversationDeletionWorker(new(tenant, "machine-a", new PartyId("party-a")), new SuspendedAuthority(entered, release));
var task = Task.Run(() => worker.AuthorizeDeliveryAsync(tenant, conversation, caller.Token));
if (!entered.Wait(TimeSpan.FromSeconds(3))) { throw new Exception("Authority invocation was not reached"); }
caller.Cancel(); await Task.Delay(250);
Console.WriteLine($"Worker completed after caller cancellation while authority invocation suspended: {task.IsCompleted}");
release.Set();
try { await task.WaitAsync(TimeSpan.FromSeconds(3)); } catch (OperationCanceledException e) { Console.WriteLine($"Eventually observed caller token after authority resumed: {e.CancellationToken == caller.Token}"); }
sealed class SuspendedAuthority(ManualResetEventSlim entered, ManualResetEventSlim release) : IConversationAgentAuthority
{
 public Task<ConversationAgentAuthorization> AuthorizeAsync(string principal, TenantId tenant, ConversationId? conversation, string operation, CancellationToken token = default)
 { entered.Set(); release.Wait(); return Task.FromResult<ConversationAgentAuthorization>(null!); }
}
