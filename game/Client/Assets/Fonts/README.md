# Bundled fonts

The game's one UI family (UI polish standard §7). It ships with the game, so text looks the same on every machine and no
system font is redistributed.

| File | Weight | SHA-256 |
|---|---|---|
| `Inter/Inter-Regular.ttf` | 400 | `40d692fce188e4471e2b3cba937be967878f631ad3ebbbdcd587687c7ebe0c82` |
| `Inter/Inter-Medium.ttf` | 500 | `97ad806f526e41546d46365bb3a393145f75b7b1568913db74549ad8b8dba872` |
| `Inter/Inter-SemiBold.ttf` | 600 | `78a843fade9d4612a5567302fb595b56976eb5fcebf4fea5a5912d638bafcde3` |
| `Inter/OFL.txt` | licence | `262481e844521b326f5ecd053e59b98c8b2da78c8ee1bdbb6e8174305e54935a` |

**Source.**
- Inter 4.1 by The Inter Project Authors, release `Inter-4.1.zip` from
  <https://github.com/rsms/inter/releases/tag/v4.1>.
- Archive SHA-256 `9883fdd4a49d4fb66bd8177ba6625ef9a64aa45899767dde3d36aa425756b11e`.
- The three files are the archive's `extras/ttf` static instances, unmodified.

**Licence.** SIL Open Font License 1.1 (`Inter/OFL.txt`, copied from the archive's `LICENSE.txt`).
- The fonts may be bundled, embedded and redistributed with the game, including commercially.
- They may not be sold on their own.
- The licence must travel with them.
- A modified font may not use the reserved name.

**Use.**
- `UiTokens.FontPath` names the files and `UiTheme.Face` loads them.
- Godot imports them as `FontFile` (`*.ttf.import`), so exports pack them like other assets.
- Glyphs Inter lacks fall back to the system symbol faces (`UiTokens.SymbolFontNames`).
