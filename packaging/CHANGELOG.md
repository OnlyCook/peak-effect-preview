## 1.0.0

Full release.

- Added support for duration based items (Lantern, Remedy Fungus, etc.) to show an icon-only preview. If such an item is held, only the icon of the designated status effect that is able to be affected by the item will change. Thanks to VanilaBOI for the suggestion!
- Added a config option to disable the weight preview.
- Added a config option to toggle the jetpack gauge needle preview.
- Made jetpack and vanilla bar count options independent of the master preview setting. Thanks to SAvGEDA for pointing this out!
- Fixed Piton, Rope Spool, and Anti-Rope Spool not showing a weight preview when they're usable.
- Fixed bonus stamina showing slightly off preview counts in rare cases.
- Fixed many animations being driven by frame counts instead of delta time, resulting in slower animation speeds on lower fps.
- Adjusted bonus stamina and Petrify animation speeds.

## 0.2.0

- Fixed snappy ghost bar removal issue that would cause all other bars to jitter slightly (now buttery smooth).
- The *Thorns* status effect is now properly supported by the preview logic (forgot about it).
- Fixed the dual-count stamina (N → M) label from abruptly appearing whilst animating the stamina bar. Also increased the stamina bar's minimum width before it can appear.
- Fixed *Petrify* label flickering while the preview was the only effect present in the bonus stamina bar.
- Fixed the infinite stamina label from flickering to show nonsensical values for a single frame. Also made the label switching anchoring mechanics more robust which should fix rarer but similar issues.
- Fixed the Scout's Initiative amulet from showing an incorrect petrify count in certain cases.
- Optimized certain code where I did some questionable things per frame (we don't talk about that though).

## 0.1.0

Initial release.

Known issues (expect more though):
- ~~When a ghost bar is removed others may jitter slightly.~~ [fixed in 0.2.0]
- Certain co-op preview logic may produce incorrect results.
