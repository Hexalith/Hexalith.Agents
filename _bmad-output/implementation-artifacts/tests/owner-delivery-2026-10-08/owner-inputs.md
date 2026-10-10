# Runtime inputs for the four owner deliveries

Recorded 2026-10-10 for EXT-PARTIES-1, EXT-CONV-AI-1, EXT-SECRETS-1 and EXT-HOST-1. This is a configuration-reference handoff, not enrollment, provider installation, a passing Live/Full verification result or an Available status. A `Pending` entry means no complete owner-specific runtime value was verified; it must not be filled from fixtures, source defaults or an inferred identity. Record credential **references only**, never credential or secret values.

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

## Read-only runtime discovery — 2026-10-10T07:54:05Z

| Verified reference or public identifier | Evidence source and limit |
| --- | --- |
| Operator inspection configuration: `/home/administrator/.kube/hexalith-production`; context `jpiquot@local`, cluster `local`, user `jpiquot`, namespace `default` | K1: `kubectl config view --kubeconfig /home/administrator/.kube/hexalith-production -o json`, reporting only context, cluster, user and credential *field names*. The kubeconfig contains embedded client certificate/key data; this path is an operator inspection credential reference, **not** an EXT-PARTIES-1, EXT-CONV-AI-1 or EXT-SECRETS-1 application credential. No credential data was copied. |
| Kubernetes API `https://192.168.1.30:6443`; node `node1`, internal IP `192.168.1.30`, kubelet `v1.34.9`, Ready=True | K1 for the API URL; K2: live `kubectl get nodes -o json`, reading only name, address, kubelet version and Ready condition. This identifies the host's cluster, not an Agents gateway. |
| `dapr-system` control plane: `dapr-operator`, `dapr-sentry`, `dapr-sidecar-injector`, `dapr-placement-server`, `dapr-scheduler-server`; all observed at 3/3 ready with `ghcr.io/dapr/*:1.18.1` images | K3: live `kubectl get deployments,statefulsets -n dapr-system -o json`, reading only names, images and ready/desired replicas. No owner-specific Dapr component or app binding follows from the control plane. |
| `openbao/StatefulSet/hexalith-keys` at 3/3 ready, image `quay.io/openbao/openbao:2.6.2@sha256:11fd73a2102cda9c55d5d881a8c3210303146a7ec1e8ac76f526e175c6d24641`; `openbao/ConfigMap/hexalith-keys-config` with data key `extraconfig-from-values.hcl`; `openbao/ServiceAccount/hexalith-keys`; services `hexalith-keys`, `hexalith-keys-active`, `hexalith-keys-internal`, `hexalith-keys-standby`, `deployment-seal-transit` | K4: live `kubectl get statefulsets,services,configmaps,serviceaccounts -n openbao -o json`, reporting only resource names, ConfigMap key *name*, image and readiness. This is a cluster resource reference, not proof of an Agents provider, tenant key inventory, signer, custody target or app credential. |
| OpenBao workload credential references: `openbao/Secret/openbao-server-tls` and `openbao/Secret/openbao-seal`; both are mounted by `openbao/StatefulSet/hexalith-keys` | K4a: live `kubectl get statefulset hexalith-keys -n openbao -o json`, reporting only pod-template Secret reference names; `kubectl get secret openbao-server-tls openbao-seal -n openbao -o custom-columns=NAME:.metadata.name,TYPE:.type` confirmed both objects exist. These are OpenBao's own references, not Agents or owner application credentials. |

K5: live `kubectl get deployments,statefulsets,daemonsets,jobs,cronjobs,pods,services,ingresses -A -o json` and `kubectl get configmaps,secrets,serviceaccounts -A -o json` name-only reports found no workload or reference named for Hexalith Agents, Parties or Conversations. K6: live `kubectl get components.dapr.io -A` metadata lists components only in `hexalith-memories`. These are Kubernetes-name observations, not proof that no service exists outside Kubernetes. Direct SSH inspection with the locally available key was denied for `administrator` and `root`; the host-side composition path remains unknown. No Secret values, ConfigMap contents, application environment values or private key bytes were displayed or recorded.

## EXT-PARTIES-1 — runtime references still to supply

| Input | Verified reference / exact missing reference | Evidence source |
| --- | --- | --- |
| Gateway URL | Pending — deployed Parties gateway Service/Ingress URL or host-side listener configuration | K5; the K1 Kubernetes API URL is not a Parties gateway. |
| Tenant A ID | Pending — immutable tenant A identifier from the active tenant registry | K5; no runtime tenant registry reference was identified. |
| Tenant B ID | Pending — immutable tenant B identifier from the active tenant registry | K5; no runtime tenant registry reference was identified. |
| Provisioner credential reference | Pending — provisioner Secret/identity reference and its Parties scope | K5; K1 is an operator inspection credential only. |
| Identity-writer credential reference | Pending — writer Secret/identity reference and its Parties scope | K5; no application credential reference was identified. |
| Reader credential reference | Pending — reader Secret/identity reference and its Parties scope | K5; no application credential reference was identified. |
| Policy ID | Pending — installed policy ID, record/path and current version confirming the accepted 365-day policy | K5; the accepted policy choice is not an installed policy record. |
| Custody target | Pending — Parties retained-history custody provider/resource path | K4–K6; the OpenBao resource has no verified Parties binding. |
| Restore target | Pending — retained-history restore target and configuration path | K5; no Parties restore target reference was identified. |
| Failure-injection target | Pending — qualified Parties failure-injection target/reference | K5; no Parties target reference was identified. |
| Current actor/role issuer and subject | Pending — current application issuer identifier, subject identifier and authority configuration path | K5; K1's kubeconfig user is not application actor/role authority. |

