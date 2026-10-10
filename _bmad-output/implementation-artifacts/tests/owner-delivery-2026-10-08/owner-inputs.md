# Runtime inputs for the four owner deliveries

Recorded 2026-10-10 for EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1. This is a configuration-reference handoff, not enrollment, provider installation, a passing Live/Full verification result or an Available status. A `Pending` entry means no value was supplied; it must not be filled from fixtures, source defaults or an inferred identity. Record credential **references only**, never credential or secret values.

## Accepted decisions — do not ask again

| Decision | Accepted value |
| --- | --- |
| Integration date | 2026-10-08 for all four owner records |
| Parties branch | Branch B; provisioned Organization Party identified by immutable tenant/id |
| Parties retained human history | 365 fixed days from binding-effective-at; exclusive expiry with no grace |
| Environment | Existing host `192.168.1.30` |
| Conversations denominator | Currently open, undeleted Conversations created in `[from,to)`, including those with zero Agent Calls |
| Conversations worker provenance | Dedicated Conversations-owned service Party with explicit tenant enrollment and current authenticated machine binding |
| Trusted-envelope timing | L=300 s, S=30 s, O=600 s, H=86400 s, R=604800 s; compromised or revoked keys fail immediately |
| Named approval | Jérôme Piquot approved as Product, Governance, Security and Architecture approver; the proposal approval is accepted |

The [accepted-input record](accepted-owner-inputs-20261008.json) and [approval authority proposal](approval-authority-proposal.md) preserve the earlier decisions. The named human approval does not supply an application actor/role issuer, independent signer enrollment or a signed runtime decision manifest.

## EXT-PARTIES-1 — runtime references still to supply

| Input | Reference / identifier |
| --- | --- |
| Gateway URL | Pending |
| Tenant A ID | Pending |
| Tenant B ID | Pending |
| Provisioner credential reference | Pending |
| Identity-writer credential reference | Pending |
| Reader credential reference | Pending |
| Policy ID | Pending |
| Custody target | Pending |
| Restore target | Pending |
| Failure-injection target | Pending |
| Current actor/role issuer and subject | Pending |

## EXT-CONV-AI-1 — runtime references still to supply

| Input | Reference / identifier |
| --- | --- |
| Dedicated service Party ID | Pending |
| Worker account and tenant enrollment | Pending |
| Current machine binding | Pending |
| Approval issuer | Pending |
| Authenticated receiver and receipt lookup | Pending |

The service Party choice is accepted; its actual ID and enrollment are unestablished. A submission acknowledgement alone does not establish authenticated receiver acceptance.

## EXT-SECRETS-1 — runtime references and decision still to supply

| Input | Reference / decision |
| --- | --- |
| Provider and per-tenant key inventory | Pending |
| Issuer, audience, profile version and validity | Pending |
| Independent signer account and public anchor | Pending |
| Restricted replay-registrar credential reference | Pending |
| Overlapping-export Product disposition | Open Product decision |

The proposed `agents-decision-issuer` label is not an enrolled account or public trust anchor. The accepted L/S/O/H/R values do not establish an issuer, audience, profile or key inventory.

## EXT-HOST-1 — runtime references still to supply

| Input | Reference / identifier |
| --- | --- |
| Composition manifest for `192.168.1.30`: versions | Pending |
| Composition manifest for `192.168.1.30`: resources | Pending |
| Composition manifest for `192.168.1.30`: credential references | Pending |
| Composition manifest for `192.168.1.30`: health | Pending |
| Composition manifest for `192.168.1.30`: telemetry | Pending |
| EXT-PROTECTION-1 engine target | Pending |
| Replicated spool backend | Pending |
| Spool replica count | Pending |
| Spool failure model | Pending |

Human exact-Conversation scope and class/time-range scope remain separate **Open Product scope decisions**. Neither is approved by this runtime-input record. The four [owner packets](README.md) retain their requirement maps and qualification gates. Complete immutable targets and accepted full compatibility commands are still TBD; all four records remain Uncommitted and Story 5.4 remains draft/backlog.
