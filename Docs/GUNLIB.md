# Volas GunLib

Volas 1.2 provides its own **local-only VR pointer library** in `GunLib/GunLib.cs`. It does not copy any third-party GunLib project and does not send Photon RPCs, alter hit colliders, or interact with other players' accounts.

## Controls

- **Right grip:** aim with the pointer. The active mods must include at least one entry in the Gun Mods category.
- **Right trigger:** execute any active click-based pointer mods. Hold it for trail drawing or grapple.
- **Left Y:** show the VR hand menu.
- **F6:** open the desktop menu and use the Gun Mods category to change range, reticle diameter, or aim-axis mode.
- **End:** disable all Volas mods and clear locally spawned gun effects.

The pointer's default axis is **-up** from the right-hand controller transform. Enable **Aim from controller forward axis** in the desktop menu if your controller requires **+forward** instead.

## Features

| Name | Behavior | Lifetime/scope |
| --- | --- | --- |
| Marker gun | Places a small luminous local marker on a surface | Temporary |
| Paint gun | Places a colored disc on the surface | Temporary |
| Beacon gun | Places a tall local light marker | Temporary |
| Ring gun | Draws a circle along the target normal | Temporary |
| Pulse gun | Draws an animated expanding ring | Temporary |
| Confetti gun | Adds small colored local spheres on activation | Temporary |
| Trail gun | Paints tiny local dots while trigger is held | Temporary |
| Inspect gun | Displays the collider's local object name and layer | While aiming |
| Ruler gun | Two trigger presses measure distance between surfaces | Temporary |
| Range finder | Displays distance to targeted surface | While aiming |
| Coordinate gun | Displays the targeted X/Y/Z location | While aiming |
| Spotlight gun | Illuminates the targeted surface locally | While aiming |
| Grapple gun | Pulls your own Rigidbody toward the targeted surface | Solo/recognized MODDED room |
| Dash gun | Pushes your own Rigidbody along the aim direction | Solo/recognized MODDED room |

Multiple pointer modes can be enabled simultaneously. Effects are local, and spawned objects use bounded counts or lifetimes. Aim does not automatically trigger a click action.

## API for future modules

The public `Volas.GunLib.Instance` is available while the plugin is running. It exposes:

- `IsAiming`, `TriggerHeld`, `FiredThisFrame`.
- `Target` (a `GunTarget` with `HasHit`, `Point`, `Normal`, `Distance`, `Direction`, and `Collider`).
- `event Action<GunTarget> Fired` for trigger-down callbacks.

New features register with `FeatureRegistry` in `Features/Guns/GunFeatures.cs`, with runtime work in `GunEffects.cs` or a separate effect service. Do not attach unmanaged objects without a reset/disposal path.

## Compatibility limits

Volas is source-only pending a successful local compile and in-game test with your Gorilla Tag build and headset. The game must expose the expected hand transforms for the VR pointer; if the pointer is backwards, change its axis in the desktop Gun Mods tab. Grapple and dash additionally require the game's player Rigidbody and MODDED/solo compatibility checks to succeed. These features may be incompatible with some game or VR-runtime versions.
