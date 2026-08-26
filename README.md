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
- *(Optional)* Know when you'd waste an item's precious stats
- *(Optional)* See status effect counts/numbers

<img width="790" height="222" alt="screenshot-2" src="https://raw.githubusercontent.com/OnlyCook/peak-effect-preview/refs/heads/main/packaging/screenshot-2.png" />

## Feedback & bug reports

Found a bug or have a suggestion? Please **[fill out this form](https://forms.gle/CWWfrk1dyKkycwN99)** or send me an email at `theactualcooker@gmail.com`.

## Configuration

Config file: `BepInEx/config/OnlyCook.EffectPreview.cfg`.

<details>

<summary><b>View config information</b></summary>

- **General**: master preview switch, world-object previews (unlit campfires, ancient luggage), player-entity previews (Thorn/Arrow stuck on you), weight preview, cooking preview toggle and its key (default **`C`**), sticky Thorn/Arrow removal, waste indicator, removal blink, ghost/vanilla bar count numbers and their font scale.

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
