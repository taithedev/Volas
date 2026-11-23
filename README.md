# Volas

Volas is a C# PCVR Gorilla Tag mod menu built as a BepInEx 5 Mono plugin. The complete menu is ONE DLL containing Core, Features, Game, Settings, Desktop UI, and VR UI.

> The source has not been compiled here against your installed game assemblies or tested in-game. Building the project and verifying runtime compatibility are separate steps.

## Build the entire menu (Windows)

1. Clone the repo and open Volas.sln in Visual Studio.
2. Install the .NET SDK, Gorilla Tag PCVR, and BepInEx 5 Mono.
3. Run build.bat in the repo root. This locates the Steam game directory, checks required Unity/BepInEx DLLs, and builds the WHOLE Volas.sln in Release mode.
4. To also copy the built DLL into the game, open a terminal in the repo folder and run:

    .\build.bat -Install

5. The first successful script run creates Volas.Local.props (gitignored) so Visual Studio also knows the game directory. Select Release and use Build > Build Solution (Ctrl+Shift+B) to build again.

If the automatic Steam scan misses the game, give the full installed game folder:

    .\build.bat -GamePath "D:\SteamLibrary\steamapps\common\Gorilla Tag" -Install

You can alternatively set the VOLAS_GAME_DIR environment variable. The build detects netstandard2.1 vs net472 from the installed Unity runtime; override with -Framework net472 or -Framework netstandard2.1 if needed.

Output: bin/Release/<framework>/Volas.dll. With -Install, the DLL is copied to the game's BepInEx/plugins directory. If using net472, the .NET Framework 4.7.2 targeting pack may also be required.

## Controls

- F6: open/close desktop panel.
- Left-controller Y: open/close VR hand menu.
- End: disable all active features.

## Project structure

- Core: plugin, registry and feature catalog.
- Features: Movement, Visuals, Telemetry, Utilities and Settings.
- Game: game/runtime bridge.
- UI: desktop and VR menus.
- Settings: configuration.
- Build: full-solution Windows build tool.

The repository currently registers 25 features. Some movement effects are limited to solo/recognized MODDED rooms; features and game-version compatibility still require actual in-game testing.

More information: Docs/TROUBLESHOOTING.md and Docs/ARCHITECTURE.md.
