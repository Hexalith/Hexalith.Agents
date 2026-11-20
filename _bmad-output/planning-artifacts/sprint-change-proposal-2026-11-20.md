# November course correction for Hexalith.Agents

**Decision.** On 20 November the owner requested that the ten recommendations from the technical audit be applied to the sprint plan and explicitly included implementation and testing. This is a moderate correction to Epics 5–8. The Product Owner owns sequencing and the module developer owns local changes. The Platform, Conversations, EventStore and CiCd owners retain their existing contracts and release responsibilities.

## Trigger and impact

At `e4b4877`, 6,521 package-mode tests passed but the Release executable exited before listening. Endpoint fixtures had supplied the missing read-model store. Ten of thirteen direct gitlinks failed exact fetches. Hosted CI stopped at checkout, and the package-consumer hook rejected seven EventStore import version changes. The source-mode Conversations adapter treated an accepted append as a posted message. The audit also found a missing result-scope check and a grant-expiry gap on asynchronous reads. These failures affect fresh contributors, callers waiting for a reply, and people entitled to different levels of audit evidence. [Audited server entry point](https://github.com/Hexalith/Hexalith.Agents/blob/e4b48774833a13be7ba8748e86da9a07f78e916d/src/Hexalith.Agents.Server/Program.cs#L28) · [CI run at the audited revision](https://github.com/Hexalith/Hexalith.Agents/actions/runs/44307493888)

## Changes to the delivery plan

| Audit finding | Previous evidence or behavior | Applied acceptance change and owner |
| --- | --- | --- |
| 1. Direct dependency pins | The parent recorded revisions that the configured remotes rejected. | Story 5.11 requires an empty-cache clone, exact direct pins and a passing Debug build. The dependency owners must supply the intended commits or a source-compatible published replacement. |
| 2. Release startup | Endpoint tests could succeed while the ordinary executable failed to bind. | Stories 5.11 and 5.6 require an actual process, authentication, an unavailable dependency response and a separate configured read in the Platform host. |
| 3. CI and release commands | The workflow expected an older Builds pin and tooling removed from the module. | Story 5.11 distinguishes module-owned tools from CiCd delivery. The release owner verifies the resulting SHA with a nonpublishing rehearsal. |
| 4. Package consumer | EventStore 5.3.0 resolved against an approved 5.2.0 import inventory. | Story 5.11 requires review of the seven matching paths and hashes, version alignment, and all seven consumer selections passing. |
| 5. Audit disclosure | A generic administration decision preceded a read whose duration could exceed the grant. | Story 8.8 requires resource- and operation-scoped authorization valid at disclosure. Fail closed when the authority cannot establish the requested scope. |
| 6. Posting acknowledgement | A successful command submission could become a terminal reply. | Stories 6.6 and 7.4 require a matching durable message, preserved identity after restart and an honest pending outcome in both response modes. |
| 7. Conversation scope | Freshness was checked, but a wrong tenant or conversation could still supply input. | Story 6.2 requires exact response scope before using members or content and an opaque correlation identifier for diagnostics. |
| 8. End-to-end qualification | Package tests and the platform launch gate had different coverage. | Story 5.6 now names the executable evidence. The launch register maps each user workflow to its required owner and receipt. The owner fixture must run one synthetic interaction through durable posting and restart. |
| 9. HTTP route compatibility | A prefix-level assertion passed with just one versioned route. | Story 5.11 requires the full supported method/path inventory and the existing legacy provider-status refusal. |
| 10. Duplicate catalog replay | A repeated delivery rebuilt and sorted the catalog before returning. | The Story 5.3 follow-up requires a per-stream fast path, gap and mixed-batch checks, and a repeated allocation measurement. |

## Requirements and design

The PRD already specifies role-scoped evidence in FR-33, governed posting and public API coverage. The architecture assigns API parity, platform composition and audit governance to AD-15, AD-16 and AD-22. The UX already distinguishes a pending reply from a posted one. Keep those definitions and use them as acceptance oracles for the implementation. The participant may see pending for longer while Conversations confirms the message. The inspector can receive an unavailable or denied response when the authority cannot establish current access. The October custody amendment and RQ-1 still govern live use. [PRD](prds/prd-agents-2026-06-23/prd.md) · [Architecture](architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) · [UX](ux-designs/ux-agents-2026-06-23/EXPERIENCE.md)

## Sequence, effort and handoff

1. **Module developer and dependency owners, about 1–3 engineer-days plus owner lead time.** Repair build inputs, the host and package evidence. The package-mode checks can run while the source owners investigate pins. Reverting all pins is viable only when the replacement builds against the current module.
2. **Module developer and test owner, about 3–6 engineer-days.** Run scope, expiry, route and replay regression cases. Catalog allocation work is bounded to roughly another half-day to one day. Reuse the existing projections, SDK authentication and resource contracts.
3. **Platform and Conversations owners with the module developer, about 1–3 engineer-weeks after owner inputs are available.** Qualify automatic and approved replies in the supported topology, including refusal, lost acknowledgement and restart. Attach the immutable owner revisions and results before progressing RQ-1.

The backlog adjustment preserves prior story history. Story 5.11 carries the outstanding delivery work. The amended acceptance criteria are in Stories 5.3, 5.6, 6.2, 6.6, 7.4 and 8.8. Product scope reduction would remove guarantees people already rely on. A larger redesign would add cross-module coordination without addressing the observed missing registrations, validity checks or evidence.

## Local implementation evidence

Ten remotes continued to reject their original recorded revisions. For each, the correction restores the directly preceding parent-recorded gitlink from the history before `887ae0f`. Across all 13 direct modules, a checkout with empty module caches fetched the recorded revisions. All 121 declared nested worktrees remained uninitialized in the working repository. The Builds CLI generated the required source mapping, and the Debug solution passed all 6,667 tests with zero skips, including 37 tests against the Conversations source client. These revisions qualify module development. The dependency register retains the separate immutable targets for owner qualification.

The Release build finished with zero warnings and errors. All 6,581 package-mode tests passed with zero skips. This includes real server startup on loopback and a synthetic Conversations recovery in which the owner persisted the message, lost the acknowledgement and returned the original message on retry. The isolated consumer hook built six libraries and validated all seven package selections. The package-authority check passed for 66 projects with 46 declared probe exclusions. The NuGet advisory query returned no advisories for the 12 projects at verification time.

On an in-memory store returning an existing catalog snapshot, a single duplicate delivery allocated 8,864, 67,720 and 676,112 bytes per dispatch before the handler change at 10, 100 and 1,000 entries. The same probe measured 632 bytes at each size afterward, using 10 warm-ups and 100 measured calls per size. Storage reads and network latency were outside this measurement.

The release planner completed a local dry run against the audited commit and all eight upstream tags. The CiCd owner must still deliver the aligned workflow. Platform and Conversations must supply live qualification receipts.

[Updated epics](epics.md) · [Sprint status](../implementation-artifacts/sprint-status.yaml) · [Qualification register](launch-readiness-register.md)