## EXT-CONV-AI-1 — runtime references still to supply

| Input | Verified reference / exact missing reference | Evidence source |
| --- | --- | --- |
| Dedicated service Party ID | Pending — immutable ID of the enrolled Conversations-owned service Party | K5; the accepted service-Party choice supplies no enrolled ID. |
| Worker account and tenant enrollment | Pending — worker account ID, tenant enrollment record/path and credential reference | K5; no Conversations worker identity reference was identified. |
| Current machine binding | Pending — authenticated worker-to-service-Party binding record/path and current version | K5; no Conversations machine binding was identified. |
| Approval issuer | Pending — application approval issuer identifier and trust configuration path | K5; named human approval does not establish this issuer. |
| Authenticated receiver and receipt lookup | Pending — receiver Service/endpoint, authenticated identity reference and independent receipt-lookup endpoint/path | K5; no Conversations receiver or lookup resource was identified. |

The service Party choice is accepted; its actual ID and enrollment are unestablished. A submission acknowledgement alone does not establish authenticated receiver acceptance.

## EXT-SECRETS-1 — runtime references and decision still to supply

| Input | Verified reference / exact missing reference | Evidence source |
| --- | --- | --- |
| Provider and per-tenant key inventory | Pending — installed EXT-SECRETS-1 provider binding/path and per-tenant key IDs/versions. `openbao/StatefulSet/hexalith-keys` is a verified cluster resource only. | K4–K6; no owner binding or tenant inventory was identified. |
| Issuer, audience, profile version and validity | Pending — installed trusted-envelope profile record/path with issuer ID, audience ID, version and validity interval | K5; accepted L/S/O/H/R values do not supply a profile. |
| Independent signer account and public anchor | Pending — enrolled signer account ID, credential reference and public trust-anchor ID/path | K5; no signer enrollment or anchor was identified. |
| Restricted replay-registrar credential reference | Pending — restricted registrar identity/Secret reference and exact EventStore scope | K5–K6; no registrar binding was identified. |
| Overlapping-export Product disposition | Open Product decision | Owner packet; no runtime configuration can substitute for this decision. |

The proposed `agents-decision-issuer` label is not an enrolled account or public trust anchor. The accepted L/S/O/H/R values do not establish an issuer, audience, profile or key inventory.

## EXT-HOST-1 — runtime references still to supply

| Input | Verified reference / exact missing reference | Evidence source |
| --- | --- | --- |
| Composition manifest for `192.168.1.30`: versions | Partial: node kubelet `v1.34.9`, Dapr control plane images `1.18.1`, OpenBao image `2.6.2`. Pending — exact Agents/Parties/Conversations/EventStore/Tenants and provider version/profile manifest path. | K2–K4; these are cluster component versions only. |
| Composition manifest for `192.168.1.30`: resources | Partial: `node1`, `dapr-system` control plane and `openbao/hexalith-keys`. Pending — exact Agents composition manifest path and its application, storage, workflow, safety and service resource IDs. | K2–K6; no matching application workload was found by name. |
| Composition manifest for `192.168.1.30`: credential references | Partial infrastructure references: operator inspection kubeconfig `/home/administrator/.kube/hexalith-production`; mounted `openbao/Secret/openbao-server-tls` and `openbao/Secret/openbao-seal`. Pending — application ServiceAccount/Secret/identity reference map for every composed owner. | K1, K4a and K5; these are operator/OpenBao references, not application credentials. |
| Composition manifest for `192.168.1.30`: health | Partial: `node1` Ready=True; Dapr control plane and OpenBao `hexalith-keys` observed 3/3 ready. Pending — Agents application health endpoint/binding map and readiness evidence. | K2–K4; infrastructure readiness is not Agents readiness. |
| Composition manifest for `192.168.1.30`: telemetry | Pending — Agents application telemetry collector/exporter endpoint and configuration path | K5–K6; no Agents telemetry binding was identified. |
| EXT-PROTECTION-1 engine target | Pending — installed protection engine resource/endpoint and independent observer/attestor reference | K4–K6; OpenBao presence does not establish this target. |
| Replicated spool backend | Pending — installed Agents denial-spool backend component/store identifier and configuration path | K5–K6; no Agents spool binding was identified. |
| Spool replica count | Pending — configured Agents spool replica count tied to that backend | K5–K6; no spool backend or replica configuration was identified. |
| Spool failure model | Pending — backend-specific replica/outage/restore failure-model record/path | K5–K6; no spool failure-model reference was identified. |

Human exact-Conversation scope and class/time-range scope remain separate **Open Product scope decisions**. Neither is approved by this runtime-input record. The four [owner packets](README.md) retain their requirement maps and qualification gates. The four records now have immutable targets and accepted full compatibility commands in the [dependency register](../../../planning-artifacts/external-dependency-register.md); all remain Committed, none is Available, and Story 5.4 remains draft/backlog.
