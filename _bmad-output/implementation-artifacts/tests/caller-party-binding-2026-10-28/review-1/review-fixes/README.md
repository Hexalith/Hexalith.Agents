# Caller identity review fixes

The Debug/source Server build completed with zero warnings or errors. Only the two edited suites, `PartiesAgentPartyDirectoryTests` and `AgentInteractionGateOrchestratorTests`, ran: **115/115** tests passed with zero failures, errors, skips or not-run cases. Exact commands, logs, XML, source patch and changed-file hashes are retained here; earlier receipts remain unchanged.

The directory rejects noncanonical stable actor ULIDs before the owner call and requires the binding to have been effective at the verified actor observation. Regression coverage includes matching malformed actor IDs, a binding effective between the earlier actor observation and later owner observation, inactive/restricted/erasing Human responses with no released Party/deadline, and serialized current/invalid replies through the real `HttpPartiesIdentityClient` using a local intercepted HTTP handler. The gate regression advances the clock to the exclusive deadline during client-extension enumeration and verifies denial without dispatch.

The evidence entry point now links prominently to the superseding full results while preserving the initial historical counts, logs and receipts. Full Local/Live verification for this changed source remains the parent task's next step. No owner source, runtime binding, dependency status or custody provisioning changed.
