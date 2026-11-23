# Volas v1.4 UI

## Desktop

The desktop UI is implemented entirely in `UI/Desktop/DesktopMenu.cs` and opens with **F6** inside Gorilla Tag PCVR. It is not a separate executable.

The layout uses the supplied first reference: a wide translucent black panel, white typography, small top navigation, search and a three-column Home dashboard. The Home columns are **Categories**, **Favorites** and **Enabled**. Click a category to browse; click a mod row to toggle it; star individual mods to save favorites. Each mod category has a details panel on the right. **Gun Mods** also exposes pointer appearance and geometry settings, and **Settings** exposes desktop transparency and VR wrist controls.

The interface uses Unity IMGUI so it does not depend on Unity UI prefabs, TextMeshPro, asset bundles, external fonts or shaders. Real backdrop blur is *not* present; IMGUI cannot reproduce that exact screenshot effect without a custom rendering solution.

## VR

The in-game menu in `UI/VR/VrMenu.cs` is a physical tablet near the left hand, matching the handheld-menu references in composition without copying their assets. It has a 3D back plate, white outline, tactile button meshes and world-space labels. It initially opens to **Categories**; select a category to view its mods. **HOME**, category arrows, page navigation and close are all physical buttons.

Use left Y to toggle. Bring the right hand/controller near a button to touch, or point at a nearby button and press the right trigger. Contacts are debounced so holding a finger against a navigation button does not continuously change pages.

The tablet's position follows the left controller and rotates toward the headset so it stays readable. Desktop → Settings exposes the **VR Size** and **Wrist Left/Right / Up/Down / Forward** sliders for adjustment. The Settings values persist in BepInEx.

## Theme

The default Volas interface is grayscale: graphite black, soft gray, bright white, with white selected states. The GunLib visual effects can still use their optional independent rainbow styling; this does not make the desktop layout purple or import Untitled themes.

## Building

The GUI source is part of the same `Volas.dll` as every other mod. `Volas.csproj` includes `UI/**/*.cs`, along with all the other source folders. To compile, pull the latest branch, open `Volas.sln` through **File → Open → Project/Solution**, configure the local PC Gorilla Tag/BepInEx references, and run `build.bat` or Visual Studio Build Solution.

**The new interface has not been compiled or play-tested against the user's local game DLLs in this session.** If the build fails, capture the first untruncated error from the Visual Studio Build Output window.
