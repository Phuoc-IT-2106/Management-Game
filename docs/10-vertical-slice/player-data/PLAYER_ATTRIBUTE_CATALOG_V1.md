# Player Attribute Catalog v1

Status: PROPOSED — NOT APPROVED  
Version: v0.1  
Owner: Data & Simulation Intelligence  
Date: 2026-10-08

Dependencies: [authority/current state](README.md), [contract](PLAYER_DATA_CONTRACT_V1.md), [formula definitions W01–W09](PLAYER_RATING_FORMULAS_V1.md), [NPC model](NPC_GENERATION_V1.md), [acceptance plan](VALIDATION_CALIBRATION_V1.md).

## Selection and scope

**FACT:** current slice needs its existing Execution, Adaptability, Consistency and Readiness; it does not require any new attribute to continue working. **PROPOSAL:** four core candidates for the next player-data design, subject to Director acceptance of their consumers. Core means specified for evaluation, not approved for loading or simulation. Vision should be deferred too if independent information/preparation decisions fail validation. The other four candidates remain explicit deferred/null fields to record the design decision; no eight-number player card or universal overall score is proposed.

| Candidate | Disposition | Value and limiting evidence |
|---|---|---|
| Laning | Core candidate; top/mid/bot | Early resource advantage informs role/lineup investment; affected by jungle help, matchup and lane swaps. |
| Combat | Core candidate; all roles | Role-context contribution informs recruit/lineup choice; DPM/KP/deaths are proxies with correlated team effects. |
| Vision | Conditional core candidate; all roles | Information contribution could support a distinct roster/preparation trade-off; raw score misses timing/usefulness. |
| Consistency | Core candidate; all roles | Separate variance preference when choosing safe performance or upset potential. |
| Resource Efficiency | Deferred diagnostic | Damage/gold reuses Combat numerator; resource allocation and champion class confound efficiency. |
| Map Impact | Deferred | KP/vision/objective participation overlaps other attributes; credible attribution needs positional/event context. |
| Champion Pool | Deferred | Breadth is observable, mastery and draft value are not established; current resolver has no champion draft. |
| Adaptability | Deferred measurement | Patch/role/roster changes confound performance changes. Current legacy Adaptability remains a different authored construct. |

Additional candidates considered: Role Proficiency is useful for assignment but needs a role-switch/learning rule before becoming a numeric rating; use role eligibility and separate evidence coverage meanwhile. Experience is dated career context, not an unconditional bonus. Leadership/personality are excluded: public match metrics cannot establish them and no approved behavior system consumes them.

## Separate concepts

| Concept | Meaning and owner | Boundary |
|---|---|---|
| Persistent skill | Frozen initial capability, later typed People progression under current Company/World owner | Longer observation window; not a daily public-data refresh. |
| Dynamic form | Proposed short-term deviation from usual performance | Diagnostic only in v0.1; must not double-count persistent observations as another strength bonus. |
| Readiness | Current People condition affecting match availability/effectiveness | Existing runtime state, not a public performance metric. |
| Development projection / potential | Uncertain future trajectory or authored latent growth limits | Deferred mechanic; never an observed fact or guaranteed growth. |
| Reputation | Public perception/commercial context | Current Company reputation is not a new person skill score. Person reputation needs separate scope. |
| Scouting estimate | Actor's dated belief/range and evidence confidence | Distinct from true internal capability and from offline measurement adequacy. |
| Fictional traits | Optional authored narrative/behavioral properties | Empty in v0.1; cannot imply traits about real people or add unapproved mechanics. |

All future consumers below are **PROPOSAL**, not current system behavior. Salary, scouting, development and resolution must each receive a reviewed mapping; no attribute creates money or training effects merely because it exists. W references point to worked examples in the formulas document; test IDs refer to validation requirements, not executed tests.

## Laning

