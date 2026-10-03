# 04 — Esports Competition

**Status:** PROPOSAL — for Project Director review  
**Scope:** Minimum medium-depth competitive simulation for the first vertical slice.

## 1. Canonical constraints carried forward

**DECISION — inherited**

The player is primarily an executive leader with selective management authority. Competitive management may be manual, but it is optional depth rather than the center of the product. Routine operational work should be abstracted or delegated to coaches/staff. 

Competitive results matter because they create business leverage, not because winning matches is the sole objective.

The first vertical slice is limited to one company, one esports discipline, one primary team, limited staff, several rivals, a limited competition cycle, high-leverage preparation, delegation, and observable competition-to-company consequences. A full tactical match simulator is explicitly outside scope.

Simulation depth is medium by default, and variation should arise from changing state and interactions rather than repeated scripted novelty. 

---

# 2. Competitive simulation objective

**PROPOSAL**

Competition should answer this question:

> Given the roster, staff, available information, preparation choices, opponent, current competitive environment, and delegated/manual decisions, how likely is this team to perform well — and why?

The system should produce enough depth that:

- stronger rosters are advantaged but not guaranteed to win;
- preparation can materially alter outcomes;
- opponent matchups matter;
- information quality matters;
- good coaches create value;
- delegation produces meaningful differences;
- repeating the same competitive plan becomes vulnerable under changing conditions;
- the player can understand the major reasons behind an outcome.

It should **not** simulate individual mechanical actions, complete drafts, every tactical instruction, or minute-by-minute match execution.

---

# 3. Minimum competitive state

## 3.1 Competition Context

**PROPOSAL**

For the current competitive event:

- event / competition identifier;
- stage;
- opponent;
- match or series format;
- competitive importance;
- current progression / standing;
- rules relevant to roster eligibility or match structure;
- current discipline meta reference.

This is contextual state only.

---

## 3.2 Team Competitive Profile

The competitive simulation consumes a compact representation of the active team:

### Player-derived capability

Minimum dimensions:

- **Execution** — ability to perform the player's assigned competitive role;
- **Role Proficiency** — suitability for the current role/position;
- **Adaptability** — capacity to perform under changed strategies/meta;
- **Competitive Consistency** — reliability of performance;
- **Current Readiness** — temporary match-relevant condition.

These are competition inputs derived from the authoritative player/people state.

Competition should not redefine the underlying player model.

### Team-derived capability

Minimum team-level dimensions:

- lineup familiarity;
- role coverage;
- team coordination;
- strategic familiarity;
- recent competitive form.

These may be derived from roster continuity, preparation and prior competitive activity rather than existing as unrelated permanent player attributes.

---

# 4. Staff inputs

## Coach

**PROPOSAL**

The vertical slice requires at least one meaningful competitive leadership role: **Head Coach**.

Relevant coach inputs:

- opponent analysis;
- strategic judgment;
- preparation quality;
- adaptability;
- player/lineup evaluation;
- delegation reliability.

Additional analysts or assistant coaches are not required as separate characters for the first slice.

If analytics capability exists elsewhere in the approved simulation, it may modify the coach's information quality without requiring another deep staff subsystem.

Staff matters because approved Product Foundation decisions already require staff to affect operational quality, information, recommendations and delegation capacity.

---

# 5. Preparation model

## Preparation Window

**PROPOSAL**

Before a competition, the team receives a finite **Preparation Capacity** supplied by existing time/staff-capacity systems.

Competition does not define the resource formula.

Preparation Capacity is distributed among three competitive priorities:

### A. Team Execution

Improves:

- coordination;
- strategic familiarity;
- reliability of the current lineup.

Characteristics:

- broadly useful;
- relatively stable;
- lower opponent specificity.

### B. Opponent-Specific Preparation

Improves performance against the known upcoming opponent through:

- studying tendencies;
- exploiting weaknesses;
- preparing responses to likely strategies.

Characteristics:

- potentially high short-term value;
- dependent on information quality;
- much less transferable to future opponents.

### C. Meta / Strategic Adaptation

Improves:

- ability to use current strong approaches;
- ability to handle recently changed competitive conditions;
- strategic flexibility.

Characteristics:

- useful when the meta changes;
- competes with immediate opponent preparation;
- retains some value across matches.

### Core trade-off

A team cannot maximize all three simultaneously.

Example:

> Spending most preparation on Rival A may increase the chance of beating Rival A but leave the team less prepared for another opponent or an emerging meta shift.

This directly implements the accepted short-term tension between opponent-specific preparation and general development.

---

# 6. Limited manual high-leverage decisions

**PROPOSAL**

The first vertical slice should expose only four competitive intervention points.

## Decision 1 — Preparation Priority

Player may approve or alter the distribution between:

- team execution;
- opponent preparation;
- meta adaptation.

---

