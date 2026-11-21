# Independent architecture validation · incompatible implementations

**Changes required.** At Agents `2b21256`, the initiative spine admits two high-impact disagreements and two medium-impact disagreements. This review uses the current working-tree spine and traces the inputs below. Only the deterministic clock and encoding examples were executed. The lifecycle cases are design walkthroughs.

## High · A generated version crosses multiple command boundaries inside one activity

AD-18 limits each replay-safe activity to one trusted command. AD-27 keeps transient Provider output inside one activity through safety evaluation and persistence. The sequence uses that same activity to reserve and commit a directory lease, persist the version on AgentInteraction and settle the lease on the directory. Those writes affect separate aggregates and require separate durable commands. The Provider contract requires outcome lookup and deduplicated invocation. It does not currently require retrieval of the exact original output.

**Replay trace.** Assume positive Provider usage, successful safety evaluation and a committed mutation lease. Interrupt immediately before the version append. A single long activity can issue the several commands and recover its lease, but breaks the activity limit. Separate single-command activities satisfy AD-18, but the second activity cannot receive a reference to the transient content. A receipt proving usage alone cannot reconstruct it. A restart must also avoid dispatching a new generation for an already charged attempt.

**Disposition.** Architecture and the Provider owner should specify the durable step boundary and how recovery obtains the exact original output or terminates safely if it cannot. Preserve the one-version, one-charge and committed-reference invariants. Add an interruption fixture at that lease-to-append boundary. [Spine AD-18, line 512, AD-27, line 608, sequence lines 932–939](../ARCHITECTURE-SPINE.md) · [Implementation conventions, lines 9–15](../IMPLEMENTATION-CONVENTIONS.md) · [Provider contract, lines 183–188](../../../external-dependency-register.md)

## High · The edit path can persist a proposal without an edit-time safety verdict

In the confirmation sequence, edit, regeneration, approval, rejection and abandonment share one target command path. Only approval passes through a depicted Safety evaluation. AD-20 enumerates prompt, generated-output, approval and pre-post checks. FR-15 additionally requires a safety decision on the human edit before the target creates that version. Always-blocked content is rejected. Restricted content is stored with its own marker and cannot be approved.

**Edit trace.** Start from a decidable proposal and an eligible editor whose edit leaves a second eligible approver. Supply an edit classified in an always-blocked category. A team following the sequence can append `Edited` and discover the verdict only on a later approval request. A team following FR-15 leaves the original version current and records a typed rejection. The two histories disagree before anyone requests approval.

**Disposition.** Add the leased edit classification and both negative and restricted outcomes to AD-20 and the sequence. Carry the AD-8 prospective approver check into the edit and regeneration branches. Verify durable version count, marker, reason and approvability for each outcome. [Spine AD-8, line 374, AD-20, line 526, sequence lines 973–982](../ARCHITECTURE-SPINE.md) · [PRD FR-15, line 412 and FR-27, line 724](../../../prds/prd-agents-2026-06-23/prd.md)

## Medium · Automatic posting can use two different expiry settings

AD-4 freezes the proposal expiry duration at request time. In the sequence, the request snapshot and acceptance are separate commits divided by external membership, safety and budget checks. FR-11 computes the automatic posting window from the Agent setting at acceptance. A configuration update can commit between those two events.

**Clock check.** Take a one-hour setting at request, change it to 24 hours before acceptance, then evaluate a system-approved record after two unpaused hours in `Approved`. Comparing the evaluation instant against both candidate deadlines yielded elapsed for the request snapshot and open for the acceptance snapshot.

**Disposition.** Bind the automatic window to the configuration value and revision required by FR-11 at acceptance, or obtain Product reconciliation if request-time pinning is intended. Exercise both orderings of the configuration update and acceptance. [Spine AD-4, line 307 and sequence lines 768 and 865](../ARCHITECTURE-SPINE.md) · [PRD FR-11, line 351](../../../prds/prd-agents-2026-06-23/prd.md)

## Medium · Independently derived IDs can differ for the same Unicode input

AD-29 specifies UTF-8, length prefixes, separators and SHA-256. It says the interaction identity follows shipped behavior. The input tuple framing does not name the unit for the prefix, the trailing separator or a portable test vector. The shipped C# code counts UTF-16 code units before UTF-8 encoding and appends the separator after every component.

**Encoding check.** For the tuple `tenant-1, hexa, conv-1, caller-1, clé-🍐`, the final component has six UTF-16 code units and nine UTF-8 bytes. Keeping all other framing identical, the respective SHA-256 outputs were `56bf269e426bed2d784af40bd0ff0a7e4ac78a8e9be95096d9312312a30d4c16` and `e17e37a38c35777f2cf5a48b332d3d04e9f1ddfed871b460a02710e3f7b53c37`. An independent producer using UTF-8 byte length would address a different interaction on replay.

**Disposition.** Publish the exact shipped encoding and one cross-language fixture with a non-ASCII component. Preserve every existing ID and compare independent producers against the same vector. [Spine AD-29, line 624](../ARCHITECTURE-SPINE.md) · [AgentInteractionIdentity, lines 46 and 53](../../../../../src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionIdentity.cs) · [AgentsSecurityIdentity, line 18](../../../../../src/Hexalith.Agents.Contracts/Security/AgentsSecurityIdentity.cs)

The OQ-35 through OQ-38 owner choices retain their stated fail-closed boundaries. This review assigns no finding for their deferred status. [Blocking Open Decisions](../ARCHITECTURE-SPINE.md#blocking-open-decisions)
