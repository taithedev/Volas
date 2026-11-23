# Volas v1.4 architecture

The plugin is packaged into **one Volas.dll**. Source files stay in separate folders so the project remains maintainable:

- `Core/`: BepInEx plugin, feature registry and category composition.
- `GunLib/`: client-side VR raycaster, beam and reticle rendering, effect events.
- `Features/Guns/`: twenty pointer modes split between `GunEffects.cs` and `CreativeGunEffects.cs`, registered in `GunFeatures.cs`.
- `Features/Visuals/`: eleven earlier visual features plus six additional `VisualDecorations` (wrist rings, crown, floor circle and controller cubes).
- `Features/Movement/`: local locomotion modes with solo/MODDED room checks.
- `Features/Telemetry/`, `Features/Utilities/`, `Features/Settings/`: independent catalog sections.
- `Game/`: game type and Photon room bridge with throttled type discovery.
- `Settings/`: persistent BepInEx configuration entries with bounded values.
- `UI/Desktop/`: reference-inspired three-column black-and-white dashboard, animated tab underline, instant search, details, favorites, enabled list and opacity settings.
- `UI/VR/`: physical wrist-anchored tablet with real 3D buttons, category launcher, paging, nearby controller trigger/touch and reusable materials.
- `src/Diagnostics/`: runtime assembly identification (compiled as part of the same DLL).
- `Build/`: Windows build helper with automatic game discovery and target framework fallback.

## Compilation

`Volas.csproj` explicitly includes `Core/**/*.cs`, `Features/**/*.cs`, `Game/**/*.cs`, `GunLib/**/*.cs`, `Settings/**/*.cs`, `UI/**/*.cs` and optional `src/**/*.cs`. It references Unity DLLs from the installed PCVR game only.

When adding a new module, put it in the correct feature folder and register it through `FeatureCatalog` or an existing category registrar. Any created local GameObjects or Materials should be cleaned up when mods are disabled, on End/reset, and on plugin unload.

No code is deployed to game servers. This remains an unverified source build until locally compiled and tested.