## Decision 2 — Lineup / Role Exception

The coach proposes the preferred lineup and role configuration.

The player intervenes only when there is a meaningful alternative.

Examples:

- proven veteran vs higher-upside developing player;
- strongest individual player vs better team fit;
- specialist lineup vs flexible lineup.

Routine obvious lineup confirmation should not require a click.

---

## Decision 3 — Strategic Posture

Select or approve one high-level approach.

Minimum abstraction:

- **Conservative** — prioritizes reliability and reduced volatility;
- **Balanced** — normal risk/reward;
- **Aggressive** — accepts higher variance to pursue stronger upside.

This is a match-resolution modifier, not a tactical minigame.

Different discipline modules may eventually replace these generic labels with discipline-specific concepts.

---

## Decision 4 — One Situational Pivot

For important matches, the player may receive **at most one high-leverage intervention opportunity** when the simulated match state materially changes.

Examples:

- remain with the prepared strategy;
- shift toward a safer plan;
- take a high-risk adaptation;
- accept the coach's recommendation.

No repeated pause-and-command loop.

A match should remain fully playable through delegation without player intervention.

---

# 7. Delegated coach decisions

**PROPOSAL**

When authority is delegated, the Head Coach performs:

- lineup recommendation;
- role configuration;
- preparation allocation;
- opponent-specific plan selection;
- strategic posture;
- situational adaptation.

The coach evaluates the same underlying simulation state available to manual control.

There must be **no hidden competitive bonus simply because the player manually controls the team**.

Manual play provides control, not supernatural information or performance.

Coach decision quality depends on:

- coach capability;
- information quality;
- familiarity with the roster;
- strategic tendencies;
- current constraints;
- opponent uncertainty.

A talented coach with poor information may still make a reasonable but incorrect choice.

---

# 8. Authority Envelope

## Concept

**PROPOSAL**

The **Authority Envelope** defines what the coach may decide independently and when the player must be consulted.

It is not simply an "AI ON/OFF" setting.

The envelope contains:

### Delegated domains

For the vertical slice:

- preparation allocation;
- lineup;
- strategic posture;
- situational adaptation.

Each can be:

- player controlled;
- coach recommends / player approves;
- coach autonomous.

---

## Strategic constraints

The player may specify boundaries such as:

- risk ceiling;
- protected / required player usage when applicable;
- development preference vs immediate strength;
- priority on opponent-specific preparation;
- restriction against major lineup experimentation.

These constrain coach decisions rather than dictate every action.

---

## Escalation

Coach should escalate a decision when:

- a proposed action lies outside the Authority Envelope;
- available information is unusually uncertain;
- two alternatives are assessed as materially close;
- an unexpected player-availability problem occurs;
- the decision would violate an explicit player constraint.

The coach provides:

- recommendation;
- main reasoning;
- estimated confidence;
- relevant risk.

---

## Override behavior

The player may override the coach.

An override:

- immediately replaces that decision;
- remains the player's responsibility;
- should not automatically damage relationships or create arbitrary penalties;
- can produce better or worse outcomes depending on actual conditions.

Repeated overrides may later interact with people/relationship systems, but deep coach-player relationship simulation is outside this specialist's first-slice requirement.

---

# 9. Information and uncertainty

**PROPOSAL**

The system should distinguish:

### Known

Examples:

- own roster;
- player eligibility;
- current competition rules;
- prior public match results.

### Estimated

Examples:

- opponent current strength;
- likely lineup;
- preferred strategies;
- role tendencies;
- likely strategic response.

### Unknown / unresolved

Examples:

- hidden opponent preparation;
- exact tactical choice;
- match-day variance.

Information quality should influence the **precision of estimates**, not reveal hidden truth directly.

Example:

Poor scouting:

> Rival aggression: possible, low confidence.

Strong scouting:

> Rival aggression: likely, high confidence; particularly against conservative opponents.

This supports the established imperfect-information design principle.

Randomness should represent unresolved competitive variance after decisions are made, not replace decision quality.

---

# 10. Opponent and rival inputs

Competition receives from World State:

- rival roster reference;
- relevant player capabilities;
- rival coach capability;
- recent form;
- observable strategy history;
- lineup stability;
- current competitive standing;
- meta adaptation state;
- information visible to the player's organization.

Competition does **not** own rival-company strategy, finances, contracts or general World State.

The underlying rival organization remains World State.

The Competition system only interprets the competitive portion needed for match and preparation resolution.

Rivals must evolve rather than act as fixed difficulty levels, consistent with the accepted product direction.

---

# 11. Match resolution abstraction

## Core resolution dimensions

**PROPOSAL**

A match/series is resolved through four major competitive dimensions:

### 1. Base Competitive Capability

Derived primarily from:

- active players;
- roles;
- current readiness;
- team coordination.

### 2. Preparation Advantage

Derived from:

