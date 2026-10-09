# Rules v3, wave 1: incentives and measurement

Status: **ACTIVE — explicit user instruction**, 2026-10-08 (Asia/Saigon). This is wave 1 of the user's
[anti-boredom Rules v3 proposal](https://claude.ai/artifact/2NFWaMQeFuQSEx9XeBoEeH) (2026-10-06).
- It moves `slice-rules-v2` to `slice-rules-v3`, content schema 3 and save schema 3.
- All numbers remain NONCANONICAL DEVELOPMENT FIXTURE values.
- Amounts here are in CU, as measured. Since the USD relabel (2026-10-08, [genre-shell README](genre-shell/README.md)),
  the game shows the same values in dollars: 1 CU = $1.
- DEC-001–023 are unchanged. Like [rules v2](RULES_V2_CONTINUOUS_CAMPAIGN.md), this scope expansion should be confirmed
  by a Director DEC entry.

Wave 1 targets the proposal's first problem: under rules v2 losing was profitable, so nothing pushed the player to win.
It also fixes reputation, which did not follow results. Waves 2–4 (league table, board objectives, player lifecycle,
facilities, active rivals, events, multi-term negotiation, Bo3) are not started.

## Result

Gate 1 passes. Measured with four owner policies × 8 seeds × 5 seasons (`build/Measure-Rules.ps1`):

| Gate 1 metric | Threshold | Rules v2 | Rules v3 wave 1 |
|---|---|---|---|
| Losing is not profitable: season-5 cash, balanced owner vs thrifty owner | balanced ≥ thrift on ≥ 6/8 seeds | **3/8** (fail) | **8/8** (pass) |
| Reputation follows results: correlation of season win rate with season reputation change | ≥ 0.6 | **0.522** (fail) | **0.807** (pass) |

| Owner policy (8 seeds) | v2 win rate | v2 season-5 cash | v3 win rate | v3 season-5 cash | v3 insolvent runs |
|---|---|---|---|---|---|
| Coach: full delegation | 23.8% | 11,074 CU | 55.6% | 9,221 CU | 0 |
| Sponsor-greedy: every offer, roster drifts to 5 | 24.1% | 9,274 CU | 54.1% | 5,288 CU | 0 |
| Thrift: thin roster, never renews | 20.9% | **16,095 CU** | 40.8% | 3,310 CU | 1 (seed 7, day 216) |
| Balanced: author's policy | 46.6% | 15,398 CU | 62.8% | **10,957 CU** | 0 |

Under v2 the thrifty owner, who won least, ended richest and with the highest reputation (55.4). Under v3 it ends
poorest, with the lowest reputation (42.0).

Win rates rose mostly because of the predictability fix below, not because of the incentives.

## What changed

| Area | Rules v2 | Rules v3 wave 1 |
|---|---|---|
| Running cost | Costs were almost only wages, so a smaller roster was always cheaper | A fixed `OperatingCost` per day (office, league registration) falls due daily whatever the headcount. The 7-day forecast and the Portal money snapshot include it |
| Roster minimum | None; a roster of five cost least | The league registers at least `MinimumRoster` (6) players. Below it, every match (won, lost or forfeited) earns no prize and owes `ShortfallFinePercent` (50%) of its prize as a fine, due next day |
| Sponsor termination | None | Each agreement carries a termination clause (`SponsorContract.ForfeitLimit`, from `SponsorForfeitLimit` = 2). The forfeit that reaches it ends the agreement that day, cancels its unpaid future instalments and cools the brand off for `RejectCooldown` days |
| Reputation | Each match moved reputation by surprise. An underdog's upset win gained far more than its expected loss cost, so a losing team drifted up toward 50 | At season end, reputation moves `ReputationRegression` (25%) of the way toward the level the record earns: `ReputationTargetLow` (15) at no wins to `ReputationTargetHigh` (85) at all wins. The season notice states the shift and the target. Per-match changes are unchanged |
| Predictability (bug fix) | A rival penalised a repeated posture by counting **every** result since the campaign began. Across continuous seasons the penalty grew without bound: a coach that always played Balanced fell to the 5% probability floor by season 3–4 while rivals got weaker | A rival reads the company's last `PredictabilityWindow` (3) results, the most v1's four-match season ever allowed |

The predictability fix is a continuity bug of rules v2, found by the measurement. It changes no other competition rule.
Measured on the same harness and economy without it, gate 1 also passes (8/8, r = 0.736). But the sponsor-greedy owner
then goes insolvent on 3 of 8 seeds, because its delegated match plans collapse in later seasons
(`evidence/measure-v3-wave1-before-predictability-fix.*`).

### Values (content schema 3)

`OperatingCost` 2500 (25 CU/day), `MinimumRoster` 6, `ShortfallFinePercent` 50, `SponsorForfeitLimit` 2,
`ReputationRegression` 25, `ReputationTargetLow` 15, `ReputationTargetHigh` 85, `PredictabilityWindow` 3.

The running cost and fine were chosen from a sweep of variants (`artifacts/rules-v3/variants`):

| Variant | Effect |
|---|---|
| 30 CU/day, or a fine of the whole prize | The thrifty owner goes insolvent on 4–6 of 8 seeds |
| 20 CU/day | The thrifty owner still gains cash |
| **25 CU/day, 50% fine (chosen)** | The thrifty owner roughly breaks even or loses money, with one late insolvency. No other policy goes insolvent. A careful first season still ends at 2,417–4,316 CU (from 2,600), so season 1 is not too harsh |

### Persistence and validation

- Saves move to schema 3, and v2 saves are rejected by design (content identity is exact; no migration, DV-08).
- `ContentLoader` requires:
  - `MinimumRoster` from 5 up to `RosterSize`, so a new company meets it;
  - non-negative costs;
  - a fine of 0–300%;
  - a target range inside 0–100;
  - a predictability window of 0–16.
- `SnapshotValidation` requires a fine to come from a recorded result and a running-cost item from a past day.

### Player-facing

- **Portal.** A roster below the minimum is a blocking Squad decision (`DecisionKind.RosterShortfall`, roster-move art,
  "Sign a player"). It does not stop Continue.
- **Finance.**
  - The Payroll card becomes Running costs: wages, operations, and the registered roster against the minimum.
  - The 14-day outlook projects running costs.
  - Its day column keeps whole labels, which were truncated before.
- **Inbox.** A league fine is a `League` message routed to Squad. A sponsor termination is a `Commercial` message about
  its brand.
- **Signing a sponsor.** The decision record states the termination clause.

## Measurement

`tools/Headless --measure` (`Measurement.cs`) plays 8 seeds (20261004, 1–7) for 5 seasons under four owner policies.

**Policies.** Every policy uses only observations and typed decisions, as a player does:
- **coach**: full delegation;
- **sponsor-greedy**: accepts every offer and asks at the top of each brand's guide;
- **thrift**: never renews, releases a second player in a role when the exit cost is below the wages saved, and covers
  missing roles with the cheapest player;
- **balanced**: the author's policy. It:
  - commits plans early with the posture matching the meta;
  - keeps six players;
  - renews good players;
  - takes sponsors within the coach's load.

**Report.** The JSON report holds per-season rows and every metric of the proposal's measurement table:
- **Season rows:** record, cash, reputation, roster, sponsors, negative-forecast weeks, decisions, prompts, longest quiet
  stretch, and match diagnostics (mean win probability, base strength, completed preparation, implied rival strength).
- **Insolvency:** a run that cannot advance (distress, no cash, no inflow) is reported as insolvent with its last cash.

**Gate.** `build/Measure-Rules.ps1 -Gate 1` fails unless every gate-1 metric passes.

**Harness fix.** An early harness committed manual plans only on match day. Preparation accumulates only while a plan is
committed, so manual policies played with no completed work. The final harness commits as soon as the fixture is known.
The v2 baseline was re-run with the final harness on a `HEAD` worktree.

| Metric | Gate | Rules v2 | Rules v3 wave 1 | Note |
|---|---|---|---|---|
| Losing is not profitable | 1 | 3/8 | 8/8 | Pass |
| Reputation follows results | 1 | 0.522 | 0.807 | Pass |
| Empty stretches (≤ 10 days) | 2 | 13 days | 13 days | Needs wave 2 (off-season, playoff) |
| Multi-year progress | 2 | — | — | Needs a league table (wave 2) |
| Decision density (≥ 2 per 7 days) | 3 | 1.82 | 1.80 | Needs waves 2–4 |
| No solved preparation | 3 | — | — | Needs weekly focus pairs (wave 3) |
| Player value (+5 to +15 points) | reference | 22.8 | 7.2 | In range once delegated plans stop collapsing |
| Financial pressure (20–60% of seasons) | reference | 0% | 0.6% | Good play still accumulates cash; sinks arrive with facilities (wave 3) |
| Outcome diversity | reference | — | — | Needs a league table (wave 2) |
| No solved negotiation | reference | — | — | Wave 4 |

## Decisions recorded for later waves

The user answered two of the proposal's open questions on 2026-10-08. They are recorded here for wave 2; nothing in
wave 1 implements them.
- **Long-term failure.** Board confidence too low for two seasons forces restructuring (DEC-016). It does not end the
  campaign.
- **Season and league structure.** Longer seasons with 6–8 rivals, rather than keeping 56 days with 4 rivals.

## Known findings, not addressed in wave 1

- **Insolvency is a dead end.**
  - A company in distress with no cash and no incoming payment within three days cannot advance. Releasing a player
    needs cash, and the weekly market does not run while time is stopped.
  - This existed in v2 but was unreachable. With running costs and fines it is reachable: the thrifty owner reached it
    once in 32 runs.
  - DEC-016's restructuring path (now the user's chosen long-term failure) is the place to resolve it.
