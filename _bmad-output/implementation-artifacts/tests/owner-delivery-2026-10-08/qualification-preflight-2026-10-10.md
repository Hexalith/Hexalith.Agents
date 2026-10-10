# Four-owner qualification preflight — 2026-10-10

This is a read-only preflight for `EXT-PARTIES-1`, `EXT-CONV-AI-1`,
`EXT-SECRETS-1`, and `EXT-HOST-1`. It does not establish Level 4/5 evidence.
No Live or Full command, deployment, or enrollment was performed, and no
credential or secret values were inspected. Story 5.4 remains draft/backlog;
the four records remain `Committed`, with none `Available`.

## Pinned source and command blockers

| Record | Registered target | Current qualification entry point |
| --- | --- | --- |
| EXT-PARTIES-1 | `56cb4401c4179d431385ad3d931fefa402e4fc5a` | `../parties/eng/verify-ext-parties-1.ps1 -Mode Live` checks twelve environment inputs, then exits 1 unconditionally even when they are supplied (lines 28–40). The separate `LiveReadiness` probe requires prior `Available` status and a passing receipt, so it cannot establish availability from `Committed`. |
| EXT-CONV-AI-1 | `8469c5c6891d12b43d2082136b1ca7ddfac52c3b` | `../conversations/eng/verify-ext-conv-ai-1.ps1 -Mode Live` throws before any call (lines 42–47). Its server DI registers unavailable defaults for authority, approval, receipts, command-source verification, catalogue, and stream reads. |
| EXT-SECRETS-1 | `f448db29493c1b30b0f18380188fe99b8b7606fe` | `../platform/eng/verify-ext-secrets-1.ps1 -Mode Live` throws before verification (lines 11–14). Custody DI defaults to unavailable HMAC-key and signing-profile providers. |
| EXT-HOST-1 | `f448db29493c1b30b0f18380188fe99b8b7606fe` | `../platform/eng/verify-agents-host.sh --mode Full` fails before its build (lines 42–44); `../platform/apphost.cs` refuses `Platform:Agents:Enabled=true` (lines 24–29). |

The register's command preflight also requires each owning checkout to equal its
target and be clean. Parties, Conversations, and Platform HEADs match their
registered targets, but unrelated tracked edits make all three checkouts dirty.
An isolated exact checkout is needed for qualification without disturbing
those edits.

The register rows now retain the literal `Command:` marker required by the
Parties readiness parser. This static compatibility correction did not run the
readiness probe or any Live/Full command.

## Current runtime observation

Using `/home/administrator/.kube/hexalith-production`, a metadata-only query of
deployment, stateful-set, daemon-set, service, and ingress names across all
namespaces found Dapr, OpenBao, Keycloak, Memories, and other infrastructure,
but no named Agents, Parties, or Conversations application workload. Dapr
component metadata was present only in `hexalith-memories`. OpenBao
`openbao/hexalith-keys` is infrastructure; its presence does not prove a
tenant key inventory or application custody binding. These name observations
do not rule out host-side services outside Kubernetes.

The read-only commands were `kubectl --kubeconfig
/home/administrator/.kube/hexalith-production get
deployments,statefulsets,daemonsets,services,ingresses -A -o
custom-columns=NAMESPACE:.metadata.namespace,KIND:.kind,NAME:.metadata.name
--no-headers` and `kubectl --kubeconfig
/home/administrator/.kube/hexalith-production get components.dapr.io -A -o
custom-columns=NAMESPACE:.metadata.namespace,NAME:.metadata.name,TYPE:.spec.type
--no-headers`. Only metadata fields were requested.

The [runtime input checklist](owner-inputs.md) still lacks the deployed
Parties gateway, tenants, scoped credentials, current actor issuer, retention
policy and custody/restore targets; Conversations service Party enrollment,
worker binding, approval issuer and authenticated receiver/receipt lookup;
Secrets provider and tenant keys, trusted-envelope profile, independent
signer and replay registrar; and the Agents host composition manifest,
application credentials, protection engine, replicated spool and failure
model. The overlapping-export Product disposition and two deletion-scope
decisions remain open.

Hexalith.Platform is the owner of the missing host composition. Its tracked
`apphost.cs` and `aspire.config.json` identify the source composition root;
the Platform README marks Agents DomainService/UI wiring as planned, and the
current Agents-enabled AppHost path refuses startup. No separate tracked
Agents deployment manifest or installed application binding was found in the
Platform repository. Platform must supply and qualify that composition; the
cluster infrastructure inventory is not a substitute for it.

## Qualification route

1. Hexalith.Platform must provide the actual deployed composition manifest
   and application configuration references for host `192.168.1.30`; record
   references and public identifiers only.
2. Implement and install the missing owner bindings and complete persisted
   Live/Full verification paths. The current fail-closed entry points cannot
   pass at their registered immutable commits.
3. Commit any owner source changes with validated commitlint messages, repin
   the affected register targets and commands, and prepare clean exact
   checkouts. Run the accepted Live/Full commands only after the required
   runtime inputs and authority are verified.
4. Obtain owner acceptance for any repinned target or command. Keep each
   accepted record `Committed` until its exact command passes the required
   live evidence; then evaluate `Available` separately. Keep Story 5.4 in
   draft/backlog through this prerequisite work.
