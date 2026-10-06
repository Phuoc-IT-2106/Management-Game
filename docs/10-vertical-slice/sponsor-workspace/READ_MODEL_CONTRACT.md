# Live sponsor read contract

Application owns `SponsorSnapshot`, `SponsorTerms`, `SponsorAgreement` and
`SponsorPayment` in `src/ManagementGame.Application/SponsorWorkspace.cs`.
`Session.ObserveSponsors()` takes the same existing gate as Observe/Submit. The
new `ISponsorSession` capability exposes this read and the existing Submit method.
The existing IGameSession/Situation and internal sponsor tab remain compatible.

| Added structured field | Source / need |
| --- | --- |
| CampaignId, CompanyId, CompanyName, Revision | Execution and Company; actor/context identity and stale safety |
| Day, nullable NextCheckpoint | Calendar and next fixture; no fabricated date at completion |
| Cash, CommittedForecast, Load, Capacity, Reputation | Existing player observations, same authoritative calculations |
| OfferId, Name, Deadline, EndDay, Payment, WinBonus, Load, MinimumReputation | Visible World offer terms; end day newly exposed instead of UI literal |
| CanAccept, Availability | Authoritative pure AcceptSponsor transition, then public claim status; covers all current guards |
| ScheduledPayments: stable financial-item ID, DueDay, Amount, Remaining | Existing signed items, or pure transition's staged items for a valid offer |
| Agreements: ID, Name, EndDay, Load, WinBonus, Payments | Player-owned signed contracts and their base receivables |

No separate SponsorId exists. Do not manufacture a new authoritative counterparty
ID. OfferId routes acceptance; agreement ID and financial item ID route signed
evidence. Display strings and sorted positions are never command keys.

Eligibility/schedule preview calls the existing immutable `Simulation.Apply` on
the captured state with AcceptSponsor. Its result is discarded after projection;
no Session mutation, receipt, gameplay RNG or new calculation occurs. This bounded
choice avoids copying cadence/legality formulas into Application/UI. Review it if
offers grow beyond the current two-item development content. Query benchmarks
include its actual cost. Invalid offers retain known raw terms but no speculative
new schedule. Signed offers expose original stored items including settlements.

Actor boundary: player Company finances and visible offer terms only. No rival
strength, budget, need, true posture, hidden probability, variance or future
outcome is returned. Tests change private rival values and compare serialized
projections byte-for-byte. No Domain objects cross into UI contracts.

Forecast semantics remain existing Finance.Forecast, displayed **Estimated** with
the seven-day committed-only basis. Known rate is not promised bonus realization.
Future wins/bonus totals stay textual **Unknown**. No confidence percentage exists.
The snapshot does not include UI draft, route, focus or scroll; no save DTO/schema
or content identity change. Live identity is Company.Name with absent emblem and
neutral theme, as explicitly authorized for Stage 4.
