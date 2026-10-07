# Approved Semantic UI Asset Manifest

Status: APPROVED SOURCE ASSETS FOR PORTAL IMPLEMENTATION
Branch authority for this pass: `stage5-sponsor-instruments`

These assets are supplied by the user and MUST be used according to the semantic role below.
Do not regenerate, reinterpret, recolor, repurpose, or substitute them with improvised imagery.

| Asset | Role | Allowed Portal use |
|---|---|---|
| `icon_module_portal_v1.png` | Portal / central management hub | Portal navigation/module identity only |
| `icon_module_squad_v1.png` | Squad / active roster | Squad navigation/module identity only |
| `icon_module_company_v1.png` | Company / organization | Company navigation/module identity only |
| `icon_module_staff_v1.png` | Staff / coach domain | Staff navigation/module identity; generic staff-domain fallback only when no person portrait exists |
| `icon_person_player_v1.png` | Generic player entity | Player/entity fallback only when an authoritative player portrait asset is unavailable |
| `icon_decision_roster_move_v1.png` | Roster move | Signing/release/transfer/roster-move decisions only |
| `icon_decision_contract_v1.png` | Contract/agreement | Player/staff contract or formal agreement decision only |
| `icon_decision_match_preparation_v1.png` | Match preparation | Match preparation / tactical preparation decision only |

## Hard constraints

- The PNGs are canonical supplied assets. Preserve their bytes and visual design.
- Do not use one semantic asset to stand in for an unrelated concept.
- Do not invent portraits, staff photos, event/news photos, sponsor logos, team crests, or character art when no authoritative asset exists.
- Missing visual content must use an existing deterministic non-image fallback already supported by the UI, or a neutral text/shape treatment. Never silently generate replacement artwork.
- Runtime data such as names, money, dates, records, statuses, transfer fees, team names and counts must come from authoritative read models/state. Values visible in mockups are visual examples, not game data.
- Navigation/control mechanics (save, load, arrows, filters, tabs, close, warning etc.) remain normal UI/vector controls and must not be replaced by these semantic 3D assets.

## Reference mockup

`docs/10-vertical-slice/ui-research/mockups/generated-reference-set/11_portal_world_transfer_graphite_mockup.png`

The mockup is a composition and visual-hierarchy reference, NOT a source of literal data or identities.