1. **Attribute ID:** laning.
2. **Display name:** Laning.
3. **Exact definition:** sustained, role-context standardized early resource advantage against the assigned opposing lane; not solo mechanics or isolated causal contribution.
4. **Gameplay decisions:** choose a lane specialist versus a cheaper/supportive profile; allocate bounded preparation toward protecting or exploiting lanes.
5. **Systems affected:** proposed roster/recruitment explanation and future early-advantage component; future targeted development only after its own rule approval.
6. **Role applicability:** top, mid, bot. jungle/support are not_applicable; secondary role has no automatic proficiency.
7. **Source signals:** gd15, xpd15, csd15 from exact paired snapshots and verified actual role.
8. **Transformation/formula:** .50 z_gd15 + .30 z_xpd15 + .20 z_csd15, missing-mask renormalization, series aggregation, one shrinkage, 50+15 adjusted_z.
9. **Normalization context:** role × competition_group × season × patch_era; reviewed pooling only; team/champion effects unresolved.
10. **Scale/bounds/units:** nullable integer 1–99 rating points; features use gold/XP/CS, not mixed raw sums.
11. **Sample-size requirements:** ≥3 admitted series, n_eff≥3, ≥50% game admission; ≥2 features with original weight coverage≥.50 each admitted game.
12. **Recency handling:** 365-day window, 180-day half-life; fresh initialization requires latest admitted row ≤180 days old.
13. **Missing-data behavior:** insufficient early snapshots → unknown; no CS/min substitution, no neutral-looking 50.
14. **Confidence methodology:** r×coverage×freshness×q_context; missing masks and assistance/context biases disclosed.
15. **Dynamic or persistent:** persistent initial capability estimate; not readiness or recent form.
16. **Update cadence:** offline versioned monthly candidate refresh or explicitly reviewed patch release; campaign value remains pinned.
17. **Risks/biases:** jungle assistance, lane swaps, matchups, assigned resource priority; role labels can be wrong.
18. **Worked numerical example:** W01 z=(1.2,1.2,1.2), n_eff=40 →62. W03 missing GD/XPD →unknown.
19. **Acceptance tests:** F04 missing early fields, F05 role exclusions, F06 context, F09 shrinkage, I03 gameplay ablation.
20. **Status:** PROPOSAL — core candidate, not approved.

## Combat

1. **Attribute ID:** combat.
2. **Display name:** Combat contribution.
3. **Exact definition:** persistent role-context contribution proxy from damage, participation and survival across games; not measured aim, mechanics, decision quality or engagement success.
4. **Gameplay decisions:** select damage-oriented versus participation-oriented roster fit; compare uncertain recruits with cost and readiness.
5. **Systems affected:** proposed match contribution and recruitment explanation; economy only through separately approved valuation and competition outcomes.
6. **Role applicability:** all five roles with different weights; no raw ADC/Support comparison.
7. **Source signals:** dpm, kp, death_rate. KDA and team wins are not additional features.
8. **Transformation/formula:** top/mid/bot .45/.35/.20; jungle .25/.55/.20; support 0/.75/.25 on z_dpm, z_kp and negatively oriented z_death_rate; then mean pipeline.
9. **Normalization context:** actual role/competition/season/patch cell; champion and team strength remain known confounders.
10. **Scale/bounds/units:** nullable integer 1–99; DPM damage/min, KP fraction, death_rate deaths/min.
11. **Sample-size requirements:** ≥3 admitted series and n_eff≥3, ≥50% games; two valid positive-weight signals and ≥.50 coverage. Support requires both signals.
12. **Recency handling:** persistent 365-day/180-day model; recent form is separate deferred diagnostic.
13. **Missing-data behavior:** zero team kills makes KP unavailable; renormalize only if gate passes. Support without KP remains unknown.
14. **Confidence methodology:** same adequacy formula; high coverage does not resolve engage/tank or team-context bias.
15. **Dynamic or persistent:** persistent capability proxy, separate mean from Consistency's variance.
16. **Update cadence:** new offline artifact at reviewed monthly/patch release; no live overwrite.
17. **Risks/biases:** damage farming, poke champions, passive play rewarded by low deaths, teammates creating participation opportunities.
18. **Worked numerical example:** synthetic observed_z=1.5, n_eff=40 → adjusted_z≈1 →65, confidence .5 in W01's complete context.
19. **Acceptance tests:** F01 zero denominators, F05 support weights, F07 clipping, F10 overlap, I03 dominance/ablation.
20. **Status:** PROPOSAL — core candidate, not approved.

