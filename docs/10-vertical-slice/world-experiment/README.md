# World experiment: dynamic potential, real rosters and a 4,000-NPC world

Date: 2026-10-15. Status: **EXPERIMENT — awaiting Director review**.

**Nothing was changed** in production code, content, rules or save contracts: the gameplay baseline is unchanged
(Domain 15/15, Integration 369/369 executed after the work).

**The experiment runs only in research tools**, outside the solution:
- `tools/WorldExperiment` (C#; references the production Domain for the rating formula, the RNG and the series
  resolver);
- `tools/lol-data-ingestion` (Python).

**Its datasets stay in git-ignored storage** (`tools/lol-data-ingestion/data/world/`), because they contain real names
or are derived from provider data.

## Reports

| Report | Question it answers |
|---|---|
| [PLAYER_POTENTIAL_TRAINING_EXPERIMENT](PLAYER_POTENTIAL_TRAINING_EXPERIMENT.md) | Where the ceiling near 93 comes from; potential up to 99; the dev2 training model and its measured outcomes |
| [REAL_TEAM_ROSTER_VALIDATION](REAL_TEAM_ROSTER_VALIDATION.md) | Which real teams have a verified starting five as of 2026-10-13, and why the others do not |
| [NPC_4000_GENERATION_REPORT](NPC_4000_GENERATION_REPORT.md) | How the 4,000 NPCs are generated and calibrated; their distributions |
| [NPC_EMPLOYMENT_DISTRIBUTION](NPC_EMPLOYMENT_DISTRIBUTION.md) | The world structure, the allocation and the census |
| [FREE_AGENT_MARKET_VALIDATION](FREE_AGENT_MARKET_VALIDATION.md) | The free-agent cohort (< 10%), its origins, and the market over 10 seasons |
| [HYBRID_WORLD_INTEGRATION_REVIEW](HYBRID_WORLD_INTEGRATION_REVIEW.md) | Architecture conflicts, performance, persistence, risks, a minimal integration proposal and the decisions needed |
| [HYBRID_WORLD_DECISION_VERIFICATION](HYBRID_WORLD_DECISION_VERIFICATION.md) | Four decisions verified in 20-season runs: real-team survival (D-H1), the rating ceiling (D1), replacement intake and free agents (E2/F1), save architecture and career history (D-H3). Awaiting Director approval |

## Reproduce

```powershell
cd tools/lol-data-ingestion
.\.venv\Scripts\python.exe rating_scale_experiment.py   # research ratings without the 93 clamp -> data/ratings/experiment-uncapped/
.\.venv\Scripts\python.exe build_real_rosters.py        # verified real rosters -> data/world/
.\.venv\Scripts\python.exe world_calibration.py         # aggregate NPC calibration -> data/world/
.\.venv\Scripts\python.exe -m unittest discover -s tests -p "test_*.py"
cd ../..
dotnet build tools/WorldExperiment/WorldExperiment.csproj -c Release
dotnet run --no-build --project tools/WorldExperiment -c Release -- develop --replicates 1000
dotnet run --no-build --project tools/WorldExperiment -c Release -- bench
dotnet run --no-build --project tools/WorldExperiment -c Release -- test --seasons 10      # world + 40 checks
dotnet run --no-build --project tools/WorldExperiment -c Release -- world --seasons 10 --seed 1 --out <dir>
dotnet run --no-build --project tools/WorldExperiment -c Release -- world --seasons 10 --variant fixed-intake --out <dir>
dotnet run --no-build --project tools/WorldExperiment -c Release -- world --seasons 10 --variant npc-fallback --out <dir>
```

- **Prerequisite.** The world commands need the staging database and the research ratings of the data tool (README
  section "Player performance and research ratings").
- **Configuration.** Every coefficient is in `tools/WorldExperiment/config/world-experiment.v1.json`.
- **Seed.** Seed 20261015 reproduces the reported numbers on this machine.
