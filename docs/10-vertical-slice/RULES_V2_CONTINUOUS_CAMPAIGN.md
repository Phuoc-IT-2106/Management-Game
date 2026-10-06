# Rules v2 — continuous campaign, negotiated sponsorship, generated world

Status: **ACTIVE — explicit user instruction**, 2026-10-06 (Asia/Saigon). Replaces the
28-day bounded fixture rules (`slice-rules-v1`) with `slice-rules-v2`, content schema 2
and save schema 2. All numbers remain NONCANONICAL DEVELOPMENT FIXTURE values.
DEC-001–023 are unchanged; this expands the Vertical Slice's time and market model
at the user's direction and should be confirmed by a Director DEC entry.

## What changed

| Area | v1 (removed) | v2 |
|---|---|---|
| Time | 28 days, then `Finished` | Absolute days in consecutive seasons (`SeasonLength`); no end date |
| Schedule | Four authored fixtures | Each season meets every rival twice in a seeded order; second half carries double stakes; regenerated at rollover |
| Sponsors | Two authored offers with fixed deadlines + one fixed initial sponsor | **Market**: weekly, brands whose reputation/audience tiers the company meets may approach; terms derive from current standing. **Negotiation**: the player proposes payment and duration; after a delay the brand accepts, counters or refuses (with a cooling-off period). Unanswered inbound offers may go to a rival |
| Starting sponsor | Fixed brand | Seeded pick among low-tier qualifying brands, one season |
| People | Fixed six players, one named coach, one candidate | Generated from name pools per seed: roster, head coach, free agents and coach candidates; the player names the company |
| Rivals | Two authored organizations | `RivalCount` organizations drawn from a name pool per seed; strength and posture evolve each season |
| Contracts | All ended on day 28 | Staggered contract end days; renewal opens `RenewalWindow` days before the end at an asking wage; unrenewed players become free agents; an unrenewed coach is replaced by a weaker interim |
| Coaching | Fixed coach | Coach market; hiring pays fee + previous coach's exit cost |
| Wages | Pre-scheduled for every day to day 28 | Accrued daily for running contracts; forecast projects them |
| History | Unbounded within 28 days | Season rollover folds settled items into `LedgerBase` (cash reconciles as base + retained settlements) and keeps the last 150 review entries |
| Advance | Ran to the next match | Stops at a result, distress, any notice (offer, negotiation answer, contract end/renewal window, new season), match day without a plan, or after a quiet week |
| Missing role | Unreachable (fixed roster) | The match is forfeited with a fixed reputation/audience penalty; markets always stock missing roles |

Generation is order-independent: pools are sorted before seeded selection, so
reordering content never changes a campaign. Text stored in campaign state is
formatted with the invariant culture.

## Content (schema 2)

`content/fixture.json` holds `Balance`, attribute `Generation` ranges, `Names`
(given/family pools), `OrganizationNames` and a `Brands` pool (tier thresholds,
base payment, win bonus, delivery load, allowed durations). Validation requires
the season schedule to fit inside a season, at least one brand open to a new
company, and enough unique names for every rival.

## Verification (2026-10-06)

Executed locally on the pinned toolchain: Integration 190 (including a three-season
autonomous career with saves, schedule regeneration, contract lapses, bounded
history and ledger reconciliation; negotiation accept/refuse/cool-off; renewal;
coach hire; forfeit; seed variety; pool-order independence; tr-TR/vi-VN replay),
SponsorWorkspace 48, UiKit 140, OperatingMap 48, Domain 4, UI 3; native kit, map and
sponsor (31) checks; internal console smoke equals Headless hash; sponsor captures
7/7; COS-05 prototype verify at 1280×720 and 1920×1080. Headless `--seasons=6`
exploration ran six seasons without error.

## Known balance findings (not yet addressed)

The six-season exploration exposed loop problems that rules v2 makes visible:
cash accumulates with few sinks, a shrinking roster lowers wages faster than it
loses income, and reputation can rise despite losing records. See the gameplay
review delivered with this change for proposed mechanics.
