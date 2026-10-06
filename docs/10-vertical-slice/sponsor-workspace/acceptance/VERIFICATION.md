# Executed acceptance verification — 2026-10-06 (Asia/Saigon)

Inspected local/remote main `7ad0501fd0719c48b8c759a491771525567fa863`; verified
baseline publication `0e9bbee0ea27ff5a6409eefa62b7d532384ce312`. Final runtime/fixture
source `043ad9bee1c149eb9cce407a419a1e5eda6f8799`. Documentation was being assembled
after that commit; screenshot manifests conservatively record `Dirty=true`.
The source inventory/digest identifies the actual tested runtime. Original Stage 4
tests retain their 2026-10-05 `a381d8f...` plus working-files provenance.

Windows; pinned SDK 10.0.401/runtime 10.0.12; Godot
4.7.2.stable.mono.official.ed1daf0bf. Rendered sponsor candidates use Compatibility
OpenGL / Intel Iris Xe, Segoe UI, Compact, text scale 1. Native kit/map/Main smoke
use headless Godot. No engine, dependency version, gameplay formula or save schema
change. Exact new content hash is in [fixture cleanup](FIXTURE_IDENTITY_CLEANUP.md).

Executed `build/Measure-SponsorWorkspace.ps1` before and after the fix, followed by
`build/Verify-SponsorAcceptance.ps1`. Native execution and Windows File.Replace
save tests used native Windows access. Final build logs contain zero warnings/errors.

| Check actually executed | Result |
| --- | --- |
| Client Debug and managed Release builds | PASS |
| Full solution Debug and three additional test-project builds | PASS |
| SponsorWorkspace | PASS 52 |
| Native sponsor review 1280×720 / 1920×1080 | PASS 29 / 29 |
| Domain / Integration / UI | PASS 4 / 162 / 3 |
| Integration phase-save continuation | PASS 63 boundaries |
| UiKit pure / native | PASS 126 / 24 |
| OperatingMap pure / native | PASS 48 / 101 |
| Headless two-cycle scenario | PASS |
| Internal Main smoke, old sponsor tab, save/load | PASS, 2 results, 7 bound rows |
| Main / Headless final canonical parity | PASS |
| Native capture matrix | PASS 7 PNG/JSON pairs, correct dimensions, empty stderr |
| Performance retained Controls | PASS 53→53 in all four runs |
| Native repeated transitions and enter/back | PASS 25 + 25 cycles at each review viewport |
| Fixture equivalence and old/new save identity | PASS; included in 52 checks |

Final Headless/Main hash:
`29ee08334c3ceccc0e9e1dad5cabaa59b2af555921e9a63b6ef1a4aec00d4984`.
Final Integration hash:
`f8614b89b1ce12b298f158337c6d40090ef64419b23b540667c57ae7d6920ec7`.
These differ from Stage 4 because company name/content identity are canonical;
the fixture equivalence test checks full state with only those fields normalized.

## Regression coverage

Review submits nothing; Confirm submits exactly once; pending refresh and repeated
Enter/click cannot resubmit; stable IDs survive sorting and refresh; stale revision
rejects without mutation/retry; cancel/back preserves state and exact return context.
List-first and optional map reach the same live presenter. Successful signing adds
the existing day-4/11/18/25 receipts and immediate load, leaves Cash unchanged,
consumes the existing slot and preserves actual-win-only bonuses. Preparation
conversion and hidden-information boundaries remain checked by existing tests.
The signed agreement survives schema-1 save/load; the internal sponsor action
continues to execute in Main smoke using the same Application path.

Six additional native checks cover retained scroll regions/position, immediate
retired callback cleanup, retained document with fresh revision metadata, exact
command ID/revision, different offer at the same revision, and changed company,
terms and Cash at the same revision. Nine fixture checks establish byte-only name
change, canonical difference with normalized equivalence, identical decisions,
63 phase boundaries, rejection of old exact-content saves and new-content roundtrip.
The additional native harness file adds one authority-boundary source check.

Screenshot source digest (UI and src inventory):
`efe569fe4bc47dfc2423c088090c5b61a294d78fe151c5b12c79d5476e2aa5c1`.
Performance digest (all Client and src C# inventory):
`736c78060aa436589d24b65d82b1cbe467f4b12e5cb85f93c8aa295c74a77aa1`.
The inventory scopes differ intentionally; they are not interchangeable hashes.

[Test/build/engine logs](evidence/checks/) · [Candidates and manifests](evidence/candidates/) ·
[Before/after performance raw samples](evidence/performance/).

No human review, physical-input latency, actual DPI/mixed-monitor, exported engine,
target hardware, second-machine determinism or distribution acceptance is inferred.
Historical QG-02 and wider DV obligations remain unchanged.
