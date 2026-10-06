# Development fixture identity

The sole content edit is `CompanyName`: `Northstar Esports` → `DEV_ORG_001`.
The former lore-like name must not imply player-company canon. The visible
**DEVELOPMENT FIXTURE — NONCANONICAL** notice remains in the live company and
sponsor presentation. Stable company ID `company:player` is unchanged.

| Identity | SHA-256 |
| --- | --- |
| Published Stage 4 fixture | `605c6673574379e0d18d97e969e8db58eb84c3f004057d22d618cc9ced9e3f89` |
| Stage 4.1 fixture | `f874fb34b3991c0167aeb6c4285ec1f71974af12d200b4ced98f43d1bb1322ea` |

`content/fixture.sha256` matches the new bytes. The targeted test reconstructs the
old bytes by reversing only that property value and requires the exact published
hash. It cannot silently pass a change to balance, offers, players, rivals or rules.

**Simulation behavior unchanged; canonical hashes change because company name and
exact content identity are canonical state.** Tests compare the entire canonical
state after normalizing only those two identity fields: initial state, sponsor
acceptance, coach delegation, two consecutive advances, and all 63 phase boundaries
through the first match with the same signed sponsor and committed plan. Receipts,
outcomes, ledger, preparation, rival behavior and every other state field must match.

Save schema remains **1**. An actual save written with the old loaded content is
rejected by the new Session with a content-identity error, preserving current state.
A new-content signed campaign saves and loads with identical canonical state.
This is development content cleanup; no migration or old-save compatibility is
promised. No abbreviation/emblem/colors persistence, branding editor, identity
system or campaign-creation feature was added.

Current Headless/Main and Integration deterministic hashes are recorded in
[verification](VERIFICATION.md). Historical Stage 4 hashes remain in their original
report; they were not forced to match the new content.
