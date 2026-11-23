# Volas build troubleshooting (v1.3)

## Open the Visual Studio solution, not Folder View

Your screenshot shows **Solution Explorer — Folder View**. In Visual Studio choose **File → Open → Project/Solution → Volas.sln** to get **Build → Build Solution**. Select **Release | Any CPU** and press **Ctrl+Shift+B**.

You can also simply run `build.bat` from the repository. For auto-install use `build.bat -Install`. The helper stores the game directory in `Volas.Local.props` (not committed to Git) so future Visual Studio builds can find it.

## CS0012: MonoBehaviour is defined in an assembly that is not referenced

This is **not caused by the presence or absence of `src/`**. It means the compiler cannot resolve a required Unity assembly reference or is mixing incompatible Unity/BepInEx runtime assemblies.

The v1.3 project explicitly compiles all source folders and links the installed game's **UnityEngine*.dll** modules as well as `BepInEx.dll`. The earlier project accidentally skipped `GunLib/`; that is also fixed.

Try these in order:

1. Pull current `main` in Visual Studio (**Git → Pull**). Open `Volas.sln`, not a folder.
2. Close the game. Run `build.bat` to locate the installed game folder. If not found, pass `-GamePath "FULL_PATH_TO_GAME"`.
3. Ensure **BepInEx 5 Mono**, not an IL2CPP-only loader, matches this installation.
4. If automatic targeting fails, run `build.bat -Framework netstandard2.1`; alternatively `build.bat -Framework net472` if compatible.
5. In Visual Studio **Build → Clean Solution**, then **Rebuild Solution**. You may delete `bin/` and `obj/` if they contain stale outputs.
6. Read the **full first CS0012 line** in Output; its named assembly is the precise file that must be reconciled, usually `UnityEngine.CoreModule`, `UnityEngine`, or `netstandard`.

The script tries both supported target frameworks automatically unless you force one. A failed local build cannot be diagnosed completely from a truncated error line.

## Missing reference DLL

The game installation must contain `Gorilla Tag_Data/Managed/UnityEngine.CoreModule.dll`, the relevant Unity engine modules, and `BepInEx/core/BepInEx.dll`. These libraries are loaded from **your own game installation**. Do not copy random versions from unrelated games into the repo.

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
