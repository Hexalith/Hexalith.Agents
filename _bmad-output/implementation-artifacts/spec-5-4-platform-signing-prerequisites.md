---
title: '5.4 Platform configured signing prerequisites'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
route: 'dispatch'
human_approval: 'accepted'
approved_on: '2026-10-06'
baseline_commit: '3d8eaf400582f0698297d393f4330a54709c9a07'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/platform/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/platform/Directory.Build.props'
  - '/home/administrator/projects/hexalith/platform/Directory.Packages.props'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
---

<frozen-after-approval reason="authorized owner prerequisites; production policy remains human-owned">

## Intent

Implement the smallest reusable Platform signing/digest prerequisite library and runnable local verification within the complete S1–S4 owner plan. The user authorized prerequisite implementation and requested pragmatic solutions without over-engineering. This child implements the independently testable S1/S2 cryptographic behavior; it does not shrink the parent scope or establish EXT-SECRETS-1 readiness. Keep real custody, independently governed decision authority, export/migration/destruction protocols and numeric production policy explicitly required and unavailable where absent.

## Constraints

Work in the sibling Platform repository. Preserve existing Identity, Works and concurrently edited admin work. Extend the existing new Hexalith.Platform.Custody skeleton. One documented type per C# file, repository conventions, Debug, individual xUnit assemblies and isolated outputs. Do not implement a vault, new database, new service, generic policy framework, custom RFC 8785 approximation, or raw domain persistence. No live dependency consumption, remote operations, stage/commit/push, deployment or nested submodules. This is a handoff in the already-rendered parent bmad-build workflow; do not render/start a second workflow.

No approved numeric profile or production key backend was supplied. Missing/invalid/stale profile or missing provider denies issuance and verification. Provider authorization must be current and exact tenant/purpose/version; no arbitrary tenant-to-secret naming or cached revoked state. Never log, serialize, return or persist key material. An independently issued decision approval cannot be created by custody or the recorder. Cryptographic validity is not domain authorization or replay registration.

## I/O & Edge-Case Matrix

| Input | Expected |
| --- | --- |
| Configured current exact tenant/purpose key and valid explicit profile | HMAC-SHA-256 tag over deterministic canonical bytes; content-free result with key version |
| Missing/invalid/stale profile or unavailable provider | Closed typed failure, no default production numbers or keys |
| Wrong tenant/purpose/version returned by provider | Refusal; reserved-system observation key cannot be used for tenant content or envelope signing |
| Any altered envelope field/tag, wrong issuer/audience/schema/principal shape | Verification denied, constant-time tag comparison |
| Future issue beyond configured skew, expired envelope, excessive lifetime | Denied using injected TimeProvider; expiry exclusive |
| Routine rotation | Issue current active only; old envelope verifies only during configured overlap and lifecycle validity |
| Emergency revocation during/after awaited resolution | Fresh revocation observed; no cached success or cancelled success |
| Recorded digest key version after rotation | Recompute with exactly the original retained version; match or conflict, never current-key false conflict |
| Redispatch | New nonce/time/current key/tag, unchanged logical ID, tuple and fingerprint/version |
| Key disposal/diagnostics | Key copy zeroed and safe ToString/serialization; no secret evidence |
| Full compatibility without S3/S4/provider/host delivery | Full gate fails before calls; Local fixture pass cannot become Available |

</frozen-after-approval>

## Code Map

