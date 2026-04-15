# Thank you for considering contributing to this project!
## Before you proceed with pull request, please consider the following:

### No AI
Please do not contribute code that was AI-generated or written with heavy AI-assistance. Exception: if a plugin allows for loadable scripts or modules, feel free to use AI for these scripts or modules without any restrictions. Output should be still human readable, though. 

### Keep additions contained
If you would add new functions, please create own classes for them and keep them contained. The plugin should not become dependent on added functions; the plugin's ability to work should not be harmed if added functions become unavailable and have to be disabled.

### Do not refactor
Even if current code is unoptimal, please avoid refactoring existing code. 
- Do not replace opcodes with hooks
- Do not replace static data with dynamic data
- Do not optimize utility functions
- Do not replace if trees with switch statements
- Do not change accessibility modifiers
- Do not replace manual ImGui.Begin calls with windows
- Do not replace regular ImGui calls with ImRaii

If you know how to significantly improve something by refactoring, please create an issue describing method of improvement instead.

### Keep your additions in uniform with the rest of the code
Even if it's not the most optimal, please consider doing this. If you're adding commands, UI elements or IPC methods in addition to the existing ones, please ensure that they are coded similar to existing ones. If you're adding new functions, please ensure that they are working similar to existing ones.

### No configuration resets
Users should not experience any partial or full configuration resets upon updating.

## Most welcomed changes
Please feel free to:
- Correct spelling mistakes
- Adjust UI components sizes if you find out that they are not properly rendered on some screens

## Relicensing
By submitting a pull request to this repository, you allow NightmareXIV to re-license your commits that were submitted to this repository. Re-licensing, in this case, means modifying and distributing your changes under a new license.
