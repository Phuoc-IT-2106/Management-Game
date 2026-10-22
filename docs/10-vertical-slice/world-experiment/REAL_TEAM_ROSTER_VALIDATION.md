# Real-team roster validation

Date: 2026-10-15. Status: **EXPERIMENT / RESEARCH**. Nothing here changes `content/`, saves, rules or the production
world. The pack it describes is local, git-ignored and holds real names; it is never committed or redistributed
(PandaScore terms 6.3–6.4; the [DEC-031](../../01-governance/PROPOSED_DECISIONS.md#dec-031--real-players-and-teams-in-the-game-from-a-locally-built-pack)
proposal). This document holds rules, aggregate counts and synthetic test cases only.

Labels: **FACT** (measured or read in code/data), **INFERENCE**, **PROPOSAL**, **OPEN QUESTION**.

## 1. What was built

| Item | Where |
|---|---|
| Roster tool | `tools/lol-data-ingestion/build_real_rosters.py` (offline; reads the staging database and the research ratings) |
| Unit tests | `tools/lol-data-ingestion/tests/test_real_rosters.py` (8 synthetic cases) |
| Output pack | `data/world/real_roster_snapshot_2026-10-13.json` (git-ignored; SHA-256 `3674c6f0…`) |
| Aggregate summary | `data/world/real_roster_validation_summary.json` (git-ignored) |
| Handle and team-name exclusions | `data/world/real_handle_exclusions.json`: 15,145 real handles and 1,940 real team names, normalized, so generated NPCs never reuse them |

As-of day: **2026-10-13**, the last game day in the staging data (the ratings use games before 2026-10-14). Ratings come
from the research model run with the uncapped scale experiment (see
[PLAYER_POTENTIAL_TRAINING_EXPERIMENT §2](PLAYER_POTENTIAL_TRAINING_EXPERIMENT.md#2-scale-experiment-the-same-model-without-the-clamp)).
For every real squad player the Overall is identical in both runs, so the roster results do not depend on that choice.

## 2. The entities kept apart

| Entity | Source | In the pack |
|---|---|---|
| Organization | None (PandaScore has no organization grouping) | One organization per provider team, `org:real:ps:{id}`; marked as an assumption |
| Competitive team | PandaScore team | `team_id`, name, acronym, location |
| Academy team | No source field | `academy_affiliation = UNKNOWN_NO_SOURCE_FIELD` for every team (§6) |
| Player | PandaScore catalogue | provider id, handle, nationality |
| Current employment | Derived (§3, authoritative employer) | one team per player |
| Squad registration | PandaScore stage registrations of the latest stage | starters and substitutes |
| Starting lineup | Leaguepedia participation (HIGH identities) | one verified player per role, or a reason |
| Tournament registration | PandaScore `ps_registrations` | every 2026 stage registration of a squad player |
| Historical membership | The same registrations, dated | 9,669 rows, immutable; 68 national-team rows kept as observations only |
| Source observation | Game keys and registration rows | evidence counts per slot |

The provider's `active` flag and `current_team` are **never** read (FACT: `build_real_rosters.py`).

## 3. Rules (all AUTHORED; printed into the pack)

1. **Team set.** A team enters when PandaScore registers it for a stage that began on or before the as-of day and ended
   in 2026. Its **latest stage** is the registered stage with the latest start.
2. **Evidence window.** The team's last 10 primary games played on or before the as-of day and no earlier than the
   latest stage's start. When that stage has fewer than 3 games, the previous stage's start is used. A game counts for
   a team only through a HIGH team alias.
3. **Starter of a role.** The player with the most evidence games in that role (ties: the later appearance). Highest
   Overall is never used. The slot is **verified** only when:
   - the seat's identity is `HIGH_CONFIDENCE_MATCH`;
   - the player played at least 3 games and at least 40% of the evidence games in that role;
   - PandaScore registers the player on the team for the latest stage (or the previous one when the evidence reaches it);
   - the player's authoritative employer is this team.
4. **Authoritative employer.** The team of the player's latest appearance on or before the as-of day. A player who
   never appears is held by the team whose latest stage began last. Every other registration is *secondary* and gives
   no squad place. So no player is in two squads.
5. **National teams** (leagues named like "Asian Games", "nations", "national") are not employers. Their games never
   decide a club, and they are never a playable team.
6. **Squad.** The five starters and the other players registered for the latest stage who are authoritative here.
   At most 16.
7. **Status.**
   - `VERIFIED`: five verified slots, five distinct players.
   - `INCOMPLETE`: a slot failed; the reasons are listed.
   - `NO_PARTICIPATION_EVIDENCE`: no game was found.
   - `SUPERSEDED`: not verified, and three or more of its starters are authoritative at one other team whose latest
     stage began later. This covers a rebrand, a new provider entity, or an event-only entity such as an Esports World
     Cup co-brand.
   - `STALE`: a verified team whose last game is more than 180 days old.
8. **Playable verified set** = `VERIFIED` and not stale. Only these teams enter the hybrid world.

A team whose five cannot be verified is excluded. No membership is invented, and no fictional player repairs it.

## 4. Coverage (FACT)

| PandaScore tier of the home league | Teams | Verified | Playable | Incomplete | Superseded | No evidence |
|---|---:|---:|---:|---:|---:|---:|
| a | 51 | 47 | 47 | 3 | 1 | 0 |
| b | 8 | 7 | 7 | 1 | 0 | 0 |
| c | 114 | 71 | 70 | 37 | 2 | 4 |
| d | 179 | 88 | 86 | 85 | 5 | 1 |
| s (event entities) | 3 | 0 | 0 | 0 | 2 | 1 |
| **Total** | **355** | **213** | **210** | **126** | **10** | **6** |

The home league is the league of most of the team's 2026 games outside tier-s and national-team events.

- **Verified:** 3 of the 213 are stale and excluded.
- **Playable teams:** 210, in 31 leagues; one league has a single playable team.
- **Players:** 1,050 real starters and 88 real substitutes in the pack; the experiment loads 85 of the 88, because 3
  substitutes have no role in any source.
- **Evidence:** 9 playable teams needed the previous stage. The median evidence window is 8 games (minimum 3). The last
  evidence game falls between 2026-05-22 and 2026-10-13.

Reasons for an incomplete team (a team can have several):

| Tier | Ownership conflict | Identity unresolved | Not registered | Thin participation |
|---|---:|---:|---:|---:|
| a | 3 | 0 | 0 | 0 |
| b | 0 | 1 | 0 | 0 |
| c | 18 | 21 | 3 | 5 |
| d | 38 | 52 | 12 | 4 |

Slot outcomes over all 355 teams: 1,432 verified, 162 ownership conflict, 96 identity unresolved, 30 no evidence,
27 not registered, 28 thin participation.

**Reading (INFERENCE).**
- **Tier-a coverage is high:** 47 of 51.
  - The three incomplete tier-a teams fail because a starter's latest appearance is at another team. Two examples are
    a main-team player who later played for the academy team, and a spring roster whose players moved.
  - That is the correct outcome: without an academy relation in the sources, the as-of lineup cannot be verified.
- **The lower tiers lose teams mostly to identity resolution.** Seats there are `REVIEW_REQUIRED` or `UNMATCHED`; better
  reconciliation, or the Oracle's Elixir file, would recover some.

**Squads of the playable teams:** 150 teams have 5 players, 37 have 6, 21 have 7, 1 has 9 and 1 has 10. None is over
16. PandaScore stage rosters seldom list substitutes, so real benches are thin. 7 starters played a different role
than the one they were registered for, and the participation role is used.

**Ratings of the real squads:**
- **Evidence class:** 986 measured, 160 limited and 11 prior-only. The 11 prior-only players are substitutes without games.
- **Starter confidence:** 910 HIGH, 119 MEDIUM, 36 LOW.
- **Mean starter Overall by tier:** a 80.6 (p10 69, p90 90), b 72.6, c 60.2, d 51.1.
- **Formula check:** recomputing each Overall with the production formula (`Ratings.InRole`) gives **0 mismatches** over
  1,135 loaded players. 6 starters whose verified role differs from their rating's primary role use the research
  rating of that role.
- **Assumed values:** 234 have no published birthdate and are given age 24; 244 have no research potential (age
  unknown, or 25 and over) and take potential = Overall. Both are marked as assumptions.

## 5. What is never invented

| Field | Treatment |
|---|---|
| Contracts and wages | Unknown. The experiment assumes terms by tier, marked `Assumed` |
| Personality (consistency … loyalty) | Unknown. A neutral 50 for every real player; never drawn at random |
| Development traits (peak age, volatility, trainability) | Neutral defaults (simulation assumption) |
| Coach quality, budgets, strategy | Simulation assumptions by tier |
| Academy affiliation | Unknown |
| Potential | The research estimate (age ≤ 25) or the Overall, labelled an estimate, not a fact |

## 6. Academy affiliation

- **FACT.** PandaScore has no academy field, and Leaguepedia team relations were not fetched.
- 26 team names match academy-like patterns ("Academy", "Challengers", "Youth" …), 15 of them playable. They are reported
  as INFERENCE candidates only and never become an affiliation. A real academy team is a separate real team with its
  own verified five.
- An affiliation needs a source: for example, the Leaguepedia `TeamRedirects`/organization relations. **OPEN QUESTION.**

## 7. In the hybrid world

- **The starting five never includes an NPC.** Real teams keep the rule through every simulated season (STRICT). A real
  team's lineup draws on its real players only.
- **NPC reserves.** A real team may employ NPC reserves only in development seats: 123 at creation, at most 2 per team,
  only when its real squad is under 7. They never start; a rotation appearance would count as taking a starting slot,
  so they play no matches.
- **Real players stay in real teams.** They move only between real teams: by transfer, or by signing as a real free
  agent after a simulated release. Fictional teams cannot sign them (`RealPlayersOnFictionalTeams = false`).
- **History is kept apart.**
  - Simulated moves are `Simulation`-era events.
  - The 6,610 source observations of the loaded players stay `Snapshot`-era.
  - The hash of the snapshot-era history was checked unchanged after every simulated season (test T04).

Results over 10 simulated seasons are in
[HYBRID_WORLD_INTEGRATION_REVIEW §4](HYBRID_WORLD_INTEGRATION_REVIEW.md#4-the-real-team-rule-over-time). **In short,
the strict rule is not sustainable:** no source can add real players, so real squads empty as players retire.

## 8. Tests (executed 2026-10-15)

`tests/test_real_rosters.py`, 8 synthetic cases, all pass:
- a five chosen by participation, with a higher-rated substitute left out;
- an unresolved identity, an unregistered starter, and thin participation each leave a team incomplete;
- an ownership conflict, and the superseded entity it reveals, with no player in two squads;
- national-team games never deciding a club;
- a stage after the as-of day ignored;
- the 16-player flag.

The full tool suite (113 tests) passes.

## 9. Open questions

- Should a team whose lineup is verified but older than 180 days (3 now) be playable with an "as of its last game" label?
- Should academy relations be fetched (Leaguepedia) so that academy players and main teams can share an organization?
- Which leagues form the playable real set: all 31, or tier a/b only (54 teams)?
- Should the identity review queue be worked through to recover lower-tier teams? 96 slots are blocked by unresolved
  identities.
