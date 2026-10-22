"""Local HTML report of the research ratings: six Top 100 lists, strongest players per competition, coverage and the
method's limits (offline).

Reads data/ratings/*.json written by rate_players.py and validate_ratings.py; writes data/ratings/top100_report.html.
Portraits and logos are referenced by relative path into data/assets/ (git-ignored) and never embedded, so the file
shows them only on this machine. It is a local research page: do not publish it.
"""
import argparse
from collections import Counter, defaultdict
import html
import json
from pathlib import Path
import sys

import rate_players as rp

LISTS = ("Overall", "Top", "Jungle", "Mid", "Bot", "Support")
BAND_LABEL = {"ELITE": "Elite", "VERY_GOOD": "Very good", "AVERAGE": "Average", "INEXPERIENCED": "Inexperienced",
              "AMATEUR_ACADEMY": "Amateur/academy"}
FILES = {"Overall": "top100_overall.csv", "Top": "top100_top.csv", "Jungle": "top100_jungle.csv", "Mid": "top100_mid.csv",
         "Bot": "top100_bot.csv", "Support": "top100_support.csv"}

CSS = """
:root{--bg:#f6f6f4;--panel:#ffffff;--ink:#1d1f21;--muted:#62676d;--line:#e2e2de;--accent:#2f5d8a;--warn:#8a5a00;--chip:#eef1f4}
@media (prefers-color-scheme:dark){:root{--bg:#141618;--panel:#1c1f22;--ink:#e6e6e3;--muted:#9aa0a6;--line:#2c3034;--accent:#8db4dc;--warn:#e0b45c;--chip:#262a2e}}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:14px/1.45 system-ui,-apple-system,"Segoe UI",sans-serif}
main{max-width:1280px;margin:0 auto;padding:24px 16px 64px}h1{font-size:22px;margin:0 0 4px}h2{font-size:17px;margin:32px 0 8px}
p.lead{color:var(--muted);margin:0 0 16px;max-width:900px}.note{border-left:3px solid var(--warn);padding:8px 12px;background:var(--panel);margin:12px 0;max-width:900px}
nav{display:flex;flex-wrap:wrap;gap:6px;margin:16px 0}nav a{padding:6px 12px;border:1px solid var(--line);border-radius:6px;color:var(--ink);text-decoration:none;background:var(--panel)}
nav a:hover{border-color:var(--accent)}.wrap{overflow-x:auto;background:var(--panel);border:1px solid var(--line);border-radius:8px}
table{border-collapse:collapse;width:100%;font-variant-numeric:tabular-nums}th,td{padding:6px 8px;border-bottom:1px solid var(--line);text-align:left;white-space:nowrap}
th{font-size:12px;color:var(--muted);font-weight:600;position:sticky;top:0;background:var(--panel)}td.n{text-align:right}td.ovr{font-weight:700;font-size:15px}
img.p{width:32px;height:32px;object-fit:cover;border-radius:4px;background:var(--chip);vertical-align:middle}img.l{width:22px;height:22px;object-fit:contain;vertical-align:middle;margin-right:6px}
.muted{color:var(--muted)}.chip{display:inline-block;padding:1px 6px;border-radius:4px;background:var(--chip);font-size:12px}
.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(300px,1fr));gap:12px}.card{background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:12px}
.card h3{font-size:14px;margin:0 0 8px}dl{display:grid;grid-template-columns:auto 1fr;gap:2px 12px;margin:0}dt{color:var(--muted)}dd{margin:0}
"""


def esc(value):
    return "" if value is None else html.escape(str(value))


def asset(path):
    return f"../assets/{path}" if path else None


def read_csv_rows(path):
    import csv
    with path.open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def titles(r):
    finals = int(r["international_finals"] or 0)
    return esc(r["titles"]) + (f" <span class='muted'>· {finals} intl final{'s' if finals > 1 else ''}</span>" if finals else "")


