[
  {
    "location": "platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanup.cs:68-74",
    "trigger_condition": "Provider blocks synchronously before returning its Task.",
    "guard_snippet": "Task<bool> pending = Task.Run(operation, token);",
    "potential_consequence": "Cleanup can hang indefinitely, bypassing timeout and delaying caller cancellation."
  },
  {
    "location": "parties/src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs:112-116",
    "trigger_condition": "Clock moves before binding start during custody await while admission remains valid.",
    "guard_snippet": "if (read.Stream!.ObservedAt > completedAt || binding is not null && completedAt < binding.ValidFrom) return new(PartyIdentityOutcome.Unavailable, null);",
    "potential_consequence": "Current query releases a future-dated binding as presently eligible."
  },
  {
    "location": "parties/eng/verify-ext-parties-1.ps1:57-63",
    "trigger_condition": "Caller supplies a relative EvidenceDirectory.",
    "guard_snippet": "$EvidenceDirectory = [System.IO.Path]::GetFullPath($EvidenceDirectory)",
    "potential_consequence": "Changing directories redirects log paths, causing verification failure or misplaced evidence."
  },
  {
    "location": "parties/eng/verify-ext-parties-history-readiness.ps1:119-138",
    "trigger_condition": "Reply contains foreign binding evidence beneath matching outer tenant and Party fields.",
    "guard_snippet": "if ($history.evidence.tenantId -cne $identity.tenantId -or $history.evidence.partyId -cne $identity.aggregateId) { throw 'Binding scope differs.' }",
    "potential_consequence": "Readiness accepts a binding belonging to another tenant or Party."
  },
  {
    "location": "parties/eng/verify-ext-parties-history-readiness.ps1:125-138",
    "trigger_condition": "Reply extends custody expiry while preserving the recorded actor and binding version.",
    "guard_snippet": "if ([DateTimeOffset]$history.evidence.custody.expiresAt -ne [DateTimeOffset]$decoded.binding.evidence.custody.expiresAt) { throw 'Recorded expiry differs.' }",
    "potential_consequence": "Readiness passes despite a changed immutable retention deadline."
  },
  {
    "location": "platform/src/Hexalith.Platform.Custody/IdentityHistoryCleanup.cs:68-74",
    "trigger_condition": "Cleanup is bounded and every provider stall remains Pending.",
    "guard_snippet": "operation() executes before WaitAsync; a 6,035 ms synchronous provider returned true despite the 5,000 ms bound.",
    "potential_consequence": "Callers cannot rely on the claimed cleanup bound.",
    "kind": "claim",
    "confidence": "high"
  }
]
