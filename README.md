# Volas v1.4 — Gorilla Tag PCVR mod menu

Volas is a modular C# / BepInEx 5 Mono mod menu with a native Unity uGUI desktop dashboard and a physical in-world VR wrist menu. **Every source folder compiles into a single Volas.dll** — including `GunLib/` and `src/`.

**Build status:** Source uploaded and statically reviewed; the DLL has **not** been compiled against your specific game or verified in-game here. Game DLLs and BepInEx are not included in the repository.

## Interface — inspired by the supplied screenshots

The desktop menu is built dynamically with Unity Canvas, Text, Image, Button, InputField, ScrollRect and an EventSystem. It recreates the reference's charcoal glass, purple logo, compact tabs, live search and flat three-column Home launcher. It deliberately uses the reference layout, but not copied Untitled artwork or branding.

**F6** opens/closes the PC menu. The header has search, a Settings shortcut, and a close button. Clicking categories opens a scrollable list of *registered* working actions, favorites, and a details panel. Desktop → Settings offers opacity, layout reset, VR compact mode and a working **Gun Studio** with beam/reticle/rainbow/hand controls. Resize from the lower-right corner or drag by the header; state is remembered via BepInEx config.

The VR menu is now a **3D OLED wrist tablet** with violet trim, raised physical button meshes, smooth left-hand anchoring, visible right-hand selection ray, proximity-touch interaction, right-trigger selection, haptics, and debounce. **Left Y** opens/closes it. Use **CATS, FAV, ON, ALL** for navigation, the row +/* control for favorites, **< / >** to page, **HOME** to return, and **RESET** to disable active features. VR scale and wrist offsets persist in BepInEx configuration.

**Appearance note:** Desktop rendering is now Unity uGUI, not IMGUI. A dark translucent panel and rounded UI textures approximate the supplied screenshot; true behind-the-panel blur and the screenshot's proprietary font/artwork are not included.

## Build fixes and reference diagnostics

The old `XRSettings` symbol has been removed from the local camera-zoom feature. The build now explicitly resolves an installed `UnityEngine.dll` facade when one exists, adds validation for uGUI DLLs, and saves the resolved reference for subsequent Visual Studio builds. If CS0012 remains because your installed BepInEx references an incompatible or missing UnityEngine assembly, the matching game/BepInEx binaries are needed; this cannot be fixed by inventing an assembly. See [Build troubleshooting](Docs/TROUBLESHOOTING.md).

## Build the entire project in Windows

1. Install PC Gorilla Tag, BepInEx **5 Mono**, and Visual Studio with the .NET desktop workload and the latest .NET SDK.
2. Clone the repository using Git, or in Visual Studio **Git → Clone Repository**.
3. In Visual Studio, choose **File → Open → Project/Solution** and open `Volas.sln`. Don't use *Open Folder* if you want the usual **Build Solution** command.
4. To detect the game installation and save its location for Visual Studio, double-click `build.bat` once. It creates the gitignored `Volas.Local.props`.
5. Select **Release**, **Any CPU**, then **Build → Build Solution** (`Ctrl+Shift+B`).
6. For a full build and automatic installation to the game, run `build.bat -Install`.

For a different Steam library, use:

```powershell
.\build.bat -GamePath "D:\SteamLibrary\steamapps\common\Gorilla Tag" -Install
```

The build helper automatically tries `netstandard2.1` / `net472` depending on installed Unity assemblies, with a fallback when no explicit framework is forced. You can force either using `-Framework netstandard2.1` or `-Framework net472`.

Output: `bin\Release\<framework>\Volas.dll` — with `-Install`, it is copied into `Gorilla Tag\BepInEx\plugins\`. See [Build troubleshooting](Docs/TROUBLESHOOTING.md) for the CS0012 MonoBehaviour diagnostic from Visual Studio.

## 72 registered feature entries

| Category | Mods |
| --- | --- |
| Movement (4) | Speed boost, high jump, float gravity, hand flight |
| Visuals (17) | Hand orbs, rainbow hands, hand trails, palm lights, pointer beam, halo, path markers, headlamp, orbiting orbs, body aura, hand axes, wrist rings, pulsing wrist rings, neon crown, floor circle, rainbow floor circle, controller cubes |
| Gun Mods (20) | Marker, paint, beacon, ring, pulse, confetti, trail, inspect, ruler, range finder, coordinates, spotlight, grapple, dash, surface grid, spiral stamp, arrow marker, VOLAS sign, sparkles, checkerboard stamp |
| Telemetry (5) | FPS HUD, clock HUD, position HUD, heading HUD, desktop crosshair |
| Utilities (5) | Audio mute, 90 FPS target, VSync off, copy coordinates, log room status |
| Settings (4) | Compact VR menu, rainbow accent, desktop panel toggle, disable all |\n| Local additions (17) | Checkpoint save/return, unstuck, momentum stop, grip boost/glide, PC strafe assist, head marker, two hand markers, desktop camera zoom, local confetti, room information, panic button, clear gun effects, disable movement/visual mods |

Most features create local effects or change your own display. Locomotion-changing mods are disabled in ordinary multiplayer rooms by default; game-version compatibility may vary. No network-targeted disruption or anti-ban functions are included.

## GunLib appearance studio

GunLib is a **VR controller pointer** with configurable **three beam styles** (straight, wave, spiral), **four reticle styles** (orb, block, ring, diamond), optional glow, rainbow color, pulse animation, hand choice, beam width, range, animation speed, stamp size, and temporary effect lifetime. The uGUI **Gun Studio** exposes the existing beam and reticle style configuration, rainbow mode, glow, range and controller hand options.

Controls: hold selected-hand **grip** to aim, press **trigger** to activate enabled pointer effects. All effects remain client-side. See [GunLib controls and details](Docs/GUNLIB.md).

## Project layout

```text
Volas/
  Core/                 plugin lifecycle, registry, feature catalog
  Features/
    Guns/               GunFeatures, GunEffects, CreativeGunEffects
    Movement/           scoped local locomotion modes
    Visuals/            standard effects, decorations, visual registries
    Telemetry/          FPS, position, clock
    Utilities/          audio, app/display utilities
    Settings/           feature action registries\n    Extras/             additional local-only effects and safety actions
  GunLib/               shared VR raycaster, effects-agnostic pointer
  Game/                 runtime game adapter
  UI/
    Desktop/            resizable OLED control panel + pointer studio
    VR/                 hand menu
  Settings/             persistent BepInEx configuration
  src/Diagnostics/      runtime assembly version reporting
  Build/                full solution build script
  Docs/                 build and extension guides
  Volas.sln
  Volas.csproj
  build.bat
```

No proprietary game DLLs are distributed. The Visual Studio project links to **your installed Unity and BepInEx game files**.

## Not included

Names in third-party menus are not proof that their implementations work on your game version. Volas does **not** implement lobby-wide lag/deafen/kick effects, report/moderation bypasses, account/rank spoofing, or unsupported network-wide actions. Any screenshot category with no registered features displays an empty state instead of a fake toggle. This is source code only until compiled and tested on your installed Gorilla Tag version.
