# Volas

**Volas 1.1 (source)** — modular C# PCVR Gorilla Tag mod menu for BepInEx 5 Mono, with a dark OLED-style desktop control panel and a 3D VR hand menu. Independent implementation, not a copy of Untitled.

> **Build status:** Source is committed, but it has **not** been compiled against your installed Gorilla Tag/BepInEx assemblies or verified in-game. The feature registry has 25 functional implementations, but game-version compatibility cannot be guaranteed.

## Repository structure

```text
Volas/
├── Core/
│   ├── VolasPlugin.cs
│   ├── FeatureRegistry.cs
│   └── FeatureCatalog.cs
├── Features/
│   ├── Movement/
│   │   ├── MovementEffects.cs
│   │   └── MovementFeatures.cs
│   ├── Visuals/
│   │   ├── VisualEffects.cs
│   │   └── VisualFeatures.cs
│   ├── Telemetry/
│   │   └── TelemetryFeatures.cs
│   ├── Utilities/
│   │   └── UtilityFeatures.cs
│   └── Settings/
│       └── SettingsFeatures.cs
├── Game/
│   └── GameBridge.cs
├── UI/
│   ├── Desktop/
│   │   └── DesktopMenu.cs
│   └── VR/
│       └── VrMenu.cs
├── Settings/
│   └── VolasConfiguration.cs
├── Build/
│   └── Build-Volas.ps1
├── Docs/
│   ├── ARCHITECTURE.md
│   └── TROUBLESHOOTING.md
├── Volas.sln
├── Volas.csproj
└── build.ps1
```

## Build

Requires Windows, Gorilla Tag PCVR, BepInEx 5 Mono, and Visual Studio with .NET Framework 4.7.2 developer targeting tools.

1. Clone: `git clone https://github.com/taithedev/Volas.git`
2. Install BepInEx 5 Mono into the PC Gorilla Tag game directory.
3. Open `Volas.sln`, select **Release**, and use **Build Solution**.
4. For one-command build and install on a default Steam setup, run:

```powershell
.\build.ps1 -Install
```

For a custom Steam library:

```powershell
.\build.ps1 -GamePath "D:\SteamLibrary\steamapps\common\Gorilla Tag" -Install
```

Without `-Install`, the compiled output is `bin/Release/net472/Volas.dll`. To install manually copy it into `Gorilla Tag/BepInEx/plugins/Volas.dll`. If Unity or BepInEx library references cannot be found, set the game path explicitly.

## Controls and UI

- **F6** toggles the resizable, draggable desktop panel.
- **Left Y** toggles the VR hand menu; touch buttons with your **right hand**.
- **End** disables every active feature.
- **Right B** applies hand flight while that feature is enabled in a compatible solo/MODDED room.
- **Search** filters features in the desktop panel. Star buttons add/remove features from the **Favorites** tab; **All** displays every feature.
- Appearance settings include an OLED background, adjustable accent hue, rainbow accent, VR menu scaling, compact VR layout, and remembered desktop window dimensions/position.
- Shortcuts, favorites, layout and appearance are saved in the BepInEx config file `BepInEx/config/com.volas.gtag.cfg`.

## 25 implemented feature entries

| Area | Features |
| --- | --- |
| Movement (4) | Speed boost, high jump, float gravity, hand flight |
| Visuals (7) | Hand orbs, rainbow hands, trails, lights, pointer beam, head halo, path markers |
| Telemetry (5) | FPS HUD, clock HUD, XYZ HUD, heading HUD, desktop crosshair |
| Utilities (5) | Mute game audio, 90 FPS target, disable VSync, copy coordinates, log room status |
| Settings (4) | Compact menu, rainbow theme, toggle desktop panel, disable all features |

Movement adjustments are local only, disabled by default, and restricted by the code to solo or rooms explicitly identified as MODDED by Photon properties. The menu does not implement anti-ban, mass tagging, report bypass, or player disruption.

Read [architecture](Docs/ARCHITECTURE.md) and [troubleshooting](Docs/TROUBLESHOOTING.md) for extending the project and diagnosing version-specific issues.
