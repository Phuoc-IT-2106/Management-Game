# Shared Godot theme

`UI/Theme/UiTheme.cs` builds a native Theme resource from UiTokens. Each UI host
owns one UiContext per density/text-scale configuration and sets its root Theme;
children inherit it. The existing internal management client retains its theme.

Theme variations: CompanyIdentity, WorkspaceTitle, SectionTitle, Body, Data and
Annotation inherit Label. The Label typography role configures Label directly,
avoiding a self-inheriting variation. SelectedEntity inherits Button. SurfaceBase,
SurfaceRaised, SurfaceInset and SurfaceOverlay inherit PanelContainer. Semantic
colors are also addressable on the Theme's `Semantic` type. Small shared factories
bind text roles and semantic tones; hosts do not create aesthetic overrides.

Native Button normal/hover/pressed/hover_pressed/disabled/focus styles are shared.
Focus draws a separate 2px outline. Selection additionally says `[Selected]`.
Status and information labels remain visible below EntityRow, so selected +
focused + estimated + warning can coexist. Native containers own size and wrapping;
Flow children receive shared minimum widths to avoid one-character wrapping.
Scroll follows focus. Static Lab specimens can be reached by focusing the scroll
region and using PageUp/PageDown/Home/End.

SystemFont uses the fallback lists in the [token catalog](TOKEN_CATALOG.md).
Data uses a provisional monospaced numeric family; no embedded text or font files.
System fallback permits missing Unicode glyphs, but glyph coverage is not certified
for all languages. Manifest records the resolved UI family. Replacing families,
role sizes, surfaces or shape values requires central token/theme changes only.

BrandResolver is pure C# and accepts OrganizationIdentityView. It preserves raw
choices, resolves each accent against neutral OrganizationSurface to >=3:1 by
mixing toward the light endpoint in at most 64 deterministic steps, then chooses
black or white text for the highest primary-accent contrast (>=4.5:1). Adjustment
flags expose fallback decisions. Identical colors are permitted; status never
depends on differentiating primary and secondary. It returns branding roles only,
so warnings/errors/focus cannot be overwritten. NavigationContext applies those
roles to the identity swatch and its organization surface. The host supplies an
already-resolved optional emblem texture; components perform no file/network IO.
Missing/null/broken references degrade to short name and `[CO]` text.

Contracts, fixture construction, contrast and intent binding compile separately
without Godot or gameplay references in `tests/UiKit`. The native client assembly
still has its existing Infrastructure reference for Main; the Lab never constructs
Composition, a Session, campaign content or saves. The Lab is a separate scene,
launched explicitly; project.godot's main entry is unchanged.

Native mechanisms follow Godot's [theme variation documentation](https://docs.godotengine.org/en/stable/tutorials/ui/gui_theme_type_variations.html)
and [keyboard navigation model](https://docs.godotengine.org/en/stable/tutorials/ui/gui_navigation.html).
No custom renderer, graph engine, layout engine or theme plugin is introduced.
