# Review patch verification handoff

The original verification/source manifests are preserved unchanged. They describe
pre-review implementations and must not be presented as final implementation
bindings. After the coordinator finishes all source, metadata and package-validation
changes and the full verification run, recompute the final source/evidence manifest,
bind executed assembly hashes and the fixed case inventory, and preserve the old
manifest bytes as historical evidence. This review handoff does not restate older
full-suite results as proof of these patches.

Fresh focused logs will be retained here. No files were staged and no gate policy
was changed. The state reproducer now allocates fresh durable output by default;
explicit destinations and dependency experiment destinations must not exist.

The compiler-input inventory now includes applicable analyzer, additional-file and
editor configuration bytes. The source/tag comparison will expose the recorded
EventStore .editorconfig byte change; final reconciliation must account for it
instead of reusing the previous graph-equality claim.
