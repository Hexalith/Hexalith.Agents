# Dedicated Conversations deletion worker — enrollment handoff

The owner has accepted a dedicated Conversations service Party. This document prepares the remaining configuration; it does not reopen that choice or claim the worker is enrolled.

Use one authenticated machine account for the deletion worker and one explicitly enrolled, active Organization service Party in each tenant it serves. Keep the account out of human approval roles. The existing command/event metadata can then carry the Party as its stable audit identity, while current machine authentication still authorizes each operation. The service Party is separate from the approving human and the immutable provisioned Agents Party.

The implemented `ConversationDeletionWorkerRegistration` takes exactly `TenantId`, `AuthenticatedPrincipalId` and `ServicePartyId`. Supply those public identifiers from actual enrollment, together with the source/configuration that proves their current association. Do not use display names, sample realm clients or a general EventStore credential as that proof. In particular, the local sample EventStore service account discovered on 2026-10-08 is not a dedicated production deletion worker.

| Configuration | Concrete remaining input |
| --- | --- |
| Machine account | Existing realm/client or workload identity identifier used by this worker; otherwise name the account to enroll. |
| Per-tenant enrollment | Exact tenant and dedicated service Party identifiers, with authoritative active Organization classification and current machine-to-Party binding source. |
| Allowed operation | Only Conversations-owned deletion publication/delivery metadata operations and their exact source/target scope. No human Agents role, approval signing, general membership administration or cross-tenant access. |
| Receiver trust | Exact Agents target/version and independently authenticated acceptance/lookup authority. A lost acknowledgement must be looked up at its original target before rollover. |
| Deployment evidence | Actual configuration location/version, credential ACL reference and live focused enrollment/denial proof. No passwords, tokens or private keys belong in this packet. |

The pragmatic recommendation is to reuse the environment's existing authentication mechanism, add one narrow machine enrollment and configure the existing worker adapter. A new identity system or service-only command/event format is unnecessary. If the machine account or service Parties do not yet exist, their enrollment remains required delivery work; a fixture binding does not replace it.

Source tests establish that a wrong tenant, principal, Party or target fails admission and that worker retries use exact persisted outcomes. They do not establish this real enrollment. Full EXT-CONV-AI-1 delivery and Story 5.4 entry remain gated.


## Existing enrollment inspection — 2026-10-08

The owner directed checking for an existing enrollment and preparing the best fallback if none is established. [Scoped source/configuration inspection](owner-authority-enrollment-inspection.json), the local realm/client observation and the production workload metadata did not establish a dedicated worker or its tenant service-Party binding. This is limited discovery; private runtime configuration may exist elsewhere.

Use the proposed machine account name `conversations-deletion-worker` if a new account is necessary. This is a proposed name, not a discovered client, accepted target or created account. Enroll it through the existing environment identity mechanism, assign its own tenant Organization service Parties through the existing authorized Parties path, and record their actual stable identifiers and current binding source. Tenant scope and Party identifiers remain unassigned; no guessed identifier can satisfy enrollment. The existing generic EventStore service account should not become a substitute.