- **Readiness drains.** Readiness falls 8 per match and recovers 1 per day, a net loss of about 8 a season. The base
  strength of a thin or rarely-rotated squad falls over seasons. The proposal replaces this tax in wave 3 (rotation)
  and wave 2 (off-season rest).
- **Delegation is fixed.** The coach always recommends 50/30/20 or 25/25/50 at its ceiling posture (problem 4). Wave 3
  replaces the percentages with weekly focus pairs.

## Verification (2026-10-08, local, Debug)

- `Verify.ps1 -Configuration Debug`:
  - Domain 4, Integration 243, UI 3, UiKit 216, OperatingMap 48, SponsorWorkspace 48.
  - Integration adds 18 checks:
    - the running cost and its forecast;
    - the roster minimum task, fine, withheld prize and notice;
    - a full roster's prize;
    - the termination clause;
    - the reputation target and season-end pull;
    - the predictability window;
    - content bounds;
    - schema-2 save rejection.
  - Headless two-cycle hash: `fd905e55…` (rules changed; v2 was `fcf6fc3e…`).
- `Verify-Shell`: 2779 checks at 1280×720 and 2808 at 1920×1080. New: a roster-shortfall Portal case leads the
  decisions as a blocking roster move.
- Native: UI kit 24, operating map 101, sponsor 31.
- `Measure-Rules.ps1 -Gate 1`: pass.

**Evidence** ([rules-v3/evidence](rules-v3/evidence/)):
- `measure-v2-baseline.*` and `measure-v3-wave1.*`;
- `measure-v3-wave1-before-predictability-fix.*`;
- `portal-case-roster-shortfall-*.png`;
- `finance-*.png`, with `before-finance-1280x720.png`.
