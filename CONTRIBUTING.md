# Thank you for considering contributing to this project!
## Before you proceed with pull request, please consider the following:
### Do not refactor
Even if current code is unoptimal, please avoid refactoring existing code. 
- Do not replace opcodes with hooks
- Do not replace static data with dynamic data
- Do not optimize utility functions
- Do not replace if trees with switch statements
- Do not change accessibility modifiers
- Do not replace manual ImGui.Begin calls with windows
If you know how to significantly improve something, please create an issue describing method of improvement instead.

### Keep your additions in uniform with the rest of the code
Even if it's not the most optimal, please consider doing this. If you're adding commands, UI elements or IPC methods in addition to the existing ones, please ensure that they are coded similar to existing ones. If you're adding new functions, please ensure that they are working similar to existing ones.

### If possible, keep additions contained
If you would add new functions, please create own classes for them and keep them contained. The plugin should not become dependent on added functions; the plugin's ability to work should not be harmed if added functions become unavailable and have to be disabled.

### No configuration resets
Users should not experience any partial or full configuration resets upon updating.

## Most welcomed changes
Please feel free to:
- Correct spelling mistakes
- Adjust UI components sizes if you find out that they are not properly rendered on some screens
