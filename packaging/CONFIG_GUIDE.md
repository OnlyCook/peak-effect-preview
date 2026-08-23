## Guide on how to edit this mod's config

Firstly you need to get to your `.../PEAK` folder, you get there like this:

<img width="410" height="349" alt="steam-peak-folder-guide" src="https://raw.githubusercontent.com/OnlyCook/peak-effect-preview/refs/heads/main/packaging/steam-peak-folder-guide.png" />

Then you need to get to your config file, it should be here: `.../PEAK/BepInEx/config/OnlyCook.EffectPreview.cfg`.

> Note: you have to launch the game with the mod installed at least once for the config file to appear.

Now you can simply edit that config file with any text editor. It may look scary to some but editing that file is very simple!

> Only the lines **without** a hash (#) are actually configurable.

Here's an example:

```
## Show a ghost preview of status effect changes on both stamina bars while holding an item that would cause them.
# Setting type: Boolean
# Default value: true
enable-preview = true
```

- The **first line** (with two hashes) tells you what the config setting is about (description).
- The **second line** tells you what kind of data type the setting uses.
- The **thid line** mentions the default value (if you want to reset it).
- The **fourth line** is the actual setting. In this case you can change it from 'true' to 'false' to disable that setting.

*Note:* there can also be another line `# Acceptable values:`, this one tells you what kind of values you to can use. `# Acceptable value range:` tells you the min and max numbers you can use.
