# Independent review and triage — 2026-10-07

Three fresh context-free Codex reviewers completed the bmad-build review before triage. The review input was the correction diff relative to selected-file pre-change snapshots plus the new synthetic harness. Existing owner edits were outside that correction baseline; no staging was used.

| Layer | Task | Returned findings |
| --- | --- | --- |
| Blind adversarial | `/root/corrections_blind_review` | Eight |
| Edge cases | `/root/corrections_edge_review` | Two |
| Verification gaps | `/root/corrections_verification_review` | Two |

The child spec's Review Triage Log records every finding independently before grouping. Twelve findings became seven patch groups, one deferred group and one rejected group. There were no intent gaps or bad-spec findings. All three layers ran; none was skipped. Reviewer minimum-count pressure was treated as a prompt constraint, not a finding.

| Finding(s) | Resolution | Executed regression |
| --- | --- | --- |
| Blind 1 | Retained events must be JSON objects; preserve arrays during parsing so singleton arrays cannot become events. | `transition-array` |
| Blind 2 | Versions and positions must be JSON integer values before conversion to Int64. | `string-predecessor`, `string-binding-version`, `string-actor-revision` |
| Blind 3 | String instants use System.Text.Json's DateTimeOffset parser; already materialized UTC DateTime/ticks remain supported. | `timestamp-grammar`, `timestamp-source-expiry-grammar`, `positive-ticks` |
| Blind 4; Edge 1; Edge 2 | Revocation custody expiry must equal its effective instant plus the accepted duration; derive the valid fixture from the revocation's own instant. | `revocation-wrong-expiry`, `revocation-past-expiry`; valid revoked/rebound controls |
| Blind 5 | An opening event must end at custody expiry before later transitions reconstruct any closure. | `opening-shortened-without-close`; valid closed/rebound intervals |
| Blind 8; Verification 1 | Execute a controlled identity reply through the DI-resolved client and check the registered clock was read exactly once. | `AddPartiesClient_IdentityAcceptanceUsesRegisteredClock`, just before and exactly at exclusive expiry |
| Verification 2 | Refuse a 307 redirect, emit no success receipt and issue zero destination requests. | `redirect-307`, separate localhost destination |
| Blind 6 | Defer owner privacy/schema-drift qualification; the pre-change parser and PartiesJsonOptions already permit unknown fields, while SDK output closes the schema through typed serialization. | New entry appended to `deferred-work.md`; no production qualification claimed |
| Blind 7 | Reject the portability concern for this explicitly local evidence harness; its owner path and commands identify the inspected checkout. | No new configuration surface |

The implementing agent applied the seven narrow patch groups sequentially, preserving prior snapshots. The owning Contracts types and PartyAggregate.Identity establish the numeric/timestamp/opening/revocation rules. The targeted review-fix build and 13 DI cases passed; the 15 targeted synthetic cases passed. Root's separate final verification packet records the subsequent complete affected source lanes and synthetic matrix. Synthetic results do not supply owner acceptance, exact installed-target compatibility or production readiness.

Final root verification completed: 238 source tests passed and all 42 synthetic cases met their expected outcomes. Both normal Debug builds reported zero warnings/errors; script parsing and owning whitespace checks passed. Selected current owner bytes match the snapshot overlay and all original story/register/sprint hashes match baseline. `final-verification/evidence.json` and `final-packet-manifest.json` record current evidence. The correction child is done; the accepted parent's no-commit/no-push boundary overrides the generic terminal commit step, and full Story 5.4 remains blocked.
