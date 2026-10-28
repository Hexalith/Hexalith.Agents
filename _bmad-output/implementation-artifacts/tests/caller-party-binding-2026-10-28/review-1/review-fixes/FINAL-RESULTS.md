# Final reviewed caller-authority correction

Root independently verified **6,001/6,001** tests across five warning-free Debug/source builds, with zero failures/errors/skips/not-run cases. The final Server project contributes 1,731 cases; all six focused suites execute **261/261** within the full run. The last two edited suites also passed **115/115** in their targeted run.

Caller validation carries the verified stable actor separately from the authenticated subject, checks exact current binding/version and owner-client interval/custody shape, and retains the earliest tenant/actor/binding/custody deadline through subsequent reads and pre-dispatch admission. Canonical actor IDs and binding-effective-time correspondence are enforced. Tests exercise the real public HTTP identity client locally, expiry during envelope preparation, and preserved accepted outcome after delayed acknowledgement.

[Verification receipt](verification.json) records exact commands, selected source/artifact hashes, original XML/build logs and executed matrix methods. [Dependency observation](post-run-dependencies.json) records all 12 root-declared owner revisions/dirty fingerprints and SDK 10.0.402 after the run. It claims no pre-run/in-run transitive or binary-execution attestation.

The separate Live verifier exits 1 as expected on EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1, with **zero external calls**. Full Story 5.4 and its live owner acceptance/runtime bindings remain incomplete; no dependency or sprint status advances.

All review patch groups are complete. Two separate follow-ups remain in the [deferred ledger](../../../../deferred-work.md): the pre-existing intermediate rollback edge inside the tenant reader and restoring omitted Conversation constraints in generated agent context. The final edge-case layer reported no additional findings. Earlier candidate receipts/logs remain historical and unchanged; this result supersedes them for current source.
