# Player Data Foundation v1 — Independent Systems Review

Status: REVIEW COMPLETE — NEEDS REVISION  
Version: v0.1  
Owner: Independent Game Systems Reviewer + Simulation Balance Reviewer  
Date: 2026-10-08  
Scope: SPEC REVIEW ONLY; no specification approval or BUILD authorization

## Executive assessment

**Reviewer conclusion: NEEDS REVISION.** The package establishes useful identity, provenance, ownership and campaign-version boundaries. Its worked examples mostly demonstrate the stated arithmetic correctly. It does **not** yet establish a sufficient five-role gameplay model, a common real/NPC capability scale, or meaningful consumers for all four core candidates. Calibrating coefficients now as if those choices were settled would calibrate unresolved constructs.

The strongest findings are reproducible without an external player dataset:

- The measured Consistency formula cannot exceed 80, and its maximum at 40 effective series is 70. A valid proposed NPC can have Consistency 99. Shared field names and numeric bounds therefore do not establish shared semantics.
- Laner NPCs have raw Laning minus Combat restricted to {-2, 0, 2}; their theoretical raw correlation is approximately 0.9921. This undermines the claimed choice between lane and combat specialists.
- Removing an unfavorable but valid Combat feature can raise the example rating from 62 to 70. Confidence decreases, but the capability consumed by simulation would still increase.
- Existing Consistency changes a symmetric perturbation of win probability. Away from the probability clamps, it does not change the mean win probability. The proposed favorite/underdog choice needs an explicit consumer contract and evidence.
- The integration requirement to preserve readiness across hiring conflicts with the current signing rule, which caps incoming readiness at 70. The proposed initial-cohort generator also does not specify how to retain the current recurring recruitment market.

Disposition: **0 Critical findings, 10 Major findings/approval blockers, 4 Minor findings.** Critical means a demonstrated authorization, ownership, save-integrity or comparable fundamental violation; none is authorized by these specification files. Major means a construct, behavior or acceptance gap that must be resolved before its dependent calibration/integration step. Minor means a bounded clarification or reproducibility issue. A known, explicitly disclosed blocker is distinguished from a newly demonstrated defect below. These counts are not an assessment of unrelated working-tree changes.

**PROPOSAL:** retain the ownership/provenance framework as the basis for revision; settle gameplay consumers, shared scale and observation model before tuning balance. Source-coverage investigation and further synthetic counterexamples can proceed within their authorized scope. This review does not block all project work, promote LoL to production canon, or approve a replacement formula.

## Evidence, authority and review method

**FACT — inspected baseline:** HEAD 14b9f48134264d8c04f3dfb268b93ee295b825f8 on stage5-sponsor-instruments, with pre-existing working changes. All six original Markdown files and both JSON examples were reread from the current worktree:

| Source | Review responsibility |
|---|---|
| [README](../../../10-vertical-slice/player-data/README.md) | Authority, source claims, exact PD-D01–D05 requests and blockers |
| [Data contract](PLAYER_DATA_CONTRACT_V1.md) | IDs, evidence, ownership, runtime and unknown values |
| [Attribute catalog](PLAYER_ATTRIBUTE_CATALOG_V1.md) | All eight candidates and their twenty-field definitions |
| [Formulas](PLAYER_RATING_FORMULAS_V1.md) | Every feature, normalization, aggregation, uncertainty and worked case |
| [NPC generation](NPC_GENERATION_V1.md) | Distribution, covariance, budget, identity, lifecycle and knowledge |
| [Validation/calibration](VALIDATION_CALIBRATION_V1.md) | Planned acceptance criteria, compatibility and prior verification claims |
| [Real-path JSON](examples/real_player.example.json) | Synthetic row and independent forty-series summary; no actual person rated |
| [NPC JSON](examples/npc_player.example.json) | Hand-worked latent example, explicitly not a seeded golden output |

**DECISION — inherited:** [Decision Log](../../../01-governance/DECISION_LOG.md) DEC-010/012 preserve ownership and derived-state discipline; DEC-014/018 preserve actor-safe delegation/information; DEC-017 preserves outcome boundaries; DEC-019 bounds competitive resolution; DEC-022 accepts the technical foundation with deferrals; DEC-023 preserves existing gameplay unless separately superseded. [Project State](../../../00-project/PROJECT_STATE.md) and [Game Vision](../../../00-project/GAME_VISION.md) retain company-first management, one initial discipline and meaningful trade-offs. None accepts this four-attribute model or LoL as production content.

Related sources checked: [content foundation](../../../09-technical-foundation/DATA_DRIVEN_CONTENT.md), [ownership foundation](../../../09-technical-foundation/STATE_OWNERSHIP_SIMULATION_BOUNDARY.md), [persistence foundation](../../../09-technical-foundation/PERSISTENCE_SAVE_LOAD.md), [specialist competition](../../../07-specialist-specs/ESPORTS_COMPETITION_SPEC.md), [economy](../../../07-specialist-specs/SIMULATION_ECONOMY_SPEC.md), [Director integration](../../../07-specialist-specs/DIRECTOR_INTEGRATION.md), [continuous-campaign rules v2](../../../10-vertical-slice/RULES_V2_CONTINUOUS_CAMPAIGN.md) and the current [rules v3, wave 1](../../../10-vertical-slice/RULES_V3_INCENTIVES.md). Historical specialist proposals and older schema descriptions do not override later authority/current schema-3 behavior.

**FACT — concurrent-work reconciliation:** during this review, the worktree advanced from rules v2/schema 2 to rules v3/schema 3 while HEAD remained unchanged. The affected Domain, Application, content-loader and persistence paths were inspected again; the compatibility assessment below uses v3. Its active-scope document attributes wave 1 to explicit user instruction and calls for later Director DEC confirmation; this review does not supply that confirmation. The v3 document reports an incentives measurement gate, but this reviewer did not rerun it and does not treat it as player-data calibration evidence. Waves 2–4 are not current behavior. Seven original player-data files retained their initial hashes; their README received a concurrent content-loader row update from 2/v2 to 3/v3, while other compatibility claims still describe v2. This review records that drift as R-m04 and does not edit those originals.

