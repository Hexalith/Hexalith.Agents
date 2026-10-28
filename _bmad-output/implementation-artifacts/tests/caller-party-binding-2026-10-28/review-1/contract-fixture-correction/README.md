# Current binding interval and custody correction

The post-audit focused Debug/source build has zero warnings or errors. The six required Server suites pass **244/244** tests with zero failures, errors, skips or not-run cases. Exact commands, logs, XML, source patch and post-run source hashes are retained here. Previous receipts and source captures remain historical and unchanged.

`HttpPartiesIdentityClient.Matches` requires a non-null binding end strictly after its effective time and custody expiry at or after that binding end. The directory now enforces those requirements in addition to its existing exact correspondence, UTC, completion-time, provenance and custody guards. Positive custody fixtures close the binding at the derived custody deadline; the open-binding positive was removed. The owner-read completion fixture and joined reader/directory/gate custody fixture use that same valid shape.

Four negative cases now cover a missing interval end, empty interval, reversed interval and custody shorter than an otherwise current binding. The earliest actor/tenant/binding/custody deadline still propagates through all later protected reads and the final pre-dispatch check. A contract-valid custody deadline may coincide with the binding end; it cannot precede that end.

This focused run supersedes the earlier 241-case focused run for current-source qualification. The parent task repeats full Local/Live verification and records the final dependency/SDK observation separately. No owner source or contract changed, no dependency became Available, and full Story 5.4 remains incomplete.