def table(rows):
    head = ["#", "", "Player", "Real name", "Role", "Team (as of)", "League", "Nat.", "OVR", "Band", "MEC", "EAR", "TFT",
            "MAC", "POOL", "Age", "POT", "Titles", "Form", "G23–26", "G26", "Conf."]
    out = ["<div class='wrap'><table><thead><tr>" + "".join(f"<th>{h}</th>" for h in head) + "</tr></thead><tbody>"]
    for r in rows:
        portrait = asset(r["player_image"])
        logo = asset(r["team_logo"])
        team = (f"<img class='l' src='{esc(logo)}' alt='' loading='lazy'>" if logo else "") + esc(r["team"]) + \
            (f" <span class='muted'>({esc(r['team_as_of'])})</span>" if r["team_as_of"] else "")
        picture = f"<img class='p' src='{esc(portrait)}' alt='' loading='lazy'>" if portrait else ""
        out.append("<tr>" + "".join([
            f"<td class='n'>{esc(r['rank'])}</td>", f"<td>{picture}</td>",
            f"<td><b>{esc(r['player'])}</b></td>", f"<td>{esc(r['real_name'])}</td>", f"<td>{esc(r['role'])}</td>", f"<td>{team}</td>",
            f"<td>{esc(r['league'])}</td>", f"<td>{esc(r['nationality'])}</td>",
            f"<td class='n ovr'>{esc(r['overall'])}</td>", f"<td><span class='chip'>{esc(BAND_LABEL.get(r['band'], r['band']))}</span></td>",
            f"<td class='n'>{esc(r['mechanics'])}</td>", f"<td class='n'>{esc(r['early_game'])}</td>",
            f"<td class='n'>{esc(r['teamfight'])}</td>", f"<td class='n'>{esc(r['macro'])}</td>",
            f"<td class='n'>{esc(r['champion_pool'])}</td>", f"<td class='n'>{esc(r['age'])}</td>",
            f"<td class='n'>{esc(r['potential'])}</td>", f"<td class='n'>{titles(r)}</td>", f"<td>{esc(r['form'])}</td>",
            f"<td class='n'>{sum(int(r[f'games_{y}'] or 0) for y in (2023, 2024, 2025, 2026))}</td>",
            f"<td class='n'>{esc(r['games_2026'])}</td>",
            f"<td><span class='chip'>{esc(r['confidence'])}</span></td>"]) + "</tr>")
    out.append("</tbody></table></div>")
    return "".join(out)


def competition_cards(players, minimum=20, per=10, competitions=24):
    groups = defaultdict(list)
    for p in players:
        if p["rating_evidence_class"] != "PRIOR_ONLY" and p["player_id"] and p["games_2026"] >= minimum and p["league"]:
            groups[(p["league"], p["region"])].append(p)
    ranked = sorted(groups.items(), key=lambda kv: -len(kv[1]))[:competitions]
    cards = []
    for (league, region), rows in ranked:
        rows.sort(key=lambda p: (-p["overall_rating"], -p["games_2026"], int(p["player_id"])))
        items = "".join(f"<tr><td class='n'>{i + 1}</td><td>{esc(p['player'])}</td><td>{esc(p['primary_role'])}</td>"
                        f"<td>{esc(p['team_2026'])}</td><td class='n ovr'>{esc(p['overall_rating'])}</td></tr>"
                        for i, p in enumerate(rows[:per]))
        cards.append(f"<div class='card'><h3>{esc(league)} <span class='muted'>{esc(region)} · {len(rows)} rated</span></h3>"
                     f"<table><tbody>{items}</tbody></table></div>")
    return "<div class='grid'>" + "".join(cards) + "</div>"


def build(out_dir=rp.OUT_DIR):
    data = json.loads((out_dir / f"{rp.OUTPUT_STEM}.json").read_text(encoding="utf-8"))
    manifest = json.loads((out_dir / "rating_manifest.json").read_text(encoding="utf-8"))
    validation_path = out_dir / "validation.json"
    validation = json.loads(validation_path.read_text(encoding="utf-8")) if validation_path.exists() else {}
    players = data["players"]
    classes = Counter(p["rating_evidence_class"] for p in players)
    lists = {label: read_csv_rows(out_dir / FILES[label]) for label in LISTS}
    outer = validation.get("outer", {}).get("models", {})
    selected = validation.get("selected", {}).get("name")
    rows = []
    for name, m in sorted(outer.items(), key=lambda kv: (kv[1] or {}).get("log_loss", 9)):
        if m:
            rows.append(f"<tr><td>{esc(name)}</td><td class='n'>{m['games']}</td><td class='n'>{m['log_loss']}</td>"
                        f"<td class='n'>{m['brier']}</td><td class='n'>{m['accuracy']}</td><td class='n'>{esc(m.get('auc'))}</td></tr>")
    validation_table = ("<div class='wrap'><table><thead><tr><th>Model</th><th>Test games</th><th>Log loss</th><th>Brier</th>"
                        "<th>Accuracy</th><th>AUC</th></tr></thead><tbody>" + "".join(rows) + "</tbody></table></div>") if rows else \
        "<p class='muted'>Validation not run.</p>"
    nav = "".join(f"<a href='#{label.lower()}'>{label}</a>" for label in LISTS) + "<a href='#competitions'>By competition</a><a href='#method'>Method</a>"
    sections = []
    for label in LISTS:
        deficit = manifest["ranking_deficits"][label]
        title = "Top 100 overall (EXPERIMENTAL cross-region)" if label == "Overall" else f"Top 100 {label}"
        sections.append(f"<h2 id='{label.lower()}'>{title}</h2><p class='muted'>{deficit['eligible']} eligible, {deficit['published']} published"
                        f"{', deficit ' + str(deficit['deficit']) if deficit['deficit'] else ''}. Ranked by Overall in the role, then raw quality, "
                        f"then 2026 games, then provider id.</p>" + table(lists[label]))
    elig = manifest["eligibility"]
    page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>LoL Research Ratings</title><style>{CSS}</style></head><body><main>
