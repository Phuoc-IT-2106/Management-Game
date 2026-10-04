# Player-company branding rules

Status: PROPOSAL contract; configurable/noncanonical identity is a binding task
constraint from the Director brief. No branding editor is built here.

## Authority and data

| Campaign identity field | Meaning / constraint |
| --- | --- |
| organizationId | Stable opaque campaign entity ID; immutable across display-name changes |
| displayName | Player-defined company name; never a code branch, lookup key or route key |
| shortName | Player-defined short label/abbreviation; full name available on focus/inspection |
| emblemReference | Optional approved presentation asset reference; no canonical logo |
| primaryIdentityColor | Player-selected identity color stored separately from system semantics |
| secondaryIdentityColor | Optional complementary identity color; independently validated |

Company owns these durable campaign facts under DEC-010. Content can seed
development examples; it must not prescribe the production player's identity.
Application exposes an immutable OrganizationIdentityView (conceptual name) with
stable ID and revision. Screen code only reads that view. Future identity edits
require validated typed commands; no screen writes a mutable Company or stores
a competing organization record in UI preferences.

Identity serialization belongs in an explicit campaign DTO/version change.
Presentation asset references resolve outside Domain. Missing images cannot
prevent loading a valid campaign. Names/colors do not influence gameplay rules,
RNG keys, eligibility or pricing. Full snapshot hashes may change when serialized
identity changes; compare gameplay outcomes, not identical whole-save hashes, in
identity-invariance tests. Do not promise backward compatibility for today's saves.

The current `Company.Id/Name` and `Situation.Company` are partial support only.
Short name, emblem, colors and user-authored initialization are migration work.
No Domain redesign or new top-level authority is needed.

## Safe presentation of arbitrary branding

Brand may affect identity accents, emblem, team/division identity surfaces and
decorative highlights. Danger, warning, positive/negative states, system errors,
selection clarity and focus use game-system tokens. They cannot be overwritten.

Retain the chosen raw campaign color. A shared presentation resolver evaluates the
actual foreground/background pair against approved contrast targets. If unsafe,
choose a suitable system text color; if still unsafe, render a neutral identity
surface with a bounded brand swatch/accent and a separating border. A swatch that
resembles danger retains an organization label; actual warnings add system icon
and text. Do not silently change the player's saved color to fix one surface.

Missing secondary color uses a neutral system role, not a prescribed second brand
color. Missing/unreadable emblem uses a neutral symbol or initials with a text
label. Missing name during loading uses a loading state; invalid identity after
load shows an identity error/fallback label (“Player company”) and a repair path
when supported, never a fictional canonical brand.

Validate text/asset/color shape at the campaign boundary; render names as plain
text, not markup. Length/character policy, asset size/type limits and editor UX
are OPEN QUESTIONS, not arbitrary limits chosen here. Layout must handle long
Unicode names with wrapping or elision plus an accessible full label. Duplicate
display names are distinguishable by context and IDs internally, not business
rules tied to the text.

## Fixture policy

Permitted example:

```text
organizationId: DEV_ORG_001
displayName: TEST_COMPANY
shortName: TEST
emblemReference: absent
primaryIdentityColor / secondaryIdentityColor: test-case values
label: DEVELOPMENT FIXTURE — NONCANONICAL
```

This is a contract example, not an instruction to rename existing save IDs.
Current `company:player` remains a stable technical ID until a separately reviewed
migration. Any fixture company identity uses a technical label and the explicit
noncanonical marker in data and visible test context. No fixture becomes canon.

## Required later checks

- Rename and recolor the same organization without changing routes, selected
  entities, commands or gameplay outcomes; save/load preserves the new identity.
- Exercise bright, dark, low-contrast, identical primary/secondary, warning-like
  and absent colors, long Unicode name and missing emblem. These are temporary
  fixtures, not final palette recommendations.
- Verify company identity across home, decision, document, timeline and save/load
  return; unknown asset uses fallback and never leaks a previous campaign logo.
- Verify semantic warning/focus survives every branding variant and no conditional
  behavior compares the company display name.

Current fixture replacement must update its content digest and declared save/test
identity through existing tooling, retain old evidence as historical, and state
the experimental compatibility boundary. That implementation is deferred.
