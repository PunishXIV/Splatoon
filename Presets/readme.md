# Splatoon Presets

This is a repository of layout and script presets for Splatoon. These presets have been developed and provided by numerous people from the [Puni.sh Discord server](https://discord.gg/Zzrcc8kmvy). The scripts in this directory are vetted and verified to be trustworthy by the Spaltoon development team.

## Definitions

There are 2 types of presets:

- **Normal presets** consists of one or multiple layouts and are shared via a line of text.
- **Scripts** are basically microplugins that run in a specific instance. They are used when mechanics are too complex to be contained in normal presets. Scripts are imported from via a URL and not from a text box.

# Tags

Presets may contain the following tags:

- `[EN]`, `[JP]`, `[DE]`, and `[FR]` mean that preset or script will work with the indicated client language(s). Usually, porting it to another language will involve adding some localized lines from game's log to triggers or object names.

- `[International]` presets or scripts do not relies on data derived from client language and can be used on any game language without modifications.

- `[Untested]` presets or scripts have no data about whether they work correctly or at all. Please use caution when adding these. It's advised to try them out in Duty Recorder before using them in a duty or event.

- `[Beta]` incidates that the preset or script has passed initial testing but it may contain problems, not cover all possible patterns or is still undergoing development. They may be significantly changed in future, so check back for updates.

# Importing Presets

> [!IMPORTANT]  
> Do not blindly import everything. You will just end up with a visual mess on your screen. Find presets for the mechanics you want to display and import those.

Once you've decided that you want to import a preset, you need to copy the preset code into your clipboard by pressing button on the right side of code block:

![](/docs/images/preset_import/image_3.png)

Then, import your preset into the Splatoon plugin by opening it with the `/splatoon` command in-game. Proceed to `Layouts` tab and then press the `Import from clipboard` button.

![](/docs/images/preset_import/image_4.png)

If you did everything correctly, the preset should be added to the list on the left side. If this isn't working, please visit the [Puni.sh Discord server](https://discord.gg/Zzrcc8kmvy) for support.

## Additional Sources of Presets

Below is a list of third-party repositories and websites that have additional Splatoon presets. If something is not here, please check these to see if what you are looking for was developed elsewhere.

> [!WARNING]
> If you are installing a script from a source other than this repo, please make sure you understand its code. Scripts have full access to your computer, similar to how plugins do. If you don't understand it, at least make a support request in their developer's Discord server before installing.

- https://github.com/adamchris1992/ffxivsplat
- https://github.com/cptjabberwock/SplatoonPresetsList/wiki
- https://github.com/Ksirashi/Presets
