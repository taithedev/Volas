# Volas v1.3 — Gorilla Tag PCVR mod menu

Volas is a modular C# / BepInEx 5 Mono mod menu with a dark OLED desktop UI and in-world VR menu. **Every source folder compiles into a single Volas.dll** — including `GunLib/` and `src/`.

**Build status:** Source uploaded and statically reviewed; the DLL has **not** been compiled against your specific game or verified in-game here. Game DLLs and BepInEx are not included in the repository.

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

## 55 feature entries

| Category | Mods |
| --- | --- |
| Movement (4) | Speed boost, high jump, float gravity, hand flight |
| Visuals (17) | Hand orbs, rainbow hands, hand trails, palm lights, pointer beam, halo, path markers, headlamp, orbiting orbs, body aura, hand axes, wrist rings, pulsing wrist rings, neon crown, floor circle, rainbow floor circle, controller cubes |
| Gun Mods (20) | Marker, paint, beacon, ring, pulse, confetti, trail, inspect, ruler, range finder, coordinates, spotlight, grapple, dash, surface grid, spiral stamp, arrow marker, VOLAS sign, sparkles, checkerboard stamp |
| Telemetry (5) | FPS HUD, clock HUD, position HUD, heading HUD, desktop crosshair |
| Utilities (5) | Audio mute, 90 FPS target, VSync off, copy coordinates, log room status |
| Settings (4) | Compact VR menu, rainbow accent, desktop panel toggle, disable all |

Most features create local effects or change your own display. Locomotion-changing mods are disabled in ordinary multiplayer rooms by default; game-version compatibility may vary. No network-targeted disruption or anti-ban functions are included.

## GunLib appearance studio

GunLib is a **VR controller pointer** with configurable **three beam styles** (straight, wave, spiral), **four reticle styles** (orb, block, ring, diamond), optional glow, rainbow color, pulse animation, hand choice, beam width, range, animation speed, stamp size, and temporary effect lifetime. The desktop panel includes **OCEAN**, **NEON**, and **PRISM** appearance presets.

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
    Settings/           feature action registries
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
