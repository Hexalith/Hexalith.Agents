# Concrete retention and custody proposal

Prepared in response to the user's request to supply the missing retention/custody behavior and compare pragmatic solutions. **Status: concrete recommendation for review; no production configuration or qualification receipt is created.** The earlier approval covered the local implementation/design before a numeric duration was proposed.

## Proposed retention

Use policy `party-actor-retention-v1`, purpose `party-actor-history-v1`, with **365 fixed days from binding-effective-at**. This proposes an annual operational review horizon; it is an engineering choice, not an inherited Agents-content policy or a statutory period. [EDPB principles](https://www.edpb.europa.eu/topics/key-gdpr-concepts/basic-principles_en) require purpose and storage limitation; they do not establish this particular number.

The configuration is in [the proposal JSON](../specs/spec-story-5-4-dependency-unblock/proposals/parties-identity-365-days.proposed.json), outside all host-loaded appsettings paths. Duration is `365.00:00:00`, exactly 31,536,000 seconds. The existing SDK derives `ExpiresAt = ValidFrom + duration`, with exclusive expiry and overflow denial. No erasure/rebind/retry/restore restarts the clock.

Alternatives: 90 days reduces the retained identity relationship but limits older investigations; 365 days supports a longer bounded review period; multi-year retention increases exposure and needs a concrete purpose beyond this initial proposal. Neither short nor long retention fixes custody/restore on its own.

**Timing limitation:** 365 days from establishment is not 365 days after every action or profile erasure. An action on binding day 364 has roughly one day of remaining binding evidence. Current binding validity is also bounded by that deadline. Authorized renewal requires a new version, not silent extension. If the required audit purpose is a full year after each action/erasure, v1 does not meet it; the owners must accept and implement a different lifecycle contract before activation. The proposed number does not resolve that mismatch by itself.

## Custody alternatives

| Option | How it works | Pros | Cons / condition |
| --- | --- | --- | --- |
| 1. Reuse existing production custody | Implement the SDK seam as a thin adapter to an existing independently operated, qualified lifecycle/key backend; reuse its destruction and restore receipts. | Least new code and operating burden; existing incident and recovery procedures. | No qualifying deployment or production `IIdentityHistoryCustody` implementation was found. General vault/audit approval is insufficient; validate this purpose and all copies. |
| 2. Managed key service plus a narrow Platform adapter | Keep events/snapshots in EventStore; custody uses purpose-specific protection and an independently durable lifecycle authority. Before decrypting, check current lifecycle and immutable expiry. A bounded cleanup worker records exact destruction outcomes. | Uses managed infrastructure and existing SDK contracts; small domain integration; no new policy engine. | Provider recovery/deletion semantics must satisfy the existing strict contract. A tombstone/read denial alone does not prove key/copy destruction. Provider credentials, independent lifecycle target, copy inventory and live restore evidence remain necessary. |
| 3. Existing Vault Transit | Use an already operated Vault cluster for purpose-specific crypto, with the same fresh lifecycle authority and copy/destruction checks. | Good fit when Vault is already the organizational standard; explicit crypto API and ACLs. | Raising `min_decryption_version` is reversible; key backups and cluster snapshots require their own treatment. Starting a new cluster solely for this feature adds substantial operations. |
| 4. Local/self-built custody | Persist keys and lifecycle ourselves, implement replication, expiry, cleanup and restore fencing. | Complete design control; useful synthetic development fixture. | Largest security/recovery burden. The current local Parties backend uses in-memory dictionaries and cannot establish production durability or restore safety. Not recommended for launch. |

Managed-provider caveats are real contract constraints: [Azure soft deletion](https://learn.microsoft.com/en-us/azure/key-vault/general/soft-delete-overview) is recoverable; [Azure backups/restored keys](https://learn.microsoft.com/en-us/azure/key-vault/general/backup) remain independent of the original key. [AWS KMS](https://docs.aws.amazon.com/kms/latest/developerguide/deleting-keys.html) has a mandatory 7–30-day deletion wait. [Vault Transit](https://developer.hashicorp.com/vault/docs/secrets/transit) permits lowering the minimum decryption version again. None of those individual operations proves this contract's irreversible expiry.

## Required behavior and qualification

Use separate profile and actor-history protection. Scope custody to tenant, purpose, binding/retention unit, version and immutable expiry, so destroying one expired unit does not destroy a live successor or another tenant. Do not retain offline plaintext key copies. Every snapshot/cache/read-model/replica/export/backup copy belongs to the inventory or is explicitly forbidden.

At expiry, all reads deny immediately. Cleanup is idempotent and outcome lookup resolves crashes/lost acknowledgements; unknown outcomes remain pending. `DestroyExpiredAsync` returns success only after authenticated irreversible destruction and copy-cleanup evidence. Recoverable soft deletion, a delayed scheduled purge, or changing a read threshold cannot be reported as completed destruction. A provider with incompatible timing either fails strict v1 or requires a separately accepted lifecycle-contract change; there is no hidden grace period.

Restore quarantines source/snapshot/key copies until it can consult fresh independent lifecycle state. A backup of the application cannot roll that authority back. Missing/stale authority denies before decryption. Expired/destroyed units cannot be revived by recovering a key, resetting a clock or restoring an older ledger. Unexpired retained bindings must still reproduce their original actor/version/interval/positions without profile decryption.

| Qualification exercise | Required persisted/result evidence |
| --- | --- |
| Erase profile before binding expiry | Profile unavailable; exact historical actor/version and opening position remain readable under independent history custody. |
| At exclusive expiry | Event, snapshot and cache reads deny; no stale-authority or cached-key bypass. |
| Destroy then restore a pre-destruction backup | Expired relationship remains unreadable; fresh lifecycle state cannot roll back; every key/copy receipt is resolved. |
| Restore a still-live successor after its predecessor expired | Successor proof remains complete and exact without reconstructing the expired predecessor's identity. The current strict fold limitation must be resolved/proven. |
| Crash/outage/lost acknowledgement during destruction | Same unit/outcome is recovered; unknown is pending, never a fabricated success or renewed expiry. |
| Wrong tenant/purpose/version and lifecycle rollback | Denial before key release/decryption; foreign persisted state unchanged. |

Qualify on an isolated owner-approved target using shortened synthetic intervals, then verify the production 365-day configuration and exact deployed target separately. Record source/provider versions, target/copy inventory, failure model, authenticated test identity, commands, persisted outcomes, signed/verified receipts and owner acceptance. Mock flags and a completed checklist cannot establish a pass.

## Recommendation and observed limits

Prefer option 1. If no suitable custody exists, use option 2 in the current platform/cloud with the smallest adapter and already qualified independent lifecycle infrastructure. Use option 3 only if Vault is already operated. Do not build new self-managed custody or a generic policy system for this feature.

Workspace investigation found interfaces and synthetic tests, but no production actor-history custody implementation. Azure account metadata was cached; actual vault enumeration failed because the account's MSAL token was unavailable. No deployment was selected, provisioned or exercised. The proposed policy parses and derives its deadline through the existing SDK contract; [configuration validation](tests/actor-retention-proposal-2026-10-06/configuration-validation.json) confirms 365 days and the expected exclusive deadline. The temporary PowerShell check initially could not load the Parties Web assembly because the ASP.NET runtime was absent from that host; it then used the SDK policy contract directly. This is not a new domain test or custody/restore qualification. Full Story 5.4 and the existing dependency register remain unchanged.
