# Stage 5 UI research and operations-room mockup

Status: DEVELOPMENT / NONCANONICAL, 2026-10-07. Director review pending.

- `OPERATIONS_ROOM_PROPOSAL_V1_REJECTED.html` — first visual proposal. Rejected by the
  user as a generic "AI dark dashboard" (25 type sizes, caps everywhere, gold misused,
  letter crests, hatching with six meanings). Kept for traceability of the critique.
- `GAME_UI_RESEARCH.html` — research of 16 well-regarded management/strategy games
  (FM26, OOTP 26, Motorsport Manager, F1 Manager, CK3, Victoria 3, Frostpunk, Civ VII,
  Into the Breach, Slay the Spire, XCOM, Balatro, Mini Metro, Persona 5, Disco Elysium,
  Hades) plus esports broadcast overlays, mapped to each shell screen and checked
  against the critique rules. Sources are linked inside.
- `OPS_ROOM_MOCKUP_1280x720.html` — clickable mockup at a true 1280×720 frame: quiet
  operations room for daily screens, broadcast language for identity and match day.
  English/Vietnamese string toggle and a live self-audit of the rules (type scale
  12/14/16/20/24/32/48, gold only for Continue/Commit, no CSS caps outside buttons, no
  truncation, must-see elements inside 720px, no " · " chaining).

Open the HTML files in a browser. Match results in the mockup are illustrative and use
the `Competition.Resolve` formula. No game code, content or save schema changed.
Next step (not started): port tokens/type scale to `UiTokens`, remove rail glyphs in
`GameShell.cs`, add a letterless crest component, then screen by screen.
