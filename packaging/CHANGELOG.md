## 0.2.0

- Fixed snappy ghost bar removal issue that would cause all other bars to jitter slightly (now buttery smooth).
- The *Thorns* status effect is now properly supported by the preview logic (forgot about it).
- Fixed the dual-count stamina (N → M) label from abruptly appearing whilst animating the stamina bar. Also increased the stamina bar's minimum width before it can appear.
- Fixed *Petrify* label flickering while the preview was the only effect present in the bonus stamina bar.
- Fixed the infinite stamina label from flickering to show nonsensical values for a single frame. Also made the label switching anchoring mechanics more robust which should fix rarer but similar issues.
- Fixed the Scout's Initiative amulet from showing an incorrect petrify count in certain cases.
- Optimized certain code where I put unnecessary work on delta time (we don't talk about that though).

## 0.1.0

Initial release.

Known issues (expect more though):
- ~~When a ghost bar is removed others may jitter slightly.~~ [fixed in 0.2.0]
- Certain co-op preview logic may produce incorrect results.