- preparation allocation;
- opponent information;
- preparation quality;
- whether the actual opponent behavior matches expectations.

### 3. Strategic Matchup

Compares:

- each team's strategic posture;
- known tendencies;
- lineup characteristics;
- current meta;
- opponent counter-preparation.

No strategy is universally dominant.

### 4. Adaptation

Represents:

- coach adaptability;
- player adaptability;
- available strategic familiarity;
- quality of situational decisions.

---

## Series resolution

For multi-game competition, resolve the contest at **game/map level**, but without internal action simulation.

Each game produces:

- winner;
- compact performance margin;
- key competitive contributors;
- possibly a meaningful state change before the next game.

Examples:

> Opponent-specific preparation worked strongly.

> Aggressive posture created an advantage but increased variance.

> The opponent adapted after Game 1.

> The unfamiliar lineup underperformed despite superior individual skill.

A best-of series therefore creates some adaptation without requiring detailed tactical execution.

---

## Variance

Competitive variance is required.

However:

> Good decisions change probabilities; they do not guarantee results.

The system should prevent both extremes:

- deterministic "higher rating always wins";
- arbitrary coin-flip outcomes that make management irrelevant.

---

# 12. Result outputs

Each resolved match should produce a **Competitive Result Package**.

Minimum outputs:

- win/loss;
- series score or placement consequence;
- competition progression;
- expected-result comparison;
- performance margin;
- major positive factors;
- major negative factors;
- preparation effectiveness;
- strategy effectiveness;
- coach decision assessment;
- notable player/team performance signals;
- newly observed opponent information;
- updated strategy exposure/history;
- downstream consequence signals.

Post-match explanation should emphasize causal factors instead of exposing raw hidden calculations.

Example:

> Victory 2–1  
> Main contributors:
> - opponent-specific preparation successfully targeted Rival A's predictable opening plan;
> - your lineup had stronger coordination;
> - Rival A adapted in Game 2;
> - the aggressive final-game posture increased both upside and risk.

---

# 13. Competition → reputation / audience / commercial interfaces

**PROPOSAL**

Competition must **emit outputs**, not directly mutate reputation, audience or commercial state.

Minimum downstream signal:

`CompetitiveOutcome`

Conceptually contains:

- competition importance;
- stage reached;
- result;
- opponent significance;
- expected vs actual performance;
- upset magnitude;
- consistency / streak context;
- rivalry context;
- achievement milestone;
- competitive visibility.

Downstream systems determine the actual consequences.

Examples:

Competition:

> Unexpected victory over a major rival in a high-importance semifinal.

Reputation system decides:

> reputation consequence.

Audience system decides:

> audience/fandom consequence.

Commercial system decides:

> sponsor/commercial opportunity consequence.

This preserves explicit system boundaries while supporting the established loop:

Competitive performance → reputation/audience → commercial leverage → future organizational capability.

Competition must not define:

- money rewards;
- sponsor payouts;
- company valuation;
- reputation formulas;
- audience-growth formulas.

---

# 14. Adaptation and anti-repetition

**PROPOSAL**

Competitive variation should come from state changes rather than arbitrary event text.

## A. Opponent adaptation

Rivals accumulate observable competitive history.

Competent opponents may react to frequently used:

- lineups;
- strategic postures;
- preparation tendencies.

A repeated plan does not automatically receive a penalty.

Instead:

> predictable behavior becomes easier for capable, informed opponents to prepare against.

---

## B. Strategy exposure

Frequently used competitive approaches become increasingly observable.

Effects depend on:

- opponent information quality;
- coach analysis capability;
- how predictable the player's organization has become.

This creates pressure to maintain strategic flexibility without forcing random strategy changes.

---

## C. Meta movement

The discipline meta may shift externally.

This changes the relative value of:

- certain player profiles;
- lineups;
- strategic approaches;
- preparation priorities.

The system needs only a limited number of meta states or shifts in the first slice.

---

## D. Roster change

Signing, benching or losing a player changes:

- capability;
- role coverage;
- coordination;
- strategic options;
- preparation needs.

Therefore a roster upgrade can create short-term integration cost rather than functioning as a simple permanent `+strength`.

---

## E. Rival development

Opponents may:

- improve;
- decline;
- change lineup;
- change coach;
- alter strategic tendencies.

Competition consumes these changes but does not own their wider World State logic.

---

# 15. State ownership

No new top-level source of truth is introduced.

## Company State — existing ownership retained

Contains authoritative player-company entities such as:

- roster/personnel;
- staff;
- player condition;
- contracts;
- company-controlled delegation settings.

Competition may own **competition-domain records inside the existing company boundary**, including:

- current preparation plan;
- chosen competitive lineup;
- strategic posture;
- Authority Envelope for competition;
- competitive history relevant to the player's team;
- strategy exposure;
- current competition entry/progression where appropriate.

