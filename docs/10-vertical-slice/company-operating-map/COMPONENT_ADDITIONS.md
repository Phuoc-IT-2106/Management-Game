# Component reuse review

SPEC decision before BUILD: zero new reusable components. UI engineer owns the
bounded OperatingMap scene and its navigation presenter. Canonical kit is unchanged.

| Proposed component | Composition decision |
| --- | --- |
| OperatingMapNode | EntityLabel in native nested containers |
| SituationMarker | EntityLabel with priority/status text; AlertItem in inspector |
| ScopeRelationship | SemanticText labels and direct EntityLabel scope links |
| ContextInspector | SectionHeader, AlertItem, ConfidenceIndicator, DocumentView |
| OperatingMapListEquivalent | Same EntityLabel bindings in indented ownership list |

Scene inputs: immutable MapSnapshot. Events: stable-ID UiIntent, local navigation.
Tokens: only existing roles/metrics. States: selected/focused/known/estimated/unknown,
quiet/missing/stale. Native labelled controls and keyboard reading regions provide
the accessibility baseline. Scene owns child lifetimes, frees replaced inspector
subtrees, wires persistent controls once. Bounded nodes and callback dispatch are
measured. No UniversalGraphNode, generic graph/workspace engine or virtualization.
Layout exception: two lanes each receive half available map width; indentation is
SpaceGroup per ancestor. These are semantic layout decisions, not new style tokens.
