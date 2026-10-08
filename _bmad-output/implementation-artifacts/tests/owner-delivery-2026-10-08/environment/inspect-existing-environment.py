"""Read selected Kubernetes metadata; never persist raw specifications or secrets."""

import datetime
import hashlib
import json
from pathlib import Path
import subprocess


directory = Path(__file__).resolve().parent
binary = "/home/administrator/hexalith-upgrade-evidence/tools/v1.34.12/kubectl"
prefix = [binary, "--context=jpiquot@local", "--request-timeout=10s"]
observations = []


def read(arguments):
    command = prefix + arguments
    result = subprocess.run(command, capture_output=True, text=True, timeout=20)
    row = {"argv": command, "exitCode": result.returncode}
    if result.returncode:
        row["outcome"] = "Read unavailable; raw error not retained."
        observations.append(row)
        return None
    value = json.loads(result.stdout)
    observations.append(row)
    return value


def metadata(item):
    value = item.get("metadata", {})
    return {"kind": item.get("kind"), "namespace": value.get("namespace"),
            "name": value.get("name"), "uid": value.get("uid"),
            "resourceVersion": value.get("resourceVersion")}


version = read(["get", "--raw=/version"])
namespaces = read(["get", "namespaces", "-o", "json"])
workloads = read(["get", "deployments,statefulsets,pods,services,ingresses", "-A", "-o", "json"])
configmaps = read(["get", "configmaps", "-A", "-o", "json"])
components = read(["get", "components.dapr.io", "-A", "-o", "json"])
configuration = read(["get", "configurations.dapr.io", "-A", "-o", "json"])
projected_workloads = []
for item in (workloads or {}).get("items", []):
    row = metadata(item)
    kind, spec, status = row["kind"], item.get("spec", {}), item.get("status", {})
    if kind in {"Deployment", "StatefulSet"}:
        row["replicas"] = spec.get("replicas")
        row["readyReplicas"] = status.get("readyReplicas", 0)
        row["images"] = [x.get("image") for x in spec.get("template", {}).get("spec", {}).get("containers", [])]
    if kind == "Pod":
        row["phase"] = status.get("phase")
        row["nodeName"] = spec.get("nodeName")
        row["containers"] = [{"name": x.get("name"), "image": x.get("image"),
                              "environmentVariableNames": [v.get("name") for v in x.get("env", [])]}
                             for x in spec.get("containers", [])]
    if kind == "Service":
        row["serviceType"] = spec.get("type")
        row["ports"] = [{key: p.get(key) for key in ["name", "port", "targetPort", "protocol"]} for p in spec.get("ports", [])]
    if kind == "Ingress":
        row["hosts"] = [x.get("host") for x in spec.get("rules", [])]
        row["className"] = spec.get("ingressClassName")
    projected_workloads.append(row)
projected_components = []
for item in (components or {}).get("items", []):
    row = metadata(item)
    spec = item.get("spec", {})
    row.update({"componentType": spec.get("type"), "version": spec.get("version"),
                "metadataFieldNames": [x.get("name") for x in spec.get("metadata", [])],
                "scopes": item.get("scopes", [])})
    projected_components.append(row)
report = {
    "capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    "ownerSuppliedEnvironment": "192.168.1.30",
    "context": "jpiquot@local",
    "endpoint": "https://192.168.1.30:6443",
    "clientBinary": binary,
    "clientBinarySha256": hashlib.sha256(Path(binary).read_bytes()).hexdigest(),
    "serverVersion": (version or {}).get("gitVersion"),
    "namespaces": [metadata(x) for x in (namespaces or {}).get("items", [])],
    "workloads": projected_workloads,
    "configMaps": [{**metadata(x), "dataKeyNames": sorted(x.get("data", {})),
                    "binaryDataKeyNames": sorted(x.get("binaryData", {}))}
                   for x in (configmaps or {}).get("items", [])],
    "daprComponents": projected_components,
    "daprConfigurations": [metadata(x) for x in (configuration or {}).get("items", [])],
    "commands": observations,
    "limits": ["Read-only target discovery, not full dependency/live security qualification.",
               "No Secret objects, secret values, raw configuration, environment values, credentials, certificates or workload logs retained.",
               "Replicas/readiness do not prove independent failure domains, all-copy key destruction, nonrollback authority or audit-spool qualification.",
               "Sequential metadata observations are not an atomic cluster snapshot."]}
(directory / "metadata-observation.json").write_text(json.dumps(report, indent=2) + "\n")
relevant = [x for x in projected_workloads if x["kind"] in {"Deployment", "StatefulSet", "Ingress"}
            and x["namespace"] in {"openbao", "keycloak", "dapr-system"}]
print(json.dumps({"serverVersion": report["serverVersion"], "readCommands": observations,
                  "selectedInfrastructure": relevant, "daprComponents": projected_components}, indent=2))
