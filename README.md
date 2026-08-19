<!-- GENERATED FILE — do not edit by hand.
     Source: packaging/README.md + packaging/README.github-extra.md
     Regenerate with: bash packaging/gen-readme.sh -->

**TODO: one-line pitch**

TODO: short paragraph, what the mod does and why

<!-- TODO: header screenshot/gif -->

Client-sided, no other player needs it installed for this to work.

---

## Features

- TODO

## Notes

- TODO

## Feedback & bug reports

Found a bug or have a suggestion? Please **[fill out this form](https://forms.gle/4Vi7kp2c42A9FfSu5)** or send me an email at `theactualcooker@gmail.com`.

## Configuration

Config file: `BepInEx/config/OnlyCook.StatPreview.cfg`.

TODO: settings summary

## Credits

TODO

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
