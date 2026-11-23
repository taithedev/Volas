# Volas

An independently built **Gorilla Tag PCVR mod menu** for **BepInEx 5 (Mono)**, written in C#. Volas includes a dark OLED-style wrist menu, a draggable/resizable desktop control panel, organized categories, a reset hotkey, and **25 implemented feature entries**. This project does **not** copy Untitled source code.

> **Status: source release / in-game verification needed.** Volas is uploaded as a complete Visual Studio project. A game-specific DLL has not been compiled or tested against the current Gorilla Tag client in this environment. Game updates, VR drivers, and the installed BepInEx version can affect compatibility. Never assume a feature is tested merely because it has a menu toggle.

## Supported platform

- Windows PC and Gorilla Tag for PCVR; not a standalone Quest APK.
- BepInEx 5 Mono (install into your own legitimate Gorilla Tag game folder).
- Visual Studio 2022 with .NET desktop development / .NET Framework 4.7.2 developer tools, or compatible dotnet CLI.
- The project references BepInEx and Unity assemblies **from your local game install**. No proprietary Gorilla Tag libraries are redistributed.

## Build and install

1. Install Gorilla Tag PCVR and **BepInEx 5 Mono**.
2. Clone the repository: `git clone https://github.com/taithedev/Volas.git`
3. Open `Volas.sln` in Visual Studio, or use the PowerShell script.
4. For a default Steam installation: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install`
5. For a custom game location: `powershell -ExecutionPolicy Bypass -File .\build.ps1 -GamePath "D:\SteamLibrary\steamapps\common\Gorilla Tag" -Install`
6. Start Gorilla Tag. The plugin goes in `Gorilla Tag/BepInEx/plugins/Volas.dll`.

Alternatively from Visual Studio Developer PowerShell:
```powershell
dotnet build Volas.csproj -c Release "-p:GorillaTagDir=D:\SteamLibrary\steamapps\common\Gorilla Tag" -p:InstallToGame=true
```

## Controls

| Control | Result |
| --- | --- |
| **Left-controller Y (secondary button)** | Show/hide VR hand menu |
| **Touch a menu item with right hand** | Toggle or execute the highlighted feature |
| **F6** | Show/hide PC control panel |
| **Drag the top of the PC window** | Move it |
| **Drag the PC window's bottom-right corner** | Resize it |
| **End** | Disable all active mods |
| **Right-controller B (secondary button)** | Apply hand flight while the feature is enabled in a supported room |

If a headset has different controller mappings, adapt the XR `secondaryButton` binding in `src/VolasPlugin.cs` and `src/MovementEffects.cs`.

## Features

| Category | Implemented entries |
| --- | --- |
| **Movement** | Speed boost, high jump, float gravity, hand flight |
| **Visuals** | Hand orbs, rainbow hands, hand trails, palm lights, pointer beam, head halo, path markers |
| **Telemetry** | VR/PC FPS HUD, clock HUD, position HUD, heading HUD, desktop crosshair |
| **Utilities** | Mute audio, 90 FPS target, VSync off, copy coordinates, log room status |
| **Settings** | Compact VR menu, rainbow accent, toggle PC panel, disable all mods |

Movement features modify your **local player only**, are **off by default**, and are restricted in code to solo or rooms whose Photon room properties explicitly identify them as **MODDED**. A normal private room is not automatically a modded room. They are not intended for public matchmaking. Local GPU, lighting, and VR-specific behavior may depend on the game/runtime.

## Architecture

- `src/VolasPlugin.cs`: BepInEx entry point, hotkeys, lifecycle.
- `src/FeatureRegistry.cs`: feature registration, state and error isolation.
- `src/FeatureCatalog.cs`: actual menu operations and descriptions.
- `src/GameBridge.cs`: runtime Gorilla Tag / Photon reflection and room checks.
- `src/MovementEffects.cs`: scoped locomotion changes with restoration.
- `src/VisualEffects.cs`: local Unity visual modules and telemetry.
- `src/VrMenu.cs`: hand-attached 3D menu with touch buttons.
- `src/DesktopMenu.cs`: draggable/resizable desktop UI.

To create a new feature, register a `Feature` in `FeatureCatalog.Register` with working enable/disable actions. Do not add empty or simulated buttons.

## Troubleshooting

- **No menu:** press **F6** or left-controller **Y**. Check `BepInEx/LogOutput.log` for `Volas loaded`.
- **Build says BepInEx not found:** set `-GamePath` to your Gorilla Tag directory and install BepInEx 5 Mono.
- **A movement feature refuses to activate:** start gameplay in solo or a recognized MODDED room; the current game build must expose the expected player fields.
- **Menu appears but visuals are missing:** inspect log and Unity shader compatibility, and confirm both hand transforms resolve.
- **Game update breaks a feature:** inspect `GameBridge.cs` for renamed player fields; the registry catches and logs feature errors instead of silently reporting success.

Volas does not implement anti-ban, report bypassing, mass tagging, disruption of other players, or game-network exploits.
