# Troubleshooting Volas

## Building the entire menu

Open Volas.sln and select Release, then Build > Build Solution (Ctrl+Shift+B). There is only one C# project, which explicitly includes all .cs files under Core, Features, Game, Settings, and UI. The result is one Volas.dll.

First run build.bat at least once to auto-detect the Steam installation and save the path for Visual Studio in Volas.Local.props. Use build.bat -Install to build and copy the plugin into BepInEx/plugins.

## Cannot find Gorilla Tag

Use build.bat -GamePath "FULL_PATH_TO_GORILLA_TAG". The path must be the directory containing Gorilla Tag_Data, not the Managed directory. The script also scans Steam libraryfolders.vdf and Steam/SteamLibrary folders on available drives.

## BepInEx 5 / Unity assemblies missing

The build uses the DLLs from your own installed game. It checks BepInEx/core/BepInEx.dll and required UnityEngine module DLLs in Gorilla Tag_Data/Managed before invoking the compiler. Install the correct BepInEx 5 Mono loader and verify the game path.

## Compiler errors

For Unity/BepInEx reference errors, read the FIRST compiler error in the build output. Newer Unity Mono installs with netstandard.dll default to netstandard2.1; other installs default to net472. Try -Framework net472 or -Framework netstandard2.1 only if needed for the specific game's assemblies. The net472 target may require the .NET Framework 4.7.2 targeting pack.

## DLL built but menu not showing

Use build.bat -Install or copy bin/Release/<framework>/Volas.dll into Gorilla Tag/BepInEx/plugins. Start Gorilla Tag on PC, press F6 for desktop menu or Y on left controller for VR menu. Check BepInEx/LogOutput.log for a Volas load error. A successful compiler run does not prove runtime compatibility.
