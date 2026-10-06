# Current sponsor mechanic audit

Historical inspection below dates to 2026-10-05. The formerly uncommitted gameplay
was later published as `0e9bbee0`, followed by Stage 4 in `7ad0501`. Stage 4.1
reinspected the current source and preserves all rules; see
[current acceptance](acceptance/README.md). Only the development company name changed.

Inspected 2026-10-05 (Asia/Saigon). Local HEAD and read-only remote main are
`a381d8f26b55132d697e9df9b93e97dcdfc474dc`. Production gameplay is present as
pre-existing uncommitted working files, as recorded by prior UX stages. This
audit concerns those C# files, not the Python prototype. Preserve that work.

## Authority and executable behavior

| Fact / rule | Production source |
| --- | --- |
| World offers have ID, name, deadline, end day, payment, win bonus, load, minimum reputation and claim owner | Domain/Model.cs: Offer |
| Company owns signed sponsor ID/name/end/load/bonus, receivables and settlements | Model.cs: Company, SponsorContract, FinancialItem |
| Sole player sponsor action is fixed-term acceptance | Application/Contracts.cs: SponsorDecision(OfferId); Session.ApplyDecision → AcceptSponsor |
| Unclaimed, deadline >= current day, end >= day + 3, adequate reputation, fewer than two active contracts, unfinished campaign | Simulation.Apply: AcceptSponsor and finished guard |
| Accept claims World offer, adds Company contract and receivables; Cash unchanged | Simulation.Apply; Finance.AddSponsor |
| Payment first due signing day + 3; repeats every 7 days through inclusive end day | Finance.AddSponsor |
| Delivery load applies immediately through inclusive end day | Simulation.Load |
| Preparation phase converts coach preparation × capacity / max(capacity, current load), existing rounding; no separate strength penalty | Simulation.Step: Preparation, Organization |
| Active sponsor win bonus creates next-day receivable only after an actual win | Finance.Consume; Finance.Settle |
| Competition emits outcomes; Finance and Commercial consume once using receipts | Simulation.Step: Competition; Finance/Competition.cs |
| Commercial applies result-based reputation/audience later; signing itself grants neither | Commercial.Consume / ApplyDue |
| Rivals can claim finite offers through their own budget/need at cadence | Simulation.Step: Rivals; hidden budgets/needs must not enter projection |

Current fixture day 1 load is 65/capacity 80. Larger offer adds 25; smaller adds
8. The larger crosses capacity, trading scheduled income against future preparation
conversion. Only one further active agreement fits. This is sufficient for the
first workspace: no stop condition #7/#8 is triggered. Values/names are development
content, not canonical balance. No new formula is needed in presentation.

## Observation and uncertainty

Situation already exposes actor-safe offer rows, current cash, seven-day committed
cash forecast, load/capacity, reputation, candidate/opponent estimates, bills and
string sponsor summaries. SponsorRow omits end day, payment dates and complete
eligibility (end date, contract count, campaign finished). The old Main confirmation
hardcodes day 28; the new projection must use authoritative terms instead.

Known: offered payment/bonus rate/load/end/deadline/eligibility, current company
resources, signed terms and actual financial items. Estimated: existing seven-day
committed cash forecast, explicitly excluding unearned future wins and unsigned
offers. Unknown: future wins/total bonuses, future rivalry/competitive response;
there is no approved probability or sponsor benefit forecast. Bonus rate is known,
bonus realization unknown. There is no separate sponsor entity ID: offer ID identifies
the counterparty's current offer; signed agreement ID is a distinct existing ID.

## Command and persistence boundaries

Request(Id, Revision, SponsorDecision(OfferId)) → Session gate → receipt lookup →
revision check → staged Domain transition → committed receipt/revision → observation.
Exact request retries return the existing receipt before revision validation;
conflicting ID reuse and stale revisions reject without mutation. Rule violations
return their existing message. No typed error enum exists; do not invent one.
New request IDs may be random presentation identifiers; they consume no gameplay RNG.

Save schema 1 already stores contracts, claims, financial items, settlements and
receipts. SnapshotStore validates exact rules/content identity and canonical hash.
Projection and navigation additions need no persistence changes. Company.Name is
the live identity; absent abbreviation/emblem/colors remain absent.

## Unsupported / deferred

No negotiation, counteroffer, renewal, termination, exclusivity, clauses, relationship
meter, marketing, new commercial value scalar, direct signing reputation/audience
gain, sponsor-specific delivery tasks/penalties or exact future preparation/win
forecast. Load is an ongoing obligation, not a task with an invented due date.
Contract signing day is not stored explicitly; after signing do not invent it.
Offer deadline, contract end, receivable due days and competition checkpoint are
real dates. No general Affairs framework is needed: link outstanding sponsor
receivables to their existing agreement cause IDs.