<h1>LoL professional players — research ratings 2023–2026</h1>
<p class="lead">{esc(manifest['rating_model_version'])} · games before {esc(manifest['as_of_date'])} · {manifest['games']:,} games,
{manifest['seats']:,} player-games · {len(players):,} catalogue players: {classes.get('MEASURED_ESTIMATE', 0):,} measured,
{classes.get('LIMITED_EVIDENCE', 0):,} limited evidence, {classes.get('PRIOR_ONLY', 0):,} prior only.</p>
<div class="note"><b>Research model, not an official rating.</b> Not a Riot, PandaScore or Leaguepedia rating and not approved game
balance. Overall bands: ELITE 90–93 (needs an international final or two tier-a titles in 2023–2026), VERY GOOD 83–89,
AVERAGE 74–82, INEXPERIENCED 55–73, AMATEUR / ACADEMY 30–54. POT = potential (age 25 or younger; equal to the Overall from 25).
Skills are proxies built from published box scores (EarlyGame has no timeline data in this source set and equals the
Overall's level). Cross-region positions rest on international games and player movement and are EXPERIMENTAL. Local file
only: portraits and logos belong to their owners and are shown from the local cache.</div>
<nav>{nav}</nav>
{''.join(sections)}
<h2 id="competitions">Strongest rated players per competition (2026, ≥ {elig['min_games_2026_in_role']} games)</h2>
<p class="muted">Within one competition the comparison does not depend on cross-region levels.</p>
{competition_cards(players)}
<h2 id="method">Method and validation</h2>
<div class="grid"><div class="card"><h3>Eligibility for the Top 100</h3><dl>
<dt>Identity</dt><dd>{esc(elig['identity'])}</dd><dt>Evidence</dt><dd>{esc(elig['evidence'])}</dd>
<dt>2026 games in role</dt><dd>≥ {elig['min_games_2026_in_role']}</dd><dt>Last game</dt><dd>within {elig['max_days_since_last_game']} days</dd>
<dt>Level SD</dt><dd>≤ {elig['max_level_sd']} points</dd></dl></div>
<div class="card"><h3>Scale</h3><dl><dt>Anchor</dt><dd>{manifest['anchor']['level']} = mean of tier-a domestic players ({manifest['anchor']['players']})</dd>
<dt>Points per logit</dt><dd>{manifest['content']['points_per_logit']} (content LogitScale {manifest['content']['logit_scale']})</dd>
<dt>Tier levels</dt><dd>{esc(', '.join(f'{k}: {v}' for k, v in manifest['tier_levels_points'].items()))}</dd>
<dt>Selected config</dt><dd>{esc(selected or json.dumps(manifest['config']))}</dd>
<dt>Bands (all catalogue)</dt><dd>{esc(', '.join(f'{BAND_LABEL.get(k, k)}: {v}' for k, v in sorted(manifest['band_counts'].items())))}</dd></dl></div></div>
<h2>Temporal validation (fit before {esc(validation.get('outer_cutoff'))}, scored after)</h2>{validation_table}
<p class="muted">Sources: Leaguepedia scoreboards (CC BY-SA text; statistics belong to Riot Games), PandaScore catalogue and stage
rosters and birthdays. Full methodology: tools/lol-data-ingestion/reports/PLAYER_RATING_MODEL.md.</p>
</main></body></html>"""
    path = out_dir / "top100_report.html"
    path.write_text(page, encoding="utf-8", newline="\n")
    return path


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--out", type=Path, default=rp.OUT_DIR)
    args = parser.parse_args()
    print(build(args.out))
    return 0


if __name__ == "__main__":
    sys.exit(main())
