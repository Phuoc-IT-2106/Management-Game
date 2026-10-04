# Open questions and Director review

Status: OPEN QUESTION unless noted. None blocks completing this PLAN / SPEC
package; listed gates constrain later implementation or acceptance.

| ID | Question / recommendation | Owner | Required before |
| --- | --- | --- | --- |
| UX-Q01 | CLOSED 2026-10-05: Director accepted DEC-023 and Company-first presentation architecture; bounded Stage 2 UI Kit authorized | Director | Acceptance recorded; later screen and art approvals remain separate |
| UX-Q02 | Which initial operating-map form is clearest? Recommend small ownership map with contextual function links and list/matrix equivalent; avoid general graph engine | UX lead + Director | Map prototype blueprint approval |
| UX-Q03 | What company identity entry/edit experience is needed first? Recommend minimal initialization contract before full editor; define allowed lengths, Unicode policy, emblem types/size and missing-field behavior | Product + Technical Leads | Campaign identity implementation; player-facing demo |
| UX-Q04 | How should new identity fields affect existing experimental saves/content identity? Recommend explicit schema boundary or tested bounded conversion, never silently mutate old fixture hashes | Persistence lead + Director | Identity serialization change; DV-08 before distribution |
| UX-Q05 | What font family, final palette, token scale, density and motion values? No choice made; review small kit against accessibility and brand variants | UI lead + Director | UI Kit aesthetic approval |
| UX-Q06 | Which exact structured situation/terms fields are needed? Recommend minimum sponsor term timing, company context and stable matter IDs; preserve existing commands | Application + UI leads | First contextual workspace |
| UX-Q07 | What production discipline replaces the fictional development fixture? Unresolved; keep role count/format/terms inside discipline adapters | Director + competition design | Production discipline/content commitment |
| UX-Q08 | Which deadline events become mandatory stops? Preserve current Application checkpoints; classify UI matters without changing rules until separately specified | Systems design + Application lead | Any new interruption behavior |
| UX-Q09 | What future support is promised for text scale, assistive technology and localization? Test current keyboard/contrast/long-name needs now in later kit; do not claim native screen-reader support without evidence | UI lead + QA + Director | Accessibility support claims/UI quality gate |
| UX-Q10 | Who fills human golden-review ownership and what target hardware/workload is accepted? Roles assigned in strategy; actual reviewer and hardware contract still needed | Director + QA | Golden acceptance and DV-05 |
| UX-Q11 | Does the first sponsor workspace prove company-level decisions better than preparation? Recommend sponsor income/load/preparation trade-off; validate repeated use with Director | UX lead | First workspace selection |
| UX-Q12 | Which long-term strategic objectives, departments, budgets, growth/recovery mechanisms and historical retention are approved? No new mechanics specified; retain current limited commands | Director + domain owners | Any expansion beyond bounded slice |

FACT: Existing source-status discrepancies and current fixture-name noncompliance
are recorded in [source review](SOURCE_REVIEW.md) and [disposition](CURRENT_UI_DISPOSITION.md).
They are not hidden by the “ready for review” status. Current code is preserved
as engineering evidence, not certified as compliant production UX.

DECISION: Phase 4 stays closed under DEC-022. DV-01–DV-10 remain governed by the
closure register; this package closes none of them. Final insolvency thresholds,
campaign scale and production save compatibility remain separately unresolved.
