### Negative custody outcomes do not verify the resulting hold state

- **Changed surface:** `../platform/src/Hexalith.Platform.Custody/CustodyKeyLifecycleActor.cs:94` removes a provisional pin after authenticated `NotPerformed`; line 96 removes an existing hold only after `Unpinned`.
- **Impacted consumer or site:** The subsequent destruction admission at `CustodyKeyLifecycleActor.cs:54` relies on the persisted pin collection.
- **Existing test evidence:** `Regression gap`. `CustodyKeyLifecycleTests.cs:37` covers successful pin/unpin/destruction; line 55 covers unresolved pin recovery. The executable provider in `CustodyKeyLifecycleFixture.cs:38` always returns `Pinned`, `Unpinned`, or `Destroyed`. Whole Platform searches for `CustodyKeyLifecycleActor`, its provider/authority interfaces, and `CustodyKeyLifecycleStatus.NotPerformed` found only these executable tests and preloaded negative outcomes in the capacity test at `CustodyKeyLifecycleTests.cs:108`; that test does not execute negative pin/unpin transitions.
- **Missing verification:** Assert persisted hold state and subsequent destruction admission after independently verified `NotPerformed` for both pin and unpin, including lookup recovery.
- **Demonstration:** Change line 96 to remove the hold after any terminal unpin outcome, including `NotPerformed`. The tests read would still pass because their executed unpins always succeed.
- **Consequence:** A failed physical unpin could clear the logical hold and admit destruction while the hold remains active.
- **Disposition:** `patch` — add `PinNotPerformedReleasesOnlyProvisionalReservation` and `UnpinNotPerformedPreservesHoldAndBlocksDestroy` to `CustodyKeyLifecycleTests`, asserting persisted state and behavior after serialized restart.

### Independent custody proof rejection is absent from verification

- **Changed surface:** `../platform/src/Hexalith.Platform.Custody/CustodyKeyLifecycleActor.cs:27` requires independent wrapped-object verification; line 92 requires independent physical-outcome verification.
- **Impacted consumer or site:** `RegisterWrappedAsync` persists eligible key objects at line 28; `RetainAsync` persists terminal physical outcomes and updates holds at line 98.
- **Existing test evidence:** `Regression gap`. `CustodyKeyLifecycleFixture.cs:27` and line 29 always return true from `VerifyRegistrationAsync` and `VerifyOutcomeAsync`. Whole Platform symbol/interface searches and reads of `CustodyKeyLifecycleTests` found no test overriding either verifier to reject an otherwise structurally valid receipt. The missing-restore test at `CustodyKeyLifecycleTests.cs:68` rejects a missing field before independent verification.
- **Missing verification:** Rejected registration proof must leave the ledger/anchor unchanged; rejected physical proof must retain `Unknown` and preserve restrictive hold state.
- **Demonstration:** Remove either independent verifier condition while retaining structural checks. Every test checked would receive the same result because both verifiers always approve.
- **Consequence:** Provider self-reported wrapping, unpinning, or destruction could become authoritative persisted evidence without independent proof.
- **Disposition:** `patch` — add `RejectedWrappedObjectProofDoesNotRegister` and `RejectedPhysicalProofRetainsUnknownAndHold` to `CustodyKeyLifecycleTests`, using well-formed receipts rejected by the authority and checking persisted end-state.
