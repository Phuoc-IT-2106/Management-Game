# Approved Semantic UI Assets

Status: APPROVED FOR THE CURRENT PORTAL / RELATED UI INTEGRATION BY EXPLICIT USER DIRECTION.

These files are runtime derivatives of the user-supplied source images. They were resized without cropping or redesign to a maximum dimension of 512 px and encoded as WebP for repository/runtime use. Preserve aspect ratio and alpha at render time.

## Canonical role mapping

| Asset | Intended role |
| --- | --- |
| `icon_module_portal_v1.webp` | Portal / central-hub module semantic art |
| `icon_module_squad_v1.webp` | Squad / roster module semantic art |
| `icon_module_staff_v1.webp` | Staff / coaching-domain semantic art; NOT a named staff portrait |
| `icon_module_company_v1.webp` | Company / organization module semantic art |
| `icon_person_player_v1.webp` | Generic player fallback/entity art; NOT a person-specific portrait |
| `icon_decision_contract_v1.webp` | Contract / renewal / formal-agreement decision art |
| `icon_decision_match_preparation_v1.webp` | Match-preparation / tactical-planning decision art |
| `icon_decision_roster_move_v1.webp` | Roster move / signing / release / transfer semantic art |

## Hard usage rules

- Do not regenerate, redraw, repaint, recolor, restyle, or reinterpret these assets.
- Do not use an asset for a semantically unrelated purpose just to fill visual space.
- Do not use the rendered semantic assets as tiny mechanical-control icons (save, load, settings, close, arrows, filters, etc.). Mechanical controls remain normal UI/vector symbols.
- Do not pretend generic player/staff art is the portrait of a named person.
- If a character, staff member, event, world-news story, team crest, organization crest, or other identity-specific image is not available, do NOT generate or fabricate replacement artwork. Use an approved deterministic fallback, existing identity-mark system, or a text-led/neutral presentation.
- Runtime AI image generation is not authorized.
- Missing art must not block truthful data presentation.
- Keep data/state ownership separate from asset selection. Use stable semantic IDs/catalog mapping rather than deriving logic from display strings.
- Every integration point must use the asset corresponding to the actual semantic role.

These rules are intentionally narrower than a general art-pipeline decision. They authorize use of this supplied set; they do not silently authorize generation of additional production art.
