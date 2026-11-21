# Data integrity and governance validation

20 November 2026 · Static review of AD-2, AD-12, AD-22, AD-30, the class model and the governance contracts. References to the spine use its current line numbers. References to the PRD, epics and registers use the planning-artifacts files.

**Verdict: changes required.** One High, one Medium and one Low finding. The first affects ordinary retention when all dependencies are available. The second affects release of an export key while its artifact is being deleted.

## DI-1 · High · Scheduled retention expiry has no authorized deletion origin

**Evidence.** PRD §9, lines 968–970, requires erasure and projection purge at the retention cutoff and explicitly includes retention expiry in hold deferral. AD-22 sets the terminal clock at spine lines 545 and 547. Story 8.1 emits exactly one eligible expiry request to Story 8.3 after a hold releases (epics lines 2905–2912). AD-30 grants the timer retention-expiry and purge commands (spine line 635), while the timer row grants retention-due without a human decision (launch register line 207). AD-2 admits exactly two deletion origins, each with specific approval evidence (spine line 272). AD-22 requires one of them on every deletion command and tombstone (line 563). Story 8.3 starts the common effect cut only for those two origins (epics lines 3031–3035 and 3048–3055).

**Scenario and impact.** A terminal interaction reaches day 365 with no hold, operator request or Conversations deletion. A timer obeying the closed origin model cannot start the purge. A timer treating its AD-30 grant as direct purge authority can skip the hold guard, accepted inventory and all-copy receipts. Releasing a hold after day 365 leaves Story 8.1's eligible expiry request at the same impasse.

**Correction and acceptance.** Specify who may turn a verified deadline into a deletion request, what evidence authorizes it and how the workflow enters the common inventory, deferral, seal and completion protocol. Reconcile the closed origin, command grants, class model and Stories 8.1/8.3. If the initiating authority requires a Product or Governance decision, identify its owner and affected operation. Exercise an ordinary expiry, a duplicate timer after a lost acknowledgement, and a hold that wins before arming then releases. Require one stable request, one receipt-backed outcome and a content-free tombstone with genuine origin evidence.

## DI-2 · Medium · A pending export-key delivery has no ordering rule against deletion of its artifact

**Evidence.** AD-12 requires current authorization for side effects (spine line 417). AD-22 orders export creation and committed-copy discovery against deletion (line 545). Key authorization checks artifact availability, but the later custodian release checks the principal and exclusive expiry (line 565 and launch register lines 303–305). The secrets contract tests actor revocation and expiry around release (external register line 228). The store contract serializes physical purge and key destruction against the protection fence (lines 246 and 249). The export-lifecycle decision names lifetime, later hold, restore and provider choice (spine line 1446). None of these specifies when an authorized, still-undelivered key becomes ineligible during deletion of that committed export.

**Scenario and impact.** A valid Inspector already holds the encrypted artifact. Key delivery is authorized and pauses before the custodian releases it. Overlapping deletion then seals, with physical destruction still pending. One custodian honors the still-valid delivery grant. Another checks the deletion seal and refuses. The same durable history gives the Inspector different access to the content.

**Correction and acceptance.** Have the lifecycle owners state the last permissible first release during overlapping deletion. Bind that choice to a shared fence or custodian operation that can order release against the selected boundary. Include authenticated lookup for a lost response and preserve the receipt for a delivery that already won. Test both winning orders with an authorized, unexpired actor and a delayed physical purge. Identify the winning event and the recorded delivery outcome in each case.

## DI-3 · Low · The class diagram leaves two export fields of authority unclear

**Evidence and impact.** The diagram names an untyped `AuditExport.ArtifactKey` and shows only an export-to-interaction relationship (spine lines 1309–1317 and 1399–1406). A reader cannot tell whether that field is the immutable store address or cryptographic key material, or which aggregate owns the commit. AD-22 gives the commit and index high-water to `ProtectionFence`, makes its secondary acknowledgement a condition for download, and sends raw key material directly from custodian to principal (lines 545 and 565). The store contract defines the address as tenant, export and manifest version (external register lines 246 and 249).

**Correction and acceptance.** Name the field for the content-free artifact locator and show the reference to the exact fence commit acknowledged by `AuditExport`. The diagram should let a reader predict the outcome after a crash between those writes: deletion sees the committed copy and key delivery waits for acknowledgement.

## Specified behavior and existing owner decisions

The shared guard orders hold registration against irreversible deletion and detects a stale seal attempt (spine lines 545 and 553). The committed-export index remains authoritative through a lost secondary acknowledgement, and a hold remains pending until its indexed artifacts are pinned (lines 545 and 557). The register requires physical action authorization and per-copy receipts across expiry, purge and restore (external register lines 246 and 249).

Hold contention after arming, prepare cancellation, export lifetime and restore, legacy plaintext disposition, and human Conversation or class/range scopes already have explicit owner decisions and restrictive interim behavior (spine lines 248–250, 541, 1443–1446 and 1453). These are separate from DI-1 and DI-2. This validation assessed implementability of the written contracts. Runtime qualification remains with the registered owners.
