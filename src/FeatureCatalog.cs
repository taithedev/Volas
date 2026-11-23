using System;
using UnityEngine;

namespace Volas
{
    internal static class FeatureCatalog
    {
        internal static void Register(VolasPlugin plugin)
        {
            var registry = plugin.Features;
            float oldVolume = AudioListener.volume;
            int oldFrameRate = Application.targetFrameRate;
            int oldVsync = QualitySettings.vSyncCount;

            registry.Add(new Feature(
                "speed", "Movement", "Speed boost", "Local 40% velocity cap boost.",
                p => { p.Movement.RequireLocomotionFields(); p.Movement.SpeedBoost = true; },
                p => p.Movement.SpeedBoost = false,
                needsModdedRoom: true));

            registry.Add(new Feature(
                "jump", "Movement", "High jump", "Higher local jump power.",
                p => { p.Movement.RequireLocomotionFields(); p.Movement.HighJump = true; },
                p => p.Movement.HighJump = false,
                needsModdedRoom: true));

            registry.Add(new Feature(
                "gravity", "Movement", "Float gravity", "Reduces effective gravity on your own Rigidbody.",
                p => { p.Movement.RequireMovementPlayer(); p.Movement.LowGravity = true; },
                p => p.Movement.LowGravity = false,
                needsModdedRoom: true));

            registry.Add(new Feature(
                "fly", "Movement", "Hand flight", "Hold right B in solo or MODDED rooms to fly in hand direction.",
                p => { p.Movement.RequireMovementPlayer(); p.Movement.Fly = true; },
                p => p.Movement.Fly = false,
                needsModdedRoom: true));

            registry.Add(new Feature(
                "orbs", "Visuals", "Hand orbs", "Render bright local orbs at both hands.",
                p => p.Effects.Orbs = true,
                p => p.Effects.Orbs = false));

            registry.Add(new Feature(
                "rainbow_orbs", "Visuals", "Rainbow hands", "Cycle the hand orb colors.",
                p => p.Effects.RainbowOrbs = true,
                p => p.Effects.RainbowOrbs = false));

            registry.Add(new Feature(
                "trails", "Visuals", "Hand trails", "Draw fading trails behind both hands.",
                p => p.Effects.Trails = true,
                p => p.Effects.Trails = false));

            registry.Add(new Feature(
                "lights", "Visuals", "Palm lights", "Attach local point lights to both hands.",
                p => p.Effects.HandLights = true,
                p => p.Effects.HandLights = false));

            registry.Add(new Feature(
                "laser", "Visuals", "Pointer beam", "Draw a raycast-based local right hand beam.",
                p => p.Effects.Laser = true,
                p => p.Effects.Laser = false));

            registry.Add(new Feature(
                "halo", "Visuals", "Head halo", "Show a local light-colored ring above the headset.",
                p => p.Effects.Halo = true,
                p => p.Effects.Halo = false));

            registry.Add(new Feature(
                "breadcrumb", "Visuals", "Path markers", "Leave a temporary trail of local glowing spheres.",
                p => p.Effects.Breadcrumbs = true,
                p => p.Effects.Breadcrumbs = false));

            registry.Add(new Feature(
                "fps_hud", "Telemetry", "FPS HUD", "Show smoothed frames per second in VR and on desktop.",
                p => p.Effects.FpsHud = true,
                p => p.Effects.FpsHud = false));

            registry.Add(new Feature(
                "clock_hud", "Telemetry", "Clock HUD", "Display the local 24-hour time.",
                p => p.Effects.ClockHud = true,
                p => p.Effects.ClockHud = false));

            registry.Add(new Feature(
                "pos_hud", "Telemetry", "Position HUD", "Show your current X / Y / Z location.",
                p => p.Effects.PositionHud = true,
                p => p.Effects.PositionHud = false));

            registry.Add(new Feature(
                "heading_hud", "Telemetry", "Heading HUD", "Show camera yaw in degrees.",
                p => p.Effects.HeadingHud = true,
                p => p.Effects.HeadingHud = false));

            registry.Add(new Feature(
                "crosshair", "Telemetry", "PC crosshair", "Show a simple non-VR crosshair in the middle of the game window.",
                p => p.Effects.DesktopCrosshair = true,
                p => p.Effects.DesktopCrosshair = false));

            registry.Add(new Feature(
                "mute", "Utilities", "Mute audio", "Mute local game audio, restoring the prior volume when disabled.",
                p => { oldVolume = AudioListener.volume; AudioListener.volume = 0f; },
                p => AudioListener.volume = oldVolume));

            registry.Add(new Feature(
                "cap90", "Utilities", "90 FPS target", "Request a local 90 FPS application cap (VR runtime may override).",
                p => { oldFrameRate = Application.targetFrameRate; Application.targetFrameRate = 90; },
                p => Application.targetFrameRate = oldFrameRate));

            registry.Add(new Feature(
                "vsync", "Utilities", "VSync off", "Disable desktop VSync locally (VR runtime may override).",
                p => { oldVsync = QualitySettings.vSyncCount; QualitySettings.vSyncCount = 0; },
                p => QualitySettings.vSyncCount = oldVsync));

            registry.Add(new Feature(
                "copy_pos", "Utilities", "Copy coordinates", "Copy your current X,Y,Z position to the Windows clipboard.",
                p =>
                {
                    var point = p.Game.Position;
                    GUIUtility.systemCopyBuffer = point.x.ToString("F2") + ", " +
                        point.y.ToString("F2") + ", " + point.z.ToString("F2");
                },
                actionOnly: true));

            registry.Add(new Feature(
                "room_status", "Utilities", "Log room status", "Record the active room name/player count in BepInEx log.",
                p => p.Log.LogInfo("Volas room status: " + p.Game.RoomSummary()),
                actionOnly: true));

            registry.Add(new Feature(
                "compact", "Settings", "Compact VR menu", "Scale the in-game VR menu down slightly.",
                p => p.Compact = true, p => p.Compact = false));

            registry.Add(new Feature(
                "rainbow_theme", "Settings", "Rainbow accent", "Cycle the OLED menu's accent color.",
                p => p.RainbowTheme = true, p => p.RainbowTheme = false));

            registry.Add(new Feature(
                "desktop", "Settings", "Toggle PC panel", "Open or close the F6 desktop menu.",
                p => p.DesktopOpen = !p.DesktopOpen, actionOnly: true));

            registry.Add(new Feature(
                "reset", "Settings", "Disable all mods", "Turn every active feature off, restoring changed settings.",
                p => p.Features.DisableAll(), actionOnly: true));
        }
    }
}
