<!-- GENERATED FILE — do not edit by hand.
     Source: packaging/README.md + packaging/README.github-extra.md
     Regenerate with: bash packaging/gen-readme.sh -->

**Shows you exactly how much of each status effect will be added/removed before having to use the held item.** All with clear indicators and purely through the game's own stamina bars which keeps the UI minimal.

<img width="790" height="222" alt="screenshot-1" src="https://raw.githubusercontent.com/OnlyCook/peak-effect-preview/refs/heads/main/packaging/screenshot-1.png" />

Fully client-sided: only you need to install the mod and only you will see it's effects.

---

## Features

- See status effect changes of the held item
- Hold an item at a lit campfire/stove and press **`C`** to see how it's stats would change when cooked
- Tell when you'd fall unconscious or turn into a statue through item usage
- See how much longer *Invincibility*, *Infinite Stamina*, or *Speed Boost* will last
- Know when you'd waste an item's precious stats *(Off by default)*
- Preview an item's effect just by aiming at it on the ground while empty-handed *(Off by default)*
- Know when an affliction will start to decay and when it finishes decaying *(Off by default)*
- See status effect counts/numbers *(Off by default)*

<img width="790" height="222" alt="screenshot-2" src="https://raw.githubusercontent.com/OnlyCook/peak-effect-preview/refs/heads/main/packaging/screenshot-2.png" />

## Feedback & bug reports

Found a bug or have a suggestion? Please **[fill out this form](https://forms.gle/CWWfrk1dyKkycwN99)** or send me an email at `theactualcooker@gmail.com`.

## Configuration

Config file: `BepInEx/config/OnlyCook.EffectPreview.cfg`.

<details>

<summary><b>View config information</b></summary>

- **General**: master preview switch.
- **Interactions**: world-object previews (unlit campfires, ancient luggage), player-entity previews (Thorn/Arrow stuck on you, cannibalism), item pickup previews, sticky Thorn/Arrow removal.
- **Item Previews**: weight preview, timed-usage preview (status effects fully/partially removed by item duration), jetpack fuel gauge preview, cooking preview toggle and its key (default **`C`**).
- **Bar Display**: waste indicator, removal blink, ghost/vanilla bar count numbers, plain (untinted) bar count style and bar count font scale, affliction countdowns (time until an affliction starts and finishes decaying) and their font scale, Invincibility/Infinite Stamina/Speed Boost remaining duration counts and bar visualization.

</details>

## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) `5.4.2403`

## For players

- You can install the mod through r2modman as `Effect_Preview`,
- On [Thunderstore](https://thunderstore.io/c/peak/p/OnlyCook/Effect_Preview/),
- Or on [Nexus Mods](https://www.nexusmods.com/peak/mods/213)

## For developers

Build:
```bash
cd src/EffectPreview
dotnet build -c Release                          # -> bin/Release/EffectPreview.dll
dotnet build -c Release -p:DeployToProfile=true  # also copy into the r2modman profile
```
