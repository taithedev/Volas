# Volas desktop UI (reference-matched)

## Desktop

The desktop UI is implemented entirely in `UI/Desktop/DesktopMenu.cs` and opens with **F6** inside Gorilla Tag PCVR. It is not a separate executable.

The desktop UI now tracks the supplied Untitled reference: compact violet logo, small gray version label, understated search, close and settings icons, tiny horizontally scrollable tabs, a nearly black rounded panel and a flat three-column Home grid. Home entries are arranged in the same columns and order as the image. The bottom of Home stays uncluttered. Other pages use the same palette, subtle rows, favorite stars and small violet switches. The categories map to real Volas features: **Projectiles → Gun Mods**, **Players → Telemetry**, **Fun → Fun**, and **VR Rig → VR Rig**. Settings now has independent uGUI appearance controls and a linked Gun Studio. Screenshot-only headings without registered modules show an honest empty state rather than fake buttons.

For the closest proportions, the default window is 760 × 500 pixels. Older installations may have persisted another window size in BepInEx settings; resizing is available from the lower-right corner. F6 toggles the panel. Font rendering depends on the game's Unity font availability, so identical typography across game versions cannot be guaranteed. Window resizing proportionally scales all contents.

The interface uses programmatically constructed **Unity uGUI**: Canvas, CanvasScaler, Image, Text, Button, InputField, ScrollRect, Slider, RectMask2D, EventSystem and pointer-drag handlers. No IMGUI call draws the desktop window. The UI creates rounded 9-slice sprites at runtime and uses only the game's local Unity UI assemblies. True background blur is not currently present; translucent layers approximate the reference.

## VR

The in-game menu in `UI/VR/VrMenu.cs` is a physical tablet near the left hand, matching the handheld-menu references in composition without copying their assets. It has a 3D back plate, white outline, tactile button meshes and world-space labels. It initially opens to **Categories**; select a category to view its mods. **HOME**, category arrows, page navigation and close are all physical buttons.

Use left Y to toggle. Bring the right hand/controller near a button to touch, or point at a nearby button and press the right trigger. Contacts are debounced so holding a finger against a navigation button does not continuously change pages.

The tablet's position follows the left controller and rotates toward the headset so it stays readable. Desktop → Settings exposes the **VR Size** and **Wrist Left/Right / Up/Down / Forward** sliders for adjustment. The Settings values persist in BepInEx.

## Theme

The PC menu uses graphite-black translucent surfaces, low-contrast gray text, compact category rows and a restrained violet active-tab / action accent. Settings adjusts panel opacity, and Gun Studio edits live pointer configuration. The GunLib visual effects can still use their optional independent rainbow styling; this does not make the desktop layout purple or import Untitled themes.

## Building

The GUI source is part of the same `Volas.dll` as every other mod. `Volas.csproj` includes `UI/**/*.cs`, along with all the other source folders. To compile, pull the latest branch, open `Volas.sln` through **File → Open → Project/Solution**, configure the local PC Gorilla Tag/BepInEx references, and run `build.bat` or Visual Studio Build Solution.

**The new interface has not been compiled or play-tested against the user's local game DLLs in this session.** If the build fails, capture the first untruncated error from the Visual Studio Build Output window.

## Features and limitations

The project currently registers **72** feature entries: 55 original entries plus 17 local movement, visual, fun and safety entries. GunLib uses the selected controller **grip to aim and trigger to fire**. Solo/MODDED room checks are applied to local locomotion changes. Lobby-wide effects and moderation evasions are intentionally absent. Local build against UnityEngine.UI.dll and UnityEngine.UIModule.dll, plus in-game verification, are required before calling this a working binary.