## Vision

1. **Attribute ID:** vision.
2. **Display name:** Vision contribution.
3. **Exact definition:** role-context rate of recorded vision activity, a proxy for information contribution; not proof that wards were strategically useful.
4. **Gameplay decisions:** evaluate a roster's information specialist against direct lane/combat capability; prioritize information-oriented preparation if a bounded consumer is approved.
5. **Systems affected:** proposed information/preparation effectiveness and scouting explanation; never an automatic global strength bonus or improved knowledge of every market.
6. **Role applicability:** all roles, strictly separate benchmarks; Support naturally has different duties and raw scores.
7. **Source signals:** vpm from a documented vision_score. Ward counts remain supporting diagnostics.
8. **Transformation/formula:** standardized vpm as a single feature; series weighting and one shrinkage; mean mapping.
9. **Normalization context:** role/competition/season/patch and metric-definition version; no cross-provider score equivalence assumed.
10. **Scale/bounds/units:** nullable 1–99 rating; raw feature score/min.
11. **Sample-size requirements:** ≥3 series, n_eff≥3, ≥50% eligible games with a valid score and benchmark. The one-feature model is explicitly allowed only here.
12. **Recency handling:** 365 days, half-life180, same staleness gate.
13. **Missing-data behavior:** no vision_score →unknown; do not approximate with wards_placed or deaths.
14. **Confidence methodology:** common adequacy score; metric construct limitations remain even at high sample size.
15. **Dynamic or persistent:** persistent proxy; not the company's Information capability or scouting confidence.
16. **Update cadence:** offline versioned update, frozen campaign initialization.
17. **Risks/biases:** duration, support item/champion, team map control, score changes between patches, weak relation to valuable information.
18. **Worked numerical example:** observed_z=.6, n_eff=40 →56; W04 identical vpm gives top70/support40 under different synthetic benchmarks.
19. **Acceptance tests:** F04 no ward substitution, F05 role context, F06 metric version, I03 independent decision value.
20. **Status:** PROPOSAL — conditional core; defer if no distinct consumer is approved.

## Consistency

1. **Attribute ID:** consistency.
2. **Display name:** Competitive consistency.
3. **Exact definition:** inverse residual variation of role-context Combat series contribution relative to a frozen reference variance; not personality or work ethic.
4. **Gameplay decisions:** choose a lower-variance favorite versus a volatile underdog; assess predictable performance alongside expected capability.
5. **Systems affected:** proposed single resolver variance term and risk-aware recruitment; never simultaneously a mean-strength bonus.
6. **Role applicability:** all roles, matched composite/mask/reference variance; no global raw-variance comparison.
7. **Source signals:** multi-series Combat composites, their weights, valid masks and reference variance; raw win/loss streaks excluded.
8. **Transformation/formula:** unbiased weighted variance / v_ref; shrink variance toward1 with r; z=(1-v_adjusted)/.5 clipped ±3; map50+15z without second shrinkage.
9. **Normalization context:** role/context and common admissible feature mask; patch scope explicitly recorded.
10. **Scale/bounds/units:** nullable 1–99; variance in standardized-composite squared units before division by v_ref.
11. **Sample-size requirements:** ≥12 series and n_eff≥12, ≥50% game admission; supported variance reference; two patch eras or explicit single-era scope.
12. **Recency handling:** same 365-day/180-day weighting; do not label a temporary stable streak persistent certainty.
13. **Missing-data behavior:** no variance reference, changing non-reconcilable masks or too few series →unknown.
14. **Confidence methodology:** common adequacy score recalculated on retained variance sample, not copied from a larger Combat sample.
15. **Dynamic or persistent:** persistent variability estimate; mean ability, readiness and form separate.
16. **Update cadence:** versioned offline artifact; potential runtime progression remains unapproved.
17. **Risks/biases:** stable weak play can score high; team/champion changes masquerade as inconsistency; few series underestimate variation.
18. **Worked numerical example:** v=.64, n_eff=40 →v_adjusted=.76 →z=.48 →57. Low Combat is not raised by this result.
19. **Acceptance tests:** F03 minimum sample, F08 stable rounding, F09 no second shrinkage, F11 zero variance denominator, I03 upset/favorite behavior.
20. **Status:** PROPOSAL — core candidate; distinct measurement semantics from current authored Consistency, so migration still requires approval.

