# Verified-current architecture review, 2026-11-20

**Final verdict.** Pass. The recheck closed VC-1. The source and package pins, hosted evidence boundary and current delivery debts agree with the Agents parent at `05ddee8`. `RQ-1` remains **NOT READY**.

## VC-1 · Medium · Closed on recheck

The initial review found that AD-18 justified restart behavior using Dapr 1.18. The revised decision requires Agents to resume or terminalize the existing nonterminal instance under its original interaction ID. Story 6.1 must verify resume and terminal recovery on the exact deployed runtime for the current package selection, with owner approval governed by `OD-DAPR-SECURITY-1`. [AD-18](../ARCHITECTURE-SPINE.md#ad-18---dapr-workflow-owns-v1-execution) · [Story 6.1](../../../../implementation-artifacts/spec-6-1-start-and-recover-an-automatic-interaction.md)

## Verification scope

- Compared the SDK, target, language, development gitlinks and package versions to `global.json`, `Directory.Build.props`, `git ls-tree HEAD references/` and the catalog at the parent-recorded Builds commit. Confirmed the Server's direct Workflow reference and its guarded opt-in outside the default host. [Stack](../ARCHITECTURE-SPINE.md#stack)
- Cross-checked the test counts, all seven package-consumer selections, loopback-only startup, outstanding hosted rehearsal, four dependency commitments and absent live qualification against the sprint correction and owner records. The debt table records the remaining approval-before-post and message provenance defects. [November sprint evidence](../../../sprint-change-proposal-2026-11-20.md#local-implementation-evidence) · [Owner commitments](../../../external-dependency-register.md#critical-dependency-records)
- Verified the index version and both rendered identifiers agree on 15, Markdown fences are paired, relative Markdown body links and heading targets resolve, and `git diff --check` passes for the spine.
