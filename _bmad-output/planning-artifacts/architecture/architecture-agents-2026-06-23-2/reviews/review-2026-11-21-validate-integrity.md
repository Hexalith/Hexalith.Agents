# Data integrity and recovery review

21 November 2026. Static review of the working spine against the current PRD, launch matrix, dependency contracts and stories. Line numbers refer to the reviewed revision.

**Changes required.** Two high findings affect concurrent-call accounting. They concern different ledgers and require separate recovery tests.

## DI-1 · High · Confirmation can keep the caller’s slot occupied after the call ends

FR-8 makes `ProposalCreated` terminal for the Confirmation call, and FR-32 bounds concurrent nonterminal calls. AD-13 requires releasing a committed caller lease using the terminal event revision. The sequence enters the human proposal workflow immediately after generating the version. It can wait there for approval before reaching the common caller-slot release, which sits beside workflow purge. The pending proposal still needs its workflow for human decisions and expiry. Its content retention clock starts at the later proposal terminal commit. [PRD, lines 307 and 785](../../../prds/prd-agents-2026-06-23/prd.md) · [Spine, lines 448, 512, 548, 972–986 and 1013–1015](../ARCHITECTURE-SPINE.md)

With a per-caller limit of one, leave a Confirmation proposal pending overnight and submit a second otherwise eligible call. The sequence permits the caller to remain blocked throughout the wait. Purging the workflow when the caller’s slot is freed would instead strand proposal expiry and recovery.

Show the durable `ProposalCreated` call transition, release the original caller lease against its revision, and keep the proposal workflow active until the proposal terminates. Its retention clock begins at that later commit. Recover a lost ledger acknowledgement by the same call revision. Verify that the second call can enter while the first proposal still accepts a decision and expires on schedule. In Automatic mode the posting-record terminal rule governs that caller slot.

## DI-2 · High · A terminal indeterminate attempt can free capacity while the Provider is still executing

AD-13 records a terminal `Indeterminate` attempt when the Provider cannot establish the outcome. AD-24 permits reclamation of an `InvocationActive` lease after a recorded terminal attempt. The launch capacity contract gives that condition its own branch, independent of the alternative requiring expiry and authoritative evidence of no active invocation. The Provider contract requires idempotent invocation and outcome lookup. It supplies no guarantee that Agents recording an indeterminate outcome stops external execution. [Spine, lines 446, 586, 915–923 and 946–947](../ARCHITECTURE-SPINE.md) · [Launch register, lines 465–467](../../../launch-readiness-register.md) · [Provider contract, lines 177–188](../../../external-dependency-register.md) · [FR-10 and NFR-12](../../../prds/prd-agents-2026-06-23/prd.md)

Set the system concurrency limit to one. Provider A continues working while its response and lookup are unavailable. Agents terminalizes A as indeterminate. The allocator can now reclaim A’s slot and dispatch B while A is still running. The original budget reservation can remain fully encumbered throughout, so budget conformance fails to detect the second live invocation.

Define the terminal evidence that permits reclamation of an active capacity slot independently of terminal call status. For a possibly live invocation, keep the capacity claim until authoritative owner evidence establishes cessation or an agreed enforceable execution bound does so. Reconcile AD-24, the launch profile and Story 6.5 with the capacity and Provider owners. Attempt B before and after obtaining that evidence and repeat across allocator restart. The initial caller may regain its separate call slot under FR-10.

## Governance and custody checks

The spine distinguishes a retention-due request from destructive authorization, preserves the exact request across duplicates and hold release, and requires receipts before claiming completion. For an already sealed export, an uncertain first key delivery is withheld and a previously delivered outcome is recovered by its original delivery identity. The guard and protection owners separately order hold contention, dispatch and actual consumption. The covered cases retain their restrictive behavior while OQ-35 and OQ-38 remain deferred. [AD-2 and AD-22](../ARCHITECTURE-SPINE.md) · [PRD §9 and OQ-35 through OQ-38](../../../prds/prd-agents-2026-06-23/prd.md)

AD-6 keeps an append pending until there is suitable Conversations evidence. As an OQ-36 acceptance case, read absence, allow the delayed append to commit, and only then obtain a closure receipt. The closure evidence and observation of absence must cover the same ordered owner history. The old absence cannot establish failure after the append has committed. AD-27 confines transient Provider output to the activity through safety and conditional persistence. A lost posting acknowledgement, denied output or missing deletion receipt supplies no evidence of completion. [AD-6, AD-22 and AD-27](../ARCHITECTURE-SPINE.md)

This review exercised written protocol histories. The configured owner operations and the tests above require their own qualification evidence.
