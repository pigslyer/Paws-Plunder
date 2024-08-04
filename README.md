# Paws & Plunder

# Building the project
Project is targeting .NET 8, make sure to install the SDK.

When cloning the repo for the first time, make sure to open the project in Godot Editor first, to prevent this error:
> Failed loading resource: ... Make sure resources have been imported by opening the project in the editor at least once.

## Visual Studio 2022
- In the root directory (not `src/`!) create a folder named `Properties` and inside create `launchSettings.json`.
- Copy the following settings:
```json
{
	"profiles": {
		"Godot": {
			"commandName": "Executable",
			"executablePath": "D:\\Godot_v4.2.2-stable_mono_win64.exe",
			"commandLineArgs": "--path $(ProjectDir) --verbose",
			"workingDirectory": ".",
			"nativeDebugging": true
		}
	}
}
```
- Make sure to change `executablePath` to the path where your Godot binary is located. Also either escape backslashes or use forward slashes.
- Open the `.sln` file in the root directory. Launch the profile which you've just configured ("Godot" by default). You should be able to debug (try setting breakpoints) and use IntelliSense with Godot specifics.
## JetBrains Rider
Godot support is now automatically integrated in the IDE and by opening the `.sln` file, you should get Godot Player and Editor profile automatically.

Follow the same instructions as for VS2022 to set up paths.
