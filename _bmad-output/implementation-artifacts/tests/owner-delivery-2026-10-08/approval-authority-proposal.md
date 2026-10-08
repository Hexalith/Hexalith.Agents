# Minimal independent approval authority — proposal

Status: proposed configuration, not an accepted policy, deployed issuer or approval receipt. Existing implementation approvals, Branch B, Party365 and the five accepted request timings remain unchanged. This answers the remaining question of who may issue governance approval evidence; it does not ask again for permission to implement prerequisites.

The recommended first step is to identify an existing approval authority and reuse it. If none exists, the smallest new arrangement is one independently controlled signing account/process, one versioned role policy and one published public verification profile. It should issue only the decision manifests already required by the register. A new identity platform, general approval framework or workflow designer is unnecessary.

| Approach | How it works | Pros | Cons |
| --- | --- | --- | --- |
| Existing independent approval issuer | The existing approval process checks authenticated people against its current Product, Governance, Security, Architecture and required maintainer policy. It signs an exact decision manifest. Platform receives only its public verification anchors/profile and checks current role, expiry and revocation evidence. | Reuses operating practices and account ownership. Keeps one source of approval authority. No new approval service if the existing process meets the contract. | Its actual policy location, issuer and anchors have not been identified. Existing login or key infrastructure alone does not prove that this process exists or that its outputs cover the required manifest. |
| Small independent signer | An account/process outside the worker, release recorder and Platform key-custody authority signs the same closed manifest only after actual required approvals are recorded. A short versioned policy maps roles to stable authenticated actor identities and names the independent source of current role/revocation evidence. | A small explicit boundary. Can use existing authentication and a narrowly scoped signer rather than another platform. Platform verification needs only public material. | Requires an actual independent account/key owner and an operating procedure for role changes, revocation and policy activation. A process running under the custodian's own unrestricted signing authority does not establish the required separation. |

The signed artifact must bind the exact decision contract/version/digest, effective predecessor or root-policy authorization, required approvers and quorum, affected evaluations, stable actors and role-authority revisions, validity and revocation. These requirements come from the existing complete EXT-SECRETS-1 contract. The proposal does not select new Product outcomes or treat an account label as approval evidence.

A successful check means: the signature uses the independently published approved anchor; every required role has valid approval evidence for this exact contract; current authority/expiry/revocation checks pass; required distinctness holds; and the recorder/key custodian cannot mint or request that evidence. A rejected or unavailable check leaves the affected operation blocked. The approved prerequisite implementation can continue with explicit synthetic authority in source tests; those tests do not create a real approver.

Only configuration references and public identifiers are needed for the owner handoff:

| Input | What to supply |
| --- | --- |
| Existing policy | Repository/configuration location and version, or an explicit statement that no policy exists. |
| Independent issuer | Approved process/account identifier and its owner; no credential or private key. |
| Verification profile | Public-anchor location/id/version, exact issuer/audience, profile version/validity and current revocation source. |
| Role authority | The existing source that resolves stable actor identity and current Product/Governance/Security/Architecture/maintainer roles. Names/emails are not required in the delivery packet. |

If no policy exists, prepare a draft using those four inputs with every unassigned authority explicit. Owner approval of the proposal and named assignments must remain separate from installing actual current actor/role and issuer-trust bindings. Never fill the empty runtime identifiers with test actors, a service Party or an export-signing key.

Recommendation: reuse an existing independent approval process if its complete contract can be demonstrated; otherwise use the small independent signer described above. Keep the Platform verifier focused on this one signed artifact and its current authority evidence. Actual independent issuer, policy and live qualification remain missing.


## Platform inspection and concrete draft — 2026-10-08

The owner directed a check of Hexalith.Platform before proposing new policy. [Scoped inspection](owner-authority-enrollment-inspection.json) found existing exact GitHub-review authentication and Platform bootstrap/operator provenance verification, but no applicable independent decision-role policy/profile binding in the searched source/configuration. The GitHub helper explicitly leaves application authorization external. Its authenticated review facts can be reused as an input where appropriate; they do not assign Product/Governance/Security/Architecture authority or replace a signed decision manifest.

[Concrete policy draft](approval-authority-policy-draft.json) contains the required role slots, issuer/profile/role-authority references, requester separation and activation conditions. Empty identities and references deliberately prevent acceptance or installation. Role slots reflect existing contract responsibilities; the draft does not require a different person for every role unless the actual decision contract requires that distinctness. No new role grants, signer service, keys or production enrollment were created.

### Named owner approval received

The owner replied **“I Jérôme Piquot approve”** to the assignment question. Jérôme Piquot is now the named human approver for the proposed Product, Governance, Security and Architecture assignments, and the policy proposal is owner-approved. Do not ask for that approval again. The proposed separate signer account is `agents-decision-issuer`, operated for this named approval process; it is not enrolled or granted runtime trust by this document.

[Existing identity inspection](named-approver-identity-inspection.json) finds Platform repository ownership references for `jpiquot` (GitHub user6775094) and existing Conversations owner records naming Jérôme Piquot. Those records do not establish a current application actor/Party binding or cryptographic issuer. The [approved proposal](approval-authority-policy-draft.json) therefore keeps actual stable actor identifiers, current role-authority revisions, independent signer enrollment and public trust/revocation profile unassigned. This is configuration/identity proof and qualification still to complete, not missing human proposal approval or renewed prerequisite implementation approval.
