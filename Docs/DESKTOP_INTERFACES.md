# Desktop interfaces

- F6 (configurable): modern Unity uGUI desktop menu with grayscale palette, search, drag, resize and shared opacity.
- F5 (configurable): legacy Unity IMGUI local display/settings panel. Includes FPS, clock, position and heading HUDs, filter, opacity, width, VR scale and appearance preferences.
- F7 and the existing left-controller Y binding continue to open the physical VR menu.
- Opening either desktop interface closes the other; the legacy interface restores the previous cursor state when closed. End disables active features.
- Settings are saved through the existing BepInEx configuration. Legacy position and width are independent of the modern window.

Build and in-game validation require the game's local Unity and BepInEx assemblies. No new package references are needed. This update does not modify gameplay logic, anti-cheat, or room restrictions.
