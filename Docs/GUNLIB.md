# Volas v1.3 GunLib — local VR pointer system

GunLib is a modular client-side VR pointer and effect library, not a player-targeting or damage system. No Photon RPCs are sent. It never alters other players' objects.

## Input

- Hold **grip** on the configured right or left controller to show the pointer.
- Press **trigger** to activate enabled effects on a targeted static surface.
- Open the desktop menu with **F6** and choose **Gun Mods** for customization.
- Press **End** to disable all active mods and clear decorative effects.

The aim transform defaults to the controller **-up** direction, with an option for **+forward**. The headset/controller game runtime must expose compatible hand transforms.

## Appearance controls

- **Beam**: Straight, Wave, Spiral
- **Reticle**: Orb, Block, Ring, Diamond
- **Visuals**: soft glow, rainbow cycling, pulsing width, animation speed
- **Geometry**: beam width, pointer range, reticle diameter
- **Input**: right/left controller, alternate aim axis
- **Stamps**: size and automatic expiry time
- **Presets**: OCEAN (cyan ring), NEON (magenta spiral), PRISM (rainbow wave)

Preferences persist in `BepInEx/config/com.volas.gtag.cfg`.

## Twenty effect modes

**Original 14:** Marker, Paint, Beacon, Ring, Pulse, Confetti, Trail, Inspect, Ruler, Range, Coordinates, Spotlight, Grapple, Dash.

**New decorative 6:** Surface Grid (local aligned grid), Spiral Stamp, Arrow Marker, VOLAS Sign, Sparkle Burst, Checkerboard Stamp.

Surface decorations have bounded lifetimes and are cleared with reset. Grapple and dash affect only your own Rigidbody and require solo or a recognized MODDED room. This does not grant permission to use the mod in public matchmaking.

## Code

- `GunLib/GunLib.cs`: `GunTarget`, `GunLib.Instance`, `Fired` event, targeting, color and reticle renderer.
- `Features/Guns/GunFeatures.cs`: 20 feature toggles.
- `Features/Guns/GunEffects.cs`: 14 existing pointer modes.
- `Features/Guns/CreativeGunEffects.cs`: six additional temporary decorative stamps.
- `Settings/VolasConfiguration.cs`: settings with input bounds.
- `UI/Desktop/DesktopMenu.cs`: pointer appearance editor.

This is source-only until it builds with a specific installation of Gorilla Tag and BepInEx 5 Mono. A successful C# compilation does not guarantee game or headset compatibility.

## v1.8.1 pointer recovery

Enable Projectiles > Pointer preview, close the menu, then hold the selected controller's grip. GunLib uses GTPlayer hand wrappers (controllerTransform), legacy transform fields, the controller input poller and Unity XR device fallback. Trigger callbacks run once per new press on a valid stationary surface; existing hold-trigger tools still use TriggerHeld. Beam and reticle objects are reused and hidden on release. The supplied example inspired the input and lifecycle design; Volas does not copy its per-frame GameObject creation or game rig/holdable manipulation. No game network messages are added. Test both hands in VR after rebuilding.