- src/Hexalith.Platform.Custody existing key scope, purpose, lifecycle, disposable snapshot, provider/result files: harden exact scope/lifecycle behavior and use a fail-closed default provider. A narrow injectable provider contract is acceptable; fixtures are not a production backend. Do not invent a backend data format or claim Dapr alone establishes current atomic custody/restore guarantees.
- Add only these two new Custody projects to Hexalith.Platform.slnx for normal discovery if needed; preserve every existing entry. Remove an unused Dapr package from our new skeleton rather than inventing a backend to justify it.
- New configured profile and authentication/digest services: required version/issuer/audience/current validity and explicit durations maximum lifetime L, skew S, overlap O, retry/recovery H and replay retention R, with R >= max(L+S+O,H). S/O may be zero, L/H/R positive; reject overflow and inconsistent lifecycle. Profile changes during awaited work must not authorize against the old revision.
- AD-29 compatibility: read Agents src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionIdentity.cs read-only. Existing framing uses invariant decimal UTF-16 string.Length + ':' + component + U+001F, then UTF-8. Preserve shipped bytes, including non-ASCII. For command-payload components, put the required raw one-byte 0x00/0x01 presence marker before each framed component so absent and explicitly empty are distinct; nested/collection flattening belongs to the owning DTO adapter. Generic canonical component framing must preserve absent versus explicitly empty values for optional fields; no arbitrary JSON hashing or business-idempotency codec substitution. This technical library need not depend on Agents.
- AD-30 envelope: purpose first; schema/profile version, issuer, principal kind; stable human actor when human; Party/binding version/role basis where present; closed Workflow kind/instance/activity/on-behalf identity for automation; concrete command contract and operation family; target tenant/resource; correlation/causation; immutable idempotency tuple, payload fingerprint and DigestKeyVersion; audience; issued/expires; LogicalCommandId; DeliveryNonce; signing key version. Validate closed principal shapes: User requires stable human actor plus Party/binding/role basis; Administrator requires stable human actor and role basis; Platform requires stable human actor, ActorTenantId=system and role basis; Workflow has no human/binding/role claims and uses only Interaction, SystemTimer, GovernanceProtection, InteractionDirectoryMigration or ConversationDeletionPropagation. Interaction alone carries OnBehalfOfPartyId; the other kinds do not. Workflow purpose/operation authorization remains external, never inferred by this crypto library. Validate expected scope/operation/identity supplied by trusted composition, not a broad family permission. Root architecture spine AD-29/AD-30 is authoritative if additional detail is needed.
- Snapshot caller-owned component lists, tag arrays and digest bytes before awaits so concurrent mutation cannot change what was authenticated; retain immutable result bytes or defensive copies. Clear owned secret/key and temporary sensitive buffers; make caller cancellation prompt even for a noncooperative provider, and dispose any owned late key result without releasing it. Add a real substitution-after-await regression.
- Envelope overlap applies only to TrustedEnvelope verification. Retained content/security digest versions must remain recomputable throughout their own provider-approved lifecycle even after the envelope overlap has ended; never manufacture an idempotency conflict after rotation. Test this boundary explicitly.
- Test-only key providers may simulate fresh authorized inventories, rotation/revocation, denied exact scope, cancellation and outage. Their evidence must remain labeled local fixture.
- eng/verify-ext-secrets-1.ps1: Local builds the exact library/tests with isolated artifacts, runs every required class and checks XML pass/no skip; full Live first checks complete accepted targets/commands/providers/prerequisites, and remains unavailable if S3/S4/exact persisted qualification are missing. Never print EXT-SECRETS-1 PASS from a subset.
- Separate owner note/evidence packet: immutable initial baseline above, current base head observation, source file hashes, exact executed commands/logs/XML, partial scope and full S1–S4/v23/host gaps. Parent original Story 5.4 and register stay unchanged.

## Tasks & Acceptance

- [x] Complete reusable configured S1/S2 scope, lifecycle, profile, canonical-byte and HMAC issuance/verification services without production defaults.
- [x] Implement tenant content digest and reserved-system observation digest with exact retained-version recomputation; use only owned opaque/provider material.
- [x] Keep fresh provider/profile, cancellation and secret material handling safe; supply closed defaults and explicit DI.
- [x] Run meaningful golden framing, field substitution, principal/scope, timing, rotation/revocation, retained digest, provider outage, cancellation and disposal tests. No reflection-derived test that merely mirrors implementation.
- [x] Add truthful Local and full gated owner verification, including missing-input zero-call checks.
- [x] Produce source/evidence notes distinguishing tested cryptographic behavior from real backend/authority/replay/export/migration/destruction delivery and acceptance.

## Verification

Use /tmp/hexalith-agents54-custody-artifacts. Build and test sequentially with set -e and source-reference flags where needed. No production keys; deterministic fixture bytes are test-only. Keep exact test names/results and logs.

## Implementation Notes

Full parent work remains required. No real production retention policy, numeric signing profile, secretstore, independent decision issuer, export store or protection-owner protocol is currently accepted. Implement safe independent code and report those exact missing dependencies; do not fabricate all-or-none destruction receipts or offline acceptance.

Local tasks implemented directly after the fresh-thread limit prevented another coding handoff. Normal Debug source solution build and all 68 local cryptographic tests passed (0 warnings/errors/failures/skips). Executed default/Live/unknown gate checks performed zero build/live calls. Source/evidence is in sibling Platform docs/implementation/ext-secrets-1-*. Full S1–S4/provider/authority/live acceptance remains incomplete; this child closes only its explicit local prerequisite tasks.

Local tasks implemented directly after the fresh-thread limit prevented another coding handoff. Normal Debug source solution build and all 68 local cryptographic tests passed (0 warnings/errors/failures/skips). Executed default/Live/unknown gate checks performed zero build/live calls. Source/evidence is in sibling Platform docs/implementation/ext-secrets-1-*. Full S1–S4/provider/authority/live acceptance remains incomplete; this child closes only its explicit local prerequisite tasks.