## Resource Efficiency

1. **Attribute ID:** resource_efficiency.
2. **Display name:** Resource efficiency.
3. **Exact definition:** desired impact per allocated resource; public damage/gold is only a narrow diagnostic, not the full construct.
4. **Gameplay decisions:** future low-resource specialist selection could create value if distinct from Combat.
5. **Systems affected:** no v0.1 consumer; possible future roster resource allocation, not a cash-efficiency modifier.
6. **Role applicability:** role/champion conditional; raw Support versus Bot comparison inappropriate.
7. **Source signals:** damage_per_gold, gold_share, damage_share; positional/objective context would be needed for broader impact.
8. **Transformation/formula:** diagnostic damage_to_champions/gold_total; **no released attribute estimator** because it overlaps DPM and lacks non-damage value.
9. **Normalization context:** exploratory role/champion/resource-budget strata; no fitted benchmark supplied.
10. **Scale/bounds/units:** diagnostic damage/gold ≥0; capability value=null, status=deferred.
11. **Sample-size requirements:** diagnostic valid per game; an exploratory study should use ≥30 series per role, which does not itself authorize rating release.
12. **Recency handling:** use the same frozen 365-day study window; no separate recency-based power.
13. **Missing-data behavior:** zero/missing gold or damage →unknown diagnostic; capability always deferred.
14. **Confidence methodology:** report coverage/sample/context; no numerical capability confidence while estimator is unidentified.
15. **Dynamic or persistent:** intended persistent specialization if validated; currently diagnostic only.
16. **Update cadence:** offline research artifact only.
17. **Risks/biases:** duplicates Combat, gold allocation endogeneity, tanks/support doing useful non-damage work, ratio instability.
18. **Worked numerical example:** 24000/15000=1.6 damage/gold; this does not produce a 1–99 rating or a bonus.
19. **Acceptance tests:** F01 zero gold, F10 overlapping features, S06 deferred/null, I03 ablation before promotion.
20. **Status:** PROPOSAL — DEFERRED.

## Map Impact

1. **Attribute ID:** map_impact.
2. **Display name:** Map impact.
3. **Exact definition:** desired contribution to useful pressure, rotations and objective opportunities relative to role; no adequate scalar identified here.
4. **Gameplay decisions:** potential choice of rotation specialist versus lane specialist, only if bounded strategic simulation supports it.
5. **Systems affected:** none in v0.1; future preparation/match matchup subject to Director scope.
6. **Role applicability:** role-dependent; Jungle relevance does not justify reusing a laner's GD15 formula.
7. **Source signals:** prospective position/time/event/objective involvement; KP and vision alone insufficient.
8. **Transformation/formula:** no estimator released. Candidate event contribution needs opportunity denominators and team attribution before even a prototype weight table.
9. **Normalization context:** future role, game phase, available objectives and pregame team context; unavailable now.
10. **Scale/bounds/units:** capability null/deferred; event diagnostics would retain counts/time, not invented rating units.
11. **Sample-size requirements:** no sample count cures missing event attribution; completeness study and independently labeled examples required first.
12. **Recency handling:** any future study must pin patch/window; no adaptation bonus inferred.
13. **Missing-data behavior:** public box score only →deferred, never zero impact.
14. **Confidence methodology:** none for capability until construct/estimator validated; event coverage may be reported separately.
15. **Dynamic or persistent:** intended persistent strategic contribution, not temporary team momentum.
16. **Update cadence:** research-only, no runtime update.
17. **Risks/biases:** duplicate KP/Vision reward, hindsight credit, teammates creating opportunities, misleading objective proximity.
18. **Worked numerical example:** KP=.7 plus vpm=1 does not identify rotations; map_impact remains null even with 40 series.
19. **Acceptance tests:** S06 deferred/null, F10 no composite reuse bonus, F12 no causal claim from box scores.
20. **Status:** PROPOSAL — DEFERRED.

## Champion Pool

