# Volas architecture

This project separates the BepInEx entry point from its menu and feature systems. There is no generic `src` directory.

- **Core:** plugin lifecycle, feature registration, state transitions, favorites, rollback and error isolation. `FeatureCatalog.cs` invokes individual category registries.
- **Features:** each feature category owns its registration file; movement and visuals have separate runtime implementations. This makes it easier to add a feature without growing a single massive catalog.
- **Game:** `GameBridge` resolves optional Gorilla Tag and Photon runtime types through reflection. Type rescans are throttled. If fields change, log an error rather than pretending the mod works.
- **UI/Desktop:** IMGUI desktop panel with drag/resize, search, favorite stars, appearance sliders and status text.
- **UI/VR:** hand-attached world-space menu rendered from Unity primitives and TextMesh, with right-hand touch buttons and paged categories.
- **Settings:** BepInEx configuration entries for hotkeys, favorites, colors, scale and desktop window position.
- **Build:** PowerShell build helper, keeping `build.ps1` at the root for backward compatibility.

## Adding a feature

1. Decide which category it belongs in, for example `Features/Visuals/VisualFeatures.cs`.
2. Register a uniquely named `Feature` using real enable/disable delegates.
3. Keep runtime objects and cleanup inside the corresponding effects service.
4. Test enabling, disabling, resetting, reconnecting and leaving MODDED rooms.
5. For a game movement effect, use `needsModdedRoom: true`; avoid public-room disruption.

`FeatureRegistry` centrally handles toggles, favorites, error reporting, and room compatibility. Changing tabs, favorites, or feature toggles increments its revision so the VR menu can redraw.

## Platform constraints

Volas targets .NET Framework 4.7.2 and references BepInEx 5 and Unity engine DLLs directly from the installed PC game. The repository does not ship those proprietary binaries. A successful local build does not imply the features have been tested against every game update or headset.
