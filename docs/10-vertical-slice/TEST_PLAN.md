# Verification contract

Engine-independent executable test projects fail with nonzero exit on assertion
failure (no third-party test framework dependency). Separate Domain, Integration
and UI presentation-model categories. Record exact commands and actual counts.

Verify RNG/numeric vectors, ownership, invalid/stale/conflicting command atomicity,
same-seed per-boundary repeatability, extra-query independence, manual/delegated
gameplay parity, two different consecutive cycles, finance-before-match timing,
hidden-truth filtering, save round trip/continuation and rejected corrupt loads.
Godot smoke must invoke real screen handlers and application commands, save/load,
and compare headless hashes. Rendered checks supplement headless tests.

Measure production roster plus bounded synthetic query scale: rows/controls,
sort/filter/search, binding/page, scrolling, acknowledgement and retained memory.
Report work time and wall-frame timing separately; synthetic input does not prove
physical usability. No clean-machine requalification or optimization project.
