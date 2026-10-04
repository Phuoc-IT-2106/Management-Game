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

Initial content: six players, one coach, two rivals, four scheduled competitions,
one candidate, active sponsor and two alternative offers, employment/sponsor
terms and bounded balance configuration. No scripts or generic mod loader.
