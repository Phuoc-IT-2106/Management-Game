# Proposed decisions — awaiting the Director

Status: **PROPOSED**, drafted 2026-10-14. Each entry below records changes that were made on the user's explicit
instruction and have been built, but have no DEC entry yet. The [decision log](DECISION_LOG.md) holds approved
decisions only, so these drafts live here.

**To accept an entry:**
1. Move it to the decision log.
2. Set `Status: Accepted` with the acceptance date and the approval owner.
3. Update the accepted range in [PROJECT_STATE](../00-project/PROJECT_STATE.md).

**To reject or change an entry:** edit it here and record why.

None of these entries reopens a phase gate, changes a QG result or discharges a DV obligation. Every number they cite
is a noncanonical development fixture value.

| Draft | Title | Built | Record |
|---|---|---|---|
| DEC-024 | Continuous campaign (rules v2) | 2026-10-06 | [RULES_V2_CONTINUOUS_CAMPAIGN](../10-vertical-slice/RULES_V2_CONTINUOUS_CAMPAIGN.md) |
| DEC-025 | Management-genre shell amendment | 2026-10-06 | [STAGE5_GENRE_SHELL_AMENDMENT](STAGE5_GENRE_SHELL_AMENDMENT.md) |
| DEC-026 | Incentives and measurement (rules v3 wave 1) | 2026-10-08 | [RULES_V3_INCENTIVES](../10-vertical-slice/RULES_V3_INCENTIVES.md) |
| DEC-027 | Money shown in US dollars | 2026-10-08 | [genre-shell README](../10-vertical-slice/genre-shell/README.md) |
| DEC-028 | The player world (rules v4) | 2026-10-09 to 13 | [RULES_V4_PLAYER_WORLD](../10-vertical-slice/RULES_V4_PLAYER_WORLD.md) |
| DEC-029 | Gate 1 reads reputation by partial correlation | 2026-10-13 | RULES_V4, phase 6, round 5 |
| DEC-030 | The head coach recruits within the owner's limits | 2026-10-14 | RULES_V4, "After rules v4" |

---

### DEC-024 — Continuous Campaign (Rules v2)
Date: 2026-10-06
Status: Proposed

Context:
The bounded 28-day fixture (`slice-rules-v1`) ended before contracts, markets or rivals could matter, so the Vertical
Slice could not show a management cycle that continues.

Decision:
- Time runs in consecutive seasons with no end date. Each season's schedule is regenerated from the seed.
- Sponsorship becomes a market:
  - brands approach when the company meets their tier;
  - the player proposes terms;
  - after a delay the brand accepts, counters or refuses.
- People, rivals and brands are generated per seed from content pools, and the player names the company.
- Contracts end on staggered days and open for renewal before they end. There is a coach market.
- A missing role forfeits the match.
- The change sets `slice-rules-v2`, content schema 2 and save schema 2. Saves from v1 are rejected, with no migration
  (DV-08).

Alternatives:
- Keep the bounded fixture and only tune it.
- Add seasons without markets or generation.

Reasoning:
A continuing campaign is the smallest scope in which contracts, markets and rival change can be tested.

Consequences:
- The Phase 3 prototype's 84-day horizon and authored fixtures are no longer used in play.
- Balance has to be measured over several seasons, which `build/Measure-Rules.ps1` now does.

Affected Systems:
- calendar and seasons
- sponsors and negotiation
- contracts, talent and coach markets
- content generation
- save schema

---

### DEC-025 — Management-Genre Shell Amendment
Date: 2026-10-06
Status: Proposed

Context:
The Stage 5 rules read the genre's familiar skeleton as a "dashboard" and rejected it outright. Under that reading a
recognizable management game could not be built.

Decision:
- The amendment pre-approves the management-genre shell:
  - a top bar with one Continue control;
  - a collapsible navigation rail;
  - a Portal home;
  - dense tables;
  - bounded tiles.
- It replaces two earlier readings: the Operating-Space-as-home reading and the dashboard hard rejection.
- Every rule on uncertainty, accessibility, data honesty and anti-SaaS styling is kept.

Alternatives:
- Keep the Operating Space as the home screen.
- Justify the shell screen by screen.

Reasoning:
Reference games (FM26, SM26, EM2026) share this skeleton. Familiarity lowers the cost of learning the game. The
amendment keeps the lessons those games teach about what fails.

Consequences:
- The genre shell is the main scene. The Company view stays as an embedded optional view.
- The 50-rule brief stays byte-identical. The amendment records how the brief now applies.

Affected Systems:
- navigation and screen composition
- UI polish standard
- screen review records

---

### DEC-026 — Incentives and Measurement (Rules v3, Wave 1)
Date: 2026-10-08
Status: Proposed

Context:
Under rules v2, losing was profitable: a thrifty owner ended richer than a balanced one on 5 of 8 seeds. Reputation did
not follow results either (r = 0.522).

Decision:
- Rules v3 wave 1 adds:
  - a fixed daily running cost;
  - a registered-roster minimum, with a fine when the roster falls short;
  - sponsor termination after two forfeits in a season;
  - a season-end pull of reputation toward the level the record earns.
- It fixes the v2 predictability penalty.
- It adds a Headless measurement (four owner policies × 8 seeds × 5 seasons) with roadmap gates.
- The change sets `slice-rules-v3`, content schema 3 and save schema 3.

Alternatives:
- Tune prizes and wages only.
- Postpone the incentive changes to the league work.

