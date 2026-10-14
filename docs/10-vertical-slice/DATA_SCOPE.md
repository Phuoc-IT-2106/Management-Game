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

Content schema 4 (rules v4, see [the player world](RULES_V4_PLAYER_WORLD.md)), with save schema 6 (since 2026-10-14; the
authority envelope's recruitment limits; `Lifecycle.PoolFloor` is a content field of schema 4):
- **from schemas 2 and 3** ([rules v2](RULES_V2_CONTINUOUS_CAMPAIGN.md), [rules v3 wave 1](RULES_V3_INCENTIVES.md)):
  balance and season shape, the incentives, rival organization names and the sponsor brand pool;
- **players:** role weights; ability bands; archetypes per role; age and nationality tables calibrated from aggregate
  2026 data (`Calibration` pins the report's SHA-256); name pools per nationality; handle parts;
- **the league:** best-of formats, playoff places and interval, place prizes, familiarity and match fatigue, and the
  logistic scale;
- **`Lifecycle`:** development bands; the retirement table and intake ages written by the calibration; youth intake and
  the free-agent pool; form, morale, popularity and conduct steps;
- **`Rivals`:** rival edge, squad sizes, wage caps, funds and recruitment limits;
- **transfer fees** in days of wage, rising with quality; the underdog shift of the company's opening squad; the base
  reputation gain for a win.

People, rivals, schedules and offers are generated per seed; no names are fixed in code. No scripts or generic mod
loader.
The v1 description (six fixed players, two rivals, four fixtures, fixed offers) is historical.
