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
