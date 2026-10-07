# Query deadline independent review — individual triage

All three context-free review layers completed before classification. The review target is the five-file scoped diff against the captured initial Parties source. The full parent remains in-progress.

| Finding | Verdict | Route | Verified evidence |
| --- | --- | --- | --- |
| B1 | medium | defer | Synchronous authority admission and local folding were already synchronous; the deadline guards refuse post-budget evidence but cannot forcibly terminate that work. Existing authority/processing responsiveness remains an owner qualification requirement, separate from the selected source/custody wait correction. |
| B2 | medium | patch | Current options access and historical deserialization precede deadline construction. Capture the monotonic start at entry and charge setup time to the same budget; preserve safe failure scope. |
| B3 | high | patch | Independent .NET 10 reproductions show a provider callback registered last can block linked-token cancellation before WaitAsync receives it. Private waiting must complete independently of provider cancellation callbacks. |
| B4 | medium | defer | Noncooperative synchronous provider invocations can retain workers after timeout. The previous direct invocation also retained a request worker indefinitely. The delivered source provider ports do not establish qualified cancellation/resource reclamation; bounded abandoned-work admission belongs to shared runtime/host qualification, not an invented local capacity policy. |
| B5 | low | patch | The second await observes an already terminal query, not actual late-provider completion. Synchronize provider completion before the terminal call-count assertions without adding an internal diagnostic API. |
| B6 | low | patch | The filed vectors deterministically cancel the caller first. Add a controlled deadline/fault-before-cancellation case where cancellation occurs before verdict release; cancellation after an already completed verdict is not required to change it. |
| B7 | medium | patch | Configured shortened budgets have only denial-path execution. Add current/historical success immediately before the exclusive budget boundary. |
| E1 | high | patch | The second independent console reproducer confirms the same shared-token callback timeout deadlock as B3. Separate wait cancellation from asynchronously requested provider cancellation. |
| V1 | medium | patch | Preverified gap: dependency expiry tests stop inside ReadAsync, while the final-authority test only cancels the caller. Add monotonic deadline expiry during final admission, with timer delayed, and assert cleared current/historical evidence. |

Patch groups: setup budgeting (B2), provider-callback isolation (B3/E1), late-completion synchronization (B5), ordered cancellation race (B6), shortened-budget success (B7), and final-authority expiry coverage (V1). Existing synchronous authority and abandoned-work resource cooperation remain explicit incomplete qualification requirements (B1/B4).