1. **Attribute ID:** champion_pool.
2. **Display name:** Champion pool.
3. **Exact definition:** desired breadth of effective playable champions under draft constraints, distinct from mere number selected.
4. **Gameplay decisions:** future draft flexibility versus specialization; absent a draft consumer, it creates no current decision.
5. **Systems affected:** none in v0.1; no new draft system authorized.
6. **Role applicability:** all roles with role/champion eligibility; observed choices are team decisions too.
7. **Source signals:** champion identities, counts, contextual performance by champion and draft opportunities if available.
8. **Transformation/formula:** entropy/effective_champions diagnostics only; no mastery or capability estimator released.
9. **Normalization context:** role, patch availability, opportunity and pool size; identical entropy across eras need not mean equal value.
10. **Scale/bounds/units:** entropy nats, effective count1..m, normalized diversity0..1; capability null/deferred.
11. **Sample-size requirements:** diagnostics require ≥1 game; mastery would need adequate per-champion and opportunity samples, unresolved.
12. **Recency handling:** patch-specific diagnostic window within persistent dataset; never merge unavailable champions across eras without mapping.
13. **Missing-data behavior:** unknown champion prevents that row's diversity use; report missing fraction, no assumption of a new champion.
14. **Confidence methodology:** diagnostic coverage/sample only; broad observation is not confidence in mastery.
15. **Dynamic or persistent:** proposed persistent repertoire with patch relevance; neither implemented nor a personality trait.
16. **Update cadence:** offline diagnostic artifact; runtime draft updates deferred.
17. **Risks/biases:** bans/team preference, meta forcing narrow choices, diversity rewarding weak off-picks, rare champions' tiny samples.
18. **Worked numerical example:** ten games each on two champions →H=ln2, effective count2, normalized diversity1; capability still null.
19. **Acceptance tests:** F01 empty sample, F08 entropy ties/profile, S06 deferred/null, I03 require meaningful draft consumer.
20. **Status:** PROPOSAL — DEFERRED.

## Adaptability

1. **Attribute ID:** adaptability.
2. **Display name:** Adaptability (unmeasured candidate).
3. **Exact definition:** desired ability to retain/recover role performance after a changed context; this is not identified by simple patch score differences.
4. **Gameplay decisions:** potential investment in flexible recruits/preparation versus established specialists; legacy gameplay already has its own authored Adaptability.
5. **Systems affected:** no replacement in v0.1; existing adaptation resolver remains untouched until a separate semantic migration.
6. **Role applicability:** all roles; do not equate role change with patch change.
7. **Source signals:** longitudinal pre/post patch, role, roster and strategy observations; actual strategy information often unavailable.
8. **Transformation/formula:** diagnostic post-context standardized mean minus pre-context mean; no causal or capability mapping. Separate baselines required.
9. **Normalization context:** supported pre/post patch eras and role/competition cells; roster/champion confounding unresolved.
10. **Scale/bounds/units:** diagnostic standardized-z difference; capability null/deferred.
11. **Sample-size requirements:** exploratory ≥10 independent series on each side; multiple context changes needed to investigate generalization, not enough to declare identifiability.
12. **Recency handling:** event-aligned pre/post windows, separately versioned; no automatic recent-form or persistent-skill bonus.
13. **Missing-data behavior:** incomplete windows or unsupported patch →unknown diagnostic; never copy current Person.Adaptability as empirical fact.
14. **Confidence methodology:** no capability confidence; report confounders, coverage and uncertainty of diagnostic separately.
15. **Dynamic or persistent:** intended persistent response tendency, distinct from the temporary adaptation state and recent form.
16. **Update cadence:** research-only; legacy campaign values do not change.
17. **Risks/biases:** regression to mean, team transfers, different schedules, baseline shifts, post-hoc selection of favorable patches.
18. **Worked numerical example:** post mean=.6 and pre mean=1.2 gives -.6z diagnostic; W05 shows a baseline shift alone can cause this, so adaptability remains null.
19. **Acceptance tests:** F06 patch calibration, F12 identifiability, S06 deferred/null, I01 legacy semantics preserved.
20. **Status:** PROPOSAL — DEFERRED; existing authored field is not retrospectively invalidated or replaced.
