## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) `5.4.2403`

## For players

- You can install the mod through r2modman as `Stat_Preview`,
- On [Thunderstore](https://thunderstore.io/c/peak/p/OnlyCook/Stat_Preview/),
- Or on Nexus Mods (TODO: link once published)

## For developers

- [`ROADMAP.md`](ROADMAP.md): full feature spec, phased plan, status, handoff notes.

Build:
```bash
cd src/StatPreview
dotnet build -c Release                          # -> bin/Release/StatPreview.dll
dotnet build -c Release -p:DeployToProfile=true  # also copy into the r2modman profile
```
