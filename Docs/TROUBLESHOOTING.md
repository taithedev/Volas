# Volas build troubleshooting

## Open the Visual Studio solution, not Folder View

Your screenshot shows **Solution Explorer — Folder View**. In Visual Studio choose **File → Open → Project/Solution → Volas.sln** to get **Build → Build Solution**. Select **Release | Any CPU** and press **Ctrl+Shift+B**.

You can also simply run `build.bat` from the repository. For auto-install use `build.bat -Install`. The helper stores the game directory in `Volas.Local.props` (not committed to Git) so future Visual Studio builds can find it.

## CS0165: Use of unassigned local variable 'position'

The physical menu now initializes its camera-relative fallback position before evaluating controller tracking. This resolves the definite-assignment error without changing the normal wrist pose.

## CS0012: MonoBehaviour is defined in an assembly that is not referenced

This is **not caused by the presence or absence of `src/`**. It means the compiler cannot resolve a required Unity assembly reference or is mixing incompatible Unity/BepInEx runtime assemblies.

The project explicitly compiles every source folder and references the installed game's Unity modules and BepInEx. The current project also searches for **UnityEngine.dll** in the game Managed folder and BepInEx directories. If that facade is missing and your BepInEx assembly requires it, simply referencing CoreModule will not resolve CS0012.

Try these in order:

1. Pull current `main` in Visual Studio (**Git → Pull**). Open `Volas.sln`, not a folder.
2. Close the game. Run `build.bat` to locate the installed game folder. If not found, pass `-GamePath "FULL_PATH_TO_GAME"`.
3. Ensure **BepInEx 5 Mono**, not an IL2CPP-only loader, matches this installation.
4. Current Volas targets `netstandard2.1`. Run `build.bat` or `build.bat -Framework netstandard2.1`. Old `net472` values in local props are ignored.
5. In Visual Studio **Build → Clean Solution**, then **Rebuild Solution**. You may delete `bin/` and `obj/` if they contain stale outputs.
6. Run `powershell -ExecutionPolicy Bypass -File .\\Build\\Diagnose-References.ps1 -GamePath "FULL_PATH_TO_GAME"` to see which **UnityEngine.dll** and uGUI DLLs exist.
7. Read the **full first CS0012 line** in Output; its named assembly is the exact dependency that must be reconciled. When the error says `UnityEngine, Version=0.0.0.0`, use a UnityEngine.dll facade from the **same compatible game/BepInEx environment**, not a random Unity version. You can supply `build.bat -UnityEngineFacadePath "FULL_PATH_TO_UnityEngine.dll"`; the root wrapper now forwards this argument and persists it in `Volas.Local.props`. The build checks the DLL assembly identity, reports the BepInEx expected UnityEngine identity when available, and stops early if the facade is missing rather than producing repeated CS0012 errors.

The script builds against the .NET Standard 2.1 SDK reference pack. A failed local build cannot be diagnosed completely from a truncated error line.

## Missing reference DLL

The game installation must contain `Gorilla Tag_Data/Managed/UnityEngine.CoreModule.dll`, the relevant Unity engine modules, and `BepInEx/core/BepInEx.dll`. These libraries are loaded from **your own game installation**. Do not copy random versions from unrelated games into the repo.

## VR menu not appearing when Y is pressed

Pull and build the latest main branch, then reinstall Volas.dll in BepInEx/plugins. Updating source alone does not update an installed DLL.

Press **F7** on your keyboard to test the physical menu independently of controller Y. Alternatively use **F6 > Settings > OPEN VR MENU / TEST**. The Settings footer reports the menu render/tracking status.

The menu now checks Gorilla Tag's ControllerInputPoller leftControllerSecondaryButton first (including SteamVR), then XR and legacy controller input. If left-hand tracking is missing, the panel appears in front of the active game camera instead of silently hiding.

Aim with your right controller and press trigger or touch the raised buttons. The selection ray is visible even over empty menu regions. You can use your mouse on the physical panel during PC preview.

If F7 does not show the menu either, read BepInEx/LogOutput.log for 'Volas physical menu rendered', 'no camera', or 'physical menu update failed'. Those messages distinguish rendering from controller input issues.

## No in-game Volas menu

Check `Gorilla Tag/BepInEx/plugins/Volas.dll`, then `BepInEx/LogOutput.log` for the Volas startup message and runtime assembly versions. Press F6 for desktop UI or left Y for VR UI.

## Pointer not visible

Enable at least one Gun Mods feature, hold grip on the selected controller, and ensure a hand transform resolves. Change controller selection or aim axis in the desktop Gun Mods panel if needed. No effects are verified in the latest game without local testing.

## Get useful build diagnostics

Open a terminal in the cloned repository and run:

```powershell
.\build.bat -GamePath "FULL_PATH_TO_GAME"
```

For a complete log, use PowerShell:

```powershell
.\build.ps1 -GamePath "FULL_PATH_TO_GAME" *> volas-build.log
```

Share the **first full error line**, framework target, and the named missing assembly to diagnose a remaining failure.


## XRSettings does not exist

`Features/Extras/LocalFeaturePack.cs` no longer references `XRSettings`. Desktop camera zoom now checks for a tracked `XRNode.Head` through `InputDevices`, avoiding an unavailable legacy symbol. Pull the latest branch so Visual Studio compiles the updated source.

## Physical VR buttons not responding

Ensure a valid left-hand controller, right-hand controller and headset are tracked. Press **left Y** to open. Aim at a menu button and press the **right trigger**, or touch a raised button using the right controller tip. Hovered buttons change shade and the pointer appears on a hit. If button text or a material fails to render, review `BepInEx/LogOutput.log`.

The physical wrist menu uses scene-created meshes, not a Unity Editor prefab, and requires no inspector wiring. The DLL must still be compiled and tested locally against the installed game version.

## CS1705: UnityEngine.AudioModule requires netstandard 2.1

Pull the latest project, close Gorilla Tag, clean the solution and run `build.bat -Install`. Current Volas targets `netstandard2.1` directly, including in Visual Studio. The SDK supplies its matching reference assembly; do not copy runtime netstandard DLLs into the project or manually add a 2.0 reference. Install a current .NET SDK if the reference pack is missing. An old `Volas.Local.props` framework setting no longer overrides the target.
