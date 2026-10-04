# Deterministic fixture matrix

Version company-map-fixtures-v1. No simulation/content pack, seed, clock or save.
Identity reuses LabFixtures with DEV_ORG_001, TEST_COMPANY / FIXTURE_ORGANIZATION,
revision 7, explicit DEVELOPMENT FIXTURE — NONCANONICAL. Company names never key
selection. Missing-emblem baseline exercises fallback in every ordinary case.

| Launch case | State / purpose |
| --- | --- |
| normal | Company selected; all four situations, two High badges |
| sponsor | Sponsor selected in Business & Finance; reason/evidence/entry |
| competitive | Team preparation selected; unknown information |
| quiet | All four matters Normal; no urgent situations |
| empty | No situations; structure and next checkpoint remain |
| long-name | Full Stage 2 Unicode long identity, same IDs |
| branding | Low-contrast raw primary/secondary; canonical resolver |
| missing-identity | Empty name/short name; Player company / neutral fallback |
| list | Sponsor selected in synchronized ownership list |
| decision | Sponsor read-only entry with origin retained |
| return | Actual sponsor entry then Back; original map context |
| affairs | Sponsor selection with all chronological links exposed |

Each captures at 1280×720 and 1920×1080. Native verification also cycles all
cases per viewport and checks no horizontal overflow. A fixed render viewport
is distinct from the OS-clamped native window. These are golden candidates only.
Additional nonvisual replacement cases: stale revision, deleted team/matter,
renamed company, new session with same IDs, invalid target and duplicate entry.

Run `./build/Run-OperatingMap.ps1 -Case sponsor` (or any case above). This is a
development fixture selector via launch argument, not an identity editor or saved
player setting. No fixture facts are production canon.