**FACT — external context only:** Riot's [official game introduction](https://www.leagueoflegends.com/en-us/how-to-play/) describes five positions and the importance of objectives/resources. The authors' [PandaSkill paper, sections III–IV](https://arxiv.org/html/2501.10049v2) describes differing role responsibilities, role-specific models and contextual rating pools. These primary sources were consulted on 2026-10-08. They support questioning role coverage and cross-context comparability; they do not validate this package's coefficients, provide a commercial data license or prove a game design. No live data API was called.

Labels: **FACT** means repository evidence, a directly checked calculation or a cited source; **DECISION** means inherited accepted authority only; **ASSUMPTION** identifies a hypothetical counterexample's conditions; **PROPOSAL** identifies a reviewer recommendation; **OPEN QUESTION** identifies a choice still requiring evidence or Director disposition. Review judgments are recommendations, not new accepted decisions. The previous document-check PASS was inspected as narrow evidence, not inherited as a systems-approval result.

## Attribute coverage matrix for five roles

**Review judgment:** four labels are not four independent gameplay dimensions. Laning is unavailable by design for two roles; Consistency is variability of Combat, not an additional kind of contribution. Even the three laner roles lack a defined runtime use for the split between early advantage, contribution and vision. A medium-depth management game may abstract omitted actions, but must name the supported decisions and demonstrate that its abstraction distinguishes the relevant roster choices.

| Role | Laning | Combat | Vision | Consistency | Material coverage gap / verdict |
|---|---|---|---|---|---|
| Top | GD/XPD/CSD15; plausible early proxy, assisted and matchup-dependent | DPM-heavy composite can favor damage profiles | Activity score, not information usefulness | Combat variation, format/mask-sensitive | Partial: frontline/utility and pressure specialists can be undervalued. No proven consumer for a lane-versus-team contribution choice. |
| Jungle | Intentionally not_applicable; appropriate not to reuse solo-lane formula | .25 DPM + .55 KP + .20 oriented death rate | Role-specific VPM | Variation of that same composite | Insufficiently specified: pressure, path/opportunity choices and objective contributions have no identified representation after Map Impact is deferred. KP is not a substitute for all of them. |
| Mid | Same early proxy as Top, separate role benchmark | .45/.35/.20; damage/participation proxy | Role-specific VPM | Combat variation | Partial: lane advantage versus movement/team utility is not distinguished reliably. Champion/context differences remain. |
| Bot/ADC | Measures resources partly enabled by Support/Jungle | Damage-heavy model most directly fits one important duty | Role-specific VPM | Combat variation | Best observed fit of the five, still not sufficient evidence: resource allocation can look like individual ability; damage output does not establish efficient or useful output. |
| Support | Intentionally not_applicable; does not mean Support has no lane contribution | .75 KP + .25 oriented death rate; no damage term | One activity-score feature | Variability of a narrow two-feature composite | Highest concern: protection, useful control, engagement and enabling teammates are not separately identified. A low-death/KP profile can look strong while missing these contributions. |

Role descriptions above are review abstractions, not a demand to implement action-level telemetry or a full tactical simulator. **PROPOSAL:** require scenario pairs covering damage versus utility Top, pressure versus farming Jungle, lane versus movement Mid, resource-heavy versus economical Bot, and engage versus protection Support. An approved model must explain which decisions it supports, which it deliberately abstracts and why each tested profile can be useful. Do not auto-promote Map Impact or add attractive but unidentified statistics to fill the matrix.

### Consequences of deferring the other four candidates

| Candidate | Review of deferral | Consequence / condition |
|---|---|---|
| Resource Efficiency | Reasonable: damage/gold overlaps Combat and misses non-damage value | Keep allocation/efficiency diagnostics for bias analysis. Do not describe high DPM as efficient use of resources or claim low-resource specialists are represented without testing. |
| Map Impact | Measurement deferral is justified | Leaves the largest coverage gap for Jungle and important gaps for Support/roaming/pressure profiles. Director must accept a narrower abstraction or commission a bounded, identifiable contribution design. A new scalar is not automatically the solution. |
| Champion Pool | Reasonable within current no-draft scope | No claims of draft flexibility, matchup mastery or specialist champion value. Entropy measures observed selection diversity; it cannot justify a flexibility bonus. |
| Adaptability | Reasonable to defer public-data estimation | Existing authored Person.Adaptability still feeds accepted adaptation behavior. Null measurement cannot mean removing, zeroing or deriving that legacy input from Consistency. A bridge/design decision is mandatory before integration. |

## Gameplay consumer mapping

Evidence: [catalog](PLAYER_ATTRIBUTE_CATALOG_V1.md), [current Competition resolver](../../../../src/ManagementGame.Domain/Competition.cs), [observations and CoachPolicy](../../../../src/ManagementGame.Application/Observations.cs).

| Attribute | Proposed decision / consumer | Current implemented consumer | Review disposition |
|---|---|---|---|
| Laning | Recruit/lineup specialization and an early-advantage contribution | None; base capability uses Person.Execution | Experimental candidate until the contribution, aggregation, trade-off and explanation are specified. Naming a recruitment decision does not define its utility. |
| Combat | Mean competitive contribution and recruit comparisons | No field mapping; existing Execution is not this measured proxy | Define the target capability and relationship to current Execution before calibration against match/economy outcomes. |
| Vision | Information-oriented lineup/preparation | None; Company.Information is a different owned capability | Conditional candidate only. Require an independent decision effect and opportunity cost; otherwise retain a diagnostic, not a gameplay rating. Do not turn ward activity into universal scouting accuracy. |
| Consistency | Favorite/underdog risk preference; one variance term | Existing authored Consistency changes probability-noise spread | Plausible intent, unproven semantic bridge. Reconcile the measured construct, NPC scale, averaging horizon and actual outcome sensitivity. |
| Resource Efficiency | Future allocation specialization | None | Deferred; no bonus. |
| Map Impact | Future pressure/rotation strategy | None | Deferred, with role-coverage debt explicitly accepted or resolved. |
| Champion Pool | Future draft flexibility | None | Deferred; no draft scope added. |
| Adaptability | Future measured response tendency | Existing authored Adaptability, a distinct construct | Preserve current behavior until a separately reviewed mapping or rules change. |

**PROPOSAL — consumer acceptance contract:** for every retained gameplay attribute specify (1) the player/staff decision, (2) the exact downstream term and owner, (3) its role/lineup aggregation and scale, (4) the cost or sacrificed alternative, (5) an actor-safe explanation, and (6) an ablation case that changes a rational choice. A data field may remain useful provenance or a diagnostic without qualifying as a gameplay attribute. No current consumer mapping is installed by this review.

## Formula review

### Feature-level assessment

The definitions are generally explicit about units and zero denominators. This is a strength. The main risks lie in interpretation, aggregation and admission bias rather than basic division arithmetic. See [feature definitions](PLAYER_RATING_FORMULAS_V1.md#feature-definitions).

| Feature(s) | Mathematical/measurement review | Required caution |
|---|---|---|
| KDA | (K+A)/max(1,D) is a defined convention; diagnostic-only status is appropriate | Deathless value is not an infinity or a measure of game sense; do not add it alongside its component features as another bonus. |
| Kill Participation | Fraction and zero-opportunity handling are internally coherent | Conditional on team's kills; does not credit missed opportunities or create causal attribution. Zero-kill games can be disproportionately excluded, especially for Support. |
| CS/min, Gold/min | Clear duration and total-CS/gold definitions | Shares the duration denominator with other rate features; role/resource allocation and game length remain confounders. Gold semantic mapping must be verified per provider. |
| Damage/min | Clear champion-only numerator | Poke/assigned resources/game state can alter output independently of useful contribution; DPM is not mechanics. |
| Damage Share, Gold Share | Same-game totals, zero-total and [0,1] bounds specified | Compositional: one teammate's increase changes everyone else's share; not independent evidence. |
| Damage per Gold | Denominator/zero behavior defined | Reuses Combat damage; ratio may reward a narrow damage style and omit utility. Deferral is appropriate. |
| Death Rate | Lower-oriented deaths/min is defined | A monotonic survival reward can penalize useful sacrifice/engagement; zero deaths is observed zero, not necessarily optimal play. |
| GD15, XPD15, CSD15 | Exact paired snapshot differences have clear units | Highly dependent opportunity/resource signals. Assistance, matchup and lane-swap exclusions affect selection. Do not treat three correlated metrics as three independent observations. |
| Vision/min | Score/min is distinct from wards/min | Quantity does not establish strategic information value. No substitute metric or universal provider equivalence is justified. |
| Champion entropy/effective count/normalized diversity | Formula and one-champion convention are coherent | Sample-dependent choice breadth, not mastery. Both two and ten equally used champions have normalized diversity1; effective count must remain alongside it if breadth is discussed. |

### Normalization, aggregation, shrinkage and confidence

| Component | What is sound | What remains unresolved |
|---|---|---|
| Role-specific feature z-scores | Prevents raw ADC/Support comparisons; frozen reference cells and zero-sigma rejection are appropriate | The weighted composite is not itself unit-variance z. Its variance is w-transpose × covariance × w and varies by role, mask and patch. A common scale15/K20 does not establish comparable attribute spread. |
| Percentiles | Tie-aware empirical formula is valid as an explanation view | Does not repair comparability between disconnected tiers or different constructs. Do not use it as an additional power adjustment. |
| Benchmark cells | Explicit minimums, prior cutoffs and declared pooling prevent silent fallback | 20 players per role-cell and 20 variance-reference players with ≥12 effective series each may exclude much of a narrow cohort. Actual feasibility has not been audited. |
| Tier/team/champion context | Package correctly disclaims causal deconfounding and global equality | A player spanning multiple cells needs a precise selection/aggregation rule and attribute-specific offset semantics. One post-aggregation tier_offset_z is insufficiently specified for mixed-tier histories. |
| Series aggregation | Reduces one source of game-level pseudoreplication | Series are not automatically independent across opponents, team eras and schedules. Series means have different sampling variance when they average different game counts. |
| Shrinkage | One mean shrinkage and one separate variance shrinkage avoid a simple double application of r | K20 is an unvalidated regularizer, not a demonstrated Bayesian posterior. Same K for different features/roles/mean and variance targets needs evidence. Measured posterior means and authored NPC capabilities remain different acquisition processes. |
| Recency | Explicit cutoff, age buckets, window and frozen decay table are reproducible design choices | Equal aging leaves Kish n_eff/r unchanged. Freshness only changes confidence until the latest-row staleness gate. A recent row can keep an otherwise old pool admissible; test this behavior rather than equating r with freshness. |
| Missingness | Unknown is not zero; masks, minimum coverage and not_applicable are explicit | Renormalization changes the construct and can raise a player's power when an unfavorable metric disappears. The 50% admission rule is not a missing-at-random guarantee. |
| Outliers | Quarantining impossible records and retaining clipped-row lineage are appropriate | Clipped features can still distort fitted mean/sigma because benchmark fitting uses raw eligible rows. More importantly, clipping before measuring variance can make strong/extreme output look artificially consistent. |
| Consistency | Weighted sample-variance expression is recognizable under fixed independent, comparable observations | Those assumptions are not established for heterogeneous series means. Role/context normalization does not remove format, mean/clipping or roster/champion effects. Common-mask intersection can turn an otherwise sufficient history into unknown. |
| Confidence | Explicitly described as adequacy, not probability; separated from scouting | Multiplicative factors/bands are uncalibrated; high confidence needs unusually fresh/large evidence. Lower confidence does not reduce an already changed rating from a missing feature. NPC null measurement confidence is honest but does not solve shared-scale calibration. |
| Reproducibility | Pinned inputs, sorted ordering, decimal profile and frozen lookups are good foundations | Benchmark-fitting/rounding details, full lineage and lookup artifacts are not complete deliverables. They are future dependencies, not demonstrated deterministic evaluation. |

**Review judgment on double-counting:** excluding KDA and damage/gold ratios from core Combat avoids obvious duplicate bonuses. However, Laning resources can support later DPM, Vision activity may accompany map control, and Consistency is computed from Combat itself. These dependencies are not automatically forbidden; they require explicit consumers and ablation so one advantage is not rewarded repeatedly. Role-specific weights do not by themselves remove team/champion confounding.

### Existing worked examples independently checked

**FACT — bounded arithmetic, not empirical calibration:**

| Original case | Independent result | Scope of evidence |
|---|---|---|
| W01 | KDA7; KP.7; CS/min9; Gold/min500; DPM800; shares .3/.25; damage/gold1.6; deaths/min.066667; early differences400/300/10; VPM1. Ratings62/65/56/57; confidence.5 | Row arithmetic and separately supplied synthetic aggregate arithmetic agree. The one JSON row cannot reproduce forty series. |
| W02 | n_eff4, mean1.2 →53; adequate-data confidence.125 | Small-sample arithmetic agrees; the one/two-series unknown gate was reviewed from the specification, not exercised in a production evaluator. |
| W03 | Missing GD/XPD fails coverage; missing only XP yields60 and confidence.35 | Correct stated fallback; not evidence of unbiased missingness. |
| W04 | Context examples70/40; synthetic offset example66 | Ties-to-even result is correct in the current file. |
| W05 | New synthetic benchmark62→56 | Arithmetic agrees; campaign freeze is a required behavior, not a tested migration. |
| W06 | Weights1/.5 give n_eff1.8, below gate | Arithmetic agrees; grouping alone does not prove independence. |
| W07 | 62.5→62, 63.5→64; equal two-champion effective count2 | Rounding/diversity checked. Zero-denominator policies are textually consistent; a full future evaluator was not run. |
| W08 | Overlap constraint, not a numerical validation | Needs consumer/ablation evidence. |
| W09 | K10/20/40 →64/62/59 | Sensitivity arithmetic agrees; no K is thereby calibrated. |

### Additional counterexamples

**N01 — measured Consistency and NPC have incompatible attainable ranges.** From the unchanged formula, v≥0 and v_adjusted=r×v+(1-r), so consistency_z≤2r and pre-round rating≤50+30r. At n_eff12 this rounds to a maximum61; at40, maximum70; as evidence grows, the limit is80. By contrast, a legal laner NPC with B86, veteran S1, steady offsets(-2,-2,-2,+6), x=-2 and y=-2 has (L,C,V,K)=(81,81,87,99), sum348=4×87, with no clipping/projection. No amount of public performance evidence can yield that Consistency99 under the current measured formula. This is a mathematical scale mismatch, not merely different population selection.

**N02 — a missing unfavorable metric increases Combat.** ASSUMPTION: forty equally weighted, fully admitted laner series, oriented feature z=(2,2,-2). Full weights give .45×2+.35×2+.20×(-2)=1.2 →62. If death_rate becomes unavailable, the remaining two weights sum.8 and renormalize to a score2 →70. Confidence drops from.5 to.4, but the proposed capability still rises eight points. PROPOSAL: evaluate distinct calibrated mask models or a reviewed fixed-evidence policy; do not adopt either silently in this review.

**N03 — composite units differ.** ASSUMPTION: independent unit-variance input z-features, used only to expose the algebra. Variances are Laning.38, laner Combat.365, Jungle Combat.405, Support Combat.625 and Vision1. Real covariance changes these values rather than making them automatically1. A weighted sum called observed_z is therefore a standardized index, not necessarily a z-score. Decide whether to relabel/scale by role or review a composite normalization before comparing population spreads or assigning equal budget costs.

**N04 — series length can masquerade as Consistency.** ASSUMPTION: identical independent game-level performance variance1; a series mean based on five games has variance1/5 while a one-game series has variance1. If both use the same reference variance1 and n_eff40, the formula gives Consistency50 versus66. This illustrates an observation-horizon confound; it is not an assertion that all best-of-five series contain five independent games. Format/observed-game-count adjustment or an explicitly series-level construct needs review.

**N05 — clipping creates mean-dependent Consistency.** Forty equally weighted series alternating component signals3 and5 have the same pre-clip sample variance40/39 as alternating0 and2. If all positive-weight features share each signal, ±3 clipping collapses the first composite to a constant3. With reference variance1, the resulting Consistency is70 versus49. Extreme performance looks more stable solely because of clipping. Record this effect and review the variance-estimation stage; do not infer stable temperament or stable latent performance.

**N06 — more admitted rows can remove the common mask.** Twelve complete Combat series can meet the sample threshold. Add one otherwise admitted game missing DPM and another missing KP: their valid feature sets intersect the complete rows only at death_rate. For laners its weight is.20 and it is only one feature, so Consistency becomes unknown under the stated rule. This is an explicit conservative policy, not a coding error, but it creates a sharp availability discontinuity and requires an appropriate reference variance for every retained mask.

**N07 — confidence bands and aging.** With full coverage/freshness and q=.75, high≥.70 requires approximately280 effective series. Pooled q=.50 never reaches high. If all evidence is18 days old, even the infinite-sample limit is about.699775; at40 series, confidence changes from.5 when fresh to.25 at age180 while the mean-rating shrinkage stays unchanged. These are properties of an adequacy index, not calibrated certainty. Band naming and target cadence need adjustment or explicit acceptance.

**N08 — the current probability-noise consumer is not general upset potential.** FACT: in Competition.cs lines20–24, mean team Consistency40/80 with the fixture's Balanced posture yields spread10/8. ASSUMPTION: average uniformly over the keyed-draw ranges while holding other inputs fixed. With pre-noise win probability.20 or.50, both spreads give exactly the same mean probability (.20/.50) because the resolver is linear before clamping. Near the lower clamp, a center.06 yields approximately.086667 versus.080706. Consistency can affect a particular seeded result, clamp behavior and downstream probability-dependent consequences; it is not globally inert. But the proposed favorite/underdog decision is not generally supported merely by pointing at the existing spread field.

## NPC distribution review

**FACT — theoretical table analysis:** a review helper summed the finite raw probability support for the proposed band/base/stage/archetype/factor tables: 2700 combinations for each of laner, Jungle and Support groups. It did not generate players, draw seeds, assign names/IDs or implement a generator. Results below assume the published shares (e.g. N200 margins) and independent keyed purposes as the intended random model. They describe marginal theoretical expectations, not a realized finite cohort or empirical calibration.

| Group | Expected raw attributes | Mean active ability | Raw Consistency>80 | Raw vectors requiring [1,99] projection |
|---|---|---|---|---|
| Top/Mid/Bot | L51.56, C51.36, V50.36, K50.56 | 50.96 | 1.545488% | .001725% |
| Jungle | C50.66, V51.86, K50.36 | 50.96 | 1.378125% | 0% |
| Support | C49.76, V52.66, K50.46 | 50.96 | 1.401450% | 0% |

The expected active-attribute mean is E[B]+E[S]=51.51-.55=50.96. Theoretical elite mean≥80 is1.875%, below the2% exceptional-band cap. These calculations support the internal quota/budget arithmetic; they do not show the resulting people fit the match/economy model. Statistics are explicitly **before** projection; no full post-projection population, seeded reproducibility or performance test is claimed. The legal Consistency99 witness in N01 already lies within bounds and survives projection unchanged.

### Correlation and diversity

For laners, e_L=e_C=2x. Their difference is therefore only the archetype offset difference: +2 for striker, -2 for controller, 0 for steady. Conditional residual correlation is1; raw population correlation is approximately.992059. Raw L/V and L/K correlations are approximately.6579/.7605 because shared base ability dominates despite conditional trade-offs. Positive raw correlations are not inherently wrong; the restrictive L/C identity is the specific diversity problem. Strong laning/weak combat and weak laning/strong combat profiles cannot meaningfully emerge from the published interior model.

**PROPOSAL:** design target profile differences first, then review factor loadings/archetype offsets against them. An additional independent capability factor or stronger opposing offsets are options for later revision, not changes made here. Name variation and different ages do not substitute for strategic capability diversity.

### Ability budget and dominance

The zero-sum offsets/residuals and integer projection preserve a point-sum budget. They do not prove equal gameplay value per point. Consistency controls a different kind of outcome from mean skill; Vision has no approved consumer; laners have four budgeted values while Jungle/Support have three. Any future overall score based on the raw sum would privilege four-field profiles.

Counterexample of the claim that equal sums alone ensure strategic balance: at A55 and x=y=0, striker=(61,59,49,51), controller=(51,53,63,53), steady=(53,53,53,61). If a hypothetical downstream decision valued only L+C, their values would be120/104/106 despite equal total220. This is a witness to consumer dependence, **not** a proposed Execution adapter. Rarity quotas also constrain mean ability, not the fraction with an exceptional individual attribute or eventual team dominance.

### Lifecycle, potential and determinism

- The tables have explicit age/experience bounds and do not promise automatic age-based growth. Potential, champion specialization, adaptability and traits are honestly deferred. Thus this model cannot yet support buying growth potential or proving multi-decade regeneration balance; it is an initial-population hypothesis.
- Exact role quotas, capped exceptional counts, no employer-derived identity, and existing rng-v1 are sound directions. Global role coverage does not prove each actual roster/candidate market remains viable after allocation and transfers.
- Current rules v3 retains the recurring candidate restocking introduced in v2. NPC v0.1 explicitly declines recurring spawning until lifecycle approval. Replacing the current generator with an initial-only cohort would therefore change active behavior unless the market strategy is resolved first.
- Stable IDs and purpose-separated draws are appropriate. Event-key construction needs an unambiguous component grammar: cohort=a:region:b, region=c and cohort=a, region=b:region:c both concatenate to cohort:a:region:b:region:c:slot:000001 under the example format. rng-v1 length-prefixes the entire eventId, not its internal components. Restrict or encode component delimiters in a later specification revision.
- The fictional January1 birth-date convention and duplicate display names are acceptable as labeled synthetic metadata. They are not evidence of realistic career trajectories. Do not let future birthday/aging mechanics inherit synchronized population effects by accident.
- The same runtime rules are a good requirement, but measured shrunk estimates and generated latent values do not yet have a common calibrated interpretation. No NPC advantage should arise merely from having fully authored inputs while real-player fields are unknown or compressed.

## Existing architecture compatibility

**FACT:** the original package is architecturally aligned in intent and explicitly not loadable as current content/saves. That is an appropriate boundary, not a request to relax validation. The following specific integration effects must be included in any later change proposal.

| Area / evidence | Current behavior | Required preservation or explicit future decision |
|---|---|---|
| [Person](../../../../src/ManagementGame.Domain/Model.cs), line12 | Id, DefinitionId, Name, Role, Execution, Adaptability, Consistency, Readiness | New vector/identity fields need typed definitions/runtime contracts. Legacy player:generated is a template; retain unique saved EntityIds. |
| Person.Execution; [Competition](../../../../src/ManagementGame.Domain/Competition.cs), line13 | Readiness-weighted lineup mean supplies base capability | No justified automatic mean/max/sum of new attributes exists. Example-only ratings cannot be inserted without changing match expectations. |
| Technical Execution; Model.cs line52 | Campaign seed, revision, rules/content identity and receipts | This record is distinct from Person.Execution and Plan/Work.Execution. Keep technical bookkeeping separate from gameplay capability and Company/World ownership. |
| Person.Adaptability; Competition.cs lines18–19; Simulation repeated-posture input | Mean player Adaptability plus coach value contributes to adaptation; posture repetition is penalized using matches in the current PredictabilityWindow (fixture: last 3 results) | Deferred measured adaptability cannot erase this input. Retain legacy authored behavior and the current bounded history, or approve a deliberate rules change/bridge. |
| Person.Consistency; Competition.cs lines20–24 | Integer team mean, integer20-point spread steps, bounded probability noise | New statistical consistency is not interchangeable by name. N01/N04/N05/N08 must be resolved before mapping; small numeric differences may have no effect within the same step. |
| Readiness; [Simulation](../../../../src/ManagementGame.Domain/Simulation.cs), signing/daily/match transitions | Hiring min(current,70); daily recovery+1 up to100; selected match players lose8 down to1 | Preserve all these current rules unless explicitly superseded. No-reroll identity policy does not imply no hire-related readiness consequence. |
| Pay/recruitment; [Seasons](../../../../src/ManagementGame.Domain/Seasons.cs), line89; [Market](../../../../src/ManagementGame.Domain/Market.cs), Refresh | Salary=floor(Execution²/5); fees/release terms depend on it | Execution70→50 would move base salary980→500 minor units/day before other terms. New capability distribution and economic calibration cannot be separated casually. These fixture coefficients remain noncanonical. |
| Rules v3 incentives; [Finance](../../../../src/ManagementGame.Domain/Finance.cs), Market.Terminations, Simulation season rollover; [fixture](../../../../content/fixture.json) | Daily operating cost 2500 minor units; minimum employed roster 6; shortfall withholds match prize and fines 50% of that prize; sponsor forfeit limit 2; reputation moves toward a results-based target | Recruitment availability, affordability and extra roster capacity matter beyond fielding five players. Preserve these current consumers when selecting an NPC lifecycle and economic baseline. These fixture values and the reported v3 gate are not calibrated player-data coefficients. |
| Company/World roster | Company.People owns employed players; World.Candidates owns available people; lineup references IDs; rivals remain compressed | No duplicate Team roster or external-source employer authority. Hire/release transaction retains IDs and financial causes; role A–E mapping requires review. |
| [Content loader](../../../../src/ManagementGame.Infrastructure/ContentLoader.cs) | Schema3/slice-rules-v3; exact format, manifest, enums/fields and bounded input | A new player pack is a new reviewed contract; never feed example envelopes to the current loader or ignore unknown properties. |
| [PersonDto](../../../../src/ManagementGame.Infrastructure/SnapshotDtos.cs), [save store](../../../../src/ManagementGame.Infrastructure/SnapshotStore.cs), [validation](../../../../src/ManagementGame.Infrastructure/SnapshotValidation.cs) | Explicit schema3 DTOs; unchanged eight-field PersonDto; exact rules/RNG/canonical/content identity; valid A–E roles and single current owner; schema2 saves rejected | Pin versions/hashes. New-campaign-only does not magically provide a multi-version loader; old saves need an explicit supported path or clear rejection in a new build. No implicit reset from fresh definitions. |
| [Observations/CoachPolicy](../../../../src/ManagementGame.Application/Observations.cs), [read models](../../../../src/ManagementGame.Application/Contracts.cs) | Owned Execution/readiness; market ability band; coach chooses by Execution×Readiness | Define permitted estimates/explanations for new attributes. Neither UI nor staff receives hidden definition/generation evidence or potential by default. |

Preparation Work.Execution is completed preparation, not durable player development. The proposed null form/development fields correctly avoid introducing new gameplay by schema alone. Engine independence, deterministic execution, outcome consumption and local save boundaries can be retained; the review finds no need for a third gameplay owner or a multi-esport framework.

## Findings and risk register

**Critical:** none demonstrated in the current proposed files. Do not interpret this as approval or low implementation risk. Silent live-data overwrites, leaking hidden state or weakening strict save validation would violate inherited decisions, but the package expressly prohibits them.

| ID / severity / type | Evidence and impact | Proposed resolution and closure evidence |
|---|---|---|
| R-M01 — Major, known design gap | Catalog §§Selection/Laning/Combat/Vision and coverage/consumer matrices above. No complete five-role consumer contract; Jungle/Support gaps; Vision may be an unused number. | PD-D02: approve supported decisions/abstractions and per-attribute consumer contracts. Demonstrate role-specific scenario/ablation value or defer the field. Blocks gameplay-balance calibration and attribute-set approval. |
| R-M02 — Major, demonstrated mismatch | Formulas §Consistency; NPC §§Archetypes/real-NPC distribution; N01. Measured maximum80 versus legal NPC99; finite evidence further compresses measured values. | PD-D03/D04: choose common latent/estimated semantics and attainable scale; review measured and generated mappings jointly. Produce comparable-cohort and acquisition-noise tests. Blocks mixed real/NPC balance calibration. |
| R-M03 — Major, construct/observation bias | Formulas §§Series aggregation/Consistency; N04–N06. Series length, clipping and common-mask intersection alter measured variance independently of the intended trait. | Define game-versus-series target, format/measurement-noise treatment, variance stage and admissible masks/reference cells; test matched-variance and additional-missing-row cases. Blocks meaningful Consistency fitting/approval. |
| R-M04 — Major, comparable-score defect | Formulas §§Role weights/missingness/benchmarks; N02/N03. Composite units differ; removing an unfavorable feature increases power despite less evidence. | Fix the measurement contract through reviewed options for mask-specific comparability or fixed-evidence treatment; quantify covariance and one-versus-multiple rewards. Require no unexplained mask-dependent advantage. Blocks joint scale calibration. |
| R-M05 — Major, known feasibility/selection gap | Formulas §§Benchmarks/Consistency and contract §Observations. Twenty distinct players per role-cell, strict early snapshots and variance references may force widespread unknowns; mixed-tier histories lack a precise aggregation policy. | PD-D03: coverage audit and explicit competition-group/patch-era/context membership policy, including multi-context histories. Report rejected cohorts and rejected zero-opportunity games. Decide bounded scope or reviewed pooling before fitting. |
| R-M06 — Major, semantic integration risk | Competition.cs lines13–24; Pay.Player; N08; compatibility table. No approved Execution/Adaptability bridge; current variance consumer has limited mean-win sensitivity; wages are nonlinear in legacy Execution. | PD-D02/D05: preserve current behavior until a versioned consumer/adapter is explicitly designed. Specify expected match, wage and delegation changes with comparison scenarios. Blocks runtime adoption, not source-quality research. |
| R-M07 — Major, concrete wording conflict | Validation I02 says preserve readiness across hire/release/renew/transfer; NPC §Identity says recruitment only changes ownership/employment. Simulation.cs line41 caps signing readiness at70. | Amend the later specification to distinguish no reroll from authorized readiness transitions, and add a90→70 hiring case. Original files remain unchanged in this review. Blocks behavior-preserving integration approval. |
| R-M08 — Major, demonstrated diversity limitation | NPC §Archetypes: e_L=e_C; theoretical L−C∈{-2,0,2}, correlation≈.9921. Equal point budgets do not establish equal utility. | PD-D04 with D02: define specialist profile targets, review correlations/offsets and consumer-weighted opportunity costs, then test strategy-dependent choices. Blocks claiming calibrated archetype diversity. |
| R-M09 — Major, known lifecycle gap | NPC §Identity excludes recurring spawn; Simulation daily refresh and Market.Refresh actively replenish recruitment. Potential/development are null. Rules v3 also penalizes a roster below six. | PD-D04: keep initial-cohort research separate; before integration choose a stock/entry/exit budget preserving recruitment opportunities, roster capacity and role recovery. No silent removal of the current recurring market. Blocks long-campaign calibration/adoption. |
| R-M10 — Major, unresolved acceptance target | Validation §Calibration plan leaves numeric tolerances/strategy coverage open. Prediction of the same proxy, true capability and company decision value are different targets. | PD-D03: preregister primary/secondary targets, holdouts, baseline, subgroup tolerance, missingness and game-decision gates before tuning. Explicitly separate measurement calibration from gameplay/economy calibration. Blocks an empirical success/approval claim. |
| R-m01 — Minor, confidence interpretation | Formulas §Recency/confidence; N07. High-band feasibility and latest-row staleness discontinuity are not reflected in an empirical display/cadence policy. | Calibrate/relabel the adequacy bands and document freshness separately; do not present them as confidence probabilities. Needed before player-facing uncertainty claims. |
| R-m02 — Minor, deterministic-key ambiguity | NPC §Inputs: composite eventId uses delimiters allowed inside identifier components; explicit collision example above. | Restrict component grammar or specify unambiguous component encoding and collision/golden tests while preserving rng-v1. Resolve before generator implementation. |
| R-m03 — Minor, artifact completeness | Contract §Provenance/evidence and formulas §Arithmetic. Examples deliberately omit complete row lineage; context catalogs and lookup/fitting artifacts are future dependencies. Role-specific evidence cannot be represented by one universal aggregate in production. | Define per-attribute/role/mask trace, context registry, benchmark-fitting numerical profile and artifact hashes. Add a full synthetic end-to-end fixture and real/NPC unknown-role cases in a later authorized task. Needed for reproducible calibration implementation. |
| R-m04 — Minor, current-state documentation drift | README content-loader row now says schema3/v3, but its save row and Validation compatibility assessment still describe schema2/v2. The runtime changed concurrently during review. | PD-D05: refresh the compatibility baseline in a separately authorized revision, pinning current rules/content/save identities and incentive consumers. Preserve this reviewed version as evidence. This is a late worktree divergence, not proof the original assessment was wrong when authored. Resolve before integration planning; offline arithmetic is unaffected. |

The R-M05 coverage concern is conditional, not an unverified assertion about a provider: for example, a hypothetical ten-team cell with one starter per role has ten distinct players per role and fails the twenty-player gate regardless of game count. No provider's actual coverage was measured here. Likewise R-M09 does not require growth or retirement mechanics now; it requires an explicit scope boundary before replacing an ongoing market.

## Director decision recommendations — exact PD-D01–D05

The request wording below is transcribed from [README, Required Director Decisions](../../../10-vertical-slice/player-data/README.md#required-director-decisions). Recommendations are **PROPOSAL**, not approvals. Blocking levels distinguish source investigation, empirical measurement calibration, gameplay-balance calibration and runtime BUILD.

### PD-D01 — discipline scope

Exact request: **Accept/reject LoL as production discipline, or keep this an isolated design exercise.**

Options/trade-offs: (A) select LoL for production, enabling committed role/content work but accepting data/rights/design costs; (B) retain it as a bounded working-candidate investigation, preserving reversibility while deferring production integration; (C) reject it, retaining reusable ownership/provenance lessons while replacing discipline-specific measurement work.

Recommended resolution: **B for the next evidence stage**. Keep LoL explicitly a working candidate; authorize only the chosen source/coverage and revised-measurement calibration scope when its prerequisites are met. Do not treat this review as a production-discipline decision. A production choice can follow evidence of supported roles, accessible data and meaningful management consumers.

Blocking level: not a blocker to this review or synthetic/source-document investigation; the research scope must be explicit before funded/data-dependent calibration. A production decision blocks production LoL adoption, not general Phase5 work.

### PD-D02 — capability set and consumers

Exact request: **Select capability set and bounded competitive/management consumers; decide legacy Execution/Adaptability treatment and role mapping.**

Options/trade-offs: (A) keep the new fields as research/diagnostics while current gameplay remains on legacy inputs; (B) design a bounded role-aware capability consumer/adapter with specified decision effects, increasing integration/calibration cost; (C) accept all four as gameplay ratings based only on the current descriptions, which leaves the demonstrated coverage/consumer gaps unresolved and is not recommended.

Recommended resolution: **A during measurement revision, with a concrete B design required before gameplay-balance calibration.** Do not finalize four Core attributes yet. Keep Vision conditional on an independent consumer; require Jungle/Support representation tests; retain authored legacy Adaptability until a separate semantic treatment is approved. Define the A–E/LoL role mapping and distinguish all uses of the word Execution. Do not commission a full tactical simulator merely to justify available statistics.

Blocking level: high for attribute-set approval, gameplay-balance calibration and BUILD; selected statistical research can continue without installing any adapter. Closes R-M01/R-M06 and informs R-M08.

### PD-D03 — data, context and calibration criteria

Exact request: **Approve data source/use rights, supported coverage, benchmark population, uncertainty display and calibration acceptance criteria.**

Options/trade-offs: (A) one verified, permitted, sufficiently covered competition/context scope; simpler lineage and defensible comparisons, less market breadth; (B) several providers/tiers with verified joins and connected calibration, broader coverage at higher rights/modeling cost; (C) synthetic-only work until permitted data exists, useful for structure/arithmetic but unable to validate real-player inference.

Recommended resolution: **A after a coverage/rights audit; C while those prerequisites are absent.** Define competition_group/patch_era membership, actual metric coverage and the treatment of unsupported/mixed contexts before fitting. Resolve shared real/NPC scale, Consistency observation horizon and missing-mask comparability first. Preregister a primary held-out performance target, a role-only baseline, temporal/series grouping, subgroup non-inferiority tolerances and admissible coverage; separately define management-decision/economy targets. Do not select numeric tolerances after seeing the final holdout. The adequacy score needs an approved label; it is not posterior certainty or scouting confidence.

Blocking level: high for empirical real-player calibration or commercial data use; high for any calibration-success claim. Does not block this arithmetic review. Closes R-M02–R-M05/R-M10 and R-m01/R-m03. No provider contract is approved here.

### PD-D04 — population and lifecycle

Exact request: **Approve NPC cohort size/role/rarity budgets and lifecycle scope; approve or defer potential and career mechanics.**

Options/trade-offs: (A) bounded initial-cohort calibration only, no potential/growth mechanics; (B) recurring entrants and exits with explicit stock budgets and recruitment guarantees, more realistic long-campaign pressure but wider state/testing scope; (C) add potential/career mechanics immediately, creating additional hidden-state, development and balance obligations before the base scale is sound.

Recommended resolution: **A first, explicitly defer C.** Use N200/one-region tables as a reviewable trial configuration, not production population canon; revise the correlated capability model and common scale before evaluating archetypes. Keep potential null and age/experience descriptive. Before runtime adoption, commission the bounded portion of B needed to preserve current v3 restocking, missing-role recovery and the six-person roster requirement, with an active-population rarity budget rather than unlimited fresh2% rolls. Size and rarity percentages remain candidates.

Blocking level: high for mixed-population balance and any long-campaign conclusion; initial distribution experiments need a frozen trial configuration but no new career subsystem. Closes R-M02/R-M08/R-M09; the continuous-market requirement cannot be waived by calling the model initial-only.

### PD-D05 — runtime/persistence and observations

Exact request: **Approve new definition/runtime DTOs, version boundaries, old-save disposition and actor-safe projections.**

Options/trade-offs: (A) new-campaign-only adoption with explicit old-build/old-pack or unsupported-version policy, lowest migration risk; (B) declared opt-in migration with ID/role/value mappings and rollback evidence, preserves campaigns but requires substantial semantic testing; (C) refresh running campaigns from current public data, which conflicts with inherited ownership/determinism rules and is not an acceptable shortcut.

Recommended resolution: **A for the first eventual implementation**, after D02–D04 and the revisions. Specify whether older campaigns continue in their original build/pack or through an explicitly implemented version path; the current v3 loader supports only its exact identity and rejects schema2 saves. Refresh stale v2 compatibility text before designing another version transition. Preserve current signing/recovery/fatigue behavior, single ownership, financial causes, seed/RNG rules and restricted actor views. Defer B until a concrete migration requirement exists. Offline source/rating artifacts can be specified without changing PersonDto or live saves.

Blocking level: high for BUILD, live adoption and migration; not a blocker to a separate, permitted offline measurement study. Closes R-M06/R-M07/R-m04, and constrains R-M09 and information exposure.

## Recommended sequence before calibration

1. **Choose the experiment:** measurement research versus gameplay/economy calibration, the working discipline scope and the intended management decisions (D01/D02/D03). These are different claims and require different evidence.
2. **Revise constructs before tuning:** resolve real/NPC Consistency scale, variance horizon/clipping, composite/mask comparability and NPC L/C diversity (R-M02–R-M04/R-M08). Preserve the original v0.1 files as the reviewed baseline; author revisions only in a separately requested task.
3. **Audit permitted data:** actual role/context/metric coverage, sample gates, rejected-game bias, reference feasibility and connected tiers (D03). An unknown value is acceptable evidence of unsupported scope, not a reason to invent real-player values.
4. **Freeze a calibration protocol:** data/model/context versions, baseline/holdouts, evaluation targets and subgroup/coverage tolerances; frozen NPC trial population and shared scale. Approve uncertainty terminology before using it to influence player trust.
5. **Keep runtime decisions explicit:** initial NPC research may defer a full lifecycle, but eventual integration must preserve recruitment and readiness effects or obtain a deliberate change decision. Select save/actor boundaries before BUILD (D04/D05).
6. **Collect the right evidence:** actual dataset calibration and later bounded game-decision/economy tests. Arithmetic, theoretical PMFs, source citations and document-check counts cannot substitute for them.

## Verification performed and limits

**FACT:** ran `python artifacts/player-data-independent-review/review_calculations.py` from the repository root. It completed **37 independent assertions**, checked the existing arithmetic, reproduced the listed counterexamples and summed the finite raw NPC probability tables. Results are recorded locally in `artifacts/player-data-independent-review/calculation-results.json`. Those ignored review artifacts are calculation aids, not a shipped evaluator or generator. Analytical cases use stated assumptions; no real-player dataset or new rating assignments were created.

The original helper was inspected to understand its 612 document/example assertions. That earlier count primarily checks parsing, fields, links and selected arithmetic; it does not test role coverage, consumer utility, shared-scale validity, empirical calibration or population balance. Its original six-Markdown inventory describes the pre-review package; adding this review does not retrospectively change its historical result.

Final review verification checks this report's local links, required coverage, exact PD-D01–D05 wording and arithmetic evidence. The initial hash inventory is retained, alongside a second snapshot of the reconciled v3 sources. Seven original specification files are byte-identical to their initial hashes. For the README, replacing only the concurrent content-loader string `Strict schema 3, slice-rules-v3` with its initial `Strict schema 2, slice-rules-v2` in memory reproduces the initial hash; the file is not reverted. Final source checks compare the affected reviewed paths against the reconciled snapshot and record other concurrent changes separately. This establishes the review's evidence boundary without attributing the dirty worktree or concurrent v3 implementation to this review.

**FACT — final document verification:** `python artifacts/player-data-independent-review/verify_review.py` completed **159 checks: PASS**, including 37 local links, the five exact Director requests, required role/attribute/section coverage, the original-file checks described above and 35 reconciled evidence-file hashes. Its local result is `artifacts/player-data-independent-review/review-verification.json`. These are structural and preservation checks; review judgments are supported by the reasoning and counterexamples above, not by the check count.

Not executed: gameplay build/test suite, seeded NPC generator, empirical player calibration, live API/import, save migration, cross-machine replay or long-campaign experiment. Planned S/F/G/I tests in the original package remain planned. No formula, gameplay code, original specification, Decision Log, commit or push was changed/performed by this review.

## Final disposition

**NEEDS REVISION.** The ownership/provenance direction is suitable to carry forward, but the four-core model and mixed real/NPC population are not ready for approval or final balance calibration. Resolve the identified measurement/consumer/scale issues and disposition PD-D01–D05 at the appropriate gates. Synthetic and coverage investigation may continue within scope; empirical or gameplay success must await matching evidence. This is the review's recommendation, not a Director decision.
