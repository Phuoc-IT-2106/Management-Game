# Content and numeric contract

Built-in versioned JSON development fixture with explicit IDs and SHA-256 identity.
Immutable typed definitions; strict validation before campaign creation. Signed
terms freeze into campaign facts. Content edits require a new campaign/identity.

All names, counts, ratings, minor-unit currency amounts, cadences and coefficients
are NONCANONICAL DEVELOPMENT FIXTURE. No prototype calibration is promoted.
Ratings use integer 0–100 units; money uses checked signed 64-bit minor units;
probabilities use integer millionths. Arithmetic rounds ties to even at explicit
division boundaries. Keyed SHA-256 rng-v1 uses length-prefixed NFC UTF-8 fields and
big-endian lengths/output, with rejection sampling. Golden vectors precede use.

Content schema 3 (rules v3 wave 1, see [incentives](RULES_V3_INCENTIVES.md)):
- balance and season shape, attribute generation ranges, name pools, rival organization names and a sponsor brand
  pool, as in schema 2 ([rules v2](RULES_V2_CONTINUOUS_CAMPAIGN.md));
- new in schema 3: the running cost, the roster minimum and its fine, the sponsor termination clause, the season-end
  reputation pull and the predictability window.

People, rivals, schedules and offers are generated per seed; no names are fixed in code. No scripts or generic mod
loader.
The v1 description (six fixed players, two rivals, four fixtures, fixed offers) is historical.
