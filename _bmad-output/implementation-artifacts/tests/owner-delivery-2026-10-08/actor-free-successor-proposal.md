# Actor-free successor proof — contract handoff

Status: proposed technical contract, not an implemented or qualified successor seam. EventStore was inspected directly at the source recorded in [inspection evidence](eventstore-successor-contract-inspection.json). The current certificate cannot authenticate an expired binding transition. Its executed source test correctly returns unavailable before releasing a successor.

The smallest proposed solution is one authenticated lifecycle boundary certificate. Establish it from the actual conditional predecessor-close/successor-open transition while that transition can still be verified. After predecessor destruction, it proves only that the excluded prefix was validly closed and that the retained successor starts at its original source position/version. It must not contain the expired actor, profile, login mapping or an actor-identifying hash. A bare list of excluded positions is insufficient.

The owning contract needs to settle these elements before public implementation/adoption:

| Element | Required meaning |
| --- | --- |
| Exact scope/source | Bind the admitted tenant/Party query, contract version, original stream and authoritative source head. |
| Closed-prefix proof | Authenticate complete original-position coverage and a valid predecessor closure/non-overlap boundary. Do not reconstruct missing transitions or use a caller-supplied assertion. |
| Retained successor | Bind its original source position and immutable binding version/effective instant. Its expiry remains its own effective instant plus the accepted 365 days. |
| Independent lifecycle | Refer to authenticated opaque custody/destruction evidence and a fresh nonrollback authority revision. Restored old certificates/keys cannot supersede current expiry or destruction. |
| Minimal retention | Define which actor-free positions/version links/opaque proof references remain necessary, their lifecycle and authorized access. Cleanup evidence cannot become a second identity-history store. |
| Exact outcome | Certificate establishment/retry/lookup must refer to one original transition. Unknown establishment cannot be treated as a valid boundary. |

Parties owns binding/closure meaning, EventStore owns exact-source certification, and Platform owns current independent lifecycle and all-copy custody receipts. The producer must be connected to the original conditional transition; a certificate invented later from today's binding cannot repair missing proof. Existing reads remain strictly unavailable until the new proof is both supported and independently authenticated.

This keeps one small source boundary rather than a new history database. It allows a successor to be proved without decrypting an expired predecessor. Its cost is one coordinated producer/validator contract, migration behavior for transitions without a certificate, and restore qualification. An existing transition with no valid pre-destruction certificate remains unavailable; the solution cannot backfill erased identity facts.

Required qualification includes exact rebind boundaries, gaps/overlap, changed source head or scope, unsupported proof version, expired successor, lost certificate acknowledgement, missing/stale authority, independent lifecycle rollback denial, restored old keys/ciphertext and absence of expired identifying data from every retained proof/copy. Synthetic positive cases alone cannot close this contract.

Recommendation: settle and deliver this one minimal boundary contract under the existing retention policy. No additional retention duration or silent renewal is proposed. The owner's “check yourself in EventStore” direction has been fulfilled by direct source inspection; supplying an unseen existing schema is no longer requested. The actual producer/trust/receipt contract and its complete delivery remain open.