Reasoning:
Winning has to pay before any later wave can be judged. A repeatable measurement turns that into a gate rather than an
opinion.

Consequences:
- Gate 1 passed: balanced ≥ thrift on 8/8 seeds, and reputation r = 0.807.
- Later waves are measured by the same tool.

Affected Systems:
- finance and running costs
- roster rules
- sponsors
- reputation
- Headless measurement

---

### DEC-027 — Money Shown in US Dollars
Date: 2026-10-08
Status: Proposed

Context:
Player-facing money used an abstract currency unit (CU), which read as a placeholder.

Decision:
- Money is shown in US dollars, at 1 CU = $1, with cents where an amount has them. One formatting module covers every
  screen.
- Only presentation changes. Stored values, balance and rules do not.

Alternatives:
- Keep CU.
- Introduce a fictional currency.
- Rescale values at the same time.

Reasoning:
A real, familiar currency reads as a game rather than a prototype. Relabelling without rescaling keeps every
measurement comparable.

Consequences:
- Documents that measured in CU stay valid, read at $1 per CU.
- Currency remains a presentation choice that could be revisited before release.

Affected Systems:
- formatting
- top bar and every money display

---

### DEC-028 — The Player World (Rules v4)
Date: 2026-10-09 to 2026-10-13
Status: Proposed

Context:
The [gameplay data review](../10-vertical-slice/GAMEPLAY_DATA_REVIEW.md) found five problems:
- matches were decided by two numbers;
- several attributes did nothing;
- players had no career;
- rivals were six numbers;
- no player stat reached the company side of the game.

Decision:
- The game has five LoL-style roles and a role-weighted player model. Each player has skills, mind, age, potential,
  form, morale and popularity.
- Matches are logistic best-of series, played in an eight-team double round robin with playoffs and place prizes.
- Players have careers: development, retirement, youth intake and a persistent free-agent pool.
- Rival organizations have real squads, coaches and funds, and recruit and trade with each other.
- The world is calibrated from aggregate 2026 data only. No real person, name or handle enters the game.
- The user made three balance decisions:
  - the company starts as an underdog;
  - every win earns a base reputation gain;
  - transfer fees rise with quality (20–60 days of wage).
- The change sets `slice-rules-v4`, content schema 4 and save schema 5.

Alternatives:
- Tune the rules v3 model.
- Import real players.
- Build the league without rival squads.

Reasoning:
These are the review's P0–P2 directions. Aggregate-only calibration gives a believable world without licensing real
people.

Consequences:
- Gates 1 and 2 pass (`Measure-Rules -Gate 2`).
- Some reference metrics are still outside their ranges; the session handoff lists them.
- The real-data tooling stays research-only, under the provider's terms.

Affected Systems:
- player and coach model
- competition and league
- careers
- rivals
- content calibration
- save schema
- Squad, Competition and Portal screens

---

### DEC-029 — Gate 1 Reads Reputation by Partial Correlation
Date: 2026-10-13
Status: Proposed

Context:
Rules v3 has reputation converge on what a season earns. Under that pull, a strong team's change is small even in a
strong season, which dilutes the raw correlation between win rate and reputation change (0.54) below the gate's 0.6.

Decision:
Gate 1 "reputation follows results" reads the partial correlation of season win rate with the season's reputation
change, holding the starting reputation fixed. The measure was 0.91. The raw correlation is still reported as a
reference.

Alternatives:
- Keep the raw measure and weaken the convergence.
- Lower the threshold.

Reasoning:
The partial correlation asks the gate's actual question, whether results move reputation, without penalizing the
convergence the rules intend.

Consequences:
- Gate 1 passes on this measure.
- The Director should check that the measure still captures the intent behind the gate.

Affected Systems:
- Headless measurement (gates)

---

### DEC-030 — The Head Coach Recruits Within the Owner's Limits
Date: 2026-10-14
Status: Proposed

Context:
Under rules v4, a coach with full delegation never recruited. The delegating owner fell far behind an owner who
recruited: the player-value reference was 37 points, against a range of 5–15. DEC-014 already allows delegation inside
an authority envelope.

Decision:
- The authority envelope gains two limits set by the owner: the largest signing fee and the largest daily wage of one
  signing. Zero turns recruiting off, and is the default.
- An autonomous coach with both limits set signs free agents inside them. It has a fixed order of priority:
  1. cover a role nobody in the squad plays;
  2. keep the league's registered-roster minimum;
  3. make at most one upgrade every 28 days, paid from no more than half the cash.
- An upgrade lets the displaced player go: the weakest player of that role who can be released, or, when the roster is
  full, the squad's weakest such player. The release happens only if the exit cost also fits the fee limit. On a full
  roster without such a release, the coach makes no upgrade.
- The coach works from the same rating estimates the owner sees, never true ratings, so it has no hidden omniscience.
- The coach does not recruit while the company is in distress or restructuring. Renewals stay with the owner.
- The change raises the save schema to 6.

Alternatives:
- Leave recruitment manual and redefine the player-value reference.
- Let the coach recruit without limits.
- Let the coach sign but never release. This was measured first: the squad filled to the limit in season 1 and the
  coach could not upgrade again, so player value stayed at 19.4 points.

Reasoning:
Delegation should be a management choice with a visible envelope, not a trap. The limits keep the owner accountable for
the money.

Consequences:
- The coach's signings are recorded like any signing and appear in Transfer News.
- Measured effect: see RULES_V4, "After rules v4".

Affected Systems:
- delegation and authority
- talent market
- Competition screen
- save schema
- Headless measurement
