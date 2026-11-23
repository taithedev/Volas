# Build and runtime troubleshooting

## Missing assembly references

Install Gorilla Tag PCVR and BepInEx 5 Mono, then use `build.ps1 -GamePath "FULL_PATH_TO_GORILLA_TAG" -Install`. The project assumes `Gorilla Tag_Data/Managed` and `BepInEx/core/BepInEx.dll` exist in the selected directory.

## Build errors in Visual Studio

Select **Release** configuration and confirm the .NET Framework 4.7.2 targeting pack is available. Build Solution writes `Volas.dll` to `bin/Release/net472/`; a build does not automatically install it unless `-p:InstallToGame=true` or the PowerShell installer was used. Check the first error in the Output / Error List; resolve missing assemblies before investigating later compiler errors.

## No menu

Ensure `Volas.dll` is in `Gorilla Tag/BepInEx/plugins`. Launch PCVR and press **F6** for desktop UI or left-controller **Y** for VR UI. Inspect `BepInEx/LogOutput.log` for `Volas loaded` or an exception.

## Feature not activating

Movement mods require a recognized MODDED room or solo state and the expected Gorilla Tag player/runtime fields. If the game updates a player field, `GameBridge.cs` and `MovementEffects.cs` may need updating. No mod can be guaranteed compatible before in-game tests.

## Layout or shortcuts

Edit `BepInEx/config/com.volas.gtag.cfg` after quitting the game to change bindings, menu scale, accent, or reset saved window coordinates. The desktop menu also provides accent and scale sliders. Press **End** to switch all features off.