## World State — existing ownership retained

Contains authoritative external entities such as:

- rival organizations;
- rival rosters and staff;
- external competition environment;
- discipline meta;
- external competition schedule/results where globally shared.

The Competition specialist does not redefine Company State or World State boundaries.

---

# 16. Test scenarios

The prototype should support at least the following tests.

### T1 — Stronger roster is not automatic victory

Team A has superior individual capability.

Team B has:

- better preparation;
- better strategic matchup;
- stronger coordination.

Expected:

Team A remains advantaged in some circumstances, but Team B has a credible path to victory.

---

### T2 — Preparation trade-off matters

Run the same matchup twice:

A:
- heavy opponent preparation.

B:
- heavy general execution preparation.

Expected:

A performs better against the target opponent, while B retains broader preparation value afterward.

---

### T3 — Incorrect intelligence

Player heavily prepares for an estimated opponent tendency.

The information was low-confidence and incorrect.

Expected:

Preparation value falls, but the loss is explainable through uncertainty rather than arbitrary punishment.

---

### T4 — Good coach delegation

Strong coach + good information + broad authority.

Expected:

Coach selects a coherent lineup, preparation plan and strategy without player input.

Player can review understandable reasoning.

---

### T5 — Authority Envelope escalation

Coach prefers an aggressive approach.

Player-defined risk ceiling prohibits it.

Expected:

Coach does not silently violate the envelope and instead escalates or selects an allowed alternative.

---

### T6 — Manual override

Coach recommends Option A.

Player chooses Option B.

Expected:

The simulator resolves B normally.

No hidden "manual control bonus" or automatic punishment exists.

---

### T7 — Repeated strategy becomes scouted

Player repeatedly uses the same strategic profile.

A rival with strong analysis encounters the player again.

Expected:

The rival can gain meaningful preparation advantage.

A weak/information-poor rival should exploit this less effectively.

---

### T8 — Meta change

A previously effective approach becomes less suitable after a meta shift.

Expected:

The old strategy remains usable but is no longer automatically optimal.

Preparation toward adaptation becomes valuable.

---

### T9 — Star roster integration

A stronger player enters the lineup immediately before competition.

Expected:

Individual capability improves while coordination/familiarity may temporarily decline.

The net competitive effect is context-dependent.

---

### T10 — Commercial interface boundary

Player defeats a major rival in an important event.

Expected:

Competition emits a rich CompetitiveOutcome.

Competition itself does not directly add money, reputation, audience or valuation.

---

### T11 — Delegated vs manual parity

Identical plan and decisions are executed once manually and once through an authorized coach.

Expected:

The competitive resolution is equivalent.

Control mode itself provides no bonus.

---

### T12 — Explainability

After an upset loss, the player should be able to identify several major causal factors.

Expected feedback:

- weak opponent preparation;
- lineup coordination problem;
- unfavorable strategic matchup;
- opponent successful adaptation.

The player should not receive only:

> "Bad luck: -17%."

---

# Outcome

A minimum **medium-depth competitive simulation** can be built around:

Roster Capability  
+ Team Coordination  
+ Staff Quality  
+ Preparation  
+ Information  
+ Strategic Matchup  
+ Adaptation  
+ Competitive Variance

→ Match / Series Result  
→ Explainable Competitive Outcome  
→ downstream company consequences.

This is substantially deeper than a simple roster-rating roll while remaining far below a complete tactical esports simulator.

# Proposed mechanics

- three-way preparation allocation;
- coach-generated competitive recommendations;
- limited manual intervention;
- Authority Envelope;
- estimated opponent information with confidence;
- strategic posture and matchup abstraction;
- game/series-level resolution;
- adaptation between games;
- strategy exposure;
- limited meta change;
- causal post-match analysis;
- explicit CompetitiveOutcome interfaces.

# State owned

Competition-domain state only:

- preparation plan;
- competition lineup/roles;
- strategic posture;
- competitive Authority Envelope;
- competition progress/results;
- team competitive history;
- strategy exposure;
- competition-specific observations.

All state remains inside the established Company State / World State ownership model.

# Inputs

From roster/people:

- execution;
- role proficiency;
- adaptability;
- consistency;
- readiness;
- availability.

From team history:

- familiarity;
- coordination;
- recent form.

From staff:

- analysis;
- strategy;
- preparation;
- adaptability;
- lineup judgment.

From simulation/world:

- competition context;
- opponent competitive state;
- meta;
- available information and confidence;
- preparation capacity.

From player:

- preparation priorities;
- Authority Envelope;
- optional lineup override;
- optional strategic posture;
- optional situational intervention.

# Outputs

- result;
- score/progression;
- performance explanation;
- preparation effectiveness;
- strategic effectiveness;
- player/team performance signals;
- coach decision feedback;
- opponent observations;
- updated strategy exposure;