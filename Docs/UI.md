# Volas desktop UI (reference-matched)

## Desktop

The desktop UI is implemented entirely in `UI/Desktop/DesktopMenu.cs` and opens with **F6** inside Gorilla Tag PCVR. It is not a separate executable.

The desktop UI now tracks the supplied Untitled reference: compact violet logo, small gray version label, understated search, close and settings icons, tiny horizontally scrollable tabs, a nearly black rounded panel and a flat three-column Home grid. Home entries are arranged in the same columns and order as the image. The bottom of Home stays uncluttered. Other pages use the same palette, subtle rows, favorite stars and small violet switches. The categories map to real Volas features: **Projectiles → Gun Mods**, **Fun → Utilities**, **Players → Telemetry**, and **VR Rig → Settings**. Screenshot-only headings without registered modules show an honest empty state rather than fake buttons.

For the closest proportions, the default window is 760 × 500 pixels. Older installations may have persisted another window size in BepInEx settings; resizing is available from the lower-right corner. F6 toggles the panel. Font rendering depends on Unity's built-in font, so pixel-perfect typography cannot be guaranteed on every display.

The interface uses Unity IMGUI so it does not depend on Unity UI prefabs, TextMeshPro, asset bundles, external fonts or shaders. Real backdrop blur is *not* present; IMGUI cannot reproduce that exact screenshot effect without a custom rendering solution.

## VR

The in-game menu in `UI/VR/VrMenu.cs` is a physical tablet near the left hand, matching the handheld-menu references in composition without copying their assets. It has a 3D back plate, white outline, tactile button meshes and world-space labels. It initially opens to **Categories**; select a category to view its mods. **HOME**, category arrows, page navigation and close are all physical buttons.

Use left Y to toggle. Bring the right hand/controller near a button to touch, or point at a nearby button and press the right trigger. Contacts are debounced so holding a finger against a navigation button does not continuously change pages.

The tablet's position follows the left controller and rotates toward the headset so it stays readable. Desktop → Settings exposes the **VR Size** and **Wrist Left/Right / Up/Down / Forward** sliders for adjustment. The Settings values persist in BepInEx.

## Theme

The PC menu uses graphite-black translucent surfaces, low-contrast gray text and a restrained purple active-tab / switch accent. The GunLib visual effects can still use their optional independent rainbow styling; this does not make the desktop layout purple or import Untitled themes.

## Building

The GUI source is part of the same `Volas.dll` as every other mod. `Volas.csproj` includes `UI/**/*.cs`, along with all the other source folders. To compile, pull the latest branch, open `Volas.sln` through **File → Open → Project/Solution**, configure the local PC Gorilla Tag/BepInEx references, and run `build.bat` or Visual Studio Build Solution.

**The new interface has not been compiled or play-tested against the user's local game DLLs in this session.** If the build fails, capture the first untruncated error from the Visual Studio Build Output window.
