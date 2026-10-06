# Traceable trade-offs

| Display | Classification and source | Meaning / limit |
| --- | --- | --- |
| Scheduled payment amount/dates | Known; offer and Finance.AddSponsor staged items | Promised base receipts, not cash now |
| Win bonus rate and next-day timing | Known; offer and Finance.Consume | Requires an actual win while contract active |
| Total future bonus / wins | Unknown | No approved forecast; never show zero as an estimate |
| Delivery load and inclusive end day | Known; SponsorContract + Simulation.Load | Applies now and shares preparation capacity |
| Current load/capacity | Known; Simulation.Load / Company.Coach.Capacity | No new projected preparation score in UI |
| Preparation consequence | Known conditional rule; Simulation.Step Preparation | Overload reduces future conversion; existing completed work retained |
| Cash now | Known; Company.Cash | Acceptance itself changes no cash |
| Next seven-day committed cash forecast | Estimated; Finance.Forecast | Current signed items only; excludes unsigned offers and unearned wins |
| Existing agreements / next outstanding receipt | Known; Company.FinancialItems | Actual remaining amount and due day, no synthetic obligation task |
| Limited flexibility | Known; AcceptSponsor active-contract guard | One additional active slot in current fixture; no cancellation action |
| Deadline / minimum reputation | Known; World offer | Deadline can expire; rival may claim finite opportunity |

The comparison is Accept versus Leave available (no command), not an invented
reject/negotiation choice. Current two offers differ in income/load and compete
for the active slot. No green/red recommendation or preference score is calculated.

Causal trace: acceptance → Company.Sponsors + World claim + scheduled FinancialItems;
active sponsor → Simulation.Load → Preparation phase conversion → future Work →
Competition outcome; actual win → Finance.Consume → next-day bonus item → Finance.Settle.
Competition → Commercial.Consume/ApplyDue affects reputation/audience separately.
Signing does not itself grant reputation, audience, growth, synergy or performance.

No exact future probability, bonus revenue, opponent response, commercial value
metric, breach penalty, delivery checklist or reputation leverage is invented.
Generic unknown fields are not padded into the document as fake depth.
