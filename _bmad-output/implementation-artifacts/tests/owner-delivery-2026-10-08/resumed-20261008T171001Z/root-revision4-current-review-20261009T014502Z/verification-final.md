### Final export-operation credential withdrawal is unverified

- **Changed surface:** `platform/src/Hexalith.Platform.Custody/ExportKeyDeliveryActor.cs:26` and `:58` recheck the private method credential before returning delivery or lookup results.
- **Impacted consumer or site:** the private actor’s `DeliverAsync` and `LookupAsync` response boundaries at `ExportKeyDeliveryActor.cs:20` and `:52`.
- **Existing test evidence:** `Regression gap`. `ExportKeyDeliveryActorTests.cs:152` revokes credentials before subsequent invocations. The boundary test at `:98` changes physical delivery authorization instead. Whole-Platform actor/interface/authority symbol searches found no other active test file covering this actor. An isolated mutation removing both final checks built successfully, and all **20 actor tests passed**. Evidence: `/tmp/hexalith-export-authorization-review-e0jr9ik6/mutation-results.xml`.
- **Missing verification:** assert that withdrawing the private method credential during delivery or lookup returns `Unavailable`, while preserving the durable original outcome.
- **Demonstration:** removing both final authorization checks allows an invocation admitted before withdrawal to return `Delivered` afterward. The existing tests still pass because they keep the private method credential valid throughout each invocation.
- **Consequence:** a formerly authorized caller receives delivery evidence after its operation credential has been withdrawn.
- **Disposition:** `patch` — add controlled in-flight credential withdrawal cases to `ExportKeyDeliveryActorTests`, suspending release and lookup before their results return.
